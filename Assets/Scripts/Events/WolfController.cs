using UnityEngine;

namespace HorseFarm.Events
{
    public class WolfController : MonoBehaviour
    {
        private Animator _animator;
        private Vector3 _targetPos;
        private float _speed = 2.0f;
        private bool _isFleeing;
        private float _flinchTimer;

        private static readonly int HashFlinch = Animator.StringToHash("Flinch");
        private static readonly int HashSpeed = Animator.StringToHash("Speed");

        private void Awake()
        {
            _animator = GetComponentInChildren<Animator>();
            if (_animator != null)
            {
                _animator.applyRootMotion = false;
            }
        }

        public void Setup(Vector3 targetBarnPos, float moveSpeed)
        {
            _targetPos = targetBarnPos;
            _speed = moveSpeed;
            _isFleeing = false;
        }

        public void SetFleeing(Vector3 awayPosition)
        {
            _isFleeing = true;
            _targetPos = awayPosition;
            _speed = 9.0f;
        }

        public void TriggerFlinch()
        {
            _flinchTimer = 0.65f;
            if (_animator != null && _animator.runtimeAnimatorController != null)
            {
                _animator.SetTrigger(HashFlinch);
            }
        }

        public void TickWolf(float deltaTime)
        {
            if (_flinchTimer > 0f)
            {
                _flinchTimer -= deltaTime;
                return;
            }

            Vector3 pos = transform.position;
            Vector3 target = new Vector3(_targetPos.x, pos.y, _targetPos.z);
            Vector3 dir = target - pos;
            dir.y = 0f;

            if (dir.sqrMagnitude > 0.02f)
            {
                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(dir.normalized), deltaTime * 8f);
                transform.position = Vector3.MoveTowards(pos, target, _speed * deltaTime);
            }

            if (_animator != null && _animator.runtimeAnimatorController != null)
            {
                _animator.SetFloat(HashSpeed, _speed);
            }

            if (_isFleeing && Vector3.Distance(transform.position, target) < 2f)
            {
                Destroy(gameObject);
            }
        }
    }
}
