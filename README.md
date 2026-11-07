# SurfaceForge

Unity 地形道具工作區，提供桌面操作與 VR 鍵鼠模擬。可拖出四種道具、預覽地形落點、放置、選取及刪除；VR 另有右球射線、左手面板與金色落點指引。

## 開始使用

- 環境：Unity **6000.3.25f1**、URP **17.3.0**、Input System **1.20.0**、DOTween 免費版 **1.3.045**。
- 開啟 `Assets/SurfaceForge/Scenes/SceneMenu.unity`，進入 Play Mode，選擇「桌面版驗收」或「VR 模擬驗收」。兩個操作場景也可直接啟動。
- 桌面左上、VR 左手面板下方提供返回入口。轉場經 Loading，重新進入會清除舊道具與操作狀態，不提供存檔或跨場景同步。

## 操作

| 模式 | 操作 |
| --- | --- |
| 桌面 | 右側 PROPS 展開清單；水平向左拖出道具，垂直拖曳／滾輪捲動；左鍵選取，DELETE 刪除 |
| VR 模擬 | WASD 移動；按住右鍵移動滑鼠觀看；Tab 開關左手面板；滑鼠瞄準右球射線，左鍵按住項目拖出，放開放置；滾輪捲動指向的清單 |
| 取消 | 在 UI 或地形外放開不放置；Esc 取消拖曳。VR 無拖曳時 Esc 清除選取並收合面板，不返回場景 |
| VR 選取 | 左鍵點選道具，射線點 DELETE 刪除。選單在物件前側偏上並隨觀看者水平繞行；右鍵觀看保留選取 |

VR 畫面頂端提供固定操作說明。放置底部錨點對齊地形命中位置與法線；允許道具重疊。失焦或中斷會清理未完成操作。VR UI 遵守場景遮擋，需移動至可見位置操作。

## 目前狀態

桌面 M1–M4、VR S0–S5、I1 場景選單及 additive Loading 已實作，已有使用者整體 Play Mode 正常回報；該回報不代表所有自動案例、比例與連續縮放通過。本次 QA 的操作說明與選單高度調整仍待使用者驗證。

I2 完整整合驗收與 Windows 建置、D1 錄影交付、實機 XR 尚未執行。專案設定整理後的重新開啟、畫面與儲存穩定性仍待確認。鍵鼠模擬不等於 XR 硬體驗證。

- [開發計畫與交付範圍](Docs/DevelopmentPlan.md)
- [VR 規格與可調參數](Docs/VRSimulationDevelopment.md)
- [本次 QA 與驗證步驟](Docs/QABeforeI2.md)
- [I1 驗證紀錄](Docs/I1Validation.md)／[設定整理紀錄](Docs/ProjectSettingsCleanup.md)
- 歷史驗證：`Docs/M1Validation.md`～`M4Validation.md`、`Docs/S0Validation.md`～`S5Validation.md`。歷史結果不視為目前版本重跑。

程式與資產位於 `Assets/SurfaceForge/`，測試位於 `Tests/Editor/`。模型、圖示與材質為本專案製作；DOTween 保留原作者授權，詳見 [DOTween 授權](https://dotween.demigiant.com/license.php)。中文字執行時使用作業系統字型，不隨專案散布。
