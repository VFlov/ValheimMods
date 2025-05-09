using System;
using UnityEngine;

// Token: 0x020001B2 RID: 434
public class SapCollector : MonoBehaviour, Hoverable, Interactable
{
	// Token: 0x06001944 RID: 6468 RVA: 0x000BC924 File Offset: 0x000BAB24
	private void Awake()
	{
		this.m_nview = base.GetComponent<ZNetView>();
		this.m_collider = base.GetComponentInChildren<Collider>();
		this.m_piece = base.GetComponent<Piece>();
		if (this.m_nview.GetZDO() == null)
		{
			return;
		}
		if (this.m_nview.IsOwner() && this.m_nview.GetZDO().GetLong(ZDOVars.s_lastTime, 0L) == 0L)
		{
			this.m_nview.GetZDO().Set(ZDOVars.s_lastTime, ZNet.instance.GetTime().Ticks);
		}
		this.m_nview.Register("RPC_Extract", new Action<long>(this.RPC_Extract));
		this.m_nview.Register("RPC_UpdateEffects", new Action<long>(this.RPC_UpdateEffects));
		base.InvokeRepeating("UpdateTick", UnityEngine.Random.Range(0f, 2f), 5f);
	}

	// Token: 0x06001945 RID: 6469 RVA: 0x000BCA08 File Offset: 0x000BAC08
	public string GetHoverText()
	{
		int level = this.GetLevel();
		string statusText = this.GetStatusText();
		string text = string.Concat(new string[]
		{
			this.m_name,
			" ( ",
			statusText,
			", ",
			level.ToString(),
			" / ",
			this.m_maxLevel.ToString(),
			" )"
		});
		if (level > 0)
		{
			text = text + "\n[<color=yellow><b>$KEY_Use</b></color>] " + this.m_extractText;
		}
		return Localization.instance.Localize(text);
	}

	// Token: 0x06001946 RID: 6470 RVA: 0x000BCA95 File Offset: 0x000BAC95
	public string GetHoverName()
	{
		return this.m_name;
	}

	// Token: 0x06001947 RID: 6471 RVA: 0x000BCAA0 File Offset: 0x000BACA0
	public bool Interact(Humanoid character, bool repeat, bool alt)
	{
		if (repeat)
		{
			return false;
		}
		if (!PrivateArea.CheckAccess(base.transform.position, 0f, true, false))
		{
			return true;
		}
		if (this.GetLevel() > 0)
		{
			this.Extract();
			Game.instance.IncrementPlayerStat(PlayerStatType.SapHarvested, 1f);
			return true;
		}
		return false;
	}

	// Token: 0x06001948 RID: 6472 RVA: 0x000BCAF0 File Offset: 0x000BACF0
	private string GetStatusText()
	{
		if (this.GetLevel() >= this.m_maxLevel)
		{
			return this.m_fullText;
		}
		if (!this.m_root)
		{
			return this.m_notConnectedText;
		}
		if (this.m_root.IsLevelLow())
		{
			return this.m_drainingSlowText;
		}
		return this.m_drainingText;
	}

	// Token: 0x06001949 RID: 6473 RVA: 0x000BCB40 File Offset: 0x000BAD40
	public bool UseItem(Humanoid user, ItemDrop.ItemData item)
	{
		return false;
	}

	// Token: 0x0600194A RID: 6474 RVA: 0x000BCB43 File Offset: 0x000BAD43
	private void Extract()
	{
		this.m_nview.InvokeRPC("RPC_Extract", Array.Empty<object>());
	}

	// Token: 0x0600194B RID: 6475 RVA: 0x000BCB5C File Offset: 0x000BAD5C
	private void RPC_Extract(long caller)
	{
		int level = this.GetLevel();
		if (level > 0)
		{
			this.m_spawnEffect.Create(this.m_spawnPoint.position, Quaternion.identity, null, 1f, -1);
			for (int i = 0; i < level; i++)
			{
				Vector3 insideUnitSphere = UnityEngine.Random.insideUnitSphere;
				Vector3 position = this.m_spawnPoint.position + insideUnitSphere * 0.2f;
				ItemDrop component = UnityEngine.Object.Instantiate<ItemDrop>(this.m_spawnItem, position, Quaternion.identity).GetComponent<ItemDrop>();
				if (component != null)
				{
					component.SetStack(Game.instance.ScaleDrops(this.m_spawnItem.m_itemData, 1));
				}
			}
			this.ResetLevel();
			this.m_nview.InvokeRPC(ZNetView.Everybody, "RPC_UpdateEffects", Array.Empty<object>());
		}
	}

	// Token: 0x0600194C RID: 6476 RVA: 0x000BCC24 File Offset: 0x000BAE24
	private float GetTimeSinceLastUpdate()
	{
		DateTime d = new DateTime(this.m_nview.GetZDO().GetLong(ZDOVars.s_lastTime, ZNet.instance.GetTime().Ticks));
		DateTime time = ZNet.instance.GetTime();
		TimeSpan timeSpan = time - d;
		this.m_nview.GetZDO().Set(ZDOVars.s_lastTime, time.Ticks);
		double num = timeSpan.TotalSeconds;
		if (num < 0.0)
		{
			num = 0.0;
		}
		return (float)num;
	}

