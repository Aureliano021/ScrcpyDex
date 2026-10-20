package com.scrcpydex.server;

import android.os.Handler;
import android.os.Looper;

import com.scrcpydex.server.wrappers.DisplayManager;

import java.util.concurrent.atomic.AtomicBoolean;

/**
 * Main Entry Point for ScrcpyDeX Server.
 * 
 * Executed via app_process in Android shell context (UID 2000).
 * Orchestrates Samsung DeX activation:
 * 1. Initializes Workarounds and Android event Looper
 * 2. Validates Samsung hardware compatibility
 * 3. Activates Samsung DeX display via loopback RTSP
 * 4. Monitors DeX display appearance via DisplayManager
 * 5. Emits DEX_ACTIVATED_DISPLAY_ID=<id> for scrcpy client consumption
 * 6. Holds session open until termination signal or disconnect request
 */
public final class Server {
    private static final AtomicBoolean running = new AtomicBoolean(true);

    private static DisplayManager displayManager;
    private static DexActivator dexActivator;

    public static void main(String[] args) {
        Ln.i("=================================================");
        Ln.i("            ScrcpyDeX Server v2.0                ");
        Ln.i("          Samsung DeX for PC via USB             ");
        Ln.i("=================================================");

        if (args.length == 0 || (!"activate".equalsIgnoreCase(args[0]) && !"disconnect".equalsIgnoreCase(args[0]))) {
            Ln.e("Invalid or missing arguments.");
            Ln.e("Usage: app_process ... com.scrcpydex.server.Server [activate|disconnect]");
            System.err.println("Usage: Server [activate|disconnect]");
            System.exit(1);
            return;
        }

        // 1. Prepare thread Looper for Handler and Callback support
        if (Looper.myLooper() == null) {
            Looper.prepare();
        }
        Handler mainHandler = new Handler(Looper.myLooper());

        // 2. Apply workarounds for app_process on Android 11+
        Workarounds.apply();

        displayManager = new DisplayManager();

        // 3. Fast manual disconnect command
        if ("disconnect".equalsIgnoreCase(args[0])) {
            Ln.i("Disconnect command received. Terminating active DeX sessions...");
            displayManager.disconnectWifiDisplay();
            Ln.i("DeX sessions terminated successfully.");
            return;
        }

        // 4. Install ShutdownHook for automatic cleanup on process termination or USB disconnect
        Runtime.getRuntime().addShutdownHook(new Thread(Server::cleanup, "ScrcpyDeX-Cleanup"));

        try {
            // 5. Validate device compatibility
            CompatCheck.verify();

            // 6. Start Display event monitor
            DisplayWatch displayWatch = new DisplayWatch();
            displayWatch.startListening(mainHandler);

            // 7. Activate DeX session via loopback
            dexActivator = new DexActivator(displayManager);
            boolean activated = dexActivator.activate();
            if (!activated) {
                throw new RuntimeException("Failed to activate DeX session over RTSP loopback.");
            }

            // 8. Wait until the 'ScrcpyDeX' display is created in SurfaceFlinger
            Ln.i("Waiting for Samsung DeX display confirmation...");
            int displayId = displayWatch.waitForDisplay(15);
            if (displayId == -1) {
                throw new RuntimeException("Timeout: Samsung DeX display was not detected within 15 seconds.");
            }
            Ln.i("Samsung DeX ready for streaming! ID=" + displayId);
            System.out.println("DEX_ACTIVATED_DISPLAY_ID=" + displayId);

            Ln.i("'activate' mode enabled. Keeping DeX session open until termination...");
            synchronized (Server.class) {
                while (running.get()) {
                    try {
                        Server.class.wait();
                    } catch (InterruptedException ignored) {}
                }
            }

        } catch (IncompatibleDeviceException ide) {
            Ln.e("Incompatibility detected: " + ide.getMessage());
        } catch (Throwable t) {
            Ln.e("Fatal error in ScrcpyDeX server: " + t.getMessage(), t);
        } finally {
            cleanup();
        }
    }

    private static void cleanup() {
        if (!running.getAndSet(false)) {
            return;
        }
        Ln.i("Shutting down ScrcpyDeX Server...");
        synchronized (Server.class) {
            Server.class.notifyAll();
        }
        if (dexActivator != null) {
            dexActivator.stop();
        }
        if (displayManager != null) {
            displayManager.disconnectWifiDisplay();
        }
        Ln.i("Cleanup complete. Process terminated.");
    }
}
