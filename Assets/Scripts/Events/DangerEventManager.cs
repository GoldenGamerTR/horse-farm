using UnityEngine;
using HorseFarm.Core;
using HorseFarm.Player;
using HorseFarm.World;
using HorseFarm.Audio;

namespace HorseFarm.Events
{
    public class DangerEventManager : MonoBehaviour
    {
        public static DangerEventManager Instance { get; private set; }

        public GameObject wolfPrefab;
        public Vector3 barnTargetPosition = new Vector3(0f, 0f, 15f);

        public bool IsEventActive { get; private set; }
        public WolfController ActiveWolf { get; private set; }
        public DangerEventConfig ActiveConfig { get; private set; }
        public int CurrentScreams { get; private set; }
        public float DistanceToBarn { get; private set; }

        private float _nextEventTimer;

        private void Awake()
        {
            Instance = this;
        }

        private void Start()
        {
            ScheduleNextEvent(false);
            GameEvents.OnPlayerScream += HandlePlayerScream;
        }

        private void OnDestroy()
        {
            GameEvents.OnPlayerScream -= HandlePlayerScream;
        }

        private void ScheduleNextEvent(bool useFullInterval)
        {
            var events = DataManager.Instance?.Events?.events;
            if (events != null && events.Count > 0)
            {
                var cfg = events[0];
                _nextEventTimer = useFullInterval
                    ? Random.Range(cfg.intervalMinSec, cfg.intervalMaxSec)
                    : Random.Range(cfg.intervalMinSec, cfg.intervalMaxSec);
            }
            else
            {
                _nextEventTimer = 540f;
            }
        }

        private void Update()
        {
            if (GameInput.IsWolfTestPressed() && !IsEventActive)
            {
                TriggerDangerEvent();
            }

            if (!IsEventActive)
            {
                _nextEventTimer -= Time.deltaTime;
                if (_nextEventTimer <= 0f)
                {
                    TriggerDangerEvent();
                }
                return;
            }

            if (ActiveWolf == null)
            {
                EndDangerEvent();
                return;
            }

            ActiveWolf.TickWolf(Time.deltaTime);
            DistanceToBarn = Vector3.Distance(
                new Vector3(ActiveWolf.transform.position.x, 0f, ActiveWolf.transform.position.z),
                new Vector3(barnTargetPosition.x, 0f, barnTargetPosition.z)
            );

            string eventName = DataManager.Instance != null ? DataManager.Instance.Tr(ActiveConfig.nameKey) : "Kurt";
            GameEvents.RaiseDangerEventUpdate(true, eventName, DistanceToBarn, CurrentScreams, ActiveConfig.requiredScreams);

            if (DistanceToBarn <= ActiveConfig.barnReachDistance)
            {
                HandleWolfReachedBarn();
            }
        }

        public void TriggerDangerEvent(string eventId = null)
        {
            if (IsEventActive) return;

            var events = DataManager.Instance?.Events?.events;
            if (events == null || events.Count == 0) return;

            ActiveConfig = events[Random.Range(0, events.Count)];
            if (!string.IsNullOrEmpty(eventId))
            {
                foreach (var e in events)
                {
                    if (e.id == eventId) { ActiveConfig = e; break; }
                }
            }

            IsEventActive = true;
            CurrentScreams = 0;

            float angle = Random.Range(-65f, 65f) * Mathf.Deg2Rad;
            float dist = Random.Range(ActiveConfig.spawnMinDistance, ActiveConfig.spawnMaxDistance);
            Vector3 spawnDir = new Vector3(Mathf.Sin(angle), 0f, -Mathf.Cos(angle)).normalized;
            Vector3 spawnPos = barnTargetPosition + spawnDir * dist;
            spawnPos.y = 0f;

            GameObject wolfObj;
            if (wolfPrefab != null)
            {
                wolfObj = Instantiate(wolfPrefab, spawnPos, Quaternion.identity);
            }
            else
            {
                wolfObj = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                wolfObj.transform.position = spawnPos;
            }
            wolfObj.name = "DangerWolf_" + ActiveConfig.id;

            ActiveWolf = wolfObj.GetComponent<WolfController>();
            if (ActiveWolf == null)
                ActiveWolf = wolfObj.AddComponent<WolfController>();

            ActiveWolf.Setup(barnTargetPosition, ActiveConfig.moveSpeed);
            DistanceToBarn = Vector3.Distance(spawnPos, barnTargetPosition);

            AudioManager.Instance?.SetDangerMusicState(true);
            AudioManager.Instance?.PlayAlarm();
            AudioManager.Instance?.PlayWolfHowl();
            RegionEnvironmentController.Instance?.SetDangerLighting(true);

            string warning = DataManager.Instance != null ? DataManager.Instance.Tr(ActiveConfig.warningKey) : "Tehlike!";
            GameEvents.RaiseNotification(warning);
            string eventName = DataManager.Instance != null ? DataManager.Instance.Tr(ActiveConfig.nameKey) : "Kurt";
            GameEvents.RaiseDangerEventUpdate(true, eventName, DistanceToBarn, CurrentScreams, ActiveConfig.requiredScreams);
        }

