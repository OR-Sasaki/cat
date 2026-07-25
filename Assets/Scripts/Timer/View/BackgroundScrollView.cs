using System;
using System.Collections.Generic;
using Timer.State;
using UnityEngine;
using VContainer;

namespace Timer.View
{
    public class BackgroundScrollView : MonoBehaviour
    {
        [SerializeField] ScrollElement[] _elements;
        [SerializeField, Min(0f)] float _breakDecelerationSeconds = 2f;
        [SerializeField, Min(0f)] float _focusAccelerationSeconds = 1.2f;
        [SerializeField] AnimationCurve _rampCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

        readonly List<ScrollLayer> _layers = new List<ScrollLayer>();
        PomodoroState _state;
        float _speedFactor = 1f;
        float _rampFrom = 1f;
        float _rampTarget = 1f;
        float _rampSeconds;
        float _rampElapsed;

        /// 現在のスクロール速度係数（0=停止、1=全速）。キャラクター側が走行速度の同期に使う
        public float CurrentSpeedFactor => _speedFactor;

        /// 休憩フェーズの減速が完了しスクロールが停止した瞬間に発火する
        public event Action BreakScrollStopped;

        [Inject]
        public void Construct(PomodoroState state)
        {
            _state = state;
        }

        void Awake()
        {
            BuildLayers();
        }

        void Start()
        {
            _state.OnPhaseChanged += OnPhaseChanged;
        }

        void OnDestroy()
        {
            if (_state == null) return;
            _state.OnPhaseChanged -= OnPhaseChanged;
        }

        void Update()
        {
            if (_state.IsPaused) return;

            UpdateRamp(Time.deltaTime);

            if (_speedFactor <= 0f) return;

            var deltaTime = Time.deltaTime * _speedFactor;
            foreach (var layer in _layers)
            {
                layer.Scroll(deltaTime);
            }
        }

        void UpdateRamp(float deltaTime)
        {
            if (_rampElapsed >= _rampSeconds) return;

            _rampElapsed += deltaTime;
            var t = Mathf.Clamp01(_rampElapsed / _rampSeconds);
            _speedFactor = Mathf.LerpUnclamped(_rampFrom, _rampTarget, _rampCurve.Evaluate(t));

            if (t < 1f) return;
            _speedFactor = _rampTarget;
            NotifyIfBreakScrollStopped();
        }

        void OnPhaseChanged(PomodoroPhase phase)
        {
            switch (phase)
            {
                case PomodoroPhase.Focus:
                    StartRamp(1f, _focusAccelerationSeconds);
                    break;
                case PomodoroPhase.Break:
                    StartRamp(0f, _breakDecelerationSeconds);
                    break;
                case PomodoroPhase.Complete:
                    SetFactorImmediate(0f);
                    break;
            }
        }

        void StartRamp(float target, float seconds)
        {
            if (seconds <= 0f)
            {
                SetFactorImmediate(target);
                return;
            }

            _rampFrom = _speedFactor;
            _rampTarget = target;
            _rampSeconds = seconds;
            _rampElapsed = 0f;
        }

        void SetFactorImmediate(float target)
        {
            _speedFactor = target;
            _rampTarget = target;
            _rampSeconds = 0f;
            _rampElapsed = 0f;
            NotifyIfBreakScrollStopped();
        }

        void NotifyIfBreakScrollStopped()
        {
            if (_speedFactor > 0f) return;
            if (_state.CurrentPhase != PomodoroPhase.Break) return;
            BreakScrollStopped?.Invoke();
        }

