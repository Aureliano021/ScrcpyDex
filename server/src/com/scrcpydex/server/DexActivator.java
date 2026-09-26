package com.scrcpydex.server;

import java.io.BufferedReader;
import java.io.IOException;
import java.io.InputStreamReader;
import java.io.OutputStream;
import java.lang.reflect.InvocationHandler;
import java.lang.reflect.Method;
import java.lang.reflect.Proxy;
import java.net.DatagramPacket;
import java.net.DatagramSocket;
import java.net.InetAddress;
import java.net.ServerSocket;
import java.net.Socket;
import java.nio.charset.StandardCharsets;
import java.util.concurrent.CountDownLatch;
import java.util.concurrent.Executors;
import java.util.concurrent.ScheduledExecutorService;
import java.util.concurrent.TimeUnit;
import java.util.concurrent.atomic.AtomicBoolean;
import java.util.concurrent.atomic.AtomicInteger;

import com.scrcpydex.server.wrappers.DisplayManager;

/**
 * Ativador do Samsung DeX nativo via Loopback Miracast (127.0.0.1).
 * 
 * Este módulo executa a engenharia reversa fundamental descoberta no projeto:
 * instrui o subsistema de WiFi Display da Samsung a conectar-se ao próprio
 * dispositivo em localhost, ativando as flags FLAG_WIRELESS_DEX_DISPLAY (0x4000000)
 * e FLAG_EXTERNAL_DEX_HOSTING (0x20000) no system_server sem root e sem Wi-Fi.
 */
public class DexActivator {
    private static final int RTSP_PORT = 7236;
    private static final String DISPLAY_NAME = "ScrcpyDeX";
    private static final String DUMMY_MAC = "00:11:22:33:44:55";

    private final DisplayManager displayManager;
    private final AtomicBoolean running = new AtomicBoolean(false);
    private final CountDownLatch rtspPlayCompleted = new CountDownLatch(1);

    private int rtpVideoPort;
    private int rtpAudioPort;
    private Socket rtspSocket;

    public DexActivator(DisplayManager displayManager) {
        this.displayManager = displayManager;
    }

    /**
     * Inicia a ativação completa do Samsung DeX.
     * 
     * @return true se o handshake RTSP completou com PLAY OK
     * @throws Exception se ocorrer falha irrecuperável
     */
    public boolean activate() throws Exception {
        Ln.i("Iniciando ativação do motor Samsung DeX via loopback local...");

        // 1. Limpeza de sessões prévias e garantia de liberação de socket
        displayManager.disconnectWifiDisplay();
        waitForPortFree(RTSP_PORT, 5000);

        // 2. Alocação de portas dinâmicas para dreno de RTP dummy
        this.rtpVideoPort = allocateUdpPort();
        this.rtpAudioPort = allocateUdpPort();
        Ln.d("Portas RTP dinâmicas alocadas: Vídeo=" + rtpVideoPort + ", Áudio=" + rtpAudioPort);

        startUdpDrain(rtpVideoPort);
        startUdpDrain(rtpAudioPort);

        // 3. Montar configuração SemWifiDisplayConfig via reflexão
        Object config = buildSemWifiDisplayConfig(rtpVideoPort, rtpAudioPort);

        // 4. Criar Proxy dinâmico para IWifiDisplayConnectionCallback
        Object connectionCallback = createWifiDisplayCallback();

        // 5. Disparar conexão IPC no DisplayManagerService da Samsung
        displayManager.connectWifiDisplayWithConfig(config, connectionCallback);
        Ln.i("Comando IPC connectWifiDisplayWithConfig disparado ao system_server.");

        // 6. Conectar ao socket RTSP que o RemoteDisplay abre em 127.0.0.1:7236
        this.rtspSocket = connectRtspLoopback(RTSP_PORT, 6000);
        if (rtspSocket == null) {
            Ln.e("Falha crítica: o RemoteDisplay do Android não abriu a porta " + RTSP_PORT);
            displayManager.disconnectWifiDisplay();
            return false;
        }

        running.set(true);

        // 7. Iniciar loop de handshake RTSP em thread dedicada
        Thread rtspThread = new Thread(this::runRtspStateMachine, "ScrcpyDeX-RTSP");
        rtspThread.setDaemon(true);
        rtspThread.start();

        // 8. Aguardar sinal de conclusão do PLAY (DeX ativo)
        boolean ready = rtspPlayCompleted.await(15, TimeUnit.SECONDS);
        if (ready) {
            Ln.i("Handshake RTSP concluído com sucesso! Samsung DeX ativo na memória.");
            return true;
        } else {
            Ln.e("Timeout aguardando conclusão do handshake RTSP (15s).");
            return false;
        }
    }

