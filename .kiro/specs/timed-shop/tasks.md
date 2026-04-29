# Implementation Plan

## Branches

**Base**: `feature/timed-shop`

| Branch | Tasks | Goal |
|--------|-------|------|
| `feature/timed-shop-foundation` | 1-3 | ショップ商品マスタ CSV のロードと `IClock` 抽象が稼働し、`MasterDataState.ShopProducts` が参照可能な状態 |
| `feature/timed-shop-logic` | 4-9 | 純粋関数と `ShopState` / `ShopService` がマスタ駆動・サイクル管理・購入分岐に対応し、Service レベルで時限ショップが回る状態 |
| `feature/timed-shop-view` | 10-13 | `ShopView` / `ProductCellView` / `TimedShopTimerView` が改修され、ユーザーがシーン編集を行えば UI として時限ショップが動く状態 |

## Tasks

### Branch: `feature/timed-shop-foundation`

- [x] 1. ショップ商品マスタの型と通貨種別 enum を整備する
  - [x] 1.1 付与アイテム種別を表す `ItemType` enum を `Shop.State` に新設する
    - 値は `Furniture` / `Outfit` / `Point` の 3 種とする
    - 名前空間は `Shop.State`
  - [x] 1.2 既存 `Shop.State.CurrencyType` に `RewardAd` 値を追加する
    - 既存 `Yarn` / `RealMoney` の数値順を変更しない
    - 既存 switch 文で `RewardAd` 未対応のケースは `default` 節へ落ちるか、明示的に「本フェーズ未対応」として early return する分岐を追加する
  - [x] 1.3 ショップ商品マスタ行を表す `ShopProduct` レコードを `Shop.State` に定義する
    - フィールド: `Id` / `Name` / `ItemType` / `ItemId` / `Price` / `CurrencyType`
    - 不変な `record` として宣言し、`#nullable enable` を付与する
  - [x] 1.4 `Root.State.MasterDataState` に `ShopProducts` 配列フィールドを追加する
    - 既存 `Outfits` / `Furnitures` と同列のパブリックフィールドとして配置する
    - 初期値は空配列でヌル安全を担保する
  - _Requirements: 1.2, 1.3, 1.4, 1.5, 1.6, 1.7, 1.8, 1.9, 1.12, 10.5_

- [x] 2. shop_products.csv の読み込み処理を `MasterDataImportService` に追加する
  - [x] 2.1 `Assets/Resources/shop_products.csv` をヘッダ行付きで作成する
    - カラム順: `id,name,item_type,item_id,price,currency_type`
    - 動作確認用に家具・衣装混在のサンプル数行を含める
  - [x] 2.2 `MasterDataImportService.Import()` から呼び出す `ImportShopProducts` を追加する
    - 既存 `outfits.csv` / `furnitures.csv` と同じ `Resources.Load<TextAsset>` → CSV パース → 配列セットのパターンを踏襲する
    - 1 行目をヘッダとしてスキップする
    - 行ごとに `int.TryParse` / `Enum.TryParse`（大文字小文字許容）でバリデーションし、失敗行は警告ログを出してスキップする
    - `currency_type` は `yarn` / `reward_ad` のみ許容（`real_money` 行はスキップ）
    - パース失敗時はクラスコンテキスト付きでエラーログを出し `MasterDataState.ShopProducts` を空配列にフォールバックする
  - _Requirements: 1.1, 1.10, 1.11_

- [x] 3. (P) `IClock` 抽象とシステム時刻実装を導入する
  - [x] 3.1 `Root.Service` 配下に `IClock` インターフェースと `SystemClock` 実装を追加する
    - `UtcNow` プロパティのみを公開する
    - `SystemClock` は `DateTimeOffset.UtcNow` を直接返す薄い実装とする
  - [x] 3.2 `RootScope` で `SystemClock` を `Lifetime.Singleton` として `IClock` に登録する
    - 既存 Singleton 登録の並びに沿って追加する
  - _Requirements: 4.2_

### Branch: `feature/timed-shop-logic`

