# Requirements Document

## Project Description (Input)
時限ショップの追加(既存ショップへの追加)

## Introduction

既存のショップシーン（`shop-scene-mock` で整備されたタブ切り替え型のショップ画面）を改修し、「時限ショップ」カテゴリを追加する。合わせて、既存ショップをこの機能に合わせて再構成する。

主な変更点は以下の通り。

1. **ショップ商品マスターデータの追加**: furniture / outfit / リワード広告視聴商品 を CSV で定義し、`MasterDataState` に統合する（既存 outfit/furniture マスターと同様の読み込みパターン）。
2. **既存ショップの再構成**: タブは 1 つだけとし、カテゴリは「時限ショップ」「リワード広告視聴商品」の 2 つに統一する。ガチャカテゴリは表示上オミットする（コード自体は削除しない）。家具・衣装の通常カテゴリは廃止し、時限ショップへ統合する（家具・衣装は時限ショップ経由でのみ販売）。
3. **時限カテゴリの追加**: 上記ショップ内に「時限ショップ」を追加。30 分ごと（定数化）に内容が更新され、更新タイミングは全端末で一致する決定論的抽選とする。次回更新までのカウントダウンタイマーを表示する。outfit は既所持なら売り切れ表示（抽選からは除外しない）、furniture は制限なく購入可能。

本フェーズではリワード広告視聴カテゴリの表示および購入処理は実装対象外（マスターデータ上のカラム定義と将来拡張余地の確保のみ）とする。サーバー連携は行わず、マスターデータ化された商品を端末時間ベースで決定論的に抽選する。

### 実装スコープ（作業分担）

本機能の実装作業は以下の分担で進める。

- **AI（Claude）の担当**: C# コードの追加・変更（`Assets/Scripts/` 配下）、CSV データの追加・更新（`Assets/Resources/*.csv`）、シーン／プレファブの**読み取り**（構造理解のため）
- **ユーザーの担当**: Unity Editor 上での UI 作業全般 — シーン（`Assets/Scenes/Shop.unity`）の編集、プレファブ作成・編集（売り切れオーバーレイ `GameObject`、タイマー UI、カテゴリ縦並びレイアウト等）、`SerializeField` 参照の割り当て、`Addressables` 設定、Inspector 上の各種設定

このため要件のうち `[SerializeField]` 受け側のフィールド宣言・API シグネチャはコード側の実装範囲とし、シーン上での GameObject 配置・参照割り当て・ビジュアル調整はユーザーが Unity Editor で行う前提とする。

### 前提となる既存サービス

本機能は以下の既存サービスを利用し、`ShopState` には通貨残高やアイテム所持情報を保持しない（別 PR で `IUserPointService` / `IUserItemInventoryService` への移行が完了済み）。

- **`Root.Service.IUserPointService`**: 毛糸残高の取得（`GetYarnBalance`）、消費（`SpendYarn`）、加算（`AddYarn`）、変更通知（`YarnBalanceChanged`）を提供。時限ショップの購入判定・消費処理・残高表示はこのサービスを直接利用する。
- **`Root.Service.IUserItemInventoryService`**: 家具の所持数管理（`GetFurnitureCount` / `AddFurniture`）、着せ替えの所持判定・付与（`HasOutfit` / `GrantOutfit`）、変更通知（`FurnitureChanged` / `OutfitChanged`）を提供。outfit の売り切れ判定と購入後の付与、家具の複数所持管理はこのサービスを直接利用する。
- **既存 `CurrencyType` enum**: `Shop.State.CurrencyType = { Yarn, RealMoney }` が存在。本機能では `RewardAd` 値を追加する形で拡張する（広告視聴で取得する商品を支払い手段の観点で識別）。既存 `RealMoney` 値はガチャ実装と共に保持されるが本機能の対象外。
- **新規 `IClock` サービス（薄い導入）**: 端末時間（UTC）の取得を抽象化し、時限ショップのサイクル算出に用いる。本機能で `Root.Service.IClock` / `SystemClock` を新規導入し、`RootScope` に `Lifetime.Singleton` で登録する。

## Requirements

