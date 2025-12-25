using Home.State;

namespace Home.Service
{
    public class HomeFooterService
    {
        readonly HomeState _homeState;

        public HomeFooterService(HomeState homeState)
        {
            _homeState = homeState;
        }

        public void SetState(HomeState.State state)
        {
            _homeState.SetState(state);
        }
    }
}
