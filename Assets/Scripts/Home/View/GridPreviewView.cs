#nullable enable
using System.Collections.Generic;
using Home.Service;
using UnityEngine;
using VContainer;

namespace Home.View
{
    public enum GridSurface { Floor, Walls }

    /// グリッド線と設置予告面の手続きメッシュ描画。判断ロジックは持たない
    public sealed class GridPreviewView : MonoBehaviour
    {
        [SerializeField] Material? _material;
        [SerializeField] Color _lineColor = new(1, 1, 1, 0.35f);
        [SerializeField] float _lineWidth = 0.02f;
        [SerializeField] int _lineSortingOrder = -10;
        [SerializeField] Color _footprintColor = new(1, 1, 1, 0.25f);
        [SerializeField] Color _footprintBlockedColor = new(1, 0, 0, 0.35f);
        [SerializeField] int _footprintSortingOrder = -9;
        [SerializeField] int _footprintSortingOrderInGroup = -1;
        [SerializeField] MeshRenderer _linesRenderer = null!;
        [SerializeField] MeshRenderer _footprintRenderer = null!;

        MeshFilter _linesMeshFilter = null!;
        MeshFilter _footprintMeshFilter = null!;

        readonly Mesh?[] _lineMeshes = new Mesh?[2];

        Mesh _footprintMesh = null!;
        Transform _footprintHome = null!;

        // 予告メッシュ再構築用の使い回しバッファ
        readonly List<Vector3> _verts = new();
        readonly List<Color> _colors = new();
        readonly List<int> _indices = new();

        [Inject]
        public void Init(GridPreviewService service)
        {
            service.AttachView(this);
        }

        void Awake()
        {
            if (_material == null)
            {
                Debug.LogError("[GridPreviewView] Material is not set", this);
                enabled = false;
                return;
            }

            transform.position = Vector3.zero;
            transform.rotation = Quaternion.identity;
            transform.localScale = Vector3.one;

            _linesMeshFilter = _linesRenderer.GetComponent<MeshFilter>();
            _footprintMeshFilter = _footprintRenderer.GetComponent<MeshFilter>();

            _linesRenderer.sharedMaterial = _material;
            _footprintRenderer.sharedMaterial = _material;
            _linesRenderer.sortingOrder = _lineSortingOrder;
            _footprintRenderer.sortingOrder = _footprintSortingOrder;

            _footprintMesh = new Mesh();
            _footprintMesh.MarkDynamic();
            _footprintMeshFilter.sharedMesh = _footprintMesh;

            _footprintHome = _footprintRenderer.transform.parent;

            Hide();
        }

        /// 面ごとの線分列（ワールド座標）から線メッシュを構築してキャッシュする。初回のみ呼ばれる想定
        public void SetLines(GridSurface surface, IReadOnlyList<(Vector3 Start, Vector3 End)> segments)
        {
            if (!enabled) return;

            var verts = new List<Vector3>(segments.Count * 4);
            var colors = new List<Color>(segments.Count * 4);
            var indices = new List<int>(segments.Count * 6);

            foreach (var (start, end) in segments)
            {
                var dir = end - start;
                var perp = new Vector3(-dir.y, dir.x, 0f).normalized * (_lineWidth * 0.5f);

                var baseIndex = verts.Count;
                verts.Add(start - perp);
                verts.Add(start + perp);
                verts.Add(end + perp);
                verts.Add(end - perp);

                for (var i = 0; i < 4; i++) colors.Add(_lineColor);

                AddQuadIndices(indices, baseIndex);
            }

            var mesh = new Mesh();
            mesh.SetVertices(verts);
            mesh.SetColors(colors);
            mesh.SetTriangles(indices, 0);
            mesh.RecalculateBounds();

            _lineMeshes[(int)surface] = mesh;
        }

        public void ShowLines(GridSurface surface)
        {
            if (!enabled) return;

            var mesh = _lineMeshes[(int)surface];
            if (mesh == null)
            {
                Debug.LogError($"[GridPreviewView] SetLines has not been called for {surface}");
                return;
            }

            _linesMeshFilter.sharedMesh = mesh;
            _linesRenderer.enabled = true;
        }

        /// 予告面を四角形単位（4頂点ずつ、ワールド座標）で再構築する。parent が非 null なら子に付け替えて面グループ内で描く
        public void SetFootprint(IReadOnlyList<Vector3> quadCorners, bool canPlace, Transform? parent)
        {
            if (!enabled) return;

            var footprintTransform = _footprintRenderer.transform;

            if (parent != null && footprintTransform.parent != parent)
            {
                ReparentFootprint(parent, _footprintSortingOrderInGroup);
            }
            else if (parent == null && footprintTransform.parent != _footprintHome)
            {
                ReparentFootprint(_footprintHome, _footprintSortingOrder);
            }

            _verts.Clear();
            _colors.Clear();
            _indices.Clear();

            var color = canPlace ? _footprintColor : _footprintBlockedColor;

            for (var i = 0; i + 3 < quadCorners.Count; i += 4)
            {
                var baseIndex = _verts.Count;
                for (var j = 0; j < 4; j++)
                {
                    var world = quadCorners[i + j];
                    var local = parent != null ? parent.InverseTransformPoint(world) : world;
                    _verts.Add(local);
                    _colors.Add(color);
                }

                AddQuadIndices(_indices, baseIndex);
            }

            _footprintMesh.Clear();
            _footprintMesh.SetVertices(_verts);
            _footprintMesh.SetColors(_colors);
            _footprintMesh.SetTriangles(_indices, 0);
            _footprintMesh.RecalculateBounds();

            _footprintRenderer.enabled = true;
        }

        public void ClearFootprint()
        {
            if (!enabled) return;
            _footprintRenderer.enabled = false;
        }

        public void Hide()
        {
            if (!enabled) return;

            _linesRenderer.enabled = false;
            _footprintRenderer.enabled = false;

            if (_footprintRenderer.transform.parent == _footprintHome) return;

            ReparentFootprint(_footprintHome, _footprintSortingOrder);
        }

        /// 予告面の親付け替えとローカル座標のリセット
        void ReparentFootprint(Transform parent, int sortingOrder)
        {
            var footprintTransform = _footprintRenderer.transform;
            footprintTransform.SetParent(parent, false);
            footprintTransform.localPosition = Vector3.zero;
            footprintTransform.localRotation = Quaternion.identity;
            footprintTransform.localScale = Vector3.one;
            _footprintRenderer.sortingOrder = sortingOrder;
        }

        /// 矩形1枚分（4頂点）のインデックスを追加
        static void AddQuadIndices(List<int> indices, int baseIndex)
        {
            indices.Add(baseIndex);
            indices.Add(baseIndex + 1);
            indices.Add(baseIndex + 2);
            indices.Add(baseIndex);
            indices.Add(baseIndex + 2);
            indices.Add(baseIndex + 3);
        }
    }
}
