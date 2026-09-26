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
 * Native Samsung DeX Activator via Miracast Loopback (127.0.0.1).
 * 
 * This module executes the core reverse-engineered discovery of this project:
 * instructs Samsung's WiFi Display subsystem to connect to the device itself
 * on localhost, enabling the FLAG_WIRELESS_DEX_DISPLAY (0x4000000)
 * and FLAG_EXTERNAL_DEX_HOSTING (0x20000) flags in system_server without root and without Wi-Fi.
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
     * Initiates full Samsung DeX activation.
     * 
     * @return true if RTSP handshake completed with PLAY OK
     * @throws Exception if an unrecoverable failure occurs
     */
    public boolean activate() throws Exception {
        Ln.i("Starting Samsung DeX engine activation via local loopback...");

        // 1. Clean up previous sessions and ensure socket release
        displayManager.disconnectWifiDisplay();
        waitForPortFree(RTSP_PORT, 5000);

        // 2. Allocate dynamic ports for dummy RTP drain
        this.rtpVideoPort = allocateUdpPort();
        this.rtpAudioPort = allocateUdpPort();
        Ln.d("Dynamic RTP ports allocated: Video=" + rtpVideoPort + ", Audio=" + rtpAudioPort);

        startUdpDrain(rtpVideoPort);
        startUdpDrain(rtpAudioPort);

        // 3. Build SemWifiDisplayConfig via reflection
        Object config = buildSemWifiDisplayConfig(rtpVideoPort, rtpAudioPort);

        // 4. Create dynamic Proxy for IWifiDisplayConnectionCallback
        Object connectionCallback = createWifiDisplayCallback();

        // 5. Trigger IPC connection on Samsung DisplayManagerService
        displayManager.connectWifiDisplayWithConfig(config, connectionCallback);
        Ln.i("IPC connectWifiDisplayWithConfig command sent to system_server.");

        // 6. Connect to RTSP socket opened by RemoteDisplay at 127.0.0.1:7236
        this.rtspSocket = connectRtspLoopback(RTSP_PORT, 6000);
        if (rtspSocket == null) {
            Ln.e("Critical failure: Android RemoteDisplay did not open port " + RTSP_PORT);
            displayManager.disconnectWifiDisplay();
            return false;
        }

        running.set(true);

        // 7. Start RTSP handshake loop on a dedicated thread
        Thread rtspThread = new Thread(this::runRtspStateMachine, "ScrcpyDeX-RTSP");
        rtspThread.setDaemon(true);
        rtspThread.start();

        // 8. Wait for PLAY completion signal (DeX active)
        boolean ready = rtspPlayCompleted.await(15, TimeUnit.SECONDS);
        if (ready) {
            Ln.i("RTSP handshake successfully completed! Native Samsung DeX active in memory.");
            return true;
        } else {
            Ln.e("Timeout waiting for RTSP handshake completion (15s).");
            return false;
        }
    }

    /**
     * Terminates the DeX session and releases network ports.
     */
    public void stop() {
        if (!running.getAndSet(false)) {
            return;
        }
        Ln.i("Terminating DeX session and releasing resources...");
        if (rtspSocket != null) {
            try {
                rtspSocket.close();
            } catch (IOException ignored) {}
        }
        displayManager.disconnectWifiDisplay();
        Ln.i("DeX session disconnected.");
    }

    /**
     * Replaces Thread.sleep(800): attempts local bind in a loop.
     * As soon as the OS releases port 7236, this method returns immediately.
     */
    private void waitForPortFree(int port, int timeoutMs) throws IOException {
        long deadline = System.currentTimeMillis() + timeoutMs;
        Ln.d("Waiting for port " + port + " to be released...");
        while (System.currentTimeMillis() < deadline) {
            try (ServerSocket ss = new ServerSocket(port, 1, InetAddress.getByName("127.0.0.1"))) {
                // If bind succeeds, the port is completely free
                Ln.d("Port " + port + " confirmed free.");
                return;
            } catch (IOException e) {
                try {
                    Thread.sleep(100);
                } catch (InterruptedException ie) {
                    Thread.currentThread().interrupt();
                    throw new IOException("Interrupted while waiting for port " + port, ie);
                }
            }
        }
        Ln.w("Warning: Port " + port + " was not released within " + timeoutMs + "ms, proceeding...");
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
                    // Silently drain dummy video RTP packets without burning CPU
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
                            Ln.i("IWifiDisplayConnectionCallback: Connection accepted by system_server (onSuccess)!");
                        } else if ("onFailure".equals(name)) {
                            int reason = args != null && args.length > 0 ? (int) args[0] : -1;
                            Ln.e("IWifiDisplayConnectionCallback: Connection failed with code: " + reason);
                        } else if ("asBinder".equals(name)) {
                            return null;
                        }
                        return null;
                    }
                }
            );
        } catch (Exception e) {
            Ln.w("Could not register Proxy for IWifiDisplayConnectionCallback: " + e.getMessage());
            return null;
        }
    }

    private Socket connectRtspLoopback(int port, int timeoutMs) {
        long deadline = System.currentTimeMillis() + timeoutMs;
        while (System.currentTimeMillis() < deadline) {
            try {
                Socket socket = new Socket("127.0.0.1", port);
                socket.setTcpNoDelay(true);
                socket.setSoTimeout(35000); // 35s read timeout
                Ln.d("Successfully connected to RTSP socket at 127.0.0.1:" + port);
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
                    Ln.w("RTSP socket closed by host.");
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

                // Process WFD RTSP handshake states
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
                        Ln.i(">>> Received 200 OK response for PLAY! Native DeX activated. <<<");
                        rtspPlayCompleted.countDown();
                    }
                }
            }
        } catch (Exception e) {
            if (running.get()) {
                Ln.e("Error in RTSP loop: " + e.getMessage(), e);
            }
        }
    }

    private void sendRtsp(OutputStream out, String msg) throws IOException {
        out.write(msg.getBytes(StandardCharsets.US_ASCII));
        out.flush();
    }
}
