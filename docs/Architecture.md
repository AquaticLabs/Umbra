# Maintained runtime

`UmbraMenu.csproj` explicitly compiles `Loader.cs`, drawing primitives, `Runtime`, `UI`, `UI/Pages`, `Gameplay`, and `Overlay`. Obsolete menu implementations, helpers, generated resources and Octokit are removed. Harmony is embedded and loaded before hook installation. Settings JSON uses the game's shipped SimpleJSON assembly, not Unity's native injected-type serialization.

## Lifecycle

- `Loader.cs`: unchanged `UmbraMenu.Loader.Load/Unload` injector contract. Requests are serialized and dispatched on `RoR2Application.onUpdate`, wait for content readiness and retry initialization up to three times. No Unity object creation occurs on the injector thread. Mono/RoR2 managed assemblies must already exist; native pre-runtime injection is outside this contract.
- `Runtime/UmbraRuntime.cs`: scene lifecycle, input routing, service updates and drawing. Initialization is explicit after readiness; shutdown is idempotent and independently releases each service. Preferences flush before teardown.
- `Runtime/RuntimeContext.cs`: nullable local-player snapshot, cleared at scene changes and unload; no menu ownership.
- `Gameplay/FeatureState.cs`: small state holders for the supported features; no legacy window constructors.
- `Gameplay/GameInput.cs`: game-owned menu/chat cursor detection. Spawn discovery belongs only to SpawnCatalog.

## UI

- `UI/MenuController.cs`: eight-page 950×750 shell, explicit page registry, host badge, cursor restoration, scrolling and two-pass card rendering. This replaces ModernMenu; there is no compatibility wrapper or competing menu owner.
- `UI/CardLayout.cs`: Unity-independent one/two-column packing. Full-width sections start below both preceding columns.
- `UI/Pages/PlayerPage.cs`, `AimbotPage.cs`, `VisualsPage.cs`, `ItemsPage.cs`, `WorldPage.cs`, `SpawnPage.cs`, `LobbyPage.cs`, `MiscPage.cs`: independent `IMenuPage` instances. Each owns its editing/filter/selection state. Existing card contents and column order are preserved. The selected inventory item is intentionally shared through the controller for Lobby gifts.
- `UI/MenuModels.cs`: the page contract, card definition, measured cursor and button action types.
- `UI/MenuInput.cs`: transparent uGUI raycast shield aligned to the window; press/release gesture ownership and text-field capture. Outside clicks clear text focus and remain available to the game.
- `UI/MenuWidgets.cs`: shared controls with identical cursor advancement in measure and paint passes. Wrapped paragraphs, hints, button rows, and narrow inputs contribute their actual heights. Only paint handles actions. Page-specific selectors remain in their page classes.
- `UI/MenuTheme.cs`: antialiased rounded-rectangle textures with matching nine-slice borders. Generated textures are owned and disposed; changing radius rebuilds them. Panel alpha does not fade text.

To add a control, account for all of its vertical space in both passes and gate interactive GUI calls behind `!c.Measuring`. To add a card, register its content with `AddCard`; do not introduce fixed content heights.

To edit a section, open its file under `UI/Pages`. To add a page, implement `IMenuPage`, then register one instance in `MenuController.CreatePages`. Window lifecycle stays in the controller, reusable controls in MenuWidgets, appearance resources in MenuTheme, and game commands in services/MenuActions. Pages are not partial fragments of the shell.

## Gameplay

- `Gameplay/MenuActions.cs`: input parsing, currency overflow checks, host/player preconditions, action feedback, spawns, stage controls, and lifecycle actions. UI actions report errors instead of silently failing.
- `Gameplay/StatOverrides.cs`: capture original values only when an override becomes active; restore on disable, body change, or unload. Recalculate only when values change.
- `Gameplay/ModernAimbot.cs`: resolve the actual selected aim point, then validate life/team, range, screen FOV, and optional world visibility. Score only eligible targets. Revalidate every frame and clear stale target indicators when aiming is unavailable.

Direct and Fire only set the game's input aim vector; Smooth interpolates it with frame-rate-independent exponential smoothing. These are not packet interception modes. No priority gets to select outside the FOV disk. Weak-point selection occurs before the disk/visibility checks, not afterward.

Camera steering uses `PlayerCharacterMasterController.RedirectCamera` (Euler-angle input). `GameHooks` runs aiming before `CameraRigController.LateUpdate`, scopes `LocalUser.isUIFocused` to Umbra capture, and applies flight after `CharacterMotor.UpdateVelocity`. All three patches use an instance-specific Harmony ID and are removed on unload. Failed installation tears down the incomplete runtime.

