# Research & Design Decisions — timed-shop

---
**Purpose**: 設計フェーズで採用したアーキテクチャ判断・外部ライブラリ調査・代替案評価を記録する。`design.md` の根拠資料として参照され、深掘りが必要な調査ノートはここに残す。
---

## Summary
- **Feature**: `timed-shop`
- **Discovery Scope**: Extension（既存 Shop シーンの再構成 + 時限ショップ追加）
- **Key Findings**:
  - 既存パターン（`MasterDataImportService` / `RootScope` / `IUserPointService` / `IUserItemInventoryService` / `CommonConfirmDialog`）が揃っており、新規導入が必要な抽象は `IClock` のみ。
  - 決定論的抽選は `System.Random(int seed)` で実装可能。サイクル ID（`long` Unix エポック秒 / `Interval` 量子化）を XOR 折り畳みで `int` シードに変換する純粋関数として切り出す。
  - サイクル切替検知は `VContainer.Unity.ITickable` を `ShopService` に実装することで集約でき、独自 Update Loop や別 `MonoBehaviour` を増やさずに済む（タイマー View だけは表示更新の責務上 `MonoBehaviour.Update` を使う）。
  - 時限ショップカテゴリのバッジ・カテゴリ見出しは要件 D10 / D14 で禁止のため、専用 View プレファブは増やさず `ProductCellView` を流用する。
  - `Random` の決定論性: .NET の `System.Random(int)` は同じシードで同じ系列を返すが、.NET ランタイム実装間で系列が必ずしも一致するわけではないことが知られている。本機能は同一ビルド・同一 Mono/IL2CPP ランタイム間での再現が要件であり、ターゲットプラットフォーム間（iOS / Android / Editor）は同一の `System.Random` 実装が用いられるため、要件 4.3「全端末で一致」は満たせると判断。

## Research Log

### `System.Random(int seed)` の決定論性
- **Context**: 要件 4.3「同一サイクル期間内ではどの端末・どのプレイヤーでも同一内容」を満たすため、抽選結果が再現可能である必要がある。
- **Sources Consulted**: gap-analysis セクション 1.6 / 2.3、Microsoft Docs `Random` クラスの "Replicating Random number sequences" 説明、`System.Random` のサブトラクティブ・ジェネレータ実装。
- **Findings**:
  - `new System.Random(int seed)` は同一プロセス内で同じシードからは必ず同じ系列を返す（Knuth サブトラクティブ法）。
  - Unity 6 の Mono ランタイム / IL2CPP ともに .NET Standard 2.1 の `System.Random` 実装に依拠するため、配信ビルド間で同一系列が得られる。
  - .NET 6 以降の新実装（`System.Random.Shared` / `XoshiroImpl`）はシードなしバージョンであり、シード付きコンストラクタは依然として旧 Knuth 実装を維持している。
- **Implications**:
  - 設計上は `new System.Random(seed)` の戻り値を直接利用してよい（独自 PRNG の導入は不要）。
  - 仕様凍結検証として、決定論性のスモークユニットテスト（同一シードで複数回呼び出し、出力配列が一致する）を実装フェーズで追加する。

### サイクル更新の駆動方式（Tickable vs UniTask.Delay）
- **Context**: 要件 4.4「現在時刻が次回更新時刻に到達したら即座に再抽選」の実装手段を選定する。
- **Sources Consulted**: `Home/Service/IsoInputService.cs`, `Root/Service/DialogContainer.cs`（既存 `ITickable` 実装）。
- **Findings**:
  - `VContainer.Unity.ITickable.Tick()` は毎フレーム呼ばれる軽量フックで、サービス層のオーケストレーションに既存実例がある。
  - `UniTask.Delay(remaining)` 方式は端末スリープから復帰した際に時刻ジャンプの取り扱いが必要で、復帰直後にサイクル境界を跨いでいるかの判定が漏れやすい。
  - `Tick()` での「現在時刻が `NextUpdateAt` を超えたら更新」シンプル比較ならスリープ復帰時も自然に再評価される。
