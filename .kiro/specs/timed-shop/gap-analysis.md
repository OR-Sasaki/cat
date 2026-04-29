# Implementation Gap Analysis — timed-shop

## 1. Current State Investigation

### 1.1 既存ショップ実装の概要

`Assets/Scripts/Shop/` 配下に標準シーン構造（Scope / Starter / Service / State / View）で実装済み。`shop-item-point-migration` 仕様の完了により、毛糸残高・家具インベントリは RootScope の `IUserPointService` / `IUserItemInventoryService` に一本化されている。

| ファイル | 役割 | 時限ショップ追加時の影響 |
|---------|------|--------------------------|
| `Shop/Service/ShopService.cs` | ビジネスロジック / モック初期化 / ガチャ抽選 / 購入処理 | **大改修**: モック削除、マスタ駆動化、抽選ロジック追加、購入分岐拡張 |
| `Shop/State/ShopState.cs` | タブ状態 + モック商品リスト | **拡張**: 表示中商品リストを時限／家具／衣装で持ち、サイクル ID と更新イベントを追加 |
| `Shop/View/ShopView.cs` | タブ UI / セル列 / 残高購読 | **改修**: タブ呼び出しオミット、カテゴリ縦並び、タイマー UI 連携、暗め表示再評価 |
| `Shop/View/ProductCellView.cs` | 汎用商品セル | **拡張**: 暗め表示 / 売り切れ表示 API を追加 |
| `Shop/View/GachaCellView.cs` | ガチャセル | 触らず（UI から呼び出し停止） |
| `Shop/Scope/ShopScope.cs` | DI 登録 | 影響なし（RootScope の依存はそのまま解決） |
| `Shop/Starter/ShopStarter.cs` | `ShopService.Initialize()` 呼び出し | 内部実装が変わるが Starter 自体は据え置き |

### 1.2 マスターデータ読み込みパターン

`Root/Service/MasterDataImportService.cs` が確立されたパターンを持つ。

```csharp
var csv = Resources.Load<TextAsset>("furnitures");
var lines = csv.text.Split('\n').Skip(1).Where(line => !string.IsNullOrWhiteSpace(line));
_masterDataState.Furnitures = lines.Select(line => {
    var columns = line.Split(',');
    return new Furniture { Id = uint.Parse(columns[0].Trim()), ... };
}).ToArray();
```

- 既存 CSV: `Assets/Resources/outfits.csv` (`id,type,name`), `Assets/Resources/furnitures.csv` (同形式)。Resources 直下にフラット配置
- `MasterDataState` のフィールドは public 配列（`Outfit[] Outfits` / `Furniture[] Furnitures`）
- Import は冪等（`IsImported` フラグで再実行を防止）

### 1.3 RootScope 提供サービス（時限ショップから利用）

| サービス | 提供 API | 時限ショップでの用途 |
|---------|---------|---------------------|
| `IUserPointService` | `GetYarnBalance` / `SpendYarn` / `AddYarn` / `YarnBalanceChanged` | 残高判定・消費・残高変動購読 |
| `IUserItemInventoryService` | `HasOutfit` / `GrantOutfit` / `AddFurniture` / `OutfitChanged` / `FurnitureChanged` | outfit 売り切れ判定／付与、家具付与 |
| `MasterDataState` | `Outfits[]` / `Furnitures[]` + 新規 `ShopProducts[]` | 抽選母集合 + 既存マスタ参照 |
| `MasterDataImportService` | `Import()` / `Imported` イベント | shop_products.csv 追加ロード |
| `IDialogService` | `OpenAsync<T, TArgs>` | `CommonConfirmDialog` / `CommonMessageDialog` 表示 |
| `SceneLoader` | `Load(string)` | 戻るボタンでのシーン遷移 |

### 1.4 ダイアログ／UI パターン

- `CommonConfirmDialog` (`Assets/Scripts/Root/View/CommonConfirmDialog.cs`) は `(Title, Message, OkButtonText, CancelButtonText)` を引数に取り `DialogResult.Ok/Cancel` を返す。Addressables 経由でロード
- `CommonMessageDialog` も同様のパターンで利用可能
- `BaseDialogView` / `BackdropView` / `FadeView` で `CanvasGroup` を `[SerializeField]` で持つ既存パターンあり → **暗め表示で `CanvasGroup.alpha` を使うのが整合的**

### 1.5 周期処理（Tick）の既存パターン

