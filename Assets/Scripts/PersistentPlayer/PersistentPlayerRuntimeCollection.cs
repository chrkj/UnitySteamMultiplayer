using UnityEngine;

[CreateAssetMenu]
public class PersistentPlayerRuntimeCollection : RuntimeCollection<PersistentPlayer>
{
    public bool TryGetPlayer(ulong clientID, out PersistentPlayer persistentPlayer)
    {
        foreach (var player in Items)
        {
            if (clientID != player.OwnerClientId) continue;
            
            persistentPlayer = player;
            return true;
        }
        persistentPlayer = null;
        return false;
    }
}