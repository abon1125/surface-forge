# S2 左球面板與 Tab 開關驗證紀錄

日期：2026-11-06（Asia/Taipei）。基準：重新 fetch 的 `c26529b`（PR #12）；分支 `codex/vr-simulation-s2`。功能提交 SHA 以本文件所屬 Git 提交為準。S0、S1 原始 SHA、job 與人工驗收事實維持原紀錄，不視為此次重跑。

## 開始狀態與實作

- 已明確讀取 AGENT.md、DevelopmentPlan、VRSimulationDevelopment、S0Validation、S1Validation、README 及 Unity Skills 協定與 Script／Scene／Editor／Test／UI 模組。沒有既存 TaskCheckpoint。main 與遠端相同，歷史包含 PR #11、#12 合併；執行快轉檢查後建立新 S2 分支，沒有改寫歷史。
- Unity 6000.3.25f1、Windows Editor；registry 綁定專案原路徑，instance `SurfaceForge_409B7451`、8090、bypass/full，所有 Editor 呼叫指定 instance。沿用 URP、Input System、UGUI，未改套件。
- 開始時 VRSimulation 為 dirty；變更日誌顯示 Camera 新增 UniversalAdditionalCameraData、Clear Flags 由 Solid Color 改為 Skybox。使用者明確選擇「保留並納入 S2 場景」，因此與面板一同由 Editor 儲存。
- MenuAnchor 位於 LeftController 下，SimulationPropMenu 常駐錨點，PropMenu 為可收合的 World Space Canvas。Tab 使用按鍵持續狀態偵測新按下；初始、停用及重啟收合，中斷後先放開 Tab 才接受新按下。Esc 關閉，沒有建立拖曳狀態或取消系統，也不操作 Cursor。
- 面板初值 380 × 380、0.001 m/pixel（0.38 m 方形），相對左球偏移 `(0, 0.29, 0.15)` m，朝向指定 Camera。抵銷左球繼承縮放，保留 S1 球偏移、球尺寸、速度與觀看參數。Inspector 可調整尺寸、偏移、縮放與朝向。
- 初次離屏構圖使用上方 0.17 m，左球遮住底部提示；實際檢視後上移 0.12 m，再擷取確認提示及四個項目完整可見。面板保持左側，中央觀看區域可見；這是代理構圖判讀，非使用者舒適度驗收。
- SimulationPropCatalog 以四個場景內固定項目重用 PropCatalogItemView.Bind 純顯示，明確參照原 Cube／Sphere／Cylinder／Cone PropDefinition。沒有複製或修改共用資料，不使用 PropCatalogView、PropItemDragHandler 或 PlacementController。四項同時顯示，本階段無需 ScrollRect。
- Canvas 無 GraphicRaycaster，CanvasGroup.interactable／blocksRaycasts=false，所有 Graphic.raycastTarget=false。保留既有單一 EventSystem，不提供普通螢幕滑鼠點選、拖曳或滾輪替代操作。
- 場景建立、調參、儲存、重載及 GUID 驗證均透過暫時 Editor menu 工具呼叫 Unity API；沒有手改 YAML。工具及 meta 已經 AssetDatabase.DeleteAsset 移除，影像與工具證據留於忽略版控的 Temp/S2Evidence。

## 本次真正執行的驗證

