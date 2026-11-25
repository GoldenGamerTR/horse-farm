using UnityEngine;
using HorseFarm.Core;

namespace HorseFarm.World
{
    public class RegionEnvironmentController : MonoBehaviour
    {
        public static RegionEnvironmentController Instance { get; private set; }

        public Renderer groundRenderer;
        public Light directionalLight;

        private Color _baseLightColor = new Color(1f, 0.96f, 0.88f);
        private float _baseLightIntensity = 1.25f;
        private bool _dangerLightingActive;

        private void Awake()
        {
            Instance = this;
        }

        private void Start()
        {
            GameEvents.OnRegionChanged += ApplyRegionTheme;
            if (directionalLight == null)
            {
                var lights = FindObjectsByType<Light>(FindObjectsSortMode.None);
                foreach (var l in lights)
                {
                    if (l.type == LightType.Directional)
                    {
                        directionalLight = l;
                        break;
                    }
                }
            }
        }

        private void OnDestroy()
        {
            GameEvents.OnRegionChanged -= ApplyRegionTheme;
        }

        private void Update()
        {
            if (directionalLight != null)
            {
                // GDD Section 6: "Tehlike olaylarında ortam ışığı kısa süre kırmızımsıya kayar"
                Color targetColor = _dangerLightingActive
                    ? Color.Lerp(_baseLightColor, new Color(1f, 0.35f, 0.28f), 0.55f + 0.25f * Mathf.Sin(Time.time * 4f))
                    : _baseLightColor;
                directionalLight.color = Color.Lerp(directionalLight.color, targetColor, Time.deltaTime * 3f);
                directionalLight.intensity = Mathf.Lerp(directionalLight.intensity, _dangerLightingActive ? 0.95f : _baseLightIntensity, Time.deltaTime * 3f);
            }
        }

        public void SetDangerLighting(bool active)
        {
            _dangerLightingActive = active;
        }

        public void ApplyRegionTheme(string regionId)
        {
            if (DataManager.Instance == null) return;
            RegionConfig reg = DataManager.Instance.GetRegion(regionId);
            if (reg == null) return;

            if (groundRenderer != null && ColorUtility.TryParseHtmlString(reg.groundColor, out Color gCol))
            {
                if (groundRenderer.material != null)
                {
                    groundRenderer.material.color = gCol;
                    if (groundRenderer.material.HasProperty("_BaseColor"))
                        groundRenderer.material.SetColor("_BaseColor", gCol);
                }
            }

            if (ColorUtility.TryParseHtmlString(reg.skyTint, out Color sCol))
            {
                if (Camera.main != null)
                    Camera.main.backgroundColor = sCol;
                RenderSettings.ambientLight = Color.Lerp(sCol, Color.white, 0.45f);
            }

            if (ColorUtility.TryParseHtmlString(reg.fogColor, out Color fCol))
            {
                RenderSettings.fog = true;
                RenderSettings.fogMode = FogMode.Linear;
                RenderSettings.fogStartDistance = 45f;
                RenderSettings.fogEndDistance = 140f;
                RenderSettings.fogColor = fCol;
            }
        }
    }
}
