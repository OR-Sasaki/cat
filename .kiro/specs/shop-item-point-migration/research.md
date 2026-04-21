# Research & Design Decisions — shop-item-point-migration

---
**Purpose**: 本仕様で検討したアーキテクチャ評価・設計判断・残リスクを記録し、`design.md` の決定背景を辿れるようにする。
---

## Summary

- **Feature**: `shop-item-point-migration`
- **Discovery Scope**: Extension (既存の `Shop` シーンと RootScope サービス間の統合)
- **Key Findings**:
  - RootScope は `IUserPointService` / `IUserItemInventoryService` を `Lifetime.Singleton` で登録済みのため、`ShopScope` 側での再登録は不要 (親スコープ解決で十分)。
  - `UserItemInventoryService.AddFurniture` は `uint` を受け取るのに対し、現行 `GachaData.RewardFurnitureIds` は `List<string>` であり、**ID 型体系の破壊的変更**が必須。
  - `UserItemInventoryService` は `MasterDataImportService.Imported` を待って Load するが、`SceneScope` が `Awake` 時点で MasterDataImport を保証するため `ShopStarter.Start` 以降は安全に呼べる。
  - `UserPointService` は PlayerPrefs 自動永続化のため、`ShopState.YarnBalance = 10000` の旧モック初期値は移行と同時に失効する。
  - `ShopView` の Yarn 残高購読経路を `ShopState` から `IUserPointService` へ直接付け替えるため、`OnDestroy` での解除規約を厳守する必要がある。

## Research Log

### Topic: RootScope サービスの解決経路

- **Context**: `ShopScope` から `IUserPointService` / `IUserItemInventoryService` を解決する際、再登録が必要か確認する必要があった。
- **Sources Consulted**:
  - `Assets/Scripts/Root/Scope/RootScope.cs` (L21–L25): `As<IUserItemInventoryService>().AsSelf()` / `As<IUserPointService>().AsSelf()` で Singleton 登録済み。
  - `Assets/Scripts/Shop/Scope/ShopScope.cs`: ShopScope は `SceneScope` (`Lifetime.Scoped`) 継承のみで RootScope のサービスを参照可能。
  - `.kiro/steering/tech.md` — 「RootScope: 全シーン共通のシングルトンサービス」。
- **Findings**: VContainer の LifetimeScope 継承で親スコープの登録はそのまま解決される。再登録すると二重登録エラーになるため避ける。
- **Implications**: 設計上、`ShopScope.Configure` の差分はゼロ。本仕様では `ShopScope.cs` を変更しない。

### Topic: ガチャ家具 ID の型不整合

- **Context**: `GachaData.RewardFurnitureIds: List<string>` と `IUserItemInventoryService.AddFurniture(uint, int)` の型が一致しない。
- **Sources Consulted**:
  - `Assets/Scripts/Shop/State/ShopState.cs` (L26–L30): `RewardFurnitureIds` は `List<string>`。
  - `Assets/Scripts/Shop/Service/ShopService.cs` (L40): モックデータに `"chair_01"` 等の文字列 ID。
  - `Assets/Scripts/Root/State/MasterDataState.cs` (L22–L26): `Furniture.Id` は `uint`。
  - `Assets/Scripts/Root/Service/UserItemInventoryService.cs` (L211–L220): `IsKnownFurnitureId` が `MasterDataState.Furnitures` の `uint` と比較。
- **Findings**: 文字列 ID をそのまま流用するには `string → uint` マッピング層が要る。マスターに `chair_01` 等は未登録。
- **Implications**: `GachaData.RewardFurnitureIds` を `IReadOnlyList<uint>` に改める。マスター側の登録は `timed-shop` 仕様に委ね、本仕様ではモック段階で `UnknownId` が返る可能性を許容する (R7.3 で仕様化済み)。

### Topic: 毛糸残高の初期値

- **Context**: `ShopState.YarnBalance` の旧モック初期値 `10000` が移行後に失われる。
- **Sources Consulted**:
  - `Assets/Scripts/Root/Service/UserPointService.cs` (L82–L103): PlayerPrefs 未保存時は `SetYarnBalance(0)`。
  - `Assets/Scripts/Root/Service/UserPointSnapshot.cs` (L*): 復元形式が確定済み。
- **Findings**: 初回起動では残高 0 からスタートする。シード投入を行う場合は `UserDataImportService` もしくは `ShopStarter` から `AddYarn` を明示呼び出しする必要がある。
- **Implications**: 本仕様では**シード投入は導入しない**。開発時は PlayerPrefs を直接書き換えるか、既存 Root サービスのデバッグ API を利用する方針とする。要件 9.5 の「初期化実行可能性」は `ShopService.Initialize` が残高非依存であることで満たす。

