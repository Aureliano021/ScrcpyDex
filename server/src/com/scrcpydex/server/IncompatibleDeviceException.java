package com.scrcpydex.server;

/**
 * Exception thrown when the device does not meet the minimum hardware
 * or software requirements to run ScrcpyDeX (e.g., non-Samsung device,
 * Android version below 11, or absence of SemWifiDisplayConfig APIs).
 */
public class IncompatibleDeviceException extends RuntimeException {
    public IncompatibleDeviceException(String message) {
        super(message);
    }

    public IncompatibleDeviceException(String message, Throwable cause) {
        super(message, cause);
    }
}
