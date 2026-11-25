using System.Collections.Generic;
using UnityEngine;
using HorseFarm.Core;
using HorseFarm.Player;
using HorseFarm.Horse;
using HorseFarm.Economy;
using HorseFarm.Events;
using HorseFarm.Audio;

namespace HorseFarm.UI
{
    public enum ActiveModalMenu
    {
        None,
        Inventory,
        Merchant,
        Market,
        Barn,
        Quests,
        Regions,
        HorseCare
    }

    public class GameHUDAndMenus : MonoBehaviour
    {
        private ActiveModalMenu _currentMenu = ActiveModalMenu.None;
        private int _merchantTab = 0; // 0 = Malzemeler, 1 = At Kataloğu
        private HorseData _inspectedHorse;
        private Vector2 _scrollPos;

        private string _toastMessage = string.Empty;
        private float _toastTimer = 0f;

        private bool _dangerActive;
        private string _dangerName = string.Empty;
        private float _dangerDist;
        private int _dangerScreams;
        private int _dangerReqScreams = 3;

        private Texture2D _texDarkBg;
        private Texture2D _texPanelBg;
        private Texture2D _texCardBg;
        private Texture2D _texGreen;
        private Texture2D _texBlue;
        private Texture2D _texGold;
        private Texture2D _texRed;
        private Texture2D _texBarBg;

        private GUIStyle _styleHeader;
        private GUIStyle _styleTitle;
        private GUIStyle _styleBody;
        private GUIStyle _styleSmall;
        private GUIStyle _styleButton;
        private GUIStyle _styleToast;
        private GUIStyle _styleTag;
        private bool _stylesInitialized;

        private void OnEnable()
        {
            GameEvents.OnNotification += ShowToast;
            GameEvents.OnDangerEventUpdate += OnDangerUpdate;
            GameEvents.OnOpenStationMenu += OnOpenStation;
            GameEvents.OnInspectHorse += OnInspectHorse;
        }

        private void OnDisable()
        {
            GameEvents.OnNotification -= ShowToast;
            GameEvents.OnDangerEventUpdate -= OnDangerUpdate;
            GameEvents.OnOpenStationMenu -= OnOpenStation;
            GameEvents.OnInspectHorse -= OnInspectHorse;
        }

        private void Update()
        {
            if (_toastTimer > 0f)
                _toastTimer -= Time.deltaTime;

            if (GameInput.IsEscapePressed() && _currentMenu != ActiveModalMenu.None)
            {
                SetMenu(ActiveModalMenu.None);
                return;
            }

            if (GameInput.IsInventoryPressed())
                ToggleMenu(ActiveModalMenu.Inventory);
            else if (GameInput.IsQuestsPressed())
                ToggleMenu(ActiveModalMenu.Quests);
            else if (GameInput.IsBarnPressed())
                ToggleMenu(ActiveModalMenu.Barn);
            else if (GameInput.IsRegionsPressed())
                ToggleMenu(ActiveModalMenu.Regions);
        }

        private void ToggleMenu(ActiveModalMenu target)
        {
            AudioManager.Instance?.PlayClick();
            SetMenu(_currentMenu == target ? ActiveModalMenu.None : target);
        }

        private void SetMenu(ActiveModalMenu menu)
        {
            _currentMenu = menu;
            _scrollPos = Vector2.zero;
            if (GameManager.Instance != null)
                GameManager.Instance.IsAnyModalOpen = (_currentMenu != ActiveModalMenu.None);
        }

        private void ShowToast(string msg)
        {
            _toastMessage = msg;
            _toastTimer = 4.0f;
        }

        private void OnDangerUpdate(bool active, string eventName, float dist, int screams, int reqScreams)
        {
            _dangerActive = active;
            _dangerName = eventName;
            _dangerDist = dist;
            _dangerScreams = screams;
            _dangerReqScreams = reqScreams;
        }

        private void OnOpenStation(StationType station)
        {
            AudioManager.Instance?.PlayClick();
            switch (station)
            {
                case StationType.Merchant: SetMenu(ActiveModalMenu.Merchant); break;
                case StationType.Market: SetMenu(ActiveModalMenu.Market); break;
                case StationType.BarnUpgrade: SetMenu(ActiveModalMenu.Barn); break;
                case StationType.RegionGate: SetMenu(ActiveModalMenu.Regions); break;
                case StationType.QuestBoard: SetMenu(ActiveModalMenu.Quests); break;
            }
        }

        private void OnInspectHorse(HorseData horse)
        {
            if (horse == null) return;
            AudioManager.Instance?.PlayClick();
            _inspectedHorse = horse;
            SetMenu(ActiveModalMenu.HorseCare);
        }

