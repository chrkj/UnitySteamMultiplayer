using UnityEngine;

public class UpdatableData : ScriptableObject 
{
	public bool AutoUpdate;
	public event System.Action OnValuesUpdated;

#if UNITY_EDITOR
	protected virtual void OnValidate() 
	{
		if (AutoUpdate)
			UnityEditor.EditorApplication.update += NotifyOfUpdatedValues;
	}

	public void NotifyOfUpdatedValues() 
	{
		UnityEditor.EditorApplication.update -= NotifyOfUpdatedValues;
		OnValuesUpdated?.Invoke();
	}
#endif
}
