# Implementation Gap Analysis: canvas-safe-area-resizer

## Summary

- **Feature**: `canvas-safe-area-resizer`
- **Discovery Scope**: New Feature (greenfield コンポーネント) / Simple Addition
- **Key Findings**:
  - 既存コードベースに SafeArea 関連の実装は **存在しない** (`grep -rln "Screen.safeArea\|SafeArea"` でヒットなし)
  - 全シーン共通の Canvas 構成は **Screen Space - Camera + Scale With Screen Size (1080×1920, MatchWidth)** が支配的。`Fade.unity` のみ **Screen Space - Overlay + Constant Pixel Size (800×600)**
  - 配置先は `Assets/Scripts/Root/View/` 配下、namespace `Root.View` が自然 (DialogCanvasView / BackdropView と同列の再利用 UI コンポーネント)
  - DI 不要の **Pure MonoBehaviour** 実装で要件を満たせる (RootScope への登録は不要)
  - 編集中プレビュー (`[ExecuteAlways]`) は既存コードベースでは未使用パターンだが、Unity 標準機能のため新規導入の障壁は低い

## Document Status

- gap-analysis.md フレームワーク (.kiro/settings/rules/gap-analysis.md) に従い、コードベース調査 (find / grep / Read) と Unity シーン設定 (.unity) の YAML パース調査を実施
- Web 検索による外部依存調査は **不要** (Unity 標準 API `Screen.safeArea` のみで完結)

## 1. Current State Investigation

### 1.1 Domain-related Assets Scan

| 調査対象 | 結果 |
|---------|------|
| `Screen.safeArea` の既存使用 | **なし** |
| `SafeArea` 命名のクラス・ファイル | **なし** |
| `RectTransform` 操作の既存ユーティリティ | **なし** (View の各 MonoBehaviour で個別に操作) |
| Canvas 構成の前例 | `Root/View/DialogCanvasView.cs` (Canvas + Backdrop の構成パターン) |

### 1.2 Existing Patterns (引き継ぐべき規約)

| カテゴリ | 規約 | ソース |
|---------|------|--------|
| ファイル先頭 | `#nullable enable` | 全 View ファイル (`DialogCanvasView.cs` ほか) |
| 必須コンポーネント | `[RequireComponent(typeof(...))]` | `BackdropView.cs`, `DialogCanvasView.cs`, `FragmentedIsoGrid.cs` |
| インスペクタ参照取得 | `void Reset()` で `GetComponent<>()` 自動取得 | `DialogCanvasView.cs`, `BackdropView.cs` |
| エラーログ | `Debug.LogError($"[ClassName] {message}")` | `DialogCanvasView.cs`, `tech.md` |
| 名前空間 | `Root.View` (再利用 UI), `{Scene}.View` (シーン固有) | `structure.md` |
| Editor 限定コード | `#if UNITY_EDITOR` ガード or `Assets/Editor/` への配置 | `FragmentedIsoGrid.cs`, `IsoGridSettingsViewEditor.cs` |
| カスタムエディタ配置 | `Assets/Editor/{ClassName}Editor.cs` namespace `Editor` | `IsoGridSettingsViewEditor.cs` |
| 依存方向 | View → Service → State (View は DI 注入のみ受ける) | `structure.md`, `tech.md` |

### 1.3 Canvas Configurations Inventory

| シーン | RenderMode | UiScaleMode | ReferenceResolution | MatchWidthOrHeight |
|-------|-----------|-------------|---------------------|---------------------|
| Logo | ScreenSpace-Camera (1) | ScaleWithScreenSize (1) | 1080×1920 | 0 (width) |
| Title | ScreenSpace-Camera (1) | ScaleWithScreenSize (1) | 1080×1920 | 0 (width) |
| Home | ScreenSpace-Camera (1) | ScaleWithScreenSize (1) | 1080×1920 | 0 (width) |
| Shop | ScreenSpace-Camera (1) | ScaleWithScreenSize (1) | 1080×1920 | 0 (width) |
| Timer | ScreenSpace-Camera (1) | ScaleWithScreenSize (1) | 1080×1920 | 0 (width) |
| History | ScreenSpace-Camera (1) | ScaleWithScreenSize (1) | 1080×1920 | 0 (width) |
| Fade | ScreenSpace-Overlay (0) | ConstantPixelSize (0) | 800×600 | 0 |

**含意**: Requirement 4 (Render Mode 互換) の対象は **Overlay / Camera の 2 モードのみ** で十分。World Space 用の Canvas は 0 件のため、Requirement 4.2 (World Space 警告) は **保険的位置付け**。

### 1.4 Integration Surfaces

