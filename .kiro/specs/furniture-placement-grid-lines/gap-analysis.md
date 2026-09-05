# Gap Analysis: 家具設置時のグリッド線表示

## 1. 現状調査 (Current State)

### 関連アセット

| 対象 | パス | 役割 |
|---|---|---|
| `IsoGridSettingsView` | `Home/View/` | グリッド原点・幅・高さ・壁高さの SSOT。`CellSize = 0.4885f` / `Angle = 15.5f` は `const` |
| `IsoGridService` | `Home/Service/` | 座標変換 (`FloorGridToWorld` / `WallGridToWorld`) と配置可否判定 (`CanPlaceFloorObject` / `CanPlaceWallObject` / `CanPlaceFragmentedObject`) |
| `IsoGridState` | `Home/State/` | `Floor` / `LeftWall` / `RightWall` / `FragmentedGrids` の `GridEntry` を保持。`GridEntry.Cells` は `int[,]` で占有 `UserFurnitureId` を保持 |
| `IsoDragService` | `Home/Service/` | ドラッグのライフサイクル。`HandlePointerDown` → `BeginDrag`、`HandlePointerDrag`、`HandlePointerUp` → `EndDrag` |
| `IsoInputService` | `Home/Service/` | `ITickable`。`HomeState.State.Redecorate` のときのみ入力を発行。Redecorate 離脱時にドラッグ中なら `OnPointerUp` を強制発行する |
| `IsoDraggableView` | `Home/View/` | `FootprintSize` / `PivotGridPosition` / `IsWallPlacement` / `WallSide` / `UserFurnitureId` を公開。`SortingGroup.sortingOrder` を操作 |
| `FragmentedIsoGrid` | `Home/View/` | 家具上グリッド。`LocalGridToWorld` / `WorldToLocalGrid` / `IsValidLocalPosition` を公開 |
| `IsoGridGizmo` | `Home/View/` | **Editor 専用** (`#if UNITY_EDITOR` + `OnDrawGizmos`)。床・左壁・右壁の線描画ロジックの参考実装 |

### 抽出した規約

- 依存方向は `View → Service → State` の一方向のみ。Service は `IsoGridState` を直接注入して読める
- ドラッグ付随機能は「Service に `OnFurnitureDragMove` / `OnFurnitureDragEnd` を生やし、`IsoDragService` から呼ぶ」形で既に2例ある (`RedecorateCameraService`, `FurnitureStowService`)。**本機能もこの確立済みパターンにそのまま乗る**
- 純粋ロジックは `{Scene}/{Feature}Logic/` の `noEngineReferences` アセンブリに切り出し、EditMode テストを同居させる (structure.md)
- Sorting Layer は `Default` と `UI` の2つのみ。床家具は配置時に `sortingOrder = 0`、家具上家具は `CalculateFragmentedSortingOrder` の正値

### 統合面

- ドラッグ開始/移動/終了の3フック (`IsoDragService`)
- 配置可否判定 (`IsoGridService.CanPlace*`) — 要件 6.10 の「実配置と同一判定」はこれを呼ぶだけで満たせる
- セル単位の占有情報 (`IsoGridState.FragmentedGrids` / `GridEntry.Cells` は public、`IsValidFloorPosition` / `GetFloorUserFurnitureId` / `IsValidWallPosition` / `GetWallUserFurnitureId` も public)

## 2. 要件 → 資産マップ

