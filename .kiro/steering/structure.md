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
**Purpose**: 全シーン共通のグローバルサービス (RootScope)
**Example**: `SceneLoader`, `PlayerPrefsService`, `MasterDataState`

### Utilities
**Location**: `Assets/Scripts/Utils/`
**Purpose**: 定数・ユーティリティクラス
**Example**: `Const.cs` (シーン名定数)

### Standalone Systems
**Location**: `Assets/Scripts/{SystemName}/`
**Purpose**: シーンに依存しない独立システム（MonoBehaviourベース）
**Pattern**: シーンフォルダ構造 (Scope/Service/etc.) を使わず、機能単位でファイルを配置
**Example**: `IsoGrid/` - アイソメトリックグリッドシステム
**Namespace**: `Cat` (プロジェクト共通) または機能名

### Scene Template
**Location**: `Assets/Scripts/TemplateScene/`
**Purpose**: 新規シーン作成時のテンプレート

### Assets Organization
```
Assets/
├── Fonts/       # フォントアセット
├── Scenes/      # .unityシーンファイル
├── Scripts/     # 上記の通り
├── Settings/
│   ├── VContainer/  # VContainerSettings.asset
│   └── UniversalRP.asset
└── Textures/    # スプライト・テクスチャ
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
- **SceneScope**: `Lifetime.Scoped` (シーンローカル)
- **EntryPoint**: `RegisterEntryPoint<TStarter>()` (IStartable)

### Scene Constants
シーン名は必ず `Const.SceneName` で定義し、マジックストリングを排除

---
_Document patterns, not file trees. New files following patterns shouldn't require updates_
