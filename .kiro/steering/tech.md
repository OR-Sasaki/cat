# Technology Stack

## Core

- **Platform**: Unity 6 (6000.x) + URP 17.3.0 / C# (.NET Standard 2.1)
- **DI**: VContainer 1.17.0 (GitHub)。シーンごとの LifetimeScope + 共通サービスの RootScope
- **Async**: UniTask / **Tween**: DOTween・Pro (`Assets/Plugins/Demigiant/`、UniTask 連携は `DOTweenAsyncExtensions`)
- **Input**: New Input System のみ。UI は `InputSystemUIInputModule`、家具ドラッグは `EnhancedTouch` ポーリング (`IsoInputService`)、全画面の押下検出は PassThrough `InputAction` の変化通知 (`TapEffectView`) — ポーリングは同一フレームの押下→離しを取りこぼす
- **Audio**: 自作 `IAudioService`。SE はプリロード・BGM はストリーム (インポート設定)、クリップは `AudioRegistry` (SO) に登録
- **Assets**: Addressables
- **Ads**: LevelPlay (ironSource)。`IRewardedAdService` で隠蔽し SDK 型を上位層へ出さない
- **他**: NavMeshPlus (2D NavMesh)、2D Animation、Cinemachine、Timeline

## Coding Conventions

- `private` は省略。private フィールドは `_fieldName`、コンストラクタ初期化のみなら `readonly`
- パターンマッチング推奨 (`while (asyncLoad is { isDone: false })`)
- Nullable 使用時はファイル先頭に `#nullable enable`
- Doc Comment は `/// <summary>` ブロックを使わず `/// comment`
- UniTask 非同期メソッドは末尾引数に `CancellationToken`
- VContainer が注入するコンストラクタに `[Inject]` (IL2CPP ストリッピング対策)
- MonoBehaviour から回す Tween は `SetLink(gameObject)`、保持する Tween は再生前と `OnDestroy` で `Kill()` (`MenuSwitchView`)
- 連打されうる演出は毎回 `Kill` して静止姿勢へ戻してから再生する。現在値起点だと連打でずれる (`FurniturePlaceEffectView`)
- 高頻度の描画系は確保を避ける — スプライトはプール、`List`・デリゲートはフィールドで使い回す (`OutfitChangeEffectView`, `GridPreviewView`)

## Logging

クラスコンテキスト付き: `Debug.LogError($"[ClassName] {e.Message}\n{e.StackTrace}")`

リリースビルド (非 Editor かつ非 DEVELOPMENT_BUILD) は `RootScope.ConfigureLogging` が `filterLogType = LogType.Error` で Log / LogWarning を抑止する。Error / Exception はクラッシュレポート SDK 不在のため意図的に残す。抑止しても `$"..."` の生成コストは残るので高頻度パスへのログ追加は避ける

## Testing

決定論的な純粋ロジックは UnityEngine 非依存アセンブリへ切り出し EditMode (NUnit) で検証する (配置は structure.md の Testable Logic Assemblies)。時刻依存は `IClock` 注入で決定化

## Key Decisions

### VContainer
- RootScope は `Lifetime.Singleton`。登録の全量は `RootScope.cs` を参照。インターフェースを持つサービスは `.As<IXxx>().AsSelf()`
- SceneScope は抽象基底 `SceneScope` を継承し `Lifetime.Scoped`。Awake で MasterDataImport を保証
- `ITickable` は毎フレーム更新が要るサービスに採用 (`ShopService` の時限サイクル監視、`IsoInputService`)。コンストラクタ DI に加え `RegisterEntryPoint` も併用
- RootScope の `IInitializable` は Awake 中に同期実行され、プレハブ子 View の Awake より先に走る。子 View に触れる初期化は `IStartable` (`AudioSceneService`)

### Scene Transition
Fade シーンを加算ロードし FadeOut → ターゲットロード → FadeIn → Fade アンロード。`SceneLoader._isLoading` で多重呼び出しを防ぐ

### Time
`DateTimeOffset.UtcNow` を直接呼ばず `IClock` (`SystemClock`) を経由する

### State Snapshot
ユーザー資産系サービスは `UserPointSnapshot` のようなイミュータブルスナップショットを返す

### Platform-Conditional Registration
外部 SDK はインターフェースで隠蔽し、RootScope で `#if UNITY_EDITOR / #elif UNITY_ANDROID || UNITY_IOS` により実装を切替える (`IRewardedAdService` は Editor でスタブ)

