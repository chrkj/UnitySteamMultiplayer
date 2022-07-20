using UnityEngine;

public static class Noise 
{
	public enum NormalizeMode { Local, Global };

	public static float[,] GenerateNoiseMap(int mapWidth, int mapHeight, NoiseSettings settings, Vector2 sampleCentre) 
	{
		float[,] noiseMap = new float[mapWidth,mapHeight];

		System.Random prng = new System.Random(settings.Seed);
		Vector2[] octaveOffsets = new Vector2[settings.Octaves];

		float amplitude = 1;
		float frequency = 1;
		float maxPossibleHeight = 0;

		for (int i = 0; i < settings.Octaves; i++) 
		{
			float offsetX = prng.Next(-100000, 100000) + settings.Offset.x + sampleCentre.x;
			float offsetY = prng.Next(-100000, 100000) - settings.Offset.y - sampleCentre.y;
			octaveOffsets [i] = new Vector2(offsetX, offsetY);

			maxPossibleHeight += amplitude;
			amplitude *= settings.Persistance;
		}

		float maxLocalNoiseHeight = float.MinValue;
		float minLocalNoiseHeight = float.MaxValue;

		float halfWidth = mapWidth / 2f;
		float halfHeight = mapHeight / 2f;
		
		for (int y = 0; y < mapHeight; y++) 
		{
			for (int x = 0; x < mapWidth; x++) 
			{
				amplitude = 1;
				frequency = 1;
				float noiseHeight = 0;

				for (int i = 0; i < settings.Octaves; i++) 
				{
					float sampleX = (x - halfWidth + octaveOffsets[i].x) / settings.Scale * frequency;
					float sampleY = (y - halfHeight + octaveOffsets[i].y) / settings.Scale * frequency;

					float perlinValue = Mathf.PerlinNoise(sampleX, sampleY) * 2 - 1;
					noiseHeight += perlinValue * amplitude;

					amplitude *= settings.Persistance;
					frequency *= settings.Lacunarity;
				}

				if (noiseHeight > maxLocalNoiseHeight)
					maxLocalNoiseHeight = noiseHeight;
				if (noiseHeight < minLocalNoiseHeight)
					minLocalNoiseHeight = noiseHeight;
				
				noiseMap[x, y] = noiseHeight;

				if (settings.NormalizeMode == NormalizeMode.Global) 
				{
					float normalizedHeight = (noiseMap[x, y] + 1) / (maxPossibleHeight / 0.9f);
					noiseMap[x, y] = Mathf.Clamp(normalizedHeight, 0, int.MaxValue);
				}
			}
		}

		if (settings.NormalizeMode != NormalizeMode.Local)
			return noiseMap;
		
		for (int y = 0; y < mapHeight; y++)
		{
			for (int x = 0; x < mapWidth; x++)
				noiseMap[x, y] = Mathf.InverseLerp(minLocalNoiseHeight, maxLocalNoiseHeight, noiseMap[x, y]);
		}
		return noiseMap;
	}
}

[System.Serializable]
public class NoiseSettings 
{
	public int Octaves = 6;
	public float Scale = 50;
	public Noise.NormalizeMode NormalizeMode;
	
	[Range(0,1)]
	public float Persistance =.6f;
	
	public int Seed;
	public Vector2 Offset;
	public float Lacunarity = 2;

	public void ValidateValues() 
	{
		Scale = Mathf.Max(Scale, 0.01f);
		Octaves = Mathf.Max(Octaves, 1);
		Lacunarity = Mathf.Max(Lacunarity, 1);
		Persistance = Mathf.Clamp01(Persistance);
	}
}