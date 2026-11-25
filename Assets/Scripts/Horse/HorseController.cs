using UnityEngine;
using HorseFarm.Core;
using HorseFarm.Player;
using HorseFarm.Audio;

namespace HorseFarm.Horse
{
    public enum HorseAIState
    {
        Wander,
        Follow,
        Interact,
        Flee,
        Panic
    }

    public class HorseController : MonoBehaviour
    {
        public HorseData Data;
        public HorseAIState CurrentState = HorseAIState.Wander;

        public Vector3 PastureCenter = new Vector3(0f, 0f, 2f);
        public Vector2 PastureExtents = new Vector2(11.5f, 7.5f);

        private Animator _animator;
        private Transform _rootMotionBone;
        private Collider _myCollider;
        private bool _ignoredPlayerCollision;
        private Vector3 _targetPosition;
        private float _stateTimer;
        private bool _isGrazing;
        private float _grazeDurationTimer;
        private float _idlePauseBeforeWalkTimer;
        private float _interactTimer;
        private float _lastInteractionRealtime;

        private static readonly int HashSpeed = Animator.StringToHash("Speed");
        private static readonly int HashIsGrazing = Animator.StringToHash("IsGrazing");
        private static readonly int HashRearUp = Animator.StringToHash("RearUp");

        // Exact duration of 1x play-through of GrazeEnter (1.633s) + GrazeMotion (3.333s) slowed down by 10% (speed = 0.9x)
        private const float SingleGrazeTotalDuration = (1.6333f + 3.3333f) / 0.9f;

        private void Awake()
        {
            _animator = GetComponentInChildren<Animator>();
            if (_animator != null)
            {
                _animator.applyRootMotion = false;
            }
            _myCollider = GetComponent<Collider>();
            CacheRootMotionBone();
        }

        private void CacheRootMotionBone()
        {
            Transform[] all = GetComponentsInChildren<Transform>(true);
            foreach (Transform t in all)
            {
                if (t.name == "Root_Motion")
                {
                    _rootMotionBone = t;
                    break;
                }
            }
        }

        private void Start()
        {
            _lastInteractionRealtime = Time.time;
            EnsureIgnorePlayerCollision();
            PickNewWanderTarget();
            _stateTimer = Random.Range(5f, 9f);
        }

        private void LateUpdate()
        {
            // Lock child Root_Motion bone to local origin so _RM animation clips never slide the mesh away and snap back
            if (_rootMotionBone != null)
            {
                _rootMotionBone.localPosition = Vector3.zero;
            }
        }

        public void Initialize(HorseData data, Vector3 pastureCenter, Vector2 pastureExtents)
        {
            Data = data;
            PastureCenter = pastureCenter;
            PastureExtents = new Vector2(Mathf.Min(pastureExtents.x, 11.5f), Mathf.Min(pastureExtents.y, 7.2f));
            _lastInteractionRealtime = Time.time;
            CacheRootMotionBone();
            EnsureIgnorePlayerCollision();
            PickNewWanderTarget();
            _stateTimer = Random.Range(5f, 9f);
        }

        private void EnsureIgnorePlayerCollision()
        {
            if (_ignoredPlayerCollision) return;
            if (_myCollider == null) _myCollider = GetComponent<Collider>();
            if (_myCollider != null && PlayerController.Instance != null && PlayerController.Instance.CharacterCtrl != null)
            {
                Physics.IgnoreCollision(_myCollider, PlayerController.Instance.CharacterCtrl, true);
                _ignoredPlayerCollision = true;
            }
        }