### Topic: `IUserPointService` 購読の解除タイミング

- **Context**: `ShopView` が `IUserPointService.YarnBalanceChanged` を購読すると、Singleton 側のハンドラを Scene 破棄時に解除し忘れるとメモリリーク / NullRef の温床になる。
- **Sources Consulted**:
  - `Assets/Scripts/Shop/View/ShopView.cs` (L74–L85, L143–L160): 既存の `OnDestroy` / `UnsubscribeFromStateEvents` 実装。
- **Findings**: MonoBehaviour の `OnDestroy` で `-=` を確実に呼ぶ既存慣習があるため、同じパターンを踏襲すれば良い。
- **Implications**: 設計側で「Scene 破棄 ⇒ ShopView 破棄 ⇒ `YarnBalanceChanged -= OnYarnBalanceChanged`」の経路を明示。

### Topic: エラーメッセージ変換の配置

- **Context**: `PointOperationErrorCode.Insufficient` を「毛糸が足りません」のような UI 文言へ変換する責務の置き場所。
- **Sources Consulted**:
  - 現行 `ShopService.OnProductCellTappedAsync` (L101–L110): 残高不足時のダイアログ文言がハードコード。
  - gap-analysis §6 「Preferred Approach」: Option A + 小ユーティリティ併用。
- **Findings**: 現状の Shop 規模 (4 ファイル) で専用ユーティリティを切る便益は小さい。
- **Implications**: 変換ロジックは `ShopService` 内の private メソッド (例: `BuildInsufficientYarnArgs`) に集約し、将来 `timed-shop` 拡張で肥大化した時点で切り出す。

## Architecture Pattern Evaluation

| Option | Description | Strengths | Risks / Limitations | Notes |
|--------|-------------|-----------|---------------------|-------|
| A: Direct Extension | `ShopService` に RootScope サービスを直接注入し、`ShopState` から該当責務を剥がす | 新規ファイル不要、差分最小、既存 DI パターンに忠実 | `ShopService` が薄い変換責務を抱えやすい (現状規模なら許容) | **採用**。gap-analysis §6 Preferred と一致 |
| B: Shop Gateway Layer | `ShopCurrencyGateway` / `ShopInventoryGateway` を新設し `ShopService` から抽象化 | ドメイン境界が明示でき、将来の時限ショップ特例を吸収しやすい | 現状ファイル 4 本に対し Gateway 2 本追加は過剰。依存段数が増え読解コスト増 | 却下。`timed-shop` 着手時に再評価 |
| C: Hybrid (A + Utility) | Option A にメッセージマッパーや ID 変換ユーティリティをピンポイント追加 | A の最小差分を保ちつつ責務分離 | 今回は `string→uint` 変換がそもそも不要 (ID 型自体を変更する) / メッセージ種類が 2–3 種のためユーティリティ化の便益薄 | 部分採用: ユーティリティ化は見送り、private メソッド化のみ |

## Design Decisions

### Decision: `GachaData.RewardFurnitureIds` を `uint[]` / `IReadOnlyList<uint>` 化

- **Context**: `IUserItemInventoryService.AddFurniture(uint, int)` と型を合わせ、冗長な変換層を排除する必要がある。
- **Alternatives Considered**:
  1. 現行 `List<string>` を維持し、`ShopService` 内で `string→uint` マップを持つ (案 γ)
  2. `GachaData` 自体を `uint[]` で再定義 (案 α)
  3. 本仕様ではガチャ家具付与を実装せず、R7 を縮退する (案 β)
- **Selected Approach**: 案 α — `RewardFurnitureIds` を `IReadOnlyList<uint>` で受け取る `record GachaData` に変更し、モックデータも `uint` リテラルで投入する。マスター未登録 ID は `AddFurniture` が `UnknownId` で弾き、`ShopService` がログ出力して継続する。
- **Rationale**: R7.2 で「`AddFurniture` が受理する `uint` 型と整合する形式で保持する」と明示されている。暫定マップは `timed-shop` 着手時に再度破棄されるため二重作業。
- **Trade-offs**: マスター未登録では獲得表示と実インベントリが乖離する (獲得メッセージは表示されるが加算は失敗) が、R7.3 / R7.5 で仕様化済み。
- **Follow-up**: `timed-shop` 仕様で家具マスター整備と同時に ID を本番値へ差し替える。

### Decision: `ShopView` は `IUserPointService` を直接 `[Inject]` で受ける

