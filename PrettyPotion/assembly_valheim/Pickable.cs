using System;
using UnityEngine;

// Token: 0x020000B1 RID: 177
public class Pickable : MonoBehaviour, Hoverable, Interactable
{
	// Token: 0x06000B3F RID: 2879 RVA: 0x0005F61C File Offset: 0x0005D81C
	private void Awake()
	{
		this.m_nview = base.GetComponent<ZNetView>();
		ZDO zdo = this.m_nview.GetZDO();
		if (zdo == null)
		{
			return;
		}
		this.m_nview.Register<bool>("RPC_SetPicked", new Action<long, bool>(this.RPC_SetPicked));
		this.m_nview.Register<int>("RPC_Pick", new Action<long, int>(this.RPC_Pick));
		this.m_picked = zdo.GetBool(ZDOVars.s_picked, this.m_defaultPicked);
		this.m_pickedTime = this.m_nview.GetZDO().GetLong(ZDOVars.s_pickedTime, 0L);
		if (this.m_enabled == 2)
		{
			this.m_enabled = (zdo.GetBool(ZDOVars.s_enabled, true) ? 1 : 0);
		}
		else if (this.m_nview.IsOwner())
		{
			zdo.Set(ZDOVars.s_enabled, this.m_enabled == 1);
		}
		if (this.m_hideWhenPicked)
		{
			this.m_hideWhenPicked.SetActive(!this.m_picked && this.m_enabled == 1);
		}
		float repeatRate = 60f;
		if (this.m_respawnTimeMinutes > 0f)
		{
			base.InvokeRepeating("UpdateRespawn", UnityEngine.Random.Range(1f, 5f), repeatRate);
		}
		if (this.m_respawnTimeMinutes <= 0f && this.m_hideWhenPicked == null && this.m_nview.GetZDO().GetBool(ZDOVars.s_picked, false))
		{
			this.m_nview.ClaimOwnership();
			this.m_nview.Destroy();
			ZLog.Log("Destroying old picked " + base.name);
		}
	}

	// Token: 0x06000B40 RID: 2880 RVA: 0x0005F7AD File Offset: 0x0005D9AD
	public string GetHoverText()
	{
		if (this.m_picked || this.m_enabled == 0)
		{
			return "";
		}
		return Localization.instance.Localize(this.GetHoverName() + "\n[<color=yellow><b>$KEY_Use</b></color>] $inventory_pickup");
	}

	// Token: 0x06000B41 RID: 2881 RVA: 0x0005F7DF File Offset: 0x0005D9DF
	public string GetHoverName()
	{
		if (!string.IsNullOrEmpty(this.m_overrideName))
		{
			return this.m_overrideName;
		}
		return this.m_itemPrefab.GetComponent<ItemDrop>().m_itemData.m_shared.m_name;
	}

	// Token: 0x06000B42 RID: 2882 RVA: 0x0005F810 File Offset: 0x0005DA10
	private void UpdateRespawn()
	{
		if (!this.m_nview.IsValid() || !this.m_nview.IsOwner())
		{
			return;
		}
		if (!this.m_picked)
		{
			return;
		}
		if (this.m_pickedTime == 0L)
		{
			this.m_pickedTime = ZNet.instance.GetTime().Ticks - TimeSpan.FromMinutes((double)UnityEngine.Random.Range(this.m_respawnTimeInitMin * 100f, this.m_respawnTimeInitMax * 100f)).Ticks;
			if (this.m_pickedTime < 1L)
			{
				this.m_pickedTime = 1L;
			}
			this.m_nview.GetZDO().Set(ZDOVars.s_pickedTime, this.m_pickedTime);
		}
		if (this.m_enabled == 0)
		{
			if (this.m_hideWhenPicked)
			{
				this.m_hideWhenPicked.SetActive(false);
			}
			return;
		}
		if (this.ShouldRespawn())
		{
			this.m_nview.InvokeRPC(ZNetView.Everybody, "RPC_SetPicked", new object[]
			{
				false
			});
		}
	}

	// Token: 0x06000B43 RID: 2883 RVA: 0x0005F908 File Offset: 0x0005DB08
	private bool ShouldRespawn()
	{
		if (!this.m_nview)
		{
			return false;
		}
		long @long = this.m_nview.GetZDO().GetLong(ZDOVars.s_pickedTime, 0L);
		DateTime d = new DateTime(@long);
		TimeSpan timeSpan = ZNet.instance.GetTime() - d;
		return (@long <= 1L || timeSpan.TotalMinutes > (double)this.m_respawnTimeMinutes) && (this.m_spawnCheck == null || this.m_spawnCheck(this));
	}

