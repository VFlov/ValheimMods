using System;
using UnityEngine;

// Token: 0x02000049 RID: 73
public class WeakSpot : MonoBehaviour
{
	// Token: 0x060005DB RID: 1499 RVA: 0x00031D88 File Offset: 0x0002FF88
	private void Awake()
	{
		this.m_collider = base.GetComponent<Collider>();
	}

	// Token: 0x04000699 RID: 1689
	public HitData.DamageModifiers m_damageModifiers;

	// Token: 0x0400069A RID: 1690
	[NonSerialized]
	public Collider m_collider;
}