### Requirement 1: ショップ商品マスターデータの追加
**Objective:** As a 開発者, I want ショップ商品がマスターデータとして CSV から管理できること, so that 商品の追加・変更をコード修正なしで運用できる

#### Acceptance Criteria
1. The MasterDataImportService shall ショップ商品用の CSV (`Resources/shop_products.csv`) を `MasterDataImportService` 経由で読み込む（既存 `outfits.csv` / `furnitures.csv` と同じパターン）
2. The ショップ商品マスター shall 以下のカラムを保持する: `id`, `name`, `item_type`, `item_id`, `price`, `currency_type`（カラム順序は設計フェーズで確定する）
3. The `item_type` カラム shall 購入時に付与されるアイテムの種類（例: `furniture` / `outfit` / `point` 等）を保持し、商品の付与区分を識別する単一の軸として機能する（広告視聴商品か否かは `currency_type` で判別する）
4. The `item_id` カラム shall NOT NULL とし、`item_type` に対応するマスタ（`furnitures.csv` / `outfits.csv` 等）の ID を参照する
5. While `item_type = furniture` または `item_type = outfit` のとき, the `item_id` shall 対応するマスタの ID を保持する
6. While `item_type = point` のとき, the `item_id` の意味と `price` カラムの解釈（付与量を `price` で表現するか別途持つか）は設計フェーズで具体化する
7. The `currency_type` カラム shall `yarn`（ゲーム内通貨で支払い）または `reward_ad`（リワード広告視聴で支払い）のいずれかの値を取り、支払い手段を識別する
8. The CurrencyType enum shall 既存 `Shop.State.CurrencyType = { Yarn, RealMoney }` に `RewardAd` 値を追加する形で拡張される
9. The MasterDataState shall ショップ商品マスターの一覧を保持する配列プロパティ（例: `ShopProducts[]`）を公開する
10. When MasterDataImportService の `Import()` が呼び出されたとき, the MasterDataImportService shall ショップ商品マスターも一括でロードする（既存の Outfits/Furnitures と同じタイミング）
11. If CSV のパースに失敗した場合, the MasterDataImportService shall クラスコンテキスト付きでエラーログを出力し、空配列として扱う
12. The ショップ商品マスター shall `item_type` と `item_id` の組み合わせにより、既存の `Outfit` または `Furniture` マスターと紐付け可能となる

### Requirement 2: 既存ショップの構造再構成
**Objective:** As a プレイヤー, I want ショップ画面が時限ショップとリワード広告視聴商品のカテゴリに整理されていること, so that 目的の商品カテゴリへ迷わずアクセスできる

#### Acceptance Criteria
1. The ShopView shall ショップ画面 UI 上からタブ要素（アイテムタブ／毛糸タブのボタン・切替表示・関連ビジュアル）を削除する
2. The Shop 実装 shall タブ切替に関連するコード（`ShopTab` enum、`ShopState.CurrentTab` / `OnTabChanged`、`ShopService.SetCurrentTab`、`ShopView.UpdateTabVisuals` / `ShowContent` 等）は削除せず、UI から呼び出されない状態で残置する
3. The ShopView shall タブを介さず、「時限ショップ」「リワード広告視聴商品」のカテゴリを単一スクロール領域内に縦並びで表示する
4. The ShopView shall 「リワード広告視聴商品」カテゴリをレイアウト上は配置するが、本フェーズでは表示・購入処理を実装しない（プレースホルダーとして存在を確保する）
5. The Shop 実装 shall 既存のガチャカテゴリのコード（`GachaCellView`、`GachaData`、`ShopService.OnGachaTappedAsync` 等）は削除せず、ショップ画面上の表示・機能呼び出しのみオミットする
6. The Shop Scene shall 既存の単一シーン（`Assets/Scenes/Shop.unity`）を再利用し、新規シーン追加は行わない
7. The ShopService shall 既存の `InitializeMockData()` に含まれるハードコードされたモック商品データ（`ItemProductList` / `PointProductList` / `GachaList` への `new ProductData(...)` / `new GachaData(...)` 列挙）を削除し、ショップ商品マスターデータ駆動の初期化に置き換える
8. While ガチャカテゴリが UI 上でオミットされている間, the ShopService shall `GachaList` をモックデータで初期化せず、空のまま保持する（抽選ロジック・型定義は残置）

