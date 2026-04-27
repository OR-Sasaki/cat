# Requirements Document

## Project Description (Input)
タイマーシーンの実装。タイマーシーンでは、Pomodoroタイマーを使うことができます。Pomodoro Timerは3つのステップがあり、集中、休憩、完了の3種類のステータスがあります。画面内には、タイマーの時計とホームシーンへ戻るボタン、ステートを集中と休憩に切り替えるボタン、一時停止がある。完了時には集中時間の合計とホームシーンへ戻るボタンがある。タイマーの後ろではキャラクターがアニメーションしています。キャラクターの後ろでは背景が右から左へ流れています。キャラクターのアニメーションは、その流れている地面を走っているような感じです。ホームシーンからタイマーシーンに遷移する時点で、Pomodoroタイマーの設定は完了しているので、遷移後すぐからタイマーがスタートする。タイマーがスタートしたときは集中タイマー。集中タイマーが0になった後もタイマーは動いており、0になったら休憩しようという文言が表示されます。ユーザーが休憩ボタンを押すと休憩ステートに移行し、休憩タイマーが終わると「集中しよう」という文言が表示され、ユーザーは集中ボタンを押すことになります。これをセット数の数だけ繰り返し、セット数の回数分、集中時間が完了したら完了ステートに移行します。

## Introduction
本ドキュメントは、Pomodoroタイマーシーン（TimerScene）の要件を定義する。タイマーシーンは、ホームシーンからの遷移後に即座にPomodoroタイマーを開始し、集中・休憩・完了の3つのステートを管理するシーンである。ユーザーは設定済みのタイマー設定（集中時間、休憩時間、セット数）に基づいてPomodoroサイクルを実行する。

## Requirements

### Requirement 1: タイマーステート管理
**Objective:** ユーザーとして、Pomodoroタイマーの集中・休憩・完了のステートを明確に把握したい。それにより、現在の作業フェーズを常に認識できる。

#### Acceptance Criteria
1. When タイマーシーンへの遷移が完了した, the TimerScene shall 集中ステートでタイマーを即座に開始する
2. When 集中タイマーが0に到達した, the TimerScene shall タイマーのカウントを継続し「休憩しよう」というメッセージを表示する
3. When 休憩ステート中に休憩タイマーが0に到達した, the TimerScene shall 「集中しよう」というメッセージを表示する
4. When すべてのセットの集中時間が完了した, the TimerScene shall 完了ステートに遷移する
5. The TimerScene shall 集中・休憩・完了の3種類のステートを持つ

### Requirement 2: タイマー表示
**Objective:** ユーザーとして、残り時間・残りセット数・これまでの合計集中時間をリアルタイムで確認したい。それにより、時間配分と進捗を意識して作業できる。

#### Acceptance Criteria
1. While 集中ステート中, the TimerScene shall 集中タイマーの残り時間を時計形式で表示する
2. While 休憩ステート中, the TimerScene shall 休憩タイマーの残り時間を時計形式で表示する
3. When 集中タイマーが0に到達した後, the TimerScene shall 0:00からの経過時間を表示し続ける
4. While 完了ステート中, the TimerScene shall 集中時間の合計を表示する
5. While 集中ステートまたは休憩ステート中, the TimerScene shall 残りのセット数を表示する
6. While 集中ステートまたは休憩ステート中, the TimerScene shall これまでの合計集中時間を表示する

### Requirement 3: ユーザー操作
**Objective:** ユーザーとして、タイマーの一時停止やステート切り替え、ホームへの帰還を操作したい。それにより、自分のペースでPomodoroサイクルを進められる。

#### Acceptance Criteria
1. When ユーザーが休憩ボタンを押した, the TimerScene shall 休憩ステートに遷移し休憩タイマーを開始する
2. When ユーザーが集中ボタンを押した, the TimerScene shall 次のセットの集中ステートに遷移し集中タイマーを開始する
3. When ユーザーが一時停止ボタンを押した, the TimerScene shall タイマーのカウントを一時停止する
4. When ユーザーが一時停止中に再開ボタンを押した, the TimerScene shall タイマーのカウントを再開する
5. When ユーザーがホームボタンを押した, the TimerScene shall ホームシーンへ遷移する
6. While 完了ステート中, the TimerScene shall ホームシーンへ戻るボタンのみを表示する

