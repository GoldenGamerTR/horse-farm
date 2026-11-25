using System.Collections.Generic;
using UnityEngine;
using HorseFarm.Core;
using HorseFarm.Audio;

namespace HorseFarm.Player
{
    [RequireComponent(typeof(CharacterController))]
    public class PlayerController : MonoBehaviour
    {
        public static PlayerController Instance { get; private set; }

        public Transform cameraTransform;
        public float cameraDistance = 5.4f;
        public float cameraHeight = 1.95f;
        public float mouseSensitivity = 0.085f;
        public float cameraRotationSmoothTime = 0.065f;
        public float cameraFollowSmoothTime = 0.045f;

        public bool IsRunning { get; private set; }
        public CharacterController CharacterCtrl => _cc;
        public float ScreamCooldownRemaining => Mathf.Max(0f, _nextScreamAllowedTime - Time.time);

        private CharacterController _cc;
        private float _verticalVelocity;
        private Vector3 _smoothedMoveDir;

        // Smooth Camera State
        private float _targetCameraYaw = 0f;
        private float _targetCameraPitch = 16f;
        private float _currentCameraYaw = 0f;
        private float _currentCameraPitch = 16f;
        private float _yawVelocity;
        private float _pitchVelocity;
        private Vector3 _smoothedFocusPoint;
        private Vector3 _focusPointVelocity;

        // Character Turning State
        private float _characterTurnVelocity;

        // Action Timers
        private float _nextScreamAllowedTime;
        private float _screamAnimTimer;
        private float _interactAnimTimer;
        private float _walkCycle;
        private float _animMoveWeight;
        private float _footstepTimer;

        // Cowboy Skeleton Bones
        private Transform _bonePelvis;
        private Transform _boneSpine;
        private Transform _boneSpine1;
        private Transform _boneNeck;
        private Transform _boneHead;
        private Transform _boneLThigh;
        private Transform _boneRThigh;
        private Transform _boneLCalf;
        private Transform _boneRCalf;
        private Transform _boneLFoot;
        private Transform _boneRFoot;
        private Transform _boneLUpperArm;
        private Transform _boneRUpperArm;
        private Transform _boneLForearm;
        private Transform _boneRForearm;

        private struct BoneBindInfo
        {
            public Quaternion localBindRotation;
            public Vector3 pitchAxisInParent; // Character Right (+X) in parent's local space
            public Vector3 yawAxisInParent;   // Character Up (+Y) in parent's local space
            public Vector3 rollAxisInParent;  // Character Forward (+Z) in parent's local space
        }

        private readonly Dictionary<Transform, BoneBindInfo> _boneBinds = new Dictionary<Transform, BoneBindInfo>();
        private Vector3 _pelvisBindLocalPos;

        // Visual scream wave ring
        private Transform _screamRing;
        private Renderer _screamRingRenderer;

        private void Awake()
        {
            Instance = this;
            _cc = GetComponent<CharacterController>();
            CacheCowboyBones();
            CreateScreamVisualRing();
        }

        private void Start()
        {
            if (cameraTransform == null && Camera.main != null)
                cameraTransform = Camera.main.transform;

            _targetCameraYaw = transform.eulerAngles.y;
            _currentCameraYaw = _targetCameraYaw;
            _targetCameraPitch = 16f;
            _currentCameraPitch = 16f;
            _smoothedFocusPoint = transform.position + Vector3.up * cameraHeight;
        }

