using System;
using System.Collections.Generic;
using UnityEngine.Serialization;

// Token: 0x02000115 RID: 277
[Serializable]
public class BiomeEnvSetup
{
	// Token: 0x0400106D RID: 4205
	public string m_name = "";

	// Token: 0x0400106E RID: 4206
	public Heightmap.Biome m_biome = Heightmap.Biome.Meadows;

	// Token: 0x0400106F RID: 4207
	public List<EnvEntry> m_environments = new List<EnvEntry>();

	// Token: 0x04001070 RID: 4208
	public string m_musicMorning = "morning";

	// Token: 0x04001071 RID: 4209
	public string m_musicEvening = "evening";

	// Token: 0x04001072 RID: 4210
	[FormerlySerializedAs("m_musicRandomDay")]
	public string m_musicDay = "";

	// Token: 0x04001073 RID: 4211
	[FormerlySerializedAs("m_musicRandomNight")]
	public string m_musicNight = "";
}
