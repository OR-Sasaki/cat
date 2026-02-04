using Cat.Furniture;
using Home.State;
using UnityEngine;
using UnityEngine.Rendering;

namespace Home.View
{
    // ドラッグ可能なアイソメトリックオブジェクト
    // Pivotはオブジェクトが床に接している面の中心に設定されている前提
    public class IsoDraggableView : MonoBehaviour
    {
        [Header("Footprint Settings")]
        [SerializeField] Vector2Int _footprintSize = Vector2Int.one;
        [Tooltip("オブジェクト内のピボット位置（IsoGrid座標）。フットプリントの左下を(0,0)とする")]
        [SerializeField] Vector2Int _pivotGridPosition = Vector2Int.zero;
        [SerializeField] Transform _viewPivot;

        [Header("Object Settings")]
        [SerializeField] int _userFurnitureId;

        [Header("左右のPivot(床の場合は設定しなくてOK)")]
        [SerializeField] GameObject _rightViewPivot;
        [SerializeField] GameObject _leftViewPivot;

        [Header("Sorting")]
        [SerializeField] SortingGroup _sortingGroup;

        bool _isDragging;
        bool _isPlacedOnGrid;
        PlacementType _placementType;
        WallSide _wallSide;
        FragmentedIsoGrid _currentFragmentedGrid;

        public bool IsDragging => _isDragging;
        public bool IsPlacedOnGrid => _isPlacedOnGrid;
        public Vector2Int FootprintSize => _footprintSize;
        public Vector2Int PivotGridPosition => _pivotGridPosition;
        public int UserFurnitureId => _userFurnitureId;
        public float ViewPivotY => _viewPivot.position.y;
        public Vector3 Position => transform.position;
        public PlacementType PlacementType => _placementType;
        public WallSide WallSide => _wallSide;
        public bool IsWallPlacement => _placementType == PlacementType.Wall;
        public FragmentedIsoGrid CurrentFragmentedGrid => _currentFragmentedGrid;

        void Awake()
        {
#if UNITY_EDITOR
            if (GetComponent<IsoDraggableGizmo>() is null)
            {
                var c = gameObject.AddComponent<IsoDraggableGizmo>();
                c.SetIsoDraggableView(this);
            }
#endif
        }

        // 位置を設定
        public void SetPosition(Vector3 position)
        {
            transform.position = position;
        }

        // ドラッグ状態を設定
        public void SetDragging(bool isDragging)
        {
            _isDragging = isDragging;
        }

        // グリッド配置状態を設定
        public void SetPlacedOnGrid(bool isPlaced)
        {
            _isPlacedOnGrid = isPlaced;
        }

        // 現在所属しているFragmentedIsoGridを設定
        public void SetCurrentFragmentedGrid(FragmentedIsoGrid grid)
        {
            _currentFragmentedGrid = grid;
        }

        // UserFurnitureIdを設定
        public void SetUserFurnitureId(int userFurnitureId)
        {
            _userFurnitureId = userFurnitureId;
        }

        // PlacementTypeを設定
        public void SetPlacementType(PlacementType placementType)
        {
            _placementType = placementType;
            SetViewPivot(true); // 初期は右壁に沿わす
        }

        // WallSideを設定
        public void SetWallSide(WallSide wallSide)
        {
            _wallSide = wallSide;
            SetViewPivot(wallSide == WallSide.Right);
        }

        void SetViewPivot(bool isRight)
        {
            if (_rightViewPivot) _rightViewPivot.SetActive(isRight);
            if(_leftViewPivot) _leftViewPivot.SetActive(!isRight);
        }

        /// SortingGroupのSortingOrderを設定
        public void SetSortingOrder(int order)
        {
            if (_sortingGroup != null)
            {
                _sortingGroup.sortingOrder = order;
            }
        }
    }
}
