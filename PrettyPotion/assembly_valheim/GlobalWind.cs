using System;
using UnityEngine;

// Token: 0x02000054 RID: 84
public class GlobalWind : MonoBehaviour
{
	// Token: 0x06000617 RID: 1559 RVA: 0x00033694 File Offset: 0x00031894
	private void Start()
	{
		if (EnvMan.instance == null)
		{
			return;
		}
		this.m_ps = base.GetComponent<ParticleSystem>();
		this.m_cloth = base.GetComponent<Cloth>();
		if (this.m_checkPlayerShelter)
		{
			this.m_player = base.GetComponentInParent<Player>();
		}
		if (this.m_smoothUpdate)
		{
			base.InvokeRepeating("UpdateWind", 0f, 0.01f);
			return;
		}
		base.InvokeRepeating("UpdateWind", UnityEngine.Random.Range(1.5f, 2.5f), 2f);
		this.UpdateWind();
	}

	// Token: 0x06000618 RID: 1560 RVA: 0x00033720 File Offset: 0x00031920
	private void UpdateWind()
	{
		if (this.m_alignToWindDirection)
		{
			Vector3 windDir = EnvMan.instance.GetWindDir();
			base.transform.rotation = Quaternion.LookRotation(windDir, Vector3.up);
		}
		if (this.m_ps)
		{
			if (!this.m_ps.emission.enabled)
			{
				return;
			}
			Vector3 windForce = EnvMan.instance.GetWindForce();
			if (this.m_particleVelocity)
			{
				ParticleSystem.VelocityOverLifetimeModule velocityOverLifetime = this.m_ps.velocityOverLifetime;
				velocityOverLifetime.space = ParticleSystemSimulationSpace.World;
				velocityOverLifetime.x = windForce.x * this.m_multiplier;
				velocityOverLifetime.z = windForce.z * this.m_multiplier;
			}
			if (this.m_particleForce)
			{
				ParticleSystem.ForceOverLifetimeModule forceOverLifetime = this.m_ps.forceOverLifetime;
				forceOverLifetime.space = ParticleSystemSimulationSpace.World;
				forceOverLifetime.x = windForce.x * this.m_multiplier;
				forceOverLifetime.z = windForce.z * this.m_multiplier;
			}
			if (this.m_particleEmission)
			{
				this.m_ps.emission.rateOverTimeMultiplier = Mathf.Lerp((float)this.m_particleEmissionMin, (float)this.m_particleEmissionMax, EnvMan.instance.GetWindIntensity());
			}
		}
		if (this.m_cloth)
		{
			Vector3 a = EnvMan.instance.GetWindForce();
			if (this.m_checkPlayerShelter && this.m_player != null && this.m_player.InShelter())
			{
				a = Vector3.zero;
			}
			this.m_cloth.externalAcceleration = a * this.m_multiplier;
			this.m_cloth.randomAcceleration = a * this.m_multiplier * this.m_clothRandomAccelerationFactor;
		}
	}

	// Token: 0x06000619 RID: 1561 RVA: 0x000338DB File Offset: 0x00031ADB
	public void UpdateClothReference(Cloth cloth)
	{
		this.m_cloth = cloth;
	}

	// Token: 0x040006E6 RID: 1766
	public float m_multiplier = 1f;

	// Token: 0x040006E7 RID: 1767
	public bool m_smoothUpdate;

	// Token: 0x040006E8 RID: 1768
	public bool m_alignToWindDirection;

	// Token: 0x040006E9 RID: 1769
	[Header("Particles")]
	public bool m_particleVelocity = true;

	// Token: 0x040006EA RID: 1770
	public bool m_particleForce;

	// Token: 0x040006EB RID: 1771
	public bool m_particleEmission;

	// Token: 0x040006EC RID: 1772
	public int m_particleEmissionMin;

	// Token: 0x040006ED RID: 1773
	public int m_particleEmissionMax = 1;

	// Token: 0x040006EE RID: 1774
	[Header("Cloth")]
	public float m_clothRandomAccelerationFactor = 0.5f;

	// Token: 0x040006EF RID: 1775
	public bool m_checkPlayerShelter;

	// Token: 0x040006F0 RID: 1776
	private ParticleSystem m_ps;

	// Token: 0x040006F1 RID: 1777
	private Cloth m_cloth;

	// Token: 0x040006F2 RID: 1778
	private Player m_player;
}
