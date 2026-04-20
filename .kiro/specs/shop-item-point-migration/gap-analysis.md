# Gap Analysis — shop-item-point-migration

## 1. Current State Investigation

### 1.1 Domain Assets Landscape

**Shop scene (移行元)**
- `Assets/Scripts/Shop/State/ShopState.cs`
  - `YarnBalance` (モック初期値 10000)、`ConsumeYarn` / `AddYarn`、`OnYarnBalanceChanged` を保持
  - 他に `CurrentTab`, `OnTabChanged`, `GachaList`, `ItemProductList`, `PointProductList` を保持 (今回の移行対象外)
  - `GachaData.RewardFurnitureIds` は `List<string>` (例: `"chair_01"`, `"table_01"` など)
- `Assets/Scripts/Shop/Service/ShopService.cs`
  - `InitializeMockData()` でガチャ/商品のハードコードデータを投入
  - 購入フロー (`OnProductCellTappedAsync`, `OnGachaTappedAsync`) で `_state.YarnBalance` を参照し `_state.ConsumeYarn` / `_state.AddYarn` を呼ぶ
  - `SetupProductCell` / `SetupGachaCell` で `_state.YarnBalance` ベースに `interactable` を計算
  - `ExecuteGacha` はローカル `System.Random` で抽選するが、結果は文字列メッセージ表示のみ (インベントリ反映なし)
- `Assets/Scripts/Shop/Scope/ShopScope.cs`
  - `ShopState` / `ShopService` を `Lifetime.Scoped` で登録、`ShopView` を `RegisterComponent`
  - RootScope のサービスを再登録していない (親スコープから解決)
- `Assets/Scripts/Shop/View/ShopView.cs`
  - `[Inject] Construct(ShopState, ShopService)` で `ShopState` に直接依存
  - `_state.OnYarnBalanceChanged`・`_state.YarnBalance` を購読/参照
  - `UpdateAllCellsInteractable()` で全セル再セットアップ
- `Assets/Scripts/Shop/View/ProductCellView.cs`
  - `SetInteractable(bool)`, `Setup(ProductData)` を公開
- `Assets/Scripts/Shop/View/GachaCellView.cs`
  - `SetButtonsInteractable(bool, bool)` (単発/10連別々)

**Root services (移行先)**
- `Assets/Scripts/Root/Service/IUserPointService.cs` / `UserPointService.cs`
  - `GetYarnBalance()`, `AddYarn(int)`, `SpendYarn(int)`, `YarnBalanceChanged`, `InitializeAsync`, `SaveAsync`
  - `PointOperationResult` ({`IsSuccess`, `Error`, `Balance`}) / `PointOperationErrorCode.{InvalidArgument, Insufficient, Overflow}`
  - コンストラクタで `PlayerPrefsService.Load<UserPointSnapshot>(PlayerPrefsKey.UserPoint)` により復元、未保存/不正時は 0
  - 書き込み操作時に `Save()` 自動発火 (`PlayerPrefsService.Save`)
- `Assets/Scripts/Root/Service/IUserItemInventoryService.cs` / `UserItemInventoryService.cs`
  - `AddFurniture(uint id, int amount)` → `ItemInventoryResult` (`UnknownId`, `InvalidArgument`)
  - `MasterDataState.Furnitures` に対する ID 検証 (`IsKnownFurnitureId`)
  - MasterData import 前に呼ぶと `UnknownId` 扱い → `MasterDataImportService.Imported` 後に Load
- `Assets/Scripts/Root/Scope/RootScope.cs`
  - `IUserPointService` / `IUserItemInventoryService` を `Lifetime.Singleton` で `As<IFace>().AsSelf()` 登録済み
  - → **ShopScope での再登録は不要**

**Master data**
- `Assets/Scripts/Root/State/MasterDataState.cs` の `Furniture` は `uint Id`, `string Type`, `string Name`
- 現状のガチャ報酬ID (`"chair_01"` など) と **型不一致**。マスターデータに該当 ID/Name は現時点で存在しない (`chair_01` のヒットなし)

### 1.2 Conventions / Dependency Direction

- Namespace: `Shop.Service`, `Shop.State`, `Shop.View`, `Shop.Scope`, `Shop.Starter`
- 依存方向: `View → Service → State` (逆向き禁止)
- `[Inject]` 属性をコンストラクタ/Construct に付与 (IL2CPP stripping 対策)
- `_camelCase` private fields、`readonly` 初期化、`#nullable enable`
- エラーログは `Debug.LogError($"[ClassName] {e.Message}\n{e.StackTrace}");`
- `SceneScope` 継承、`Lifetime.Scoped`

### 1.3 Integration Surfaces

