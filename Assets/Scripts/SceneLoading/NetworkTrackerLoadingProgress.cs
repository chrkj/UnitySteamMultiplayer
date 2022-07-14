using Unity.Netcode;

namespace SceneLoading
{
    public class NetworkTrackerLoadingProgress : NetworkBehaviour
    {
        public NetworkVariable<float> Progress = new NetworkVariable<float>(0, readPerm: NetworkVariableReadPermission.Everyone, writePerm: NetworkVariableWritePermission.Owner);
    }
}