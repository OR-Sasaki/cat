# Implementation Plan

## Branches

**Base**: `feature/pomodoro-timer-scene`

| Branch | Tasks | Goal |
|--------|-------|------|
| `feature/pomodoro-timer-scene-core` | 1-2 | Pomodoroタイマーのコアロジックが動作し、ステートマシンとカウントダウンが機能する |
| `feature/pomodoro-timer-scene-ui` | 3-4 | 集中・休憩・完了の3パネルUIが表示され、ボタン操作とスライドアニメーションが機能する |
| `feature/pomodoro-timer-scene-visual` | 5 | 背景スクロールとキャラクターアニメーションが動作する |

## Tasks

### Branch: `feature/pomodoro-timer-scene-core`

- [x] 1. Pomodoroタイマーの状態管理とビジネスロジックを構築する
  - [x] 1.1 Pomodoroの全状態を保持し、イベント通知する状態クラスを作成する
    - 集中・休憩・完了の3フェーズを表す列挙型を定義する
    - 現在のフェーズ、タイマー残り時間、タイマー超過フラグ、現在のセット番号、総セット数、残りセット数、合計集中時間、一時停止フラグを保持する
    - フェーズ変更、タイマー更新（毎フレーム）、タイマー0到達、一時停止変更の4種類のActionイベントを提供する
    - セッターメソッド群を用意し、値変更時にイベントを発火する
    - 総セット数を初期設定するSetupメソッドを提供する
  - [x] 1.2 タイマーカウントダウン、フェーズ遷移、サイクル管理のサービスを作成する
    - コンストラクタでPomodoroState、PlayerPrefsService、CancellationTokenを受け取る
    - PlayerPrefsからタイマー設定（集中時間、休憩時間、セット数）を読み込み、集中フェーズでカウントダウンを開始する非同期メソッドを実装する
    - 設定が未保存の場合はデフォルト値（25分/5分/2セット）で開始する
    - UniTaskベースのタイマーループで毎フレーム残り時間を減算し、一時停止中はスキップする
    - 集中タイマーが0に到達したらタイマー超過フラグを立て、そのままカウントを継続する（超過時間表示用）
    - 集中中の経過時間を合計集中時間に累算する
    - 休憩フェーズへの遷移メソッドを実装する: 集中フェーズかつタイマー超過時のみ動作し、最終セット（CurrentSet == TotalSets）なら完了フェーズへ直接遷移、それ以外なら休憩タイマーを開始する
    - 次セットの集中フェーズへの遷移メソッドを実装する: 休憩フェーズかつタイマー超過時のみ動作し、セット番号を進めて集中タイマーを開始する
    - 一時停止と再開のメソッドを実装する
    - 事前条件不成立の遷移呼び出しはスキップしDebug.LogWarningでログ出力する
  - _Requirements: 1.1, 1.2, 1.3, 1.4, 1.5, 3.1, 3.2, 3.3, 3.4, 5.1, 5.2, 5.3_

- [x] 2. TimerScopeの拡張とシーン起動処理を実装する
  - [x] 2.1 TimerScopeにCancellationTokenの生成とPomodoro関連コンポーネントのDI登録を追加する
    - CancellationTokenSourceを生成し、CancellationTokenをコンテナに登録する
    - PomodoroStateとPomodoroServiceをScoped登録する
    - TimerStarterをエントリーポイントとして登録する
    - 既存のReturnButtonView登録を削除する
    - OnDestroyでCancellationTokenSourceをCancel・Disposeする
  - [x] 2.2 シーン起動時にタイマー設定を読み込みPomodoroサービスを開始するStarterを作成する
    - IStartableを実装し、Start()でPomodoroServiceの非同期開始メソッドを呼び出す
    - FadeStarterと同じasync void Startパターンで例外をcatchしDebug.LogErrorでログ出力する
  - _Requirements: 1.1, 5.1, 9.3_

### Branch: `feature/pomodoro-timer-scene-ui`

