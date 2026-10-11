# Desktop Pet — Personality, Animation, and Movement Map

Status: Proposed implementation and art specification; not yet implemented  
Date: October 1, 2026  
Related: [Features](FEATURES.md) · [Personality settings](PERSONALITY.md)

## Purpose and ownership

Connect personality settings to visible behavior and give developers and artists a shared list of actions and assets. Build the cat first; reuse the action meanings for later characters.

The flow is: personality and current situation → eligible action → movement plan → character animation.

- Personality decides how likely an action is, how long it lasts, and which expression variant fits.
- The behavior controller chooses actions, handles input priority, and respects cooldowns.
- The movement controller owns position, velocity, platform contact, and reachability.
- The animation controller displays the matching pose and synchronizes it with movement events.
- The character pack supplies clips, anchors, body dimensions, supported actions, and tuning defaults.

Artwork never moves the world position by itself. A paw drawn outside the body does not enlarge the collision body or jump reach. This prevents different sprite frames from making the pet slide or bounce unexpectedly.

All numeric values below are initial tuning suggestions. Existing feature constraints, including upward jumps strictly below five standing body heights, remain authoritative.

## Personality-to-behavior mapping

| Setting | Action selection and timing | Visible cat expression | Movement effect |
| --- | --- | --- | --- |
| Playfulness | Increases chase, stalk, and paw-swipe selection when eligible | Alert ears, tail twitch, crouch before a pounce | More play movement; does not directly increase speed |
| Sleepiness | Increases nap selection and duration | Slow blink, settle, breathing loop, waking stretch | More stationary time; walking speed stays independent |
| Curiosity | Increases roaming and inspection of reachable destinations | Look around, sniff, head tilt | Chooses more varied safe destinations |
| Affection | Lengthens positive petting reactions and increases voluntary approach | Closed happy eyes, head lean, optional roll | May approach a nearby pointer, stopping outside its click area |
| Boldness | Reduces hesitation before unfamiliar toys or rapid pointer movement | Low: watch cautiously; high: ears forward, ready stance | Shorter delay before approaching; identical safety checks |
| Sensitivity | Increases sleep-disturbance accumulation from close movement | Ear twitch, then one-eye peek as disturbance grows | No direct speed effect |
| Patience | Raises annoyance/wake threshold | Tolerant cats settle back down; impatient cats give an annoyed look sooner | May choose a short safe relocation after waking |
| Speed multiplier | Does not change action-selection weights | Walk/run cadence follows travel speed | Scales requested horizontal speed within character limits |

Variants are optional. The first cat can use shared clips for several personality values; differences in frequency, duration, and sequence should already convey personality. Do not make a separate sprite sheet for every preset.

## Action and animation contract

Clip names below are stable logical identifiers, not required filenames. “Core” means required for the first cat's finished animation set; a prototype may use placeholder poses. “Later” actions require their corresponding interaction feature to exist.

