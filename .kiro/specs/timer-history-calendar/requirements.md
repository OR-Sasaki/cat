# Requirements Document

## Project Description (Input)
タイマーの記録を確認できるカレンダー画面を実装したい。画面のイメージは @history_image.png である。「何日連続でタイマーを使ったか」「集中時間によって日別のアイコンの色が変わるカレンダー」「選択日の集中時間」「合計の集中時間」を表示したい。また、タイマーの記録は現在実装されていないので、そこから実装する必要がある。

## Introduction
本機能は、Pomodoro/タイマー機能で集中した時間を日単位で記録・蓄積し、Historyシーンのカレンダーレイアウト上で可視化する機能である。
プレイヤーは「いつ、どれだけ集中したか」を一目で把握でき、連続使用日数 (ストリーク) によって継続的なプレイ動機を得られる。本機能のスコープは以下の二系統で構成される。

1. **タイマー集中時間の記録 (新規実装)**: 既存のタイマー機能 (`Timer` シーン内 Pomodoro) と統合し、完了した集中時間を日別に永続化する記録サービス。
2. **履歴カレンダー画面 (Historyシーン拡張)**: 既存の薄い `History` シーンに、月別カレンダー・連続日数・選択日と合計の集中時間サマリ表示を追加する。

UIイメージは `history_image.png` を基準とする 
(連続日数の大見出し、月送り対応の7×N週カレンダー、日別の猫アイコンが集中時間量で色変化、選択日のハイライト、フッターに選択日と合計時間)。永続化は既存パターン (PlayerPrefs) に倣い、時刻取得は `IClock` 経由で行う。

集中時間の単位方針は次のとおりとする。

- **内部記録単位**: 秒 (永続化・計算はすべて秒で扱う)
- **画面表示単位**: 分 (連続日数表記を除き、ストリーク以外の集中時間表示はすべて分で行う)
- **秒→分の変換規則**: 切り捨て (`floor(seconds / 60)`) で統一する。例: 89 秒 → 1 分、3599 秒 → 59 分。

集中セッションの記録方針は次のとおりとする。

- **記録対象**: セッションが正常完了した場合だけでなく、ホーム遷移・他シーン遷移・タイマー一時停止後の離脱・アプリ終了等で **中断** された場合も、それまでに累積した集中時間 (秒) を当日の日別記録へ加算する。
- セッションごとに再加算が発生しないよう、確定済みの集中時間は二重加算しない。
- **加算先日付**: 集中時間は記録の確定 (Flush) 呼出時点の `IClock` 当日 (ローカル日付) に一括加算する。日跨ぎセッション (例: 23:55 開始 → 0:30 完了) を開始日に按分する処理は本機能の対象外とし、Flush 時点の当日に一括加算される挙動を許容する。
- **アプリ強制終了の例外**: OS による即時 kill 等で Pause/Quit ライフサイクルが呼ばれない場合の未確定秒数は救済対象外とする。

履歴カレンダー画面の表示更新方針は次のとおりとする。

- **日跨ぎ自動更新は対象外**: 履歴カレンダー画面を開いたまま 0 時を跨いだ場合、「今日の合計集中時間」やストリーク等の当日基準の表示を自動で再計算しない。再評価のトリガはユーザー操作 (月送り / 日付タップ) およびシーン再入場のみとする。

日別アイコンの色段階は、コードで動的にカラーを生成するのではなく、用意された 4 段階分のアイコン画像 (Sprite) を集中時間量に応じて差し替える方式で実現する。アイコン Sprite (4 枚) はデザイナーから受領済みで、本機能のアセットフォルダに配置可能な状態である。

4 段階のマッピング閾値は次の初期値で確定する (`ScriptableObject` 等で外部化し、後日コード改修なしに調整可能とする)。