| 統合面 | 状況 |
|-------|------|
| データモデル / マスターデータ | 不要 |
| サービス層との連携 | 不要 (View 内で完結) |
| VContainer DI 登録 | 不要 (`[Inject]` を持たない Pure MonoBehaviour) |
| ダイアログシステム連携 | 不要 (BaseDialogView や DialogCanvasView 側で必要に応じて本コンポーネントを併用) |

## 2. Requirements Feasibility Analysis

### 2.1 Requirement-to-Asset Map

| 要件 | 必要な技術要素 | 既存資産 | ギャップタグ |
|------|---------------|---------|-------------|
| Req 1: 自動フィット | `Screen.safeArea`、`RectTransform.anchorMin/anchorMax/offsetMin/offsetMax` | なし | **Missing** (新規実装) |
| Req 2: 画面状態追従 | Update polling (Screen.safeArea / width / height / orientation) | なし | **Missing** (新規実装) |
| Req 3: 適用辺の個別制御 | SerializeField bool フラグ × 4 + 条件適用ロジック | なし | **Missing** (新規実装) |
| Req 4: Render Mode 互換 | `Canvas.renderMode` 判定 | 全 Canvas が Overlay / Camera のみ使用 | **Constraint** (World Space は実機運用なし) |
| Req 5: エディタプレビュー | `[ExecuteAlways]` + Editor での Update 駆動 | プロジェクト内に `[ExecuteAlways]` 使用例なし | **Missing** (新規パターン導入) |
| Req 6: パフォーマンス / ログ衛生 | 差分検知ロジック + `[CanvasSafeAreaResizer]` プレフィックス | ログプレフィックス規約は確立済 (`tech.md`) | **Missing** (差分ロジックは新規) |

### 2.2 Complexity Signals

- **アルゴリズム**: 単純なベクトル正規化 (`safeArea / Screen.size`) のみ。複雑性なし
- **外部統合**: Unity 標準 API のみ (`Screen.safeArea`)
- **ワークフロー**: 単一フレームループ内での状態監視のみ
- **ステートフル度**: 直前値キャッシュのみ (内部 Vector2 / Rect)

→ **Simple Addition** に分類。

### 2.3 Research Needed (設計フェーズへ持ち越し)

1. **エディタプレビュー時の `Screen.safeArea` 値**: 通常の Game ビューは `Screen.safeArea = Screen.size` (フルレクト) を返す。Device Simulator パッケージ未導入時の挙動と、シミュレータ有効時の Editor プレビュー精度を design 段階で確認
2. **親 RectTransform が全画面でない場合の挙動**: 本コンポーネントが「親の rect 内で正規化アンカーを切る」ため、親が Canvas ルートではなく中間レイアウト要素 (例: HorizontalLayoutGroup の子) の場合に意図と異なる結果になる。設計段階で「親は Canvas 直下 or 全画面 RectTransform 限定」とするか「Canvas ルートを基準に再計算する」かを決定
3. **`[ExecuteAlways]` 中の Undo / Prefab 編集影響**: 編集中に anchor を毎フレーム上書きすることで Undo スタック汚染やプレファブ差分発生のリスク。設計段階で `OnValidate` + `Update` 駆動 vs. `Canvas.willRenderCanvases` フック等を比較
4. **VR / マルチディスプレイ**: 本プロジェクトでは無関係 (2D モバイル想定) ながら、`Display.displays` ではなく `Screen` 単独参照で問題ないことを明示
5. **CanvasScaler の Match モードと SafeArea 計算独立性**: `Screen.safeArea / Screen.size` はピクセル空間で完結するため CanvasScaler 設定に依存しないが、reference resolution が極端に異なる場合のサニティチェックを設計で記述

## 3. Implementation Approach Options

### Option A: 既存コンポーネントへの拡張

**該当なし**: SafeArea 関連の既存コンポーネントが存在しないため、拡張対象が不在。`DialogCanvasView` を拡張する案も考えられるが、責務 (Dialog 管理 ↔ SafeArea 適用) が異なるため不適。

### Option B: 新規コンポーネント作成 ✅ **推奨**

**配置**:
- ファイル: `Assets/Scripts/Root/View/CanvasSafeAreaResizer.cs`
- 名前空間: `Root.View`
- (任意) Editor: `Assets/Editor/CanvasSafeAreaResizerEditor.cs`

**責務境界**:
- 自身の RectTransform をセーフエリアに合わせる単一責務
- 子 RectTransform への伝播はしない (Unity の親子レイアウト機構が自動的に伝播)
- Service / State / Manager 層への依存は持たない

**統合ポイント**:
- 各シーンの Canvas 直下に「SafeAreaPanel」のような GameObject を作り、本コンポーネントをアタッチして UI 全体をその子に配置 (典型パターン)
- BaseDialogView / DialogCanvasView は本コンポーネントを **使う側** として併用可能 (内部実装は変更なし)

