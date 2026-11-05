# SurfaceForge

A Unity desktop terrain workspace with a finite terrain scene and a draggable prop catalog.

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

Selection, deletion and guide lines remain for later milestones. See the M3 validation record for the outstanding physical mouse acceptance checks.

| Stable ID / name | Model size (metres) | Model collider |
| --- | --- | --- |
| `cube` / Cube | 1.5 × 1.5 × 1.5 | BoxCollider |
| `sphere` / Sphere | 1.5 diameter | SphereCollider |
| `cylinder` / Cylinder | 1.5 diameter × 1.5 height | Convex MeshCollider |
| `cone` / Cone | 1.5 diameter × 1.5 height | Convex MeshCollider |

All prop roots use identity transforms and bottom-centre placement anchors. Their child `Model` is offset upward by 0.75 m. Colliders live on the model child; future selection should resolve the owning root from the hit child. Props use the Default layer, separate from the terrain's Surface layer, and have no Rigidbody.

Assets under `Assets/SurfaceForge/`:

- `Prefabs/Props/`: Cube, Sphere, Cylinder and Cone prefabs.
- `Data/Props/`: matching `PropDefinition` assets, with ID, display name, icon and prefab references.
- `UI/Prefabs/PropCatalogItem.prefab`: reusable icon/name row with a raycastable background.
- `UI/Icons/` and `Materials/`: matching icons and materials; `Models/Cone.asset` contains the original closed 32-sided cone mesh.
- `Scripts/`: catalog, drag gesture routing, placement lifecycle, explicit TerrainCollider query and placed-prop marker. Main assigns its camera, Surface mask, collider and two shared preview materials in Inspector. Runtime previews use Ignore Raycast with disabled colliders; placed props use Default, separate from Surface.
- `Tests/Editor/`: placement regression tests. Run `SurfaceForge.Tests.PlacementTests` from the Test Runner's EditMode tab; the fixture enters Play Mode in an isolated scene to exercise runtime callbacks. Save your scene before running tests.

The models use Unity primitives and an original cone mesh; icons and material colors were authored for this project. No external artwork was added.

See [M1 validation](Docs/M1Validation.md), [M2 validation](Docs/M2Validation.md) and [M3 validation](Docs/M3Validation.md) for performed checks and limitations. M3 awaits user manual acceptance; automated event dispatch and code review do not replace it. No desktop build or recording is included yet. Desktop executable and continuous native-window resize acceptance are deferred until the necessary features are complete.
