using System;
using UnityEngine;

// Token: 0x020001BF RID: 447
public class SpawnPrefab : MonoBehaviour
{
	// Token: 0x06001A18 RID: 6680 RVA: 0x000C2848 File Offset: 0x000C0A48
	private void Start()
	{
		this.m_nview = base.GetComponentInParent<ZNetView>();
		if (this.m_nview == null)
		{
			ZLog.LogWarning("SpawnerPrefab cant find netview " + base.gameObject.name);
			return;
		}
		base.InvokeRepeating("TrySpawn", 1f, 1f);
	}

	// Token: 0x06001A19 RID: 6681 RVA: 0x000C28A0 File Offset: 0x000C0AA0
	private void TrySpawn()
	{
		if (!this.m_nview.IsValid() || !this.m_nview.IsOwner())
		{
			return;
		}
		string name = "HasSpawned_" + base.gameObject.name;
		if (!this.m_nview.GetZDO().GetBool(name, false))
		{
			ZLog.Log("SpawnPrefab " + base.gameObject.name + " SPAWNING " + this.m_prefab.name);
			UnityEngine.Object.Instantiate<GameObject>(this.m_prefab, base.transform.position, base.transform.rotation);
			this.m_nview.GetZDO().Set(name, true);
		}
		base.CancelInvoke("TrySpawn");
	}

	// Token: 0x06001A1A RID: 6682 RVA: 0x000C295B File Offset: 0x000C0B5B
	private void OnDrawGizmos()
	{
	}

	// Token: 0x04001A95 RID: 6805
	public GameObject m_prefab;

	// Token: 0x04001A96 RID: 6806
	private ZNetView m_nview;
}
