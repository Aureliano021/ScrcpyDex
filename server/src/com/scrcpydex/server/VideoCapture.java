package com.scrcpydex.server;

import android.hardware.display.DisplayManager;
import android.hardware.display.VirtualDisplay;
import android.media.MediaCodec;
import android.media.MediaCodecInfo;
import android.media.MediaFormat;
import android.view.Surface;

import java.io.IOException;
import java.io.OutputStream;
import java.lang.reflect.Method;
import java.nio.ByteBuffer;
import java.util.concurrent.atomic.AtomicBoolean;

/**
 * Real-Time Video Capture and Encoding Module (H.264 / MediaCodec).
 * 
 * Uses the native DisplayManager.createVirtualDisplay(name, w, h, displayIdToMirror, surface)
 * API introduced in Android 14+ to mirror the DeX display directly to the
 * MediaCodec hardware encoder with minimum latency, streaming Annex B frames over TCP socket.
 */
public class VideoCapture {
    private static final String MIME_TYPE = "video/avc"; // H.264
    private static final int DEFAULT_BITRATE = 8_000_000; // 8 Mbps
    private static final int DEFAULT_FPS = 60;
    private static final int DEFAULT_I_FRAME_INTERVAL = 10; // Seconds between keyframes
    private static final long REPEAT_FRAME_DELAY_US = 100_000; // 100ms to keep stream alive

    private final int displayId;
    private final int width;
    private final int height;
    private final int bitrate;
    private final int fps;
    private final OutputStream videoOutput;
    private final AtomicBoolean running = new AtomicBoolean(false);

    private MediaCodec codec;
    private Surface inputSurface;
    private VirtualDisplay virtualDisplay;

    public VideoCapture(int displayId, int width, int height, OutputStream videoOutput) {
        this(displayId, width, height, DEFAULT_BITRATE, DEFAULT_FPS, videoOutput);
    }

    public VideoCapture(int displayId, int width, int height, int bitrate, int fps, OutputStream videoOutput) {
        this.displayId = displayId;
        this.width = width;
        this.height = height;
        this.bitrate = bitrate;
        this.fps = fps;
        this.videoOutput = videoOutput;
    }

    /**
     * Initializes MediaCodec and projects the DeX display to the encoder.
     */
    public void start() throws Exception {
        Ln.i("Initializing H.264 hardware encoder (" + width + "x" + height + " @ " + fps + "fps, " + (bitrate / 1_000_000) + " Mbps)...");

        // 1. Configure MediaFormat with required width and height for minimum latency
        MediaFormat format = MediaFormat.createVideoFormat(MIME_TYPE, width, height);
        format.setInteger(MediaFormat.KEY_BIT_RATE, bitrate);
        format.setInteger(MediaFormat.KEY_FRAME_RATE, fps);
        format.setInteger(MediaFormat.KEY_COLOR_FORMAT, MediaCodecInfo.CodecCapabilities.COLOR_FormatSurface);
        format.setInteger(MediaFormat.KEY_I_FRAME_INTERVAL, 1); // 1 keyframe every 1s for fast video synchronization
        format.setLong(MediaFormat.KEY_REPEAT_PREVIOUS_FRAME_AFTER, 50_000); // Repeat after 50ms to keep continuous stream
        format.setInteger(MediaFormat.KEY_PRIORITY, 0); // Realtime priority
        format.setInteger(MediaFormat.KEY_LATENCY, 1);  // Output frame as soon as ready
        try {
            format.setInteger("prepend-sps-pps-to-idr-frames", 1);
        } catch (Throwable ignored) {}

        // 2. Instantiate and configure MediaCodec
        codec = MediaCodec.createEncoderByType(MIME_TYPE);
        codec.configure(format, null, null, MediaCodec.CONFIGURE_FLAG_ENCODE);
        inputSurface = codec.createInputSurface();
        codec.start();

        // 3. Connect DeX mirroring via DisplayManager.createVirtualDisplay (Android 14+)
        Method createVirtualDisplayMethod = DisplayManager.class.getMethod(
            "createVirtualDisplay", String.class, int.class, int.class, int.class, Surface.class);
        this.virtualDisplay = (VirtualDisplay) createVirtualDisplayMethod.invoke(
            null, "scrcpydex", width, height, displayId, inputSurface);
        Ln.i("Mirroring pipeline DisplayManager -> MediaCodec connected successfully!");

        running.set(true);

        // 4. Start encoding and transmission loop
        encodeLoop();
    }

    private void encodeLoop() {
        MediaCodec.BufferInfo bufferInfo = new MediaCodec.BufferInfo();
        byte[] buffer = new byte[65536];

        try {
            boolean firstFrameLogged = false;
            while (running.get()) {
                int outputBufferIndex = codec.dequeueOutputBuffer(bufferInfo, 10_000); // 10ms timeout
                if (outputBufferIndex >= 0) {
                    ByteBuffer outputBuffer = codec.getOutputBuffer(outputBufferIndex);
                    if (outputBuffer != null && bufferInfo.size > 0) {
                        outputBuffer.position(bufferInfo.offset);
                        outputBuffer.limit(bufferInfo.offset + bufferInfo.size);

                        while (outputBuffer.hasRemaining()) {
                            int toRead = Math.min(outputBuffer.remaining(), buffer.length);
                            outputBuffer.get(buffer, 0, toRead);
                            videoOutput.write(buffer, 0, toRead);
                        }
                        videoOutput.flush();

                        if (!firstFrameLogged) {
                            firstFrameLogged = true;
                            Ln.i(">>> First H.264 video frame successfully transmitted to client! <<<");
                        }
                    }
                    codec.releaseOutputBuffer(outputBufferIndex, false);
                } else if (outputBufferIndex == MediaCodec.INFO_OUTPUT_FORMAT_CHANGED) {
                    Ln.d("MediaCodec: Video format configured: " + codec.getOutputFormat());
                }
            }
        } catch (IOException ioe) {
            if (running.get()) {
                Ln.w("Video connection closed by client (Broken pipe).");
            }
        } catch (Exception e) {
            if (running.get()) {
                Ln.e("Error in video encoding loop: " + e.getMessage(), e);
            }
        } finally {
            stop();
        }
    }

    /**
     * Terminates loop and releases graphics resources.
     */
    public void stop() {
        if (!running.getAndSet(false)) {
            return;
        }
        Ln.i("Shutting down video capture pipeline...");

        if (virtualDisplay != null) {
            virtualDisplay.release();
            virtualDisplay = null;
        }
        if (inputSurface != null) {
            inputSurface.release();
            inputSurface = null;
        }
        if (codec != null) {
            try {
                codec.stop();
                codec.release();
            } catch (Exception ignored) {}
            codec = null;
        }
        Ln.i("MediaCodec and VirtualDisplay resources released.");
    }
}
