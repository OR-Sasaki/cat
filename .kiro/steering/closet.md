---
inclusion: fileMatch
fileMatchPattern: "Assets/Scripts/Home/View/Closet*|Assets/Scripts/Home/Service/ClosetScrollerService*|Assets/Scripts/Home/State/ClosetOutfitData*|Assets/Scripts/Home/State/OutfitAssetState*|Assets/Scripts/Home/Starter/OutfitAssetStarter*|Assets/UI/Home/Closet/**|Assets/Arts/Character/Scripts/Outfit*|Assets/Arts/Character/Scripts/Outfits/*"
---

# Closet (クローゼット) 機能 実装ガイド

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

#### `Home.State.OutfitAssetState` (`Assets/Scripts/Home/State/OutfitAssetState.cs`)
- 役割: Addressables からロード済みの `Outfit` アセット (キャラクター用ScriptableObject) のキャッシュ。
- API:
  - `bool IsLoaded { get; set; }`
  - `event Action OnLoaded` — 全件ロード完了通知
  - `void Add(string name, Outfit outfit)`
  - `Outfit Get(string name)` — 未ロードは null
  - `IReadOnlyDictionary<string, Outfit> GetAll()`
  - `void NotifyLoaded()` — `IsLoaded = true` + `OnLoaded` 発火
- 寿命: `HomeScope` で `Lifetime.Scoped`。

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
  - `Home.State.OutfitAssetState` (Addressablesキャッシュ)
- ライフサイクル:
  - `Start()` で `_closetUiView.OnOpen` に `Initialize` を、`_cellSelectedEvent` に `OnCellViewSelected` を購読。
  - `Initialize()` は Open 毎に呼ばれ、`scroller.Delegate = this` を再設定。`OutfitAssetState.IsLoaded` 済なら `LoadData()`、未ロードなら `OnLoaded` を1回だけ購読。
- データ構築 (`LoadData`):
  1. 既存 `_data` の `SelectedChanged` リスナを全クリア。
  2. `MasterDataState.Outfits` を順に走査し `OutfitAssetState.Get(masterOutfit.Name)` で実体取得 (null はスキップ)。
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

#### `Home.Starter.OutfitAssetStarter` (`IStartable`)
- 役割: Homeシーン起動直後に `MasterDataState.Outfits` を全件 Addressables ロードし `OutfitAssetState` に投入する。
- アドレス規約: `address = $"{masterOutfit.Type}/{outfitName}.asset"` (例: `Body/Body001.asset`)。
- 同時並列ロード: `_pendingLoadCount` をデクリメントし 0 になったら `_outfitAssetState.NotifyLoaded()`。
- マスターが空なら即 `NotifyLoaded()` (ClosetScrollerService 側のハンドラがフリーズしないように)。

#### `Home.Starter.HomeStarter` (`IStartable`)
- 役割: 起動時の装備適用。`OutfitAssetState.IsLoaded` を待って `ApplyDefaultOutfits` → `ApplyPlayerOutfits` を実行。
- `ApplyDefaultOutfits`: 新規ユーザー向け。`Resources/default_outfits.csv` (id,outfit_id) を読み、未装備の `OutfitType` のみデフォルトを Equip → 1度だけ Save。
- `ApplyPlayerOutfits`: 永続化済みの装備 (UserEquippedOutfitState) を `CharacterView.SetOutfit` で適用。

## DI 登録 (`Home.Scope.HomeScope`)

```csharp
// View — エディタ参照を Component 登録
builder.RegisterComponent(_closetUiView);
// State
builder.Register<HomeState>(Lifetime.Scoped);
builder.Register<OutfitAssetState>(Lifetime.Scoped);
// Service (DIのみ)
builder.Register<HomeStateSetService>(Lifetime.Scoped);
// EntryPoint (IStartable / IInitializable / ITickable)
builder.RegisterEntryPoint<OutfitAssetStarter>();
builder.RegisterEntryPoint<ClosetScrollerService>();
builder.RegisterEntryPoint<HomeViewService>();
builder.RegisterEntryPoint<HomeStarter>();
```

## データフロー (シーケンス)

### A. Homeシーン起動 → クローゼット使用可能になるまで

1. `HomeScope.Awake` (`SceneScope`) → `MasterDataImportService.Import()` で `MasterDataState.Outfits` 構築 (CSV)。
2. `OutfitAssetStarter.Start()` → 全 Outfit を Addressables 並列ロード → 完了で `OutfitAssetState.NotifyLoaded()`。
3. `HomeStarter.Start()` → ロード待ち (購読 or 即時) → デフォルト装備適用 + 既存装備適用 → `CharacterView.SetOutfit`。
4. `HomeViewService.Initialize()` → `HomeState.ForceSetState(Home)` → Home の View が Open。
5. `ClosetScrollerService.Start()` → `_closetUiView.OnOpen` を購読 (Closet が開かれるたびに `Initialize()` を呼ぶ)。

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
- `default_outfits.csv` — 新規ユーザーへの初期装備。`id, outfit_id` (`outfit_id` はマスターの `name`)。
- `user_outfits.csv` — 参考 (現状コードからの直接参照なし)。

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
| `PlayerPrefsKey.UserItemInventory` | 所持 Outfit/Furniture | `UserItemInventoryService.Save()` (Shop購入等) — Closet からは未参照 |

