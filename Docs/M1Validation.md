# M1 validation

歷史提交參照：本文件保留原 SHA 與驗證當時的敘述；改寫後提交請查閱 [Git 歷史遷移紀錄](GitHistoryMigration.md)。測試 job ID 不是提交 SHA，保持原值。舊測試與審查結果仍對應原提交，不代表已在新 SHA 重跑。

Environment: Unity 6000.3.25f1, Windows Editor, URP 17.3.0, Input System 1.20.0. Date: 2026-11-05.

## Implemented

- Main scene under `Assets/SurfaceForge/Scenes/`, with one 80 × 64 m Terrain and a saved 257 × 257 heightmap.
- Shared TerrainData for rendering and TerrainCollider, on the dedicated Surface layer. Sculpted high and low areas with smooth transitions; sampled relief approximately 10.94 m.
- Directional lighting, URP terrain material, original solid-color terrain texture, and an orthographic camera that reserves the sidebar area and fits the terrain bounds.
- UGUI sidebar, initially collapsed catalog, clipped vertical ScrollRect, layout group, Canvas Scaler (1920 × 1080, match 0.5), and InputSystemUIInputModule.
- Main enabled as the startup scene in the shared scene list. Desktop settings: windowed, 1280 × 720, resizable.

## Performed checks

- Clean compilation and Play Mode entry.
- Temporary Editor-driven physics checks: 81 interior downward rays hit TerrainCollider; four rays immediately outside the four boundaries miss the Surface mask. The Terrain and TerrainCollider use the same saved asset.
- During a single Play session, changed the actual Game View resolution to 1280 × 720, 1920 × 1080, 1024 × 768, 2560 × 1080, and 640 × 480. Checked toggle screen bounds, UI raycast reachability, expand/collapse via UGUI pointer events, and positive scroll viewport height. Inspected captured frames for terrain bounds and sidebar layout.
- Direct desktop mouse clicks in the 1280 × 720 Game View expanded and collapsed the catalog through Input System.
- A virtual mouse experiment did not deliver clicks through the Editor and caused a follow-on exception in the temporary validator. It was removed; it is not counted as a passing input check or a product failure.

## Self review

Reviewed scene references, layers, collision data, startup scene settings, UI anchors, input module, listener lifetime, camera framing, and scope. Fixed the content layout width after screenshot inspection showed excessive text wrapping. Added a dedicated LFS rule for binary TerrainData so the generic .asset YAML line-ending rule cannot alter it; verified the staged LFS SHA-256 matches the source bytes. Temporary authoring/validation scripts and screenshots are excluded from the change. No third-party artwork was introduced.

## Limits and follow-up

- No standalone desktop build or executable test was performed for M1. Native window dragging/resizing and continuous resize behavior remain to be tested in a desktop build; discrete runtime Game View size changes were tested.
- The list contains only an empty-state message. Populated list scrolling, prop interactions, and placement lifecycle belong to subsequent milestones.
- Physical mouse input was checked at 1280 × 720; the other resolutions used UGUI event dispatch and raycast/bounds checks.

## Manual reproduction

1. Open Main and enter Play Mode; confirm the list is collapsed.
2. Click the catalog button twice and verify expansion then collapse.
3. Switch Game View through the resolutions above. Check the full terrain boundary and sidebar remain visible and the toggle remains reachable.
4. Inspect the Terrain and TerrainCollider references and Surface layer; use downward physics rays inside and beyond the terrain bounds to repeat the collision check.
5. Build Windows x64 and test native window resizing when performing desktop acceptance.