### Config-as-Asset
構成値・機密はコードリテラルから排除し ScriptableObject を `Resources` からロードする。欠落時は RootScope で fail-fast (`RewardedAdConfig`, `AudioRegistry`)。キーはビルド時に env / ローカル `.env` から注入

### World-Space Sorting (Home)
家具は `SortingGroup` の `sortingOrder = (x + y) * 1000 + x`。グリッド線・床の設置予告は負値 (家具より奥)、ワールド空間の演出は 32000 (全家具より手前)。家具の上面 (`FragmentedIsoGrid`) は自前の `SortingGroup` を持ち、グループ内は予告 (-1) < 子家具 (0 以上)

### Audio
- `AudioService` が BGM クロスフェード・SE 再生・音量 / ON-OFF の永続化 (PlayerPrefs) を担う。純粋計算は `Root/AudioLogic`
- `AudioSceneService` が `sceneLoaded` でシーン別 BGM を再生し、`ButtonSeAttacher` が Button に `ButtonSe` を自動付与する。上書き・無音化は `ButtonSe` を手置きして `SeId` 指定 (`None` で無音)
- `SeId` / `BgmId` enum の追加は必ず末尾 (シリアライズ済み値の保護)

### Timer Session Lifecycle
- 計測中は `ScreenSleepService` (TimerScope の `Lifetime.Scoped`) が `Screen.sleepTimeout = NeverSleep` を張り、一時停止で端末設定へ戻す。`Dispose` が離脱時の解除を保証する (`OnDisable` へ移すと DI ライフサイクルの保証から外れる)
- バックグラウンド中はフレーム更新を止め、復帰時に `IClock` の壁時計差分を**現フェーズの残り時間を上限に**反映する (`PomodoroService.NotifyEnteredBackground` / `NotifyReturnedToForeground`)。無制限に加算すると就寝などの長時間離脱が `TimerRecordService` の記録に乗る
- フォアグラウンドのオーバータイム (`+MM:SS`) は無制限に記録する。**この非対称は意図的** — 前面の超過は集中継続だが背面はそうとは限らない
- フェーズは復帰時に自動送りしない。送りはユーザー操作起点 (`TransitionToBreak` / `TransitionToFocus`) で、UI は `IsTimerExpired` 後にボタンを出して待つ
- `_backgroundedAt != null` 中にフレーム更新を止めるのは、エディタや `runInBackground` の Standalone などフレームが回り続ける環境での二重計上対策
- OS レベルの常駐実行 (iOS Background Modes / Android フォアグラウンドサービス) は採らない — タイマー用途の audio / location 宣言は審査で通らない

### Screen Orientation
`defaultScreenOrientation = Portrait` 固定 (AutoRotation + 縦のみ許可ではない)。UI は縦専用設計 (structure.md の Screen Safe Area)
- `androidResizeableActivity` は `true` のまま**意図的に未変更**。分割画面では向き指定が無視され、targetSdk 36 は大画面端末で向き制限を無視する。`false` で強制できるがマルチウィンドウ対応を落とす製品判断

### Store Privacy (広告ID・トラッキング)
- iOS の App Privacy 宣言はビルド後処理 (`Assets/Editor/RewardedAdBuildPostProcessor.cs`) で、Unity 生成の `PrivacyInfo.xcprivacy` へ `NSPrivacyTracking` / `NSPrivacyCollectedDataTypes` だけ追記する。`Assets/` へ静的配置すると Unity 生成分と二重になり、Xcode の Resources へも自動登録されない
- `NSPrivacyCollectedDataTypes` は App Store Connect の入力と食い違わせない。両ストアの申告内容と根拠は `docs/store-privacy-declaration.md`
- `TrackingDomains` は意図的に空 — アプリ自身はトラッキング通信をせず (`UnityWebRequest` 利用ゼロ、Addressables もローカルのみ)、SDK が自前のマニフェストで宣言する
- 全体が `#if UNITY_IOS` 内のため Android ターゲット中はコンパイルされない。変更したら iOS へ切り替えて確認する
- Android の `com.google.android.gms.permission.AD_ID` は `play-services-ads-identifier` の AAR から自動マージされる

---
_標準とパターンのみ記す。依存関係の網羅や実装スナップショットは書かない_
