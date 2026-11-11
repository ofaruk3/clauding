# Changelog

The source of truth is [`web/changelog.json`](web/changelog.json) (English + Turkish, shown in Settings → About).
Every release needs an entry there first: the self-test fails when the top entry does not match the version being built,
and the release workflow turns that entry into the GitHub release notes.

## 0.1.2 · 2026-11-11

- Fix: after an update was downloaded, the next Claude prompt could close Clauding instead of restarting it. Updates now install only when Clauding itself starts, quits or restarts from the tray.
- Updating from 0.1.1 to this version brings Clauding back up on its own if it closed.

Known issue: the full-screen peek (hovering over the small picture) does not show yet.

## 0.1.1 · 2026-11-11

- Your tiled Claudes now share one Clauding button on the taskbar instead of one button each. Click it to bring them all forward, click again to hide them, once more to bring them back.
- Hovering the button shows a small picture of your Claudes.
- Detached terminals, and ones you minimize on their own, keep their own taskbar button so you can always find them.
- New setting: General → One taskbar button (on by default).
- Settings → About now lists what's new.

Known issue: the full-screen peek (hovering over the small picture) does not show yet.

## 0.1.0 · 2026-11-11

- First public release.
