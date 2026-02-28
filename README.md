# MakeAssets
アセットの作成に使用する

## インベントリシステム

### ⚔ *ローグ系*

### 🍖 *サバイバル系*

- `ItemData` を作成（Project右クリック → `Create` → `Inventory` → `Survival Item`）
- `ItemDataBase` をシーンに配置し、`items` に `ItemData` を登録
- `Canvas` 内に `SurvivalInventoryCanvas` を作成
- `SU_SlotTemplate` を付与したスロットテンプレート
  - `container`/`item`/`count`/`selectionFrame` を割り当て
- `SU_InventoryManager` に `ItemDataBase` を割り当て
- `Groups` にスロット群を追加
  - `slotsContainer` / `slotTemplate` / `slotCount` / `slotMargin` / `maxWeight` / `weightText` / `initialItems`
- ドラッグ表示用に `Image` を作成し `dragIcon` に割り当て
- 選択情報の表示用に `Text` を作成し `selectedItemNameText` / `selectedItemWeightText` / `selectedItemDescriptionText` に割り当て

**操作**
- 左クリック：選択
- 右クリック：1個ずつ持ち上げ/配置
- 左ドラッグ：持ち上げて移動（上限まで配置、超過分は戻る）

### 🌳 *マイクラ系*

- `Canvas` 内に `SC_ItemCrafting` を配置
- `PlayerSlots` と `Items` にて画像を差し込み数量を変更する形で，アイテムの設定・スロットの設定・初期装備等．
- `Stackable` をオンでスタック可能
- ゲーム起動後にアイテムを配置し、`Craft` ボタンでコンソールにレシピが出力
- 出力されたレシピを `Items` のレシピ欄へ記入するとクラフト可能

**操作**
- 左クリック：まとめて持ち上げ/配置
- 右クリック：1個ずつ持ち上げ/配置
- `Craft` ボタン：レシピ出力

### 📱 *ソシャゲ系*

### ❗ *RPG系*

### 💻 *フリーゲーム系*

