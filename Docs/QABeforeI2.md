# I2 前 QA 紀錄

日期：2026-11-07（Asia/Taipei）。基準 `9a460945549674755de6fc478e6f1f7d1f138d59`，分支 `codex/qa-before-i2`。PR #19 已確認合併，fetch 後 main／origin/main 一致，且已包含 PR #20。開始時工作區與暫存區乾淨。本次僅本機修正與提交，不推送、不建立 PR 或請求審查；不開始 I2／Windows、D1 或實機 XR。

## QA 項目與處理

| 項目 | 重現／原狀 | 預期與處理 | 影響範圍／原因 |
| --- | --- | --- | --- |
| 1. VR 操作說明 | 開啟 VRSimulation，畫面沒有固定完整按鍵說明 | 新增頂端 Screen Space Overlay 三行說明，涵蓋 WASD、右鍵、Tab、左鍵射線／拖出放置、滾輪、Esc 及返回入口 | 僅 VR 場景新增顯示用 Canvas；原本只有左手面板的局部提示 |
| 2. 刪除選單位置 | 放置並選取道具，選單中心高於包圍盒頂端 | Offset Ratio 從 (0,0.6,0) 改為 (0,0.3,0)，位於朝向觀看者的前側偏上，保留水平 orbit around | 程式預設與 VR 序列化場景值同步；原 Y 偏移為物件高度的 0.6 倍，從中心計算已超過頂端 |
| 3. 精簡 README | README 混合操作、實作細節及多輪歷史補記，入口敘述互相矛盾 | 保留簡介、環境、入口、操作、目前狀態與文件索引；歷史結果仍保存於各驗證文件與 Git | 修正舊 Main 啟動入口，移除重複歷史敘述 |
| 4. 文件核對 | 開發計畫摘要落後於末節補記，VR 文件仍將 I1 列為後續規劃 | 更新目前進度、I1 additive Loading 與 PR #18／#19／#20 狀態、QA 高度值及 Esc 說明；歷史案例／SHA／job／未執行結果保留 | AGENT、DevelopmentPlan、VRSimulationDevelopment、I1Validation、S5Validation、ProjectSettingsCleanup 與 README |

## 實作與可調設定

- 場景以 `sf_author_simulation_qa` Editor API 編輯，確認實例 `SurfaceForge_409B7451`、SurfaceForge、6000.3.25f1，bypass／full；先 dry run。作者拒絕 Play Mode、未儲存場景及重複 UI，完成後恢復原場景配置。現有場景已建立 UI，**不要重跑作者**。
- `Simulation Controls UI`：Screen Space Overlay，sortingOrder=10，Canvas Scaler 1920×1080、match=0.5。Controls 以頂端水平延伸錨點，左右各 16、頂距 16、高 112；Instructions 字級 26、三行。文字、字級、RectTransform 邊距與高度皆可在 Inspector 調整。沿用 SceneNavigation 的 navigationLabels／OS 字型設定，不新增 runtime 輸入元件。
- 說明 Canvas 無 GraphicRaycaster，CanvasGroup 的 interactable／blocksRaycasts 皆 false，Image／Text 的 raycastTarget 皆 false；既有世界射線仍只查詢左手與刪除 Canvas。
- `SimulationSelection → SimulationContextMenuView`：Offset Ratio=(0,0.3,0)，Y 為包圍盒中心上方 0.3 倍高度；Width Ratio=0.5、Orbit Gap Ratio=0.1。保留世界尺寸、水平繞行、面向觀看者、自然裁切及場景遮擋。
- 本次未修改生命週期、輸入路由或狀態切換；既有 `OrbitMenuStaysOnViewerSideWithStableHeightAndSize` 更新高度預期與測後重設值，保留四種道具／尺寸／比例／方位／遮擋及右鍵觀看案例，沒有為純外觀新增重複測試。

## 文件核對依據與結論

- 核對 ProjectVersion、manifest、四場景清單、SceneNavigation／LoadingController、SimulationPropMenu／SimulationRayInputModule／SimulationWorldRaycaster／SimulationContextMenuView，以及 I1、S5、設定整理與各階段紀錄的狀態摘要。
- SceneMenu 為入口，場景順序 SceneMenu → Main → VRSimulation → Loading；Loading 仍先覆蓋、卸載來源再載入目標，預設 0.25／1.5／0.25 秒、unscaled time。這些設定未因 QA 改動。
- M1–M4、S0–S5、I1 為已實作，S5／I1 的使用者整體正常回報僅適用原版本（8db0f54／3a373fc），不能推定本次 UI、所有自動測試、三比例、連續縮放、Windows 或 XR 通過。
- M1–M4、S0–S4 與 Git 遷移紀錄維持歷史用途，不把其中當時「尚未開始下一階段」改寫為新結果。I1／S5／設定整理文件增加現況提示；六檔不可提交的舊限制已結束，舊 TaskCheckpoint 不再作為現況來源。
- PR #19 後重新開啟、光照／後製畫面及儲存穩定性仍待使用者確認；本次沒有證據可改列通過。本次所有 Unity 內測試由使用者執行，此限制已寫入 AGENT 的本次 QA 範圍，未修改一般自動測試政策。