| Action ID | Trigger / eligibility | Cat clips, in order | Movement | Completion / next state | Scope |
| --- | --- | --- | --- | --- | --- |
| `idle` | Supported and awake; default fallback | `idle` loop; optional `blink` | Stationary relative to support | Timer or input → select action | Core |
| `observe` | Nearby cursor noticed after a delay, or interesting destination | `look` loop, facing target | No translation; brief glance rather than constant tracking | Target leaves or short timer → idle | Core |
| `approach_cursor` | Sustained nearby presence, safe same-surface destination, shared cursor cooldown clear | `walk` loop → `look` or `sit` | Slow, bounded approach to a sampled offset destination | Stop outside cursor gap → resume own activity and cooldown | Core |
| `walk` | Safe destination on current surface | `walk` loop | Move to destination, stop before edge | Destination reached → idle | Core |
| `sit` | Supported, quiet activity | `sit_down` → `sit` loop → `stand_up` | Stationary | Timer/input → standing action | Core |
| `groom` | Supported and awake | `groom` loop | Stationary | Finish a short cycle → idle | Core, cat-specific |
| `stretch` | Awake, commonly after sleep | `stretch` once | Stationary | Clip ends → idle | Core |
| `sleep` | Supported; autonomous choice or explicit command | `settle` → `sleep` loop; optional `sleep_twitch` / `sleep_peek` | Stationary relative to support | Nap ends, wake input, or disturbance threshold crossed → wake | Core |
| `wake` | Sleep interrupted or finished | `wake` → `stretch` | Stationary | Sequence ends → idle | Core |
| `annoyed_wake` | Sleeping disturbance crosses personality threshold | `wake` → optional `annoyed_look` / `tail_flick` | Stationary; optional safe short walk after reaction | Idle, relocate, or return to sleep; no automatic chase | Core action; variants optional |
| `pet_response` | Short click on supported cat; wake first if asleep | `pet_happy` once; optional `pet_reserved` / `pet_lean` | Stationary | Clip ends → prior safe rest state or idle | Core |
| `chase` | Sustained nearby playful movement, cursor cooldown clear, safe route | Wiggling `stalk` → `run` or `jump_rise` → `jump_fall` | Bounded running, same-platform pounces, or jumps to exposed platforms that bring the cat closer | Time/budget limit or unreachable target → observe and cooldown | Core |
| `paw_swipe` | Cursor within 2D paw range | Repeated `paw_swipe` for 1.25–1.9 seconds on the ground; brief swipe during a pursuit jump | Stationary on a platform; airborne swipes preserve the ballistic trajectory | Attack timer ends → observe and cooldown | Core |
| `jump` | Reachable validated platform | `jump_crouch` → `jump_rise` → `jump_fall` → `land` | Planned arc, then contact resolution | Actual landing → land recovery → idle | Core |
| `fall` | Support lost, failed destination, or airborne release | `fall` loop → `land` | Gravity; collide with crossed valid surfaces | Actual contact → land recovery → idle | Core |
| `carried` | Pointer drag threshold crossed | `carried` loop | Follow pointer at stored grab offset | Release/capture loss → support check or fall | Core |
| `play_toy` | Toy exists, awake, cooldown clear | Reuse `observe`, `run`, `paw_swipe`; optional `pounce` | Use toy-specific safe movement rules | Toy removed or play timer ends → idle | Later |
| `rest_in_shelter` | Reachable bed/box/dock exists | Approach clips → optional `knead` → `sleep` | Move to shelter anchor, then rest | Wake or removal → wake/support check | Later |

An optional pounce that leaves the surface uses the jump controller and its checks. It must not become a separate unchecked movement path. No toy, shelter, or platform means the corresponding action is ineligible.

## Motion parameters and synchronization

Let H be the fixed standing collision-body height at the selected pet size, excluding tail and animation pose changes. Let M be the saved speed multiplier, initially 0.5–1.5.

| Parameter | Proposed default | Rule |
| --- | --- | --- |
| Walk speed | 1.0H per second × M | Clamp to pack limits and available stopping distance |
| Chase speed | 2.5H per second × M | Use only on a valid surface; stop before edges |
| Cursor stopping distance | 0.65H for approaches; 0.35H for engaged chases | Shorter play distance makes pursuit visible; user can still move closer to pet it |
| Autonomous chase burst | 3–6 seconds; 6–12H of travel, scaled by Playfulness | Brief crouch then bouncing run; swipe on catch; stop sooner when unsafe |
| Movement cooldown | 1–3 seconds, shorter for playful pets | Close-range swipes have their own 0.6–1.8 second cooldown |
| Upward reach | Rise strictly less than 5H | Also require horizontal reach, headroom, and landing clearance |
| Horizontal jump gap | At most 4H, proposed | Further restricted by a feasible trajectory |

- Movement updates use elapsed time; changing animation frame rate must not change travel distance.
- Walk/run playback follows actual speed relative to each clip's authored reference speed. At zero speed, switch to idle rather than cycling feet in place. Clamp playback to a visually acceptable range per clip.
- Sleeping, breathing, grooming, and petting use their own authored timing; the movement-speed slider does not accelerate them.
- A crouch ends at its `takeoff` event. Revalidate the target at that event; cancel to idle if it is no longer reachable.
- Switch rising/falling poses at the trajectory apex. Trigger `land` only on actual contact, never merely because an airborne clip ended.
- Validate the entire trajectory before launch. Freeze its parameters until landing; settings changes apply to the next movement action.
- When a supporting window moves, update the support-relative position without playing walk/run. Use idle or an optional balance reaction.
- For taskbar-only roaming, choose destinations along the floor. Personality stays visible through reactions and timing even when there are no reachable window tops.

