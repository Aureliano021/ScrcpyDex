# Documentação Técnica da Engenharia Reversa do Samsung DeX
**Sistema Alvo:** One UI 8.5 / Android 16 (Samsung Galaxy S23 `SM-S911B`)  
**Arquivos Binários Analisados:** `/system/framework/services.jar` e `/system/framework/framework.jar`  
**Container DEX:** Formato v41 (Páginas de 16 KB)  

---

## 1. Visão Geral da Arquitetura do Samsung DeX no Android 16

Diferente do desktop padrão do Google AOSP (que é apenas um modo de exibição de janelas livres simples), o **Samsung DeX** é um subsistema proprietário profundamente acoplado ao `system_server` da Samsung.

Ele depende da coordenação entre três subsistemas centrais:
```
                      [ SemWifiDisplayConfig ]
                                 │
                                 ▼
                    [ DisplayManagerService ]
                    /                       \
 [ WifiDisplayAdapter ]                   [ LogicalDisplayMapper ]
          │                                          │
 [ RemoteDisplay (RTSP) ]                            ▼
          │                                  [ LogicalDisplay ]
          │                                (Verifica Flags DeX)
          ▼                                          │
[ Display "ScrcpyDeX" ] ── (FLAG_EXTERNAL_DEX_HOSTING)
                                                     │
                                                     ▼
                                          [ DexController (WM) ]
                                                     │
                                     ┌───────────────┴───────────────┐
                                     ▼                               ▼
                           [ SecondaryLauncher ]            [ DexTaskbarWindow ]
```

---

## 2. A "Chave-Mestra": `LogicalDisplay.java`

O arquivo `com.android.server.display.LogicalDisplay` é o guardião responsável por atribuir as características lógicas a qualquer tela do Android.

### Trecho de Código Descompilado (`LogicalDisplay.java`):

```java
// Localização: com.android.server.display.LogicalDisplay.java (linhas 288-305)

if (this.mCanHostTasks && (
        displayInfo.type == 2 || 
        (deviceFlags & 67108864) != 0 || 
        (CoreRune.DW_EXTERNAL_OVERLAY_DISPLAY && displayInfo.type == 4)
    )) {
    displayInfo.flags |= 131072; // 0x20000 = FLAG_EXTERNAL_DEX_HOSTING
}
```

### Análise das Constantes e Flags:
* **`FLAG_EXTERNAL_DEX_HOSTING` (`131072` / `0x20000`):** É a flag obrigatória. Sem ela, o `DexController` do WindowManager ignora a tela e a trata como um espelho de celular comum.
* **`displayInfo.type == 2` (`Display.TYPE_EXTERNAL`):** Telas HDMI/DisplayPort conectadas fisicamente pela porta USB-C.
* **`deviceFlags & 67108864` (`0x4000000`):** Flag privada `FLAG_WIRELESS_DEX_DISPLAY`.
* **`CoreRune.DW_EXTERNAL_OVERLAY_DISPLAY`:** Recurso de teste interno da Samsung para emuladores com `type == 4` (`TYPE_OVERLAY`), desativado em aparelhos de produção (`false`).
* **Por que `VirtualDisplay` comum falha:** Todo display criado por `DisplayManager.createVirtualDisplay(...)` recebe `type = 5` (`Display.TYPE_VIRTUAL`) e **nunca** recebe a flag `67108864`. Logo, a condição falha e o DeX nunca é ativado.

---

## 3. O Ponto de Injeção: `SemWifiDisplayConfig.java`

No `framework.jar`, a Samsung estendeu o sistema de exibição sem fio criando a classe proprietária `android.hardware.display.SemWifiDisplayConfig`.

### Trecho de Código Descompilado (`SemWifiDisplayConfig.java`):

