# Research & Design Decisions

## Summary
- **Feature**: `furniture-placement-grid-lines`
- **Discovery Scope**: Extension (light discovery)
- **Key Findings**:
  - 描画順の実測値: 部屋の背景 (Base プレハブ) は `sortingOrder -47 / -46`、床家具は `SortingGroup 0`、家具上家具は `(x+y)*1000+x` の正値。2D Renderer は `TransparencySortMode = CustomAxis (0,1,0)`。グリッド線は `-10`、床・壁の予告面は `-9` で背景と家具の間に確定的に入る
  - 家具上グリッド (`FragmentedIsoGrid`) の予告は、親家具の `SortingGroup` 内で親スプライト (order 0) と子家具 (order ≥ 0) の間に整数の空きがない。`FragmentedIsoGrid` に order 1 の `SortingGroup` を挟んで「面グループ」を作り、その中で予告を -1 に置く (Codex 相談の結論)
  - 予告の可否判定と離した時の判定を構造的に一致させるため、`IsoDragService` がドラッグ中に毎フレーム「落下先 (`DropTarget`)」を解決して保持し、離した時も同じ値で配置する
  - ランタイム描画は手続きメッシュ 1 枚 + `Sprite-Unlit-Default` (URP 2D) + 頂点カラーで足りる。線メッシュは面ごとに一度だけ生成し、予告メッシュだけ毎フレーム再構築する

## Research Log

### 描画順の実測 (sortingLayer / sortingOrder)
- **Context**: 要件 3.1 / 6.12 (背景より手前、家具より奥) の具体値が不明だった
- **Sources Consulted**: `Assets/Arts/Furniture/Furnitures/Base/*.prefab`, `Floor/Bed/Bed.prefab`, `Assets/Settings/Renderer2D.asset`, `ProjectSettings/TagManager.asset`, `IsoDraggableView.CalculateFragmentedSortingOrder`
- **Findings**:
  - Sorting Layer は `Default` と `UI` の 2 つのみ。ゲーム内オブジェクトはすべて `Default`
  - Base (床・壁の背景) は `-47` (BaseA〜C) / `-46` (BaseD)
  - 床家具プレハブはルートに `SortingGroup (order 0)`、子 `SpriteRenderer` は `order 0`、`SpriteSortPoint = Center`
  - 家具上家具は `FragmentedIsoGrid` の Transform 直下に再親子付けされ、親の `SortingGroup` 内で `(frontCell.x + frontCell.y) * 1000 + frontCell.x` の order を持つ (最小 0)
  - `Renderer2D.asset` の `m_TransparencySortMode: 3 (CustomAxis)`、軸 `(0,1,0)`。同一 order 内は Y 座標でソートされる
- **Implications**: 床・壁のグリッド線 `-10`、床・壁の予告面 `-9` で背景 (-47) と家具 (0 以上) の間に確定的に入る。家具上グリッドの予告は親 `SortingGroup` の内側で解決する必要がある (下記の決定を参照)

