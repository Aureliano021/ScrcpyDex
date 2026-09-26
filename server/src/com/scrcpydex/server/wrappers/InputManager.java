package com.scrcpydex.server.wrappers;

import android.os.IBinder;
import android.view.InputEvent;
import com.scrcpydex.server.Ln;

import java.lang.reflect.Method;

/**
 * Reflection wrapper for Android's IInputManager service.
 * 
 * Allows injecting hardware events (mouse, keyboard, touch, and scroll)
 * directly into Android's WindowManager input subsystem,
 * targeting events specifically to the Samsung DeX display.
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
            throw new AssertionError("Failed to initialize InputManager wrapper", e);
        }
    }

    private InputManager() {}

    /**
     * Injects an input event associated with the target display ID.
     * 
     * @param event Instance of MotionEvent or KeyEvent
     * @param displayId Display ID (e.g., DeX display)
     * @param mode Injection mode (0 = ASYNC for maximum fluidity)
     * @return true if the event was accepted by WindowManager
     */
    public static boolean injectInputEvent(InputEvent event, int displayId, int mode) {
        try {
            if (SET_DISPLAY_ID_METHOD != null) {
                SET_DISPLAY_ID_METHOD.invoke(event, displayId);
            }
            boolean success = (boolean) INJECT_INPUT_EVENT_METHOD.invoke(MANAGER, event, mode);
            if (!success) {
                Ln.w("injectInputEvent returned false for event on display " + displayId + ": " + event);
            }
            return success;
        } catch (Exception e) {
            Ln.e("Error injecting input event: " + e.getMessage(), e);
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
