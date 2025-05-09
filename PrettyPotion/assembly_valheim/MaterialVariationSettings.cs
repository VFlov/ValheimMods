using System;
using UnityEngine;

// Token: 0x02000024 RID: 36
[Serializable]
public struct MaterialVariationSettings
{
	// Token: 0x040003F0 RID: 1008
	public Material[] m_materials;

	// Token: 0x040003F1 RID: 1009
	public Room.Theme m_dungeonThemeCondition;

	// Token: 0x040003F2 RID: 1010
	public Heightmap.Biome m_biomeCondition;
}
