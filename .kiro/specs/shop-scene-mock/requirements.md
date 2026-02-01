# Requirements Document

## Introduction
ショップ画面（シーン）のモック実装。プレイヤーがアイテムや毛糸（ゲーム内通貨）を閲覧・購入できるショップ機能の基盤となるシーンを、プロジェクトの標準的なシーンアーキテクチャに従って実装する。本フェーズではモック（プレースホルダーUI・仮データ）としての実装を行い、実際の購入処理やサーバー連携は後続の実装で対応する。

## Requirements

### Requirement 1: シーン構造の準備
**Objective:** As a 開発者, I want ショップシーンがプロジェクトの標準的なシーンアーキテクチャに従っていること, so that 保守性と一貫性が保たれる

#### Acceptance Criteria
1. The Shop Scene shall シーンファイル `Assets/Scenes/Shop.unity` を持つ
2. The Shop Scene shall `Assets/Scripts/Shop/` 配下に標準的なフォルダ構造 (Manager/, Scope/, Service/, Starter/, State/, View/) を持つ
3. The ShopScope shall VContainerのLifetimeScopeを継承し、RootScopeを親として設定する
4. The ShopStarter shall IStartableを実装し、シーン初期化のエントリーポイントとして機能する
5. The Shop Scene shall シーン名定数を `Const.SceneName.Shop` として定義する

### Requirement 2: タブ切り替え
**Objective:** As a プレイヤー, I want 「アイテム」と「毛糸」のタブを切り替えられること, so that 購入したいカテゴリの商品を閲覧できる

#### Acceptance Criteria
1. The ShopView shall 画面上部に「アイテム」タブ（ItemButton）と「毛糸」タブ（PointButton）を横並びで固定表示する
2. When ショップシーンがロードされたとき, the ShopView shall デフォルトで「アイテム」タブを選択状態にする
3. When タブがタップされたとき, the ShopView shall タップされたタブを選択状態にし、他のタブを非選択状態にする
4. When タブが切り替わったとき, the ShopView shall タブより下のCategories領域の内容を切り替える
5. The ShopView shall 選択中のタブを視覚的に区別できるスタイルで表示する
6. The ShopView shall タブは固定し、Categories領域のみスクロール可能とする

### Requirement 3: アイテムタブ - カテゴリ表示
**Objective:** As a プレイヤー, I want アイテムタブで複数のカテゴリ（ガチャ、アイテム等）を閲覧できること, so that 目的の商品を見つけやすい

#### Acceptance Criteria
1. While 「アイテム」タブが選択されているとき, the ShopView shall Categories領域にカテゴリ一覧をスクロール可能な形式で表示する
2. The ShopView shall 各カテゴリに見出し（Header）とセル一覧（Cells）を表示する
3. The ShopView shall カテゴリとセルをシーン上に手動配置する（動的生成ではない）
4. The ShopView shall モック用に最低2つのカテゴリ（「ガチャ」「アイテム」）を表示する

### Requirement 4: アイテムタブ - ガチャカテゴリ
**Objective:** As a プレイヤー, I want ガチャカテゴリでガチャを引いて家具を入手できること, so that 部屋をカスタマイズできる

#### Acceptance Criteria
1. The ShopView shall ガチャカテゴリのセルをシーン上に手動配置する（1行1セルの縦並びレイアウト）
2. The ShopView shall 各ガチャセルにガチャ名、サムネイル画像を表示する
3. The ShopService shall 各ガチャセルの表示内容（ガチャ名、サムネイル、価格）をコードから設定する
4. The ShopView shall 各ガチャセルに「1連」ボタンと「10連」ボタンを表示する
5. The ShopView shall 各ボタンに回数（1回 or 10回）と消費毛糸の数を表示する
6. The ShopView shall ガチャセル自体はタップ不可（interactable: false）とし、ボタンのみタップ可能とする
7. When 「1連」ボタンが押されたとき, the ShopService shall そのセルのガチャを1回実行し、1つの家具を排出する（モック動作）
8. When 「10連」ボタンが押されたとき, the ShopService shall そのセルのガチャを10回実行し、10個の家具を排出する（モック動作）
9. The ShopView shall モック用に最低2件のガチャセルを配置する

### Requirement 5: アイテムタブ - アイテムカテゴリ
**Objective:** As a プレイヤー, I want アイテムカテゴリでアイテムを購入できること, so that ゲームを有利に進められる

#### Acceptance Criteria
1. The ShopView shall アイテムカテゴリのセルをシーン上に手動配置する（横長1セル or グリッド用小セル）
2. The ShopView shall 各アイテムセルにアイコン、商品名、価格を縦並びで表示する
3. The ShopService shall 各セルの表示内容（アイコン、商品名、価格）をコードから設定する
4. When アイテムセルがタップされたとき, the ShopService shall そのセルに設定された商品の購入確認ダイアログを表示する
5. The ShopView shall モック用に最低5件のアイテムセルを配置する

### Requirement 6: 毛糸タブ - 毛糸パック表示
**Objective:** As a プレイヤー, I want 毛糸タブで毛糸パックを購入できること, so that ゲーム内通貨を入手できる