### `SortingGroup` 内の同順位タイブレーク
- **Context**: 家具上グリッドで、予告を親スプライト (0) と子家具 (0 以上) の間に置く手段。Y ソートのタイブレーク (`order 0` で並べて Y で勝たせる) が使えるか
- **Sources Consulted**: Codex (gpt-6-astra / medium) への相談、[Unity Sorting Group docs](https://docs.unity3d.com/6000.0/Documentation/Manual/sprite/sorting-group/sort-renderers-within-sorting-group.html)
- **Findings**:
  - `SortingGroup` 内の子 Renderer は Sorting Layer → Order in Layer で並び、同順位のタイブレークはカメラ距離の扱いが個別 Renderer と異なり信頼できない (Codex の指摘)。現行の家具上家具が order 0 で親スプライトと同順位になっているケースも、たまたま成立している可能性がある
  - 解決策は `FragmentedIsoGrid` の GameObject に `SortingGroup (order 1)` を追加して「面グループ」を作ること。親スプライト (0) < 面グループ (1) となり、面グループ内で予告 (-1) < 子家具 (0 以上) が確定する
  - `sortAtRoot` は無効のまま (親グループに従属させる)
- **Implications**: `FragmentedIsoGrid` に `SortingGroup` を追加する。プレハブ全数編集を避けるため `Awake` で `AddComponent` する (`RequireComponent` はエディタで付け直しが要るため不採用)。既存の家具上家具の描画順が「常に親より手前」に変わるため、実機での目視確認をタスクに含める

### 予告判定と実配置判定の一致 (要件 6.10)
- **Context**: 予告の色と実際に置けるかが食い違うと UX が破綻する
- **Sources Consulted**: `IsoDragService.EndFloorDrag / EndWallDrag / UpdateDragSortingOrder`、Codex の指摘 (「preview and release must call the same evaluator」)
- **Findings**:
  - 現行はドラッグ中 (`UpdateDragSortingOrder`) と離した時 (`EndFloorDrag`) が別々に Raycast と `CanPlace*` を呼んでおり、同じ計算が 2 か所に書かれている
  - 「落下先」を値型 `DropTarget` (面の種別・壁の側・家具上グリッド参照・フットプリント開始位置・配置可否) として毎フレーム解決し、`IsoDragService` が保持すれば、予告も配置も同一の値を参照できる
- **Implications**: `IsoDragService` に `ResolveDropTarget` を導入して 2 か所の重複を 1 つに寄せる。`GridPreviewService` は `DropTarget` を受け取るだけで自前の判定を持たない

### ランタイム描画手段
- **Context**: プロジェクトに `LineRenderer` / `Tilemap` / 自作シェーダ / 手続きメッシュの実績がない
- **Sources Consulted**: Codex 相談、`Library/PackageCache/com.unity.render-pipelines.universal@*/Runtime/Materials/Sprite-Unlit-Default.mat` の存在確認
- **Findings**:
  - 手続きメッシュ (`MeshFilter` + `MeshRenderer`、線は細長い四角形、セルは四角形、頂点カラー) が、線の太さ調整 (要件 4.2) とセル単位の色分け (要件 6.3 / 6.4) を 1 系統で満たす
  - `LineRenderer` は線ごとに GameObject が必要 (床だけで 66 本) で、セル面には別系統が要る。フルスクリーンシェーダは有限グリッド・家具上面・占有状態の表現が難しい
  - `Universal Render Pipeline/2D/Sprite-Unlit-Default` は頂点カラー・透過・`ZWrite Off` に対応。マテリアルアセットを Inspector で参照させ、ビルドにシェーダを含める
  - `MeshRenderer` には `SpriteSortPoint` がないため、order の一致に頼らない設計にする (上記の -10 / -9 / 面グループで解決済み)
  - 線メッシュは面ごと (床 / 左壁 / 右壁) に一度生成してキャッシュし、予告メッシュ (最大でも十数枚の四角形) だけを毎フレーム再構築する。`Mesh.MarkDynamic`、頂点・色・インデックスのバッファ再利用、`SetVertices` 系の NoAlloc API を使う
- **Implications**: `GridPreviewView` が線用と予告用の 2 つの `MeshRenderer` を持つ。線用は面ごとの表示切替のみ、予告用は毎フレーム更新

### View 未配置時の縮退 (要件 4.3)
- **Context**: VContainer は解決できないコンストラクタ引数で例外を投げる。`[SerializeField]` 未設定の View を `RegisterComponent` すると起動時に落ちる
- **Sources Consulted**: `structure.md` (Scene View への注入 3 経路)、`HomeScope.cs`、`Home.unity` の `autoInjectGameObjects`
- **Findings**: 「自分で完結する View」の経路 (`autoInjectGameObjects` + `[Inject] Init`) を使い、View 側が Service に自分を登録する形にすれば、View が無ければ登録が起きないだけで Service は no-op になる。依存方向 View → Service は許可されている
- **Implications**: `GridPreviewService` は View を nullable で保持し、`AttachView` で受け取る。`GridPreviewView` は `HomeScope` の `autoInjectGameObjects` に登録する

## Architecture Pattern Evaluation

| Option | Description | Strengths | Risks / Limitations | Notes |
|--------|-------------|-----------|---------------------|-------|
| A: `IsoDragService` 拡張 | ドラッグサービスに描画状態を持たせる | ファイル最小 | 400 行のサービスに描画責務が混ざる。`IsoGridGizmo` は `OnDrawGizmos` 依存で転用不可 | 不採用 |
| B: 新規 Service + View | `GridPreviewService` / `GridPreviewView` を新設し、既存の 3 フックパターンで接続 | `RedecorateCameraService` / `FurnitureStowService` と同型。描画を View に閉じ込められる | 新規ファイル 2〜3 | **採用** |
| C: B + 純粋ロジックアセンブリ | セル列挙と可否判定を `Home/GridPreviewLogic/` に切り出す | 要件 6.4〜6.6 の境界値を EditMode でテストできる | asmdef 管理が増える | **採用** (要件 6 を含むため) |

## Design Decisions

### Decision: 落下先の解決を `IsoDragService` に一本化する
- **Context**: 予告と実配置で判定が食い違ってはならない (6.10)
- **Alternatives Considered**:
  1. `GridPreviewService` が独自に Raycast と `CanPlace*` を呼ぶ
  2. `IsoDragService` が毎フレーム `DropTarget` を解決して保持し、予告にも配置にも使う
- **Selected Approach**: 2
- **Rationale**: 現行の `UpdateDragSortingOrder` と `EndFloorDrag` の重複が解消され、判定の一致が構造的に保証される
- **Trade-offs**: `IsoDragService` に小さなリファクタが入る。テストは EditMode で書けない (Raycast 依存) ため、判定の純粋部分は Logic アセンブリで担保する
- **Follow-up**: 壁ドラッグの `WallSide` 切替 (`x < 0` で反転) が `DropTarget` の解決より前に走る順序を維持する

### Decision: 家具上グリッドに面 `SortingGroup` を挿入する
- **Context**: 親スプライト (0) と子家具 (≥0) の間に予告を割り込ませる整数 order がない
- **Alternatives Considered**:
  1. 予告を order 0 で子にして Y ソートのタイブレークに任せる
  2. 全家具プレハブの親スプライトを order -1 に変える
  3. `FragmentedIsoGrid` に `SortingGroup (order 1)` を `Awake` で追加し、その中で予告を -1、子家具は既存 order のまま
- **Selected Approach**: 3
- **Rationale**: プレハブを触らず、タイブレークにも頼らない。子家具の order 計算 (`CalculateFragmentedSortingOrder`) はそのまま使える
- **Trade-offs**: 既存の家具上家具が「常に親スプライトより手前」に固定される。現状もそう見えているはずだが、実機で全家具を目視確認する
- **Follow-up**: `FragmentedIsoGrid` が `SortingGroup` を持つプレハブが将来現れた場合は `AddComponent` をスキップする (`GetComponent` で存在確認)

### Decision: 描画は手続きメッシュ 2 枚 (線 / 予告)
- **Context**: 線の太さ・色と、セル単位の色分けを 1 つの View で扱いたい
- **Alternatives Considered**: `LineRenderer` 群、セルごとの `SpriteRenderer`、フルスクリーンシェーダ
- **Selected Approach**: `GridPreviewView` 配下に `GridLinesRenderer` (床・左壁・右壁の 3 メッシュをキャッシュ) と `FootprintPreviewRenderer` (毎フレーム再構築) の 2 renderer
- **Rationale**: draw call 最小、Inspector で色・太さ・order を調整可能、URP 2D 標準マテリアルで完結
- **Trade-offs**: 頂点生成コードを自前で書く。線の太さはワールド単位 (ズームで見た目が変わる)
- **Follow-up**: 実機でマテリアル (`Sprite-Unlit-Default`) が 2D Renderer で透過描画されることを確認

### Decision: View は `autoInjectGameObjects` 経由で Service に自己登録する
- **Context**: 要件 4.3 (View 未配置でもドラッグは動く)
- **Selected Approach**: `GridPreviewView.[Inject] Init(GridPreviewService)` が `service.AttachView(this)` を呼ぶ。Service は View を nullable で保持し、`== null` なら描画をスキップ
- **Rationale**: `structure.md` の既存経路そのもの。VContainer の必須解決に引っかからない
- **Trade-offs**: `HomeScope` の Inspector 設定 (`autoInjectGameObjects`) に依存する。設定漏れは無言で no-op になる
- **Follow-up**: `GridPreviewService` は View 未登録なら `Debug.LogWarning` を一度だけ出す

## Risks & Mitigations
- 面 `SortingGroup` 追加で既存の家具上家具の描画が変わる — 実機・Editor で全家具の目視確認をタスク化。問題があれば `FragmentedIsoGrid` 側の order を 0 にして予告のみ -1 → 0 の間を再検討
- `Sprite-Unlit-Default` が実機で期待どおり透過しない — 実機確認をタスク化。代替は `Sprites/Default` (Built-in) を URP 2D で使用
- `MeshRenderer` の bounds がカメラのカリングに影響 — 予告メッシュ更新時に `RecalculateBounds` を必ず呼ぶ
- `(x+y)*1000+x` の order は大きな家具上グリッドで 32767 を超え得る — 現行最大 4x3 では 5003 で問題なし。本機能では触らない

## References
- [Unity Sorting Group](https://docs.unity3d.com/6000.0/Documentation/Manual/sprite/sorting-group/sort-renderers-within-sorting-group.html) — グループ内ソートの規則
- [Renderer.sortingOrder](https://docs.unity3d.com/6000.0/Documentation/ScriptReference/Renderer-sortingOrder.html) — 範囲 -32768〜32767
- `Assets/Settings/Renderer2D.asset` — `TransparencySortMode = CustomAxis (0,1,0)`
- `.kiro/specs/furniture-placement-grid-lines/gap-analysis.md` — 既存資産と要件のマップ
- Codex (gpt-6-astra / medium) 相談ログ (2026-09-05) — 描画手段・面 `SortingGroup`・評価器一本化の助言

### Decision: 家具上グリッドで配置不可の時は床にフォールスルーする (設計レビュー 2026-09-05)
- **Context**: Bed 上の占有セルにかざした時、予告を Bed 上に赤で出すか、床に落として床の予告を赤にするか
- **Alternatives Considered**:
  1. 現行どおり床にフォールスルーし、床の予告を赤にする
  2. Raycast ヒット時は家具上に固定し、`CanPlace = false` なら Bed 上に赤を出す (離した時の床配置試行が無くなる)
- **Selected Approach**: 1 (暫定)
- **Rationale**: 既存挙動を変えず、6.10 の一致を保てる
- **Follow-up**: 実装後に実際の見え方を確認し、違和感が強ければ 2 を再検討する
