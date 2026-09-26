package com.scrcpydex.server.wrappers;

import java.lang.reflect.Method;
import android.os.IBinder;
import com.scrcpydex.server.Ln;

/**
 * Reflection wrapper for Android's IDisplayManager service.
 * 
 * Allows invoking display management methods, specifically
 * the WiFi Display (Miracast) APIs that Samsung uses to activate
 * Wireless DeX.
 */
public final class DisplayManager {
    private final Object manager;
    private final Method disconnectWifiDisplayMethod;
    private final Method connectWifiDisplayWithConfigMethod;

    public DisplayManager() {
        try {
            IBinder binder = ServiceManager.getService("display");
            Class<?> stubClass = Class.forName("android.hardware.display.IDisplayManager$Stub");
            Method asInterface = stubClass.getMethod("asInterface", IBinder.class);
            this.manager = asInterface.invoke(null, binder);

            // Default method to disconnect any active Miracast session
            this.disconnectWifiDisplayMethod = manager.getClass().getMethod("disconnectWifiDisplay");

            // Samsung proprietary method to connect with SemWifiDisplayConfig
            Method connectMethod = null;
            for (Method m : manager.getClass().getMethods()) {
                if ("connectWifiDisplayWithConfig".equals(m.getName())) {
                    connectMethod = m;
                    break;
                }
            }
            if (connectMethod == null) {
                throw new NoSuchMethodException("Method connectWifiDisplayWithConfig not found in IDisplayManager");
            }
            this.connectWifiDisplayWithConfigMethod = connectMethod;

        } catch (Exception e) {
            throw new RuntimeException("Failed to initialize DisplayManager wrapper", e);
        }
    }

    /**
     * Terminates any active WiFi Display / DeX session on the device.
     */
    public void disconnectWifiDisplay() {
        try {
            disconnectWifiDisplayMethod.invoke(manager);
            Ln.d("disconnectWifiDisplay command executed successfully.");
        } catch (Exception e) {
            Ln.w("Failed to invoke disconnectWifiDisplay: " + e.getMessage());
        }
    }

    /**
     * Initiates a WiFi Display connection using custom Samsung configuration.
     * 
     * @param config Instance of android.hardware.display.SemWifiDisplayConfig
     * @param callback Instance (or Proxy) of android.hardware.display.IWifiDisplayConnectionCallback
     */
    public void connectWifiDisplayWithConfig(Object config, Object callback) {
        try {
            connectWifiDisplayWithConfigMethod.invoke(manager, config, callback);
            Ln.d("connectWifiDisplayWithConfig command triggered.");
        } catch (Exception e) {
            throw new RuntimeException("Error connecting WiFi Display with SemWifiDisplayConfig", e);
        }
    }

    /**
     * Returns the underlying IDisplayManager object for additional invocations.
     */
    public Object getRawManager() {
        return manager;
    }
}