- **Implications**:
  - サイクル切替検知は `ShopService : ITickable` で実装する。
  - タイマー UI の毎秒表示更新は `MonoBehaviour.Update()` 内で `IClock` から残り時間を取り直す形で十分（要件 6.7 の最小構成）。

### 外部ライブラリ確認
- **Context**: 新規ライブラリ追加が要件達成に必要かを判定する。
- **Sources Consulted**: `Packages/manifest.json`, `tech.md`。
- **Findings**:
  - DOTween / UniTask / VContainer / Addressables / TextMeshPro / NewInputSystem は導入済み。
  - 時限ショップ機能は CSV ロード・乱数抽選・UI 更新・通貨/インベントリ操作のみで、新規外部依存は不要。
- **Implications**:
  - パッケージ追加なし。`Packages/manifest.json` 変更は本機能のスコープ外。

### 既存 `ShopState` / `ProductData` 拡張の互換性
- **Context**: 既存 `ProductData` に `item_type` 軸を加えるか、別レコードに分離するかを判断する。
- **Sources Consulted**: `Shop/State/ShopState.cs`, gap-analysis セクション 1.8。
- **Findings**:
  - 既存 `ProductData` は表示用の派生情報（`IconPath`, `YarnAmount`）を含み、マスタ行と直接対応していない。
  - マスタ行を表す型（`ShopProduct`）と表示用派生型（`ProductData` 拡張）に分けると、マスタ層と表示層の責務が崩れない。
- **Implications**:
  - `Root/State/ShopProduct.cs` を新設し、CSV から直接マッピング。
  - `Shop.State.ProductData` には `ItemType`（付与アイテム種別）と任意の `ProductId`（マスタ ID）を追加する。`YarnAmount` は `point` 商品用に残置するが、本フェーズでは `point` 商品の購入処理は対象外。

### `IClock` 抽象の最小 API
- **Context**: 要件 4.2「Unix エポックからの経過時間」の取得を抽象化する。
- **Sources Consulted**: `gap-analysis` セクション 1.7、ステアリング `tech.md`（Root サービスの Singleton 規約）。
- **Findings**:
  - 現状 `DateTime.UtcNow` を直接呼ぶサービスは存在せず、ここで初導入となる。
  - 最初のクライアントは時限ショップだけだが、将来 Daily Reset / Cron / 任意の期限イベントで再利用できる広い抽象。
- **Implications**:
  - 最小 API は `DateTimeOffset UtcNow { get; }` 1 メンバーのみ。
  - `RootScope` に `Lifetime.Singleton` で `IClock` / `SystemClock` を登録する。

## Architecture Pattern Evaluation

| Option | Description | Strengths | Risks / Limitations | Notes |
|--------|-------------|-----------|---------------------|-------|
| A. `ShopService` 単独拡張 | 既存 `ShopService` にサイクル管理・抽選・購入分岐をすべて追加 | ファイル増加最小、DI 変更不要 | `ShopService` の責務が肥大、抽選アルゴリズムの単体テスト不可 | gap-analysis Option A |
| B. 新規 `TimedShopService` 分離 | `TimedShopService` / `TimedShopState` / `TimedShopCellView` を新設 | 責務分離が明快、テスト容易 | 新規ファイル 5–6 件、`ShopScope` 修正、`ShopService` との責務境界の合意が必要 | gap-analysis Option B |
| C. ハイブリッド（純粋関数を分離） | `TimedShopCycleCalculator` / `TimedShopLottery` を純粋関数として切り出し、`ShopService` を `ITickable` 化 | テストしやすい純粋関数 + 既存責務を維持 | 純粋関数の I/O 設計に判断が必要 | **採用** (gap-analysis Option C / 要件レビューで D1 確定) |

## Design Decisions

### Decision: 時限ショップ機能はハイブリッド（Option C）で構築する
- **Context**: 抽選ロジックの再利用性・テスト容易性と、既存 Shop 構造への侵入度の最小化を両立する必要がある。
- **Alternatives Considered**:
  1. `ShopService` 単独拡張 — ファイル増加は最小だが責務肥大とテスト困難。
  2. `TimedShopService` 分離 — 責務分離は最もきれいだが、`ShopService` との購入フロー統合に再合意が必要。
