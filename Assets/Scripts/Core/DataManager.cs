using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace HorseFarm.Core
{
    public class DataManager : MonoBehaviour
    {
        public static DataManager Instance { get; private set; }

        public BalanceConfigRoot Balance { get; private set; }
        public ItemsConfigRoot Items { get; private set; }
        public BreedsConfigRoot Breeds { get; private set; }
        public RegionsConfigRoot Regions { get; private set; }
        public QuestsConfigRoot Quests { get; private set; }
        public EventsConfigRoot Events { get; private set; }

        private readonly Dictionary<string, string> _strings = new Dictionary<string, string>();
        private readonly Dictionary<string, ItemConfig> _itemMap = new Dictionary<string, ItemConfig>();
        private readonly Dictionary<string, BreedConfig> _breedMap = new Dictionary<string, BreedConfig>();
        private readonly Dictionary<string, RegionConfig> _regionMap = new Dictionary<string, RegionConfig>();
        private readonly Dictionary<string, QuestConfig> _questMap = new Dictionary<string, QuestConfig>();
        private readonly Dictionary<int, RaritySettingConfig> _rarityMap = new Dictionary<int, RaritySettingConfig>();

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            LoadAllData();
        }

        public void LoadAllData()
        {
            Balance = LoadJson<BalanceConfigRoot>("balance.json") ?? new BalanceConfigRoot();
            Items = LoadJson<ItemsConfigRoot>("items.json") ?? new ItemsConfigRoot();
            Breeds = LoadJson<BreedsConfigRoot>("breeds.json") ?? new BreedsConfigRoot();
            Regions = LoadJson<RegionsConfigRoot>("regions.json") ?? new RegionsConfigRoot();
            Quests = LoadJson<QuestsConfigRoot>("quests.json") ?? new QuestsConfigRoot();
            Events = LoadJson<EventsConfigRoot>("events.json") ?? new EventsConfigRoot();
            StringsConfigRoot stringsRoot = LoadJson<StringsConfigRoot>("strings.tr.json") ?? new StringsConfigRoot();

            _strings.Clear();
            foreach (var entry in stringsRoot.entries)
            {
                if (!string.IsNullOrEmpty(entry.key))
                    _strings[entry.key] = entry.value;
            }

            _itemMap.Clear();
            foreach (var item in Items.items)
                _itemMap[item.id] = item;

            _breedMap.Clear();
            foreach (var breed in Breeds.breeds)
                _breedMap[breed.id] = breed;

            _regionMap.Clear();
            foreach (var reg in Regions.regions)
                _regionMap[reg.id] = reg;

            _questMap.Clear();
            foreach (var q in Quests.quests)
                _questMap[q.id] = q;

            _rarityMap.Clear();
            foreach (var r in Balance.raritySettings)
                _rarityMap[r.rarity] = r;
        }

        private T LoadJson<T>(string fileName) where T : class
        {
            try
            {
                string externalPath = Path.Combine(Application.dataPath, "..", "data", fileName);
                if (File.Exists(externalPath))
                {
                    string json = File.ReadAllText(externalPath);
                    return JsonUtility.FromJson<T>(json);
                }

                string resourceName = "Data/" + Path.GetFileNameWithoutExtension(fileName);
                TextAsset asset = Resources.Load<TextAsset>(resourceName);
                if (asset != null)
                {
                    return JsonUtility.FromJson<T>(asset.text);
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[DataManager] Error loading {fileName}: {ex.Message}");
            }
            return null;
        }

        public string Tr(string key, params object[] args)
        {
            if (string.IsNullOrEmpty(key)) return string.Empty;
            if (!_strings.TryGetValue(key, out string val))
                val = key;

            if (args != null && args.Length > 0)
            {
                try
                {
                    return string.Format(val, args);
                }
                catch
                {
                    return val;
                }
            }
            return val;
        }

        public ItemConfig GetItem(string id)
        {
            if (string.IsNullOrEmpty(id)) return null;
            _itemMap.TryGetValue(id, out var cfg);
            return cfg;
        }

        public BreedConfig GetBreed(string id)
        {
            if (string.IsNullOrEmpty(id)) return null;
            _breedMap.TryGetValue(id, out var cfg);
            return cfg;
        }

        public RegionConfig GetRegion(string id)
        {
            if (string.IsNullOrEmpty(id)) return null;
            _regionMap.TryGetValue(id, out var cfg);
            return cfg;
        }

        public QuestConfig GetQuest(string id)
        {
            if (string.IsNullOrEmpty(id)) return null;
            _questMap.TryGetValue(id, out var cfg);
            return cfg;
        }

        public RaritySettingConfig GetRaritySetting(int rarity)
        {
            if (_rarityMap.TryGetValue(rarity, out var cfg))
                return cfg;
            return new RaritySettingConfig
            {
                rarity = 1,
                nameKey = "rarity_1",
                baseSellValue = 100,
                merchantPrice = 150,
                trustMultiplier = 1f,
                panicThreshold = 10f
            };
        }

        public string GetPlayerTitle(int level)
        {
            foreach (var t in Balance.titles)
            {
                if (level >= t.minLevel && level <= t.maxLevel)
                    return Tr(t.titleKey);
            }
            return Tr("title_groom");
        }
    }
}
