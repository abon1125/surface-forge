# S3 右球射線與 UI 路由驗證紀錄

日期：2026-11-06（Asia/Taipei）。Unity 6000.3.25f1／Windows Editor，維持現有 URP、Input System、UGUI 與 DOTween 免費版 1.3.045，沒有 XR。基準為重新 fetch 確認的 origin/main `dd90baf9dbfc153d32dd993daf6338c5615833ec`（PR #13 合併），本機 main 同步後建立 `codex/vr-simulation-s3`。本文件隨功能提交，提交 SHA 以 Git 歷史為準。

## 實作與責任

- RightController 新增 RayOrigin，位置在右球前表面；SimulationWorldRaycaster 明確參照 Camera、RayOrigin、角色根節點與左手 Canvas，100 m 距離、排除 Ignore Raycast。Physics 查詢另排除角色階層，控制器與射線視覺不會自命中。
- Camera＋滑鼠位置只取得參考點；實際操作一律從 RayOrigin 朝參考點重新查詢。UI 的世界平面交點、矩形與場景碰撞距離決定命中；投影只用於既有 UGUI mask／CanvasGroup filter，沒有普通螢幕點選的替代路徑。場景較近或同距離時優先阻擋，面板空白背景也阻擋。
- 專用 SimulationRay 材質、0.003 m LineRenderer 與直徑 0.016 m 命中球顯示實際結果；未命中使用有限終點並隱藏標記。沒有地形放置指引或落點標記。
- VR 原有 InputSystemUIInputModule 在場景停用；SimulationRayInputModule 為單一 EventSystem 的唯一有效模組，不派送普通滑鼠、navigation、submit 或拖曳。LateUpdate 在 S1 Update 與 S2 錨點跟隨後更新射線與 UI。
- SimulationPropItem 提供 hover 藍色、press 橘色、click 綠色閃示及 CLICK: ID。點選只保留正確 PropDefinition 與次數，不建立道具、預覽或放置流程。
- SimulationPropCatalog.Awake 不再強制關閉 raycastTarget。場景僅項目背景、viewport 與面板背景接收查詢；圖示與文字不接收，沒有 GraphicRaycaster、PropCatalogView 或 PropItemDragHandler。
- CanvasGroup 由 SimulationPropMenu 管理：展開完成且有效才 IsInteractive；收合要求立即停止輸入，可見的動畫背景仍阻擋。S2 原 wasPressedThisFrame、快速反向、Tab release 防護與 Tween 自有清理保持。
- viewport／content 均 332×288 px，四列各 72 px、項目高 66 px。ScrollRect 垂直、Clamped、無慣性，24 px／步，滾輪 120 units／步；正常四項無溢出。僅實際命中可見且未遮擋的 viewport 才派送滾輪，沒有桌面拖曳手勢。
- 右鍵觀看、失焦、暫停、離開有效 Game View、停用、收合及卸載取消 hover／press，不合成 click。右鍵觀看期間停止瞄準與 UI 派送，清除射線命中顯示；不接管 S1 游標。中斷後需先放開左鍵。Editor 另檢查焦點與 mouseOverWindow 均為 GameView。

## 已執行的技術驗證

| 項目 | 真正執行的結果 |
| --- | --- |
| 正式／工具編譯 | 初次工具 `b764c055` 缺少 Newtonsoft 組件；改為無相依文字輸出後 `3a4b4174` completed、errorCount=0。測試 `02fed4ac` 缺少 DOTween 擴充方法 using，修正後 `ab76ed1b` completed、errorCount=0。後續 `fce299bd`、`a1f2593d` 皆 completed、errorCount=0。不宣稱全程沒有錯誤 |
| 首輪執行問題 | `c14c7a83` job 因 batch_state.json 分享衝突 failed、totalTests=0；Editor 已停止，原始 TestResults.xml 顯示 4 項失敗。主要為 UGUI IndexedSet.GetEnumerator 不實作；改為索引走訪。沒有清除歷史診斷或刪除工具狀態檔 |
| 首輪修正測試 | `96b195bd` completed、3/4 通過；剩餘動畫 gate 測試在快速反向時取樣已到終點。改為先對齊收合再控制 Tween 進度，並補同影格放開再按住案例 |
| 回接衝突與原始結果 | `1f9b0a7e` job 因同一分享衝突 failed、totalTests=0，不算 job 通過。確認 Editor 非 Play／非編譯後，保存原始 TestResults.xml，四項均 Passed；再重試取得下列可回接結果 |
| S3 最終測試 | `9b2c87e7` completed，4/4 通過（69 秒），failed／skipped／inconclusive 均 0；每項真正載入 VRSimulation、進出 Play Mode |
| S2 回歸 | `6003bd06` completed，SimulationPropMenuTests 5/5 通過（83 秒），無略過；同影格 Tab／Esc、按住不重播、資料與項目不重建、快速動畫反向、跟隨／Tween 卸載、自有清理及右鍵游標皆隨原測試重跑。只把 S2「所有 Graphic 不接收」斷言更新為 S3 四個項目存在，歷史 S2Validation 不改寫 |
| S1 回歸 | `b629d570` completed，SimulationPlayerTests 2/2 通過（43 秒），無略過；移動、碰撞、合成坡面／重力／邊界、右鍵／游標生命週期，S1 參數未修改 |
| 四項與事件次數 | Input System 測試 Mouse 經正式 LateUpdate 路由：四項 hover／press／release／click，ID 為 cube／sphere／cylinder／cone，Definition 為原 Bind 物件；按住不重複 click、同影格按放、放開再按住、移出到另一項放開不誤點。沒有 PlacedProp 或桌面拖曳元件 |
| 世界射線與遮擋 | 直接 Query 配合正式輸入路由：Camera 與 RayOrigin 起點相距超過 0.2 m；執行期小型 BoxCollider 只擋右球射線、Camera 參考仍命中項目，實際不 hover、標記位於阻擋點。空白背景命中 Canvas；朝天空無命中，舊標記隱藏、LineRenderer 終點符合有限距離 |
| 滾輪 | 僅執行期把 viewport 高度縮至 144 px，原四項溢出時能捲動。清單外標題、前景遮擋、收合、展開動畫中與右鍵觀看均不捲動；動畫中按住左鍵到完成不能取得新 press，放開再按才可操作。未變更共享資料或加入正式假道具 |
| 中斷與跟隨 | 正式 Mouse 輸入按住後，焦點／暫停生命週期回呼、元件實際停用重啟、滑鼠測試座標移出視窗、面板關閉、右鍵觀看均清理 press，仍按住不重取，放開不 click。移動、水平 40°、俯仰 -30° 後，起點等於 RayOrigin，仍命中項目；實際卸載與重載場景後收合且無 press |
| 元件及序列化 | 建置後儲存、磁碟重載讀回：有效 Camera／AudioListener 各 1、EventSystem 1、遺失腳本 0、遺失外部 GUID 0。S3 測試亦確認只有一個有效 BaseInputModule。DOTween 套件 driver 不計為額外專案輸入系統 |
| 地形與保護範圍 | Terrain／TerrainCollider 仍同指 VR TerrainData（GUID 84f1dc098353f554995b824d4460383f）、Surface layer=8。Main、兩份 TerrainData、EditorBuildSettings 及五個原有未提交檔案均以前後 SHA-256 比對；未改桌面腳本、共享 PropDefinition／圖示／prefab、TerrainLayer／貼圖、套件或 LFS 規則 |

