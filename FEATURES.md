# Umbra Features

This document lists the features currently exposed by Umbra's maintained runtime, grouped by the menu page where each feature appears.

## Legend

- **Host only** — requires server authority. The control is disabled when the local user is not hosting.
- **Local** — affects only the local client or local presentation.
- **Persistent** — the preference or key assignment is restored after restarting Umbra.
- **Session only** — resets when Umbra is unloaded or the game closes.
- **Confirmation required** — a destructive or bulk action must be clicked twice within five seconds.

## Player

### Economy

- [x] **View current run money**
- [x] **Give money** — adds a validated unsigned amount to the local character master. **Host only.**
- [x] **Set money** — changes the local character master's balance to an exact value. **Host only.**
- [x] **Zero money** — sets the local balance to zero. **Host only.**
- [x] **View lunar coins**
- [x] **Give lunar coins** — awards persistent lunar currency through the game's network-user API. **Host only.**
- [x] **Set lunar coins** — calculates and applies the required award or deduction to reach an exact balance. **Host only.**
- [x] **Remove lunar coins** — deducts up to the current balance without unsigned underflow. **Host only.**
- [x] Currency inputs reject invalid values and arithmetic overflow.

> Lunar coin changes affect the player's persistent profile and are not reversed by Disable Gameplay Mods or unloading Umbra.

### Experience and vitals

- [x] **View level and experience** for the active local body.
- [x] **Give experience** — awards a validated 64-bit amount. **Host only.**
- [x] **Heal** — restores the active body to full health. **Host only.**
- [x] **Respawn** — requests an extra-life respawn through the local character master. **Host only.**
- [x] **Infinite skills** — repeatedly applies the game's ammo-pack refill behavior. **Host only; session only.**

### God mode

- [x] **Master god-mode toggle** — state is safely restored on disable, mode change, scene change, or unload. **Host only; session only.**
- [x] **Invulnerable** — enables the body's native god-mode flag while active.
- [x] **Intangible** — disables the player's hurtboxes using an owned deactivation counter.
- [x] **Regeneration** — repeatedly heals missing health; it does not prevent lethal one-shot damage.
- [x] **Auto revive** — requests a respawn after death; it cannot prevent a run from ending first.

### Movement

- [x] **Always sprint** — automatically sprints while directional input is present. **Local; session only.**
- [x] **Flight** — replaces motor velocity after native gravity and acceleration are evaluated. **Local; session only.**
  - Normal movement uses flight velocity.
  - Sprint increases flight speed.
  - Jump ascends.
  - `X` descends.
  - Opening Umbra suspends movement input and holds altitude instead of slowly falling.
  - Fall-damage and anti-gravity ownership are restored when flight is disabled, the body changes, the scene changes, or Umbra unloads.
- [x] **Jump pack** — applies an upward motor velocity while jump is held. **Local; session only.**
- [x] **Reset movement** — disables Always Sprint, Flight, and Jump Pack and immediately releases Umbra-owned movement state.

### Character-specific tools

- [x] **Railgunner Perfect Reload** — detects the active Railgunner reload boost window and attempts the perfect reload automatically. **Local; session only.**
  - State-machine lookup is cached per character body.
  - Requires local authority.
  - Cache is cleared on disable, scene change, and unload.

### Stat tuning

- [x] **Damage scaling** — configurable damage gained per level. **Host only; session only.**
- [x] **Critical scaling** — configurable critical chance gained per level. **Host only; session only.**
- [x] **Attack-speed override** — configurable base attack speed. **Host only; session only.**
- [x] **Armor override** — configurable base armor. **Host only; session only.**
- [x] **Move-speed override** — configurable base move speed. **Host only; session only.**
- [x] Original stat values are captured before modification and restored when a toggle is disabled, the character changes, the scene changes, or Umbra unloads.

## Aimbot

### Activation

