# Requirements Document

## Project Description (Input)
ショップのアイテム/ポイント管理処理の移行

## Introduction

現状のショップシーンは、`ShopState` 内に毛糸残高 (`YarnBalance`) や商品リスト (`ItemProductList` / `PointProductList` / `GachaList`) を保持し、購入時の毛糸消費・加算を `ShopState.ConsumeYarn` / `ShopState.AddYarn` で実行している。一方、RootScope には `user-inventory-management` 仕様で整備済みの共通サービスとして `IUserPointService` (毛糸残高) と `IUserItemInventoryService` (家具所持数 / 着せ替え保有) が存在し、永続化・変更通知・エラーハンドリングを含む正式な契約を提供している。

本機能は、ショップ配下の「アイテム (家具等の所持物) 管理処理」と「ポイント (毛糸残高) 管理処理」を、これら RootScope サービスに移行する。移行後は `ShopState` からポイント/アイテム管理責務を除去し、`ShopService` / `ShopView` はサービス契約を介してのみ状態を読み書き・購読する。

### スコープ
- 毛糸残高の参照・加算・消費を `IUserPointService` に一本化する。
- ガチャ当選・商品購入による家具獲得を `IUserItemInventoryService.AddFurniture` 経由で行う。
- `ShopState` から `YarnBalance` / `ConsumeYarn` / `AddYarn` / `OnYarnBalanceChanged` を除去する。
- `ShopView` の残高表示・セル `interactable` 更新を `IUserPointService.YarnBalanceChanged` に購読替えする。

### スコープ外
- 時限ショップ機能 (`timed-shop` 仕様で別途実装)。
- ショップ商品マスターデータ化 (`timed-shop` 仕様で別途実装)。
- リアルマネー (`CurrencyType.RealMoney`) の課金処理実装 (従来どおりモック扱い)。
- 消費系モック商品 (経験値ブースト等) の獲得処理 (対応するインベントリサービスが存在しないため、購入成功メッセージのみ継続)。
- 着せ替え (`outfit`) 商品の購入処理 (現状ショップに存在しない。`timed-shop` で追加予定)。

## Requirements

### Requirement 1: 毛糸残高参照の `IUserPointService` 移行
**Objective:** As a ショップ利用プレイヤー, I want ショップ画面に表示される毛糸残高が RootScope で管理される最新の残高と常に一致していること, so that ショップ外で変動した残高 (将来の報酬付与等) もショップ画面に正しく反映される

#### Acceptance Criteria
1. The ShopService shall 毛糸残高の参照に `IUserPointService.GetYarnBalance()` を用いる。
2. The ShopView shall 画面表示する毛糸残高を `IUserPointService.GetYarnBalance()` から取得した値から算出する。
3. The ShopService shall `ShopState.YarnBalance` プロパティへの参照を行わない。
4. When ショップシーンが開始される, the ShopView shall `IUserPointService.GetYarnBalance()` の現在値を初期表示に反映する。
5. The ShopState shall 毛糸残高を保持するフィールド・プロパティ・メソッドを公開しない。

### Requirement 2: 毛糸消費処理の `IUserPointService` 移行
**Objective:** As a ショップ利用プレイヤー, I want 購入・ガチャによる毛糸消費が永続化されたポイント残高に対して実行されること, so that 購入結果がアプリ再起動や他シーン遷移後も保持される

#### Acceptance Criteria
1. When 毛糸通貨 (`CurrencyType.Yarn`) の商品購入確認で「はい」が選択された, the ShopService shall `IUserPointService.SpendYarn(price)` を呼び出して毛糸を消費する。
2. When ガチャの購入確認で「はい」が選択された, the ShopService shall `IUserPointService.SpendYarn(price)` を呼び出して毛糸を消費する。
3. If `IUserPointService.SpendYarn` が `Insufficient` を返した, then the ShopService shall 「購入できません (毛糸が足りません)」相当のメッセージダイアログを表示し、後続の購入処理を実行しない。
4. If `IUserPointService.SpendYarn` が `InvalidArgument` を返した, then the ShopService shall クラスコンテキスト付きでエラーログを出力し、後続の購入処理を実行しない。
5. The ShopService shall `ShopState.ConsumeYarn` を呼び出さない。
6. The ShopState shall 毛糸消費 API (`ConsumeYarn` 等) を公開しない。

