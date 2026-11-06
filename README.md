# SurfaceForge

以 Unity 製作的桌面地形工作區，提供有限起伏地形、可拖曳的道具清單，以及道具選取與刪除功能。

- 開發環境：Unity 6000.3.25f1、URP 17.3.0、Input System 1.20.0。
- 開啟 `Assets/SurfaceForge/Scenes/Main.unity`，進入 Play Mode 即可操作。
- 右側清單初始收合。點擊 **PROPS / OPEN +** 展開可捲動清單，再次點擊即可收合。
- 攝影機會保留側欄空間，並隨可視區域變化調整地形取景。
- 目前共用建置場景清單以 Main 為啟動場景；桌面預設為可調整大小的 1280 × 720 視窗。

清單提供四種道具，各自使用本專案製作的圖示與不同的 URP 材質。清單項目依 Sidebar 的 `PropCatalogView` 明確指定的道具定義建立，且只建立一次。

- 將項目**水平向左拖曳**，超過 UI 拖曳門檻後即可複製道具。垂直拖曳用於捲動清單；初始方向決定整次手勢的用途，也可使用滑鼠滾輪捲動。
- 命中合法地形時顯示綠色預覽，底部錨點對齊命中位置，向上軸對齊地形法線。未命中地形時顯示紅色預覽；指標位於 UI 上時隱藏預覽。
- 在地形上放開可放置一個靜態道具；在任何 UI 上或地形外放開則取消。按 Esc、失去焦點、離開 Game View，或停用來源項目／控制器時也會取消。
- 原始清單項目會保留，可重複拖曳。已放置道具保留道具定義與模型碰撞器；允許彼此重疊，不跨執行階段保存放置結果。

- 以滑鼠左鍵點擊已放置道具即可選取，拖曳不視為點擊。點擊另一個可見道具可切換目標；點擊地形或場景空白處可清除選取；點擊 UI 則保留目前目標。
- 物件旁選單顯示所選道具名稱，點擊 **DELETE** 只會刪除該實例。選單隨目標與可視區域更新位置，保持在畫布內、側欄左方；目標在攝影機後方或可視區域外時隱藏。
- 開始清單拖曳（包含垂直捲動）、失去焦點，或停用選取元件／選單時會清除選取。放置結束的同一次放開不會額外觸發選取，必須重新點擊場景。

指引線與落點標記規劃於獨立 VR 模擬場景實作，不列入桌面場景。早期代理輸入測試的限制保留於驗證紀錄作為歷史觀察；使用者已於 2026-11-06 確認 Editor Play Mode 人工測試沒有問題。

鍵鼠 VR 模擬在 `Assets/SurfaceForge/Scenes/VRSimulation.unity` 逐階段開發，使用獨立 TerrainData 副本並共用道具資產。兩個場景完成後，才加入啟動場景選單，讓驗收者在同一個 Windows App 選擇桌面版或 VR 模擬版，並可返回選單。完整邊界驗收與正式建置延後到整合階段，各功能開發期間仍進行基本驗證。目前已完成 S0 獨立場景及地形副本與 S1 模擬角色，可直接開啟 `Assets/SurfaceForge/Scenes/VRSimulation.unity` 進入 Play Mode。WASD 依水平觀看方向貼地行走，按住右鍵移動滑鼠觀看，放開恢復游標；不跳躍，CharacterController 處理碰撞、45° 坡度限制及 Terrain 邊界停止。第一人稱畫面包含固定跟隨的左右小球，膠囊預設僅投射陰影。速度、尺寸、球偏移、靈敏度與俯仰限制可於 SimulationPlayer Inspector 調整。S1 已完成 Editor 自動測試及構圖檢查，已獲使用者 Editor Play Mode 人工確認（陡坡人工測試略過）；S2 已加入左球上方的 World Space 道具面板：Tab 每次新按下切換，Esc 關閉，初始／重啟／重載收合，失焦後需放開 Tab 再按。四種名稱與圖示共用既有資料，僅供顯示，尚無點選、拖曳或捲動。MenuAnchor 的 SimulationPropMenu 可調面板公尺偏移、畫布大小、縮放及朝向，不更動 S1 設定。S2 人工可讀性及操作舒適度待確認；S3–S5、道具互動及啟動選單尚未實作。實際結果與限制見 [S0 驗證紀錄](Docs/S0Validation.md)、[S1 驗證紀錄](Docs/S1Validation.md)及 [S2 驗證紀錄](Docs/S2Validation.md)。詳細內容請參閱[開發計畫](Docs/DevelopmentPlan.md)與 [VR 模擬規格](Docs/VRSimulationDevelopment.md)。鍵鼠模擬不等於 XR 硬體驗證。