        // Tiles that share a scroll speed belong to the same parallax layer. Each layer
        // measures its loop width from the tiles' authored positions instead of a
        // hand-entered value, so the seamless loop always matches the actual artwork.
        void BuildLayers()
        {
            _layers.Clear();
            if (_elements == null) return;

            var grouped = new Dictionary<float, List<ScrollElement>>();
            foreach (var element in _elements)
            {
                if (element == null || element.Transform == null) continue;

                if (!grouped.TryGetValue(element.ScrollSpeed, out var siblings))
                {
                    siblings = new List<ScrollElement>();
                    grouped.Add(element.ScrollSpeed, siblings);
                }
                siblings.Add(element);
            }

            foreach (var siblings in grouped.Values)
            {
                var layer = ScrollLayer.TryCreate(siblings);
                if (layer != null)
                {
                    _layers.Add(layer);
                }
                else
                {
                    Debug.LogWarning(
                        "[BackgroundScrollView] A parallax layer needs at least 2 tiles at " +
                        "different positions to loop seamlessly; skipping one.", this);
                }
            }
        }

        [Serializable]
        class ScrollElement
        {
            [SerializeField] Transform _transform;
            [SerializeField] float _scrollSpeed = 2f;

            public Transform Transform => _transform;
            public float ScrollSpeed => _scrollSpeed;
        }

        // A single parallax layer made of two or more identical tiles laid edge-to-edge.
        // Tiles scroll right together; when one passes the layer's right edge it is
        // re-anchored one spacing behind the current left-most tile. Because the spacing is
        // derived from the authored layout and recycling is relative to a live sibling, the
        // loop can neither be misconfigured nor accumulate drift over long sessions.
        class ScrollLayer
        {
            readonly Transform[] _tiles;
            readonly float _scrollSpeed;
            readonly float _spacing;
            readonly float _rightEdgeX;

            ScrollLayer(Transform[] tiles, float scrollSpeed, float spacing, float rightEdgeX)
            {
                _tiles = tiles;
                _scrollSpeed = scrollSpeed;
                _spacing = spacing;
                _rightEdgeX = rightEdgeX;
            }

            // Builds a layer from same-speed tiles, deriving the spacing from their authored
            // positions. Returns null when the tiles cannot form a loop (fewer than two, or
            // all at the same position).
            public static ScrollLayer TryCreate(List<ScrollElement> elements)
            {
                if (elements == null || elements.Count < 2) return null;

                var tiles = new Transform[elements.Count];
                var minX = float.PositiveInfinity;
                var maxX = float.NegativeInfinity;
                for (int i = 0; i < elements.Count; i++)
                {
                    tiles[i] = elements[i].Transform;
                    var x = tiles[i].localPosition.x;
                    if (x < minX) minX = x;
                    if (x > maxX) maxX = x;
                }

                var spacing = (maxX - minX) / (elements.Count - 1);
                if (spacing <= 0f) return null;

                return new ScrollLayer(tiles, elements[0].ScrollSpeed, spacing, maxX);
            }

            public void Scroll(float deltaTime)
            {
                var delta = _scrollSpeed * deltaTime;
                foreach (var tile in _tiles)
                {
                    var pos = tile.localPosition;
                    pos.x += delta;
                    tile.localPosition = pos;
                }

                // Recycle every tile that has scrolled past the authored right edge, placing
                // it one spacing behind the current left-most tile so the seam stays exact.
                // The safety bound guards against a pathologically large delta looping forever.
                var safety = _tiles.Length;
                while (safety-- > 0 && TryFindTileToRecycle(out var tile, out var leftmostX))
                {
                    var pos = tile.localPosition;
                    pos.x = leftmostX - _spacing;
                    tile.localPosition = pos;
                }
            }

            bool TryFindTileToRecycle(out Transform rightmost, out float leftmostX)
            {
                rightmost = null;
                leftmostX = float.PositiveInfinity;
                var maxX = float.NegativeInfinity;
                foreach (var tile in _tiles)
                {
                    var x = tile.localPosition.x;
                    if (x < leftmostX) leftmostX = x;
                    if (x > maxX)
                    {
                        maxX = x;
                        rightmost = tile;
                    }
                }
                return maxX >= _rightEdgeX;
            }
        }
    }
}
