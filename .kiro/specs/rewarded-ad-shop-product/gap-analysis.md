# Gap Analysis: rewarded-ad-shop-product

承認済み要件 (`requirements.md`) と既存実装の差分を整理し、設計フェーズの判断材料を提供する。

---

## 1. 現状調査サマリ

### 1.1 既に用意されているフックポイント (流用可能な既存資産)

| 場所 | 役割 | 状況 |
|---|---|---|
| `Shop.State.CurrencyType.RewardAd` | 通貨種別 enum | **定義済み** (`Shop/State/ShopState.cs`) |
| `Shop.State.ShopState.RewardAdProductList` | リワード広告商品データの保持先 | **定義済み**、`ShopService.Initialize` で `Clear()` も実施 |
| `Shop.View.ShopView._rewardAdCells` | リワード広告商品セルの SerializeField 枠 | **定義済み**、`SetupCategoryCells` / `RefreshCategoryAppearance` / `SubscribeToCellEvents` の全パスに既に組込み済 (現状コメントに「placeholder, 0 cells」) |
| `Shop.Service.ShopService.OnProductCellTappedAsync` | タップ時の入口 | `CurrencyType.RewardAd` の分岐がスタブ (`return` のみ) として設置済み (`Shop/Service/ShopService.cs:286-290`) |
| `Shop.Service.ShopService.IsAffordable` | 購入可否判定 | `RewardAd` 分岐は `false` 固定 (実装ポイント) |
| `Shop.Service.ShopService.IsSoldOut` | 売り切れ判定 | Outfit 既所持判定のみ。**日次残数 0 判定追加点はここ** |
| `Shop.Service.ShopService.SplitShopProductsForTimedShop` | `CurrencyType.RewardAd` を時限抽選から既に除外している | 既存挙動の維持で OK |
| `Shop.View.ProductCellView.SetSoldOut` | 売り切れ Overlay 表示 | **既存 UI を流用可能** (要件「日次残数 0 → 売り切れ表示流用」と合致) |
| `Root.Service.MasterDataImportService.ImportShopProducts` | CSV パーサ | 6 カラム前提。**拡張ポイント** |
| `Assets/Resources/shop_products.csv` | マスター CSV | `id,name,item_type,item_id,price,currency_type` の 6 列。リワード広告行はまだ存在しない |
| `Root.Service.PlayerPrefsService` + `PlayerPrefsKey` | Json による永続化基盤 | 新規 enum 値を追加するだけで日次カウンタ用に流用可能 |
| `Root.Service.IClock` (`SystemClock`) | UTC 時刻供給 | **JST 変換ヘルパは未存在**。ローカル 0:00 境界判定で要対応 |
| `Root.Service.IDialogService` + `CommonConfirmDialog` / `CommonMessageDialog` | 確認/メッセージダイアログ基盤 | そのまま流用可 |
| `Root.Scope.RootScope` | Root Singleton の登録場所 | `IRewardedAdService` 登録の追加先 |

### 1.2 既存パターンの再確認

- **Service レイヤの命名/登録**: `Root.Service.UserPointService` が `IUserPointService` を実装し、`RootScope` で `Lifetime.Singleton + .As<IXxx>().AsSelf()` 登録。`UserPointService` のように `_state` + `_playerPrefsService` 依存 + `Load()` をコンストラクタ末尾で呼ぶ流儀
- **永続化**: `PlayerPrefsService.Save<T>(key, value)` は `JsonUtility.ToJson` 経由。`[Serializable]` クラスに `Version` フィールドを持たせる慣習 (`UserPointSnapshot.Version != CurrentVersion` ならリセット)
- **DI コンストラクタ**: `[Inject]` 必須 (`tech.md` 規約、IL2CPP ストリッピング対策)
- **UniTask + CancellationToken**: 非同期 API は末尾引数に `CancellationToken` を取る (`tech.md` 規約)
- **`Debug.LogError($"[ClassName] {e.Message}\n{e.StackTrace}");`** クラスコンテキスト付きログ

---

