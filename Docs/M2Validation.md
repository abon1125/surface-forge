# M2 validation

## User acceptance update — 2026-11-06

The user confirmed desktop manual testing in Unity Editor Play Mode with no issues found. Editor Play Mode manual acceptance is recorded as complete based on that report; the individual checklist coverage, resolutions and repetition counts were not separately reported. Earlier pending-acceptance statements below are historical. Windows executable and continuous native-window resize acceptance remain scheduled for I2.

Environment: Unity 6000.3.25f1 Windows Editor, URP 17.3.0, Input System 1.20.0. Date: 2026-11-05. Baseline: main `73ffede`, including M1 `85c767a`.

## Delivered

- Four bottom-centred prop prefabs, four matching PropDefinition assets, four original icons, four URP materials, and an original closed cone mesh. The complete inventory and dimensions are in README.
- Reusable PropCatalogItem UI prefab with explicit Image/Text references and a bound Definition property for later pointer handling.
- Main Sidebar explicitly references the ordered definitions, row prefab and existing scroll content. Awake creates the rows once; expansion only changes visibility. The toggle listener is registered once and removed on destruction.
- M1 terrain, camera, input system and viewport layout are unchanged. Main's changes replace the empty state with data references and update its caption.

## Performed checks

- Clean compilation and Main Play Mode entry.
- Editor API asset inspection: exactly four unique, nonempty IDs; complete name/icon/prefab references; identity root transforms; model bottom-centres at the root; enabled non-trigger child colliders. Instantiated collider bounds start at y=0 and have 1.5 m height (0.002 m tolerance). Cylinder and cone use convex colliders referencing their model meshes. Cone triangle winding points outward.
- Runtime inspection: exactly four rows in the configured order; matching definition, label and icon; raycastable row backgrounds. Fifty consecutive UGUI pointer-click event dispatches each produced exactly one visibility transition. Disabling/re-enabling the component and Sidebar retained four rows and a single toggle response.
- Actual desktop mouse clicks in the 1280 × 720 Game View expanded and collapsed the list through Input System. This is distinct from the event-dispatch checks.
- Changed the actual Game View resolution during Play Mode to 1280 × 720, 1920 × 1080, 1024 × 768, 2560 × 1080 and 640 × 480. At each size checked Screen dimensions, toggle bounds, EventSystem raycast reachability, expand/collapse via UGUI events and positive viewport dimensions. Visually inspected correctly timed screenshots of the four rows at all five sizes.
- At each size added sixteen temporary row copies, confirmed content exceeded the viewport, dispatched a UGUI scroll event and verified content moved. Removed all temporary copies and confirmed exactly four production rows remained. This was event simulation, not a physical wheel-input test.
- Instantiated the four actual prefabs in a temporary Play Mode display and visually confirmed blue cube, orange sphere, green cylinder and purple cone with working materials. The display temporarily disabled terrain/camera fitting; exiting Play Mode discarded those changes. No model instances were saved into Main.
- Temporary authoring/validation code, overflow rows and screenshots are excluded from delivery. A temporary validator namespace compile error was corrected before continuing. Initial screenshots taken during resolution changes were rejected and recaptured after the target frame settled; a first-frame model capture before shader compilation settled was also not used as material evidence.

## Self review

Reviewed all scripts, scene changes, data references, prefab transforms, collider ownership and shape, row layout/raycast targets, listener lifetime, repeated visibility changes, metadata and asset serialization. Replaced Unity's default cylinder CapsuleCollider with a matching convex MeshCollider to avoid inaccurate rounded picking. Cube and sphere retain their exact primitive colliders. The cone mesh is Unity text-serialized YAML; PNG icons use the existing Git LFS rules. No binary asset was subjected to YAML newline conversion. No third-party artwork was introduced.

## Limits and manual acceptance

- The catalog is display-only. Drag preview, placement, selection, deletion and guides remain for later milestones. No placement or slope-alignment behavior is claimed by these prefab checks.
- Definitions and row dependencies are authored in Inspector; runtime editing/rebuilding of the catalog is not supported in this milestone. No permanent test harness or validation menu is shipped.
- Actual mouse input was checked at 1280 × 720. Other sizes and overflow scrolling used event dispatch plus geometry/raycast checks and screenshot inspection.
- No desktop build, executable input or continuous native-window resize test was performed. These are deferred until the necessary features are complete.
- Agent validation and cloud code review do not constitute user acceptance. Awaiting user manual acceptance.

1. Open Main and enter Play Mode: the catalog starts collapsed, with no new Console errors.
2. Click PROPS to open it: Cube, Sphere, Cylinder and Cone each show their own icon. Repeatedly close/open: exactly four rows remain.
3. Switch Game View to the five sizes above: the toggle and rows remain reachable. Physical wheel scrolling can be checked by temporarily duplicating rows during Play Mode; exit Play Mode to discard those copies.
4. Inspect each definition in Data/Props and its linked prefab/icon. Open prefabs: root transform is identity, child Model is at y=0.75, and its bottom is at local y=0. Collider shape matches the model; no Rigidbody is present.
5. Exit Play Mode. Do not save temporary runtime verification changes. Desktop acceptance remains deferred.
