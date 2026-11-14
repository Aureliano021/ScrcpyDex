# Agent Rules

## Communication
- Never use hyperbolic or absolute language ("100%", "perfect", "guaranteed", 
  "super", "ultra", "flawless", "absolute success"). Report outcomes factually 
  and neutrally.
- State confidence and uncertainty explicitly. If something wasn't verified 
  end-to-end, say so instead of declaring full success.
- Prefer plain, technical wording over marketing-style superlatives.

## Language
- All repository code, comments, in-repo documentation (`ScrcpyDex/docs/`), logs, and the default user interface text (labels, buttons, tooltips, dialogs, notifications, etc.) must be written in English.
- Localized user interface strings are permitted only inside dedicated translation resource files (e.g., `.resx`). English is the default and fallback language, and every string must exist in English first.
- Portuguese is permitted in chat interactions with the user, in translation resource files, and in the external Obsidian knowledge base (`C:\obsidian\Projetos\ScrcpyDex\DOCS`).

## Obsidian Knowledge Base Synchronization
- **Pre-Task Context Acquisition**: Whenever starting to analyze, debug, or implement any feature or change, first consult the external Obsidian documentation at `C:\obsidian\Projetos\ScrcpyDex\DOCS` (starting at `00 - Índice e Mapa de Conteúdo (MOC).md`) to rapidly acquire system context before scanning the repository.
- **Post-Task Documentation Maintenance**: Whenever concluding an implementation, architectural change, or technical analysis, verify and update the affected topic notes in `C:\obsidian\Projetos\ScrcpyDex\DOCS` to correct outdated details and keep the knowledge base synchronized with the current state of the project.

## Privacy & Data Minimization
- Never commit or document real personal, machine, network, or hardware identifiers.
- Redact or replace real hardware serial numbers (e.g., Android/ADB device serials), IMEIs, MAC addresses, public/private IP addresses, and physical host usernames with generic placeholders (e.g., `<DEVICE_SERIAL>`, `R5CW123456`, `<HOST_IP>`, `<USERNAME>`).
- Mock fixtures, unit tests, and documentation examples must exclusively use synthetic, non-identifying values.

## Operational Traps & Critical Agent Alerts
- **Working Directory Trap**: The outer directory (`scrcpy-dex/`) is a wrapper containing NO `.git` folder and NO `.sln`/`.csproj` files. Always change directory to `ScrcpyDex/` (e.g. `cd ScrcpyDex; ...`) before executing Git, .NET, or build scripts. Running commands in the outer directory will fail.
- **.NET SDK Resolution**: If the standard `dotnet` command is not available in the active PowerShell session PATH, invoke the local host binary via `& "$env:LOCALAPPDATA\Microsoft\dotnet\dotnet.exe"`.
- **Live User Testing Locks (`MSB3026` / `MSB3027`)**: The user frequently tests the GUI (`ScrcpyDex.App`) while pair programming. If a build fails due to a locked `.dll` or `.exe`, NEVER kill the user's running application. Verify code validity using `/t:Compile` or build to an isolated directory (`-o <temp>`), and run unit tests against test assemblies.
- **Diagnostic Stream Trap**: External CLI tools (`scrcpy`, `adb`) write benign transfer statistics (`file pushed`, `MB/s`), daemon notices, and harmless framework warnings to `stderr`. Never treat non-empty `stderr` as an immediate failure or crash.
- **Process Scoping**: Never run blanket termination commands (`taskkill /F /IM scrcpy.exe`, `pkill scrcpy`). Users often have unrelated Android/ADB sessions running on the host machine.
- **CRLF / LF Matching on Windows**: Line-ending mismatches can cause targeted string replacement tools to fail. When targeted replacements fail, re-verify line endings or write the full file.

## Process & Stream Management
- Treat external CLI tool streams (`scrcpy`, `adb`) as diagnostic streams rather than pure failure indicators.
- Classify process output into three distinct tiers: `Info` (progress, transfer statistics, daemon notices), `Warning` (non-fatal alerts, DRM buffer notices, deprecations), and `Error` (critical failures, fatal exceptions, unhandled crashes).
- Never use blanket process termination commands (`taskkill /F /IM ...`, `pkill`) that could terminate unrelated host processes; scope process termination strictly to managed child PIDs.

## Build & Verification Safety
- When .NET builds encounter file access locks (`MSB3026` / `MSB3027`) caused by active user testing, do not terminate running application processes. Verify compilation using `/t:Compile` or an isolated output directory (`-o <temp>`), and run unit tests against test project assemblies.

## Localization & Resource Integrity
- Maintain strict 1:1 parity between `Strings.resx` and `Strings.Designer.cs`. Every resource key added or modified in the XML resource file must have an equivalent property in the strongly-typed designer class.

## Repository Structure & Workspace Hygiene
- Canonical project root is strictly `ScrcpyDex/`. The outer directory (`scrcpy-dex/`) is a wrapper and must only contain `ScrcpyDex/`, root `README.md`, `AGENTS.md`, and `.gitignore`. Never create or duplicate top-level project directories (`.github/`, `docs/`, `packaging/`, `scripts/`, `server/`, `tests/`) in the outer workspace.
- Release distribution artifacts (`*.zip`, `*.exe`, `*.deb`) must never be placed or left loose in the repository root or source folders. All release binaries must reside strictly under `ScrcpyDex/publish/` (ignored by VCS).
- Never commit, bundle, or distribute proprietary vendor code, decompiled system dumps, or closed framework binaries. All platform interoperability must rely exclusively on clean-room implementations and dynamic runtime reflection over standard Android service interfaces.

## Commit Standards for Releases & Milestones
- When concluding a release audit, major feature set, or user testing cycle, consolidate changes into comprehensive, well-structured milestone commits rather than scattered incremental micro-commits.
- Commit messages must reflect the full scope of changes against the previous baseline, following Conventional Commits format (`feat(...)`, `fix(...)`, `refactor(...)`) with bulleted technical breakdowns categorized by subsystem (Logging & Diagnostics, UI & Console UX, Localization, Architecture & Hygiene).
