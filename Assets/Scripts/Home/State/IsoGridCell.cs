namespace Home.State
{
    // IsoGrid上の1セルの情報
    public struct IsoGridCell
    {
        // 配置されているオブジェクトのID（0 = 何も配置されていない）
        public int ObjectId;

        // このセルが占有されているか
        public bool IsOccupied => ObjectId != 0;

        // セル情報をクリア
        public void Clear()
        {
            ObjectId = 0;
        }
    }
}