- **Selected Approach**: 純粋関数（`TimedShopCycleCalculator` / `TimedShopLottery`）を切り出し、`ShopService` は `ITickable` を実装してサイクル切替を検知し、`ShopState` の時限ショップ用フィールドを更新する。View / 購入フローは既存 `ProductCellView` / `OnProductCellTappedAsync` を流用する。
- **Rationale**: 再現性の要であるサイクル算出と抽選を純粋関数化することで実装フェーズで単体テスト可能とし、副作用層（State 更新・ダイアログ・サービス呼び出し）は既存 `ShopService` に集約できるため、依存方向（View → Service → State）も維持される。
- **Trade-offs**:
  - 利点: テスト容易、依存方向を保つ、既存 DI（`ShopScope`）を最小変更で済ませられる。
  - 欠点: 純粋関数 / Service / State の I/O 境界を慎重に設計する必要がある。
- **Follow-up**: 実装フェーズで `TimedShopCycleCalculator` / `TimedShopLottery` の単体テストを追加する。

### Decision: 時刻取得抽象 `IClock` を Root に新設する
- **Context**: 要件 4.2「Unix エポックからの経過時間で量子化」を、テスト容易性とプラットフォーム依存性の観点で抽象化する。
- **Alternatives Considered**:
  1. `DateTime.UtcNow` を `ShopService` 内で直接呼ぶ。
  2. `IClock` インターフェースを Root に導入し、`SystemClock` で `DateTimeOffset.UtcNow` を返す。
- **Selected Approach**: `Root.Service.IClock` を導入する（要件レビューで D2 確定）。`SystemClock` は `DateTimeOffset.UtcNow` を返すだけの薄い実装。
- **Rationale**: テストでサイクル境界を任意時刻に固定できる。Daily Reset 等の将来用途でも再利用可能。
- **Trade-offs**: 利点はテスト性と将来拡張性。欠点はファイル 2 件の追加。
- **Follow-up**: 実装フェーズで `RootScope` に `Lifetime.Singleton` 登録を追加する。

### Decision: マスタ行と表示行を 2 層に分割する
- **Context**: `ProductData` は表示派生情報を持ち、マスタ行を直接表現していない（要件 R3）。
- **Alternatives Considered**:
  1. `ProductData` に CSV 由来カラムをすべて追加する。
  2. マスタ行 `ShopProduct`（Root.State）と表示行 `ProductData`（Shop.State）に分割する。
- **Selected Approach**: `Root/State/ShopProduct.cs` を新設し、CSV から直接マッピング。`ShopService` がマスタ行と既存 `Furniture` / `Outfit` マスタを結合して `ProductData` を生成する。
- **Rationale**: マスタ層と表示層の責務分離、`MasterDataImportService` の既存パターン踏襲。
- **Trade-offs**: 利点は責務分離。欠点はマッピング処理の追加。
- **Follow-up**: 設計の Components 節でマッピング責務を `ShopService` に明記する。

### Decision: サイクル切替検知は `ITickable` で集約する
- **Context**: 要件 4.4 / 6.5（更新時の即時再抽選）と要件 9.7（ダイアログ表示中のサイクル切替検知）の両方を 1 箇所で扱う必要がある。
- **Alternatives Considered**:
  1. `UniTask.Delay(残り時間)` で次サイクルを待機。
  2. `ShopService : ITickable` でフレーム比較。
  3. `MonoBehaviour.Update()` で View が時刻判定。
- **Selected Approach**: `ShopService` を `ITickable` 実装に拡張し、`Tick()` 内で `IClock.UtcNow` と `_state.NextUpdateAt` を比較。境界越えで `RecomputeTimedShop()` を呼び `OnTimedShopUpdated` イベントを発火する。
- **Rationale**: 端末スリープ復帰時の時刻ジャンプも自然に検知できる。View 側は ITickable 経由でのイベント受信に専念できる（依存方向維持）。
- **Trade-offs**: 利点は単純で堅牢。欠点は毎フレーム軽量比較が走る（`IClock.UtcNow` 1 回 + 比較 1 回程度なので無視できる）。
- **Follow-up**: 購入確認ダイアログ表示中のサイクル切替を検知するため、`ShopService` でサイクル切替前のサイクル ID を保持し購入確定時に比較する（要件 9.7 ロジック）。

