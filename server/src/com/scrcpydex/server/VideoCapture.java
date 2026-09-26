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
 * Módulo de Captura e Codificação de Vídeo em Tempo Real (H.264 / MediaCodec).
 * 
 * Utiliza a API nativa DisplayManager.createVirtualDisplay(name, w, h, displayIdToMirror, surface)
 * introduzida no Android 14+ para espelhar o display DeX diretamente para o codificador
 * de hardware MediaCodec com latência mínima, transmitindo frames Annex B pelo socket TCP.
 */
public class VideoCapture {
    private static final String MIME_TYPE = "video/avc"; // H.264
    private static final int DEFAULT_BITRATE = 8_000_000; // 8 Mbps
    private static final int DEFAULT_FPS = 60;
    private static final int DEFAULT_I_FRAME_INTERVAL = 10; // Segundos entre keyframes
    private static final long REPEAT_FRAME_DELAY_US = 100_000; // 100ms para manter stream vivo

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
     * Inicializa o MediaCodec e projeta o display DeX para o encoder.
     */
    public void start() throws Exception {
        Ln.i("Inicializando codificador de hardware H.264 (" + width + "x" + height + " @ " + fps + "fps, " + (bitrate / 1_000_000) + " Mbps)...");

        // 1. Configurar MediaFormat com largura e altura obrigatórias para latência mínima
        MediaFormat format = MediaFormat.createVideoFormat(MIME_TYPE, width, height);
        format.setInteger(MediaFormat.KEY_BIT_RATE, bitrate);
        format.setInteger(MediaFormat.KEY_FRAME_RATE, fps);
        format.setInteger(MediaFormat.KEY_COLOR_FORMAT, MediaCodecInfo.CodecCapabilities.COLOR_FormatSurface);
        format.setInteger(MediaFormat.KEY_I_FRAME_INTERVAL, 1); // 1 keyframe a cada 1s para sincronização rápida de vídeo
        format.setLong(MediaFormat.KEY_REPEAT_PREVIOUS_FRAME_AFTER, 50_000); // Repetir após 50ms para manter fluxo contínuo
        format.setInteger(MediaFormat.KEY_PRIORITY, 0); // Prioridade de tempo real
        format.setInteger(MediaFormat.KEY_LATENCY, 1);  // Emite frame assim que pronto
        try {
            format.setInteger("prepend-sps-pps-to-idr-frames", 1);
        } catch (Throwable ignored) {}

        // 2. Instanciar e configurar o MediaCodec
        codec = MediaCodec.createEncoderByType(MIME_TYPE);
        codec.configure(format, null, null, MediaCodec.CONFIGURE_FLAG_ENCODE);
        inputSurface = codec.createInputSurface();
        codec.start();

        // 3. Conectar espelhamento do DeX via DisplayManager.createVirtualDisplay (Android 14+)
        Method createVirtualDisplayMethod = DisplayManager.class.getMethod(
            "createVirtualDisplay", String.class, int.class, int.class, int.class, Surface.class);
        this.virtualDisplay = (VirtualDisplay) createVirtualDisplayMethod.invoke(
            null, "scrcpydex", width, height, displayId, inputSurface);
        Ln.i("Pipeline de espelhamento DisplayManager ➔ MediaCodec conectado com sucesso!");

        running.set(true);

        // 4. Iniciar loop de codificação e transmissão
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
                            Ln.i(">>> Primeiro frame de vídeo H.264 transmitido com sucesso ao cliente! <<<");
                        }
                    }
                    codec.releaseOutputBuffer(outputBufferIndex, false);
                } else if (outputBufferIndex == MediaCodec.INFO_OUTPUT_FORMAT_CHANGED) {
                    Ln.d("MediaCodec: Formato de vídeo configurado: " + codec.getOutputFormat());
                }
            }
        } catch (IOException ioe) {
            if (running.get()) {
                Ln.w("Conexão de vídeo encerrada pelo cliente (Broken pipe).");
            }
        } catch (Exception e) {
            if (running.get()) {
                Ln.e("Erro no loop de codificação de vídeo: " + e.getMessage(), e);
            }
        } finally {
            stop();
        }
    }

    /**
     * Encerra o loop e libera os recursos gráficos.
     */
    public void stop() {
        if (!running.getAndSet(false)) {
            return;
        }
        Ln.i("Finalizando pipeline de captura de vídeo...");

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
        Ln.i("Recursos do MediaCodec e VirtualDisplay liberados.");
    }
}