`VContainer.Unity.ITickable` を実装したサービスが 3 つ存在:

| 実装 | 用途 |
|------|------|
| `Root/Service/DialogContainer.cs` | ダイアログのバックボタン制御 |
| `Home/Service/IsoInputService.cs` | アイソメトリック入力検知 |
| `Home/Service/RedecorateCameraService.cs` | カメラ操作 |

→ **時限ショップサービスも `ITickable` 実装でカウントダウン更新が可能**（独自 Update Loop は不要）。MonoBehaviour 経由のフレーム更新（`destroyCancellationToken`）は `ShopView.OnGachaCellTapped` 等で実績あり。

### 1.6 乱数

`ShopService.cs:18` で `static readonly System.Random Rng = new();` が宣言されているが、**シードなし**で初期化されているため決定論性なし。要件 4.3 / 5.4 のため**サイクル ID をシードに `new System.Random(seed)` を生成する必要あり**。

### 1.7 時刻取得

- 全コードを `DateTime.Now` / `DateTime.UtcNow` で grep → **0 件**
- 時刻抽象化（`IClock` / `ITimeService`）は存在しない
- → 時限ショップは時刻取得抽象を**新規導入するか、`DateTime.UtcNow` を直接使うか**の判断が必要

### 1.8 既存 CurrencyType / ProductData

```csharp
// Shop/State/ShopState.cs
public enum CurrencyType { Yarn, RealMoney }
public enum ProductType { Item, YarnPack }
public record ProductData(string Name, string IconPath, int Price,
    CurrencyType CurrencyType, ProductType ProductType, int? YarnAmount = null);
```

- `RealMoney` は毛糸パック課金（既存）
- `reward_ad` を追加すると enum 拡張で済むが、`ProductType` との関係が論点（reward_ad 商品は `ProductType` を何にするか？ `Furniture` / `Outfit` ／別カテゴリ用？）

### 1.9 PlayerPrefsService

`UserPointService` / `UserItemInventoryService` が `PlayerPrefsService.Save/Load<T>` パターンで永続化。**時限ショップは更新サイクル ID をローカル時刻から都度算出するため永続化不要**（決定論性で再現可能）。

---

## 2. Requirements Feasibility Analysis

### 2.1 Requirement-to-Asset Map

| 要件 | 既存資産 | ギャップ | 種別 |
|------|---------|---------|------|
| Req 1: ショップ商品マスター CSV | `MasterDataImportService` パターン、`Resources/*.csv` 配置 | `shop_products.csv`、`ShopProduct` レコード、`MasterDataState.ShopProducts`、`ImportShopProducts()` | **Missing** |
| Req 1.4: `currency_type = reward_ad` | `CurrencyType { Yarn, RealMoney }` | `RewardAd` 値の追加（または別 enum） | **Missing (small)** |
| Req 2: 既存ショップ再構成（タブ削除） | `ShopView` / `ShopScope` / `ProductCellView` | タブ UI のシーン側削除、カテゴリ縦並びレイアウト、4 カテゴリのセル手動配置 | **Constraint (Scene 編集)** |
| Req 2.6: リワード広告カテゴリ プレースホルダー | （既存資産なし） | レイアウト枠のみ（Unity Ads 未統合） | **Missing (placeholder)** |
| Req 2.11–2.13: モック削除 | `ShopService.InitializeMockData()` | ハードコード削除＋マスタ駆動初期化 | **Constraint (refactor)** |
| Req 3: 時限カテゴリ配置 | `ProductCellView` 流用可 | 時限セル用バッジ／枠の視覚スタイル（プレファブ拡張 or 新規 View） | **Missing** |
| Req 4: 更新サイクル | （時刻抽象なし） | `IClock` 導入 or `DateTime.UtcNow` 直接利用 + サイクル算出ロジック | **Missing / Unknown** |
| Req 5: 抽選ロジック | `System.Random` あり（無シード） | サイクル ID をシードに `Random` 生成、Fisher-Yates 風の非復元抽出、不足時の復元抽出 | **Missing** |
| Req 6: タイマー UI | `CanvasGroup` パターン、`ITickable` 既存 | カウントダウン表示用 `TextMeshPro`、`ITickable` 経由の毎秒更新 | **Missing** |
| Req 7: outfit 売り切れ表示 | `IUserItemInventoryService.HasOutfit` / `OutfitChanged` 既存 | セルのバッジ／グレーアウト表示、`OutfitChanged` 購読再評価 | **Missing (UI)** |
| Req 8: 家具複数所持 | `IUserItemInventoryService.AddFurniture` 既存 | そのまま利用、追加ロジックなし | **None** |
| Req 9: 購入処理 | `CommonConfirmDialog` / `CommonMessageDialog` / `SpendYarn` 既存 | 暗め表示切替 API、購入確認中のサイクル切替検知 | **Missing** |
| Req 9.4 / 9.10: 暗め表示 | `CanvasGroup` パターンあり、`Button.interactable` あり | `ProductCellView.SetDimmed(bool)` 等の API 拡張 | **Missing (small)** |
| Req 10: 状態管理 | `ShopState` 既存（タブ / List<ProductData>） | 時限ショップ用フィールド・イベント追加、サイクル ID 保持 | **Constraint (extend)** |