- [x] 4. 時限ショップの定数と決定論的計算用の純粋関数を実装する
  - [x] 4.1 `Shop.Service.TimedShopConstants` を新設する
    - 更新間隔（30 分）、家具抽選枠数（6）、衣装抽選枠数（6）を定数として定義する
    - マジックナンバーを使わない構造にする
  - [x] 4.2 (P) `TimedShopCycleCalculator` を純粋関数として実装する
    - `Calculate(utcNow, interval)` がサイクル ID・開始時刻・次回更新時刻・残り時間・抽選シードを `TimedShopCycleSnapshot` で返す
    - サイクル ID は Unix エポック秒 / 更新間隔秒の整数除算で算出する
    - シードは サイクル ID の高 32 ビットと低 32 ビットを XOR 折り畳みして `int` 化する
    - `interval <= TimeSpan.Zero` を `ArgumentOutOfRangeException` で弾く
  - [x] 4.3 (P) `TimedShopLottery` を純粋関数として実装する
    - `DrawTimedProducts(source, slotCount, seed)` を `System.Random(int seed)` で実装する
    - `source.Count == 0` は空配列を返す
    - `source.Count >= slotCount` は Fisher-Yates シャッフルの先頭 `slotCount` を返す（非復元抽出）
    - `0 < source.Count < slotCount` は `Random.Next(count)` を `slotCount` 回呼ぶ復元抽出で枠を埋める
    - 入力 `source` を破壊しない（コピーして処理する）
    - `slotCount <= 0` を `ArgumentOutOfRangeException` で弾く
  - [ ] 4.4* `TimedShopCycleCalculator` / `TimedShopLottery` の単体テストを追加する
    - サイクル境界直前・直後・中央、連続呼出の単調性
    - 母集合 0 件・slot 未満・slot 同数・slot 超過、同一シードで再現、異シードで差異
    - 受入基準 4.2 / 4.3 / 4.5 / 4.6 / 5.3 / 5.4 / 5.6 / 5.7 / 5.8 / 5.9 を直接被覆する補助テストとして MVP 後に着手可能
  - _Requirements: 4.1, 4.2, 4.3, 4.5, 4.6, 5.3, 5.4, 5.5, 5.6, 5.7, 5.8, 5.9_

- [x] 5. `ProductData` と `ShopState` を時限ショップ向けに拡張する
  - [x] 5.1 `Shop.State.ProductData` レコードに付与アイテム種別とマスタ参照 ID を追加する
    - `ItemType`、`ProductId`（`ShopProduct.Id`）、`ItemId`（`Outfit.Id` / `Furniture.Id`）を追加する
    - 既存呼出箇所（モック初期化など）はビルドが通らなくなるため、後続タスク 6 で同時に整理する前提で追加する
    - `CurrencyType == RewardAd` で広告視聴商品を識別する方針を維持する
  - [x] 5.2 `Shop.State.ShopState` に時限ショップ用のフィールドとイベントを追加する
    - 表示中商品リスト: `FurnitureProductList` / `OutfitProductList` / `RewardAdProductList` / `TimedFurnitureProductList` / `TimedOutfitProductList`
    - サイクル情報: `CurrentCycleId`（初期値 0）、`NextUpdateAt`
    - イベント: `OnTimedShopUpdated`
    - 状態更新 API: `ApplyTimedShopUpdate(cycleId, nextUpdateAt, timedFurniture, timedOutfit)`（リスト差し替え後にイベント発火）
    - 既存 `ShopTab` / `CurrentTab` / `OnTabChanged` / `SetCurrentTab` / `GachaList` は残置する
    - 既存 `ItemProductList` / `PointProductList` は本タスクで削除する（`InitializeMockData` の撤去と同時に消費者がなくなる前提）
    - 通貨残高や所持情報を保持しない既存方針を維持する
  - _Requirements: 1.3, 10.1, 10.2, 10.3, 10.4, 10.5, 10.6, 10.7_

- [x] 6. `ShopService` をマスタ駆動初期化に切り替える
  - [x] 6.1 既存のモック初期化と毛糸不足ダイアログ呼出を撤去する
    - `InitializeMockData()` を削除する
    - `ShowYarnInsufficientAsync()` を削除する（残高不足はタップ無効化で防ぐ方針へ移行）
    - `GachaList` はモック投入を行わず空のまま保持する
  - [x] 6.2 `ShopProduct` から `ProductData` への射影ヘルパーを追加する
    - `ItemType` 別に `Outfit` / `Furniture` マスタを引いて `Name` / `IconPath` を解決する
    - 参照解決失敗（マスタに該当 ID なし）はクラスコンテキスト付きで警告ログを出しスキップする
  - [x] 6.3 通常カテゴリ（家具・衣装）の表示リストをマスタ駆動で初期化する
    - `Initialize()` でマスタ `ShopProducts` を `ItemType` で絞り、家具 6 件・衣装 6 件を `ShopState.FurnitureProductList` / `OutfitProductList` に投入する
    - `RewardAdProductList` は本フェーズでは投入しない（プレースホルダー）
    - コンストラクタには `[Inject]` を付与し IL2CPP のストリッピングを防ぐ
  - _Requirements: 2.7, 2.8, 2.11, 2.12, 2.13_

