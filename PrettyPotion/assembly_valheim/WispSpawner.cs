using System;
using System.Collections.Generic;
using UnityEngine;

// Token: 0x020001E0 RID: 480
public class WispSpawner : MonoBehaviour, Hoverable
{
	// Token: 0x06001B93 RID: 7059 RVA: 0x000CEA94 File Offset: 0x000CCC94
	private void Start()
	{
		WispSpawner.s_spawners.Add(this);
		this.m_nview = base.GetComponentInParent<ZNetView>();
		base.InvokeRepeating("TrySpawn", 10f, 10f);
		base.InvokeRepeating("UpdateDemister", UnityEngine.Random.Range(0f, 2f), 2f);
	}

	// Token: 0x06001B94 RID: 7060 RVA: 0x000CEAEC File Offset: 0x000CCCEC
	private void OnDestroy()
	{
		WispSpawner.s_spawners.Remove(this);
	}

	// Token: 0x06001B95 RID: 7061 RVA: 0x000CEAFC File Offset: 0x000CCCFC
	public string GetHoverText()
	{
		switch (this.GetStatus())
		{
		case WispSpawner.Status.NoSpace:
			return Localization.instance.Localize(this.m_name + " ( $piece_wisplure_nospace )");
		case WispSpawner.Status.TooBright:
			return Localization.instance.Localize(this.m_name + " ( $piece_wisplure_light )");
		case WispSpawner.Status.Full:
			return Localization.instance.Localize(this.m_name + " ( $piece_wisplure_full )");
		case WispSpawner.Status.Ok:
			return Localization.instance.Localize(this.m_name + " ( $piece_wisplure_ok )");
		default:
			return "";
		}
	}

	// Token: 0x06001B96 RID: 7062 RVA: 0x000CEB99 File Offset: 0x000CCD99
	public string GetHoverName()
	{
		return this.m_name;
	}

	// Token: 0x06001B97 RID: 7063 RVA: 0x000CEBA4 File Offset: 0x000CCDA4
	private void UpdateDemister()
	{
		if (this.m_wispsNearbyObject)
		{
			int wispsInArea = LuredWisp.GetWispsInArea(this.m_spawnPoint.position, this.m_nearbyTreshold);
			this.m_wispsNearbyObject.SetActive(wispsInArea > 0);
		}
	}

	// Token: 0x06001B98 RID: 7064 RVA: 0x000CEBE4 File Offset: 0x000CCDE4
	private WispSpawner.Status GetStatus()
	{
		if (Time.time - this.m_lastStatusUpdate < 4f)
		{
			return this.m_status;
		}
		this.m_lastStatusUpdate = Time.time;
		this.m_status = WispSpawner.Status.Ok;
		if (!this.HaveFreeSpace())
		{
			this.m_status = WispSpawner.Status.NoSpace;
		}
		else if (this.m_onlySpawnAtNight && EnvMan.IsDaylight())
		{
			this.m_status = WispSpawner.Status.TooBright;
		}
		else if (LuredWisp.GetWispsInArea(this.m_spawnPoint.position, this.m_maxSpawnedArea) >= this.m_maxSpawned)
		{
			this.m_status = WispSpawner.Status.Full;
		}
		return this.m_status;
	}

	// Token: 0x06001B99 RID: 7065 RVA: 0x000CEC74 File Offset: 0x000CCE74
	private void TrySpawn()
	{
		if (!this.m_nview.IsValid() || !this.m_nview.IsOwner())
		{
			return;
		}
		DateTime time = ZNet.instance.GetTime();
		DateTime d = new DateTime(this.m_nview.GetZDO().GetLong(ZDOVars.s_lastSpawn, 0L));
		if ((time - d).TotalSeconds < (double)this.m_spawnInterval)
		{
			return;
		}
		if (UnityEngine.Random.value > this.m_spawnChance)
		{
			return;
		}
		if (this.GetStatus() != WispSpawner.Status.Ok)
		{
			return;
		}
		Vector3 position = this.m_spawnPoint.position + Quaternion.Euler(0f, (float)UnityEngine.Random.Range(0, 360), 0f) * Vector3.forward * this.m_spawnDistance;
		UnityEngine.Object.Instantiate<GameObject>(this.m_wispPrefab, position, Quaternion.identity);
		this.m_nview.GetZDO().Set(ZDOVars.s_lastSpawn, ZNet.instance.GetTime().Ticks);
	}

