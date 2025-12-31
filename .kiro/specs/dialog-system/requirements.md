# Requirements Document

## Introduction

本ドキュメントは、ダイアログシステムの新規実装に関する要件を定義する。ダイアログシステムは、ゲーム内でモーダルダイアログ（ポップアップ）を統一的に管理するシステムである。スタック構造による複数ダイアログの管理、backdrop表示、開閉アニメーションなどの機能を持ち、VContainerのRootScopeで管理されるゲーム基盤の重要な要素として実装する。

## Requirements

### Requirement 1: ダイアログ表示
**Objective:** As a プレイヤー, I want ボタン押下などのアクションでダイアログを表示したい, so that 詳細情報や確認画面を見ることができる

#### Acceptance Criteria
1. When ダイアログ表示がリクエストされた, the Dialog System shall ダイアログを画面中央に表示する
2. The Dialog System shall 既存の表示物（背景など）の上にダイアログを配置する
3. When ダイアログが表示された, the Dialog System shall ダイアログ周辺に半透明な黒のbackdropを表示する
4. While backdropが表示されている, the Dialog System shall 背景が見える状態を維持する

### Requirement 2: ダイアログスタック管理
**Objective:** As a プレイヤー, I want 複数のダイアログを順番に開閉できる, so that 階層的な情報にアクセスできる

#### Acceptance Criteria
1. When ダイアログが表示されている状態で別のダイアログが開かれた, the Dialog System shall 新しいダイアログを最前面に表示する
2. When 最前面のダイアログが閉じられた, the Dialog System shall 一つ前のダイアログを最前面に表示する
3. When 複数のダイアログが重なった, the Dialog System shall backdropの暗さを加算的に増加させてもよい
4. The Dialog System shall スタック構造でダイアログを管理する
5. When ダイアログが閉じられる際に親ダイアログも閉じる指定がされた, the Dialog System shall 指定されたダイアログまでのスタックを連続して閉じる

### Requirement 3: ダイアログ閉じる操作
**Objective:** As a プレイヤー, I want 複数の方法でダイアログを閉じたい, so that 直感的に操作できる

#### Acceptance Criteria
1. When 閉じるボタンが押された, the Dialog System shall 最前面のダイアログを閉じる
2. When backdropがクリック/タップされた, the Dialog System shall 最前面のダイアログを閉じる
3. When Androidの戻るボタンが押された, the Dialog System shall 最前面のダイアログを閉じる
4. When すべてのダイアログが閉じられた, the Dialog System shall backdropを非表示にする

### Requirement 4: ダイアログアニメーション
**Objective:** As a プレイヤー, I want ダイアログの開閉時にアニメーションを見たい, so that 滑らかなUI体験ができる

#### Acceptance Criteria
1. When ダイアログが開かれた, the Dialog System shall 開くアニメーションを再生する
2. When ダイアログが閉じられた, the Dialog System shall 閉じるアニメーションを再生する
3. The Dialog System shall UnityのAnimation/Animatorを用いてアニメーションを制御する
4. While アニメーションが再生中, the Dialog System shall ダイアログに対するすべての操作入力を無視する

### Requirement 5: ダイアログプレハブ構造
**Objective:** As a 開発者, I want ダイアログをプレハブとして管理したい, so that 新しいダイアログを効率的に作成できる

#### Acceptance Criteria
1. The Dialog System shall 1ダイアログ1プレハブの構造に対応する
2. The Dialog System shall ベースダイアログプレハブを提供する
3. The Dialog System shall ベースからのVariant Prefabとして新規ダイアログ作成を推奨する
4. Where ダイアログが全く異なる構造を必要とする, the Dialog System shall 独立したプレハブでの作成に対応する
5. The Dialog System shall Addressablesを用いてダイアログプレハブをロードする
6. The Dialog System shall ダイアログプレハブ用のAddressables Asset Groupを使用する

### Requirement 6: ダイアログ引数
**Objective:** As a 開発者, I want ダイアログを開く時に引数を渡したい, so that ダイアログ内の表示内容を動的に制御できる

#### Acceptance Criteria
1. When ダイアログが開かれる, the Dialog System shall 引数をダイアログに渡すことに対応する
2. The Dialog System shall ダイアログごとに任意の引数型を定義できる
3. Where ダイアログが引数を必要としない, the Dialog System shall 引数なしでダイアログを開くことに対応する
4. The Dialog System shall 引数をimmutable（不変）として扱う
5. The Dialog System shall C# recordを用いた引数型定義に対応する（IsExternalInit定義により有効化）

### Requirement 7: ダイアログ結果通知
**Objective:** As a 開発者, I want ダイアログが閉じられた時に結果を受け取りたい, so that ユーザーの選択に応じた処理を実行できる

#### Acceptance Criteria
1. When ダイアログが閉じられた, the Dialog System shall 閉じた結果を呼び出し元に通知する
2. The Dialog System shall ダイアログごとに任意の結果型を返すことに対応する
3. When backdropがクリックされてダイアログが閉じられた, the Dialog System shall キャンセル相当の結果を返す
4. The Dialog System shall UniTaskを用いた非同期APIを提供し、awaitでダイアログが閉じられるまで待機できる

### Requirement 8: VContainer統合
**Objective:** As a 開発者, I want ダイアログシステムをVContainerのRoot層で管理したい, so that ゲーム基盤として全シーンから利用できる

#### Acceptance Criteria
1. The Dialog System shall RootScopeでSingletonとして登録される
2. The Dialog System shall Assets/Scripts/Root/配下にスクリプトファイルを配置する
3. The Dialog System shall 任意のシーンからインジェクションで利用可能である
4. The Dialog System shall View → Service → State の依存方向を遵守する
