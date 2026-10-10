# Simplified ASCII Cat Sprite Library

Version: 1 — prototype art, October 1, 2026.

This is the source artwork for image extraction. The [Python renderer](../tools/render_ascii_sprites.py) produces [PNG frames and a manifest](png/manifest.json). It contains all 30 named clips from [the behavior specification](../BEHAVIOR_ANIMATION_MAP.md): 20 core, 8 optional expression clips, and 2 later additions. The recommended first set is the core clips plus sleep_twitch, sleep_peek, annoyed_look, and tail_flick.

## Extraction contract

- Each `###` heading below is a stable clip ID. Metadata applies until the next clip heading.
- Each `text` fenced block is exactly one frame, in playback order. No other fenced text blocks appear in this document.
- Preserve spaces, blank lines, backslashes, apostrophes, and backticks inside frames literally. Do not trim or interpret them as Markdown escapes.
- Every frame has 6 rows on a 16-column logical canvas. Right-pad short rows with spaces; do not center or crop individual frames. Blank first rows are intentional. Use a monospaced font and the same cell dimensions for every image.
- Coordinates use the upper-left corner of the canvas. The common support anchor is the grid point `(8, 6)`, at the bottom edge. Airborne clips retain this origin; physics moves the canvas. Frame art is not a collision mask: use a stable body collider separately.
- Render white ASCII text on a fully transparent background, with no silhouette backing. Spaces between characters remain transparent. Sprite sets live in `sprites/white-ascii/` and `sprites/black-ascii-sticker/`.
- The head is on the left for locomotion. Generate right-facing variants by horizontally flipping the rendered character layer. The sleep `z` symbols are embedded in these frames; retain the canonical sleep orientation so the letters are not mirrored. Later, separate those symbols into an overlay if both sleep directions are needed.
- `frame_ms` is the duration of each frame, in milliseconds. `loop: false` clips emit completion and hold their last pose until the controller chooses another clip. Physics-triggered transitions override clip duration.
- `takeoff` occurs after the last crouch frame. `jump_rise` changes to `jump_fall` at the physical apex; `land` plays only after contact. Single-frame clips are intentional held poses.
- These are minimal keyframes for functional testing, not a finished smooth animation set. Repeated poses across different clips are intentional. Faster movement and different personalities reuse this artwork.

## Core clips

### idle
scope: core | loop: true | frame_ms: 900, 900

```text


 /\_/\____
( o.o )   )~
 `---'----'
  | |  | |
```

```text


 /\_/\____  ~
( o.o )   )'
 `---'----'
  | |  | |
```

### look
scope: core | loop: true | frame_ms: 650, 650

```text


 /\_/\____
( o.o )   )~
 `---'----'
  | |  | |
```

```text


 /\_/\____
( O.O )   )~
 `---'----'
  | |  | |
```

### walk
scope: core | loop: true | frame_ms: 180, 180

```text


 /\_/\____
( o.o )   )~
 `---'----'
  / |  / |
```

```text


 /\_/\____
( o.o )   )~
 `---'----'
  | \  | \
```

### run
scope: core | loop: true | frame_ms: 110, 110

```text


 /\_/\____
( o.o )   )~~
 `---'----'
 / /    \ \
```

```text


 /\_/\____
( o.o )   )~~
 `---'----'
   >>  <<
```

### sit_down
scope: core | loop: false | frame_ms: 180, 220

```text


 /\_/\____
( o.o )   )~
 `---'----'
  | |  \_)
```

```text

 /\_/\
( o.o )
 /   \
( | | )~
 `---'
```

### sit
scope: core | loop: true | frame_ms: 1000

```text

 /\_/\
( o.o )
 /   \
( | | )~
 `---'
```

### stand_up
scope: core | loop: false | frame_ms: 180, 220

```text

 /\_/\
( o.o )
 /   \
( | | )~
 `---'
```

```text


 /\_/\____
( o.o )   )~
 `---'----'
  | |  | |
```

### groom
scope: core | loop: true | frame_ms: 350, 250

```text

 /\_/\
( -.- )
 /  o\
( |   )~
 `---'
```

```text

 /\_/\
( -.- )
 / vo\
( |   )~
 `---'
```

### stretch
scope: core | loop: false | frame_ms: 400, 350

```text


       ___~
 /\_/\/   )
( -.- )--'|
 `--'_____|
```

```text


 /\_/\____
( -.- )   )~
 `---'----'
  | |  | |
```

### settle
scope: core | loop: false | frame_ms: 350, 450

```text


 /\_/\____
( -.- )   )~
 `---'----'
  (_)  (_)
```

```text



 /\_/\____
( -.- )   )
 `---'----'
```

### sleep
scope: core | loop: true | frame_ms: 1200, 1200

```text


         z
 /\_/\____
( -.- )   )
 `---'----'
```

```text

          z

 /\_/\____
( -.- )   )
 `---'----'
```

### wake
scope: core | loop: false | frame_ms: 400, 300

```text



 /\_/\____
( o.- )   )
 `---'----'
```

```text


 /\_/\____
( o.o )   )~
 `---'----'
  | |  | |
```

### pet_happy
scope: core | loop: false | frame_ms: 350, 450

```text


 /\_/\____
( ^.^ )   )~
 `---'----'
  | |  | |
```

```text


 /\_/\____  ~
( ^.^ )   )'
 `---'----'
  | |  | |
