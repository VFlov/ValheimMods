using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

// Token: 0x020001BC RID: 444
public class Smelter : MonoBehaviour, IHasHoverMenuExtended
{
	// Token: 0x060019E6 RID: 6630 RVA: 0x000C1228 File Offset: 0x000BF428
	private void Awake()
	{
		this.m_nview = base.GetComponent<ZNetView>();
		if (this.m_nview == null)
		{
			this.m_nview = base.GetComponentInParent<ZNetView>();
		}
		if (this.m_nview == null || this.m_nview.GetZDO() == null)
		{
			return;
		}
		if (this.m_addOreSwitch)
		{
			Switch addOreSwitch = this.m_addOreSwitch;
			addOreSwitch.m_onUse = (Switch.Callback)Delegate.Combine(addOreSwitch.m_onUse, new Switch.Callback(this.OnAddOre));
			this.m_addOreSwitch.m_onHover = new Switch.TooltipCallback(this.OnHoverAddOre);
		}
		if (this.m_addWoodSwitch)
		{
			Switch addWoodSwitch = this.m_addWoodSwitch;
			addWoodSwitch.m_onUse = (Switch.Callback)Delegate.Combine(addWoodSwitch.m_onUse, new Switch.Callback(this.OnAddFuel));
			this.m_addWoodSwitch.m_onHover = new Switch.TooltipCallback(this.OnHoverAddFuel);
		}
		if (this.m_emptyOreSwitch)
		{
			Switch emptyOreSwitch = this.m_emptyOreSwitch;
			emptyOreSwitch.m_onUse = (Switch.Callback)Delegate.Combine(emptyOreSwitch.m_onUse, new Switch.Callback(this.OnEmpty));
			Switch emptyOreSwitch2 = this.m_emptyOreSwitch;
			emptyOreSwitch2.m_onHover = (Switch.TooltipCallback)Delegate.Combine(emptyOreSwitch2.m_onHover, new Switch.TooltipCallback(this.OnHoverEmptyOre));
		}
		this.m_nview.Register<string>("RPC_AddOre", new Action<long, string>(this.RPC_AddOre));
		this.m_nview.Register("RPC_AddFuel", new Action<long>(this.RPC_AddFuel));
		this.m_nview.Register("RPC_EmptyProcessed", new Action<long>(this.RPC_EmptyProcessed));
		WearNTear component = base.GetComponent<WearNTear>();
		if (component)
		{
			WearNTear wearNTear = component;
			wearNTear.m_onDestroyed = (Action)Delegate.Combine(wearNTear.m_onDestroyed, new Action(this.OnDestroyed));
		}
		base.InvokeRepeating("UpdateSmelter", 1f, 1f);
	}

	// Token: 0x060019E7 RID: 6631 RVA: 0x000C1404 File Offset: 0x000BF604
	private void DropAllItems()
	{
		this.SpawnProcessed();
		if (this.m_fuelItem != null)
		{
			float num = (this.m_nview.GetZDO() == null) ? 0f : this.m_nview.GetZDO().GetFloat(ZDOVars.s_fuel, 0f);
			for (int i = 0; i < (int)num; i++)
			{
				Vector3 position = base.transform.position + Vector3.up + UnityEngine.Random.insideUnitSphere * 0.3f;
				Quaternion rotation = Quaternion.Euler(0f, (float)UnityEngine.Random.Range(0, 360), 0f);
				ItemDrop.OnCreateNew(UnityEngine.Object.Instantiate<GameObject>(this.m_fuelItem.gameObject, position, rotation));
			}
		}
		while (this.GetQueueSize() > 0)
		{
			string queuedOre = this.GetQueuedOre();
			this.RemoveOneOre();
			Smelter.ItemConversion itemConversion = this.GetItemConversion(queuedOre);
			if (itemConversion != null)
			{
				Vector3 position2 = base.transform.position + Vector3.up + UnityEngine.Random.insideUnitSphere * 0.3f;
				Quaternion rotation2 = Quaternion.Euler(0f, (float)UnityEngine.Random.Range(0, 360), 0f);
				ItemDrop.OnCreateNew(UnityEngine.Object.Instantiate<GameObject>(itemConversion.m_from.gameObject, position2, rotation2));
			}
		}
	}

