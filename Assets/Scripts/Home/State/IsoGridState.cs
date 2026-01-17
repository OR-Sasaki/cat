namespace Home.State
{
    /// <summary>
    /// IsoGridのセル状態を保持するState
    /// </summary>
    public class IsoGridState
    {
        public IsoGridCell[,] FloorCells { get; private set; }
        public int GridWidth { get; private set; }
        public int GridHeight { get; private set; }

        /// <summary>
        /// セル配列を初期化
        /// </summary>
        public void Initialize(int gridWidth, int gridHeight)
        {
            GridWidth = gridWidth;
            GridHeight = gridHeight;
            FloorCells = new IsoGridCell[gridWidth, gridHeight];
        }
    }
}