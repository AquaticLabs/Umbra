# Umbra — Release Features

Umbra is a modern, in-game Risk of Rain 2 mod menu with a clean deep-blue interface, organized pages, configurable controls, and host-aware features.

> Features marked **Host only** require you to be the lobby or run host. Umbra automatically disables unavailable controls when you are not hosting.

## Player

### Economy

- [x] View your current money and lunar coin balances.
- [x] Give yourself money. **Host only.**
- [x] Set your money to an exact amount. **Host only.**
- [x] Reset your money to zero. **Host only.**
- [x] Give yourself lunar coins. **Host only.**
- [x] Set your lunar coins to an exact amount. **Host only.**
- [x] Remove lunar coins. **Host only.**

> Lunar coin changes affect your saved profile and are not undone when Umbra is disabled or unloaded.

### Experience and health

- [x] View your current level and experience.
- [x] Give yourself experience. **Host only.**
- [x] Restore your health. **Host only.**
- [x] Respawn your character. **Host only.**
- [x] Infinite skill charges. **Host only.**

### God mode

- [x] **Invulnerable** — blocks incoming damage.
- [x] **Intangible** — prevents most attacks from contacting your character.
- [x] **Regeneration** — continuously restores missing health.
- [x] **Auto Revive** — automatically attempts to respawn you after death.

### Movement

- [x] **Always Sprint** — automatically sprints while moving.
- [x] **Flight**
  - Move normally to fly horizontally.
  - Sprint to increase flight speed.
  - Jump to ascend.
  - Hold `X` to descend.
- [x] **Jump Pack** — increases upward movement while jumping.
- [x] **Reset Movement** — disables all Umbra movement modifications.

### Character-specific features

- [x] **Railgunner Perfect Reload** — automatically activates successful boosted reloads.

### Stat tuning

- [x] Damage scaling with adjustable damage per level. **Host only.**
- [x] Critical-chance scaling with adjustable critical chance per level. **Host only.**
- [x] Adjustable base attack speed. **Host only.**
- [x] Adjustable base armor. **Host only.**
- [x] Adjustable base movement speed. **Host only.**
- [x] Modified stats return to their original values when their options are disabled.

## Aimbot

### Activation

- [x] Enable or disable the aimbot at any time.
- [x] **Hold Key** activation using a customizable key.
- [x] **Constant** activation while aimbot is enabled.
- [x] Customizable aimbot toggle and activation keybinds.
- [x] Current target display.

### Aim modes

- [x] **Direct** — immediately aims at the selected target.
- [x] **Smooth** — gradually pulls the camera toward the selected target.
- [x] **Fire Only** — aims only while primary fire is held.
- [x] Adjustable smoothing speed.
- [x] Optional primary-fire requirement for Direct and Smooth modes.

### Target selection

- [x] **Crosshair Priority** — targets the enemy closest to the center of the screen.
- [x] **Distance Priority** — targets the nearest eligible enemy.
- [x] **Lowest Health Priority** — targets the enemy with the lowest remaining health percentage.
- [x] **Boss First** — prioritizes bosses before regular enemies.
- [x] **Weak Point** targeting.
- [x] **Center Mass** targeting.
- [x] **Nearest Hurtbox** targeting.
- [x] Adjustable targeting field of view.
- [x] Adjustable maximum targeting distance.
- [x] Optional visibility checks.
- [x] Team, health, distance, field-of-view, and visibility filtering.

### Visual feedback

- [x] Customizable FOV circle.
  - Adjustable RGBA color.
  - Adjustable line thickness.
- [x] Customizable target line from the cursor or crosshair to the selected enemy.
  - Adjustable RGBA color.
  - Adjustable line thickness.

## Visuals

### ESP and world overlays

- [x] Enemy ESP.
- [x] Boss ESP.
- [x] Ally ESP.
- [x] Chest and interactable ESP.
- [x] Teleporter overlay.
- [x] Dropped-item and equipment overlays.
- [x] Distance labels.
- [x] Health bars.
- [x] Full or corner-style boxes.
- [x] Adjustable render distance.
- [x] Adjustable label size.
- [x] One-click essential-overlay preset.
- [x] One-click disable-all option.
- [x] Manual world-object refresh.
- [x] Properly centered labels when boxes are disabled.
- [x] Dynamic screen-space boxes that scale with objects at different distances.

### Full ESP customization

- [x] Each category can have its own:
  - Visibility setting.
  - Box setting.
  - Label setting.
  - RGBA color.
  - Outline thickness.
- [x] Customizable categories include:
  - Enemies and bosses.
  - Allies.
  - Chests and printers.
  - Equipment and item tiers.
  - Lunar and Void objects.
  - Chance, Blood, Combat, Mountain, Woods, and Newt shrines.
  - Barrels, scrappers, secrets, and teleporters.
  - Other uncategorized world objects.

### Individual item styles

- [x] Search items by display name or catalog name.
- [x] Create a custom ESP style for an individual item.
- [x] Independently customize its visibility, box, label, color, and thickness.
- [x] Restore an item to its normal tier style at any time.
- [x] Item colors also apply when displaying revealed chest contents.

### Reticle

- [x] Custom crosshair.
- [x] Adjustable crosshair color and thickness.
- [x] Aimbot FOV guide.
- [x] Adjustable FOV color and thickness.
- [x] Current aimbot target display.

## Items

### Item and equipment browser