| 段階 | 当日の集中時間 (合計, 秒) | 用途 |
|---|---|---|
| 段階 1: 未着手 | `seconds == 0` | 集中時間 0 の日 |
| 段階 2: 〜25 分 | `0 < seconds < 25 * 60` | 1 ポモドーロ未満 |
| 段階 3: 〜60 分 | `25 * 60 ≤ seconds < 60 * 60` | 1 ポモドーロ以上、1 時間未満 |
| 段階 4: 60 分超 | `60 * 60 ≤ seconds` | 1 時間以上 |

## Requirements

### Requirement 1: タイマー集中時間の記録
**Objective:** プレイヤーとして、タイマーで集中した時間が日別に自動で蓄積されてほしい。中断したときの集中時間も無駄にせず加算されることで、後から自身の集中履歴を正確に振り返ることができる。

#### Acceptance Criteria
1. When プレイヤーがタイマーで集中セッションを正常完了した場合, the Timer Record Service shall 当該セッションの集中時間を秒単位で、ローカル日付の日別記録に加算する。
2. When プレイヤーが集中セッション中にホーム遷移・他シーン遷移などでセッションを中断した場合, the Timer Record Service shall それまでに累積した集中時間 (秒) を、ローカル日付の日別記録に加算する。
3. When プレイヤーが集中セッションを一時停止した状態でアプリを終了 / 強制終了した場合, the Timer Record Service shall それまでに確定済みの集中時間 (秒) を当日の日別記録から失わせない。
4. When 同じ日付に複数回の集中セッション (正常完了・中断を問わず) が発生した場合, the Timer Record Service shall 当該日付の集中時間を秒単位の累計値として加算保持する。
5. The Timer Record Service shall 一度記録に加算した集中時間を、同一セッションの後続イベントによって二重に加算しない。
6. The Timer Record Service shall 集中時間記録を秒単位の整数値として保持し、内部計算 (合計値・ストリーク判定の入力等) もすべて秒単位で行う。
7. The Timer Record Service shall 日付の判定を `IClock` から取得した現在時刻のローカル日付で行う。
8. The Timer Record Service shall 集中時間記録を `PlayerPrefsService` 経由で永続化し、アプリ再起動後も秒単位の値を復元する。
9. If 永続化されたデータが存在しない初回起動である場合, the Timer Record Service shall 空の記録セットを初期状態として返す。
10. If 加算しようとする集中時間が 0 秒以下である場合, the Timer Record Service shall 当該加算を記録対象から除外する。
11. The Timer Record Service shall 記録の戻り値をイミュータブルなスナップショット形式で提供する (プロジェクトの State Snapshot Pattern に準拠)。

### Requirement 2: 履歴カレンダー画面への遷移
**Objective:** プレイヤーとして、Home画面から履歴カレンダーへ遷移し、戻ることができてほしい。これにより、集中履歴の閲覧を任意のタイミングで行える。

#### Acceptance Criteria
1. When プレイヤーが履歴閲覧の導線 (Homeシーン上のエントリ) を操作した場合, the Scene Loader shall フェード遷移で `History` シーンを開く。
2. When プレイヤーが履歴画面の戻るボタンを操作した場合, the Scene Loader shall フェード遷移で前のシーン (Home) に戻る。
3. While `History` シーン遷移が処理中である間, the Scene Loader shall 多重遷移を抑止する。

### Requirement 3: 月別カレンダーの表示と月送り
**Objective:** プレイヤーとして、任意の月のカレンダーを閲覧し、前月・翌月へ移動できてほしい。これにより、過去の集中履歴を時系列で振り返れる。

