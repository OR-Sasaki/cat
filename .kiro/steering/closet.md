---
inclusion: fileMatch
fileMatchPattern: "Assets/Scripts/Home/View/Closet*|Assets/Scripts/Home/Service/ClosetScrollerService*|Assets/Scripts/Home/Service/ClosetTabService*|Assets/Scripts/Home/State/ClosetOutfitData*|Assets/Scripts/Home/State/ClosetTabState*|Assets/Scripts/Home/State/MajorTab*|Assets/Scripts/Home/Starter/OutfitAssetStarter*|Assets/Scripts/Root/State/OutfitAssetState*|Assets/Scripts/Root/Service/OutfitAssetService*|Assets/Scripts/Root/Service/CharacterOutfitService*|Assets/Scripts/Root/Starter/CharacterOutfitStarter*|Assets/UI/Home/Closet/**|Assets/Arts/Character/Scripts/Outfit*|Assets/Arts/Character/Scripts/Outfits/*"
---

# Closet (クローゼット) 機能 実装ガイド

> **⚠️ 更新中 (2026-08-03 時点)**: 2階層タブ UI (メジャー/マイナータブ) が別 spec `closet-two-level-tabs` で実装中です。`Home.State.ClosetTabState` / `Home.State.MajorTab`、`Home.Service.ClosetTabService`、`Home.View.ClosetMajorTabsView` / `ClosetMajorTabItemView` / `ClosetMinorTabsView` / `ClosetMinorTabItemView` が追加され (C# はコミット済み・DI 登録済み)、`ClosetScrollerService` もタブ連携を持ちます。プレハブ/シーン階層の配線 (tasks 7) と自動テスト (tasks 8) は未完。**本ガイド下部の「`HeadingItem`/`TabItem` は未結線」「タブ切替は未実装」といった記述はこの実装により置き換えられます。** タブ配線完了後に本ガイドの全面更新を推奨。
>
> また **Outfit のロード基盤は Root スコープへ移設済み**です。以下は現行の対応関係で、本文中の該当箇所は追記で補正してありますが、シーケンス図など細部は旧構成の記述が残っています。
> | 旧 | 現行 |
> | --- | --- |
> | `Home.State.OutfitAssetState` (`IsLoaded` / `OnLoaded` / `NotifyLoaded`) | `Root.State.OutfitAssetState` (`IsAllLoaded` / `OnAllLoaded` / `NotifyAllLoaded` / `Contains`) |
> | `Home.Starter.OutfitAssetStarter` が Addressables を直接叩く | `Root.Service.OutfitAssetService.LoadAllAsync` へ委譲 (Starter は先読みトリガのみ) |
> | `Home.Starter.HomeStarter` (デフォルト装備 + 起動時適用) | **削除**。`Root.Starter.CharacterOutfitStarter` → `Root.Service.CharacterOutfitService.ApplyEquippedAsync` が担当 (未装備部位のデフォルトは `default_outfits.csv` から補完) |
> | 全マスター Outfit を表示 | `IUserItemInventoryService.HasOutfit(id)` で所持分のみ表示 (実装済み) |

Homeシーン内の「服を着替える」UI機能。`HomeFooterView` のクローゼットボタン押下で `HomeState.State.Closet` に遷移し、所持しているOutfitアセットをグリッド表示する。セル選択でキャラクター (`CharacterView`) に即時適用 + `PlayerPrefs` に保存される。

## アーキテクチャ概要

シーン横断ルール (詳細は `structure.md` / `tech.md`) と同じ4層 + Starter で構成される。`Home.Service.ClosetScrollerService` を中核に、Master Data (`Root.State.MasterDataState`) と Addressables からロードされた `Outfit` (ScriptableObject) を、`EnhancedScroller` 上にグリッドレイアウトする。

```
[FooterView] ─click→ [HomeStateSetService] → [HomeState] ─OnStateChange→ [HomeViewService]
                                                                              │
                                                              ┌───────────────┴────────────────┐
                                                       Close 前View                     Open 新View (= ClosetUiView)
                                                                                              │ (Open)
                                                                                              ↓ OnOpen
                                                                                  [ClosetScrollerService.Initialize]
                                                                                              │
                                            ┌─────────────────────────────────────────────────┴─────────────────────┐
                                            ↓                                                                       ↓
                                      [OutfitAssetState] ─IsLoaded?→ LoadData()                              [EnhancedScroller]
                                            │                            │                                          ↑
                                            │       Master×UserEquipped から ClosetOutfitData[] 構築                 │
                                            │                            │                                          │
                                            └────────────────────────────┴────── ReloadData ───────────────────────┘
                                                                                              ↓ GetCellView
                                                                                  [ClosetCellView] (1行=N個)
                                                                                              │
                                                                              [ClosetRowCellView] × NumberOfCellsPerRow
                                                                                              │ (Button.OnClick → OnSelected)
                                                                                              ↓
                                                                          [ClosetScrollerService.OnCellViewSelected]
                                                                                  ├── CharacterView.SetOutfit
                                                                                  └── UserEquippedOutfitService.Equip → Save
```

依存方向は厳密に **View → Service → State**。Closet の Service (`ClosetScrollerService`) は Service 層から `UserEquippedOutfitService` を経由して `UserEquippedOutfitState` を更新し、View (`CharacterView`) を直接叩いて即時反映する。

## クラス詳細

### View 層

#### `Home.View.ClosetUiView` (`Assets/Scripts/Home/View/ClosetUiView.cs`)
- 役割: クローゼット画面のルートUI。`UiView` を継承し In/Out アニメーション (Timeline `ClosetIn.playable` / `ClosetOut.playable`) と `OnOpen` イベントを担う。
- `[SerializeField]`:
  - `Button _backButton` — Home へ戻るボタン
  - `EnhancedScroller _scroller` — グリッド本体
  - `EnhancedScrollerCellView _cellViewPrefab` — `CellView.prefab` (= `ClosetCellView`)
  - `int _numberOfCellsPerRow = 4` / `float _cellViewSize = 100f` / `float _bottomPadding = 50f`
- 公開API: `Scroller`, `CellViewPrefab`, `NumberOfCellsPerRow`, `CellViewSize`, `BottomPadding`
- DI: `[Inject] Init(HomeStateSetService)` で `_backButton.onClick` に `SetState(HomeState.State.Home)` を結線。
- 配置: `HomeScope` で `RegisterComponent(_closetUiView)`。ヒエラルキ上は `HomeScope` のSerializedField経由で参照。

#### `Home.View.ClosetCellView` (`Assets/Scripts/Home/View/ClosetCellView.cs`)
- 役割: `EnhancedScrollerCellView` を継承した「行」セル。1行 = `RowCellViews` に並ぶ複数 (現状3〜4) の `ClosetRowCellView` を束ねる (`CellView.prefab` の `RowCellViews` SerializedField)。
- API: `void SetData(ref SmallList<ClosetOutfitData> data, int startingIndex, UnityEvent<ClosetRowCellView> selected)`
  - `data[startingIndex + i]` を各 `RowCellViews[i]` に渡す。範囲外なら `null` を渡し非表示にする。
- 注意: `ref SmallList` で渡すのは EnhancedScroller の流儀。データの所有権は Service。

#### `Home.View.ClosetRowCellView` (`Assets/Scripts/Home/View/ClosetRowCellView.cs`)
- 役割: グリッドの個別セル。Outfitサムネイル + 選択枠の表示と、選択イベント発火。
- `[SerializeField]`:
  - `GameObject _container` — セル中身のオン/オフ
  - `Image _outfitImage` — サムネイル
  - `GameObject _selectedView` — 選択中の枠
- 公開: `int DataIndex`, `void SetData(int dataIndex, ClosetOutfitData data, UnityEvent<ClosetRowCellView> selected)`, `void OnSelected()`
- データバインド: `_data.SelectedChanged` (UnityEvent\<bool\>) を購読し、選択枠の表示を切替。`OnDestroy` 時に必ず解除。
- ボタンクリック → `OutfitCell.prefab` の Button OnClick で `OnSelected` を呼び、外部から渡された `_selected` UnityEvent を発火する (Service が購読)。

#### 連携する関連 View
- `Home.View.HomeFooterView` (`Assets/Scripts/Home/View/HomeFooterView.cs`)
  - クローゼットボタンクリック → `HomeStateSetService.SetState(HomeState.State.Closet)`。
- `Home.View.HomeViewService` 経由の `UiView` 切替対象に `_closetUiView` が含まれる。
- `Cat.Character.CharacterView` (`Assets/Arts/Character/Scripts/CharacterView.cs`)
  - `void SetOutfit(Outfit)` でリフレクションを使い `Outfit` の `OutfitPart` フィールドを `SpriteRenderer` にバインドし、`OutfitPartOrderSetting` で `sortingOrder = order * 100` を設定。
  - `void RemoveOutfit(OutfitType)` で対応 PartType の sprite を null 化。
- `Home.View.UiView` (基底) — `PlayableDirector + In/Out PlayableAsset` のアニメーション、`OnOpen` UnityEvent、`SetBlocksRaycast(bool)` を提供。`OnAnimationEnd` は発火後に自動 `RemoveAllListeners`。

### State 層

#### `Home.State.ClosetOutfitData` (`Assets/Scripts/Home/State/ClosetOutfitData.cs`)
- 役割: Closet UI 用に `Cat.Character.Outfit` を包む薄いラッパー。「選択中フラグ」を `UnityEvent<bool> SelectedChanged` で通知する。
- API: `Outfit Outfit { get; }`, `bool Selected { get; set; }` (差分のみ Invoke), `UnityEvent<bool> SelectedChanged`
- 寿命: `ClosetScrollerService._data` (`SmallList<ClosetOutfitData>`) が所有。`LoadData` 時に毎回 `RemoveAllListeners` → 新規構築する。

#### `Root.State.OutfitAssetState` (`Assets/Scripts/Root/State/OutfitAssetState.cs`)
- 役割: Addressables からロード済みの `Outfit` アセット (キャラクター用ScriptableObject) のキャッシュ。Home で着替えた見た目を Timer など他シーンでも再現するため **Root スコープに常駐**する。
- 名前空間の注意: 同じ `Root.State` にマスタデータの `Outfit` があるため、アセット側は常に `Cat.Character.Outfit` と完全修飾する。
- API:
  - `bool IsAllLoaded { get; }` — マスタ全件ロード済みか (装備分だけのロードでは true にならない)
  - `event Action? OnAllLoaded` — 全件ロード完了通知。**Root 常駐なので購読側はシーン破棄時に必ず解除する**
  - `void Add(string name, Cat.Character.Outfit outfit)`
  - `bool Contains(string name)`
  - `Cat.Character.Outfit? Get(string name)` — 未ロードは null
  - `IReadOnlyDictionary<string, Cat.Character.Outfit> GetAll()`
  - `void NotifyAllLoaded()` — `IsAllLoaded = true` + `OnAllLoaded` 発火
- 寿命: `RootScope` で `Lifetime.Singleton`。ロード済みアセットは意図的に Release しない。

#### `Home.State.HomeState` (`Assets/Scripts/Home/State/HomeState.cs`)
- Closet 関連で重要なのは `enum State { Home, Redecorate, Closet, Timer, Shop, History }` と `OnStateChange(prev, curr)`。
- `SetState` は同値遷移を弾き、`ForceSetState` は強制発火 (初期化時に `HomeViewService.Initialize` で `Home` に強制)。

#### 関連: `Root.State`
- `Root.State.MasterDataState.Outfits` : `Outfit[]` (id, type, name) — `MasterDataImportService` が `Resources/outfits.csv` から構築。
- `Root.State.UserEquippedOutfitState` — `Dictionary<OutfitType, uint>` を保持。`Equip / Unequip / GetEquippedOutfitId / GetAllEquippedOutfitIds`。
- `Root.State.UserItemInventoryState` — 将来の所持判定用 (現状の Closet は所持有無を判定していない / 全件表示)。

### Service 層

#### `Home.Service.ClosetScrollerService` (`Assets/Scripts/Home/Service/ClosetScrollerService.cs`)
- 役割: **Closet の中核**。`IEnhancedScrollerDelegate` + `IStartable` を実装。`HomeScope` で `RegisterEntryPoint<ClosetScrollerService>()` 登録。
- DI コンストラクタ依存:
  - `Cat.Character.CharacterView` (装備プレビュー)
  - `Home.View.ClosetUiView` (Scroller・設定値)
  - `Root.State.UserEquippedOutfitState` (装備済みID参照)
  - `Root.Service.UserEquippedOutfitService` (装備変更 + 永続化)
  - `Root.State.MasterDataState` (Outfitsマスター)
  - `Root.Service.IUserItemInventoryService` (所持判定)
  - `Root.State.OutfitAssetState` (Addressablesキャッシュ)
  - `Home.State.ClosetTabState` / `Home.Service.ClosetTabService` (2階層タブ連携)
- ライフサイクル:
  - `Start()` で `_closetUiView.OnOpen` に `OnOpen` を、`ClosetTabState.MinorChanged` に `OnMinorChanged` を、`_cellSelectedEvent` に `OnCellViewSelected` を購読。
  - `OnOpen()` は `ClosetTabService.ResetToDefault()` (この間の `MinorChanged` は `_suppressMinorReload` で抑止して二重 `LoadData` を防ぐ) → `Initialize()`。
  - `Initialize()` は Open 毎に呼ばれ、`scroller.Delegate = this` を再設定。`OutfitAssetState.IsAllLoaded` 済なら `LoadData()`、未ロードなら `OnAllLoaded` を1回だけ購読。
- データ構築 (`LoadData`):
  1. 既存 `_data` の `SelectedChanged` リスナを全クリア。
  2. `MasterDataState.Outfits` を順に走査し、`IUserItemInventoryService.HasOutfit(masterOutfit.Id)` が false のものはスキップ (**所持分のみ表示**)。さらに `OutfitAssetState.Get(masterOutfit.Name)` で実体取得 (null はスキップ)。選択中タブ (`ClosetTabState`) でも絞る。
  3. `UserEquippedOutfitState.GetAllEquippedOutfitIds()` を引き、対応 `OutfitType` の装備IDが `masterOutfit.Id` と一致するなら `Selected = true`。
  4. `Scroller.ReloadData()`。
- 選択処理 (`OnCellViewSelected`):
  - 同じ `OutfitType` の他データの `Selected` を false に倒し、選択行のみ true。
  - `_characterView.SetOutfit(selectedData.Outfit)` で即時反映。
  - `MasterDataState.Outfits.FirstOrDefault(o => o.Name == outfit.name)` で MasterId を引き、`UserEquippedOutfitService.Equip(type, id) → Save()`。
- IEnhancedScrollerDelegate 実装:
  - `DataRowCount = ceil(_data.Count / NumberOfCellsPerRow)`
  - `GetNumberOfCells = DataRowCount + 1` (末尾にダミー行=余白)
  - `GetCellViewSize` — 末尾は `BottomPadding`、それ以外は `CellViewSize`
  - `GetCellView` — `cellViewPrefab` から `ClosetCellView` を取得し `SetData(ref _data, di, _cellSelectedEvent)`。`di = dataIndex * cellsPerRow`。

#### `Home.Service.HomeStateSetService`
- ただの薄いラッパー。`HomeState.SetState(state)` を委譲。`ClosetUiView` の戻るボタンと `HomeFooterView` のクローゼットボタンが利用。

#### `Home.Service.HomeViewService` (`IInitializable`)
- 全 `UiView` (`Home`, `Closet`, `Redecorate`) の Open/Close を `HomeState.OnStateChange` で切替。
- `OpenView`: `gameObject.SetActive(true)` → `SetBlocksRaycast(true)` → `PlayAnimation(In)` → `Open()` (= `OnOpen.Invoke`、これが ClosetScrollerService.Initialize の起点)。
- `CloseView`: `PlayAnimation(Out)` → `SetBlocksRaycast(false)` → `OnAnimationEnd` で `gameObject.SetActive(false)`。

#### 関連: `Root.Service.UserEquippedOutfitService`
- `Equip(OutfitType, uint)` / `Unequip(OutfitType)` / `GetAllEquippedOutfitIds()` / `Save()` / コンストラクタで `Load()`。
- 永続化フォーマット: `UserEquippedOutfitData { UserEquippedOutfit[] Outfits { Type, OutfitId } }` を `PlayerPrefsService.Save(PlayerPrefsKey.UserEquippedOutfit, data)`。

### Starter 層

#### `Home.Starter.OutfitAssetStarter` (`IStartable`, `IDisposable`)
- 役割: **一覧表示用の先読みトリガ**。`Root.Service.OutfitAssetService.LoadAllAsync` を叩くだけで、Addressables を直接扱わない。
- `_cts` (`CancellationTokenSource`) を持ち `Dispose` でキャンセル。`OperationCanceledException` はシーン破棄の正常動作として無視し、それ以外は `[OutfitAssetStarter]` 付きで LogError。

#### 関連: `Root.Service.OutfitAssetService`
- Outfit アセットの Addressables ロードとキャッシュを一元管理 (`LoadAllAsync` / `LoadAsync(names)`)。
- アドレス規約: `address = $"{masterOutfit.Type}/{outfitName}.asset"` (例: `Body/Body001.asset`)。
- `LoadAllAsync` 完了時に `OutfitAssetState.NotifyAllLoaded()`。マスターが空でも通知する (一覧側が待ち続けないように)。

#### 関連: `Root.Starter.CharacterOutfitStarter` + `Root.Service.CharacterOutfitService`
- 起動時の装備適用は Root 側に集約されており、`CharacterView` を持つシーン (Home / Timer) が `RegisterEntryPoint<CharacterOutfitStarter>()` する。
- `CharacterOutfitService.ApplyEquippedAsync(characterView, ct)`: 装備分 + `default_outfits.csv` 分のアセットを `OutfitAssetService.LoadAsync` でまとめてロード → 未装備スロットにデフォルトを Equip → `CharacterView` へ適用。
- ※ 旧 `Home.Starter.HomeStarter` はこの2つに置き換わり削除済み。新規ユーザーへの**所持**アイテム付与は `Root.Service.InitialItemService` の役割 (装備の補完とは別)。

## DI 登録 (`Home.Scope.HomeScope`)

```csharp
// View — エディタ参照を Component 登録
builder.RegisterComponent(_closetUiView);
// State
builder.Register<HomeState>(Lifetime.Scoped);
builder.Register<ClosetTabState>(Lifetime.Scoped);
// Service (DIのみ)
builder.Register<HomeStateSetService>(Lifetime.Scoped);
builder.Register<ClosetTabService>(Lifetime.Scoped);
// EntryPoint (IStartable / IInitializable / ITickable)
builder.RegisterEntryPoint<OutfitAssetStarter>();
builder.RegisterEntryPoint<ClosetScrollerService>();
builder.RegisterEntryPoint<HomeViewService>();
builder.RegisterEntryPoint<CharacterOutfitStarter>();
```
※ `OutfitAssetState` / `OutfitAssetService` / `CharacterOutfitService` は `RootScope` 側で `Lifetime.Singleton` 登録済み。`HomeScope` では登録しない。

## データフロー (シーケンス)

### A. Homeシーン起動 → クローゼット使用可能になるまで

1. `HomeScope.Awake` (`SceneScope`) → `MasterDataImportService.Import()` で `MasterDataState.Outfits` 構築 (CSV)。
2. `OutfitAssetStarter.Start()` → `OutfitAssetService.LoadAllAsync` で全 Outfit を先読み → 完了で `OutfitAssetState.NotifyAllLoaded()`。
3. `CharacterOutfitStarter.Start()` → `CharacterOutfitService.ApplyEquippedAsync` (装備分 + デフォルト分をロード → 未装備スロットを補完 → `CharacterView` へ適用)。2 とは独立に走る。
4. `HomeViewService.Initialize()` → `HomeState.ForceSetState(Home)` → Home の View が Open。
5. `ClosetScrollerService.Start()` → `_closetUiView.OnOpen` を購読 (Closet が開かれるたびに `OnOpen()` → `Initialize()` を呼ぶ)。

### B. クローゼットを開く → セル表示

1. `HomeFooterView._closetButton.onClick` → `HomeStateSetService.SetState(Closet)`。
2. `HomeState.OnStateChange(Home, Closet)` → `HomeViewService.OnStateChange`:
   - 前 (`Home`) を Close (Out アニメ → SetActive(false))
   - 新 (`Closet`) を Open (SetActive(true) → In アニメ → `OnOpen`)
3. `_closetUiView.OnOpen` → `ClosetScrollerService.Initialize()` → `LoadData()` → `Scroller.ReloadData()`。
4. `EnhancedScroller` がコールバックで `GetNumberOfCells / GetCellViewSize / GetCellView` を呼び、`ClosetCellView` (1行) が生成され `SetData` で `ClosetRowCellView`s にデータを流し込む。

### C. セル選択 → 装備適用 + 永続化

1. `OutfitCell.prefab` 上のボタン → `ClosetRowCellView.OnSelected()` → `_selected.Invoke(this)` (= `_cellSelectedEvent`)。
2. `ClosetScrollerService.OnCellViewSelected`:
   - 同タイプ全件の `Selected` を再計算 → `ClosetOutfitData.SelectedChanged` 発火 → `ClosetRowCellView._selectedView.SetActive`。
   - `CharacterView.SetOutfit(outfit)` で即時 SpriteRenderer 反映。
   - `UserEquippedOutfitService.Equip(type, id) + Save()` → `PlayerPrefs` に永続化。
3. 戻るボタン → `HomeStateSetService.SetState(Home)` → 同様のアニメ遷移で Closet を Out。

## マスターデータ・アセット

### CSV (`Assets/Resources/`)
- `outfits.csv` — マスター。`id, type, name` (例: `1,Body,Body001`)。`type` は `Cat.Character.OutfitType` の文字列名 (アルファベット順: `Body, Cloth, Face, HandAccessory, HeadAccessory, LegAccessory, Tail`)。
- `default_outfits.csv` — 新規ユーザーへの初期装備。`id, outfit_id` (`outfit_id` はマスターの `name`)。`CharacterOutfitService` (未装備スロットの補完) と `InitialItemService` (初期所持の付与) の両方が参照する。

### Addressables 配置
- アドレス: `"{type}/{outfitName}.asset"` (例: `Body/Body001.asset`)
- 実体: `Cat.Character.Outfit` を継承した ScriptableObject (`Body / Cloth / Face / HandAccessory / HeadAccessory / LegAccessory / Tail` のいずれか)。

### `Cat.Character.Outfit` 階層
- `abstract class Outfit : ScriptableObject` (`Assets/Arts/Character/Scripts/Outfit.cs`)
  - `abstract OutfitType OutfitType { get; }`
  - `Sprite Thumbnail` — Closet グリッドに使用
- 具象 (`Assets/Arts/Character/Scripts/Outfits/`):
  - `Body` — BackFootPart / BackHandPart / BodyPart / FrontFoot / FrontFootLine / FrontHand
  - `Cloth` — ClothBackPart / ClothBodyPart / ClothCollarPart / ClothFrontPart
  - `Face` — FacePart
  - `HandAccessory` — HandAccessoryPart
  - `HeadAccessory` — HeadAccessoryPart
  - `LegAccessory` — LegAccessoryBackPart / LegAccessoryFrontPart
  - `Tail` — TailPart
- 各 `OutfitPart` (ScriptableObject) は `PartType` + `Sprite` を持つ。`PartType` は `BackFoot, BackHand, Body, ClothBack, ClothBody, ClothCollar, ClothFront, Face, FrontFoot, FrontFootLine, FrontHand, HandAccessory, HeadAccessory, LegAccessoryBack, LegAccessoryFront, Tail`。
- `CharacterView.SetOutfit` は **リフレクション** で `Outfit` 派生型の public フィールドを走査し、`OutfitPart` 型のフィールドのみ抽出して `SpriteRenderer` に流す (TODO: リフレクションを使わない設計に置換)。
- `OutfitPartOrderSetting` で `PartType` ごとの描画順を管理し、`spriteRenderer.sortingOrder = order * 100` を設定。

## 永続化

| キー | 内容 | 書き込み元 |
| --- | --- | --- |
| `PlayerPrefsKey.UserEquippedOutfit` | OutfitType ごとの装備 OutfitId | `UserEquippedOutfitService.Save()` (Closet 選択時) |
| `PlayerPrefsKey.UserItemInventory` | 所持 Outfit/Furniture | `UserItemInventoryService.Save()` (Shop購入・初期アイテム付与等) |

`UserItemInventoryService` は `MasterDataState.IsImported` を待って Load し、`EnsureEquippedOutfitsOwned()` で「装備中のOutfitは強制的に所持済みにする」整合保証を行う。Closet は `IUserItemInventoryService.HasOutfit(id)` で**所持分のみに絞って表示する (実装済み)**。

## Prefab 構成 (`Assets/UI/Home/Closet/Prefabs/`)

### `CellView.prefab` (= `ClosetCellView`)
- ルート: `HorizontalLayoutGroup` + `ClosetCellView (cellIdentifier=ClosetCellView)`
- 子: `OutfitCell` を `RowCellViews` 配列分インスタンス (現状3個)。
- 行高は `ClosetUiView.CellViewSize` で決定。

### `OutfitCell.prefab` (= `ClosetRowCellView`)
- ルート: `OutfitCell` (RectTransform 255x255, Mask, Button, ClosetRowCellView)
  - `Container` (`_container`) - 表示切替対象
    - `OutfitImage` (`_outfitImage`, Image, PreserveAspect=true) - サムネイル
    - `SelectionPanel` - Button のクリック判定 Image
    - `SelectedView` (`_selectedView`) - 選択枠
- Button.OnClick → `ClosetRowCellView.OnSelected` を直接呼ぶ。

### `HeadingItem.prefab` / `TabItem.prefab` (新規・コード未配線)
- 現状コードからの参照なし (将来の OutfitType タブ/見出し切替UI用)。
- HeadingItem: `Icon` 子 + Image + Button (背景: `dressup_heading.png`)
- TabItem: `Icon` 子 + Image (背景: `dressup_tab_open.png`)
- 利用する場合は `ClosetUiView` にタブ親 Transform / プレハブ参照を追加し、`ClosetScrollerService` 側で `OutfitType` フィルタ + `LoadData()` の再実行を実装する。

### Animations (`Assets/UI/Home/Closet/Animations/`)
- `ClosetIn.playable` / `ClosetOut.playable` — `UiView._playableDirector` に渡す In/Out Timeline。
- `BackButtonOut.anim` / `BackButtonOut_Reversed.anim` / `ContentOut.anim` / `ContentOut_Reversed.anim` — 上記 Timeline 内のクリップ。

## 型・名前のクイックリファレンス

| 種類 | 名前空間.型名 | パス |
| --- | --- | --- |
| View | `Home.View.ClosetUiView` | `Assets/Scripts/Home/View/ClosetUiView.cs` |
| View | `Home.View.ClosetCellView` | `Assets/Scripts/Home/View/ClosetCellView.cs` |
| View | `Home.View.ClosetRowCellView` | `Assets/Scripts/Home/View/ClosetRowCellView.cs` |
| Service | `Home.Service.ClosetScrollerService` | `Assets/Scripts/Home/Service/ClosetScrollerService.cs` |
| Service | `Home.Service.HomeStateSetService` | `Assets/Scripts/Home/Service/HomeStateSetService.cs` |
| Service | `Home.Service.HomeViewService` | `Assets/Scripts/Home/Service/HomeViewService.cs` |
| State | `Home.State.ClosetOutfitData` | `Assets/Scripts/Home/State/ClosetOutfitData.cs` |
| State | `Home.State.ClosetTabState` / `MajorTab` | `Assets/Scripts/Home/State/ClosetTabState.cs` / `MajorTab.cs` |
| State | `Home.State.HomeState` | `Assets/Scripts/Home/State/HomeState.cs` |
| Service | `Home.Service.ClosetTabService` | `Assets/Scripts/Home/Service/ClosetTabService.cs` |
| Starter | `Home.Starter.OutfitAssetStarter` | `Assets/Scripts/Home/Starter/OutfitAssetStarter.cs` |
| Scope | `Home.Scope.HomeScope` | `Assets/Scripts/Home/Scope/HomeScope.cs` |
| Root State | `Root.State.OutfitAssetState` | `Assets/Scripts/Root/State/OutfitAssetState.cs` |
| Root Service | `Root.Service.OutfitAssetService` | `Assets/Scripts/Root/Service/OutfitAssetService.cs` |
| Root Service | `Root.Service.CharacterOutfitService` | `Assets/Scripts/Root/Service/CharacterOutfitService.cs` |
| Root Starter | `Root.Starter.CharacterOutfitStarter` | `Assets/Scripts/Root/Starter/CharacterOutfitStarter.cs` |
| Asset | `Cat.Character.Outfit` (abstract) | `Assets/Arts/Character/Scripts/Outfit.cs` |
| Asset | `Cat.Character.OutfitPart` | `Assets/Arts/Character/Scripts/OutfitPart.cs` |
| View | `Cat.Character.CharacterView` | `Assets/Arts/Character/Scripts/CharacterView.cs` |
| Root State | `Root.State.MasterDataState` | `Assets/Scripts/Root/State/MasterDataState.cs` |
| Root State | `Root.State.UserEquippedOutfitState` | `Assets/Scripts/Root/State/UserEquippedOutfitState.cs` |
| Root Service | `Root.Service.UserEquippedOutfitService` | `Assets/Scripts/Root/Service/UserEpuippedOutfitService.cs` (※ファイル名typo) |
| Root Service | `Root.Service.UserItemInventoryService` | `Assets/Scripts/Root/Service/UserItemInventoryService.cs` |

## 実装時の注意 (拡張・改修指針)

1. **データ追加のフロー**: 新Outfitを追加するなら `outfits.csv` (id/type/name) → Addressables に `{type}/{name}.asset` 配置 → 必要なら `default_outfits.csv` 追記、の3点。コード変更不要 (`MasterDataImportService` と `OutfitAssetService` が自動で追従する)。ただし Closet は**所持分のみ表示**なので、新Outfitを一覧に出すには入手経路 (Shop / 初期アイテム) が必要。
2. **OutfitType を追加するなら** `OutfitType` enum に **アルファベット順で挿入** + `CharacterView.GetPartTypes` の switch に対応 PartType を追加 + 必要なら `OutfitPart` の `PartType` enum もアルファベット順で追加 + `OutfitPartOrderSetting` の `_partOrder` にも追加 (OnValidate で自動検査される)。
3. **未所持アイテムをロック表示したい場合** 現状は `HasOutfit` が false のものを `LoadData` で除外している。グレーアウトして見せるなら除外をやめ、`ClosetOutfitData` に `Owned` を追加 → `ClosetRowCellView` でロック表示。`UserItemInventoryService.OutfitChanged` を購読して動的更新もできる。
4. **タブ切替を実装する場合** `HeadingItem` / `TabItem` プレハブを使い、選択された `OutfitType` を `ClosetScrollerService` 側に通知 → `LoadData` のフィルタ条件として使用。EnhancedScroller の差分更新 (`ReloadData(scrollPositionFactor: 0)`) を呼ぶ。
5. **再描画の最適化**: 現状 `LoadData` は毎 Open ごとに全件再構築。マスター数が増えたら `ClosetOutfitData` のキャッシュ + `Selected` だけ書き換える方式に置換可能。
6. **キャンセル**: Closet 内に async は無いが、将来追加する場合は `CancellationToken` を末尾引数にとる (`tech.md` のUniTask規約)。
7. **コーディング規約再掲**: `private` 省略・`_camelCase`・`/// comment` 形式・`#nullable enable` (使う場合)・`[Inject]` を VContainer 注入コンストラクタへ。

## 既知の制限・TODO

- `CharacterView.SetOutfit` のリフレクション依存 (本人コメント済 TODO)。
- 2階層タブのプレハブ/シーン配線 (`closet-two-level-tabs` tasks 7) と自動テスト (tasks 8) が未完。C#・DI 登録は完了済み。
- `HeadingItem.prefab` / `TabItem.prefab` は **未結線**: タブUI実装時に `ClosetUiView` への SerializeField 追加が必要 (フィルタロジック自体は `ClosetTabService` / `ClosetTabState` 側に実装済み)。
- `EnhancedScroller` の `cellViewPrefab` 切替・行内セル数の動的化は未対応 (`NumberOfCellsPerRow` は固定)。
- `Outfit.Thumbnail` 未設定の Outfit を扱った場合 `Image.sprite = null` になる (Logは出ない)。マスター追加時の運用注意。
- Closet 系の一部ファイル (`ClosetOutfitData` / `ClosetCellView` / `ClosetRowCellView`) が `/// <summary>` 形式のまま。`tech.md` の Doc Comments 規約 (`/// comment`) に合わせて整理したい。

---
_更新: 2026-08-03 — Outfit ロード基盤の Root 移設 (OutfitAssetState/OutfitAssetService/CharacterOutfitService) と HomeStarter 削除を反映・所持判定が実装済みである点を修正・fileMatchPattern を追補_
