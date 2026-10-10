# First alpha review — 2026-10-10

Suitable for an initial Windows alpha release, with the manual checks below still outstanding. No upload or Git commit was performed.

## Fixed during review

- Fresh launches now select black ASCII on white rounded stickers. Saved sprite choices still take precedence.
- Personality, sprite choice, and logs now use the user's local application-data directory instead of requiring a writable source/install folder. Existing development preferences migrate without replacing already-saved preferences.
- Packaged builds locate assets beside the executable; development builds retain their repository layout.
- Startup error reporting tolerates a failure to write its diagnostic log.
- Added a root README and portable ZIP packaging script. Build output, personal profiles, logs, and separate ChatGPT art experiments are excluded from Git.

## Validation

Compilation passed using the Windows .NET Framework compiler. All five probes passed in the normal Windows desktop session: 20 behavior checks, 24 cursor-controller checks, the actual modeless personality editor, native platform discovery/jumping/support movement/removal, and startup smoke testing.

The ZIP was extracted into a fresh directory and its self-test and startup smoke test passed with an unrelated working directory. Both sprite sets contain 30 compatible clips and 54 frames each. The ZIP includes the executable, README, and both runtime asset trees; no personal profile is bundled. A scan of app code, scripts, and design documents found no credential-pattern matches. The separate art-generation directory remains local and ignored.

## Before announcing a release

- Manually exercise click-through, petting, dragging, tray controls, and keyboard focus in normal desktop use.
- Check at least one additional device or display configuration, especially scaling and taskbar placement/auto-hide.
- Choose a project license if desired; none has been selected or added.

Primary-monitor support is the current scope. Recoloring, fullscreen hiding, multimonitor roaming, toys, and saved position are not implemented. The executable is unsigned. Automated cursor replay verifies behavior logic without moving the physical mouse; it does not replace the manual interaction checks.

Run `prototype/release.ps1` after future code or asset changes to rebuild the ZIP. Upload source to the repository and the ZIP as a release asset separately; `dist/` is intentionally ignored.
