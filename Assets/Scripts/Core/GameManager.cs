using System;
using System.Collections.Generic;
using UnityEngine;
using HorseFarm.Horse;
using HorseFarm.World;
using HorseFarm.Player;
using HorseFarm.Economy;
using HorseFarm.Events;
using HorseFarm.Audio;

namespace HorseFarm.Core
{
    [Serializable]
    public class HorseCoatPrefabEntry
    {
        public string coatId;
        public GameObject prefab;
    }

    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        public PlayerState State = new PlayerState();
        public List<HorseCoatPrefabEntry> horseCoatPrefabs = new List<HorseCoatPrefabEntry>();
        public RuntimeAnimatorController horseAnimatorController;
        public Vector3 pastureCenter = new Vector3(0f, 0f, 3f);
        public Vector2 pastureExtents = new Vector2(13f, 11f);

        public readonly List<HorseController> SpawnedHorses = new List<HorseController>();
        public readonly List<InteractableStation> Stations = new List<InteractableStation>();

        public bool IsAnyModalOpen { get; set; }

        private float _autoSaveTimer = 25f;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            Application.targetFrameRate = 60;
        }

        private void Start()
        {
            CoreLogicTests.RunAllTests();
            EnsureConnectedPastureFencesRuntime();

            Stations.Clear();
            Stations.AddRange(FindObjectsByType<InteractableStation>(FindObjectsSortMode.None));

            PlayerState loaded = SaveManager.Load();
            if (loaded != null)
            {
                State = loaded;
            }
            else
            {
                InitializeNewGameState();
            }

            QuestSystem.EnsureStarterQuestsActive(State, DataManager.Instance);
            SpawnAllHorsesFromState();
            RegionEnvironmentController.Instance?.ApplyRegionTheme(State.currentRegionId);

            GameEvents.RaiseGoldChanged(State.gold);
            GameEvents.RaiseXpOrLevelChanged(State.level, State.xp, EconomySystem.GetRequiredXpForLevel(State.level));
            GameEvents.RaiseInventoryChanged();
            GameEvents.RaiseHorsesChanged();
            GameEvents.RaiseQuestsChanged();
        }

        private void Update()
        {
            if (DataManager.Instance == null) return;

            float dt = Time.deltaTime;
            var needsCfg = DataManager.Instance.Balance.needs;
            int maxAI = DataManager.Instance.Balance.ai.maxActiveAIHorses;

            Transform playerTr = PlayerController.Instance != null ? PlayerController.Instance.transform : null;
            bool playerRunning = PlayerController.Instance != null && PlayerController.Instance.IsRunning;
            bool dangerActive = DangerEventManager.Instance != null && DangerEventManager.Instance.IsEventActive;
            Vector3 dangerPos = (dangerActive && DangerEventManager.Instance.ActiveWolf != null)
                ? DangerEventManager.Instance.ActiveWolf.transform.position
                : Vector3.zero;

            // GDD 5.4 & 10: Update at most 20 active AI horses
            int count = Mathf.Min(SpawnedHorses.Count, maxAI);
            for (int i = 0; i < count; i++)
            {
                var h = SpawnedHorses[i];
                if (h == null || h.Data == null) continue;
                var rarityCfg = DataManager.Instance.GetRaritySetting(h.Data.rarity);
                h.TickNeeds(dt, needsCfg, rarityCfg);
                h.TickAI(dt, playerTr, playerRunning, dangerActive, dangerPos);
            }

            _autoSaveTimer -= dt;
            if (_autoSaveTimer <= 0f)
            {
                _autoSaveTimer = 25f;
                SaveManager.Save(State);
            }
        }

        public void InitializeNewGameState()
        {
            var startCfg = DataManager.Instance?.Balance?.startingState ?? new StartingStateConfig();
            State = new PlayerState
            {
                saveVersion = 1,
                gold = startCfg.gold,
                level = startCfg.level,
                xp = 0,
                currentRegionId = startCfg.startingRegionId,
                horseCapacity = startCfg.horseCapacity,
                inventory = new InventoryData { slots = startCfg.inventorySlots },
                upgrades = new UpgradesData { barn = 0, storage = 0 }
            };

            State.unlockedRegions.Add(startCfg.startingRegionId);
            State.unlockedBreedIds.Add("breed_farm_horse");

            foreach (var item in startCfg.startingItems)
            {
                InventorySystem.AddItem(State, item.itemId, item.count, DataManager.Instance.Balance.inventory.maxStackSize);
            }

            // GDD 5.0: 2 Çiftlik Atı (1 dişi, 1 erkek), güven 30
            var farmBreed = DataManager.Instance?.GetBreed("breed_farm_horse");
            HorseData mare = EconomySystem.CreateNewHorse(farmBreed, "Papatya", "mare", startCfg.startingHorseTrust);
            HorseData stallion = EconomySystem.CreateNewHorse(farmBreed, "Rüzgar", "stallion", startCfg.startingHorseTrust);
            State.horses.Add(mare);
            State.horses.Add(stallion);

            QuestSystem.EnsureStarterQuestsActive(State, DataManager.Instance);
            SaveManager.Save(State);
        }

        public void ResetToNewGame()
        {
            SaveManager.DeleteSave();
            for (int i = 0; i < SpawnedHorses.Count; i++)
            {
                if (SpawnedHorses[i] != null)
                    Destroy(SpawnedHorses[i].gameObject);
            }
            SpawnedHorses.Clear();
            InitializeNewGameState();
            SpawnAllHorsesFromState();
            RegionEnvironmentController.Instance?.ApplyRegionTheme(State.currentRegionId);
            GameEvents.RaiseGoldChanged(State.gold);
            GameEvents.RaiseXpOrLevelChanged(State.level, State.xp, EconomySystem.GetRequiredXpForLevel(State.level));
            GameEvents.RaiseInventoryChanged();
            GameEvents.RaiseHorsesChanged();
            GameEvents.RaiseQuestsChanged();
        }

        private void SpawnAllHorsesFromState()
        {
            for (int i = 0; i < SpawnedHorses.Count; i++)
            {
                if (SpawnedHorses[i] != null)
                    Destroy(SpawnedHorses[i].gameObject);
            }
            SpawnedHorses.Clear();

            for (int i = 0; i < State.horses.Count; i++)
            {
                SpawnHorseInstance(State.horses[i], i);
            }
        }

        private HorseController SpawnHorseInstance(HorseData hData, int indexOffset)
        {
            GameObject prefab = GetPrefabForCoat(hData.genes.coat);
            float angle = indexOffset * 1.35f;
            float radius = 3.2f + (indexOffset % 3) * 2.2f;
            Vector3 spawnPos = pastureCenter + new Vector3(Mathf.Cos(angle) * radius, 0f, Mathf.Sin(angle) * radius);
            spawnPos.y = 0f;

            GameObject obj;
            if (prefab != null)
            {
                obj = Instantiate(prefab, spawnPos, Quaternion.Euler(0f, UnityEngine.Random.Range(0f, 360f), 0f));
            }
            else
            {
                obj = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                obj.transform.position = spawnPos;
            }
            obj.name = $"Horse_{hData.name}_{hData.id}";

            Animator anim = obj.GetComponentInChildren<Animator>();
            if (anim != null && horseAnimatorController != null)
            {
                anim.runtimeAnimatorController = horseAnimatorController;
                anim.applyRootMotion = false;
            }

            if (obj.GetComponent<Collider>() == null)
            {
                BoxCollider bc = obj.AddComponent<BoxCollider>();
                bc.center = new Vector3(0f, 1.1f, 0f);
                bc.size = new Vector3(0.9f, 2.0f, 2.2f);
            }

            HorseController ctrl = obj.GetComponent<HorseController>();
            if (ctrl == null)
                ctrl = obj.AddComponent<HorseController>();

            ctrl.Initialize(hData, pastureCenter, pastureExtents);
            SpawnedHorses.Add(ctrl);
            return ctrl;
        }

        private GameObject GetPrefabForCoat(string coatId)
        {
            for (int i = 0; i < horseCoatPrefabs.Count; i++)
            {
                if (horseCoatPrefabs[i].coatId == coatId && horseCoatPrefabs[i].prefab != null)
                    return horseCoatPrefabs[i].prefab;
            }
            if (horseCoatPrefabs.Count > 0)
                return horseCoatPrefabs[0].prefab;
            return null;
        }

        public HorseController FindHorseController(string horseId)
        {
            for (int i = 0; i < SpawnedHorses.Count; i++)
            {
                if (SpawnedHorses[i] != null && SpawnedHorses[i].Data != null && SpawnedHorses[i].Data.id == horseId)
                    return SpawnedHorses[i];
            }
            return null;
        }

        // --- Care Actions (GDD 5.3 & 5.6 & 5.7) ---

        public bool FeedHorse(HorseData horse)
        {
            if (horse == null || DataManager.Instance == null) return false;
            var breed = DataManager.Instance.GetBreed(horse.breedId);
            string requiredFeedId = breed != null && !string.IsNullOrEmpty(breed.requiredFeedId)
                ? breed.requiredFeedId
                : "item_basic_feed";

            var itemCfg = DataManager.Instance.GetItem(requiredFeedId);
            if (!InventorySystem.RemoveItem(State, requiredFeedId, 1))
            {
                string feedName = itemCfg != null ? DataManager.Instance.Tr(itemCfg.nameKey) : requiredFeedId;
                GameEvents.RaiseNotification(DataManager.Instance.Tr("notify_missing_feed", feedName));
                return false;
            }

            var rarityCfg = DataManager.Instance.GetRaritySetting(horse.rarity);
            float hungerRed = itemCfg != null ? itemCfg.hungerReduction : 30f;
            float baseTrustGain = itemCfg != null ? itemCfg.trustGain : DataManager.Instance.Balance.needs.feedTrustGain;
            float trustGain = baseTrustGain * rarityCfg.trustMultiplier;

            horse.hunger = Mathf.Clamp(horse.hunger - hungerRed, 0f, 100f);
            if (itemCfg != null && itemCfg.thirstReduction > 0f)
                horse.thirst = Mathf.Clamp(horse.thirst - itemCfg.thirstReduction, 0f, 100f);
            horse.trust = Mathf.Clamp(horse.trust + trustGain, 0f, 100f);

            AudioManager.Instance?.PlayChew();
            FindHorseController(horse.id)?.TriggerInteractReaction(false);

            int xpGain = DataManager.Instance.Balance.xp.feedOrWater;
            AddXp(xpGain);
            QuestSystem.RecordAction(State, DataManager.Instance, "feed", 1);
            QuestSystem.RecordAction(State, DataManager.Instance, "trust", 0);

            GameEvents.RaiseNotification(DataManager.Instance.Tr("notify_fed_horse", horse.name, xpGain));
            GameEvents.RaiseHorsesChanged();
            SaveManager.Save(State);
            return true;
        }

        public bool WaterHorse(HorseData horse)
        {
            if (horse == null || DataManager.Instance == null) return false;
            if (!InventorySystem.RemoveItem(State, "item_water", 1))
            {
                GameEvents.RaiseNotification(DataManager.Instance.Tr("notify_missing_water"));
                return false;
            }

            var itemCfg = DataManager.Instance.GetItem("item_water");
            var rarityCfg = DataManager.Instance.GetRaritySetting(horse.rarity);
            float thirstRed = itemCfg != null ? itemCfg.thirstReduction : 40f;
            float trustGain = DataManager.Instance.Balance.needs.waterTrustGain * rarityCfg.trustMultiplier;

            horse.thirst = Mathf.Clamp(horse.thirst - thirstRed, 0f, 100f);
            horse.trust = Mathf.Clamp(horse.trust + trustGain, 0f, 100f);

            AudioManager.Instance?.PlayDrink();
            FindHorseController(horse.id)?.TriggerInteractReaction(false);

            int xpGain = DataManager.Instance.Balance.xp.feedOrWater;
            AddXp(xpGain);
            QuestSystem.RecordAction(State, DataManager.Instance, "water", 1);
            QuestSystem.RecordAction(State, DataManager.Instance, "trust", 0);

            GameEvents.RaiseNotification(DataManager.Instance.Tr("notify_watered_horse", horse.name, xpGain));
            GameEvents.RaiseHorsesChanged();
            SaveManager.Save(State);
            return true;
        }

        public bool GroomHorse(HorseData horse)
        {
            if (horse == null || DataManager.Instance == null) return false;

            float cooldown = DataManager.Instance.Balance.needs.groomCooldownSec;
            float elapsed = Time.time - horse.lastGroomTime;
            if (elapsed < cooldown)
            {
                int waitSec = Mathf.CeilToInt(cooldown - elapsed);
                GameEvents.RaiseNotification(DataManager.Instance.Tr("notify_groom_cooldown", waitSec));
                return false;
            }

            if (!InventorySystem.RemoveItem(State, "item_grooming_kit", 1))
            {
                GameEvents.RaiseNotification(DataManager.Instance.Tr("notify_missing_groom"));
                return false;
            }

            horse.lastGroomTime = Time.time;
            var rarityCfg = DataManager.Instance.GetRaritySetting(horse.rarity);
            float trustGain = DataManager.Instance.Balance.needs.groomTrustGain * rarityCfg.trustMultiplier;
            horse.trust = Mathf.Clamp(horse.trust + trustGain, 0f, 100f);

            AudioManager.Instance?.PlayGroom();
            FindHorseController(horse.id)?.TriggerInteractReaction(true);

            int xpGain = DataManager.Instance.Balance.xp.groom;
            AddXp(xpGain);
            QuestSystem.RecordAction(State, DataManager.Instance, "care", 1);
            QuestSystem.RecordAction(State, DataManager.Instance, "trust", 0);

            GameEvents.RaiseNotification(DataManager.Instance.Tr("notify_groomed_horse", horse.name, Mathf.RoundToInt(trustGain), xpGain));
            GameEvents.RaiseHorsesChanged();
            SaveManager.Save(State);
            return true;
        }

        // --- Merchant & Market & Upgrades (GDD 5.6, 5.7, 5.8, 5.9) ---

        public bool BuyItemFromMerchant(string itemId, int quantity)
        {
            if (DataManager.Instance == null || quantity <= 0) return false;
            quantity = Mathf.Clamp(quantity, 1, DataManager.Instance.Balance.inventory.maxBulkBuy);

            var itemCfg = DataManager.Instance.GetItem(itemId);
            if (itemCfg == null) return false;

            int totalCost = itemCfg.price * quantity;
            if (State.gold < totalCost)
            {
                GameEvents.RaiseNotification(DataManager.Instance.Tr("notify_not_enough_gold"));
                return false;
            }

            if (!InventorySystem.CanAddItem(State, itemId, quantity, DataManager.Instance.Balance.inventory.maxStackSize))
            {
                GameEvents.RaiseNotification(DataManager.Instance.Tr("notify_inventory_full"));
                return false;
            }

            State.gold -= totalCost;
            InventorySystem.AddItem(State, itemId, quantity, DataManager.Instance.Balance.inventory.maxStackSize);
            AudioManager.Instance?.PlayCoin();
            GameEvents.RaiseGoldChanged(State.gold);
            GameEvents.RaiseNotification(DataManager.Instance.Tr("notify_bought_item", quantity, DataManager.Instance.Tr(itemCfg.nameKey)));
            SaveManager.Save(State);
            return true;
        }

        public bool BuyHorseFromMerchant(string breedId)
        {
            if (DataManager.Instance == null) return false;
            var breed = DataManager.Instance.GetBreed(breedId);
            if (breed == null) return false;

            if (!EconomySystem.IsBreedUnlocked(State, breed))
                return false;

            if (State.horses.Count >= State.horseCapacity)
            {
                GameEvents.RaiseNotification(DataManager.Instance.Tr("notify_capacity_full"));
                return false;
            }

            if (State.gold < breed.price)
            {
                GameEvents.RaiseNotification(DataManager.Instance.Tr("notify_not_enough_gold"));
                return false;
            }

            State.gold -= breed.price;
            if (!State.unlockedBreedIds.Contains(breed.id))
                State.unlockedBreedIds.Add(breed.id);

            // GDD 5.6: "Tüccardan satın alınan at, çiftliğe hemen eklenir (kota yeterliyse) ve güven 20 ile başlar."
            HorseData newHorse = EconomySystem.CreateNewHorse(breed, null, null, 20f);
            State.horses.Add(newHorse);
            SpawnHorseInstance(newHorse, State.horses.Count - 1);

            int xpGain = DataManager.Instance.Balance.xp.buyNewBreed;
            AddXp(xpGain);
            AudioManager.Instance?.PlayCoin();
            AudioManager.Instance?.PlayNeigh();

            GameEvents.RaiseGoldChanged(State.gold);
            GameEvents.RaiseHorsesChanged();
            GameEvents.RaiseNotification(DataManager.Instance.Tr("notify_bought_horse", DataManager.Instance.Tr(breed.nameKey), xpGain));
            SaveManager.Save(State);
            return true;
        }

        public bool SellHorseAtMarket(HorseData horse)
        {
            if (horse == null || DataManager.Instance == null) return false;
            if (!State.horses.Contains(horse)) return false;

            int price = EconomySystem.CalculateHorseSellPrice(horse, DataManager.Instance);
            int xpGain = Mathf.RoundToInt(price * DataManager.Instance.Balance.xp.sellHorsePriceRatio);

            State.horses.Remove(horse);
            HorseController ctrl = FindHorseController(horse.id);
            if (ctrl != null)
            {
                SpawnedHorses.Remove(ctrl);
                Destroy(ctrl.gameObject);
            }

            AddGold(price);
            AddXp(xpGain);
            AudioManager.Instance?.PlayCoin();
            QuestSystem.RecordAction(State, DataManager.Instance, "sell", 1);

            GameEvents.RaiseHorsesChanged();
            GameEvents.RaiseNotification(DataManager.Instance.Tr("notify_sold_horse", horse.name, price, xpGain));
            SaveManager.Save(State);
            return true;
        }

        public bool UpgradeBarn()
        {
            if (DataManager.Instance == null) return false;
            int nextLv = State.upgrades.barn + 1;
            BarnUpgradeConfig nextCfg = null;
            foreach (var b in DataManager.Instance.Balance.barnUpgrades)
            {
                if (b.level == nextLv) { nextCfg = b; break; }
            }
            if (nextCfg == null) return false;

            if (State.gold < nextCfg.cost)
            {
                GameEvents.RaiseNotification(DataManager.Instance.Tr("notify_not_enough_gold"));
                return false;
            }

            State.gold -= nextCfg.cost;
            State.upgrades.barn = nextLv;
            State.horseCapacity = nextCfg.capacity;

            AudioManager.Instance?.PlayCoin();
            GameEvents.RaiseGoldChanged(State.gold);
            GameEvents.RaiseHorsesChanged();
            GameEvents.RaiseNotification(DataManager.Instance.Tr("notify_upgraded_barn", State.horseCapacity));
            SaveManager.Save(State);
            return true;
        }

        public bool UpgradeStorage()
        {
            if (DataManager.Instance == null) return false;
            int nextLv = State.upgrades.storage + 1;
            StorageUpgradeConfig nextCfg = null;
            foreach (var s in DataManager.Instance.Balance.storageUpgrades)
            {
                if (s.level == nextLv) { nextCfg = s; break; }
            }
            if (nextCfg == null) return false;

            if (State.gold < nextCfg.cost)
            {
                GameEvents.RaiseNotification(DataManager.Instance.Tr("notify_not_enough_gold"));
                return false;
            }

            State.gold -= nextCfg.cost;
            State.upgrades.storage = nextLv;
            State.inventory.slots = nextCfg.slots;

            AudioManager.Instance?.PlayCoin();
            GameEvents.RaiseGoldChanged(State.gold);
            GameEvents.RaiseInventoryChanged();
            GameEvents.RaiseNotification(DataManager.Instance.Tr("notify_upgraded_storage", State.inventory.slots));
            SaveManager.Save(State);
            return true;
        }

        // --- Quests, XP, Levels & Regions (GDD 5.2 & 5.10) ---

        public void CompleteQuest(string questId)
        {
            if (DataManager.Instance == null) return;
            var q = DataManager.Instance.GetQuest(questId);
            if (q == null) return;

            State.activeQuests.Remove(questId);
            if (!State.completedQuests.Contains(questId))
                State.completedQuests.Add(questId);

            AddGold(q.goldReward);
            AddXp(q.xpReward);
            AudioManager.Instance?.PlayCoin();

            GameEvents.RaiseNotification(DataManager.Instance.Tr("notify_quest_completed",
                DataManager.Instance.Tr(q.titleKey), q.goldReward, q.xpReward));

            if (!string.IsNullOrEmpty(q.unlockBreedId) && !State.unlockedBreedIds.Contains(q.unlockBreedId))
            {
                State.unlockedBreedIds.Add(q.unlockBreedId);
                var unlockedBreed = DataManager.Instance.GetBreed(q.unlockBreedId);
                if (unlockedBreed != null)
                {
                    GameEvents.RaiseNotification(DataManager.Instance.Tr("notify_breed_unlocked",
                        DataManager.Instance.Tr(unlockedBreed.nameKey)));
                }
            }

            QuestSystem.EnsureStarterQuestsActive(State, DataManager.Instance);
            GameEvents.RaiseQuestsChanged();
            SaveManager.Save(State);
        }

        public void AddGold(int amount)
        {
            if (amount == 0) return;
            State.gold = Mathf.Max(0, State.gold + amount);
            GameEvents.RaiseGoldChanged(State.gold);
        }

        public void AddXp(int amount)
        {
            if (amount <= 0 || DataManager.Instance == null) return;
            State.xp += amount;

            int req = EconomySystem.GetRequiredXpForLevel(State.level);
            while (State.xp >= req)
            {
                State.xp -= req;
                State.level++;
                req = EconomySystem.GetRequiredXpForLevel(State.level);

                AudioManager.Instance?.PlayLevelUp();
                string title = DataManager.Instance.GetPlayerTitle(State.level);
                GameEvents.RaiseNotification(DataManager.Instance.Tr("notify_level_up", State.level, title));

                CheckRegionUnlocks();
            }

            GameEvents.RaiseXpOrLevelChanged(State.level, State.xp, req);
        }

        private void CheckRegionUnlocks()
        {
            if (DataManager.Instance?.Regions?.regions == null) return;
            foreach (var r in DataManager.Instance.Regions.regions)
            {
                if (State.level >= r.unlockLevel && !State.unlockedRegions.Contains(r.id))
                {
                    State.unlockedRegions.Add(r.id);
                    GameEvents.RaiseNotification(DataManager.Instance.Tr("notify_region_unlocked",
                        DataManager.Instance.Tr(r.nameKey)));
                }
            }
            QuestSystem.EnsureStarterQuestsActive(State, DataManager.Instance);
        }

        public bool TravelToRegion(string regionId)
        {
            if (DataManager.Instance == null) return false;
            var reg = DataManager.Instance.GetRegion(regionId);
            if (reg == null) return false;

            if (State.level < reg.unlockLevel && !State.unlockedRegions.Contains(regionId))
                return false;

            if (!State.unlockedRegions.Contains(regionId))
                State.unlockedRegions.Add(regionId);

            State.currentRegionId = regionId;
            QuestSystem.EnsureStarterQuestsActive(State, DataManager.Instance);
            GameEvents.RaiseRegionChanged(regionId);
            GameEvents.RaiseQuestsChanged();
            GameEvents.RaiseNotification(DataManager.Instance.Tr("notify_region_traveled",
                DataManager.Instance.Tr(reg.nameKey)));
            SaveManager.Save(State);
            return true;
        }

        // --- Danger Event Outcomes (GDD 5.5) ---

        public void ApplyMinorStartleTrustLoss(float loss)
        {
            for (int i = 0; i < State.horses.Count; i++)
            {
                State.horses[i].trust = Mathf.Clamp(State.horses[i].trust - loss, 0f, 100f);
            }
            GameEvents.RaiseHorsesChanged();
        }

        public void ApplyWolfBarnFailurePenalty(float trustPenalty, float panicDurationSec)
        {
            if (State.horses.Count == 0) return;
            int idx = UnityEngine.Random.Range(0, State.horses.Count);
            HorseData victim = State.horses[idx];
            victim.trust = Mathf.Clamp(victim.trust - trustPenalty, 0f, 100f);

            HorseController ctrl = FindHorseController(victim.id);
            if (ctrl != null)
            {
                ctrl.TriggerPanic(panicDurationSec);
            }

            if (DataManager.Instance != null)
            {
                GameEvents.RaiseNotification(DataManager.Instance.Tr("notify_wolf_reached_barn", victim.name, Mathf.RoundToInt(trustPenalty)));
            }
            GameEvents.RaiseHorsesChanged();
        }

        private void EnsureConnectedPastureFencesRuntime()
        {
            if (GameObject.Find("FenceWallCollider") != null) return;

            GameObject oldFences = GameObject.Find("PastureFences");
            if (oldFences == null || oldFences.transform.childCount == 0) return;

            GameObject sampleFence = oldFences.transform.GetChild(0).gameObject;
            MeshFilter mf = sampleFence.GetComponentInChildren<MeshFilter>();
            if (mf == null || mf.sharedMesh == null) return;

            GameObject fenceTemplate = Instantiate(sampleFence);
            fenceTemplate.transform.localScale = Vector3.one;
            fenceTemplate.SetActive(false);

            Transform parent = oldFences.transform.parent;
            Destroy(oldFences);

            GameObject newFenceRoot = new GameObject("PastureFences");
            if (parent != null) newFenceRoot.transform.SetParent(parent);

            BuildRuntimeFenceLine(newFenceRoot.transform, new Vector3(-16f, 0f, -10f), new Vector3(-16f, 0f, 14f), fenceTemplate);
            BuildRuntimeFenceLine(newFenceRoot.transform, new Vector3(16f, 0f, -10f), new Vector3(16f, 0f, 14f), fenceTemplate);
            BuildRuntimeFenceLine(newFenceRoot.transform, new Vector3(-16f, 0f, 14f), new Vector3(-5.2f, 0f, 14f), fenceTemplate);
            BuildRuntimeFenceLine(newFenceRoot.transform, new Vector3(5.2f, 0f, 14f), new Vector3(16f, 0f, 14f), fenceTemplate);
            BuildRuntimeFenceLine(newFenceRoot.transform, new Vector3(-16f, 0f, -10f), new Vector3(-3.2f, 0f, -10f), fenceTemplate);
            BuildRuntimeFenceLine(newFenceRoot.transform, new Vector3(3.2f, 0f, -10f), new Vector3(16f, 0f, -10f), fenceTemplate);

            Destroy(fenceTemplate);
        }

        private void BuildRuntimeFenceLine(Transform parent, Vector3 startPos, Vector3 endPos, GameObject fenceTemplate)
        {
            MeshFilter mf = fenceTemplate.GetComponentInChildren<MeshFilter>();
            Bounds b = mf.sharedMesh.bounds;
            bool xIsLongAxis = b.size.x >= b.size.z;
            float rawLength = xIsLongAxis ? b.size.x : b.size.z;
            if (rawLength < 0.1f) rawLength = 2.5f;

            Vector3 lineVec = endPos - startPos;
            lineVec.y = 0f;
            float totalDist = lineVec.magnitude;
            if (totalDist < 0.2f) return;

            Vector3 lineDir = lineVec / totalDist;
            int count = Mathf.Max(1, Mathf.RoundToInt(totalDist / rawLength));
            float segmentDist = totalDist / count;
            float lengthScale = (segmentDist / rawLength) * 1.03f;

            Quaternion lineRot = Quaternion.LookRotation(lineDir, Vector3.up);
            Quaternion pieceRot = xIsLongAxis ? (lineRot * Quaternion.Euler(0f, -90f, 0f)) : lineRot;
            Vector3 pieceScale = xIsLongAxis ? new Vector3(lengthScale, 1f, 1f) : new Vector3(1f, 1f, lengthScale);

            for (int i = 0; i < count; i++)
            {
                Vector3 segCenterWorld = startPos + lineDir * ((i + 0.5f) * segmentDist);
                GameObject piece = Instantiate(fenceTemplate, parent);
                piece.SetActive(true);
                piece.name = $"FenceSeg_{i}";
                piece.transform.rotation = pieceRot;
                piece.transform.localScale = pieceScale;

                Vector3 scaledLocalCenter = Vector3.Scale(b.center, pieceScale);
                Vector3 horizOffset = pieceRot * new Vector3(scaledLocalCenter.x, 0f, scaledLocalCenter.z);
                piece.transform.position = segCenterWorld - horizOffset;
            }

            GameObject wallColObj = new GameObject("FenceWallCollider");
            wallColObj.transform.SetParent(parent);
            wallColObj.transform.position = (startPos + endPos) * 0.5f + Vector3.up * 0.75f;
            wallColObj.transform.rotation = lineRot;
            BoxCollider wallBc = wallColObj.AddComponent<BoxCollider>();
            wallBc.size = new Vector3(0.35f, 1.5f, totalDist);
        }
    }
}
