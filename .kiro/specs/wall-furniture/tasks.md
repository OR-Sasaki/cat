# 壁配置Furniture 実装タスク

## Phase 0: Furniture に PlacementType を追加

### Task 0.1: PlacementType enum を追加
- [ ] `Assets/Arts/Furniture/Scripts/Furniture.cs` に `PlacementType` enum を追加
```csharp
public enum PlacementType
{
    Floor,  // 床に配置
    Wall,   // 壁に配置
}
```

### Task 0.2: Furniture クラスにフィールドを追加
- [ ] `Furniture` クラスに `PlacementType PlacementType` フィールドを追加

### Task 0.3: 既存アセットの更新
- [ ] 既存の床家具アセットに `PlacementType = Floor` を設定（Unity Inspector）

---

## Phase 1: データ構造の準備

### Task 1.1: WallSide enum と WallObjectPosition を追加
- [ ] `Assets/Scripts/Home/State/IsoGridState.cs` に `WallSide` enum を追加
- [ ] `WallObjectPosition` 構造体を追加

### Task 1.2: IsoGridState に壁グリッドを追加
- [ ] `LeftWallCells[,]` プロパティを追加
- [ ] `RightWallCells[,]` プロパティを追加
- [ ] `WallHeight` プロパティを追加
- [ ] `WallObjectFootprintStartPositions` ディクショナリを追加
- [ ] `Initialize` メソッドを拡張

### Task 1.3: IsoGridSettingsView に WallHeight を追加
- [ ] `Assets/Scripts/Home/View/IsoGridSettingsView.cs` に `_wallHeight` フィールドとプロパティを追加

---

## Phase 2: IsoGridService の拡張

### Task 2.1: 既存メソッドをリネーム
- [ ] `GridToWorld` → `FloorGridToWorld`
- [ ] `IsValidPosition` → `IsValidFloorPosition`
- [ ] `GetUserFurnitureId` → `GetFloorUserFurnitureId`
- [ ] `PlaceObject` → `PlaceFloorObject`
- [ ] `RemoveObject` → `RemoveFloorObject`
- [ ] `GetObjectFootprintStart` → `GetFloorObjectFootprintStart`
- [ ] `CanPlaceObject` → `CanPlaceFloorObject`

### Task 2.2: 呼び出し元のメソッド名を更新
- [ ] `FurniturePlacementService.cs` のメソッド呼び出しを更新
- [ ] `IsoDragService.cs` のメソッド呼び出しを更新
- [ ] `IsoDraggableGizmo.cs` のメソッド呼び出しを更新

### Task 2.3: Initialize で壁グリッドを初期化
- [ ] `IsoGridService` コンストラクタで `_state.Initialize` に `wallHeight` を渡す

### Task 2.4: 壁用メソッドを追加
- [ ] `WallGridToWorld(WallSide, Vector2Int)` を追加
- [ ] `IsValidWallPosition(WallSide, Vector2Int)` を追加
- [ ] `GetWallUserFurnitureId(WallSide, Vector2Int)` を追加
- [ ] `CanPlaceWallObject(WallSide, Vector2Int, Vector2Int, int)` を追加
- [ ] `PlaceWallObject(WallSide, Vector2Int, Vector2Int, int)` を追加
- [ ] `RemoveWallObject(int, Vector2Int)` を追加
- [ ] `GetWallObjectFootprintStart(int)` を追加

---

## Phase 3: FurniturePlacementService の拡張

### Task 3.1: PlaceFurniture に PlacementType 分岐を追加
- [ ] `PlacementType.Wall` の場合は壁配置ロジックへ
- [ ] `PlacementType.Floor` の場合は既存ロジックへ

### Task 3.2: 壁配置メソッドを追加
- [ ] `PlaceWallFurniture(int, Furniture)` を追加
- [ ] `FindAvailableWallPosition(WallSide, Vector2Int)` を追加
- [ ] `PlaceWallFurnitureAt(int, Furniture, WallSide, Vector2Int)` を追加

### Task 3.3: RemoveFurniture を修正
- [ ] `PlacementType.Wall` の場合は `RemoveWallObject` を呼び出す

---

## Phase 4: セーブ/ロードの対応

### Task 4.1: セーブデータ構造を追加
- [ ] `Assets/Scripts/Root/State/UserState.cs` に `IsoGridWallObjectPosition` クラスを追加
- [ ] `IsoGridSaveData` に `WallObjectPositions` フィールドを追加

### Task 4.2: IsoGridSaveService を拡張
- [ ] `Assets/Scripts/Home/Service/IsoGridSaveService.cs` で壁オブジェクトのセーブ処理を追加

### Task 4.3: IsoGridLoadService を拡張
- [ ] `Assets/Scripts/Home/Service/IsoGridLoadService.cs` で壁オブジェクトのロード処理を追加

---

## Phase 5: UI統合

### Task 5.1: RedecorateScrollerService の選択状態チェックを更新
- [ ] `UpdateSelectionStates` で床と壁の両方をチェック
- [ ] `LoadData` で床と壁の両方をチェック

---

## 検証

### 配置テスト
- [ ] PlacementType.Wall の家具アセットを作成
- [ ] Redecorate UIで選択して配置されることを確認
- [ ] 左壁→右壁の順で自動配置されることを確認

### 削除テスト
- [ ] 配置済み壁家具を選択して削除されることを確認

### セーブ/ロードテスト
- [ ] 壁家具を配置してシーン再読み込み
- [ ] 壁家具が正しい位置に復元されることを確認

### UI表示テスト
- [ ] 床家具と壁家具が同じリストに表示されることを確認
- [ ] 配置済み状態が正しく表示されることを確認
