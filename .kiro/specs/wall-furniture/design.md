# 壁配置Furniture 技術設計

## 変更対象ファイル一覧

| ファイル | 変更内容 |
|---------|---------|
| `Arts/Furniture/Scripts/Furniture.cs` | PlacementType enum 追加、Furniture クラスにフィールド追加 |
| `Home/State/IsoGridState.cs` | WallSide enum, WallObjectPosition, 壁グリッド配列追加 |
| `Home/View/IsoGridSettingsView.cs` | WallHeight プロパティ追加 |
| `Home/Service/IsoGridService.cs` | 床用メソッドリネーム、壁用メソッド群を追加 |
| `Home/Service/FurniturePlacementService.cs` | メソッド呼び出しをリネーム後に変更、PlacementType分岐、壁配置メソッド追加 |
| `Home/Service/IsoDragService.cs` | メソッド呼び出しをリネーム後に変更 |
| `Home/View/IsoDraggableGizmo.cs` | メソッド呼び出しをリネーム後に変更 |
| `Root/State/UserState.cs` | セーブデータ構造拡張 |
| `Home/Service/IsoGridSaveService.cs` | 壁オブジェクト保存処理追加 |
| `Home/Service/IsoGridLoadService.cs` | 壁オブジェクト読込処理追加 |
| `Home/Service/RedecorateScrollerService.cs` | 選択状態チェック更新 |

---

## 1. PlacementType の追加

### 1.1 PlacementType enum
ファイル: `Assets/Arts/Furniture/Scripts/Furniture.cs`

```csharp
public enum PlacementType
{
    Floor,  // 床に配置
    Wall,   // 壁に配置
}
```

### 1.2 Furniture クラスの拡張
```csharp
public class Furniture : ScriptableObject
{
    public FurnitureType FurnitureType;
    public PlacementType PlacementType;  // 追加
    public Sprite Thumbnail;
    public IsoDraggableView SceneObject;
}
```

---

## 2. IsoGridState の拡張

### 2.1 WallSide enum と WallObjectPosition 構造体
ファイル: `Assets/Scripts/Home/State/IsoGridState.cs`

```csharp
public enum WallSide { Left, Right }

public struct WallObjectPosition
{
    public WallSide Side;
    public Vector2Int Position;
}
```

### 2.2 壁グリッド配列
```csharp
public IsoGridCell[,] LeftWallCells { get; private set; }
public IsoGridCell[,] RightWallCells { get; private set; }
public int WallHeight { get; private set; }
public Dictionary<int, WallObjectPosition> WallObjectFootprintStartPositions { get; } = new();
```

### 2.3 Initialize メソッドの拡張
```csharp
public void Initialize(int gridWidth, int gridHeight, int wallHeight)
{
    GridWidth = gridWidth;
    GridHeight = gridHeight;
    WallHeight = wallHeight;

    FloorCells = new IsoGridCell[gridWidth, gridHeight];
    LeftWallCells = new IsoGridCell[gridHeight, wallHeight];  // (y, z)
    RightWallCells = new IsoGridCell[gridWidth, wallHeight];  // (x, z)

    ObjectFootprintStartPositions.Clear();
    WallObjectFootprintStartPositions.Clear();
}
```

---

## 3. IsoGridService の拡張

### 3.1 既存メソッドのリネーム

| 現在のメソッド名 | 新しいメソッド名 |
|---|---|
| `GridToWorld` | `FloorGridToWorld` |
| `IsValidPosition` | `IsValidFloorPosition` |
| `GetUserFurnitureId` | `GetFloorUserFurnitureId` |
| `PlaceObject` | `PlaceFloorObject` |
| `RemoveObject` | `RemoveFloorObject` |
| `GetObjectFootprintStart` | `GetFloorObjectFootprintStart` |
| `CanPlaceObject` | `CanPlaceFloorObject` |

### 3.2 壁用メソッドの追加

