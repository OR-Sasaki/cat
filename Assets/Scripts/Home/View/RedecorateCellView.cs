using EnhancedUI;
using EnhancedUI.EnhancedScroller;
using Home.State;
using UnityEngine.Events;

namespace Home.View
{
    /// <summary>
    /// Redecorateスクローラーの行ビュー（EnhancedScrollerのセル）
    /// </summary>
    public class RedecorateCellView : EnhancedScrollerCellView
    {
        public RedecorateRowCellView[] RowCellViews;

        public void SetData(ref SmallList<RedecorateFurnitureData> data, int startingIndex, UnityEvent<RedecorateRowCellView> selected)
        {
            for (var i = 0; i < RowCellViews.Length; i++)
            {
                var index = startingIndex + i;
                RowCellViews[i].SetData(index, index < data.Count ? data[index] : null, selected);
            }
        }
    }
}