### 2.2 複雑度シグナル

- **Algorithmic logic** (Medium): 決定論的シードによる非復元抽選 + 不足時の復元抽出。Fisher-Yates 風アルゴリズムで実装可能だが境界条件（マスタ件数 = 0／未満／以上）を慎重に。
- **Time-driven workflow** (Medium): カウントダウン + サイクル切替 + 切替時の再抽選 + 購入確認中の切替検知。`ITickable` で集約可能。
- **UI workflow** (Medium): 4 カテゴリ × セル表示状態（通常／暗め／売り切れ／非表示）の組合せが増える。`ProductCellView` の状態管理を整理。
- **External integration** (None / Low): Unity Ads は対象外（プレースホルダーのみ）。サーバー連携なし。

### 2.3 制約・前提

- **Scene 編集**: `Assets/Scenes/Shop.unity` でのタブ要素削除・カテゴリ縦並び再配置・時限セル配置・タイマー UI 配置が必要（コードからは制御不可）
- **後方互換**: 未リリースのため `CurrencyType` 拡張・`ProductData` 拡張で破壊的変更を許容できる（既存 spec の Non-Goals でも明記されている）
- **outfit マスタ件数**: 現在 43 件。`type` カラムは `Body / Cloth / Face / HandAccessory / HeadAccessory / LegAccessory / Tail` で細分化されている。**ショップ商品マスタの `outfit` 全件抽選とどう整合させるか**が論点（既存 outfit 全件をショップ対象とする / shop_products.csv に列挙された outfit のみを対象とする）
- **furniture マスタ件数**: 現在 4 件のみ → 抽選枠 6 件に対して**復元抽出（重複許可）が確定発動**するため、Req 5.8 の挙動が初期 UX となる
- **乱数の決定論性**: `System.Random(int seed)` はクロスプラットフォームで同一結果を保証しない場合がある（.NET 実装依存。Unity の Mono ランタイムでは検証が必要）。要件 4.3 を満たすには独自実装または明示的にプラットフォーム検証が必要

---

## 3. Implementation Approach Options

### Option A: 既存コンポーネント拡張中心（最小増加）

**Scope**:
- `ShopState` に時限ショップ用フィールドを追加（`CurrentCycleId` / `TimedFurnitureProducts` / `TimedOutfitProducts` / `OnTimedShopUpdated` / `OnSoldOutChanged`）
- `ShopService` を `ITickable` 実装に拡張し、毎秒の Tick でサイクル切替判定 → 抽選 → イベント発火
- `ProductCellView` に `SetDimmed(bool)` / `SetSoldOut(bool)` API を追加し、既存 `[SerializeField] CanvasGroup` を導入
- `MasterDataState` に `ShopProducts[]` を追加、`MasterDataImportService.ImportShopProducts()` メソッドを追加
- `CurrencyType` enum に `RewardAd` を追加。`ProductData` に `Type` カラム追加（`ShopProductType.Furniture/Outfit/RewardAd`）
- 時限ショップカテゴリは `ProductCellView` を流用、シーン側で「時限」バッジ用 `GameObject` を `[SerializeField]` で参照

**Trade-offs**:
- ✅ 新規ファイル追加が最小（CSV 1 件、enum 拡張、State/Service 拡張のみ）
- ✅ 既存命名・依存関係が維持される
- ✅ DI 登録（ShopScope）の変更不要
- ❌ `ShopService` のサイズ増（`ITickable` + 抽選 + 購入分岐 + サイクル管理）
- ❌ `ProductCellView` の責務肥大（通常／暗め／売り切れ／時限バッジ ON/OFF）