### Requirement 3: 時限ショップカテゴリの配置
**Objective:** As a プレイヤー, I want ショップ画面で時限ショップを閲覧できること, so that 期間ごとに更新される特別な商品を購入できる

#### Acceptance Criteria
1. The ShopView shall ショップ画面内に時限ショップ用のセル領域を持ち、リワード広告視聴カテゴリと同じスクロール領域内で配置する
2. The ShopView shall 時限ショップカテゴリの商品セルを手動配置する（動的生成は行わない）
3. The ShopView shall 時限ショップセルに商品アイコン、商品名、価格、通貨種別の情報を表示する
4. The ShopView shall 時限ショップセルに「時限であること」を示す専用バッジ・装飾・枠・カテゴリ見出しラベルなどの視覚要素を**表示しない**（時限性は更新タイマー UI のみで示す）
5. The ShopService shall 時限ショップに表示される各セルの内容を、ショップ商品マスターから抽選された結果に基づいてコードから設定する

### Requirement 4: 時限ショップの更新サイクル
**Objective:** As a プレイヤー, I want 時限ショップの内容が定期的に更新されること, so that 同じ商品に飽きず継続的に楽しめる

#### Acceptance Criteria
1. The ShopService shall 時限ショップの更新間隔を定数として定義し、マジックナンバーを使用しない（初期値 30 分）
2. The ShopService shall 更新サイクルを端末時間（ローカル時刻）に基づき、Unix エポックからの経過時間を更新間隔で量子化して決定する
3. The ShopService shall 同一の更新サイクル期間内では、どの端末・どのプレイヤーでも同一の時限ショップ内容となる決定論的抽選を行う
4. When 現在時刻が次回更新時刻に到達したとき, the ShopService shall 時限ショップの内容を即座に再抽選して表示を更新する
5. When ショップシーンがロードされたとき, the ShopService shall 現在時刻が属する更新サイクルに対応する時限ショップ内容を算出して表示する
6. The ShopService shall プレイヤーが同じ更新サイクル期間内にショップを再訪問しても、同一内容を表示する

### Requirement 5: 時限ショップ商品の抽選
**Objective:** As a プレイヤー, I want 時限ショップの商品がマスターデータからランダムかつ重複なく選ばれること, so that 期間ごとに新しい出会いがあり、同じ商品が並ばない

#### Acceptance Criteria
1. The ShopService shall 時限ショップに表示する家具商品を、ショップ商品マスター（`shop_products.csv`）のうち `item_type = furniture` の**全行**を母集合としてランダムに抽選する
2. The ShopService shall 時限ショップに表示する衣装商品を、ショップ商品マスターのうち `item_type = outfit` の**全行**を母集合としてランダムに抽選する
3. The ShopService shall 抽選時に商品ごとの重み付けを行わず、一様乱数で選出する
4. The ShopService shall 抽選には `System.Random(int seed)` を用い、シードを更新サイクルを一意に識別する値（例: サイクル開始時刻の Unix エポック秒を `int` に畳み込んだ値）から決定論的に算出することで、同一サイクル内では同じ結果を再現する
5. The ShopService shall 時限ショップで表示する家具・衣装それぞれの抽選枠数を定数として定義し、マジックナンバーを使用しない
6. The ShopService shall 時限ショップの抽選結果件数を、マスター件数に関わらず常に抽選枠数ぶんだけ生成する
7. While 該当 type のマスター商品件数が抽選枠数以上であるとき, the ShopService shall 抽選結果に同一商品 ID が重複して含まれないよう、非復元抽出（ユニーク抽選）で選出する
8. If 該当 type のマスター商品件数が抽選枠数より少ない場合, the ShopService shall 重複を許容する復元抽出で抽選枠数ぶんの商品を選出し、全枠を埋める（マスター件数が 0 の場合を除く）
9. If 該当 type のマスター商品件数が 0 件の場合, the ShopService shall 該当カテゴリのセルを空表示または非表示として扱い、クラスコンテキスト付きで警告ログを出力する
10. The ShopService shall 家具と衣装の抽選を独立して行い、家具セル間・衣装セル間それぞれで上記ユニーク性／重複ルールを適用する（家具と衣装をまたいだユニーク性制約は課さない）
11. The ショップ商品マスター shall 時限ショップ専用の絞り込み列（`is_timed` 等）を持たず、`item_type = furniture` / `item_type = outfit` の全行が時限ショップの抽選母集合となる