## Subtle cursor attraction

The cursor is a mild point of interest, not a destination the pet continually tries to reach. These defaults replace the earlier frequent-chase tuning. All distances use H as defined above and all timings are proposed playtest values.

### Distance and personality model

Use the shortest distance d from the cursor to the pet's stable body boundary, in screen coordinates, normalized by standing height H. Set d to zero when the cursor is inside that body. Do not measure from an animated tail or changing sprite bounds.

Define proximity attraction as `G = 1 / (1 + (d / (2H))^2)`. This is a softened inverse-square curve inspired by gravity, not a physical force applied to the pet. It stays finite at contact. G is 1.0 at contact, 0.8 at 1H, 0.5 at 2H, 0.2 at 4H, and 0.1 at 6H. Apply a smooth cutoff from 6H to 8H using `F = 1 - 3t^2 + 2t^3`, where `t = clamp((d/H - 6)/2, 0, 1)`, and use `G × F` below.

Normalize personality values to 0–1: P = playfulness, C = curiosity, A = affection, B = boldness, S = sleepiness. Proposed cursor-action scores:

| Action | Score before eligibility gates |
| --- | --- |
| Look | `G × F × (0.25 + 0.60C + 0.15P)` |
| Approach | `G × F × (0.10 + 0.45C + 0.35A + 0.10B) × (1 - 0.4S)` |
| Chase | `G × F × (0.05 + 0.70P + 0.25B) × (1 - 0.6S) × E` |
| Playful scratch / paw swipe | `G × F × (0.05 + 0.80P + 0.15B) × (1 - 0.4S) × E` |

E is a smoothed motion-engagement value from 0 to 1, increasing with sustained nearby cursor movement and decaying after movement stops. Ignore tiny input jitter and cap the contribution of fast movements. A single fast pass cannot immediately fill E. Start with roughly 3 seconds to build engagement and 2 seconds to decay, then playtest.

Scores adjust selection likelihood, not movement force. Evaluate cursor reactions at natural action boundaries and at most about six times per second during idle, sitting, looking, or walking. Sustained movement accumulates chase interest, instead of requiring repeated unlikely random rolls. The same personality and motion pattern yields higher scores at shorter distances, but eligibility still matters: a swipe needs paw range; approaching stops inside the comfort gap; sleep uses the disturbance model below. Speed does not enter these scores. Habituation, cooldowns, and safety checks can suppress an otherwise high score.

1. Use the continuous distance scores above, with a 0.5–1.5 second notice delay. Brief passes do not trigger pursuit. Smooth scores over time to avoid rapid toggling; attraction fades to zero by 8H.
2. Usually respond with a 0.5–2 second glance. Smooth the gaze target, allow a small dead zone, and rate-limit changes of facing direction so small pointer movements do not cause twitching. Leave at least 5–10 seconds between autonomous glances.
3. After the cursor remains nearby for 2–4 seconds, occasionally consider an approach at a natural action boundary. Do not interrupt grooming, petting, explicit stay, or another committed action just to follow it. During sleep, use accumulated disturbance instead of awake attraction.
4. Approach at about 60% of normal walking speed, for at most 2 seconds or 2H of travel, whichever comes first. Sample a destination offset from the pointer on the current surface; do not continuously steer after every pointer movement. If a safe destination outside the gap cannot be found, observe instead.
5. Stop at least 0.65H from the body boundary on a passive approach, or 0.35H during an engaged chase. Recheck the gap before every movement step. Stop if the cursor enters the gap; do not automatically retreat from intentional petting. During a chase, refresh the target at most every 80ms and follow movement away on the same surface. Turn with a short crouch when the cursor changes sides; stop when it leaves the nearby range.
6. Repeated nearby movement should visibly invite play: Balanced Cat starts chasing within about 1–3 seconds in the close-range replay test. Roll once per new chase: a 10% chance shows a 0.9–1.2-second stalk/crouch with a visible side-to-side wiggle; the remaining 90% launch immediately. Turns retain their short crouch without wiggling or rerolling. This is a probability, not a fixed every-tenth-chase schedule. Ground pursuit uses 3–6 seconds or 6–12H according to Playfulness. Pursuit may transition to a planned jump: choose an exposed landing surface, keep the apex below 5H above takeoff, limit horizontal displacement to 4H, reserve full-canvas headroom, and require enough time and movement budget to finish. Commit to the trajectory after takeoff. A moved or removed target cancels horizontal travel and lets gravity recover the cat to another support or the floor.
7. Paw attacks require actual 2D proximity within 0.6H; horizontal alignment alone is not a catch. Repeat the paw clip for at most `1.25 + 0.65 × Playfulness` seconds (trait normalized to 0–1). Recheck proximity each frame and stop when the cursor leaves range, with a 0.25-second recovery. This also cancels airborne swipes while preserving the jump trajectory. Unreachable targets receive a 1.8-second watching pose and attention wind-down. Boldness sets the minimum interval between pursuit jumps to `2.5 + 2 × (1 - Boldness)` seconds.

