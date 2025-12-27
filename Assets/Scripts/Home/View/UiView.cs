using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Playables;
using UnityEngine.Timeline;

namespace Home.View
{
    public abstract class UiView : MonoBehaviour
    {
        [SerializeField] PlayableDirector _playableDirector;
        [SerializeField] CanvasGroup _canvasGroup;

        [SerializeField] PlayableAsset _inPlayable;
        [SerializeField] PlayableAsset _outPlayable;

        public readonly UnityEvent OnAnimationEnd = new ();
        public readonly UnityEvent OnOpen = new();

        public enum AnimationType
        {
            In,
            Out
        }

        void Start()
        {
            _playableDirector.stopped += OnEnd;
        }

        void OnEnd(PlayableDirector _)
        {
            OnAnimationEnd.Invoke();
            OnAnimationEnd.RemoveAllListeners();
        }

        public void PlayAnimation(AnimationType type)
        {
            _playableDirector.playableAsset = type == AnimationType.In ? _inPlayable :  _outPlayable;
            _playableDirector.Play();
        }

        public void SetBlocksRaycast(bool value)
        {
            _canvasGroup.blocksRaycasts = value;
        }

        // UIを開いた時のイベント
        public void Open()
        {
            OnOpen.Invoke();
        }
    }
}
