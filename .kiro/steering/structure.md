# Project Structure

## Organization Philosophy

**Scene-based architecture**: 各シーンが独立したフォルダ構造を持ち、機能ごとに明確に分離。依存性注入により疎結合を保ちながら、共通サービスはRootScopeで一元管理。

## Directory Patterns

### Scene Structure
**Location**: `Assets/Scripts/{SceneName}/`
**Purpose**: 各シーンの機能を標準化された層に分割
**Pattern**:
```
{SceneName}/
├── Manager/   # ビジネスロジック・オーケストレーション
├── Scope/     # VContainer依存性注入設定
├── Service/   # サービス層実装 (ビジネス操作)
├── Starter/   # IStartable実装 (エントリーポイント)
├── State/     # 状態管理 (データ)
└── View/      # MonoBehaviour UI/ビュー実装
```

### Root Services
**Location**: `Assets/Scripts/Root/`
**Purpose**: 全シーン共通のグローバルサービス (RootScope)。シーンと同じ層構造 (Service/State/View) を持つ
**Key Services**: `SceneLoader`, `PlayerPrefsService`, `DialogService/IDialogService`, `DialogContainer`, `MasterDataImportService`, `UserEquippedOutfitService`, `OutfitAssetService`, `CharacterOutfitService`, `UserPointService/IUserPointService`, `UserItemInventoryService/IUserItemInventoryService`, `InitialItemService`, `UserFurnitureInstanceService`, `TimerRecordService/ITimerRecordService`, `IRewardedAdService` (`EditorRewardedAdService` / `LevelPlayRewardedAdService`), `IClock` (`SystemClock`)
**見た目のシーン横断共有**: Outfit アセットのロード/キャッシュ (`OutfitAssetService` + `OutfitAssetState`) と `CharacterView` への適用 (`CharacterOutfitService`) は Root に置き、`CharacterView` を持つシーンが `CharacterOutfitStarter` を `RegisterEntryPoint` して呼び出す。未装備部位のデフォルトは `default_outfits.csv` から補完する
**所持アイテムの単一ソース**: `UserItemInventoryService` (PlayerPrefs・数量ベース)。初回起動時は `InitialItemService` が `initial_furnitures.csv` / `default_outfits.csv` から解決した初期アイテムのみを付与する。IsoGrid が要求する個体単位の `UserFurnitureId` は `UserFurnitureInstanceService` が所持数から決定論的に採番する (`FurnitureId * SlotStride + slot + 1`)
**Key States**: `MasterDataState`, `DialogState`, `UserState`, `UserEquippedOutfitState`, `OutfitAssetState`, `UserPointState`, `UserItemInventoryState`, `TimerRecordState`, `SceneLoaderState`
**Key Snapshots**: `UserPointSnapshot`, `UserItemInventorySnapshot`, `TimerRecordSnapshot` (状態系サービスのイミュータブル戻り値)
**Key Configs**: `RewardedAdConfig` (`Resources/RewardedAdConfig.asset` からロードする ScriptableObject 構成)
**Key Views**: `DialogCanvasView`, `BackdropView`, `BaseDialogView` (継承ベースのダイアログ基底クラス), `CommonConfirmDialog`, `CommonMessageDialog`

### Utilities
**Location**: `Assets/Scripts/Utils/`
**Purpose**: 定数・ユーティリティクラス
**Example**: `Const.cs` (シーン名定数)

### Scene-Integrated Systems
**Pattern**: 大規模な機能もシーンフォルダ内の層 (Service/State/View) に統合
**Example**: IsoGrid機能は `Home/Service/IsoGridService.cs`, `Home/State/IsoGridState.cs`, `Home/View/IsoGridGizmo.cs`, `Home/View/FragmentedIsoGrid.cs` としてHomeシーンに統合。Closet/Redecorate機能もHomeシーン内のUI機能として統合
**Namespace**: `Cat` (プロジェクト共通) または `{SceneName}.{Layer}` (例: `Home.Service`)

### Testable Logic Assemblies
**Pattern**: 決定論的な純粋ロジックはシーンフォルダ配下の独立アセンブリ (`.asmdef`, `noEngineReferences: true`) に切り出し、`Tests/` サブフォルダに EditMode テストアセンブリ (`{Name}.Tests`, `includePlatforms: [Editor]`) を同居させる
**Location**: `Assets/Scripts/{Scene}/{Feature}Logic/`
**Example**: `Shop/RewardAdLogic/` — `JstDateHelper` / `RewardAdDailyCount` / `ShopProductCsvParser` (本体) + `Tests/*Tests.cs`。UnityEngine 非依存に保つことで純粋にユニットテスト可能

