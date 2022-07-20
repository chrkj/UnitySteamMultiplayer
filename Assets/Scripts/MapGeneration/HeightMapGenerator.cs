using UnityEngine;

public static class HeightMapGenerator 
{
	public static HeightMap GenerateHeightMap(int width, int height, HeightMapSettings settings, Vector2 sampleCentre) 
	{
		float[,] values = Noise.GenerateNoiseMap(width, height, settings.NoiseSettings, sampleCentre);
		AnimationCurve heightCurve = new AnimationCurve(settings.HeightCurve.keys);
		var falloffMap = FalloffGenerator.GenerateFalloffMap(width, height);
		
		float minValue = float.MaxValue;
		float maxValue = float.MinValue;

		for (int i = 0; i < width; i++) 
		{
			for (int j = 0; j < height; j++) 
			{
				if (settings.UseFalloff)
					values[i, j] = Mathf.Clamp01(values[i, j] - falloffMap[i, j]);
				
				values[i, j] *= heightCurve.Evaluate(values[i, j]) * settings.HeightMultiplier;
				if (values[i, j] > maxValue) 
					maxValue = values[i, j];
				if (values[i, j] < minValue) 
					minValue = values[i, j];
			}
		}
		return new HeightMap(values);
	}
}

public readonly struct HeightMap 
{
	public readonly float[,] Values;
	public HeightMap(float[,] values) { Values = values; }
}

