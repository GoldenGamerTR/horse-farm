using System;
using UnityEngine;
using HorseFarm.Core;

namespace HorseFarm.Economy
{
    public static class EconomySystem
    {
        private static readonly string[] TurkishHorseNames =
        {
            "Rüzgar", "Yıldız", "Şimşek", "Doruk", "Poyraz", "Fırtına",
            "Gölge", "Karayel", "Alaca", "Bozkır", "Güneş", "Ceylan",
            "Duman", "Aybüke", "Kırat", "Boran", "İnci", "Şahbatur"
        };

        // GDD 5.8 Price Formula:
        // fiyat = yuvarla( temelDeğer[nadirlik] * (1 + (hız + dayanıklılık) / 40) * (0.8 + 0.4 * güven / 100) )
        public static int CalculateHorseSellPrice(int baseRarityValue, int speed, int stamina, float trust)
        {
            float statFactor = 1f + (speed + stamina) / 40f;
            float trustFactor = 0.8f + 0.4f * (Mathf.Clamp(trust, 0f, 100f) / 100f);
            return Mathf.RoundToInt(baseRarityValue * statFactor * trustFactor);
        }

        public static int CalculateHorseSellPrice(HorseData horse, DataManager data)
        {
            if (horse == null) return 0;
            int baseVal = 100;
            if (data != null)
            {
                var breed = data.GetBreed(horse.breedId);
                if (breed != null && breed.baseSellValue > 0)
                    baseVal = breed.baseSellValue;
                else
                    baseVal = data.GetRaritySetting(horse.rarity).baseSellValue;
            }
            return CalculateHorseSellPrice(baseVal, horse.genes.speed, horse.genes.stamina, horse.trust);
        }

        // GDD 5.2 XP Formula:
        // Seviye n'den n+1'e geçmek için gereken XP: yuvarla(100 * n^1.5)
        public static int GetRequiredXpForLevel(int level)
        {
            int n = Mathf.Max(1, level);
            return Mathf.RoundToInt(100f * Mathf.Pow(n, 1.5f));
        }

        public static bool IsBreedUnlocked(PlayerState state, BreedConfig breed)
        {
            if (state == null || breed == null) return false;
            if (state.unlockedBreedIds.Contains(breed.id)) return true;

            bool levelMet = state.level >= breed.requiredLevel;
            bool questMet = !string.IsNullOrEmpty(breed.requiredQuestId) && state.completedQuests.Contains(breed.requiredQuestId);

            // GDD 5.7: "Seviye 5 veya ilgili görev serisi" for Rarity 2, "Seviye 10 + görev" for higher rarities
            if (breed.rarity <= 2)
                return levelMet || questMet;

            if (string.IsNullOrEmpty(breed.requiredQuestId))
                return levelMet;

            return levelMet && questMet;
        }

        public static HorseData CreateNewHorse(BreedConfig breed, string forcedName = null, string forcedSex = null, float startingTrust = 20f)
        {
            string sex = forcedSex;
            if (string.IsNullOrEmpty(sex))
                sex = UnityEngine.Random.value > 0.5f ? "mare" : "stallion";

            string hName = forcedName;
            if (string.IsNullOrEmpty(hName))
                hName = TurkishHorseNames[UnityEngine.Random.Range(0, TurkishHorseNames.Length)];

            int speed = breed != null ? UnityEngine.Random.Range(breed.minSpeed, breed.maxSpeed + 1) : 10;
            int stamina = breed != null ? UnityEngine.Random.Range(breed.minStamina, breed.maxStamina + 1) : 10;

            return new HorseData
            {
                id = "horse_" + Guid.NewGuid().ToString("N").Substring(0, 8),
                name = hName,
                breedId = breed != null ? breed.id : "breed_farm_horse",
                sex = sex,
                stage = "adult",
                rarity = breed != null ? breed.rarity : 1,
                hunger = 15f,
                thirst = 15f,
                trust = startingTrust,
                genes = new HorseGenes
                {
                    speed = speed,
                    stamina = stamina,
                    temperament = UnityEngine.Random.Range(3, 9),
                    coat = breed != null && !string.IsNullOrEmpty(breed.coatPrefab) ? breed.coatPrefab : "Brown_01"
                }
            };
        }
    }
}