- [x] **Aimbot master toggle** — can also be assigned a custom toggle key. **Local; session only.**
- [x] **Hold-key activation** — targets only while the configurable Aim Activation key is held; defaults to right mouse.
- [x] **Constant activation** — targets continuously while the aimbot is enabled and gameplay input is available.
- [x] Aiming pauses while Umbra is capturing input, a key is being rebound, chat/game UI owns the cursor, or no valid local character exists.
- [x] Current target name and camera/input hook status are shown in the menu.

### Aim modes

- [x] **Direct** — immediately redirects the camera and input aim vector to the selected target.
- [x] **Smooth** — physically turns the camera toward the selected target using frame-rate-independent spherical interpolation.
  - Smoothing speed is adjustable from `1` to `30`.
- [x] **Fire only** — aims only while primary fire is held.
- [x] **Require primary fire** — can independently require primary fire in Direct or Smooth mode.

> Fire Only redirects the game aim vector; it is not packet-level pseudo-silent aiming.

### Target rules

- [x] **Crosshair priority** — chooses the eligible target nearest the center of the screen.
- [x] **Distance priority** — chooses the nearest eligible target in world space.
- [x] **Lowest-health priority** — prefers the lowest remaining health percentage, with screen distance as a tie-breaker.
- [x] **Boss-first priority** — prefers bosses, then uses screen distance.
- [x] **Weak-point aim** — uses a sniper-designated hurtbox when available.
- [x] **Center-mass aim** — targets the character body's core position.
- [x] **Nearest-hurtbox aim** — selects the hurtbox nearest the local aim origin.
- [x] **Adjustable FOV** — circular camera-space half-angle from `2°` to `60°`.
- [x] **Adjustable maximum range** — `25` to `500` metres.
- [x] **Visibility check** — rejects targets blocked by world geometry.
- [x] Team, health, range, FOV, and visibility filters are applied before priority scoring; a priority mode cannot select an invalid or off-FOV target.

### Aim feedback

- [x] **FOV circle** — enable/disable, RGBA color, and `1–6 px` thickness. **Persistent.**
- [x] **Target line** — draws from the cursor, or crosshair while locked, to the selected aim point. **Persistent.**
  - Independent RGBA color.
  - Adjustable `1–6 px` thickness.

## Visuals

### World overlays

- [x] **Enemy ESP** — renders living hostile character bodies.
- [x] **Interactable ESP** — renders discovered interactables such as chests, shrines, printers, barrels, scrappers, and special interactables.
- [x] **Teleporter overlay** — independently controls teleporter visibility.
- [x] **Dropped-pickup overlay** — displays world pickups.
- [x] **Ally overlay** — optionally displays friendly character bodies.
- [x] **Distance labels** — appends range to overlay labels.
- [x] **Health bars** — renders live health for character overlays.
- [x] **Corner boxes** — switches full rectangles to compact corner outlines.
- [x] **Render range** — adjustable from `25` to `1,000` metres.
- [x] **Label size** — adjustable from `10` to `20 px`.
- [x] **Enable essentials** — enables enemies, interactables, teleporter, and crosshair together.
- [x] **Disable all** — disables enemy/interactable/pickup/ally/teleporter overlays, crosshair, FOV guide, and active-mod display.
- [x] **Refresh objects** — immediately rebuilds the cached world-object list.
- [x] Boxless mode centers a compact text block instead of leaving space for an invisible box.
- [x] ESP boxes use projected live mesh/collider bounds rather than a fixed distance estimate, allowing boxes to scale with the object on screen.

### Category styling

- [x] Every ESP category has independent controls for:
  - Enabled/disabled state.
  - Box visibility.
  - Label visibility.
  - RGBA color.
  - `1–6 px` outline thickness.
- [x] Editable categories:
  - Enemy
  - Boss
  - Ally
  - Chest
  - Printer
  - Equipment
  - Lunar
  - Void
  - Chance Shrine
  - Blood Shrine
  - Combat Shrine
  - Mountain Shrine
  - Shrine of the Woods
  - Newt Altar
  - Barrel
  - Scrapper
  - Secret
  - Teleporter
  - Common Item
  - Uncommon Item
  - Legendary Item
  - Boss Item
  - Lunar Item
  - Void Item
  - Other

