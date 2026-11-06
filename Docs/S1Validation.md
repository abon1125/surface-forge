# S1 膠囊、雙球、第一人稱攝影機與移動驗證紀錄

日期：2026-11-06（Asia/Taipei）

階段：S1；基準 `024ce4fbd6d133fb1de01e8dce85545bb122b9d2`，分支 `codex/vr-simulation-s1`。本文件隨 S1 提交，對應提交 SHA 以 Git 歷史為準。

## 開始狀態及確認

- 明確讀取 AGENT.md、DevelopmentPlan、VRSimulationDevelopment、S0Validation、README 及 Unity Skills 協定、Editor／Scene／Script／Test／Perception 模組。
- 已重新 `git fetch origin`；`git ls-remote origin refs/heads/main` 確認遠端為上述基準，提交訊息為 PR #10 合併，且 S0 功能提交 `b261524` 為其祖先。本機 main 已確認與 origin/main 相同，再建立全新 S1 分支。沒有沿用 S0 分支或改寫歷史。
- GitHub 連接器確認 PR #10 merged=true，但回傳 merge SHA `8a3ed97f21841f62089f04da3cfa6f29ea34494e` 與實際 Git 遠端不一致；本次基準以重新 fetch 及 ls-remote 的實際遠端為準，不沿用連接器中的舊 SHA。
- 工作區原有四個已追蹤設定變更與一個未追蹤 SceneTemplateSettings.json；暫存區原為空。全程保留，逐檔雜湊比對未變，排除 S1 提交。
- Unity Skills registry／health 確認 Editor 綁定 `C:\Users\abon1125\unity-projects\SurfaceForge`，Unity 6000.3.25f1，instance `SurfaceForge_409B7451`、port 8090、bypass/full；寫入均指定該 instance。
- 使用者在實作前確認：貼地、重力、不跳躍、CharacterController、45° 坡度上限、階差 0.25 m、有限 Terrain 內縮邊界、異常超界回復至最近有效地面。先寫入 VR 規格，再實作移動。

## 實作

- `SimulationPlayer` 集中明確 Inspector 參照、尺寸、移動、重力、邊界、觀看與球偏移參數。根節點單位縮放、負責水平轉向，ViewPivot 負責俯仰。
- 場景階層：SimulationPlayer → Body、ViewPivot；ViewPivot → Camera、LeftController、RightController。雙球固定相對位置並跟隨俯仰。未建立 MenuAnchor、RayOrigin、UI 或射線。
- 將 S0 暫時攝影機重新命名及歸入角色，取代固定視角；沒有第二套有效攝影機或 AudioListener。角色膠囊預設 ShadowsOnly，雙球無碰撞器，根節點 CharacterController 負責碰撞。
- WASD 依水平朝向移動，斜向輸入限制為單位長度；右鍵新按下且位於攝影機視窗內才進入觀看，啟動影格忽略滑鼠 delta，後續 delta 控制水平與俯仰。右鍵結束、中斷、停用及銷毀清理游標；中斷後仍按住右鍵須放開再按，不自動重鎖。
- 透過指定 TerrainCollider 向下射線取得地面，使用 CharacterController.Move 處理碰撞與重力，接地時加下坡黏地距離；陡坡朝上移動被阻止。依 Terrain 世界 XZ 範圍內縮 0.38 m，正常走到邊緣停止；超界或異常掉至地形下方時回復地面。
- 新增兩個專用 URP 球材質，沒有修改共享材質、TerrainLayer、貼圖、TerrainData 或套件。
- 場景及材質由暫時 Editor menu 工具呼叫 Unity API 建立／儲存／重載，未手改 YAML；該工具及其 meta 已透過 AssetDatabase 移除。截圖、雜湊及工具證據留在忽略版控的 Temp/S1Evidence，未納入提交。

## 真正執行的檢查