        private void InitStyles()
        {
            if (_stylesInitialized) return;
            _stylesInitialized = true;

            _texDarkBg = MakeTex(new Color(0.08f, 0.11f, 0.14f, 0.90f));
            _texPanelBg = MakeTex(new Color(0.12f, 0.16f, 0.20f, 0.96f));
            _texCardBg = MakeTex(new Color(0.18f, 0.23f, 0.28f, 0.95f));
            _texGreen = MakeTex(new Color(0.24f, 0.76f, 0.38f, 1f));
            _texBlue = MakeTex(new Color(0.22f, 0.62f, 0.92f, 1f));
            _texGold = MakeTex(new Color(0.92f, 0.72f, 0.18f, 1f));
            _texRed = MakeTex(new Color(0.88f, 0.24f, 0.24f, 1f));
            _texBarBg = MakeTex(new Color(0.05f, 0.07f, 0.09f, 0.9f));

            _styleHeader = new GUIStyle(GUI.skin.label)
            {
                fontSize = 18,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleLeft
            };
            _styleHeader.normal.textColor = new Color(0.98f, 0.92f, 0.72f);

            _styleTitle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 15,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleLeft
            };
            _styleTitle.normal.textColor = Color.white;

            _styleBody = new GUIStyle(GUI.skin.label)
            {
                fontSize = 13,
                wordWrap = true,
                alignment = TextAnchor.MiddleLeft
            };
            _styleBody.normal.textColor = new Color(0.90f, 0.93f, 0.96f);

            _styleSmall = new GUIStyle(GUI.skin.label)
            {
                fontSize = 11,
                wordWrap = true,
                alignment = TextAnchor.MiddleLeft
            };
            _styleSmall.normal.textColor = new Color(0.75f, 0.82f, 0.88f);

            _styleButton = new GUIStyle(GUI.skin.button)
            {
                fontSize = 13,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter
            };
            _styleButton.normal.textColor = Color.white;

            _styleToast = new GUIStyle(GUI.skin.box)
            {
                fontSize = 15,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                wordWrap = true
            };
            _styleToast.normal.textColor = new Color(1f, 0.95f, 0.75f);
            _styleToast.normal.background = _texDarkBg;

            _styleTag = new GUIStyle(GUI.skin.box)
            {
                fontSize = 11,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter
            };
            _styleTag.normal.textColor = Color.white;
            _styleTag.normal.background = _texDarkBg;
        }

        private Texture2D MakeTex(Color col)
        {
            Texture2D tex = new Texture2D(2, 2);
            Color[] pix = { col, col, col, col };
            tex.SetPixels(pix);
            tex.Apply();
            return tex;
        }

        private void OnGUI()
        {
            if (DataManager.Instance == null || GameManager.Instance == null) return;
            InitStyles();

            DrawWorldSpaceMarkers();
            DrawTopRightPlayerHUD();
            DrawTopLeftActiveQuestsHUD();
            DrawBottomLeftNearbyHorseHUD();
            DrawBottomControlsBar();

            if (_dangerActive)
            {
                DrawDangerBanner();
            }

            if (_toastTimer > 0f && !string.IsNullOrEmpty(_toastMessage))
            {
                Rect toastRect = new Rect(Screen.width * 0.5f - 250f, _dangerActive ? 74f : 18f, 500f, 42f);
                GUI.Box(toastRect, _toastMessage, _styleToast);
            }

            if (_currentMenu != ActiveModalMenu.None)
            {
                DrawActiveModalWindow();
            }
        }

        // --- World-Space Overhead Markers (GDD 5.4 & 5.12) ---
        private void DrawWorldSpaceMarkers()
        {
            Camera cam = Camera.main;
            if (cam == null) return;

            float crit = DataManager.Instance.Balance.needs.criticalThreshold;
            var horses = GameManager.Instance.SpawnedHorses;
            for (int i = 0; i < horses.Count; i++)
            {
                var h = horses[i];
                if (h == null || h.Data == null) continue;

                Vector3 worldPos = h.transform.position + Vector3.up * 2.35f;
                Vector3 screenPos = cam.WorldToScreenPoint(worldPos);
                if (screenPos.z <= 0f || screenPos.z > 45f) continue;

                float guiX = screenPos.x;
                float guiY = Screen.height - screenPos.y;

                bool hungry = h.Data.hunger >= crit;
                bool thirsty = h.Data.thirst >= crit;
                bool panic = h.Data.isPanicking;

                string label = $"{h.Data.name} (♥{Mathf.RoundToInt(h.Data.trust)})";
                Rect nameRect = new Rect(guiX - 65f, guiY - 22f, 130f, 22f);
                GUI.Box(nameRect, label, _styleTag);

                if (panic || hungry || thirsty)
                {
                    string warn = panic
                        ? DataManager.Instance.Tr("horse_state_panic")
                        : (hungry && thirsty ? "AÇ & SUSUZ!" : (hungry ? DataManager.Instance.Tr("horse_need_hungry") : DataManager.Instance.Tr("horse_need_thirsty")));

                    Rect warnRect = new Rect(guiX - 58f, guiY - 46f, 116f, 22f);
                    GUI.DrawTexture(warnRect, _texRed);
                    GUI.Label(warnRect, "⚠ " + warn, _styleTag);
                }
            }

            // Wolf Overhead Marker during Danger Event
            if (_dangerActive && DangerEventManager.Instance != null && DangerEventManager.Instance.ActiveWolf != null)
            {
                Vector3 wPos = DangerEventManager.Instance.ActiveWolf.transform.position + Vector3.up * 1.6f;
                Vector3 sp = cam.WorldToScreenPoint(wPos);
                if (sp.z > 0f)
                {
                    Rect wRect = new Rect(sp.x - 80f, Screen.height - sp.y - 26f, 160f, 26f);
                    GUI.DrawTexture(wRect, _texRed);
                    GUI.Label(wRect, $"⚠ {_dangerName} ({Mathf.RoundToInt(_dangerDist)}m) [F]", _styleTag);
                }
            }

            // Floating Interaction Prompt above highlighted object (GDD 5.12)
            if (_currentMenu == ActiveModalMenu.None && InteractionController.Instance != null && InteractionController.Instance.HasInteractable)
            {
                Vector3 iPos = InteractionController.Instance.HighlightWorldPosition + Vector3.up * 1.85f;
                Vector3 sp = cam.WorldToScreenPoint(iPos);
                if (sp.z > 0f)
                {
                    Rect pRect = new Rect(sp.x - 155f, Screen.height - sp.y - 32f, 310f, 30f);
                    GUI.Box(pRect, InteractionController.Instance.ActivePromptText, _styleToast);
                }
            }
        }