- [x] 7. `ShopService` にサイクル管理と再抽選フローを実装する
  - [x] 7.1 `ShopService` を `VContainer.Unity.ITickable` 実装に変更する
    - `Initialize()` 末尾で初回サイクル算出と `RebuildTimedShop` を呼び、`ApplyTimedShopUpdate` で State へ反映する
    - `Tick()` 内で `IClock.UtcNow` から最新スナップショットを算出し、`ShopState.CurrentCycleId` と異なる場合のみ再抽選を実行する
  - [x] 7.2 サイクル切替時の再抽選と State 反映を実装する
    - `RebuildTimedShop(seed)` 内でマスタを `ItemType` ごとに分け、`TimedShopLottery.DrawTimedProducts` を 2 回（家具・衣装）呼ぶ
    - 抽選結果を `ProductData` 列に射影して `ApplyTimedShopUpdate` で State に反映する
    - 母集合 0 件の場合はクラスコンテキスト付きで警告ログを出し、対応リストを空のまま反映する
    - 家具と衣装はそれぞれ独立に抽選し、跨ぎユニーク制約は課さない
  - _Requirements: 4.4, 5.1, 5.2, 5.5, 5.6, 5.9, 5.10, 5.11, 6.5_

- [x] 8. 時限ショップの購入分岐と購入確認フローを実装する
  - [x] 8.1 売り切れ・残高不足・時限由来かを判定する API を追加する
    - `IsSoldOut(data)`: `ItemType == Outfit` のとき `IUserItemInventoryService.HasOutfit(data.ItemId)` で判定する。家具は常に `false`
    - `IsAffordable(data, balance)`: `CurrencyType == Yarn` の商品で `balance >= price` を判定する。`RewardAd` 商品は本フェーズでは常に `false`
    - `IsTimedShopProduct(data)`: `_state.TimedFurnitureProductList` / `TimedOutfitProductList` への参照等価で判定する
  - [x] 8.2 `OnProductCellTappedAsync` で購入確認フローとサイクル切替検知を実装する
    - タップ時点の `CycleId` をローカル保持し、時限商品の場合のみ確認後に `_state.CurrentCycleId` と再比較する
    - サイクルが切り替わっていた場合は `CommonMessageDialog` で「時限ショップが更新されました」を表示して購入を中止する
    - 通常カテゴリ家具・衣装の購入はサイクル切替検知の対象外とする
    - 購入確認は既存 `CommonConfirmDialog` を使用し、「いいえ」選択時はキャンセルする
  - [x] 8.3 通貨消費と所持品付与を実装する
    - `IUserPointService.SpendYarn(price)` を呼び、`Insufficient` 戻り値の場合は毛糸不足ダイアログを出さずクラスコンテキスト付きでログ出力して中断する
    - 成功時は `ItemType` に応じて `IUserItemInventoryService.GrantOutfit(itemId)` または `AddFurniture(itemId, 1)` を呼ぶ
    - 購入完了メッセージを `CommonMessageDialog` で表示する
    - async API は `CancellationToken` を末尾引数として受け取る
  - [x] 8.4 `RewardAd` 商品向けのスタブ分岐を残す
    - `CurrencyType == RewardAd` の場合は本フェーズでは即 return とし、将来の Unity Ads 統合点が分岐として確認できる状態にする
  - _Requirements: 7.1, 7.5, 7.7, 8.3, 8.4, 8.5, 8.6, 9.1, 9.2, 9.3, 9.7, 9.8, 9.9, 10.7, 10.8_

- [x] 9. `ShopScope` の DI 登録を `ITickable` 対応に更新する
  - `ShopService` を `AsSelf().As<ITickable>()` の複数登録に変更する
  - `ShopState` の登録は既存どおり `Lifetime.Scoped` を維持する
  - 既存 `ShopStarter` 等のエントリポイント登録に影響を与えない
  - _Requirements: 4.1, 4.2_

### Branch: `feature/timed-shop-view`