| 項目 | 結果／證據 |
| --- | --- |
| 編譯 | 正式腳本 `b253952c`、建立工具 `5e4239f2`、測試 `a86eee64`、擷取工具 `96f5a914`、最終參數與追加測試 `69b9277f`、清理工具 `c19375c0` 均 completed，errorCount=0 |
| 工具移除後 | 最後編譯 succeeded=true、errorCount=0（2026-11-06T06:12:53.2428986Z）；`9669cda1` 獨立 Play Mode 觀察 5 秒後自動退出，healthy=true、errorCount=0。此結果只代表該觀察區間，不宣稱全程 Console 沒有工具診斷 |
| 首輪工具問題 | `8e8adec0` 於重接測試回呼時遇 Library/UnitySkills/batch_state.json 分享衝突而 failed，totalTests=0；不計為通過。確認 Editor 已停止後重試，未清除歷史紀錄 |
| S2 測試 | `6ec1ff69` 2/2 通過（34 秒）；加入右鍵觀看同時切換後 `5648db7a` 2/2 通過（31 秒），failed／skipped／inconclusive 均 0。篩選 SurfaceForge.Tests.SimulationPropMenuTests，每項真正進出 Play Mode |
| 輸入與生命週期 | 初始收合、Tab 開啟／關閉、按住 12 個影格不反覆切換、12 次放開再按、Esc 關閉、失焦／回焦仍按住不切換、元件停用立即收合且重啟按住不開啟、暫停回呼後不誤切換、場景實際重新載入後收合 |
| 四種資料及重複項目 | ID 為 cube／sphere／cylinder／cone；名稱與 Image.sprite 符合各自 Definition，prefab 非空。反覆切換前後四個項目 InstanceID 一致；沒有桌面拖曳元件、GraphicRaycaster 或可接收射線的 Graphic |
| 跟隨與朝向 | 角色位置變更、水平 123°、俯仰 -75°／0°／75°，正式 LateUpdate 後符合左球公尺偏移、朝向觀看者，世界縮放為 0.001；參照未從 Main 搜尋 |
| 右鍵觀看與 Tab | 測試 Mouse 進入正式 Update 右鍵觀看，Tab 關閉／開啟均保持 IsLooking、Cursor Locked 且不可見；右鍵 delta 轉動後面板仍開啟，放開右鍵恢復游標 |
| S1 回歸 | 本次重新執行既有 SimulationPlayerTests：`ab7d1835` 2/2 通過（43 秒），沒有略過。涵蓋移動、碰撞、合成坡面／重力／邊界、WASD、俯仰、右鍵與游標生命週期；不更動 S1Validation 的歷史 job |
| 獨立 Play Mode 與影像 | `7799eded`、`aea5d88e` 獨立進入 VRSimulation 並退出；最終上移後重新擷取及實際檢視 1280×720、1024×768、1720×720 Camera.Render 離屏影像。四項及提示完整、左球不遮擋提示；沒有載入 Main 或 SceneMenu |
| 元件及地形 | S2 執行期測試有效 Camera／AudioListener 各 1，EventSystem 1；Terrain／TerrainCollider 指向相同 VR TerrainData，Surface layer。VR TerrainData GUID 仍為 84f1dc098353f554995b824d4460383f |
| 重載與資產 | 從磁碟重新 OpenScene 後 isDirty=false，遺失腳本 0；27 個不同 GUID 均解析成功（含 2 個 Unity 內建 GUID）。正式新增 2 個腳本及 1 個測試皆有 Editor 產生的 meta，無暫時工具參照 |
| 保護範圍 | 開始前後 SHA-256 比對 Main.unity、桌面及 VR TerrainData、EditorBuildSettings.asset 與下列五個既有檔案相同。未修改桌面腳本、共享道具／圖示／prefab／材質／TerrainLayer／貼圖、套件及 LFS 規則 |

保留且排除 S2 提交的原有變更：DefaultVolumeProfile.asset、PC_RPAsset.asset、UniversalRenderPipelineGlobalSettings.asset、ProjectSettings/ProjectSettings.asset、未追蹤的 ProjectSettings/SceneTemplateSettings.json（前三者位於 Assets/Settings）。

## 限制與待人工確認

- 輸入測試採 Input System 測試 Keyboard／Mouse；焦點與暫停使用生命週期回呼，沒有實際操作原生 Tab／Esc、OS Alt-Tab 或原生右鍵連續操作。不將上述技術測試稱為人工驗收。
- 三比例為離屏 Camera.Render，沒有變更原生 Game View 比例，沒有驗證執行中原生視窗連續縮放。俯仰與轉向的相對姿態由數值測試驗證，沒有逐角度人工截圖判讀。
- 使用者尚未驗收 S2 面板可讀性、構圖、觀看與操作舒適度。請實際開啟 VRSimulation，確認 Tab／Esc、移動與右鍵俯仰時的閱讀、按住 Tab、焦點往返及停用／重啟。S1 既有使用者確認不延伸為 S2 通過。
- 不含 S3–S5 射線、RayOrigin、UI 路由、拖曳、預覽、指引、選取刪除或場景選單；沒有 Windows 建置、XR 或桌面互動完整回歸。未推送、建立 PR 或合併。

## DOTween 開合動畫補記（2026-11-06）

基準為初次 S2 提交 `954d0fd68fa085a27c355ec0a719ecffb3598cd4`。使用者選擇「從左球展開」、自行導入 DOTween 後授權實行。本節追加實際動畫開發結果，不將上節或 S0／S1 歷史檢查改寫為本次重跑。

