# S5 射線選取與刪除驗證紀錄

> 2026-11-07 QA 現況：PR #18（I1）、#19（設定整理）已合併；S5／I1 的使用者整體正常回報保留原適用版本，不視為本次重驗。原「六檔排除」為歷史限制，已結束；舊 TaskCheckpoint 不再作為現況來源。本次 Unity 內測試由使用者執行，新增操作說明及選單高度的驗證見 [QABeforeI2.md](QABeforeI2.md)。設定整理後的重新開啟、畫面及儲存穩定性仍待確認。

> 2026-11-07 操作與驗證規則釐清：本文將 Unity 內自動測試交由使用者執行的舊限制，已由 [AGENT.md](../AGENT.md#驗證與完成定義) 取代。代理可透過 Unity Skills Editor API 或命令列執行編譯及 Editor／EditMode／PlayMode 自動測試，包括測試所需的進出 Play Mode；禁止使用 Codex Computer Use 操作 Unity 圖形介面。下文保留當時測試未執行及使用者 Test Runner 操作步驟的歷史紀錄，不代表目前禁止代理執行，也不改列通過。使用者親自點擊、拖曳、移動及畫面、手感與功能的人工驗收仍須另行回報；未回報項目仍為「待使用者驗證」，自動測試通過不能代替人工驗收。

日期：2026-11-06–07（Asia/Taipei）。Unity 6000.3.25f1／Windows Editor，沿用 URP、Input System、UGUI 與 DOTween 1.3.045，未新增 XR 或升級套件。

> 最新狀態：本文末段「水平繞行選單」取代先前定位規則；本次待使用者驗證。歷史通過結果不代表新版本通過。

> 2026-11-07 顯示調整：使用者要求近距離／視野邊緣不要自動隱藏。下列初次交付內容以 d3147cd 為歷史基準，其隱藏規則由文件末段的顯示調整取代；歷史測試與 SHA／job 保留。

## 起始狀態與範圍

- 已讀取 AGENT.md、DevelopmentPlan、VRSimulationDevelopment、S0–S4Validation、M4Validation、README、舊 TaskCheckpoint 及 Unity Skills 協定與 Script／Scene／Test 模組。
- 重新 fetch 後 main／origin/main 為 `fdd6325b39b65e012306d8ea1de12f535ba44e92`；GitHub PR #15 metadata 確認 merged=true、merge SHA 相符。main 快轉檢查後建立 `codex/vr-simulation-s5`，沒有沿用 S4 分支或重複推送／審查／合併。
- 初始暫存區空，五個原有設定及未追蹤 TaskCheckpoint 均保留。保護雜湊保存於忽略版控的 `Temp/S5Evidence/protected-hashes.json`，TaskCheckpoint 已更新過時 S4 交接，不納入功能提交。
- Unity Skills registry／health 及 authoring 讀回確認實際路徑 `C:/Users/abon1125/unity-projects/SurfaceForge`，instance `SurfaceForge_409B7451`、port 8090、bypass/full。場景修改由暫時 Editor API 工具完成，未手改場景 YAML。
- 本次只到 S5。沒有 SceneMenu、返回入口、Main 啟動／建置設定變更、Windows 建置、XR、推送、PR 或合併。

## 實作及操作決策

- `PropSelectionState` 共用有效實例、子碰撞器解析、切換／清除及單次刪除。有效目標須啟用、有 Definition、屬已載入場景且根節點為 PlacedProp layer。來源 prefab、預覽與角色不符合規則。目標停用會撤銷核心參照。
- 桌面 `PropSelectionController` 保留 Mouse／onEvent、按下來源、移動門檻、同影格事件順序、延後選取與放置消耗防護；僅將目標及刪除交給共用核心，桌面選單不改。
- VR 的 `SimulationSelectionController` 不讀桌面輸入。唯一 `SimulationRayInputModule` 依序處理 Input System 動態事件，使用事件當時的位置，於 LateUpdate 重新 Aim／Query 真正右球射線。攝影機只提供瞄準參考。
- 優先順序為已擁有拖曳、未被遮擋的 UI、場景。兩份 Canvas 與場景碰撞體統一比較最近距離，同距離場景優先，空白背景及停用按鈕仍阻擋。隱藏 Canvas 不參與查詢。
- 場景點擊：按下與放開命中同一有效目標（或同為空目標），期間不得經過 UI／視窗外或離開按下點超過 6 螢幕像素。移出再返回仍拒絕；按住不重複選取。UI 起始不轉成場景點擊，DELETE 拖動不算點擊。短按／同影格再按依事件順序處理。
- 新清單拖曳清除選取；提交／取消的放開由放置擁有權消耗。刪除清除核心目標、選單及路由暫存後，才停用／銷毀唯一實例，並中止同批剩餘輸入，避免後方物件接管。
- Esc 保留優先取消拖曳；無拖曳時清除選取並收合左手面板。Tab 只切換左手面板、取消未完成點擊，保留已完成的有效選取。S2 短按／反向動畫、S3 滾輪 3／24、S4 拖出門檻／指引／左右鍵放開防護不變。
- 右鍵觀看、失焦、暫停、離開有效 Game View、選取／選單／路由／角色／射線／左手面板停用及卸載清除目標及輸入；中斷後先放開按鍵才接受新手勢。目標外部停用／銷毀清除參照；選單因視野隱藏時取消舊 UI 按下。

世界選單明確指定 Camera，位於包圍盒側邊並朝畫面中央的一側，前移至包圍盒前方，正面朝向觀看者。不使用桌面邊界夾取，不更改道具資產或模型外觀。基準 260×120 UI 單位、0.002 m/pixel（0.52×0.24 m），側邊及前方間距 0.15 m；超過 2 m 隨距離等比放大維持閱讀角度。距離 0.6–15 m、目標及完整面板在視野內才顯示，近處／遠處／邊緣／後方安全隱藏，移回恢復。邊緣或近距離需移動／轉向後操作，精準度及可讀性初值仍待使用者試玩。

## 執行中的驗證追蹤

編譯 jobs `7e61921c`、`15a2ca03`、`e0cd1dce`、`b6911f70`、`e1051b65`、`921c1534`、`3fb282f3`、`8d8479a0` 均 completed、errorCount=0；另自動編譯於 2026-11-06T15:43:43.3857514Z succeeded=true、errorCount=0。後續最終結果另記，不把早期失敗輪次改成通過。

| 批次 | 實際結果與原因 |
| --- | --- |
| `6a243aef` | 0/4；5 m 測試道具未被右球命中 |
| `8bfe9b3f` | 單一診斷 0/1；證實前景 Terrain 比測試道具近，射線正確阻擋。改用較近、略高的測試配置，沒有放寬遮擋 |
| `f7bd7104` | Unity Skills 分享衝突 failed_reconnect、totalTests=0；Editor 已退出，原始 TestResults.xml 保存於 Temp/S5Evidence，實際 0/4，路由 CurrentRay 為空，結束後原生游標在 Game View 外；未保留該輪逐幀焦點值。沒有刪除狀態檔或重複啟動仍在跑的測試 |
| `959e38b3` | 2/4；測試空白點／停用後 Canvas 配置不正確 |
| `5a896dbd` | 1/4；確認上方選單的 DELETE 射線可能先撞到所選道具，改為側邊並前移。近距離移動測試原本碰撞推動角色，改為測試期間暫時關閉測試道具 Collider，再復原 |
| `9e33d427` | 0/5；CurrentRay 為空，與焦點中斷一致；該輪未保留逐幀焦點值。保留正式焦點檢查，重建原生焦點後重跑 |
| `514eccd8` | 4/5；刪除事件、中斷、遮擋、目標失效及重載通過；第二個切換測試實例被新 UI 遮住，移至另一側，保留 UI 優先 |
| `764a2c79` | Unity Skills 分享衝突 failed_reconnect、工具 totalTests=0；等待實際 Runner 結束後新 XML 為 **38/38 Passed，零失敗／略過**，保存於 Temp/S5Evidence/764a2c79-TestResults.xml。結束時 Editor 非 Play／暫停／編譯；不將回接工具 job 稱為 completed |

完整回歸 XML 的每個 test-case 均為 Passed；最外層 duration／start-time 在本機 Test Runner 重建結果後僅反映結果輸出區間，不能當作整批執行耗時。本輪沒有以此宣稱 38 項只花 2 秒。

目前測試涵蓋實際世界 Ray／子 Collider、四種原 prefab 與同種類實例、選取切換／清除、UI／前景遮擋、兩 Canvas 前後交換與空白背景、停用按鈕、同批刪除防穿透、UI 起始移到場景、場景／DELETE 移出再返回、短按／同影格再按、持續按住、失焦／暫停回呼、路由／角色／射線／選取／選單停用重啟、合成座標離開 Game View、右鍵互斥、外部銷毀／停用、放置提交／取消放開消耗、Tab／Esc 與卸載重載。實際通過範圍以最終批次為準。

## 方法與人工待確認

Input System Mouse／Keyboard 為測試裝置；S5／S4 測試暫時停用其他同類裝置並在 teardown 復原。Game View 使用 computer-use 原生聚焦及移入游標，這只是測試環境，不等於原生點選／拖曳或 OS Alt-Tab 驗收。失焦／暫停以生命週期回呼、視窗外以合成座標、部分開面板以直接方法／回呼對齊 Tween 終點；UI 互動仍經正式射線模組。Canvas 前後交換是執行期直接配置，用來驗證 Query，沒有寫回場景。

使用者尚未確認 S5 選取精準度、DELETE 可讀性與操作舒適度。請人工檢查四種道具、多個同種類實例、坡面／遠近／視野邊緣、按住移出／返回、放置後再選取、DELETE 後繼續放置、左右鍵互斥、OS 失焦／回焦、暫停、Game View 往返及連續縮放。S1–S4 的既有使用者整體確認不延伸為 S5 通過。



## 最後清理補強與重驗

完整 38/38 回歸後，桌面刪除改為由共用核心先捕捉／清空目標，再回呼 Interrupt 清除選單及輸入，最後停用／銷毀。VR 在舊按鈕所屬物件隱藏時取消按下，保留完成的選取；新增「按住 DELETE → 目標移到後方 → 回到視野 → 放開不能刪除」案例。編譯 d6f6c203 completed、errorCount=0；8a61463e 為 0/5，CurrentRay 均為空、與焦點中斷一致；未以該輪未記錄的焦點值作確定歸因。加入暫時唯讀焦點診斷並改用持續 API 客戶端後，**9fc965cb completed、5/5 通過，零失敗／略過**；focus.csv 各案例採樣均 focusedWindow=GameView、mouseOverWindow=GameView、Application.isFocused=true，沒有放寬產品防護。桌面最終 d27695be completed，**10/10 通過，零失敗／略過**（388 秒），包含 50 次切換／清除／刪除、UI 消耗、同影格事件與延後選取防護。之後僅清理兩份檔案的 EOF 多餘空行，沒有再修改互動行為。先前 38/38 是這兩項補強前的完整基準，不冒充最終腳本的全套重跑。


## 畫面檢視

- 獨立 Play Mode 進入 caaeec46；首次配置以螢幕地形點準備道具過近，完整世界選單超出視野而隱藏，不當成可讀性通過。退出後僅修改暫時工具，編譯 399adb95／49819570 均 errorCount=0。
- 再次獨立進入 de22fa0d，在執行期以指定 TerrainCollider 的向下射線把四種原 prefab 放到前方約 8 m 的實際地形，直接選取 Sphere；僅執行期調整觀看俯仰取景，未更改 S1 序列化參數。
- 實際檢視 1280×720（16:9）、1024×768（4:3）、1720×720（寬螢幕）Camera.Render 離屏 PNG：左手四列完整、名稱及紅底白字 DELETE 清楚，選單顯示在 Sphere 側邊／前方。其他模型可依距離被選單遮擋，左手面板也會遮住後方模型，沒有要求 UI 永遠置頂穿透互動。
- 讀回 visible=true、Target=Sphere，Camera／AudioListener／EventSystem 各 1。影像及狀態在忽略版控的 Temp/S5Evidence；這是直接方法配置與離屏檢視，不是原生 Game View 比例切換、原生選取／刪除或連續縮放測試，不是使用者可讀性／舒適度驗收。


## 場景與清理核對

- Editor API 儲存 VRSimulation 後從磁碟重新開啟，讀回 missing=0、有效 Camera／AudioListener／EventSystem／BaseInputModule 各 1、目標與選單參照為空、isDirty=false。
- Terrain／TerrainCollider 同指 Assets/SurfaceForge/Terrain/VRSimulationTerrain.asset，Surface layer=8；沒有加入桌面 PropSelectionController 或 GraphicRaycaster。
- 42 個不同場景 GUID 全部解析成功（含內建資源），新增正式腳本／測試都有 Editor 產生的 meta；暫時 S5Authoring 及 Editor 資料夾／meta 已透過 AssetDatabase 刪除。
- 工具移除後編譯 succeeded=true、errorCount=0，時間 2026-11-06T16:30:53.7463585Z。
- 原有五個設定完整保留；Unity 在驗證中自動把兩份 S4 SimulationPreview 材質的隱藏 _Color 同步為 _BaseColor 的 alpha=0.35（另有色彩浮點尾差）。_BaseColor、透明設定與外觀參數未修改；最終已還原這兩份自動寫入並排除提交，不能混為 S5 變更。


## 最終結果

- 最後獨立 Play Mode 6d84647f completed：5.002 秒、healthy=true、errorCount=0，已退出；不是整個開發期間 Console 零歷史診斷，也不是原生鍵鼠人工驗收。
- 正式腳本最後驗證為 S5 9fc965cb 5/5、桌面 d27695be 10/10；完整基準 764a2c79 的原始 XML 為 38/38（回接 job failed_reconnect）。沒有把補強前的完整批次冒充最終全套重跑。
- Main、兩份 TerrainData、建置設定、共享 PropDefinition／prefab／圖示／模型／材質，以及五個原有設定均通過開始／結束 SHA-256 比對；兩份預覽材質自動同步已還原。五個原有設定與未追蹤 TaskCheckpoint 排除功能提交，沒有 reset／clean。
- 14 筆既有 LFS 指標與磁碟內容 SHA-256 全數相符；沒有新增二進位資產、改動 .gitattributes、套件、共享素材或建置清單。
- 只完成 S5，尚待使用者確認選取精準度、世界選單可讀性、舒適度、原生焦點及連續縮放。沒有 Windows 建置、XR、I1／I2、推送、PR 或合併。

最後 staged 檢查後移除兩份 C#／測試的 EOF 多餘空行；編譯 2c131e7e completed、errorCount=0（2026-11-06T16:40:33.4609308Z）。該格式清理後未重跑互動測試；63 個保護檔案雜湊再次全部相同，meta／LFS／staged diff 及空白檢查通過（Unity 場景序列化的空值行尾空白保留）。


## 近距離／視野邊緣持續顯示（2026-11-07）

依使用者回饋修改 SimulationContextMenuView，近距離與畫面邊緣不再因完整面板超出視野而 SetActive(false)。保留物件側邊／前方的優先位置，再在指定 Camera 的投影中，將世界 Canvas 放在至少 1 m 深度、四周各 4% 內距的可視範圍；必要時等比例縮小。面板平面與 Camera 平行，以可驗證的投影尺寸保留完整名稱與 DELETE。

仍然是 World Space Canvas，操作繼續由真正右球射線與所有場景／UI 比較距離，沒有改成螢幕 UI 或穿透置頂。前方但超出畫面的目標，其選單停靠對應邊緣。目標在攝影機後方、超過 15 m、失效或既有中斷時仍隱藏／清理。道具／地形／左手 UI 的物理遮擋仍有效；物件佔滿視野或選單被其他實體遮住時，不承諾強制可見／可點。

本次只修改 VR 選單定位、相關 S5 測試及目前說明，沒有更改輸入、共用選取／刪除核心、桌面場景、共享資產、S1–S4 參數、場景／建置設定或 I1／I2。編譯 e614e0ad completed、errorCount=0。S5 重驗 **9549df48 completed，6/6 通過，零失敗／略過**（246 秒）；新增三比例 × 四距離（0.15／0.5／1.2／3 m）× 五位置的完整四角檢查，以及調整後正式世界 Ray 命中 DELETE；近距離測試道具暫時設為 trigger 以避免推動角色，遮擋保留由原有正式實體案例驗證。離屏 PNG 是 Camera.Render，不能視為原生比例／連續縮放或人工驗收。

實際檢視三張邊緣離屏 PNG，選單完整在畫面內、名稱及 DELETE 可讀。60 組四角檢查與三比例世界 Ray 命中均通過，原有切換／清除／刪除、遮擋及生命週期五項也一併重跑。63 個保護檔案雜湊相同，沒有新增或修改場景／meta／LFS 資產。此次未重跑桌面測試、完整 38 項或原生焦點／連續縮放；先前證據維持歷史，不能視為此次重跑。


## 物件附著式選單（2026-11-07，待使用者驗證）

使用者回報此前點選刪除正常，但不接受位置自動變動與距離隱藏，因此本節取代前述兩版選單定位。

選單改為附著物件：以有效 Collider 世界包圍盒中心加上世界軸向偏移，預設 Offset Ratio=(0, 0.6, 0)，各軸分別乘包圍盒寬／高／深。Width Ratio 預設 0.5，選單寬為包圍盒最大邊長的一半，維持 260:120 寬高比；兩項皆可在 SimulationSelection 的 SimulationContextMenuView Inspector 調整。選單始終面向觀看者並保持世界向上。移除距離縮放、15 m 隱藏、最小深度、畫面邊緣夾限及自動前移；攝影機移動只改變朝向，不改位置或實際尺寸。近遠、畫面外或攝影機後方不主動停用 Canvas，自然透視、裁切及場景遮擋仍有效，由使用者移動至適當位置。右球射線上限仍為 100 m，這與選單顯示限制不同。

僅修改選單定位、相應測試與文件，不修改場景 YAML、共享資產、輸入路由或刪除核心。新序列化欄位使用程式預設值，無須手動重接參照；舊定位欄位已移除。本次依 AGENT.md 不操作 Unity 圖形介面、不執行 Editor 測試或 Play Mode。既有測試結果全部維持歷史事實；更新後六項 S5 測試尚未執行。

### 使用者操作步驟與預期結果

1. 開啟 VRSimulation，等待 Unity 完成編譯，確認 Console 無編譯錯誤。選取 Hierarchy 的 SimulationSelection，確認 SimulationContextMenuView 顯示 Offset Ratio=(0,0.6,0)、Width Ratio=0.5。
2. 進入 Play Mode，以既有左手清單放置 Cube、Sphere、Cylinder、Cone，再用右球射線逐一選取。預期名稱／DELETE 在物件中心偏上，寬約最大邊長一半；切換目標時更新名稱與位置。
3. 向物件靠近、遠離至超過 15 m，再繞到另一側。預期面板中心不漂移、實際尺寸不變，文字面向觀看者；視覺大小自然改變，畫面外或被擋住可自行移動找回。需在既有 100 m 射線範圍內操作。
4. Play Mode 中將 Offset Ratio 的 Y 改為 1、Width Ratio 改為 0.75。預期面板升高並變寬；測試後退出 Play Mode，避免誤認執行期設定已儲存。若需永久調整，在非 Play Mode 設定並儲存場景。
5. 射線點 DELETE，預期僅刪除該實例、關閉選單，之後仍可放置、選取及刪除。另測空白清除、Esc、新拖曳、按住移出再放開、失焦回復，預期無誤刪或穿透。
6. 以 16:9、4:3、寬螢幕及執行中縮放重試。預期不為保持畫面可見而改變世界位置／尺寸，射線仍對應 UI；近裁切與場景遮擋屬預期行為。
7. 選用自動案例：Window > General > Test Runner > EditMode，執行 SurfaceForge.Tests.SimulationSelectionTests 全部六項。測試會自行進出 Play Mode，請讓 Game View 保持可互動，回報結果與失敗訊息。本次已將舊畫面內四角斷言替換為物件比例／固定位置／朝向與自訂參數斷言，保留其他生命週期及遮擋案例。

上述項目目前全部為待使用者驗證，不將先前 6/6 當作本次結果。

Unity 外檢查：先以 dotnet restore SurfaceForge.Tests.csproj --source Temp 離線產生 Temp 內的還原資料，再執行 dotnet build SurfaceForge.Tests.csproj --no-restore --verbosity quiet，成功，0 警告／0 錯誤；此為 Unity 產生專案的外部 C# 編譯，不代表 Unity Editor 匯入或六項互動測試通過。git diff --check 通過。未操作 Unity API、圖形介面或啟動 Play Mode。


## 水平繞行選單（2026-11-07，待使用者驗證）

本節取代前一節固定世界位置的規則，保留歷史編譯與測試事實，不視為本次通過。

選單採水平繞行：以物件有效 Collider 世界包圍盒中心至攝影機的水平朝向定位，繞行半徑為包圍盒水平外接圓半徑，加上最大邊長乘 Orbit Gap Ratio（預設 0.1）。Offset Ratio 預設 (0,0.6,0)，X 為繞行座標水平側移、Y 為世界高度、Z 為朝向使用者的額外偏移，分別乘包圍盒寬／高／深。Width Ratio 預設 0.5，寬為最大邊長一半，維持 260:120 比例。三項可在 SimulationSelection 的 SimulationContextMenuView Inspector 調整。選單始終面向攝影機；觀看者繞行時位置隨之繞到朝向使用者的一側，高度與實際尺寸不隨觀看距離改變。攝影機正上／下方沿用最近水平朝向；首次無方向採攝影機水平後方，垂直觀看則使用 camera up 的水平分量作穩定備援。切換或清除目標會重設方向記憶。不依距離隱藏、縮放或夾限畫面；保留自然裁切、世界遮擋與 100 m 右球射線範圍。

未修改場景或資產，原 Offset Ratio 的序列化數值保留，但 X／Z 改為繞行座標，不再為世界軸向。新增 Orbit Gap Ratio 使用程式預設值。未修改選取／刪除／射線路由或 I1／I2。

### 使用者操作與預期結果

1. 開啟 VRSimulation，等待編譯完成，確認 Console 無編譯錯誤。SimulationSelection 的 SimulationContextMenuView 應顯示新增 Orbit Gap Ratio=0.1。
2. Play Mode 放置四種道具，逐一選取並以 WASD／右鍵觀看繞行。預期選單位置移到物件朝向使用者的一側、文字面向使用者；高度不隨水平繞行變動。只旋轉視角而不改變使用者位置，不應改變繞行位置。
3. 靠近、遠離至超過 15 m；預期實際尺寸與繞行半徑不變、沒有距離隱藏。必要時移動至合適距離或避開其他遮擋。
4. 調整 Offset Ratio 的 X／Y／Z 與 Orbit Gap Ratio，確認側移／高度／向外偏移及間距生效；Width Ratio 控制大小。正式調整需在非 Play Mode 修改並儲存場景。
5. 由不同方向操作 DELETE，預期只刪除選中實例，選單關閉；切換、空白清除、Esc、新拖曳與放置後再次選取均正常。其他道具或地形遮擋仍會阻擋操作。
6. 重試 16:9、4:3、寬螢幕與執行中縮放，預期射線對應正確、不因畫面比例重排世界位置。
7. 選用 Test Runner > EditMode > SurfaceForge.Tests.SimulationSelectionTests：更新的 OrbitMenuStaysOnViewerSideWithStableHeightAndSize 涵蓋四種道具、三種尺寸、三比例、近遠與八方位，以及正上方方向記憶、初始備援與自訂參數；既有案例保留 DELETE 射線／遮擋與生命週期檢查。全部待使用者執行，不宣稱已通過。

本次 Unity 外檢查：dotnet build SurfaceForge.Tests.csproj --no-restore --verbosity quiet 成功，0 警告／0 錯誤；git diff --check 通過。未操作 Unity API／介面、未執行 Editor 測試或 Play Mode，因此互動與畫面仍待使用者驗證。


## 右鍵觀看保留選單（2026-11-07，待使用者驗證）

本節取代歷史記錄中「右鍵觀看清除選取」規則。觀看保留已完成選取與選單，仍更新水平繞行；取消未完成場景／UI 按下並暫停射線互動，結束後先放開左鍵才接受新點擊。同批短按右鍵也適用。失焦、暫停、停用、Esc、新拖曳及目標失效仍依原規則清理。

新增 RightLookPreservesSelectionAndCancelsPendingDelete，原中斷案例移除右鍵清除的舊預期。Unity 測試共七項，未代為執行。

使用者驗證：開啟 VRSimulation 並進入 Play Mode，放置並選取物件；按住右鍵轉動視角、走動後放開，預期選單仍附著同一物件並隨位置繞行。另測 DELETE 按住左鍵 → 按右鍵觀看 → 放開右鍵 → 放開左鍵，預期物件未刪除，再次正常點 DELETE 才刪除。快速右鍵按放也不應關閉選單。確認 Esc、空白清除、失焦與新拖曳仍正常，並重試三種比例與縮放。可自行執行 Test Runner 的 SurfaceForge.Tests.SimulationSelectionTests 七項。

本次 Unity 外檢查：dotnet build SurfaceForge.Tests.csproj --no-restore --verbosity quiet 成功，0 警告／0 錯誤；git diff --check 通過。未操作 Unity API／介面或執行 Unity 內驗證。


2026-11-07 使用者回報目前測試正常，並要求提交 PR 與審查；確認對應右鍵觀看保留選單修正 8db0f54。未另列逐項案例、解析度或自動測試結果，不擴大為七項 Test Runner、原生連續縮放、Windows 或 XR 已通過。歷史待驗證文字保留其當時狀態，本補記為最新整體人工回報。

## QA：選單淡入淡出與本地 Y 軸（2026-11-07，待使用者驗證）

目前改為物件本地 Y 軸繞行（含斜坡傾斜），取代上方世界水平面的舊規則；尺寸／半徑改由物件座標包圍盒計算。新增 TiltedPropMenuOrbitsItsLocalYAxis，SimulationSelectionTests 共八項；獨立多 Canvas 遮擋案例明確開啟測試用 CanvasGroup。新增共用 ContextMenuFadeTests 一項，涵蓋 timeScale=0、淡出撤銷射線、快速反向及清理不被舊回呼重開。全部未由代理執行。兩版淡入 0.18 秒、淡出 0.12 秒，關閉立即清除有效目標、完成才停用圖像。操作步驟與實際外部檢查見 [QA 紀錄](QABeforeI2.md) 最新補記；先前七項／其他歷史測試結果仍對應原版本。

### QA 阻斷與初始化補強（2026-11-07）

使用者實測 ed10e06 後回報兩版刪除 UI 均未出現且 Console 無 error，上一節動畫／本地 Y 軸驗證因此未完成。唯讀現場觀察有有效選取、ContextMenu 停用且缺少 CanvasGroup；顯示入口已補上執行期綁定檢查與重建，首次初始化漏失的確切觸發因素仍未證實。新增真正 View 入口的初始化遺失回歸案例，VR 測試目前九項，均未代跑。實際證據、修正範圍及重新測試步驟見 [QA 紀錄](QABeforeI2.md) 最新補記；本次修正仍待使用者確認。
