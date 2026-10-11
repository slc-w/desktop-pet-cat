# Desktop Cat — Feature Specification

Status: Initial draft for development  
Date: September 30, 2026  
Updated: October 11, 2026<br>
Target: Windows desktop application

## Product goal

A playful cat lives on the desktop, roams across window tops, reacts to the mouse, and spends plenty of time sleeping. It should feel like a small companion while allowing the user to keep working.

The requested core features are customizable fur color, cursor interaction, autonomous cat behavior, reachable-platform jumping, and direct dragging. Details and numeric defaults below are proposed design choices that can be tuned during development.

## First-version scope

| Feature | Expected behavior |
| --- | --- |
| Desktop overlay | Multiple independently controlled pets appear over normal desktop applications on transparent backgrounds, managed by one application and one tray icon. |
| Multiple pets | Add another pet from the cat or tray menu. Each pet has separate personality, size, sprites, pause state, and movement. Remove one without closing the others; Exit all preserves the pet list for the next launch. Pet windows and their settings editors are excluded from platform discovery. |
| Starting position | The cat starts at the bottom of the primary display's usable desktop, above a visible bottom taskbar. |
| Platforms | The desktop floor, the upper edge of a visible bottom taskbar, and exposed top edges of ordinary application windows support the cat. |
| Jumping | The cat can jump upward to a platform strictly less than five cat heights above its current support surface, subject to horizontal reach and landing clearance. |
| Dragging | Press and hold the left mouse button on the cat to pick it up, move it, and release it elsewhere. |
| Cursor interaction | The cat is subtly attracted to the cursor: brief glances and occasional slow approaches, with rare playful reactions after sustained nearby movement. |
| Autonomous behavior | Idle, walk, sit, groom, stretch, jump, land, and sleep animations run with varied timing. |
| Appearance | A color picker and presets change fur color immediately and persist across restarts. |
| Controls | A tray menu provides settings, pause/resume, return to floor, and exit. |

## Platform rules

Use operating-system window geometry rather than recognizing pixels or application contents. This is easier and more predictable than finding lines in webpages. Ordinary application windows can participate even when their internal controls are custom drawn.

- Only exposed portions of visible, non-minimized application window top edges are eligible. Exclude the cat's own overlay, tooltips, menus, and transient popups.
- Subtract portions covered by windows in front. A landing segment must fit the cat's collision width with a small safety margin.
- A platform is eligible only when there is enough space above it to display the cat. A maximized window flush with the screen top usually provides no usable top platform.
- Internal title-bar separators, buttons, scrollbars, browser tabs, webpage lines, and other controls are outside the first-version scope.
- The usable desktop bottom is always the fallback floor. A visible bottom taskbar acts as the floor boundary; the cat must not obscure its buttons while standing there.
- When all application windows are maximized, the cat remains visible above them and walks along the top of the visible bottom taskbar. This is a normal supported mode: it can still sleep, groom, react to the cursor, be petted, and be dragged without any reachable window-top platforms.
- For an auto-hidden taskbar, use the display bottom as the floor while it is hidden and adjust when it appears. Side or top taskbars reduce usable space but are not added as platforms in the first version.
- Refresh platforms when windows move, resize, minimize, close, or change stacking order. If a supporting window moves, carry the cat with it while its support remains valid. If support disappears, let the cat fall to the next valid surface.
- Initial release uses the primary monitor. Multi-monitor roaming is a later enhancement; display changes must still recover the cat to a visible position.

## Jumping and falling

Define cat height H using a fixed standing collision body at the selected scale, excluding animation variations such as a raised tail. Measure upward height from the current support surface to the destination surface.

- An upward target is reachable only when 0 < vertical rise < 5H. Exactly 5H is not allowed.
- Proposed maximum horizontal gap: 4H, further limited by the jump trajectory. A target must satisfy both reach limits.
- Use a visible crouch, airborne arc, and landing animation. Check landing clearance and display boundaries before starting.
- Platforms act as one-way surfaces: the cat can travel upward past an edge and land on it while descending.
- Recheck the destination during the jump. If it moves or disappears, continue into a controlled fall and land on the first valid surface below.
- Dragging is unrestricted by jump height. Dropping from above 5H is allowed, with no injury or punishment.
- While falling, detect crossed surfaces so the cat cannot skip a platform between animation frames. The floor prevents it from leaving the desktop.

## Direct interaction

| Input | Cat response |
| --- | --- |
| Cursor moves nearby | Eyes/head turn toward it; an awake cat may approach or bat at it. |
| Short left click | A brief petting response, such as a blink, head tilt, or happy expression. |
| Left press and drag | Suspend autonomous movement and show a dangling/carry animation while following the pointer. |
| Release after dragging | Land if a valid support is immediately underneath; otherwise fall naturally. |
| Right click | Open a small menu with pet, sleep/wake, color/settings, and return-to-floor actions. |

Use the system drag threshold to distinguish a click from a drag. Keep the original grab point under the cursor and capture the pointer so dragging continues outside the cat's original bounds. If capture is lost, end the drag safely and resolve a landing or fall.

Only the cat's visible interaction area and open menus consume mouse input; transparent overlay space passes input to the underlying application. Autonomous movement must not steal keyboard focus, move the user's cursor, or send clicks to other applications.

