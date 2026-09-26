# Relatório Técnico: Etapa 3 — Cliente Unificado, Desligamento Automatizado e Mouse Físico UHID

**Data:** 25/09/2026  
**Dispositivo de Teste:** Samsung Galaxy S23 (`SM-S911B`), One UI 8.5 / Android 16 (`RQCW201XFDX`)  
**Status do Hard Gate 3:** **APROVADO & VALIDADO EM HARDWARE**  

---

## 1. Resumo Executivo

A Etapa 3 consolidou todas as descobertas anteriores em uma experiência de usuário de nível desktop comercial:
1. **Janela Única de Alta Performance:** Eliminação completa da sobreposição de janelas duplas (vídeo vs canvas de input), unificando a decodificação Direct3D11 acelerada por GPU, reprodução de áudio de baixa latência e controle de input em uma única janela.
2. **Ciclo de Vida e Desligamento 100% Automatizado:** Ao fechar a janela (clique no `X` ou `Alt+F4`), os processos no PC e no Galaxy S23 encerram instantaneamente a sessão DeX, restaurando o celular ao estado normal em menos de 1 segundo, sem processos órfãos.
3. **Mouse Físico Real Nativo (Kernel UHID):** Superação do modelo de emulação de toque (`--mouse=sdk`), ativando o driver virtual de mouse no kernel Linux (`/dev/uhid`), proporcionando a setinha de cursor nativa do Samsung DeX com hover real, clique direito de menu de contexto e scroll suave.

---

## 2. Diagnóstico das Limitações Anteriores

| Componente | Abordagem Anterior (Gate 2) | Limitação Identificada | Nova Solução (Gate 3) |
|---|---|---|---|
| **Janelas** | `ffplay` de vídeo + Janela WinForms preta de input | Usuário precisava alternar e redimensionar manualmente as duas janelas. | Janela única Direct3D11 associada diretamente ao `displayId` do DeX. |
| **Input Mouse** | Modo SDK (`--mouse=sdk`) | Cliques tratados como toque com o dedo (`SOURCE_TOUCHSCREEN`), sem cursor visível ao mover o mouse (sem hover) e botão direito disparando `BACK`. | Modo UHID (`--mouse=uhid`), registrando um dispositivo `Classes: CURSOR` no EventHub do kernel. |
| **Teardown** | Script manual complexo com travamentos | Sessão DeX ficava presa no celular após desconexão do PC. | Bloco `finally` automático no PowerShell e script `stop-dex.bat` não-interativo com fallback forçado. |

---

## 3. Arquitetura da Solução Implementada

### 3.1 Loopback Ativador + Bridging de Display
1. O backend `scrcpydex-server.jar` é iniciado com o comando `activate`:
   - Configura o listener RTSP loopback em `127.0.0.1:7236`.
   - Inicializa a interface DeX no `SemDesktopModeManager`.
   - Mantém o heartbeat RTSP (`GET_PARAMETER`) ativo em segundo plano com custo mínimo de CPU.
2. O orquestrador (`run-scrcpydex.ps1`) monitora o `dumpsys display` e captura o display específico instanciado com o nome `"ScrcpyDeX"` (ex: `Display 52` associado ao `wifi:desktop:00:11:22:33:44:55`), ignorando qualquer display virtual residual.
3. O cliente `scrcpy` conecta diretamente a esse `displayId` com aceleração gráfica Direct3D11 e canal de áudio via USB ADB.

### 3.2 Injeção de Mouse Físico via Kernel Linux (`/dev/uhid`)
No Galaxy S23 (Android 16), o usuário `shell` pertence ao grupo `3011(uhid)`. Ao configurar `--mouse=uhid`:
* O cliente cria uma interface virtual HID conectada a `/dev/uhid`.
* O subsistema `EventHub` do Android registra o dispositivo como `Device: scrcpy, Classes: CURSOR, Path: /dev/input/event12`.
* O `InputManagerService` associa automaticamente a porta do mouse à tela de desktop DeX.
* **Resultados:**
  - Cursor clássico de seta do Samsung DeX visível continuamente na tela.
  - Hover ativo (ícones ganham destaque antes do clique, tooltips aparecem).
  - Botão direito abre menus de contexto do DeX.
  - A tecla `Alt` (Left Alt) alterna suavemente a captura do cursor entre o DeX e a área de trabalho do Windows.

---

## 4. Scripts e Binários Entregues

1. **`ScrcpyDeX.bat`**: Lançador de 1 clique para o usuário final, com resolução de path independente e compatibilidade com PowerShell 5.1 e 7+.
2. **`run-scrcpydex.ps1`**: Orquestrador inteligente do ciclo de vida, contendo detecção dinâmica de display, injeção UHID e bloco de limpeza `finally`.
3. **`stop-dex.bat`**: Botão de emergência / kill switch de desligamento instantâneo.
4. **`server/scrcpydex-server.jar`**: Binário compilado com os modos `activate`, `control` e `capture`.

---

## 5. Validação em Hardware

- **Aparelho:** Samsung Galaxy S23 (SM-S911B)
- **Display DeX Alocado:** `Display 44`
- **Dispositivo de Input Criado:** `scrcpy` (Classes: `CURSOR`, `/dev/input/event12`)
- **Renderizador de Vídeo:** Direct3D11 (60 FPS, sem drop de quadros)
- **Áudio:** Encaminhado nativamente do DeX para as caixas/fones do PC.
- **Encerramento:** Testado e aprovado com encerramento automático sem processos pendentes.
