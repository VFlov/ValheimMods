using System;
using UnityEngine;

// Token: 0x02000060 RID: 96
public class OceanBubbleParticles : MonoBehaviour
{
	// Token: 0x0600067D RID: 1661 RVA: 0x00036722 File Offset: 0x00034922
	private void Start()
	{
		this.m_particleSystem = base.GetComponent<ParticleSystem>();
	}

	// Token: 0x0600067E RID: 1662 RVA: 0x00036730 File Offset: 0x00034930
	private void Update()
	{
		if (this.m_particles == null)
		{
			this.m_particles = new ParticleSystem.Particle[this.m_particleSystem.main.maxParticles];
		}
		int particles = this.m_particleSystem.GetParticles(this.m_particles);
		for (int i = 0; i < particles; i++)
		{
			float liquidLevel = Floating.GetLiquidLevel(this.m_particles[i].position, 1f, LiquidType.All);
			Vector3 position = this.m_particles[i].position;
			position.y = liquidLevel;
			this.m_particles[i].position = position;
		}
		this.m_particleSystem.SetParticles(this.m_particles);
	}

	// Token: 0x0400077A RID: 1914
	private ParticleSystem m_particleSystem;

	// Token: 0x0400077B RID: 1915
	private ParticleSystem.Particle[] m_particles;
}
