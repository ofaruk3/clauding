<p align="center"><img src="icon.png" width="96" alt=""></p>

<h1 align="center">Clauding</h1>
<p align="center">A tiling window manager for Claude Code on Windows.<br>Every Claude window tiled, tracked, and one keystroke away.</p>

---

Running five Claude Code sessions at once means five terminal windows to arrange, and no idea which one is waiting for you. Clauding tiles them for you, draws a quiet overlay behind them, and marks each window: **working**, **needs you** (a permission prompt or a question) or **done**.

## What it does

- **Tiles every Claude window** in a work area on your main display, and moves the overflow onto a second display.
- **Shows each Claude's state.** A small Claude Code hook reports when a session is waiting for you or done; the tile's corners and header light up.
- **Keyboard first.** One modifier combo (default <kbd>Ctrl</kbd>+<kbd>Win</kbd>+<kbd>Alt</kbd>) for everything: open a Claude, jump to the one waiting for you, move focus, maximize, change layout. Hold the combo for a moment to see every shortcut.
- **Layouts:** auto grid, columns, main + stack and rows. Advanced rules work like a tiling WM, for example "if tiles get narrower than 1100 px, put at most 2 in a row". Portrait displays get their own layout.
- **Several project folders.** The New Claude shortcut opens the default one; turn on the picker and press <kbd>1</kbd>–<kbd>9</kbd> for the others.
- **Any shell:** PowerShell 7, Windows PowerShell, Command Prompt, Git Bash or WSL, in Windows Terminal or the classic console.
- **Drag to rearrange:** drag a header onto another tile to swap them, drag it out to float the window, drag the dividers to resize.
- **Themes:** Charcoal, Ember and Paper, with an accent color. English and Turkish.
- **Macro pads:** CH57x 3×4 + 2 knob pads are detected; their keys and knobs can be mapped in the Shortcuts panel.
- **Updates itself** from GitHub Releases.

## Install

1. Download `Clauding-win-Setup.exe` from the [latest release](https://github.com/ofaruk3/clauding/releases/latest) and run it. It installs for your user only (no admin rights) and installs the .NET 9 Desktop Runtime if it is missing.
2. The installer is not code-signed yet, so Windows SmartScreen may warn you. Choose **More info → Run anyway**.
3. A short setup walks you through the shell, your project folders, the status hooks, the shortcut key, the layout and the look. Everything can be changed later in **Settings** (tray icon → Settings…).

**Requirements:** Windows 10 or 11, [Claude Code](https://docs.anthropic.com/en/docs/claude-code/setup) on your PATH. Windows Terminal is recommended; without it Clauding uses the classic console.

## Default shortcuts

All shortcuts use <kbd>Ctrl</kbd>+<kbd>Win</kbd>+<kbd>Alt</kbd> plus a key. Every action can get its own combo in **Shortcuts** (hold the combo, or tray → Shortcuts…).

| Key | Action | Key | Action |
|---|---|---|---|
| <kbd>N</kbd> | New Claude | <kbd>←</kbd> <kbd>↑</kbd> <kbd>→</kbd> <kbd>↓</kbd> | Move focus |
| <kbd>W</kbd> | Go to the Claude waiting for you | <kbd>1</kbd>–<kbd>9</kbd> | Go to tile 1–9 |
| <kbd>Y</kbd> | Approve (Enter on the permission prompt) | <kbd>,</kbd> <kbd>.</kbd> | Move earlier / later in the order |
| <kbd>Esc</kbd> | Interrupt the Claude you are in | <kbd>Enter</kbd> | Maximize / restore |
| <kbd>Q</kbd> | Close | <kbd>D</kbd> | Detach / attach |
| <kbd>F</kbd> <kbd>[</kbd> <kbd>]</kbd> | Area: full / left half / right half | <kbd>A</kbd> | Cycle the area |
| <kbd>L</kbd> | Next layout | <kbd>T</kbd> | Re-tile, bring detached windows back |
| <kbd>M</kbd> | Move everything to the other display | <kbd>H</kbd> | Hide / show all |
| <kbd>P</kbd> | Pause tiling | <kbd>K</kbd> | Shortcuts panel |

Clauding never uses <kbd>Ctrl</kbd>+<kbd>Win</kbd>+<kbd>Alt</kbd>+<kbd>Shift</kbd>, because Windows reserves it for the Office key.

## Status hooks and privacy

To know when a Claude is waiting for you, Clauding adds three hooks to `~/.claude/settings.json` (`Notification`, `Stop`, `UserPromptSubmit`). Each one runs `Clauding.exe --signal …`, which only tells the running app which window the event came from. Nothing is read from your conversations and nothing leaves your PC.

The hooks sit next to your own hooks and never replace them. Before the first change, a copy is saved as `settings.json.clauding-backup`. Turning the hooks off in Settings, or uninstalling Clauding, removes exactly those three entries.

Clauding makes one kind of network request: the update check against this repository's releases (it can be turned off).

## Files

| What | Where |
|---|---|
| Settings | `%APPDATA%\Clauding\settings.json` |
| Logs | `%LOCALAPPDATA%\ClaudingData\logs` (tray → Report a problem… zips them to your desktop) |
| App | `%LOCALAPPDATA%\Clauding` |

## Build from source

```powershell
git clone https://github.com/ofaruk3/clauding
cd clauding
dotnet build -c Release
bin\Release\net9.0-windows\Clauding.exe --self-test results.txt   # layout, shortcut and hook tests
```

Run the built `Clauding.exe` directly to try it. Copies that were not installed by the installer skip update checks.

**Releases:** pushing a tag like `v0.2.0` runs `.github/workflows/release.yml`, which tests, builds, packs with [Velopack](https://velopack.io) and publishes a GitHub Release. `scripts/pack.ps1` builds the same installer locally.

**Code layout:** the tray app is C# (.NET 9, WinForms) in the repo root; the overlay, the shortcuts panel and the setup pages are HTML in `web/`, hosted in WebView2. Identifiers are partly Turkish (the project started as a personal tool); comments are in English.

## FAQ

**Does it work with WSL?** Yes, Claude starts inside your default WSL distribution. The status hooks are not available there yet, so WSL tiles show "working" from the window title but not "needs you".

**Single display, no macro pad?** Both are fine. The display and macro pad options only appear when one is connected.

**A shortcut does not work.** Another app has probably taken it. The Shortcuts panel marks those in red; give the action another combo.

## Credits

Made by [ofc.works](https://ofc.works). Claude and Claude Code are products of Anthropic; Clauding is an independent project and is not affiliated with Anthropic. See [THIRD-PARTY-NOTICES.md](THIRD-PARTY-NOTICES.md).

MIT License.
