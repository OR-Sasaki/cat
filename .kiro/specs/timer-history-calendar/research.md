# Research & Gap Analysis — timer-history-calendar

## Summary
- **Feature**: `timer-history-calendar`
- **Discovery Scope**: Extension (既存 `History` シーン拡張) + New Feature (タイマー記録系の新規追加)
- **Key Findings**:
  - `History` シーンは雛形のみ存在 (`HistoryScope` 空 Configure / `ReturnButtonView` のみ)。Service / State / Manager / Starter は空。
  - `Home/View/HomeFooterView` に `_historyButton` の `[SerializeField]` 宣言は存在するが、`Init` 内で `onClick.AddListener` が未配線 (履歴導線が未接続)。
  - `Timer/Service/PomodoroService` は `_state.TotalFocusTime` (秒, float) をフェーズ Focus 中に毎フレーム加算済み。`PomodoroPhase.Complete` 遷移で完了通知が出るため、「完了時に集中時間を記録に確定」というフックは無改修で取れる。
  - 永続化基盤 (`PlayerPrefsService` + JsonUtility / `[Serializable]` Snapshot) と時刻抽象 (`IClock` = `DateTimeOffset UtcNow`) はそのまま流用可能。`UserPointSnapshot` / `UserItemInventorySnapshot` がスナップショット書式の参考実装になる。
  - `IClock` は UTC のみ提供 (`LocalNow` 等なし)。要件はローカル日付ベースのため、サービス側で `clock.UtcNow.LocalDateTime.Date` 変換を行う設計が必要。

## Research Log

### Topic: 既存 History シーンの実装状況
- **Context**: 拡張ベースかフルスクラッチかを判断するため。
- **Sources Consulted**: `Assets/Scripts/History/`, `Assets/Scenes/History.unity` (29KB)、`Assets/Scripts/Utils/Const.cs`。
- **Findings**:
  - `Const.SceneName.History = "History"` 定義済み。
  - `History/Scope/HistoryScope.cs` は `SceneScope` 継承の空 `Configure` のみ。
  - `History/View/ReturnButtonView.cs` は Home 戻るボタンのみ実装済み。
  - Service / State / Manager / Starter フォルダはすべて `.gitkeep` のみ。
- **Implications**: 既存「薄いシーン」のパターン (steering structure.md 記載) に準拠した拡張が可能。Scope に新規 Service / State / Starter / View を追加し、History.unity 上の UI 配置を別途行う形が自然。

### Topic: タイマー集中時間の発生源
- **Context**: 要件 1 の「集中セッション完了」と「集中時間 (秒)」の調達元を確認するため。
- **Sources Consulted**: `Timer/Service/PomodoroService.cs`、`Timer/State/PomodoroState.cs`、`Timer/View/CompletePanelView.cs`。
- **Findings**:
  - `PomodoroService.RunTimerLoopAsync` は `Time.deltaTime` を `TotalFocusTime` に Focus フェーズ中のみ加算 (秒, float)。
  - `Pause` 中は加算されないため、要件 1 の「実際の集中時間のみ加算」要件と整合。
  - 最終セットの Focus 完了時に `_state.SetPhase(PomodoroPhase.Complete)` が呼ばれ、`OnPhaseChanged` イベント経由で View が更新される。
  - `CompletePanelView` は `OnPhaseChanged → UpdateTotalFocusTimeText` で表示更新するパターン。Home ボタンで `SceneLoader.Load(Const.SceneName.Home)`。
- **Implications**:
  - 「完了時に記録を 1 度確定」する設計が最も既存パターンに馴染む (`OnPhaseChanged(Complete)` で `TimerRecordService.AddSeconds(...)` を呼ぶ)。
  - 「Focus→Break 遷移ごとに 1 セット分を記録」する選択肢もあるが、PomodoroState 上は「累積 TotalFocusTime」しか持たないため、毎回 1 セット分を計算する必要が出る (差分計算が要る)。
  - **未確定**: セッション途中で Home ボタン離脱 / アプリ kill された場合の扱い (記録するか / 棄却するか)。

