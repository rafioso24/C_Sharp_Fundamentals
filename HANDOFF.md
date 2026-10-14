# Handoff: Flappy Diver

This file picks up a Claude Code session that built a phone game from scratch.
Read it first, then open `FlappyBird/index.html`.

## What it is

**Flappy Diver** is an underwater take on Flappy Bird. A scuba diver swims past
reefs, wrecks, and sea creatures, using one-tap controls. It runs in a phone browser.

- **Code:** `FlappyBird/index.html` (one file: HTML, CSS, and JS together, no build step)
- **Branch:** `ccr-a2485d86-1hjgem`
- **Live version:** https://claude.ai/artifact/ABTB8LrXi5A51iE87svYB4 (private to the owner)
- **To play locally:** open `FlappyBird/index.html` in a browser.
- **Note:** The file has no `<!doctype>`, `<html>`, `<head>`, or `<body>` tags on
  purpose. The artifact host adds those when it publishes. Browsers still open it fine.

## How it got here (the user's requests, in order)

1. A basic Flappy Bird to play on a phone (bird and pipes).
2. Underwater theme: the bird became a diver, and sea creatures and hazards were added.
3. A **Sink / Float** buoyancy switch on the start screen. Float mode flips gravity
   and the tap direction. Sink mode loses at the sea floor, Float mode at the surface.
4. Score in **meters** instead of pipes. Random spacing: normal, **wide** (2 to 3
   hazards), and **calm** (empty, with a "CALM WATERS" banner).
5. **AI audit mode:** an AI plays using only taps. It logs why it crashed so the
   game can be made winnable.
6. Bug fix: obstacles stopped appearing for good after the screen briefly had zero
   size (on phones, switching apps can cause this).
7. Replaced the straight pillars with **17 shaped obstacle types**, each with
   irregular sizes and matching hit areas.

The user wants answers at a 7th-grade reading level, kept short.

## How the code is organized (inside the one `<script>`)

| Part | What it does |
|---|---|
| Constants | `GRAVITY`, `KICK`, `SPEED`, `FLOOR`, `DIVER_R`, `STEP` (fixed 1/60 s), `PX_PER_M` |
| Buoyancy | `mode` is `"negative"` (sink) or `"positive"` (float); `dir()` returns 1 or -1 |
| Obstacle library | `TOPS` (8 hang from the surface) and `BOTTOMS` (9 rise from the floor). Each has `name`, `minH`, and `build(h)`, which returns `{ name, w, prims, draw }` |
| Hit shapes | `prims` are `R` (rectangle), `C` (circle), and `E` (ellipse) in piece-local x and world y. They are kept **inside** the art so near misses look fair |
| Gates | `addGate(x)` pairs one top piece with one bottom piece around a clear opening (165 to 195 tall). The opening moves at most 170 between gates. Stored in the `cols` array |
| Gaps | `pickGap()` chooses normal (180), wide (390 to 550), or calm (580 to 730, never twice in a row) |
| Hazards | `addHazard()` places a jellyfish, shark, pufferfish, sea mine, or toxic barrel in the open water. `hazardAt(h, k)` predicts where it will be k steps ahead |
| Collision | `hitTest(y, k, margin)`: **the game and the AI share this one function**, so the AI's predictions match the game exactly |
| Game loop | Fixed-step loop: `loop()`, then `aiThink()` (in audit mode), then `update(STEP)`, then `draw()` |
| AI | `plan()` searches tap / no-tap choices every 3 steps, up to 1.8 s ahead (depth-first, skips repeated states). It tries a 4px safety cushion first, then none. `wantsTap()` decides which choice to try first |
| Crash log | `recordCrash()` saves what was hit and a verdict. "No safe path" means the layout is likely unwinnable. "Search ran out of time" means it may still be winnable. "AI error" means the AI's model of the game is off. It also saves the scene where trouble started |
| Audit HUD | Buttons: Speed 1×/4×, Hitboxes on/off, Crash log (with Copy), Stop |
| Test hook | `window.flappyAudit` has `start()`, `crashes`, `stats()`, and `setSpeed(n)` |

## Testing (what worked in the cloud session)

- Headless Chromium through Playwright. Use the **headless shell** binary:
  `/opt/pw-browsers/chromium_headless_shell-1194/chrome-linux/headless_shell`
  (that was the cloud container's path; on a desktop, use the normal Playwright install).
- Click `#audit-start`, then call `window.flappyAudit.setSpeed(20)`, wait about 60 s,
  and read `window.flappyAudit.stats()` and `.crashes`.
- **Last result:** zero crashes in about 20 minutes of game time in both Sink and Float mode.
- To test the crash log, run a copy with a tiny opening so crashes are sure to happen.

## Known limits and ideas for next time

- No sound yet.
- Best score is saved in `localStorage` under `flappy-diver-best-m`. The buoyancy
  choice is saved under `flappy-diver-buoyancy`.
- The free-floating top pieces (whale, fish school) leave open water above them.
  That is on purpose: it gives the player another way through.
- The AI's planner only looks 1.8 s ahead. If big new obstacles get added, check
  the audit for "Search ran out of time" verdicts.

## Side projects the user asked about

- **Demo reel outline** (about 45 s): hook, the first prompt, plain v1, glow-up
  montage (diver, buoyancy, calm waters, meters), the AI playing with hitboxes,
  the "ocean ran out of stuff" bug, the 17-obstacle finale, the user losing to
  their own game.
- **Face or avatar overlay:** CapCut overlay with Remove background (phone), or a
  PNGTuber setup with Veadotube mini and OBS, or VTube Studio for a face-tracked avatar.
