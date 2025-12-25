using System;
using System.Collections.Generic;
using Home.State;
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

        public void SetState(HomeState.State state)
        {
            foreach (var c in contexts)
            {
                c.gameObject.SetActive(c.state == state);
            }
        }
    }
}