#### Acceptance Criteria
1. While 「毛糸」タブが選択されているとき, the ShopView shall 毛糸パック一覧をスクロール可能な形式で表示する
2. The ShopView shall 毛糸パックのセルをシーン上に手動配置する（横長1セル or グリッド用小セル）
3. The ShopView shall 各毛糸パックセルにアイコン、商品名、価格を縦並びで表示する
4. The ShopService shall 各セルの表示内容（アイコン、商品名、価格）をコードから設定する
5. When 毛糸パックセルがタップされたとき, the ShopService shall そのセルに設定された商品の購入確認ダイアログを表示する
6. The ShopView shall モック用に最低3件の毛糸パックセルを配置する

### Requirement 7: 汎用確認ダイアログ
**Objective:** As a 開発者, I want 汎用的な確認ダイアログを使用できること, so that 購入確認やガチャ確認など様々な場面で再利用できる

#### Acceptance Criteria
1. The ConfirmDialog shall Addressables経由で動的にロード可能なプレファブとして実装する
2. The ConfirmDialog shall タイトル、メッセージ、確認ボタン、キャンセルボタンを表示する
3. The ConfirmDialog shall 確認ボタンとキャンセルボタンのテキストをカスタマイズ可能とする
4. When 確認ボタンが押されたとき, the ConfirmDialog shall コールバックを実行しダイアログを閉じる
5. When キャンセルボタンが押されたとき, the ConfirmDialog shall コールバックを実行しダイアログを閉じる
6. The ConfirmDialog shall 既存のダイアログシステム（DialogService）と統合する
7. The ConfirmDialog shall モーダル表示（背景タップで閉じない）とする

### Requirement 8: 購入処理（モック）
**Objective:** As a プレイヤー, I want 商品を購入できること, so that アイテムや毛糸を入手できる

#### Acceptance Criteria
1. When 商品セルがタップされたとき, the ShopService shall 汎用確認ダイアログを使用して購入確認を表示する
2. When 購入確認で「はい」が選択されたとき, the ShopService shall 購入完了メッセージを表示する（モック動作）
3. When 購入確認で「いいえ」が選択されたとき, the ShopService shall ダイアログを閉じてキャンセルする
4. The ShopService shall ゲーム内通貨（毛糸）による購入に対応する（モック動作）
5. The ShopService shall リアルマネー（Playストア/App Store）による購入に対応可能な設計とする（モック動作）
6. If 所持毛糸が不足している場合（ゲーム内通貨購入時）, the ShopView shall 購入不可を示す表示をする

### Requirement 9: ガチャ実行処理（モック）
**Objective:** As a プレイヤー, I want ガチャを引いて結果を確認できること, so that 入手した家具を把握できる

#### Acceptance Criteria
1. When ガチャボタンが押されたとき, the ShopService shall 汎用確認ダイアログを使用してガチャ確認を表示する
2. When ガチャが確定されたとき, the ShopService shall ガチャ結果（排出された家具リスト）を表示する（モック動作）
3. If 所持毛糸が不足している場合, the ShopView shall ガチャボタンを無効化し、毛糸不足を示す表示をする
4. The ShopService shall 1連の場合は1つ、10連の場合は10個の家具をランダムに選出する（モック動作）

### Requirement 10: シーン遷移
**Objective:** As a プレイヤー, I want ショップ画面から他の画面に戻れること, so that ゲームを継続できる

#### Acceptance Criteria
1. The ShopView shall 戻るボタン（BackButton）を表示する
2. When 戻るボタンが押されたとき, the ShopService shall SceneLoaderを使用して前のシーンに遷移する
3. While シーン遷移中, the Shop Scene shall 既存のフェードトランジションを使用する

### Requirement 11: 状態管理
**Objective:** As a 開発者, I want ショップの状態が適切に管理されていること, so that 後続の実装で拡張しやすい

#### Acceptance Criteria
1. The ShopState shall 現在選択中のタブ情報を保持する
2. The ShopState shall モック用の所持毛糸情報を保持する
3. The ShopState shall モック用のカテゴリリスト（アイテムタブ用）を保持する
4. The ShopState shall モック用のガチャリスト（ガチャ名、1連/10連価格、排出家具リスト）を保持する
5. The ShopState shall モック用のアイテムリスト（商品名、アイコン、価格、通貨種別）を保持する
6. The ShopState shall モック用の毛糸パックリスト（商品名、アイコン、価格、毛糸量）を保持する

### Requirement 12: セルプレファブとコンテンツ設定
**Objective:** As a 開発者, I want 手動配置されたセルの内容をコードから設定できること, so that 商品データに応じた表示と購入処理が可能になる

#### Acceptance Criteria
1. The Shop Scene shall セルをシーン上に手動で配置する（コードからの配置/生成は行わない）
2. The Shop Scene shall セルプレファブ（アイテム用、ガチャ用等）を用意し、シーン配置時のテンプレートとして使用する
3. The ShopService shall 手動配置されたセルに対して商品データ（アイコン、商品名、価格、通貨種別）を設定する
4. The ShopView shall 各セルにクリックイベントを設定し、タップ時にそのセルの商品を購入処理する
5. The ShopState shall 各セルに対応する商品データを保持する

#### Design Investigation Required
- 手動配置されたセルに対してLayout Groupによる自動レイアウトが適用可能かの調査
- 異なるサイズのセル（横長1セル、グリッド用小セル）が混在する場合の自動レイアウト適用方法の検討
