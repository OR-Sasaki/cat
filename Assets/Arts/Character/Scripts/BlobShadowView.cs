using UnityEngine;

namespace Cat.Character
{
    [RequireComponent(typeof(SpriteRenderer))]
    public class BlobShadowView : MonoBehaviour
    {
        [SerializeField] SpriteRenderer _spriteRenderer;
        [SerializeField, Min(0f)] float _width = 1f;
        [SerializeField, Min(0f)] float _height = 0.5f;
        [SerializeField, Range(0f, 1f)] float _opacity = 0.35f;
        [SerializeField] Vector2 _offset;

        void Awake()
        {
            if (_spriteRenderer == null)
            {
                Debug.LogError("[BlobShadowView] SpriteRenderer が設定されていません");
                return;
            }

            Apply();
        }

        void OnValidate()
        {
            if (_spriteRenderer == null)
            {
                _spriteRenderer = GetComponent<SpriteRenderer>();
            }

            Apply();
        }

        /// localScale・localPosition・不透明度を現在の値に反映する
        void Apply()
        {
            transform.localScale = new Vector3(_width, _height, 1f);
            transform.localPosition = _offset;

            var color = _spriteRenderer.color;
            color.a = _opacity;
            _spriteRenderer.color = color;
        }
    }
}
