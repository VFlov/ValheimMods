using System;
using System.Collections.Generic;
using UnityEngine;

// Token: 0x0200018D RID: 397
public class Ledge : MonoBehaviour
{
	// Token: 0x060017C3 RID: 6083 RVA: 0x000B0CFC File Offset: 0x000AEEFC
	private void Awake()
	{
		if (base.GetComponent<ZNetView>().GetZDO() == null)
		{
			return;
		}
		this.m_collider.enabled = true;
		TriggerTracker above = this.m_above;
		above.m_changed = (Action)Delegate.Combine(above.m_changed, new Action(this.Changed));
	}

	// Token: 0x060017C4 RID: 6084 RVA: 0x000B0D4C File Offset: 0x000AEF4C
	private void Changed()
	{
		List<Collider> colliders = this.m_above.GetColliders();
		if (colliders.Count == 0)
		{
			this.m_collider.enabled = true;
			return;
		}
		bool enabled = false;
		using (List<Collider>.Enumerator enumerator = colliders.GetEnumerator())
		{
			while (enumerator.MoveNext())
			{
				if (enumerator.Current.transform.position.y > base.transform.position.y)
				{
					enabled = true;
					break;
				}
			}
		}
		this.m_collider.enabled = enabled;
	}

	// Token: 0x04001791 RID: 6033
	public Collider m_collider;

	// Token: 0x04001792 RID: 6034
	public TriggerTracker m_above;
}
