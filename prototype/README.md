# Desktop Cat prototype

Second milestone: a primary-screen WPF desktop pet using the existing ASCII PNG stickers, now with native window-top platforms and personality-driven cursor behavior. Built with the Windows-installed .NET Framework C# compiler; no NuGet dependencies or SDK installation required.

## Build and run

From the repository root in PowerShell:

```powershell
& '.\Start Cat.cmd'
```

The launcher compiles on first use and after source changes, storing its local output in the ignored `prototype/bin` folder. Keep both sprite trees under `art/`. Use `prototype/release.ps1` to package source, launcher, and sprites as a ZIP containing no executable.

## Controls

- Click the cat to pet it.
- Hold and drag to pick it up; release to fall onto an exposed window top or the primary display's work-area floor, above the taskbar.
- Right-click a cat for its controls, or select its named controls entry in the shared tray menu. **Add another pet** creates an independently controlled pet. **Remove this pet** removes that pet; **Exit all pets** closes the app while preserving the roster for the next launch.
- Double-click the tray icon to recover all pets to the floor.
- Sleep and Wake preserve the current supporting platform; an airborne cat returns to the floor first.
- Sprite set in the cat or tray menu discovers named sets under `art/sprites/` each time its dropdown opens. Copy a compatible template into a new folder and customize its artwork and manifest name to add a selectable style. Open sprite folder reveals the location in Explorer. The current pose, movement, and animation timing continue; the choice is saved in `%LOCALAPPDATA%\DesktopCat\pets\<pet-id>\sprite-set.txt` for the next launch.
- Personality & size opens seven trait sliders, movement speed, presets, a cursor-attention toggle, and a Cat size slider (0.50x–2.00x, default 1.25x relative to the previous size). Size applies on Save and persists as PetSize; switching personality presets preserves it. Older profiles without PetSize use 1.25x. Save applies changes and stores `%LOCALAPPDATA%\DesktopCat\pets\<pet-id>\personality.json`; Cancel keeps the current values. Malformed/unsupported files load defaults and are backed up before replacement on an explicit Save.
- Personality is a normal non-blocking WPF window with a taskbar entry. The cat continues animating and moving while it is open; cursor play is temporarily suppressed so it does not chase the pointer used to adjust sliders. Save, Cancel, Escape, and the window's close button all leave the pet operational. Reopening focuses the existing editor. An explicitly paused cat stays paused.
- Jump to a reachable window attempts a safe upward jump. If no exposed, wide-enough platform is within range, the cat stays where it is.

The cat walks, sits, grooms, naps, and sometimes explores reachable window tops. Native window bounds are refreshed four times a second; windows in front cut away occupied landing space. Moving supports carry the cat, and closing/minimizing/covering a support causes a fall. The full sprite canvas is reserved for clearance, so platform selection is intentionally conservative. Upward rises must be strictly below five standing-body heights; horizontal travel must also fit the jump limits.

Cursor attraction uses a softened inverse-square proximity score. Curiosity favors looking, affection favors approach, and playfulness/boldness favor swiping and brief chasing. Playfulness controls chase duration, breaks, and the movement budget: more playful pets engage much more often. Repeated very close motion can disturb sleep; sensitivity speeds accumulation while patience and sleepiness raise the wake threshold. Sleep twitch/peek sprites show accumulating disturbance. Moving the cursor away lets disturbance decay.

To try chasing: wake the cat, then move the cursor gently back and forth about one cat-height ahead of it at roughly its body level for a few seconds, without holding a mouse button. Sustained motion builds chase interest. On each new chase there is a 10% chance of a 0.9–1.2-second crouch and wiggle; otherwise it launches immediately. It runs or pounces, follows ground-level direction changes, and repeats its paw animation for up to 1.25–1.9 seconds while the cursor stays close. Pawing cancels on cursor exit, including in flight, followed by a 0.25-second recovery. Same-platform hop height is max(0.1 body heights, feet Y minus cursor Y minus half a body height); elevated landings retain their required clearance. The planned arc must enter paw range before its first landing. An overhead cursor can trigger a jump back onto the current surface or onto a reachable window; each jump has a planned landing, stays below five body-heights of rise, and keeps its trajectory after takeoff. A nearby cursor can receive an airborne swipe. Horizontal alignment alone does not count as a catch; unreachable targets get a 1.8-second watching pose. Playfulness controls the limits: 3–6 second bouts, 6–12 body-heights of travel, and 1–3 second breaks. The default personality engages within about 1–3 seconds in close-range replay tests. Chasing updates the moving destination and stops 0.35 body heights short of the pointer; passive approaches stop 0.65 heights away. It will not chase a distant pointer high above its platform.

