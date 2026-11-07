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
