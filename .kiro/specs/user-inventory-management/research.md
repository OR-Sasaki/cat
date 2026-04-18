# Research & Design Decisions: user-inventory-management

## Summary
- **Feature**: `user-inventory-management`
- **Discovery Scope**: Extension (既存 VContainer + PlayerPrefs アーキテクチャへの追加)
- **Key Findings**:
  - 既存 `UserEquippedOutfitService` が Service+State+`PlayerPrefs` 永続化の参照実装として確立されており、そのまま踏襲可能。
  - `PlayerPrefsService` は `JsonUtility` ベースのため、`Dictionary<,>` 直接シリアライズ不可 → 家具・着せ替えの DTO は配列で表現する必要あり。
  - 要件スコープは「インターフェース + 最小限の実装」に限定。家具の所持モデルは **数量ベース (Dictionary<uint,int>)** を採用し、将来のインスタンス管理への拡張は別タスクで対応する。

## Research Log

### 既存 Service/State 永続化パターン
- **Context**: `IUserItemInventoryService` / `IUserPointService` を既存パターンと整合させる必要がある。
- **Sources Consulted**:
  - `Assets/Scripts/Root/Service/UserEpuippedOutfitService.cs`
  - `Assets/Scripts/Root/State/UserEquippedOutfitState.cs`
  - `Assets/Scripts/Root/Service/PlayerPrefsService.cs`
  - `Assets/Scripts/Root/Scope/RootScope.cs`
- **Findings**:
  - `Service` がコンストラクタ内で `Load()` を呼び `State` を復元。
  - `Save()` は `JsonUtility.ToJson` で `PlayerPrefsKey` enum 値をキーにして `PlayerPrefs.SetString`。
  - Dictionary は `JsonUtility` で扱えないため、既存コードは配列 (`UserEquippedOutfit[]`) を DTO として用いる。
  - RootScope は `builder.Register<T>(Lifetime.Singleton)` で State と Service の両方を登録。
  - 既存 `UserEquippedOutfitService` は変更通知 (イベント) を公開していない — 新 Service は自前でイベントを追加する必要がある。
- **Implications**:
  - 新 Service も同じ「コンストラクタで `Load()` → 変更時に `Save()`」フローで実装。
  - 家具・着せ替えの保存 DTO は配列型 (`FurnitureHoldingEntry[]`, `uint[]`) で構成する。
  - 変更通知は既存プロジェクトが `Action` / `Action<T>` を採用しているため、`event Action<T>` パターンで公開する。

### 既存モックとの共存
- **Context**: `UserState.UserOutfits[]` / `UserState.UserFurnitures[]` / `ShopState.YarnBalance` を残したまま新 Service を追加する。
- **Sources Consulted**:
  - `Assets/Scripts/Root/State/UserState.cs` (モックの UserOutfit/UserFurniture)
  - gap-analysis.md
- **Findings**:
  - モック側は `UserFurniture.Id`（個体識別）と `UserFurniture.FurnitureID`（マスター参照）を持つインスタンス型。
  - 既存 `RedecorateScrollerService` 等がモックを参照しているが、本スペックは改修対象外。
- **Implications**:
  - 新 State/Service は独立した型・キーで実装し、既存モックには一切触れない。
  - 将来の移行タスクで `UserState.UserFurnitures` (インスタンス型) と新 State (数量型) のブリッジ戦略を検討する必要あり。

### CLAUDE.md / Steering ルールの確認
- **Context**: コーディング規約と DI 規則への準拠。
- **Sources Consulted**:
  - `CLAUDE.md` (プロジェクトルート)
  - `.kiro/steering/tech.md`, `.kiro/steering/structure.md`
- **Findings**:
  - `#nullable enable` をファイル先頭に付与。
  - UniTask 非同期メソッドは最終引数に `CancellationToken` を取る。
  - コンストラクタに `[Inject]` を付与し IL2CPP stripping を防止。
  - `///` コメント形式を採用 (`/// <summary>` は使わない)。
  - `View → Service → State` の層依存を遵守。