- 使用者導入 DOTween 免費版 1.3.045、Modules 及 DOTweenSettings；保留完整原始套件、readme、著作權與 meta，依 [官方授權](https://dotween.demigiant.com/license.php)使用，不修改第三方來源。Unity、URP、Input System 與 UGUI 版本未變，沒有 XR 或 Pro。
- 正式元件以一個 progress Tween 驅動位移、縮放與透明度，LateUpdate 保留錨點跟隨並套用動畫狀態。預設開啟 0.25 秒 OutCubic、收回 0.18 秒 InCubic、起始比例 15%，Inspector 可調。最終位置、尺寸及 S1 參數不變。IsExpanded 改為目標狀態；收回完成才隱藏 Canvas，快速 Tab 從目前進度反向，Esc 也收回。
- 失焦／應用程式暫停時終止 Tween 並立即對齊目標狀態；元件停用／重啟立即收合、銷毀時清理。動畫使用 unscaled time、SetTarget(this) 及 SetLink(MenuAnchor)，不變更游標、不清除其他系統 Tween。DOTween 自身會有套件持久 driver；專案沒有另加 DontDestroyOnLoad、跨場景單例或全域 KillAll。
- 編譯：暫停前 `db70097d`、`697b7425` 成功；續作追加直接使用 Tween 的測試時 `8ff3034c`／`04c4c042` 出現缺少 DG 組件參照。已在既有測試 asmdef 明確參照 DOTween.dll 與 NUnit，`392cf95b` completed、errorCount=0。移除暫時工具後最後編譯 succeeded=true、errorCount=0（2026-11-06T08:12:21.2597286Z）。保留上述診斷，不宣稱全程 Console 零錯誤。
- 測試工具：`71cde627`（暫停前）及 `7de743d4`（續作）因 batch_state.json 分享衝突而 failed、totalTests=0，不能算通過。續作首輪 `467487ca` 3/3 通過（41 秒）；最終 `60b00ba4` 4/4 通過（75 秒），failed／skipped／inconclusive 均 0。
- 最終測試涵蓋原 Tab 邊緣、按住／反覆切換、Esc、資料與項目數量、回呼焦點、元件停用／重啟、場景重載、右鍵與游標；新增以 Tween.Pause／Goto 控制中間時間，確認反向不跳進度、終點透明度及位置、關閉後停用且 Tween 參照清空。另在移動、水平轉向與俯仰時檢查動畫中位置／縮放／透明度，實際卸載 VR 場景證明自有 Tween 停止、其他 Tween 仍有效，游標恢復。
- 動畫導入後 S1 回歸：`856cf0a1` 首輪因相同工具分享衝突 failed、totalTests=0；確認 Editor 停止後重試 `b4ad344f`，SimulationPlayerTests 2/2 通過（58 秒），沒有略過。此為本次動畫版本的移動、觀看及游標回歸，不沿用初次 S2 的 ab7d1835。
- 場景設定由暫時 Editor API 工具儲存並重新開啟讀回：openDuration=0.25、closeDuration=0.18、collapsedScale=0.15、openEase=9（OutCubic）、closeEase=8（InCubic），初始 CanvasGroup.alpha=0。工具與 meta 已用 AssetDatabase 移除，未手改 YAML。
- 最終獨立 Play Mode 觀察 `677731f0` completed，5 秒、healthy=true、errorCount=0，已退出。DOTween 診斷確認免費版安裝、Pro 未安裝、設定資產可解析及 7 個模組可見。新增依賴沒有缺少 meta；7 個二進位 LFS 指標與磁碟 SHA-256 相符，既有 TerrainData LFS 規則未變。
- 影像：獨立 Play Mode `b38467ec`，以 DOTween.Goto 控制開啟 0／40／80／120／180／250 ms、收回 0／40／80／120／180 ms，使用 Camera.Render 離屏擷取；實際檢視代表性中間階段及完成畫面，面板從左球附近移出、放大並淡入，收回反向，最終構圖與原 S2 相同。另檢視 1280×720、1024×768、1720×720 最終影像，四項及提示完整、中央視野保留。產物位於忽略版控的 Temp/S2AnimationEvidence，不是原生 Game View 比例、連續縮放或動畫實況錄影。
- 保護範圍：續作前後 Main、桌面與 VR TerrainData、EditorBuildSettings、原有三個渲染設定及 SceneTemplateSettings 雜湊相同。ProjectSettings.asset 額外出現各平台 DOTWEEN 符號；已核對 Unity Skills 的 DOTweenPresenceDetector 會自動寫入符號，原有 batching 調整保留，整份檔案繼續排除提交。沒有重設或清理原有五個未提交檔案，也未將它們納入動畫提交。
- 尚未人工驗收動畫速度、可讀性與舒適度；未做原生鍵鼠／OS 焦點或視窗連續縮放驗收。仍僅 S2，未推送、建立 PR、開始 S3–S5、建置或 XR。
