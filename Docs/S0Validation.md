# S0 獨立場景與地形副本驗證紀錄

日期：2026-11-06（Asia/Taipei）
階段：S0；基準 `d1d5f49`（PR #8），分支 `codex/vr-simulation`。本紀錄隨 S0 資產提交，同筆提交 SHA 以 Git 歷史為準。

## 實作範圍

- 新增 `Assets/SurfaceForge/Scenes/VRSimulation.unity` 與 `.meta`。
- 使用 Unity Editor `AssetDatabase.CopyAsset` 複製桌面 `WorkspaceTerrain.asset` 為 `Assets/SurfaceForge/Terrain/VRSimulationTerrain.asset`，由 Editor 產生獨立 `.meta`。
- `.gitattributes` 將 VR TerrainData 與桌面 TerrainData 一樣指定為 LFS 二進位資產，避免通用 `.asset` YAML 換行規則改動二進位內容。
- 場景僅四個根節點：`VR Simulation Terrain`、`Sun`、`Temporary Terrain Camera`、`EventSystem`。地形與燈光以既有 Main 元件為起點，明確重接 VR TerrainData；攝影機為全視窗固定透視視角，附單一 AudioListener，EventSystem 使用現有 InputSystemUIInputModule 與預設 UI actions。
- 地形尺寸 80 × 18 × 64 公尺、高度圖解析度 257，未雕塑或改變尺寸；TerrainLayer、貼圖、地形材質沿用共用資產且未修改。未新增道具資產或套件。
- 沒有桌面側欄、Canvas、螢幕空間刪除選單、TerrainOverviewCamera、操作狀態元件、跨場景單例或 DontDestroyOnLoad。

## 環境與方法

Unity 6000.3.25f1，現有 URP 17.3.0、Input System 1.20.0、UGUI。開始前明確讀取 AGENT.md、開發計畫、VR 規格與 README，確認遠端 main 仍為 `d1d5f49`，暫存區為空。Unity Skills registry 與 health 確認 Editor 綁定 `C:\Users\abon1125\unity-projects\SurfaceForge`，instance `SurfaceForge_409B7451`、port 8090、bypass/full；所有修改要求綁定該 instance。

透過暫時 Editor menu 工具呼叫 Unity API 建立及驗證資產，未手改場景或資產 YAML。工具與截圖產物已移除於 Assets 之外，不列入提交。以下為實際執行結果，不代表完整 VR 操作驗收。

## 實際檢查

| 檢查 | 結果與證據 |
| --- | --- |
| 編譯 | 匯入暫時工具的 compile job `0478027a` 完成，`hasErrors=false`、`errorCount=0`；移除工具後 compile job `ca8e8bdd` 亦完成，`hasErrors=false`、`errorCount=0` |
| 獨立啟動 | 只載入 VRSimulation，未載入 Main 或 SceneMenu；Play Mode job `84abbab9`、`6220f147` 均完成，兩次進入並退出 |
| 序列化參照 | 退出後從磁碟重新開啟 VRSimulation，再執行同一驗證通過；場景 `isDirty=false`、根節點數 4 |
| 地形參照 | Terrain 與 TerrainCollider 均指向 GUID `84f1dc098353f554995b824d4460383f`；桌面 TerrainData GUID `33de80dc870203844af81d64401c5899`，為不同資產／物件；Terrain 為 Surface layer（8） |
| 射線命中 | 編輯模式、Play Mode 與磁碟重載後，從地形正規化座標 (0.15, 0.15)、(0.5, 0.5)、(0.85, 0.85) 上方向下做 Surface Physics.Raycast，均 3/3 命中指定 TerrainCollider；左後邊界外 (-1, -1) 無命中 |
| 元件唯一性 | 編輯模式及 Play Mode 有效 Camera、AudioListener、EventSystem 各 1；沒有 Canvas 或 SurfaceForge 操作元件 |
| 畫面 | 實際檢視 Play Mode Game View 擷取影像，地形起伏、完整有限邊界及背景可見，無桌面 UI；擷取實際為 3840 × 2160，未以要求的截圖參數冒充其他視窗比例測試 |
| Console | 讀取持續歷史紀錄，7 筆既有 Error／Exception 均為 2026-11-05，沒有本次 S0 新增錯誤；未清除歷史紀錄 |
| 資產與 meta | 新場景、TerrainData 各有 Editor 產生的 meta；場景 7 個外部 GUID 均解析至專案或現有套件，另 2 個為 Unity 內建資源 GUID；LFS 指標 SHA-256 與 VR TerrainData 工作區二進位內容一致。保留 Unity 序列化輸出的行尾空白，Git 檢查排除此類空白後通過 |
| 原資產與設定 | 開始前後 SHA-256 比對：Main.unity、WorkspaceTerrain.asset、EditorBuildSettings.asset 與下列五個原有未提交檔案全部相同；Main 啟動與建置清單未變 |

保護並排除提交的原有變更：

- `Assets/Settings/DefaultVolumeProfile.asset`
- `Assets/Settings/PC_RPAsset.asset`
- `Assets/Settings/UniversalRenderPipelineGlobalSettings.asset`
- `ProjectSettings/ProjectSettings.asset`
- `ProjectSettings/SceneTemplateSettings.json`（未追蹤）

## 限制與後續

- 地形隔離依據為 Editor 物件／資產參照不同及桌面檔案雜湊未變；本次沒有實際雕塑、縮放或寫入高度後再復原的破壞性演練。共享 TerrainLayer、貼圖或材質若需調整仍須先建立 VR 專用副本。
- S0 只有固定檢視攝影機；角色貼地、碰撞、移動邊界須於 S1 前確認。沒有提前實作 S1–S5、世界空間 UI、射線互動、放置核心重構、指引線、選取刪除或 SceneMenu。
- 未重跑桌面 M1–M4 互動測試、全視窗比例及連續縮放測試；本次沒有修改桌面場景、腳本或共享資產。既有使用者人工驗收紀錄保持原意。
- 未執行 Windows 建置、Editor 外驗收、實機 XR、錄影交付、推送或 PR；I1–I2 仍為後續工作。
