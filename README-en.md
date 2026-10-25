# Borderless Window Utility

[한국어](README.md) | **English**

A Windows tray program that removes the title bar and borders from game windows and places them at the size and position you choose, automatically.

It is handy on super ultrawide monitors such as 32:9 (5120×1440), for example to run a game at QHD (2560×1440) in the middle of the screen.
It works at any monitor resolution, including 21:9, 32:9 and 4K.
In fullscreen the sides are filled with black bars, and in windowed mode the title bar pushes the bottom of the window off the screen.
This program turns the game window into a borderless window and puts it exactly where you want it.

## Features

- **Automatic**: when a registered game window appears, its borders are removed and its size and position are set within seconds.
- **Left / center / right**: places the window against the side you choose, based on the monitor's resolution
  (2560×1440 on 5120×1440 → left 0, center 1280, right 2560). You can switch it from the tray menu while playing.
- **Easy setup**: pick the game from the window list in the tray menu and add it with one button.
- **Start with Windows**
- **English / Korean**
- **No installation**: a single exe of about 150 KB.

## Requirements

- Windows 10 (1903 or later) or Windows 11
- .NET Framework 4.8 (included with those Windows versions, nothing to install)

## Installation

1. Download `BorderlessWindowUtility.exe` from [Releases](../../releases).
2. Put it in any folder and run it. A tray icon appears in the notification area at the right of the taskbar.
3. Right-click the tray icon and turn on **Start with Windows** to run it every time you sign in.

> The exe is not code-signed, so Windows SmartScreen may warn you the first time you run it.
> Click **More info → Run anyway**.

## Usage

1. Run the game in **windowed mode** (a fullscreen window cannot be moved).
2. Double-click the tray icon to open the **window list**.
3. Select the game; its name and process are filled in. Check the size and alignment (left/center/right) and click **Add to profiles.ini**.
4. The window is updated within a few seconds. From then on, just start the game and it is applied automatically.

### Tray menu

| Item | Description |
|---|---|
| Window list / add profile | Shows open windows with their process names and adds a game as a profile. (Double-click the icon) |
| Edit profiles.ini | Opens the profile file in your default editor (usually Notepad). Changes apply as soon as you save. |
| Open log | Shows what was applied and any errors. |
| Alignment | Moves the running game to the left, center or right immediately and saves it to its profile (a fixed `x` in that profile is removed). |
| Language | Automatic (system language), English or 한국어. |
| Start with Windows | Runs the program when you sign in. |
| Pause auto-apply | Temporarily stops applying profiles. |
| Exit | Closes the program. |

Running the exe again while it is already running opens the window list.

## Resolutions

The position is calculated from the monitor's actual resolution each time.
Left is the monitor's left edge, center is `(monitor width − window width) ÷ 2` and right is `monitor width − window width`;
vertically the window is centered.
When you add a profile from the window list, the suggested size is 16:9 at the monitor's full height.

| Monitor | Suggested size | Left | Center | Right |
|---|---|---|---|---|
| 21:9 UWFHD 2560×1080 | 1920×1080 | 0, 0 | 320, 0 | 640, 0 |
| 21:9 WQHD 3440×1440 | 2560×1440 | 0, 0 | 440, 0 | 880, 0 |
| 32:9 DFHD 3840×1080 | 1920×1080 | 0, 0 | 960, 0 | 1920, 0 |
| 32:9 DQHD 5120×1440 | 2560×1440 | 0, 0 | 1280, 0 | 2560, 0 |
| 32:9 DUHD 7680×2160 | 3840×2160 | 0, 0 | 1920, 0 | 3840, 0 |
| 16:9 4K 3840×2160 | 3840×2160 | 0, 0 | 0, 0 | 0, 0 |

On a 16:9 monitor the window fills the screen, so it becomes a borderless fullscreen window whatever the alignment.

- **Profile sizes are in pixels.** After switching to a monitor with a different resolution, add the profile again from the window list
  or change `width` and `height`. For example, a 2560×1440 profile on DUHD (7680×2160) shows up small in the middle of the screen.