	// Token: 0x06000B44 RID: 2884 RVA: 0x0005F984 File Offset: 0x0005DB84
	public bool Interact(Humanoid character, bool repeat, bool alt)
	{
		if (!this.m_nview.IsValid() || this.m_enabled == 0)
		{
			return false;
		}
		if (this.m_tarPreventsPicking)
		{
			if (this.m_floating == null)
			{
				this.m_floating = base.GetComponent<Floating>();
			}
			if (this.m_floating && this.m_floating.IsInTar())
			{
				character.Message(MessageHud.MessageType.Center, "$hud_itemstucktar", 0, null);
				return this.m_useInteractAnimation;
			}
		}
		int num = 0;
		if (!this.m_picked && this.m_pickRaiseSkill != Skills.SkillType.None)
		{
			Player player = character as Player;
			if (player != null)
			{
				player.RaiseSkill(this.m_pickRaiseSkill, 1f);
				float skillFactor = player.GetSkillFactor(this.m_pickRaiseSkill);
				if (UnityEngine.Random.value < skillFactor * this.m_maxLevelBonusChance)
				{
					num = this.m_bonusYieldAmount;
					DamageText.instance.ShowText(DamageText.TextType.Bonus, base.transform.position + Vector3.up * this.m_spawnOffset, string.Format("+{0}", num), true);
					this.m_bonusEffect.Create(base.transform.position, Quaternion.identity, null, 1f, -1);
					ZLog.Log("Bonus food picked!");
				}
			}
		}
		this.m_nview.InvokeRPC("RPC_Pick", new object[]
		{
			num
		});
		return this.m_useInteractAnimation;
	}

	// Token: 0x06000B45 RID: 2885 RVA: 0x0005FAE0 File Offset: 0x0005DCE0
	private void RPC_Pick(long sender, int bonus)
	{
		if (!this.m_nview.IsOwner())
		{
			return;
		}
		if (this.m_picked)
		{
			return;
		}
		Vector3 basePos = this.m_pickEffectAtSpawnPoint ? (base.transform.position + Vector3.up * this.m_spawnOffset) : base.transform.position;
		this.m_pickEffector.Create(basePos, Quaternion.identity, null, 1f, -1);
		int num = this.m_dontScale ? this.m_amount : Mathf.Max(this.m_minAmountScaled, Game.instance.ScaleDrops(this.m_itemPrefab, this.m_amount));
		num += bonus;
		int num2 = 0;
		for (int i = 0; i < num; i++)
		{
			this.Drop(this.m_itemPrefab, num2++, 1);
		}
		if (!this.m_extraDrops.IsEmpty())
		{
			foreach (ItemDrop.ItemData itemData in this.m_extraDrops.GetDropListItems())
			{
				this.Drop(itemData.m_dropPrefab, num2++, itemData.m_stack);
			}
		}
		if (this.m_aggravateRange > 0f)
		{
			BaseAI.AggravateAllInArea(base.transform.position, this.m_aggravateRange, BaseAI.AggravatedReason.Theif);
		}
		this.m_nview.InvokeRPC(ZNetView.Everybody, "RPC_SetPicked", new object[]
		{
			true
		});
	}

	// Token: 0x06000B46 RID: 2886 RVA: 0x0005FC60 File Offset: 0x0005DE60
	private void RPC_SetPicked(long sender, bool picked)
	{
		this.SetPicked(picked);
	}

	// Token: 0x06000B47 RID: 2887 RVA: 0x0005FC6C File Offset: 0x0005DE6C
	public void SetPicked(bool picked)
	{
		this.m_picked = picked;
		if (this.m_hideWhenPicked)
		{
			this.m_hideWhenPicked.SetActive(!picked);
		}
		if (this.m_nview && this.m_nview.IsOwner())
		{
			if (this.m_respawnTimeMinutes > 0f || this.m_hideWhenPicked != null)
			{
				this.m_nview.GetZDO().Set(ZDOVars.s_picked, this.m_picked);
				if (picked && this.m_respawnTimeMinutes > 0f)
				{
					DateTime time = ZNet.instance.GetTime();
					this.m_nview.GetZDO().Set(ZDOVars.s_pickedTime, time.Ticks);
					return;
				}
			}
			else if (picked)
			{
				this.m_nview.Destroy();
			}
		}
	}

	// Token: 0x06000B48 RID: 2888 RVA: 0x0005FD35 File Offset: 0x0005DF35
	public bool GetPicked()
	{
		return this.m_picked;
	}

	// Token: 0x06000B49 RID: 2889 RVA: 0x0005FD3D File Offset: 0x0005DF3D
	public void SetEnabled(bool value)
	{
		this.SetEnabled(value ? 1 : 0);
	}

	// Token: 0x06000B4A RID: 2890 RVA: 0x0005FD4C File Offset: 0x0005DF4C
	public void SetEnabled(int value)
	{
		this.m_enabled = value;
		if (this.m_nview && this.m_nview.IsOwner() && this.m_nview.GetZDO() != null)
		{
			this.m_nview.GetZDO().Set(ZDOVars.s_enabled, base.enabled);
		}
		if (this.m_hideWhenPicked)
		{
			this.m_hideWhenPicked.SetActive(base.enabled && this.ShouldRespawn());
		}
	}