### Topic: スナップショット永続化の書式
- **Context**: 日別集中時間という「Dictionary<Date, Seconds>」相当のデータを JsonUtility で扱える形式に落とす方法を確認するため。
- **Sources Consulted**: `Root/Service/UserPointSnapshot.cs`、`Root/Service/UserItemInventorySnapshot.cs`、`Root/Service/UserItemInventoryService.cs`、`Root/Service/PlayerPrefsService.cs`。
- **Findings**:
  - JsonUtility は `Dictionary` / `HashSet` を直列化できないため、`UserItemInventorySnapshot.Furnitures` のように `[Serializable]` エントリ配列で保持する慣習。
  - 全スナップショットに `public const int CurrentVersion` と `public int Version` を持たせ、ロード時に不一致なら破棄するマイグレーション余地を確保している。
  - `PlayerPrefsService` は `JsonUtility.ToJson(value)` で文字列化し PlayerPrefs に保存する薄いラッパ。`Save` ログを `Debug.Log` で出力する仕様。
  - キー追加は `PlayerPrefsKey` enum (現: `Outfit`, `UserEquippedOutfit`, `IsoGrid`, `TimerSetting`, `UserItemInventory`, `UserPoint`) への追加が必要。
- **Implications**:
  - `TimerRecordSnapshot { int Version; TimerRecordEntry[] Entries; }` + `TimerRecordEntry { string Date /* yyyy-MM-dd */ or int Year/Month/Day; int Seconds; }` 形式が無難。
  - `PlayerPrefsKey.TimerRecord` (仮称) を追加要。

### Topic: シーンスコープと DI 登録パターン
- **Context**: TimerRecord 系を Singleton にすべきか Scoped にすべきかを判断するため。
- **Sources Consulted**: `Root/Scope/RootScope.cs`、`Root/Scope/SceneScope.cs`、`Timer/Scope/TimerScope.cs`、`History/Scope/HistoryScope.cs`。
- **Findings**:
  - 横断的にユーザー資産を保持するもの (`UserPointService`, `UserItemInventoryService`) は `Lifetime.Singleton` で `RootScope` に登録、`I*Service` と `AsSelf()` の両登録。
  - `IClock` (実装 `SystemClock`) も RootScope で `As<IClock>()` 登録済み。
  - `SceneScope` 基底クラスは `Awake` で `MasterDataImportService.Import()` を呼ぶ。各シーンScope はその恩恵を受ける。
  - `TimerScope` は CancellationToken を `RegisterInstance` して破棄時に Cancel する方式。
- **Implications**:
  - TimerRecordService は Timer / History / 将来的に Home からも参照される可能性が高いため、Singleton で `RootScope` に置く方が一貫する (Option C 推奨理由)。
  - Streak Calculator / カレンダー描画用 ViewModel 系は History シーン内に閉じる Scoped が自然。

### Topic: Home → History 遷移導線
- **Context**: 要件 2.1 の入口がどこに既に用意されているかを確認するため。
- **Sources Consulted**: `Home/View/HomeFooterView.cs`。
- **Findings**:
  - `_historyButton` (`[SerializeField] Button`) は既に宣言され、Inspector 上の参照保持を想定。
  - 一方、`Init` メソッド内では `_historyButton.onClick.AddListener(...)` が呼ばれていない (`_redecorateButton` / `_closetButton` / `_timerButton` のみ配線、`_shopButton` も未配線に見える)。
- **Implications**: 履歴シーンの導線は「フッターの History ボタンに `sceneLoader.Load(Const.SceneName.History)` を bind するだけ」の小作業で完結できる。

### Topic: アイコン段階表現と差し替え方式
- **Context**: 要件 4 の「コードで色生成しない・4 段階画像を差し替え」を満たす実装パターンを評価するため。
- **Sources Consulted**: ステアリング `tech.md` (Addressables 利用)、ScriptableObject の利用は本コードベースで未確認だが標準 Unity 機能として利用可。
- **Findings**:
  - 4 枚の `Sprite` を保持する設定アセットを `ScriptableObject` で実装し、HistoryScope の `RegisterInstance` または `[SerializeField]` 経由で View に注入する形が、コード改修なしで差し替え可能な最小構成。
  - 閾値も同 ScriptableObject に `int[] secondsThresholds` で持たせれば Inspector 上で調整可能。
  - Addressables を必須とするほどのアセット規模ではない (固定 4 枚) ため、Resources / 直接参照で十分。
