package com.scrcpydex.server;

import java.lang.reflect.Method;
import android.os.Build;

/**
 * Módulo de Verificação de Compatibilidade.
 * 
 * Executa antes de qualquer tentativa de ativação do DeX para garantir
 * que o ambiente é suportado, emitindo diagnósticos claros caso o aparelho
 * não seja Samsung ou falte a API de espelhamento proprietária.
 */
public final class CompatCheck {

    private CompatCheck() {
        // Classe utilitária
    }

    /**
     * Lê propriedades do sistema Android via classe oculta SystemProperties.
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
     * Executa a bateria de testes de compatibilidade.
     * 
     * @throws IncompatibleDeviceException se alguma exigência falhar
     */
    public static void verify() {
        Ln.i("Executando verificações de compatibilidade do dispositivo...");

        // 1. Verificar Fabricante (Exige Samsung)
        String manufacturer = Build.MANUFACTURER;
        if (manufacturer == null || manufacturer.isEmpty()) {
            manufacturer = getSystemProperty("ro.product.manufacturer", "unknown");
        }
        Ln.d("Fabricante detectado: " + manufacturer);

        if (!"samsung".equalsIgnoreCase(manufacturer)) {
            throw new IncompatibleDeviceException(
                "Dispositivo incompatível: Fabricante '" + manufacturer + "'. O ScrcpyDeX requer um aparelho Samsung.");
        }

        // 2. Verificar Versão do Android (SDK 30+ / Android 11+)
        int sdkInt = Build.VERSION.SDK_INT;
        Ln.d("Versão do Android SDK: " + sdkInt);
        if (sdkInt < 30) {
            throw new IncompatibleDeviceException(
                "Versão do Android incompatível: SDK " + sdkInt + ". O ScrcpyDeX requer Android 11 (SDK 30) ou superior.");
        }

        // 3. Verificar presença da API proprietária Samsung SemWifiDisplayConfig
        try {
            Class.forName("android.hardware.display.SemWifiDisplayConfig$Builder");
            Ln.d("API SemWifiDisplayConfig encontrada com sucesso.");
        } catch (ClassNotFoundException e) {
            String oneUiVer = getSystemProperty("ro.build.version.oneui", "desconhecida");
            throw new IncompatibleDeviceException(
                "A API SemWifiDisplayConfig da Samsung não está presente neste firmware. One UI: " + oneUiVer, e);
        }

        // 4. Detalhes de diagnóstico no log
        String oneUiVersion = getSystemProperty("ro.build.version.oneui", "N/A");
        String deviceModel = Build.MODEL;
        Ln.i("Dispositivo validado com sucesso: " + deviceModel + 
             " | One UI: " + oneUiVersion + " | Android SDK: " + sdkInt);
    }
}