### Requirement 6: 次回更新までのタイマー表示
**Objective:** As a プレイヤー, I want 次回の時限ショップ更新までの残り時間を確認できること, so that 購入や再訪のタイミングを判断できる

#### Acceptance Criteria
1. The Shop Scene shall ショップ画面全体で**ただ 1 つ**の時限ショップ用タイマー UI を持ち、各セルや各カテゴリごとには持たない
2. The ShopView shall 単一のタイマー UI に次回更新までの残り時間を表示する
3. The ShopView shall 残り時間を人間が読みやすい形式（例: `HH:mm:ss` もしくは `mm:ss`）で表示する
4. While 時限ショップカテゴリが表示されているとき, the ShopView shall 残り時間を 1 秒以上の間隔で継続的に更新する
5. When 残り時間が 0 になったとき, the ShopService shall 時限ショップの内容を再抽選し、the ShopView shall セル表示とタイマー表示を新しい更新サイクルに切り替える
6. While シーンがアクティブでないとき, the ShopView shall タイマー更新処理を停止する
7. The タイマー UI 実装 shall 最小構成（単一の `MonoBehaviour` View ＋ `TextMeshProUGUI` ＋ `Update()` ベースの毎フレーム残り時間再計算）で実装し、過剰な抽象化や独自フレームワーク導入を避ける

### Requirement 7: 衣装（outfit）商品の売り切れ表示
**Objective:** As a プレイヤー, I want 既に所持している衣装は売り切れ表示されること, so that 重複購入を避けつつ抽選確率を把握できる

#### Acceptance Criteria
1. The ShopService shall 時限ショップの衣装商品抽選時に、プレイヤー所持状況によって抽選対象を除外しない
2. The ProductCellView (または衣装セルプレファブ) shall 売り切れ表示用の専用オーバーレイ `GameObject` を持ち、その中に半透明の黒画像と「売り切れ」ラベルを含む
3. If 抽選結果の衣装を既にプレイヤーが所持している場合, the ShopView shall 該当セルの売り切れオーバーレイ `GameObject` を `SetActive(true)` に切り替える（または同等の `CanvasGroup` 表示制御を行う）
4. While 衣装セルが売り切れ状態のとき, the ShopView shall 該当セルをタップ不可（`Button.interactable = false`）にする
5. The ShopService shall 衣装の所持判定に `IUserItemInventoryService.HasOutfit(uint outfitId)` を用いる（衣装は 1 種類につき 1 つしか所持できないため、所持済み = 売り切れ）
6. When `IUserItemInventoryService.OutfitChanged` が発火したとき, the ShopView shall 現在表示中の時限ショップ衣装セルの売り切れ状態を再評価し、オーバーレイの `SetActive` 状態を更新する
7. When 衣装セルの購入が成功したとき, the ShopService shall `IUserItemInventoryService.GrantOutfit(uint outfitId)` を呼び出してプレイヤーに付与する
8. When 衣装の購入が成功したとき, the ShopView shall 同一 `outfitId` のすべての時限ショップ衣装セル（重複抽選で複数表示されている場合を含む）の売り切れオーバーレイを即座にアクティブ化する
9. The ShopView shall 売り切れ表示のセルでも商品アイコン・名前・価格を通常通り閲覧可能とする（オーバーレイは半透明であり下層を完全には隠さない）

### Requirement 8: 家具（furniture）商品の購入制約
**Objective:** As a プレイヤー, I want 家具は制限なく購入できること, so that 好きな家具を複数所持して自由に部屋を作れる

