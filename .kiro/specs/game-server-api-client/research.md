# Research & Design Decisions

---
**Purpose**: game-server-api-clientの設計に必要な調査結果と設計判断を記録する。
---

## Summary
- **Feature**: `game-server-api-client`
- **Discovery Scope**: New Feature（既存アーキテクチャへの統合を伴う新機能）
- **Key Findings**:
  - UniTaskは`UnityWebRequestException`を自動的にスローし、Result/ResponseCode/Textプロパティで詳細な分類が可能
  - Newtonsoft.Json（`com.unity.nuget.newtonsoft-json` 3.0.2）がトランジティブ依存として利用可能。JsonUtilityではAPI通信に不十分（Dictionary、nullable、プロパティ非対応）
  - 既存のDialogService/SceneLoaderパターンがErrorHandlerの設計に直接適用可能

## Research Log

### UnityWebRequest + UniTask 統合パターン
- **Context**: HTTP通信の非同期パターンの選定
- **Sources**: UniTask GitHub、UniTask ソースコード（Library/PackageCache内）
- **Findings**:
  - `request.SendWebRequest().WithCancellation(cancellationToken)` が標準パターン
  - キャンセル時、UniTaskが内部で `request.Abort()` を呼び出し `OperationCanceledException` をスロー
  - エラー時は `UnityWebRequestException` を自動スロー（Result, ResponseCode, Text, Error, ResponseHeaders プロパティ付き）
  - `UnityWebRequest.Result` enum: `Success`, `ConnectionError`, `ProtocolError`, `DataProcessingError`
  - タイムアウトは UniTask の `.Timeout(TimeSpan)` を使用。`TimeoutException` がスローされるため型安全に判定可能。catch内で `request.Abort()` を呼び出してリクエストを中断する
- **Implications**: APIクライアントは `UnityWebRequestException` および `TimeoutException` をキャッチして `ApiResult<T>` に変換する設計が適切

### JSON シリアライゼーション選定
- **Context**: APIリクエスト/レスポンスのJSON処理
- **Sources**: Unity公式ドキュメント、Newtonsoft.Json UPMパッケージドキュメント
- **Findings**:
  - JsonUtility: 高速・低GCだが、Dictionary非対応、プロパティ非対応、nullable制限あり、ルート配列非対応
  - Newtonsoft.Json: Dictionary/nullable/プロパティ対応、`[JsonProperty]`属性、カスタムコンバータ、IL2CPP対応
  - System.Text.Json: Unity 6で利用不可
  - `com.unity.nuget.newtonsoft-json` 3.2.1 がプロジェクトに直接依存としてインストール済み
- **Implications**: API通信にはNewtonsoft.Jsonを採用。追加の導入作業は不要

### 既存RootScope登録パターン
- **Context**: 新サービスのVContainer登録方法
- **Sources**: `Assets/Scripts/Root/Scope/RootScope.cs`
- **Findings**:
  - `builder.Register<ServiceClass>(Lifetime.Singleton).As<IInterface>()` パターン
  - `[Inject]` 属性をコンストラクタに付与
  - `RegisterEntryPoint<T>()` で `IInitializable`/`IStartable` を登録
- **Implications**: ApiClient、ApiErrorHandlerも同じパターンで登録

### DialogService インターフェース
- **Context**: エラーハンドリングサービスからのダイアログ表示
- **Sources**: `IDialogService.cs`, `CommonConfirmDialog.cs`, `CommonMessageDialog.cs`
- **Findings**:
  - `OpenAsync<TDialog, TArgs>(args, ct)` → `UniTask<DialogResult>`
  - `DialogResult`: Ok, Cancel, Close
  - `CommonConfirmDialogArgs(Title, Message, OkButtonText, CancelButtonText)` — record型
  - `CommonMessageDialogArgs(Title, Message, OkButtonText)` — record型
- **Implications**: ApiErrorHandlerはCommonConfirmDialog（リトライ/タイトル選択）とCommonMessageDialog（エラー通知）をそのまま利用可能

### SceneLoader
- **Context**: エラーハンドリングからのタイトル画面遷移
- **Sources**: `Assets/Scripts/Root/Service/SceneLoader.cs`
- **Findings**:
  - `Load(targetSceneName, fadeOutDuration, fadeInDuration)` メソッド
  - タイトル画面: `SceneLoader.Load("Title")`（`Const.SceneName` で定数管理されている可能性あり）
- **Implications**: ApiErrorHandlerがSceneLoaderを注入してタイトル遷移を実行

