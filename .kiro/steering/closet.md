---
inclusion: fileMatch
fileMatchPattern: "Assets/Scripts/Home/View/Closet*|Assets/Scripts/Home/Service/ClosetScrollerService*|Assets/Scripts/Home/Service/ClosetTabService*|Assets/Scripts/Home/State/ClosetOutfitData*|Assets/Scripts/Home/State/ClosetTabState*|Assets/Scripts/Home/State/MajorTab*|Assets/Scripts/Home/Starter/OutfitAssetStarter*|Assets/Scripts/Root/State/OutfitAssetState*|Assets/Scripts/Root/Service/OutfitAssetService*|Assets/Scripts/Root/Service/CharacterOutfitService*|Assets/Scripts/Root/Starter/CharacterOutfitStarter*|Assets/UI/Home/Closet/**|Assets/Arts/Character/Scripts/Outfit*|Assets/Arts/Character/Scripts/Outfits/*"
---

# Closet (クローゼット) 機能ガイド

Home シーン内の着せ替え UI。`HomeFooterView` のボタンで `HomeState.State.Closet` に遷移し、**所持している** Outfit のみをグリッド表示。セル選択で `CharacterView` に即時適用し PlayerPrefs へ保存する。2階層タブ (大タブ「体/服」+ 小タブ = `OutfitType`) は実装・シーン配線済み。

## 全体像 (View → Service → State)

- 中核は `Home.Service.ClosetScrollerService` (`IEnhancedScrollerDelegate` + `IStartable`)。`ClosetUiView.OnOpen` → `ClosetTabService.ResetToDefault()` (既定: 体 + Face) → `Initialize()` → `LoadData()` → `Scroller.ReloadData()`
- `LoadData` のフィルタ: `IUserItemInventoryService.HasOutfit` (所持分のみ) → `OutfitAssetState.Get` (ロード済みのみ) → 選択中の小タブ (`ClosetTabState`)
- セル選択 (`OnCellViewSelected`): 同 `OutfitType` の `Selected` を付け替え → `CharacterView.SetOutfit` で即時反映 → `UserEquippedOutfitService.Equip + Save` → 装備 SE (`SeId.OutfitEquip`。大タブが Body 以外はピッチを下げる)
- タブ: `ClosetTabState` が SSOT (`MajorTab` ↔ 小タブ集合のマッピング、差分通知)。`ClosetTabService` が遷移ロジック (同値 no-op、大タブ切替で小タブを既定値に同期)。リセット中は `_suppressMinorReload` フラグで二重 `LoadData` を抑止
- タブ View: `ClosetMajorTabsView` / `ClosetMinorTabsView` (コンテナ、`ClosetUiView` から参照) + `ClosetMajorTabItemView` / `ClosetMinorTabItemView` (`HeadingItem.prefab` / `TabItem.prefab` に配線済み)
- Outfit ロード基盤は Root 常駐 (シーン横断で見た目を共有するため): `OutfitAssetService` が Addressables ロード/キャッシュを一元管理し、`LoadAllAsync` 完了で `OutfitAssetState.NotifyAllLoaded()` (マスター0件でも通知)。起動時の装備適用は `CharacterOutfitStarter` → `CharacterOutfitService.ApplyEquippedAsync` (未装備部位は `default_outfits.csv` で補完)。`Home.Starter.OutfitAssetStarter` は全件先読みのトリガのみ
- DI: Root 系は `RootScope` (Singleton) 登録済み。Home 側の登録は `HomeScope.cs` を参照

## データ追加のフロー

- **新 Outfit**: `Assets/Resources/outfits.csv` (id,type,name) に追記 → Addressables に `{type}/{name}.asset` を配置 (例: `Body/Body001.asset`) → 必要なら `default_outfits.csv` (id, outfit_id=マスターの name) に追記。コード変更不要。ただし一覧は所持分のみ表示なので、入手経路 (Shop / 初期アイテム) がないと表示されない
- **OutfitType 追加**: enum は明示的な数値を持つ (`Body = 1` 〜 `Effect = 9`)。末尾に次の番号で追加し、`CharacterView.GetPartTypes` の switch、`OutfitPart.PartType`、`OutfitPartOrderSetting._partOrder` (OnValidate で検査)、`MajorTab` のタブマッピングをセットで更新する。※ `FaceMakeup` / `Effect` は enum とタブマッピングのみで、具象 Outfit クラス (`Outfits/*.cs`) は未作成

## ハマりどころ

- `Cat.Character.Outfit` (ScriptableObject。`Assets/Arts/Character/Scripts/`) と `Root.State.Outfit` (マスター行) が同名。アセット側は常に完全修飾で書く
- `OutfitAssetState.OnAllLoaded` は Root 常駐イベント。シーン側で購読したら破棄時に必ず解除する
- `CharacterView.SetOutfit` はリフレクションで `OutfitPart` 型の public フィールドを列挙して `SpriteRenderer` に流す (TODO: 非リフレクション化)。描画順は `OutfitPartOrderSetting` の `sortingOrder = order * 100`
- `Outfit.Thumbnail` 未設定だとセル画像が無言で null になる (ログなし)。マスター追加時に注意
- ファイル名 typo: `Root.Service.UserEquippedOutfitService` の実体ファイルは `UserEpuippedOutfitService.cs`
- 永続化: 装備 = `PlayerPrefsKey.UserEquippedOutfit`、所持 = `UserItemInventory`。`UserItemInventoryService.EnsureEquippedOutfitsOwned()` が「装備中は必ず所持済み」を保証

## 残タスク

- 2階層タブの自動テスト (`closet-two-level-tabs` tasks 8、任意) は未着手
- 一部 Closet 系ファイルが `/// <summary>` 形式のまま (tech.md 規約は `/// comment`)

---
_更新: 2026-08-23 — タブ配線完了を反映。実装スナップショット (クラス別詳細・シーケンス図・プレハブ内部構造) を削除して全面縮小_
