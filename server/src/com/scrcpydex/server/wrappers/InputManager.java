package com.scrcpydex.server.wrappers;

import android.os.IBinder;
import android.view.InputEvent;
import com.scrcpydex.server.Ln;

import java.lang.reflect.Method;

/**
 * Wrapper de reflexão para o serviço IInputManager do Android.
 * 
 * Permite injetar eventos de hardware (mouse, teclado, touch e scroll)
 * diretamente no subsistema de entrada do WindowManager do Android,
 * direcionando os eventos especificamente para o display do Samsung DeX.
 */
public final class InputManager {
    public static final int INJECT_INPUT_EVENT_MODE_ASYNC = 0;
    public static final int INJECT_INPUT_EVENT_MODE_WAIT_FOR_RESULT = 1;

    private static final Object MANAGER;
    private static final Method INJECT_INPUT_EVENT_METHOD;
    private static final Method SET_DISPLAY_ID_METHOD;

    static {
        try {
            IBinder binder = ServiceManager.getService("input");
            Class<?> stubClass = Class.forName("android.hardware.input.IInputManager$Stub");
            Method asInterface = stubClass.getMethod("asInterface", IBinder.class);
            MANAGER = asInterface.invoke(null, binder);

            INJECT_INPUT_EVENT_METHOD = MANAGER.getClass().getMethod("injectInputEvent", InputEvent.class, int.class);

            Method setDisplayId = null;
            try {
                setDisplayId = InputEvent.class.getMethod("setDisplayId", int.class);
            } catch (NoSuchMethodException ignored) {}
            SET_DISPLAY_ID_METHOD = setDisplayId;

        } catch (Exception e) {
            throw new AssertionError("Falha ao inicializar wrapper InputManager", e);
        }
    }

    private InputManager() {}

    /**
     * Injeta um evento de entrada associando-o ao ID da tela de destino.
     * 
     * @param event Instância de MotionEvent ou KeyEvent
     * @param displayId ID do display (ex: display do DeX)
     * @param mode Modo de injeção (0 = ASYNC para máxima fluidez)
     * @return true se o evento foi aceito pelo WindowManager
     */
    public static boolean injectInputEvent(InputEvent event, int displayId, int mode) {
        try {
            if (SET_DISPLAY_ID_METHOD != null) {
                SET_DISPLAY_ID_METHOD.invoke(event, displayId);
            }
            boolean success = (boolean) INJECT_INPUT_EVENT_METHOD.invoke(MANAGER, event, mode);
            if (!success) {
                Ln.w("injectInputEvent retornou false para evento no display " + displayId + ": " + event);
            }
            return success;
        } catch (Exception e) {
            Ln.e("Erro ao injetar evento de input: " + e.getMessage(), e);
            return false;
        }
    }

    private static Method setActionButtonMethod;

    public static boolean setActionButton(android.view.MotionEvent motionEvent, int actionButton) {
        try {
            if (setActionButtonMethod == null) {
                setActionButtonMethod = android.view.MotionEvent.class.getMethod("setActionButton", int.class);
            }
            setActionButtonMethod.invoke(motionEvent, actionButton);
            return true;
        } catch (Throwable ignored) {
            return false;
        }
    }
}
