# Requirements Document

## Project Description (Input)
ユーザの所持アイテム管理 (家具 (furniture)、着せ替え (outfit)、毛糸 (point系))

## Constraints
- 現時点ではサーバ連携を考慮しない。すべての所持情報はローカル (インメモリ + PlayerPrefs) で管理する。
- **本スペックのスコープはインターフェース (および最小限の実装) の用意のみ**。既存のモック実装 (`UserState.UserOutfits` / `UserState.UserFurnitures` / `ShopState.YarnBalance` 等) の置き換え・改修は別タスクで実施する。既存の参照元 (`RedecorateScrollerService` / `IsoGridLoadService` / `ShopService` など) は本スペックでは変更しない。

## Introduction

本機能は、プレイヤーが所持する「家具」「着せ替え」「毛糸 (ポイント系通貨)」をローカルで管理する仕組みを定義する。性質の異なる 2 系統に分割して管理する:

- **Item Inventory Service** (`IUserItemInventoryService`): コレクション型 — 家具 (数量管理) と着せ替え (保有フラグ管理)
- **Point Service** (`IUserPointService`): スカラー型 — 毛糸の残高 (int) 増減

両サービスは RootScope のシングルトンとして登録され、Shop・Closet・Redecorate など複数シーンから独立して利用される。クロスカテゴリ操作 (例: 毛糸を消費して家具を取得) は呼び出し元の Manager 層が両サービスを協調させる責務を持つ。

### アイテムカテゴリ
- **家具 (Furniture)**: 部屋に配置可能なアイテム。同一 ID を複数個所持可能 (数量管理)。
- **着せ替え (Outfit)**: キャラクターに装備可能なアイテム。原則 1 個単位で所持する (保有フラグ管理)。
- **毛糸 (Yarn / Point)**: ゲーム内通貨。残高 (int) として管理し、増減操作を提供する。

## Requirements

### Requirement 1: アイテム所持状態の初期化
**Objective:** プレイヤーとして、ゲーム起動時に所持アイテム (家具・着せ替え) が自動的に読み込まれていることを望む、そうすることで各機能シーンで待機なくアイテムを閲覧・利用できるため。

#### Acceptance Criteria
1. When アプリケーションが起動し RootScope が構築される, the User Item Inventory Service shall 空の所持アイテム状態 (`UserItemInventoryState`) を RootScope にシングルトンとして登録する。
2. When `PlayerPrefsService` に前回保存済みのスナップショットが存在する, the User Item Inventory Service shall 家具・着せ替えの所持情報を復元し `UserItemInventoryState` に反映する。
3. If `PlayerPrefsService` にスナップショットが存在しない、または保存データのフォーマットが現行バージョンと非互換な場合, then the User Item Inventory Service shall 空の初期状態で `UserItemInventoryState` を構築する。
4. The User Item Inventory Service shall マスターデータ (`MasterDataState`) との紐付けキー (家具 ID・着せ替え ID) を読み込み時に検証する。

### Requirement 2: 家具の所持管理
**Objective:** プレイヤーとして、家具アイテムの所持数を正しく把握・増減できることを望む、そうすることで模様替えシーンで配置可能な家具を選択できるため。

#### Acceptance Criteria
1. When 呼び出し元が家具 ID を指定して所持数を問い合わせる, the User Item Inventory Service shall 該当 ID の所持数 (未所持は 0) を返却する。
2. When 家具アイテムの取得処理が完了した (例: 購入・ガチャ・報酬), the User Item Inventory Service shall 対象家具 ID の所持数を指定数だけ増加させ `UserItemInventoryState` を更新する。
3. When 家具の所持数が変化する, the User Item Inventory Service shall 家具変更通知 (イベント) を購読者へ発行する。
4. If 存在しない家具 ID に対して増減操作が要求された場合, then the User Item Inventory Service shall 操作を実行せずエラー結果を返却する。
5. If 家具の所持数を 0 未満にする減算が要求された場合, then the User Item Inventory Service shall 操作を拒否し不正操作エラーを返却する。
6. The User Item Inventory Service shall 所持している全家具の一覧 (ID と数量) を列挙する読み取り API を提供する。

### Requirement 3: 着せ替えの所持管理
**Objective:** プレイヤーとして、着せ替えアイテムの所持状態を管理できることを望む、そうすることで Closet シーンで所持済みの衣装のみを選択できるため。

#### Acceptance Criteria
1. When 呼び出し元が着せ替え ID を指定して所持判定を問い合わせる, the User Item Inventory Service shall 保有の真偽値 (`true` / `false`) を返却する。
2. When 着せ替えアイテムの取得処理が完了した, the User Item Inventory Service shall 対象着せ替え ID を所持済み集合に追加し `UserItemInventoryState` を更新する。
3. When 着せ替えの所持集合が変化する, the User Item Inventory Service shall 着せ替え変更通知 (イベント) を購読者へ発行する。
4. If 既に所持している着せ替え ID を再度取得しようとした場合, then the User Item Inventory Service shall 二重付与を行わず正常結果 (冪等) として扱う。
5. If 存在しない着せ替え ID に対して付与操作が要求された場合, then the User Item Inventory Service shall 操作を実行せずエラー結果を返却する。
6. Where 現在装備中の着せ替え (`UserEquippedOutfitState`) と整合する必要がある, the User Item Inventory Service shall 装備中の着せ替えが所持集合に含まれることを保証する。
7. The User Item Inventory Service shall 所持している全着せ替え ID の一覧を列挙する読み取り API を提供する。