        private void OnDisable()
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        private void CacheCowboyBones()
        {
            Transform[] all = GetComponentsInChildren<Transform>(true);
            foreach (Transform t in all)
            {
                switch (t.name)
                {
                    case "Pelvis": _bonePelvis = t; _pelvisBindLocalPos = t.localPosition; break;
                    case "Spine": _boneSpine = t; break;
                    case "Spine1": _boneSpine1 = t; break;
                    case "Neck": _boneNeck = t; break;
                    case "Head": _boneHead = t; break;
                    case "L Thigh": _boneLThigh = t; break;
                    case "R Thigh": _boneRThigh = t; break;
                    case "L Calf": _boneLCalf = t; break;
                    case "R Calf": _boneRCalf = t; break;
                    case "L Foot": _boneLFoot = t; break;
                    case "R Foot": _boneRFoot = t; break;
                    case "L UpperArm": _boneLUpperArm = t; break;
                    case "R UpperArm": _boneRUpperArm = t; break;
                    case "L Forearm": _boneLForearm = t; break;
                    case "R Forearm": _boneRForearm = t; break;
                    case "CowBoys_Pisto":
                        t.gameObject.SetActive(false);
                        break;
                }
            }

            RecordBind(_bonePelvis);
            RecordBind(_boneSpine);
            RecordBind(_boneSpine1);
            RecordBind(_boneNeck);
            RecordBind(_boneHead);
            RecordBind(_boneLThigh);
            RecordBind(_boneRThigh);
            RecordBind(_boneLCalf);
            RecordBind(_boneRCalf);
            RecordBind(_boneLFoot);
            RecordBind(_boneRFoot);
            RecordBind(_boneLUpperArm);
            RecordBind(_boneRUpperArm);
            RecordBind(_boneLForearm);
            RecordBind(_boneRForearm);
        }

        private void RecordBind(Transform bone)
        {
            if (bone == null || bone.parent == null || _boneBinds.ContainsKey(bone)) return;

            BoneBindInfo info = new BoneBindInfo
            {
                localBindRotation = bone.localRotation,
                pitchAxisInParent = bone.parent.InverseTransformDirection(transform.right).normalized,
                yawAxisInParent = bone.parent.InverseTransformDirection(transform.up).normalized,
                rollAxisInParent = bone.parent.InverseTransformDirection(transform.forward).normalized
            };
            _boneBinds[bone] = info;
        }

        private void CreateScreamVisualRing()
        {
            GameObject ringObj = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            ringObj.name = "ScreamSonicWave";
            Destroy(ringObj.GetComponent<Collider>());
            ringObj.transform.SetParent(transform, false);
            ringObj.transform.localPosition = new Vector3(0f, 1.0f, 0f);
            ringObj.transform.localScale = new Vector3(0.1f, 0.02f, 0.1f);
            _screamRingRenderer = ringObj.GetComponent<Renderer>();
            if (_screamRingRenderer != null)
            {
                Shader sh = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
                Material mat = new Material(sh);
                Color col = new Color(1f, 0.72f, 0.25f, 0.65f);
                mat.color = col;
                if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", col);
                _screamRingRenderer.material = mat;
            }
            ringObj.SetActive(false);
            _screamRing = ringObj.transform;
        }

