using UnityEngine;
using UnityEngine.Events;

namespace Home.State
{
    public class HomeState
    {
        public enum State
        {
            Home,
            Redecorate,
            Closet,
            Timer,
            Shop,
            History
        }

        public State Current { get; private set; } =  State.Home;
        public UnityEvent<State> OnStateChange = new();

        public void ForceSetState(State state)
        {
            Current = state;
            OnStateChange.Invoke(Current);
        }

        public void SetState(State state)
        {
            if (state == Current)
                return;

            Current = state;
            OnStateChange.Invoke(Current);
        }
    }
}
