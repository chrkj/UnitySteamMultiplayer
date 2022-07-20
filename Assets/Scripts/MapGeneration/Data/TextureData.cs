using UnityEngine;
using System.Linq;

[CreateAssetMenu()]
public class TextureData : UpdatableData 
{
	public Layer[] Layers;
	
	private float m_SavedMinHeight;
	private float m_SavedMaxHeight;
	
	private const int m_TextureSize = 512;
	private const TextureFormat m_TextureFormat = TextureFormat.RGB565;

	public void ApplyToMaterial(Material material) 
	{
		material.SetInt("layerCount", Layers.Length);
		material.SetColorArray("baseColours", Layers.Select(x => x.Tint).ToArray());
		material.SetFloatArray("baseStartHeights", Layers.Select(x => x.StartHeight).ToArray());
		material.SetFloatArray("baseBlends", Layers.Select(x => x.BlendStrength).ToArray());
		material.SetFloatArray("baseColourStrength", Layers.Select(x => x.TintStrength).ToArray());
		material.SetFloatArray("baseTextureScales", Layers.Select(x => x.TextureScale).ToArray());
		var texturesArray = GenerateTextureArray(Layers.Select(x => x.Texture).ToArray());
		material.SetTexture("baseTextures", texturesArray);

		UpdateMeshHeights(material, m_SavedMinHeight, m_SavedMaxHeight);
	}

	public void UpdateMeshHeights(Material material, float minHeight, float maxHeight) 
	{
		m_SavedMinHeight = minHeight;
		m_SavedMaxHeight = maxHeight;

		material.SetFloat("minHeight", minHeight);
		material.SetFloat("maxHeight", maxHeight);
	}

	private Texture2DArray GenerateTextureArray(Texture2D[] textures) 
	{
		var textureArray = new Texture2DArray(m_TextureSize, m_TextureSize, textures.Length, m_TextureFormat, true);
		for (int i = 0; i < textures.Length; i++)
			textureArray.SetPixels(textures[i].GetPixels(), i);
		
		textureArray.Apply();
		return textureArray;
	}

	[System.Serializable]
	public class Layer 
	{
		[Range(0, 1)]
		public float TintStrength;
		[Range(0, 1)]
		public float StartHeight;
		[Range(0, 1)]
		public float BlendStrength;
		
		public Color Tint;
		public Texture2D Texture;
		public float TextureScale;
	}
}
