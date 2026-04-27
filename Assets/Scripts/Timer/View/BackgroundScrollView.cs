using System;
using Timer.State;
using UnityEngine;
using VContainer;

namespace Timer.View
{
    public class BackgroundScrollView : MonoBehaviour
    {
        [SerializeField] ScrollElement[] _elements;

        PomodoroState _state;
        bool _isScrolling = true;

        [Inject]
        public void Construct(PomodoroState state)
        {
            _state = state;
        }

        void Start()
        {
            _state.OnPhaseChanged += OnPhaseChanged;
            _state.OnPauseChanged += OnPauseChanged;
        }

        void OnDestroy()
        {
            if (_state == null) return;
            _state.OnPhaseChanged -= OnPhaseChanged;
            _state.OnPauseChanged -= OnPauseChanged;
        }

        void Update()
        {
            if (!_isScrolling) return;

            foreach (var element in _elements)
            {
                element.Scroll();
            }
        }

        void OnPhaseChanged(PomodoroPhase phase)
        {
            _isScrolling = phase != PomodoroPhase.Complete;
        }

        void OnPauseChanged(bool paused)
        {
            _isScrolling = !paused && _state.CurrentPhase != PomodoroPhase.Complete;
        }

        [Serializable]
        class ScrollElement
        {
            [SerializeField] Transform _transform;
            [SerializeField] float _scrollSpeed = 2f;
            [SerializeField] float _width = 20f;

            public void Scroll()
            {
                _transform.Translate(Vector3.right * (_scrollSpeed * Time.deltaTime));

                var pos = _transform.localPosition;
                if (pos.x >= _width)
                {
                    _transform.localPosition = new Vector3(pos.x - _width * 2f, pos.y, pos.z);
                }
            }
        }
    }
}
