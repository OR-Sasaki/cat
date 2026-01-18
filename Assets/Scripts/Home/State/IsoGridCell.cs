namespace Home.State
{
    // IsoGrid上の1セルの情報
    public struct IsoGridCell
    {
        // 配置されているユーザー家具のID（0 = 何も配置されていない）
        public int UserFurnitureId;

        // このセルが占有されているか
        public bool IsOccupied => UserFurnitureId != 0;

        // セル情報をクリア
        public void Clear()
        {
            UserFurnitureId = 0;
        }
    }
}