	// Token: 0x060019E8 RID: 6632 RVA: 0x000C154D File Offset: 0x000BF74D
	private void OnDestroyed()
	{
		if (this.m_nview.IsOwner())
		{
			this.DropAllItems();
		}
	}

	// Token: 0x060019E9 RID: 6633 RVA: 0x000C1562 File Offset: 0x000BF762
	private bool IsItemAllowed(ItemDrop.ItemData item)
	{
		return this.IsItemAllowed(item.m_dropPrefab.name);
	}

	// Token: 0x060019EA RID: 6634 RVA: 0x000C1578 File Offset: 0x000BF778
	private bool IsItemAllowed(string itemName)
	{
		using (List<Smelter.ItemConversion>.Enumerator enumerator = this.m_conversion.GetEnumerator())
		{
			while (enumerator.MoveNext())
			{
				if (enumerator.Current.m_from.gameObject.name == itemName)
				{
					return true;
				}
			}
		}
		return false;
	}

	// Token: 0x060019EB RID: 6635 RVA: 0x000C15E4 File Offset: 0x000BF7E4
	private ItemDrop.ItemData FindCookableItem(Inventory inventory)
	{
		foreach (Smelter.ItemConversion itemConversion in this.m_conversion)
		{
			ItemDrop.ItemData item = inventory.GetItem(itemConversion.m_from.m_itemData.m_shared.m_name, -1, false);
			if (item != null)
			{
				return item;
			}
		}
		return null;
	}

	// Token: 0x060019EC RID: 6636 RVA: 0x000C1658 File Offset: 0x000BF858
	private bool OnAddOre(Switch sw, Humanoid user, ItemDrop.ItemData item)
	{
		if (item == null)
		{
			item = this.FindCookableItem(user.GetInventory());
			if (item == null)
			{
				user.Message(MessageHud.MessageType.Center, "$msg_noprocessableitems", 0, null);
				return false;
			}
		}
		if (!this.IsItemAllowed(item.m_dropPrefab.name))
		{
			user.Message(MessageHud.MessageType.Center, "$msg_wontwork", 0, null);
			return false;
		}
		ZLog.Log("trying to add " + item.m_shared.m_name);
		if (this.GetQueueSize() >= this.m_maxOre)
		{
			user.Message(MessageHud.MessageType.Center, "$msg_itsfull", 0, null);
			return false;
		}
		user.Message(MessageHud.MessageType.Center, "$msg_added " + item.m_shared.m_name, 0, null);
		user.GetInventory().RemoveItem(item, 1);
		this.m_nview.InvokeRPC("RPC_AddOre", new object[]
		{
			item.m_dropPrefab.name
		});
		this.m_addedOreTime = Time.time;
		if (this.m_addOreAnimationDuration > 0f)
		{
			this.SetAnimation(true);
		}
		return true;
	}

	// Token: 0x060019ED RID: 6637 RVA: 0x000C1754 File Offset: 0x000BF954
	private float GetBakeTimer()
	{
		if (!this.m_nview.IsValid())
		{
			return 0f;
		}
		return this.m_nview.GetZDO().GetFloat(ZDOVars.s_bakeTimer, 0f);
	}

	// Token: 0x060019EE RID: 6638 RVA: 0x000C1783 File Offset: 0x000BF983
	private void SetBakeTimer(float t)
	{
		if (!this.m_nview.IsValid())
		{
			return;
		}
		this.m_nview.GetZDO().Set(ZDOVars.s_bakeTimer, t);
	}

	// Token: 0x060019EF RID: 6639 RVA: 0x000C17A9 File Offset: 0x000BF9A9
	private float GetFuel()
	{
		if (!this.m_nview.IsValid())
		{
			return 0f;
		}
		return this.m_nview.GetZDO().GetFloat(ZDOVars.s_fuel, 0f);
	}

	// Token: 0x060019F0 RID: 6640 RVA: 0x000C17D8 File Offset: 0x000BF9D8
	private void SetFuel(float fuel)
	{
		if (!this.m_nview.IsValid())
		{
			return;
		}
		this.m_nview.GetZDO().Set(ZDOVars.s_fuel, fuel);
	}

	// Token: 0x060019F1 RID: 6641 RVA: 0x000C17FE File Offset: 0x000BF9FE
	private int GetQueueSize()
	{
		if (!this.m_nview.IsValid())
		{
			return 0;
		}
		return this.m_nview.GetZDO().GetInt(ZDOVars.s_queued, 0);
	}

