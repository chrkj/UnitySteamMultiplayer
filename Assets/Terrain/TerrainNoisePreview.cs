using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(TerrainGenerator))]
public class TerrainNoisePreview : Editor 
{
    public override void OnInspectorGUI() 
    {
        TerrainGenerator terrainGenerator = (TerrainGenerator)target;

        if (DrawDefaultInspector()) 
        {
            if (terrainGenerator.AutoUpdate)
                terrainGenerator.DrawNoiseMap();
        }

        if (GUILayout.Button("Generate"))
            terrainGenerator.DrawNoiseMap();
    }
}