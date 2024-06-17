using UnityEngine;
using UnityEditor;

namespace MapTerrain
{
    [CustomEditor(typeof(TerrainErosionTools))]
    public class ErosionToolsEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            TerrainErosionTools terrain = (TerrainErosionTools)target;

            if (GUILayout.Button("Hydraulic Erode"))
            {
                terrain.HydraulicErode();
            }

            if (GUILayout.Button("Wind Erode"))
            {
                terrain.WindErode();
            }

            if (GUILayout.Button("Terrace"))
            {
                terrain.Terrace();
            }

            if (GUILayout.Button("Smooth"))
            {
                terrain.Smooth(terrain.SmoothingWidth);
            }

            if (GUILayout.Button("Sharpen"))
            {
                terrain.Sharpen();
            }
        }
    }
}