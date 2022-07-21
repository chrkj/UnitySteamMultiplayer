using UnityEngine;

public class DepthCam : MonoBehaviour
{
    private void Start()
    {
        Camera cam = GetComponent<Camera>();
        cam.depthTextureMode |= DepthTextureMode.Depth;
    }
}
