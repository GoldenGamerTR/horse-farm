using System;

namespace HorseFarm.Core
{
    public enum StationType
    {
        None,
        Merchant,
        Market,
        BarnUpgrade,
        RegionGate,
        QuestBoard
    }

    public static class GameEvents
    {
        public static event Action<int> OnGoldChanged;
        public static event Action<int, int, int> OnXpOrLevelChanged; // level, xp, nextLevelXp
        public static event Action OnInventoryChanged;
        public static event Action OnHorsesChanged;
        public static event Action OnQuestsChanged;
        public static event Action<string> OnRegionChanged;
        public static event Action<string> OnNotification;
        public static event Action<bool, string, float, int, int> OnDangerEventUpdate; // active, eventName, distance, screams, reqScreams
        public static event Action OnPlayerScream;
        public static event Action<StationType> OnOpenStationMenu;
        public static event Action<HorseData> OnInspectHorse;

        public static void RaiseGoldChanged(int gold) => OnGoldChanged?.Invoke(gold);
        public static void RaiseXpOrLevelChanged(int level, int xp, int nextXp) => OnXpOrLevelChanged?.Invoke(level, xp, nextXp);
        public static void RaiseInventoryChanged() => OnInventoryChanged?.Invoke();
        public static void RaiseHorsesChanged() => OnHorsesChanged?.Invoke();
        public static void RaiseQuestsChanged() => OnQuestsChanged?.Invoke();
        public static void RaiseRegionChanged(string regionId) => OnRegionChanged?.Invoke(regionId);
        public static void RaiseNotification(string msg) => OnNotification?.Invoke(msg);
        public static void RaiseDangerEventUpdate(bool active, string name, float dist, int screams, int reqScreams)
            => OnDangerEventUpdate?.Invoke(active, name, dist, screams, reqScreams);
        public static void RaisePlayerScream() => OnPlayerScream?.Invoke();
        public static void RaiseOpenStationMenu(StationType type) => OnOpenStationMenu?.Invoke(type);
        public static void RaiseInspectHorse(HorseData horse) => OnInspectHorse?.Invoke(horse);
    }
}
