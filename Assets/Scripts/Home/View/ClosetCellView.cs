using EnhancedUI;
using EnhancedUI.EnhancedScroller;
using Home.State;

namespace Home.View
{
    /// <summary>
    /// Closetスクローラーの行ビュー（EnhancedScrollerのセル）
    /// </summary>
    public class ClosetCellView : EnhancedScrollerCellView
    {
        public ClosetRowCellView[] RowCellViews;

        public void SetData(ref SmallList<ClosetOutfitData> data, int startingIndex, ClosetCellSelectedDelegate selected)
        {
            for (var i = 0; i < RowCellViews.Length; i++)
            {
                var index = startingIndex + i;
                RowCellViews[i].SetData(index, index < data.Count ? data[index] : null, selected);
            }
        }
    }
}

