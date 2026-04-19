# Research & Design Decisions

## Summary
- **Feature**: `home-to-timer-scene-transition`
- **Discovery Scope**: Extension（既存システムの拡張）
- **Key Findings**:
  - DialogServiceは`OpenAsync<TDialog, TArgs>(args, ct)`でダイアログを表示し、`DialogResult`（Ok/Cancel/Close）を返す。設定値の受け渡しにはDialogResultでは不足するため、共有State経由で値を渡す設計が必要
  - HomeFooterViewは現在`sceneLoader.Load(Const.SceneName.Timer)`を直接呼んでおり、ダイアログ表示を挟むにはasyncハンドラへの変更と`IDialogService`の注入が必要
  - TimerSettingフォルダ構造（Manager/Scope/Service/Starter/State/View）は既に用意されており、コードファイルのみが未作成
  - 多重遷移防止はFadeシーンの全画面CanvasGroup（blocksRaycasts）で対応済み

## Research Log

### ダイアログからの設定値受け渡しパターン
- **Context**: DialogServiceの`OpenAsync`は`DialogResult`（Ok/Cancel/Close）のみ返す。ポモドーロ設定値（集中時間・休憩時間・セット回数）をダイアログからシーン遷移先に渡す手段が必要
- **Findings**:
  - 既存パターン: RootScopeのSingleton Stateを経由してシーン間でデータ共有（例: `SceneLoaderState`）
  - PlayerPrefsServiceによるJSON永続化も利用可能（`JsonUtility.ToJson/FromJson<T>`）
  - ダイアログ内でPlayerPrefsに保存→Timer シーン起動時にPlayerPrefsから読み取りが最もシンプル
- **Implications**: RootScopeに新規Stateを追加する方法とPlayerPrefs経由の2択があるが、要件6「前回保存された設定値を復元」があるため永続化は必須。PlayerPrefs経由なら永続化と受け渡しを兼ねられる

### HomeFooterViewの変更影響
- **Context**: タイマーボタンの動作を「直接遷移」から「ダイアログ表示→遷移」に変更
- **Findings**:
  - 現在のInit: `homeStateSetService`と`sceneLoader`の2つを注入
  - 変更後: `IDialogService`を追加注入し、タイマーボタンのリスナーをasyncに変更
  - `_timerButton.onClick.AddListener`はasync voidを直接受け付けないため、UniTaskVoid経由のラッパーが必要
- **Implications**: HomeFooterViewの変更は最小限で済む。asyncハンドラパターンは既にプロジェクト内で確立済み

### BaseDialogViewの×ボタン対応
- **Context**: BaseDialogViewは`[SerializeField] Button? _closeButton`を持ち、Awakeで自動的に`OnCloseButtonClicked`→`RequestClose(DialogResult.Close)`を接続
- **Findings**:
  - ×ボタンはBaseDialogViewのシリアライズフィールドとして標準装備
  - Prefab上で`_closeButton`にボタンをアサインするだけで動作
  - 追加コード不要
- **Implications**: ダイアログの×ボタン（要件1.3, 1.4）はBaseDialogViewの標準機能で対応

## Architecture Pattern Evaluation

| Option | Description | Strengths | Risks / Limitations | Notes |
|--------|-------------|-----------|---------------------|-------|
| A: Root/Viewにダイアログ配置 | ダイアログViewをRoot/View、ロジック不要 | 既存Common系ダイアログと同じ配置 | ドメイン分離が弱い | シンプルだがTimerSettingフォルダが活用されない |
| B: TimerSetting配下にすべて配置 | View/State/ServiceをTimerSetting配下に | フォルダ構造が活用される | Addressablesのパス規約（`Dialogs/`）との整合性要確認 | ダイアログPrefabパスは`Dialogs/`固定のため注意 |
| C: ハイブリッド | ダイアログViewはTimerSetting/View、State/ServiceもTimerSetting配下 | ドメイン分離OK、Addressablesパスは変更なし | ファイル分散 | **採用** |

## Design Decisions

### Decision: ダイアログViewの配置場所
- **Context**: TimerSettingDialogのC#スクリプトをどこに配置するか
- **Alternatives Considered**:
  1. Root/View — 既存のCommon系ダイアログと同じ場所
  2. TimerSetting/View — ドメイン別の配置
- **Selected Approach**: TimerSetting/View に配置
- **Rationale**: TimerSettingフォルダ構造が既に用意されており、ドメイン固有のダイアログはドメインフォルダに配置するのが自然。Addressablesのキーは`Dialogs/TimerSettingDialog.prefab`のままで、C#スクリプトの配置場所とは独立
- **Trade-offs**: Root/Viewの他のダイアログと場所が異なるが、ドメイン固有であるため妥当

### Decision: 設定値の永続化と受け渡し
- **Context**: ダイアログで入力した設定値をPlayerPrefsに保存し、Timerシーンで読み取る
- **Selected Approach**: PlayerPrefsService経由でJSON永続化
- **Rationale**: 要件2.5/2.6で永続化が必要。RootScope Stateの追加は不要で、PlayerPrefsから読み書きするだけで要件を満たせる
- **Follow-up**: PlayerPrefsKey enumにTimerSettingを追加、JsonUtility対応のSerializableなデータクラスを定義

### Decision: HomeFooterViewの変更方針
- **Context**: タイマーボタン押下時にダイアログ→遷移のフローに変更
- **Selected Approach**: HomeFooterViewにIDialogServiceを注入追加し、タイマーボタンのハンドラをasyncに変更
- **Rationale**: 最小限の変更で要件を満たす。新たなServiceクラスを挟む必要はない
- **Trade-offs**: HomeFooterViewの責務がやや増えるが、ボタン→ダイアログ→遷移の単純なフローであり許容範囲

## Risks & Mitigations
- **JsonUtilityの制約**: `record`型は`JsonUtility`でシリアライズ不可 → `[Serializable] class`を使用
- **Addressables Prefab未作成**: ダイアログPrefabはUnity Editor上で手動またはUnityMCP経由で作成が必要 → 実装タスクに含める
- **FadeManagerのエラーハンドリング**: 既に`try-catch`でログ出力+`FadePhase.None`リセットが実装済み → 追加対応不要

## References
- VContainer公式ドキュメント: LifetimeScope、IStartable
- UniTask: UniTaskVoid、CancellationTokenパターン
- Unity JsonUtility: Serializable制約
