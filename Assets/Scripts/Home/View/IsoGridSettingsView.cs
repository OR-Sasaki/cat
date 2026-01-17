using System.Collections;
using Home.Service;
using NavMeshPlus.Components;
using UnityEngine;
using VContainer;

namespace Home.View
{
    /// IsoGridの設定値を保持しServiceに提供するView
    public class IsoGridSettingsView : MonoBehaviour
    {
        [Header("Grid Settings")]
        [SerializeField] int _gridWidth = 10;
        [SerializeField] int _gridHeight = 10;
        [SerializeField] float _cellSize = 1f;
        [SerializeField, Range(0f, 90f)] float _angle = 30f;

        [Header("NavMesh Settings")]
        [SerializeField] NavMeshSurface _surface2D;

        IsoGridService _isoGridService;

        public int GridWidth => _gridWidth;
        public int GridHeight => _gridHeight;
        public float CellSize => _cellSize;
        public float Angle => _angle;
        public Vector3 Origin => transform.position;
        public IsoGridService Service => _isoGridService;

        [Inject]
        public void Init(IsoGridService service)
        {
            _isoGridService = service;
            _isoGridService.InitializeCells(_gridWidth, _gridHeight);
            _isoGridService.InitializeGridSettings(transform.position, _cellSize, _angle);
            _isoGridService.OnObjectPlaced += HandleObjectPlaced;
        }

        void OnDestroy()
        {
            if (_isoGridService != null)
            {
                _isoGridService.OnObjectPlaced -= HandleObjectPlaced;
            }
        }

        void HandleObjectPlaced()
        {
            StartCoroutine(BuildNavMeshDelayed());
        }

        IEnumerator BuildNavMeshDelayed()
        {
            yield return null;
            _surface2D.BuildNavMeshAsync();
        }
    }
}