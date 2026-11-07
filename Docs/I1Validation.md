# I1 啟動場景選單與返回入口

日期：2026-11-07。基準為 PR #17 合併後的 `e315510da4e6c29cc4d9828e9e8f06478f09dd31`，分支 `codex/scene-menu-i1`。本紀錄只涵蓋 I1；S5 歷史結果見 [S5Validation.md](S5Validation.md)，不代表本版重跑。

## 已實作

- `SceneMenu.unity` 提供「桌面版驗收」與「VR 模擬驗收」。兩版各有「返回場景選單」；桌面位於左上，VR 位於 Tab 左手面板下方，沿用同一 Canvas、CanvasGroup 與右球射線路由，不增加滑鼠模組或 GraphicRaycaster。
- SceneNavigation 先記錄第一個有效請求，在 UI 派送後的 LateUpdate 停用場景輸入，取消拖曳、預覽、指引、選取、面板及未完成按鍵，再恢復可見且未鎖定的游標，以 `LoadSceneMode.Single` 替換場景。重複請求不能改寫目的地。
- 不新增 DontDestroyOnLoad、存檔或跨場景道具同步；舊場景卸載，重新進入使用場景的初始配置。輸入元件的 OnDisable 會移除訂閱並恢復 Input System 設定。
- Esc 沿用取消拖曳／清除選取／關閉面板語意，不返回場景。VR 右鍵觀看仍保留已完成的選取，並暫停射線。
- 標準全域及當前場景清單順序為 SceneMenu → Main → VRSimulation；不執行 Windows 建置。
- 透過 Unity Skills 的專案自訂 Editor API 產生與儲存場景，未手改 YAML。場景作者 `sf_author_scene_menu` 拒絕 Play Mode、未儲存場景與覆寫既有 SceneMenu；不要在現有場景上重跑。`sf_configure_scene_order` 可重新設定這三個場景的清單，不產生建置。

## Unity 外檢查與狀態

- 遠端 PR #17 已合併；fetch 後 main 與 origin/main 均為上述基準 SHA，從 main 新建 I1 分支。
- 初次 `dotnet build Assembly-CSharp-Editor.csproj --no-restore` 因缺少 SDK 還原資產失敗；本機 restore 後編譯成功，0 警告／0 錯誤。最終編譯與差異檢查結果於下方補記。
- Unity 資產匯入與 API 寫入僅用於實作，不視為 Editor 測試或 Play Mode 驗證。
- 本次沒有代理執行的 Unity 內測試。以下所有操作案例及新增的 `SceneNavigationTests.SingleLoadCleansAndReentryStartsFresh` 都是**待使用者驗證**。
- 原有四個受追蹤設定檔，以及未追蹤的 SceneTemplateSettings.json、TaskCheckpoint.md 保留並排除提交。儲存 VR 場景時，Unity 將 S5 已移除的舊序列化參數換成目前程式預設的 Offset Ratio／Width Ratio／Orbit Gap Ratio，沒有改動其有效值。

## 基本功能驗證步驟（由使用者執行）

先儲存你正在編輯的場景。開啟 `Assets/SurfaceForge/Scenes/SceneMenu.unity`，進入 Play Mode；以滑鼠按兩個入口，VR 內先放開按鍵再操作。每次檢查 Console 是否出現錯誤。