        // --- Top-Right HUD: Gold, Level, Title, XP Bar, Capacity, Region (GDD 5.12) ---
        private void DrawTopRightPlayerHUD()
        {
            var st = GameManager.Instance.State;
            var dm = DataManager.Instance;

            float w = 310f;
            float h = 158f;
            Rect r = new Rect(Screen.width - w - 16f, 16f, w, h);
            GUI.DrawTexture(r, _texDarkBg);

            GUILayout.BeginArea(new Rect(r.x + 12f, r.y + 10f, r.width - 24f, r.height - 20f));

            string title = dm.GetPlayerTitle(st.level);
            GUILayout.BeginHorizontal();
            GUILayout.Label(dm.Tr("hud_gold", st.gold), _styleHeader);
            GUILayout.FlexibleSpace();
            GUILayout.Label(dm.Tr("hud_level", st.level, title), _styleTitle);
            GUILayout.EndHorizontal();

            int reqXp = EconomySystem.GetRequiredXpForLevel(st.level);
            Rect xpRect = GUILayoutUtility.GetRect(r.width - 24f, 18f);
            DrawProgressBar(xpRect, st.xp / (float)reqXp, _texBlue, dm.Tr("hud_xp", st.xp, reqXp));

            GUILayout.Space(4f);
            var reg = dm.GetRegion(st.currentRegionId);
            string regName = reg != null ? dm.Tr(reg.nameKey) : st.currentRegionId;

            GUILayout.BeginHorizontal();
            GUILayout.Label(dm.Tr("hud_capacity", st.horses.Count, st.horseCapacity), _styleBody);
            GUILayout.FlexibleSpace();
            GUILayout.Label(dm.Tr("hud_region", regName), _styleBody);
            GUILayout.EndHorizontal();

            GUILayout.Space(6f);
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Envanter [I]", _styleButton, GUILayout.Height(26f))) ToggleMenu(ActiveModalMenu.Inventory);
            if (GUILayout.Button("Görevler [J]", _styleButton, GUILayout.Height(26f))) ToggleMenu(ActiveModalMenu.Quests);
            if (GUILayout.Button("Ahır [B]", _styleButton, GUILayout.Height(26f))) ToggleMenu(ActiveModalMenu.Barn);
            if (GUILayout.Button("Bölge [M]", _styleButton, GUILayout.Height(26f))) ToggleMenu(ActiveModalMenu.Regions);
            GUILayout.EndHorizontal();

            GUILayout.EndArea();
        }

        // --- Top-Left HUD: Active Quests List (GDD 5.2 & 5.12) ---
        private void DrawTopLeftActiveQuestsHUD()
        {
            var st = GameManager.Instance.State;
            var dm = DataManager.Instance;
            int maxQ = dm.Balance.xp.maxActiveQuests;

            float w = 310f;
            float h = 44f + Mathf.Max(1, st.activeQuests.Count) * 52f;
            Rect r = new Rect(16f, 16f, w, h);
            GUI.DrawTexture(r, _texDarkBg);

            GUILayout.BeginArea(new Rect(r.x + 12f, r.y + 10f, r.width - 24f, r.height - 16f));
            GUILayout.Label(dm.Tr("hud_quests_header", st.activeQuests.Count, maxQ), _styleTitle);

            for (int i = 0; i < st.activeQuests.Count; i++)
            {
                var q = dm.GetQuest(st.activeQuests[i]);
                if (q == null) continue;
                int prog = st.GetQuestProgress(q.id);
                GUILayout.Label($"• {dm.Tr(q.titleKey)} (+{q.goldReward} Altın)", _styleBody);
                Rect barR = GUILayoutUtility.GetRect(r.width - 24f, 15f);
                DrawProgressBar(barR, Mathf.Clamp01(prog / (float)q.targetValue), _texGold, $"{dm.Tr(q.descKey)} ({prog}/{q.targetValue})");
                GUILayout.Space(3f);
            }
            GUILayout.EndArea();
        }

