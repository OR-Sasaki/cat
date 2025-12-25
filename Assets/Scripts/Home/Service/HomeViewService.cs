using Home.State;
using Home.View;
using UnityEngine;
using VContainer.Unity;

namespace Home.Service
{
    public class HomeViewService : IInitializable
    {
        readonly HomeState _homeState;
        readonly HomeUiView _homeUiView;
        readonly ClosetUiView _closetUiView;
        readonly CameraView _cameraView;

        public HomeViewService(HomeState homeState, HomeUiView homeUiView, ClosetUiView closetUiView, CameraView cameraView)
        {
            _homeState = homeState;
            _homeUiView = homeUiView;
            _closetUiView = closetUiView;
            _cameraView = cameraView;
        }

        public void Initialize()
        {
            _homeState.OnStateChange.AddListener(OnStateChange);

            // 初期状態を設定
            _homeUiView.gameObject.SetActive(true);
            _homeUiView.SetBlocksRaycast(true);
            _closetUiView.gameObject.SetActive(false);
        }

        void OnStateChange(HomeState.State state)
        {
            Debug.Log($"OnStateChange: {state}");
            _cameraView.SetState(state);
            switch (state)
            {
                case HomeState.State.Home:
                    ShowHomeView();
                    break;
                case HomeState.State.Closet:
                    ShowClosetView();
                    break;
            }
        }

        void ShowHomeView()
        {
            // クローゼットを閉じる
            _closetUiView.PlayAnimation(UiView.AnimationType.Out);
            _closetUiView.SetBlocksRaycast(false);
            _closetUiView.OnAnimationEnd.AddListener(() =>
            {
                _closetUiView.gameObject.SetActive(false);
            });

            // ホームを開く
            _homeUiView.gameObject.SetActive(true);
            _homeUiView.SetBlocksRaycast(true);
            _homeUiView.PlayAnimation(UiView.AnimationType.In);
        }

        void ShowClosetView()
        {
            // ホームを閉じる
            _homeUiView.PlayAnimation(UiView.AnimationType.Out);
            _homeUiView.SetBlocksRaycast(false);
            _homeUiView.OnAnimationEnd.AddListener(() =>
            {
                _homeUiView.gameObject.SetActive(false);
            });

            // クローゼットを開く
            _closetUiView.gameObject.SetActive(true);
            _closetUiView.SetBlocksRaycast(true);
            _closetUiView.PlayAnimation(UiView.AnimationType.In);
        }
    }
}

