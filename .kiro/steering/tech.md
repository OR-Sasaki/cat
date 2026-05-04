# Technology Stack

## Architecture

シーンベースアーキテクチャ + VContainerによる依存性注入パターン。各シーンが独立したLifetimeScopeを持ち、RootScopeから全シーン共通のサービスを利用。

## Core Technologies

- **Platform**: Unity 6 (6000.x.x)
- **Render Pipeline**: Universal Render Pipeline (URP) 17.3.0
- **Language**: C# (.NET Standard 2.1)
- **DI Framework**: VContainer 1.17.0 (GitHub経由)

## Key Libraries

- **Async**: UniTask (async/await拡張、UniTaskVoid、CancellationToken対応)
- **Tweening**: DOTween / DOTween Pro (`Assets/Plugins/Demigiant/`)。UniTaskとの連携 (`DOTweenAsyncExtensions`) を利用可能
- **Input System**: New Input System 1.19.0
- **Navigation**: NavMeshPlus (2D用NavMesh)
- **Animation**: 2D Animation 13.0.4, Cinemachine 3.1.5
- **Asset Management**: Addressables 2.9.1
- **Timeline**: Unity Timeline 1.8.11

## Development Standards

### Coding Conventions
- **Access Modifiers**: `private`は省略 (デフォルト)
- **Field Naming**: privateフィールドは `_fieldName` (アンダースコアプレフィックス)
- **Readonly**: コンストラクタでのみ初期化されるフィールドは `readonly`
- **Pattern Matching**: 推奨 - `while (asyncLoad is { isDone: false })`
- **Nullable**: 利用する場合はファイル先頭に `#nullable enable` を付与
- **Doc Comments**: `/// <summary>` ブロックは使わず `/// comment` で記述
- **UniTask**: 非同期メソッドは末尾引数に `CancellationToken` を受け取り、外部キャンセル可能にする
- **DI Constructors**: VContainerが注入するコンストラクタには `[Inject]` を付与 (IL2CPPでのストリッピング対策)

### Error Logging
常にクラスコンテキスト付き:
```csharp
Debug.LogError($"[ClassName] {e.Message}\n{e.StackTrace}");
```

### Scene Naming
シーン名定数は `Assets/Scripts/Utils/Const.cs` で管理

## Development Environment

### Required Tools
- Unity 6 with URP support
- Unity Hub

### Common Commands
```bash
# Unity起動: Unity Hubからプロジェクト選択
# 初期シーン: Assets/Scenes/Logo.unity
```

## Key Technical Decisions

### VContainer DI Pattern
- **RootScope**: 全シーン共通のシングルトンサービス (`SceneLoader`, `PlayerPrefsService`, `DialogService`, `DialogContainer`, `MasterDataImportService`, `UserDataImportService`, `UserEquippedOutfitService`, `UserPointService`, `UserItemInventoryService`, `IClock` (`SystemClock`) など)
- **SceneScope**: 抽象基底クラス `SceneScope` を継承。Awake時にMasterDataImportを保証。各シーンスコープ (`HomeScope`, `TitleScope`, `ShopScope`, `TimerScope`, `HistoryScope`, `LogoScope` など)
- **Lifetime**: `Singleton` (RootScope), `Scoped` (SceneScope)
- **ITickable**: VContainerの毎フレーム更新インターフェース。継続的な状態更新が必要なサービスに採用 (例: `ShopService` が時限ショップのサイクル監視に使用、`DialogContainer`、`Home/Service/IsoInputService`、`Home/Service/RedecorateCameraService`)。コンストラクタDIに加え `RegisterEntryPoint` も併用

### Scene Transition System
Fadeシーンを加算的にロードし、FadeOut → ターゲットロード → FadeIn → Fadeアンロードの順でシーン遷移を実行。`SceneLoader._isLoading`フラグによる多重呼び出し防止機構あり

### Time Abstraction
`IClock` (実装: `SystemClock`) 経由で `DateTimeOffset.UtcNow` を取得。テスト容易性および時限機能 (時限ショップのサイクル決定論など) の決定的計算のために `DateTimeOffset.UtcNow` を直接呼ばず常に `IClock` を経由する

### State Snapshot Pattern
`UserPointSnapshot`, `UserItemInventorySnapshot` のようにユーザー資産系サービスは「現在状態のイミュータブルなスナップショット」を返すアクセサを提供。Viewへ渡す際の参照整合性とテスト容易性を確保

### Dependency Direction (厳密なルール)
```
View → Service → State
  ↓        ↓
Starter  Manager  (Scope が全体を構成)
```
逆方向の依存は禁止 (例: State → Service)

---
_Document standards and patterns, not every dependency_
