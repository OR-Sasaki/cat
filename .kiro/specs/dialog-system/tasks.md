# Implementation Plan

## Tasks

- [x] 1. プロジェクトセットアップとダイアログ基盤型定義
  - Unityでrecord構文を使用可能にするIsExternalInit定義を追加
  - ダイアログ結果を表すenum（Ok, Cancel, Close）を定義
  - ダイアログ引数のマーカーインターフェースを定義
  - 引数付きダイアログ用のジェネリックインターフェースを定義
  - ダイアログ実行時インスタンスを表すクラスを定義（ID、View参照、完了通知、描画順）
  - _Requirements: 6.2, 6.4, 6.5, 7.2_
  - _Branch: `feature/dialog-system-foundation-types`_

- [x] 2. ダイアログ状態管理の実装
  - Stack構造で表示中ダイアログをLIFO管理する状態クラスを実装
  - スタックへのPush/Pop操作を実装
  - 現在のダイアログ数や最前面のダイアログを取得するプロパティを実装
  - 指定ダイアログまで連続でPopする機能を実装（親ダイアログ連続クローズ用）
  - 描画順（sortingOrder）の自動計算ロジックを実装
  - _Requirements: 2.1, 2.2, 2.4, 2.5_
  - _Branch: `feature/dialog-system-dialog-state`_

- [x] 3. ダイアログUI基盤の実装
  - **Backdrop表示コンポーネント**
    - 半透明の黒背景を表示するViewコンポーネントを実装
    - CanvasGroupによるalpha制御（基底0.5、ダイアログ毎に+0.1、上限0.9）
    - backdropタップ時のイベント通知機能を実装
  - **ダイアログ基底クラス**
    - 全ダイアログ共通の基底MonoBehaviourクラスを実装
    - Animatorによる開閉アニメーション制御機能を実装
    - アニメーション完了検知（AnimationEventまたはStateMachineBehaviour）を実装
    - アニメーション中の操作ブロック機能（CanvasGroup.interactable制御）を実装
    - 閉じるボタンのイベントハンドリング機能を実装
    - 引数付きダイアログ用の初期化メソッドを定義
  - _Requirements: 1.3, 1.4, 2.3, 3.1, 3.2, 4.1, 4.2, 4.3, 4.4, 6.1_
  - _Branch: `feature/dialog-system-dialog-ui`_

- [x] 4. ダイアログコンテナの実装
  - **Addressablesによるプレハブロードと表示管理**
    - ダイアログ用Canvasの管理機能を実装
    - Addressablesによるダイアログプレハブのロード機能を実装
    - ロード済みプレハブのキャッシュ機能を実装
    - ダイアログインスタンスの生成とCanvas配置機能を実装
    - ダイアログの表示・非表示制御機能を実装
    - アセット解放（ReleaseInstance）機能を実装
  - **Androidバックボタン監視**
    - レガシーInput Managerを使用したバックボタン検知を実装
    - ダイアログ表示中のみバックボタン監視を有効化
    - バックボタン押下時のダイアログクローズ通知機能を実装
  - _Requirements: 1.1, 1.2, 3.3, 5.1, 5.5, 5.6_
  - _Branch: `feature/dialog-system-dialog-container`_

- [x] 5. ダイアログサービスの実装
  - **非同期ダイアログ表示API**
    - UniTaskCompletionSourceを使用した非同期待機管理を実装
    - 引数なしダイアログを開くOpenAsyncメソッドを実装
    - 引数付きダイアログを開くOpenAsyncジェネリックメソッドを実装
  - **ダイアログクローズ処理**
    - ダイアログを閉じるCloseメソッドを実装
    - 親ダイアログも連続して閉じるオプション機能を実装
    - backdropクリック時のキャンセル結果返却を実装
  - **キャンセル対応**
    - CancellationToken対応を実装
    - トークン発火時のダイアログ即座クローズ処理を実装
  - _Requirements: 1.1, 6.1, 6.3, 7.1, 7.3, 7.4_
  - _Branch: `feature/dialog-system-dialog-service`_

- [x] 6. (P) VContainer統合とダイアログプレハブ作成
  - **RootScopeへの登録**
    - DialogService、DialogState、DialogContainerをRootScopeにSingleton登録
    - インターフェースによる依存性注入設定を実装
    - View → Service → State の依存方向を遵守した設定を確認
  - **ベースダイアログプレハブ作成**
    - ダイアログ用Canvasプレハブを作成
    - BackdropViewを含むベースダイアログプレハブを作成
    - 開閉アニメーション用のAnimatorControllerを作成（Open/Closeトリガー）
  - **Addressables設定**
    - ダイアログ用Addressables Asset Groupを作成
    - ベースダイアログプレハブをAddressablesに登録
    - Variant Prefabとして新規ダイアログを作成できる構造を確認
  - _Requirements: 5.2, 5.3, 5.4, 5.6, 8.1, 8.2, 8.3, 8.4_
  - _Branch: `feature/dialog-system-vcontainer-integration`_

- [ ] 7. 統合テストと動作検証
  - **サンプルダイアログ実装**
    - 確認ダイアログ（OK/キャンセル）のサンプル実装
    - 引数付きダイアログ（メッセージ表示）のサンプル実装
  - **スタック動作検証**
    - 複数ダイアログのスタック表示テスト
    - backdrop暗さの加算表示確認
  - **クローズ操作検証**
    - 各種クローズ操作（ボタン、backdrop、戻るボタン）の動作確認
    - アニメーション中の操作ブロック確認
  - **統合確認**
    - 任意のシーンからのダイアログ呼び出し確認
    - 全要件の受け入れ基準を満たすことを確認
  - _Requirements: 1.1, 1.2, 1.3, 1.4, 2.1, 2.2, 2.3, 3.1, 3.2, 3.3, 3.4, 4.1, 4.2, 4.4, 6.1, 6.2, 7.1, 7.3, 7.4, 8.3_
  - _Branch: `feature/dialog-system-integration-test`_

## Requirements Coverage

| Requirement | Tasks |
|-------------|-------|
| 1.1 | 4, 5, 7 |
| 1.2 | 4, 7 |
| 1.3 | 3, 7 |
| 1.4 | 3, 7 |
| 2.1 | 2, 7 |
| 2.2 | 2, 7 |
| 2.3 | 3, 7 |
| 2.4 | 2 |
| 2.5 | 2 |
| 3.1 | 3, 7 |
| 3.2 | 3, 7 |
| 3.3 | 4, 7 |
| 3.4 | 7 |
| 4.1 | 3, 7 |
| 4.2 | 3, 7 |
| 4.3 | 3 |
| 4.4 | 3, 7 |
| 5.1 | 4 |
| 5.2 | 6 |
| 5.3 | 6 |
| 5.4 | 6 |
| 5.5 | 4 |
| 5.6 | 4, 6 |
| 6.1 | 3, 5, 7 |
| 6.2 | 1, 7 |
| 6.3 | 5 |
| 6.4 | 1 |
| 6.5 | 1 |
| 7.1 | 5, 7 |
| 7.2 | 1 |
| 7.3 | 5, 7 |
| 7.4 | 5, 7 |
| 8.1 | 6 |
| 8.2 | 6 |
| 8.3 | 6, 7 |
| 8.4 | 6 |
