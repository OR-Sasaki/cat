using System.Collections;
using Home.Service;
using Home.State;
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
        [SerializeField] int _gridWidth = 32;
        [SerializeField] int _gridHeight = 32;

        public const float CellSize = 0.4885f;
        public const float Angle = 15.5f;

        [Header("Wall Settings")]
        [SerializeField] int _wallHeight = 10;

        [Header("NavMesh Settings")]
        [SerializeField] NavMeshSurface _surface2D;

        public int GridWidth => _gridWidth;
        public int GridHeight => _gridHeight;
        public int WallHeight => _wallHeight;
        public Vector3 Origin => transform.position;
        public NavMeshSurface Surface2D => _surface2D;

        // ランタイムでIsoGridStateの中身をインスペクタから確認するための参照
        public IsoGridState IsoGridStateRef { get; private set; }

        [Inject]
        void Construct(IsoGridState isoGridState)
        {
            IsoGridStateRef = isoGridState;
        }
    }
}
