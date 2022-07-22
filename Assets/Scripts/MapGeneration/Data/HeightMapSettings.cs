using UnityEngine;

[CreateAssetMenu()]
public class HeightMapSettings : UpdatableData
{
	public bool UseFalloff;
	public float HeightMultiplier;
	public AnimationCurve HeightCurve;
	public NoiseSettings NoiseSettings;

	public float MinHeight => HeightMultiplier * HeightCurve.Evaluate(0);
	public float MaxHeight => HeightMultiplier * HeightCurve.Evaluate(1);

#if UNITY_EDITOR
	protected override void OnValidate()
	{
		NoiseSettings.ValidateValues();
		base.OnValidate();
	}
#endif

}
