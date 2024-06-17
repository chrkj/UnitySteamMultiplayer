using UnityEngine;
using UnityEditor;

namespace MapTerrain
{
    public class TerrainErosionTools : MonoBehaviour
    {
        [Header("Hydraulics")] [SerializeField]
        private int h_Iterations = 100000;

        [SerializeField] private int h_Resolution = 256;
        [Range(1, 8)] [SerializeField] private int h_ErosionSteps = 3;

        [Header("Wind")] [Range(0, 5)] [SerializeField]
        private float Windiness = 0.5f;

        [SerializeField] private Vector2 WindDirection = new Vector2(1, 2);
        [SerializeField] private int w_Resolution = 257;

        [Header("Terrace")] [SerializeField] private float TerraceSpacing = 15;
        [Range(0, 90)] [SerializeField] private float TerraceAngle = 20;

        [Header("Sharpen")] [SerializeField] private float SharpenStrength = 5;
        [SerializeField] private int sharpenIterations = 10;
        [Range(0, 1)] [SerializeField] private float PeakMixStrength = 0.7f;

        [Header("Smoothing")] public int SmoothingWidth = 2;

        [Header("Compute Shaders")] public ComputeShader HydraulicErosionComputeShader;
        public ComputeShader WindErosionComputeShader;
        public ComputeShader SmoothSharpenComputeShader;
        public ComputeShader TerraceComputeShader;

        private Terrain terrain;
        private float[,] heightMap;

        private RenderTexture rt;
        private Texture2D heightmapTex;
        private Texture2D rtTex2d;

        public void HydraulicErode()
        {
            terrain = GetComponent<Terrain>();
            SetUndo(terrain, "Her");

            int heightmapResolution = terrain.terrainData.heightmapResolution;
            heightMap = terrain.terrainData.GetHeights(0, 0, heightmapResolution, heightmapResolution);

            int iterations = h_Iterations;
            int resolution = h_Resolution;
            for (int i = 0; i < h_ErosionSteps; i++)
            {
                heightMap = TerrainHydraulics.Erode(HydraulicErosionComputeShader, iterations, heightMap,
                    heightmapResolution, resolution + 1);

                iterations *= 2;
                resolution *= 2;
            }

            terrain.terrainData.SetHeights(0, 0, heightMap);
        }

        public void WindErode()
        {
            terrain = GetComponent<Terrain>();
            SetUndo(terrain, "Wer");

            int heightmapResolution = terrain.terrainData.heightmapResolution;
            heightMap = terrain.terrainData.GetHeights(0, 0, heightmapResolution, heightmapResolution);

            heightMap = TerrainWind.Erode(WindErosionComputeShader, (int)(600000 * Windiness), heightMap,
                heightmapResolution, w_Resolution, WindDirection);

            terrain.terrainData.SetHeights(0, 0, heightMap);

            Smooth(4);
            Smooth(2);
        }

        public void Terrace()
        {
            terrain = GetComponent<Terrain>();
            SetUndo(terrain, "Terrace");

            int heightmapResolution = terrain.terrainData.heightmapResolution;
            heightMap = terrain.terrainData.GetHeights(0, 0, heightmapResolution, heightmapResolution);

            heightmapTex = ConvertHeightmapToTexture(heightmapResolution);

            CreateRT(heightmapResolution);

            //Setup Compute Shader
            TerraceComputeShader.SetTexture(TerraceComputeShader.FindKernel("Terrace"), "Result", rt);
            TerraceComputeShader.SetTexture(TerraceComputeShader.FindKernel("Terrace"), "heightMap", heightmapTex);
            TerraceComputeShader.SetTexture(TerraceComputeShader.FindKernel("Terrace"), "normalHeightmap",
                heightmapTex);
            TerraceComputeShader.SetInt("heightmapResolution", heightmapResolution);

            TerraceComputeShader.SetFloat("terraceSpacing", TerraceSpacing);
            TerraceComputeShader.SetFloat("angle", TerraceAngle);
            TerraceComputeShader.SetFloat("terrainHeight", terrain.terrainData.size.y);
            TerraceComputeShader.SetFloat("terrainWidth", terrain.terrainData.size.x);

            TerraceComputeShader.Dispatch(TerraceComputeShader.FindKernel("Terrace"), rt.width / 8, rt.height / 8, 1);

            ReadRenderTextureToCPU(heightmapResolution);

            terrain.terrainData.SetHeights(0, 0, heightMap);

            Smooth(1);
        }

