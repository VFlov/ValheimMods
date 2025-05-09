using System;
using System.Collections.Generic;
using UnityEngine;

// Token: 0x020001A7 RID: 423
public class ProximityState : MonoBehaviour
{
	// Token: 0x060018E9 RID: 6377 RVA: 0x000BA33F File Offset: 0x000B853F
	private void Start()
	{
		this.m_animator.SetBool("near", false);
	}

	// Token: 0x060018EA RID: 6378 RVA: 0x000BA354 File Offset: 0x000B8554
	private void OnTriggerEnter(Collider other)
	{
		if (this.m_playerOnly)
		{
			Character component = other.GetComponent<Character>();
			if (!component || !component.IsPlayer())
			{
				return;
			}
		}
		if (this.m_near.Contains(other))
		{
			return;
		}
		this.m_near.Add(other);
		if (!this.m_animator.GetBool("near"))
		{
			this.m_animator.SetBool("near", true);
			this.m_movingClose.Create(base.transform.position, base.transform.rotation, null, 1f, -1);
		}
	}

	// Token: 0x060018EB RID: 6379 RVA: 0x000BA3E8 File Offset: 0x000B85E8
	private void OnTriggerExit(Collider other)
	{
		this.m_near.Remove(other);
		if (this.m_near.Count == 0 && this.m_animator.GetBool("near"))
		{
			this.m_animator.SetBool("near", false);
			this.m_movingAway.Create(base.transform.position, base.transform.rotation, null, 1f, -1);
		}
	}

	// Token: 0x04001918 RID: 6424
	public bool m_playerOnly = true;

	// Token: 0x04001919 RID: 6425
	public Animator m_animator;

	// Token: 0x0400191A RID: 6426
	public EffectList m_movingClose = new EffectList();

	// Token: 0x0400191B RID: 6427
	public EffectList m_movingAway = new EffectList();

	// Token: 0x0400191C RID: 6428
	private List<Collider> m_near = new List<Collider>();
}
