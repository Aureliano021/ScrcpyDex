package com.scrcpydex.server;

import android.content.ContextWrapper;

/**
 * Contexto simulado ("FakeContext") para processos executados via app_process.
 * 
 * Como o app_process roda como processo de terminal (UID shell),
 * ele não possui uma Application padrão do Android. O FakeContext fornece
 * o package name "com.android.shell" para APIs do sistema que realizam
 * validação de chamador.
 */
public final class FakeContext extends ContextWrapper {
    public static final String PACKAGE_NAME = "com.android.shell";

    private static final FakeContext INSTANCE = new FakeContext();

    public static FakeContext get() {
        return INSTANCE;
    }

    private FakeContext() {
        super(null);
    }

    @Override
    public String getPackageName() {
        return PACKAGE_NAME;
    }

    @Override
    public String getOpPackageName() {
        return PACKAGE_NAME;
    }
}
