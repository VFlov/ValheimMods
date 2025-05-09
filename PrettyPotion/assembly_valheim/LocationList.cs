using System;
using System.Collections.Generic;
using UnityEngine;

// Token: 0x02000125 RID: 293
public class LocationList : MonoBehaviour
{
	// Token: 0x0600129E RID: 4766 RVA: 0x0008B0A9 File Offset: 0x000892A9
	private void Awake()
	{
		LocationList.m_allLocationLists.Add(this);
	}

	// Token: 0x0600129F RID: 4767 RVA: 0x0008B0B6 File Offset: 0x000892B6
	private void OnDestroy()
	{
		LocationList.m_allLocationLists.Remove(this);
	}

	// Token: 0x060012A0 RID: 4768 RVA: 0x0008B0C4 File Offset: 0x000892C4
	public static List<LocationList> GetAllLocationLists()
	{
		return LocationList.m_allLocationLists;
	}

	// Token: 0x04001258 RID: 4696
	private static List<LocationList> m_allLocationLists = new List<LocationList>();

	// Token: 0x04001259 RID: 4697
	public int m_sortOrder;

	// Token: 0x0400125A RID: 4698
	public List<ZoneSystem.ZoneLocation> m_locations = new List<ZoneSystem.ZoneLocation>();

	// Token: 0x0400125B RID: 4699
	public List<ZoneSystem.ZoneVegetation> m_vegetation = new List<ZoneSystem.ZoneVegetation>();

	// Token: 0x0400125C RID: 4700
	public List<EnvSetup> m_environments = new List<EnvSetup>();

	// Token: 0x0400125D RID: 4701
	public List<BiomeEnvSetup> m_biomeEnvironments = new List<BiomeEnvSetup>();

	// Token: 0x0400125E RID: 4702
	public List<RandomEvent> m_events = new List<RandomEvent>();

	// Token: 0x0400125F RID: 4703
	public List<ClutterSystem.Clutter> m_clutter = new List<ClutterSystem.Clutter>();

	// Token: 0x04001260 RID: 4704
	[HideInInspector]
	public List<Heightmap.Biome> m_biomeFolded = new List<Heightmap.Biome>();

	// Token: 0x04001261 RID: 4705
	[HideInInspector]
	public List<Heightmap.Biome> m_vegetationFolded = new List<Heightmap.Biome>();

	// Token: 0x04001262 RID: 4706
	[HideInInspector]
	public List<Heightmap.Biome> m_locationFolded = new List<Heightmap.Biome>();
}
