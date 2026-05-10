# Research & Design Decisions: canvas-safe-area-resizer

---
**Purpose**: 設計フェーズで参照する技術調査・意思決定の根拠を集約し、`design.md` の決定背景をトレース可能にする。

**Usage**:
- gap-analysis.md で抽出した Research Items (R1〜R4) の追加調査結果を記録
- 設計の主要 Trade-off (Update 方式 / 親 RT 制約 / Editor プレビュー戦略) を確定
- 実装フェーズで再参照する技術判断のソースを明示
---

## Summary

- **Feature**: `canvas-safe-area-resizer`
- **Discovery Scope**: New Feature (greenfield コンポーネント) / Simple Addition
- **Discovery Process**: Light Discovery (`.kiro/settings/rules/design-discovery-light.md`)
- **Key Findings**:
  1. Unity 標準 API `Screen.safeArea` (Rect, ピクセル単位) を `Screen.width / Screen.height` で正規化することで、CanvasScaler 設定や RenderMode (Overlay / Camera) に依存しない単一ロジックでセーフエリアアンカーを算出できる。
  2. 親 RectTransform は **Canvas 直下、または Canvas 全体を埋める RectTransform** であることを設計上の前提として明示する。Unity の anchorMin/Max は親 rect 内の正規化座標であり、親が全画面でない場合は計算結果が崩れるため、コンポーネント側で複雑な逆算をするより前提を明確化する方が単純で堅牢。
  3. 既存コードベースには `[ExecuteAlways]` 使用例なし。新規導入になるが、anchor を「変化があった場合のみ書き換える」差分検知で Undo/Prefab 差分汚染を最小化できる (Req 6.1 と整合)。
  4. Game ビューのセーフエリアは Unity の Device Simulator (`com.unity.device-simulator.devices` 系) を有効化したときのみ実機相当の `Screen.safeArea` が返る。本コンポーネントは API 呼び出し側に責任を委ね、独自にシミュレートしない。

## Research Log

