# Umbra 2.4.0 — Trident proof

A minimal injected Risk of Rain 2 menu with a deep-blue, Trident-inspired Unity IMGUI interface. It is still **Umbra**; it does not embed Dear ImGui or require the Trident application to render. Use in solo/private sessions with everyone informed.

## What's changed

- Default window is **950 × 750** (clamped to the screen), with Player, Aimbot, Visuals, Items, World, Spawn, Lobby and Misc pages.
- Host badge and a soft red non-host banner. Host-only controls gray out automatically; their keybinds also enforce host authority.
- Dedicated Aimbot page: constant or hold-key activation (right mouse by default), editable activation/master-toggle keys, actual camera steering in Smooth mode, and configurable cursor-to-target line. Camera aiming runs before the game's camera update through an owned hook.
- Toggle buttons have clickable key assignments. Escape cancels binding, Delete clears it, and assigning an existing key moves it from the old binding. Insert/End remain reserved.
- A transparent raycast shield blocks underlying game UI inside the window. Local gameplay input is suppressed over Umbra, while editing a field, and during drags that began inside; clicking outside releases text focus and leaves outside clicks available. Holding Tab still uses the game's normal cursor behavior.
- Items: icon/name search, tier/equipment/inventory filters, pagination, give/drop/drop-from-inventory, random rolls, bulk give, restack, clear removable items, and nearby chest replacement. Bulk/destructive tools require a second click.
- Spawn: exact categorized addressable cards loaded asynchronously, including installed portal cards. World exposes individual blue/gold/green/celestial/void/tech and other catalog portals. Portals spawn immediately; DLC and destination/stage rules still apply.
- Lobby: explicitly choose a connected recipient, then give money, lunar coins, or the item selected in Items. Equipment gifts replace their active slot. Lunar gifts affect that recipient's persistent profile.
- God modes: invulnerable, intangible hurtboxes, regeneration, and auto revive. Regeneration does not stop lethal one-shots; auto revive cannot stop the run ending.
- Boxless ESP now centers compact stacked labels and uses a horizontal health bar under the block.
- Misc additions: right-side FPS/ping, run timer, coordinates, and a host position bookmark that expires on stage change.
- Native-pixel text and nine-sliced, antialiased corners replace stretched button textures.
- Slightly transparent window (82% default), adjustable opacity and corner radius under **Misc → Appearance**. Text remains opaque.
- Cards measure their actual controls and wrapped text before layout. Pages scroll to their full height and collapse to one column on narrow screens.
- Aimbot selection and the FOV guide share the same camera projection and circular boundary. The final hurtbox/weak point must pass FOV, range, team, health, and optional visibility checks before any priority is applied.
- ESP projects live world-space mesh bounds, including near-plane clipping, instead of estimating size from distance. Outline thickness remains in screen pixels.
- **Visuals → Category style** contains 25 categories with independent RGBA sliders, thickness, visibility, boxes, and labels. **Item overrides** adds individual item styles by display name or catalog ID. Equipment uses its category color.
- FOV and crosshair each have RGBA and thickness controls. Visual preferences save when the menu closes or **Save visuals** is selected.
- Each menu page is an independent class under `UI/Pages`. `MenuController` owns the window; `UmbraRuntime` owns the game lifecycle. Obsolete runtime sources and unused resources/dependencies have been removed.
- Trident loads are queued onto the game's main loop and wait for content readiness. Repeated loads are idempotent; failed initialization cleans up before bounded retries.
- Flight overrides native motor velocity after gravity and hovers while Umbra is open. Railgunner perfect reload caches state machines per body, supports rebinding, and respects disable-all.
- Spawn cards use native monster portraits or interactable inspect sprites where available, with a category fallback for assets without artwork.
- Settings use managed JSON for explicit key and palette-list serialization, atomic replacement, backup recovery, and debounced autosaving.

## Included pages

**Player:** give/set/zero money; give/set/remove lunar coins; XP, heal, respawn; god-mode selection; movement, Railgunner perfect reload, reversible stat overrides and infinite skills.

**Aimbot:** activation, camera mode/smoothing, target rules, FOV styling and target line.

**Visuals:** enemies, bosses, allies, interactables, teleporter, dropped items, labels/distances/health bars, corner boxes, render range, category/item styling, crosshair/FOV.