	// Token: 0x0600194D RID: 6477 RVA: 0x000BCCAF File Offset: 0x000BAEAF
	private void ResetLevel()
	{
		this.m_nview.GetZDO().Set(ZDOVars.s_level, 0, false);
	}

	// Token: 0x0600194E RID: 6478 RVA: 0x000BCCC8 File Offset: 0x000BAEC8
	private void IncreseLevel(int i)
	{
		int num = this.GetLevel();
		num += i;
		num = Mathf.Clamp(num, 0, this.m_maxLevel);
		this.m_nview.GetZDO().Set(ZDOVars.s_level, num, false);
	}

	// Token: 0x0600194F RID: 6479 RVA: 0x000BCD05 File Offset: 0x000BAF05
	private int GetLevel()
	{
		if (this.m_nview.GetZDO() == null)
		{
			return 0;
		}
		return this.m_nview.GetZDO().GetInt(ZDOVars.s_level, 0);
	}

	// Token: 0x06001950 RID: 6480 RVA: 0x000BCD2C File Offset: 0x000BAF2C
	private void UpdateTick()
	{
		if (this.m_mustConnectTo && !this.m_root)
		{
			Collider[] array = Physics.OverlapSphere(base.transform.position, 0.2f);
			for (int i = 0; i < array.Length; i++)
			{
				ResourceRoot componentInParent = array[i].GetComponentInParent<ResourceRoot>();
				if (componentInParent != null)
				{
					this.m_root = componentInParent;
					break;
				}
			}
		}
		if (this.m_nview.IsOwner())
		{
			float timeSinceLastUpdate = this.GetTimeSinceLastUpdate();
			if (this.GetLevel() < this.m_maxLevel && this.m_root && this.m_root.CanDrain(1f))
			{
				float num = this.m_nview.GetZDO().GetFloat(ZDOVars.s_product, 0f);
				num += timeSinceLastUpdate;
				if (num > this.m_secPerUnit)
				{
					int num2 = (int)(num / this.m_secPerUnit);
					if (this.m_root)
					{
						num2 = Mathf.Min((int)this.m_root.GetLevel(), num2);
					}
					if (num2 > 0)
					{
						this.IncreseLevel(num2);
						if (this.m_root)
						{
							this.m_root.Drain((float)num2);
						}
					}
					num = 0f;
				}
				this.m_nview.GetZDO().Set(ZDOVars.s_product, num);
			}
		}
		this.UpdateEffects();
	}

	// Token: 0x06001951 RID: 6481 RVA: 0x000BCE80 File Offset: 0x000BB080
	private void RPC_UpdateEffects(long caller)
	{
		this.UpdateEffects();
	}

	// Token: 0x06001952 RID: 6482 RVA: 0x000BCE88 File Offset: 0x000BB088
	private void UpdateEffects()
	{
		int level = this.GetLevel();
		bool active = level < this.m_maxLevel && this.m_root && this.m_root.CanDrain(1f);
		this.m_notEmptyEffect.SetActive(level > 0);
		this.m_workingEffect.SetActive(active);
	}

	// Token: 0x040019AF RID: 6575
	public string m_name = "";

	// Token: 0x040019B0 RID: 6576
	public Transform m_spawnPoint;

	// Token: 0x040019B1 RID: 6577
	public GameObject m_workingEffect;

	// Token: 0x040019B2 RID: 6578
	public GameObject m_notEmptyEffect;

	// Token: 0x040019B3 RID: 6579
	public float m_secPerUnit = 10f;

	// Token: 0x040019B4 RID: 6580
	public int m_maxLevel = 4;

	// Token: 0x040019B5 RID: 6581
	public ItemDrop m_spawnItem;

	// Token: 0x040019B6 RID: 6582
	public EffectList m_spawnEffect = new EffectList();

	// Token: 0x040019B7 RID: 6583
	public ZNetView m_mustConnectTo;

	// Token: 0x040019B8 RID: 6584
	public bool m_rayCheckConnectedBelow;

	// Token: 0x040019B9 RID: 6585
	[Header("Texts")]
	public string m_extractText = "$piece_sapcollector_extract";

	// Token: 0x040019BA RID: 6586
	public string m_drainingText = "$piece_sapcollector_draining";

	// Token: 0x040019BB RID: 6587
	public string m_drainingSlowText = "$piece_sapcollector_drainingslow";

	// Token: 0x040019BC RID: 6588
	public string m_notConnectedText = "$piece_sapcollector_notconnected";

	// Token: 0x040019BD RID: 6589
	public string m_fullText = "$piece_sapcollector_isfull";

	// Token: 0x040019BE RID: 6590
	private ZNetView m_nview;

	// Token: 0x040019BF RID: 6591
	private Collider m_collider;

	// Token: 0x040019C0 RID: 6592
	private Piece m_piece;

	// Token: 0x040019C1 RID: 6593
	private ZNetView m_connectedObject;

	// Token: 0x040019C2 RID: 6594
	private ResourceRoot m_root;
}
