# M3 validation

歷史提交參照：本文件保留原 SHA 與驗證當時的敘述；改寫後提交請查閱 [Git 歷史遷移紀錄](GitHistoryMigration.md)。測試 job ID 不是提交 SHA，保持原值。舊測試與審查結果仍對應原提交，不代表已在新 SHA 重跑。

## User acceptance update — 2026-11-06

The user confirmed desktop manual testing in Unity Editor Play Mode with no issues found. Editor Play Mode manual acceptance is recorded as complete based on that report; individual gestures, resolutions and repetition counts were not separately reported. The agent's earlier unsuccessful drag-injection attempts remain historical observations and do not override the user's acceptance. Earlier pending-acceptance statements below describe the status before this update. Windows executable and continuous native-window resize acceptance remain scheduled for I2.

Environment: Unity 6000.3.25f1 Windows Editor, URP 17.3.0, Input System 1.20.0. Date: 2026-11-05. Baseline: main `8ae340d`, including merged M2 `359bb93` (PR #3). No package changes.

## Delivered behavior

- `PropItemDragHandler` routes a threshold-qualified left-button horizontal gesture to placement and a vertical gesture to the existing ScrollRect. The initial direction owns the gesture until release. Wheel scrolling still bubbles to ScrollRect. The ScrollRect is never disabled; horizontal gestures stop its inertia, and vertical end/disable/focus-loss paths end its drag. Rows are not moved or reordered.
- `PlacementController` owns one preview, source and definition. Preview and placed instances have separate lifetimes. Release checks current coordinates, screen bounds, every GraphicRaycaster UI result and a fresh surface query before creating exactly one object. Escape, missing/released mouse, leaving screen bounds, focus/pause loss and controller/source disable cancel. LateUpdate handles a release that UI did not deliver after UI has had its Update opportunity.
- `SurfacePlacementQuery` uses an explicitly assigned camera, TerrainCollider, Surface mask, 500 m maximum ray distance and 50 m invalid ray depth. It checks the collider's layer against the mask and raycasts that collider directly. Props cannot occlude it. The shared pose calculation places the root at the hit point and rotates local up to the hit normal, with no world-Y correction.
- Preview colliders are disabled and the hierarchy uses Ignore Raycast. Two authored URP Unlit materials distinguish green valid and red invalid states. Shared prefab/material assets are untouched; no runtime material clones are created. UI hides the preview; a miss uses the current pointer ray at fixed depth. Placed props use Default, keep model colliders and carry a `PlacedProp.Definition` reference. No Rigidbody, overlap rejection, persistence, selection, deletion or guides were added.
- Main and the reusable row prefab are wired through Unity Editor APIs. M1 terrain/camera and M2 prop/data/icon/model assets are retained. The sidebar caption explains horizontal dragging.

## Performed validation

### Automated tests

The permanent fixture is listed under EditMode and explicitly enters Play Mode in a fresh isolated scene for runtime callbacks. It covers anchor/normal pose, surface invalidation at release, exactly-once commit and data/collider retention, preview/commit pose equivalence and shared preview material, UI becoming raycastable at release, repeated controller/source-disable and focus-loss cancellation, release coordinates outside the surface, and cleanup on a following frame without an EndDrag event. Initial implementation run: 7/7 passed (job `6cac8105`). After the cloud-review fix and fixture improvements, the complete final run passed 8/8, 0 failed or skipped (job `7dbbc01f`). The added regression routes right-button drag/end events with the same pointer ID during a left-button drag and confirms they cannot move or commit it.

Earlier test iterations exposed fixture issues: existing Main UI contaminated EditMode queries; EditMode did not exercise runtime disable callbacks; the test source lacked its controller binding; newly created/re-enabled UI had not received rendered depth for GraphicRaycaster. The fixture now isolates the scene, enters Play Mode, binds its source and waits for UI rendering. A later full rerun exposed background PlayerLoop timing in the two asynchronous cases; the final fixture uses an isolated neutral Input System mouse, CanvasGroup UI blocking, temporary runInBackground (restored in teardown), and explicit Time.frameCount advancement with QueuePlayerLoopUpdate. The intermediate 6/8 results are superseded by the complete 8/8 run. Earlier failures are not counted as passing runs.

### Main-scene UGUI event simulation and Editor inspection

- Actual Game View sizes were set to 1280 x 720, 1024 x 768, 2560 x 1080, 640 x 480 and 1920 x 1080 during Play Mode. At each size, dispatched 100 threshold-initialized UGUI begin/drag/end gestures across all four definitions: 50 successful releases and 50 cancellations (25 over a row, 25 over background). All cancellations first visited a valid point. Each batch increased placed count by exactly 50: 0 -> 50 -> 100 -> 150 -> 200 -> 250. No preview remained after each batch; subsequent-frame snapshots found zero preview objects and exactly two loaded preview materials.
- At each size, added 16 temporary row copies and dispatched vertical UGUI drag gestures. Content moved by approximately 150, 162.38, 86.60, 259.81 and 100 UI units respectively. No placement preview started. Removed the copies and confirmed four production rows. This is event simulation, not physical wheel or held-mouse scrolling.
- Visually inspected the populated Main interface at the five sizes. The wide Game View initially retained an excessive Editor zoom, so reduced the Editor preview scale to show the whole image before inspection. This changes Editor viewing zoom, not product layout. No temporary props or rows were saved into Main.
- Final compilation after removing temporary tools had no errors; Main entered and exited Play Mode, with no new captured Console errors from this stage. Scene/data/prefab/material/meta references were checked. Temporary author/probe tools used prefixed Unity Skills methods, schema discovery and dry-run before use; they are removed from delivery. A temporary snapshot serializer initially rejected raw Unity Vector values; it was corrected to numeric arrays and is not counted as product behavior.

### Actual desktop input

- Mouse click expanded the catalog in the 1280 x 720 Game View. A normal row click did not increase placed count.
- Several desktop drag-injection attempts from the Cube row at 1280 x 720 and 2560 x 1080 did not produce a placed object. Temporary diagnostics observed no placement Begin call from those attempts; the same row/controller references worked through UGUI dispatch. The cause of the missing input event was not established. These attempts are **not passing drag tests**, and no claim is made that all four shapes were physically dragged successfully.
- The available desktop API exposes an atomic drag, without a supported held-button/move/release sequence across tool calls. Escape during a held physical drag, switching focus mid-drag and leaving the window before releasing were not reliably exercised. Callback-based tests do not replace these input checks.

## Cloud review

The first cloud review completed for `41a3bb0` and raised one valid P2: right-button drag/end callbacks share the mouse pointer ID and could finish a left-button gesture. Added left-button filtering to initialize/drag/end and the regression above. Latest-head re-review is tracked in PR #4; pending review is not a pass.

## Self review and remaining acceptance

Reviewed data references, colliders/layers, direct TerrainCollider rays, anchor/normal pose, fresh release evaluation, UI blocking, gesture ownership, event ordering, preview materials and destruction, focus/disable/pause cleanup and restart. Fixed release fallback ordering by handling an undelivered button release in LateUpdate, after UGUI Update, and reset source gesture ownership on controller cancellation. Added focus/pause cleanup for vertical scrolling and per-frame preview refresh. The two preview materials are assets with no per-drag allocation or destruction responsibility.

Physical dragging of every prop at high/low/slope/boundary locations, continuous preview appearance, held-input Escape/focus interruption, outside-window release/return, and physical scrolling remain user manual acceptance gaps. Agent tests and cloud review do not constitute user acceptance. Desktop build/executable and continuous native-window resize acceptance remain deferred to completion of the necessary features.

## Manual acceptance

1. Open Main, enter Play Mode and click PROPS. Click a row normally: no prop appears. Drag each of Cube, Sphere, Cylinder and Cone left out of its row, then move over terrain high/low/slope/inside-edge locations: a single green preview follows; release adds exactly one matching prop and removes the preview. All four rows remain. Compare preview and placed pose; root anchor touches the hit, local up follows the slope.
2. Start another drag, visit a valid point, then release over a row, blank sidebar, toggle, background or beyond the terrain edge: count must not increase. Preview is hidden on UI and red at a no-hit ray position; no old valid position may be reused. Drag over already placed props: terrain remains queryable.
3. While dragging, press Escape; repeat with focus switching and with the controller or source/list disabled. Each must clear the preview. Re-enable and start a fresh valid drag: it works. Leave the window with the button held, release outside and return: nothing may be placed. Repeat success/cancel operations at least 50 times and inspect Console and object counts.
4. Try the five Game View sizes above. Drag horizontally to place; use vertical gestures or wheel to scroll. For overflow, duplicate rows only during Play Mode, verify vertical drag scrolls without a preview, then exit Play Mode to discard temporary changes. Save no runtime copies.
5. Exit Play Mode. Report any missing-input or unexpected placement with the gesture, resolution and Console details. Do not treat M2's merge or M3's automated review as manual acceptance.

Status: awaiting user manual acceptance. No automatic merge.
