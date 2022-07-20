using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(MapPreview))]
public class MapPreviewEditor : Editor 
{
	public override void OnInspectorGUI() 
	{
		MapPreview mapPreview = (MapPreview)target;

		if (DrawDefaultInspector()) 
		{
			if (mapPreview.AutoUpdate)
				mapPreview.DrawMap();
		}

		if (GUILayout.Button("Generate"))
			mapPreview.DrawMap();
	}
}