- **Display scaling (DPI) does not matter**: positions are calculated in real pixels, so they stay correct at 150% scaling and similar.
- **With several monitors**, the window is placed on the primary monitor by default.
  If your ultrawide is not the primary monitor, add `monitor=window` to the profile to use the monitor the game opened on.
- Games that keep a fixed internal render resolution may look stretched when their window is resized.
  In that case, set the game's resolution to match the window size.

## Profiles (profiles.ini)

Profiles are stored in `%APPDATA%\BorderlessWindowUtility\profiles.ini`. Add them from the window list or edit the file directly.

```ini
[EZ2ON]
process=ez2on.exe
width=2560
height=1440
align=center
```

| Key | Description | Default |
|---|---|---|
| `[name]` | Profile name. | |
| `process` | Executable name of the game (see Task Manager → Details). | |
| `title` | Text contained in the window title. Used together with `process`, both must match. | The profile name when `process` is not set |
| `width`, `height` | Window size. | |
| `align` | Horizontal placement: `left`, `center` or `right`. | `center` |
| `x`, `y` | Fixed window position. `x` overrides `align`; leave it empty to place the window by `align`. An empty `y` centers vertically. | Empty |
| `monitor` | Monitor to place the window on: `primary` or `window` (the monitor the game opened on). | `primary` |
| `delay` | Seconds to wait after the window appears before applying. | `3` |
| `auto` | Set to `false` to skip this profile when applying automatically. | `true` |

Lines starting with `;` or `#` are comments.

## Command line

```
BorderlessWindowUtility.exe --install              start at sign-in (current user)
BorderlessWindowUtility.exe --install --elevated   start at sign-in with admin rights (run from an administrator terminal)
BorderlessWindowUtility.exe --uninstall            stop starting at sign-in
BorderlessWindowUtility.exe --window-list          open only the window list
```

## Troubleshooting

**The profile is not applied**
- If the game is in fullscreen mode, switch it to windowed mode.
- If the game runs as administrator, this program needs admin rights too.
  Run `BorderlessWindowUtility.exe --install --elevated` from an administrator terminal, then sign in again.
- Games protected by anti-cheat may hide their executable name. Register them by `title` (window title) instead of `process`.
- **Open log** in the tray menu shows what went wrong.

**Will anti-cheat flag it?**
The program only changes a window's borders, position and size, using the standard functions Windows uses to manage windows.
It does not read or write game memory, inject code into the game, or simulate input.
That said, it cannot be guaranteed for every game, so check the game's terms of service or official guidance if you are concerned.

**Does it affect performance?**
It checks the list of open windows once per second, which has no noticeable impact.

**How do I remove it?**
Turn off **Start with Windows** in the tray menu, exit, and delete the exe.
To remove the settings as well, delete the `%APPDATA%\BorderlessWindowUtility` folder.

## Building

Install the [.NET SDK](https://dotnet.microsoft.com/download) 8 or later and run `build.bat`. The result is `dist\BorderlessWindowUtility.exe`.
Exit the running program from its tray icon before rebuilding.

### CI / releases

GitHub Actions (`.github/workflows/build.yml`) builds the program automatically.

- Every push and pull request to `main` builds the exe. Download it from the run's **Artifacts** in the Actions tab.
- Pushing a version tag such as `v1.0.0` builds that version and publishes a GitHub Release with the exe attached.

  ```
  git tag v1.0.0
  git push origin v1.0.0
  ```

  Tags with a suffix, such as `v1.1.0-beta.1`, are published as pre-releases.

### Project layout

| File | Contents |
|---|---|
| `src/Program.cs` | Command line options, single instance |
| `src/TrayApp.cs` | Tray icon and menu |
| `src/Watcher.cs` | Checks windows every second and applies profiles |
| `src/Borderless.cs` | Reading/writing profiles, changing window style, size and position |
| `src/WindowListForm.cs` | Window list / add profile window |
| `src/I18n.cs` | English / Korean text |
| `src/Startup.cs` | Start with Windows (registry or Task Scheduler) |
| `src/IniFile.cs` | Reading .ini files |
| `src/Native.cs` | Win32 API declarations |
