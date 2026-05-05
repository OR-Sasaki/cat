# Research & Design Decisions — closet-two-level-tabs

## Summary
- **Feature**: `closet-two-level-tabs`
- **Discovery Scope**: Extension (既存 Closet UI への 2 段階タブ機構の追加)
- **Key Findings**:
  - Closet 機能は既に `View → Service → State` の 4 層で構成されており、本タブ機能は同パターンに沿った「タブ専用の State / Service / View 一式」を追加するのが最も自然である。
  - `ClosetScrollerService.LoadData()` はマスターデータ走査時にフィルタ条件を差し込むだけで `OutfitType` 絞り込みが完結する。再描画コストは現状 1 シーンに数十件規模のため、フィルタ + `Scroller.ReloadData(0)` で十分軽量。
  - 必要な UI 素材 (背景・アイコン・選択状態) は `Assets/UI/Home/Closet/Textures/` に配置済みで、`HeadingItem.prefab` / `TabItem.prefab` も準備済み。本機能では新規アセットを追加しない。

## Research Log

### Topic: 既存 Closet 関連クラス構成の確認
- **Context**: 拡張に必要な接続点 (Service / State / View) を特定する。
- **Sources Consulted**:
  - `.kiro/steering/closet.md`
  - `Assets/Scripts/Home/View/ClosetUiView.cs`
  - `Assets/Scripts/Home/Service/ClosetScrollerService.cs`
  - `Assets/Scripts/Home/State/ClosetOutfitData.cs`
  - `Assets/Scripts/Home/Scope/HomeScope.cs`
- **Findings**:
  - `ClosetUiView` は In/Out アニメ後に `OnOpen` を発火する `UiView` 派生で、SerializeField 経由で `EnhancedScroller` と本体設定値のみ保持。
  - `ClosetScrollerService` は `_closetUiView.OnOpen` を購読し `LoadData()` でグリッドを毎回再構築する。`LoadData` 内のループがフィルタ差し込みポイント。
  - DI は `HomeScope` で完結 (`RegisterComponent(_closetUiView)`、`RegisterEntryPoint<ClosetScrollerService>()`)。タブ用の追加登録もここに統合可能。
- **Implications**:
  - タブ選択を保持する `ClosetTabState`、変更を駆動する `ClosetTabService`、UI 制御を担う `ClosetTabsView` を追加し、既存層構成を踏襲する。
  - `ClosetScrollerService` には「現在の `OutfitType` を `ClosetTabState` から参照する」最小変更で済む。

### Topic: `OutfitType` の最新ラインアップとタブ割当
- **Context**: 要件 2.x で定義された大タブ → 小タブの対応を、現行 enum と突き合わせて検証する。
- **Sources Consulted**: `Assets/Arts/Character/Scripts/Outfit.cs`
- **Findings**:
  - 現行 `OutfitType` は `Body, Cloth, Face, FaceMakeup, HandAccessory, HeadAccessory, LegAccessory, Tail, Effect` の 9 種類 (アルファベット順 + 末尾に `Effect`)。
  - 要件 2.1 では「体」= `Body, Face, Tail, FaceMakeup`、要件 2.2 では「服」= `Cloth, HandAccessory, HeadAccessory, LegAccessory, Effect` と定義。9 種すべてが何らかの大タブに割当済み。
  - 要件 2.3 の例 (`FaceMakeup` を未割当として例示) は 2.1 の割当と矛盾するが、本設計では 2.1 / 2.2 の明示マッピングを正とし、`FaceMakeup` は「体」に含める。`research.md` のリスクとして残す。
- **Implications**:
  - 「大タブ ↔ 小タブ集合」のマッピングはコード上で**唯一の事実源 (single source of truth)** を用意し、全コンポーネントから参照させる。`ClosetTabState` に静的 readonly テーブルとして同梱する。

### Topic: 利用可能な UI アセット
- **Context**: 新規アセット追加禁止 (要件 7.5) を満たしつつ、既存 Texture / Prefab で 2 段階タブを構築できるか確認する。
- **Sources Consulted**: `Assets/UI/Home/Closet/Textures/`, `Assets/UI/Home/Closet/Prefabs/HeadingItem.prefab`, `Assets/UI/Home/Closet/Prefabs/TabItem.prefab`
- **Findings**:
  - 大タブ背景: `dressup_heading.png` (非選択) / `dressup_heading_select.png` (選択) — `HeadingItem.prefab` に配線可。
  - 小タブ背景: `dressup_tab_open.png` (選択) / `dressup_tab_close.png` (非選択) — `TabItem.prefab` に配線可。
  - 小タブアイコン (Icons/): `dressup_tab_cat_white.png`/`cat_pink.png` (Body), `dressup_tab_face_white.png` (Face), `dressup_tab_tail_white.png` (Tail), `dressup_tab_pattern_white.png` (FaceMakeup), `dressup_tab_clothes_white.png`/`clothes_pink.png` (Cloth), `hed_white 1.png` (HeadAccessory), `belongings_white 1.png` (HandAccessory), `shoes_white 1.png` (LegAccessory), `effect_white 1.png` (Effect)。pink を選択中色として扱う運用も可能。
  - `HeadingItem.prefab`、`TabItem.prefab` 共にコードからの参照は現状無し (steering 記載どおり)。
