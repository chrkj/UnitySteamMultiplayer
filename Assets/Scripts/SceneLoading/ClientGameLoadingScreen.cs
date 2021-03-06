using UnityEngine;

namespace SceneLoading
{
    public class ClientGameLoadingScreen : ClientLoadingScreen
    {
        [SerializeField]
        private PersistentPlayerRuntimeCollection m_PersistentPlayerRuntimeCollection;
        protected override void AddOtherPlayerProgressBar(ulong clientId, NetworkTrackerLoadingProgress progressTracker)
        {
            base.AddOtherPlayerProgressBar(clientId, progressTracker);
            m_LoadingProgressBars[clientId].NameText.text = GetPlayerName(clientId);
        }

        protected override void UpdateOtherPlayerProgressBar(ulong clientId, int progressBarIndex)
        {
            base.UpdateOtherPlayerProgressBar(clientId, progressBarIndex);
            m_LoadingProgressBars[clientId].NameText.text = GetPlayerName(clientId);
        }

        private string GetPlayerName(ulong clientId)
        {
            foreach (var player in m_PersistentPlayerRuntimeCollection.Items)
            {
                if (clientId == player.OwnerClientId)
                {
                    return player.NetworkNameState.Name.Value;
                }
            }
            return "Unknown";
        }
    
    }
}