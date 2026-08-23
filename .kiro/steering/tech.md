# Technology Stack

## Architecture

シーンベースアーキテクチャ + VContainerによる依存性注入パターン。各シーンが独立したLifetimeScopeを持ち、RootScopeから全シーン共通のサービスを利用。

## Core Technologies

- **Platform**: Unity 6 (6000.x.x) + Universal Render Pipeline (URP) 17.3.0
- **Language**: C# (.NET Standard 2.1)
- **DI Framework**: VContainer 1.17.0 (GitHub経由)

## Key Libraries

- **Async**: UniTask (async/await拡張、CancellationToken対応)
- **Tweening**: DOTween / DOTween Pro (`Assets/Plugins/Demigiant/`)。UniTask連携は `DOTweenAsyncExtensions`
- **Input System**: New Input System (Active Input Handling は新のみ)。UI は `InputSystemUIInputModule`、Home の家具ドラッグは `EnhancedTouch` ポーリング (`IsoInputService`)、全画面共通の押下検出は PassThrough `InputAction` (`TapEffectView`)
- **Audio**: 自作基盤 `IAudioService` (`AudioService`)。SE はプリロード・BGM はストリーム再生 (インポート設定)。クリップは `AudioRegistry` (ScriptableObject) に登録 (詳細は Audio System)
- **Asset Management**: Addressables
- **その他**: NavMeshPlus (2D NavMesh)、2D Animation、Cinemachine、Timeline
- **Ads**: LevelPlay (ironSource) SDK — `IRewardedAdService` ポートで隠蔽し、SDK 型を上位層に露出させない

## Development Standards

### Coding Conventions
- `private` は省略 (デフォルト)。private フィールドは `_fieldName`、コンストラクタでのみ初期化するなら `readonly`
- パターンマッチング推奨 — `while (asyncLoad is { isDone: false })`
- Nullable を使う場合はファイル先頭に `#nullable enable`
- Doc Comments は `/// <summary>` ブロックを使わず `/// comment` で記述
- UniTask 非同期メソッドは末尾引数に `CancellationToken` を受け取り、外部キャンセル可能にする
- VContainer が注入するコンストラクタには `[Inject]` を付与 (IL2CPP ストリッピング対策)
- MonoBehaviour から回す Tween は `SetLink(gameObject)` で寿命に紐付け、保持している Tween は再生前と `OnDestroy` で `Kill()` (例: `Menu.View.MenuSwitchView`)

### Error Logging
常にクラスコンテキスト付き:
```csharp
Debug.LogError($"[ClassName] {e.Message}\n{e.StackTrace}");
```

### Testing
決定論的な純粋ロジックは UnityEngine 非依存の独立アセンブリに切り出し、EditMode (NUnit) で検証する (配置ルールと実例は structure.md の Testable Logic Assemblies)。時刻依存は `IClock` を注入して決定化する

## Key Technical Decisions

### VContainer DI Pattern
- **RootScope**: `Lifetime.Singleton`。登録の全量は `RootScope.cs` を参照。インターフェースを持つサービスは `.As<IXxx>().AsSelf()` で契約と実体の両方を解決可能に登録
- **SceneScope**: 抽象基底クラス `SceneScope` を継承し `Lifetime.Scoped` で登録。Awake 時に MasterDataImport を保証
- **ITickable**: 毎フレーム更新が必要なサービスに採用 (例: `ShopService` の時限サイクル監視、`IsoInputService`)。コンストラクタ DI に加え `RegisterEntryPoint` も併用
- **IInitializable vs IStartable**: RootScope の `IInitializable` は Awake 中に同期実行され、プレハブ子 View (`AudioPlayerView` 等) の Awake より先に走る。子 View に触れる初期化は `IStartable` にする (例: `AudioSceneService`)

### Scene Transition System
Fade シーンを加算ロードし、FadeOut → ターゲットロード → FadeIn → Fade アンロードの順で遷移。`SceneLoader._isLoading` フラグで多重呼び出しを防止

### Time Abstraction
`DateTimeOffset.UtcNow` を直接呼ばず常に `IClock` (`SystemClock`) を経由する (テスト容易性・時限機能の決定論)

### State Snapshot Pattern
ユーザー資産系サービスは `UserPointSnapshot` のようなイミュータブルスナップショットを返すアクセサを提供し、View への参照整合性とテスト容易性を確保

### Platform-Conditional Service Registration
外部 SDK 依存はインターフェースで隠蔽し、RootScope で `#if UNITY_EDITOR / #elif UNITY_ANDROID || UNITY_IOS` により実装を切替える (例: `IRewardedAdService` は Editor でスタブ、実機で `LevelPlayRewardedAdService`)

### Config-as-Asset (機密のコード排除)
構成値・機密はコードリテラルから排除し、ScriptableObject として `Resources` からロードする。アセット欠落時は RootScope で fail-fast (例: `RewardedAdConfig`, `AudioRegistry`)。キーはビルド時に env / ローカル `.env` から注入

### Audio System
- `IAudioService` (`AudioService`) が BGM クロスフェード・SE 再生・音量/ON-OFF 設定の永続化 (PlayerPrefs) を担う。音量計算・SE ソース選択の純粋計算は `Root/AudioLogic` に分離
- `AudioSceneService` が `sceneLoaded` でシーン対応 BGM (`AudioRegistry` のシーン別割当) を再生し、`ButtonSeAttacher` でシーン内の Button に `ButtonSe` を自動付与する。個別ボタンの SE 上書き・無音化は `ButtonSe` を手置きして `SeId` を指定 (`None` で無音)
- `SeId` / `BgmId` enum は追加時に必ず末尾へ (シリアライズ済み値の保護)

---
_Document standards and patterns, not every dependency_
_更新: 2026-08-23 — 全体縮小 (structure.md との重複を排除)。Audio System (IAudioService / AudioRegistry / AudioLogic) を追記_