- **Implications**:
  - 新規 Prefab を追加せず、既存 `HeadingItem` / `TabItem` を「選択状態スプライト切替」「アイコン色切替」前提で使い回す。`SerializeField` で選択/非選択スプライトを保持する `ClosetMajorTabItemView` / `ClosetMinorTabItemView` を新設する。

### Topic: タブ切替時の再描画コスト
- **Context**: 要件 5.1〜5.4 のフィルタ動作とスクロールリセットが、既存 EnhancedScroller の API でまかなえるか確認。
- **Sources Consulted**: `Assets/Scripts/Home/Service/ClosetScrollerService.cs`、`EnhancedScroller` ドキュメント (公開済 API: `ReloadData(scrollPositionFactor)`)。
- **Findings**:
  - `Scroller.ReloadData(0f)` でスクロール位置を先頭に強制リセット可能。
  - `LoadData` 内でフィルタ済みリストを構築すれば `_data.Count` が 0 のとき `GetNumberOfCells = 1` (ダミー行のみ) となり、空状態が自然に成立する。
- **Implications**:
  - 要件 5.2 (先頭リセット) と 5.3 (空状態) は `LoadData(filterType)` + `ReloadData(0f)` の組み合わせで満たせる。
  - 要件 5.4 (連続選択時の再構築抑止) は `ClosetTabService` が「現在値と異なる場合のみ」変更通知する形で達成。

## Architecture Pattern Evaluation

| Option | Description | Strengths | Risks / Limitations | Notes |
|--------|-------------|-----------|---------------------|-------|
| A. State + Service 分離 (採用) | `ClosetTabState` がタブ状態を保持、`ClosetTabService` が遷移・既定値・通知を担当、`ClosetScrollerService` はフィルタ参照のみ | View → Service → State 規約に完全準拠、テスト容易、責務単一 | クラス数が増える | 現行 Closet と Redecorate の構成と同型 |
| B. ClosetScrollerService 内に統合 | タブの状態と切替ロジックを `ClosetScrollerService` に集約 | 追加クラス最少 | Service 単一責務逸脱、テスト性低下、要件 8.4 (最小変更) に反する | 不採用 |
| C. View にロジック保持 | `ClosetUiView` がタブ状態とフィルタを管理 | 配線最短 | View → Service の依存方向違反 (要件 8.1 違反)、Service が View に強依存となる | 不採用 |

## Design Decisions

### Decision: タブ状態を `ClosetTabState` (新規) に集約する
- **Context**: 要件 8.2「タブ状態を `Home.State` 配下に保持し View が直接相互依存しない」の達成。
- **Alternatives Considered**:
  1. `HomeState` に内包 — シーン全体状態と粒度が異なるため不適切。
  2. `ClosetUiView` の private フィールド — 依存方向違反、テスト不能。
- **Selected Approach**: `Home.State.ClosetTabState` を新設し、選択中の `MajorTab`/`OutfitType` と「大タブ ↔ 小タブ集合」マッピング (静的 readonly) を保持。`UnityEvent<MajorTab>` / `UnityEvent<OutfitType>` で変更を通知。
- **Rationale**: 既存 `OutfitAssetState` 等と同じ層・寿命 (`Lifetime.Scoped`) で構成でき、Service / View 双方から最小依存で参照可能。
- **Trade-offs**: ファイル増加 1 件、しかし責務分離と将来拡張 (例: 並び順カスタマイズ) が容易になる。
- **Follow-up**: マッピング定義はテストで列挙網羅 (全 `OutfitType` がいずれか 1 つの `MajorTab` に属する) を確認。

### Decision: タブ切替操作は `ClosetTabService` を経由する
- **Context**: 要件 4.x / 5.4 の「既定値選択」「連続選択時の再構築抑止」を一元管理する。
- **Alternatives Considered**:
  1. View が直接 `ClosetTabState` を書き換える — 依存方向違反。
  2. `ClosetScrollerService` に統合 — 要件 8.4 に反する。