- **Implications**: `HistoryCalendarSettings : ScriptableObject { Sprite[4] icons; int[3] thresholdsInSeconds; }` のような単一アセットで完結する。

## Architecture Pattern Evaluation

| Option | Description | Strengths | Risks / Limitations | Notes |
|--------|-------------|-----------|---------------------|-------|
| **A. RootScope 集約** | TimerRecord の Service/State/Snapshot/Streak Calculator まで Root に置く | 既存 UserPoint パターンと最も近似 | History 固有の Calendar 計算まで Root に置くと責務肥大 | Streak は Root でも妥当だが Calendar VM は Scoped 寄りが自然 |
| **B. History 配下集約** | TimerRecord 系も含め全部 History/Service に置く | History 関連が 1 箇所に集約 | Timer から History.Service を参照することになり依存方向 (シーン横断) が逆流 | 不採用 |
| **C. Hybrid (推奨)** | 記録系 (Service/State/Snapshot) は RootScope (Singleton)、Streak Calculator と Calendar VM は HistoryScope (Scoped) | 既存 UserPoint パターン踏襲 + History 固有計算は Scoped で閉じる | ファイル数は最も多い | TimerRecordService は Timer / History 双方から参照される横断サービスとして自然 |

## Requirement-to-Asset Map

| 要件 | 既存資産 / 関連 | 新規必要 | 種別 |
|---|---|---|---|
| **Req 1.1-1.8** タイマー記録 | `PomodoroService.RunTimerLoopAsync` (TotalFocusTime 加算)、`PomodoroState.OnPhaseChanged`、`PlayerPrefsService`、`IClock` | `TimerRecordService` / `TimerRecordState` / `TimerRecordSnapshot` / `PlayerPrefsKey.TimerRecord` 追加、`PomodoroService` から `TimerRecordService.AddSeconds(...)` 呼出を追加 | **Missing** (Service/State/Snapshot)、**Constraint** (JsonUtility は Dictionary 不可) |
| **Req 2.1-2.3** 画面遷移 | `SceneLoader._isLoading`、`History.View.ReturnButtonView`、`HomeFooterView._historyButton` (フィールド宣言済み) | `HomeFooterView.Init` で `_historyButton.onClick.AddListener(() => sceneLoader.Load(Const.SceneName.History))` を追加 | **Missing** (配線のみ) |
| **Req 3.1-3.7** 月別カレンダー / 月送り | `IClock`、`History.unity` シーンの素地 | `HistoryCalendarView` (グリッド/ヘッダ描画)、`HistoryCalendarState` (表示中年月)、`History.unity` 上の UI Prefab 構築 | **Missing** (View / State / UI) |
| **Req 4.1-4.7** 段階アイコン差し替え | (なし) | `HistoryCalendarSettings : ScriptableObject` (`Sprite[4] icons`, `int[3] thresholds`)、デザイン側からの 4 枚 Sprite 受領 | **Missing** (アセット 4 枚 + 設定 SO) |
| **Req 5.1-5.7** ストリーク | `IClock`、`TimerRecordService` (新規) | `StreakCalculator` (HistoryScope Scoped、Service として 1 メソッド)、ストリーク表示 View | **Missing** |
| **Req 6.1-6.8** 選択日 | `IClock` (当日初期選択) | `HistoryCalendarState.SelectedDate`、Cell タップハンドラ、ハイライト/フッター連動描画 | **Missing** |
| **Req 7.1-7.7** 合計時間 | `TimerRecordService` (新規) | 月合計 / 当日合計の集計関数、秒→分変換ユーティリティ (画面共通)、フッター表示 View | **Missing** (集計 + 単位変換ルール 1 か所) |
| **Req 8.1-8.7** アーキ整合 | RootScope 既存登録、SceneScope 基底、`#nullable enable` ＋ `[Inject]` ＋ `CancellationToken` 末尾引数の慣習 | (新規実装が同パターンに準拠していること) | **Constraint** |

