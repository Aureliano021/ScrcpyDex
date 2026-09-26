package com.scrcpydex.server;

import java.text.SimpleDateFormat;
import java.util.Date;
import java.util.Locale;

/**
 * Utilitário de Logging para o ScrcpyDex Server.
 * 
 * Escreve diretamente no stdout/stderr com formatação limpa e timestamp.
 * Como o servidor roda via app_process (processo nativo de linha de comando
 * no Android), o log vai para a saída padrão do processo, que pode ser
 * lida diretamente pelo ADB ou redirecionada para um arquivo de log.
 */
public final class Ln {
    private static final String TAG = "[ScrcpyDeX]";
    private static final SimpleDateFormat DATE_FORMAT = 
            new SimpleDateFormat("HH:mm:ss.SSS", Locale.US);

    private Ln() {
        // Classe utilitária estática
    }

    private static String getTimestamp() {
        return DATE_FORMAT.format(new Date());
    }

    public static void i(String message) {
        System.out.println(getTimestamp() + " " + TAG + " [INFO] " + message);
    }

    public static void w(String message) {
        System.out.println(getTimestamp() + " " + TAG + " [WARN] " + message);
    }

    public static void e(String message) {
        System.err.println(getTimestamp() + " " + TAG + " [ERRO] " + message);
    }

    public static void e(String message, Throwable throwable) {
        System.err.println(getTimestamp() + " " + TAG + " [ERRO] " + message);
        if (throwable != null) {
            throwable.printStackTrace(System.err);
        }
    }

    public static void d(String message) {
        // Habilitado para depuração durante a fase de engenharia reversa
        System.out.println(getTimestamp() + " " + TAG + " [DEBUG] " + message);
    }
}
