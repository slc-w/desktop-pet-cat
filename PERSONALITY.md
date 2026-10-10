# Desktop Pet — Personality Settings

Status: Proposed design, not yet implemented  
Date: October 1, 2026  
Related: [Feature specification](FEATURES.md)
Animation and movement contract: [Behavior mapping](BEHAVIOR_ANIMATION_MAP.md)

## Purpose

Describe how a pet behaves independently of its artwork. Develop expressive cat personalities first, while keeping the same settings usable for dogs, creatures, robots, and other characters.

Use a readable JSON profile for saved settings and a simple settings panel with presets and sliders. Users should not need to edit a file. All values and timings below are starting points for playtesting.

## Separate personality, movement, and temporary state

Personality is a lasting preference: how playful, sleepy, curious, or affectionate the pet tends to be. Movement controls how quickly it acts. Temporary state reflects what is happening now, such as sleeping, being carried, or cooling down after a chase.

A lazy cat can still sprint after a toy. A playful cat can move slowly. Do not put lazy and fast at opposite ends of one slider.

Character capabilities describe which actions and animations exist. Personality chooses among supported actions; it does not require every character to behave like a cat.

## User-facing settings

Use values from 0 to 100 for personality sliders. The endpoints express relative preferences, not guaranteed frequencies or percentages.

| Setting | Low value | High value | Main effect |
| --- | --- | --- | --- |
| Playfulness | Usually observes | Often joins play | Chance of chasing, batting, and playing with available toys |
| Sleepiness | Shorter, fewer naps | Longer, more frequent naps | Rest selection and nap duration |
| Curiosity | Stays around familiar spots | Explores available platforms | Destination selection and response to new toys |
| Affection | Brief, reserved reactions | Longer, enthusiastic reactions | Petting response and occasional approach behavior |
| Boldness | Watches before approaching | Approaches readily | Hesitation before engaging with a fast cursor or unfamiliar toy |
| Sensitivity | Hard to disturb | Notices small disturbances | How quickly repeated close cursor movement disturbs sleep |
| Patience | Quickly becomes annoyed | Tolerates repeated disturbance | Disturbance threshold before waking with an annoyed reaction |
| Movement speed | Leisurely | Brisk | Walking/chasing speed within the character's supported range |

Movement speed uses a proposed multiplier from 0.5 to 1.5, with 1.0 as normal. Keep it under a separate Movement section in settings.

Start the UI with a preset selector, the original temperament and speed controls, and an advanced section for Sensitivity and Patience. Sleepiness means desire to rest; Sensitivity means how easily rest is disturbed; Patience means tolerance before an annoyed response.

## Cat presets

Presets are starting values that users can customize. Editing a preset creates a Custom profile rather than modifying the bundled preset.

| Preset | Playfulness | Sleepiness | Curiosity | Affection | Boldness | Speed |
| --- | ---: | ---: | ---: | ---: | ---: | ---: |
| Balanced Cat — default | 55 | 70 | 50 | 65 | 50 | 1.0 |
| Sleepy Lap Cat | 25 | 95 | 20 | 90 | 35 | 0.7 |
| Playful Kitten | 95 | 55 | 90 | 75 | 80 | 1.3 |
| Curious Explorer | 65 | 60 | 95 | 45 | 85 | 1.1 |
| Gentle Observer | 40 | 75 | 60 | 60 | 20 | 0.8 |

Proposed Sensitivity / Patience defaults: Balanced Cat 50 / 65; Sleepy Lap Cat 25 / 85; Playful Kitten 75 / 40; Curious Explorer 60 / 55; Gentle Observer 80 / 70. Profiles missing these fields use their character defaults.

Cat profiles retain frequent naps, including the kitten. Balanced Cat targets roughly 60% sleep during a long unattended session, as proposed in the feature document. A sleepiness value of 70 does not mean exactly 70% sleep. Direct interaction will change observed totals.

## How the settings affect behavior

At the end of an action, choose the next action from eligible options using personality-adjusted weights. Check current support, reachable platforms, nearby toys, cursor distance, temporary energy, and cooldowns first.

