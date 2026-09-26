package com.scrcpydex.server;

import android.os.Handler;
import android.os.Looper;

import com.scrcpydex.server.wrappers.DisplayManager;

import java.io.IOException;
import java.net.InetAddress;
import java.net.ServerSocket;
import java.net.Socket;
import java.util.concurrent.atomic.AtomicBoolean;

/**
 * Ponto de Entrada Principal (Entry Point) do ScrcpyDeX Server.
 * 
 * Executado via app_process no contexto do shell Android (UID 2000).
 * Orquestra o ciclo de vida completo:
 * 1. Inicializa Workarounds e Looper de eventos do Android
 * 2. Valida compatibilidade do hardware Samsung
 * 3. Ativa o motor nativo Samsung DeX via loopback Miracast
 * 4. Monitora o surgimento do display DeX via DisplayManagerGlobal
 * 5. Abre a porta TCP 27183 e aguarda conexão do cliente/ffplay
 * 6. Transmite o fluxo contínuo H.264 gerado pelo MediaCodec
 */
public final class Server {
    private static final int VIDEO_PORT = 27183;
    private static final AtomicBoolean running = new AtomicBoolean(true);

    private static DisplayManager displayManager;
    private static DexActivator dexActivator;
    private static VideoCapture videoCapture;
    private static ControlChannel controlChannel;
    private static ServerSocket videoServerSocket;

    public static void main(String[] args) {
        Ln.i("=================================================");
        Ln.i("      ScrcpyDeX Server v1.0 (Etapa 1)            ");
        Ln.i("   Samsung DeX for PC via USB / Zero Wi-Fi Latency");
        Ln.i("=================================================");

        // 1. Preparar Looper de thread para suporte a Handlers e Callbacks
        if (Looper.myLooper() == null) {
            Looper.prepare();
        }
        Handler mainHandler = new Handler(Looper.myLooper());

        // 2. Aplicar workarounds para app_process no Android 11+
        Workarounds.apply();

        displayManager = new DisplayManager();

        // 3. Suporte ao comando rápido de desconexão manual
        if (args.length > 0 && "disconnect".equalsIgnoreCase(args[0])) {
            Ln.i("Comando de desconexão recebido. Encerrando sessões DeX ativas...");
            displayManager.disconnectWifiDisplay();
            Ln.i("Sessões DeX encerradas com sucesso.");
            return;
        }

        // 4. Instalar ShutdownHook para limpeza automática ao desconectar USB ou fechar processo
        Runtime.getRuntime().addShutdownHook(new Thread(Server::cleanup, "ScrcpyDeX-Cleanup"));

        try {
            // 5. Validar compatibilidade do dispositivo
            CompatCheck.verify();

            // 6. Abrir ServerSocket na porta de vídeo 27183 antes de ativar o DeX
            videoServerSocket = new ServerSocket(VIDEO_PORT, 1, InetAddress.getByName("127.0.0.1"));
            videoServerSocket.setReuseAddress(true);
            Ln.i("Servidor de vídeo escutando em 127.0.0.1:" + VIDEO_PORT + " (aguardando cliente)...");

            // 7. Iniciar monitor de eventos do Display
            DisplayWatch displayWatch = new DisplayWatch();
            displayWatch.startListening(mainHandler);

            // 8. Ativar motor nativo DeX via loopback
            dexActivator = new DexActivator(displayManager);
            boolean activated = dexActivator.activate();
            if (!activated) {
                throw new RuntimeException("Falha ao ativar sessão DeX no loopback RTSP.");
            }

            // 9. Aguardar até o display "ScrcpyDeX" ser criado no SurfaceFlinger
            Ln.i("Aguardando confirmação do display Samsung DeX...");
            int displayId = displayWatch.waitForDisplay(15);
            if (displayId == -1) {
                throw new RuntimeException("Timeout: display Samsung DeX não foi detectado em 15 segundos.");
            }
            Ln.i(">>> Samsung DeX pronto para transmissão! ID=" + displayId + " <<<");
            System.out.println("DEX_ACTIVATED_DISPLAY_ID=" + displayId);

            // Modo Apenas Ativação (para uso direto com scrcpy --display-id)
            if (args.length > 0 && "activate".equalsIgnoreCase(args[0])) {
                Ln.i("Modo 'activate' ativo. Mantendo sessão DeX aberta até encerramento...");
                synchronized (Server.class) {
                    while (running.get()) {
                        try {
                            Server.class.wait();
                        } catch (InterruptedException ignored) {}
                    }
                }
                return;
            }

            // 10. Iniciar canal de controle e input na porta 27184
            controlChannel = new ControlChannel(
                displayId,
                displayWatch.getWidth(),
                displayWatch.getHeight(),
                displayWatch.getDpi(),
                Server::cleanup
            );
            controlChannel.start();

            // 11. Aguardar cliente de vídeo conectar (ex: ffplay ou client C#)
            Ln.i("Pronto para receber conexão na porta " + VIDEO_PORT + "!");
            Socket clientSocket = videoServerSocket.accept();
            clientSocket.setTcpNoDelay(true);
            Ln.i("Cliente conectado: " + clientSocket.getRemoteSocketAddress());

            // 12. Iniciar captura e encoding H.264
            videoCapture = new VideoCapture(
                displayId,
                displayWatch.getWidth(),
                displayWatch.getHeight(),
                clientSocket.getOutputStream()
            );
            videoCapture.start();

        } catch (IncompatibleDeviceException ide) {
            Ln.e("Incompatibilidade detectada: " + ide.getMessage());
        } catch (Throwable t) {
            Ln.e("Erro fatal no servidor ScrcpyDeX: " + t.getMessage(), t);
        } finally {
            cleanup();
        }
    }

    private static void cleanup() {
        if (!running.getAndSet(false)) {
            return;
        }
        Ln.i("Executando desligamento seguro do ScrcpyDeX Server...");
        if (controlChannel != null) {
            controlChannel.stop();
        }
        if (videoCapture != null) {
            videoCapture.stop();
        }
        if (videoServerSocket != null && !videoServerSocket.isClosed()) {
            try {
                videoServerSocket.close();
            } catch (IOException ignored) {}
        }
        if (dexActivator != null) {
            dexActivator.stop();
        }
        if (displayManager != null) {
            displayManager.disconnectWifiDisplay();
        }
        Ln.i("Limpeza concluída. Processo encerrado.");
    }
}
