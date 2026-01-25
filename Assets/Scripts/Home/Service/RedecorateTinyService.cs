using Home.View;
using UnityEngine;
using VContainer.Unity;

namespace Home.Service
{
    /// UIを画面下部へ縮小するTiny機能を管理するサービス
    public class RedecorateTinyService : IStartable
    {
        static readonly int TinyParam = Animator.StringToHash("Tiny");

        readonly RedecorateUiView _redecorateUiView;

        bool _isTiny;

        public bool IsTiny => _isTiny;

        public RedecorateTinyService(RedecorateUiView redecorateUiView)
        {
            _redecorateUiView = redecorateUiView;
        }

        public void Start()
        {
            _redecorateUiView.TinyButton.onClick.AddListener(ToggleTiny);
            _redecorateUiView.OnOpen.AddListener(ResetTiny);
        }

        /// Tiny状態をトグルする
        public void ToggleTiny()
        {
            SetTiny(!_isTiny);
        }

        /// Tiny状態を設定する
        public void SetTiny(bool isTiny)
        {
            _isTiny = isTiny;
            _redecorateUiView.TinyAnimator.SetBool(TinyParam, _isTiny);
        }

        /// Tiny状態をリセットする（画面を開いた時に呼ばれる）
        void ResetTiny()
        {
            _isTiny = false;
            _redecorateUiView.TinyAnimator.SetBool(TinyParam, false);
        }
    }
}
