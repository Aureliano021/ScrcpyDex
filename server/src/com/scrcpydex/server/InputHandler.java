package com.scrcpydex.server;

import android.os.SystemClock;
import android.view.InputDevice;
import android.view.KeyEvent;
import android.view.MotionEvent;

import com.scrcpydex.server.wrappers.InputManager;

/**
 * Input Event Injection Handler (Mouse, Keyboard, and Scroll).
 * 
 * Injects events directly into the Samsung DeX display with calibrated
 * parameters for pressure, size, and native input sources (Touchscreen and Mouse).
 */
public class InputHandler {
    private final int displayId;
    private long lastTouchDown = 0;
    private int currentButtons = 0;

    private final MotionEvent.PointerProperties[] pointerProperties = new MotionEvent.PointerProperties[1];
    private final MotionEvent.PointerCoords[] pointerCoords = new MotionEvent.PointerCoords[1];

    public InputHandler(int displayId) {
        this.displayId = displayId;

        pointerProperties[0] = new MotionEvent.PointerProperties();
        pointerProperties[0].id = 0;
        pointerProperties[0].toolType = MotionEvent.TOOL_TYPE_FINGER;

        pointerCoords[0] = new MotionEvent.PointerCoords();
        pointerCoords[0].pressure = 1.0f;
        pointerCoords[0].size = 1.0f;
    }

    private void setCoordinates(int x, int y, float pressure) {
        pointerCoords[0].x = x;
        pointerCoords[0].y = y;
        pointerCoords[0].pressure = pressure;
        pointerCoords[0].size = 1.0f;
    }

    /**
     * Injects cursor movement without clicking (Hover Move) or with dragging (Move).
     */
    public void handleMouseMove(int x, int y) {
        long now = SystemClock.uptimeMillis();
        if (currentButtons == 0) {
            // Idle cursor moving (Hover)
            pointerProperties[0].toolType = MotionEvent.TOOL_TYPE_MOUSE;
            setCoordinates(x, y, 0.0f);

            MotionEvent event = MotionEvent.obtain(
                now,
                now,
                MotionEvent.ACTION_HOVER_MOVE,
                1,
                pointerProperties,
                pointerCoords,
                0,
                0,
                1.0f,
                1.0f,
                0,
                0,
                InputDevice.SOURCE_MOUSE,
                0
            );
            InputManager.injectInputEvent(event, displayId, InputManager.INJECT_INPUT_EVENT_MODE_ASYNC);
        } else {
            // Dragging cursor with button pressed (Move)
            pointerProperties[0].toolType = MotionEvent.TOOL_TYPE_FINGER;
            setCoordinates(x, y, 1.0f);

            MotionEvent event = MotionEvent.obtain(
                lastTouchDown,
                now,
                MotionEvent.ACTION_MOVE,
                1,
                pointerProperties,
                pointerCoords,
                0,
                0,
                1.0f,
                1.0f,
                0,
                0,
                InputDevice.SOURCE_TOUCHSCREEN,
                0
            );
            InputManager.injectInputEvent(event, displayId, InputManager.INJECT_INPUT_EVENT_MODE_ASYNC);
        }
    }