	// Token: 0x060019F2 RID: 6642 RVA: 0x000C1828 File Offset: 0x000BFA28
	private void RPC_AddOre(long sender, string name)
	{
		if (!this.m_nview.IsOwner())
		{
			return;
		}
		if (!this.IsItemAllowed(name))
		{
			ZLog.Log("Item not allowed " + name);
			return;
		}
		this.QueueOre(name);
		this.m_oreAddedEffects.Create(base.transform.position, base.transform.rotation, null, 1f, -1);
		ZLog.Log("Added ore " + name);
	}

	// Token: 0x060019F3 RID: 6643 RVA: 0x000C18A0 File Offset: 0x000BFAA0
	private void QueueOre(string name)
	{
		int queueSize = this.GetQueueSize();
		this.m_nview.GetZDO().Set("item" + queueSize.ToString(), name);
		this.m_nview.GetZDO().Set(ZDOVars.s_queued, queueSize + 1, false);
	}

	// Token: 0x060019F4 RID: 6644 RVA: 0x000C18EF File Offset: 0x000BFAEF
	private string GetQueuedOre()
	{
		if (this.GetQueueSize() == 0)
		{
			return "";
		}
		return this.m_nview.GetZDO().GetString(ZDOVars.s_item0, "");
	}

	// Token: 0x060019F5 RID: 6645 RVA: 0x000C191C File Offset: 0x000BFB1C
	private void RemoveOneOre()
	{
		int queueSize = this.GetQueueSize();
		if (queueSize == 0)
		{
			return;
		}
		for (int i = 0; i < queueSize; i++)
		{
			string @string = this.m_nview.GetZDO().GetString("item" + (i + 1).ToString(), "");
			this.m_nview.GetZDO().Set("item" + i.ToString(), @string);
		}
		this.m_nview.GetZDO().Set(ZDOVars.s_queued, queueSize - 1, false);
	}

	// Token: 0x060019F6 RID: 6646 RVA: 0x000C19A6 File Offset: 0x000BFBA6
	private bool OnEmpty(Switch sw, Humanoid user, ItemDrop.ItemData item)
	{
		if (this.GetProcessedQueueSize() <= 0)
		{
			return false;
		}
		this.m_nview.InvokeRPC("RPC_EmptyProcessed", Array.Empty<object>());
		return true;
	}

	// Token: 0x060019F7 RID: 6647 RVA: 0x000C19C9 File Offset: 0x000BFBC9
	private void RPC_EmptyProcessed(long sender)
	{
		if (!this.m_nview.IsOwner())
		{
			return;
		}
		this.SpawnProcessed();
	}

	// Token: 0x060019F8 RID: 6648 RVA: 0x000C19E0 File Offset: 0x000BFBE0
	private bool OnAddFuel(Switch sw, Humanoid user, ItemDrop.ItemData item)
	{
		if (item != null && item.m_shared.m_name != this.m_fuelItem.m_itemData.m_shared.m_name)
		{
			user.Message(MessageHud.MessageType.Center, "$msg_wrongitem", 0, null);
			return false;
		}
		if (this.GetFuel() > (float)(this.m_maxFuel - 1))
		{
			user.Message(MessageHud.MessageType.Center, "$msg_itsfull", 0, null);
			return false;
		}
		if (!user.GetInventory().HaveItem(this.m_fuelItem.m_itemData.m_shared.m_name, true))
		{
			user.Message(MessageHud.MessageType.Center, "$msg_donthaveany " + this.m_fuelItem.m_itemData.m_shared.m_name, 0, null);
			return false;
		}
		user.Message(MessageHud.MessageType.Center, "$msg_added " + this.m_fuelItem.m_itemData.m_shared.m_name, 0, null);
		user.GetInventory().RemoveItem(this.m_fuelItem.m_itemData.m_shared.m_name, 1, -1, true);
		this.m_nview.InvokeRPC("RPC_AddFuel", Array.Empty<object>());
		return true;
	}

	// Token: 0x060019F9 RID: 6649 RVA: 0x000C1AF8 File Offset: 0x000BFCF8
	private void RPC_AddFuel(long sender)
	{
		if (!this.m_nview.IsOwner())
		{
			return;
		}
		float fuel = this.GetFuel();
		this.SetFuel(fuel + 1f);
		this.m_fuelAddedEffects.Create(base.transform.position, base.transform.rotation, base.transform, 1f, -1);
	}

