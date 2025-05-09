using System;
using UnityEngine;

// Token: 0x02000184 RID: 388
public class HitArea : MonoBehaviour, IDestructible
{
	// Token: 0x06001775 RID: 6005 RVA: 0x000AE8AA File Offset: 0x000ACAAA
	public DestructibleType GetDestructibleType()
	{
		return DestructibleType.Default;
	}

	// Token: 0x06001776 RID: 6006 RVA: 0x000AE8AD File Offset: 0x000ACAAD
	public void Damage(HitData hit)
	{
		if (this.m_onHit != null)
		{
			this.m_onHit(hit, this);
		}
	}

	// Token: 0x0400174E RID: 5966
	public Action<HitData, HitArea> m_onHit;

	// Token: 0x0400174F RID: 5967
	public float m_health = 1f;

	// Token: 0x04001750 RID: 5968
	[NonSerialized]
	public GameObject m_parentObject;
}
