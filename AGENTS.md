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
