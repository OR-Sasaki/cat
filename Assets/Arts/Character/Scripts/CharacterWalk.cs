using System.Collections;
using UnityEngine;
using UnityEngine.AI;

namespace Cat.Character
{
    public class CharacterWalk : MonoBehaviour
    {
        static readonly int Walk = Animator.StringToHash("Walk");
        static readonly int Run = Animator.StringToHash("Run");

        [SerializeField] CharacterWalkableArea _characterWalkableArea;
        [SerializeField] float _minWaitTime = 1f;
        [SerializeField] float _maxWaitTime = 5f;
        [SerializeField] Transform _flipper;

        [SerializeField] Animator _animator;

        [SerializeField] float _walkSpeed;
        [SerializeField] float _runSpeed;

        NavMeshAgent _navMeshAgent;

        void Start()
        {
            _navMeshAgent = GetComponent<NavMeshAgent>();
            _navMeshAgent.updateRotation = false;
            _navMeshAgent.updateUpAxis = false;

            StartCoroutine(WalkRoutine());
        }

        void Update()
        {
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
                _navMeshAgent.speed = runOrWalk == 0 ? _runSpeed : _walkSpeed;
                _animator.SetBool(runOrWalk == 0 ? Run : Walk, true);

                yield return new WaitUntil(HasArrived);

                _animator.SetBool(runOrWalk == 0 ? Run : Walk, false);

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
