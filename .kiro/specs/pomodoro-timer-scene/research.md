# リサーチ & デザイン決定ログ

## サマリー
- **フィーチャー**: `pomodoro-timer-scene`
- **ディスカバリースコープ**: Extension（既存 Timer シーン scaffold の拡張）
- **主要な知見**:
  - Timer シーンの scaffold（フォルダ構造・TimerScope・ReturnButtonView）は作成済みだが、ロジックは未実装
  - タイマー設定データ（TimerSettingData）は PlayerPrefs 経由で永続化済み。シーン遷移時に読み出すだけで受け渡し可能
  - DOTween 等の Tween ライブラリは未導入。AnimationCurve + UniTask による手動アニメーションが既存パターン

## リサーチログ

### タイマー設定データの受け渡し
- **コンテキスト**: ホームシーンで設定した Pomodoro パラメータをタイマーシーンで取得する方法
- **調査先**: `TimerSettingDialog.cs`, `PlayerPrefsService.cs`, `HomeFooterView.cs`
- **知見**:
  - `TimerSettingDialog` は「開始」ボタン押下時に `PlayerPrefsService.Save<TimerSettingData>()` で永続化
  - `HomeFooterView` は DialogResult.Ok 後に `SceneLoader.Load("Timer")` を呼び出す
  - タイマーシーン側で `PlayerPrefsService.Load<TimerSettingData>()` するだけで取得可能
- **影響**: 追加のデータ受け渡し機構は不要。既存の PlayerPrefs パターンをそのまま利用

### UI アニメーション方式
- **コンテキスト**: 集中/休憩/完了 UI のスライド切替アニメーション実装方式
- **調査先**: `FadeView.cs`（AnimationCurve パターン）、パッケージ一覧
- **知見**:
  - `FadeView` は `AnimationCurve.EaseInOut` + `Task.Yield()` ループでフェードアニメーションを実現
  - DOTween / LeanTween / iTween は未導入
  - UniTask の `UniTask.Yield()` を使えば同様のパターンで RectTransform のスライドアニメーションが実装可能
- **影響**: AnimationCurve + UniTask によるスライドアニメーションを採用。新規ライブラリ不要

### キャラクターアニメーション
- **コンテキスト**: タイマーシーンでのキャラクター走行・完了アニメーション
- **調査先**: `CharacterView.cs`, Unity 2D Animation パッケージ
- **知見**:
  - 既存の `CharacterView` はスプライト差し替え専用。Animator コンポーネントへの参照を持たない
  - プロジェクトには 2D Animation 12.0.3 が導入済み
  - タイマーシーン用のキャラクターは `CharacterView` とは別に、Animator 付きの専用 View が必要
- **影響**: タイマーシーン専用の `TimerCharacterView` を新規作成し、Animator を制御する設計とする

### 背景スクロール
- **コンテキスト**: キャラクターの後ろで右→左に流れる背景の実装
- **調査先**: 既存コードベース
- **知見**:
  - 既存コードベースにスクロール背景の実装はない
  - 2D 横スクロール背景の一般的な手法: (1) Transform 移動 + ループ配置、(2) マテリアル UV オフセット
  - Transform 移動 + 2枚のスプライトを交互配置するパターンが最もシンプル
- **影響**: MonoBehaviour の Update で Transform.Translate を使用し、画面外に出たスプライトを反対側に再配置するループ方式を採用

## アーキテクチャパターン評価

| オプション | 説明 | 強み | リスク/制約 | 備考 |
|-----------|------|------|-----------|------|
| フラット Service/State | 1つの PomodoroService + PomodoroState で全ロジック管理 | シンプル、ファイル数少 | Service 肥大化リスク | 既存パターンと一致 |
| ステートパターン | フェーズごとにクラス分離 + ステートマシン | 拡張性高、責務分離 | オーバーエンジニアリング | 3ステートには過剰 |
| **ハイブリッド（採用）** | enum ベースステートマシン + Action イベント通知 | シンプルかつ拡張可能 | 特になし | 既存の複雑度に合致 |

## デザイン決定

### 決定: ステート管理方式
- **コンテキスト**: Pomodoro の 3 フェーズ（Focus/Break/Complete）をどう管理するか
- **代替案**:
  1. ステートパターン（ITimerPhase インターフェース + 各フェーズクラス）
  2. enum + switch ベースのシンプルなステートマシン
- **採用**: enum + Action イベント通知
- **理由**: 3 ステートの単純な遷移であり、ステートパターンは過剰。enum で十分な表現力があり、既存コードベースの複雑度に合致
- **トレードオフ**: シンプルさと引き換えに、フェーズ追加時は Service の修正が必要
- **フォローアップ**: なし

### 決定: View への状態通知方式
- **コンテキスト**: PomodoroService から各 View にフェーズ変更やタイマー更新を通知する方式
- **代替案**:
  1. Action/event ベースの通知（C# events / Action delegate）
  2. UniRx の Observable パターン
  3. VContainer の ITickable + ポーリング
- **採用**: Action delegate（`Action<PomodoroPhase>` 等）を PomodoroState に配置
- **理由**: UniRx は未導入。Action delegate はシンプルで既存パターンに合致。View が State のイベントを購読する形
- **トレードオフ**: リアクティブストリームの合成はできないが、この機能では不要
- **フォローアップ**: なし

### 決定: ReturnButtonView の扱い
- **コンテキスト**: 既存の ReturnButtonView をそのまま使うか、新しい統合 View に組み込むか
- **代替案**:
  1. 既存 ReturnButtonView をそのまま活用（完了画面にも同じボタンを配置）
  2. 新規の統合 View に組み込み、ステートに応じた表示制御を行う
- **採用**: 既存 ReturnButtonView を削除し、各 UI パネル（集中/休憩/完了）にホームボタンを組み込む
- **理由**: 各パネルがスライドイン/アウトするため、パネル内にボタンを含める方が自然。完了画面ではホームボタンのみ表示という要件にも合致
- **トレードオフ**: 既存コードの削除が発生するが、わずか 18 行の View であり影響は軽微
- **フォローアップ**: TimerScope から ReturnButtonView の登録を削除

## リスク & 緩和策
- **アニメーションアセット不足** — 走行スプライト・背景テクスチャが未準備の場合、仮素材（単色スプライト等）で実装を進め、後からアセット差し替え
- **タイマー精度（バックグラウンド復帰）** — `Time.deltaTime` ベースではアプリバックグラウンド時にタイマーが停止する。MVP ではこの挙動を許容し、必要に応じて `DateTime` ベースの補正を後から追加
- **UI レイアウト調整** — 3面 UI のスライドアニメーションは RectTransform の配置に依存。プレハブ設計時に画面外の初期位置を正確に設定する必要がある

## 参考資料
- Unity AnimationCurve: 既存 `FadeView.cs` の実装パターン
- UniTask: プロジェクト標準の非同期ライブラリ
- VContainer: プロジェクト標準の DI フレームワーク