```

### paw_swipe
scope: core | loop: false | frame_ms: 160, 120, 180

```text


 /\_/\____
( o.o )   )~
 `---'----'
  (o)  | |
```

```text


 /\_/\____
( o.o )   )~
o----'----'
    |  | |
```

```text


 /\_/\____
( o.o )   )~
 `---'----'
  | |  | |
```

### jump_crouch
scope: core | loop: false | frame_ms: 180
event: takeoff after frame 1

```text



 /\_/\____
( o.o )___)~
 `--'(_) (_)
```

### jump_rise
scope: core | loop: true | frame_ms: 200
transition: jump_fall at physical apex

```text

 /\_/\
( o.o )___
 `---'    )~
  / /----'
       \ \
```

### jump_fall
scope: core | loop: true | frame_ms: 200
transition: land on physical contact

```text


 /\_/\____
( o.o )   )~
 `---'----'
 /  \ /  \
```

### land
scope: core | loop: false | frame_ms: 120, 180

```text



 /\_/\____
( >.< )___)~
 `--'(_) (_)
```

```text


 /\_/\____
( o.o )   )~
 `---'----'
  | |  | |
```

### fall
scope: core | loop: true | frame_ms: 200
transition: land on physical contact

```text


 /\_/\____
( O.O )   )~
 `---'----'
 /  \ /  \
```

### carried
scope: core | loop: true | frame_ms: 450, 450

```text
 /\_/\
( o.o )
 `---'
 /   \~
 |   |
 o   o
```

```text
 /\_/\
( o.o )
 `---'
 /   \ ~
 |   |
 o   o
```

## Optional expressions

These use the same extraction format. Sleep disturbance expressions are recommended for the first test set; the others can be added as needed.

### blink
scope: optional | loop: false | frame_ms: 120, 160

```text


 /\_/\____
( -.- )   )~
 `---'----'
  | |  | |
```

```text


 /\_/\____
( o.o )   )~
 `---'----'
  | |  | |
```

### sleep_twitch
scope: optional-recommended | loop: false | frame_ms: 160, 240

```text


         z
 /\__\____
( -.- )   )
 `---'----'
```

```text


         z
 /\_/\____
( -.- )   )
 `---'----'
```

### sleep_peek
scope: optional-recommended | loop: false | frame_ms: 450, 300

```text



 /\_/\____
( o.- )   )
 `---'----'
```

```text


         z
 /\_/\____
( -.- )   )
 `---'----'
```

### annoyed_look
scope: optional-recommended | loop: false | frame_ms: 650

```text


 /\_/\____
( >.< )   )~
 `---'----'
  | |  | |
```

### tail_flick
scope: optional-recommended | loop: false | frame_ms: 160, 160, 200

```text

           /
 /\_/\____/
( >.< )   )
 `---'----'
  | |  | |
```

```text


 /\_/\____
( >.< )   )--
 `---'----'
  | |  | |
```

```text


 /\_/\____
( >.< )   )~
 `---'----'
  | |  | |
```

### pet_reserved
scope: optional | loop: false | frame_ms: 500

```text


 /\_/\____
( -.- )   )~
 `---'----'
  | |  | |
```

### pet_lean
scope: optional | loop: false | frame_ms: 350, 450

```text


 /\_/\____
( ^.^ )   )~
 `---'----'
  | |  | |
```

```text


 /\_/\____
( ^.^ )   )~
 `---'----'
   / / | |
```

### stalk
scope: optional | loop: true | frame_ms: 350, 350

```text



 /\_/\____
( o.o )___)~
 `--'/_)(_)
```

```text



 /\_/\____
( o.o )___)~
 `--'(_)/_)
```

## Later additions

### pounce
scope: later | loop: true | frame_ms: 150
transition: land on physical contact; use validated jump physics

```text


 /\_/\____
( O.O )   )~~
o----'----'
       \ \
```

### knead
scope: later | loop: true | frame_ms: 350, 350

```text


 /\_/\____
( ^.^ )   )~
 `---'----'
  o_   (_)
```

```text


 /\_/\____
( ^.^ )   )~
 `---'----'
  _o   (_)
```

## Behavior reuse and limitations

- Cursor approach uses walk and look. Cursor chase uses stalk (optional) and run. Different attention strengths change selection and timing, not artwork.
- Annoyed waking uses wake, annoyed_look, and optionally tail_flick. Sleep disturbance uses sleep_twitch and sleep_peek without leaving the sleeping state.
- Toy play reuses look, run, and paw_swipe. Shelter rest reuses walk, settle, sleep, and optionally knead. No separate toy or shelter artwork is included here.
- These are full-body frames. Do not layer a full-body tail_flick or blink over another full-body sprite. Separate facial/tail layers can be introduced later.
- Look currently changes eye openness only; arbitrary directional gaze is not implemented by these two frames. The runtime can change facing, and dedicated directional expressions can be added after testing.
- The sitting and carrying silhouettes deliberately use compact test shapes. More natural transition poses and anatomically accurate paws are future polish.
- Current PNG export uses Consolas at 32 pixels, a 20-by-38-pixel character grid, and 10-pixel canvas padding. All 54 frames are 340 by 248 pixels with RGBA transparency. The renderer preserves the grid and exports frame timings and support anchors in `png/manifest.json`; `png/contact-sheet.png` is an opaque review sheet, not an animation asset.
