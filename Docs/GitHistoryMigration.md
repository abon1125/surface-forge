# Git 歷史繁體中文遷移紀錄

日期：2026-11-06。使用者明確授權此次一次性改寫；後續仍遵循不任意重寫歷史的規則。

## 範圍與保留內容

將當時所有本機與遠端分支可達的 25 筆提交訊息統一為繁體中文，包含合併提交；「初始入庫」保持原值。處理 8 個分支，當時無標籤。保留標準提交前綴、技術識別字、作者及提交者身分與日期、每筆 tree SHA、父提交順序及合併關係。語言規則與本紀錄另以新文件提交加入，不混入重寫的既有提交。

既有 PR、審查留言與驗證文件原文保留。歷史文件內的舊 SHA 透過下表查詢；job ID 不更動。舊審查與 CI 結果不自動轉移至新 SHA，也不代表重新驗證。6 筆 GitHub 合併簽章無法沿用，重寫時移除；原始簽章可由備份 bundle 取得。

2026-11-06 後續規則更新：使用者要求紀錄統一為繁體中文，不再要求保留英文敘述；上述「原文保留」僅描述歷史改寫當時的範圍。已將已合併 PR #2、#3、#4、#5、#7 的標題與描述翻譯，PR #1 原已為繁體中文；README 亦已翻譯。其他既有紀錄可依權限翻譯，但須保留歷史事實、驗證證據、SHA、job ID、日期與連結。留言未因上述 PR 描述翻譯而修改，也不宣稱所有歷史文件已完成翻譯。

## 驗證與備份

- 在隔離 bare 副本重寫，逐筆比對 tree、作者、提交者及日期，確認父提交依對照表轉換且順序不變。
- Git 完整性、文件差異及本機 Markdown 連結檢查通過後，使用各分支的確切舊 SHA 作為 force-with-lease 租約，以 atomic push 一次更新明確列出的 8 個分支；若遠端有新提交或保護阻擋，停止更新並重新核對。
- 本次不執行 Unity 測試或建置；原工作區 Unity 設定與未追蹤檔案由位元組備份及 SHA-256 保護，不包含在文件提交中。
- 本機備份位置：`C:/Users/abon1125/.codex/visualizations/2026/11/06/01a1aeb2-adba-78a2-8163-f3319d6a9eeb/git-history-zh/`。此備份不隨儲存庫散布，應妥善保留。
- `before.bundle` 保存原始 Git 歷史與參照；`old-refs.json` 保存分支舊值；`working-files/`、`working-hashes.json`、`working-tree.patch` 與 `index.patch` 保存未提交變更；JSON 對照及驗證資料保存在同一目錄。

## 復原與其他副本

1. 需要復原時，先停止推送，備份復原當下的分支與未提交檔案；不可直接清除或強制重設目前工作區。
2. 用 `git clone --bare <備份目錄>/before.bundle <新的隔離目錄>` 檢查原始提交及分支；依 `old-refs.json` 及下表確認復原目標。
3. 若需復原遠端，先取得當下分支 SHA，確認沒有遷移後的新工作，再逐一明確指定 refspec 與當下 SHA 的 force-with-lease；使用 atomic push，勿使用無差別鏡像推送。復原屬另一項歷史改寫，需另獲授權。
4. 其他電腦或 worktree 先保存未提交及本機獨有提交，再重新 clone 或依對照遷移；不要直接合併舊、新歷史。未提交檔案只在確認目標及雜湊後按需從備份還原。
5. PR #8 保持未合併並核對新的 base/head；其討論保留原文，歷史行內審查可能顯示過時。未來 GitHub 合併時需手動確認繁體中文提交訊息。

## 完整提交對照

