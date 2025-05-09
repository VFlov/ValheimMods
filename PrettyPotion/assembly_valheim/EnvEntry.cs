using System;

// Token: 0x02000116 RID: 278
[Serializable]
public class EnvEntry
{
	// Token: 0x04001074 RID: 4212
	public string m_environment = "";

	// Token: 0x04001075 RID: 4213
	public float m_weight = 1f;

	// Token: 0x04001076 RID: 4214
	public bool m_ashlandsOverride;

	// Token: 0x04001077 RID: 4215
	public bool m_deepnorthOverride;

	// Token: 0x04001078 RID: 4216
	[NonSerialized]
	public EnvSetup m_env;
}