`MovementController` owns one anti-gravity grant and overrides motor velocity after native gravity/acceleration. Opening Umbra sets requested flight velocity to zero without disabling the flight hold. Body changes, disable and unload restore only Umbra's gravity/fall-protection ownership. `RailgunPerfectReload` caches local state machines per body, attempts boosts only inside the reload window with local authority, and clears its cache on disable/scene change/unload.

`KeyBindings` registers live getter/setter actions independently from page visits. Host restrictions apply at shortcut invocation as well as in the UI. Binding JSON contains assignments only. Capture consumes input exclusively, protects Insert/End, and resolves duplicate assignment by unbinding the old owner.

`ItemService` works with permanent stack APIs, validates host/count/run availability, and removes transferred stacks only after a network pickup exists. Equipment Give deliberately replaces the active slot. A single isolated private-setter adapter supports current-build chest replacement. Bulk destructive actions use a two-click confirmation.

`SpawnCatalog` discovers typed addressable csc/isc keys, loads four operations concurrently outside OnGUI, deduplicates exact names, and retains/releases its own handles. It clones spawn cards before setting network flags instead of editing shared game assets. Monster categories use body champion flags. Portal entries remain separate and retain native destination rules.

Monster artwork comes from CharacterBody.portraitIcon; interactable artwork comes from available IInspectInfoProvider inspect visuals. Sprite atlas UVs are used when drawing. Cards with no native icon display their category initial. Addressable discovery waits for content/resource locators rather than permanently accepting an empty early catalog.

`GodModes` owns invulnerability and hurtbox counters and restores only its changes. Regeneration and one-shot auto-revive requests are separate modes. `MiscFeatures` provides sampled FPS/ping, run timer/coordinates, and a scene-scoped bookmark. UI unloading is deferred to Update to avoid invalidating an active card iteration.

## Overlays

- `Overlay/ProjectionMath.cs`: shared perspective half-angle-to-pixel radius and inclusive disk test.
- `Overlay/OverlayGeometry.cs`: selects the local scene camera, translates to GUI coordinates, rejects offscreen/behind-camera aim points, and projects AABB corners plus near-plane intersections for ESP rectangles. Boxes are clipped to the viewport; a bounds volume containing the camera is omitted.
- `Overlay/EspRenderer.cs`: deduplicated scene-object cache refreshed outside `OnGUI`; current mesh bounds are recomputed for each repaint. Mesh/skinned-mesh bounds exclude particle/trail renderers. Colliders and small world-space bounds are fallbacks. Body/team/category/master visibility gates remain independent.
- `Overlay/VisualSettings.cs`: stable palette keys, tier fallback, exact item overrides, numeric validation and debounced autosaving. Missing fields in older files retain defaults; invalid palette entries are repaired.
- `Gameplay/SettingsJson.cs`: explicit managed serialization of preference fields, RGBA colors and style lists; key assignments likewise use explicit JSON arrays. No reliance on Unity's native type cache for injected classes.
- `Gameplay/SettingsFile.cs`: flushed UTF-8 temporary sibling followed by atomic replacement and `.bak`; tries the backup after a malformed primary and preserves unreadable files before later replacement. No gameplay modifier is enabled by restoring preferences.
- `ESPHelper.cs`: shared pixel texture and screen-space line, box, and circle primitives. Drawing restores incoming GUI tint/matrix state.

FOV radius is `viewportHeight / 2 × tan(aimHalfAngle) / tan(cameraVerticalFov / 2)`. Both the circle and final-point selection call this same calculation, using the camera center rather than the previously redirected aim vector. ESP dimensions come from perspective projection of world bounds; thickness remains constant in pixels.

## Scope and limitations

The styling is Trident-inspired but rendered inside Unity IMGUI. Item-level overrides cover `ItemCatalog`; equipment has a category-wide style. Prefab classification has an editable `Other` fallback for unrecognized interactables. Discovery is periodic, so newly spawned objects can take up to 1.5 seconds to appear unless refreshed manually. Boxless labels are one centered block rather than being anchored to invisible box edges. Host-only modifiers do not bypass game authority. One-shot changes are not rolled back by unload. Loading a portal does not guarantee its destination is valid in every stage/run type.

Final tests were explicitly skipped for this handoff. The build output demonstrates compatibility with the installed assembly signatures, not runtime correctness on every game version.
