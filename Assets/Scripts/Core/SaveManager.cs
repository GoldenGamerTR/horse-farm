using System;
using System.IO;
using UnityEngine;

namespace HorseFarm.Core
{
    public static class SaveManager
    {
        private const string FileName = "horse_farm_save_v1.json";

        public static string GetSaveFilePath()
        {
            return Path.Combine(Application.persistentDataPath, FileName);
        }

        public static void Save(PlayerState state)
        {
            if (state == null) return;
            try
            {
                state.saveVersion = 1;
                string json = JsonUtility.ToJson(state, true);
                File.WriteAllText(GetSaveFilePath(), json);
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[SaveManager] Failed to save game: {ex.Message}");
            }
        }

        public static PlayerState Load()
        {
            try
            {
                string path = GetSaveFilePath();
                if (File.Exists(path))
                {
                    string json = File.ReadAllText(path);
                    PlayerState state = JsonUtility.FromJson<PlayerState>(json);
                    if (state != null && state.saveVersion >= 1 && state.horses != null && state.horses.Count > 0)
                    {
                        return state;
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[SaveManager] Failed to load save file: {ex.Message}");
            }
            return null;
        }

        public static void DeleteSave()
        {
            try
            {
                string path = GetSaveFilePath();
                if (File.Exists(path))
                    File.Delete(path);
            }
            catch { }
        }
    }
}