| 要件 | 既存資産 | ギャップ |
|---|---|---|
| 1. ドラッグ中のグリッド線表示 | `IsoDragService` のフック、`IsoGridSettingsView` の寸法 | **Missing**: ランタイム線描画の手段が皆無 (`LineRenderer` / `Tilemap` / 手続きメッシュの使用実績がプロジェクト全体でゼロ)。`IsoGridGizmo` は Editor 専用で実機に出ない |
| 1.4 グリッド定義との一致 | `IsoGridGizmo` の描画式 (`xAxis` / `yAxis` / `zAxis`) | **Constraint**: 同じ式が Gizmo とランタイムに二重化する。共通化するか割り切るかの判断が要る |
| 2. 配置面に応じた切り替え | `IsoDraggableView.IsWallPlacement`, `RaycastForFragmentedGrid` | ギャップ小。要件 2.3 (家具上家具) は床グリッド表示なので分岐は床/壁の2値で足りる |
| 3.1 / 6.12 描画順 | `SortingGroup`、床家具 `sortingOrder = 0` | **Unknown**: 部屋の背景 (床・壁スプライト) の sortingOrder が未確認。線と予告を負値のどこに置くかは実物確認が必要 |
| 3.2 入力を消費しない | `TapEffectView` の前例 (`raycastTarget = false`、`GraphicRaycaster` なし) | ギャップなし。Collider を付けなければ `IsoDragService` の Raycast にも掛からない |
| 4. Inspector 調整 | View 層の `[SerializeField]` 慣習 | 線の太さは描画手段に依存 (下記オプション参照) |
| 4.3 View 未配置でも動く | — | **Missing**: 現状 `IsoDragService` は必須依存しか持たない。任意依存にするか、空実装で吸収するかの設計判断 |
| 5.2 モード離脱時に残らない | `IsoInputService.OnStateChange` がドラッグ中の離脱で `OnPointerUp` を強制発行 | **ギャップなし**。既存機構で自動的に満たされる |
| 6.1〜6.4 予告表示と赤色化 | `CanPlace*` | **Missing**: セル面の塗り描画手段。判定自体は既存 API で足りる |
| 6.5 範囲外セルは描かない (部分はみ出し) | `IsValidFloorPosition` / `IsValidWallPosition` / `FragmentedIsoGrid.IsValidLocalPosition` が public | **Missing**: `CanPlace*` は bool のみでセル単位の内訳を返さない。ただし上記 public API でセル単位判定を新サービス側で再構成でき、`IsoGridService` の改修は不要 |
| 6.7 家具上グリッドへの予告 | `RaycastForFragmentedGrid`、`FragmentedIsoGrid.LocalGridToWorld` | `RaycastForFragmentedGrid` は `IsoDragService` の private。切り出すか、判定結果を引数で渡すかの判断が要る |
| 6.8 壁への予告 | `WorldToWallGrid` / `WallGridToWorld` | ギャップ小。壁は `WallSide` 切替がドラッグ中に走るため追従が要る |

### 複雑度シグナル

アルゴリズム的ロジック (アイソメトリック座標の線分・セル頂点生成) + レンダリング統合。外部連携なし、永続化なし、CRUD なし。

## 3. 実装アプローチ (Options)

### Option A: 既存コンポーネントの拡張

`IsoDragService` に描画状態を持たせ、`IsoGridGizmo` からランタイム描画へ条件コンパイルを外して転用する。

- ✅ 新規ファイル最小
- ❌ `IsoDragService` は既に約 400 行でドラッグ・スナップ・SortingOrder・Stow 連携を抱えており、描画責務の追加は単一責任を明確に壊す
- ❌ `IsoGridGizmo` は `OnDrawGizmos` 前提で、`Gizmos.DrawLine` はランタイムに出せない。実質書き直しになり「拡張」の利点が消える
- **評価: 非推奨**

### Option B: 新規コンポーネント (推奨)

`Home/Service/GridPreviewService` + `Home/View/GridPreviewView` を新設し、`IsoDragService` から既存パターンどおりに3フックを呼ぶ。

```
IsoDragService ──OnFurnitureDragBegin/Move/End──> GridPreviewService
                                                       │ CanPlace* / セル単位判定
                                                       ├──> IsoGridService, IsoGridState
                                                       └──> GridPreviewView (線・面の描画)
```

- ✅ `RedecorateCameraService` / `FurnitureStowService` と完全に同型で、レビュー時の認知負荷が最小
- ✅ 描画を View 1つに閉じ込められるため、要件 4.3 (View 未配置でも動く) は null 許容で素直に満たせる
- ✅ 判定ロジックを Service に置けば描画手段を後から差し替えられる
- ❌ 新規ファイル 2〜3 個
- **評価: 推奨**