    /**
     * Encerra a sessão DeX e libera portas de rede.
     */
    public void stop() {
        if (!running.getAndSet(false)) {
            return;
        }
        Ln.i("Encerrando sessão DeX e limpando recursos...");
        if (rtspSocket != null) {
            try {
                rtspSocket.close();
            } catch (IOException ignored) {}
        }
        displayManager.disconnectWifiDisplay();
        Ln.i("Sessão DeX desconectada.");
    }

    /**
     * Substitui Thread.sleep(800): tenta realizar o bind local em loop.
     * Assim que o SO liberar a porta 7236, o método retorna imediatamente.
     */
    private void waitForPortFree(int port, int timeoutMs) throws IOException {
        long deadline = System.currentTimeMillis() + timeoutMs;
        Ln.d("Aguardando liberação da porta " + port + "...");
        while (System.currentTimeMillis() < deadline) {
            try (ServerSocket ss = new ServerSocket(port, 1, InetAddress.getByName("127.0.0.1"))) {
                // Se o bind teve sucesso, a porta está completamente livre
                Ln.d("Porta " + port + " confirmada livre.");
                return;
            } catch (IOException e) {
                try {
                    Thread.sleep(100);
                } catch (InterruptedException ie) {
                    Thread.currentThread().interrupt();
                    throw new IOException("Interrompido enquanto aguardava porta " + port, ie);
                }
            }
        }
        Ln.w("Aviso: Porta " + port + " não liberou no prazo de " + timeoutMs + "ms, prosseguindo...");
    }

    private int allocateUdpPort() throws IOException {
        try (DatagramSocket ds = new DatagramSocket(0)) {
            return ds.getLocalPort();
        }
    }

    private void startUdpDrain(int port) {
        Thread t = new Thread(() -> {
            try (DatagramSocket ds = new DatagramSocket(port, InetAddress.getByName("127.0.0.1"))) {
                byte[] buf = new byte[4096];
                DatagramPacket dp = new DatagramPacket(buf, buf.length);
                while (running.get() || !Thread.currentThread().isInterrupted()) {
                    ds.receive(dp);
                    // Drena silenciosamente os pacotes RTP de vídeo dummy sem alocar CPU
                }
            } catch (Exception ignored) {}
        }, "UdpDrain-" + port);
        t.setDaemon(true);
        t.start();
    }

    private Object buildSemWifiDisplayConfig(int videoPort, int audioPort) throws Exception {
        Class<?> builderClass = Class.forName("android.hardware.display.SemWifiDisplayConfig$Builder");
        Object builder = builderClass.getDeclaredConstructor().newInstance();

        Method setApConnection = builderClass.getMethod("setApConnection", 
                String.class, String.class, String.class, String.class);
        setApConnection.invoke(builder, "127.0.0.1", String.valueOf(RTSP_PORT), DISPLAY_NAME, DUMMY_MAC);

        Method setMode = builderClass.getMethod("setMode", int.class);
        setMode.invoke(builder, 2); // MODE_WIRELESS_DEX = 2

        Method build = builderClass.getMethod("build");
        return build.invoke(builder);
    }

