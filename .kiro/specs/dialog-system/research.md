# Research & Design Decisions

## Summary
- **Feature**: dialog-system
- **Discovery Scope**: New Feature（新規のゲーム基盤機能）
- **Key Findings**:
  - UniTaskはGitHub URL経由で手動インストール、Addressablesとの統合サポートあり
  - 既存のRootScope/SceneScopeパターンに従い、DialogServiceをRootScopeにSingletonとして登録
  - Androidの戻るボタンは新InputSystemでは課題があり、Active Input Handlingを"Both"に設定する必要がある可能性

## Research Log

### UniTaskのインストールと使用方法
- **Context**: ダイアログの非同期API（async/await）を実現するためにUniTaskを導入
- **Sources Consulted**:
  - [GitHub - Cysharp/UniTask](https://github.com/Cysharp/UniTask)
  - [OpenUPM - UniTask](https://openupm.com/packages/com.cysharp.unitask/)
- **Findings**:
  - UniTaskはzero-allocationのasync/await実装を提供
  - GitHub URL経由で手動インストール: `https://github.com/Cysharp/UniTask.git?path=src/UniTask/Assets/Plugins/UniTask`
  - Addressablesサポートは自動的に有効化される（UniTask.Addressables asmdef）
  - Unity 2018.3以降（C# 7.0）が必要（本プロジェクトはUnity 6で問題なし）
- **Implications**:
  - DialogServiceのOpenAsync<TDialog>()メソッドでUniTask<TResult>を返す設計が可能
  - UniTaskCompletionSource<TResult>を使用してダイアログ結果を非同期で待機

### Addressablesによるプレハブロード
- **Context**: ダイアログプレハブをAddressablesで管理し、動的ロードを実現
- **Sources Consulted**:
  - [Unity Addressables Documentation](https://docs.unity3d.com/Packages/com.unity.addressables@1.20/manual/LoadingAddressableAssets.html)
  - [Addressables.InstantiateAsync](https://docs.unity3d.com/Packages/com.unity.addressables@1.14/manual/InstantiateAsync.html)
- **Findings**:
  - 2つのアプローチ: `LoadAssetAsync` + 手動Instantiate vs `InstantiateAsync`
  - ダイアログの場合は`LoadAssetAsync`でプレハブを保持し、必要に応じてInstantiateが適切
  - 参照カウント管理: `InstantiateAsync`は毎回インクリメント、`LoadAssetAsync`は1回のみ
  - 解放忘れはメモリリークの原因となるため、ダイアログクローズ時に適切に解放が必要
- **Implications**:
  - DialogContainerがロード済みプレハブのキャッシュを管理
  - ダイアログ毎に`AssetReference`または文字列アドレスでロード
  - クローズ時に`Addressables.ReleaseInstance()`で解放

### Androidの戻るボタン対応
- **Context**: Androidの戻るボタンでダイアログを閉じる機能
- **Sources Consulted**:
  - [Unity Discussions - Android back button with InputSystem](https://discussions.unity.com/t/how-to-get-back-key-pressed-event-on-android-by-inputsystem/1553240)
  - [Unity Documentation - Input.backButtonLeavesApp](https://docs.unity3d.com/ScriptReference/Input-backButtonLeavesApp.html)
- **Findings**:
  - レガシーInput Manager: `Input.GetKeyDown(KeyCode.Escape)`で検出可能
  - 新Input Systemでは課題があり、`Keyboard.current.escapeKey`が動作しない報告あり
  - 対策: Player Settings > Active Input Handlingを"Both"に設定
  - `Input.backButtonLeavesApp = false`でアプリ終了を防止
- **Implications**:
  - 安全のためレガシーInput Managerを使用（`KeyCode.Escape`）
  - DialogContainerのUpdateでバックボタンを監視
  - 将来的にInput System対応が安定したら移行検討

### 既存のVContainerパターン分析
- **Context**: 既存のRoot層構造を理解し、ダイアログシステムを適切に統合
- **Sources Consulted**: プロジェクト内コード分析
  - `Assets/Scripts/Root/Scope/RootScope.cs`
  - `Assets/Scripts/Root/Scope/SceneScope.cs`
  - `Assets/Scripts/Root/Service/SceneLoader.cs`
- **Findings**:
  - RootScope: Singletonライフタイムでグローバルサービスを登録
  - 既存パターン: Service → State の依存方向
  - View層はMonoBehaviourで`[Inject]`属性を使用
  - 現状は標準の`System.Threading.Tasks.Task`を使用（`FadeView.AnimateFade`）
- **Implications**:
  - DialogService, DialogState をRootScopeに登録
  - DialogViewBase (MonoBehaviour) は[Inject]でServiceを受け取る
  - 既存のasync/awaitパターンと一貫性を持たせつつUniTaskに移行

### C# record構文の有効化
- **Context**: ダイアログ引数をimmutableなrecordとして定義
- **Sources Consulted**: プロジェクト要件からの情報
- **Findings**:
  - Unity（.NET Standard 2.1）ではC# 9のrecord構文がデフォルトで無効
  - `IsExternalInit`クラスを定義することで有効化可能
  - 配置場所: `Assets/Scripts/Utils/IsExternalInit.cs`
- **Implications**:
  - ダイアログ引数は`record`で定義し、immutabilityを保証
  - 例: `public record ConfirmDialogArgs(string Message, string OkText = "OK");`

## Architecture Pattern Evaluation

| Option | Description | Strengths | Risks / Limitations | Notes |
|--------|-------------|-----------|---------------------|-------|
| Service-State-View | 既存のVContainerパターンに準拠 | 既存コードとの一貫性、チーム理解容易 | 特になし | **採用** - steering準拠 |
| MVVM | ViewModel層を追加 | データバインディングの明確化 | 既存パターンと不一致、過剰設計 | 不採用 |
| Event-Driven | イベントベースの通信 | 疎結合 | デバッグ困難、既存パターンと不一致 | 不採用 |

## Design Decisions

### Decision: スタック管理の実装方式
- **Context**: 複数ダイアログのスタック管理と結果通知の実現方法
- **Alternatives Considered**:
  1. Stack<DialogInstance>をDialogStateで管理
  2. LinkedListで双方向リンク管理
  3. 各ダイアログがparent参照を持つ
- **Selected Approach**: Stack<DialogInstance>をDialogStateで管理
- **Rationale**: LIFO構造がダイアログの開閉順序と一致、シンプルで理解しやすい
- **Trade-offs**: 中間のダイアログを直接閉じる操作が複雑になるが、要件では最前面からの閉じのみ
- **Follow-up**: 親ダイアログ連続クローズの実装詳細を設計時に決定

### Decision: Addressablesのロード戦略
- **Context**: ダイアログプレハブのロード・キャッシュ方式
- **Alternatives Considered**:
  1. 毎回LoadAssetAsync + Instantiate（使い捨て）
  2. プレハブをキャッシュし、Instantiateのみ繰り返し
  3. InstantiateAsyncで都度インスタンス化
- **Selected Approach**: プレハブをキャッシュし、Instantiateのみ繰り返し
- **Rationale**: 同じダイアログを複数回開く可能性があり、ロードオーバーヘッド削減
- **Trade-offs**: メモリ使用量増加の可能性あるが、ダイアログ数は限定的
- **Follow-up**: キャッシュのクリアタイミング（シーン遷移時など）を検討

### Decision: ダイアログ結果の型設計
- **Context**: ダイアログが返す結果の型をどう設計するか
- **Alternatives Considered**:
  1. 単一のenum `DialogResult { Ok, Cancel, ... }`
  2. ジェネリック `TResult` で任意の型を返す
  3. 基底インターフェース `IDialogResult` を実装
- **Selected Approach**: デフォルトenum + 将来的にジェネリック拡張可能な設計
- **Rationale**: YAGNIに従い現時点はシンプルに、拡張ポイントは残す
- **Trade-offs**: ジェネリック対応時にAPI変更が必要になる可能性
- **Follow-up**: 具体的な値を返す要件が発生した時点でジェネリック版を追加

### Decision: アニメーション制御方式
- **Context**: ダイアログの開閉アニメーション制御
- **Alternatives Considered**:
  1. Animatorコンポーネント + AnimatorController
  2. Animationクリップ直接再生
  3. DOTweenによるコード制御
- **Selected Approach**: Animatorコンポーネント + AnimatorController
- **Rationale**: 要件でAnimation/Animator使用が明記、デザイナーが調整可能
- **Trade-offs**: AnimatorControllerのセットアップが必要
- **Follow-up**: アニメーション完了検知にはStateMachineBehaviourまたはアニメーションイベントを使用

## Risks & Mitigations
- **Risk 1**: Androidの戻るボタンが新Input Systemで動作しない可能性 → レガシーInput Managerを使用、Active Input Handlingを"Both"に設定
- **Risk 2**: Addressablesのメモリリーク → ダイアログクローズ時に明示的にReleaseInstance呼び出し
- **Risk 3**: アニメーション中の操作ブロックが不完全 → CanvasGroupのinteractableとblocksRaycastsを使用
- **Risk 4**: 複数ダイアログの順序管理が複雑化 → スタック構造で明確に管理、sortingOrderを動的に設定

## References
- [Cysharp/UniTask GitHub](https://github.com/Cysharp/UniTask) — UniTask公式リポジトリ
- [Unity Addressables Manual](https://docs.unity3d.com/Packages/com.unity.addressables@2.7/manual/index.html) — Addressables公式ドキュメント
- [VContainer Documentation](https://vcontainer.hadashikick.jp/) — VContainer公式ドキュメント
