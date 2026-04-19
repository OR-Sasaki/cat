# Implementation Plan

## Branches

**Base**: `feature/home-to-timer-scene-transition`

| Branch | Tasks | Goal |
|--------|-------|------|
| `feature/home-to-timer-scene-transition-setting-dialog` | 1-2 | タイマー設定ダイアログの表示・操作・永続化が動作する |
| `feature/home-to-timer-scene-transition-scene-flow` | 3-4 | ダイアログからタイマーシーンへの遷移とホーム復帰が動作する |

## Tasks

### Branch: `feature/home-to-timer-scene-transition-setting-dialog`

- [x] 1. タイマー設定データの永続化基盤を構築する
  - [x] 1.1 タイマー設定データクラスを作成する
    - 集中時間（デフォルト25分）、休憩時間（デフォルト5分）、セット回数（デフォルト2回）を保持するSerializableなデータクラスを定義する
    - JsonUtilityでシリアライズ可能な形式にする
  - [x] 1.2 PlayerPrefsServiceにタイマー設定の永続化キーを追加する
    - PlayerPrefsKey enumに新しいキーを追加する
    - 既存のSave/Loadメソッドでタイマー設定データの読み書きが正しく動作することを確認する
  - _Requirements: 2.1, 2.2, 2.3, 2.5, 2.6_

- [x] 2. タイマー設定ダイアログを作成する
  - [x] 2.1 タイマー設定ダイアログのスクリプトを実装する
    - BaseDialogViewを継承し、集中時間・休憩時間・セット回数の表示・増減UIを構成する
    - ダイアログ表示時にPlayerPrefsから前回の設定値を読み込み、データがない場合はデフォルト値を使用する
    - +/-ボタンで各設定値を増減し、範囲制約（集中時間1〜60分、休憩時間1〜60分、セット回数1〜10回）を適用する
    - Startボタン押下時に設定値をPlayerPrefsに保存し、Ok結果でダイアログを閉じる
    - ×ボタンによる閉じる機能はBaseDialogViewの標準機能を利用する（×ボタン押下時も設定値を保存する）
    - PlayerPrefsServiceをVContainerのDI注入で受け取る
  - [ ] 2.2 タイマー設定ダイアログのPrefabをAddressablesに登録する
    - ダイアログPrefabを作成し、集中時間・休憩時間・セット回数のテキスト表示、+/-ボタン、Startボタン、×ボタンをレイアウトする
    - Addressablesに`Dialogs/TimerSettingDialog.prefab`として登録する
    - Animatorに"Open"/"Close"アニメーションを設定する
  - _Requirements: 1.2, 1.3, 1.4, 2.1, 2.2, 2.3, 2.4, 2.5, 2.6_

### Branch: `feature/home-to-timer-scene-transition-scene-flow`

- [x] 3. ホームフッターのタイマーボタンにダイアログ表示と遷移フローを接続する
  - [x] 3.1 HomeFooterViewにダイアログ→遷移フローを実装する
    - IDialogServiceのDI注入を追加する
    - タイマーボタン押下時にTimerSettingDialogを表示する
    - ダイアログがOk結果で閉じた場合のみ、タイマーシーンへのフェード遷移を開始する
    - ダイアログがCancel/Closeで閉じた場合はシーン遷移を行わない
  - _Requirements: 1.1, 3.1, 3.2, 3.3, 3.4_

- [x] 4. タイマーシーンに戻るボタンを追加してホーム復帰遷移を実装する
  - [x] 4.1 タイマーシーンのViewと戻るボタンを実装する
    - 戻るボタンを常時表示するMonoBehaviourを作成する（既存のReturnButtonViewを利用）
    - 戻るボタン押下でホームシーンへのフェード遷移を実行する
    - SceneLoaderをVContainerのDI注入で受け取る
  - [x] 4.2 TimerScopeにDI登録を追加する
    - ReturnButtonViewをシーン内コンポーネントとして登録する
  - _Requirements: 4.1, 4.2, 4.3, 5.1, 5.2, 5.3_