```csharp
/// 壁グリッド座標をワールド座標に変換
public Vector3 WallGridToWorld(WallSide side, Vector2Int gridPos)
{
    var zOffset = Vector3.up * gridPos.y * _cellSize;
    if (side == WallSide.Left)
    {
        var worldOffset = gridPos.x * _yAxis;
        return _origin + (Vector3)worldOffset + zOffset;
    }
    else
    {
        var worldOffset = gridPos.x * _xAxis;
        return _origin + (Vector3)worldOffset + zOffset;
    }
}

/// 壁グリッド座標が有効範囲内かチェック
public bool IsValidWallPosition(WallSide side, Vector2Int gridPos)
{
    var maxWidth = side == WallSide.Left ? _state.GridHeight : _state.GridWidth;
    return gridPos.x >= 0 && gridPos.x < maxWidth
        && gridPos.y >= 0 && gridPos.y < _state.WallHeight;
}

/// 壁への配置可能チェック
public bool CanPlaceWallObject(WallSide side, Vector2Int footprintStart, Vector2Int footprintSize, int selfUserFurnitureId = 0)

/// 壁にオブジェクトを配置
public void PlaceWallObject(WallSide side, Vector2Int footprintStart, Vector2Int footprintSize, int userFurnitureId)

/// 壁からオブジェクトを削除
public void RemoveWallObject(int userFurnitureId, Vector2Int footprintSize)

/// 壁オブジェクトのフットプリント開始位置を取得
public WallObjectPosition GetWallObjectFootprintStart(int userFurnitureId)
```

---

## 4. FurniturePlacementService の拡張

### 4.1 PlaceFurniture の PlacementType 分岐
```csharp
public Vector3? PlaceFurniture(int userFurnitureId, Cat.Furniture.Furniture furniture)
{
    if (furniture.SceneObject == null) return null;

    if (furniture.PlacementType == PlacementType.Wall)
    {
        return PlaceWallFurniture(userFurnitureId, furniture);
    }

    // 既存の床配置ロジック
    // ...
}
```

### 4.2 壁配置メソッド
```csharp
/// 壁家具を空き位置に配置（左壁→右壁の順で探索）
Vector3? PlaceWallFurniture(int userFurnitureId, Cat.Furniture.Furniture furniture)

/// 壁の空き位置を探す
Vector2Int? FindAvailableWallPosition(WallSide side, Vector2Int footprintSize)

/// 壁の指定位置に家具を配置
public Vector3? PlaceWallFurnitureAt(int userFurnitureId, Cat.Furniture.Furniture furniture, WallSide side, Vector2Int gridPos)
```

### 4.3 RemoveFurniture の修正
```csharp
if (furniture.PlacementType == PlacementType.Wall)
{
    _isoGridService.RemoveWallObject(userFurnitureId, targetView.FootprintSize);
}
else
{
    _isoGridService.RemoveFloorObject(userFurnitureId, targetView.FootprintSize);
}
```

---

## 5. セーブ/ロードの拡張

### 5.1 セーブデータ構造
ファイル: `Assets/Scripts/Root/State/UserState.cs`

```csharp
[Serializable]
public class IsoGridSaveData
{
    public IsoGridObjectPosition[] ObjectPositions;
    public IsoGridWallObjectPosition[] WallObjectPositions; // 追加
}

[Serializable]
public class IsoGridWallObjectPosition
{
    public int UserFurnitureId;
    public int Side; // 0=Left, 1=Right
    public int X;
    public int Z;
}
```

---

## 6. RedecorateScrollerService の修正

### 6.1 選択状態チェックの更新
```csharp
// 床と壁の両方をチェック
data.Selected = _isoGridState.ObjectFootprintStartPositions.ContainsKey(data.UserFurnitureId)
             || _isoGridState.WallObjectFootprintStartPositions.ContainsKey(data.UserFurnitureId);
```
