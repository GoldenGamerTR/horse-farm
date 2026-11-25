using UnityEngine;
using HorseFarm.Core;
using HorseFarm.Horse;
using HorseFarm.World;

namespace HorseFarm.Player
{
    public class InteractionController : MonoBehaviour
    {
        public static InteractionController Instance { get; private set; }

        public HorseController NearestHorse { get; private set; }
        public InteractableStation NearestStation { get; private set; }
        public string ActivePromptText { get; private set; }
        public Vector3 HighlightWorldPosition { get; private set; }
        public bool HasInteractable { get; private set; }

        private Transform _highlightRing;

        private void Awake()
        {
            Instance = this;
            CreateHighlightRing();
        }

        private void CreateHighlightRing()
        {
            GameObject ring = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            ring.name = "InteractHighlightRing";
            Destroy(ring.GetComponent<Collider>());
            ring.transform.localScale = new Vector3(1.8f, 0.02f, 1.8f);
            var rend = ring.GetComponent<Renderer>();
            if (rend != null)
            {
                Shader sh = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
                Material mat = new Material(sh);
                Color col = new Color(0.25f, 0.92f, 0.45f, 1f);
                mat.color = col;
                if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", col);
                rend.material = mat;
            }
            ring.SetActive(false);
            _highlightRing = ring.transform;
        }

        private void Update()
        {
            if (GameManager.Instance == null) return;
            float range = DataManager.Instance?.Balance?.player != null
                ? DataManager.Instance.Balance.player.interactRange
                : 4.5f;

            FindNearestTargets(range);

            if (_highlightRing != null)
            {
                _highlightRing.gameObject.SetActive(HasInteractable);
                if (HasInteractable)
                {
                    float pulse = 1.65f + Mathf.Sin(Time.time * 4.5f) * 0.15f;
                    _highlightRing.position = new Vector3(HighlightWorldPosition.x, 0.03f, HighlightWorldPosition.z);
                    _highlightRing.localScale = new Vector3(pulse, 0.02f, pulse);
                }
            }

            if (!GameManager.Instance.IsAnyModalOpen && HasInteractable && GameInput.IsInteractPressed())
            {
                PlayerController.Instance?.TriggerInteractAnimation();
                if (NearestHorse != null && NearestStation == null)
                {
                    GameEvents.RaiseInspectHorse(NearestHorse.Data);
                }
                else if (NearestStation != null)
                {
                    NearestStation.Interact();
                }
                else if (NearestHorse != null)
                {
                    GameEvents.RaiseInspectHorse(NearestHorse.Data);
                }
            }
        }

        private void FindNearestTargets(float interactRange)
        {
            NearestHorse = null;
            NearestStation = null;
            ActivePromptText = string.Empty;
            HasInteractable = false;

            Vector3 pPos = transform.position;
            float bestHorseDist = 14f; // Also used for bottom-left HUD horse status panel
            float bestInteractScore = interactRange;

            var horses = GameManager.Instance.SpawnedHorses;
            for (int i = 0; i < horses.Count; i++)
            {
                var h = horses[i];
                if (h == null || h.Data == null) continue;
                float d = Vector3.Distance(pPos, h.transform.position);
                if (d < bestHorseDist)
                {
                    bestHorseDist = d;
                    NearestHorse = h;
                }
            }

            if (NearestHorse != null && bestHorseDist <= interactRange)
            {
                bestInteractScore = bestHorseDist;
                HasInteractable = true;
                HighlightWorldPosition = NearestHorse.transform.position;
                ActivePromptText = DataManager.Instance.Tr("prompt_horse_interact", NearestHorse.Data.name);
            }

            var stations = GameManager.Instance.Stations;
            for (int i = 0; i < stations.Count; i++)
            {
                var st = stations[i];
                if (st == null) continue;
                float d = Vector3.Distance(pPos, st.transform.position);
                if (d <= interactRange && d < bestInteractScore)
                {
                    bestInteractScore = d;
                    NearestStation = st;
                    HasInteractable = true;
                    HighlightWorldPosition = st.transform.position;
                    ActivePromptText = st.GetPromptText();
                }
            }
        }
    }
}
