using Home.State;

namespace Home.Service
{
    public class HomeStateSetService
    {
        readonly HomeState _homeState;

        public HomeStateSetService(HomeState homeState)
        {
            _homeState = homeState;
        }

        public void SetState(HomeState.State state)
        {
            _homeState.SetState(state);
        }
    }
}
