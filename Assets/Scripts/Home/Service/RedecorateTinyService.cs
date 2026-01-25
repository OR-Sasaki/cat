using Home.View;
using VContainer.Unity;

namespace Home.Service
{
    /// UIを画面下部へ縮小するTiny機能を管理するサービス
    public class RedecorateTinyService : IStartable
    {
        readonly RedecorateUiView _redecorateUiView;

        public RedecorateTinyService(RedecorateUiView redecorateUiView)
        {
            _redecorateUiView = redecorateUiView;
        }

        public void Start()
        {
            _redecorateUiView.TinyButton.onClick.AddListener(ToggleTiny);
            _redecorateUiView.OnOpen.AddListener(_redecorateUiView.ResetTiny);
        }

        /// Tiny状態をトグルする
        public void ToggleTiny()
        {
            _redecorateUiView.SetTiny(!_redecorateUiView.IsTiny);
        }

        /// Tiny状態を設定する
        public void SetTiny(bool isTiny)
        {
            _redecorateUiView.SetTiny(isTiny);
        }
    }
}
