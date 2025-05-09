using System;

// Token: 0x02000169 RID: 361
public interface IDestructible
{
	// Token: 0x060015ED RID: 5613
	void Damage(HitData hit);

	// Token: 0x060015EE RID: 5614
	DestructibleType GetDestructibleType();
}
