# Research & Design Decisions

## Summary
- **Feature**: furniture-on-furniture
- **Discovery Scope**: Extension（既存IsoGridシステムの拡張）
- **Key Findings**:
  - IsoDragService.EndFloorDrag()が配置終了時の処理を担当しており、ここにRayCast判定を追加する
  - BeginFloorDrag()でのFragmentedIsoGrid解除処理も必要（状態管理の一貫性）
  - FragmentedIsoGrid内にIsoGridStateと同様のグリッドセル配列を持ち、複数オブジェクト配置を管理
  - Physics2D.RaycastAllを使用した検出パターン（自身のCollider除外含む）がRaycastForDraggableで既に実装されている

## Research Log

### 既存アーキテクチャの分析
- **Context**: FragmentedIsoGrid機能を既存システムに統合するため、現在のドラッグ・配置フローを調査
- **Sources Consulted**: `IsoDragService.cs`, `IsoGridService.cs`, `IsoGridState.cs`, `IsoDraggableView.cs`
- **Findings**:
  - IsoDragServiceはPointerDown/Drag/Upイベントを購読し、ドラッグ操作を管理
  - EndFloorDrag()で床への配置処理を実行（グリッドスナップ、配置可否チェック、配置実行）
  - RaycastForDraggableで2D Raycastを使用してIsoDraggableViewを検出するパターンが存在
  - 壁配置システム（wall-furniture）が既に同様のパターンで実装済み
- **Implications**:
  - EndFloorDrag()の先頭でFragmentedIsoGridへのRayCast判定を追加する設計が自然
  - 既存のRaycastパターンを踏襲することで一貫性を保つ

### 配置制約の実装方針
- **Context**: IsWallPlacement制約とfootprintサイズ制約の実装方法
- **Sources Consulted**: `IsoDraggableView.cs`, `FurniturePlacementService.cs`
- **Findings**:
  - IsoDraggableView.IsWallPlacementプロパティで壁配置家具かどうかを判定可能
  - FootprintSizeプロパティでオブジェクトの占有サイズを取得可能
  - CanPlaceFloorObject/CanPlaceWallObjectパターンで配置可否判定を実装
- **Implications**:
  - FragmentedIsoGridにサイズプロパティを持たせ、footprintと比較する
  - 配置可否判定はIsoDragService内で実行し、不可の場合は元の床配置フローにフォールバック

### 2D Collider要件
- **Context**: FragmentedIsoGridがRayCastで検出可能であるための要件
- **Sources Consulted**: Unity Physics2D documentation, `IsoDraggableView.cs`
- **Findings**:
  - Physics2D.RaycastAllでCollider2Dを検出
  - RequireComponentアトリビュートで2D Colliderの必須化が可能
  - BoxCollider2Dが家具上面の矩形領域に適している
- **Implications**:
  - FragmentedIsoGridにRequireComponent(typeof(Collider2D))を指定
  - Colliderのサイズは家具の上面サイズに合わせて設定

## Architecture Pattern Evaluation

| Option | Description | Strengths | Risks / Limitations | Notes |
|--------|-------------|-----------|---------------------|-------|
| IsoDragService拡張 | EndFloorDragにFragmentedIsoGrid判定を追加 | 既存フローとの統合が容易、コード変更が最小限 | IsoDragServiceの責務が増える | 選択案 |
| 専用Service分離 | FragmentedIsoGridPlacementServiceを新設 | 責務が明確に分離 | オーバーエンジニアリング、依存関係が複雑化 | 不採用 |

## Design Decisions

### Decision: FragmentedIsoGridコンポーネント設計
- **Context**: 家具の上に配置可能なグリッド領域を表現するコンポーネントが必要
- **Alternatives Considered**:
  1. IsoGridServiceを拡張して家具上グリッドを管理 — 既存システムの変更が大きい
  2. 独立したMonoBehaviourコンポーネント — シンプルで家具プレハブにアタッチ可能
- **Selected Approach**: 独立したMonoBehaviourコンポーネントとして実装、内部にグリッドセル配列を持つ
- **Rationale**: 家具プレハブに個別にアタッチでき、IsoGridStateと同様のパターンで複数オブジェクト管理が可能
- **Trade-offs**: 各FragmentedIsoGridが独自のセル配列を持つため、メモリ使用量は増加
- **Follow-up**: 座標変換ロジックはIsoGridServiceを参考に実装

### Decision: ドラッグ開始時のFragmentedIsoGrid解除
- **Context**: FragmentedIsoGrid上のオブジェクトを再度ドラッグした際の状態管理
- **Alternatives Considered**:
  1. EndFloorDragでのみ処理 — 状態不整合のリスク
  2. BeginFloorDragで解除処理を追加 — 既存パターンと一貫性あり
- **Selected Approach**: BeginFloorDragでFragmentedIsoGridからの解除処理を追加
- **Rationale**: 床配置のBeginFloorDrag/EndFloorDragパターンと同様のライフサイクル管理
- **Trade-offs**: IsoDragServiceに追加のステート（_dragStartFragmentedGrid）が必要

### Decision: RayCast時の自己検出回避
- **Context**: ドラッグ中のオブジェクト自身にFragmentedIsoGridがある場合の誤検出防止
- **Alternatives Considered**:
  1. ドラッグ中にColliderを無効化 — 他の検出にも影響
  2. RayCast結果から自身を除外 — 既存パターンと一貫性あり
- **Selected Approach**: RayCast結果で親IsoDraggableViewが_currentIsoDraggableViewと一致する場合はスキップ
- **Rationale**: RaycastForDraggableと同様のフィルタリングパターン

### Decision: RayCast判定タイミング
- **Context**: EndFloorDrag()のどの時点でFragmentedIsoGridを検出するか
- **Alternatives Considered**:
  1. 配置可否チェック前にRayCast — FragmentedIsoGrid優先
  2. 床配置失敗後にRayCast — 床配置を優先
- **Selected Approach**: 配置可否チェック前にRayCast（FragmentedIsoGrid優先）
- **Rationale**: ユーザーがドラッグした位置に家具があればそこに配置する意図と解釈
- **Trade-offs**: 床に配置したい場合は家具を避ける必要がある

## Risks & Mitigations
- RayCastで自身のFragmentedIsoGridを検出してしまう — 親IsoDraggableViewの一致チェックで除外
- RayCastでFragmentedIsoGridと通常家具のColliderが重複検出される — GetComponentでFragmentedIsoGridを持つものを優先
- 家具移動時に上に乗っているオブジェクトの処理 — 初期実装では未対応（Non-Goalsに明記）
- 座標変換の精度 — IsoGridServiceの計算ロジックを参考に、同じcellSizeとangle設定を使用

## References
- Unity Physics2D.RaycastAll: https://docs.unity3d.com/ScriptReference/Physics2D.RaycastAll.html
- RequireComponent: https://docs.unity3d.com/ScriptReference/RequireComponent.html