        // --- Bottom-Left HUD: Nearby Horse Panel (GDD 5.3 & 5.12) ---
        private void DrawBottomLeftNearbyHorseHUD()
        {
            HorseController nearest = InteractionController.Instance != null ? InteractionController.Instance.NearestHorse : null;
            if (nearest == null && GameManager.Instance.SpawnedHorses.Count > 0)
                nearest = GameManager.Instance.SpawnedHorses[0];

            if (nearest == null || nearest.Data == null) return;

            var h = nearest.Data;
            var dm = DataManager.Instance;
            var breed = dm.GetBreed(h.breedId);
            string breedName = breed != null ? dm.Tr(breed.nameKey) : h.breedId;
            string rarityName = dm.Tr(dm.GetRaritySetting(h.rarity).nameKey);
            string stateName = dm.Tr(nearest.GetStateDisplayKey());

            float w = 335f;
            float height = 185f;
            Rect r = new Rect(16f, Screen.height - height - 40f, w, height);
            GUI.DrawTexture(r, _texDarkBg);

            GUILayout.BeginArea(new Rect(r.x + 12f, r.y + 10f, r.width - 24f, r.height - 16f));
            GUILayout.Label($"{h.name} — {breedName} ({rarityName})", _styleTitle);
            GUILayout.Label($"Durum: {stateName} | Hız: {h.genes.speed} | Dayanıklılık: {h.genes.stamina}", _styleSmall);

            float crit = dm.Balance.needs.criticalThreshold;

            // Hunger Bar (Critical >= 70 turns red)
            Rect hRect = GUILayoutUtility.GetRect(r.width - 24f, 18f);
            DrawProgressBar(hRect, h.hunger / 100f, h.hunger >= crit ? _texRed : _texGold,
                $"{dm.Tr("horse_hunger")}: {Mathf.RoundToInt(h.hunger)}/100");
            GUILayout.Space(4f);

            // Thirst Bar (Critical >= 70 turns red)
            Rect tRect = GUILayoutUtility.GetRect(r.width - 24f, 18f);
            DrawProgressBar(tRect, h.thirst / 100f, h.thirst >= crit ? _texRed : _texBlue,
                $"{dm.Tr("horse_thirst")}: {Mathf.RoundToInt(h.thirst)}/100");
            GUILayout.Space(4f);

            // Trust ("Sevgi") Bar
            Rect trRect = GUILayoutUtility.GetRect(r.width - 24f, 18f);
            DrawProgressBar(trRect, h.trust / 100f, _texGreen,
                $"{dm.Tr("horse_trust")}: {Mathf.RoundToInt(h.trust)}/100");

            GUILayout.Space(8f);
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Besle", _styleButton, GUILayout.Height(26f)))
                GameManager.Instance.FeedHorse(h);
            if (GUILayout.Button("Su Ver", _styleButton, GUILayout.Height(26f)))
                GameManager.Instance.WaterHorse(h);
            if (GUILayout.Button("Tımarla", _styleButton, GUILayout.Height(26f)))
                GameManager.Instance.GroomHorse(h);
            GUILayout.EndHorizontal();

            GUILayout.EndArea();
        }

        private void DrawDangerBanner()
        {
            Rect r = new Rect(Screen.width * 0.5f - 310f, 16f, 620f, 50f);
            GUI.DrawTexture(r, _texRed);
            string txt = DataManager.Instance.Tr("danger_alert_banner",
                _dangerName, Mathf.RoundToInt(_dangerDist), _dangerScreams, _dangerReqScreams);
            GUI.Label(r, "⚠ " + txt, _styleToast);
        }

        private void DrawBottomControlsBar()
        {
            Rect r = new Rect(16f, Screen.height - 28f, Screen.width - 32f, 22f);
            GUI.DrawTexture(r, _texDarkBg);
            string screamCd = PlayerController.Instance != null && PlayerController.Instance.ScreamCooldownRemaining > 0f
                ? $" (Bağırma Bekleme: {PlayerController.Instance.ScreamCooldownRemaining:0.0}sn)"
                : "";
            GUI.Label(new Rect(r.x + 10f, r.y + 2f, r.width - 20f, 18f),
                DataManager.Instance.Tr("hud_controls_hint") + screamCd, _styleSmall);
        }

