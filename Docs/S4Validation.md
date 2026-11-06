# S4 射線放置、共用核心與落點指引驗證紀錄

日期：2026-11-06（Asia/Taipei）。基準為重新 fetch 的 `6b6649ad4c6f830a5702bafde02e68d9d1a2aefa`；GitHub PR #14 metadata 確認已合併且 merge SHA 相符。本機 main 快轉檢查後建立 `codex/vr-simulation-s4`，未沿用 S3 分支。提交 SHA 以本文件所屬 Git 歷史為準。

## 實作與操作

- SurfacePlacementQuery 增加世界 Ray 入口，桌面螢幕座標轉接同一指定 TerrainCollider／Surface mask／有限距離與 PoseAt 規則。PlacementController 共用預覽、底部姿態、有效性、提交、PlacedProp layer 與取消；桌面輸入生命週期只處理桌面手勢，VR 由既有專用路由擁有。未調整選取／刪除核心，VR 未配置該功能。
- 左鍵按住項目，實際右球射線與來源平面的交點相對按下位置移動至少 18 UI 單位（最終比例約 0.018 m），且離開來源矩形後取得拖曳擁有權；不依水平／垂直分類。普通短按保留 S3 CLICK 回饋，不建立道具。門檻是可調初值，尚待使用者試玩。
- 取得擁有權後只保留一個預覽，來源四項不重建；送取消用 pointerUp、清除 hover，不派送其他項目的新 press／click，也不捲動清單。候選按住期間也暫停滾輪。保留 Canvas 阻擋，不以 blocksRaycasts=false 淡化穿透。
- 每次 LateUpdate 使用右球 Aim／Query，再以同一實際 Ray 查詢指定 VR TerrainCollider。UI 上隱藏預覽；無地形時在射線前方 5 m 顯示紅色無效預覽，不沿用舊合法點。放開當影格重查 UI，EndWorld 再查地形，合法才建立一個正式道具，同次放開不送 click。
- 既有操作射線維持直線與原材質。VR 專用半透明預覽材質由共用材質複製，alpha=0.35；金色指引線寬 0.018 m，從預覽沿法線的頂部上方 0.12 m 連至真正落點上方 0.015 m。落點圓環寬 0.025 m、半徑 0.95 m、48 段，朝向地形法線；較大的圓環與透明預覽讓底部落點可見。共享材質與 prefab 不變。
- 指引只在非 UI 且合法地形命中時顯示；UI、無命中、提交、取消、停用與卸載清理。放置結束仍保留控制器操作射線。
- 拖曳期間禁止進入右鍵觀看，仍按住的右鍵在結束後需放開再按。Esc 優先取消拖曳，沒有拖曳才關面板；Tab 收合先取消。來源／路由／放置核心／角色／射線元件停用、失焦、暫停、離開有效 Game View 與卸載均取消，左鍵需放開後才能重取。保留 S2 動畫反向、短按及 Tab release 防護與 S3 遮擋／viewport 規則、3／24 滾輪設定。
- 場景與三份專用材質透過暫時 Editor API 工具建立、儲存及磁碟重載；沒有手改 Unity YAML。Unity Skills 確認路徑為本專案，instance `SurfaceForge_409B7451`、8090、bypass/full。Unity／套件版本保持原樣。

## 實際測試與診斷

