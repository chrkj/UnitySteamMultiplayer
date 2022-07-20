using System.Threading.Tasks;
using UnityEngine;

public class MapPreview : MonoBehaviour 
{
	public bool AutoUpdate;
	public MeshFilter MeshFilter;
	public MeshCollider MeshCollider;
	public TextureData TextureData;
	public MeshSettings MeshSettings;
	public HeightMapSettings HeightMapSettings;
	public Material TerrainMaterial;

	private void Awake()
	{
		DrawMap();
	}

	private void Start()
	{
		WaitToApplyMaterial();
	}

	private async void WaitToApplyMaterial()
	{
		await Task.Delay(1000);
		TextureData.ApplyToMaterial(TerrainMaterial);
	}
	
	public void DrawMap() 
	{
		TextureData.UpdateMeshHeights(TerrainMaterial, HeightMapSettings.MinHeight, HeightMapSettings.MaxHeight);
		var heightMap = HeightMapGenerator.GenerateHeightMap(MeshSettings.numVertsPerLine, MeshSettings.numVertsPerLine, HeightMapSettings, Vector2.zero);

		DrawMesh(MeshGenerator.GenerateTerrainMesh(heightMap.Values, MeshSettings, 0));
	}

	private void DrawMesh(MeshData meshData)
	{
		var mesh = meshData.CreateMesh();
		MeshFilter.sharedMesh = mesh;
		MeshCollider.sharedMesh = mesh;
		
		MeshFilter.gameObject.SetActive(true);
	}

	private void OnValuesUpdated() 
	{
		if (!Application.isPlaying)
			DrawMap();
	}

	private void OnTextureValuesUpdated() 
	{
		TextureData.ApplyToMaterial(TerrainMaterial);
	}

	private void OnValidate() 
	{
		if (MeshSettings != null) 
		{
			MeshSettings.OnValuesUpdated -= OnValuesUpdated;
			MeshSettings.OnValuesUpdated += OnValuesUpdated;
		}
		if (HeightMapSettings != null) 
		{
			HeightMapSettings.OnValuesUpdated -= OnValuesUpdated;
			HeightMapSettings.OnValuesUpdated += OnValuesUpdated;
		}
		if (TextureData != null) 
		{
			TextureData.OnValuesUpdated -= OnTextureValuesUpdated;
			TextureData.OnValuesUpdated += OnTextureValuesUpdated;
		}
	}
}
