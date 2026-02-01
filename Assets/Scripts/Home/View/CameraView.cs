using System;
using System.Collections.Generic;
using System.Linq;
using Home.State;
using Unity.Cinemachine;
using UnityEngine;

namespace Home.View
{
    public class CameraView : MonoBehaviour
    {
        [Serializable]
        class Context
        {
            public GameObject gameObject;
            public HomeState.State state;
        }
        [SerializeField] List<Context> contexts;

        readonly Dictionary<HomeState.State, CinemachineCamera> _cinemachineCameraCache = new();

        public CinemachineCamera GetCinemachineCamera(HomeState.State state)
        {
            if (_cinemachineCameraCache.TryGetValue(state, out var cached))
            {
                return cached;
            }

            var context = contexts.FirstOrDefault(c => c.state == state);
            if (context == null) return null;

            var cinemachineCamera = context.gameObject.GetComponentInChildren<CinemachineCamera>();
            if (cinemachineCamera != null)
            {
                _cinemachineCameraCache[state] = cinemachineCamera;
            }
            return cinemachineCamera;
        }

        public void SetState(HomeState.State state)
        {
            foreach (var c in contexts)
            {
                c.gameObject.SetActive(c.state == state);
            }
        }
    }
}
