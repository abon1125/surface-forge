# QA 前專案設定整理

> 2026-11-07 QA 現況：PR #18（I1）、#19（設定整理）已合併；S5／I1 的使用者整體正常回報保留原適用版本，不視為本次重驗。原「六檔排除」為歷史限制，已結束；舊 TaskCheckpoint 不再作為現況來源。本次 Unity 內測試由使用者執行，新增操作說明及選單高度的驗證見 [QABeforeI2.md](QABeforeI2.md)。設定整理後的重新開啟、畫面及儲存穩定性仍待確認。

日期：2026-11-07（Asia/Taipei）。基準為 PR #18 合併後的 `04dd677b3179b5f90889d063b5de3a448808acb0`，Unity 6000.3.25f1、URP 17.3.0。僅整理既有設定，不開始 QA 修正、I2、Windows 建置或 XR。

## 處理決策與依據

| 檔案 | 決策與依據 |
| --- | --- |
| `Assets/Settings/DefaultVolumeProfile.asset` | 保留 ProbeVolumesOptions 的零向量 worldOffset 與 Bloom 的 Gaussian filter 預設欄位。透過 `AssetDatabase.RemoveScriptableObjectsWithMissingScript`、`SaveAssetIfDirty` 移除 9 個失效子資產；保留原有 19 個有效元件及其參數。 |
| `Assets/Settings/PC_RPAsset.asset` | 保留既有 shader 預篩選資料。URP 的 `UniversalRenderPipelineAssetPrefiltering.UpdateShaderKeywordPrefiltering` 會從收集結果更新這些欄位，`ShaderBuildPreprocessor` 會保存它們；本次不觸發建置，也不另行修改畫質。 |
| `Assets/Settings/UniversalRenderPipelineGlobalSettings.asset` | 一般 Editor 儲存後 runtime 清單回到空值，與 Git 基準相同。原有 16 個 rid 皆能解析，但不是應永久保留的新功能設定：Core 的 `RenderPipelineGraphicsSettingsContainer.OnAfterDeserialize` 清空清單，`OnBeforeSerialize` 僅在 `BuildPipeline.isBuildingPlayer` 時填入裁切結果。 |
| `ProjectSettings/ProjectSettings.asset` | 保留已安裝 DOTween 的平台 defines。DOTweenEditor.dll 的 `DOTweenDefines` 宣告 DOTWEEN，`EditorUtils.AddGlobalDefine` 透過 PlayerSettings 寫入平台符號；正式 VR 面板亦使用 DG.Tweening。Standalone batching 的已生效值為 static=true、dynamic=false；這是現有設定，未據此宣稱它等於此 Unity 版本的原始預設。 |
| `ProjectSettings/SceneTemplateSettings.json` | 納入共用設定。JSON 可解析，22 個 dependencyTypeInfos 全為 userAdded=false，沒有固定範本、個人路徑或自訂依賴項目。 |
| `Docs/TaskCheckpoint.md` | 刪除未追蹤的舊 S5 交接文件；其最終提交、測試與限制已由 S5Validation 正式紀錄承接，執行中指令不可再當目前狀態。保留原檔備份，不加入永久忽略規則；未來真的暫停時依 AGENT.md 重新建立。 |

9 個失效子資產為 OutlineVolumeComponent、OasisFogVolumeComponent、CopyPasteTestComponent1／2／3、TestAnimationCurveVolumeComponent、VolumeComponentSupportedEverywhere、VolumeComponentSupportedOnAnySRP、TestVolume。它們均不在目前有效 components 清單中；掃描專案 asset／scene／prefab／material 沒有指向這些子資產的外部引用，Outline／OasisFog 腳本 GUID 在目前 Assets 與套件中也不存在。不新增或恢復已不存在的效果套件。

Standalone batching 的預設值來源無法證實，已向使用者明確說明；使用者確認「保留現況並提交」，因此將 static=true、dynamic=false 納入本次設定基準，沒有為了確認預設而重設 PlayerSettings。

## 已執行檢查與證據

- 六個原檔、SHA256 與原始 Git patch 已備份到忽略版控的 `Temp/SettingsCleanupBackup/`；此備份為本機資料，不隨提交散布。
- Unity Skills health 及唯讀資料確認專案路徑為 SurfaceForge、instance `SurfaceForge_409B7451`，操作時未進入 Play Mode。資產操作經 dry run 與 Editor API 完成，沒有手改 Unity YAML。
- API 讀回 Profile 的 missing script 子資產數由 9 降為 0；有效元件仍為同一組 19 個。Global Settings 沒有遺失的 managed reference 類型。
- 磁碟內容比對確認僅刪除 9 個失效子資產，剩餘 20 個序列化區塊（含 Profile 本體）與整理前備份完全相同；未更改有效元件參數。暫時 Editor 工具與 meta 已移除，原始碼僅留在本機備份。
- 場景範本 JSON、序列化引用與差異檢查已執行；Unity 產生的空值欄位行尾空白保留。
- Unity 外 `dotnet build SurfaceForge.Tests.csproj --no-restore --verbosity quiet` 與 `dotnet build Assembly-CSharp-Editor.csproj --no-restore --verbosity quiet` 均成功，各 0 警告、0 錯誤。這是 C# 編譯，不是 Test Runner 執行結果。

## 待使用者驗證

1. 重新開啟專案並等待匯入完成，確認 Console 沒有新增編譯／Missing Script 錯誤。
2. 從 SceneMenu 進入桌面與 VR 模擬各一次，確認地形、光照與後製外觀沒有變化，VR 左手面板展開／收合動畫正常。
3. 退出 Play Mode、儲存後檢查 Git：本次五份專案設定不應反覆出現相同的非預期差異。若重現，保留差異與觸發操作再排查；未以自動還原掩蓋原因。

上述重新開啟、畫面與儲存穩定性皆待使用者驗證。本次不執行 Unity Editor 測試、Play Mode、Windows 建置或效能驗收，不擴大既有 I1 的人工測試回報。
