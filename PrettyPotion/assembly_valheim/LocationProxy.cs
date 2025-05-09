using System;
using UnityEngine;

// Token: 0x02000126 RID: 294
public class LocationProxy : MonoBehaviour
{
	// Token: 0x060012A3 RID: 4771 RVA: 0x0008B14E File Offset: 0x0008934E
	private void Awake()
	{
		this.m_nview = base.GetComponent<ZNetView>();
		this.SpawnLocation();
	}

	// Token: 0x060012A4 RID: 4772 RVA: 0x0008B163 File Offset: 0x00089363
	private void Update()
	{
		if (!this.m_locationNeedsSpawn)
		{
			return;
		}
		this.SpawnLocation();
	}

	// Token: 0x060012A5 RID: 4773 RVA: 0x0008B175 File Offset: 0x00089375
	private void OnDestroy()
	{
		if (this.m_zdoSetToBeLoadingInZone != null)
		{
			ZoneSystem.instance.UnsetLoadingInZone(this.m_zdoSetToBeLoadingInZone);
			this.m_zdoSetToBeLoadingInZone = null;
		}
	}

	// Token: 0x060012A6 RID: 4774 RVA: 0x0008B198 File Offset: 0x00089398
	public void SetLocation(string location, int seed, bool spawnNow)
	{
		int stableHashCode = location.GetStableHashCode();
		this.m_nview.GetZDO().Set(ZDOVars.s_location, stableHashCode, false);
		this.m_nview.GetZDO().Set(ZDOVars.s_seed, seed, false);
		if (spawnNow)
		{
			this.SpawnLocation();
		}
	}

	// Token: 0x060012A7 RID: 4775 RVA: 0x0008B1E4 File Offset: 0x000893E4
	private bool SpawnLocation()
	{
		int @int = this.m_nview.GetZDO().GetInt(ZDOVars.s_location, 0);
		int int2 = this.m_nview.GetZDO().GetInt(ZDOVars.s_seed, 0);
		if (@int == 0)
		{
			return false;
		}
		if (ZoneSystem.instance.ShouldDelayProxyLocationSpawning(@int))
		{
			this.m_locationNeedsSpawn = true;
			if (this.m_zdoSetToBeLoadingInZone == null)
			{
				this.m_zdoSetToBeLoadingInZone = this.m_nview.GetZDO();
				ZoneSystem.instance.SetLoadingInZone(this.m_zdoSetToBeLoadingInZone);
			}
			return false;
		}
		this.m_instance = ZoneSystem.instance.SpawnProxyLocation(@int, int2, base.transform.position, base.transform.rotation);
		if (this.m_instance == null)
		{
			return false;
		}
		this.m_instance.transform.SetParent(base.transform, true);
		this.m_nview.LoadFields();
		this.m_locationNeedsSpawn = false;
		if (this.m_zdoSetToBeLoadingInZone != null)
		{
			ZoneSystem.instance.UnsetLoadingInZone(this.m_zdoSetToBeLoadingInZone);
			this.m_zdoSetToBeLoadingInZone = null;
		}
		return true;
	}

	// Token: 0x04001263 RID: 4707
	private bool m_locationNeedsSpawn;

	// Token: 0x04001264 RID: 4708
	private ZDO m_zdoSetToBeLoadingInZone;

	// Token: 0x04001265 RID: 4709
	private GameObject m_instance;

	// Token: 0x04001266 RID: 4710
	private ZNetView m_nview;
}