Cursor-directed jump height uses `desired rise = feet Y - (cursor Y + 0.5H)` and `jump height = max(0.1H, desired rise)`. Higher landing platforms may require additional clearance; level landings do not add a hidden minimum rise. Sample the planned arc at least 120 times per second (at least 16 samples per hop), stopping at the first predicted landing, and require the body to enter the cursor's 0.6H paw range before accepting the plan. A rejected arc leaves ground pursuit available so the cat can approach a better takeoff position. The trajectory remains fixed after takeoff even if the cursor moves.
8. Limit autonomous cursor-directed movement to 18–60 seconds total in any rolling minute, scaled by Playfulness, in addition to cooldowns. Use the current personality cap and never exceed 60 seconds. Glances have their own cooldown and do not consume movement time.
9. When the cursor is far above a taskbar cat, the cat may glance up if within detection range, but does not pace beneath a distant pointer. If the cursor is stationary, interest fades rather than causing repeated approaches to the same spot; require meaningful new movement (about 0.5H) to invite another approach.
10. Disable autonomous attraction while a mouse button is held, including dragging something in another app. Resume after release with a brief settling delay. Direct dragging of the pet still follows the pointer immediately and is exempt from autonomous movement budgets.

Prototype tuning update: cursor travel is aggregated over 100ms so slow motion is recognized consistently across frame rates. Chase interest accumulates from distance and personality during sustained play; passive approaches do not consume the cooldown while that play is building. Idle/walk can yield to cursor interest, but sleeping, grooming, petting, and airborne actions cannot. More-visible-play tuning uses a 1.2-second engagement buildup, 0.18 accumulated chase-interest threshold, and decisions every 250ms. It replaces the earlier rare-chase tuning with the longer burst, shorter cooldown, and 12-second rolling movement budget above.

Expose an optional “Cursor attention” switch, enabled by default, separately from personality. Turning it off disables autonomous glances, approaches, chases, swipes, and cursor-induced sleep disturbance; direct petting and dragging remain available. No extra attraction-strength slider is needed initially.

### Sleep disturbance and annoyance

Before entering sleep autonomously, check the attention wind-down timer. Active nearby engagement renews a 15–30 second personality-dependent awake period, including while chase cooldowns or movement limits prevent running. A stationary cursor lets it expire. This gate only postpones voluntary naps; explicit Sleep overrides it and already-sleeping cats still use disturbance accumulation below.

Keep a temporary disturbance accumulator D while sleeping. It is separate from awake interest and is never persisted. Normalize Sensitivity as Q and Patience as T, both 0–1; S remains Sleepiness.

- Close-range influence: `N = max(0, 1 - d/(1.5H))^2`. Only movement very near the body contributes; movement farther away has no effect on sleep.
- Let V be cursor travel speed in H/second, after filtering tiny jitter. Let `m = clamp(V / 2, 0, 1)`. Mouse teleports/discontinuities reset the sample rather than counting as extreme travel. Smooth m over roughly 0.5 seconds, with substeps as necessary so frame rate does not change accumulation.
- Per elapsed second, update `D = max(0, D + dt × (N × m × (0.5 + Q) - 0.25))`. Clamp D to the wake threshold. D decays whenever movement stops or the cursor leaves; a stationary cursor cannot eventually wake the pet.
- Wake threshold: `W = 3 + 4T + 3S`. Higher Patience and Sleepiness require more accumulated disturbance; greater Sensitivity builds it faster.
- At about 35% of W, optionally twitch an ear; at 70%, briefly peek or flick the tail. Rate-limit these reactions to once per 5 seconds and retain the sleeping state. Missing variants can retain the sleeping pose.
- At W, enter `annoyed_wake`, reset D, and start a 30-second cursor-wake cooldown. After reacting, choose idle, a short safe relocation, or more sleep according to personality and available space. Chasing is not automatic; normal awake gates still apply.
- During the cooldown, D stays zero even if the pet returns to sleep. Pause, Cursor attention off, and mouse-button-held suppression also clear D and disable accumulation. Direct petting, pickup, and explicit wake remain effective.

