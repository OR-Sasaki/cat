using UnityEngine;

namespace Cat
{
    public class IsoDraggable : MonoBehaviour
    {
        [Header("Drag Settings")]
        [SerializeField] int _dragSortingOrderBoost = 100;

        IsoGridSystem _gridSystem;
        bool _isDragging;
        Vector3 _dragOffset;
        Camera _mainCamera;
        SpriteRenderer _spriteRenderer;
        int _originalSortingOrder;

        void Awake()
        {
            _mainCamera = Camera.main;
            _spriteRenderer = GetComponent<SpriteRenderer>();
            _gridSystem = FindFirstObjectByType<IsoGridSystem>();

            if (_gridSystem == null)
            {
                Debug.LogError($"[IsoDraggable] IsoGridSystem not found in scene.");
            }
        }

        void OnMouseDown()
        {
            if (_gridSystem == null)
            {
                Debug.LogError($"[IsoDraggable] IsoGridSystem is not assigned.");
                return;
            }

            _isDragging = true;

            // マウス位置とオブジェクト位置の差分を記録
            var mouseWorldPos = GetMouseWorldPosition();
            _dragOffset = transform.position - mouseWorldPos;

            // ソートオーダーを一時的に上げる
            if (_spriteRenderer != null)
            {
                _originalSortingOrder = _spriteRenderer.sortingOrder;
                _spriteRenderer.sortingOrder += _dragSortingOrderBoost;
            }
        }

        void OnMouseDrag()
        {
            if (!_isDragging) return;

            var mouseWorldPos = GetMouseWorldPosition();
            transform.position = mouseWorldPos + _dragOffset;
        }

        void OnMouseUp()
        {
            if (!_isDragging) return;

            _isDragging = false;

            // グリッドにスナップ
            if (_gridSystem != null)
            {
                transform.position = _gridSystem.SnapToFloorGrid(transform.position);
            }

            // ソートオーダーを元に戻す
            if (_spriteRenderer != null)
            {
                _spriteRenderer.sortingOrder = _originalSortingOrder;
            }
        }

        /// <summary>
        /// マウスのワールド座標を取得
        /// </summary>
        Vector3 GetMouseWorldPosition()
        {
            var mousePos = Input.mousePosition;
            mousePos.z = -_mainCamera.transform.position.z;
            return _mainCamera.ScreenToWorldPoint(mousePos);
        }
    }
}