The transparent windows use native layered-window transparency and alpha-aware hit testing. They do not activate when clicked. One application manages multiple pets with one tray icon. Each pet has independent behavior, pause state, settings, size, and sprites. All pet overlays and their settings windows are excluded from native platform discovery, including occlusion calculations. New pets start at separate floor positions when space permits; moving pets can overlap and do not interact with each other yet.

The roster is saved in `%LOCALAPPDATA%\DesktopCat\pets.json`. Individual preferences and diagnostics live in `%LOCALAPPDATA%\DesktopCat\pets\<pet-id>\`. On migration, the first pet (`cat-1`) inherits legacy personality and sprite preferences; later pets start with defaults. Removing a pet removes it from the roster but retains its profile files. Exiting the app preserves the roster; if every pet has been removed, the next launch starts with Cat 1. Running the launcher again while the app is open does not add a pet.

Active cursor attention suppresses autonomous sleep even during chase cooldowns or when the movement budget is exhausted. After engagement stops, a 15–30 second personality-dependent wind-down remains (about 20 seconds for Balanced Cat). The cat can still sit, groom, or walk while awake. A stationary cursor does not refresh that timer; explicit Sleep overrides it immediately, and disabling Cursor attention clears it.

## Validation

Run `& '.\Start Cat.cmd' --self-test` and inspect `%LOCALAPPDATA%\DesktopCat\last-run.log`. The check loads both sprite sets, verifies matching clips, dimensions, loop flags, and frame timings, then loads all 54 active assets and runs 26 behavior checks covering occlusion, exact 5H rejection, reachable trajectories, swept landings, proximity, disturbance, and settings validation. Runtime failures also go to this log. `--smoke-test` briefly starts the actual WPF window, enumerates desktop platforms, records runtime state, and exits after four seconds; close the normal instance first.

`--integration-test` creates its own temporary native window, then verifies discovery, a real timed jump and landing, following the moved support, and recovery after closure. The fixture and pet close automatically. This test passed on the development desktop, alongside the self-test and smoke test. These checks do not simulate mouse dragging, confirm click-through into other apps, or visually judge animation quality; those remain manual checks.

`--cursor-test` replays synthetic pointer samples through the actual attention/movement controller without moving the OS mouse. Fifty-three checks cover visible pursuit at 30/60/120Hz, following a target moved away, stopping at the comfort gap, the movement budget, no chase for stationary/distant/disabled/sleeping conditions, staying awake during engagement, eventual wind-down, explicit Sleep overriding attention, sustained wiggles, repeated paw animation, 2D pounces at 30/60/120Hz, committed airborne trajectories, pursuit platform landings, unreachable targets, recovery after target removal, exact low-hop heights, full-arc reachability, and ground/air paw cancellation at 30/60/120Hz. It closes automatically and does not save changes to the user's profile.

Manual desktop checks: click an application through empty space around the cat; click its white sticker body; drag/drop over a restored window; use Jump with a reachable window; move, cover, minimize, and close the supporting window; change personality and restart; hold the pointer still versus moving it near a sleeping cat; pause/resume; use tray Exit all pets. Verify keyboard focus remains in the original app. DPI and taskbar configurations need on-device interaction testing.

`--settings-test` opens the actual WPF editor and checks continued movement, duplicate-open handling, Cancel, preset selection and Save, persistence to a dedicated test profile, preserving a pre-existing pause, and resumed movement after closure. It does not modify the user's profile. Half-size movement uses fractional position accumulators so subpixel walking/approach steps do not round to zero.

`--multi-pet-test` uses an isolated data folder to verify adding pets, separate positions and profiles, independent size/sprites and pause/movement, the shared tray and per-pet menus, native-window exclusion, removing one pet without stopping the other, restoring saved preferences, and preserving the roster on exit.

## Current boundaries

This milestone does not yet implement recoloring, saved position, full-screen hiding, toys, or multimonitor roaming. Personality and sprite-set choice are persisted. Primary-monitor work-area geometry is refreshed while running; auto-hidden taskbar reveal and mixed-DPI behavior need further work. Rectangular window bounds conservatively approximate rounded or nonrectangular windows. Native platform discovery does not read application content. Desktop art is displayed at 29% of its exported size multiplied by Cat size, default 1.25x (36.25% of the source canvas). Body dimensions, interaction distances, walking speed, and jump reach scale with it. Resizing preserves the feet position when possible; an oversized cat falls safely if it no longer fits its support. Active jumps are cancelled into a safe fall when the size changes.

The two-frame look clip changes eye openness and facing, rather than smoothly turning eyes toward every pointer direction. Sleep and wake use simplified transitions. Personality timing is initial prototype tuning and should be judged through use, not treated as a guaranteed sleep percentage.