測試裝置為 Input System 新增的 Mouse／Keyboard；Game View 有原生焦點與指標停留，但測試座標、按鍵與滾輪由測試裝置提供。Open helper 直接呼叫 SetExpanded 與焦點回呼對齊 Tween 終點；動畫 gate 使用 Tween.Pause／Goto，不能稱為原生 Tab 操作。焦點／暫停回呼不同於 OS Alt-Tab；合成座標移出也不同於實際原生滑鼠離開 Game View。S1／S2 歷史 job 與使用者整體確認不算此次重跑。

工具移除後，只清理兩個測試檔 EOF 空白：編譯 `78dd2118` 與最終 `53486225` 均 completed、success=true、errorCount=0（最終 2026-11-06T09:19:16.5382142Z）；另讀回 SimulationRayInputModule 編譯診斷為 errorCount=0。最後場景檢查仍 isDirty=false、遺失 GUID／腳本為 0。暫時工具與 meta 均已由 AssetDatabase 移除，功能資產與測試的 meta 成對。最終獨立 Play Mode 觀察 `bf6934d6` completed，5.001 秒、healthy=true、errorCount=0，已退出；此為該區間結果，不將早期修正前診斷清除或稱為全程零錯誤。最後讀取持續保留的 Error／Exception 群組，最新為 08:57:30Z 的已修正 IndexedSet 錯誤，沒有該時間後的新 Error／Exception 群組。

## 原生 Game View 與視覺檢查

- 使用 computer-use 的 Windows 原生 UI 輸入，實際點擊 Play 並獨立啟動 VRSimulation，再點擊 Stop 退出。面板開啟使用 `component_invoke_method` 直接呼叫 SetExpanded(true)，job `aa0d444d` completed；不是原生 Tab 通過。工具注入 Tab 的嘗試未觀察到面板展開，讀回 IsExpanded=false；注入路徑原因未定，不推論為硬體操作通過或既有 S2 退化，保留使用者後續實際鍵盤確認。
- 在原生 4K UHD（3840×2160、16:9，Game View 縮放 0.19×）逐一以工具注入原生滑鼠點選四列，實際畫面顯示 CLICK: cube／sphere／cylinder／cone，hover 與右球射線終點一致。這是代理 UI 自動化檢查，非使用者精準度／舒適度驗收，也不將原生短暫 press 色未擷取到的影格宣稱為逐幀視覺驗證。
- 透過 Game View 原生解析度選單加入並切換 1024×768（4:3，顯示 0.71×）與 1720×720（寬螢幕，顯示 0.42×）；實際檢視四項、標題與提示完整且雙球未遮擋面板。4:3 點選 Cube、寬螢幕點選 Sphere 後對應 CLICK ID 正確。切換發生於同一次執行中的 Play Mode，不是 Camera.Render 離屏擷取；這是離散解析度切換，沒有做原生視窗連續拖曳縮放。
- 操作解析度 popup 時射線與 hover 清除，重新取得 Game View 焦點後可再瞄準。未以此推定「按住跨視窗／OS Alt-Tab」通過。結束前恢復原 4K UHD 選項；兩個測試解析度選項留在本機 Editor 偏好，未加入專案設定。畫面證據在此次工具回傳中，沒有製作錄影。

## 限制與人工確認

S3 尚未獲使用者對射線精準度、閱讀與操作舒適度的人工確認。原生焦點往返、按住跨視窗、停用重啟及連續縮放仍需使用者實際試玩。沒有 Windows 建置、Editor 外驗收、XR 或錄影交付；沒有開始 S4–S5、共用放置核心重構或場景選單。本次僅建立本機提交，不推送、建立 PR 或合併。

保留且排除提交的原有檔案：Assets/Settings/DefaultVolumeProfile.asset、PC_RPAsset.asset、UniversalRenderPipelineGlobalSettings.asset、ProjectSettings/ProjectSettings.asset 與未追蹤的 ProjectSettings/SceneTemplateSettings.json。原有 batching 與各平台 DOTWEEN 符號保留，未 reset、clean 或覆寫。