    private Object createWifiDisplayCallback() {
        try {
            Class<?> callbackInterface = Class.forName("android.hardware.display.IWifiDisplayConnectionCallback");
            return Proxy.newProxyInstance(
                callbackInterface.getClassLoader(),
                new Class<?>[]{callbackInterface},
                new InvocationHandler() {
                    @Override
                    public Object invoke(Object proxy, Method method, Object[] args) {
                        String name = method.getName();
                        if ("onSuccess".equals(name)) {
                            Ln.i("IWifiDisplayConnectionCallback: Conexão aceita pelo system_server (onSuccess)!");
                        } else if ("onFailure".equals(name)) {
                            int reason = args != null && args.length > 0 ? (int) args[0] : -1;
                            Ln.e("IWifiDisplayConnectionCallback: Conexão falhou com código: " + reason);
                        } else if ("asBinder".equals(name)) {
                            return null;
                        }
                        return null;
                    }
                }
            );
        } catch (Exception e) {
            Ln.w("Não foi possível registrar Proxy para IWifiDisplayConnectionCallback: " + e.getMessage());
            return null;
        }
    }

    private Socket connectRtspLoopback(int port, int timeoutMs) {
        long deadline = System.currentTimeMillis() + timeoutMs;
        while (System.currentTimeMillis() < deadline) {
            try {
                Socket socket = new Socket("127.0.0.1", port);
                socket.setTcpNoDelay(true);
                socket.setSoTimeout(35000); // 35s timeout para leitura
                Ln.d("Conectado com sucesso ao socket RTSP em 127.0.0.1:" + port);
                return socket;
            } catch (Exception e) {
                try {
                    Thread.sleep(150);
                } catch (InterruptedException ie) {
                    Thread.currentThread().interrupt();
                    return null;
                }
            }
        }
        return null;
    }