## 已檢查

- Editor API 讀回：VRSimulation 已儲存且 isDirty=false；新 Canvas 為 ScreenSpaceOverlay，CanvasGroup 不互動／不阻擋；選單 Offset Ratio=(0,0.3,0)、Width Ratio=0.5、Orbit Gap Ratio=0.1。
- 磁碟場景區塊比對：僅修改三個既有區塊（高度、navigationLabels 加入說明文字、SceneRoots 加入說明 UI），新增 13 個說明 UI 區塊；沒有其他既有場景區塊改動。新區塊內部 fileID 引用可解析，沒有新增 Camera／AudioListener／輸入模組；沒有手改 Unity YAML。
- Unity 外 `dotnet build Assembly-CSharp-Editor.csproj --no-restore --verbosity quiet` 成功，0 警告／0 錯誤；`dotnet build SurfaceForge.Tests.csproj --no-restore --verbosity normal` 成功，0 警告／0 錯誤。測試專案初次 quiet 建置失敗且無診斷，重試可見共用編譯伺服器 named pipe 逾時後回退完成；最終作者加入 Canvas.ForceUpdateCanvases 後，Editor 專案的平行 MSBuild 相依解析仍偶發無診斷失敗；以 `dotnet build Assembly-CSharp-Editor.csproj --no-restore --verbosity quiet -m:1 -p:UseSharedCompilation=false` 重新編譯成功（0／0）。未改套件或專案檔。
- Editor 匯入程式完成（job 21f557d2／5f1a1311，errorCount=0），僅作資產編輯所需匯入，不是 Test Runner 或 Play Mode 驗證。
- 本次 API 首次寫入在回傳 Vector3 時發生序列化錯誤；場景已儲存，改為陣列回傳並以唯讀查詢及磁碟比對確認結果，未重跑作者。
- 場景引用、全部專案 Markdown 本機檔案連結與差異檢查通過；staged diff 提交前逐項檢視。Unity 序列化空值行尾空白保留，場景差異檢查排除 blank-at-eol。
- Editor 匯入後 `SimulationPreviewValid.mat` 出現 `_Color` 自動同步差異；依使用者保留新未提交變更的要求，未還原、未納入 QA 提交。它不屬於本次已提交修正，且會影響目前工作區的實際試玩版本，測試回報請一併註明。

## 使用者驗證（全部未執行）

先儲存目前工作，等待匯入完成並確認 Console。由使用者開啟 SceneMenu 並進入 Play Mode。

| 案例 | 操作 | 預期結果 |
| --- | --- | --- |
| QA1 說明顯示 | 進入 VR；WASD、右鍵轉向、Tab 開關左手面板 | 三行按鍵說明固定於畫面上方，不隨角色／面板移動；中文字完整可讀，內容符合實際操作 |
| QA1 比例／縮放 | 在 1280×720、1024×768、1920×800 查看，再連續縮放 Game View | 說明保持頂端並隨 Canvas 縮放，不裁字或遮掉主要操作；極窄視窗若換行不足，記錄尺寸再調整，不視為已通過 |
| QA1 輸入回歸 | 讓射線經過頂端說明區，點選其後可見道具；正常開面板、拖出及取消 | 說明本身不吃 hover／點擊、不改射線目標，不攔截原本操作；原左手 UI／場景遮擋仍有效 |
| QA2 高度與繞行 | 放置 Cube、Sphere、Cylinder、Cone，逐一選取，WASD 繞行並按右鍵觀看 | 名稱／DELETE 在物件朝向觀看者的前側偏上，隨水平位置繞行；尺寸／高度不隨距離改變，右鍵保留選取 |
| QA2 刪除與遮擋 | 從不同方向點 DELETE；另用其他道具遮住射線後嘗試 | 每次只刪選中實例；有遮擋時不可穿透。近裁切／畫面外仍需自行移動至可見位置 |
| 基本互動回歸 | VR 各測合法放置、UI／無地形放開、Esc／Tab 取消、空白清除、重新選取與刪除；桌面放置、選取及刪除一次 | 無殘留預覽／指引、無誤放置或誤刪除；Esc 不返回選單，兩版原功能仍可用 |
| 返回與重入 | VR 左手面板返回，再進入；另直接開 VRSimulation 測一次 | 經既有 Loading，沒有重複說明；舊道具與選取清除、面板收合；說明只在 VR 存在，Loading 能覆蓋畫面 |
| QA3／4 文件 | 核對 README 操作與本紀錄的進度、限制 | 入口與按鍵符合實作，沒有把歷史結果寫成本次通過；未執行項目仍明列 |

