---
inclusion: fileMatch
fileMatchPattern: "Assets/Scripts/Home/View/Closet*|Assets/Scripts/Home/Service/ClosetScrollerService*|Assets/Scripts/Home/Service/ClosetTabService*|Assets/Scripts/Home/State/ClosetOutfitData*|Assets/Scripts/Home/State/ClosetTabState*|Assets/Scripts/Home/State/MajorTab*|Assets/Scripts/Home/Starter/OutfitAssetStarter*|Assets/Scripts/Root/State/OutfitAssetState*|Assets/Scripts/Root/Service/OutfitAssetService*|Assets/Scripts/Root/Service/CharacterOutfitService*|Assets/Scripts/Root/Starter/CharacterOutfitStarter*|Assets/UI/Home/Closet/**|Assets/Arts/Character/Scripts/Outfit*|Assets/Arts/Character/Scripts/Outfits/*"
---

# Closet (クローゼット)

Home 内の着せ替え UI。`HomeFooterView` から `HomeState.State.Closet` へ遷移し、**所持している** Outfit だけをグリッド表示。セル選択で `CharacterView` へ即時適用し PlayerPrefs へ保存。2 階層タブ (大「体 / 服」+ 小 = `OutfitType`) は配線済み

## 流れ (View → Service → State)

- 中核は `ClosetScrollerService` (`IEnhancedScrollerDelegate` + `IStartable`)。`ClosetUiView.OnOpen` → `ClosetTabService.ResetToDefault()` (既定: 体 + Face) → `Initialize()` → `LoadData()` → `Scroller.ReloadData()`
- `LoadData` のフィルタ: `HasOutfit` (所持分) → `OutfitAssetState.Get` (ロード済み) → 選択中の小タブ (`ClosetTabState`)
- セル選択: 同 `OutfitType` の `Selected` を付け替え → `OutfitChangeEffectView.Play` → 雲が覆い切ったピークのコールバックで `CharacterView.SetOutfit` → `UserEquippedOutfitService.Equip + Save` → `SeId.OutfitEquip` (大タブが Body 以外はピッチを下げる)
- タブ: `ClosetTabState` が SSOT (`MajorTab` ↔ 小タブ集合、差分通知)、`ClosetTabService` が遷移ロジック (同値 no-op、大タブ切替で小タブを既定へ同期)。リセット中は `_suppressMinorReload` で二重 `LoadData` を抑止。View は `Closet{Major|Minor}TabsView` + `Closet{Major|Minor}TabItemView` (`HeadingItem.prefab` / `TabItem.prefab`)
- Outfit ロード基盤は Root 常駐 (シーン横断で見た目を共有)。`OutfitAssetService` が Addressables のロード / キャッシュを一元管理し、`LoadAllAsync` 完了で `OutfitAssetState.NotifyAllLoaded()` (マスター 0 件でも通知)。起動時の適用は `CharacterOutfitStarter` → `CharacterOutfitService.ApplyEquippedAsync`。`Home.Starter.OutfitAssetStarter` は全件先読みのトリガのみ

## データ追加

- **新 Outfit**: `Assets/Resources/outfits.csv` (id, type, name, display_name — display_name 空欄なら name で代替) に追記 → Addressables へ `{type}/{name}.asset` を配置 (`Body/Body001.asset`) → 必要なら `default_outfits.csv` (id, outfit_id = マスターの name) に追記。コード変更は不要。ただし一覧は所持分のみなので入手経路 (Shop / 初期アイテム) がないと出ない
- **OutfitType 追加**: enum は明示的な数値を持つ (`Body = 1` 〜 `Effect = 9`)。末尾に次番号で追加し、`CharacterView.GetPartTypes` の switch、`OutfitPart.PartType`、`OutfitPartOrderSetting._partOrder` (OnValidate で検査)、`MajorTab` のマッピングをセットで更新する。`FaceMakeup` / `Effect` は enum とタブマッピングのみで、具象クラス (`Outfits/*.cs`) は未作成

## ハマりどころ

- `Cat.Character.Outfit` (ScriptableObject) と `Root.State.Outfit` (マスター行) が同名。アセット側は常に完全修飾で書く
- `OutfitAssetState.OnAllLoaded` は Root 常駐イベント。シーン側で購読したら破棄時に解除する
- `CharacterView.SetOutfit` はリフレクションで `OutfitPart` 型の public フィールドを列挙し `SpriteRenderer` へ流す (TODO: 非リフレクション化)。描画順は `OutfitPartOrderSetting` の `sortingOrder = order * 100`
- 着せ替え演出は再生中に別セルを選ばれうる。`OutfitChangeEffectView` は保留中の差し替えコールバックを取りこぼさず、キャラのスケールを戻してから次を再生する前提。演出変更時はこの 2 点を壊さない
- `Outfit.Thumbnail` 未設定だとセル画像が無言で null になる (ログなし)
- クラス `UserEquippedOutfitService` の実体ファイルは `UserEpuippedOutfitService.cs` (typo)
- 永続化は装備 = `PlayerPrefsKey.UserEquippedOutfit`、所持 = `UserItemInventory`。`UserItemInventoryService.EnsureEquippedOutfitsOwned()` が「装備中は必ず所持済み」を保証