```java
package android.hardware.display;

public final class SemWifiDisplayConfig {
    private String mApConnectionIp;
    private String mApConnectionPort;
    private String mApConnectionName;
    private String mApConnectionMac;
    private int mMode; // 1 = NORMAL_MIRRORING, 2 = MODE_WIRELESS_DEX

    public static final class Builder {
        public Builder setApConnection(String ip, String port, String name, String mac) {
            this.mApConnectionIp = ip;
            this.mApConnectionPort = port;
            this.mApConnectionName = name;
            this.mApConnectionMac = mac;
            return this;
        }

        public Builder setMode(int mode) {
            this.mMode = mode;
            return this;
        }

        public SemWifiDisplayConfig build() {
            return new SemWifiDisplayConfig(...);
        }
    }
}
```

### O que isso possibilita:
Normalmente, o Miracast do Android busca redes Wi-Fi Direct (P2P). No entanto, o método `setApConnection` instrui o sistema a ignorar a interface Wi-Fi física e utilizar um socket TCP/IP preexistente fornecido diretamente na configuração.

---

## 4. O Mecanismo de Conexão: `WifiDisplayAdapter.java`

O `com.android.server.display.WifiDisplayAdapter` gerencia a interface entre o serviço de displays e o controlador Miracast.

### Trecho de Código Descompilado:

```java
// Localização: com.android.server.display.WifiDisplayAdapter.java (linhas 637-665)

public void requestConnectLocked(SemWifiDisplayConfig config, IWifiDisplayConnectionCallback callback) {
    if (config != null && config.isApConnection()) {
        String ip = config.getApConnectionIp();
        int port = Integer.parseInt(config.getApConnectionPort());
        String name = config.getApConnectionName();
        String mac = config.getApConnectionMac();
        int mode = config.getMode();

        Slog.d(TAG, "connectWithConfig via AP: " + ip + ":" + port + ", mode=" + mode);

        // Se mode == 2 (MODE_WIRELESS_DEX), o adaptador marca as flags do dispositivo:
        int deviceFlags = 0;
        if (mode == 2) {
            deviceFlags |= 67108864; // 0x4000000 = FLAG_WIRELESS_DEX_DISPLAY
        }

        // Pula o P2P e conecta diretamente via socket de rede
        this.mDisplayController.requestConnectDirect(ip, port, name, mac, deviceFlags);
    }
}
```

### O Efeito:
Ao passar `mode = 2` e `ip = "127.0.0.1"`, o `WifiDisplayAdapter` injeta a flag mágica `67108864` (`FLAG_WIRELESS_DEX_DISPLAY`) no `DisplayDeviceInfo`, satisfazendo perfeitamente a checagem do `LogicalDisplay.java` sem precisar de nenhum cabo HDMI ou root!

---

## 5. Ciclo de Vida do Socket: `WifiDisplayController.java`

No arquivo `com.android.server.display.WifiDisplayController`, encontramos o motivo dos erros de reconexão e como garantir estabilidade:

### Trecho de Código Descompilado:

```java
// Localização: com.android.server.display.WifiDisplayController.java

public void disconnectWifiDisplay() {
    synchronized (this.mSyncRoot) {
        if (this.mRemoteDisplay != null) {
            this.mRemoteDisplay.dispose(); // Libera o socket TCP e as portas UDP
            this.mRemoteDisplay = null;
        }
        if (this.mAdvertisedDisplay != null) {
            sendDisplayDeviceEventLocked(this.mAdvertisedDisplayDevice, 3); // Remove display
            this.mAdvertisedDisplay = null;
        }
        this.mConnectionState = 0; // STATE_NOT_CONNECTED
    }
}
```

