using Root.Service;
using UnityEngine;
using UnityEngine.UI;
using VContainer;

namespace Redecorate.View
{
    public class ReturnButtonView : MonoBehaviour
    {
        [SerializeField] Button _backButton;

        [Inject]
        public void Init(SceneLoader sceneLoader)
        {
            _backButton.onClick.AddListener(() => sceneLoader.Load(Const.SceneName.Home));
        }
    }
}

