# SurfaceForge

A Unity desktop terrain workspace with a finite terrain scene, draggable prop catalog, and prop selection/deletion.

- Unity 6000.3.25f1, URP 17.3.0, Input System 1.20.0.
- Open `Assets/SurfaceForge/Scenes/Main.unity` and enter Play Mode.
- The right sidebar starts collapsed. Click **PROPS / OPEN +** to expand the scrollable list area; click again to close.
- The camera reserves space for the sidebar and fits the terrain as the viewport changes.
- The shared build scene list starts with Main. Desktop defaults are a resizable 1280 × 720 window.

The catalog displays four props with original icons and distinct URP materials. Rows are created once from the explicitly assigned definitions on the Sidebar's `PropCatalogView`.

- Drag a row **horizontally to the left** past the UI drag threshold to copy a prop. Vertical gestures scroll the list; the initial direction owns the entire gesture. Mouse wheel scrolling remains available.
- A green preview follows a valid terrain hit and aligns its bottom anchor and up axis with the hit point and normal. Red indicates no terrain hit; the preview is hidden while over UI.
- Release on the terrain to place one static prop. Release over any UI or outside the terrain to cancel. Escape, focus loss, leaving the Game View, or disabling the source/controller also cancel.
- Original catalog rows remain available. Placed props retain their definition and model colliders. Overlap is allowed and placement is not saved between sessions.

- Left-click a placed prop without dragging to select it. Click another visible prop to switch; click terrain or empty scene space to clear. UI clicks retain the scene target.
- The nearby menu identifies the selected prop. **DELETE** removes only that instance. The menu follows the target and viewport, stays inside the canvas and left of the sidebar, and hides behind the camera or outside its viewport.
- Starting a catalog drag (including vertical scrolling), losing focus, or disabling selection/menu clears selection. A placement release never doubles as a selection click; a fresh scene click is required.

M3 physical input acceptance gaps are retained in the validation records. The desktop preview anchor already coincides with the terrain hit, so a preview-to-ground guide line is no longer the next planned feature. A visible simulated-controller pointing ray belongs to the later simulation mode.

M5 adds **V** to switch between overview and a desktop first-person VR-style simulation. One capsule player prefab is spawned with a head anchor; the existing camera moves to that anchor and the sidebar slides fully off canvas. Press **V** again to restore overview and the same catalog state. Placed props are retained. Transitions reject reentry, cancel placement/selection/scroll gestures and suspend overview input. Held mouse buttons must be released before overview input resumes. Focus loss or disabling the mode controller returns to overview.

Current stage and planned work (roadmap revised 2026-11-05):

| Stage | Scope |
| --- | --- |
| M5 (implemented; manual acceptance pending) | Press **V** to switch between the current overview and a keyboard/mouse VR-style first-person view. Entering spawns one simulated player and moves the camera to its head anchor; the right sidebar slides off to the right and stops receiving input. Returning detaches/restores the overview camera before removing the simulated player, and slides the sidebar back. Cancel active gestures/selection and retain placed props across switches. |
| M6 | Capsule player with two controller spheres at fixed relative positions; WASD movement, right-button mouse look, mouse aiming and a visible ray originating from the right sphere. |
| M7 | **Tab** toggles a world-space prop panel beside the left sphere. The right-sphere ray operates UI and drags copies onto terrain, sharing placement/cancellation rules. No hand-flip gestures. |
| M8 | Ray-based selection/deletion, mode arbitration, interruption handling and full regression checks. |
| M9 | Windows build, manual acceptance of both modes, native-window resizing, recording and delivery documentation. |
| M10 | Optional real XR integration and device testing when hardware is available. |

M5 is limited to switching, player/camera lifecycle and sidebar animation. It does not yet provide simulated locomotion, controller interaction or the left-controller panel. Transitions must not re-enter or create duplicate players, cameras/listeners or input handling. The existing overview fitting is suspended in simulation and restored on return, including after a viewport resize. VR-style simulation uses a normal desktop view and does not constitute headset tracking, stereo rendering or hardware acceptance; no XR package installation is required for this plan.

| Stable ID / name | Model size (metres) | Model collider |
| --- | --- | --- |
| `cube` / Cube | 1.5 × 1.5 × 1.5 | BoxCollider |
| `sphere` / Sphere | 1.5 diameter | SphereCollider |
| `cylinder` / Cylinder | 1.5 diameter × 1.5 height | Convex MeshCollider |
| `cone` / Cone | 1.5 diameter × 1.5 height | Convex MeshCollider |

All prop roots use identity transforms and bottom-centre placement anchors. Their child `Model` is offset upward by 0.75 m. Selection resolves model child colliders to the owning `PlacedProp` root. Source prefabs use Default; the complete hierarchy of each placed instance uses the dedicated PlacedProp layer, separate from Surface. No Rigidbody is present.

Assets under `Assets/SurfaceForge/`:

- `Prefabs/Player/SimulatedPlayer.prefab`: identity root, capsule body and explicitly assigned 1.7 m HeadAnchor. Main assigns a safe spawn transform and all camera/UI/input dependencies. No camera or AudioListener is stored in the prefab.
- `Prefabs/Props/`: Cube, Sphere, Cylinder and Cone prefabs.
- `Data/Props/`: matching `PropDefinition` assets, with ID, display name, icon and prefab references.
- `UI/Prefabs/PropCatalogItem.prefab`: reusable icon/name row with a raycastable background.
- `UI/Icons/` and `Materials/`: matching icons and materials; `Models/Cone.asset` contains the original closed 32-sided cone mesh.
- `Scripts/`: catalog, gesture routing, placement, TerrainCollider query, placed-prop marker, selection and UGUI context menu. Main explicitly assigns camera, masks, collider, shared preview materials and UI references. Previews use Ignore Raycast with disabled colliders. Selection queries PlacedProp within 500 m; neither query changes the other.
- `Tests/Editor/`: placement, selection and view-mode regression tests. Run `SurfaceForge.Tests` from the Test Runner's EditMode tab; fixtures enter Play Mode and wait for real PlayerLoop frames. Placement uses an isolated scene; selection integration opens Main. Save your scene before running tests. Input System test mice and UGUI invocation are distinct from desktop input.

The models use Unity primitives and an original cone mesh; icons and material colors were authored for this project. No external artwork was added.

See [M1 validation](Docs/M1Validation.md), [M2 validation](Docs/M2Validation.md), [M3 validation](Docs/M3Validation.md), [M4 validation](Docs/M4Validation.md) and [M5 validation](Docs/M5Validation.md) for performed checks and limitations. The user has reported normal basic M4 interaction in Play Mode; detailed physical-input edge cases remain tracked separately. Automation, code review and PR merging do not replace manual acceptance. No desktop build or recording is included yet. Both-mode desktop executable and continuous native-window resize acceptance are planned for M9.