## 2. 要件 → 資産マッピングと欠落 (Gap)

| Requirement | 必要となる資産 | 既存 | 欠落 (Missing / Unknown / Constraint) |
|---|---|---|---|
| **Req 1** SDK 初期化・ライフサイクル | `IRewardedAdService`, `LevelPlayRewardedAdService` | なし | **Missing** — 新規作成必須 |
| **Req 2** ロード・自動再ロード・指数バックオフ | 同上、内部ステートマシン | なし | **Missing** |
| **Req 3** ショップ画面表示 | `ShopState.RewardAdProductList` + `_rewardAdCells` 群 | **あり** | フックは存在。`RewardAdProductList` への投入処理 / セル個数の Inspector 設定 / `IsAffordable` / `IsSoldOut` 拡張が **Missing** |
| **Req 4** 視聴フロー | `ShopService.OnProductCellTappedAsync` の RewardAd 分岐 | スタブのみ | 実装が **Missing**。`TryGrantPurchasedItem` 流用は `ItemType.Point` 分岐追加が **Missing** |
| **Req 5** 二重付与防止 | `_isProcessing` (`ShopView`) + Service 側のセッション管理 | View 側だけあり | Service 側のセッション管理が **Missing** |
| **Req 6** 失敗時通知 | `IDialogService` で既存ダイアログ | あり | テキスト定義が **Missing** |
| **Req 7** プラットフォーム別 / Editor スタブ | `RootScope` で実装切替、`#if UNITY_EDITOR` 分岐 | パターンなし | **Missing**。iOS ATT は **Unknown** (Unity Ads ATT サポートパッケージ採用可否) |
| **Req 8** アーキテクチャ遵守 | Root.Service レイヤ + VContainer | あり | 規約遵守の問題のみ |
| **Req 9** マスターデータ拡張 (`Amount` / `DailyCap`) | `ShopProduct` レコード + CSV + パーサ | 6 カラム | **Missing**: `ShopProduct` フィールド 2 つ追加、CSV ヘッダ拡張、`ImportShopProducts` のカラム数判定とパース処理拡張、`ProductData` への引き渡し |
| **Req 10** 日次キャップ + 永続化 + リセット | 新規 State + PlayerPrefsKey 追加 | パターンあり (`UserPointService` Load/Save) | **Missing** — 新規 State / Snapshot / Service or ShopService 拡張、JST 0:00 境界判定 (**Constraint**: `IClock` は UTC のみ) |
| **Req 11** `n/m` 残数表示 | `ProductCellView` の TMP_Text | 既存 `_nameText` / `_priceText` あり | **Missing** — 新規 SerializeField (`_remainingCountText` 等) を `ProductCellView` または派生クラスに追加 |

### 2.1 マスター CSV 拡張の影響

現状: `id,name,item_type,item_id,price,currency_type`  
新規: `id,name,item_type,item_id,price,currency_type,amount,daily_cap` (案)

- パーサ: `if (columns.Length < 6)` → `if (columns.Length < 8)` に変更
- `Yarn` 商品にも `amount`/`daily_cap` カラムは付与されるが、`Yarn` 商品では `amount`/`daily_cap` 列は無視される (現状の `ShopProduct.Price` で十分)
- 既存 46 行全てを 8 カラムに書き換える必要あり (空文字 2 カラム追加)
- **Constraint**: `Yarn` 通貨で `ItemType.Point` (毛糸パック) を将来扱う場合、`amount` を毛糸付与数として再利用すれば自然 (要件 9-8 の後方互換性に合致)

### 2.2 日次境界判定の制約

- `IClock.UtcNow` は UTC のみ。JST 0:00 は `UtcNow.AddHours(9).Date` 等で計算可能
- 既存 `TimedShopCycleCalculator` も UTC を直接扱っており、JST 変換ロジックは新規追加が必要
- **Research Needed**: 日次境界を JST 固定にするか、`TimeZoneInfo.Local` 依存にするか

---

## 3. 実装アプローチ Options