- **Selected Approach**: `Home.Service.ClosetTabService` を新設し、`SelectMajorTab(MajorTab)` / `SelectMinorTab(OutfitType)` / `ResetToDefault()` を公開。差分のみ `ClosetTabState` に書込み、変更時のみイベント発火。
- **Rationale**: 状態遷移ロジックを 1 箇所に集約し、View / `ClosetScrollerService` は purely reactive にできる。
- **Trade-offs**: クラス数 +1 だが、テストとレビューが容易。
- **Follow-up**: 既定値ロジック (`MajorTab.Body → OutfitType.Face`、`MajorTab.Cloth → OutfitType.Cloth`) を unit-testable に保つ。

### Decision: フィルタは `ClosetScrollerService.LoadData()` のループ内で実施する
- **Context**: 要件 8.4 (Scroller への変更最小化)、要件 5.1。
- **Alternatives Considered**:
  1. 別 Service に再構築委譲 — 既存責務の二分化で複雑化。
  2. View 側でセルを非表示にする — グリッド整列が崩れる。
- **Selected Approach**: `LoadData()` の引数または `ClosetTabState` 参照で `currentOutfitType` を受け取り、`masterOutfit.Type` 不一致をスキップして `_data` に積む。
- **Rationale**: 既存の構造を一切壊さず、追加コードはフィルタ条件 1 行 + Tab 変更購読 1 箇所のみ。
- **Trade-offs**: `LoadData` を毎回フル再構築する点は既存仕様のまま (将来差分更新へ最適化可能)。
- **Follow-up**: 要件 6.3 を満たすため、フィルタ後にも装備中アウトフィットの `Selected` 復元ロジックが正しく動作することを確認。

### Decision: `ClosetUiView` への追加は SerializeField 拡張のみとする
- **Context**: 要件 8.5 (規約遵守) と要件 7.x (視覚状態) を最小変更で達成。
- **Alternatives Considered**:
  1. `ClosetUiView` を分割 — 既存 In/Out アニメや `OnOpen` の参照点を壊すリスク。
  2. View ロジックを Manager 層に移動 — 規模に対し過剰。
- **Selected Approach**: `ClosetUiView` に `ClosetMajorTabsView` / `ClosetMinorTabsView` への参照 (SerializeField) を追加。`Init` 内で `ClosetTabService` を注入し、各タブビューにバインドする。
- **Rationale**: 既存フィールド構成を壊さず、配線変更は Inspector 上で完結。
- **Trade-offs**: View が中継役になるが、ロジックは Service 側に分離されているため依存方向は維持される。
- **Follow-up**: Inspector 上で SerializeField の null チェックを `[Conditional]` などで早期検出可能にする (任意)。

## Risks & Mitigations
- リスク: 要件 2.1 と要件 2.3 (`FaceMakeup` 未割当例示) の不整合が将来仕様変更時に再燃する。
  - 緩和: `ClosetTabState` のマッピング定義を SSOT 化し、`OutfitType` 全件の網羅テストを追加。`requirements.md` 修正を spec フェーズで提案。
- リスク: `Effect` / `FaceMakeup` のマスターデータが未投入のため、初期表示で空状態が頻発する可能性。
  - 緩和: 要件 5.3 を満たす空状態 (グリッド 0 件) が UI 上で「壊れて見えない」ことを Open 時に確認 (デバッグ時の手順で `outfits.csv` の状態を確認するチェックリストを `research.md` に残す)。
- リスク: `HeadingItem.prefab` / `TabItem.prefab` が未配線のため、Prefab 側の Image 構造に対するスクリプト前提が変わると壊れる。
  - 緩和: 専用 View コンポーネント (`ClosetMajorTabItemView`, `ClosetMinorTabItemView`) で Image 参照を SerializeField 化し、Prefab 側の構造変更に強くする。
- リスク: 要件 4.3 (同一大タブ再タップ時に小タブを変更しない) の実装漏れにより、再タップで `Face` に戻る不具合が発生する。
  - 緩和: `ClosetTabService.SelectMajorTab` で「現値と同一なら no-op」のガードを最初のステートメントに置く。

## References
- `.kiro/steering/closet.md` — 既存アーキテクチャと拡張ガイドの一次情報。
- `.kiro/steering/structure.md` / `.kiro/steering/tech.md` — 層構成と命名・コーディング規約。
- `Assets/Arts/Character/Scripts/Outfit.cs` — `OutfitType` 定義 (本機能の分類対象)。
- `Assets/UI/Home/Closet/Prefabs/HeadingItem.prefab`、`TabItem.prefab` — 流用対象の UI Prefab。