### Decision: 時限セルは `ProductCellView` を流用する
- **Context**: 要件 D10 / D14 / 3.4 によりバッジ・カテゴリ見出しを表示しない。
- **Alternatives Considered**:
  1. `TimedShopCellView` を新設。
  2. 既存 `ProductCellView` を流用し、シーン側で時限カテゴリ枠の `[SerializeField] List<ProductCellView>` を保持する。
- **Selected Approach**: `ProductCellView` を流用し、`ShopView` に時限セル用の `_timedFurnitureCells` / `_timedOutfitCells` を追加。売り切れ表示・暗め表示の API は `ProductCellView` に追加する。
- **Rationale**: 視覚区別がない以上、専用 View を作らないほうが責務が単純。
- **Trade-offs**: 利点はファイル削減。欠点は `ProductCellView` の API 増（`SetDimmed` / `SetSoldOut`）。
- **Follow-up**: 売り切れオーバーレイ `GameObject` を `[SerializeField]` で受ける形に拡張する。

### Decision: 売り切れオーバーレイは `GameObject.SetActive` 切替
- **Context**: 要件 7.2 / 7.3「専用オーバーレイ `GameObject` を `SetActive(true)` に切り替える」に従う。
- **Selected Approach**: `ProductCellView._soldOutOverlay : GameObject?` を `[SerializeField]` で受け、`SetSoldOut(bool)` で `SetActive` を切り替える。下層の Image / Text は通常表示を維持できるため、要件 7.9 の「半透明で閲覧可能」を満たす。
- **Rationale**: シーン側で UI 構造を組み立てる前提（D15）と整合。
- **Trade-offs**: ユーザー側でプレファブにオーバーレイ `GameObject` を追加する作業が発生する。設計ドキュメントで明示する。

## Risks & Mitigations
- **Risk**: クロスプラットフォーム間で `System.Random` 系列が異なる可能性 — Mitigation: スモークテストで Editor / Mono / IL2CPP ビルドの結果を比較。差異が確認された場合のみ独自 PRNG（XorShift32 等）に差し替える。
- **Risk**: 端末時間がユーザー操作で書き換えられた場合の更新サイクル不整合 — Mitigation: `IClock.UtcNow` をそのまま使い、サーバー認証は本フェーズ非対象（仕様上 Non-Goal）。`Tick()` ベースの単純比較ならば「時刻が戻った」場合にも自然に挙動する。
- **Risk**: 抽選母集合 0 件・抽選枠未満のエッジケース — Mitigation: `TimedShopLottery` の単体テストで 0 件 / 不足 / 充足 / 過剰の 4 ケースを検証し、警告ログ + 空表示挙動を担保。
- **Risk**: 購入確認ダイアログ中にサイクル切替が起きた際の二重消費 — Mitigation: `ShopService` でダイアログ起動前のサイクル ID を保持し、`SpendYarn` 直前に再比較。一致しない場合は `Insufficient` 同様に中断し、再評価メッセージを表示。
- **Risk**: ショップ閉鎖→再訪問でのサイクル整合 — Mitigation: 状態は `IClock` から都度算出するため永続化不要。再訪問時に `Initialize()` 内で同じサイクルを再現する。

## References
- `.kiro/specs/timed-shop/requirements.md`
- `.kiro/specs/timed-shop/gap-analysis.md`
- `Assets/Scripts/Root/Service/MasterDataImportService.cs` — マスタ CSV ロードパターンの参考
- `Assets/Scripts/Root/Service/IUserPointService.cs` — 通貨操作 API
- `Assets/Scripts/Root/Service/IUserItemInventoryService.cs` — インベントリ操作 API
- `Assets/Scripts/Shop/Service/ShopService.cs` — 既存ショップサービスの実装参考
- Microsoft Learn: `System.Random` クラス — 決定論的 PRNG の説明（シードベース系列再現性）