- **Context**: 残高変更通知と画面更新を `ShopState` 経由から `IUserPointService` 直接購読へ切り替える必要がある。
- **Alternatives Considered**:
  1. `ShopView` が `ShopService` 経由で残高イベントを中継してもらう (ShopService が Observer パターンをラップ)
  2. `ShopView.Construct` に `IUserPointService` を追加して直接購読 (案 α)
- **Selected Approach**: 案 α — `Construct(ShopState state, ShopService shopService, IUserPointService userPointService)` へ拡張。
- **Rationale**: 依存方向ルール「View → Service → State」で、`IUserPointService` は Service 層の契約なので規約違反にならない。中継を ShopService に置くとイベントの二重伝播が必要となり、かえって複雑化する。要件 9.2 も本案を許容。
- **Trade-offs**: `ShopView` の `[Inject]` 引数が 1 つ増えるが、既存 VContainer 慣習に沿う。
- **Follow-up**: `OnDestroy` での購読解除は R4.4 に準拠しテスト観点に含める。

### Decision: `ShopState` の一括撤去

- **Context**: `ShopState.YarnBalance` / `ConsumeYarn` / `AddYarn` / `OnYarnBalanceChanged` が旧パス経由で呼ばれ続けると、真実の源が二重化する。本アプリは未リリースのため後方互換性は不要。
- **Selected Approach**: 該当 API を一括削除し、呼び出し側 (`ShopService` / `ShopView`) を同タイミングで書き換える。段階的縮退や `[Obsolete]` 経過期間は設けない。
- **Rationale**: R8.1 / R3.5 / R2.6 が「API を公開しない」と明記。呼び出し元は Shop シーン内部に閉じており、コンパイルエラーで抜け漏れが検知できる。未リリースのため下流影響もない。
- **Trade-offs**: コミット単位が大きくなるが、3 ファイル (`ShopState` / `ShopService` / `ShopView`) に収まる。
- **Follow-up**: `GachaData` の型変更も同タイミングで実施して問題ない。

### Decision: エラーメッセージ変換は `ShopService` 内 private メソッドに集約

- **Context**: `PointOperationErrorCode` / `ItemInventoryErrorCode` をユーザー向け文言 / ログへ変換する責務の置き場所。
- **Alternatives Considered**:
  1. 専用ユーティリティ `PurchaseMessageMapper` を新設 (Option C)
  2. `ShopService` 内 private メソッドで変換 (案 α)
- **Selected Approach**: 案 α。
- **Rationale**: 変換ルートが 5 種未満かつ表示文言は全て `CommonMessageDialog` 向け。ユーティリティ化の便益は現段階では乏しい。
- **Trade-offs**: `ShopService` 行数は微増するが、テスト対象は同じクラスに集中する。
- **Follow-up**: `timed-shop` / 時限ショップ特例で変換パターンが 5 種を超えた場合に抽出を再検討。

## Risks & Mitigations

- **R-A: 呼び出し抜けによる動作不整合** — `ShopState` の API 削除で抜け漏れがあればコンパイルエラーで検知可能。`ShopState` の該当 API 削除を先行してコンパイラ検証を徹底する。
- **R-B: ガチャ家具加算の `UnknownId` 頻発** — マスター未登録の暫定 ID を使う限り `AddFurniture` が常時失敗する。R7.3 に沿ってログ出力のみに留め、ガチャ結果メッセージ (R7.5) は `MasterDataState.Furnitures` で解決できない場合 ID 表示にフォールバックする。
- **R-C: `YarnBalanceChanged` 購読リーク** — Singleton 側にハンドラが残ると Scene 再訪問で多重更新。`ShopView.OnDestroy` での解除と、単体テスト (「再ロード後の発火数が 1」) でカバー。
- **R-D: 初期残高 0 スタートによる開発体験低下** — 開発フェーズでは全セルが `interactable = false` になる可能性。開発者には PlayerPrefs 操作手順 (もしくは Editor メニュー拡張) を周知。

## References

- `Assets/Scripts/Root/Service/IUserPointService.cs` — サービス契約定義。
- `Assets/Scripts/Root/Service/IUserItemInventoryService.cs` — 家具/着せ替えインベントリ契約。
- `Assets/Scripts/Root/Scope/RootScope.cs` — Singleton 登録。
- `Assets/Scripts/Shop/Service/ShopService.cs` — 現行購入ロジック。
- `Assets/Scripts/Shop/View/ShopView.cs` — 現行残高表示とイベント配線。
- `Assets/Scripts/Shop/State/ShopState.cs` — 縮退対象の旧責務。
- `.kiro/steering/tech.md` / `structure.md` — プロジェクト規約 (依存方向・命名・ログ形式)。
- `.kiro/specs/shop-item-point-migration/gap-analysis.md` — 現状分析と採用案の根拠。
