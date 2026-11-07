# I2 前 QA 紀錄

> 2026-11-07 結案補記：本 QA 已隨 PR #21 合併，使用者已確認修正後測試正常，並於後續確認 I2 驗收、Windows 建置及 D1 發布交付完成。詳見 [完成與發布交付紀錄](DeliveryCompletion.md)；以下保留 QA 過程、失敗與未執行項目的歷史事實。

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

## 再次阻斷、實際重現與根因（2026-11-07）

使用者回報 1b92f9e 後 Main／VR 仍不顯示。使用者同時釐清授權：禁止的是 Codex Computer Use 接管滑鼠操作 Unity；所有 Unity Skills 操作、進出 Play Mode、重現與自動測試均允許。AGENT 與開發計畫已更新；先前「由使用者執行所有 Unity 內測試」及「未執行」保留當時事實，本節起以實際執行結果為準，人工觀感仍另由使用者確認。

本次先透過 Unity Skills 執行原有 ContextMenuFadeTests，在修正前重現失敗：原生元件 CanvasGroup 沒有建立，存取 alpha 時拋 MissingComponentException。Test Runner XML：2026-11-07 08:54:39Z～08:54:41Z，1 個案例、0 通過／1 失敗；失敗行為對應 ContextMenuFadeTests.cs 的 CanvasGroup.alpha 讀取。Unity Skills job 335aae10 在 domain reload 後追蹤原始 Test Runner job 失敗，回報 total=0；實際測試失敗證據取自該次新寫入的 TestResults.xml，而非宣稱 job 統計正常。原檔位於本機 LocalLow/DefaultCompany/SurfaceForge/TestResults.xml，後續測試會覆寫，提交前在忽略版控的 Temp 備份各輪證據。

已證實根因：初始化使用 `GetComponent<CanvasGroup>() ?? AddComponent<CanvasGroup>()`，但 Unity 在原生元件缺漏時可能回傳 Unity 判定為 null 的 wrapper，CLR 的 `??` 不會把它視為真正的 null，因此沒有新增 CanvasGroup。SetVisible 再以 Unity bool 檢查失效參照並直接返回，兩版均無畫面且未自行拋出錯誤。上一輪 Show 的重新初始化沿用同一錯誤判斷，故未解決問題；其不確定根因分析由本節實測取代。

修正為先 GetComponent，再用 `if (!group)` 判定並 AddComponent；共用測試在讀取 alpha 之前以 TryGetComponent 斷言原生元件存在。既有顯示前綁定檢查、動畫、立即撤銷互動及 VR 本地 Y 軸繞行保留，不修改場景或套件。其餘程式的 ?? 用於 Input System 可空布林值，無同樣的 GetComponent 合併寫法。

### 本輪驗證過程

- 修正後第一次共用測試 dd00b856：原生 CanvasGroup 已建立，但 alpha 仍為 0。該案例沒有像既有整合測試一樣開啟 Application.runInBackground；補上測試期間背景執行並於 teardown 還原後，同一案例 a0933836 的新 XML 為 **1/1 通過、0 失敗／略過**（2026-11-07 09:01:13Z～09:01:16Z），涵蓋 timeScale=0、淡出互動關閉、快速反向及清理。
- 兩版入口合併執行 2cace07c：XML 為 2 個案例、Main 通過、VR 失敗（2026-11-07 09:03:59Z～09:04:03Z）。VR 當下已能建立／開啟 CanvasGroup，但隨後背景焦點防護由正式射線模組清選取，alpha 淡回 0。顯示案例因此先停用輸入路由，再直接由選取入口驗證 alpha、真實世界射線 Query 到 DELETE 及按鈕事件的單次刪除；僅測試隔離，正式場景的焦點／射線輸入規則未改。
- 上述各次 Unity Skills 工作在重載後皆回報原始 Test Runner job 無法還原（total=0），故通過／失敗數採核對時間與案例名稱的新 TestResults.xml；不把工具 job 狀態描述為成功。XML 已按工作 ID 備份到 Temp/QAMenuVisibilityEvidence/，為本機忽略產物，未提交。

### 最終定點結果