### Dialog-based Feature Folders
**Pattern**: シーンではないがシーン構造に準じたフォルダ (State/View) を持つ機能。使う層だけフォルダを作れば足りる (6層すべてを空フォルダで先置きするかは任意)
**Examples**:
- `TimerSetting/` - `BaseDialogView<TArgs>` (`IDialogWithArgs<TArgs>` 契約) を継承。6層フォルダを先置きし `State/TimerSettingData.cs` と `View/TimerSettingDialog.cs` のみを持つ
- `Menu/` - ホームのメニュー (設定) ダイアログ。`State/` と `View/` のみを作成し、`View/MenuDialog.cs` (引数なし `BaseDialogView` 継承) と再利用UI部品 `View/MenuSwitchView.cs` を置く
**Dialog の実体**: プレハブは `Assets/UI/Dialog/` に `{型名}.prefab` で置き、`BaseDialog.prefab` のネストプレハブとして作る。`Assets/UI/Dialog` 自体が Addressables のフォルダエントリ (address: `Dialogs`) なので、`DialogService` が組む `Dialogs/{型名}.prefab` キーへ自動で解決され個別登録は不要
**呼び出し側**: ダイアログを開くボタンはそのシーンの View 層に置き (例: `Home/View/HomeMenuButtonView.cs`)、`IDialogService.OpenAsync<TDialog>` を呼ぶ

### Thin Scenes
**Pattern**: 機能の薄いシーンも標準の6層フォルダを用意するが、必要な層のみにファイルを配置
**Example**: `Title/` - `Scope/TitleScope.cs` と `View/TitleStartButtonView.cs` のみ。他層は空。`Logo/` も `Scope` + `Starter` のみの薄いシーン
**Note**: 機能が育つと層が埋まる。`History/` は当初 thin だったが集中時間カレンダー機能 (State/Starter/Service/View 多数) を持つ通常のシーンへ成長した

### Assets Organization
```
Assets/
├── AddressableAssetsData/  # Addressables 設定
├── Arts/                   # アート素材
├── Editor/                 # エディタ拡張
├── Fonts/                  # フォントアセット
├── ImportedAssets/         # 外部から取り込んだアセット
├── LevelPlay/              # LevelPlay (ironSource) SDK
├── Plugins/                # サードパーティ (Demigiant/DOTween, DOTween Pro 等)
├── Resources/              # Resources.Load 対象 (DOTweenSettings 等)
├── Scenes/                 # .unityシーンファイル
├── Scripts/                # 上記の通り
├── Settings/
│   ├── VContainer/         # VContainerSettings.asset
│   └── UniversalRP.asset
├── Textures/               # スプライト・テクスチャ
└── UI/                     # UI関連アセット
```

## Naming Conventions

- **Files**: PascalCase - `SceneLoader.cs`, `HomeFooterView.cs`
- **Classes**: PascalCase - シーン名 + 役割 (例: `HomeScope`, `TitleStarter`)
- **Fields**: `_camelCase` (private), `PascalCase` (public)
- **Constants**: `PascalCase` (static class内)

## Import Organization

```csharp
// 標準的なインポートパターン
using Root.Service;   // Rootサービス
using Root.State;     // Root状態
using VContainer;     // VContainer本体
using VContainer.Unity; // IStartableなど
using UnityEngine;    // Unity標準
```

**Namespace Rules**:
- シーン名 + 役割: `Home.Service`, `Fade.Scope`, `Root.State`

## Code Organization Principles

### Dependency Rules (厳格)
```
View  →  Service  →  State
  ↓          ↓
Starter    Manager
```
- **許可**: 上位層 → 下位層
- **禁止**: 逆方向 (State → Service, Service → View)

### VContainer Registration Pattern
- **RootScope**: `Lifetime.Singleton` (全シーン共通)
- **SceneScope**: 抽象基底 `SceneScope` を継承し `Lifetime.Scoped` で登録。Awake時にMasterDataImport保証
- **EntryPoint**: `RegisterEntryPoint<TStarter>()` (IStartable)

### Scene View への注入 (2通り)
シーン上の MonoBehaviour が `[Inject]` を受け取る経路は2つあり、用途で使い分ける
- **他層から解決される View**: Scope の `[SerializeField]` + `builder.RegisterComponent(_view)` (例: `HomeUiView`, `ClosetUiView`)。Service 側がコンストラクタで受け取れる
- **自分で完結する View**: Scope コンポーネントの `autoInjectGameObjects` にそのGameObjectを登録し、View 側は `[Inject] public void Init(...)` を持つ (例: `Canvas/Home/Footer` の `HomeFooterView`、`Canvas/Home/MenuButton` の `HomeMenuButtonView`)。リストは親GameObject単位で再帰注入されるため、既に登録済みの祖先を重複登録しない
- **動的生成されるダイアログ**: `DialogContainer` が Instantiate 後に `IObjectResolver.InjectGameObject` を呼ぶので登録不要。`[Inject] public void Construct(...)` は `Awake` の後に走る

### Scene Constants
シーン名は必ず `Const.SceneName` で定義し、マジックストリングを排除

---
_Document patterns, not file trees. New files following patterns shouldn't require updates_
_更新: 2026-08-03 — Menu ダイアログ追加に伴い Dialog-based Feature Folders を整理 (プレハブ配置・Addressables キー規約を明記)・シーン View への注入経路 (RegisterComponent / autoInjectGameObjects / 動的注入) を追記・Outfit 系を Root サービスとして反映_
