using UnityEngine;

public class Water : MonoBehaviour
{
    public MeshFilter WaterMeshFilter;
    public MeshSettings WaterMeshSetting;

    public float FresnelPower;
    public float ColorDepthFactor;
    public float MinAlpha;
    public float ShorelineFadeStrength;
    public float Smoothness;

    public Transform Sun;
    private Vector3 DirToSun => -Sun.forward;

    public Color DeepColor;
    public Color ShallowColor;

    public Texture2D WaveNormalA;
    public Texture2D WaveNormalB;
    
    private Material WaterMaterial;
    
    private void Start()
    {
        WaterMaterial = GetComponent<MeshRenderer>().sharedMaterial;
        
        var flatMap = new float[WaterMeshSetting.numVertsPerLine, WaterMeshSetting.numVertsPerLine];
        var meshData = MeshGenerator.GenerateTerrainMesh(flatMap, WaterMeshSetting, 0);
        WaterMeshFilter.sharedMesh = meshData.CreateMesh();
    }

    private void Update()
    {
        WaterMaterial.SetFloat("FresnelPower", FresnelPower);
        WaterMaterial.SetFloat("ColorDepthFactor", ColorDepthFactor);
        WaterMaterial.SetFloat("MinAlpha", MinAlpha);
        WaterMaterial.SetFloat("Smoothness", Smoothness);
        WaterMaterial.SetFloat("ShorelineFadeStrength", ShorelineFadeStrength);
        
        WaterMaterial.SetVector("DirToSun", DirToSun);
        WaterMaterial.SetVector("DeepColor", DeepColor);
        WaterMaterial.SetVector("ShallowColor", ShallowColor);
        
        WaterMaterial.SetTexture("WaveNormalA", WaveNormalA);
        WaterMaterial.SetTexture("WaveNormalB", WaveNormalB);
    }
}