### Option A — 既存 ShopService に全部詰める
- ShopService 内に LevelPlay 呼び出し + 日次キャップ + RewardAd 分岐を全部書く
- ✅ 新規ファイル最小、初期スピード最速
- ❌ ShopService が既に 530 行。さらに LevelPlay 制御まで持つと **責務超過**
- ❌ Editor / 実機切替が `#if` でスパゲッティ化、テスト分離困難
- ❌ ショップ以外 (例: ホーム画面のリワード広告) へ転用できない

### Option B — Root.Service に `IRewardedAdService`、Shop.Service に新規 `RewardAdShopService`
- LevelPlay 制御は `Root.Service.IRewardedAdService` (Singleton)、ショップ寄りの日次キャップ + 残数算出 + 視聴フローオーケストレーションは Shop 内に新設 `RewardAdShopService` (Scoped) に分割
- ✅ レイヤと責務が綺麗に分離。SDK 制御は再利用可能
- ✅ 既存 `ShopService` は触りを最小化 (`OnProductCellTappedAsync` の RewardAd 分岐は `RewardAdShopService` へ委譲)
- ✅ Editor スタブの差し替えが容易
- ❌ ファイル数増 (Service 2 つ + 設定 ScriptableObject + Snapshot + State 拡張)

### Option C (推奨) — ハイブリッド: SDK は新規 Root.Service、日次キャップは既存 ShopService に統合
- LevelPlay 制御だけ `Root.Service.IRewardedAdService` (Singleton) として新設
- 日次キャップ管理は既存 `ShopService` (Shop 層) を拡張する形で完結 — 残数取得 API (`GetDailyRemainingCount(productId)`) と `IsSoldOut` 拡張、`OnProductCellTappedAsync` の RewardAd 分岐実装を `ShopService` 内で行う
- 永続化用 `RewardAdDailyCountSnapshot` + `PlayerPrefsKey.RewardAdDailyCount` を新規追加
- ✅ 「広告 SDK 抽象」と「ショップビジネスロジック」のレイヤ違いを保ちつつ、ファイル数を最小化
- ✅ 既存 ShopService の `IsSoldOut` / `IsAffordable` / `TryGrantPurchasedItem` を最大限再利用
- ✅ 将来ホーム画面等で `IRewardedAdService.ShowAsync` 単体利用が可能
- ❌ ShopService の責務は微増 (許容範囲)

---

## 4. 効果・リスク見積もり

| 項目 | 評価 | 根拠 |
|---|---|---|
| **Effort** | **M (3–7 日)** | LevelPlay 実装 + Editor スタブ 1.5 日、マスター拡張 + CSV 更新 + パーサ 0.5 日、ShopService 拡張 + 日次キャップ + 永続化 1.5 日、View (`ProductCellView` 拡張 + n/m 表示) 0.5 日、Android/iOS 実機検証 1–2 日 |
| **Risk** | **Medium** | LevelPlay 自体は公式 SDK で安定。ただし (a) iOS ATT/SKAdNetwork 検証、(b) `OnAdRewarded`/`OnAdClosed` 順序非依存設計、(c) JST 境界とリセットの遡及計算、の 3 点が予測困難。Editor スタブで本体ロジックは安全に検証可能 |

---

## 5. 推奨される設計フェーズへの引き継ぎ

### 5.1 推奨アプローチ
**Option C (ハイブリッド)** を起点に設計を開始する。具体的な構成要素：

