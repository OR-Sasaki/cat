# Project Structure

## Organization Philosophy

**Scene-based architecture**: 各シーンが独立したフォルダ構造を持ち、機能ごとに明確に分離。依存性注入により疎結合を保ちながら、共通サービスはRootScopeで一元管理。

## Directory Patterns

### Scene Structure
**Location**: `Assets/Scripts/{SceneName}/`
```
{SceneName}/
├── Manager/   # ビジネスロジック・オーケストレーション
├── Scope/     # VContainer依存性注入設定
├── Service/   # サービス層実装 (ビジネス操作)
├── Starter/   # IStartable実装 (エントリーポイント)
├── State/     # 状態管理 (データ)
└── View/      # MonoBehaviour UI/ビュー実装
```
薄いシーンは必要な層のみにファイルを置けば足りる (例: `Title/` は Scope + View のみ)。機能が育てば層が埋まる (`History/` は thin から通常シーンへ成長した)。

### Root Services
**Location**: `Assets/Scripts/Root/` (シーンと同じ層構造)
- **登録の単一ソース**: `Root/Scope/RootScope.cs`。全シーン共通サービス (SceneLoader, DialogService, MasterDataImportService, ユーザー資産系, TimerRecordService, AudioService, IRewardedAdService, IClock など) を `Lifetime.Singleton` で登録。一覧はここに列挙せず現物を参照する
- **シーン横断の見た目共有**: Outfit のロード/キャッシュ (`OutfitAssetService` / `OutfitAssetState`) と適用 (`CharacterOutfitService`) は Root に常駐し、`CharacterView` を持つシーンが `CharacterOutfitStarter` を `RegisterEntryPoint` する。未装備部位は `default_outfits.csv` で補完
- **所持アイテムの単一ソース**: `UserItemInventoryService` (PlayerPrefs・数量ベース)。初回起動時の付与は `InitialItemService` のみ。個体単位の `UserFurnitureId` は `UserFurnitureInstanceService` が所持数から決定論的に採番 (`FurnitureId * SlotStride + slot + 1`)
- **DI 経路外への提供**: 動的生成ボタン等からは `AudioServiceHandle` の静的ブリッジで `IAudioService` に到達する (RootScope の `RegisterBuildCallback` で初期化)

### Utilities
`Assets/Scripts/Utils/Const.cs` — シーン名定数。シーン名は必ず `Const.SceneName` を使いマジックストリングを排除する

### Scene-Integrated Systems
大規模機能もシーンフォルダ内の層に統合する (IsoGrid / Closet / Redecorate はすべて `Home/` 内)。Namespace は `{SceneName}.{Layer}` (例: `Home.Service`) またはプロジェクト共通の `Cat`

### Testable Logic Assemblies
決定論的な純粋ロジックは `{Scene|Root}/{Feature}Logic/` の独立アセンブリ (`.asmdef`, `noEngineReferences: true`) に切り出し、`Tests/` サブフォルダに EditMode テストアセンブリ (`{Name}.Tests`, `includePlatforms: [Editor]`, `defineConstraints: [UNITY_INCLUDE_TESTS]`) を同居させる
**Examples**: `Shop/RewardAdLogic/` (`JstDateHelper`, `ShopProductCsvParser`, `RewardAdDailyCount`)、`Root/AudioLogic/` (`AudioVolumeLogic`, `SeSourcePicker`)

### Dialog-based Feature Folders
シーンではないがシーン構造に準じたフォルダを持つ機能。使う層だけ作れば足りる (例: `TimerSetting/`, `Menu/`, `DebugPanel/`)
- プレハブは `Assets/UI/Dialog/{型名}.prefab` に置き、`BaseDialog.prefab` のネストプレハブとして作る。`Assets/UI/Dialog` が Addressables のフォルダエントリ (address: `Dialogs`) なので個別登録不要
- UI をコード生成するダイアログ (デバッグ用途など) はプレハブを器 (RectTransform + CanvasGroup + 本体スクリプト) に留め、`[Inject] Construct` 内で組み立てる (`DebugPanelDialog` + `DebugUiFactory`)。`BaseDialogView` の `_animator` / `_closeButton` 未設定なら開閉アニメはスキップされる
- 開くボタンは各シーンの View 層に置き `IDialogService.OpenAsync<TDialog>` を呼ぶ

### Debug-Only Features
開発時のみの機能は RootScope の `RegisterEntryPoint` を `#if UNITY_EDITOR || DEVELOPMENT_BUILD` で囲み、リリースでは起点ごと消す。クラス本体は条件コンパイルしない (プレハブのスクリプト参照が切れるため)
**Note**: DebugPanel の透明ボタン (画面左上) は raycast を奪う。各シーン左上の UI と重ねない (Shop / History の戻るボタンは上端 90px 以降)。Canvas の sortingOrder は DialogCanvas (1000) より下に置く

### Root-Resident Overlay Effects
全シーン常駐の演出 (タップエフェクト等) は View のみの機能フォルダ + 専用 Overlay Canvas プレハブを `RootScope.prefab` にネストする。sortingOrder は最前面 (TapEffect 30000 > ダイアログ 1000+ > フェード 999)。GraphicRaycaster を付けず `raycastTarget = false` で入力を奪わない。全画面の押下検出は PassThrough `InputAction` の変化通知で拾う (ポーリングは同一フレーム押下→離しを取りこぼす)

### Assets Organization (非自明なもののみ)
- `Assets/Arts/Character/Scripts/` — キャラクター関連 (`CharacterView`, `Outfit` 系, `BlobShadowView` (足元の丸影))
- `Assets/Plugins/Demigiant/` — DOTween / DOTween Pro
- `Assets/Resources/` — `Resources.Load` 対象 (マスター CSV, `RewardedAdConfig`, `AudioRegistry` 等)
- `Assets/ImportedAssets/` / `Assets/LevelPlay/` — 外部アセット / SDK
- `Assets/Settings/VContainer/` — VContainerSettings.asset

## Naming Conventions

- **Files / Classes**: PascalCase、シーン名 + 役割 (`HomeScope`, `TitleStarter`)
- **Fields**: `_camelCase` (private) / `PascalCase` (public)、**Constants**: PascalCase (static class 内)
- **Namespace**: `{SceneName}.{Layer}` (`Home.Service`, `Root.State`)

## Dependency Rules (厳格)

```
View  →  Service  →  State
  ↓          ↓
Starter    Manager
```
上位層 → 下位層のみ許可。逆方向 (State → Service, Service → View) は禁止

## Scene View への注入 (3経路)

- **他層から解決される View**: Scope の `[SerializeField]` + `builder.RegisterComponent(_view)` (例: `HomeUiView`)。Service がコンストラクタで受け取れる
- **自分で完結する View**: Scope の `autoInjectGameObjects` に登録し、View 側に `[Inject] public void Init(...)` (例: `HomeFooterView`)。親 GameObject 単位で再帰注入されるため、登録済み祖先の子を重複登録しない
- **動的生成ダイアログ**: `DialogContainer` が Instantiate 後に `InjectGameObject` するので登録不要。`[Inject] Construct` は `Awake` の後に走る

---
_Document patterns, not file trees. New files following patterns shouldn't require updates_
_更新: 2026-08-23 — 全体縮小 (サービス等の網羅列挙を廃止し RootScope.cs 参照に変更)。audio-manager / blob-shadow を反映_
