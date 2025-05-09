using System;
using UnityEngine;

// Token: 0x02000005 RID: 5
public interface IProjectile
{
	// Token: 0x06000048 RID: 72
	void Setup(Character owner, Vector3 velocity, float hitNoise, HitData hitData, ItemDrop.ItemData item, ItemDrop.ItemData ammo);

	// Token: 0x06000049 RID: 73
	string GetTooltipString(int itemQuality);
}
