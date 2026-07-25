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

        readonly List<ScrollLayer> _layers = new List<ScrollLayer>();
        PomodoroState _state;
        bool _isScrolling = true;

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

            var deltaTime = Time.deltaTime;
            foreach (var layer in _layers)
            {
                layer.Scroll(deltaTime);
            }
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
