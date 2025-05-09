using System;
using UnityEngine;
using UnityEngine.Serialization;

// Token: 0x02000117 RID: 279
[Serializable]
public class EnvSetup
{
	// Token: 0x0600114A RID: 4426 RVA: 0x00080AEA File Offset: 0x0007ECEA
	public EnvSetup Clone()
	{
		return base.MemberwiseClone() as EnvSetup;
	}

	// Token: 0x04001079 RID: 4217
	public string m_name = "";

	// Token: 0x0400107A RID: 4218
	public bool m_default;

	// Token: 0x0400107B RID: 4219
	[Header("Gameplay")]
	public bool m_isWet;

	// Token: 0x0400107C RID: 4220
	public bool m_isFreezing;

	// Token: 0x0400107D RID: 4221
	public bool m_isFreezingAtNight;

	// Token: 0x0400107E RID: 4222
	public bool m_isCold;

	// Token: 0x0400107F RID: 4223
	public bool m_isColdAtNight = true;

	// Token: 0x04001080 RID: 4224
	public bool m_alwaysDark;

	// Token: 0x04001081 RID: 4225
	[Header("Ambience")]
	public Color m_ambColorNight = Color.white;

	// Token: 0x04001082 RID: 4226
	public Color m_ambColorDay = Color.white;

	// Token: 0x04001083 RID: 4227
	[Header("Fog-ambient")]
	public Color m_fogColorNight = Color.white;

	// Token: 0x04001084 RID: 4228
	public Color m_fogColorMorning = Color.white;

	// Token: 0x04001085 RID: 4229
	public Color m_fogColorDay = Color.white;

	// Token: 0x04001086 RID: 4230
	public Color m_fogColorEvening = Color.white;

	// Token: 0x04001087 RID: 4231
	[Header("Fog-sun")]
	public Color m_fogColorSunNight = Color.white;

	// Token: 0x04001088 RID: 4232
	public Color m_fogColorSunMorning = Color.white;

	// Token: 0x04001089 RID: 4233
	public Color m_fogColorSunDay = Color.white;

	// Token: 0x0400108A RID: 4234
	public Color m_fogColorSunEvening = Color.white;

	// Token: 0x0400108B RID: 4235
	[Header("Fog-distance")]
	public float m_fogDensityNight = 0.01f;

	// Token: 0x0400108C RID: 4236
	public float m_fogDensityMorning = 0.01f;

	// Token: 0x0400108D RID: 4237
	public float m_fogDensityDay = 0.01f;

	// Token: 0x0400108E RID: 4238
	public float m_fogDensityEvening = 0.01f;

	// Token: 0x0400108F RID: 4239
	[Header("Sun")]
	public Color m_sunColorNight = Color.white;

	// Token: 0x04001090 RID: 4240
	public Color m_sunColorMorning = Color.white;

	// Token: 0x04001091 RID: 4241
	public Color m_sunColorDay = Color.white;

	// Token: 0x04001092 RID: 4242
	public Color m_sunColorEvening = Color.white;

	// Token: 0x04001093 RID: 4243
	public float m_lightIntensityDay = 1.2f;

	// Token: 0x04001094 RID: 4244
	public float m_lightIntensityNight;

	// Token: 0x04001095 RID: 4245
	public float m_sunAngle = 60f;

	// Token: 0x04001096 RID: 4246
	[Header("Wind")]
	public float m_windMin;

	// Token: 0x04001097 RID: 4247
	public float m_windMax = 1f;

	// Token: 0x04001098 RID: 4248
	[Header("Effects")]
	public GameObject m_envObject;

	// Token: 0x04001099 RID: 4249
	public GameObject[] m_psystems;

	// Token: 0x0400109A RID: 4250
	public bool m_psystemsOutsideOnly;

	// Token: 0x0400109B RID: 4251
	public float m_rainCloudAlpha;

	// Token: 0x0400109C RID: 4252
	[Header("Audio")]
	public AudioClip m_ambientLoop;

	// Token: 0x0400109D RID: 4253
	public float m_ambientVol = 0.3f;

	// Token: 0x0400109E RID: 4254
	public string m_ambientList = "";

	// Token: 0x0400109F RID: 4255
	[Header("Music overrides")]
	public string m_musicMorning = "";

	// Token: 0x040010A0 RID: 4256
	public string m_musicEvening = "";

	// Token: 0x040010A1 RID: 4257
	[FormerlySerializedAs("m_musicRandomDay")]
	public string m_musicDay = "";

	// Token: 0x040010A2 RID: 4258
	[FormerlySerializedAs("m_musicRandomNight")]
	public string m_musicNight = "";
}