### Per-item overrides

- [x] Search the loaded item catalog by display name or internal catalog ID.
- [x] Create a dedicated style for an individual item.
- [x] Configure the item's visibility, box, label, RGBA color, and thickness independently from its tier.
- [x] **Use tier style** removes the individual override and restores category inheritance.
- [x] Item colors also apply to revealed chest contents.

### Reticle

- [x] **Crosshair** — enable/disable, RGBA color, and `1–6 px` thickness. **Persistent.**
- [x] **FOV guide** — shares the aimbot's exact camera projection and accepted radius. **Persistent.**
- [x] Current aimbot target is shown on the page.

### Visual preference management

- [x] Save visual preferences immediately.
- [x] Reset visual preferences to validated defaults.
- [x] Visual preferences autosave after a short debounce and flush when the menu closes, the game loses focus, Umbra unloads, or the application exits.

## Items

### Item and equipment catalog

- [x] Search by localized display name or internal catalog ID.
- [x] Filter by All, Common, Uncommon, Legendary, Boss, Lunar, Void, Equipment, Other, or Inventory.
- [x] Paginated results with native item/equipment icons.
- [x] Inventory filtering displays owned permanent item stacks.
- [x] DLC or currently unavailable pickups remain browseable; mutation actions validate run availability before applying them.
- [x] Selected-item state is shared with the Lobby page for player gifts.

### Selected pickup actions

- [x] **View owned count** for the selected item or equipment.
- [x] **Give** — adds permanent item stacks to the local inventory. Equipment replaces the active equipment slot and requires quantity `1`. **Host only.**
- [x] **Drop** — creates new networked ground pickups around the player without removing inventory. **Host only.**
- [x] **Drop from inventory** — creates pickups and removes owned permanent copies only after each pickup is successfully created. **Host only.**
- [x] Quantity validation from `1` to `100`.
- [x] **Replace nearest chest** — changes the selected reward of the nearest unopened chest within `25 m`. **Host only; confirmation required.**

### Random items

- [x] Roll `1–100` random items from the currently selected item tier.
- [x] All-tier mode includes every eligible item tier.
- [x] Equipment, hidden/internal definitions, and unavailable run items are excluded. **Host only.**

### Inventory tools

- [x] **Give all +1** — adds one eligible permanent copy of every available item; equipment is excluded. **Host only; confirmation required.**
- [x] **Restack inventory** — applies the game's Shrine of Order restacking behavior. **Host only; confirmation required.**
- [x] **Clear removable items** — clears removable permanent items while preserving hidden/internal items, temporary state, and equipment. **Host only; confirmation required.**

## World

### Individual portals

- [x] Discovers and lists every loaded portal spawn card independently.
- [x] Known portal labels include:
  - Blue — Bazaar Between Time
  - Gold — Gilded Coast
  - Green — Colossus
  - Celestial — Obliteration
  - Void Fields
  - Deep Void
  - Void Exit
  - Destination Portal
  - Tech — Hardware Progression
  - Tech — Haunt
  - Eye Portal
  - Solus Exchange
  - Solus Return
  - Infinite Tower
- [x] Additional installed or DLC portal variants remain individually selectable using generated names.
- [x] Each button spawns only its selected portal. **Host only.**

> Portal availability and the eventual destination still depend on installed content, enabled expansions, stage rules, and game mode.

### Teleporter

- [x] Detect whether an active teleporter exists.
- [x] **Instant charge** — reduces the active holdout zone's base charge duration to one second. **Host only.**
- [x] **Mountain +1** — adds one Shrine of the Mountain stack to the active teleporter. **Host only.**

### Stage controls

- [x] View the current scene and whether a run is active.
- [x] **Skip stage** — advances to the run's configured next stage. **Host only.**
- [x] **Kill enemies** — kills living Monster, Void, and Lunar team bodies without targeting Player or Neutral teams. **Host only.**

### Cleanup