### R1: Editor 実行時の `Screen.safeArea` 動作
- **Context**: Req 5「エディタでのプレビュー」を満たすには、Game ビューでセーフエリアの形状を確認できる必要がある。Editor 実行時に `Screen.safeArea` がどの値を返すかを確定したい。
- **Sources Consulted**:
  - Unity ScriptReference: [Screen.safeArea](https://docs.unity3d.com/ScriptReference/Screen-safeArea.html)
  - Unity Manual: Device Simulator (Game View モード切替)
  - Unity フォーラム/Issue Tracker (Screen.safeArea returns full screen in editor without simulator)
- **Findings**:
  - 通常の Game ビュー: `Screen.safeArea == new Rect(0, 0, Screen.width, Screen.height)` (フル領域)
  - Device Simulator (Window > General > Device Simulator) でデバイスを選択時: 実機相当のセーフエリア値が返る
  - Editor 実行時 (非 Play モード) も `Screen.width / height` は Game ビューの解像度を返し、`safeArea` は同様のルールに従う
- **Implications**:
  - 本コンポーネントは「`Screen.safeArea` が返した値をそのまま正規化する」責務に専念し、Editor 表示の正確さは Unity 側 (Device Simulator) に委ねる
  - Editor で開発者がデバイス別レイアウトを確認したい場合は Device Simulator を有効化する旨をドキュメント (XML コメントまたは README) に明記する

### R2: 親 RectTransform 制約
- **Context**: 本コンポーネントは `transform.anchorMin/anchorMax` を Screen 正規化値に書き換える。アンカー値は親 RectTransform の rect 内の正規化座標であるため、親が全画面でないと結果がずれる。設計でどう扱うかを決める必要がある。
- **Sources Consulted**:
  - Unity ScriptReference: [RectTransform.anchorMin](https://docs.unity3d.com/ScriptReference/RectTransform-anchorMin.html)
  - Unity Manual: Basic Layout / Anchoring
- **Findings**:
  - `anchorMin/Max` は常に親 RectTransform の rect 内の `[0,1]` 正規化座標
  - 親が Canvas 直下なら親 rect == Canvas rect == Screen rect (Overlay/Camera 共通の挙動)
  - 親が中間レイアウト要素 (例: HorizontalLayoutGroup の子) の場合、親 rect は画面サイズと異なるため、Screen ベースの正規化値を素直に書き込むと意図しない結果になる
  - 「Canvas ルート基準で計算して中間 RectTransform を経由して逆算する」案は実装複雑度・パフォーマンスコストが大きい
- **Implications**:
  - **設計決定**: 本コンポーネントの **使用前提** として「直近の親 RectTransform は Canvas root もしくは Canvas を完全に埋める RectTransform であること」を明示する
  - 違反時のランタイム検知は行わない (親の充填判定は厳密にやるとコスト高で誤検知も多い)
  - 典型運用パターン: `Canvas (Screen Space - Camera/Overlay)` 直下に `SafeAreaPanel` という空 RectTransform を配置し、本コンポーネントをアタッチ。実 UI はその子に配置する
  - `OnTransformParentChanged` フックで親が Canvas 直下でない場合に警告ログを出す軽量チェックは検討余地あり (実装コスト低)

### R3: `[ExecuteAlways]` と Undo / Prefab 差分対策
- **Context**: Editor 編集中に毎フレーム anchor を書き換えると、Undo スタック汚染やプレファブの差分が発生する懸念がある。
- **Sources Consulted**:
  - Unity ScriptReference: [ExecuteAlways](https://docs.unity3d.com/ScriptReference/ExecuteAlways.html)
  - Unity ScriptReference: [Undo.RecordObject](https://docs.unity3d.com/ScriptReference/Undo.RecordObject.html)
  - Unity フォーラム議論: SafeArea component pattern (Mininum78氏 / TheCelt 氏らの一般実装)
- **Findings**:
  - `[ExecuteAlways]` は Edit/Play 両モードで `Awake/OnEnable/Update/OnDisable` を呼ぶ
  - 値変更時のみ書き換えれば Undo/Prefab 差分は最低限に抑えられる
  - エディタでの書き換えに `Undo.RecordObject` を介さない場合、シリアライズ済みの値変更が「ダーティ未マーク」状態になる可能性 → `RectTransform` のアンカーは PrefabUtility/Undo を介さなくても次回シーン保存時に自動的にダーティ化される (Unity の標準挙動)
  - 過度な `Undo.RecordObject` の使用は Undo 履歴汚染の原因になるため避ける
- **Implications**:
  - **設計決定**: 値が前回適用時から変化した時のみ `RectTransform` を書き換える (差分検知ガード)
  - `Undo.RecordObject` は使用しない (毎フレーム判定のため Undo 履歴汚染を避ける)
  - Editor 操作中にユーザが手動でアンカーを編集した場合は、本コンポーネントが次フレームで上書きする (Req 5.2 の通り)。この挙動はドキュメントで明示

### R4: 設定値変更時の即時反映 (`OnValidate`)
- **Context**: Inspector で適用辺フラグ (`_applyTop` 等) を切り替えた瞬間に Editor プレビューに反映したい。
- **Sources Consulted**:
  - Unity ScriptReference: [MonoBehaviour.OnValidate](https://docs.unity3d.com/ScriptReference/MonoBehaviour.OnValidate.html)
- **Findings**:
  - `OnValidate` は Inspector 値変更時・スクリプトリロード時に呼ばれる (Edit Mode 含む)
  - `OnValidate` 内で `RectTransform` を直接変更するのは非推奨 (シリアライゼーションタイミングの都合) → ダーティフラグだけ立てて次の Update で適用する
- **Implications**:
  - **設計決定**: `OnValidate()` で `_isDirty = true` をセット、`Update()` で `_isDirty || HasScreenChanged()` を判定して書き換え

## Architecture Pattern Evaluation

| Option | Description | Strengths | Risks / Limitations | Notes |
|--------|-------------|-----------|---------------------|-------|
| **A. Update polling (採用)** | `Update()` で毎フレーム `Screen.safeArea` 等の差分を検知し、変化時のみ書き換え | 実装単純、`[ExecuteAlways]` と相性良、外部依存なし | 毎フレームの値比較コスト (Vector 比較数回のみ、無視できる) | Unity 標準 SafeArea コンポーネントの一般的実装パターン |
| B. `Canvas.willRenderCanvases` 購読 | Canvas 描画前イベントで再計算 | 描画と同期、Update 不要 | Canvas 単位の購読が必要、ライフサイクル管理が複雑、Edit Mode でのイベント発火タイミング不安定 | 本ユースケースで利得が薄い |
| C. `OnRectTransformDimensionsChange` | 親 RT サイズ変更時のみ反応 | イベント駆動で省コスト | `Screen.safeArea` の変化はトリガしない (画面回転やノッチ変化を取りこぼす) | 完全性に欠ける |

→ **採用: Option A** (Update polling + 差分ガード)

## Design Decisions

### Decision: 1 ファイル Pure MonoBehaviour 構成
- **Context**: 単一責務の View ユーティリティで、State / Service への依存も他コンポーネントへの拡張ポイントも持たない。
- **Alternatives Considered**:
  1. Pure MonoBehaviour 1 ファイル
  2. ICanvasResizer インターフェース + 実装クラス (将来的な差し替えを意識)
- **Selected Approach**: 案 1 (Pure MonoBehaviour `CanvasSafeAreaResizer`)
- **Rationale**: 本機能で差し替えバリアントを作る計画はなく、抽象化はオーバーエンジニアリング。プロジェクトの既存 View (`BackdropView`, `DialogCanvasView`) も同様にインターフェースを切らず Pure MonoBehaviour で構成。
- **Trade-offs**: ✅ シンプル、テスト容易 / ❌ 差し替え容易性なし (要件外なので問題なし)
- **Follow-up**: なし

### Decision: 配置先 `Assets/Scripts/Root/View/`
- **Context**: 全シーンで再利用可能な UI コンポーネント。
- **Alternatives Considered**:
  1. `Assets/Scripts/Root/View/CanvasSafeAreaResizer.cs` (namespace `Root.View`)
  2. `Assets/Scripts/Utils/CanvasSafeAreaResizer.cs` (namespace `Cat`)
- **Selected Approach**: 案 1
- **Rationale**: `structure.md` の「Root Services は全シーン共通のグローバルサービス」「シーンと同じ層構造 (Service/State/View)」に準拠。`Utils` は静的定数 (Const.cs) 用途のため、MonoBehaviour 配置先として不適。
- **Trade-offs**: ✅ 既存規約に準拠、namespace で意図が伝わる / ❌ なし
- **Follow-up**: なし

### Decision: Update polling + 差分ガード方式
- **Context**: 画面状態 (safeArea / size / orientation / 設定フラグ) の変化を検知して `RectTransform` を書き換える方式の選定。
- **Alternatives Considered**: 上記 Architecture Pattern Evaluation 表参照
- **Selected Approach**: `Update()` で `Screen.safeArea`・`Screen.width`・`Screen.height`・`Screen.orientation`・`_isDirty` フラグを評価し、いずれかに変化があった場合のみ再計算と `RectTransform` 書き換えを実施
- **Rationale**: 実装単純、`[ExecuteAlways]` と相性良、Edit Mode でも安定動作、毎フレーム比較コストは無視できる規模 (Rect 1 個 + Vector2 1 個 + enum 1 個 + bool 1 個)
- **Trade-offs**: ✅ 単純で安定 / ❌ 毎フレーム比較が走る (非常に軽量だが厳密にはイベント駆動より高コスト)
- **Follow-up**: 差分なしフレームでの `RectTransform` 触らない動作を PlayMode テストで検証

### Decision: 親 RectTransform は「Canvas 全体を埋める RT」を前提
- **Context**: anchorMin/Max は親 rect 内の正規化値。任意の親で正しく動かすには複雑な逆算が必要だが、ユースケース (Canvas 直下の SafeArea パネル配置) には不要。
- **Alternatives Considered**:
  1. 「親は Canvas 全体を埋める RT 限定」と明示
  2. Canvas root を `GetComponentInParent<Canvas>()` で取得し、その rect を基準に逆算
- **Selected Approach**: 案 1
- **Rationale**: 案 2 は実装コストが高く、Unity の親子レイアウト機構と二重に座標変換するため不直感的。標準的な「Canvas → SafeAreaPanel → 各 UI」運用で困らない。
- **Trade-offs**: ✅ シンプル、副作用なし / ❌ 中間レイアウト要素の子で使えない (要件外)
- **Follow-up**: XML コメントで前提を明示。違反時の警告は Phase 2 (taskで決定)

### Decision: 適用辺は SerializeField bool ×4 で公開
- **Context**: 上下左右の各辺ごとに ON/OFF 制御が必要 (Req 3)。
- **Alternatives Considered**:
  1. `bool _applyTop, _applyBottom, _applyLeft, _applyRight` の 4 フィールド
  2. `[Flags] enum SafeAreaEdges` 1 フィールド
- **Selected Approach**: 案 1
- **Rationale**: Inspector でのチェックボックス UI が直感的、デフォルト値 (全 true) のシリアライズも自然。Flags enum は表示が冗長になる。
- **Trade-offs**: ✅ Inspector が直感的 / ❌ フィールド数が増えるが 4 個なので問題なし
- **Follow-up**: なし

### Decision: World Space Canvas は対象外、警告ログ
- **Context**: Req 4.2 で World Space は警告ログ + 処理スキップと定義済み。`Screen.safeArea` は World Space Canvas に対して意味を持たない (画面ピクセル ↔ 3D ワールドの直接対応がない)。
- **Alternatives Considered**:
  1. 警告ログ + 処理スキップ (要件通り)
  2. World Space を強制的に Camera 相当扱い (要件外、誤動作の温床)
- **Selected Approach**: 案 1
- **Rationale**: 本プロジェクトに World Space Canvas は存在せず、対応コストに見合うメリットがない。要件にも合致。
- **Trade-offs**: ✅ 単純 / ❌ World Space 対応が必要になったら別途設計
- **Follow-up**: なし

## Risks & Mitigations

- **Risk 1**: Editor 実行中に毎フレーム `RectTransform` を触ることによる Undo 履歴汚染や Prefab 差分発生
  - **Mitigation**: 差分ガード (前回適用値と一致なら何もしない)、`Undo.RecordObject` を使わない、ドキュメントで動作を明示
- **Risk 2**: 親 RectTransform が全画面でない場合に意図しないアンカー値が書き込まれる
  - **Mitigation**: XML コメントで前提を明示、典型運用パターン (Canvas 直下に SafeAreaPanel) を README/コメントに記載
- **Risk 3**: Device Simulator 未利用時の Editor プレビューでセーフエリアが画面全体扱いとなり、開発者が「コンポーネントが動いていない」と誤認する
  - **Mitigation**: Inspector へのヘルプテキスト (HelpURL or HelpBox) で Device Simulator の利用を案内 (Phase 2 で検討)、もしくは XML コメントで明記
- **Risk 4**: `Screen.orientation` の値遷移が `AutoRotation` モードで不安定な場合がある
  - **Mitigation**: `Screen.safeArea`・`Screen.width/height` の変化検知が常時走っているため、orientation を取りこぼしても他の経路で追従する (二重防御)

## References

- [Unity ScriptReference: Screen.safeArea](https://docs.unity3d.com/ScriptReference/Screen-safeArea.html) — セーフエリア API の戻り値仕様
- [Unity ScriptReference: RectTransform.anchorMin / anchorMax](https://docs.unity3d.com/ScriptReference/RectTransform-anchorMin.html) — アンカー座標系の定義
- [Unity ScriptReference: ExecuteAlways](https://docs.unity3d.com/ScriptReference/ExecuteAlways.html) — Edit/Play 両モード実行属性
- [Unity ScriptReference: MonoBehaviour.OnValidate](https://docs.unity3d.com/ScriptReference/MonoBehaviour.OnValidate.html) — Inspector 値変更フック
- [Unity Manual: Device Simulator](https://docs.unity3d.com/Manual/device-simulator.html) — Editor でのセーフエリアシミュレーション手段
- プロジェクト内 `.kiro/steering/structure.md` — Root/View 配置規約
- プロジェクト内 `.kiro/steering/tech.md` — `#nullable enable`, `Debug.LogError($"[ClassName] ...")` 規約
- プロジェクト内 `.kiro/specs/canvas-safe-area-resizer/gap-analysis.md` — 既存資産・配置先・Effort/Risk 評価