- **PlayerPrefs 永続化**: `UserPointService` は書き込み時に `PlayerPrefsKey.UserPoint` へ自動保存。ShopState の mock 初期値 10000 は移行後に失効 → 実残高は 0 から始まる (もしくは PlayerPrefs 復元値)
- **Master data import**: `UserItemInventoryService` は `MasterDataState.IsImported` を待機。`SceneScope` が `Awake` 時に MasterDataImport を保証しているため、`ShopStarter.Start` 時点では import 完了済み
- **`UserState`**: `UserState.UserOutfits` / `UserState.UserFurnitures` などの旧モックは `user-inventory-management` 仕様の制約で今回は触らない (既存参照は別スコープで別途移行)

## 2. Requirement-to-Asset Map

| Req | 必要能力 | 現状アセット | ギャップ種別 |
|----|---------|------------|-------------|
| R1: 毛糸残高参照の移行 | `IUserPointService.GetYarnBalance()` | RootScope に登録済 | **Constraint** (参照差し替えのみ) |
| R2: 毛糸消費処理の移行 | `SpendYarn` + `Insufficient`/`InvalidArgument` ハンドリング | Service 実装済 | **Missing wiring** (ShopService から結果判定が必要) |
| R3: 毛糸加算処理 (YarnPack) の移行 | `AddYarn` + `Overflow` ハンドリング | Service 実装済 | **Missing wiring** |
| R4: `YarnBalanceChanged` 購読替え | `IUserPointService.YarnBalanceChanged` | Event 実装済 | **Missing wiring** + ShopView `[Inject]` 追加 |
| R5: 商品セル `interactable` 判定移行 | 残高参照を UserPointService 経由 | — | **Constraint** |
| R6: 確認ダイアログ前の残高検証 | 事前 `GetYarnBalance` チェック | — | **Constraint** |
| R7: ガチャ家具獲得の InventoryService 連携 | `AddFurniture(uint, int)` | Service 実装済だが **ID 型不一致** (`string` vs `uint`) + マスター未整備 | **Missing (data)** / **Unknown** |
| R8: `ShopState` からポイント/インベントリ責務除去 | — | `ShopState.YarnBalance` 等が残存 | **Missing (cleanup)** |
| R9: DI と依存方向の整合 | RootScope からの解決 | RootScope 側は登録済、ShopScope は再登録不要 | **Constraint** |
| R10: 動作互換性 | ダイアログ文言・多重実行ガード等 | 既存実装あり | **Constraint** (既存維持) |

### Notable Gaps
- **G1 (Missing)**: ガチャ報酬家具 ID が `string` なのに `AddFurniture` は `uint`。さらにマスターに `chair_01` 等は未登録。**Research Needed**: マスターに家具 ID をどう格納するか (まずは暫定 ID でモック的に登録するか、今回は家具付与だけ行わず「ガチャ結果メッセージ表示」まで維持するか) を設計フェーズで判断。
- **G2 (Missing data)**: PlayerPrefs 未保存時の毛糸残高は 0。旧モック初期値 10000 が失われる。**Research Needed**: 開発用に初回起動時のシード (例: `AddYarn(10000)` を `UserDataImportService` か ShopStarter で流し込む) を入れるか、0 スタートに倒すかの方針。
- **G3 (Constraint)**: `ShopState` のタブ状態・商品リストは `timed-shop` 仕様で再構成されるが、本仕様ではそれらを触らず、毛糸/インベントリ責務だけを抽出する。

## 3. Implementation Approach Options

### Option A: ShopService/ShopView/ShopState を直接書き換え (Extend)

**内容**
- `ShopService` のコンストラクタに `IUserPointService`, `IUserItemInventoryService` を追加
- `_state.YarnBalance` → `_userPointService.GetYarnBalance()`
- `_state.ConsumeYarn` → `_userPointService.SpendYarn` + 結果判定
- `_state.AddYarn` → `_userPointService.AddYarn` + 結果判定
- `ShopView.Construct` に `IUserPointService` を追加し、`YarnBalanceChanged` を購読
- `ShopState` から `YarnBalance`/`ConsumeYarn`/`AddYarn`/`OnYarnBalanceChanged` を削除
- `GachaData.RewardFurnitureIds` は設計方針に合わせ `uint[]` に変更、もしくは文字列→uint マッピング層を設計フェーズで決定

**トレードオフ**
- ✅ 新規ファイルほぼなし、最小差分で移行完了
- ✅ 既存テスト・シーン参照 (`ShopScope` の SerializedField 等) に影響なし
- ❌ `ShopState` の縮退が破壊的変更 (呼び出し元が全てショップ内なので影響は閉じる)
- ❌ ガチャ家具付与を同時にやる場合、ID 型変換の判断が必要

### Option B: ShopCurrencyGateway / ShopInventoryGateway を新設 (New Components)

