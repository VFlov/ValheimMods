using System;
using UnityEngine;

// Token: 0x02000018 RID: 24
public class EggGrow : MonoBehaviour, Hoverable
{
	// Token: 0x06000263 RID: 611 RVA: 0x00015A20 File Offset: 0x00013C20
	private void Start()
	{
		this.m_nview = base.GetComponent<ZNetView>();
		this.m_item = base.GetComponent<ItemDrop>();
		base.InvokeRepeating("GrowUpdate", UnityEngine.Random.Range(this.m_updateInterval, this.m_updateInterval * 2f), this.m_updateInterval);
		if (this.m_growingObject)
		{
			this.m_growingObject.SetActive(false);
		}
		if (this.m_notGrowingObject)
		{
			this.m_notGrowingObject.SetActive(true);
		}
	}

	// Token: 0x06000264 RID: 612 RVA: 0x00015AA0 File Offset: 0x00013CA0
	private void GrowUpdate()
	{
		float num = this.m_nview.GetZDO().GetFloat(ZDOVars.s_growStart, 0f);
		if (!this.m_nview.IsValid() || !this.m_nview.IsOwner() || this.m_item.m_itemData.m_stack > 1)
		{
			this.UpdateEffects(num);
			return;
		}
		if (this.CanGrow())
		{
			if (num == 0f)
			{
				num = (float)ZNet.instance.GetTimeSeconds();
			}
		}
		else
		{
			num = 0f;
		}
		this.m_nview.GetZDO().Set(ZDOVars.s_growStart, num);
		this.UpdateEffects(num);
		if (num > 0f && ZNet.instance.GetTimeSeconds() > (double)(num + this.m_growTime))
		{
			Character component = UnityEngine.Object.Instantiate<GameObject>(this.m_grownPrefab, base.transform.position, base.transform.rotation).GetComponent<Character>();
			this.m_hatchEffect.Create(base.transform.position, base.transform.rotation, null, 1f, -1);
			if (component)
			{
				component.SetTamed(this.m_tamed);
				component.SetLevel(this.m_item.m_itemData.m_quality);
			}
			this.m_nview.Destroy();
		}
	}

	// Token: 0x06000265 RID: 613 RVA: 0x00015BE8 File Offset: 0x00013DE8
	private bool CanGrow()
	{
		if (this.m_item.m_itemData.m_stack > 1)
		{
			return false;
		}
		if (this.m_requireNearbyFire && !EffectArea.IsPointInsideArea(base.transform.position, EffectArea.Type.Heat, 0.5f))
		{
			return false;
		}
		if (this.m_requireUnderRoof)
		{
			float num;
			bool flag;
			Cover.GetCoverForPoint(base.transform.position, out num, out flag, 0.1f);
			if (!flag || num < this.m_requireCoverPercentige)
			{
				return false;
			}
		}
		return true;
	}

	// Token: 0x06000266 RID: 614 RVA: 0x00015C64 File Offset: 0x00013E64
	private void UpdateEffects(float grow)
	{
		if (this.m_growingObject)
		{
			this.m_growingObject.SetActive(grow > 0f);
		}
		if (this.m_notGrowingObject)
		{
			this.m_notGrowingObject.SetActive(grow == 0f);
		}
	}

	// Token: 0x06000267 RID: 615 RVA: 0x00015CB4 File Offset: 0x00013EB4
	public string GetHoverText()
	{
		if (!this.m_item)
		{
			return "";
		}
		if (!this.m_nview || !this.m_nview.IsValid())
		{
			return this.m_item.GetHoverText();
		}
		bool flag = this.m_nview.GetZDO().GetFloat(ZDOVars.s_growStart, 0f) > 0f;
		string text = (this.m_item.m_itemData.m_stack > 1) ? "$item_chicken_egg_stacked" : (flag ? "$item_chicken_egg_warm" : "$item_chicken_egg_cold");
		string hoverText = this.m_item.GetHoverText();
		int num = hoverText.IndexOf('\n');
		if (num > 0)
		{
			return hoverText.Substring(0, num) + " " + Localization.instance.Localize(text) + hoverText.Substring(num);
		}
		return this.m_item.GetHoverText();
	}

	// Token: 0x06000268 RID: 616 RVA: 0x00015D8F File Offset: 0x00013F8F
	public string GetHoverName()
	{
		return this.m_item.GetHoverName();
	}

	// Token: 0x04000364 RID: 868
	public float m_growTime = 60f;

	// Token: 0x04000365 RID: 869
	public GameObject m_grownPrefab;

	// Token: 0x04000366 RID: 870
	public bool m_tamed;

	// Token: 0x04000367 RID: 871
	public float m_updateInterval = 5f;

	// Token: 0x04000368 RID: 872
	public bool m_requireNearbyFire = true;

	// Token: 0x04000369 RID: 873
	public bool m_requireUnderRoof = true;

	// Token: 0x0400036A RID: 874
	public float m_requireCoverPercentige = 0.7f;

	// Token: 0x0400036B RID: 875
	public EffectList m_hatchEffect;

	// Token: 0x0400036C RID: 876
	public GameObject m_growingObject;

	// Token: 0x0400036D RID: 877
	public GameObject m_notGrowingObject;

	// Token: 0x0400036E RID: 878
	private ZNetView m_nview;

	// Token: 0x0400036F RID: 879
	private ItemDrop m_item;
}
