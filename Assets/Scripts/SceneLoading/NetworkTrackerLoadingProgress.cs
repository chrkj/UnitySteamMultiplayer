using Unity.Netcode;

public class NetworkTrackerLoadingProgress : NetworkBehaviour
{
    public NetworkVariable<float> Progress { get; } = new NetworkVariable<float>(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
}