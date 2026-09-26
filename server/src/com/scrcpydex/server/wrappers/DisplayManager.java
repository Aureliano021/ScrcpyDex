package com.scrcpydex.server.wrappers;

import java.lang.reflect.Method;
import android.os.IBinder;
import com.scrcpydex.server.Ln;

/**
 * Wrapper de reflexão para o serviço IDisplayManager do Android.
 * 
 * Permite invocar métodos de gerenciamento de telas, especificamente
 * as APIs de WiFi Display (Miracast) que a Samsung utiliza para ativar
 * o Wireless DeX.
 */
public final class DisplayManager {
    private final Object manager;
    private final Method disconnectWifiDisplayMethod;
    private final Method connectWifiDisplayWithConfigMethod;

    public DisplayManager() {
        try {
            IBinder binder = ServiceManager.getService("display");
            Class<?> stubClass = Class.forName("android.hardware.display.IDisplayManager$Stub");
            Method asInterface = stubClass.getMethod("asInterface", IBinder.class);
            this.manager = asInterface.invoke(null, binder);

            // Método padrão para desconectar qualquer sessão ativa de Miracast
            this.disconnectWifiDisplayMethod = manager.getClass().getMethod("disconnectWifiDisplay");

            // Método proprietário da Samsung para conectar com SemWifiDisplayConfig
            Method connectMethod = null;
            for (Method m : manager.getClass().getMethods()) {
                if ("connectWifiDisplayWithConfig".equals(m.getName())) {
                    connectMethod = m;
                    break;
                }
            }
            if (connectMethod == null) {
                throw new NoSuchMethodException("Método connectWifiDisplayWithConfig não encontrado em IDisplayManager");
            }
            this.connectWifiDisplayWithConfigMethod = connectMethod;

        } catch (Exception e) {
            throw new RuntimeException("Falha ao inicializar wrapper DisplayManager", e);
        }
    }

    /**
     * Encerra qualquer sessão ativa de WiFi Display / DeX no dispositivo.
     */
    public void disconnectWifiDisplay() {
        try {
            disconnectWifiDisplayMethod.invoke(manager);
            Ln.d("Comando disconnectWifiDisplay executado com sucesso.");
        } catch (Exception e) {
            Ln.w("Falha ao invocar disconnectWifiDisplay: " + e.getMessage());
        }
    }

    /**
     * Inicia uma conexão de WiFi Display usando uma configuração personalizada da Samsung.
     * 
     * @param config Instância de android.hardware.display.SemWifiDisplayConfig
     * @param callback Instância (ou Proxy) de android.hardware.display.IWifiDisplayConnectionCallback
     */
    public void connectWifiDisplayWithConfig(Object config, Object callback) {
        try {
            connectWifiDisplayWithConfigMethod.invoke(manager, config, callback);
            Ln.d("Comando connectWifiDisplayWithConfig disparado.");
        } catch (Exception e) {
            throw new RuntimeException("Erro ao conectar WiFi Display com SemWifiDisplayConfig", e);
        }
    }

    /**
     * Retorna o objeto IDisplayManager subjacente para chamadas adicionais.
     */
    public Object getRawManager() {
        return manager;
    }
}
