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

        public State Current { get; private set; } = State.Home;
        public UnityEvent<State, State> OnStateChange = new();

        public void ForceSetState(State state)
        {
            var previous = Current;
            Current = state;
            OnStateChange.Invoke(previous, Current);
        }

        public void SetState(State state)
        {
            if (state == Current)
                return;

            var previous = Current;
            Current = state;
            OnStateChange.Invoke(previous, Current);
        }
    }
}
