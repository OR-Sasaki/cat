using System.Collections;
using UnityEngine;
using UnityEngine.AI;

namespace Cat.Character
{
    public class CharacterWalk : MonoBehaviour
    {
        static readonly int Walk = Animator.StringToHash("Walk");
        static readonly int Run = Animator.StringToHash("Run");
        static readonly int Speed = Animator.StringToHash("Speed");

        [SerializeField] CharacterWalkableArea _characterWalkableArea;
        [SerializeField] float _minWaitTime = 1f;
        [SerializeField] float _maxWaitTime = 5f;
        [SerializeField] Transform _flipper;

        [SerializeField] Animator _animator;

        [SerializeField] float _walkSpeed;
        [SerializeField] float _runSpeed;

        /// Walk.anim / Run.anim が接地するように作られた移動速度。
        /// CatLocomotionClipBuilder と同じ値にしておく (変えたらクリップを作り直す)
        [SerializeField] float _walkClipSpeed = 0.9f;
        [SerializeField] float _runClipSpeed = 1.9f;

        NavMeshAgent _navMeshAgent;
        float _currentClipSpeed = 1f;

        void Start()
        {
            _navMeshAgent = GetComponent<NavMeshAgent>();
            _navMeshAgent.updateRotation = false;
            _navMeshAgent.updateUpAxis = false;

            StartCoroutine(WalkRoutine());
        }

        void Update()
        {
            // 歩幅はクリップに焼かれているので、実速度に合わせて再生速度を変えないと加減速中に足が滑る
            _animator.SetFloat(Speed, Mathf.Clamp(_navMeshAgent.velocity.magnitude / _currentClipSpeed, 0.2f, 2f));

            if (_navMeshAgent.velocity.magnitude < 0.1f)
                return;

            var scaleX = _navMeshAgent.velocity.x > 0 ? -1f : 1f;
            _flipper.localScale = new Vector3(scaleX, _flipper.localScale.y, _flipper.localScale.z);
        }

        IEnumerator WalkRoutine()
        {
            while (true)
            {
                var destination = _characterWalkableArea.GetRandomPoint();
                _navMeshAgent.SetDestination(destination);

                var runOrWalk = Random.Range(0, 2);
                var isRun = runOrWalk == 0;
                _navMeshAgent.speed = isRun ? _runSpeed : _walkSpeed;
                _currentClipSpeed = isRun ? _runClipSpeed : _walkClipSpeed;
                _animator.SetBool(isRun ? Run : Walk, true);

                yield return new WaitUntil(HasArrived);

                _animator.SetBool(isRun ? Run : Walk, false);

                var waitTime = Random.Range(_minWaitTime, _maxWaitTime);
                yield return new WaitForSeconds(waitTime);
            }
        }

        bool HasArrived()
        {
            if (_navMeshAgent.pathPending) return false;
            if (_navMeshAgent.remainingDistance > _navMeshAgent.stoppingDistance) return false;
            return _navMeshAgent.velocity.sqrMagnitude < 0.01f;
        }
    }
}