**内容**
- `Shop.Service` 配下に `ShopCurrencyGateway`, `ShopInventoryGateway` を作り、`IUserPointService` / `IUserItemInventoryService` を薄くラップ
- `ShopService` はゲートウェイ経由でのみ残高/インベントリを扱う
- UI 互換エラーメッセージ (例: `Insufficient` → 「毛糸が足りません」) を Gateway 層に集約

**トレードオフ**
- ✅ Shop ドメインの境界が明示され、将来「時限ショップ特殊ルール」を差し込みやすい
- ✅ メッセージ変換・ログの単一責任
- ❌ 新規ファイル 2 つ、依存関係が一段増える
- ❌ 現状のショップ規模では過剰設計になりがち

### Option C: Hybrid — ShopService は直接参照、ただし `PurchaseResult` ヘルパーを切り出す

**内容**
- `IUserPointService` / `IUserItemInventoryService` は `ShopService` から直接参照 (Option A)
- 購入成功時のユーザー通知文言・エラーコード→メッセージ変換は `Shop.Service.PurchaseMessageMapper` 等の純粋関数ユーティリティへ切り出す
- ガチャ家具 ID 変換ロジック (string→uint) もユーティリティ化し、マスター整備が済むまで `UnknownId` に対する安全な縮退動作を提供

**トレードオフ**
- ✅ 小回り: 本仕様で必要な境界のみ切り出し、将来拡張余地も確保
- ✅ `ShopService` の肥大化回避
- ❌ ファイル分割の粒度判断が必要
- ❌ テスト観点が増える (ユーティリティと Service の両方)

## 4. Out-of-Scope / Research Needed

- **RN1**: ガチャ家具 ID 体系の整備 (`RewardFurnitureIds` を `uint[]` 化し、マスターに該当家具を登録するか、暫定 ID で投入するか) → 設計フェーズで決定
- **RN2**: 毛糸初期残高 (0 スタート vs シード投入) の方針 → 設計フェーズで決定
- **RN3**: `UserState` に残存する旧モック (`UserFurnitures` 等) は `user-inventory-management` 仕様スコープ外として触らない (確認済)
- **RN4**: `ShopState.InitializeMockData` の商品リストは、マスターデータ化される `timed-shop` 仕様が吸収するため、本仕様では移動しない

## 5. Effort & Risk

- **Effort**: **S (1–3 days)**
  - 対象は `Shop` 配下の 4 ファイル + `ShopScope` の DI 設定 1 箇所。既存パターン (RootScope 解決・`[Inject]` 注入) に沿うだけで、新規アーキテクチャ導入は不要。
- **Risk**: **Medium**
  - `ShopState.YarnBalance` を破壊的に削除するため、呼び出し抜けがあるとコンパイルエラーで検知できるが、ガチャ家具 ID 型変更 (`string` → `uint`) は実行時まで気付きにくい。さらに PlayerPrefs 復元が有効化される副作用 (初回/再起動時の残高挙動変化) に注意。

## 6. Recommendations for Design Phase

### Preferred Approach
**Option A (Extend) を基本線**とし、Option C の小ユーティリティ (メッセージマッパー・ID 変換) をピンポイントで併用する。Option B は現時点の規模に対し過剰。

### Key Design Decisions to Resolve
1. **ガチャ家具 ID 体系** (RN1):
   - 案 α: `GachaData.RewardFurnitureIds` を `uint[]` に変更し、マスターに暫定家具 (`Id = 1..5`) を登録
   - 案 β: 今回は家具付与を実装せず、`AddFurniture` 呼び出しは `timed-shop` 仕様までペンディング (要件 R7 を設計フェーズで縮退)
   - 案 γ: 既存文字列 ID を保持し、`ShopService` 内で `string → uint` マップを持たせる (暫定)
2. **初期毛糸残高** (RN2):
   - 案 α: PlayerPrefs 復元値 (未保存は 0) をそのまま受け入れ、テスト用に Editor メニューから `AddYarn` できるデバッグツール追加
   - 案 β: `ShopStarter.Start` で残高 0 を検知したら開発用に `AddYarn(10000)` を 1 回だけ注入
3. **エラーメッセージ集約**:
   - `PointOperationErrorCode` → 表示メッセージの変換を `ShopService` に置くか別ユーティリティに切り出すかの境界線
4. **`ShopView` の `[Inject] Construct` 引数追加**:
   - 既存シグネチャ `Construct(ShopState, ShopService)` に `IUserPointService` を追加するか、`ShopService` 経由で残高を公開するかの選択 (View → Service 方向の徹底を推奨)

### Research Items to Carry Forward
- マスターデータへの家具 ID 登録状況の確認 (CSV ソース位置・追加手順)
- PlayerPrefs リセット手順 (テスト再現性確保)
- `IUserItemInventoryService.AddFurniture` が `MasterDataImportService.Imported` を前提としている点のテストケース整理