| 穩定 ID／名稱 | 模型尺寸（公尺） | 模型碰撞器 |
| --- | --- | --- |
| `cube`／Cube（方塊） | 1.5 × 1.5 × 1.5 | BoxCollider |
| `sphere`／Sphere（球體） | 直徑 1.5 | SphereCollider |
| `cylinder`／Cylinder（圓柱） | 直徑 1.5 × 高 1.5 | 凸面 MeshCollider |
| `cone`／Cone（錐體） | 直徑 1.5 × 高 1.5 | 凸面 MeshCollider |

所有道具根節點均使用單位變換（位置與旋轉歸零、縮放為 1），放置錨點位於底部中心；子物件 `Model` 向上偏移 0.75 公尺。選取時會從模型子物件的碰撞器找到所屬 `PlacedProp` 根節點。來源 prefab 使用 Default layer；每個已放置實例的完整階層使用專用 PlacedProp layer，與 Surface 分開。道具不包含 Rigidbody。

`Assets/SurfaceForge/` 下的資產配置：

- `Prefabs/Props/`：Cube、Sphere、Cylinder 與 Cone 的 prefab。
- `Data/Props/`：對應的 `PropDefinition` 資產，包含 ID、顯示名稱、圖示與 prefab 參照。
- `UI/Prefabs/PropCatalogItem.prefab`：可重用的圖示／名稱清單項目，背景可接受射線查詢。
- `UI/Icons/` 與 `Materials/`：對應圖示與材質；`Models/Cone.asset` 保存本專案製作的封閉 32 邊錐體網格。
- `Scripts/`：道具清單、手勢路由、放置、TerrainCollider 查詢、已放置道具標記、選取與 UGUI 物件旁選單。Main 明確指定攝影機、遮罩、碰撞器、共用預覽材質及 UI 參照。預覽使用 Ignore Raycast layer 並停用碰撞器；選取查詢 PlacedProp layer，距離上限為 500 公尺，選取與地形查詢互不改變對方行為。
- `Tests/Editor/`：放置、選取及 S1 角色的測試（S1 可用 `SurfaceForge.Tests.SimulationPlayerTests` 篩選）。從 Test Runner 的 EditMode 分頁執行 `SurfaceForge.Tests`；測試會進入 Play Mode，並等待實際 PlayerLoop 影格。放置測試使用隔離場景，選取整合測試會開啟 Main，執行前請先儲存場景。Input System 測試滑鼠與 UGUI 事件呼叫不同於實際桌面輸入。

模型採用 Unity 基本形狀與本專案製作的錐體網格；圖示與材質配色皆為本專案製作，未加入外部美術素材。

已執行的檢查與限制請參閱 [M1 驗證紀錄](Docs/M1Validation.md)、[M2 驗證紀錄](Docs/M2Validation.md)、[M3 驗證紀錄](Docs/M3Validation.md)與 [M4 驗證紀錄](Docs/M4Validation.md)。使用者已於 2026-11-06 確認桌面 Editor Play Mode 人工驗收沒有問題，未另行記錄逐項覆蓋範圍與重複次數。目前尚未提供 Windows 執行檔或錄影；執行檔驗收及原生視窗連續縮放驗收，將在兩個場景完成後的場景選單整合階段執行。