- Playfulness increases play selection after a nearby cursor has invited engagement; it does not cause continuous cursor pursuit. Subtle cursor attraction is the default for every preset, including Playful Kitten.
- Sleepiness increases the likelihood and duration of naps. It does not reduce movement speed.
- Curiosity increases roaming and platform exploration. With no reachable platforms, it can produce looking around or walking along the taskbar.
- Affection changes the warmth and duration of interaction feedback. Even a reserved cat always acknowledges a successful petting input.
- Boldness reduces hesitation. It never relaxes safe landing checks or jump limits.
- Curiosity and affection can modestly increase cursor observation and occasional approaches. These traits share the same cursor-interaction budget; their effects must not stack into constant following. Timing, distance, and cooldown limits in BEHAVIOR_ANIMATION_MAP.md override personality weights.
- Cursor interest increases continuously as distance decreases, using a softened inverse-square curve. Curiosity primarily drives looking, affection and curiosity drive approach, and playfulness and boldness drive chase/swipe choices. Sleepiness reduces active engagement. Movement speed affects execution rather than interest.
- Sleeping pets ignore ordinary movement. Repeated movement very close to the body builds temporary disturbance: Sensitivity increases buildup, while Patience and Sleepiness raise the wake threshold. Disturbance fades when the cursor stops or leaves. This can wake an annoyed cat after sustained pestering; it does not imply an immediate chase.
- Speed changes horizontal movement within safe bounds. Keep gravity and the less-than-five-heights jump rule separate, and recalculate trajectory feasibility when movement parameters change.
- Temporary energy can decrease during play and recover during rest. Keep it internal: no hunger meter, maintenance chore, or penalty for leaving the pet alone.
- Active cursor interest postpones autonomous naps through chase cooldowns and movement-budget pauses. After the last meaningful engagement, retain an awake wind-down of `15 + 15 × playfulness × (1 - 0.5 × sleepiness)` seconds, with traits normalized to 0–1. Playful cats linger longer, while sleepy cats still get at least 15 seconds. Stationary or distant cursor presence cannot renew it indefinitely. An explicit Sleep command overrides this rule.
- Use random variation within bounds, minimum action durations, and cooldowns to avoid repetitive or rapidly switching behavior.

Illustrative unattended cat nap durations could range from 30 seconds to several minutes. Tune durations and action weights together to achieve the intended sleep share; do not derive that share directly from one slider.

## Input and comfort rules

Personality affects voluntary actions and expressions, not the reliability of controls.

1. Picking up the pet immediately interrupts autonomous behavior.
2. Release resolves a safe landing or fall before another voluntary action begins.
3. Explicit sleep, wake, stay, pause, and return-to-floor commands take priority over personality preferences when those commands are implemented.
4. Ordinary cursor movement leaves sleep undisturbed. Sustained close movement can cross a personality-dependent disturbance threshold; use decay and a wake cooldown to prevent repeated wake loops.
5. Cursor chasing is time-limited and followed by a cooldown. The pet leaves space around the pointer for normal work.
6. All personalities remain draggable and responsive. A sleepy or shy pet must not feel broken.
7. Sound, reduced motion, and interaction enable/disable preferences override personality. Affection must never enable sound if the user has muted it.

## Example saved profile

This is a proposed file format, not a configuration the current project can load yet.

```json
{
  "schemaVersion": 1,
  "characterId": "cat.default",
  "personality": {
    "presetId": "custom",
    "playfulness": 75,
    "sleepiness": 80,
    "curiosity": 55,
    "affection": 90,
    "boldness": 40,
    "sensitivity": 50,
    "patience": 65
  },
  "movement": {
    "speedMultiplier": 0.85
  }
}
```

Store appearance and global app preferences separately from this personality profile. Save edits locally and apply them at the next appropriate action boundary. Do not interrupt an airborne jump to apply a speed change.

Validate types and ranges on load. Fill missing fields from character defaults, clamp out-of-range numeric values, and fall back to defaults for invalid values. Preserve an unreadable or newer unsupported profile rather than silently overwriting it; report the fallback in settings. Version the format so future fields can be migrated deliberately.

## Extending beyond cats

Each character pack supplies supported actions, animations, default personality values, and movement limits. The behavior system uses shared meanings and delegates their expression to the character.

| Shared concept | Cat | Robot | Slime |
| --- | --- | --- | --- |
| Affection | Lean into petting | Happy lights | Wiggle toward the touch |
| Playfulness | Bat and pounce | Chase or spin | Hop after a toy |
| Sleepiness | Curl up and nap | Enter standby | Settle into a puddle |
| Curiosity | Sniff and inspect | Scan | Lean toward an object |
| Being carried | Dangle paws | Fold limbs | Stretch |

Missing optional actions are removed from selection, with idle/rest as a fallback. Character-specific actions such as grooming or kneading belong to the cat pack. Creature defaults may have different rest patterns; the cat's frequent-sleep baseline is not universal.

## Acceptance criteria

- Presets visibly differ over repeated sessions rather than requiring every individual action to differ.
- A highly playful, highly sleepy cat can take long naps and have enthusiastic awake periods.
- Adjusting speed alone does not change sleep preference or action-selection weights.
- A shy or sleepy profile still acknowledges clicks and responds immediately to dragging.
- Unreachable targets and unsupported actions are never selected, regardless of personality.
- Settings survive a restart, and malformed profiles recover without losing the original file.
- Switching to another character uses supported reactions without requiring cat-only animations.
- Pause and other explicit controls remain effective for every personality.