        public void Smooth(int width)
        {
            terrain = GetComponent<Terrain>();
            SetUndo(terrain, "Smooth");

            int heightmapResolution = terrain.terrainData.heightmapResolution;
            heightMap = terrain.terrainData.GetHeights(0, 0, heightmapResolution, heightmapResolution);

            heightmapTex = ConvertHeightmapToTexture(heightmapResolution);

            CreateRT(heightmapResolution);

            //Setup Compute Shader
            SmoothSharpenComputeShader.SetTexture(SmoothSharpenComputeShader.FindKernel("Smooth"), "Result", rt);
            SmoothSharpenComputeShader.SetTexture(SmoothSharpenComputeShader.FindKernel("Smooth"), "heightMap",
                heightmapTex);
            SmoothSharpenComputeShader.SetInt("heightSize", heightmapResolution);
            SmoothSharpenComputeShader.SetInt("sWidth", SmoothingWidth);
            SmoothSharpenComputeShader.Dispatch(SmoothSharpenComputeShader.FindKernel("Smooth"), rt.width / 8,
                rt.height / 8, 1);

            ReadRenderTextureToCPU(heightmapResolution);

            terrain.terrainData.SetHeights(0, 0, heightMap);
        }

        public void Sharpen()
        {
            terrain = GetComponent<Terrain>();
            SetUndo(terrain, "Sharpen");

            int heightmapResolution = terrain.terrainData.heightmapResolution;
            heightMap = terrain.terrainData.GetHeights(0, 0, heightmapResolution, heightmapResolution);

            for (int i = 0; i < sharpenIterations; i++)
            {
                heightmapTex = ConvertHeightmapToTexture(heightmapResolution);

                CreateRT(heightmapResolution);

                //Setup Compute Shader
                SmoothSharpenComputeShader.SetTexture(SmoothSharpenComputeShader.FindKernel("Sharpen"), "Result", rt);
                SmoothSharpenComputeShader.SetTexture(SmoothSharpenComputeShader.FindKernel("Sharpen"), "heightMap",
                    heightmapTex);
                SmoothSharpenComputeShader.SetInt("heightSize", heightmapResolution);
                SmoothSharpenComputeShader.SetFloat("shstr", SharpenStrength);
                SmoothSharpenComputeShader.SetFloat("pmstr", PeakMixStrength);
                SmoothSharpenComputeShader.Dispatch(SmoothSharpenComputeShader.FindKernel("Sharpen"), rt.width / 8,
                    rt.height / 8, 1);

                ReadRenderTextureToCPU(heightmapResolution);
            }

            terrain.terrainData.SetHeights(0, 0, heightMap);
        }

        void CreateRT(int heightmapResolution)
        {
            //Initialize Render Texture
            rt = new RenderTexture(heightmapResolution, heightmapResolution, 256, RenderTextureFormat.ARGBFloat);
            rt.enableRandomWrite = true;
            rt.Create();
        }

        void ReadRenderTextureToCPU(int heightmapResolution)
        {
            //Read RT To CPU
            Rect rectReadPicture = new Rect(0, 0, rt.width, rt.height);

            RenderTexture.active = rt;

            rtTex2d = new Texture2D(rt.width, rt.height, TextureFormat.RGBAFloat, false);
            rtTex2d.filterMode = FilterMode.Point;
            rtTex2d.ReadPixels(rectReadPicture, 0, 0);
            rtTex2d.Apply();

            RenderTexture.active = null;

            //Read Heightmap To Terrain
            for (int x = 0; x < heightmapResolution; x++)
            {
                for (int y = 0; y < heightmapResolution; y++)
                {
                    heightMap[x, y] = rtTex2d.GetPixel((int)(((float)x / heightmapResolution) * rtTex2d.width),
                        (int)(((float)y / heightmapResolution) * rtTex2d.height)).r;
                }
            }
        }

        Texture2D ConvertHeightmapToTexture(int heightmapResolution)
        {
            Texture2D tex = new Texture2D(heightmapResolution, heightmapResolution, TextureFormat.RGBAFloat, false);
            tex.filterMode = FilterMode.Point;
            tex.Apply();

            for (int x = 0; x < heightmapResolution; x++)
            {
                for (int y = 0; y < heightmapResolution; y++)
                {
                    tex.SetPixel((int)(((float)x / heightmapResolution) * tex.width),
                        (int)(((float)y / heightmapResolution) * tex.height), Color.white * heightMap[x, y]);
                }
            }

            tex.Apply();

            return tex;
        }

        void SetUndo(Terrain terrain, string title)
        {
            Undo.RegisterFullObjectHierarchyUndo(terrain.terrainData, title + ": " + terrain.name);
            EditorUtility.SetDirty(terrain);
        }
    }
}