### レスポンスタイム計測
- **Context**: APIログ出力でのレスポンスタイム計測
- **Sources**: Unity公式ドキュメント、コミュニティ調査
- **Findings**:
  - UnityWebRequestにはelapsedプロパティが存在しない
  - `System.Diagnostics.Stopwatch` で計測する必要がある
- **Implications**: SendAsync内でStopwatchを使用。ログ出力ディレクティブ内に配置

## Architecture Pattern Evaluation

| Option | Description | Strengths | Risks / Limitations | Notes |
|--------|-------------|-----------|---------------------|-------|
| Result型パターン | ApiClient内で例外をキャッチしApiResult<T>に変換 | 呼び出し元で例外処理不要、エラー種別が型安全 | ラッパー型のオーバーヘッド | 要件3.1の「構造化された型で返す」に合致 |
| 例外伝播パターン | UnityWebRequestExceptionをそのまま伝播 | シンプル、UniTask標準動作 | エラー分類が呼び出し元の責務になる | 要件3と不整合 |

## Design Decisions

### Decision: Result型によるエラー返却
- **Context**: 要件3「構造化された型で返す」の実現方法
- **Alternatives Considered**:
  1. 例外伝播 — UniTaskの標準動作に従い例外をそのまま伝播
  2. Result型 — ApiClient内で例外をキャッチし、成功/失敗を表す型で返す
- **Selected Approach**: Result型（ApiResult<T>）
- **Rationale**: エラー種別の分類（3.2）、リトライ可否の判定（3.4）をAPIクライアント内で完結させ、呼び出し元やエラーハンドラが構造化された情報を受け取れる
- **Trade-offs**: OperationCanceledException（キャンセル）はResult型に包まず例外のまま伝播させる。キャンセルはエラーではなく正常な制御フローのため
- **Follow-up**: なし

### Decision: Newtonsoft.Jsonの採用
- **Context**: APIレスポンスのJSON処理
- **Alternatives Considered**:
  1. JsonUtility — 既存プロジェクトで使用中、高速
  2. Newtonsoft.Json — 機能豊富、既にトランジティブ依存で存在
- **Selected Approach**: Newtonsoft.Json
- **Rationale**: APIレスポンスにはDictionary、nullable型、snake_caseプロパティなどJsonUtilityでは扱えない構造が想定される
- **Trade-offs**: JsonUtilityより低速・GC多いが、API通信頻度を考慮すれば問題なし
- **Follow-up**: `manifest.json`に直接依存として追加

### Decision: タイトル遷移後の制御フロー
- **Context**: エラーハンドリングサービスがタイトル画面に遷移した後、呼び出し元のasync処理をどう終了させるか
- **Alternatives Considered**:
  1. OperationCanceledException をスロー — 標準的なキャンセルシグナル
  2. カスタム例外 — 明示的な型で識別可能
  3. 何もしない — シーン破棄で自然にキャンセル
- **Selected Approach**: OperationCanceledException をスロー
- **Rationale**: シーン遷移後に呼び出し元のコードが継続するのを防ぐ。VContainerのスコープ破棄とCancellationTokenの連携で自然に処理される
- **Trade-offs**: シーン遷移の完了を待たずに例外がスローされるが、呼び出し元のスタック巻き戻しとしては十分
- **Follow-up**: なし

## Risks & Mitigations
- **Newtonsoft.Json**: 3.2.1が直接依存として導入済み。追加対応不要
- **タイムアウト判定**: UniTask `.Timeout()` + `TimeoutException` で型安全に判定。catch内で `request.Abort()` を明示的に呼び出す必要がある
- **IL2CPP環境でのJSON デシリアライズ**: Newtonsoft.JsonのUnity公式パッケージはIL2CPP対応済み。ただしリフレクションベースのデシリアライズではlink.xml設定が必要な場合がある

## References
- [UniTask GitHub](https://github.com/Cysharp/UniTask) — UnityWebRequest統合パターン
- [Unity 6 UnityWebRequest.Result](https://docs.unity3d.com/6000.0/Documentation/ScriptReference/Networking.UnityWebRequest.Result.html)
- [com.unity.nuget.newtonsoft-json](https://docs.unity3d.com/Packages/com.unity.nuget.newtonsoft-json@3.0/manual/index.html)
- [Unity 6 Custom Scripting Symbols](https://docs.unity3d.com/6000.3/Documentation/Manual/custom-scripting-symbols.html)
