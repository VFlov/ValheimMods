using System;
using System.Collections.Generic;
using UnityEngine;

// Token: 0x0200014A RID: 330
public class SpawnSystemList : MonoBehaviour
{
	// Token: 0x06001429 RID: 5161 RVA: 0x000946C8 File Offset: 0x000928C8
	public void GetSpawners(Heightmap.Biome biome, List<SpawnSystem.SpawnData> spawners)
	{
		foreach (SpawnSystem.SpawnData spawnData in this.m_spawners)
		{
			if ((spawnData.m_biome & biome) != Heightmap.Biome.None || spawnData.m_biome == biome)
			{
				spawners.Add(spawnData);
			}
		}
	}

	// Token: 0x040013E6 RID: 5094
	public List<SpawnSystem.SpawnData> m_spawners = new List<SpawnSystem.SpawnData>();

	// Token: 0x040013E7 RID: 5095
	[HideInInspector]
	public List<Heightmap.Biome> m_biomeFolded = new List<Heightmap.Biome>();
}
