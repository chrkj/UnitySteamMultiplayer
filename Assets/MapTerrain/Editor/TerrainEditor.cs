using UnityEngine;
using UnityEditor;

namespace MapTerrain
{
    [CustomEditor(typeof(TerrainGenerator))]
    public class TerrainEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            TerrainGenerator terrain = (TerrainGenerator)target;

            if (GUILayout.Button("Generate"))
            {
                terrain.spawnPrefabs = true;
                terrain.Generate();
            }

            if (GUILayout.Button("Generate Random"))
            {
                terrain.seed = Random.value * 50000f;
                terrain.spawnPrefabs = true;
                terrain.Generate();
            }

            if (GUILayout.Button("Spawn Prefabs"))
            {
                terrain.SpawnTrees();
            }

            if (GUILayout.Button("Spawn Details"))
            {
                terrain.SpawnDetailTextures();
            }
        }
    }
}