#### Acceptance Criteria
1. When `History` シーンが開かれた場合, the History Calendar View shall `IClock` から取得した現在月のカレンダーを初期表示する。
2. The History Calendar View shall カレンダーヘッダーに表示中の年月 (例: `2026/1`) を表示する。
3. The History Calendar View shall 曜日ヘッダーを日・月・火・水・木・金・土の順に表示する。
4. The History Calendar View shall 表示中月の各日付セルを 7 列 × 必要週数のグリッドで配置する。
5. When プレイヤーが「次月」ボタンを操作した場合, the History Calendar View shall 表示対象を翌月に切り替えて再描画する。
6. When プレイヤーが「前月」ボタンを操作した場合, the History Calendar View shall 表示対象を前月に切り替えて再描画する。
7. The History Calendar View shall 表示中月以外の日付セル (前月末・翌月頭の余白) を視覚的に区別 (淡色等) して描画する。

### Requirement 4: 集中時間に応じた日別アイコン画像の差し替え
**Objective:** プレイヤーとして、各日付の集中量を一目で把握できてほしい。これにより、集中量の多かった日と少なかった日をカレンダー上で直感的に識別できる。デザイナーとして、コード変更なしに用意した画像差し替えのみで色段階のビジュアルを調整できてほしい。

#### Acceptance Criteria
1. The History Calendar View shall 各日付セルに猫シルエットアイコンを 1 枚の Sprite として描画する。
2. The History Calendar View shall 集中時間量に対応する 4 段階分の Sprite アセット (未着手 / 〜25 分 / 〜60 分 / 60 分超 の 4 枚) を外部から差し替え可能な形で参照する。
3. The History Calendar View shall 当該日の集中時間 (秒) を次の規則で 4 段階にマッピングし、対応する Sprite を当該日付セルのアイコンに割り当てる: 段階 1 (未着手) は `seconds == 0`、段階 2 (〜25 分) は `0 < seconds < 1500`、段階 3 (〜60 分) は `1500 ≤ seconds < 3600`、段階 4 (60 分超) は `3600 ≤ seconds`。
4. If 当該日の集中時間が 0 秒である場合, the History Calendar View shall 段階 1 (未着手) に割り当てられた Sprite を当該セルに表示する。
5. The History Calendar View shall 4 段階のマッピング閾値 (初期値: 0 秒, 1500 秒 = 25 分, 3600 秒 = 60 分) を `ScriptableObject` 等の設定アセットとして外部化し、コード改修なしに調整可能にする。
6. The History Calendar View shall コード上で色値 (`Color`) を生成・乗算してアイコンの段階表現を行わず、段階表現は Sprite 画像の差し替えのみで実現する。
7. The History Calendar View shall 表示中月以外の日付セル (前月末・翌月頭の余白) についても 4 段階の Sprite 割り当てルールを同様に適用する。

### Requirement 5: 連続使用日数 (ストリーク) の表示
**Objective:** プレイヤーとして、自分が何日連続でタイマーを使い続けているかを大きく表示してほしい。これにより、継続のモチベーションを維持できる。

#### Acceptance Criteria
1. When `History` シーンが開かれた場合, the History Calendar View shall 画面上部に現在の連続使用日数を `[N] DAY 連続中！` 形式で表示する。
2. The Streak Calculator shall 連続使用日数を「`IClock` の現在ローカル日付 (当日) から過去方向に向かって、集中時間記録が途切れずに連続している日数」として算出する。
3. If 当日の集中時間記録が存在しない場合, the Streak Calculator shall 連続日数を 0 として算出する (当日未記録は連続が途切れたものとみなす)。
4. While 当日から過去方向に集中時間記録のある日付が連続している間, the Streak Calculator shall 連続日数を 1 ずつ加算する。
5. When 過去方向に走査して集中時間記録のない日付に到達した場合, the Streak Calculator shall その日付以降は加算せず、その時点までのカウントをストリーク値として確定する。
6. If 集中時間記録が一切存在しない場合, the Streak Calculator shall 連続日数を 0 として算出する。
7. The Streak Calculator shall 日付の境界判定を `IClock` のローカル日付で行う。

### Requirement 6: 選択日の集中時間表示
**Objective:** プレイヤーとして、カレンダー上の任意の日付を選択して、その日の集中時間を確認したい。これにより、特定日の集中量を詳細に把握できる。