        private void HandlePlayerScream()
        {
            if (!IsEventActive || ActiveWolf == null || PlayerController.Instance == null)
                return;

            var pCfg = DataManager.Instance?.Balance?.player ?? new PlayerBalanceConfig();
            Vector3 pPos = PlayerController.Instance.transform.position;
            Vector3 wPos = ActiveWolf.transform.position;
            Vector3 toWolf = wPos - pPos;
            toWolf.y = 0f;

            float dist = toWolf.magnitude;
            Vector3 pForward = PlayerController.Instance.transform.forward;
            pForward.y = 0f;

            float angle = Vector3.Angle(pForward.normalized, toWolf.normalized);

            // GDD 5.5: "Oyuncunun kurda bakması ve 15 birim içinde olması gerekir. Bağırma 3 kez üst üste yapılırsa kurt kaçar."
            if (dist <= pCfg.screamRange && angle <= pCfg.screamAngleDeg)
            {
                CurrentScreams++;
                ActiveWolf.TriggerFlinch();
                AudioManager.Instance?.PlayWolfHowl();

                if (CurrentScreams >= ActiveConfig.requiredScreams)
                {
                    HandleWolfRepelled();
                }
                else
                {
                    string msg = DataManager.Instance.Tr("notify_scream_hit", CurrentScreams, ActiveConfig.requiredScreams);
                    GameEvents.RaiseNotification(msg);
                    string eventName = DataManager.Instance.Tr(ActiveConfig.nameKey);
                    GameEvents.RaiseDangerEventUpdate(true, eventName, DistanceToBarn, CurrentScreams, ActiveConfig.requiredScreams);
                }
            }
            else
            {
                GameEvents.RaiseNotification(DataManager.Instance.Tr("notify_scream_miss"));
            }
        }

        private void HandleWolfRepelled()
        {
            int xpGain = ActiveConfig.xpReward;
            int goldBonus = Random.Range(ActiveConfig.goldRewardMin, ActiveConfig.goldRewardMax + 1);

            if (GameManager.Instance != null)
            {
                GameManager.Instance.AddGold(goldBonus);
                GameManager.Instance.AddXp(xpGain);
                GameManager.Instance.ApplyMinorStartleTrustLoss(ActiveConfig.successTrustLoss);
            }

            if (ActiveWolf != null)
            {
                Vector3 away = (ActiveWolf.transform.position - barnTargetPosition).normalized * 60f;
                ActiveWolf.SetFleeing(ActiveWolf.transform.position + away);
            }

            GameEvents.RaiseNotification(DataManager.Instance.Tr("notify_wolf_repelled", xpGain, goldBonus));
            EndDangerEvent();
        }

        private void HandleWolfReachedBarn()
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.ApplyWolfBarnFailurePenalty(ActiveConfig.trustPenalty, ActiveConfig.panicDurationSec);
            }

            if (ActiveWolf != null)
            {
                Destroy(ActiveWolf.gameObject);
            }
            EndDangerEvent();
        }

        private void EndDangerEvent()
        {
            IsEventActive = false;
            ActiveWolf = null;
            AudioManager.Instance?.SetDangerMusicState(false);
            RegionEnvironmentController.Instance?.SetDangerLighting(false);
            GameEvents.RaiseDangerEventUpdate(false, string.Empty, 0f, 0, 3);
            ScheduleNextEvent(true);
        }
    }
}
