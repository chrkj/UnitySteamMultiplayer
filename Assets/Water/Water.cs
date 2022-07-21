using UnityEngine;

public class Water : MonoBehaviour
{
    public MeshFilter WaterMeshFilter;
    public MeshSettings WaterMeshSetting;

    public float FresnelPower;
    public float ColorDepthFactor;
    public float MinAlpha;
    public float ShorelineFadeStrength;

    public Transform Sun;
    private Vector3 DirToSun => -Sun.forward;

    public Color DeepColor;
    public Color ShallowColor;
    
    private Material WaterMaterial;
    
    private void Start()
    {
        WaterMaterial = GetComponent<MeshRenderer>().material;
        
        var flatMap = new float[WaterMeshSetting.numVertsPerLine, WaterMeshSetting.numVertsPerLine];
        var meshData = MeshGenerator.GenerateTerrainMesh(flatMap, WaterMeshSetting, 0);
        WaterMeshFilter.sharedMesh = meshData.CreateMesh();
    }

    private void Update()
    {
        WaterMaterial.SetFloat("FresnelPower", FresnelPower);
        WaterMaterial.SetFloat("ColorDepthFactor", ColorDepthFactor);
        WaterMaterial.SetFloat("MinAlpha", MinAlpha);
        WaterMaterial.SetFloat("ShorelineFadeStrength", ShorelineFadeStrength);
        WaterMaterial.SetVector("DirToSun", DirToSun);
        WaterMaterial.SetVector("DeepColor", DeepColor);
        WaterMaterial.SetVector("ShallowColor", ShallowColor);
    }
}
