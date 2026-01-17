namespace Home.State
{
    /// <summary>
    /// IsoGrid上の1セルの情報
    /// </summary>
    public struct IsoGridCell
    {
        /// <summary>
        /// 配置されているオブジェクトのID（0 = 何も配置されていない）
        /// </summary>
        public int ObjectId;

        /// <summary>
        /// このセルが占有されているか
        /// </summary>
        public bool IsOccupied => ObjectId != 0;

        /// <summary>
        /// セル情報をクリア
        /// </summary>
        public void Clear()
        {
            ObjectId = 0;
        }
    }
}