### 決定が必要な未解決項目 (要件・設計フェーズで解消済み)
1. ~~**集中セッションの「完了」定義**~~ → 要件 1.1-1.3 で「正常完了 + 中断 + Pause/Quit のすべてで加算」、設計で Flush 3 系統を確定。
2. ~~**アプリ kill 中断時の扱い**~~ → Pause/Quit は `TimerLifecycleManager` で対応。即時 kill は Non-Goals に明示。
3. ~~**日別レコードの内部表現**~~ → 内部 `Dictionary<DateTime, int>` (`Date` 部分のみ)、永続化 Snapshot は `"yyyy-MM-dd"` 文字列で確定。
4. ~~**秒→分変換ルール**~~ → 要件 7.7 で切り捨てに確定 (`FocusTimeFormatter.SecondsToMinutes` に集約)。
5. ~~**Sprite 4 枚閾値の初期値**~~ → 要件 4 で `{0, 1500, 3600}` 秒に確定。
6. ~~**カレンダーセルの実装パターン**~~ → 設計で固定 42 セル Pool に確定 (`CalendarGridView`)。
7. **Home フッターの History ボタン Inspector 割当**: Inspector 上に実 Button が割り当て済みかは実装フェーズの最初に Home.unity を開いて目視確認。割り当て済み前提で onClick 配線 1 行のみ追加する。

### 設計フェーズで新たに加えた決定
- **Flush 戦略**: `PomodoroService._flushedSeconds` による差分加算で「中断時も加算」と「二重加算防止」を同時実現。
- **TimerLifecycleManager**: Pure C# サービスでは捕捉不可なアプリ Pause/Quit を `MonoBehaviour` で受け、`PomodoroService.RequestFlush()` を呼ぶ薄い橋渡し役を新設。
- **HistoryCalendarSettings (ScriptableObject)**: 4 段階アイコン Sprite + 3 閾値 + 月外 Tint Color を 1 アセットに集約。`GetSpriteForSeconds(int)` を提供してコード側に閾値判定ロジックを漏らさない。
- **FocusTimeFormatter (static)**: `floor(seconds / 60)` の 1 箇所集約。要件 7.7 の「画面全体で一貫」を構造的に保証。

## Design Decisions (設計フェーズで確定)

### Decision: TimerRecordService の配置先
- **Selected Approach**: `Root/Service/TimerRecordService.cs` + `Root/State/TimerRecordState.cs` + `Root/Service/TimerRecordSnapshot.cs` を新設、`RootScope` に `Lifetime.Singleton` で `As<ITimerRecordService>().AsSelf()` 登録。`PlayerPrefsKey.TimerRecord` を追加。
- **Rationale**: Timer / History / 将来の他シーンから横断参照される可能性が高く、UserPoint / UserItemInventory と同類のユーザー資産的データのため、既存パターンに揃える。
- **Trade-offs**: Root 登録が増えるが、既存 11 サービスに 1 つ追加するのみで影響軽微。

### Decision: PomodoroService → TimerRecordService の連携
- **Alternatives**:
  1. `PomodoroState.OnPhaseChanged` を `HistoryScope` 経由で購読 (View 層から記録呼出)
  2. `PomodoroService` 内で完了時に `TimerRecordService.AddSeconds` を直接呼ぶ
  3. `TimerRecordWriter` のような薄い Service を Timer 配下に作り PomodoroService から呼ぶ
- **Selected Approach**: 案 2 + Flush パターンの採用。`PomodoroService` に `ITimerRecordService` を `[Inject]` し、内部 `_flushedSeconds` で「すでに記録に書き込んだ秒数」を保持。Flush 時に `floor(TotalFocusTime) - _flushedSeconds` の差分のみ `AddSeconds(...)` に渡す。Flush タイミングは ① フェーズ遷移 (`TransitionToBreak` / `Complete` 突入) ② `RunTimerLoopAsync` の `try/finally` ③ `RequestFlush()` 公開メソッド (`TimerLifecycleManager` から呼ばれる) の 3 系統。
- **Rationale**: Service → Service 呼出はプロジェクトの依存方向ルール (View → Service → State) に違反しない。View 経由の購読は副作用が見えづらくテストが困難。Flush パターンにより「中断時も加算」(要件 1.2)・「二重加算防止」(要件 1.5) を 1 つの不変量で同時に満たせる。
- **Trade-offs**: PomodoroService が Root の Service に依存することになるが、既に PlayerPrefsService への依存があるため新規依存ではない (粒度の追加のみ)。アプリ強制 kill 時の未確定秒数は救済不可 (Non-Goals)。