### Option C: Option B + 純粋ロジックのテスト用アセンブリ

Option B に加え、`Home/GridPreviewLogic/` (`noEngineReferences`) に以下を切り出す。

- フットプリントのセル列挙 (`footprintStart` + `footprintSize` → `Vector2Int[]`)
- セル単位の可否判定 (範囲内か / 占有 `id` が自分以外か) → 要件 6.4 / 6.5 / 6.6 の分岐表
- 線分の格子頂点生成 (原点・軸ベクトル・本数 → 端点列)

- ✅ 要件 6 の分岐 (可 / 不可 / 部分はみ出し / 全域外) は境界値が多く、EditMode テストの費用対効果が高い
- ✅ structure.md の Testable Logic Assemblies 規約に合致
- ❌ アセンブリ定義とテストアセンブリで管理対象が増える
- **評価: 要件 6 を実装するなら妥当。要件 1〜5 だけなら過剰**

### 描画手段のサブオプション (Research Needed)

| 手段 | 線の太さ | 描画コスト | 備考 |
|---|---|---|---|
| `LineRenderer` を線ごとに配置 | `widthMultiplier` で可 | 床だけで 66 本 = 66 GameObject | 実装は最も素直。要件 6 のセル面塗りには別手段が要る |
| 手続きメッシュ 1枚 (`MeshFilter` + `MeshRenderer`) | 四角形として生成するので自由 | 1 draw call | 線もセル面も同一メッシュに載せられ、頂点カラーで要件 6.4 の色分けができる。頂点生成コードは要る |
| スプライトのタイリング / 9-slice | スプライト依存 | 低 | アイソメトリックの斜め格子と相性が悪い |
| 全面クアッド + シェーダで格子を描く | シェーダパラメータ | 1 draw call | URP 用シェーダの新規作成が必要。プロジェクトに自作シェーダの実績なし |

**現時点の所感**: 手続きメッシュ 1枚が要件 4 (太さ調整) と要件 6 (セル単位の色分け) を同時に満たす唯一の手段で、`LineRenderer` は要件 6 のために結局もう一系統を要求する。ただし確定は設計フェーズで行う。

## 4. 工数とリスク

- **Effort: M (3〜7日)** — 既存パターン (Service 3フック) に乗るため統合は容易だが、ランタイム描画がプロジェクト初導入で、アイソメトリック格子の頂点生成と描画順の実地調整に時間が要る
- **Risk: Medium** — 技術的な未知は「描画手段の選定」に局所化されている。座標系と可否判定は既存資産で確定済みで、永続データにも触れないため失敗時の影響範囲が狭い

## 5. 設計フェーズへの申し送り

### 推奨方針

Option B を骨格とし、要件 6 のセル単位判定と頂点生成については Option C のロジックアセンブリを併用する。

### 決めるべき論点

1. 描画手段 (手続きメッシュ / `LineRenderer`) の確定
2. グリッド線・予告面・背景・家具の sortingOrder の具体値
3. `IsoGridGizmo` とランタイム描画で軸ベクトル計算式を共通化するか、Gizmo は Editor 専用として重複を許容するか
4. `RaycastForFragmentedGrid` の扱い (`IsoDragService` private のまま結果を渡すか、`IsoGridService` 等へ移すか)
5. 要件 4.3 の実現形 (View を null 許容の任意依存にするか、no-op 実装を注入するか)

### Research Needed

- 部屋の背景 (床・壁スプライト) の sortingLayer / sortingOrder の実測値
- URP + `SortingGroup` 環境で手続きメッシュを家具スプライトの間に差し込む際の適切なマテリアル (`Sprites/Default` で足りるか)
- ドラッグ追従時のメッシュ再生成頻度が実機で許容できるか (毎フレーム再構築か、色のみ更新か)