	// Token: 0x060019FA RID: 6650 RVA: 0x000C1B58 File Offset: 0x000BFD58
	private double GetDeltaTime()
	{
		DateTime time = ZNet.instance.GetTime();
		DateTime d = new DateTime(this.m_nview.GetZDO().GetLong(ZDOVars.s_startTime, time.Ticks));
		double totalSeconds = (time - d).TotalSeconds;
		this.m_nview.GetZDO().Set(ZDOVars.s_startTime, time.Ticks);
		return totalSeconds;
	}

	// Token: 0x060019FB RID: 6651 RVA: 0x000C1BBE File Offset: 0x000BFDBE
	private float GetAccumulator()
	{
		if (!this.m_nview.IsValid())
		{
			return 0f;
		}
		return this.m_nview.GetZDO().GetFloat(ZDOVars.s_accTime, 0f);
	}

	// Token: 0x060019FC RID: 6652 RVA: 0x000C1BED File Offset: 0x000BFDED
	private void SetAccumulator(float t)
	{
		if (!this.m_nview.IsValid())
		{
			return;
		}
		this.m_nview.GetZDO().Set(ZDOVars.s_accTime, t);
	}

	// Token: 0x060019FD RID: 6653 RVA: 0x000C1C13 File Offset: 0x000BFE13
	private void UpdateRoof()
	{
		if (this.m_requiresRoof)
		{
			this.m_haveRoof = Cover.IsUnderRoof(this.m_roofCheckPoint.position);
		}
	}

	// Token: 0x060019FE RID: 6654 RVA: 0x000C1C33 File Offset: 0x000BFE33
	private void UpdateSmoke()
	{
		if (this.m_smokeSpawner != null)
		{
			this.m_blockedSmoke = this.m_smokeSpawner.IsBlocked();
			return;
		}
		this.m_blockedSmoke = false;
	}

	// Token: 0x060019FF RID: 6655 RVA: 0x000C1C5C File Offset: 0x000BFE5C
	private void UpdateSmelter()
	{
		if (!this.m_nview.IsValid())
		{
			return;
		}
		this.UpdateRoof();
		this.UpdateSmoke();
		this.UpdateState();
		if (!this.m_nview.IsOwner())
		{
			return;
		}
		double deltaTime = this.GetDeltaTime();
		float num = this.GetAccumulator();
		num += (float)deltaTime;
		if (num > 3600f)
		{
			num = 3600f;
		}
		float num2 = this.m_windmill ? this.m_windmill.GetPowerOutput() : 1f;
		while (num >= 1f)
		{
			num -= 1f;
			float num3 = this.GetFuel();
			string queuedOre = this.GetQueuedOre();
			if ((this.m_maxFuel == 0 || num3 > 0f) && (this.m_maxOre == 0 || queuedOre != "") && this.m_secPerProduct > 0f && (!this.m_requiresRoof || this.m_haveRoof) && !this.m_blockedSmoke)
			{
				float num4 = 1f * num2;
				if (this.m_maxFuel > 0)
				{
					float num5 = this.m_secPerProduct / (float)this.m_fuelPerProduct;
					num3 -= num4 / num5;
					if (num3 < 0.0001f)
					{
						num3 = 0f;
					}
					this.SetFuel(num3);
				}
				if (queuedOre != "")
				{
					float num6 = this.GetBakeTimer();
					num6 += num4;
					this.SetBakeTimer(num6);
					if (num6 >= this.m_secPerProduct)
					{
						this.SetBakeTimer(0f);
						this.RemoveOneOre();
						this.QueueProcessed(queuedOre);
					}
				}
			}
		}
		if (this.GetQueuedOre() == "" || ((float)this.m_maxFuel > 0f && this.GetFuel() == 0f))
		{
			this.SpawnProcessed();
		}
		this.SetAccumulator(num);
	}

