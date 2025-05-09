using System;
using System.Collections.Generic;
using UnityEngine;

// Token: 0x0200001D RID: 29
public class Growup : MonoBehaviour
{
	// Token: 0x06000287 RID: 647 RVA: 0x0001688E File Offset: 0x00014A8E
	private void Start()
	{
		this.m_baseAI = base.GetComponent<BaseAI>();
		this.m_nview = base.GetComponent<ZNetView>();
		base.InvokeRepeating("GrowUpdate", UnityEngine.Random.Range(10f, 15f), 10f);
	}

	// Token: 0x06000288 RID: 648 RVA: 0x000168C8 File Offset: 0x00014AC8
	private void GrowUpdate()
	{
		if (!this.m_nview.IsValid() || !this.m_nview.IsOwner())
		{
			return;
		}
		if (this.m_baseAI.GetTimeSinceSpawned().TotalSeconds > (double)this.m_growTime)
		{
			Character component = base.GetComponent<Character>();
			Character component2 = UnityEngine.Object.Instantiate<GameObject>(this.GetPrefab(), base.transform.position, base.transform.rotation).GetComponent<Character>();
			if (component && component2)
			{
				if (this.m_inheritTame)
				{
					component2.SetTamed(component.IsTamed());
				}
				component2.SetLevel(component.GetLevel());
			}
			this.m_nview.Destroy();
		}
	}

	// Token: 0x06000289 RID: 649 RVA: 0x00016978 File Offset: 0x00014B78
	private GameObject GetPrefab()
	{
		if (this.m_altGrownPrefabs == null || this.m_altGrownPrefabs.Count == 0)
		{
			return this.m_grownPrefab;
		}
		float num = 0f;
		foreach (Growup.GrownEntry grownEntry in this.m_altGrownPrefabs)
		{
			num += grownEntry.m_weight;
		}
		float num2 = UnityEngine.Random.Range(0f, num);
		float num3 = 0f;
		for (int i = 0; i < this.m_altGrownPrefabs.Count; i++)
		{
			num3 += this.m_altGrownPrefabs[i].m_weight;
			if (num2 <= num3)
			{
				return this.m_altGrownPrefabs[i].m_prefab;
			}
		}
		return this.m_altGrownPrefabs[0].m_prefab;
	}

	// Token: 0x040003A5 RID: 933
	public float m_growTime = 60f;

	// Token: 0x040003A6 RID: 934
	public bool m_inheritTame = true;

	// Token: 0x040003A7 RID: 935
	public GameObject m_grownPrefab;

	// Token: 0x040003A8 RID: 936
	public List<Growup.GrownEntry> m_altGrownPrefabs;

	// Token: 0x040003A9 RID: 937
	private BaseAI m_baseAI;

	// Token: 0x040003AA RID: 938
	private ZNetView m_nview;

	// Token: 0x02000233 RID: 563
	[Serializable]
	public class GrownEntry
	{
		// Token: 0x04001F85 RID: 8069
		public GameObject m_prefab;

		// Token: 0x04001F86 RID: 8070
		public float m_weight = 1f;
	}
}
