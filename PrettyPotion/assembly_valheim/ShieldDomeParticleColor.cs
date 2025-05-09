using System;
using UnityEngine;

// Token: 0x02000064 RID: 100
public class ShieldDomeParticleColor : MonoBehaviour
{
	// Token: 0x0600069A RID: 1690 RVA: 0x000378D8 File Offset: 0x00035AD8
	private void Start()
	{
		Color domeColor = ShieldDomeImageEffect.GetDomeColor(ShieldGenerator.GetClosestShieldGenerator(base.transform.position, this.m_colorMode == ShieldDomeParticleColor.ColorMode.ClosestShieldGenerator).GetFuelRatio());
		foreach (ParticleSystem particleSystem in this.m_particleSystems)
		{
			ParticleSystem.MainModule main = particleSystem.main;
			Color color = particleSystem.main.startColor.color;
			domeColor.a = color.a;
			main.startColor = domeColor;
		}
	}

	// Token: 0x040007B1 RID: 1969
	public ShieldDomeParticleColor.ColorMode m_colorMode;

	// Token: 0x040007B2 RID: 1970
	public ParticleSystem[] m_particleSystems;

	// Token: 0x02000262 RID: 610
	[Serializable]
	public enum ColorMode
	{
		// Token: 0x040020AC RID: 8364
		ClosestShieldWall,
		// Token: 0x040020AD RID: 8365
		ClosestShieldGenerator
	}
}