- [x] **Destroy Umbra spawns** — removes only objects tracked as created by Umbra; native stage objects are untouched. **Host only; confirmation required.**

## Spawn

### Spawn catalog

- [x] Asynchronously discovers loaded and addressable character/interactable spawn cards without blocking the GUI.
- [x] Search by display name or exact spawn-card ID.
- [x] Filter by All, Common, Boss, Chest, Shrine, Drone, Printer, Portal, or Other.
- [x] Paginated results keep variants as independent entries rather than collapsing similar names.
- [x] Monster categorization uses the character body's champion/boss classification.
- [x] Monster cards use native body portrait icons when available.
- [x] Chests and other interactables use native inspect-info artwork when available.
- [x] Atlas-aware sprite coordinates prevent packed icons from drawing the wrong image.
- [x] Assets with no artwork display a category-letter fallback.
- [x] Catalog status reports loaded, pending, and unavailable entries.

### Placement and spawning

- [x] Spawn the exact selected card through the game's Director system. **Host only.**
- [x] Select Monster, Neutral, or Player team for character spawns.
- [x] Configure minimum placement distance from `1–30 m`.
- [x] Configure maximum placement distance from `10–100 m`.
- [x] Expansion requirements are checked before spawning.
- [x] Invalid placement reports an error instead of silently failing.
- [x] Spawned objects are networked and tracked for later cleanup.
- [x] **Clean up Umbra spawns** is available directly from this page. **Host only; confirmation required.**

## Lobby

### Player selection

- [x] Lists connected network users.
- [x] Requires explicit recipient selection; a disconnected recipient never silently redirects a gift.

### Host gifts and player actions

- [x] **Give money** to the selected player's character master with overflow validation. **Host only.**
- [x] **Give lunar coins** to the selected player's persistent network profile with overflow validation. **Host only.**
- [x] **Choose item** opens the Items page while retaining the selected lobby recipient.
- [x] **Give item/equipment** sends the item selected on the Items page to the recipient's inventory. Equipment replaces their active slot. **Host only.**
- [x] **Attempt revive** respawns a dead recipient at their recorded death position and rejects players who are already alive. **Host only.**

### Session information

- [x] Connected-player count.
- [x] Local-player name.
- [x] Current run seed.
- [x] **Copy run seed** to the system clipboard.
- [x] Toggle the active-mod overlay strip.
- [x] Display current run difficulty as read-only information.
- [x] Display the application's modded state.

## Misc

### On-screen telemetry

- [x] **FPS and ping** — right-aligned overlay updated four times per second. Hosts display `Host / 0 ms`; clients display measured RTT when connected. **Persistent.**
- [x] **Run timer** — displays the active run stopwatch as `hh:mm:ss`. **Persistent.**
- [x] **Coordinates** — displays the local character's world position. **Persistent.**

### Position bookmark

- [x] **Save position** — records the local character's position in the current scene.
- [x] **Return** — teleports the host, their minions, and relevant state back to the saved position. **Host only.**
- [x] Bookmarks expire automatically when the active scene changes.

### Cooldowns and state

- [x] **Infinite skills** — duplicate access to the Player-page skill refill toggle. **Host only; session only.**
- [x] **Infinite equipment** — resets active equipment recharge time while preserving equipment and charges. **Host only; session only.**
- [x] **Active mod strip** — shows currently active Umbra features on screen. **Persistent.**
- [x] **Disable gameplay mods** — disables aimbot, god mode, infinite skills/equipment, Railgunner reload assistance, stat overrides, and movement modifications, restoring owned transient state.

> Disable Gameplay Mods does not undo one-shot actions such as currency changes, experience, item gifts, drops, spawns, stage changes, or portal creation.

### Appearance

- [x] Adjustable window opacity from `40%` to `100%`; text remains opaque. **Persistent.**
- [x] Adjustable rounded-corner radius from `0–10 px`. **Persistent.**
- [x] Save or reset visual preferences.
- [x] Center the menu window.

### Lifecycle

