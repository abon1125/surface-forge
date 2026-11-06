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

Guide lines and landing markers are planned for the independent VR simulation scene, not the desktop scene. M3 physical input acceptance gaps are retained in the validation records.

Planned development continues with a keyboard/mouse VR simulation in `Assets/SurfaceForge/Scenes/VRSimulation.unity`, using a separate TerrainData copy and shared prop assets. After both scenes are ready, a startup scene menu will let reviewers choose Desktop or VR Simulation in one Windows app and return to the menu. Complete boundary validation and the final build are deferred to that integration stage; basic checks continue during feature development. The VR scene and startup menu are not implemented yet. See the [development plan](Docs/DevelopmentPlan.md) and [VR simulation specification](Docs/VRSimulationDevelopment.md). Keyboard/mouse simulation does not constitute hardware XR validation.

| Stable ID / name | Model size (metres) | Model collider |
| --- | --- | --- |
| `cube` / Cube | 1.5 × 1.5 × 1.5 | BoxCollider |
| `sphere` / Sphere | 1.5 diameter | SphereCollider |
| `cylinder` / Cylinder | 1.5 diameter × 1.5 height | Convex MeshCollider |
| `cone` / Cone | 1.5 diameter × 1.5 height | Convex MeshCollider |

All prop roots use identity transforms and bottom-centre placement anchors. Their child `Model` is offset upward by 0.75 m. Selection resolves model child colliders to the owning `PlacedProp` root. Source prefabs use Default; the complete hierarchy of each placed instance uses the dedicated PlacedProp layer, separate from Surface. No Rigidbody is present.

Assets under `Assets/SurfaceForge/`:

- `Prefabs/Props/`: Cube, Sphere, Cylinder and Cone prefabs.
- `Data/Props/`: matching `PropDefinition` assets, with ID, display name, icon and prefab references.
- `UI/Prefabs/PropCatalogItem.prefab`: reusable icon/name row with a raycastable background.
- `UI/Icons/` and `Materials/`: matching icons and materials; `Models/Cone.asset` contains the original closed 32-sided cone mesh.
- `Scripts/`: catalog, gesture routing, placement, TerrainCollider query, placed-prop marker, selection and UGUI context menu. Main explicitly assigns camera, masks, collider, shared preview materials and UI references. Previews use Ignore Raycast with disabled colliders. Selection queries PlacedProp within 500 m; neither query changes the other.
- `Tests/Editor/`: placement and selection regression tests. Run `SurfaceForge.Tests` from the Test Runner's EditMode tab; fixtures enter Play Mode and wait for real PlayerLoop frames. Placement uses an isolated scene; selection integration opens Main. Save your scene before running tests. Input System test mice and UGUI invocation are distinct from desktop input.

The models use Unity primitives and an original cone mesh; icons and material colors were authored for this project. No external artwork was added.

See [M1 validation](Docs/M1Validation.md), [M2 validation](Docs/M2Validation.md), [M3 validation](Docs/M3Validation.md) and [M4 validation](Docs/M4Validation.md) for performed checks and limitations. M3 and M4 await user manual acceptance; automation and code review do not replace it. No desktop build or recording is included yet. Executable and continuous native-window resize acceptance will be completed during scene-menu integration after both scenes are ready.
