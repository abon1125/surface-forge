# 合併提交預設標題恢復紀錄

日期：2026-11-06。使用者另行明確授權此次歷史整理，以免每次在 GitHub 合併時手動翻譯。

## 範圍與驗證

- 六筆先前中文化的合併標題恢復 GitHub 預設格式，合併內文不變；其他提交訊息全部保持原值。
- PR #8 的預設英文標題與內文不變，其 SHA 因祖先改寫而變更。原 GitHub 簽章因此失效並移除，原始簽章仍在備份。
- 逐筆驗證 tree SHA、作者與提交者身分及日期不變，父提交依對照轉換且順序不變；Git 完整性檢查通過。
- 新增的本機 VR S0 提交及其資產內容保留，僅更新祖先參照；該分支不推送至遠端。
- 工作區原有未提交檔案以 SHA-256、狀態及差異比對確認不變；本次未執行 Unity 測試或建置。
- 規則更新另建文件提交於 `codex/merge-message-policy`，透過 PR 交付，不混入既有功能提交。此文件及前次對照均為歷史快照，後續分支尖端以 Git 為準。

## 備份與復原

本機備份：`C:/Users/abon1125/.codex/visualizations/2026/11/06/01a1aeb2-adba-78a2-8163-f3319d6a9eeb/git-default-merges/`，不隨專案散布。`before.bundle` 保存此次整理前歷史；`old-local-refs.json` 與 `old-remote-refs.json` 保存參照；`working-files/`、`working-hashes.json`、`working.patch` 與 `index.patch` 保存工作區變更。

若需復原，先保存當下所有新工作，在新隔離目錄 clone 備份 bundle，依舊參照及下表確認目標；經另行授權後才使用逐分支明確當下 SHA 的 force-with-lease 與 atomic push 復原。不得無差別鏡像推送或丟棄原工作區。其他 session 或副本應重新確認分支與 SHA，不可直接合併新舊歷史。前次初始 SHA 須先查 [前次遷移紀錄](GitHistoryMigration.md)，再接續本表。

## 全部提交對照