必要自動回歸可由使用者執行 Test Runner → EditMode → `SurfaceForge.Tests.SimulationSelectionTests`（七項，含繞行與右鍵觀看）；測試會進出 Play Mode，先儲存工作。若要確認轉場沿用 `SceneNavigationTests`、`LoadingProgressTests`。本次代理未執行任何 Unity 內測試，不以編譯／匯入代替互動驗證。

本表為本次變更的基本回歸，不取代 I2 的完整整合、反覆往返、Windows 執行檔與原生視窗驗收。字型後備、極小畫面及近距離可讀性仍需人工確認。

## 後續 QA：說明文字與 Main 縮放（2026-11-07）

基準 d9c39977595f7187ee223bb185cfff550d7afcb1，沿用同一 QA 分支。開始時只有 SimulationPreviewValid.mat 的既有未提交差異，繼續保留並排除。

| 項目 | 重現／原狀 | 預期與修正 | 影響範圍 |
| --- | --- | --- | --- |
| VR 說明 | 頂端列出滾輪，但未明確描述刪除流程 | 移除滾輪文字，新增點選物件後點 DELETE 刪除 | 僅 VR 說明 Text 與對應作者；VR 滾輪功能維持 |
| Main 縮放 | 在場景上滾輪，視角沒有縮放；攝影機每幀重算地形完整取景 | 原自動構圖乘上縮放倍率，上滾拉近、下滾拉遠 | TerrainOverviewCamera 與 Main 的 placement／navigation 明確參照；不改 VR 攝影機 |

設定位置：Main Camera → TerrainOverviewCamera，Zoom Per Step=1.15、Minimum Zoom=0.35、Maximum Zoom=1.5、Wheel Units Per Step=120。倍率越小視野越近，1 為原始完整地形取景。不同滾輪裝置的 units 需依實際手感調整。縮放固定以既有畫面中心為中心，不新增平移或滑鼠落點對焦。

輸入保護：只在有效 Game View／App 焦點且游標位於攝影機 viewport 內接受輸入，透過既有 PlacementController.IsOverUI 即時查 UI，UI／拖曳／左鍵持有／失焦／暫停／轉場及等待放鍵時均阻擋。中斷後須先經過零滾輪影格；重啟元件／重新載入場景重設倍率，視窗比例與側欄自動構圖仍保留。本次加入 TerrainOverviewCameraTests 兩項有關中斷後不沿用輸入、倍率上下限及重啟重設的回歸案例；它們只檢查狀態／倍率，不代替真實 UI、焦點及裝置輸入測試，未由代理執行。

使用者驗證（全部待執行）：

1. VR 進入後，預期三行說明不再列出滾輪，且清楚顯示「點選物件後，點 DELETE 刪除」；四種道具選取／刪除與原清單滾輪功能仍可用。
2. Main 游標放場景空白處，先停一下再上／下滾；預期平順拉近／拉遠且有上下限。持續滾動至限制後反向可立即縮回，UI 尺寸不隨場景變化。
3. 在清單／側欄／刪除選單／返回按鈕上滾動；預期視角不動，清單捲動仍有效。游標回到場景後先讓滾輪歸零，再滾動才縮放。
4. 拖出預覽期間、按住左鍵、切到 Inspector／其他視窗、轉場期間滾動；預期不縮放、不放置或選取額外道具。回焦後停止滾動再重新滾動才接受。
5. 縮放後測合法放置、UI／無命中取消、重複拖曳、選取／刪除與選單跟隨；預期以新攝影機射線正常操作。返回再進 Main 應恢復初始取景，VR 不受影響。
6. 16:9、4:3、寬螢幕及 Game View 連續縮放各測一次；預期側欄保留，取景依當前倍率重新適應。放大後部分地形離開畫面屬預期，縮小找回。
7. 使用者可執行 Test Runner → EditMode → SurfaceForge.Tests.TerrainOverviewCameraTests，及既有 PlacementTests／SelectionTests 回歸；先儲存場景。本次均未執行，不取代 I2。