        // --- Modal Menus ---
        private void DrawActiveModalWindow()
        {
            float w = 680f;
            float h = 480f;
            Rect winRect = new Rect((Screen.width - w) * 0.5f, (Screen.height - h) * 0.5f, w, h);
            GUI.DrawTexture(winRect, _texPanelBg);

            GUILayout.BeginArea(new Rect(winRect.x + 18f, winRect.y + 16f, winRect.width - 36f, winRect.height - 32f));

            switch (_currentMenu)
            {
                case ActiveModalMenu.Inventory: DrawInventoryModal(); break;
                case ActiveModalMenu.Merchant: DrawMerchantModal(); break;
                case ActiveModalMenu.Market: DrawMarketModal(); break;
                case ActiveModalMenu.Barn: DrawBarnModal(); break;
                case ActiveModalMenu.Quests: DrawQuestsModal(); break;
                case ActiveModalMenu.Regions: DrawRegionsModal(); break;
                case ActiveModalMenu.HorseCare: DrawHorseCareModal(); break;
            }

            GUILayout.FlexibleSpace();
            if (GUILayout.Button(DataManager.Instance.Tr("btn_close"), _styleButton, GUILayout.Height(32f)))
            {
                AudioManager.Instance?.PlayClick();
                SetMenu(ActiveModalMenu.None);
            }

            GUILayout.EndArea();
        }

        private void DrawInventoryModal()
        {
            var st = GameManager.Instance.State;
            var dm = DataManager.Instance;
            int used = InventorySystem.GetUsedSlots(st);

            GUILayout.Label(dm.Tr("menu_inventory_title", used, st.inventory.slots), _styleHeader);
            GUILayout.Space(8f);

            _scrollPos = GUILayout.BeginScrollView(_scrollPos, GUILayout.Height(350f));
            for (int i = 0; i < st.inventory.items.Count; i++)
            {
                var stack = st.inventory.items[i];
                var cfg = dm.GetItem(stack.itemId);
                string iName = cfg != null ? dm.Tr(cfg.nameKey) : stack.itemId;
                string iDesc = cfg != null ? dm.Tr(cfg.descKey) : "";

                GUILayout.BeginVertical(_styleTag);
                GUILayout.Label($"{iName}  x{stack.count} / {dm.Balance.inventory.maxStackSize}", _styleTitle);
                GUILayout.Label(iDesc, _styleSmall);
                GUILayout.EndVertical();
                GUILayout.Space(4f);
            }
            GUILayout.EndScrollView();
        }

        private void DrawMerchantModal()
        {
            var st = GameManager.Instance.State;
            var dm = DataManager.Instance;

            GUILayout.Label(dm.Tr("menu_merchant_title"), _styleHeader);
            GUILayout.Space(6f);

            GUILayout.BeginHorizontal();
            if (GUILayout.Button(dm.Tr("menu_merchant_items_tab"), _styleButton, GUILayout.Height(30f)))
                _merchantTab = 0;
            if (GUILayout.Button(dm.Tr("menu_merchant_horses_tab"), _styleButton, GUILayout.Height(30f)))
                _merchantTab = 1;
            GUILayout.EndHorizontal();
            GUILayout.Space(8f);

            _scrollPos = GUILayout.BeginScrollView(_scrollPos, GUILayout.Height(325f));

            if (_merchantTab == 0)
            {
                foreach (var item in dm.Items.items)
                {
                    bool regionUnlocked = string.IsNullOrEmpty(item.requiredRegionId)
                        || st.unlockedRegions.Contains(item.requiredRegionId)
                        || st.currentRegionId == "region_legend_summit";

                    int owned = InventorySystem.GetItemCount(st, item.id);
                    GUILayout.BeginVertical(_styleTag);
                    GUILayout.BeginHorizontal();
                    GUILayout.BeginVertical();
                    GUILayout.Label($"{dm.Tr(item.nameKey)} (Envanterde: {owned})", _styleTitle);
                    GUILayout.Label(dm.Tr(item.descKey), _styleSmall);
                    GUILayout.EndVertical();
                    GUILayout.FlexibleSpace();

                    if (regionUnlocked)
                    {
                        if (GUILayout.Button(dm.Tr("btn_buy", item.price), _styleButton, GUILayout.Width(135f), GUILayout.Height(28f)))
                            GameManager.Instance.BuyItemFromMerchant(item.id, 1);
                        if (GUILayout.Button(dm.Tr("btn_buy_5", item.price * 5), _styleButton, GUILayout.Width(120f), GUILayout.Height(28f)))
                            GameManager.Instance.BuyItemFromMerchant(item.id, 5);
                    }
                    else
                    {
                        var reqReg = dm.GetRegion(item.requiredRegionId);
                        string reqRegName = reqReg != null ? dm.Tr(reqReg.nameKey) : item.requiredRegionId;
                        GUILayout.Label($"Kilitli ({reqRegName})", _styleSmall, GUILayout.Width(160f));
                    }

                    GUILayout.EndHorizontal();
                    GUILayout.EndVertical();
                    GUILayout.Space(4f);
                }
            }
            else
            {
                foreach (var breed in dm.Breeds.breeds)
                {
                    bool unlocked = EconomySystem.IsBreedUnlocked(st, breed);
                    string rarityName = dm.Tr(dm.GetRaritySetting(breed.rarity).nameKey);
                    var feedCfg = dm.GetItem(breed.requiredFeedId);
                    string feedName = feedCfg != null ? dm.Tr(feedCfg.nameKey) : breed.requiredFeedId;

                    GUILayout.BeginVertical(_styleTag);
                    GUILayout.BeginHorizontal();
                    GUILayout.BeginVertical();
                    GUILayout.Label($"{dm.Tr(breed.nameKey)} — [{rarityName}]", _styleTitle);
                    GUILayout.Label($"{dm.Tr(breed.descKey)} | Hız: {breed.minSpeed}-{breed.maxSpeed} | Zorunlu Yem: {feedName}", _styleSmall);
                    GUILayout.EndVertical();
                    GUILayout.FlexibleSpace();

                    if (unlocked)
                    {
                        if (GUILayout.Button(dm.Tr("btn_buy", breed.price), _styleButton, GUILayout.Width(155f), GUILayout.Height(30f)))
                            GameManager.Instance.BuyHorseFromMerchant(breed.id);
                    }
                    else
                    {
                        var q = dm.GetQuest(breed.requiredQuestId);
                        string qTitle = q != null ? dm.Tr(q.titleKey) : "";
                        string lockReason = string.IsNullOrEmpty(qTitle)
                            ? $"Kilitli (Sv. {breed.requiredLevel})"
                            : $"Kilitli (Sv. {breed.requiredLevel} / {qTitle})";
                        GUILayout.Label(lockReason, _styleSmall, GUILayout.Width(190f));
                    }

                    GUILayout.EndHorizontal();
                    GUILayout.EndVertical();
                    GUILayout.Space(4f);
                }
            }

            GUILayout.EndScrollView();
        }