### Decision: History シーン内の責務分割
- **Selected Approach**:
  - `HistoryScope` で `HistoryCalendarState` (`Lifetime.Scoped`) / `StreakCalculator` (Scoped) / `HistoryCalendarSettings` (`RegisterInstance`) を登録、`HistoryStarter` (`IStartable`) を EntryPoint。
  - View: `StreakLabelView` (連続日数大見出し)、`MonthHeaderView` (年月＋月送り)、`CalendarGridView` (7×N セル)、`DayCellView` (日付 + Sprite + 選択ハイライト)、`SelectedDayFooterView` (選択日 + 合計表示)。
  - 既存の `ReturnButtonView` はそのまま流用。
- **Rationale**: 単一責任で View を分割し、CalendarGridView ↔ DayCellView を Pool 化することで月送り時の GC 影響を抑える。
- **Trade-offs**: ファイル数が多くなるが、既存の Timer シーン (View 5 つ) と同程度の粒度で違和感なし。

## Risks & Mitigations
- **Risk: 集中時間の未確定リーク (中断/kill 時)** — Mitigation: 設計フェーズで「完了時のみ記録 (シンプル)」と「N 秒ごとフラッシュ (堅牢)」のどちらを採るか確定。MVP は前者推奨。
- **Risk: JsonUtility の Date 直列化** — Mitigation: `DateOnly` は使えないため `string yyyy-MM-dd` か `int yyyymmdd` で保持し、サービス内で変換ユーティリティを 1 箇所に集約。
- **Risk: 月送りと選択日の整合** — Mitigation: 要件 6.7-6.8 で「選択日は維持」「表示外ならハイライトのみ非描画でフッターは継続表示」と確定済み。実装時は `SelectedDate ∉ DisplayedMonth` の判定を 1 ヘルパに集約。
- **Risk: Inspector 上の `_historyButton` 未割当** — Mitigation: 設計フェーズ初動で Home.unity を開いて Button 参照と onClick イベントを目視確認。

## Effort & Risk

| 軸 | 評価 | 1 行根拠 |
|---|---|---|
| **Effort** | **M (5–8 日)** | 既存パターンに沿った Service / Scope 追加 + 新規カレンダー UI / セル Prefab + Pomodoro 配線 + Home 導線。新パターンは Calendar Grid のみ。 |
| **Risk** | **Medium** | カレンダー描画 + 月送り + 選択日の月またぎ + JsonUtility 上の日別レコード形式が新規。集中時間記録の確定タイミングに 1 つ要決定事項あり。 |

## Recommendations for Design Phase

1. **採用案**: Option C (Hybrid)。TimerRecord 系は RootScope Singleton、Streak/Calendar 計算は HistoryScope Scoped。
2. **設計フェーズで確定する事項**:
   - PomodoroPhase.Complete のみで記録するか / 中断時の扱い
   - 日別レコードのキー型 (`yyyy-MM-dd` 文字列 推奨)
   - 秒→分変換規則 (切り捨て 推奨)
   - 4 段階閾値の初期値
   - DayCell の Pool 戦略 (固定 42 セル Pool 推奨)
3. **設計フェーズで揃えるアセット**:
   - 4 段階猫アイコン Sprite (デザイナー受領)
   - `HistoryCalendarSettings.asset` (ScriptableObject)
   - `History.unity` 上の UI Prefab 構成 (Streak ラベル / 月ヘッダ / 7 列グリッド / フッター)
4. **PR 分割案** (実装フェーズ参考):
   - PR1: TimerRecordService + Snapshot + PlayerPrefsKey 追加 (Pomodoro連携含む)
   - PR2: HistoryScope 拡張 + Calendar UI + StreakCalculator
   - PR3: Home フッター History ボタン配線 + HistoryCalendarSettings アセット
