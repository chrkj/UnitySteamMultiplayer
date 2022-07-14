namespace SceneLoading
{
    public class ClientGameLoadingScreen : ClientLoadingScreen
    {
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
            return "PlayerX";
        }
    
    }
}