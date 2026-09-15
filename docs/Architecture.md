# Maintained runtime

`UmbraMenu.csproj` explicitly compiles the root runtime files plus `UI`, `Gameplay`, and `Overlay`. Original `Menus`, `Overrides`, and helper/settings sources are migration references, not active runtime modules. Do not add them back through a recursive compile glob: their types overlap with the minimal feature state. Harmony is embedded and loaded before installing the camera/input hooks.

## Lifecycle

- `Loader.cs`: stable injector entry/exit points and single-instance replacement. Cleanup runs before Unity's delayed destruction.
- `UmbraMenu.cs`: local player references, scene changes, input routing, gameplay updates, ESP/UI rendering, and reversible god/movement ownership. Cleanup is idempotent.
- `Gameplay/FeatureState.cs`: small state holders for the supported features; no legacy window constructors.
- `Gameplay/GameUtility.cs`: game cursor detection and sorted loaded spawn-card discovery.

## UI

- `ModernMenu.cs`: eight-page 950×750 shell, host capability display, cursor restoration, scrolling and two-pass card rendering.
- `UI/CardLayout.cs`: Unity-independent one/two-column packing. Full-width sections start below both preceding columns.
- `UI/MenuPages.cs`, `AimPage.cs`, `ItemPage.cs`, `SpawnPage.cs`, `LobbyGifts.cs`, `MiscExtras.cs`: measured page definitions and compact catalog browsers; no guessed card heights.
- `UI/MenuInput.cs`: transparent uGUI raycast shield aligned to the window; press/release gesture ownership and text-field capture. Outside clicks clear text focus and remain available to the game.
- `UI/MenuControls.cs`: the same cursor advancement in measure and paint passes. Wrapped paragraphs, hints, button rows, and narrow input layouts contribute their actual heights. Only paint handles user actions.
- `UI/MenuTheme.cs`: antialiased rounded-rectangle textures with matching nine-slice borders. Generated textures are owned and disposed; changing radius rebuilds them. Panel alpha does not fade text.

To add a control, account for all of its vertical space in both passes and gate interactive GUI calls behind `!c.Measuring`. To add a card, register its content with `AddCard`; do not introduce fixed content heights.

## Gameplay

- `Gameplay/MenuActions.cs`: input parsing, currency overflow checks, host/player preconditions, action feedback, spawns, stage controls, and lifecycle actions. UI actions report errors instead of silently failing.
- `Gameplay/StatOverrides.cs`: capture original values only when an override becomes active; restore on disable, body change, or unload. Recalculate only when values change.
- `Gameplay/ModernAimbot.cs`: resolve the actual selected aim point, then validate life/team, range, screen FOV, and optional world visibility. Score only eligible targets. Revalidate every frame and clear stale target indicators when aiming is unavailable.

Direct and Fire only set the game's input aim vector; Smooth interpolates it with frame-rate-independent exponential smoothing. These are not packet interception modes. No priority gets to select outside the FOV disk. Weak-point selection occurs before the disk/visibility checks, not afterward.

Version 2.3 also steers the actual camera through `PlayerCharacterMasterController.RedirectCamera` (Euler-angle input). `GameHooks` runs aiming before `CameraRigController.LateUpdate` and scopes `LocalUser.isUIFocused` to Umbra's input capture. Both patches use an owned Harmony ID and are removed on unload. The Aimbot page reports hook installation status. A failed hook is not silently reported as working.

`KeyBindings` registers live getter/setter actions independently from page visits. Host restrictions apply at shortcut invocation as well as in the UI. Binding JSON contains assignments only. Capture consumes input exclusively, protects Insert/End, and resolves duplicate assignment by unbinding the old owner.

`ItemService` works with permanent stack APIs, validates host/count/run availability, and removes transferred stacks only after a network pickup exists. Equipment Give deliberately replaces the active slot. A single isolated private-setter adapter supports current-build chest replacement. Bulk destructive actions use a two-click confirmation.

`SpawnCatalog` discovers typed addressable csc/isc keys, loads four operations concurrently outside OnGUI, deduplicates exact names, and retains/releases its own handles. It clones spawn cards before setting network flags instead of editing shared game assets. Monster categories use body champion flags. Portal entries remain separate and retain native destination rules.

`GodModes` owns invulnerability and hurtbox counters and restores only its changes. Regeneration and one-shot auto-revive requests are separate modes. `MiscFeatures` provides sampled FPS/ping, run timer/coordinates, and a scene-scoped bookmark. UI unloading is deferred to Update to avoid invalidating an active card iteration.

## Overlays

- `Overlay/ProjectionMath.cs`: shared perspective half-angle-to-pixel radius and inclusive disk test.
- `Overlay/OverlayGeometry.cs`: selects the local scene camera, translates to GUI coordinates, rejects offscreen/behind-camera aim points, and projects AABB corners plus near-plane intersections for ESP rectangles. Boxes are clipped to the viewport; a bounds volume containing the camera is omitted.
- `Overlay/EspRenderer.cs`: deduplicated scene-object cache refreshed outside `OnGUI`; current mesh bounds are recomputed for each repaint. Mesh/skinned-mesh bounds exclude particle/trail renderers. Colliders and small world-space bounds are fallbacks. Body/team/category/master visibility gates remain independent.
- `Overlay/VisualSettings.cs`: stable palette keys, tier fallback, exact item overrides, numeric validation, and visual JSON persistence using a temporary sibling file before replacement.
- `ESPHelper.cs`: shared pixel texture and screen-space line, box, and circle primitives. Drawing restores incoming GUI tint/matrix state.

FOV radius is `viewportHeight / 2 × tan(aimHalfAngle) / tan(cameraVerticalFov / 2)`. Both the circle and final-point selection call this same calculation, using the camera center rather than the previously redirected aim vector. ESP dimensions come from perspective projection of world bounds; thickness remains constant in pixels.

## Scope and limitations

The styling is Trident-inspired but rendered inside Unity IMGUI. Item-level overrides cover `ItemCatalog`; equipment has a category-wide style. Prefab classification has an editable `Other` fallback for unrecognized interactables. Discovery is periodic, so newly spawned objects can take up to 1.5 seconds to appear unless refreshed manually. Boxless labels are one centered block rather than being anchored to invisible box edges. Host-only modifiers do not bypass game authority. One-shot changes are not rolled back by unload. Loading a portal does not guarantee its destination is valid in every stage/run type.

Final tests were explicitly skipped for this handoff. The build output demonstrates compatibility with the installed assembly signatures, not runtime correctness on every game version.
