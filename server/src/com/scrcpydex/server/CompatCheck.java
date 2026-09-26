package com.scrcpydex.server;

import java.lang.reflect.Method;
import android.os.Build;

/**
 * Compatibility Verification Module.
 * 
 * Runs before any DeX activation attempt to ensure the environment
 * is supported, issuing clear diagnostics if the device is not Samsung
 * or lacks the proprietary wireless display mirroring API.
 */
public final class CompatCheck {

    private CompatCheck() {
        // Utility class
    }

    /**
     * Reads Android system properties via the hidden SystemProperties class.
     */
    public static String getSystemProperty(String key, String defaultValue) {
        try {
            Class<?> spClass = Class.forName("android.os.SystemProperties");
            Method getMethod = spClass.getMethod("get", String.class, String.class);
            return (String) getMethod.invoke(null, key, defaultValue);
        } catch (Throwable t) {
            return defaultValue;
        }
    }

    /**
     * Executes the compatibility test suite.
     * 
     * @throws IncompatibleDeviceException if any requirement fails
     */
    public static void verify() {
        Ln.i("Running device compatibility checks...");

        // 1. Verify Manufacturer (Requires Samsung)
        String manufacturer = Build.MANUFACTURER;
        if (manufacturer == null || manufacturer.isEmpty()) {
            manufacturer = getSystemProperty("ro.product.manufacturer", "unknown");
        }
        Ln.d("Detected manufacturer: " + manufacturer);

        if (!"samsung".equalsIgnoreCase(manufacturer)) {
            throw new IncompatibleDeviceException(
                "Incompatible device: Manufacturer '" + manufacturer + "'. ScrcpyDeX requires a Samsung device.");
        }

        // 2. Verify Android Version (SDK 30+ / Android 11+)
        int sdkInt = Build.VERSION.SDK_INT;
        Ln.d("Android SDK version: " + sdkInt);
        if (sdkInt < 30) {
            throw new IncompatibleDeviceException(
                "Incompatible Android version: SDK " + sdkInt + ". ScrcpyDeX requires Android 11 (SDK 30) or higher.");
        }

        // 3. Verify presence of Samsung proprietary SemWifiDisplayConfig API
        try {
            Class.forName("android.hardware.display.SemWifiDisplayConfig$Builder");
            Ln.d("SemWifiDisplayConfig API successfully found.");
        } catch (ClassNotFoundException e) {
            String oneUiVer = getSystemProperty("ro.build.version.oneui", "unknown");
            throw new IncompatibleDeviceException(
                "Samsung's SemWifiDisplayConfig API is not present on this firmware. One UI: " + oneUiVer, e);
        }

        // 4. Diagnostic details in log
        String oneUiVersion = getSystemProperty("ro.build.version.oneui", "N/A");
        String deviceModel = Build.MODEL;
        Ln.i("Device validated successfully: " + deviceModel + 
             " | One UI: " + oneUiVersion + " | Android SDK: " + sdkInt);
    }
}