- [x] 3. 集中・休憩・完了の3パネルUIを実装する
  - [x] 3.1 集中フェーズのUIパネルを作成する
    - タイマー残り時間をmm:ss形式で表示し、PomodoroStateのタイマー更新イベントで毎フレーム更新する
    - タイマー超過後は0:00からの経過時間を表示する
    - タイマー超過時に「休憩しよう」メッセージを表示する
    - 休憩ボタンをタイマー超過時のみ表示し、押下でPomodoroServiceの休憩遷移を呼び出す
    - 一時停止/再開ボタンを実装し、PomodoroServiceの一時停止/再開を呼び出す
    - ホームボタンを実装し、SceneLoaderでホームシーンへ遷移する
    - 残りセット数と合計集中時間を常時表示する
    - OnDestroyでイベント購読を解除する
  - [x] 3.2 休憩フェーズのUIパネルを作成する
    - 集中パネルと対称的な構造で、休憩タイマーの残り時間をmm:ss形式で表示する
    - タイマー超過時に「集中しよう」メッセージを表示する
    - 集中ボタンをタイマー超過時のみ表示し、押下でPomodoroServiceの集中遷移を呼び出す
    - 一時停止/再開ボタン、ホームボタン、残りセット数、合計集中時間の表示は集中パネルと同様
    - OnDestroyでイベント購読を解除する
  - [x] 3.3 完了フェーズのUIパネルを作成する
    - 合計集中時間を表示する
    - ホームボタンのみ表示し、SceneLoaderでホームシーンへ遷移する
  - [x] 3.4 TimerScopeに3パネルのView登録を追加する
    - FocusPanelView、BreakPanelView、CompletePanelViewをRegisterComponentInHierarchyで登録する
  - _Requirements: 1.2, 1.3, 2.1, 2.2, 2.3, 2.4, 2.5, 2.6, 3.1, 3.2, 3.3, 3.4, 3.5, 3.6, 4.1, 4.2, 4.3, 4.4, 5.4, 9.1, 9.2_

- [x] 4. UIスライドアニメーションによるパネル切り替え演出を実装する
  - [x] 4.1 フェーズ変更に応じて3面パネルをスライドイン/アウトするManagerを作成する
    - MonoBehaviourとして実装し、3パネルのRectTransformとAnimationCurveをSerializeFieldで受け取る
    - 初期状態で集中パネルを画面内、休憩・完了パネルを画面外（右側）に配置する
    - PomodoroStateのフェーズ変更イベントを購読し、フェーズに応じてスライドアニメーションを実行する
    - 現在のパネルを左へスライドアウトし、次のパネルを右からスライドインする（同時実行）
    - AnimationCurve + UniTaskでRectTransformのanchoredPositionを補間する
    - OnDestroyでイベント購読を解除する
  - [x] 4.2 TimerScopeにUiSlideManagerの登録を追加する
  - _Requirements: 6.1, 6.2, 6.3, 6.4_

### Branch: `feature/pomodoro-timer-scene-visual`

- [x] 5. 背景スクロールとキャラクターアニメーションを実装する
  - [x] 5.1 (P) 背景を右から左へ無限スクロールさせるViewを作成する
    - 2枚の背景スプライトを横に並べ、Updateで毎フレーム左方向に移動する
    - 画面外に出たスプライトを反対側に再配置し、無限ループを実現する
    - スクロール速度をSerializeFieldで設定可能にする
    - PomodoroStateのフェーズ変更イベントを購読し、完了フェーズでスクロールを停止する
    - PomodoroStateの一時停止イベントを購読し、一時停止時にスクロールを停止する
    - OnDestroyでイベント購読を解除する
  - [x] 5.2 (P) キャラクターの走行・完了アニメーションを制御するViewを作成する
    - AnimatorコンポーネントをSerializeFieldで参照する
    - PomodoroStateのフェーズ変更イベントを購読し、集中/休憩中は走行アニメーション、完了時は完了モーションを再生する
    - PomodoroStateの一時停止イベントを購読し、一時停止時にAnimator.speedを0にする
    - OnDestroyでイベント購読を解除する
  - [x] 5.3 TimerScopeにBackgroundScrollViewとTimerCharacterViewの登録を追加する
  - _Requirements: 7.1, 7.2, 8.1, 8.2, 8.3_

## Requirements Coverage

| 要件 | タスク |
|------|--------|
| 1.1 | 1.2, 2.2 |
| 1.2 | 1.2, 3.1 |
| 1.3 | 1.2, 3.2 |
| 1.4 | 1.2 |
| 1.5 | 1.1 |
| 2.1 | 3.1 |
| 2.2 | 3.2 |
| 2.3 | 3.1 |
| 2.4 | 3.3 |
| 2.5 | 3.1, 3.2 |
| 2.6 | 3.1, 3.2 |
| 3.1 | 1.2, 3.1 |
| 3.2 | 1.2, 3.2 |
| 3.3 | 1.2, 3.1, 3.2 |
| 3.4 | 1.2, 3.1, 3.2 |
| 3.5 | 3.1, 3.2, 3.3 |
| 3.6 | 3.3 |
| 4.1 | 3.1 |
| 4.2 | 3.1 |
| 4.3 | 3.2 |
| 4.4 | 3.2 |
| 5.1 | 1.2 |
| 5.2 | 1.2 |
| 5.3 | 1.2 |
| 5.4 | 3.3 |
| 6.1 | 4.1 |
| 6.2 | 4.1 |
| 6.3 | 4.1 |
| 6.4 | 4.1 |
| 7.1 | 5.2 |
| 7.2 | 5.2 |
| 8.1 | 5.1 |
| 8.2 | 5.1 |
| 8.3 | 5.1 |
| 9.1 | 3.1, 3.2 |
| 9.2 | 3.1, 3.2, 3.3 |
| 9.3 | 2.1 |
