# ScrcpyDeX — Samsung DeX for PC via USB

O **ScrcpyDeX** é uma solução de engenharia reversa que ativa e executa o **Samsung DeX nativo** diretamente no PC via cabo USB, proporcionando uma experiência de desktop com **zero latência de Wi-Fi**, aceleração de hardware **Direct3D11**, áudio integrado e mouse físico nativo.

Desenvolvido e testado em hardware real no **Samsung Galaxy S23 (SM-S911B)** rodando **Android 16 / One UI 8.5**.

---

## 🚀 Como Iniciar

1. Conecte seu smartphone Galaxy ao computador via **cabo USB** com a **Depuração USB** ativada.
2. Dê dois cliques em **`ScrcpyDeX.bat`** (ou execute no terminal):
   ```cmd
   .\ScrcpyDeX.bat
   ```
3. A janela única do Samsung DeX abrirá diretamente no monitor com aceleração gráfica e áudio.

---

## 🖱️ Controles e Atalhos do Mouse e Janela

| Ação | Atalho / Comando | Descrição |
|---|---|---|
| **Alternar Tela Cheia** | `Alt + F` (ou `F11`) | Expande o Samsung DeX para o monitor inteiro. |
| **Liberar Mouse para o PC** | `Alt` (Alt Esquerdo) ou `Win` | Solta o cursor da janela do DeX de volta para a área de trabalho do Windows. |
| **Menu de Contexto** | `Botão Direito` | Abre menus de contexto nativos do DeX (organizar ícones, opções, etc.). |
| **Voltar (Android Back)** | `Shift + Botão Direito` | Envia o comando Voltar nativo do Android. |
| **Início (Home)** | `Shift + Clique da Rodinha` | Retorna para a tela de início do DeX. |
| **Desligar e Restaurar** | `Alt + F4` ou Fechar (`X`) | Encerra a sessão DeX no PC e desativa o modo DeX no smartphone automaticamente. |

---

## 🛑 Desligamento de Emergência (Kill Switch)

Caso deseje forçar o desligamento imediato de todas as sessões em segundo plano:
```cmd
.\stop-dex.bat
```
Restaura o Galaxy ao estado padrão em menos de 1 segundo.

---

## 🛠️ Arquitetura do Sistema

1. **Ativador Miracast Loopback (`server/scrcpydex-server.jar`):**
   * Inicializa o subsistema `SemWifiDisplay` em `127.0.0.1:7236` via IPC nativa sem necessidade de root.
   * Ativa as flags internas `FLAG_WIRELESS_DEX_DISPLAY` e `FLAG_EXTERNAL_DEX_HOSTING` no `DisplayManager`.
2. **Orquestrador de Ciclo de Vida (`run-scrcpydex.ps1`):**
   * Detecta com precisão cirúrgica o display registrado como `"ScrcpyDeX"`.
   * Lança o cliente com aceleração gráfica Direct3D11 e mouse UHID.
   * Bloco `finally` resiliente garante o teardown limpo na desconexão.
3. **Driver de Mouse Físico (`--mouse=uhid`):**
   * Cria um nó `/dev/uhid` no kernel Linux do Galaxy S23 sob o grupo `uhid(3011)`.
   * Reconhecido pelo Android como `Device: scrcpy, Classes: CURSOR`.
   * Oferece renderização contínua da seta do mouse nativa, hover real e menus secundários.

---

## 📂 Relatórios Técnicos

- [Etapa 1: Estrutura, Loopback RTSP e Codificação H.264](relatórios/scrcpyDex/01_estrutura_e_fundacao_servidor.md)
- [Etapa 2: Canal de Controle TCP e Injeção de Input](relatórios/scrcpyDex/02_etapa2_canal_de_controle_e_injecao_input.md)
- [Etapa 3: Cliente Unificado, Desligamento Automatizado e Mouse UHID](relatórios/scrcpyDex/03_etapa3_cliente_unificado_e_input_uhid.md)