#### Acceptance Criteria
1. The ShopService shall 時限ショップの家具商品に対して、所持数による購入制限を課さない（在庫上限なし、売り切れ概念なし）
2. The ShopView shall 家具セルに売り切れ表示を行わない（購入後も同一サイクル内で再購入可能な状態を維持する）
3. When 家具セルがタップされたとき, the ShopService shall 通貨残高と更新サイクルのみをチェックして購入処理を進める
4. When 家具セルの購入が成功したとき, the ShopService shall `IUserItemInventoryService.AddFurniture(uint furnitureId, int amount = 1)` を呼び出してプレイヤーに付与する
5. When 家具の購入が成功したとき, the ShopView shall 当該セルおよび同一 `furnitureId` の他セルの購入可否状態を、更新後の通貨残高に基づいて再評価する（売り切れ表示には切り替えない）
6. The ShopService shall 家具購入成功時にプレイヤーが同じ家具を複数所持できるようなデータ構造前提で処理する（`IUserItemInventoryService` がこれを保証する）

### Requirement 9: 時限ショップ商品の購入処理
**Objective:** As a プレイヤー, I want 時限商品を既存の購入フローで購入できること, so that 操作感が統一されている

#### Acceptance Criteria
1. When 時限ショップセルがタップされたとき, the ShopService shall 既存の汎用確認ダイアログ（`CommonConfirmDialog`）を用いて購入確認を表示する
2. When 購入確認で「はい」が選択されたとき, the ShopService shall `IUserPointService.SpendYarn(int)` で通貨を消費し、成功時に購入完了メッセージを表示する
3. When 購入確認で「いいえ」が選択されたとき, the ShopService shall ダイアログを閉じて処理をキャンセルする
4. If `IUserPointService.GetYarnBalance()` の返却値が商品価格に満たない場合（`currency_type = yarn` の商品）, the ShopView shall 時限ショップセルを暗めの視覚表現（例: 半透明・グレーアウト）に切り替え、`interactable = false` としてタップ自体を無効化する
5. While セルが残高不足によりタップ不可状態であるとき, the ShopView shall タップ操作で毛糸不足ダイアログを表示しない（タップ自体が発火しない）
6. When `IUserPointService.YarnBalanceChanged` が発火したとき, the ShopView shall 時限ショップセルのタップ可否状態と暗め表示の有無を残高に基づき再評価する
7. If 購入確認ダイアログを開いている間に更新サイクルが切り替わった場合, the ShopService shall 購入を中止し、更新されたことを通知するメッセージを表示する
8. If `IUserPointService.SpendYarn` が `Insufficient` を返した場合（購入確認中に他経路で残高が変動したケース）, the ShopService shall 毛糸不足ダイアログを表示せず、購入処理を中断してクラスコンテキスト付きでログ出力し、セル表示を最新残高に基づき再評価する
9. The ShopService shall `currency_type = reward_ad` の商品に対する購入処理は本フェーズでは実装せず、将来の Unity Ads 統合に備えてインターフェース上の分岐を残す
10. The 残高不足時の暗め表示・タップ無効ルール shall 時限ショップの `currency_type = yarn` の全セルに一律適用する

### Requirement 10: 状態管理とデータ構造
**Objective:** As a 開発者, I want 時限ショップ関連の状態が既存の ShopState パターンに統合されていること, so that 後続機能拡張（マスターデータ拡張・サーバー連携）で構造を崩さず拡張できる

#### Acceptance Criteria
1. The ShopState shall 現在の更新サイクル識別子（例: サイクル開始時刻の Unix エポック）を保持する
2. The ShopState shall 現在の時限ショップに表示中の家具商品リスト、衣装商品リストを保持する
3. The ShopState shall 時限ショップ更新イベント（内容更新時に発火）を提供する
4. The ShopState shall 次回更新までの残り時間を算出可能な API を提供する
5. The ShopState shall ショップ商品マスターの型に基づく商品データ構造を保持する（`id`, `name`, `type`, `item_id`, `price`, `currency_type` を含む）
6. The ShopState shall 通貨残高・アイテム所持情報を自身の状態として保持しない（これらは `IUserPointService` / `IUserItemInventoryService` の責務）
7. The ShopService shall 時限ショップ関連の状態更新は ShopState を介して行い、View は ShopState のイベントおよび `IUserPointService` / `IUserItemInventoryService` のイベントを購読して表示を更新する（依存方向: View → Service → State を維持）
