# Agent Rules

## Communication
- Never use hyperbolic or absolute language ("100%", "perfect", "guaranteed", 
  "super", "ultra", "flawless", "absolute success"). Report outcomes factually 
  and neutrally.
- State confidence and uncertainty explicitly. If something wasn't verified 
  end-to-end, say so instead of declaring full success.
- Prefer plain, technical wording over marketing-style superlatives.

## Language
- All code, comments, documentation, logs, and the default user interface text (labels, buttons, tooltips, dialogs, notifications, etc.) must be written in English.
- Localized user interface strings are permitted only inside dedicated translation resource files (e.g., `.resx`). English is the default and fallback language, and every string must exist in English first.
- Portuguese is permitted only in chat interactions with the user and in translation resource files.

## Privacy & Data Minimization
- Never commit or document real personal, machine, network, or hardware identifiers.
- Redact or replace real hardware serial numbers (e.g., Android/ADB device serials), IMEIs, MAC addresses, public/private IP addresses, and physical host usernames with generic placeholders (e.g., `<DEVICE_SERIAL>`, `R5CW123456`, `<HOST_IP>`, `<USERNAME>`).
- Mock fixtures, unit tests, and documentation examples must exclusively use synthetic, non-identifying values.

## Process & Stream Management
- Treat external CLI tool streams (`scrcpy`, `adb`) as diagnostic streams rather than pure failure indicators.
- Classify process output into three distinct tiers: `Info` (progress, transfer statistics, daemon notices), `Warning` (non-fatal alerts, DRM buffer notices, deprecations), and `Error` (critical failures, fatal exceptions, unhandled crashes).
- Never use blanket process termination commands (`taskkill /F /IM ...`, `pkill`) that could terminate unrelated host processes; scope process termination strictly to managed child PIDs.

## UI & Layout Invariants
- In Avalonia/XAML item templates, status badges (`ERR`, `WARN`, `INFO`) positioned next to multiline or wrapping text must explicitly specify `VerticalAlignment="Top"` to prevent vertical badge stretching.

## Build & Verification Safety
- When .NET builds encounter file access locks (`MSB3026` / `MSB3027`) caused by active user testing, do not terminate running application processes. Verify compilation using `/t:Compile` or an isolated output directory (`-o <temp>`), and run unit tests against test project assemblies.

## Localization & Resource Integrity
- Maintain strict 1:1 parity between `Strings.resx` and `Strings.Designer.cs`. Every resource key added or modified in the XML resource file must have an equivalent property in the strongly-typed designer class.

## Repository Structure & Workspace Hygiene
- Canonical project root is strictly `ScrcpyDex/`. The outer directory (`scrcpy-dex/`) is a workspace wrapper and must only contain `ScrcpyDex/`, root `README.md`, `AGENTS.md`, and `.gitignore`. Never create or duplicate top-level project directories (`.github/`, `docs/`, `packaging/`, `scripts/`, `server/`, `tests/`) in the outer workspace.
- Release distribution artifacts (`*.zip`, `*.exe`, `*.deb`) must never be placed or left loose in the repository root or source folders. All release binaries must reside strictly under `ScrcpyDex/publish/` (ignored by VCS).
- Never commit, bundle, or distribute proprietary vendor code, decompiled system dumps, or closed framework binaries. All platform interoperability must rely exclusively on clean-room implementations and dynamic runtime reflection over standard Android service interfaces.

## Commit Standards for Releases & Milestones
- When concluding a release audit, major feature set, or user testing cycle, consolidate changes into comprehensive, well-structured milestone commits rather than scattered incremental micro-commits.
- Commit messages must reflect the full scope of changes against the previous baseline, following Conventional Commits format (`feat(...)`, `fix(...)`, `refactor(...)`) with bulleted technical breakdowns categorized by subsystem (Logging & Diagnostics, UI & Console UX, Localization, Architecture & Hygiene).