### Requirement 3: 毛糸加算処理 (毛糸パック) の `IUserPointService` 移行
**Objective:** As a ショップ利用プレイヤー, I want 毛糸パック購入で獲得した毛糸が永続化された残高に加算されること, so that 獲得した毛糸をアプリ再起動後も利用できる

#### Acceptance Criteria
1. When `ProductType.YarnPack` の商品購入が成功した, the ShopService shall 商品の `YarnAmount` を `IUserPointService.AddYarn(yarnAmount)` に渡して残高を加算する。
2. If `IUserPointService.AddYarn` が `Overflow` を返した, then the ShopService shall クラスコンテキスト付きでエラーログを出力し、購入成立後のメッセージでは加算失敗を利用者に通知する。
3. If `IUserPointService.AddYarn` が `InvalidArgument` を返した (`YarnAmount` が null または 0 以下), then the ShopService shall クラスコンテキスト付きでエラーログを出力し、毛糸加算処理をスキップする。
4. The ShopService shall `ShopState.AddYarn` を呼び出さない。
5. The ShopState shall 毛糸加算 API (`AddYarn` 等) を公開しない。

### Requirement 4: 毛糸残高変更通知の購読替え
**Objective:** As a ショップ利用プレイヤー, I want 毛糸残高の変化が画面表示とセルの購入可否に即座に反映されること, so that 購入後の残高と購入可否を一瞥で把握できる

#### Acceptance Criteria
1. The ShopView shall 毛糸残高の変更通知として `IUserPointService.YarnBalanceChanged` イベントを購読する。
2. When `IUserPointService.YarnBalanceChanged` が発火した, the ShopView shall 毛糸残高表示 (`_yarnBalanceText`) を受領した新残高で更新する。
3. When `IUserPointService.YarnBalanceChanged` が発火した, the ShopService / ShopView shall 各商品セルおよびガチャセルの `interactable` 状態を新残高に基づき再計算する。
4. When ショップシーンの破棄または `ShopView.OnDestroy` が呼ばれた, the ShopView shall `IUserPointService.YarnBalanceChanged` の購読を解除する。
5. The ShopView shall `ShopState.OnYarnBalanceChanged` を購読しない。
6. The ShopState shall `OnYarnBalanceChanged` イベントを公開しない。

### Requirement 5: 商品セルの購入可否判定の移行
**Objective:** As a ショップ利用プレイヤー, I want 所持毛糸が不足している毛糸通貨商品はタップ不可で表示されること, so that 購入できない商品を誤タップしないで済む

#### Acceptance Criteria
1. When `ShopService.SetupProductCell` が呼び出された, the ShopService shall `CurrencyType.Yarn` の商品の `interactable` 判定に `IUserPointService.GetYarnBalance() >= data.Price` を用いる。
2. When `ShopService.SetupGachaCell` が呼び出された, the ShopService shall ガチャの単発/10連ボタンの `interactable` 判定に `IUserPointService.GetYarnBalance()` を用いる。
3. The ShopService shall 毛糸通貨商品の購入可否判定時に `ShopState.YarnBalance` を参照しない。
4. Where 商品が `CurrencyType.RealMoney` の場合, the ShopService shall 毛糸残高に依存せず `interactable` を `true` として扱う (既存挙動を維持)。

### Requirement 6: 購入時の残高不足事前チェックの移行
**Objective:** As a ショップ利用プレイヤー, I want 残高不足時に購入確認ダイアログを開かず、明確な理由が提示されること, so that 無駄な操作や混乱を避けられる

#### Acceptance Criteria
1. When 毛糸通貨商品のセルがタップされた, the ShopService shall 購入確認ダイアログ表示前に `IUserPointService.GetYarnBalance() >= data.Price` を検証する。
2. If 購入確認ダイアログ表示前の残高検証で残高不足と判定された, then the ShopService shall `CommonMessageDialog` で「毛糸が足りません」メッセージを表示し、購入確認ダイアログを開かない。
3. When ガチャセルがタップされた, the ShopService shall ガチャ確認ダイアログ表示前に `IUserPointService.GetYarnBalance() >= price` を検証する。
4. If ガチャ確認ダイアログ表示前の残高検証で残高不足と判定された, then the ShopService shall `CommonMessageDialog` で「毛糸が足りません」メッセージを表示し、ガチャ確認ダイアログを開かない。
5. The ShopService shall 残高検証時に `ShopState.YarnBalance` を参照しない。

