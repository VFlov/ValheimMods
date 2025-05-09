using System;
using System.Collections.Generic;
using UnityEngine;

// Token: 0x02000061 RID: 97
[ExecuteAlways]
public class ParticleDecal : MonoBehaviour
{
	// Token: 0x06000680 RID: 1664 RVA: 0x000367E6 File Offset: 0x000349E6
	private void Awake()
	{
		this.part = base.GetComponent<ParticleSystem>();
		this.collisionEvents = new List<ParticleCollisionEvent>();
	}

	// Token: 0x06000681 RID: 1665 RVA: 0x00036800 File Offset: 0x00034A00
	private void OnParticleCollision(GameObject other)
	{
		if (this.m_chance < 100f && UnityEngine.Random.Range(0f, 100f) > this.m_chance)
		{
			return;
		}
		int num = this.part.GetCollisionEvents(other, this.collisionEvents);
		for (int i = 0; i < num; i++)
		{
			ParticleCollisionEvent particleCollisionEvent = this.collisionEvents[i];
			Vector3 eulerAngles = Quaternion.LookRotation(particleCollisionEvent.normal).eulerAngles;
			eulerAngles.x = -eulerAngles.x + 180f;
			eulerAngles.y = -eulerAngles.y;
			eulerAngles.z = (float)UnityEngine.Random.Range(0, 360);
			ParticleSystem.EmitParams emitParams = default(ParticleSystem.EmitParams);
			emitParams.position = particleCollisionEvent.intersection;
			emitParams.rotation3D = eulerAngles;
			emitParams.velocity = -particleCollisionEvent.normal * 0.001f;
			this.m_decalSystem.Emit(emitParams, 1);
		}
	}

	// Token: 0x0400077C RID: 1916
	public ParticleSystem m_decalSystem;

	// Token: 0x0400077D RID: 1917
	[Range(0f, 100f)]
	public float m_chance = 100f;

	// Token: 0x0400077E RID: 1918
	private ParticleSystem part;

	// Token: 0x0400077F RID: 1919
	private List<ParticleCollisionEvent> collisionEvents = new List<ParticleCollisionEvent>();
}