        private void Update()
        {
            var pCfg = DataManager.Instance?.Balance?.player ?? new PlayerBalanceConfig();

            bool menuOpen = GameManager.Instance != null && GameManager.Instance.IsAnyModalOpen;
            bool cursorFree = menuOpen || GameInput.IsAltHeld();

            Cursor.lockState = cursorFree ? CursorLockMode.None : CursorLockMode.Locked;
            Cursor.visible = cursorFree;

            if (!cursorFree)
            {
                // Always rotate smoothly with Mouse movement (no right-click required)
                Vector2 mouseDelta = GameInput.GetMouseLookDelta();
                _targetCameraYaw += mouseDelta.x * mouseSensitivity;
                _targetCameraPitch = Mathf.Clamp(_targetCameraPitch - mouseDelta.y * mouseSensitivity, 4f, 52f);

                // Scream Mechanic (F key - GDD 5.1 & 5.5)
                if (GameInput.IsScreamPressed())
                {
                    TryScream(pCfg);
                }
            }

            // Smooth camera yaw/pitch interpolation
            _currentCameraYaw = Mathf.SmoothDampAngle(_currentCameraYaw, _targetCameraYaw, ref _yawVelocity, cameraRotationSmoothTime);
            _currentCameraPitch = Mathf.SmoothDampAngle(_currentCameraPitch, _targetCameraPitch, ref _pitchVelocity, cameraRotationSmoothTime);

            // Movement (WASD + Shift + Space)
            Vector2 input = menuOpen ? Vector2.zero : GameInput.GetMoveVector();
            IsRunning = !menuOpen && GameInput.IsSprintHeld() && input.sqrMagnitude > 0.05f;
            float targetSpeed = IsRunning ? pCfg.runSpeed : pCfg.walkSpeed;

            Quaternion yawRot = Quaternion.Euler(0f, _currentCameraYaw, 0f);
            Vector3 camForward = yawRot * Vector3.forward;
            Vector3 camRight = yawRot * Vector3.right;

            Vector3 desiredMoveDir = (camForward * input.y + camRight * input.x);
            if (desiredMoveDir.sqrMagnitude > 1f) desiredMoveDir.Normalize();

            _smoothedMoveDir = Vector3.MoveTowards(_smoothedMoveDir, desiredMoveDir, Time.deltaTime * 8.5f);

            if (desiredMoveDir.sqrMagnitude > 0.02f)
            {
                float targetAngle = Mathf.Atan2(desiredMoveDir.x, desiredMoveDir.z) * Mathf.Rad2Deg;
                float smoothAngle = Mathf.SmoothDampAngle(transform.eulerAngles.y, targetAngle, ref _characterTurnVelocity, 0.085f);
                transform.rotation = Quaternion.Euler(0f, smoothAngle, 0f);
            }

            if (_cc.isGrounded)
            {
                if (_verticalVelocity < 0f)
                    _verticalVelocity = -2f;

                if (!menuOpen && GameInput.IsJumpPressed())
                {
                    _verticalVelocity = Mathf.Sqrt(pCfg.jumpHeight * 2f * 18f);
                }
            }
            else
            {
                _verticalVelocity -= 18f * Time.deltaTime;
            }

            Vector3 velocity = _smoothedMoveDir * targetSpeed + Vector3.up * _verticalVelocity;
            _cc.Move(velocity * Time.deltaTime);

            // Footstep Audio
            float horizSpeed = new Vector2(_cc.velocity.x, _cc.velocity.z).magnitude;
            if (_cc.isGrounded && horizSpeed > 0.5f)
            {
                _footstepTimer -= Time.deltaTime;
                if (_footstepTimer <= 0f)
                {
                    AudioManager.Instance?.PlayFootstep();
                    _footstepTimer = IsRunning ? 0.28f : 0.44f;
                }
            }

            UpdateScreamRingVisual();
        }

        private void LateUpdate()
        {
            AnimateCowboySkeleton();
            UpdateThirdPersonCamera();
        }

        public void TriggerInteractAnimation()
        {
            _interactAnimTimer = 0.7f;
        }

        private void TryScream(PlayerBalanceConfig pCfg)
        {
            if (Time.time < _nextScreamAllowedTime) return;

            _nextScreamAllowedTime = Time.time + pCfg.screamCooldownSec;
            _screamAnimTimer = 0.85f;
            if (_screamRing != null)
            {
                _screamRing.gameObject.SetActive(true);
                _screamRing.localScale = new Vector3(0.5f, 0.02f, 0.5f);
            }
            AudioManager.Instance?.PlayScream();
            GameEvents.RaisePlayerScream();
        }

        private void UpdateScreamRingVisual()
        {
            if (_screamRing == null || !_screamRing.gameObject.activeSelf) return;
            if (_screamAnimTimer > 0f)
            {
                float progress = 1f - (_screamAnimTimer / 0.85f);
                float radius = Mathf.Lerp(1.2f, 14f, progress);
                _screamRing.localScale = new Vector3(radius, 0.02f, radius);
            }
            else
            {
                _screamRing.gameObject.SetActive(false);
            }
        }

