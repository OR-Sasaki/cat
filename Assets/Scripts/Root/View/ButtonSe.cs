#nullable enable

using Root.Service;
using UnityEngine;
using UnityEngine.UI;

namespace Root.View
{
    /// ボタンのクリック SE を既定から上書きする、または動的生成ボタンへ事前付与するための自己配線コンポーネント
    [RequireComponent(typeof(Button))]
    public sealed class ButtonSe : MonoBehaviour
    {
        [SerializeField] SeId _seId = SeId.Click;

        void Awake()
        {
            if (_seId == SeId.None)
            {
                return;
            }

            var button = GetComponent<Button>();
            // クリック時点で Current を評価する遅延参照にし、DI 構築順に依存しないようにする
            button.onClick.AddListener(() => AudioServiceHandle.Current?.PlaySe(_seId));
        }
    }
}
