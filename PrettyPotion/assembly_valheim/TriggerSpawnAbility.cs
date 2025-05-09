using System;
using UnityEngine;

// Token: 0x0200000A RID: 10
public class TriggerSpawnAbility : MonoBehaviour, IProjectile
{
	// Token: 0x0600006F RID: 111 RVA: 0x00008336 File Offset: 0x00006536
	public void Setup(Character owner, Vector3 velocity, float hitNoise, HitData hitData, ItemDrop.ItemData item, ItemDrop.ItemData ammo)
	{
		this.m_owner = owner;
		TriggerSpawner.TriggerAllInRange(base.transform.position, this.m_range);
	}

	// Token: 0x06000070 RID: 112 RVA: 0x00008355 File Offset: 0x00006555
	public string GetTooltipString(int itemQuality)
	{
		return "";
	}

	// Token: 0x04000155 RID: 341
	[Header("Spawn")]
	public float m_range = 10f;

	// Token: 0x04000156 RID: 342
	private Character m_owner;
}