    private void runRtspStateMachine() {
        try {
            BufferedReader in = new BufferedReader(
                new InputStreamReader(rtspSocket.getInputStream(), StandardCharsets.US_ASCII));
            OutputStream out = rtspSocket.getOutputStream();

            String presentationUrl = "rtsp://127.0.0.1/wfd1.0/streamid=0";
            String sessionId = null;
            AtomicInteger clientCSeq = new AtomicInteger(1);

            while (running.get()) {
                String firstLine = in.readLine();
                if (firstLine == null) {
                    Ln.w("Socket RTSP fechado pelo host.");
                    break;
                }
                if (firstLine.trim().isEmpty()) continue;

                Ln.d("[RTSP <<] " + firstLine);
                String cseq = "1";
                int contentLength = 0;
                String sessionHeader = null;

                String line;
                while ((line = in.readLine()) != null && !line.isEmpty()) {
                    String lower = line.toLowerCase();
                    if (lower.startsWith("cseq:")) {
                        cseq = line.substring(5).trim();
                    } else if (lower.startsWith("content-length:")) {
                        contentLength = Integer.parseInt(line.substring(15).trim());
                    } else if (lower.startsWith("session:")) {
                        sessionHeader = line.substring(8).trim();
                        if (sessionHeader.contains(";")) {
                            sessionHeader = sessionHeader.substring(0, sessionHeader.indexOf(";")).trim();
                        }
                    }
                }

                String body = "";
                if (contentLength > 0) {
                    char[] buf = new char[contentLength];
                    int read = 0;
                    while (read < contentLength) {
                        int r = in.read(buf, read, contentLength - read);
                        if (r < 0) break;
                        read += r;
                    }
                    body = new String(buf, 0, read);
                }

                // Processar estados do handshake RTSP WFD
                if (firstLine.startsWith("OPTIONS")) {
                    String resp = "RTSP/1.0 200 OK\r\n" +
                                  "CSeq: " + cseq + "\r\n" +
                                  "Public: org.wfa.wfd1.0, GET_PARAMETER, SET_PARAMETER\r\n\r\n";
                    sendRtsp(out, resp);

                    String m2 = "OPTIONS * RTSP/1.0\r\n" +
                                "CSeq: " + clientCSeq.getAndIncrement() + "\r\n" +
                                "Require: org.wfa.wfd1.0\r\n\r\n";
                    sendRtsp(out, m2);

                } else if (firstLine.startsWith("GET_PARAMETER")) {
                    if (body.contains("wfd_video_formats")) {
                        String m3Body = "wfd_client_rtp_ports: RTP/AVP/UDP;unicast " + rtpVideoPort + " 0 mode=play\r\n" +
                                        "wfd_audio_codecs: LPCM 00000003 00, AAC 0000000f 00\r\n" +
                                        "wfd_video_formats: 40 00 01 10 0001bdeb 1fffffff 00003fff 10 0000 001f 11 0780 0438, 02 10 0001bdeb 1fffffff 00000fff 10 0000 001f 11 0780 0438\r\n" +
                                        "wfd_content_protection: none\r\n" +
                                        "wfd_uibc_capability: input_category_list=HIDC;hidc_cap_list=Keyboard/USB, Mouse/USB, MultiTouch/USB, Gesture/USB, RemoteControl/USB;port=none\r\n";
                        byte[] bodyBytes = m3Body.getBytes(StandardCharsets.US_ASCII);
                        String resp = "RTSP/1.0 200 OK\r\n" +
                                      "CSeq: " + cseq + "\r\n" +
                                      "Content-Type: text/parameters\r\n" +
                                      "Content-Length: " + bodyBytes.length + "\r\n\r\n" +
                                      m3Body;
                        sendRtsp(out, resp);
                    } else {
                        String resp = "RTSP/1.0 200 OK\r\n" +
                                      "CSeq: " + cseq + "\r\n\r\n";
                        sendRtsp(out, resp);
                    }

                } else if (firstLine.startsWith("SET_PARAMETER")) {
                    for (String bLine : body.split("\r?\n")) {
                        if (bLine.startsWith("wfd_presentation_URL:")) {
                            String[] parts = bLine.substring(21).trim().split("\\s+");
                            if (parts.length > 0 && parts[0].startsWith("rtsp://")) {
                                presentationUrl = parts[0];
                            }
                        }
                    }

                    String resp = "RTSP/1.0 200 OK\r\n" +
                                  "CSeq: " + cseq + "\r\n\r\n";
                    sendRtsp(out, resp);

                    if (body.contains("wfd_trigger_method: SETUP")) {
                        String setupReq = "SETUP " + presentationUrl + " RTSP/1.0\r\n" +
                                          "CSeq: " + clientCSeq.getAndIncrement() + "\r\n" +
                                          "Transport: RTP/AVP/UDP;unicast;client_port=" + rtpVideoPort + "-" + rtpAudioPort + "\r\n\r\n";
                        sendRtsp(out, setupReq);
                    }

                } else if (firstLine.startsWith("RTSP/1.0 200 OK")) {
                    if (sessionHeader != null && sessionId == null) {
                        sessionId = sessionHeader;
                        String playReq = "PLAY " + presentationUrl + " RTSP/1.0\r\n" +
                                         "CSeq: " + clientCSeq.getAndIncrement() + "\r\n" +
                                         "Session: " + sessionId + "\r\n\r\n";
                        sendRtsp(out, playReq);
                    } else if (sessionId != null) {
                        Ln.i(">>> Resposta 200 OK para PLAY recebida! DeX nativo ativado. <<<");
                        rtspPlayCompleted.countDown();
                    }
                }
            }
        } catch (Exception e) {
            if (running.get()) {
                Ln.e("Erro no loop RTSP: " + e.getMessage(), e);
            }
        }
    }

    private void sendRtsp(OutputStream out, String msg) throws IOException {
        out.write(msg.getBytes(StandardCharsets.US_ASCII));
        out.flush();
    }
}
