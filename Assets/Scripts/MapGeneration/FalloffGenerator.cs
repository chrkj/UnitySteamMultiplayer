using UnityEngine;

public static class FalloffGenerator 
{
	public static float[,] GenerateFalloffMap(int sizeX, int sizeY) 
	{
		var map = new float[sizeX, sizeY];
		for (int i = 0; i < sizeX; i++) 
		{
			for (int j = 0; j < sizeY; j++) 
			{
				float x = i / (float)sizeX * 2 - 1;
				float y = j / (float)sizeY * 2 - 1;

				float value = Mathf.Max(Mathf.Abs(x), Mathf.Abs(y));
				map[i, j] = Evaluate(value);
			}
		}
		return map;
	}

	private static float Evaluate(float value) 
	{
		float a = 3;
		float b = 2.2f;
		return Mathf.Pow(value, a) / (Mathf.Pow(value, a) + Mathf.Pow(b - b * value, a));
	}
}
