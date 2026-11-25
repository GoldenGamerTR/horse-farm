using System.Collections.Generic;
using UnityEngine;
using HorseFarm.Core;

namespace HorseFarm.Economy
{
    public static class QuestSystem
    {
        public static void EnsureStarterQuestsActive(PlayerState state, DataManager data)
        {
            if (state == null || data?.Quests?.quests == null) return;
            int maxActive = data.Balance?.xp != null ? data.Balance.xp.maxActiveQuests : 3;

            foreach (var q in data.Quests.quests)
            {
                if (state.activeQuests.Count >= maxActive)
                    break;

                if (state.activeQuests.Contains(q.id) || state.completedQuests.Contains(q.id))
                    continue;

                if (!state.unlockedRegions.Contains(q.regionId))
                    continue;

                bool prereqOk = string.IsNullOrEmpty(q.prerequisiteQuestId) || state.completedQuests.Contains(q.prerequisiteQuestId);
                if (prereqOk)
                {
                    state.activeQuests.Add(q.id);
                }
            }
        }

        public static bool AcceptQuest(PlayerState state, DataManager data, string questId)
        {
            if (state == null || data == null) return false;
            int maxActive = data.Balance?.xp != null ? data.Balance.xp.maxActiveQuests : 3;
            if (state.activeQuests.Count >= maxActive)
            {
                GameEvents.RaiseNotification(data.Tr("notify_quest_limit"));
                return false;
            }

            var q = data.GetQuest(questId);
            if (q == null || state.activeQuests.Contains(questId) || state.completedQuests.Contains(questId))
                return false;

            state.activeQuests.Add(questId);
            GameEvents.RaiseNotification(data.Tr("notify_quest_accepted", data.Tr(q.titleKey)));
            GameEvents.RaiseQuestsChanged();
            return true;
        }

        public static void RecordAction(PlayerState state, DataManager data, string actionType, int amount = 1)
        {
            if (state == null || data == null) return;

            float maxTrust = 0f;
            for (int i = 0; i < state.horses.Count; i++)
            {
                if (state.horses[i].trust > maxTrust)
                    maxTrust = state.horses[i].trust;
            }

            List<string> completedNow = new List<string>();

            for (int i = 0; i < state.activeQuests.Count; i++)
            {
                string qId = state.activeQuests[i];
                var q = data.GetQuest(qId);
                if (q == null) continue;

                int current = state.GetQuestProgress(qId);
                if (q.type == "trust")
                {
                    current = Mathf.Max(current, Mathf.RoundToInt(maxTrust));
                }
                else if (q.type == actionType)
                {
                    current += amount;
                }
                else if (q.type == "chain" && (actionType == "feed" || actionType == "water" || actionType == "care"))
                {
                    current += amount;
                }

                state.SetQuestProgress(qId, current);
                if (current >= q.targetValue)
                {
                    completedNow.Add(qId);
                }
            }

            if (completedNow.Count > 0 && GameManager.Instance != null)
            {
                foreach (string doneId in completedNow)
                {
                    GameManager.Instance.CompleteQuest(doneId);
                }
            }

            GameEvents.RaiseQuestsChanged();
        }
    }
}
