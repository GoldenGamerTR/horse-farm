using System;
using System.Collections.Generic;
using UnityEngine;

namespace HorseFarm.Core
{
    [Serializable]
    public class StartingItemConfig
    {
        public string itemId;
        public int count;
    }

    [Serializable]
    public class StartingStateConfig
    {
        public int gold = 100;
        public int inventorySlots = 12;
        public int horseCapacity = 2;
        public int level = 1;
        public string startingRegionId = "region_starter_farm";
        public float startingHorseTrust = 30f;
        public List<StartingItemConfig> startingItems = new List<StartingItemConfig>();
    }

    [Serializable]
    public class PlayerBalanceConfig
    {
        public float walkSpeed = 4.5f;
        public float runSpeed = 8.0f;
        public float jumpHeight = 1.4f;
        public float interactRange = 4.5f;
        public float screamCooldownSec = 3.0f;
        public float screamRange = 15.0f;
        public float screamAngleDeg = 65.0f;
    }

    [Serializable]
    public class NeedsBalanceConfig
    {
        public float hungerIncreasePerMin = 10.0f;
        public float thirstIncreasePerMin = 12.0f;
        public float trustLossHighNeedsPerMin = 3.0f;
        public float trustLossNoInteractionPerMin = 1.0f;
        public float groomTrustGain = 8.0f;
        public float groomCooldownSec = 30.0f;
        public float feedTrustGain = 2.0f;
        public float waterTrustGain = 2.0f;
        public float criticalThreshold = 70.0f;
    }

    [Serializable]
    public class AIBalanceConfig
    {
        public float followTrustThreshold = 60.0f;
        public float followMaxDistance = 15.0f;
        public float followStopDistance = 2.8f;
        public float fleeTrustThreshold = 20.0f;
        public float fleeRunDistance = 8.0f;
        public float panicTrustLossPerSec = 5.0f;
        public float panicDurationSec = 120.0f;
        public int maxActiveAIHorses = 20;
    }

    [Serializable]
    public class InventoryBalanceConfig
    {
        public int maxStackSize = 20;
        public int maxBulkBuy = 20;
    }

    [Serializable]
    public class XpBalanceConfig
    {
        public int feedOrWater = 5;
        public int groom = 8;
        public int buyNewBreed = 30;
        public int repelThreat = 20;
        public float sellHorsePriceRatio = 0.10f;
        public float levelBaseXp = 100.0f;
        public float levelExponent = 1.5f;
        public int maxActiveQuests = 3;
    }

    [Serializable]
    public class RaritySettingConfig
    {
        public int rarity;
        public string nameKey;
        public int baseSellValue;
        public int merchantPrice;
        public float trustMultiplier;
        public float panicThreshold;
    }

    [Serializable]
    public class BarnUpgradeConfig
    {
        public int level;
        public int capacity;
        public int cost;
    }

    [Serializable]
    public class StorageUpgradeConfig
    {
        public int level;
        public int slots;
        public int cost;
    }

    [Serializable]
    public class TitleConfig
    {
        public int minLevel;
        public int maxLevel;
        public string titleKey;
    }

    [Serializable]
    public class BalanceConfigRoot
    {
        public StartingStateConfig startingState = new StartingStateConfig();
        public PlayerBalanceConfig player = new PlayerBalanceConfig();
        public NeedsBalanceConfig needs = new NeedsBalanceConfig();
        public AIBalanceConfig ai = new AIBalanceConfig();
        public InventoryBalanceConfig inventory = new InventoryBalanceConfig();
        public XpBalanceConfig xp = new XpBalanceConfig();
        public List<RaritySettingConfig> raritySettings = new List<RaritySettingConfig>();
        public List<BarnUpgradeConfig> barnUpgrades = new List<BarnUpgradeConfig>();
        public List<StorageUpgradeConfig> storageUpgrades = new List<StorageUpgradeConfig>();
        public List<TitleConfig> titles = new List<TitleConfig>();
    }

    [Serializable]
    public class ItemConfig
    {
        public string id;
        public string nameKey;
        public string descKey;
        public string category; // "water", "feed", "groom"
        public float hungerReduction;
        public float thirstReduction;
        public float trustGain;
        public int price;
        public string requiredRegionId;
    }

    [Serializable]
    public class ItemsConfigRoot
    {
        public List<ItemConfig> items = new List<ItemConfig>();
    }

    [Serializable]
    public class BreedConfig
    {
        public string id;
        public string nameKey;
        public string descKey;
        public int rarity;
        public int price;
        public int baseSellValue;
        public string requiredFeedId;
        public int requiredLevel;
        public string requiredQuestId;
        public string regionId;
        public string coatPrefab;
        public int minSpeed;
        public int maxSpeed;
        public int minStamina;
        public int maxStamina;
    }