| 案例 | 操作 | 預期結果 |
| --- | --- | --- |
| 桌面進入／返回／再進入 | 點「桌面版驗收」，展開清單、放置兩個道具、選取其中一個，點左上「返回場景選單」，再進桌面 | 顯示原地形與 UI；返回後選單可點；再進入無前次道具、預覽、選取或刪除選單，清單收合 |
| VR 進入／返回／再進入 | 返回選單選「VR 模擬驗收」，Tab 展開、射線拖出及放置、選取，射線點面板下方「返回場景選單」，再進 VR | 仍只有右球射線操作返回；再進入角色與左右球回到場景初始配置、面板收合、無舊道具與選取 |
| 兩方向交叉進入 | 桌面 → 選單 → VR → 選單 → 桌面；再做 VR → 選單 → 桌面 → 選單 → VR | 各場景獨立初始化，沒有舊 UI、視角、選取、重複事件或額外放置 |
| 桌面拖曳清理 | 從清單拖出道具，移到返回按鈕上放開；再正常點返回 | 在 UI 放開取消且不誤放置；未形成普通 click 時不強制返回。返回再進入沒有預覽；仍能正常放置 |
| VR 拖曳清理 | 射線拖出道具，瞄準返回按鈕放開，再重新點返回 | 拖曳擁有權優先：第一次放開只取消放置，指引消失；新點擊才返回，不穿透放置或選取 |
| Esc 保留 | 桌面拖曳時 Esc；VR 拖曳時 Esc，無拖曳時再按 Esc | 取消拖曳；VR 無拖曳時清選取並關面板；都留在原場景 |
| 游標恢復 | VR 按住右鍵觀看、放開，Tab 開面板、射線返回；再從選單進桌面 | 選單及桌面游標可見、未鎖定、能點擊；右鍵觀看時返回 UI 不被觸發 |
| UI 優先與遮擋 | 桌面在返回 UI 上放開；VR 讓道具位於眼睛／右球至返回按鈕間，嘗試點擊，再移到無遮擋位置點擊 | UI 不產生背景放置／選取；VR 被前景遮擋時不可穿透點返回，無遮擋且面板完全展開才可操作 |
| 原功能基本回歸 | 兩版各放置、選取及刪除一次；VR WASD、右鍵觀看、Tab 面板、射線與落點指引各操作一次 | 原有功能仍可用，桌面與 VR TerrainData 分離，未改地形與既有互動參數 |
| 單套有效元件 | 在選單及兩版各暫停觀察 Hierarchy／Inspector | 只有當前場景，一個有效 Camera、AudioListener、EventSystem；VR 只有 SimulationRayInputModule 有效，沒有新滑鼠模組 |
| 比例與縮放 | 分別在 1280×720（16:9）、1024×768（4:3）、1920×800（寬螢幕）操作兩入口及兩返回按鈕；執行中拖動 Game View 大小 | 按鈕、中文字可見且可點；VR 射線跟隨真正 UI，沒有偏移或穿透；原面板仍可操作。記錄實際解析度與結果，不由固定比例推定連續縮放通過 |

清理的非 UI 可達狀態（例如右鍵鎖定中／持有拖曳時直接請求返回）可由下述測試補足，不需新增快捷鍵或競爭輸入。

## 高風險生命週期測試（已加入，未執行）

由使用者開啟 Window → General → Test Runner，EditMode 篩選 `SurfaceForge.Tests.SceneNavigationTests`，執行該類別。測試自行進出 Play Mode、載入場景；先儲存工作，完成後再自行開啟所需場景。

案例對兩版各建立正式道具、啟動拖曳預覽，VR 加入選取，設定鎖定游標，再同批請求返回及第二個目的地；預期第一個請求生效、舊實例消失、游標恢復、僅單套有效元件、再進入為乾淨狀態。此測試不模擬原生滑鼠／精準右球遮擋，不取代上表人工操作。

## 可調設定與限制

- `SceneNavigation` Inspector：明確場景參照、Navigation Labels、Font Names。新增中文字執行時使用 Windows 的 Microsoft JhengHei（後備 Noto Sans CJK TC／Arial Unicode MS）；不散布 OS 字型。缺少這些字型的環境中文字需另行確認。編輯模式使用內建字型可能顯示缺字，Play Mode 才套用 OS 字型。
- 按鈕尺寸與錨點可在各場景 ReturnToMenu 的 RectTransform 調整；VR 返回按鈕為既有左手 Canvas 下方額外一列，跟隨面板動畫與可互動狀態。SceneMenu Canvas Scaler 為 1920×1080、match=0.5。
- 不包含 I2 至少十次反覆往返、完整邊界／失焦／連續縮放矩陣與 Windows 執行檔驗收；不包含 D1 錄影、上傳或實機 XR。沒有推送、PR、審查或合併。

## 最終 Unity 外檢查補記

- `dotnet build SurfaceForge.Tests.csproj --no-restore -v minimal` 成功，0 警告／0 錯誤，確認生成的專案檔包含新增 SceneNavigationTests；只編譯，未執行測試。
- `dotnet build Assembly-CSharp-Editor.csproj --no-restore -v minimal` 成功，0 警告／0 錯誤，包含場景作者與最終全域清單程式。
- 磁碟靜態檢查：三場景各一個序列化 Camera、AudioListener、SceneNavigation，所有場景內部 fileID 參照均可解析；全域清單順序正確。此檢查不能證明執行時輸入或事件生命週期通過。
- 程式／文件的標準 `git diff --check` 通過；Unity 序列化場景保留空欄位後的慣用空白，完整標準檢查會回報 trailing whitespace。未為修空白手改 YAML；以停用 blank-at-eol 的差異檢查確認其他空白問題。staged diff 另外逐項檢視。
- Editor 自動同步的兩份 SimulationPreview 材質差異已還原（開始時沒有變更），不納入 I1；指定六個既有未提交檔案保留。
- 最後額度檢查：5 小時剩餘 73%、每週剩餘 21%，未觸及暫停門檻。
