using Root.Service;
using UnityEngine;
using UnityEngine.UI;
using VContainer;

namespace Title.View
{
    public class TitleStartButtonView : MonoBehaviour
    {
        [SerializeField] Button _startButton;

        [Inject]
        public void Init(SceneLoader sceneLoader)
        {
            _startButton.onClick.AddListener(() => sceneLoader.Load(Const.SceneName.Home));
        }
    }
}
