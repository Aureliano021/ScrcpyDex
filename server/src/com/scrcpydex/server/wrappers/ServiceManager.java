package com.scrcpydex.server.wrappers;

import java.lang.reflect.Method;
import android.os.IBinder;

/**
 * Wrapper de reflexão para a classe oculta do Android android.os.ServiceManager.
 * 
 * Permite obter referências de Binder para serviços centrais do sistema operacional
 * (como "display", "input", "window") a partir de um processo executado via app_process.
 */
public final class ServiceManager {
    private static final Method GET_SERVICE_METHOD;

    static {
        try {
            Class<?> serviceManagerClass = Class.forName("android.os.ServiceManager");
            GET_SERVICE_METHOD = serviceManagerClass.getMethod("getService", String.class);
        } catch (Exception e) {
            throw new AssertionError("Falha crítica ao carregar android.os.ServiceManager via reflexão", e);
        }
    }

    private ServiceManager() {
        // Classe utilitária
    }

    /**
     * Retorna a interface IBinder de um serviço do sistema pelo seu nome público.
     * 
     * @param serviceName Nome do serviço (ex: "display", "input", "package")
     * @return IBinder para comunicação IPC
     */
    public static IBinder getService(String serviceName) {
        try {
            IBinder binder = (IBinder) GET_SERVICE_METHOD.invoke(null, serviceName);
            if (binder == null) {
                throw new IllegalStateException("Serviço do sistema não disponível: " + serviceName);
            }
            return binder;
        } catch (Exception e) {
            throw new RuntimeException("Erro ao consultar serviço " + serviceName + " no ServiceManager", e);
        }
    }
}
