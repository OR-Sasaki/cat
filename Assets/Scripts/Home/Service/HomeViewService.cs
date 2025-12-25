using System.Collections.Generic;
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
        readonly Dictionary<HomeState.State, UiView> _stateViewMap;

        public HomeViewService(HomeState homeState, HomeUiView homeUiView, ClosetUiView closetUiView, CameraView cameraView)
        {
            _homeState = homeState;
            _homeUiView = homeUiView;
            _closetUiView = closetUiView;
            _cameraView = cameraView;

            _stateViewMap = new Dictionary<HomeState.State, UiView>
            {
                { HomeState.State.Home, _homeUiView },
                { HomeState.State.Closet, _closetUiView }
            };
        }

        public void Initialize()
        {
            _homeState.OnStateChange.AddListener(OnStateChange);

            // 初期状態を設定
            _homeUiView.gameObject.SetActive(true);
            _homeUiView.SetBlocksRaycast(true);
            _closetUiView.gameObject.SetActive(false);
        }

        void OnStateChange(HomeState.State previous, HomeState.State current)
        {
            _cameraView.SetState(current);

            // 前のステートのViewを閉じる
            CloseView(previous);

            // 新しいステートのViewを開く
            OpenView(current);
        }

        void CloseView(HomeState.State state)
        {
            if (!_stateViewMap.TryGetValue(state, out var view))
                return;

            view.PlayAnimation(UiView.AnimationType.Out);
            view.SetBlocksRaycast(false);
            view.OnAnimationEnd.AddListener(() =>
            {
                view.gameObject.SetActive(false);
            });
        }

        void OpenView(HomeState.State state)
        {
            if (!_stateViewMap.TryGetValue(state, out var view))
                return;

            view.gameObject.SetActive(true);
            view.SetBlocksRaycast(true);
            view.PlayAnimation(UiView.AnimationType.In);
        }
    }
}