- [x] Search by item name or catalog name.
- [x] Browse items using their native icons.
- [x] Filter by:
  - All
  - Common
  - Uncommon
  - Legendary
  - Boss
  - Lunar
  - Void
  - Equipment
  - Other
  - Currently owned items
- [x] Paginated results for fast browsing.
- [x] View how many copies of the selected item you own.

### Item actions

- [x] Give yourself selected items. **Host only.**
- [x] Give or replace your active equipment. **Host only.**
- [x] Drop new items or equipment into the world. **Host only.**
- [x] Drop items directly from your inventory. **Host only.**
- [x] Select quantities from `1–100`.
- [x] Replace the reward in the nearest unopened chest. **Host only.**

### Random items

- [x] Roll up to 100 random items at once. **Host only.**
- [x] Roll from a selected item tier.
- [x] Roll from all eligible item tiers.

### Inventory tools

- [x] Give one copy of every available item. **Host only.**
- [x] Restack your inventory using Shrine of Order behavior. **Host only.**
- [x] Clear removable items while preserving equipment and protected items. **Host only.**
- [x] Destructive and bulk actions require confirmation.

## World

### Portals

- [x] Spawn individual portal types instead of spawning every portal at once. **Host only.**
- [x] Supported portal types include:
  - Blue / Bazaar
  - Gold / Gilded Coast
  - Green / Colossus
  - Celestial / Obliteration
  - Void Fields
  - Deep Void
  - Void Exit
  - Destination
  - Tech / Hardware Progression
  - Tech / Haunt
  - Eye
  - Solus Exchange
  - Solus Return
  - Infinite Tower
- [x] Additional installed and DLC portal variants appear automatically when available.

> Portal destinations can depend on the current stage, game mode, and enabled expansions.

### Teleporter

- [x] Detect the active teleporter.
- [x] Accelerate teleporter charging. **Host only.**
- [x] Add a Shrine of the Mountain stack. **Host only.**

### Stage controls

- [x] Display the current scene and run state.
- [x] Skip to the next stage. **Host only.**
- [x] Kill hostile enemies without targeting players or neutral characters. **Host only.**

### Cleanup

- [x] Remove pickups, characters, portals, and interactables spawned through Umbra. **Host only.**
- [x] Native stage objects remain untouched.
- [x] Cleanup requires confirmation.

## Spawn

### Spawn browser

- [x] Search by name or spawn-card ID.
- [x] Browse separate variants rather than combining similarly named entries.
- [x] Filter by:
  - All
  - Common enemies
  - Bosses
  - Chests
  - Shrines
  - Drones
  - Printers
  - Portals
  - Other interactables
- [x] Native enemy portraits and interactable icons when available.
- [x] Clear category fallback for entries without artwork.
- [x] Loading status for large content catalogs.

### Placement

- [x] Spawn the exact selected entry. **Host only.**
- [x] Select Monster, Neutral, or Player team for character spawns.
- [x] Adjustable minimum and maximum spawn distance.
- [x] Expansion availability checks.
- [x] Clear feedback when a valid spawn location cannot be found.
- [x] One-click access to confirmed Umbra-spawn cleanup.

## Lobby

### Player selection

- [x] View connected players.
- [x] Explicitly select the player who should receive an action or gift.

### Player actions

- [x] Give money to another player. **Host only.**
- [x] Give lunar coins to another player. **Host only.**
- [x] Give the selected item or equipment to another player. **Host only.**
- [x] Attempt to revive a dead player. **Host only.**

> Lunar coins affect the selected player's saved profile. Confirm with them before making changes.

### Session information

- [x] Connected-player count.
- [x] Local-player name.
- [x] Current run seed.
- [x] Copy the run seed to the clipboard.
- [x] Current difficulty display.
- [x] Active-mod status overlay toggle.
- [x] Modded-session status.

## Misc

### On-screen information

- [x] FPS and ping display on the right side of the screen.
- [x] Run timer.
- [x] Player coordinates.

### Position bookmark

- [x] Save your current position.
- [x] Return to the saved position. **Host only.**
- [x] Saved positions automatically expire when changing stages.

### Cooldowns and active state

- [x] Infinite skills. **Host only.**
- [x] Infinite equipment cooldowns. **Host only.**
- [x] Active-mod status strip.
- [x] Disable all reversible gameplay modifications with one button.

### Appearance and lifecycle

- [x] Adjustable window transparency.
- [x] Adjustable rounded-corner radius.
- [x] Save or reset visual preferences.
- [x] Center the menu window.
- [x] View the installed Umbra version.
- [x] Unload Umbra from inside the menu.

## Interface and usability

- [x] Host-status badge.
- [x] In-run and no-character status display.
- [x] Non-host warning banner.
- [x] Host-only controls automatically gray out when unavailable.
- [x] Automatic preference saving.

## Controls and keybinds

- [x] `Insert` — open or close Umbra.
- [x] `End` — disable reversible gameplay modifications.
- [x] Assign keyboard or mouse buttons to supported toggles.
- [x] Assign a separate hold key for aimbot activation.
- [x] Assign shortcuts for opening Player, Items, and World pages.
- [x] `Escape` — cancel key capture.
- [x] `Delete` — clear a selected keybind.
- [x] Duplicate key assignments automatically move to the newly selected feature.
- [x] Host-only keybinds remain unavailable to non-host clients.

### Default keybinds

- `Mouse1` — "right click" hold-to-aim activation.
- `C` — Flight.
- `Z` — open Player.
- `I` — open Items.
- `B` — open World.
