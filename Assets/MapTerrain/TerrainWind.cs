using UnityEngine;
using System.Collections.Generic;

namespace MapTerrain
{
    public static class TerrainWind
    {
        static int erosionRadius = 2;

        public static float[,] Erode(ComputeShader erosion, int numIterations, float[,] heightmap,
            int heightmapResolution, int erosionResolution, Vector2 windDirection)
        {
            float
                inertia = .05f; // At zero, water will instantly change direction to flow downhill. At 1, water will never change direction. 
            float sedimentCapacityFactor = 4; // Multiplier for how much sediment a droplet can carry
            float
                minSedimentCapacity =
                    .01f; // Used to prevent carry capacity getting too close to zero on flatter terrain
            float erodeSpeed = .3f;
            float depositSpeed = .3f;
            float evaporateSpeed = .01f;
            float gravity = 4;
            int maxDropletLifetime = 30;

            float initialWaterVolume = 1;
            float initialSpeed = 1;

            int mapSize = erosionResolution;

            int originalMapSize = heightmapResolution;
            //int mapSize = 256;
            float[,] heightMap = heightmap;

            List<float> mapArray = new List<float>();
            for (int x = 0; x < mapSize; x++)
            {
                for (int y = 0; y < mapSize; y++)
                {
                    int xx = Mathf.FloorToInt(((float)x / ((float)mapSize - 1)) * ((float)originalMapSize - 1));
                    int yy = Mathf.FloorToInt(((float)y / ((float)mapSize - 1)) * ((float)originalMapSize - 1));

                    mapArray.Add(heightMap[xx, yy]);
                }
            }

            float[] map = mapArray.ToArray();

            ////

            int numThreads = numIterations / 1024;

            // Create brush
            List<int> brushIndexOffsets = new List<int>();
            List<float> brushWeights = new List<float>();

            float weightSum = 0;
            for (int brushY = -erosionRadius; brushY <= erosionRadius; brushY++)
            {
                for (int brushX = -erosionRadius; brushX <= erosionRadius; brushX++)
                {
                    float sqrDst = brushX * brushX + brushY * brushY;
                    if (sqrDst < erosionRadius * erosionRadius)
                    {
                        brushIndexOffsets.Add(brushY * mapSize + brushX);
                        float brushWeight = 1 - Mathf.Sqrt(sqrDst) / erosionRadius;
                        weightSum += brushWeight;
                        brushWeights.Add(brushWeight);
                    }
                }
            }

            for (int i = 0; i < brushWeights.Count; i++)
            {
                brushWeights[i] /= weightSum;
            }

            // Send brush data to compute shader
            ComputeBuffer brushIndexBuffer = new ComputeBuffer(brushIndexOffsets.Count, sizeof(int));
            ComputeBuffer brushWeightBuffer = new ComputeBuffer(brushWeights.Count, sizeof(int));
            brushIndexBuffer.SetData(brushIndexOffsets);
            brushWeightBuffer.SetData(brushWeights);
            erosion.SetBuffer(0, "brushIndices", brushIndexBuffer);
            erosion.SetBuffer(0, "brushWeights", brushWeightBuffer);

            // Generate random indices for droplet placement
            int[] randomIndices = new int[numIterations];
            for (int i = 0; i < numIterations; i++)
            {
                int randomX = UnityEngine.Random.Range(erosionRadius, mapSize + erosionRadius);
                int randomY = UnityEngine.Random.Range(erosionRadius, mapSize + erosionRadius);
                randomIndices[i] = randomY * mapSize + randomX;
            }

            // Send random indices to compute shader
            ComputeBuffer randomIndexBuffer = new ComputeBuffer(randomIndices.Length, sizeof(int));
            randomIndexBuffer.SetData(randomIndices);
            erosion.SetBuffer(0, "randomIndices", randomIndexBuffer);

            // Heightmap buffer
            ComputeBuffer mapBuffer = new ComputeBuffer(map.Length, sizeof(float));
            mapBuffer.SetData(map);
            erosion.SetBuffer(0, "map", mapBuffer);

            // Settings
            erosion.SetInt("borderSize", erosionRadius);
            erosion.SetInt("mapSize", mapSize);
            erosion.SetInt("brushLength", brushIndexOffsets.Count);
            erosion.SetInt("maxLifetime", maxDropletLifetime);
            erosion.SetFloat("inertia", inertia);
            erosion.SetFloat("sedimentCapacityFactor", sedimentCapacityFactor);
            erosion.SetFloat("minSedimentCapacity", minSedimentCapacity);
            erosion.SetFloat("depositSpeed", depositSpeed);
            erosion.SetFloat("erodeSpeed", erodeSpeed);
            erosion.SetFloat("evaporateSpeed", evaporateSpeed);
            erosion.SetFloat("gravity", gravity);
            erosion.SetFloat("startSpeed", initialSpeed);
            erosion.SetFloat("startWind", initialWaterVolume);
            erosion.SetVector("windDirection", windDirection);

            // Run compute shader
            erosion.Dispatch(0, numThreads, 1, 1);
            mapBuffer.GetData(map);

            // Release buffers
            mapBuffer.Release();
            randomIndexBuffer.Release();
            brushIndexBuffer.Release();
            brushWeightBuffer.Release();


            ////

            //Normalize Heightmap
            int xPos = 0;
            int yPos = 0;
            float[,] scaledHeightMap = new float[mapSize, mapSize];
            for (int i = 0; i < map.Length; i++)
            {
                scaledHeightMap[yPos, xPos] = map[i];

                if (xPos == mapSize - 1)
                {
                    xPos = 0;
                    yPos += 1;
                }
                else
                {
                    xPos += 1;
                }
            }

            //Upscale heightmap
            for (int x = 0; x < originalMapSize; x++)
            {
                for (int y = 0; y < originalMapSize; y++)
                {
                    float xP = (((float)x / (originalMapSize - 1)) * (mapSize - 1));
                    float yP = (((float)y / (originalMapSize - 1)) * (mapSize - 1));

                    int x1 = Mathf.FloorToInt(xP);
                    int y1 = Mathf.FloorToInt(yP);

                    int x2 = Mathf.FloorToInt(xP) + 1;
                    int y2 = Mathf.FloorToInt(yP) + 1;

                    float lerpValueX = xP - Mathf.FloorToInt(xP);
                    float lerpValueY = yP - Mathf.FloorToInt(yP);

                    if (x2 < mapSize && y2 < mapSize)
                    {
                        float avgHeightX = Mathf.Lerp(scaledHeightMap[x1, y1], scaledHeightMap[x2, y1], lerpValueX);
                        float avgHeightY = Mathf.Lerp(scaledHeightMap[x1, y2], scaledHeightMap[x2, y2], lerpValueX);
                        float avgHeight = Mathf.Lerp(avgHeightX, avgHeightY, lerpValueY);

                        heightMap[x, y] = avgHeight;
                    }
                }
            }

            return heightMap;
        }
    }
}