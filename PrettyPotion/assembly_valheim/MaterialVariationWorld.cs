using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;

// Token: 0x02000023 RID: 35
public class MaterialVariationWorld : MonoBehaviour
{
	// Token: 0x060002F1 RID: 753 RVA: 0x0001A3E8 File Offset: 0x000185E8
	private void Update()
	{
		Location zoneLocation = Location.GetZoneLocation(base.transform.position);
		if (!zoneLocation)
		{
			return;
		}
		base.GetComponentsInChildren<MeshRenderer>(MaterialVariationWorld.mrs);
		DungeonGenerator generator = zoneLocation.m_generator;
		foreach (MaterialVariationSettings materialVariationSettings in this.m_variations)
		{
			if (generator && materialVariationSettings.m_dungeonThemeCondition != Room.Theme.None && generator.m_themes.HasFlag(materialVariationSettings.m_dungeonThemeCondition))
			{
				this.<Update>g__change|0_0(materialVariationSettings);
			}
			if (zoneLocation && materialVariationSettings.m_biomeCondition != Heightmap.Biome.None)
			{
				if (zoneLocation.m_biome == Heightmap.Biome.None)
				{
					zoneLocation.m_biome = WorldGenerator.instance.GetBiome(zoneLocation.transform.position);
				}
				if (materialVariationSettings.m_biomeCondition == zoneLocation.m_biome)
				{
					this.<Update>g__change|0_0(materialVariationSettings);
				}
			}
		}
		base.enabled = false;
	}

	// Token: 0x060002F4 RID: 756 RVA: 0x0001A50C File Offset: 0x0001870C
	[CompilerGenerated]
	private void <Update>g__change|0_0(MaterialVariationSettings mvs)
	{
		foreach (MeshRenderer meshRenderer in MaterialVariationWorld.mrs)
		{
			meshRenderer.materials = mvs.m_materials;
			Terminal.Log(string.Format("Replaced material on {0} for dungeon {1} or {2}", base.gameObject.name, mvs.m_dungeonThemeCondition, mvs.m_biomeCondition));
		}
	}

	// Token: 0x040003EE RID: 1006
	public List<MaterialVariationSettings> m_variations = new List<MaterialVariationSettings>();

	// Token: 0x040003EF RID: 1007
	private static List<MeshRenderer> mrs = new List<MeshRenderer>();
}