        public void TickNeeds(float deltaTime, NeedsBalanceConfig needsCfg, RaritySettingConfig rarityCfg)
        {
            if (Data == null || needsCfg == null) return;

            float dtMin = deltaTime / 60f;
            Data.hunger = Mathf.Clamp(Data.hunger + needsCfg.hungerIncreasePerMin * dtMin, 0f, 100f);
            Data.thirst = Mathf.Clamp(Data.thirst + needsCfg.thirstIncreasePerMin * dtMin, 0f, 100f);

            if (Data.hunger >= needsCfg.criticalThreshold || Data.thirst >= needsCfg.criticalThreshold)
            {
                float lossRate = (Data.hunger >= 99f || Data.thirst >= 99f)
                    ? needsCfg.trustLossHighNeedsPerMin * 2.5f
                    : needsCfg.trustLossHighNeedsPerMin;
                Data.trust = Mathf.Clamp(Data.trust - lossRate * dtMin, 0f, 100f);
            }
            else if (Time.time - _lastInteractionRealtime > 45f)
            {
                Data.trust = Mathf.Clamp(Data.trust - needsCfg.trustLossNoInteractionPerMin * dtMin, 0f, 100f);
            }

            if (Data.isPanicking)
            {
                Data.panicRemainingSec -= deltaTime;
                float panicLoss = DataManager.Instance?.Balance?.ai != null
                    ? DataManager.Instance.Balance.ai.panicTrustLossPerSec
                    : 5f;
                Data.trust = Mathf.Clamp(Data.trust - panicLoss * deltaTime, 0f, 100f);
                if (Data.panicRemainingSec <= 0f)
                {
                    Data.isPanicking = false;
                    Data.panicRemainingSec = 0f;
                }
            }
        }

        public void TickAI(float deltaTime, Transform playerTransform, bool playerIsRunning, bool isDangerActive, Vector3 dangerPosition)
        {
            if (Data == null) return;
            EnsureIgnorePlayerCollision();

            var aiCfg = DataManager.Instance?.Balance?.ai ?? new AIBalanceConfig();

            // Priority Order (GDD 5.4): Interact > Panic > Flee > Follow > Wander
            if (_interactTimer > 0f)
            {
                _interactTimer -= deltaTime;
                _isGrazing = false;
                CurrentState = HorseAIState.Interact;
                if (playerTransform != null)
                {
                    Vector3 lookDir = playerTransform.position - transform.position;
                    lookDir.y = 0f;
                    if (lookDir.sqrMagnitude > 0.05f)
                    {
                        transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(lookDir), deltaTime * 5f);
                    }
                }
                UpdateAnimator(0f, false);
                return;
            }

            if (Data.isPanicking)
            {
                _isGrazing = false;
                CurrentState = HorseAIState.Panic;
                _stateTimer -= deltaTime;
                if (_stateTimer <= 0f || Vector3.Distance(transform.position, _targetPosition) < 1.8f)
                {
                    PickNewWanderTarget();
                    _stateTimer = Random.Range(2.0f, 3.8f);
                }
                if (!MoveTowardsTargetSafe(_targetPosition, 5.5f, deltaTime, playerTransform))
                {
                    PickNewWanderTarget();
                }
                UpdateAnimator(3f, false);
                return;
            }

            float distToPlayer = playerTransform != null ? Vector3.Distance(transform.position, playerTransform.position) : 999f;
            bool fleeFromPlayer = Data.trust < aiCfg.fleeTrustThreshold && playerIsRunning && distToPlayer < aiCfg.fleeRunDistance;
            bool fleeFromDanger = isDangerActive && Vector3.Distance(transform.position, dangerPosition) < 22f;

            if (fleeFromPlayer || fleeFromDanger)
            {
                _isGrazing = false;
                CurrentState = HorseAIState.Flee;
                Vector3 threatPos = fleeFromDanger ? dangerPosition : (playerTransform != null ? playerTransform.position : PastureCenter);
                Vector3 awayDir = (transform.position - threatPos);
                awayDir.y = 0f;
                if (awayDir.sqrMagnitude < 0.01f) awayDir = transform.forward;
                Vector3 fleeTarget = ClampToPasture(transform.position + awayDir.normalized * 7f);
                MoveTowardsTargetSafe(fleeTarget, 4.5f, deltaTime, playerTransform);
                UpdateAnimator(2.5f, false);
                return;
            }

            if (Data.trust >= aiCfg.followTrustThreshold && distToPlayer <= aiCfg.followMaxDistance && Data.hunger < 99f && Data.thirst < 99f)
            {
                _isGrazing = false;
                CurrentState = HorseAIState.Follow;
                float stopDist = Mathf.Max(aiCfg.followStopDistance, 3.0f);
                if (distToPlayer > stopDist && playerTransform != null)
                {
                    Vector3 toHorse = (transform.position - playerTransform.position);
                    toHorse.y = 0f;
                    if (toHorse.sqrMagnitude < 0.01f) toHorse = -playerTransform.forward;
                    Vector3 followPoint = playerTransform.position + toHorse.normalized * (stopDist - 0.2f);
                    followPoint.y = 0f;
                    float spd = distToPlayer > 6.5f ? 4.0f : 2.2f;
                    MoveTowardsTargetSafe(followPoint, spd, deltaTime, playerTransform);
                    UpdateAnimator(distToPlayer > 6.5f ? 2f : 1f, false);
                }
                else
                {
                    UpdateAnimator(0f, false);
                }
                return;
            }