- **Implications**:
  - 新 Service/State すべてのファイルで `#nullable enable` を宣言。
  - 非同期 API を用意する場合は `CancellationToken` を受け取る形で定義 (永続化のみ非同期化を検討)。

## Architecture Pattern Evaluation

| Option | Description | Strengths | Risks / Limitations | Notes |
|--------|-------------|-----------|---------------------|-------|
| 案 α (数量) | `Dictionary<uint, int>` で家具 ID → 所持数を管理 | 要件 R2 の API 定義に最も自然、実装最小 | 個体識別 (IsoGrid 配置) への対応は別タスク | **採用**: スコープ「最小限の実装」と整合 |
| 案 β (インスタンス) | `List<FurnitureInstance>` で個体管理 | 将来の IsoGrid 配置連携がスムーズ | API が一段重い、本スペックの要件 R2-6 (ID と数量) のために集計処理が必要 | 将来の拡張候補 |
| 案 γ (ハイブリッド) | 内部インスタンス + 外部数量 API | 両立可能 | 実装コスト・テスト複雑度が増加 | オーバースコープ |

## Design Decisions

### Decision: 家具の所持モデルは数量ベース
- **Context**: 要件 R2 は「所持数の取得・増減」と「ID+数量の一覧」を要求。本スペックは「インターフェース + 最小限の実装」が対象。
- **Alternatives Considered**:
  1. 数量管理 (`Dictionary<uint, int>`)
  2. インスタンス管理 (`List<FurnitureInstance>`)
  3. ハイブリッド
- **Selected Approach**: 案 α — `IUserItemInventoryState` 内部で `Dictionary<uint, int>` を保持し、数量ベースの API を提供。
- **Rationale**: 要件のシグネチャ (`GetFurnitureCount(uint)`, `AddFurniture(uint, int)`) と完全整合し、実装コストが最小。既存モックの置き換えは別タスクで段階的に行うため、現時点で個体識別を持ち込まない。
- **Trade-offs**:
  - ✅ 実装・テストともに最小。
  - ❌ 将来の IsoGrid 配置連携時に DTO ブリッジかモデル拡張が必要。移行コストは別タスクで吸収。
- **Follow-up**: 移行タスクで `UserFurniture[]` (instance-based) からの変換方針を決定する。

### Decision: 毛糸の桁あふれは不正引数エラー
- **Context**: 要件 R5-7 — `int.MaxValue` を超える加算の扱いを仕様化する。
- **Alternatives Considered**:
  1. 上限で丸めて正常結果
  2. 加算を拒否し不正引数エラーを返す
- **Selected Approach**: 案 B — 加算後の残高が `int.MaxValue` を超える場合は `PointOperationError.Overflow` を返し、残高を変更しない。
- **Rationale**: 通貨系の暗黙の丸めは会計上のバグを誘発しやすく、呼び出し元が明示的に扱えるほうが安全。
- **Trade-offs**:
  - ✅ 呼び出し元が失敗を検知し上位でハンドリング可能。
  - ❌ 上限近くで通常プレイ中に加算失敗する可能性 (現実的にはゲーム内収支では `int.MaxValue` に到達し難い)。
- **Follow-up**: 将来的に上限値がゲームデザイン上問題になる場合は `long` 化を検討 (DTO バージョンで吸収)。

### Decision: PlayerPrefs DTO にバージョンフィールドを導入
- **Context**: 要件 R7-4/R7-5 — 将来のフォーマット変更に対応するためのバージョン管理。
- **Alternatives Considered**:
  1. `int Version` フィールドを DTO に含めて、非互換時は破棄し空初期化
  2. 保存キーの suffix (`UserItemInventory_v1`) でバージョン分離