    [Serializable]
    public class BreedsConfigRoot
    {
        public List<BreedConfig> breeds = new List<BreedConfig>();
    }

    [Serializable]
    public class RegionConfig
    {
        public string id;
        public string nameKey;
        public string descKey;
        public int unlockLevel;
        public List<string> availableBreeds = new List<string>();
        public List<string> availableItems = new List<string>();
        public string groundColor;
        public string skyTint;
        public string fogColor;
    }

    [Serializable]
    public class RegionsConfigRoot
    {
        public List<RegionConfig> regions = new List<RegionConfig>();
    }

    [Serializable]
    public class QuestConfig
    {
        public string id;
        public string regionId;
        public string titleKey;
        public string descKey;
        public string type; // "feed", "water", "care", "trust", "sell", "chain"
        public int targetValue;
        public int goldReward;
        public int xpReward;
        public string prerequisiteQuestId;
        public string unlockBreedId;
    }

    [Serializable]
    public class QuestsConfigRoot
    {
        public List<QuestConfig> quests = new List<QuestConfig>();
    }

    [Serializable]
    public class DangerEventConfig
    {
        public string id;
        public string type;
        public string nameKey;
        public string warningKey;
        public float intervalMinSec = 480f;
        public float intervalMaxSec = 900f;
        public float firstTriggerDelaySec = 75f;
        public float spawnMinDistance = 50f;
        public float spawnMaxDistance = 75f;
        public float moveSpeed = 2.0f;
        public float barnReachDistance = 5.0f;
        public int requiredScreams = 3;
        public int xpReward = 20;
        public int goldRewardMin = 10;
        public int goldRewardMax = 30;
        public float successTrustLoss = 5f;
        public float trustPenalty = 20f;
        public float panicDurationSec = 120f;
    }

    [Serializable]
    public class EventsConfigRoot
    {
        public List<DangerEventConfig> events = new List<DangerEventConfig>();
    }

    [Serializable]
    public class StringEntry
    {
        public string key;
        public string value;
    }

    [Serializable]
    public class StringsConfigRoot
    {
        public List<StringEntry> entries = new List<StringEntry>();
    }

    // --- Runtime Save Models (GDD Section 7) ---

    [Serializable]
    public class HorseGenes
    {
        public int speed = 10;
        public int stamina = 10;
        public int temperament = 5;
        public string coat = "Brown_01";
    }

    [Serializable]
    public class HorseData
    {
        public string id;
        public string name;
        public string breedId;
        public string sex; // "mare" | "stallion"
        public string stage = "adult";
        public HorseGenes genes = new HorseGenes();
        public int rarity = 1;
        public float hunger = 20f;
        public float thirst = 20f;
        public float trust = 30f;
        public bool isPanicking = false;
        public float panicRemainingSec = 0f;
        public float lastGroomTime = -999f;
    }

    [Serializable]
    public class ItemStack
    {
        public string itemId;
        public int count;
    }

    [Serializable]
    public class InventoryData
    {
        public int slots = 12;
        public List<ItemStack> items = new List<ItemStack>();
    }

    [Serializable]
    public class UpgradesData
    {
        public int barn = 0;
        public int storage = 0;
    }

    [Serializable]
    public class QuestProgressEntry
    {
        public string questId;
        public int progress;
    }

    [Serializable]
    public class PlayerState
    {
        public int saveVersion = 1;
        public int gold = 100;
        public int level = 1;
        public int xp = 0;
        public string currentRegionId = "region_starter_farm";
        public InventoryData inventory = new InventoryData();
        public int horseCapacity = 2;
        public List<HorseData> horses = new List<HorseData>();
        public List<string> unlockedBreedIds = new List<string>();
        public UpgradesData upgrades = new UpgradesData();
        public List<string> unlockedRegions = new List<string>();
        public List<QuestProgressEntry> questProgress = new List<QuestProgressEntry>();
        public List<string> activeQuests = new List<string>();
        public List<string> completedQuests = new List<string>();

        public int GetQuestProgress(string qId)
        {
            for (int i = 0; i < questProgress.Count; i++)
            {
                if (questProgress[i].questId == qId)
                    return questProgress[i].progress;
            }
            return 0;
        }

        public void SetQuestProgress(string qId, int value)
        {
            for (int i = 0; i < questProgress.Count; i++)
            {
                if (questProgress[i].questId == qId)
                {
                    questProgress[i].progress = value;
                    return;
                }
            }
            questProgress.Add(new QuestProgressEntry { questId = qId, progress = value });
        }
    }
}
