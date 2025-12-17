using UnityEngine;

namespace Cat.Character
{
    public class CharacterWalkableArea : MonoBehaviour
    {
        [Header("エリア設定")]
        [SerializeField] Vector2 _areaSize = new(10f, 10f);
        [SerializeField] Vector2 _areaOffset = Vector2.zero;

        [Header("表示設定")]
        [SerializeField] bool _showGizmos = true;
        [SerializeField] Color _fillColor = new(0f, 1f, 0f, 0.2f);
        [SerializeField] Color _wireColor = new(0f, 1f, 0f, 1f);

        public Vector2 AreaSize => _areaSize;
        public Vector2 AreaOffset => _areaOffset;

        public Vector3 AreaCenter => transform.position + new Vector3(_areaOffset.x, _areaOffset.y, 0);

        public bool IsInArea(Vector2 position)
        {
            var center = AreaCenter;
            var halfSize = _areaSize * 0.5f;
            return position.x >= center.x - halfSize.x &&
                   position.x <= center.x + halfSize.x &&
                   position.y >= center.y - halfSize.y &&
                   position.y <= center.y + halfSize.y;
        }

        public Vector3 GetRandomPoint()
        {
            var center = AreaCenter;
            var halfSize = _areaSize * 0.5f;
            var randomX = Random.Range(center.x - halfSize.x, center.x + halfSize.x);
            var randomY = Random.Range(center.y - halfSize.y, center.y + halfSize.y);
            return new Vector3(randomX, randomY, center.z);
        }

#if UNITY_EDITOR
        void OnDrawGizmos()
        {
            if (!_showGizmos) return;

            var center = AreaCenter;
            var size = new Vector3(_areaSize.x, _areaSize.y, 0.01f);

            // 塗りつぶし
            Gizmos.color = _fillColor;
            Gizmos.DrawCube(center, size);

            // 枠線
            Gizmos.color = _wireColor;
            Gizmos.DrawWireCube(center, size);

            // 左上にラベル表示
            var topLeft = center + new Vector3(-_areaSize.x * 0.5f, _areaSize.y * 0.5f, 0);
            var style = new GUIStyle
            {
                fontSize = 10,
                normal = { textColor = _wireColor }
            };
            UnityEditor.Handles.Label(topLeft, "CharacterWalkableArea", style);
        }
#endif
    }
}
