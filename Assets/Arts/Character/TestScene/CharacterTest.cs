using UnityEngine;
using UnityEngine.InputSystem;

namespace Cat.Character
{
    public class CharactorTest : MonoBehaviour
    {
        static readonly int Run = Animator.StringToHash("Run");
        [SerializeField] Animator animator;
        [SerializeField] float moveSpeed = 5f;
        [SerializeField] Transform view;


        // 動作確認用のスクリプト
        void Update()
        {
            float moveX = 0f;
            float moveY = 0f;

            if (Keyboard.current.aKey.isPressed || Keyboard.current.leftArrowKey.isPressed)
            {
                moveX = -1f;
            }
            else if (Keyboard.current.dKey.isPressed || Keyboard.current.rightArrowKey.isPressed)
            {
                moveX = 1f;
            }

            if (Keyboard.current.wKey.isPressed || Keyboard.current.upArrowKey.isPressed)
            {
                moveY = 1f;
            }
            else if (Keyboard.current.sKey.isPressed || Keyboard.current.downArrowKey.isPressed)
            {
                moveY = -1f;
            }

            bool isRunning = Mathf.Abs(moveX) > 0f || Mathf.Abs(moveY) > 0f;
            animator.SetBool(Run, isRunning);

            if (isRunning)
            {
                Vector3 moveDirection = new Vector3(moveX, moveY, 0f).normalized;
                transform.position += moveDirection * moveSpeed * Time.deltaTime;

                if (view is { } && Mathf.Abs(moveX) > 0f)
                {
                    Vector3 scale = view.localScale;
                    scale.x = moveX < 0f ? 1f : -1f;
                    view.localScale = scale;
                }
            }
        }
    }

}
