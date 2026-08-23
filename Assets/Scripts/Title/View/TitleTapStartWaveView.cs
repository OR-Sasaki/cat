using TMPro;
using UnityEngine;

namespace Title.View
{
    /// テキストを 1 文字ずつ上下に波打たせる
    public class TitleTapStartWaveView : MonoBehaviour
    {
        [SerializeField] TMP_Text _text;
        /// 波の高さ (px)
        [SerializeField] float _amplitude = 8f;
        /// 波の速さ (rad/秒)
        [SerializeField] float _speed = 4f;
        /// 隣の文字との位相差 (rad)
        [SerializeField] float _phaseOffset = 0.45f;

        void Reset()
        {
            _text = GetComponent<TMP_Text>();
        }

        void Awake()
        {
            if (_text == null) _text = GetComponent<TMP_Text>();
        }

        void Update()
        {
            if (_text == null) return;

            _text.ForceMeshUpdate();
            var textInfo = _text.textInfo;

            for (var i = 0; i < textInfo.characterCount; i++)
            {
                var charInfo = textInfo.characterInfo[i];
                if (!charInfo.isVisible) continue;

                var vertices = textInfo.meshInfo[charInfo.materialReferenceIndex].vertices;
                var offset = Mathf.Sin(Time.time * _speed - i * _phaseOffset) * _amplitude;
                for (var j = 0; j < 4; j++)
                {
                    vertices[charInfo.vertexIndex + j].y += offset;
                }
            }

            for (var i = 0; i < textInfo.meshInfo.Length; i++)
            {
                var meshInfo = textInfo.meshInfo[i];
                meshInfo.mesh.vertices = meshInfo.vertices;
                _text.UpdateGeometry(meshInfo.mesh, i);
            }
        }
    }
}
