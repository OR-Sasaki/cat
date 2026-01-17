using System.Collections;
using Home.Service;
using NavMeshPlus.Components;
using UnityEngine;
using VContainer;

namespace Home.View
{
    // IsoGridの設定をインスペクタから変更したいため、Viewとしてシーンに配置している
    // IsoGridの設定は背景や家具の画像にかなり依存するため、実際に動かしながら設定したい
    public class IsoGridSettingsView : MonoBehaviour
    {
        [Header("Grid Settings")]
        [SerializeField] int _gridWidth = 10;
        [SerializeField] int _gridHeight = 10;
        [SerializeField] float _cellSize = 1f;
        [SerializeField, Range(0f, 90f)] float _angle = 30f;

        [Header("NavMesh Settings")]
        [SerializeField] NavMeshSurface _surface2D;

        public int GridWidth => _gridWidth;
        public int GridHeight => _gridHeight;
        public float CellSize => _cellSize;
        public float Angle => _angle;
        public Vector3 Origin => transform.position;
        public NavMeshSurface Surface2D => _surface2D;
    }
}
