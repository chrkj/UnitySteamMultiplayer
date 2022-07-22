using UnityEngine;

public class TerrainGenerator: MonoBehaviour
{
    public bool AutoUpdate;
    public Renderer NoiseMap;
    
    public int Depth = 20;
    public int Width = 256;
    public int Height = 256;
    public HeightMapSettings Settings;

    private Terrain m_Terrain;
    private float[,] m_HeightMap;

    private void Start()
    {
        m_Terrain = GetComponentInChildren<Terrain>();
        m_Terrain.terrainData = GenerateTerrain(m_Terrain.terrainData);
    }

    private TerrainData GenerateTerrain(TerrainData terrainData)
    {
        terrainData.heightmapResolution = Width + 1;
        terrainData.size = new Vector3(Width, Depth, Height);
        m_HeightMap = GenerateHeightMap();
        terrainData.SetHeights(0, 0, m_HeightMap);
        return terrainData;
    }

    private float[,] GenerateHeightMap()
    {
        return HeightMapGenerator.GenerateHeightMap(Width, Height, Settings, Vector2.zero).Values;
    }

    public void DrawNoiseMap()
    {
        m_HeightMap = GenerateHeightMap();
        NoiseMap.sharedMaterial.mainTexture = TextureGenerator.TextureFromHeightMap(m_HeightMap);;
    }
}