        private void DrawMarketModal()
        {
            var st = GameManager.Instance.State;
            var dm = DataManager.Instance;

            GUILayout.Label(dm.Tr("menu_market_title"), _styleHeader);
            GUILayout.Space(8f);

            _scrollPos = GUILayout.BeginScrollView(_scrollPos, GUILayout.Height(350f));
            for (int i = 0; i < st.horses.Count; i++)
            {
                var h = st.horses[i];
                var breed = dm.GetBreed(h.breedId);
                string bName = breed != null ? dm.Tr(breed.nameKey) : h.breedId;
                int sellPrice = EconomySystem.CalculateHorseSellPrice(h, dm);
                var ctrl = GameManager.Instance.FindHorseController(h.id);
                bool following = ctrl != null && ctrl.CurrentState == HorseAIState.Follow;

                GUILayout.BeginVertical(_styleTag);
                GUILayout.BeginHorizontal();
                GUILayout.BeginVertical();
                string followBadge = following ? " [Seni Takip Ediyor]" : "";
                GUILayout.Label($"{h.name} ({bName}){followBadge}", _styleTitle);
                GUILayout.Label($"Sevgi (Güven): {Mathf.RoundToInt(h.trust)}/100 | Hız: {h.genes.speed} | Dayanıklılık: {h.genes.stamina}", _styleSmall);
                GUILayout.EndVertical();
                GUILayout.FlexibleSpace();

                if (GUILayout.Button(dm.Tr("btn_sell", sellPrice), _styleButton, GUILayout.Width(150f), GUILayout.Height(30f)))
                {
                    GameManager.Instance.SellHorseAtMarket(h);
                    break;
                }
                GUILayout.EndHorizontal();
                GUILayout.EndVertical();
                GUILayout.Space(4f);
            }
            GUILayout.EndScrollView();
        }