	// Token: 0x17000050 RID: 80
	// (get) Token: 0x06000B4B RID: 2891 RVA: 0x0005FDCB File Offset: 0x0005DFCB
	public int GetEnabled
	{
		get
		{
			return this.m_enabled;
		}
	}

	// Token: 0x06000B4C RID: 2892 RVA: 0x0005FDD3 File Offset: 0x0005DFD3
	public bool CanBePicked()
	{
		return (this.m_hideWhenPicked && this.m_hideWhenPicked.activeInHierarchy) || (!this.m_picked && this.m_enabled == 1);
	}

	// Token: 0x06000B4D RID: 2893 RVA: 0x0005FE04 File Offset: 0x0005E004
	private void Drop(GameObject prefab, int offset, int stack)
	{
		Vector2 vector = UnityEngine.Random.insideUnitCircle * 0.2f;
		Vector3 position = base.transform.position + Vector3.up * this.m_spawnOffset + new Vector3(vector.x, 0.5f * (float)offset, vector.y);
		Quaternion rotation = Quaternion.Euler(0f, (float)UnityEngine.Random.Range(0, 360), 0f);
		GameObject gameObject = UnityEngine.Object.Instantiate<GameObject>(prefab, position, rotation);
		ItemDrop component = gameObject.GetComponent<ItemDrop>();
		if (component != null)
		{
			component.SetStack(stack);
			ItemDrop.OnCreateNew(component);
		}
		gameObject.GetComponent<Rigidbody>().velocity = Vector3.up * 4f;
	}

	// Token: 0x06000B4E RID: 2894 RVA: 0x0005FEB4 File Offset: 0x0005E0B4
	public bool UseItem(Humanoid user, ItemDrop.ItemData item)
	{
		return false;
	}

	// Token: 0x04000C7F RID: 3199
	public GameObject m_hideWhenPicked;

	// Token: 0x04000C80 RID: 3200
	public GameObject m_itemPrefab;

	// Token: 0x04000C81 RID: 3201
	public int m_amount = 1;

	// Token: 0x04000C82 RID: 3202
	public int m_minAmountScaled = 1;

	// Token: 0x04000C83 RID: 3203
	public bool m_dontScale;

	// Token: 0x04000C84 RID: 3204
	public DropTable m_extraDrops = new DropTable();

	// Token: 0x04000C85 RID: 3205
	public string m_overrideName = "";

	// Token: 0x04000C86 RID: 3206
	public float m_respawnTimeMinutes;

	// Token: 0x04000C87 RID: 3207
	public float m_respawnTimeInitMin;

	// Token: 0x04000C88 RID: 3208
	public float m_respawnTimeInitMax;

	// Token: 0x04000C89 RID: 3209
	public float m_spawnOffset = 0.5f;

	// Token: 0x04000C8A RID: 3210
	public EffectList m_pickEffector = new EffectList();

	// Token: 0x04000C8B RID: 3211
	public bool m_pickEffectAtSpawnPoint;

	// Token: 0x04000C8C RID: 3212
	public bool m_useInteractAnimation;

	// Token: 0x04000C8D RID: 3213
	public bool m_tarPreventsPicking;

	// Token: 0x04000C8E RID: 3214
	public float m_aggravateRange;

	// Token: 0x04000C8F RID: 3215
	public bool m_defaultPicked;

	// Token: 0x04000C90 RID: 3216
	public bool m_defaultEnabled = true;

	// Token: 0x04000C91 RID: 3217
	public Pickable.SpawnCheck m_spawnCheck;

	// Token: 0x04000C92 RID: 3218
	public bool m_harvestable;

	// Token: 0x04000C93 RID: 3219
	public Skills.SkillType m_pickRaiseSkill;

	// Token: 0x04000C94 RID: 3220
	public float m_maxLevelBonusChance = 0.25f;

	// Token: 0x04000C95 RID: 3221
	public int m_bonusYieldAmount = 1;

	// Token: 0x04000C96 RID: 3222
	public EffectList m_bonusEffect;

	// Token: 0x04000C97 RID: 3223
	private ZNetView m_nview;

	// Token: 0x04000C98 RID: 3224
	private Floating m_floating;

	// Token: 0x04000C99 RID: 3225
	private bool m_picked;

	// Token: 0x04000C9A RID: 3226
	private int m_enabled = 2;

	// Token: 0x04000C9B RID: 3227
	private long m_pickedTime;

	// Token: 0x020002D5 RID: 725
	// (Invoke) Token: 0x06002155 RID: 8533
	public delegate bool SpawnCheck(Pickable p);
}