| 項目 | 實際結果／證據 |
| --- | --- |
| 編譯 | 初始 compile job `c934c86a`、測試更新 `831f0b24`、`b9c3a835`、`b3b65ae0` 均 completed，hasErrors=false；移除暫時工具後最後編譯 succeeded=true、errorCount=0（2026-11-06T05:23:46.5851989Z）；最後僅移除 BOM／行尾空白後，compile job `e057025e` 亦完成且 errorCount=0 |
| S1 自動測試 | EditMode 篩選 `SurfaceForge.Tests.SimulationPlayerTests`；首輪 `dbdf2b38` 2/2 通過。補入 ASD、合成緩坡與重力案例後 `afd2335f` 2/2 通過，failed=0、skipped=0、inconclusive=0，57 秒；每個測試真正進出 Play Mode |
| 移動與碰撞 | 測試原地貼地、水平轉向 90° 後前進、仰望仍水平移動、±75° 俯仰限制；完整高度的實體 BoxCollider 牆阻擋 CharacterController；使用 Input System 測試 Keyboard 驅動 WASD 四方向 |
| 重力與坡面 | 執行期間建立暫時 TerrainData 高度場，驗證高處受重力落到緩坡、上下緩坡及下坡持續貼地，另用超過 45° 的高度場測試不能向上爬；僅執行期替換參照，未寫入原 TerrainData |
| 邊界 | 四個角落逐一測試從地形外／下方回復、XZ 保持內縮範圍、向外斜走停止；測試呼叫正式 Move 路徑，不只測 clamp 算式 |
| 右鍵與游標 | Input System 測試 Mouse 經 Update 進入右鍵觀看、delta 水平轉向、放開後 Cursor=None 且 visible=true；直接呼叫 OnApplicationFocus(false/true) 測試清理及按住不重鎖；實際停用／重啟元件測試相同狀態；實際卸載 VR 場景後游標可見且不鎖定 |
| 獨立 Play Mode | `00c22d8a` 獨立觀察 30 秒後退出，healthy=true、errorCount=0；未載入 Main／SceneMenu。移除工具後另執行 `9b9102b7` 5 秒觀察並退出，healthy=true、errorCount=0 |
| 磁碟重載與參照 | 建立後儲存並重新 OpenScene，測試亦每次從磁碟載入；結束於 VRSimulation、isDirty=false。角色七個 Inspector 參照完整，階層與設計相符 |
| 元件唯一性 | Edit Mode 與 30 秒 Play Mode 中以 Editor API 檢查有效 Camera／AudioListener／EventSystem 各 1；無額外 UI 或桌面控制器。最後讀回 CharacterController 高 1.8、半徑 0.3、center=(0,0.9,0)、slopeLimit=45、stepOffset=0.25、skinWidth=0.03 |
| 畫面 | 代理實際檢視 Play Mode 角色 Camera.Render 的 1280×720 離屏影像：左右球位於左右下方、地形可見、膠囊不遮擋主畫面。這不是原生 Game View 視窗比例或人工舒適度驗收 |
| GUID／meta | 場景 13 個不同 GUID：11 個解析至專案／現有套件，2 個為 Unity 內建資源；新增腳本、測試與兩個材質皆有 Editor 產生的 meta，未殘留暫時工具參照 |
| 地形 | Terrain 與 TerrainCollider 同指 VR TerrainData GUID `84f1dc098353f554995b824d4460383f`、Surface layer=8；VR 與桌面 TerrainData 本次前後 SHA-256 均相同，既有 LFS 規則未變 |
| 保護範圍 | Main.unity、桌面 TerrainData、EditorBuildSettings.asset、VR TerrainData 及以下五個原有未提交檔案前後 SHA-256 全數一致；差異未包含桌面程式、套件、共享資產或建置設定 |

保留且排除提交的原有檔案：

- Assets/Settings/DefaultVolumeProfile.asset
- Assets/Settings/PC_RPAsset.asset
- Assets/Settings/UniversalRenderPipelineGlobalSettings.asset
- ProjectSettings/ProjectSettings.asset
- ProjectSettings/SceneTemplateSettings.json（未追蹤）

## 初次實作時的診斷與限制（人工回報前）

- 本次 Console 歷史新增兩筆工具相關診斷，未清除歷史：建立場景時暫時 S1Authoring.Build 的 `SendMessage("OnValidate")` 觸發 `ShouldRunBehaviour()` assertion（2026-11-06T05:18:41.9061695Z）；其後磁碟重載、參數讀回與測試均正常，該工具已移除，不存在於交付內容。
- 2026-11-06T05:20:06.5124136Z 出現 `Failed to store screen shot (...Assets\\Screenshots\\S0Play.png)`；此路徑非本次主動要求的截圖路徑，觸發來源未定，不據此宣稱 Game View 截圖成功。本次另以角色 Camera.Render 成功寫入及檢視 Temp/S1Evidence/first-person.png。30 秒 Play Mode 觀察 job 的錯誤數為 0，不等於整個工作期間 Console 完全沒有診斷。既有 2026-11-05 的七筆診斷仍保留。
- 輸入為 Input System 測試裝置；失焦為生命週期回呼測試，未實際操作 OS Alt-Tab。未以鍵鼠人工連續遊走、檢查原生視窗失焦／回焦及游標感受；未驗證其他系統同時占用游標。使用者尚未對 S1 人工驗收。
- 觀看舒適度、球尺寸／偏移、速度與靈敏度需使用者在 Editor 實際試玩確認。建議檢查右鍵俯仰、WASD 上下坡、四邊停止、右鍵放開及切換視窗時游標是否符合預期。
- 地形保持 S0 高度與尺寸，測試未破壞性修改共享地形。碰撞牆及坡面測試為暫時執行期測試環境，場景未新增測試障礙物。
- 未重跑桌面 M1–M4 互動、完整比例縮放、Windows 建置、Editor 外驗收或 XR。既有桌面使用者驗收及 S0 歷史 SHA／證據保持原意，不記成此次重跑。
- 僅完成 S1；沒有 S2–S5、SceneMenu、返回入口、主場景啟動／建置設定變更、推送或 PR。


## 使用者 Editor Play Mode 人工驗收補記（2026-11-06）

適用功能提交：`e0426817d24df2358989b43d4d8d3f26cfd0c80f`。以下依使用者實際回報記錄，補充並取代上節「尚未人工驗收」的目前狀態；代理未於此次文件更新重跑測試，原有 job、SHA 及工具診斷保持不變。

| 項目 | 使用者回報 |
| --- | --- |
| WASD 與右鍵觀看 | 移動與轉動視角正常 |
| 上下坡 | 檢查正常；目前場景沒有陡坡，陡坡人工測試略過 |
| 地形邊界 | 檢查正常 |
| 俯仰限制 | 檢查正常 |
| 游標恢復 | 檢查正常 |
| 停止與重新執行 | 檢查正常 |
| 觀看舒適度 | 先維持現狀，日後有需求再調整；本次未調整參數 |

S1 已獲使用者對上述 Editor Play Mode 項目的人工確認。未另行回報各子案例的重複次數、解析度或焦點切換操作細節，不擴大推定覆蓋範圍。陡坡仍只有先前合成高度場的自動測試證據，沒有人工測試通過紀錄；Windows 建置、XR、S2–S5 及場景選單仍未執行或實作。
