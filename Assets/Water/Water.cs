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
    private static readonly int Power = Shader.PropertyToID("FresnelPower");
    private static readonly int DepthFactor = Shader.PropertyToID("ColorDepthFactor");
    private static readonly int Alpha = Shader.PropertyToID("MinAlpha");
    private static readonly int Smoothness1 = Shader.PropertyToID("Smoothness");
    private static readonly int FadeStrength = Shader.PropertyToID("ShorelineFadeStrength");
    private static readonly int ToSun = Shader.PropertyToID("DirToSun");
    private static readonly int DeepColor1 = Shader.PropertyToID("DeepColor");
    private static readonly int ShallowColor1 = Shader.PropertyToID("ShallowColor");
    private static readonly int NormalA = Shader.PropertyToID("WaveNormalA");
    private static readonly int NormalB = Shader.PropertyToID("WaveNormalB");

    private void Start()
    {
        WaterMaterial = GetComponent<MeshRenderer>().sharedMaterial;
        
        var waterMap = new float[WaterMeshSetting.numVertsPerLine, WaterMeshSetting.numVertsPerLine];
        var meshData = MeshGenerator.GenerateTerrainMesh(waterMap, WaterMeshSetting, 0);
        WaterMeshFilter.sharedMesh = meshData.CreateMesh();
    }

    private void Update()
    {
        WaterMaterial.SetFloat(Power, FresnelPower);
        WaterMaterial.SetFloat(DepthFactor, ColorDepthFactor);
        WaterMaterial.SetFloat(Alpha, MinAlpha);
        WaterMaterial.SetFloat(Smoothness1, Smoothness);
        WaterMaterial.SetFloat(FadeStrength, ShorelineFadeStrength);
        
        WaterMaterial.SetVector(ToSun, DirToSun);
        WaterMaterial.SetVector(DeepColor1, DeepColor);
        WaterMaterial.SetVector(ShallowColor1, ShallowColor);
        
        WaterMaterial.SetTexture(NormalA, WaveNormalA);
        WaterMaterial.SetTexture(NormalB, WaveNormalB);
    }
}
