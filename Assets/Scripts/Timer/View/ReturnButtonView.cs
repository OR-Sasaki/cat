using Timer.Service;
using UnityEngine;
using UnityEngine.UI;
using VContainer;

namespace Timer.View
{
    public class ReturnButtonView : MonoBehaviour
    {
        [SerializeField] Button _backButton;

        [Inject]
        public void Init(ReturnButtonService returnButtonService)
        {
            _backButton.onClick.AddListener(() => returnButtonService.NavigateToScene(Const.SceneName.Home));
        }
    }
}