#### Acceptance Criteria
1. The History Calendar View shall シーン初期表示時に当日の日付を選択状態として描画する。
2. When プレイヤーがカレンダー上の日付セルを操作した場合, the History Calendar View shall 当該日付を新たな選択日に切り替える。
3. The History Calendar View shall 選択日のセルを視覚的に強調 (背景色ハイライト等) して他の日付と区別する。
4. The History Calendar View shall フッター領域に選択日の日付 (例: `11/14`) を表示する。
5. The History Calendar View shall フッター領域に選択日の集中時間を分単位で表示する。
6. If 選択日に集中時間記録が存在しない場合, the History Calendar View shall 集中時間を 0 分として表示する。
7. When プレイヤーが月送りで表示月を切り替えた場合, the History Calendar View shall 選択日 (年月日) を維持し、別月の選択日として保持し続ける。
8. While 選択日が現在表示中の月に含まれていない場合, the History Calendar View shall カレンダーグリッド上の選択ハイライトを描画しない一方で、フッター領域には選択日の日付・集中時間を引き続き表示する。

### Requirement 7: 合計集中時間の表示
**Objective:** プレイヤーとして、月単位および当日単位の合計集中時間を確認できてほしい。これにより、長期的な集中量と当日の進捗を同時に把握できる。

#### Acceptance Criteria
1. The History Calendar View shall フッター領域に「今月の合計集中時間」を分単位で表示する。
2. The History Calendar View shall 「今月の合計集中時間」を、現在カレンダーで表示中の月における全日付の集中時間 (秒) の合計を分単位に変換した値として算出する。
3. When プレイヤーが月送りで表示月を切り替えた場合, the History Calendar View shall 「今月の合計集中時間」を切り替え後の月の合計値で再描画する。
4. The History Calendar View shall フッター領域に「今日の合計集中時間」を、`IClock` の現在ローカル日付における集中時間 (秒) を分単位に変換した値として表示する。
5. If 集中時間記録が存在しない場合, the History Calendar View shall 該当の合計値を 0 分として表示する。
6. The History Calendar View shall 集中時間の表示単位を画面内で「分」に統一する (連続日数表記である `[N] DAY` を除く)。
7. The History Calendar View shall 秒から分への変換を切り捨て (`floor(seconds / 60)`) で行い、画面全体でこの変換規則を一貫して適用する。

### Requirement 8: 既存アーキテクチャとの整合
**Objective:** 開発者として、本機能が既存のシーンベース + VContainer DI アーキテクチャに整合した形で実装されていてほしい。これにより、保守性とテスト容易性を維持できる。

#### Acceptance Criteria
1. The Timer History Calendar System shall タイマー記録サービス・履歴ビューを `Assets/Scripts/{SceneName}/{Layer}/` 構造 (Service / State / View / Scope / Starter / Manager) に従って配置する。
2. The Timer History Calendar System shall 依存方向を View → Service → State の単方向に保ち、逆方向の依存を持たない。
3. The Timer History Calendar System shall 全シーンから利用される記録サービスを `RootScope` に `Lifetime.Singleton` で登録するか、`HistoryScope` 内で適切なライフタイムで登録する。
4. The Timer History Calendar System shall 現在時刻の取得を必ず `IClock` 経由で行い、`DateTime.Now` / `DateTimeOffset.UtcNow` を直接呼び出さない。
5. The Timer History Calendar System shall 永続化を `PlayerPrefsService` 経由で行い、生の `PlayerPrefs` API を直接呼び出さない。
6. The Timer History Calendar System shall 非同期メソッドの末尾引数に `CancellationToken` を受け取り、外部からのキャンセルを可能にする。
7. The Timer History Calendar System shall VContainer 注入対象のコンストラクタに `[Inject]` 属性を付与する。