本輪已檢查：Editor API 回傳完整新說明文字、Overview Camera 的 placement／navigation 已接線；場景區塊比對確認 Main 僅攝影機設定區塊變動、VR 僅說明 Text 區塊變動，無新增／刪除既有區塊，兩份 Main 新參照可解析。兩個 Unity 外專案以 `dotnet build <專案> --no-restore --verbosity quiet -m:1 -p:UseSharedCompilation=false` 編譯成功，各 0 警告／0 錯誤；最終測試檔另編譯成功。Markdown 本機檔案連結與差異檢查通過；Unity 序列化空白依既有方式保留。場景作者需先離開 Play Mode 並儲存未儲存場景，這是編輯保護，不是 Unity 測試結果。既有材質差異仍未提交。

## 後續 QA：兩版刪除選單動畫與 VR 繞行軸（2026-11-07）

基準 3b8a9b90c5c36332a0d91113daf4337207133ae5，工作區原有 SimulationPreviewValid.mat 差異保留排除。本輪不修改場景／資產；新增序列化 Fade 設定使用程式預設值，CanvasGroup 於各場景自身的選單初始化時取得／建立，不跨場景保存。

| 項目 | 重現／原因 | 修正／預期 |
| --- | --- | --- |
| 兩版開合動畫 | 選取或清除道具，選單直接顯示／停用 | 共用 DOTween alpha 淡入 0.18 秒 OutCubic、淡出 0.12 秒 InCubic，使用 unscaled time；Fade Inspector 可調時間與 Ease |
| VR 繞行軸 | 道具貼合斜坡後 Y 軸傾斜，原投影／高度／半徑使用世界 Y 與世界 AABB | 改在物件本地 XZ 平面、沿 transform.up 繞行，尺寸與中心由物件座標包圍盒計算；保留前側偏上、朝向觀看者與遮擋 |

關閉當下 Target 與有效選取立即清除，CanvasGroup.interactable／blocksRaycasts=false；文字與最後位置只保留到淡出完成，不能繼續刪除。重新開啟從目前 alpha 反向，切換有效物件維持已開啟進度並立即更新目標文字／位置。停用、卸載、失焦／暫停立即 Kill 自己的 Tween 並歸零，不靠 Safe Mode 或 KillAll 清理。淡入期間可點擊，動畫不改幾何尺寸；桌面原有畫布邊界夾限、VR 右鍵保留選取及 UI 遮擋規則不變。VR IsVisible 表示目前開啟且有互動意圖，淡出期間雖仍有圖像但回傳 false。

必要回歸已加入、全部未執行：ContextMenuFadeTests（暫停時間、快速反向、撤銷互動與立即清理）；SimulationSelectionTests 新增傾斜立方體八方位本地 Y 軸檢查；SelectionTests 等待關閉動畫完成後再判定圖像停用，VR 多 Canvas 人工配置案例明確設定 CanvasGroup。這些測試不代替真實滑鼠／射線及畫面驗收。

使用者驗證（全部待執行）：

1. Main、VR 各放置並選取物件，預期約 0.18 秒淡入；點空白／Esc／刪除時約 0.12 秒淡出，關閉立即不可點 DELETE，僅刪原選中實例。
2. 快速選取→空白→再選取，及連續切換物件；預期不閃回起始透明度、不重複建立 UI、不讓舊淡出回呼關掉新目標。刪除後可再次正常放置與選取。
3. 在斜坡放置四種道具並選取，以 WASD 繞行並右鍵觀看；預期軌道沿道具自己的 Y 軸、平行於本地 XZ 平面，高度沿該軸保持固定。平地仍水平繞行，文字面向觀看者。其他道具／地形遮擋仍不可穿透。
4. 淡入或淡出中失焦／暫停／停用選單，再恢復或返回重入；預期無殘影、Tween 回呼錯誤、幽靈射線或舊選取。timeScale=0 時動畫仍可完成。
5. 測 16:9、4:3、寬螢幕／縮放，桌面邊界選單與滾輪縮放後仍可用；VR 左手面板／放置預覽／取消／返回維持。相機位於物件 Y 軸附近時不因投影零向量出現無效座標。
6. 使用者可在 Test Runner → EditMode 執行 ContextMenuFadeTests、SimulationSelectionTests（八項）、SelectionTests；先儲存場景。此為基本回歸，非 I2 完整整合、Windows 或 XR 驗收。

