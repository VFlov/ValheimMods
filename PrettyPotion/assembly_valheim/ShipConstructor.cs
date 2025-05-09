using System;
using UnityEngine;

// Token: 0x020001B5 RID: 437
public class ShipConstructor : MonoBehaviour
{
	// Token: 0x060019AC RID: 6572 RVA: 0x000BFEE0 File Offset: 0x000BE0E0
	private void Start()
	{
		this.m_nview = base.GetComponent<ZNetView>();
		if (this.m_nview == null || this.m_nview.GetZDO() == null)
		{
			return;
		}
		if (this.m_nview.IsOwner() && this.m_nview.GetZDO().GetLong(ZDOVars.s_spawnTime, 0L) == 0L)
		{
			this.m_nview.GetZDO().Set(ZDOVars.s_spawnTime, ZNet.instance.GetTime().Ticks);
		}
		base.InvokeRepeating("UpdateConstruction", 5f, 1f);
		if (this.IsBuilt())
		{
			this.m_hideWhenConstructed.SetActive(false);
			return;
		}
	}

	// Token: 0x060019AD RID: 6573 RVA: 0x000BFF8C File Offset: 0x000BE18C
	private bool IsBuilt()
	{
		return this.m_nview.GetZDO().GetBool(ZDOVars.s_done, false);
	}

	// Token: 0x060019AE RID: 6574 RVA: 0x000BFFA4 File Offset: 0x000BE1A4
	private void UpdateConstruction()
	{
		if (!this.m_nview.IsOwner())
		{
			return;
		}
		if (this.IsBuilt())
		{
			this.m_hideWhenConstructed.SetActive(false);
			return;
		}
		DateTime time = ZNet.instance.GetTime();
		DateTime d = new DateTime(this.m_nview.GetZDO().GetLong(ZDOVars.s_spawnTime, 0L));
		if ((time - d).TotalMinutes > (double)this.m_constructionTimeMinutes)
		{
			this.m_hideWhenConstructed.SetActive(false);
			UnityEngine.Object.Instantiate<GameObject>(this.m_shipPrefab, this.m_spawnPoint.position, this.m_spawnPoint.rotation);
			this.m_nview.GetZDO().Set(ZDOVars.s_done, true);
		}
	}

	// Token: 0x04001A33 RID: 6707
	public GameObject m_shipPrefab;

	// Token: 0x04001A34 RID: 6708
	public GameObject m_hideWhenConstructed;

	// Token: 0x04001A35 RID: 6709
	public Transform m_spawnPoint;

	// Token: 0x04001A36 RID: 6710
	public long m_constructionTimeMinutes = 1L;

	// Token: 0x04001A37 RID: 6711
	private ZNetView m_nview;
}
