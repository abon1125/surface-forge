# SurfaceForge

A Unity desktop terrain workspace with a finite terrain scene and a data-driven prop catalog.

- Unity 6000.3.25f1, URP 17.3.0, Input System 1.20.0.
- Open `Assets/SurfaceForge/Scenes/Main.unity` and enter Play Mode.
- The right sidebar starts collapsed. Click **PROPS / OPEN +** to expand the scrollable list area; click again to close.
- The camera reserves space for the sidebar and fits the terrain as the viewport changes.
- The shared build scene list starts with Main. Desktop defaults are a resizable 1280 × 720 window.

The catalog displays four props with original icons and distinct URP materials. Rows are created once from the explicitly assigned definitions on the Sidebar's `PropCatalogView`. `PropCatalogItemView.Definition` exposes each row's bound data for a future pointer handler; dragging, placement, selection, deletion, and guide lines are not implemented yet.

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

The models use Unity primitives and an original cone mesh; icons and material colors were authored for this project. No external artwork was added.

See [M1 validation](Docs/M1Validation.md) and [M2 validation](Docs/M2Validation.md) for performed checks and limitations. No desktop build or recording is included yet. Desktop executable and continuous native-window resize acceptance are deferred until the necessary features are complete.