**Items:** searchable items/equipment, give/drop/transfer, roll random items and inventory tools.

**World:** teleporter charging, shrine stack, individual portals, stage actions and cleanup of Umbra-tracked spawns.

**Spawn:** paginated catalog of exact monster/boss/chest/shrine/drone/printer/portal/other cards, team selection and placement range.

**Lobby:** recipient gifts and revival, session diagnostics and run-seed copying; difficulty remains read-only.

**Misc:** cooldowns, telemetry, position bookmark, page bindings, disable-all, unload and window styling.

Aim modes are **Direct**, **Smooth**, and **Fire only**. Fire only redirects the aim vector while primary fire is held; it is not packet-level pseudo-silent aiming. FOV is a camera-centered **half-angle** in degrees. Smoothing affects Smooth mode only.

## Build in VS Code

1. Install Visual Studio 2022 or Build Tools with MSBuild and open this folder in VS Code.
2. Install Risk of Rain 2. For a custom Steam library, set `UMBRA_GAME_MANAGED_DIR` to its `Risk of Rain 2_Data\Managed` folder.
3. Save your changes, then press **Ctrl+Shift+B** for Release. Choose **Build Umbra (Debug)** from Terminal → Run Build Task when needed. The `bin` folder is visible in the Explorer so you can find the DLL easily.

Alternatively, from this directory:

```powershell
.\build.ps1 -Configuration Release
# Custom library:
.\build.ps1 -Configuration Release -GameManagedDir 'D:\SteamLibrary\steamapps\common\Risk of Rain 2\Risk of Rain 2_Data\Managed'
```

Output: `bin\Release\UmbraMenu.dll` (x64). References come from the installed game's managed assemblies; do not distribute those game assemblies. No BepInEx, R2API or Octokit installation is needed. The repository's Harmony dependency is embedded for camera/input hooks; no separate deployment is required.

## Load and use

Use your existing Mono injector with namespace `UmbraMenu`, class `Loader`, method `Load`. `Loader.Unload` releases the runtime. Restart the game when replacing an already loaded assembly with a new build; Mono may retain the previous assembly in memory.

Early injection is supported once Mono and the RoR2 managed assemblies are available, including before the main menu. `Loader.Status` reports waiting, ready, or initialization failure. Loading never waits synchronously on the injector thread.

- **Insert:** open/close Umbra. Drag its header to move it.
- **End:** disable gameplay modifiers.
- **Z:** open Player. **B:** open World. **I:** open Items. **C:** toggle flight (editable).
- Aimbot activation defaults to holding **right mouse**, after enabling its master toggle. Switch Activation to Constant if desired.
- During flight, jump ascends and **X** descends; opening Umbra pauses flight input and holds position.

Gameplay toggle shortcuts are suppressed while the menu or game cursor is active. Click a toggle's key button to rebind it. Server-authoritative actions require the host; stat and cooldown modifiers only apply on the host. Lunar coins affect the persistent game profile and are not undone by disable-all or unloading. Back up that profile before editing currency. Money, XP, spawns, stage actions, and other one-shot operations are also not reversible via disable-all.

Visual settings: `%APPDATA%\UmbraMenu\visuals.json`. Key assignments: `%APPDATA%\UmbraMenu\keys.json`. Gameplay mutations are never enabled by loading preferences. Window position/style, ESP switches and colors, FOV/target-line styling, and Misc telemetry choices persist. Changes autosave, with a final flush on close/focus loss/unload. Failed writes are shown in the footer and retried. Previous versions use `.bak`; unreadable primary files are preserved as `.invalid-*` before replacement. A malformed or empty key file cannot recover assignments it never stored, but a valid backup is tried first.

## Code and validation

See [Architecture](docs/Architecture.md) for module responsibilities and behavior notes. Methods in the maintained runtime have XML summaries and logical sections. [Legacy README](docs/Legacy-README.md) preserves historical instructions and acknowledgements; its feature list does not describe this minimal build.

Final runtime/visual/regression tests were skipped at the user's request. Compilation is not a claim of in-game validation; especially verify gameplay, camera behavior, and UI appearance on the target game build before regular use.

Based on Umbra by Aquatic Labs, originally derived from BennettStaley's Spektre/RoR2ModMenu and Lodington's fork. Existing repository licensing and attribution remain applicable.
