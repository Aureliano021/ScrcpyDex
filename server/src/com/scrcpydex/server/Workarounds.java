package com.scrcpydex.server;

import android.app.Application;
import android.app.Instrumentation;
import android.content.pm.ApplicationInfo;
import android.os.Build;

import java.lang.reflect.Constructor;
import java.lang.reflect.Field;

/**
 * Ajustes de ambiente ("Workarounds") para app_process no Android 11+.
 * 
 * Em aparelhos Samsung executando One UI, métodos internos como
 * DisplayManagerGlobal.getDisplayInfoLocked() tentam consultar
 * ActivityThread.currentActivityThread().getConfiguration(), que por padrão
 * gera NullPointerException em processos standalone de linha de comando.
 * Esta classe simula a estrutura do ActivityThread para garantir execução estável.
 */
public final class Workarounds {

    private static final Class<?> ACTIVITY_THREAD_CLASS;
    private static final Object ACTIVITY_THREAD;

    static {
        try {
            ACTIVITY_THREAD_CLASS = Class.forName("android.app.ActivityThread");
            Constructor<?> activityThreadConstructor = ACTIVITY_THREAD_CLASS.getDeclaredConstructor();
            activityThreadConstructor.setAccessible(true);
            ACTIVITY_THREAD = activityThreadConstructor.newInstance();

            Field sCurrentActivityThreadField = ACTIVITY_THREAD_CLASS.getDeclaredField("sCurrentActivityThread");
            sCurrentActivityThreadField.setAccessible(true);
            sCurrentActivityThreadField.set(null, ACTIVITY_THREAD);

            Field mSystemThreadField = ACTIVITY_THREAD_CLASS.getDeclaredField("mSystemThread");
            mSystemThreadField.setAccessible(true);
            mSystemThreadField.setBoolean(ACTIVITY_THREAD, true);
        } catch (Exception e) {
            throw new AssertionError("Falha crítica ao inicializar Workarounds do ActivityThread", e);
        }
    }

    private Workarounds() {}

    public static void apply() {
        if (Build.VERSION.SDK_INT >= 31) { // Android 12+
            fillConfigurationController();
        }
        fillAppInfo();
        fillAppContext();
    }

    private static void fillConfigurationController() {
        try {
            Class<?> configurationControllerClass = Class.forName("android.app.ConfigurationController");
            Class<?> activityThreadInternalClass = Class.forName("android.app.ActivityThreadInternal");

            Constructor<?> constructor = configurationControllerClass.getDeclaredConstructor(activityThreadInternalClass);
            constructor.setAccessible(true);
            Object configController = constructor.newInstance(ACTIVITY_THREAD);

            Field field = ACTIVITY_THREAD_CLASS.getDeclaredField("mConfigurationController");
            field.setAccessible(true);
            field.set(ACTIVITY_THREAD, configController);
        } catch (Throwable t) {
            Ln.d("Workaround ConfigurationController ignorado: " + t.getMessage());
        }
    }

    private static void fillAppInfo() {
        try {
            Class<?> appBindDataClass = Class.forName("android.app.ActivityThread$AppBindData");
            Constructor<?> constructor = appBindDataClass.getDeclaredConstructor();
            constructor.setAccessible(true);
            Object appBindData = constructor.newInstance();

            ApplicationInfo appInfo = new ApplicationInfo();
            appInfo.packageName = FakeContext.PACKAGE_NAME;

            Field appInfoField = appBindDataClass.getDeclaredField("appInfo");
            appInfoField.setAccessible(true);
            appInfoField.set(appBindData, appInfo);

            Field mBoundApplicationField = ACTIVITY_THREAD_CLASS.getDeclaredField("mBoundApplication");
            mBoundApplicationField.setAccessible(true);
            mBoundApplicationField.set(ACTIVITY_THREAD, appBindData);
        } catch (Throwable t) {
            Ln.d("Workaround AppInfo ignorado: " + t.getMessage());
        }
    }

    private static void fillAppContext() {
        try {
            Application app = Instrumentation.newApplication(Application.class, FakeContext.get());
            Field mInitialApplicationField = ACTIVITY_THREAD_CLASS.getDeclaredField("mInitialApplication");
            mInitialApplicationField.setAccessible(true);
            mInitialApplicationField.set(ACTIVITY_THREAD, app);
        } catch (Throwable t) {
            Ln.d("Workaround AppContext ignorado: " + t.getMessage());
        }
    }
}