For sustained close movement directly over the body with m = 1, Balanced Cat (Q=.5, T=.65, S=.7) wakes after about 10 seconds; Sleepy Lap Cat (Q=.25, T=.85, S=.95) after about 19 seconds; Playful Kitten (Q=.75, T=.4, S=.55) after about 6 seconds. These are illustrative continuous-stimulus timings; intermittent movement takes longer or never reaches the threshold. Proximity alone is insufficient.

## Selection model

Normalize personality sliders to 0–1. After excluding invalid actions, give each autonomous candidate a nonnegative weight and sample among the remaining candidates. Suggested starting weights are:

| Candidate | Illustrative weight |
| --- | --- |
| Idle / sit | 1.0 each |
| Groom | 0.7, cat only |
| Walk / explore | 0.3 + 1.7 × curiosity |
| Sleep | 0.2 + 2.8 × sleepiness |
| Cursor approach | 0.25 × Approach score from the distance/personality model |
| Cursor chase | 0.12 × Chase score; only after the sustained-play gate |
| Cursor swipe | 0.20 × Swipe score; only within paw range and cooldown clear |
| Cursor observe | 0.80 × Look score; subject to glance cooldown |
| Observe surroundings | 0.4 + 0.8 × curiosity; independent of cursor |

These are relative weights, not percentages or sleep-time guarantees. Duration and energy also affect time spent in each state. Tune selection weights and nap durations together toward the cat sleep targets in PERSONALITY.md. Keep idle as a valid fallback.

Affection can interpolate pet-response duration from roughly 0.6 to 1.8 seconds. Boldness can interpolate extra approach hesitation from roughly 1.5 to 0.2 seconds, after the cursor-presence gate has passed. These voluntary delays never apply to dragging, menu commands, or basic input acknowledgment.

## Interruptions and state transitions

- Direct pickup immediately cancels the current action, including a jump or nap, and enters `carried` without waiting for an animation to finish.
- On release, clear carried velocity; if supported, resolve contact, otherwise enter `fall`. Throwing based on drag speed is outside the current scope.
- Support loss interrupts all grounded actions, including sleep and petting, with `fall`. A cat that loses support wakes into a falling pose.
- Sleep/wake requests during a jump are deferred until safe landing. Return-to-floor can explicitly reset position and velocity immediately.
- Pause freezes autonomous action and simulation advancement; it is a controller flag, not an animation clip. Menus and dragging remain usable. Release while paused holds the pose until resume, when support is revalidated and any necessary fall begins.
- Petting during carried/airborne states must not replace the required movement pose. A supported sleeping cat uses a short wake transition before its petting response.
- At an ordinary action boundary, honor pending user commands before selecting another autonomous action. A stay command, when implemented, blocks voluntary travel until released.
- Optional facial/tail overlays may accompany compatible body clips. Never run two competing full-body clips or let a reaction override physics.

## Sprite authoring specification

Prototype artwork is now defined in [the ASCII sprite library](art/ASCII_SPRITES.md). Its extraction contract governs the ASCII test assets; the vector/raster guidance below remains for later polished artwork.

Use consistent logical canvas dimensions within a character pack. Exact pixel dimensions and frame counts should be chosen after testing the cat at intended desktop sizes.

Each clip needs:

| Metadata | Purpose |
| --- | --- |
| `id` | Logical clip identifier referenced by actions |
| `frames` and frame duration | Ordered sprite rectangles/files with positive display durations |
| `loop` | Whether to repeat or emit a completion event |
| `footAnchor` | Per-frame point aligned to the pet's support position, preventing foot drift |
| `facing` / `mirrorAllowed` | Whether right/left clips exist or horizontal mirroring is safe |
| `events` | Optional named events such as takeoff; each fires once per intended cycle |
| `referenceSpeed` | Authored speed for locomotion playback synchronization |
| `furMask` or tint layer | Recolor fur while preserving face, eyes, and outline |

