# Implementation Plan

## ブランチ戦略
- 作業ブランチ: `feature/dialog-system`
- 各タスクは `feature/dialog-system` から派生してPRを作成
- PRマージ後、次のタスクブランチを派生

---

- [ ] 1. プロジェクト基盤の準備
  - UniTaskパッケージをインストール（Package Managerから `com.cysharp.unitask`）
  - IsExternalInit定義を追加（`Assets/Scripts/Utils/IsExternalInit.cs`）
  - _Requirements: 4.1.1, 4.2.3_
  - _PR: `feature/dialog-system/setup-foundation`_

- [ ] 2. DialogState機能の実装
  - DialogResult enum（`Assets/Scripts/Root/View/DialogResult.cs`）
  - IDialogArgsインターフェース（`Assets/Scripts/Root/View/IDialogArgs.cs`）
  - DialogEntryクラス（`Assets/Scripts/Root/State/DialogEntry.cs`）
  - DialogStateクラス（`Assets/Scripts/Root/State/DialogState.cs`）
  - _Requirements: 1.1, 3.1, 3.3, 4.1.2, 4.2.1_
  - _PR: `feature/dialog-system/add-dialog-state`_

- [ ]* 2.1 DialogStateのプロパティテストを作成する
  - **Property 1: ダイアログ追加でスタックカウント増加**
  - **Property 3: スタックのLIFO順序**
  - **Property 4: ダイアログ削除でスタックカウント減少**
  - **Validates: Requirements 1.1, 3.1, 3.3**
  - _PR: `feature/dialog-system/add-dialog-state-tests`_

- [x] 3. DialogViewBase機能の実装
  - DialogViewBase基底クラス（`Assets/Scripts/Root/View/DialogViewBase.cs`）
  - DialogViewBase<TArgs>ジェネリック版（同ファイル内）
  - Initialize, Close, SetCloseCount, PlayOpenAnimation, PlayCloseAnimation
  - SetArgs, OnArgsSet, Argsプロパティ
  - _Requirements: 4.1, 4.2, 6.1, 6.2, 8.1, 4.2.2, 4.2.4, 4.2.5_
  - _PR: `feature/dialog-system/add-dialog-view-base`_

- [ ]* 3.1 引数受け渡しのプロパティテストを作成する
  - **Property 7: 引数の受け渡し**
  - **Validates: Requirements 4.2.5**
  - _PR: `feature/dialog-system/add-dialog-view-base-tests`_

- [ ] 4. Backdrop機能の実装
  - BackdropViewクラス（`Assets/Scripts/Root/View/BackdropView.cs`）
  - バックドロッププレハブ（`Assets/Arts/Prefabs/Dialog/Backdrop.prefab`）
  - _Requirements: 2.1, 2.2, 5.1_
  - _PR: `feature/dialog-system/add-backdrop`_

- [ ] 5. DialogCanvas機能の実装
  - DialogCanvasクラス（`Assets/Scripts/Root/View/DialogCanvas.cs`）
  - RootScopeプレハブにDialogCanvas用Canvasを子オブジェクトとして追加
  - _Requirements: 1.2, 1.3, 2.1_
  - _PR: `feature/dialog-system/add-dialog-canvas`_

- [ ] 6. DialogService機能の実装
  - DialogServiceクラス（`Assets/Scripts/Root/Service/DialogService.cs`）
  - Open<TDialog>()、Open<TDialog, TArgs>()メソッド
  - Close()メソッド（連続クローズ対応含む）
  - バックドロップタップ処理
  - Android戻るボタン処理
  - RootScopeへのDI登録
  - _Requirements: 1.1, 3.3, 4.1.3, 4.2.1, 4.2.5, 4.3, 5.1, 6.1, 6.2, 6.3, 7.3, 8.1, 8.2, 8.3, 9.1, 9.2, 9.3_
  - _PR: `feature/dialog-system/add-dialog-service`_

- [ ]* 6.1 DialogServiceのプロパティテストを作成する
  - **Property 2: ダイアログ表示時にバックドロップ生成**
  - **Property 5: スタック空でバックドロップなし**
  - **Property 6: ダイアログ結果の返却**
  - **Property 8: アニメーション中の操作無視**
  - **Property 9: 連続クローズ**
  - **Property 10: 連続クローズ時の複数UniTask完了**
  - **Validates: Requirements 2.1, 2.3, 3.5, 4.3, 6.3, 8.1, 8.2, 8.3**
  - _PR: `feature/dialog-system/add-dialog-service-tests`_

- [ ] 7. Checkpoint - すべてのテストが通ることを確認
  - Ensure all tests pass, ask the user if questions arise.

- [ ] 8. サンプルダイアログ（ConfirmDialog）の実装
  - DialogBaseプレハブ（`Assets/Arts/Prefabs/Dialog/DialogBase.prefab`）
  - ConfirmDialogArgs（`Assets/Scripts/Root/View/ConfirmDialogArgs.cs`）
  - ConfirmDialogクラス（`Assets/Scripts/Root/View/ConfirmDialog.cs`）
  - ConfirmDialogプレハブ（`Assets/Arts/Prefabs/Dialog/ConfirmDialog.prefab`）
  - Addressablesに `Dialogs/ConfirmDialog` として登録
  - _Requirements: 4.2.1, 4.2.3, 7.1, 7.2, 7.3, 7.4_
  - _PR: `feature/dialog-system/add-confirm-dialog`_

- [ ] 9. 動作確認
  - 任意のシーンにテスト用UIを追加
  - Open/Close、連続クローズ、Android戻るボタンの動作確認
  - _Requirements: 1.1, 3.1, 3.3, 5.1, 8.1, 9.1_
  - _PR: `feature/dialog-system/add-integration-test`_

- [ ] 10. Final Checkpoint - すべてのテストが通ることを確認
  - Ensure all tests pass, ask the user if questions arise.