- [ ] 10. (P) `ProductCellView` を暗め表示と売り切れ表示に対応させる
  - `_canvasGroup` / `_soldOutOverlay` / `_dimmedAlpha` を `[SerializeField]` で受ける
  - `SetDimmed(bool)` で `CanvasGroup.alpha` を切り替える（標準時 1.0、暗め時は `_dimmedAlpha`）
  - `SetSoldOut(bool)` でオーバーレイ `GameObject` の `SetActive` を切り替え、`Button.interactable` を `false` に固定する
  - 売り切れ ＞ タップ無効の優先度を保証する
  - 参照が未割り当ての場合はクラスコンテキスト付きの警告ログを出して当該操作を no-op にする
  - 既存 `Setup` / `SetInteractable` / `OnTapped` API は変更しない
  - _Requirements: 7.2, 7.3, 7.4, 7.9, 9.4, 9.5_

- [ ] 11. (P) `TimedShopTimerView` を新設する
  - `MonoBehaviour` ＋ `TextMeshProUGUI` ＋ `Update()` の最小構成で実装する
  - `Construct(IClock, ShopState)` メソッドインジェクションで依存を受け取る
  - `Update()` で `state.NextUpdateAt - clock.UtcNow` を再計算し、負値は 0 にクランプする
  - 残り時間が 1 時間未満は `mm:ss`、それ以上は `HH:mm:ss` 形式で表示する
  - `state.NextUpdateAt == default` の初期化前は `--:--` を表示する
  - `_remainingText` 未割り当て時はクラスコンテキスト付きで警告ログを出し、Update を no-op にする
  - GameObject 非アクティブ時は `Update()` が呼ばれず更新が止まる Unity 標準挙動を活用する
  - _Requirements: 6.1, 6.2, 6.3, 6.4, 6.6, 6.7_

- [ ] 12. `ShopView` をタブ撤去後のカテゴリ縦並び構成に改修する
  - [ ] 12.1 カテゴリ別セルリストとタイマー参照の `[SerializeField]` を整備する
    - `_furnitureCells` / `_outfitCells` / `_rewardAdCells` / `_timedFurnitureCells` / `_timedOutfitCells` / `_timerView` / `_yarnBalanceText` を追加する
    - 既存タブ関連 `[SerializeField]` はコード上残置し、シーン側で参照解除されてもヌル安全に動作する
    - `_gachaCells` も既存どおり残置する（UI 起動から呼出は行わない）
  - [ ] 12.2 起動時のセル Setup フローをマスタ駆動に置き換える
    - `Start()` 系のフローから `UpdateTabVisuals` / `ShowContent` の呼出を撤去する
    - 各カテゴリのセルを対応する `ShopState` リストの要素で `ProductCellView.Setup` する
    - 母集合不足や空リストの場合に該当セル GameObject を `SetActive(false)` でフォールバックする
  - [ ] 12.3 状態変更イベントを購読してセル状態を再評価する
    - `IUserPointService.YarnBalanceChanged` を購読し、各セルの暗め表示とタップ可否を `ShopService.IsAffordable` に基づき更新する
    - `IUserItemInventoryService.OutfitChanged` を購読し、衣装セル全体の売り切れ表示を `ShopService.IsSoldOut` で再評価する（同一 outfitId が複数表示される場合を含む）
    - `ShopState.OnTimedShopUpdated` を購読し、時限カテゴリのセル再 Setup と全カテゴリの可否再評価を実行する
    - 暗め判定と売り切れ判定は独立に評価し、売り切れセルでも残高十分なら暗めにしない
    - 残高不足ルールは時限・家具・衣装の `Yarn` 商品すべてに一律適用する
  - [ ] 12.4 タップハンドラとシーン遷移を整える
    - 各セルの `OnTapped` で `ShopService.OnProductCellTappedAsync` を呼ぶ
    - 既存の戻るボタン / シーン遷移ハンドリングを維持する
  - _Requirements: 2.1, 2.3, 2.4, 2.5, 2.6, 3.1, 3.2, 3.3, 3.5, 6.5, 7.6, 7.8, 9.4, 9.5, 9.6, 9.10, 10.8_

- [ ] 13. `ShopScope` に `TimedShopTimerView` のコンポーネント登録を追加する
  - `ShopView` と同じく `[SerializeField]` で `_timerView` を受け、非 null のとき `builder.RegisterComponent(_timerView)` を呼ぶ
  - 既存 `ShopView` の `RegisterComponent` パターンに揃える
  - シーン上に TimerView が未配置の場合でもエラーにならないよう null チェックを徹底する
  - _Requirements: 6.1_
