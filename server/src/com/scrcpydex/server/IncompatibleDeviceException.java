package com.scrcpydex.server;

/**
 * Exceção lançada quando o dispositivo não atende aos requisitos mínimos
 * de hardware ou software para a execução do ScrcpyDeX (ex: aparelho não-Samsung,
 * versão do Android inferior a 11, ou ausência de APIs SemWifiDisplayConfig).
 */
public class IncompatibleDeviceException extends RuntimeException {
    public IncompatibleDeviceException(String message) {
        super(message);
    }

    public IncompatibleDeviceException(String message, Throwable cause) {
        super(message, cause);
    }
}
