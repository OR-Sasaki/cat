namespace Home.State
{
    /// 現在 RoomBackGround 配下に配置中の Base の userFurnitureId のみを保持する純粋データ State。
    /// 不変条件: PlacedBaseUserFurnitureId != UnplacedId のとき RoomBackGroundView.BaseRoot 配下に
    /// 対応する Base GameObject がちょうど 1 個存在する。
    /// 値の変更は FurniturePlacementService.PlaceBase および IsoGridLoadService の復元経路からのみ行うこと。
    /// それ以外の経路から SetPlaced / Clear を呼ぶと State と GameObject の整合が崩れる。
    public class RoomBaseState
    {
        /// 未配置を表す sentinel 値。UserFurnitureId は 1 以上を前提とするため -1 を採用。
        public const int UnplacedId = -1;

        public int PlacedBaseUserFurnitureId { get; private set; } = UnplacedId;

        public bool IsPlaced => PlacedBaseUserFurnitureId != UnplacedId;

        public void SetPlaced(int userFurnitureId) => PlacedBaseUserFurnitureId = userFurnitureId;

        public void Clear() => PlacedBaseUserFurnitureId = UnplacedId;
    }
}