### Requirement 4: ステート切り替えボタンの制御
**Objective:** ユーザーとして、現在のステートに適したボタンのみを操作したい。それにより、誤操作なくPomodoroサイクルを進められる。

#### Acceptance Criteria
1. While 集中タイマーが0に到達していない間, the TimerScene shall 休憩ボタンを非表示にする
2. When 集中タイマーが0に到達した, the TimerScene shall 休憩ボタンを表示する
3. While 休憩タイマーが0に到達していない間, the TimerScene shall 集中ボタンを非表示にする
4. When 休憩タイマーが0に到達した, the TimerScene shall 集中ボタンを表示する

### Requirement 5: Pomodoroサイクル管理
**Objective:** ユーザーとして、設定したセット数に基づいてPomodoroサイクルを完了したい。それにより、計画的な作業と休憩のリズムを実現できる。

#### Acceptance Criteria
1. The TimerScene shall ホームシーンから渡されたタイマー設定（集中時間、休憩時間、セット数）を使用する
2. When 1セットの集中時間が完了しユーザーが休憩ボタンを押した, the TimerScene shall 休憩タイマーを開始し現在のセット数を1つ進める
3. When セット数の回数分の集中時間がすべて完了した, the TimerScene shall 完了ステートに遷移する
4. While 完了ステート中, the TimerScene shall 全セットを通じた集中時間の合計を表示する

### Requirement 6: UI切り替え演出
**Objective:** ユーザーとして、ステート切り替え時にUIがスライドするアニメーション演出を見たい。それにより、フェーズの切り替わりを視覚的に実感できる。

#### Acceptance Criteria
1. The TimerScene shall 集中UI・休憩UI・完了UIをそれぞれ独立したUIとして持つ
2. When 集中ステートから休憩ステートに切り替わった, the TimerScene shall 集中UIを画面外へスライドアウトし、休憩UIを画面外から画面内へスライドインする
3. When 休憩ステートから集中ステートに切り替わった, the TimerScene shall 休憩UIを画面外へスライドアウトし、集中UIを画面外から画面内へスライドインする
4. When 完了ステートに遷移した, the TimerScene shall 現在表示中のUI（集中UIまたは休憩UI）を画面外へスライドアウトし、完了UIを画面外から画面内へスライドインする

### Requirement 7: キャラクターアニメーション
**Objective:** ユーザーとして、タイマー中にキャラクターのアニメーションを見たい。それにより、楽しみながら集中・休憩サイクルを過ごせる。

#### Acceptance Criteria
1. While 集中ステートまたは休憩ステート中, the TimerScene shall キャラクターが地面を走っているようなアニメーションを再生する
2. When 完了ステートに遷移した, the TimerScene shall キャラクターが完了モーションを再生する

### Requirement 8: 背景スクロール
**Objective:** ユーザーとして、流れる背景でキャラクターの走行感を感じたい。それにより、視覚的にタイマーの進行を体感できる。

#### Acceptance Criteria
1. While 集中ステートまたは休憩ステート中, the TimerScene shall 背景を右から左へスクロールさせる
2. The TimerScene shall キャラクターの後ろに背景を配置する
3. When 完了ステートに遷移した, the TimerScene shall 背景スクロールを停止する

### Requirement 9: シーン遷移
**Objective:** ユーザーとして、ホームシーンとタイマーシーンの間をスムーズに行き来したい。それにより、アプリ全体の使い勝手が良い。

#### Acceptance Criteria
1. When ホームシーンからタイマーシーンへの遷移が要求された, the TimerScene shall フェード効果を用いたシーン遷移を実行する
2. When タイマーシーンからホームシーンへの遷移が要求された, the TimerScene shall フェード効果を用いたシーン遷移を実行する
3. The TimerScene shall 既存のSceneLoaderを使用してシーン遷移を行う
