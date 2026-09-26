package com.scrcpydex.server;

import android.os.Handler;
import android.os.Looper;

import java.lang.reflect.Field;
import java.lang.reflect.Method;
import java.lang.reflect.Proxy;
import java.util.concurrent.CountDownLatch;
import java.util.concurrent.TimeUnit;
import java.util.concurrent.atomic.AtomicInteger;

/**
 * Monitor Orientado a Eventos do Samsung DeX Display.
 * 
 * Substitui o polling lento e instável via "dumpsys display | grep ScrcpyDeX".
 * Utiliza o DisplayManagerGlobal diretamente via IPC nativa em memória,
 * escutando eventos de criação de tela para capturar o ID do display DeX
 * no momento exato em que ele surge no sistema.
 */
public class DisplayWatch {
    private static final String TARGET_DISPLAY_NAME = "ScrcpyDeX";

    private final Object displayManagerGlobal;
    private final CountDownLatch displayLatch = new CountDownLatch(1);
    private final AtomicInteger detectedDisplayId = new AtomicInteger(-1);

    private int width = 1920;
    private int height = 1080;
    private int dpi = 160;

    public DisplayWatch() {
        try {
            Class<?> dmgClass = Class.forName("android.hardware.display.DisplayManagerGlobal");
            Method getInstance = dmgClass.getDeclaredMethod("getInstance");
            this.displayManagerGlobal = getInstance.invoke(null);
        } catch (Exception e) {
            throw new RuntimeException("Falha ao obter DisplayManagerGlobal", e);
        }
    }

    /**
     * Inicia a escuta de criação de displays e verifica se o display já existe.
     */
    public void startListening(Handler handler) {
        // 1. Verificar se o display já está ativo antes mesmo do listener
        if (checkExistingDisplays()) {
            return;
        }

        // 2. Registrar listener nativo de displays via Proxy dinâmico
        try {
            Class<?> listenerClass = Class.forName("android.hardware.display.DisplayManager$DisplayListener");
            Object listenerProxy = Proxy.newProxyInstance(
                listenerClass.getClassLoader(),
                new Class<?>[]{listenerClass},
                (proxy, method, args) -> {
                    String name = method.getName();
                    if ("onDisplayAdded".equals(name) || "onDisplayChanged".equals(name)) {
                        int displayId = (int) args[0];
                        inspectDisplay(displayId);
                    }
                    return null;
                }
            );

            // Tentar registrar com handler
            try {
                displayManagerGlobal.getClass()
                    .getMethod("registerDisplayListener", listenerClass, Handler.class, long.class, String.class)
                    .invoke(displayManagerGlobal, listenerProxy, handler, 7L, FakeContext.PACKAGE_NAME);
            } catch (NoSuchMethodException e1) {
                try {
                    displayManagerGlobal.getClass()
                        .getMethod("registerDisplayListener", listenerClass, Handler.class, long.class)
                        .invoke(displayManagerGlobal, listenerProxy, handler, 7L);
                } catch (NoSuchMethodException e2) {
                    displayManagerGlobal.getClass()
                        .getMethod("registerDisplayListener", listenerClass, Handler.class)
                        .invoke(displayManagerGlobal, listenerProxy, handler);
                }
            }
            Ln.d("DisplayListener registrado no DisplayManagerGlobal.");
        } catch (Exception e) {
            Ln.w("Falha ao registrar DisplayListener via Proxy: " + e.getMessage() + ". Utilizando verificação ativa.");
        }
    }

    /**
     * Aguarda até que o display DeX seja detectado com timeout.
     * 
     * @param timeoutSeconds Tempo limite em segundos
     * @return ID do display DeX ou -1 se timeout
     */
    public int waitForDisplay(int timeoutSeconds) throws InterruptedException {
        long deadline = System.currentTimeMillis() + (timeoutSeconds * 1000L);
        while (System.currentTimeMillis() < deadline) {
            if (detectedDisplayId.get() != -1) {
                return detectedDisplayId.get();
            }
            if (checkExistingDisplays()) {
                return detectedDisplayId.get();
            }
            if (displayLatch.await(200, TimeUnit.MILLISECONDS)) {
                return detectedDisplayId.get();
            }
        }
        return -1;
    }

    private boolean checkExistingDisplays() {
        try {
            Method getDisplayIds = displayManagerGlobal.getClass().getMethod("getDisplayIds");
            int[] ids = (int[]) getDisplayIds.invoke(displayManagerGlobal);
            if (ids != null) {
                for (int id : ids) {
                    if (inspectDisplay(id)) {
                        return true;
                    }
                }
            }
        } catch (Exception e) {
            Ln.w("Erro ao consultar getDisplayIds: " + e.getMessage());
        }
        return false;
    }

    private boolean inspectDisplay(int displayId) {
        try {
            Method getDisplayInfo = displayManagerGlobal.getClass().getMethod("getDisplayInfo", int.class);
            Object displayInfo = getDisplayInfo.invoke(displayManagerGlobal, displayId);
            if (displayInfo == null) return false;

            Class<?> cls = displayInfo.getClass();
            Field nameField = cls.getDeclaredField("name");
            nameField.setAccessible(true);
            String name = (String) nameField.get(displayInfo);

            if (TARGET_DISPLAY_NAME.equals(name)) {
                try {
                    Field wField = cls.getDeclaredField("logicalWidth");
                    Field hField = cls.getDeclaredField("logicalHeight");
                    Field dpiField = cls.getDeclaredField("logicalDensityDpi");
                    wField.setAccessible(true);
                    hField.setAccessible(true);
                    dpiField.setAccessible(true);

                    this.width = wField.getInt(displayInfo);
                    this.height = hField.getInt(displayInfo);
                    this.dpi = dpiField.getInt(displayInfo);
                } catch (Throwable ignored) {}

                Ln.i("Display Samsung DeX detectado com sucesso: ID=" + displayId + 
                     " (" + width + "x" + height + " @ " + dpi + "dpi)");
                detectedDisplayId.set(displayId);
                displayLatch.countDown();
                return true;
            }
        } catch (Exception ignored) {}
        return false;
    }

    public int getDetectedDisplayId() {
        return detectedDisplayId.get();
    }

    public int getWidth() {
        return width;
    }

    public int getHeight() {
        return height;
    }

    public int getDpi() {
        return dpi;
    }
}