### Option B: 新規コンポーネント中心（責務分離）

**Scope**:
- `Shop/Service/TimedShopService.cs` を新規（`ITickable` 実装、抽選・サイクル管理を専任）
- `Shop/Service/IClock.cs` / `SystemClock.cs` を新規（時刻取得抽象化、テスト容易性）
- `Shop/State/TimedShopState.cs` を新規（サイクル ID、表示中商品、更新イベント）
- `Shop/View/TimedShopCellView.cs` を新規（時限バッジ、売り切れ、暗め表示）
- `Shop/View/TimedShopTimerView.cs` を新規（カウントダウン UI）
- `Root/State/MasterDataState` に `ShopProduct[]` 追加（最低限の拡張）
- `ShopService` は時限ショップ関連の購入分岐のみ担当、`TimedShopService` から商品情報を取得

**Trade-offs**:
- ✅ 単一責任原則を厳守、テスト容易性が高い
- ✅ 既存 `ShopService` / `ShopState` への影響を最小化
- ✅ 将来の拡張（複数の時限ショップ枠、サーバー連携）に対応しやすい
- ❌ 新規ファイルが 5–6 個増える
- ❌ `ShopScope` への DI 登録追加が必要
- ❌ `ShopService` と `TimedShopService` 間の責務分担を慎重に設計する必要

### Option C: ハイブリッド（推奨）

**Scope**:
- **新規追加（責務分離する単位）**:
  - `Shop/Service/TimedShopCycleCalculator.cs` (純粋関数、時刻 → サイクル ID／残り時間／決定論シード)
  - `Shop/Service/TimedShopLottery.cs` (純粋関数、シード + 母集合 + 枠数 → 抽選結果)
  - `Shop/View/TimedShopTimerView.cs` (`MonoBehaviour`、毎秒タイマー更新、`destroyCancellationToken` で停止)
  - `Root/Service/IClock.cs` + `SystemClock.cs` (将来テスト・サーバー連携用に薄く導入)
- **既存拡張**:
  - `ShopState` に時限ショップ関連フィールドとイベントを追加
  - `ShopService` を `ITickable` 化（サイクル切替検知のみ） + マスタ駆動初期化
  - `ProductCellView` に `SetDimmed(bool)` / `SetSoldOut(bool)` を追加（`CanvasGroup.alpha` ＋ バッジ `GameObject` 切替）
  - `MasterDataState` / `MasterDataImportService` に `ShopProducts` を追加（既存パターン踏襲）
  - `CurrencyType` 拡張、`ProductData` に `Type` カラム追加
- **時限セルは `ProductCellView` を流用** + シーン側で「時限」バッジ `GameObject` を `[SerializeField]` で持つ（独立 View は作らない）

**Trade-offs**:
- ✅ 純粋関数（Calculator/Lottery）は単体テスト可能
- ✅ View / State / Service の既存責務分離を維持
- ✅ ファイル増加は 4 件程度に抑制
- ✅ `IClock` を導入することで時刻依存をテスト可能化（後の時限機能拡張時に有利）
- ❌ Calculator / Lottery の純粋関数化と Service の責務分担で設計判断が増える

---

## 4. Effort & Risk

| 項目 | ラベル | 一行根拠 |
|------|-------|----------|
| **Effort（AI 担当のコード実装のみ）** | **S–M (2–5 日)** | 既存パターン（マスタ／DI／ダイアログ）に乗るため定型的。抽選アルゴリズム・サイクル制御・暗め表示／売り切れ API・タイマー View 実装が主タスク。Scene 編集はスコープ外（D15）のため AI 側工数からは除外 |
| **Risk** | **Low–Medium** | 決定論的乱数のクロスプラットフォーム再現性が要検証。アーキテクチャ的シフトはなく、既存資産（IUserPointService / IUserItemInventoryService / MasterData）が揃っているため統合リスクは低い。ユーザー側の Unity 作業との同期・割り当てズレがあれば手戻りの可能性 |

---

## 5. Decisions Made (要件レビューで確定)

ユーザーレビューで以下が確定。設計フェーズはこれらを前提に進める。