**Trade-offs**:
- ✅ 単一責務・他機能への影響ゼロ
- ✅ 既存パターン (`Root/View` の Pure MonoBehaviour) に完全適合
- ✅ 単独でテスト可能 (PlayMode テストで `Screen.safeArea` 値を差し替えて検証)
- ❌ 各シーンの既存 Canvas プレファブに「SafeAreaPanel」を追加する **シーン側の改修** が必要 (ただしコンポーネント自体の責務外)

### Option C: ハイブリッド (新規コンポーネント + DialogCanvasView 拡張)

**戦略**:
- 新規 `CanvasSafeAreaResizer` を作成
- 加えて `DialogCanvasView` に SafeArea 自動適用フックを追加 (例: `_dialogCanvas` 配下に SafeArea ノードを自動挿入)

**Trade-offs**:
- ✅ ダイアログは自動的に SafeArea 適用される (開発者の手間削減)
- ❌ Dialog Canvas のヒエラルキー構造を強制的に変更する副作用
- ❌ SafeArea を **適用したくない** ダイアログ (画面端の角丸などを意図的に超えて描画したい) の自由度を失う
- ❌ Requirement に「ダイアログ自動適用」が含まれていないため、過剰実装

→ **本フェーズでは不採用**。将来的にダイアログ自動適用が要件化された場合に再検討。

## 4. Implementation Complexity & Risk

| 指標 | 評価 | 一行根拠 |
|------|------|----------|
| Effort | **S (1–3 日)** | 単一 MonoBehaviour、Unity 標準 API のみ、既存パターン (View / RequireComponent / Reset) に乗る |
| Risk | **Low** | 既知パターン、外部統合なし、依存方向ルール (View → Service → State) を逸脱しない、テスト容易 |

## 5. Recommendations for Design Phase

### 推奨アプローチ
**Option B (新規コンポーネント作成)** を採用。`Assets/Scripts/Root/View/CanvasSafeAreaResizer.cs` (namespace `Root.View`) として 1 ファイル実装。Editor カスタマイズが必要なら `Assets/Editor/CanvasSafeAreaResizerEditor.cs` を追加。

### Key Decisions (設計時に決める項目)
1. **更新トリガ方式**: `Update()` 毎フレーム差分検知 vs. `Canvas.willRenderCanvases` イベント購読 vs. `OnRectTransformDimensionsChange` フック → **推奨**: Update() 差分検知 (シンプル・予測可能)
2. **親 RectTransform の前提**: 本コンポーネントが書き換えるアンカーは「親 RectTransform の正規化座標」。親は Canvas 直下 or 全画面 RectTransform を **要件として明示** する
3. **適用辺フラグの構造**: 4 つの bool (`_applyTop`, `_applyBottom`, `_applyLeft`, `_applyRight`) を SerializeField で公開 (デフォルト全て true)
4. **エディタプレビュー粒度**: `[ExecuteAlways]` + `void Update()` 内で `Application.isPlaying` 不問で実行。Undo 影響を最小化するため、変更がないフレームでは `RectTransform` を一切触らない (Req 6.1)
5. **API 公開範囲**: SerializeField 設定のみ。public メソッドは「強制再計算」用の `Apply()` 1 つに留める (テスト容易性)
6. **ログ出力**: 警告は初期化時 1 回のみ (毎フレーム警告で出さない)、`Debug.LogWarning($"[CanvasSafeAreaResizer] ...")` 形式で

### Research Items to Carry Forward (設計フェーズで決定)
- R1: Device Simulator 不在時の Editor プレビュー期待値 (Game ビューと一致 / 差異の許容)
- R2: 親 RectTransform 制約 (Canvas 直下限定 vs. 任意の全画面 RectTransform 許可)
- R3: `[ExecuteAlways]` での Undo / Prefab 差分発生リスク低減策
- R4: `OnValidate` 経由のフラグ変更時の即時反映方式

### Out of Scope (本機能に含めない)
- 子要素への個別 SafeArea 伝播 (子は親 RectTransform 経由で自動追従)
- 動的な `Canvas.renderMode` 変更を「リアルタイムで」検出する仕組み (Req 4.3 は次フレーム反映で十分)
- VContainer 登録 (Pure MonoBehaviour で完結)
- Addressables 経由のリソース取得 (静的コンポーネントのため不要)

## Next Steps

設計フェーズへ進む準備が整いました。Option B (新規コンポーネント作成) の前提で `design.md` を生成します。

```
# 設計フェーズへ進む (要件承認込みで自動進行)
/kiro:spec-design canvas-safe-area-resizer -y
```

要件側に修正が必要な場合は `/kiro:spec-requirements canvas-safe-area-resizer` で再生成してください。