	// Token: 0x06001A00 RID: 6656 RVA: 0x000C1E1C File Offset: 0x000C001C
	private void QueueProcessed(string ore)
	{
		if (!this.m_spawnStack)
		{
			this.Spawn(ore, 1);
			return;
		}
		if (!this.m_nview.IsValid())
		{
			return;
		}
		string @string = this.m_nview.GetZDO().GetString(ZDOVars.s_spawnOre, "");
		int num = this.m_nview.GetZDO().GetInt(ZDOVars.s_spawnAmount, 0);
		if (@string.Length <= 0)
		{
			this.m_nview.GetZDO().Set(ZDOVars.s_spawnOre, ore);
			this.m_nview.GetZDO().Set(ZDOVars.s_spawnAmount, 1, false);
			return;
		}
		if (@string != ore)
		{
			this.SpawnProcessed();
			this.m_nview.GetZDO().Set(ZDOVars.s_spawnOre, ore);
			this.m_nview.GetZDO().Set(ZDOVars.s_spawnAmount, 1, false);
			return;
		}
		num++;
		Smelter.ItemConversion itemConversion = this.GetItemConversion(ore);
		if (itemConversion == null || num >= itemConversion.m_to.m_itemData.m_shared.m_maxStackSize)
		{
			this.Spawn(ore, num);
			this.m_nview.GetZDO().Set(ZDOVars.s_spawnOre, "");
			this.m_nview.GetZDO().Set(ZDOVars.s_spawnAmount, 0, false);
			return;
		}
		this.m_nview.GetZDO().Set(ZDOVars.s_spawnAmount, num, false);
	}

	// Token: 0x06001A01 RID: 6657 RVA: 0x000C1F6C File Offset: 0x000C016C
	private void SpawnProcessed()
	{
		if (!this.m_nview.IsValid())
		{
			return;
		}
		int @int = this.m_nview.GetZDO().GetInt(ZDOVars.s_spawnAmount, 0);
		if (@int > 0)
		{
			string @string = this.m_nview.GetZDO().GetString(ZDOVars.s_spawnOre, "");
			this.Spawn(@string, @int);
			this.m_nview.GetZDO().Set(ZDOVars.s_spawnOre, "");
			this.m_nview.GetZDO().Set(ZDOVars.s_spawnAmount, 0, false);
		}
	}

	// Token: 0x06001A02 RID: 6658 RVA: 0x000C1FF6 File Offset: 0x000C01F6
	private int GetProcessedQueueSize()
	{
		if (!this.m_nview.IsValid())
		{
			return 0;
		}
		return this.m_nview.GetZDO().GetInt(ZDOVars.s_spawnAmount, 0);
	}

	// Token: 0x06001A03 RID: 6659 RVA: 0x000C2020 File Offset: 0x000C0220
	private void Spawn(string ore, int stack)
	{
		Smelter.ItemConversion itemConversion = this.GetItemConversion(ore);
		if (itemConversion != null && itemConversion.m_to != null)
		{
			this.m_produceEffects.Create(base.transform.position, base.transform.rotation, null, 1f, -1);
			ItemDrop component = UnityEngine.Object.Instantiate<GameObject>(itemConversion.m_to.gameObject, this.m_outputPoint.position, this.m_outputPoint.rotation).GetComponent<ItemDrop>();
			component.m_itemData.m_stack = stack;
			ItemDrop.OnCreateNew(component);
		}
	}

	// Token: 0x06001A04 RID: 6660 RVA: 0x000C20AC File Offset: 0x000C02AC
	private Smelter.ItemConversion GetItemConversion(string itemName)
	{
		foreach (Smelter.ItemConversion itemConversion in this.m_conversion)
		{
			if (itemConversion.m_from.gameObject.name == itemName)
			{
				return itemConversion;
			}
		}
		return null;
	}

	// Token: 0x06001A05 RID: 6661 RVA: 0x000C2118 File Offset: 0x000C0318
	private void UpdateState()
	{
		bool flag = this.IsActive();
		this.m_enabledObject.SetActive(flag);
		if (this.m_disabledObject)
		{
			this.m_disabledObject.SetActive(!flag);
		}
		if (this.m_haveFuelObject)
		{
			this.m_haveFuelObject.SetActive(this.GetFuel() > 0f);
		}
		if (this.m_haveOreObject)
		{
			this.m_haveOreObject.SetActive(this.GetQueueSize() > 0);
		}
		if (this.m_noOreObject)
		{
			this.m_noOreObject.SetActive(this.GetQueueSize() == 0);
		}
		if (this.m_addOreAnimationDuration > 0f && Time.time - this.m_addedOreTime < this.m_addOreAnimationDuration)
		{
			flag = true;
		}
		this.SetAnimation(flag);
	}