        private void DrawBarnModal()
        {
            var st = GameManager.Instance.State;
            var dm = DataManager.Instance;

            GUILayout.Label(dm.Tr("menu_barn_title"), _styleHeader);
            GUILayout.Space(8f);

            // Barn Upgrade Row
            int nextBarnLv = st.upgrades.barn + 1;
            BarnUpgradeConfig nextBarn = null;
            foreach (var b in dm.Balance.barnUpgrades)
                if (b.level == nextBarnLv) { nextBarn = b; break; }

            GUILayout.BeginVertical(_styleTag);
            GUILayout.BeginHorizontal();
            GUILayout.Label($"Ahır Seviyesi: {st.upgrades.barn} (At Kotası: {st.horseCapacity})", _styleTitle);
            GUILayout.FlexibleSpace();
            if (nextBarn != null)
            {
                if (GUILayout.Button($"Kota {nextBarn.capacity} → " + dm.Tr("btn_upgrade", nextBarn.cost), _styleButton, GUILayout.Width(210f), GUILayout.Height(28f)))
                    GameManager.Instance.UpgradeBarn();
            }
            else
            {
                GUILayout.Label("Maksimum Seviye", _styleSmall);
            }
            GUILayout.EndHorizontal();
            GUILayout.EndVertical();

            GUILayout.Space(6f);

            // Storage Upgrade Row
            int nextStorLv = st.upgrades.storage + 1;
            StorageUpgradeConfig nextStor = null;
            foreach (var s in dm.Balance.storageUpgrades)
                if (s.level == nextStorLv) { nextStor = s; break; }

            GUILayout.BeginVertical(_styleTag);
            GUILayout.BeginHorizontal();
            GUILayout.Label($"Depo Seviyesi: {st.upgrades.storage} (Envanter: {st.inventory.slots} Slot)", _styleTitle);
            GUILayout.FlexibleSpace();
            if (nextStor != null)
            {
                if (GUILayout.Button($"{nextStor.slots} Slot → " + dm.Tr("btn_upgrade", nextStor.cost), _styleButton, GUILayout.Width(210f), GUILayout.Height(28f)))
                    GameManager.Instance.UpgradeStorage();
            }
            else
            {
                GUILayout.Label("Maksimum Seviye", _styleSmall);
            }
            GUILayout.EndHorizontal();
            GUILayout.EndVertical();

            GUILayout.Space(8f);

            // Audio Settings (GDD Phase 8)
            if (AudioManager.Instance != null)
            {
                GUILayout.BeginHorizontal();
                GUILayout.Label("Müzik Sesi:", _styleBody, GUILayout.Width(90f));
                AudioManager.Instance.musicVolume = GUILayout.HorizontalSlider(AudioManager.Instance.musicVolume, 0f, 1f, GUILayout.Width(140f));
                GUILayout.Space(20f);
                GUILayout.Label("Efekt Sesi:", _styleBody, GUILayout.Width(90f));
                AudioManager.Instance.sfxVolume = GUILayout.HorizontalSlider(AudioManager.Instance.sfxVolume, 0f, 1f, GUILayout.Width(140f));
                GUILayout.EndHorizontal();
            }

            GUILayout.Space(8f);
            _scrollPos = GUILayout.BeginScrollView(_scrollPos, GUILayout.Height(210f));
            for (int i = 0; i < st.horses.Count; i++)
            {
                var h = st.horses[i];
                var breed = dm.GetBreed(h.breedId);
                string bName = breed != null ? dm.Tr(breed.nameKey) : h.breedId;
                GUILayout.BeginVertical(_styleTag);
                GUILayout.BeginHorizontal();
                GUILayout.Label($"{h.name} ({bName}) — Açlık: {Mathf.RoundToInt(h.hunger)} | Susuzluk: {Mathf.RoundToInt(h.thirst)} | Sevgi: {Mathf.RoundToInt(h.trust)}", _styleBody);
                GUILayout.FlexibleSpace();
                if (GUILayout.Button("Bakım", _styleButton, GUILayout.Width(80f)))
                {
                    _inspectedHorse = h;
                    SetMenu(ActiveModalMenu.HorseCare);
                }
                GUILayout.EndHorizontal();
                GUILayout.EndVertical();
                GUILayout.Space(3f);
            }
            GUILayout.EndScrollView();
        }

        private void DrawQuestsModal()
        {
            var st = GameManager.Instance.State;
            var dm = DataManager.Instance;

            GUILayout.Label(dm.Tr("menu_quests_title"), _styleHeader);
            GUILayout.Space(8f);

            _scrollPos = GUILayout.BeginScrollView(_scrollPos, GUILayout.Height(350f));
            foreach (var q in dm.Quests.quests)
            {
                bool active = st.activeQuests.Contains(q.id);
                bool completed = st.completedQuests.Contains(q.id);
                bool regionOk = st.unlockedRegions.Contains(q.regionId);
                bool prereqOk = string.IsNullOrEmpty(q.prerequisiteQuestId) || st.completedQuests.Contains(q.prerequisiteQuestId);

                int prog = st.GetQuestProgress(q.id);
                GUILayout.BeginVertical(_styleTag);
                GUILayout.BeginHorizontal();
                GUILayout.BeginVertical();
                string statusTag = completed ? "[TAMAMLANDI]" : (active ? "[AKTİF]" : (regionOk && prereqOk ? "[MÜSAİT]" : "[KİLİTLİ]"));
                GUILayout.Label($"{statusTag} {dm.Tr(q.titleKey)} — Ödül: {q.goldReward} Altın, {q.xpReward} XP", _styleTitle);
                GUILayout.Label($"{dm.Tr(q.descKey)} ({prog}/{q.targetValue})", _styleSmall);
                GUILayout.EndVertical();
                GUILayout.FlexibleSpace();

                if (!active && !completed && regionOk && prereqOk)
                {
                    if (GUILayout.Button(dm.Tr("btn_accept_quest"), _styleButton, GUILayout.Width(120f), GUILayout.Height(28f)))
                        QuestSystem.AcceptQuest(st, dm, q.id);
                }
                GUILayout.EndHorizontal();
                GUILayout.EndVertical();
                GUILayout.Space(4f);
            }
            GUILayout.EndScrollView();
        }