| 原 SHA | 改寫後 SHA | 繁體中文標題 |
| --- | --- | --- |
| `df0154196c9157658e60a56b0269af740fc21e5b` | `df0154196c9157658e60a56b0269af740fc21e5b` | 初始入庫 |
| `cef0e08a0fcc3c0818a953029da0cbf347fab9cf` | `410b9e8bceac48904b3fd4abd33e0c2b6edebe4a` | docs: 新增開發規範與本機檔案排除規則 |
| `8b25a339aa115a32536ddfbf1c4818f94a2d54b1` | `71e3e66c0893c966580c0d7355f1d633582d8de4` | chore: 新增 Unity Skills 編輯器工具相依套件 |
| `858dee5a9f7e8a6058c29ca53f857efd863cc015` | `a041d72063ca013bc43bcbf3194f3ce00b98a3a0` | chore: 記錄 Unity 編輯器初始化變更 |
| `a1f19245d0231842cea6ed01ab07f0fc520feef1` | `a7b63eb946d1d826617597d02d77b7eb0f7c1a46` | chore: 透過 Git LFS 儲存範本圖示 |
| `4b167b250e40d291087aa7286aa20911d7d7ec17` | `a0e947d9ed5dfcb073fe2279aa75db126b359ad1` | 合併 PR #1：納入 codex/repository-setup 分支 |
| `85c767a3f42f2c11d7ea9cf4d8ab25ef17fbf13f` | `656b4ad321a81355066d05eace379192b97b62ed` | feat: 新增地形場景與自適應版面 |
| `73ffede57d1322009dadd81ae7242923626141a7` | `f0dba5fba3f3dec31695439acb55494c2dcb9c2a` | 合併 PR #2：納入 codex/terrain-scene 分支 |
| `359bb93030e963f30e17b68f0d5c782f877e9265` | `ab375b2877cddcd5680f4765ad4c66c9277cad2e` | feat: 新增道具清單 |
| `8ae340d1e41eaa40dfca84d4acba9f96aed4d46b` | `56982d439476bc810a768e1b8e39eac5948f407c` | 合併 PR #3：納入 codex/prop-catalog 分支 |
| `41a3bb05ec987551d87671fb8148bc291c7f0470` | `fc5cd1a58d23e3a5139431c0a55151b278a92984` | feat: 實作道具放置 |
| `0e032b8e332c68a4c4a7d728ffaa5ab0110c5792` | `c0fdf2f67b663cc3484e7392e979d28067d6e8f4` | fix: 依滑鼠按鍵隔離放置手勢 |
| `0ec4d6cb089799d87956d5d0666f4b4b480df458` | `20b1228143d44286e10fcc891cb346c850c496db` | 合併 PR #4：納入 codex/prop-placement 分支 |
| `86a6c202c596705452b10cad3accbf08af909336` | `15b6d104da21ad6a0af2bc39b21d3e6c27c1cf94` | feat: 新增道具選取與刪除 |
| `58d6350d548666a003ab22bc0f7db5e622848d03` | `31537a2aca3cfea451be67cd5297a9c914d6e125` | fix: 保留批次處理的選取輸入事件 |
| `0a5ef40bd6da92c569593718734992ccb430a29e` | `3e9059cee177d91aa6d0192a3329cd6ad57861cc` | fix: 新拖曳開始時取消延後處理的選取 |
| `3ebc1b59ab69aba13e64a1ff9b3983eb37872e48` | `550928d49c79d299a3651f31fab64c1d29cfd163` | 合併 PR #5：納入 codex/prop-selection 分支 |
| `067b9c70888c37cc9264d738cee827f25801d1eb` | `9c127692a4e84c892a147d4f633c8d0f22594aff` | docs: 納入開發計畫並更新文件規範 |
| `aa370b74ed36dc289ea2d2e02f9328e2748db5ae` | `9fb9fba5bf80209a59b23e2ab073c59e0eb16a5f` | docs: 釐清還原計畫中的狀態快照 |
| `157e25e7800f1c6a583e71b2c9ba8c1a1c3cc866` | `9335dbdc313e51e9686fdd4095381aef01421074` | chore: 忽略本機 Visual Studio 設定 |
| `fbf892f97de672f059d70739fa824da4ef180d4c` | `3c9f679f31839e5c0fe7178dba98d46682e1e13a` | 合併 PR #7：納入 codex/development-docs 分支 |
| `9fe42e1c566116277cce9051427c16a96b882301` | `71cca95bde383a1048fcf6cc8408f3c2fe861b1a` | docs: 規劃獨立 VR 模擬場景與場景選單交付 |
| `b174788b6f330f5b2dfb73661c28e85221fc41fd` | `c71540ab2f2613ca72ab76e8e7fef211dbb50dcf` | docs: 記錄使用者桌面 Play Mode 驗收結果 |
| `8f67712242645da790db8b5ab21c77cee83279a3` | `3f1961bd3f2c1417afbd0bada6b00b3b2c67000e` | docs: 定義合併後的分支銜接流程 |
| `bc92725210095adab21243917a8d279d85cd0023` | `de8b411eafbc4789b7bea0660d8a37b45f75df3d` | docs: 釐清目前進度與 VR 設計確認點 |

## 分支對照

| 分支 | 原 SHA | 改寫後 SHA（不含後續文件提交） |
| --- | --- | --- |
| `codex/development-docs` | `157e25e7800f1c6a583e71b2c9ba8c1a1c3cc866` | `9335dbdc313e51e9686fdd4095381aef01421074` |
| `codex/prop-catalog` | `359bb93030e963f30e17b68f0d5c782f877e9265` | `ab375b2877cddcd5680f4765ad4c66c9277cad2e` |
| `codex/prop-placement` | `0e032b8e332c68a4c4a7d728ffaa5ab0110c5792` | `c0fdf2f67b663cc3484e7392e979d28067d6e8f4` |
| `codex/prop-selection` | `0a5ef40bd6da92c569593718734992ccb430a29e` | `3e9059cee177d91aa6d0192a3329cd6ad57861cc` |
| `codex/repository-setup` | `a1f19245d0231842cea6ed01ab07f0fc520feef1` | `a7b63eb946d1d826617597d02d77b7eb0f7c1a46` |
| `codex/terrain-scene` | `85c767a3f42f2c11d7ea9cf4d8ab25ef17fbf13f` | `656b4ad321a81355066d05eace379192b97b62ed` |
| `codex/vr-simulation-plan` | `bc92725210095adab21243917a8d279d85cd0023` | `de8b411eafbc4789b7bea0660d8a37b45f75df3d` |
| `main` | `fbf892f97de672f059d70739fa824da4ef180d4c` | `3c9f679f31839e5c0fe7178dba98d46682e1e13a` |