	// Token: 0x06001A06 RID: 6662 RVA: 0x000C21E8 File Offset: 0x000C03E8
	private void SetAnimation(bool active)
	{
		foreach (Animator animator in this.m_animators)
		{
			if (animator.gameObject.activeInHierarchy)
			{
				animator.SetBool("active", active);
				animator.SetFloat("activef", active ? 1f : 0f);
			}
		}
	}

	// Token: 0x06001A07 RID: 6663 RVA: 0x000C2244 File Offset: 0x000C0444
	public bool IsActive()
	{
		return (this.m_maxFuel == 0 || this.GetFuel() > 0f) && (this.m_maxOre == 0 || this.GetQueueSize() > 0) && (!this.m_requiresRoof || this.m_haveRoof) && !this.m_blockedSmoke;
	}

	// Token: 0x06001A08 RID: 6664 RVA: 0x000C2294 File Offset: 0x000C0494
	private string OnHoverAddFuel()
	{
		float fuel = this.GetFuel();
		return Localization.instance.Localize(string.Format("{0} ({1} {2}/{3})\n[<color=yellow><b>$KEY_Use</b></color>] $piece_smelter_add {4}", new object[]
		{
			this.m_name,
			this.m_fuelItem.m_itemData.m_shared.m_name,
			Mathf.Ceil(fuel),
			this.m_maxFuel,
			this.m_fuelItem.m_itemData.m_shared.m_name
		}));
	}

	// Token: 0x06001A09 RID: 6665 RVA: 0x000C2318 File Offset: 0x000C0518
	private string OnHoverEmptyOre()
	{
		int processedQueueSize = this.GetProcessedQueueSize();
		return Localization.instance.Localize(string.Format("{0} ({1} $piece_smelter_ready) \n[<color=yellow><b>$KEY_Use</b></color>] {2}", this.m_name, processedQueueSize, this.m_emptyOreTooltip));
	}

	// Token: 0x06001A0A RID: 6666 RVA: 0x000C2354 File Offset: 0x000C0554
	private string OnHoverAddOre()
	{
		this.m_sb.Clear();
		int queueSize = this.GetQueueSize();
		this.m_sb.Append(string.Format("{0} ({1}/{2}) ", this.m_name, queueSize, this.m_maxOre));
		if (this.m_requiresRoof && !this.m_haveRoof && Mathf.Sin(Time.time * 10f) > 0f)
		{
			this.m_sb.Append(" <color=yellow>$piece_smelter_reqroof</color>");
		}
		this.m_sb.Append("\n[<color=yellow><b>$KEY_Use</b></color>] " + this.m_addOreTooltip);
		return Localization.instance.Localize(this.m_sb.ToString());
	}

	// Token: 0x06001A0B RID: 6667 RVA: 0x000C240C File Offset: 0x000C060C
	public bool TryGetItems(Player player, Switch switchRef, out List<string> items)
	{
		items = new List<string>();
		if (!this.CanUseItems(player, switchRef, true))
		{
			return true;
		}
		if (switchRef == this.m_addOreSwitch)
		{
			items = (from conversion in this.m_conversion
			select conversion.m_from.m_itemData.m_shared.m_name).ToList<string>();
		}
		else if (switchRef == this.m_addWoodSwitch)
		{
			items.Add(this.m_fuelItem.m_itemData.m_shared.m_name);
		}
		return true;
	}

