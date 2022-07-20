using UnityEngine;

[CreateAssetMenu()]
public class MeshSettings : UpdatableData 
{
	[Range(0, numSupportedChunkSizes - 1)]
	public int ChunkSizeIndex;
	
	public float meshScale = 2.5f;
	// num verts per line of mesh rendered at LOD = 0. Includes the 2 extra verts that are excluded from final mesh, but used for calculating normals
	public int numVertsPerLine => supportedChunkSizes[ChunkSizeIndex] + 5;
	public float meshWorldSize => (numVertsPerLine - 3) * meshScale;
	
	private const int numSupportedChunkSizes = 9;
	private static readonly int[] supportedChunkSizes = { 48, 72, 96, 120, 144, 168, 192, 216, 864 };

}
