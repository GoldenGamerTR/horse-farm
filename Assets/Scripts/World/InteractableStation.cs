using UnityEngine;
using HorseFarm.Core;

namespace HorseFarm.World
{
    public class InteractableStation : MonoBehaviour
    {
        public StationType stationType = StationType.Merchant;
        public string promptKey = "prompt_merchant_interact";

        public string GetPromptText()
        {
            if (DataManager.Instance == null) return promptKey;
            return DataManager.Instance.Tr(promptKey);
        }

        public void Interact()
        {
            GameEvents.RaiseOpenStationMenu(stationType);
        }
    }
}