	// Token: 0x06001A0C RID: 6668 RVA: 0x000C249C File Offset: 0x000C069C
	public bool CanUseItems(Player player, Switch switchRef, bool sendErrorMessage = true)
	{
		if (switchRef == this.m_emptyOreSwitch)
		{
			return false;
		}
		if (switchRef == this.m_addOreSwitch)
		{
			if (this.GetQueueSize() >= this.m_maxOre)
			{
				if (sendErrorMessage)
				{
					player.Message(MessageHud.MessageType.Center, "$msg_itsfull", 0, null);
				}
				return false;
			}
			if (player.GetInventory().CountItemsByName((from conversion in this.m_conversion
			select conversion.m_from.m_itemData.m_shared.m_name).ToArray<string>(), -1, true, false) > 0)
			{
				return true;
			}
			if (sendErrorMessage)
			{
				player.Message(MessageHud.MessageType.Center, "$msg_noprocessableitems", 0, null);
			}
			return false;
		}
		else
		{
			if (switchRef != this.m_addWoodSwitch)
			{
				return false;
			}
			if (this.GetFuel() > (float)(this.m_maxFuel - 1))
			{
				if (sendErrorMessage)
				{
					player.Message(MessageHud.MessageType.Center, "$msg_itsfull", 0, null);
				}
				return false;
			}
			if (player.GetInventory().HaveItem(this.m_fuelItem.m_itemData.m_shared.m_name, true))
			{
				return true;
			}
			if (sendErrorMessage)
			{
				player.Message(MessageHud.MessageType.Center, "$msg_donthaveany " + this.m_fuelItem.m_itemData.m_shared.m_name, 0, null);
			}
			return false;
		}
	}

	// Token: 0x04001A70 RID: 6768
	public string m_name = "Smelter";

	// Token: 0x04001A71 RID: 6769
	public string m_addOreTooltip = "$piece_smelter_additem";

	// Token: 0x04001A72 RID: 6770
	public string m_emptyOreTooltip = "$piece_smelter_empty";

	// Token: 0x04001A73 RID: 6771
	public Switch m_addWoodSwitch;

	// Token: 0x04001A74 RID: 6772
	public Switch m_addOreSwitch;

	// Token: 0x04001A75 RID: 6773
	public Switch m_emptyOreSwitch;

	// Token: 0x04001A76 RID: 6774
	public Transform m_outputPoint;

	// Token: 0x04001A77 RID: 6775
	public Transform m_roofCheckPoint;

	// Token: 0x04001A78 RID: 6776
	public GameObject m_enabledObject;

	// Token: 0x04001A79 RID: 6777
	public GameObject m_disabledObject;

	// Token: 0x04001A7A RID: 6778
	public GameObject m_haveFuelObject;

	// Token: 0x04001A7B RID: 6779
	public GameObject m_haveOreObject;

	// Token: 0x04001A7C RID: 6780
	public GameObject m_noOreObject;

	// Token: 0x04001A7D RID: 6781
	public Animator[] m_animators;

	// Token: 0x04001A7E RID: 6782
	public ItemDrop m_fuelItem;

	// Token: 0x04001A7F RID: 6783
	public int m_maxOre = 10;

	// Token: 0x04001A80 RID: 6784
	public int m_maxFuel = 10;

	// Token: 0x04001A81 RID: 6785
	public int m_fuelPerProduct = 4;

	// Token: 0x04001A82 RID: 6786
	public float m_secPerProduct = 10f;

	// Token: 0x04001A83 RID: 6787
	public bool m_spawnStack;

	// Token: 0x04001A84 RID: 6788
	public bool m_requiresRoof;

	// Token: 0x04001A85 RID: 6789
	public Windmill m_windmill;

	// Token: 0x04001A86 RID: 6790
	public SmokeSpawner m_smokeSpawner;

	// Token: 0x04001A87 RID: 6791
	public float m_addOreAnimationDuration;

	// Token: 0x04001A88 RID: 6792
	public List<Smelter.ItemConversion> m_conversion = new List<Smelter.ItemConversion>();

	// Token: 0x04001A89 RID: 6793
	public EffectList m_oreAddedEffects = new EffectList();

	// Token: 0x04001A8A RID: 6794
	public EffectList m_fuelAddedEffects = new EffectList();

	// Token: 0x04001A8B RID: 6795
	public EffectList m_produceEffects = new EffectList();

	// Token: 0x04001A8C RID: 6796
	private ZNetView m_nview;

	// Token: 0x04001A8D RID: 6797
	private bool m_haveRoof;

	// Token: 0x04001A8E RID: 6798
	private bool m_blockedSmoke;

	// Token: 0x04001A8F RID: 6799
	private float m_addedOreTime = -1000f;

	// Token: 0x04001A90 RID: 6800
	private StringBuilder m_sb = new StringBuilder();

	// Token: 0x02000384 RID: 900
	[Serializable]
	public class ItemConversion
	{
		// Token: 0x0400267A RID: 9850
		public ItemDrop m_from;

		// Token: 0x0400267B RID: 9851
		public ItemDrop m_to;
	}
}