            // Default: Wander & Single-Shot Grazing
            CurrentState = HorseAIState.Wander;

            if (distToPlayer < 2.3f && !_isGrazing)
            {
                UpdateAnimator(0f, false);
                return;
            }

            if (_isGrazing)
            {
                _grazeDurationTimer -= deltaTime;

                // Stop grazing immediately when the single-play grazing animation finishes
                bool animFinished = _grazeDurationTimer <= 0f;
                if (_animator != null && _animator.runtimeAnimatorController != null)
                {
                    AnimatorStateInfo stInfo = _animator.GetCurrentAnimatorStateInfo(0);
                    if (stInfo.IsName("Graze") && stInfo.normalizedTime >= 0.97f && !_animator.IsInTransition(0))
                    {
                        animFinished = true;
                    }
                }

                if (animFinished)
                {
                    _isGrazing = false;
                    _idlePauseBeforeWalkTimer = Random.Range(1.2f, 2.4f);
                    UpdateAnimator(0f, false);
                    PickNewWanderTarget();
                    _stateTimer = Random.Range(6f, 10f);
                }
                else
                {
                    UpdateAnimator(0f, true);
                }
            }
            else if (_idlePauseBeforeWalkTimer > 0f)
            {
                _idlePauseBeforeWalkTimer -= deltaTime;
                UpdateAnimator(0f, false);
            }
            else
            {
                _stateTimer -= deltaTime;
                float dist = Vector3.Distance(transform.position, _targetPosition);
                if (dist < 1.25f || _stateTimer <= 0f)
                {
                    // Start a single non-looping graze animation (10% slowed down)
                    _isGrazing = true;
                    _grazeDurationTimer = SingleGrazeTotalDuration;
                    UpdateAnimator(0f, true);
                }
                else
                {
                    if (!MoveTowardsTargetSafe(_targetPosition, 1.75f, deltaTime, playerTransform))
                    {
                        PickNewWanderTarget();
                        _stateTimer = Random.Range(5f, 8f);
                    }
                    else
                    {
                        UpdateAnimator(1f, false);
                    }
                }
            }
        }

        public void TriggerInteractReaction(bool rearUp = false)
        {
            _lastInteractionRealtime = Time.time;
            _interactTimer = 2.0f;
            _isGrazing = false;
            CurrentState = HorseAIState.Interact;
            if (_animator != null && rearUp)
            {
                _animator.SetTrigger(HashRearUp);
            }
            AudioManager.Instance?.PlayNeigh();
        }

        public void TriggerPanic(float durationSec)
        {
            if (Data == null) return;
            Data.isPanicking = true;
            Data.panicRemainingSec = durationSec;
            _isGrazing = false;
            CurrentState = HorseAIState.Panic;
            if (_animator != null)
            {
                _animator.SetTrigger(HashRearUp);
            }
            AudioManager.Instance?.PlayNeigh();
        }