- [x] Display the current Umbra build version.
- [x] **Unload Umbra** — defers teardown outside the active GUI iteration.
- [x] Unload restores owned movement, god-mode, stat, cursor, input, hook, texture, catalog, and cache state.

## Shared menu and input features

- [x] Modern deep-blue Unity IMGUI shell with a default `950 × 750` window.
- [x] Responsive one- or two-column measured-card layout with scrolling; controls are measured before painting to prevent clipped or unreachable settings.
- [x] Slightly transparent panels with opaque text and native-resolution typography.
- [x] Eight dedicated pages: Player, Aimbot, Visuals, Items, World, Spawn, Lobby, and Misc.
- [x] In-run/no-character indicator.
- [x] Host capability badge.
- [x] Non-host warning banner explaining that host-only controls are unavailable.
- [x] Host-only cards are automatically grayed out when server authority is absent.
- [x] Transparent input shield blocks click-through inside the menu while leaving clicks outside the menu available to the game.
- [x] Clicking outside clears Umbra text focus.
- [x] Cursor visibility and lock state are restored when the menu closes.
- [x] `Insert` opens or closes Umbra.
- [x] `End` disables gameplay modifiers.

## Keybindings

- [x] Custom toggle keys for gameplay and overlay features.
- [x] Custom hold key for aimbot activation.
- [x] Custom shortcuts for opening Player, Items, and World pages.
- [x] Click a binding control, then press a keyboard or mouse button to assign it.
- [x] `Escape` cancels capture.
- [x] `Delete` clears a binding.
- [x] Duplicate assignments are resolved by unbinding the previous owner.
- [x] `Insert`, `End`, `Escape`, and `Delete` are protected lifecycle/capture keys.
- [x] Host-only shortcuts enforce host authority when invoked.
- [x] Gameplay shortcuts are suppressed while Umbra is open or the game owns the cursor.
- [x] Defaults include:
  - `Mouse1` — hold-to-aim activation.
  - `C` — Flight toggle.
  - `Z` — open Player.
  - `I` — open Items.
  - `B` — open World.
- [x] Key assignments are stored separately from gameplay toggle state. **Persistent.**

## Settings and startup behavior

- [x] Shared project identity:
  - Settings base: `Trident`
  - Game name: `Umbra`
- [x] Per-game settings directory: `%APPDATA%\Trident\Games\Umbra\`
- [x] Visual settings file: `settings.json`
- [x] Keybinding file: `keys.json`
- [x] Atomic saves use a temporary sibling and retain the previous version as `.bak`.
- [x] Invalid primary settings can recover from the backup and are preserved as `.invalid-*` before replacement.
- [x] Legacy settings can be imported from `%APPDATA%\UmbraMenu\` without deleting the old files.
- [x] Numeric values and colors are validated and clamped during load/save.
- [x] Loading preferences never enables gameplay mutations; gameplay modifiers remain session-only.
- [x] Load requests are queued onto RoR2's main update loop rather than creating Unity objects on the injector thread.
- [x] Early managed injection waits for RoR2 content initialization.
- [x] Repeated load calls are idempotent.
- [x] Initialization failures clean up partial state and retry a bounded number of times.
- [x] Hook ownership uses an instance-specific Harmony ID so unloading removes only Umbra's patches.

## Current scope and limitations

- Umbra is intended for private testing with informed participants.
- Host-only controls do not bypass Risk of Rain 2 network authority.
- The interface is Trident-inspired Unity IMGUI; it does not embed Dear ImGui.
- Difficulty is intentionally read-only after a run starts.
- Aimbot Fire Only is aim-vector redirection, not packet-level pseudo-silent aiming.
- Regeneration cannot protect against lethal one-shot damage.
- Auto Revive cannot guarantee recovery after the run has already ended.
- One-shot mutations are not automatically reversible.
- Portal destination behavior remains controlled by the game and installed content.
- Spawn and item catalogs include only content available to the running game build and loaded addressable catalogs.
- Compilation confirms compatibility with the installed managed assemblies; runtime behavior should still be validated in the target game build.