| レイヤ | 新規/既存 | 要素 |
|---|---|---|
| `Root.Service` | 新規 | `IRewardedAdService` インターフェース |
| `Root.Service` | 新規 | `LevelPlayRewardedAdService` (実機実装) |
| `Root.Service` | 新規 | `EditorRewardedAdService` (Editor スタブ) |
| `Root.Service` (設定) | 新規 | `RewardedAdConfig` (ScriptableObject、App Key / Ad Unit ID 保持) |
| `Root.Scope.RootScope` | 拡張 | 条件付き `IRewardedAdService` 登録 |
| `Shop.State.ShopProduct` | 拡張 | `int Amount` / `int? DailyCap` 追加 |
| `Shop.State.ProductData` | 拡張 | `int? DailyCap`, `int? RemainingCount` 追加 (もしくは Service 経由で算出) |
| `Shop.State.ShopState` | 拡張 (検討) | 日次カウントの保持 (もしくは ShopService 内 dict) |
| `Shop.Service.ShopService` | 拡張 | RewardAd 視聴フロー、`IsSoldOut` 拡張、`GetDailyRemainingCount(productId)` 追加 |
| `Shop.View.ProductCellView` | 拡張 | `_remainingCountText` 追加、`SetRemainingCount(int n, int m)` API 追加 |
| `Shop.View.ShopView` | 微修正 | 残数表示の Refresh パスを RewardAd セルにも適用 |
| `Root.Service.MasterDataImportService` | 拡張 | `ImportShopProducts` を 8 カラム対応に拡張 |
| `Root.Service.PlayerPrefsKey` | 拡張 | `RewardAdDailyCount` 追加 |
| `Assets/Resources/shop_products.csv` | 拡張 | ヘッダ 2 列追加、既存行に空文字 2 列付与、リワード広告行を新規追加 |
| プラットフォーム設定 | 新規 | iOS Info.plist: `NSUserTrackingUsageDescription` / Android: AD_ID パーミッション (既存 EDM4U の解決済み範疇か確認) |

### 5.2 Research Needed (設計フェーズで決定すべき項目)

1. **iOS ATT 連携方式** — Unity 公式 `com.unity.ads.ios-support` パッケージを採用するか、自前 Native Plugin か。LevelPlay 側で ATT 状態を自動取得する仕組みがあるか要調査
2. **日次境界の固定タイムゾーン** — JST 固定 (UTC+9) を採用するか、端末ロケール (`TimeZoneInfo.Local`) に従うか
3. **マスター CSV カラム順** — 末尾追加方式 (互換性最優先) を採用するが、`Yarn` 商品行の `amount`/`daily_cap` 空文字を許容する記法を確定
4. **`Amount` フィールドの解釈** — `ItemType.Point` でのみ毛糸付与数として使う / `Furniture` / `Outfit` では常に 1 個固定とする
5. **`n/m` 表示の UI 実装** — `ProductCellView` 共通拡張 (毛糸/時限商品では非表示) vs `RewardAdProductCellView` 派生クラス
6. **`ShopView._rewardAdCells` の Prefab 構成** — Inspector で何枠設置するか (マスターで定義する商品数の上限を決める必要)
7. **LevelPlay の Test Suite 起動条件** — Editor かつ特定フラグでのみ `LevelPlay.LaunchTestSuite()` を呼ぶ仕組みを入れるか
8. **広告ロード開始タイミング** — RootScope 起動時 vs Shop シーン進入時 vs LazyInit。Req 1-1 は「アプリ起動時」を示唆するが、iOS ATT 応答待ちと両立する必要あり
9. **`OnAdRewarded` / `OnAdClosed` の順序非依存ステートマシン設計** — `UniTaskCompletionSource` を 1 個で済ますか、両方発火を待ち合わせる方式か
10. **マスター再インポートと日次カウント整合** — 商品 ID の付け替え時にカウントが宙ぶらりんになる可能性。整合性チェックの設計判断必要

---

## 6. 結論

要件は既存のショップ実装の**意図的に空けられたフックポイント**に綺麗にハマる構造になっており、ブラウンフィールド統合としては良条件。LevelPlay 制御を Root.Service に閉じ込め、ショップ寄りの責務 (日次キャップ・視聴フローオーケストレーション・UI) は既存 `ShopService` / `ShopView` / `ProductCellView` を拡張する **Option C** が、既存パターンへの整合・将来再利用・実装工数・テスト容易性のバランスで最も妥当。

設計フェーズでは特に「iOS ATT 連携」「JST 境界判定」「Reward/Closed 順序非依存ステートマシン」の 3 点を最初に詰める必要がある。
