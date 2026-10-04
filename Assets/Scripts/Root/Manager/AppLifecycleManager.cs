#nullable enable

using Root.Service;
using UnityEngine;
using VContainer;

namespace Root.Manager
{
    /// アプリのバックグラウンド遷移 / 終了を捕捉し、PlayerPrefs をディスクへ確定させる。
    /// PlayerPrefs.SetString はメモリキャッシュを書き換えるだけで、Unity が自動で書き出すのは
    /// OnApplicationQuit のみ。モバイルではバックグラウンドのまま OS に終了されると
    /// OnApplicationQuit が呼ばれず、そのセッションの保存内容が丸ごと失われるため、
    /// バックグラウンド遷移時に必ず書き出す。
    /// PlayerPrefsService は Pure C# クラスでライフサイクルフックを持たないため、
    /// MonoBehaviour 側で OnApplicationPause / OnApplicationQuit を捕捉する。
    /// RootScope.prefab (DontDestroyOnLoad) に置くので全シーンで効く
    public class AppLifecycleManager : MonoBehaviour
    {
        PlayerPrefsService? _playerPrefsService;

        [Inject]
        public void Construct(PlayerPrefsService playerPrefsService)
        {
            _playerPrefsService = playerPrefsService;
        }

        void OnApplicationPause(bool pause)
        {
            if (!pause) return;

            _playerPrefsService?.Flush();
        }

        /// 正常終了時は Unity 側でも書き出されるが、Flush は冪等なので保険として呼ぶ
        void OnApplicationQuit()
        {
            _playerPrefsService?.Flush();
        }
    }
}
