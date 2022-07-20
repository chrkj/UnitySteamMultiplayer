using StarterAssets;
using Unity.Netcode;
using UnityEngine.InputSystem;

public class ControllerManager : NetworkBehaviour
{
    public override void OnNetworkSpawn()
    {
        if (OwnerClientId == NetworkManager.Singleton.LocalClientId) return;
        
        Destroy(transform.Find("MainCamera").gameObject);
        Destroy(transform.Find("PlayerFollowCamera").gameObject);
        transform.Find("PlayerFollowCamera").gameObject.GetComponent<PlayerInput>();
        transform.Find("PlayerFollowCamera").gameObject.GetComponent<StarterAssetsInputs>();
    }
}