        private void DrawRegionsModal()
        {
            var st = GameManager.Instance.State;
            var dm = DataManager.Instance;

            GUILayout.Label(dm.Tr("menu_regions_title"), _styleHeader);
            GUILayout.Space(8f);

            _scrollPos = GUILayout.BeginScrollView(_scrollPos, GUILayout.Height(350f));
            foreach (var r in dm.Regions.regions)
            {
                bool unlocked = st.level >= r.unlockLevel || st.unlockedRegions.Contains(r.id);
                bool current = st.currentRegionId == r.id;

                GUILayout.BeginVertical(_styleTag);
                GUILayout.BeginHorizontal();
                GUILayout.BeginVertical();
                GUILayout.Label($"{dm.Tr(r.nameKey)} (Açılış Seviyesi: {r.unlockLevel})", _styleTitle);
                GUILayout.Label(dm.Tr(r.descKey), _styleSmall);
                GUILayout.EndVertical();
                GUILayout.FlexibleSpace();

                if (current)
                {
                    GUILayout.Label("Şu An Buradasın", _styleTitle, GUILayout.Width(140f));
                }
                else if (unlocked)
                {
                    if (GUILayout.Button(dm.Tr("btn_travel"), _styleButton, GUILayout.Width(130f), GUILayout.Height(30f)))
                    {
                        GameManager.Instance.TravelToRegion(r.id);
                        SetMenu(ActiveModalMenu.None);
                    }
                }
                else
                {
                    GUILayout.Label($"Seviye {r.unlockLevel} Gerekli", _styleSmall, GUILayout.Width(140f));
                }

                GUILayout.EndHorizontal();
                GUILayout.EndVertical();
                GUILayout.Space(4f);
            }
            GUILayout.EndScrollView();
        }

        private void DrawHorseCareModal()
        {
            if (_inspectedHorse == null)
            {
                SetMenu(ActiveModalMenu.None);
                return;
            }

            var h = _inspectedHorse;
            var dm = DataManager.Instance;
            var breed = dm.GetBreed(h.breedId);
            string bName = breed != null ? dm.Tr(breed.nameKey) : h.breedId;
            string sexName = h.sex == "mare" ? dm.Tr("sex_mare") : dm.Tr("sex_stallion");
            string rarityName = dm.Tr(dm.GetRaritySetting(h.rarity).nameKey);
            string reqFeedId = breed != null ? breed.requiredFeedId : "item_basic_feed";
            var feedItem = dm.GetItem(reqFeedId);
            string feedName = feedItem != null ? dm.Tr(feedItem.nameKey) : reqFeedId;

            int feedCount = InventorySystem.GetItemCount(GameManager.Instance.State, reqFeedId);
            int waterCount = InventorySystem.GetItemCount(GameManager.Instance.State, "item_water");
            int groomCount = InventorySystem.GetItemCount(GameManager.Instance.State, "item_grooming_kit");

            GUILayout.Label($"{h.name} — {bName} ({rarityName})", _styleHeader);
            GUILayout.Label($"Cinsiyet: {sexName} | Hız: {h.genes.speed} | Dayanıklılık: {h.genes.stamina}", _styleBody);
            GUILayout.Space(12f);

            float crit = dm.Balance.needs.criticalThreshold;

            Rect hRect = GUILayoutUtility.GetRect(600f, 24f);
            DrawProgressBar(hRect, h.hunger / 100f, h.hunger >= crit ? _texRed : _texGold,
                $"{dm.Tr("horse_hunger")}: {Mathf.RoundToInt(h.hunger)} / 100");
            GUILayout.Space(8f);

            Rect tRect = GUILayoutUtility.GetRect(600f, 24f);
            DrawProgressBar(tRect, h.thirst / 100f, h.thirst >= crit ? _texRed : _texBlue,
                $"{dm.Tr("horse_thirst")}: {Mathf.RoundToInt(h.thirst)} / 100");
            GUILayout.Space(8f);

            Rect trRect = GUILayoutUtility.GetRect(600f, 24f);
            DrawProgressBar(trRect, h.trust / 100f, _texGreen,
                $"{dm.Tr("horse_trust")}: {Mathf.RoundToInt(h.trust)} / 100 (≥60 olunca seni takip eder)");

            GUILayout.Space(18f);

            if (GUILayout.Button($"{dm.Tr("btn_feed", feedName)} [Envanterde: {feedCount}]", _styleButton, GUILayout.Height(38f)))
                GameManager.Instance.FeedHorse(h);

            GUILayout.Space(6f);
            if (GUILayout.Button($"{dm.Tr("btn_water")} [Envanterde: {waterCount}]", _styleButton, GUILayout.Height(38f)))
                GameManager.Instance.WaterHorse(h);

            GUILayout.Space(6f);
            if (GUILayout.Button($"{dm.Tr("btn_groom")} [Envanterde: {groomCount}]", _styleButton, GUILayout.Height(38f)))
                GameManager.Instance.GroomHorse(h);
        }

        private void DrawProgressBar(Rect rect, float normalizedValue, Texture2D fillTex, string label)
        {
            normalizedValue = Mathf.Clamp01(normalizedValue);
            GUI.DrawTexture(rect, _texBarBg);
            if (normalizedValue > 0.005f)
            {
                Rect fillRect = new Rect(rect.x + 2f, rect.y + 2f, (rect.width - 4f) * normalizedValue, rect.height - 4f);
                GUI.DrawTexture(fillRect, fillTex);
            }
            GUI.Label(rect, label, _styleTag);
        }
    }
}
