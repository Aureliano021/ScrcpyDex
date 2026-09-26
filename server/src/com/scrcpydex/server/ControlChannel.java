package com.scrcpydex.server;

import java.io.DataInputStream;
import java.io.DataOutputStream;
import java.io.IOException;
import java.net.InetAddress;
import java.net.ServerSocket;
import java.net.Socket;
import java.util.concurrent.atomic.AtomicBoolean;

/**
 * Bidirectional Control and Messaging Channel (TCP Port 27184).
 * 
 * Implements the binary protocol specified in docs/PROTOCOL.md:
 * - Emits MSG_DEX_READY and MSG_DISPLAY_INFO to the connected client
 * - Processes incoming client input messages (Mouse, Keyboard, Scroll)
 *   and dispatches immediately to InputHandler with TCP_NODELAY.
 */
public class ControlChannel {
    public static final int CONTROL_PORT = 27184;

    // Message types defined in docs/PROTOCOL.md
    public static final byte MSG_DEX_READY = 0x01;
    public static final byte MSG_DISPLAY_INFO = 0x02;
    public static final byte MSG_HEARTBEAT = 0x03;
    public static final byte MSG_ERROR = (byte) 0xFF;

    public static final byte MSG_MOUSE_MOVE = 0x10;
    public static final byte MSG_MOUSE_BUTTON = 0x11;
    public static final byte MSG_KEY_EVENT = 0x12;
    public static final byte MSG_SCROLL = 0x13;
    public static final byte MSG_SET_CONFIG = 0x20;
    public static final byte MSG_DISCONNECT = (byte) 0xFE;

    private final int displayId;
    private final int width;
    private final int height;
    private final int dpi;
    private final InputHandler inputHandler;
    private final Runnable onDisconnectCallback;
    private final AtomicBoolean running = new AtomicBoolean(false);

    private ServerSocket serverSocket;
    private Socket clientSocket;
    private DataOutputStream out;
    private DataInputStream in;

    public ControlChannel(int displayId, int width, int height, int dpi, Runnable onDisconnectCallback) {
        this.displayId = displayId;
        this.width = width;
        this.height = height;
        this.dpi = dpi;
        this.inputHandler = new InputHandler(displayId);
        this.onDisconnectCallback = onDisconnectCallback;
    }

    /**
     * Starts the control channel server on a separate thread.
     */
    public void start() throws IOException {
        serverSocket = new ServerSocket(CONTROL_PORT, 1, InetAddress.getByName("127.0.0.1"));
        serverSocket.setReuseAddress(true);
        running.set(true);

        Thread thread = new Thread(this::listenLoop, "ScrcpyDeX-Control");
        thread.setDaemon(true);
        thread.start();
        Ln.i("TCP control channel listening on 127.0.0.1:" + CONTROL_PORT);
    }

    private void listenLoop() {
        try {
            clientSocket = serverSocket.accept();
            clientSocket.setTcpNoDelay(true);
            out = new DataOutputStream(clientSocket.getOutputStream());
            in = new DataInputStream(clientSocket.getInputStream());

            Ln.i("Control client connected: " + clientSocket.getRemoteSocketAddress());

            // 1. Send DeX ready event with display ID
            out.writeByte(MSG_DEX_READY);
            out.writeInt(displayId);
            out.flush();

            // 2. Send display dimensions and density
            out.writeByte(MSG_DISPLAY_INFO);
            out.writeInt(width);
            out.writeInt(height);
            out.writeInt(dpi);
            out.flush();

            Ln.d("Initialization events (MSG_DEX_READY and MSG_DISPLAY_INFO) sent to client.");

            // 3. Continuous read loop for incoming client messages
            while (running.get()) {
                int msgType = in.read();
                if (msgType == -1) {
                    Ln.w("Control channel connection closed by client.");
                    break;
                }

                switch ((byte) msgType) {
                    case MSG_MOUSE_MOVE: {
                        int x = in.readInt();
                        int y = in.readInt();
                        inputHandler.handleMouseMove(x, y);
                        break;
                    }

                    case MSG_MOUSE_BUTTON: {
                        int x = in.readInt();
                        int y = in.readInt();
                        int button = in.readUnsignedByte();
                        int action = in.readUnsignedByte();
                        inputHandler.handleMouseButton(x, y, button, action);
                        break;
                    }

                    case MSG_KEY_EVENT: {
                        int keyCode = in.readInt();
                        int action = in.readUnsignedByte();
                        inputHandler.handleKeyEvent(keyCode, action);
                        break;
                    }

                    case MSG_SCROLL: {
                        int x = in.readInt();
                        int y = in.readInt();
                        float hScroll = in.readFloat();
                        float vScroll = in.readFloat();
                        inputHandler.handleScroll(x, y, hScroll, vScroll);
                        break;
                    }

                    case MSG_SET_CONFIG: {
                        int jsonLen = in.readUnsignedShort();
                        byte[] jsonBytes = new byte[jsonLen];
                        in.readFully(jsonBytes);
                        Ln.i("Configuration received from client: " + new String(jsonBytes, "UTF-8"));
                        break;
                    }

                    case MSG_DISCONNECT: {
                        Ln.i("MSG_DISCONNECT command received from client.");
                        if (onDisconnectCallback != null) {
                            onDisconnectCallback.run();
                        }
                        return;
                    }

                    default:
                        Ln.w("Unknown message type on control channel: 0x" + Integer.toHexString(msgType));
                        break;
                }
            }

        } catch (IOException ioe) {
            if (running.get()) {
                Ln.w("Control channel closed: " + ioe.getMessage());
            }
        } finally {
            stop();
        }
    }

    /**
     * Stops the control channel and closes sockets.
     */
    public void stop() {
        if (!running.getAndSet(false)) {
            return;
        }
        try {
            if (clientSocket != null && !clientSocket.isClosed()) {
                clientSocket.close();
            }
            if (serverSocket != null && !serverSocket.isClosed()) {
                serverSocket.close();
            }
        } catch (IOException ignored) {}
        Ln.i("Control channel terminated.");
    }
}
