# Make the pet your own

ASCII is one possible art style. The runtime uses PNG images, not text, so you can draw pixel art, cartoons, robots, blobs, or another character. Artwork changes appearance; the existing controller still uses cat behaviors and movement rules.

## Add a selectable sprite set

Two bundled sets are available as templates:

| Menu choice | Image folder |
| --- | --- |
| Black ASCII (white sticker), the default | `art/sprites/black-ascii-sticker/` |
| White ASCII (transparent) | `art/sprites/white-ascii/` |

1. Copy a bundled folder to a new folder such as `art/sprites/my-robot/`. Keep the bundled sets available.
2. Add or edit the top-level `name` in the copied `manifest.json`, for example `"name": "My Robot"`. Keep valid JSON syntax. If there is no name, the folder name becomes the menu label.
3. Replace PNGs at the paths listed in the manifest. Start with `idle/idle_01.png` and `idle/idle_02.png` to check the look, then work through the other animations.
4. Preserve the existing animation structure: **30 clips and 54 frames**, canvas dimensions, frame counts, loop flags, and durations. A custom set must match the active set so switching can preserve its current pose and frame timing. Filenames can differ if the manifest points to the correct images inside the set folder.
5. Open **Sprite set** in the cat or tray menu and select **My Robot**. The dropdown rescans immediate subfolders each time it opens, so new folders appear without a code rebuild or restart. **Open sprite folder** takes you to the right location.

The selected folder name is saved for the next launch. If that set is missing or incompatible on startup, the bundled sticker set remains active. Invalid JSON manifests appear as disabled entries; missing images or incompatible animations show an error when selected and leave the current pet unchanged.

Every set needs its own `manifest.json`; images alone are not discovered. Use unique display names to make sets easy to tell apart. Finish copying a set before selecting it. To reload edited images in the current set, select another set and switch back, or restart the app.

An example folder layout:

```text
art/sprites/my-robot/
  manifest.json
  idle/
    idle_01.png
    idle_02.png
  walk/
    walk_01.png
    walk_02.png
  ...all remaining clips and frames from the template
```

## Draw frames that fit

- Export each frame as a **340 × 248 pixel PNG**, preferably RGBA with a transparent background. Keep the canvas size even when the drawing is small.
- Place the character using the bundled frames as alignment templates. Keep the feet near the existing baseline and match the existing occupied area; the controller's dimensions and landing offset are fixed in code.
- Keep the anchor stable across frames to avoid accidental jitter. Intentionally moving limbs or the body within the canvas is fine.
- Match the bundled facing direction. The app mirrors most animations horizontally to turn the pet; sleep, sleep twitch, and sleep peek keep their original orientation.
- Leave empty surroundings transparent. Opaque pixels determine the clickable area, so a solid rectangular background makes the whole rectangle clickable.
- Preview at desktop size: frames display at 29% of their exported dimensions, approximately **99 × 72 pixels**. Small details and thin lines may be difficult to see.

A white sticker outline is optional. You can use colorful sprites with no backing at all. Export SVGs or other source artwork to PNG before using them; the documented sprite workflow uses PNG frames.

## Animate the personality

Begin with idle, look, walk, run, sleep, and paw swipe. Then replace jump transitions, falling, landing, carrying, grooming, and the other expressions listed in the manifest. Keep the intended meaning of each clip: a sleep pose should read as sleeping, and a carried pose should look different from walking.

The [behavior and animation map](../BEHAVIOR_ANIMATION_MAP.md) explains which actions use which animations. The [ASCII sprite library](ASCII_SPRITES.md) can serve as a pose reference; you do not need to draw in ASCII or run the ASCII renderer to create your own PNGs. Running that renderer overwrites `art/sprites/white-ascii/`, so preserve any custom artwork first.

## Check your artwork

From PowerShell in the project root, with the pet closed:

```powershell
& '.\Start Cat.cmd' --self-test
```

Read `%LOCALAPPDATA%\DesktopCat\last-run.log`. A passing result checks the bundled sets and custom-set discovery/selection behavior using temporary test artwork. To validate your own set, select it from the dropdown: loading checks its frames and compatibility before applying it. Neither check judges artwork, alignment, or animation quality.

Launch with **Start Cat.cmd**, select your set, and try walking, chasing, petting, dragging, sleeping, and jumping. Check that feet meet platforms and the character does not wobble unexpectedly. Select a bundled set to return to the original artwork. The source packaging script includes all sprite folders with a manifest.

## Beyond replacement artwork

Adding selectable sets needs no code changes. Changing character size, collision geometry, frame counts, or animation timing still requires code changes with the current implementation. Sprite selection lives in `prototype/SpriteSets.cs`; canvas size, display scale, and foot offset live in `prototype/DesktopCat.cs`, while body dimensions and behavior live in `prototype/CatBehavior.cs`. Changing manifest metadata alone does not change those physical dimensions.