        private bool MoveTowardsTargetSafe(Vector3 target, float speed, float deltaTime, Transform playerTransform)
        {
            Vector3 current = transform.position;
            target.y = current.y;
            Vector3 desiredDir = target - current;
            desiredDir.y = 0f;
            if (desiredDir.sqrMagnitude < 0.02f) return true;
            desiredDir.Normalize();

            if (GameManager.Instance != null)
            {
                var allHorses = GameManager.Instance.SpawnedHorses;
                for (int i = 0; i < allHorses.Count; i++)
                {
                    var other = allHorses[i];
                    if (other == null || other == this) continue;
                    Vector3 diff = current - other.transform.position;
                    diff.y = 0f;
                    float dist = diff.magnitude;
                    if (dist < 2.4f && dist > 0.001f)
                    {
                        desiredDir = (desiredDir + diff.normalized * ((2.4f - dist) * 1.2f)).normalized;
                    }
                }
            }

            if (playerTransform != null)
            {
                Vector3 fromPlayer = current - playerTransform.position;
                fromPlayer.y = 0f;
                float pDist = fromPlayer.magnitude;
                if (pDist < 2.2f && pDist > 0.001f)
                {
                    if (Vector3.Dot(desiredDir, -fromPlayer.normalized) > 0.1f)
                    {
                        return false;
                    }
                }
            }

            float[] candidateAngles = { 0f, 35f, -35f, 70f, -70f, 110f, -110f };
            Vector3 chosenDir = Vector3.zero;
            bool foundClear = false;
            float checkDist = Mathf.Max(1.45f, speed * deltaTime * 2.5f);

            for (int i = 0; i < candidateAngles.Length; i++)
            {
                Vector3 testDir = Quaternion.Euler(0f, candidateAngles[i], 0f) * desiredDir;
                Vector3 nextPos = current + testDir * (speed * deltaTime);
                if (!IsDirectionBlocked(current, testDir, checkDist) && !IsPositionOverlappingObstacle(nextPos))
                {
                    chosenDir = testDir;
                    foundClear = true;
                    break;
                }
            }

            if (!foundClear)
            {
                return false;
            }

            Quaternion targetRot = Quaternion.LookRotation(chosenDir);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, deltaTime * 5.0f);
            transform.position = current + chosenDir * (speed * deltaTime);
            return true;
        }

        private bool IsDirectionBlocked(Vector3 originPos, Vector3 dir, float checkDist)
        {
            Vector3 rayOrigin = originPos + Vector3.up * 0.85f;
            RaycastHit[] hits = Physics.SphereCastAll(rayOrigin, 0.55f, dir, checkDist, ~0, QueryTriggerInteraction.Ignore);
            for (int i = 0; i < hits.Length; i++)
            {
                Collider col = hits[i].collider;
                if (IsObstacleCollider(col))
                    return true;
            }
            return false;
        }

        private bool IsPositionOverlappingObstacle(Vector3 testPos)
        {
            Vector3 center = testPos + Vector3.up * 0.9f;
            Collider[] overlaps = Physics.OverlapSphere(center, 0.65f, ~0, QueryTriggerInteraction.Ignore);
            for (int i = 0; i < overlaps.Length; i++)
            {
                if (IsObstacleCollider(overlaps[i]))
                    return true;
            }
            return false;
        }

        private bool IsObstacleCollider(Collider col)
        {
            if (col == null) return false;
            if (col == _myCollider || col.transform.IsChildOf(transform)) return false;
            if (col.gameObject.name == "FarmGround") return false;
            if (PlayerController.Instance != null && col.transform.IsChildOf(PlayerController.Instance.transform)) return false;
            return true;
        }

        private void PickNewWanderTarget()
        {
            for (int attempt = 0; attempt < 12; attempt++)
            {
                float rx = Random.Range(-PastureExtents.x, PastureExtents.x);
                float rz = Random.Range(-PastureExtents.y, PastureExtents.y);
                Vector3 candidate = new Vector3(PastureCenter.x + rx, transform.position.y, PastureCenter.z + rz);
                if (Vector3.Distance(candidate, transform.position) > 3.5f && !IsPositionOverlappingObstacle(candidate))
                {
                    _targetPosition = candidate;
                    return;
                }
            }
            _targetPosition = PastureCenter;
        }

        private Vector3 ClampToPasture(Vector3 pos)
        {
            pos.x = Mathf.Clamp(pos.x, PastureCenter.x - PastureExtents.x, PastureCenter.x + PastureExtents.x);
            pos.z = Mathf.Clamp(pos.z, PastureCenter.z - PastureExtents.y, PastureCenter.z + PastureExtents.y);
            return pos;
        }

        private void UpdateAnimator(float speedParam, bool grazing)
        {
            if (_animator == null || _animator.runtimeAnimatorController == null) return;
            _animator.speed = grazing ? 0.9f : 1.0f;
            _animator.SetFloat(HashSpeed, speedParam);
            _animator.SetBool(HashIsGrazing, grazing);
        }

        public string GetStateDisplayKey()
        {
            switch (CurrentState)
            {
                case HorseAIState.Follow: return "horse_state_follow";
                case HorseAIState.Interact: return "horse_state_interact";
                case HorseAIState.Flee: return "horse_state_flee";
                case HorseAIState.Panic: return "horse_state_panic";
                default: return "horse_state_wander";
            }
        }
    }
}