- **Selected Approach**: 案 1 — DTO 先頭に `int Version` を置き、現行バージョン (`1`) と一致しない場合は読み飛ばして空状態を初期化。
- **Rationale**: 既存 `PlayerPrefsKey` enum を乱さず、DTO 内でバージョンを自己記述できる。
- **Trade-offs**:
  - ✅ enum を汚染せず DTO 単位で完結。
  - ❌ 将来マイグレーション戦略 (旧バージョンからの変換) は別途検討が必要。
- **Follow-up**: 本スペックでは「非互換 → 破棄 + 空初期化」のみ実装。旧バージョン保存データのマイグレーションは必要性が生じた時点で別タスクとする。

### Decision: 変更通知は `event Action<T>` で公開
- **Context**: 要件 R6 — View 層が購読可能な変更通知を提供する。
- **Alternatives Considered**:
  1. `event Action<T>` / `event Action` (既存プロジェクトパターン)
  2. UniRx / R3 の `Observable<T>`
  3. `UnityEvent<T>`
- **Selected Approach**: 案 1 — C# の `event Action<T>` を採用。購読者例外は `try-catch` で吸収し、他の購読者への通知継続を保証する。
- **Rationale**: 既存コードベースで Observable や UnityEvent (非 UI 用途) を使用していない。`event Action<T>` は標準的で Root 層に追加依存を持ち込まない。
- **Trade-offs**:
  - ✅ 既存パターン整合、追加ライブラリ不要。
  - ❌ 購読解除ミスによるメモリリーク対策は呼び出し元責務 (View 層の `OnDestroy` 等で `-=` 解除)。
- **Follow-up**: 将来 R3 / UniRx 導入時に `Observable` アダプタを追加可能。

### Decision: 永続化は同期 API、非同期境界は将来拡張用に確保
- **Context**: 要件 R8-8 — UniTask 非同期 API を `CancellationToken` 付きで提供する必要がある。
- **Alternatives Considered**:
  1. 内部は同期で `Save()` 実行、非同期メソッドは将来追加
  2. 初期化・永続化を `UniTask` 化
- **Selected Approach**: インターフェースに非同期 API (`InitializeAsync`, `SaveAsync`) を`UniTask` + `CancellationToken` 付きで用意する。実装は PlayerPrefs が同期 API のため `UniTask.CompletedTask` を返す。
- **Rationale**: 将来サーバ連携 (ApiClient 経由の同期) を追加する際、インターフェース変更なしで拡張できる。
- **Trade-offs**:
  - ✅ 将来のサーバ連携タスクで Service 型を差し替えやすい。
  - ❌ 現状は同期処理のため見かけ上のオーバーヘッド (実害なし)。

## Risks & Mitigations
- **リスク1**: モック (`UserState.UserOutfits`) と新 State の二重管理で状態が乖離する → 本スペックでは共存前提 (既存参照元は改修対象外)。移行タスク側でブリッジを明示。
- **リスク2**: 装備中 (`UserEquippedOutfitState`) が所持外の着せ替えを指す不整合 → 要件 R3-6 に従い、`UserItemInventoryService.RemoveOutfit` 等の減算 API はスコープ外。本スペックでは「付与のみ」なので装備整合性は自動的に保たれる。
- **リスク3**: PlayerPrefs の保存失敗でインメモリ状態だけ進む → 要件 R7-3 に従いログ出力のみ。呼び出し元には `Action<T>` 通知が先行発火するため、UI 表示とは整合する。

## References
- プロジェクト内: `Assets/Scripts/Root/Service/UserEpuippedOutfitService.cs` — 参照実装
- プロジェクト内: `Assets/Scripts/Root/Service/PlayerPrefsService.cs` — 永続化基盤
- Steering: `.kiro/steering/tech.md` — 層依存・コーディング規約
- CLAUDE.md: `#nullable enable` / `[Inject]` / UniTask 規約