### Requirement 4: ポイント残高の初期化
**Objective:** プレイヤーとして、ゲーム起動時に毛糸残高が自動的に読み込まれていることを望む、そうすることで各機能シーンで即座に残高を確認・利用できるため。

#### Acceptance Criteria
1. When アプリケーションが起動し RootScope が構築される, the User Point Service shall ポイント残高状態 (`UserPointState`) を RootScope にシングルトンとして登録する。
2. When `PlayerPrefsService` に前回保存済みの残高データが存在する, the User Point Service shall 毛糸残高を復元し `UserPointState` に反映する。
3. If `PlayerPrefsService` に残高データが存在しない、またはフォーマットが非互換な場合, then the User Point Service shall 残高 0 で `UserPointState` を初期化する。

### Requirement 5: 毛糸 (ポイント系通貨) の残高管理
**Objective:** プレイヤーとして、自分の毛糸残高を確認し、購入やガチャで正しく消費できることを望む、そうすることで経済活動を安心して行えるため。

#### Acceptance Criteria
1. The User Point Service shall 現在の毛糸残高 (`int`) を取得する読み取り API を提供する。
2. When 毛糸の加算が要求される, the User Point Service shall 指定された正の数量を残高に加算し `UserPointState` を更新する。
3. When 毛糸の減算が要求され残高が充足している, the User Point Service shall 指定された数量を残高から減算し正常結果を返却する。
4. If 毛糸残高が要求された減算額に満たない場合, then the User Point Service shall 減算を実行せず残高不足エラーを返却する。
5. If 負数または 0 の加減算が要求された場合, then the User Point Service shall 操作を拒否し不正引数エラーを返却する。
6. When 毛糸残高が変化する, the User Point Service shall 毛糸変更通知 (イベント) を購読者へ発行する。
7. The User Point Service shall `int.MaxValue` を超える加算を桁あふれさせず、上限で丸めるか不正引数エラーとして扱う (いずれかを仕様として定義する)。

### Requirement 6: 変更通知と状態一貫性
**Objective:** View 層開発者として、所持アイテムやポイントの変更を購読できることを望む、そうすることで UI を手動更新せずに最新の状態を表示できるため。

#### Acceptance Criteria
1. The User Item Inventory Service shall 家具・着せ替えそれぞれの変更通知ストリーム (またはイベント) を公開する。
2. The User Point Service shall 毛糸残高の変更通知ストリーム (またはイベント) を公開する。
3. If 通知購読者側でハンドラが例外をスローした場合, then 各 Service shall 内部状態を破壊せず他の購読者への通知を継続する。

### Requirement 7: ローカル永続化
**Objective:** プレイヤーとして、アプリを再起動しても所持アイテムとポイントが保持されていることを望む、そうすることで毎回データが消えない安定した体験が得られるため。

#### Acceptance Criteria
1. When アイテム所持状態が変化する, the User Item Inventory Service shall 変更後の `UserItemInventoryState` のスナップショットを `PlayerPrefsService` に保存する。
2. When ポイント残高が変化する, the User Point Service shall 変更後の `UserPointState` を `PlayerPrefsService` に保存する。
3. If 保存処理中に例外が発生した場合, then 各 Service shall インメモリ状態を破壊せず、エラーログを出力する。
4. The User Item Inventory Service shall スナップショットのバージョン番号を含め、将来のフォーマット変更に対応可能とする。
5. The User Point Service shall 保存データのバージョン番号を含め、将来のフォーマット変更に対応可能とする。

### Requirement 8: 依存性注入と層構造の遵守
**Objective:** プロジェクト開発者として、本機能が既存の VContainer アーキテクチャに統合されていることを望む、そうすることで各シーンから一貫した方法で利用できるため。

#### Acceptance Criteria
1. The User Item Inventory Service shall `RootScope` に `Lifetime.Singleton` で登録される。
2. The User Point Service shall `RootScope` に `Lifetime.Singleton` で登録される。
3. The User Item Inventory Service shall `IUserItemInventoryService` インターフェースを公開し、実装クラスとの疎結合を保つ。
4. The User Point Service shall `IUserPointService` インターフェースを公開し、実装クラスとの疎結合を保つ。
5. 両 Service shall 依存関係方向 `View → Service → State` を遵守し、State 層から Service 層への逆依存を持たない。
6. Where 着せ替え装備と連動する必要がある, the User Item Inventory Service shall `UserEquippedOutfitService` 経由でのみ装備状態を参照し、`UserEquippedOutfitState` を直接書き換えない。
7. 両 Service shall `#nullable enable` を宣言した C# ファイルで実装される。
8. 両 Service shall `CancellationToken` を最終引数に取る非同期メソッドを `UniTask` ベースで提供する。
9. 両 Service shall コンストラクタに `[Inject]` 属性を付与し、IL2CPP ビルドでの stripping を防止する。