| # | トピック | 決定内容 |
|---|---------|----------|
| D1 | アプローチ | **Option C（ハイブリッド）** を採用 |
| D2 | `IClock` 導入 | **導入する**。`Root.Service.IClock` インターフェース ＋ `SystemClock` 実装を新規追加。`RootScope` に `Lifetime.Singleton` で登録 |
| D3 | 乱数 | **`System.Random(int seed)`** を採用。新規ライブラリは導入しない。設計フェーズで Mono/IL2CPP のシード再現性を確認テストとして追加（既知の通り `System.Random` は .NET ランタイム間で同一のサブトラクティブ・ジェネレータ実装を採用しており再現性が期待できるが、Unity ターゲット環境で念のため確認） |
| D4 | `shop_products.csv` のカラム順序 | 設計フェーズで決定（カラム集合は要件 1 で確定） |
| D5 | `item_id` の NOT NULL | NOT NULL（必ず付与対象あり） |
| D6 | `currency_type` の値域 | `yarn` / `reward_ad` の 2 値。広告視聴商品は `currency_type = reward_ad` で識別する |
| D7 | カラム命名: `type` → `item_type` | `type` カラムは命名があいまいなため廃止し、`item_type` 単一の軸（`furniture` / `outfit` / `point` 等の付与アイテム種別）にまとめる。広告かどうかは `currency_type` で判別 |
| D8 | `is_timed` カラム | **設けない**。`item_type = furniture` / `item_type = outfit` の全行を時限ショップの抽選母集合とする |
| D9 | `ProductData` 構造 | **既存拡張を基本路線**としつつ、設計フェーズで「Root マスタを表す `ShopProduct` レコード」と「Shop 表示用 `ProductData`」の 2 層に分割する案を推奨ベースに具体化 |
| D10 | 時限セルの視覚区別 | **時限性を示すバッジ・装飾・枠を表示しない**（通常セルと同一の見た目）。時限性はカテゴリ見出し＋更新タイマー UI のみで示す |
| D11 | 売り切れ表示の実装方式 | **専用オーバーレイ `GameObject`** を `SetActive(bool)` で切替。半透明黒画像 ＋「売り切れ」ラベルを含む（`CanvasGroup` でも代替可） |
| D12 | タイマー UI の実装 | 最小構成（`MonoBehaviour` ＋ `TextMeshProUGUI` ＋ `Update()` で残り時間を再計算）で実装。**ショップ画面に 1 つだけ存在**し、各セルや各カテゴリごとには持たない |
| D13 | `CurrencyType` enum 拡張 | 既存 `Shop.State.CurrencyType = { Yarn, RealMoney }` に **`RewardAd` 値を追加**する形で拡張する |
| D14 | 時限ショップカテゴリ見出しラベル | **設けない**。時限性はタイマー UI のみで示し、カテゴリ見出しテキストや「時限ショップ」というラベル UI は配置しない |
| D15 | 作業分担（AI / ユーザー） | **AI（Claude）は C# コード ＋ CSV のみを実装担当**。Unity Editor 上の作業（シーン編集、プレファブ作成、`SerializeField` 割当、Addressables 設定、ビジュアル調整）は**ユーザー側で実施**。AI はシーン／プレファブの**読み取り**のみ可（構造理解のため） |

## 6. Research Needed (設計フェーズに引き継ぎ)

設計フェーズ着手後に判断するか、設計内で具体化するもの。

| # | トピック | 内容 |
|---|---------|------|
| R1 | サイクル境界処理 | 「`UniTask.Delay(残り時間)` で次サイクルを待機する」方式と「`ITickable` / View の毎秒ポーリング」方式の比較。電源スリープ復帰時の挙動。タイマー View の `Update` で十分か、`ShopService` 側でも別途 Tick が必要か |
| R2 | サイクル切替中の購入確認ダイアログ処理 | Req 9.7 の「ダイアログを開いている間に切替が起きた場合」の検知方法。`CancellationToken` での即時キャンセル vs 完了後に再評価 |
| R3 | サイクル切替アニメーション | サイクル切替時に商品が突然差し替わる UX の妥当性（フェードや「更新中」表示は本フェーズでは不要、と暫定） |
| R4 | リワード広告セルの扱い | 要件 2.6 で「レイアウトのみ確保」。空 `GameObject` で良いか／プレースホルダーセル View が必要か（設計フェーズで簡易な空 `GameObject` を推奨予定） |
| R5 | `ProductCellView` の暗め表示の数値 | 要件 9.4 の「半透明・グレーアウト」の具体値（`CanvasGroup.alpha` 0.4–0.5 程度の目安）と、ボタン以外の Image / Text への影響範囲 |
| R6 | `System.Random` シードの安全な算出 | サイクル開始時刻の Unix エポック秒（`long`）を `int` シードに畳み込む方式（XOR 折り畳み等）の確定 |

