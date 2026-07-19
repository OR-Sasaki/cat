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
- **Ads**: LevelPlay (ironSource) SDK — リワード広告。実装 (`LevelPlayRewardedAdService`) は `IRewardedAdService` ポートで隠蔽し、SDK 型を上位層に露出させない

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

### Testing
決定論的な純粋ロジックは EditMode ユニットテスト (NUnit) で検証する。テスト対象は UnityEngine 非依存の独立アセンブリ (例: `Cat.Shop.RewardAdLogic`, `noEngineReferences: true`) に切り出し、`{Name}.Tests` アセンブリ (`includePlatforms: [Editor]`, `defineConstraints: [UNITY_INCLUDE_TESTS]`) から参照してテストする。日付・CSVパース・上限計算などはこのパターンで検証 (`JstDateHelper`, `ShopProductCsvParser`, `RewardAdDailyCount`)。時刻依存は `IClock` を注入して決定化する

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
- **RootScope**: 全シーン共通のシングルトンサービス (`SceneLoader`, `PlayerPrefsService`, `DialogService`, `DialogContainer`, `MasterDataImportService`, `UserDataImportService`, `UserEquippedOutfitService`, `UserPointService`, `UserItemInventoryService`, `TimerRecordService`, `IRewardedAdService`, `RewardedAdConfig`, `IClock` (`SystemClock`) など)。インターフェースを持つサービスは `.As<IXxx>().AsSelf()` で契約と実体の両方を解決可能に登録
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

### Platform-Conditional Service Registration
外部SDK依存サービスは抽象ポート (インターフェース) で隠蔽し、RootScope で `#if UNITY_EDITOR / #elif UNITY_ANDROID || UNITY_IOS` によって実装を切替える。例: `IRewardedAdService` は Editor で `EditorRewardedAdService` (スタブ)、実機で `LevelPlayRewardedAdService` を登録。上位層 (Shop) は SDK 型を参照しない

### Config-as-Asset (機密のコード排除)
App Key / Ad Unit ID などの構成値・機密はコードリテラルから排除し、`ScriptableObject` (`RewardedAdConfig`) として `Resources/RewardedAdConfig.asset` から `Resources.Load` する。キーはビルド時に env / ローカル `.env` から注入。アセット欠落時は RootScope で fail-fast (例外送出)

---
_Document standards and patterns, not every dependency_
_更新: 2026-07-19 — リワード広告SDK / EditMode テスト方針 / プラットフォーム条件付きDI を追記_
