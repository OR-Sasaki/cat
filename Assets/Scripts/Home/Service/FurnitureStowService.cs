using Home.View;
using UnityEngine;
using UnityEngine.Events;
using VContainer;

namespace Home.Service
{
    /// 家具ドラッグ中に画面下部の「しまう」ゾーンへ入ったかを判定し、
    /// UIの表示・ハイライトと、しまう実処理（シーンから除去してState同期）を担当するService
    public class FurnitureStowService
    {
        readonly FurnitureStowView _stowView;
        readonly IsoGridService _isoGridService;

        /// 直近のドラッグ位置がしまうゾーン内にあるか（ドラッグ終了時の判定に使用）
        public bool IsPointerInZone { get; private set; }

        /// 家具がしまわれたときに発火する（在庫リストの選択状態更新などに使用）
        public readonly UnityEvent OnFurnitureStowed = new();

        [Inject]
        public FurnitureStowService(FurnitureStowView stowView, IsoGridService isoGridService)
        {
            _stowView = stowView;
            _isoGridService = isoGridService;
        }

        /// ドラッグ中に呼ぶ。バーをうっすら表示しつつ、ポインター位置に応じてゾーン判定と強調表示を更新する
        public void OnFurnitureDragMove(Vector2 pointerScreenPosition)
        {
            IsPointerInZone = _stowView.ContainsScreenPoint(pointerScreenPosition);
            _stowView.SetDisplay(true, IsPointerInZone);
        }

        /// ドラッグ終了時に呼ぶ。UIを隠してゾーン状態をリセットする
        public void OnFurnitureDragEnd()
        {
            IsPointerInZone = false;
            _stowView.Hide();
        }

        /// 家具をしまう（シーンから除去し、State上のFragmentedGridエントリも破棄する）
        /// ドラッグ開始時点でグリッドからは既に除去済みのため、ここではグリッド操作は行わない
        public void Stow(IsoDraggableView view)
        {
            if (view == null) return;

            // 自身に載っている家具のFragmentedIsoGridエントリを破棄（孤児エントリ防止）
            var childGrids = view.GetComponentsInChildren<FragmentedIsoGrid>();
            foreach (var grid in childGrids)
            {
                _isoGridService.UnregisterFragmentedGrid(grid);
            }

            Object.Destroy(view.gameObject);

            OnFurnitureStowed.Invoke();
        }
    }
}