本輪已檢查：Unity 外 Assembly-CSharp-Editor.csproj 與 SurfaceForge.Tests.csproj 以既有單節點、停用共用編譯程序的指令成功，各 0 警告／0 錯誤；最後測試配置修改另編譯成功。Editor 資產匯入 job 9f334490 completed、errorCount=0，僅為程式匯入，不是測試。Main／VR 場景檔與本輪 HEAD 逐位元組正規化比對相同；Markdown 本機檔案連結與差異檢查通過。新增腳本及其 meta 一同提交，既有預覽材質差異仍排除。人工與 Test Runner 皆未執行，故無動畫舒適度、斜坡繞行或實際射線互動通過宣稱。

## 阻斷回報：兩版刪除 UI 未顯示（2026-11-07）

適用基準 ed10e061e3b18c3f0c052d1ced7642dfe729d47e。使用者回報 Main／VR 的刪除 UI 都沒有出現，Console 無 error，導致上一輪動畫及繞行測試無法進行。這是該版的實際失敗回報，不能以先前外部編譯成功視為 UI 通過。

在使用者自行進入 Play Mode、放置並點選 Sphere 後，代理只讀取 API 現場：SimulationContextMenuView.Target=Sphere(Clone)，IsVisible=false；ContextMenu 已有位置／縮放更新但 activeSelf=false，且查無 CanvasGroup。後續讀取時 Target 已清除，符合離開 Game View／失焦會清除的既有流程，不能將這兩次讀值視為原子快照。Console 診斷為 0 error／0 warning。使用者隨後確認已停止 Play Mode；代理沒有進出 Play Mode、模擬輸入或執行測試。

程式可確認的缺口：ContextMenuFade 只在 View.OnEnable 初始化；SetVisible 在 root／group 未綁定時直接返回，不建立圖像或記錄錯誤。現場缺少 CanvasGroup 與此顯示路徑相符，但尚無證據確認首次初始化為何未建立／保留綁定，不將反序列化或執行順序宣稱為已證實的唯一根因。

修正：新增 ContextMenuFade.Show(host,panel)，兩版 RefreshPosition 在顯示前傳入明確 host／panel，檢查執行期綁定與 CanvasGroup；缺少時 Initialize 後才開啟。原開關動畫、快速反向、立即撤銷互動與生命週期清理維持，未修改場景、射線、選取核心、縮放或繞行定位。

兩版既有整合測試各新增 MenuRepairsMissingFadeBindingsBeforeDisplaying：以 JsonUtility 只複製序列化設定、更換 helper 並移除 CanvasGroup，直接透過真正的 View／Selection 顯示入口，檢查面板啟用、CanvasGroup 重建、alpha 到 1，關閉立即撤銷射線。這些案例未由代理執行；它們覆蓋本次初始化缺口，不證明現場最初的觸發因素。

使用者重測（全部待執行）：

1. 等待匯入完成，從 SceneMenu 進 Main，放置 Cube 並點選；預期名稱與 DELETE 淡入，Hierarchy 的 Menu Panel 啟用且有 CanvasGroup，約 0.18 秒後 alpha=1。
2. 進 VR，放置 Sphere 並用右球射線點選；預期 ContextMenu 啟用且有 CanvasGroup，名稱與 DELETE 可見可點。若仍不顯示，維持現場並回報，繼續唯讀查核 Target、activeSelf、alpha 與 Tween 狀態。
3. 兩版各測 DELETE 刪除一次、空白清除、快速清除再選取；預期僅刪選中物件，淡出期間不可再刪除，不留下無形 UI 或舊回呼。
4. 可見性恢復後，再執行上一輪動畫／斜坡本地 Y 軸繞行、三比例及返回回歸，之前被阻斷的案例仍不列為通過。
5. 使用者可執行 Test Runner 的 SelectionTests、SimulationSelectionTests（目前九項）、ContextMenuFadeTests；本次代理均未執行。

額度規則另依使用者本輪釐清更新：只以 5 小時剩餘 10% 以下（含）判斷暫停，每週額度不列入。先前因每週剩餘 10% 宣告暫停為規則修正前的歷史處理，不再適用；當時尚未寫入修正，也未實際建立 TaskCheckpoint。本輪重新確認 5 小時剩餘 76% 後繼續。

本次外部檢查：Assembly-CSharp-Editor.csproj 與 SurfaceForge.Tests.csproj 皆以單節點／停用共用編譯的 dotnet build 成功，各 0 警告／0 錯誤；桌面測試改用有效選取入口後另編譯成功。Editor 匯入 7688d3c8 completed、errorCount=0，僅為資產匯入。文件連結及差異檢查通過；沒有場景修改、Play Mode 操作或 Test Runner 執行。實際恢復可見與動畫仍待使用者確認。
