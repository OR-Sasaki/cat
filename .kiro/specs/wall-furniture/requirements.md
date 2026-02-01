# 壁配置Furniture 要件定義

## 概要
壁に配置する家具を実装し、左壁・右壁の両方に自動配置可能とする。

## 機能要件

### FR-1: PlacementType による配置場所の指定
- Furniture に `PlacementType` enum を追加
- `PlacementType.Floor`: 床に配置
- `PlacementType.Wall`: 壁に配置

### FR-2: 壁グリッドシステム
- 左壁と右壁の2つの壁面に配置可能
- 各壁面は2次元グリッドで管理
  - 左壁: (y, z) 座標系、y: 0〜GridHeight, z: 0〜WallHeight
  - 右壁: (x, z) 座標系、x: 0〜GridWidth, z: 0〜WallHeight

### FR-3: 自動配置
- 壁家具選択時、空いている位置に自動配置
- 配置優先順位: 左壁 → 右壁
- 空き位置がない場合は配置しない

### FR-4: UI表示
- 床家具と壁家具を同じリストに混在表示
- 配置済み/未配置の状態を正しく表示

### FR-5: 削除機能
- 配置済み壁家具を選択して削除可能

### FR-6: セーブ/ロード
- 壁家具の配置位置を保存
- シーン再読み込み時に正しい位置に復元

## 非機能要件

### NFR-1: ドラッグ配置非対応
- 壁家具はドラッグによる位置調整を行わない
- 自動配置のみをサポート

## 壁グリッド座標系

| 壁 | 座標系 | 範囲 |
|---|---|---|
| 左壁 (LeftWall) | (y, z) | y: 0〜GridHeight, z: 0〜WallHeight |
| 右壁 (RightWall) | (x, z) | x: 0〜GridWidth, z: 0〜WallHeight |
