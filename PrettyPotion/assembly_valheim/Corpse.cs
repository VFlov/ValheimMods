using System;
using UnityEngine;

// Token: 0x02000015 RID: 21
public class Corpse : MonoBehaviour
{
	// Token: 0x0600024C RID: 588 RVA: 0x00014C58 File Offset: 0x00012E58
	private void Awake()
	{
		this.m_nview = base.GetComponent<ZNetView>();
		this.m_container = base.GetComponent<Container>();
		if (this.m_nview.IsOwner() && this.m_nview.GetZDO().GetLong(ZDOVars.s_timeOfDeath, 0L) == 0L)
		{
			this.m_nview.GetZDO().Set(ZDOVars.s_timeOfDeath, ZNet.instance.GetTime().Ticks);
		}
		base.InvokeRepeating("UpdateDespawn", Corpse.m_updateDt, Corpse.m_updateDt);
	}

	// Token: 0x0600024D RID: 589 RVA: 0x00014CE0 File Offset: 0x00012EE0
	private void UpdateDespawn()
	{
		if (!this.m_nview.IsOwner())
		{
			return;
		}
		if (this.m_container.IsInUse())
		{
			return;
		}
		if (this.m_container.GetInventory().NrOfItems() <= 0)
		{
			this.m_emptyTimer += Corpse.m_updateDt;
			if (this.m_emptyTimer >= this.m_emptyDespawnDelaySec)
			{
				ZLog.Log("Despawning looted corpse");
				this.m_nview.Destroy();
				return;
			}
		}
		else
		{
			this.m_emptyTimer = 0f;
		}
	}

	// Token: 0x04000332 RID: 818
	private static readonly float m_updateDt = 2f;

	// Token: 0x04000333 RID: 819
	public float m_emptyDespawnDelaySec = 10f;

	// Token: 0x04000334 RID: 820
	private float m_emptyTimer;

	// Token: 0x04000335 RID: 821
	private Container m_container;

	// Token: 0x04000336 RID: 822
	private ZNetView m_nview;
}
