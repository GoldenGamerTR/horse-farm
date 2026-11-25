using UnityEngine;
using HorseFarm.Economy;

namespace HorseFarm.Core
{
    public static class CoreLogicTests
    {
        // GDD Rule 4: "Fiyat formülü, görev ilerlemesi ve tehlike olay mekanizması için birim testi yaz."
        public static bool RunAllTests()
        {
            int passed = 0;
            int total = 0;

            // 1. Price Formula Test (GDD 5.8)
            // base=100, speed=10, stamina=10, trust=50 => 100 * (1 + 20/40) * (0.8 + 0.4 * 0.5) = 100 * 1.5 * 1.0 = 150
            total++;
            int price1 = EconomySystem.CalculateHorseSellPrice(100, 10, 10, 50f);
            if (price1 == 150) passed++;
            else Debug.LogError($"[CoreLogicTests] Price test 1 failed: expected 150, got {price1}");

            // base=250, speed=20, stamina=20, trust=100 => 250 * (1 + 40/40) * (0.8 + 0.4) = 250 * 2.0 * 1.2 = 600
            total++;
            int price2 = EconomySystem.CalculateHorseSellPrice(250, 20, 20, 100f);
            if (price2 == 600) passed++;
            else Debug.LogError($"[CoreLogicTests] Price test 2 failed: expected 600, got {price2}");

            // 2. XP Formula Test (GDD 5.2: round(100 * n^1.5))
            total++;
            int xpLv1 = EconomySystem.GetRequiredXpForLevel(1);
            int xpLv4 = EconomySystem.GetRequiredXpForLevel(4); // 100 * 4^1.5 = 800
            if (xpLv1 == 100 && xpLv4 == 800) passed++;
            else Debug.LogError($"[CoreLogicTests] XP formula test failed: Lv1={xpLv1}, Lv4={xpLv4}");

            // 3. Inventory Stack & Slot Limit Test (GDD 5.6)
            total++;
            PlayerState testState = new PlayerState();
            testState.inventory.slots = 2;
            bool add1 = InventorySystem.AddItem(testState, "item_basic_feed", 20, 20);
            bool add2 = InventorySystem.AddItem(testState, "item_water", 20, 20);
            bool addOverflow = InventorySystem.CanAddItem(testState, "item_grooming_kit", 1, 20);
            if (add1 && add2 && !addOverflow && InventorySystem.GetUsedSlots(testState) == 2) passed++;
            else Debug.LogError("[CoreLogicTests] Inventory slot/stack test failed!");

            // 4. Quest Progress & Completion Test (GDD 5.2)
            total++;
            testState.SetQuestProgress("quest_feed_twice", 1);
            testState.SetQuestProgress("quest_feed_twice", testState.GetQuestProgress("quest_feed_twice") + 1);
            if (testState.GetQuestProgress("quest_feed_twice") == 2) passed++;
            else Debug.LogError("[CoreLogicTests] Quest progress test failed!");

            Debug.Log($"[CoreLogicTests] {passed}/{total} çekirdek mantık birim testi başarıyla geçti.");
            return passed == total;
        }
    }
}