Define collision body and standing height at pack level, independent of sprite transparency. Define interaction masks or simplified hit areas per pose so the user can grab the visible body without invisible padding consuming clicks. Use a common logical origin across clips and retain the pointer grab offset during carry transitions to avoid sudden jumps.

Start with right-facing sprites and mirror where appropriate. Asymmetric markings, accessories, or directional text may require separately authored left-facing art. Essential poses must remain readable at the smallest supported size.

## Example character action binding

This illustrative JSON fragment describes animation/movement bindings, not the user's saved personality profile or a complete pack schema.

```json
{
  "schemaVersion": 1,
  "characterId": "cat.default",
  "actions": {
    "walk": {
      "loopClip": "walk",
      "movementMode": "surface",
      "baseSpeedHeightsPerSecond": 1.0
    },
    "sleep": {
      "entryClip": "settle",
      "loopClip": "sleep",
      "exitAction": "wake",
      "movementMode": "supported"
    },
    "carried": {
      "loopClip": "carried",
      "movementMode": "pointer"
    }
  }
}
```

The user profile supplies personality and speed; the pack supplies supported bindings and limits. The engine supplies safety rules. Validate missing clips, frame rectangles, duration values, and action references when loading a pack. Reject a pack missing essential idle, carry, fall, or contact presentation, preserving the previously working character. Optional missing actions are excluded from selection; optional variants fall back to their base clip.

## Example personality sequences

| Profile | Example sequence when not being directly controlled |
| --- | --- |
| Sleepy Lap Cat | Sleep → wake/stretch → slow walk → sit → sleep; a click produces a warm petting response |
| Playful Kitten | Brief glance → independent walk → sustained nearby play invites a rare short chase → cooldown/groom → nap |
| Curious Explorer | Look around → walk → validated platform jump → land → inspect → rest |
| Gentle Observer | Sit → watch pointer → hesitate → short approach → idle; pickup still responds immediately |

These illustrate tendencies rather than scripts that repeat exactly.

## Future character mapping

Keep shared action IDs while replacing their visual expression. A robot's `sleep` can play shutdown/standby/startup clips; a slime's `walk` can use a sliding loop; a dragon's `carried` can show tucked wings. Cat-only grooming is an optional capability.

Changing art alone does not grant new physics. Flying, wall climbing, or teleportation require explicitly designed movement modes and their own constraints. Until implemented, all packs use the same surface/jump/fall system.

## Validation checklist

- Each core cat action resolves to a clip and a defined movement mode.
- Every animation transition keeps the support anchor stable, except intentional movement or direct positioning.
- Recoloring preserves readable facial features in every clip.
- Playful and sleepy profiles change action frequency/duration; speed changes only movement and locomotion cadence.
- Takeoff, apex, and landing poses match physics events at different frame rates.
- Pickup interrupts every state immediately; release and disappearing support recover correctly.
- Missing optional assets never cause selection of an impossible action.
- Taskbar-only play remains varied, and all profiles retain reliable input handling.
- Fast cursor passes and ordinary distant movement do not cause pursuit; a stationary cursor does not attract repeated approaches.
- Autonomous cursor movement stays within the personality-dependent rolling-minute cap and shared cooldown across all presets.
- Moving the cursor toward an approaching cat stops its movement before crowding the pointer; intentional petting does not trigger evasive behavior.
- Ordinary or distant cursor movement does not wake the cat. Sustained very close motion can wake it only through the disturbance threshold; cooldown prevents repeated wake loops.
- Holding personality, habituation, and motion constant, attraction scores decrease monotonically with distance and remain finite at contact. Selection frequencies, not individual random outcomes, reflect these scores.
- Increasing Sensitivity shortens disturbance wake time; increasing Patience or Sleepiness lengthens it under the same stimulus. Stationary cursor input only decays D.
- Frame-rate variation does not materially change disturbance timing or action-selection rates. Cursor attention off preserves direct petting/dragging while disabling accumulated disturbance.
- Test both facing directions and all supported sizes for sliding feet, clipped tails, and inaccurate hit areas.