| 此次整理前 SHA | 此次整理後 SHA | 提交標題 |
| --- | --- | --- |
| `df0154196c9157658e60a56b0269af740fc21e5b` | `df0154196c9157658e60a56b0269af740fc21e5b` | 初始入庫 |
| `410b9e8bceac48904b3fd4abd33e0c2b6edebe4a` | `410b9e8bceac48904b3fd4abd33e0c2b6edebe4a` | docs: 新增開發規範與本機檔案排除規則 |
| `71e3e66c0893c966580c0d7355f1d633582d8de4` | `71e3e66c0893c966580c0d7355f1d633582d8de4` | chore: 新增 Unity Skills 編輯器工具相依套件 |
| `a041d72063ca013bc43bcbf3194f3ce00b98a3a0` | `a041d72063ca013bc43bcbf3194f3ce00b98a3a0` | chore: 記錄 Unity 編輯器初始化變更 |
| `a7b63eb946d1d826617597d02d77b7eb0f7c1a46` | `a7b63eb946d1d826617597d02d77b7eb0f7c1a46` | chore: 透過 Git LFS 儲存範本圖示 |
| `a0e947d9ed5dfcb073fe2279aa75db126b359ad1` | `97c3880144094f23a44317e4be3490c9f3c698ad` | Merge pull request #1 from abon1125/codex/repository-setup |
| `656b4ad321a81355066d05eace379192b97b62ed` | `11aa86bd60f63dd75f9cae251131d7dce0db97e1` | feat: 新增地形場景與自適應版面 |
| `f0dba5fba3f3dec31695439acb55494c2dcb9c2a` | `845516fd5cd4db6b8e118d4e88680b169d6d4d59` | Merge pull request #2 from abon1125/codex/terrain-scene |
| `ab375b2877cddcd5680f4765ad4c66c9277cad2e` | `0b50920f3f7d2e6376bd058cec7bf942ef9ade02` | feat: 新增道具清單 |
| `56982d439476bc810a768e1b8e39eac5948f407c` | `2c2b08eadc4293c0244161030e60229880c6f34c` | Merge pull request #3 from abon1125/codex/prop-catalog |
| `fc5cd1a58d23e3a5139431c0a55151b278a92984` | `5afc1d4caca0301820d063ffec212b7373e3eef7` | feat: 實作道具放置 |
| `c0fdf2f67b663cc3484e7392e979d28067d6e8f4` | `73a850d754e1abecfa76db9d5e22eea1813b01dd` | fix: 依滑鼠按鍵隔離放置手勢 |
| `20b1228143d44286e10fcc891cb346c850c496db` | `2dae13ee2fbe7660c13e897e8720aafa86a3924f` | Merge pull request #4 from abon1125/codex/prop-placement |
| `15b6d104da21ad6a0af2bc39b21d3e6c27c1cf94` | `62fc4e300d6bae7c52b6e5e2c218748a5c3a8dad` | feat: 新增道具選取與刪除 |
| `31537a2aca3cfea451be67cd5297a9c914d6e125` | `1842eab75e15fe1587da990b4a59d957fb0bd33d` | fix: 保留批次處理的選取輸入事件 |
| `3e9059cee177d91aa6d0192a3329cd6ad57861cc` | `0668f166f592c9d9137c43f3cc86bf4bb7d92a3f` | fix: 新拖曳開始時取消延後處理的選取 |
| `550928d49c79d299a3651f31fab64c1d29cfd163` | `1e90ca5ba06bf7313c6d79ca28624d898b6ff272` | Merge pull request #5 from abon1125/codex/prop-selection |
| `9c127692a4e84c892a147d4f633c8d0f22594aff` | `86a3bdb464bdaa795e2e78cc51f4335826dbe32d` | docs: 納入開發計畫並更新文件規範 |
| `9fb9fba5bf80209a59b23e2ab073c59e0eb16a5f` | `bad8cf2a43ae8473318bf8f1094b29e4441cc1b5` | docs: 釐清還原計畫中的狀態快照 |
| `9335dbdc313e51e9686fdd4095381aef01421074` | `56153230f292a9f212f61241320cd2222bd30223` | chore: 忽略本機 Visual Studio 設定 |
| `3c9f679f31839e5c0fe7178dba98d46682e1e13a` | `594b42de6f0f7aa4f3b8ca80772c22d897939ae4` | Merge pull request #7 from abon1125/codex/development-docs |
| `71cca95bde383a1048fcf6cc8408f3c2fe861b1a` | `9255c6184c93456c02eda0496a4d908f4ce31778` | docs: 規劃獨立 VR 模擬場景與場景選單交付 |
| `c71540ab2f2613ca72ab76e8e7fef211dbb50dcf` | `8dd4b156dc36feca7118fd14a6faecc0c3d72c09` | docs: 記錄使用者桌面 Play Mode 驗收結果 |
| `3f1961bd3f2c1417afbd0bada6b00b3b2c67000e` | `0c04eac8f88f4560e589bc4b40427ff99a56e94a` | docs: 定義合併後的分支銜接流程 |
| `de8b411eafbc4789b7bea0660d8a37b45f75df3d` | `ae7676d4b2c9c813c9afb7d6c4c3bdd0e31dd6ba` | docs: 釐清目前進度與 VR 設計確認點 |
| `f8f7df4fd5365110cef0499adbf2d33585a87187` | `99a1f5e4d9281aed526706ef79b006788f3d17f2` | docs: 統一繁體中文提交規則並記錄歷史遷移 |
| `49734fa17201d990554f55f8e77529697378e3c2` | `78f7362286fd2bd7fc07326baf273315cdc1c46d` | docs: 將 README 翻譯為繁體中文 |
| `a9dd7ad028ff8449ef512aa32eb14396262c3342` | `1e7f3db4b4b2eb294ef08f5d5957f928889abcb0` | docs: 補齊文件一致性與繁體中文紀錄規則 |
| `d58caafe3a455086f299d43937df2a9e176ed505` | `e5efc3441b87b25ecc5e7ab70dc63b3691e2f0be` | docs: 將 M1 至 M4 驗證紀錄完整翻譯為繁體中文 |
| `d1d5f49adef5003c6961140875cf8892f0b0daf9` | `be014ceea3aa100ff5011d43dcde7fb7fbb9b521` | Merge pull request #8 from abon1125/codex/vr-simulation-plan |
| `9620c5b0b256aea6964dca773d2a0cc3753918e0` | `401b46d2b0d194729b6a8486b35b95d43f33b53f` | feat: 建立 S0 獨立 VR 模擬場景與地形副本 |

## 分支對照（不含後續文件提交）

| 分支 | 整理前 SHA | 整理後 SHA |
| --- | --- | --- |
| `codex/development-docs` | `9335dbdc313e51e9686fdd4095381aef01421074` | `56153230f292a9f212f61241320cd2222bd30223` |
| `codex/prop-catalog` | `ab375b2877cddcd5680f4765ad4c66c9277cad2e` | `0b50920f3f7d2e6376bd058cec7bf942ef9ade02` |
| `codex/prop-placement` | `c0fdf2f67b663cc3484e7392e979d28067d6e8f4` | `73a850d754e1abecfa76db9d5e22eea1813b01dd` |
| `codex/prop-selection` | `3e9059cee177d91aa6d0192a3329cd6ad57861cc` | `0668f166f592c9d9137c43f3cc86bf4bb7d92a3f` |
| `codex/repository-setup` | `a7b63eb946d1d826617597d02d77b7eb0f7c1a46` | `a7b63eb946d1d826617597d02d77b7eb0f7c1a46` |
| `codex/terrain-scene` | `656b4ad321a81355066d05eace379192b97b62ed` | `11aa86bd60f63dd75f9cae251131d7dce0db97e1` |
| `codex/vr-simulation` | `9620c5b0b256aea6964dca773d2a0cc3753918e0` | `401b46d2b0d194729b6a8486b35b95d43f33b53f` |
| `codex/vr-simulation-plan` | `d58caafe3a455086f299d43937df2a9e176ed505` | `e5efc3441b87b25ecc5e7ab70dc63b3691e2f0be` |
| `main` | `d1d5f49adef5003c6961140875cf8892f0b0daf9` | `be014ceea3aa100ff5011d43dcde7fb7fbb9b521` |