| 案例／證據 | 實際結果 | 覆蓋與限制 |
| --- | --- | --- |
| 共用動畫 a0933836，XML 09:01:13Z～09:01:16Z | 1/1 通過，0 失敗／略過 | 原生 CanvasGroup 建立、timeScale=0 淡入、立即撤銷互動、快速反向與清理 |
| Main 入口，2cace07c XML 中 SelectionTests.MenuRepairsMissingFadeBindingsBeforeDisplaying | 通過（同輪 VR 的焦點失敗如上保留） | 真正桌面 SelectAt 入口、缺失綁定重建、面板啟用與 alpha=1、清除後不阻擋射線 |
| VR 入口 49db0f54，XML 09:07:51Z～09:07:54Z | 1/1 通過，0 失敗／略過 | 停用輸入模組以隔離原生焦點；真正 Selection／View 入口、alpha=1、右球實際世界射線 Query 命中 DELETE、按鈕事件只刪目標並撤銷射線 |

以上是三個不同定點案例的最終通過證據，不是一次完整套件 3/3，也不掩蓋前面的失敗。49db0f54 同樣有工具工作重載追蹤失敗，結果以核對名稱及時間的新 XML 為準。外部 Editor／Tests 專案編譯成功，各 0 警告／0 錯誤，最終 VR 測試另編譯成功；程式與文件差異／本機 Markdown 連結檢查通過。未修改 Main／VR 場景、套件或正式輸入防護，既有預覽材質差異保留排除。

未執行整個 SelectionTests／SimulationSelectionTests 套件、原生滑鼠輸入矩陣、三比例／連續縮放、I2／Windows／D1／XR；沒有截圖或人工視覺驗收。手動重測可從 SceneMenu 進兩版各放置／選取／DELETE 一次，再檢查快速開合、斜坡繞行與返回。上述定點成功不擴大為所有原有互動已驗收。

## 使用者確認、Main 滾輪設定與 PR 授權（2026-11-07）

使用者在 6d4e2a1190a58948a196935dfdef99e74b8484a7 修正後回報「以上測試正常」，並表示已將 Main 滾動速度改成 3，授權「提交 PR 並審查」。記錄為上述 QA 的整體人工測試確認，不推定完整套件、三比例／連續縮放、原生輸入矩陣、I2／Windows／D1 或 XR 已通過。前節未執行及失敗結果均保留原適用版本，不改寫歷史。

磁碟差異確認使用者調整的是 Overview Camera → TerrainOverviewCamera → Wheel Units Per Step：120 → 3；Zoom Per Step 仍為 1.15。此值為滾輪輸入換算分母，數值越小縮放越快，不將它誤記為 Zoom Per Step=3。Main 正常儲存亦將 Fade 的 0.18／0.12 秒與 Ease=OutCubic／InCubic 預設值寫入場景，與現行程式一致，一併納入。本輪未手改場景 YAML，未重新調整使用者設定，也沒有因純 Inspector 調參重跑 Unity 測試。

本次 PR 包含整個 codex/qa-before-i2 自 main 分出後的 QA 與規範／文件提交；排除並保留 SimulationPreviewValid.mat 既有未提交差異，未強制還原。以 GitHub Codex Review 審查最新提交並追蹤實際結果，不以代理自行檢查代替、不自行合併。

## 納入預覽材質同步差異（2026-11-07）

PR #21 初次審查後，使用者明確要求提交先前排除的 SimulationPreviewValid.mat。此次只有 `_Color` 從 alpha=1 同步為 0.35，與未變動的 `_BaseColor` 一致，並包含 r 分量的浮點序列化差異；Shader、keywords、render queue、blend、ZWrite 等其餘設定完全不變。

本機 URP 17.3.0 的 Editor/ShaderGUI/BaseShaderGUI.cs:939–940 明確將 `_BaseColor` 複製至 `_Color`；Unlit.shader 以 _BaseColor 為 MainColor，_Color 為隱藏欄位。因此將這份正常同步結果納入版控，不再沿用本文件前面「保留但排除」的歷史決策；沒有在磁碟手改 Unity YAML。先前排除是 QA 範圍判斷，不是禁止提交的規則。

本次只核對材質單行差異、URP 來源與 staged diff，未新增視覺調整或重跑 Unity／Windows／XR 測試。使用者此前整體正常回報是於這份差異已存在的本機工作區進行，仍不擴大為所有案例通過。提交後推送既有 PR #21，更新其最終差異說明並審查最新 head；不自行合併。
