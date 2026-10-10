# Desktop Cat

[English](README.md) | [繁體中文](README.zh-TW.md) | [日本語](README.ja.md)

程式碼使用 **Codex GPT6 Astra** 撰寫。

一位小小的桌面同事。大部分時間在睡覺，偶爾追逐你的滑鼠游標。

<img src="art/sprites/black-ascii-sticker/sleep/sleep_01.png" alt="頭上有小小 Z 字的睡覺貓咪貼紙" width="170">

Desktop Cat 會在工作列上方散步、探索視窗頂端、打盹，並和滑鼠游標玩耍。ASCII 只是內附的美術風格：程式使用 PNG 動畫影格，因此也能換成像素圖、手繪角色或其他風格。預設是黑字白底的圓角 ASCII 貼紙，另外也附有透明背景的白字版本。

## 系統需求

Windows 10 或 11、x64、.NET Framework 4.8，以及桌面工作階段。不需要系統管理員權限、NuGet 套件或另行安裝 .NET SDK。目前已在開發用的 Windows 桌面驗證；其他 DPI 與工作列配置仍需測試。

## 把貓咪帶回家

1. 在 GitHub 選擇 **Code → Download ZIP** 下載，或使用 Git 複製儲存庫。也可以下載 Release 中的原始碼 ZIP。
2. 解壓縮至可寫入的資料夾，讓 `prototype`、`art` 資料夾與 **Start Cat.cmd** 保持在一起。
3. 按兩下 **Start Cat.cmd**。第一次啟動會自動編譯原始碼，之後只有程式碼更新時才會重新編譯。

啟動時會短暫出現主控台視窗。若要關閉寵物，請使用系統匣圖示選單中的 **Exit**。更新程式碼後若需要重新編譯，請先關閉正在執行的貓咪。

儲存庫與原始碼 Release ZIP 都不包含執行檔。啟動器會在本機的 `prototype/bin` 產生執行檔；Git 會忽略這個資料夾及所有其他 `.exe` 檔案。

在專案資料夾中使用 PowerShell 啟動：

```powershell
& '.\Start Cat.cmd'
```

啟動器的 PowerShell 執行原則設定只影響當次程序，不會更改已儲存的 PowerShell 原則。編譯失敗時，主控台會保持開啟並顯示錯誤。

## 和貓咪交朋友

| 摸摸我 | 陪我追逐 | 把我抱起來 | 跳到高處 |
| :---: | :---: | :---: | :---: |
| <img src="art/sprites/black-ascii-sticker/pet_happy/pet_happy_01.png" alt="被摸摸後開心的貓咪" width="120"> | <img src="art/sprites/black-ascii-sticker/paw_swipe/paw_swipe_02.png" alt="伸出爪子的貓咪" width="120"> | <img src="art/sprites/black-ascii-sticker/carried/carried_01.png" alt="被抱起、四肢垂下的貓咪" width="120"> | <img src="art/sprites/black-ascii-sticker/jump_rise/jump_rise_01.png" alt="跳向平台的貓咪" width="120"> |

- 點一下貓咪來摸摸牠；拖曳可以抱起，放開就會落下。
- 在貓咪身體高度附近輕輕移動游標，吸引牠注意、追逐或揮爪。在 **Personality** 中選擇 **Playful Kitten**，牠會更常玩耍。
- 在貓咪或系統匣圖示上按右鍵，可設定個性、選擇圖像集、睡覺／喚醒、暫停、跳躍或離開。按兩下系統匣圖示可讓貓咪回到工作列上方。
- 向上跳躍的目標必須是露出的平台，高度差小於貓咪身高的五倍，且有足夠空間落地。即使視窗全部最大化，工作區底部仍可供貓咪活動。

設定與診斷資料儲存在 `%LOCALAPPDATA%\DesktopCat`：`personality.json`、`sprite-set.txt` 和 `last-run.log`。第一次使用時會移轉既有的開發版設定。使用 Release 套件時，刪除兩個設定檔即可重設偏好。

## 幫寵物換個造型

<img src="art/sprites/black-ascii-sticker/look/look_01.png" alt="內附 ASCII 貼紙風格的貓咪" width="120">

準備自己的 **340 × 248 像素、透明背景 PNG 影格**。把內附圖像集複製到 `art/sprites/` 下的新資料夾，替換圖片，並在 `manifest.json` 設定 `name`。在貓咪或系統匣選單的 **Sprite set** 中選擇新造型。每次開啟下拉選單都會重新搜尋圖像集，選擇也會保留到下次啟動。不需要修改程式碼。

資料夾位置、對齊方式、動畫要求與驗證步驟，請參閱[自訂圖像集指南（英文）](art/CUSTOM_SPRITES.md)。可以繪製任何角色；目前角色的動作與個性仍採用貓咪行為。

## 驗證與原始碼打包

在專案資料夾中使用 PowerShell：

```powershell
& '.\Start Cat.cmd' --self-test
& '.\Start Cat.cmd' --cursor-test
& '.\Start Cat.cmd' --settings-test
& '.\Start Cat.cmd' --integration-test
& '.\Start Cat.cmd' --smoke-test
```

執行測試前，請先關閉一般執行中的貓咪。逐一執行各項測試，完成後查看 `%LOCALAPPDATA%\DesktopCat\last-run.log`。整合測試會短暫建立自己的測試視窗。

使用 `.\prototype\release.ps1` 建立原始碼 Release ZIP，輸出至 Git 忽略的 `dist/`。ZIP 包含啟動器、原始碼與圖像，不包含已編譯執行檔。將原始碼放在儲存庫，ZIP 則附加至 GitHub Release。

## 目前支援範圍

Desktop Cat 目前支援主要螢幕。更換顏色、多螢幕漫遊、全螢幕時隱藏、玩具與位置儲存仍在規劃中。混合 DPI、自動隱藏工作列、實際拖放、透明區域點擊穿透及焦點行為，仍需在更多裝置上手動測試。

功能與設計文件（英文）：[功能](FEATURES.md)、[個性](PERSONALITY.md)、[動畫對應](BEHAVIOR_ANIMATION_MAP.md)、[原型細節](prototype/README.md)。

目前尚未選定專案授權條款。