---

## 7. Recommendations for Design Phase

### 7.1 推奨アプローチ（確定）

**Option C（ハイブリッド）** で進める。設計フェーズでは以下を具体化する。

- **新規追加ファイル（4–5 件想定）**:
  - `Assets/Scripts/Root/Service/IClock.cs`
  - `Assets/Scripts/Root/Service/SystemClock.cs`
  - `Assets/Scripts/Shop/Service/TimedShopCycleCalculator.cs` (純粋関数: 時刻 → サイクル ID／残り時間／シード)
  - `Assets/Scripts/Shop/Service/TimedShopLottery.cs` (純粋関数: シード + 母集合 + 枠数 → 抽選結果)
  - `Assets/Scripts/Shop/View/TimedShopTimerView.cs` (`MonoBehaviour` ＋ `TextMeshProUGUI` ＋ `Update()`)
  - （任意）`Assets/Scripts/Root/State/ShopProduct.cs`（マスタを表すレコード）
- **新規追加 CSV**:
  - `Assets/Resources/shop_products.csv`
- **既存拡張**:
  - `MasterDataState`: `ShopProducts[]` プロパティ追加
  - `MasterDataImportService`: `ImportShopProducts()` 追加
  - `ShopState`: 時限ショップ用フィールド・イベント追加（サイクル ID、表示中商品リスト、`OnTimedShopUpdated`、家具／衣装の表示中リスト）
  - `ShopService`: `ITickable` 化（毎秒ポーリングでサイクル切替検知）、マスタ駆動初期化、抽選呼び出し、購入分岐
  - `ProductCellView`: 暗め表示 API（`SetDimmed(bool)`）と売り切れオーバーレイ参照（`[SerializeField] GameObject _soldOutOverlay`）を追加
  - `ShopView`: タブ呼び出しオミット、カテゴリ縦並び、タイマー連携、暗め／売り切れ再評価
  - 既存 `ShopService.InitializeMockData()` ／ `ShowYarnInsufficientAsync` 等は削除

### 7.2 設計時の注意点

- 時限ショップの**専用プレファブは原則作らない**（D10 / D14 によりバッジもカテゴリ見出しも非表示）。家具用 / 衣装用の通常 `ProductCellView` プレファブをそのまま流用する
- `MasterDataImportService.Import()` の `Imported` イベントは現状 1 回だけ発火する仕様。**ショップ商品マスタ追加で既存購読側（`UserItemInventoryService`）への影響なし**
- `ShopService.OnProductCellTappedAsync` の毛糸不足ダイアログ呼び出し（`ShowYarnInsufficientAsync`）は要件 9.5 / 9.10 で撤去対象。本実装に伴って既存の `ShowYarnInsufficientAsync` メソッド自体が不要
- `ShopService.OnGachaTappedAsync` は UI 経由で呼び出されないため改修不要（要件 2.9）。コード残置
- リワード広告カテゴリは「レイアウトのみ確保」のためセル配置は不要、`GameObject` でカテゴリ枠だけ用意する形がシンプル
- `Root.Service.IClock` は本機能で唯一のクライアントになる。設計時に「`UtcNow()` のみ」の最小 API で開始し、必要に応じて `Now()`（ローカル時刻）を追加する形を推奨

### 7.3 作業分担に関する設計上の留意点（D15）

- 各 View クラスの `[SerializeField]` フィールドは**コード側で宣言まで実施**し、Inspector 上の参照割り当てはユーザー側で行う前提で設計する
- 必要なシリアライズ対象（売り切れオーバーレイ `GameObject`、暗め表示用 `CanvasGroup`、タイマー `TextMeshProUGUI`、各カテゴリのセルリスト等）は**設計ドキュメントで明示一覧化**し、ユーザーが Unity Editor 上で何を割り当てるかを迷わない構成にする
- プレファブ・シーンに既存の何を流用し、何を新規追加するかを設計に明記（例: `ProductCellView` プレファブに「売り切れオーバーレイ `GameObject`」を子要素として追加）
- AI 側がシーン構造理解のため `Shop.unity` / プレファブを Read することは認可されている。Edit/Write は不可