	// Token: 0x06001B9A RID: 7066 RVA: 0x000CED70 File Offset: 0x000CCF70
	private bool HaveFreeSpace()
	{
		if (this.m_maxCover <= 0f)
		{
			return true;
		}
		float num;
		bool flag;
		Cover.GetCoverForPoint(this.m_coverPoint.position, out num, out flag, 0.5f);
		return num < this.m_maxCover;
	}

	// Token: 0x06001B9B RID: 7067 RVA: 0x000CEDAE File Offset: 0x000CCFAE
	private void OnDrawGizmos()
	{
	}

	// Token: 0x06001B9C RID: 7068 RVA: 0x000CEDB0 File Offset: 0x000CCFB0
	public static WispSpawner GetBestSpawner(Vector3 p, float maxRange)
	{
		WispSpawner wispSpawner = null;
		float num = 0f;
		foreach (WispSpawner wispSpawner2 in WispSpawner.s_spawners)
		{
			float num2 = Vector3.Distance(wispSpawner2.m_spawnPoint.position, p);
			if (num2 <= maxRange)
			{
				WispSpawner.Status status = wispSpawner2.GetStatus();
				if (status != WispSpawner.Status.NoSpace && status != WispSpawner.Status.TooBright && (status != WispSpawner.Status.Full || num2 <= wispSpawner2.m_maxSpawnedArea) && (num2 < num || wispSpawner == null))
				{
					num = num2;
					wispSpawner = wispSpawner2;
				}
			}
		}
		return wispSpawner;
	}

	// Token: 0x04001C87 RID: 7303
	public string m_name = "$pieces_wisplure";

	// Token: 0x04001C88 RID: 7304
	public float m_spawnInterval = 5f;

	// Token: 0x04001C89 RID: 7305
	[Range(0f, 1f)]
	public float m_spawnChance = 0.5f;

	// Token: 0x04001C8A RID: 7306
	public int m_maxSpawned = 3;

	// Token: 0x04001C8B RID: 7307
	public bool m_onlySpawnAtNight = true;

	// Token: 0x04001C8C RID: 7308
	public bool m_dontSpawnInCover = true;

	// Token: 0x04001C8D RID: 7309
	[Range(0f, 1f)]
	public float m_maxCover = 0.6f;

	// Token: 0x04001C8E RID: 7310
	public GameObject m_wispPrefab;

	// Token: 0x04001C8F RID: 7311
	public GameObject m_wispsNearbyObject;

	// Token: 0x04001C90 RID: 7312
	public float m_nearbyTreshold = 5f;

	// Token: 0x04001C91 RID: 7313
	public Transform m_spawnPoint;

	// Token: 0x04001C92 RID: 7314
	public Transform m_coverPoint;

	// Token: 0x04001C93 RID: 7315
	public float m_spawnDistance = 20f;

	// Token: 0x04001C94 RID: 7316
	public float m_maxSpawnedArea = 10f;

	// Token: 0x04001C95 RID: 7317
	private ZNetView m_nview;

	// Token: 0x04001C96 RID: 7318
	private WispSpawner.Status m_status = WispSpawner.Status.Ok;

	// Token: 0x04001C97 RID: 7319
	private float m_lastStatusUpdate = -1000f;

	// Token: 0x04001C98 RID: 7320
	private static readonly List<WispSpawner> s_spawners = new List<WispSpawner>();

	// Token: 0x0200039C RID: 924
	public enum Status
	{
		// Token: 0x040026E3 RID: 9955
		NoSpace,
		// Token: 0x040026E4 RID: 9956
		TooBright,
		// Token: 0x040026E5 RID: 9957
		Full,
		// Token: 0x040026E6 RID: 9958
		Ok
	}
}