    /**
     * Injects mouse button press or release.
     * 
     * Button 1 (Left): Injected as Touchscreen (TOOL_TYPE_FINGER) for
     * maximum compatibility with DeX buttons, icons, and windows.
     * Button 2 (Right): Injected as SOURCE_MOUSE with BUTTON_SECONDARY
     * to open context menus in DeX.
     */
    public void handleMouseButton(int x, int y, int button, int action) {
        long now = SystemClock.uptimeMillis();

        if (button == 2) {
            // Right Button: Context menu in DeX
            pointerProperties[0].toolType = MotionEvent.TOOL_TYPE_MOUSE;
            setCoordinates(x, y, action == 0 ? 1.0f : 0.0f);

            if (action == 0) { // DOWN
                lastTouchDown = now;
                currentButtons |= MotionEvent.BUTTON_SECONDARY;

                MotionEvent pressEvent = MotionEvent.obtain(
                    lastTouchDown,
                    now,
                    MotionEvent.ACTION_BUTTON_PRESS,
                    1,
                    pointerProperties,
                    pointerCoords,
                    0,
                    MotionEvent.BUTTON_SECONDARY,
                    1.0f,
                    1.0f,
                    0,
                    0,
                    InputDevice.SOURCE_MOUSE,
                    0
                );
                InputManager.setActionButton(pressEvent, MotionEvent.BUTTON_SECONDARY);
                InputManager.injectInputEvent(pressEvent, displayId, InputManager.INJECT_INPUT_EVENT_MODE_ASYNC);

            } else { // UP
                currentButtons &= ~MotionEvent.BUTTON_SECONDARY;

                MotionEvent releaseEvent = MotionEvent.obtain(
                    lastTouchDown,
                    now,
                    MotionEvent.ACTION_BUTTON_RELEASE,
                    1,
                    pointerProperties,
                    pointerCoords,
                    0,
                    0,
                    1.0f,
                    1.0f,
                    0,
                    0,
                    InputDevice.SOURCE_MOUSE,
                    0
                );
                InputManager.setActionButton(releaseEvent, MotionEvent.BUTTON_SECONDARY);
                InputManager.injectInputEvent(releaseEvent, displayId, InputManager.INJECT_INPUT_EVENT_MODE_ASYNC);
                lastTouchDown = 0;
            }
            return;
        }

        // Button 1 (Left / Primary):
        pointerProperties[0].toolType = MotionEvent.TOOL_TYPE_FINGER;

        if (action == 0) { // DOWN
            lastTouchDown = now;
            currentButtons |= MotionEvent.BUTTON_PRIMARY;
            setCoordinates(x, y, 1.0f);

            MotionEvent downEvent = MotionEvent.obtain(
                lastTouchDown,
                now,
                MotionEvent.ACTION_DOWN,
                1,
                pointerProperties,
                pointerCoords,
                0,
                0,
                1.0f,
                1.0f,
                0,
                0,
                InputDevice.SOURCE_TOUCHSCREEN,
                0
            );
            InputManager.injectInputEvent(downEvent, displayId, InputManager.INJECT_INPUT_EVENT_MODE_ASYNC);

        } else { // UP
            currentButtons &= ~MotionEvent.BUTTON_PRIMARY;
            setCoordinates(x, y, 1.0f);

            MotionEvent upEvent = MotionEvent.obtain(
                lastTouchDown == 0 ? now : lastTouchDown,
                now,
                MotionEvent.ACTION_UP,
                1,
                pointerProperties,
                pointerCoords,
                0,
                0,
                1.0f,
                1.0f,
                0,
                0,
                InputDevice.SOURCE_TOUCHSCREEN,
                0
            );
            InputManager.injectInputEvent(upEvent, displayId, InputManager.INJECT_INPUT_EVENT_MODE_ASYNC);
            lastTouchDown = 0;
        }
    }

    /**
     * Injects keyboard key event.
     * 
     * @param keyCode Key code (Android KeyEvent.KEYCODE_*)
     * @param action 0=Pressed (DOWN), 1=Released (UP)
     */
    public void handleKeyEvent(int keyCode, int action) {
        long now = SystemClock.uptimeMillis();
        int keyAction = (action == 0) ? KeyEvent.ACTION_DOWN : KeyEvent.ACTION_UP;
        KeyEvent event = new KeyEvent(now, now, keyAction, keyCode, 0);
        event.setSource(InputDevice.SOURCE_KEYBOARD);
        InputManager.injectInputEvent(event, displayId, InputManager.INJECT_INPUT_EVENT_MODE_ASYNC);
    }

    /**
     * Injects mouse wheel scroll event.
     */
    public void handleScroll(int x, int y, float hScroll, float vScroll) {
        long now = SystemClock.uptimeMillis();
        pointerProperties[0].toolType = MotionEvent.TOOL_TYPE_MOUSE;
        setCoordinates(x, y, 0.0f);
        pointerCoords[0].setAxisValue(MotionEvent.AXIS_HSCROLL, hScroll);
        pointerCoords[0].setAxisValue(MotionEvent.AXIS_VSCROLL, vScroll);

        MotionEvent event = MotionEvent.obtain(
            lastTouchDown == 0 ? now : lastTouchDown,
            now,
            MotionEvent.ACTION_SCROLL,
            1,
            pointerProperties,
            pointerCoords,
            0,
            0,
            1.0f,
            1.0f,
            0,
            0,
            InputDevice.SOURCE_MOUSE,
            0
        );
        InputManager.injectInputEvent(event, displayId, InputManager.INJECT_INPUT_EVENT_MODE_ASYNC);
    }
}
