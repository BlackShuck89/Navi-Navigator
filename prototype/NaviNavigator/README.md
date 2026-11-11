NAVI NAVIGATOR 0.0.1 - WINDOWS TECHNICAL PREVIEW
================================================

This is a RUNNABLE application prototype for Windows 10/11 x64.

HOW TO START
1. Extract the entire ZIP archive to a folder.
2. Run NaviNavigator.exe.
3. If Windows SmartScreen warns about an unrecognized app, be aware
   this early test binary is unsigned. Only run it if you trust its source.
4. The Microsoft Edge WebView2 Evergreen Runtime is required.
   Windows 11 generally already includes it. If it is missing, install
   the official runtime from Microsoft.

BASIC CONTROLS
- Type a website or search into the address bar and press Enter.
- Ctrl+T opens a tab; Ctrl+W closes the active tab; Ctrl+L focuses URL.
- Back / Forward / Reload / Home work via the toolbar.
- Bookmarks > Bookmark This Page (Ctrl+D).
- View > Skins switches between Modern, Classic, and Fusion.
- The status bar shows navigation and download activity.
- WebView2's normal context menu and download behavior are enabled.

IMPORTANT LIMITATIONS
- This test prototype uses Microsoft's WebView2 Chromium renderer,
  NOT the Firefox/Gecko engine planned for the main Navi Navigator fork.
- It is a distinct EXE and does not need Firefox installed, but WebView2
  runtime is required. The .NET runtime is bundled in the ZIP.
- It has not been tested extensively for security or stability; do not
  use for sensitive banking, passwords, or other private tasks.
- It is not code-signed, has no automatic updates, and does not install
  system-wide or modify your default browser.
- Browsing data is stored in your own user profile under:
  %LOCALAPPDATA%\Navi Navigator\Standalone Preview
- Changing skins affects the application's native toolbars and tabs.
  It does not change webpage content or Firefox.
- Navi's full fox mascot artwork is not in this build. The app uses
  an original compass placeholder for now.
- Do not rename the executable to firefox.exe or overwrite Firefox.

Source: https://github.com/BlackShuck89/Navi-Navigator