- 編譯 `c82fb6a5`、`ba722cef`、`fa9abd22`、`0529cf7c`、`9ba96d3a`、`6b1301ec` 均 completed，errorCount=0。
- `28af9230` 因 batch_state.json 分享衝突停在 queued；確認沒有 runnerJobId、Editor 未執行測試後，工具恢復等待最終 failed，totalTests=0。未刪除檔案、清除診斷或重複啟動仍在執行的測試。
- `4723e6e9` 0/4；`1afbb364` 1/4；`6494e6af` 2/4。首輪世界查詢測試未先展開面板以觸發 catalog Awake／Bind，已修正初始化；輸入案例受原生 Game View 焦點／裝置及近距離落點遮擋影響。以 computer-use 原生聚焦 Game View，測試暫時停用其他 Mouse／Keyboard 並於 teardown 復原，改用右側落點。沒有放寬正式焦點與遮擋防護。
- 最終 S4 `0560da88`：4/4 通過，failed／skipped／inconclusive 均 0，53 秒。每項真正載入 VRSimulation 並進出 Play Mode。
- 世界查詢案例：掃描原 TerrainData 找出高、低、緩坡樣本，再由上方向下 Raycast 真正 Collider，另測邊界內及外；四種原 prefab 共 16 次提交，預覽／正式錨點與旋轉一致、法線朝向、Model 底部偏移、碰撞器與 layer、非擁有者拒絕與重複 End 不重建。放開前關閉 TerrainCollider、改為 UI 或地形外均不建立。
- 正式路由案例：Input System 測試 Mouse 經正式 LateUpdate，四種道具各重複兩次，共 8 次；來源 InstanceID 保留、每次單一預覽、放開單次建立、無額外 click。驗證 UI 隱藏、無命中紅色預覽、不沿用舊點、金線與圓環清理及操作射線恢復。
- 門檻／互斥：短移動、普通短按、同影格按放、拖曳後移到另一項、溢出 viewport 滾輪不動、右鍵不能進入觀看，放置後仍按住不能接管，放開再按才觀看。
- 取消：焦點／暫停生命週期回呼、實際停用重啟路由／來源／核心／角色／射線／面板、測試座標離開視窗、Input System Esc／Tab、持續按住防護、實際卸載重載。取消後無道具、預覽、指引或擁有權殘留。
- 完整專案回歸 `729f7dd0`：33/33 通過，428 秒，failed／skipped／inconclusive 均 0；包含桌面 PlacementTests／SelectionTests 與 S1／S2／S3／S4。S3 跟隨案例改為保留角色元件有效，符合 S4「角色停用需取消路由」的新規則，沒有略過既有行為。

## 限制與待確認

輸入測試使用 Input System 測試裝置；焦點／暫停取消使用回呼，與原生 OS Alt-Tab 不同。原生點擊聚焦 Game View 只證明建立測試環境，不能視為所有原生拖放、Tab／Esc 或焦點案例通過。代理實際檢視 Camera.Render 的 1280×720、1024×768、1720×720 離屏影像：四個項目完整可讀，透明預覽可辨識、金色線與圓環可見，與青色操作直線有別。使用暫時 Editor 方法直接建立預覽與指引，並非原生拖放或原生 Game View 比例／連續縮放驗證。獨立 Play Mode 進入 job 3e57e4bd，擷取後已退出；有效 Camera、AudioListener、EventSystem 各一。

S4 尚未獲使用者對放置精準度、18 單位門檻、透明度、圓環大小或操作舒適度的人工驗收。尚無 Windows 建置、Editor 外驗收、XR 或連續縮放結果。只到 S4，沒有 VR 選取／刪除、SceneMenu、返回入口、套件升級或建置設定修改；不推送、建立 PR 或合併。S0–S3 歷史 SHA、job 與使用者確認範圍維持原紀錄。


最終檢查：暫時工具與 meta 已經 AssetDatabase.DeleteAsset 移除。工具移除後編譯 succeeded=true、errorCount=0（2026-11-06T10:19:06.7530326Z）；獨立 Play Mode 0827c21c completed，healthy=true、errorCount=0，5 秒後已退出，VRSimulation isDirty=false。保護範圍開始／結束 SHA-256 全數一致，包含 Main、兩份 TerrainData、建置設定、共享資產與原有五個設定；後者完整排除功能提交。Git 空白檢查排除 Unity 序列化空值欄位的行尾空白，其餘通過。
資產參照檢查：場景新增 GUID 全數解析至本專案 meta，其他 GUID 與既有場景參照集合相符；暫時工具不存在。新增腳本、測試及三份材質 meta 成對，兩份 TerrainData 的既有 LFS 規則與內容保持不變。

## 使用者試玩與 PR 授權補記（2026-11-06）

適用功能提交 f8033dba911314db42b4a06cadca0ba6bc6ea5aa。使用者回報「目前 play 測試正常」，並要求提交 PR 及審查，記錄為 S4 Editor Play Mode 整體試玩確認。本補記更新目前人工確認與推送授權狀態；上述初次交付時的限制及未推送敘述保留為歷史，不代表此次沒有 PR 授權。

使用者未逐項回報放置精準度、門檻、透明度、圓環、舒適度、原生焦點與縮放等案例，不將整體確認擴大成逐項驗收，也不包含 Windows 或 XR。本次文件補記沒有重跑 Unity 測試，原有 job 與診斷保持原意。依 AGENT.md 請求 GitHub Codex Review，以繁體中文審查並確認最新 head；本次沒有合併或 S5 授權。