Cursor attraction is subtle by default and grows with proximity using a softened inverse-square distance curve. Personality determines whether interest becomes looking, approaching, playing, chasing, or a playful scratch/swipe. Most responses are brief glances; occasional approaches retain a comfortable gap. The cat does not pursue every pointer movement or jump platforms merely to follow it. Ordinary cursor movement leaves sleep undisturbed, but sustained close pestering can wake it after a personality-dependent annoyance threshold. Picking it up always takes priority. Exact formulas, timing, and limits are defined in [Behavior mapping](BEHAVIOR_ANIMATION_MAP.md#subtle-cursor-attraction).

## Personality and everyday behavior

- Sleep is the dominant behavior: proposed target is about 60% of unattended time over a long session, with randomized naps and awake periods.
- An awake cat alternates between short walks, watching the pointer, sitting, grooming, stretching, and occasional jumps.
- Sleep starts with settling down and ends with a stretch. Sensitivity, Patience, and Sleepiness determine how much repeated close cursor movement is needed to disturb it. Distant motion and a stationary cursor do not accumulate disturbance. Petting or picking it up can wake it directly.
- Petting produces immediate feedback. Repeated petting can trigger an occasional extra reaction, such as rolling over, without demanding ongoing care.
- Avoid rapid repetitive state changes. Randomize action duration and use cooldowns for special reactions.
- There are no hunger meters, chores, or penalties for ignoring the cat.
- Sound is optional and off by default. Visual reactions must work without audio.

Additional fun features for a later release: a temporary toy ball, a user-controlled chase toy, cardboard-box naps, more coat patterns, and occasional rare animations.

## Appearance and settings

- First version: base fur color, several presets, and a reset-to-default action. Keep facial details readable across light and dark fur colors.
- Proposed size options: small, medium, and large. Collision size and jump limits scale together.
- Save appearance, size, sound preference, and activity level locally.
- Personality presets and adjustable temperament/movement settings are defined in [PERSONALITY.md](PERSONALITY.md). These replace the initial relaxed/balanced/playful activity options; cat presets retain substantial sleeping time.
- Start with Windows is optional and off by default.
- Pause freezes autonomous activity while keeping menu controls available. Resume restores normal behavior.
- Return to floor recovers the cat to a visible starting position.

## Desktop behavior and quality expectations

- Target Windows 11 initially; compatibility with other Windows versions requires separate validation.
- Stay visible over normal windows, including maximized windows, without taking keyboard focus. Proposed default: hide during full-screen applications and restore afterward. Maximizing an ordinary window must not trigger this hiding behavior.
- Handle DPI scaling consistently so visuals, mouse hit testing, window geometry, and jump distances agree.
- Keep sleeping and idle behavior inexpensive; avoid continuously scanning screen pixels.
- Work locally without an account, internet connection, screen capture, or reading application content.
- Closing settings leaves the cat running. Exit from the tray fully stops the application.

## Acceptance criteria

1. On launch, the cat appears fully visible at the primary desktop floor and can roam without taking keyboard focus.
2. Changing fur color updates the cat immediately and survives restarting the app.
3. Clicking transparent space reaches the underlying application; clicking the cat produces its own reaction.
4. Dragging picks the cat up from its current grab point, follows the cursor smoothly, and ends safely on release or capture loss.
5. Releasing the cat in empty space makes it fall and land on an eligible surface without leaving the display.
6. A clear, reachable target 4.9H above the current surface is eligible; targets at 5H and 5.1H are ineligible. Horizontal reach and clearance can independently reject a target.
7. Minimized, covered, off-screen, and transient window edges do not become landing targets.
8. Moving, resizing, minimizing, or closing the supporting window leaves the cat in a valid supported or falling state.
9. Auto-hiding or restoring the taskbar updates the floor without trapping the cat off-screen.
10. During an unattended session, the cat varies its actions and spends roughly the configured majority of its time sleeping.
11. Pause, resume, return to floor, and exit remain accessible through the tray menu.
12. Scaling and display changes keep the cat visible and its click/drag area aligned with its body.
13. With every application window maximized and the bottom taskbar visible, the cat can walk, sleep, and respond to petting and dragging along the taskbar's upper edge without covering taskbar buttons.

## Suggested development stages

1. Build a transparent overlay with a placeholder cat, input pass-through, dragging, floor physics, and tray controls.
2. Add window/taskbar platform discovery, reachable jumps, occlusion handling, and recovery from window changes.
3. Add finished animations, cursor play, petting, sleep behavior, fur customization, and saved settings.
4. Validate normal desktop use, DPI changes, full-screen behavior, taskbar changes, and resource use before adding optional toys or multiple monitors.

## Technical direction and references

See [Behavior and animation mapping](BEHAVIOR_ANIMATION_MAP.md) for the connection between personality, sprite assets, and movement rules.

C# with WPF and Win32 interop is a candidate implementation. Validate transparent rendering and mouse hit testing in the first prototype before committing to the rendering approach.

Windows provides APIs for enumerating top-level desktop windows and querying their rectangles. Geometry still needs filtering for visibility, stacking order, DPI, and invisible resize borders. Taskbar position and monitor work-area information provide a basis for defining the floor; they do not automatically solve platform physics.

- [EnumWindows — enumerate top-level windows](https://learn.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-enumwindows)
- [GetWindowRect — window bounds and DPI/border considerations](https://learn.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-getwindowrect)
- [ABM_GETTASKBARPOS — taskbar bounds and monitor work-area guidance](https://learn.microsoft.com/en-us/windows/win32/shell/abm-gettaskbarpos)