### Requirement 7: ガチャ家具獲得の `IUserItemInventoryService` 連携
**Objective:** As a ショップ利用プレイヤー, I want ガチャで獲得した家具が所持インベントリに加算されること, so that 獲得した家具を模様替えシーンで実際に配置できる

#### Acceptance Criteria
1. When ガチャ購入後の抽選結果が確定した, the ShopService shall 抽選された家具ごとに `IUserItemInventoryService.AddFurniture(furnitureId, 1)` を呼び出して所持数を加算する。
2. The ShopService shall ガチャ抽選結果として扱う家具識別子を、`IUserItemInventoryService.AddFurniture` が受理する `uint` 型と整合する形式で保持する。
3. If `IUserItemInventoryService.AddFurniture` が `UnknownId` を返した, then the ShopService shall クラスコンテキスト付きでエラーログを出力し、該当家具の加算をスキップしつつ残りの結果処理を継続する。
4. If `IUserItemInventoryService.AddFurniture` が `InvalidArgument` を返した, then the ShopService shall クラスコンテキスト付きでエラーログを出力し、該当家具の加算をスキップする。
5. The ShopService shall ガチャ結果メッセージに表示する家具名称を、`MasterDataState` の家具マスターから取得する (未登録 ID の場合は ID をそのまま表示)。

### Requirement 8: `ShopState` からのポイント・アイテム管理責務の除去
**Objective:** As a 開発者, I want `ShopState` が UI/ショップ固有の状態のみを保持し、グローバル状態の二重管理を発生させないこと, so that 状態の真実の源が一元化され保守性が向上する

#### Acceptance Criteria
1. The ShopState shall `YarnBalance` プロパティ、`ConsumeYarn` / `AddYarn` メソッド、`OnYarnBalanceChanged` イベントを公開しない。
2. The ShopState shall ショップタブ状態 (`CurrentTab` / `SetCurrentTab` / `OnTabChanged`) や商品リスト (`GachaList` / `ItemProductList` / `PointProductList`) などの「ショップシーン固有の状態」は引き続き保持可能である。
3. The ShopService shall 毛糸残高を参照・更新する際に `ShopState` を経由せず、`IUserPointService` を直接参照する。
4. The ShopView shall 毛糸残高の購読を `ShopState` 経由で行わず、`IUserPointService` のイベントを直接購読する。
5. The ShopState shall 毛糸残高や家具所持数のキャッシュを独自フィールドとして保持しない。

### Requirement 9: DI 登録と依存関係の整合
**Objective:** As a 開発者, I want `ShopService` / `ShopView` が RootScope のサービスを正しく解決できる形で登録されていること, so that 移行後もシーンロード時に VContainer が依存を解決できる

#### Acceptance Criteria
1. The ShopService shall コンストラクタ引数で `IUserPointService` および `IUserItemInventoryService` を受け取り、`[Inject]` 属性を付与する。
2. The ShopView shall `Construct` メソッドで必要な場合に `IUserPointService` を受け取り、`[Inject]` 属性を付与する。
3. The ShopScope shall `IUserPointService` / `IUserItemInventoryService` を自ら再登録せず、RootScope のシングルトン登録を継承して利用する。
4. The ShopService / ShopView shall 依存方向の規約 (View → Service → State) を維持し、State から Service/View への参照を発生させない。
5. When `ShopStarter.Start` が呼び出された, the ShopService shall `IUserPointService.GetYarnBalance()` を前提とした初期化 (モックデータ投入など) を実行できる。

### Requirement 10: 移行後の動作互換性
**Objective:** As a ショップ利用プレイヤー, I want 移行後もショップ画面の見た目と操作感が従来と等価であること, so that 移行が利用者体験を退行させない

#### Acceptance Criteria
1. The ShopView shall 移行後も既存のタブ構成・セル配置・購入確認/購入完了ダイアログのフローを同一に保つ。
2. When 毛糸通貨商品の購入確認で「はい」が選択された, the ShopService shall 従来と同等の購入完了メッセージ (`"{商品名}を購入しました！"` 相当) を表示する。
3. When 購入確認ダイアログで「いいえ」が選択された, the ShopService shall 残高操作を一切行わずダイアログを閉じる。
4. When 連続で複数回の購入操作が要求された, the ShopView shall 既存の `_isProcessing` ガードと同等の挙動で多重実行を防止する。
5. When ショップシーン離脱後に再訪問した, the ShopService shall 前回訪問時の毛糸消費結果を `IUserPointService` 経由で引き継いだ残高で表示する。