        private void AnimateCowboySkeleton()
        {
            if (_screamAnimTimer > 0f) _screamAnimTimer -= Time.deltaTime;
            if (_interactAnimTimer > 0f) _interactAnimTimer -= Time.deltaTime;

            float horizSpeed = new Vector2(_cc.velocity.x, _cc.velocity.z).magnitude;
            float targetWeight = Mathf.Clamp01(horizSpeed / 3.5f);
            _animMoveWeight = Mathf.MoveTowards(_animMoveWeight, targetWeight, Time.deltaTime * 6.5f);

            float cycleFreq = IsRunning ? 10.5f : 6.8f;
            if (_animMoveWeight > 0.01f)
            {
                _walkCycle += Time.deltaTime * cycleFreq * Mathf.Lerp(0.6f, 1f, _animMoveWeight);
            }

            float pL = _walkCycle;
            float pR = _walkCycle + Mathf.PI;

            float sinL = Mathf.Sin(pL) * _animMoveWeight;
            float sinR = Mathf.Sin(pR) * _animMoveWeight;

            float hipAmplitude = IsRunning ? 34f : 22f;
            float kneeAmplitude = IsRunning ? 48f : 30f;
            float armAmplitude = IsRunning ? 28f : 16f;

            // Subtle vertical stride bounce on Pelvis
            if (_bonePelvis != null)
            {
                float strideBob = Mathf.Abs(Mathf.Cos(pL)) * (IsRunning ? 0.045f : 0.022f) * _animMoveWeight;
                float idleBreath = Mathf.Sin(Time.time * 2.1f) * 0.008f * (1f - _animMoveWeight);
                _bonePelvis.localPosition = _pelvisBindLocalPos + new Vector3(0f, strideBob + idleBreath, 0f);
                ApplyCharacterSpaceOffset(_bonePelvis, 0f, -sinL * 3.5f, 0f);
            }

            // Legs:
            // For downward-hanging limbs, negative pitch (-X) swings FORWARD, positive pitch (+X) swings BACKWARD.
            // Knees only bend backward (+X > 0), peaking as the leg swings from back to front.
            float lHipPitch = -sinL * hipAmplitude;
            float rHipPitch = -sinR * hipAmplitude;

            float lKneeBend = Mathf.Max(0f, Mathf.Sin(pL - 0.85f)) * kneeAmplitude * _animMoveWeight + 3f * _animMoveWeight;
            float rKneeBend = Mathf.Max(0f, Mathf.Sin(pR - 0.85f)) * kneeAmplitude * _animMoveWeight + 3f * _animMoveWeight;

            if (!_cc.isGrounded)
            {
                lHipPitch = -18f;
                rHipPitch = 10f;
                lKneeBend = 28f;
                rKneeBend = 22f;
            }

            ApplyCharacterSpaceOffset(_boneLThigh, lHipPitch, 0f, 0f);
            ApplyCharacterSpaceOffset(_boneRThigh, rHipPitch, 0f, 0f);
            ApplyCharacterSpaceOffset(_boneLCalf, lKneeBend, 0f, 0f);
            ApplyCharacterSpaceOffset(_boneRCalf, rKneeBend, 0f, 0f);
            ApplyCharacterSpaceOffset(_boneLFoot, -lKneeBend * 0.35f + sinL * 8f, 0f, 0f);
            ApplyCharacterSpaceOffset(_boneRFoot, -rKneeBend * 0.35f + sinR * 8f, 0f, 0f);

            // Torso & Arms
            if (_screamAnimTimer > 0f)
            {
                // Scream Pose: torso leans back slightly, arms raised wide
                ApplyCharacterSpaceOffset(_boneSpine, -6f, 0f, 0f);
                ApplyCharacterSpaceOffset(_boneSpine1, -10f, 0f, 0f);
                ApplyCharacterSpaceOffset(_boneHead, -18f, 0f, 0f);
                ApplyCharacterSpaceOffset(_boneLUpperArm, -35f, 0f, -25f);
                ApplyCharacterSpaceOffset(_boneRUpperArm, -35f, 0f, 25f);
                ApplyCharacterSpaceOffset(_boneLForearm, -35f, 0f, 0f);
                ApplyCharacterSpaceOffset(_boneRForearm, -35f, 0f, 0f);
            }
            else if (_interactAnimTimer > 0f)
            {
                // Groom / Feed reaching motion
                float wave = Mathf.Sin(Time.time * 15f) * 14f;
                ApplyCharacterSpaceOffset(_boneSpine, 6f, 0f, 0f);
                ApplyCharacterSpaceOffset(_boneSpine1, 8f, 0f, 0f);
                ApplyCharacterSpaceOffset(_boneRUpperArm, -48f + wave, 0f, -12f);
                ApplyCharacterSpaceOffset(_boneRForearm, -28f, 0f, 0f);
                ApplyCharacterSpaceOffset(_boneLUpperArm, -15f, 0f, 16f);
                ApplyCharacterSpaceOffset(_boneLForearm, -14f, 0f, 0f);
            }
            else
            {
                float forwardLean = (IsRunning ? 7f : 2.5f) * _animMoveWeight;
                float breath = Mathf.Sin(Time.time * 2.1f) * 1.5f;
                ApplyCharacterSpaceOffset(_boneSpine, forwardLean * 0.5f + breath * 0.5f, sinL * 2.5f, 0f);
                ApplyCharacterSpaceOffset(_boneSpine1, forwardLean * 0.5f + breath, sinL * 3.5f, 0f);
                ApplyCharacterSpaceOffset(_boneHead, -forwardLean * 0.4f, Mathf.Sin(Time.time * 0.9f) * 3f * (1f - _animMoveWeight), 0f);

                // Arms swing opposite to legs: when Left leg is forward (sinL > 0), Right arm swings forward (-sinL) and Left arm swings back (+sinL).
                // Roll (+16 / -16) relaxes the arms down from the bind A-pose toward the body sides.
                float lArmPitch = sinL * armAmplitude;
                float rArmPitch = -sinL * armAmplitude;

                float lElbowBend = -10f - Mathf.Max(0f, -sinL) * (IsRunning ? 28f : 14f);
                float rElbowBend = -10f - Mathf.Max(0f, sinL) * (IsRunning ? 28f : 14f);

                ApplyCharacterSpaceOffset(_boneLUpperArm, lArmPitch, 0f, 16f);
                ApplyCharacterSpaceOffset(_boneRUpperArm, rArmPitch, 0f, -16f);
                ApplyCharacterSpaceOffset(_boneLForearm, lElbowBend, 0f, 0f);
                ApplyCharacterSpaceOffset(_boneRForearm, rElbowBend, 0f, 0f);
            }
        }

