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
**Key Services**: `SceneLoader`, `PlayerPrefsService`, `DialogService/IDialogService`, `DialogContainer`, `MasterDataImportService`, `UserDataImportService`, `UserEquippedOutfitService`
**Key States**: `MasterDataState`, `DialogState`, `UserState`, `UserEquippedOutfitState`, `SceneLoaderState`
**Key Views**: `DialogCanvasView`, `BackdropView`, `BaseDialogView` (継承ベースのダイアログ基底クラス), `CommonConfirmDialog`, `CommonMessageDialog`

### Utilities
**Location**: `Assets/Scripts/Utils/`
**Purpose**: 定数・ユーティリティクラス
**Example**: `Const.cs` (シーン名定数)

### Scene-Integrated Systems
**Pattern**: 大規模な機能もシーンフォルダ内の層 (Service/State/View) に統合
**Example**: IsoGrid機能は `Home/Service/IsoGridService.cs`, `Home/State/IsoGridState.cs`, `Home/View/IsoGridGizmo.cs`, `Home/View/FragmentedIsoGrid.cs` としてHomeシーンに統合。Closet/Redecorate機能もHomeシーン内のUI機能として統合
**Namespace**: `Cat` (プロジェクト共通) または `{SceneName}.{Layer}` (例: `Home.Service`)

### Dialog-based Feature Folders
**Pattern**: シーンではないがシーン構造に準じたフォルダ (State/View) を持つ機能
**Example**: `TimerSetting/` - ダイアログベースの設定機能。BaseDialogViewを継承し、独自のState/Viewを持つ

### Assets Organization
```
Assets/
├── AddressableAssetsData/  # Addressables 設定
├── Arts/                   # アート素材
├── Editor/                 # エディタ拡張
├── Fonts/                  # フォントアセット
├── ImportedAssets/         # 外部から取り込んだアセット
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

### Scene Constants
シーン名は必ず `Const.SceneName` で定義し、マジックストリングを排除

---
_Document patterns, not file trees. New files following patterns shouldn't require updates_
