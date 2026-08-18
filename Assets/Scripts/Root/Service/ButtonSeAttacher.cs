#nullable enable

using Root.View;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Root.Service
{
    /// シーン内・生成階層内の Button を走査し、既定クリック SE を再生する ButtonSe を自動付与する
    public sealed class ButtonSeAttacher
    {
        public void AttachToScene(Scene scene)
        {
            foreach (var rootObject in scene.GetRootGameObjects())
            {
                AttachToHierarchy(rootObject);
            }
        }

        public void AttachToHierarchy(GameObject root)
        {
            var buttons = root.GetComponentsInChildren<Button>(true);
            foreach (var button in buttons)
            {
                AttachToButton(button);
            }
        }

        void AttachToButton(Button button)
        {
            if (button.TryGetComponent<ButtonSe>(out _))
            {
                return;
            }

            button.gameObject.AddComponent<ButtonSe>();
        }
    }
}