        private void ApplyCharacterSpaceOffset(Transform bone, float pitchDeg, float yawDeg, float rollDeg)
        {
            if (bone == null || !_boneBinds.TryGetValue(bone, out BoneBindInfo info)) return;

            Quaternion delta =
                Quaternion.AngleAxis(pitchDeg, info.pitchAxisInParent) *
                Quaternion.AngleAxis(yawDeg, info.yawAxisInParent) *
                Quaternion.AngleAxis(rollDeg, info.rollAxisInParent);

            Quaternion targetRot = delta * info.localBindRotation;
            bone.localRotation = Quaternion.Slerp(bone.localRotation, targetRot, Time.deltaTime * 18f);
        }

        private void UpdateThirdPersonCamera()
        {
            if (cameraTransform == null) return;

            Vector3 targetFocus = transform.position + Vector3.up * cameraHeight;
            _smoothedFocusPoint = Vector3.SmoothDamp(_smoothedFocusPoint, targetFocus, ref _focusPointVelocity, cameraFollowSmoothTime);

            Quaternion camRot = Quaternion.Euler(_currentCameraPitch, _currentCameraYaw, 0f);
            Vector3 desiredPos = _smoothedFocusPoint - (camRot * Vector3.forward * cameraDistance);
            if (desiredPos.y < 0.75f) desiredPos.y = 0.75f;

            cameraTransform.position = desiredPos;
            cameraTransform.LookAt(_smoothedFocusPoint);
        }
    }
}
