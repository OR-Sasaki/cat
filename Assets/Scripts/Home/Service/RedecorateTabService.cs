using Cat.Furniture;
using Home.State;
using VContainer;

namespace Home.Service
{
    /// リデコレートタブの遷移と既定値復帰を集約する Service
    public sealed class RedecorateTabService
    {
        readonly RedecorateTabState _redecorateTabState;

        [Inject]
        public RedecorateTabService(RedecorateTabState redecorateTabState)
        {
            _redecorateTabState = redecorateTabState;
        }

        /// タブを選択する。同値時は no-op、差分時のみ State の `Changed` が発火する
        public void Select(FurnitureType type)
        {
            _redecorateTabState.WriteCurrent(type);
        }

        /// 既定タブ (`RedecorateTabState.Default`) に戻す
        public void ResetToDefault()
        {
            _redecorateTabState.WriteCurrent(RedecorateTabState.Default);
        }
    }
}