`UserItemInventoryService` は `MasterDataState.IsImported` を待って Load し、`EnsureEquippedOutfitsOwned()` で「装備中のOutfitは強制的に所持済みにする」整合保証を行う。Closet から所持Outfitに絞った表示をする場合、この `IUserItemInventoryService.HasOutfit / GetAllOwnedOutfitIds` を導入してフィルタする (現状未実装)。

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
| State | `Home.State.OutfitAssetState` | `Assets/Scripts/Home/State/OutfitAssetState.cs` |
| State | `Home.State.HomeState` | `Assets/Scripts/Home/State/HomeState.cs` |
| Starter | `Home.Starter.OutfitAssetStarter` | `Assets/Scripts/Home/Starter/OutfitAssetStarter.cs` |
| Starter | `Home.Starter.HomeStarter` | `Assets/Scripts/Home/Starter/HomeStarter.cs` |
| Scope | `Home.Scope.HomeScope` | `Assets/Scripts/Home/Scope/HomeScope.cs` |
| Asset | `Cat.Character.Outfit` (abstract) | `Assets/Arts/Character/Scripts/Outfit.cs` |
| Asset | `Cat.Character.OutfitPart` | `Assets/Arts/Character/Scripts/OutfitPart.cs` |
| View | `Cat.Character.CharacterView` | `Assets/Arts/Character/Scripts/CharacterView.cs` |
| Root State | `Root.State.MasterDataState` | `Assets/Scripts/Root/State/MasterDataState.cs` |
| Root State | `Root.State.UserEquippedOutfitState` | `Assets/Scripts/Root/State/UserEquippedOutfitState.cs` |
| Root Service | `Root.Service.UserEquippedOutfitService` | `Assets/Scripts/Root/Service/UserEpuippedOutfitService.cs` (※ファイル名typo) |
| Root Service | `Root.Service.UserItemInventoryService` | `Assets/Scripts/Root/Service/UserItemInventoryService.cs` |

## 実装時の注意 (拡張・改修指針)

1. **データ追加のフロー**: 新Outfitを追加するなら `outfits.csv` (id/type/name) → Addressables に `{type}/{name}.asset` 配置 → 必要なら `default_outfits.csv` 追記、の3点。コード変更不要 (`MasterDataImportService` と `OutfitAssetStarter` が自動で追従する)。
2. **OutfitType を追加するなら** `OutfitType` enum に **アルファベット順で挿入** + `CharacterView.GetPartTypes` の switch に対応 PartType を追加 + 必要なら `OutfitPart` の `PartType` enum もアルファベット順で追加 + `OutfitPartOrderSetting` の `_partOrder` にも追加 (OnValidate で自動検査される)。
3. **Closet で所持/未所持を区別する場合** `IUserItemInventoryService.HasOutfit(uint)` を `ClosetScrollerService.LoadData()` の各イテレーションで呼び、`ClosetOutfitData` に `Owned` プロパティを追加 → `ClosetRowCellView` でロック表示。`UserItemInventoryService.OutfitChanged` を購読して動的に更新できる。
4. **タブ切替を実装する場合** `HeadingItem` / `TabItem` プレハブを使い、選択された `OutfitType` を `ClosetScrollerService` 側に通知 → `LoadData` のフィルタ条件として使用。EnhancedScroller の差分更新 (`ReloadData(scrollPositionFactor: 0)`) を呼ぶ。
5. **再描画の最適化**: 現状 `LoadData` は毎 Open ごとに全件再構築。マスター数が増えたら `ClosetOutfitData` のキャッシュ + `Selected` だけ書き換える方式に置換可能。
6. **キャンセル**: Closet 内に async は無いが、将来追加する場合は `CancellationToken` を末尾引数にとる (`tech.md` のUniTask規約)。
7. **コーディング規約再掲**: `private` 省略・`_camelCase`・`/// comment` 形式・`#nullable enable` (使う場合)・`[Inject]` を VContainer 注入コンストラクタへ。

## 既知の制限・TODO

- `CharacterView.SetOutfit` のリフレクション依存 (本人コメント済 TODO)。
- `Closet` から **所持判定をしていない**: 全マスター Outfit を表示する。Shop で購入したかどうかは `IUserItemInventoryService` に保持されているが Closet 側未連携。
- `HeadingItem.prefab` / `TabItem.prefab` は **未結線**: タブUI実装時に `ClosetUiView` への SerializeField 追加 + Service 側でフィルタロジック実装が必要。
- `EnhancedScroller` の `cellViewPrefab` 切替・行内セル数の動的化は未対応 (`NumberOfCellsPerRow` は固定)。
- `Outfit.Thumbnail` 未設定の Outfit を扱った場合 `Image.sprite = null` になる (Logは出ない)。マスター追加時の運用注意。