### Lições Críticas da Engenharia Reversa:
1. **Liberação de Porta:** Se um processo encerra abruptamente sem chamar `disconnectWifiDisplay()`, o objeto nativo `RemoteDisplay` permanece em estado `ESTABLISHED` ou `FIN_WAIT` na porta `7236`.
2. **Tempo de Drenagem:** O método nativo `dispose()` assíncrono necessita de aproximadamente `500ms` a `800ms` para desalocar os buffers de socket no kernel do Android. Por isso, a inclusão do `Thread.sleep(800)` no início do [`DexTriggerTest.java`](file:///C:/Users/aurel/OneDrive/Documents/scrcpy-dex/DexTriggerTest.java) eliminou em definitivo os erros de porta ocupada (`EADDRINUSE`).

---

## 6. O Gerenciador de Janelas: `DexController.java`

No subsistema de janelas (`com.android.server.wm.DexController`), descompilamos as regras que acionam a interface de usuário do DeX:

### Trecho de Código Descompilado:

```java
// Localização: com.android.server.wm.DexController.java

public void onDisplayAdded(int displayId) {
    DisplayContent displayContent = this.mWmService.mRoot.getDisplayContent(displayId);
    if (displayContent != null && (displayContent.mDisplayInfo.flags & 131072) != 0) {
        Slog.i(TAG, "DeX display added: " + displayId + ", launching desktop environment...");
        
        // 1. Configura janela em modo livre (Freeform Mode)
        displayContent.setWindowingMode(5); // WINDOWING_MODE_FREEFORM
        
        // 2. Dispara o launcher da área de trabalho Samsung
        Intent homeIntent = new Intent("android.intent.action.MAIN");
        homeIntent.addCategory("android.intent.category.SECONDARY_HOME");
        homeIntent.setComponent(new ComponentName(
            "com.sec.android.app.desktoplauncher",
            "com.sec.android.app.desktoplauncher.SecondaryLauncher"
        ));
        homeIntent.putExtra("android.intent.extra.DISPLAY_ID", displayId);
        this.mContext.startActivityAsUser(homeIntent, UserHandle.CURRENT);

        // 3. Inicia a barra de tarefas proprietária
        this.mDexTaskbarController.showTaskbar(displayId);
    }
}
```

Isso explica por que, no momento em que o nosso handshake RTSP termina em `127.0.0.1:7236`, a barra de tarefas do DeX, o menu Iniciar e o suporte a janelas flutuantes surgem instantaneamente.

---

## 7. A Resolução do Bloqueio de Espelhamento (`VirtualDisplayAdapter`)

Durante a tentativa do `scrcpy` de espelhar o `Display 7`, inspecionamos o `VirtualDisplayAdapter.java`:

```java
// Localização: com.android.server.display.VirtualDisplayAdapter.java (linhas 161-167)

boolean z = (i2 & 16) == 0 && (i2 & 1) == 0 && (34816 & i2) == 0;
this.mNeverBlank = z;
if (z) {
    this.mDisplayState = 2; // STATE_ON
} else {
    this.mDisplayState = 0; // STATE_UNKNOWN / OFF
}
```

* Quando um gravador passa a flag `VIRTUAL_DISPLAY_FLAG_AUTO_MIRROR` (`16`), a variável `z` vira `false`.
* Isso faz o display espelho nascer em estado `OFF` até receber uma transição de energia explícita ou até que um cliente interativo direto (como o executável do `scrcpy` executado pelo usuário no Windows) solicite o pipeline de captura de superfícies via `MediaCodec`.

---

## 8. Conclusão da Análise de Engenharia Reversa

A ativação do Samsung DeX via USB sem root só foi possível porque conectamos com precisão cirúrgica três pontas do sistema operacional:

1. **Permissão Válida:** O shell ADB possui permissão `CONFIGURE_WIFI_DISPLAY`.
2. **API Sem Fio via Loopback:** A classe oculta `SemWifiDisplayConfig` aceita conexões diretas via IP local (`127.0.0.1`), dispensando qualquer hardware Wi-Fi.
3. **Flag Nativa Injetada:** O `WifiDisplayAdapter` atribui a flag binária `67108864` (`FLAG_WIRELESS_DEX_DISPLAY`), que o `LogicalDisplay` converte na flag mestre `131072` (`FLAG_EXTERNAL_DEX_HOSTING`), acordando todo o ecossistema do Samsung DeX diretamente para a captura ultrarrápida do `scrcpy` via cabo USB.
