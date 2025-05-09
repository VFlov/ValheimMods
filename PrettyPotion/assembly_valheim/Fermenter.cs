using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;

// Token: 0x02000177 RID: 375
public class Fermenter : MonoBehaviour, Hoverable, Interactable
{
	// Token: 0x0600167F RID: 5759 RVA: 0x000A636C File Offset: 0x000A456C
	private void Awake()
	{
		this.m_nview = base.GetComponent<ZNetView>();
		this.m_fermentingObject.SetActive(false);
		this.m_readyObject.SetActive(false);
		this.m_topObject.SetActive(true);
		if (this.m_nview == null || this.m_nview.GetZDO() == null)
		{
			return;
		}
		this.m_nview.Register<string>("RPC_AddItem", new Action<long, string>(this.RPC_AddItem));
		this.m_nview.Register("RPC_Tap", new Action<long>(this.RPC_Tap));
		if (this.GetStatus() == Fermenter.Status.Fermenting)
		{
			base.InvokeRepeating("SlowUpdate", 2f, 2f);
		}
		else
		{
			base.InvokeRepeating("SlowUpdate", 0f, 2f);
		}
		WearNTear component = base.GetComponent<WearNTear>();
		if (component)
		{
			WearNTear wearNTear = component;
			wearNTear.m_onDestroyed = (Action)Delegate.Combine(wearNTear.m_onDestroyed, new Action(this.OnDestroyed));
		}
	}

	// Token: 0x06001680 RID: 5760 RVA: 0x000A6464 File Offset: 0x000A4664
	private void DropAllItems()
	{
		Fermenter.Status status = this.GetStatus();
		string content = this.GetContent();
		if (!string.IsNullOrEmpty(content))
		{
			if (status == Fermenter.Status.Ready)
			{
				Fermenter.ItemConversion itemConversion = this.GetItemConversion(content);
				if (itemConversion != null)
				{
					for (int i = 0; i < itemConversion.m_producedItems; i++)
					{
						this.<DropAllItems>g__drop|2_0(itemConversion.m_to);
					}
				}
			}
			else
			{
				GameObject prefab = ZNetScene.instance.GetPrefab(content);
				if (prefab != null)
				{
					ItemDrop component = prefab.GetComponent<ItemDrop>();
					if (component)
					{
						this.<DropAllItems>g__drop|2_0(component);
					}
				}
			}
			this.m_nview.GetZDO().Set(ZDOVars.s_content, "");
			this.m_nview.GetZDO().Set(ZDOVars.s_startTime, 0, false);
		}
	}

	// Token: 0x06001681 RID: 5761 RVA: 0x000A651A File Offset: 0x000A471A
	private void OnDestroyed()
	{
		this.m_nview.IsOwner();
	}

	// Token: 0x06001682 RID: 5762 RVA: 0x000A6528 File Offset: 0x000A4728
	public string GetHoverName()
	{
		return this.m_name;
	}

	// Token: 0x06001683 RID: 5763 RVA: 0x000A6530 File Offset: 0x000A4730
	public string GetHoverText()
	{
		if (!PrivateArea.CheckAccess(base.transform.position, 0f, false, false))
		{
			return Localization.instance.Localize(this.m_name + "\n$piece_noaccess");
		}
		switch (this.GetStatus())
		{
		case Fermenter.Status.Empty:
		{
			string text = "$piece_container_empty";
			if (!this.m_hasRoof)
			{
				text += ", $piece_fermenter_needroof";
			}
			else if (this.m_exposed)
			{
				text += ", $piece_fermenter_exposed";
			}
			return Localization.instance.Localize(this.m_name + " ( " + text + " )\n[<color=yellow><b>$KEY_Use</b></color>] $piece_fermenter_add");
		}
		case Fermenter.Status.Fermenting:
		{
			string contentName = this.GetContentName();
			if (!this.m_hasRoof)
			{
				return Localization.instance.Localize(this.m_name + " ( " + contentName + ", $piece_fermenter_needroof )");
			}
			if (this.m_exposed)
			{
				return Localization.instance.Localize(this.m_name + " ( " + contentName + ", $piece_fermenter_exposed )");
			}
			return Localization.instance.Localize(this.m_name + " ( " + contentName + ", $piece_fermenter_fermenting )");
		}
		case Fermenter.Status.Ready:
		{
			string contentName2 = this.GetContentName();
			return Localization.instance.Localize(this.m_name + " ( " + contentName2 + ", $piece_fermenter_ready )\n[<color=yellow><b>$KEY_Use</b></color>] $piece_fermenter_tap");
		}
		}
		return this.m_name;
	}

	// Token: 0x06001684 RID: 5764 RVA: 0x000A668C File Offset: 0x000A488C
	public bool Interact(Humanoid user, bool hold, bool alt)
	{
		if (hold)
		{
			return false;
		}
		this.UpdateCover(0f, true);
		if (!PrivateArea.CheckAccess(base.transform.position, 0f, true, false))
		{
			return true;
		}
		Fermenter.Status status = this.GetStatus();
		if (status == Fermenter.Status.Empty)
		{
			if (!this.m_hasRoof)
			{
				user.Message(MessageHud.MessageType.Center, "$piece_fermenter_needroof", 0, null);
				return false;
			}
			if (this.m_exposed)
			{
				user.Message(MessageHud.MessageType.Center, "$piece_fermenter_exposed", 0, null);
				return false;
			}
			ItemDrop.ItemData itemData = this.FindCookableItem(user.GetInventory());
			if (itemData == null)
			{
				user.Message(MessageHud.MessageType.Center, "$msg_noprocessableitems", 0, null);
				return false;
			}
			this.AddItem(user, itemData);
			return true;
		}
		else
		{
			if (status == Fermenter.Status.Ready)
			{
				this.m_nview.InvokeRPC("RPC_Tap", Array.Empty<object>());
				return true;
			}
			return false;
		}
	}

	// Token: 0x06001685 RID: 5765 RVA: 0x000A6746 File Offset: 0x000A4946
	public bool UseItem(Humanoid user, ItemDrop.ItemData item)
	{
		return PrivateArea.CheckAccess(base.transform.position, 0f, true, false) && this.AddItem(user, item);
	}

	// Token: 0x06001686 RID: 5766 RVA: 0x000A676C File Offset: 0x000A496C
	private void SlowUpdate()
	{
		this.UpdateCover(2f, false);
		switch (this.GetStatus())
		{
		case Fermenter.Status.Empty:
			this.m_fermentingObject.SetActive(false);
			this.m_readyObject.SetActive(false);
			this.m_topObject.SetActive(false);
			return;
		case Fermenter.Status.Fermenting:
			this.m_readyObject.SetActive(false);
			this.m_topObject.SetActive(true);
			this.m_fermentingObject.SetActive(!this.m_exposed && this.m_hasRoof);
			return;
		case Fermenter.Status.Exposed:
			break;
		case Fermenter.Status.Ready:
			this.m_fermentingObject.SetActive(false);
			this.m_readyObject.SetActive(true);
			this.m_topObject.SetActive(true);
			break;
		default:
			return;
		}
	}

	// Token: 0x06001687 RID: 5767 RVA: 0x000A6821 File Offset: 0x000A4A21
	private Fermenter.Status GetStatus()
	{
		if (string.IsNullOrEmpty(this.GetContent()))
		{
			return Fermenter.Status.Empty;
		}
		if (this.GetFermentationTime() > (double)this.m_fermentationDuration)
		{
			return Fermenter.Status.Ready;
		}
		return Fermenter.Status.Fermenting;
	}

	// Token: 0x06001688 RID: 5768 RVA: 0x000A6844 File Offset: 0x000A4A44
	private bool AddItem(Humanoid user, ItemDrop.ItemData item)
	{
		if (this.GetStatus() != Fermenter.Status.Empty)
		{
			return false;
		}
		if (!this.IsItemAllowed(item))
		{
			return false;
		}
		if (!user.GetInventory().RemoveOneItem(item))
		{
			return false;
		}
		this.m_nview.InvokeRPC("RPC_AddItem", new object[]
		{
			item.m_dropPrefab.name
		});
		return true;
	}

	// Token: 0x06001689 RID: 5769 RVA: 0x000A689C File Offset: 0x000A4A9C
	private void RPC_AddItem(long sender, string name)
	{
		if (!this.m_nview.IsOwner())
		{
			return;
		}
		if (this.GetStatus() != Fermenter.Status.Empty)
		{
			return;
		}
		if (!this.IsItemAllowed(name))
		{
			ZLog.DevLog("Item not allowed");
			return;
		}
		this.m_addedEffects.Create(base.transform.position, base.transform.rotation, null, 1f, -1);
		this.m_nview.GetZDO().Set(ZDOVars.s_content, name);
		this.m_nview.GetZDO().Set(ZDOVars.s_startTime, ZNet.instance.GetTime().Ticks);
	}

	// Token: 0x0600168A RID: 5770 RVA: 0x000A693C File Offset: 0x000A4B3C
	private void RPC_Tap(long sender)
	{
		if (!this.m_nview.IsOwner())
		{
			return;
		}
		if (this.GetStatus() != Fermenter.Status.Ready)
		{
			return;
		}
		this.m_delayedTapItem = this.GetContent();
		base.Invoke("DelayedTap", this.m_tapDelay);
		this.m_tapEffects.Create(base.transform.position, base.transform.rotation, null, 1f, -1);
		this.m_nview.GetZDO().Set(ZDOVars.s_content, "");
		this.m_nview.GetZDO().Set(ZDOVars.s_startTime, 0, false);
	}

	// Token: 0x0600168B RID: 5771 RVA: 0x000A69D8 File Offset: 0x000A4BD8
	private void DelayedTap()
	{
		this.m_spawnEffects.Create(this.m_outputPoint.transform.position, Quaternion.identity, null, 1f, -1);
		Fermenter.ItemConversion itemConversion = this.GetItemConversion(this.m_delayedTapItem);
		if (itemConversion != null)
		{
			float d = 0.3f;
			for (int i = 0; i < itemConversion.m_producedItems; i++)
			{
				Vector3 position = this.m_outputPoint.position + Vector3.up * d;
				ItemDrop.OnCreateNew(UnityEngine.Object.Instantiate<ItemDrop>(itemConversion.m_to, position, Quaternion.identity));
			}
		}
	}

	// Token: 0x0600168C RID: 5772 RVA: 0x000A6A68 File Offset: 0x000A4C68
	private void ResetFermentationTimer()
	{
		if (this.GetStatus() == Fermenter.Status.Fermenting)
		{
			this.m_nview.GetZDO().Set(ZDOVars.s_startTime, ZNet.instance.GetTime().Ticks);
		}
	}

	// Token: 0x0600168D RID: 5773 RVA: 0x000A6AA8 File Offset: 0x000A4CA8
	private double GetFermentationTime()
	{
		DateTime d = new DateTime(this.m_nview.GetZDO().GetLong(ZDOVars.s_startTime, 0L));
		if (d.Ticks == 0L)
		{
			return -1.0;
		}
		return (ZNet.instance.GetTime() - d).TotalSeconds;
	}

	// Token: 0x0600168E RID: 5774 RVA: 0x000A6B00 File Offset: 0x000A4D00
	private string GetContentName()
	{
		string content = this.GetContent();
		if (string.IsNullOrEmpty(content))
		{
			return "";
		}
		Fermenter.ItemConversion itemConversion = this.GetItemConversion(content);
		if (itemConversion == null)
		{
			return "Invalid";
		}
		return itemConversion.m_from.m_itemData.m_shared.m_name;
	}

	// Token: 0x0600168F RID: 5775 RVA: 0x000A6B48 File Offset: 0x000A4D48
	private string GetContent()
	{
		if (this.m_nview.GetZDO() == null)
		{
			return "";
		}
		return this.m_nview.GetZDO().GetString(ZDOVars.s_content, "");
	}

	// Token: 0x06001690 RID: 5776 RVA: 0x000A6B78 File Offset: 0x000A4D78
	private void UpdateCover(float dt, bool forceUpdate = false)
	{
		this.m_updateCoverTimer -= dt;
		if (this.m_updateCoverTimer <= 0f || forceUpdate)
		{
			this.m_updateCoverTimer = 10f;
			float num;
			bool hasRoof;
			Cover.GetCoverForPoint(this.m_roofCheckPoint.position, out num, out hasRoof, 0.5f);
			this.m_exposed = (num < 0.7f);
			this.m_hasRoof = hasRoof;
			if ((this.m_exposed || !this.m_hasRoof) && this.m_nview.IsOwner())
			{
				this.ResetFermentationTimer();
			}
		}
	}

	// Token: 0x06001691 RID: 5777 RVA: 0x000A6C03 File Offset: 0x000A4E03
	private bool IsItemAllowed(ItemDrop.ItemData item)
	{
		return this.IsItemAllowed(item.m_dropPrefab.name);
	}

	// Token: 0x06001692 RID: 5778 RVA: 0x000A6C18 File Offset: 0x000A4E18
	private bool IsItemAllowed(string itemName)
	{
		using (List<Fermenter.ItemConversion>.Enumerator enumerator = this.m_conversion.GetEnumerator())
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

	// Token: 0x06001693 RID: 5779 RVA: 0x000A6C84 File Offset: 0x000A4E84
	private ItemDrop.ItemData FindCookableItem(Inventory inventory)
	{
		foreach (Fermenter.ItemConversion itemConversion in this.m_conversion)
		{
			ItemDrop.ItemData item = inventory.GetItem(itemConversion.m_from.m_itemData.m_shared.m_name, -1, false);
			if (item != null)
			{
				return item;
			}
		}
		return null;
	}

	// Token: 0x06001694 RID: 5780 RVA: 0x000A6CF8 File Offset: 0x000A4EF8
	private Fermenter.ItemConversion GetItemConversion(string itemName)
	{
		foreach (Fermenter.ItemConversion itemConversion in this.m_conversion)
		{
			if (itemConversion.m_from.gameObject.name == itemName)
			{
				return itemConversion;
			}
		}
		return null;
	}

	// Token: 0x06001696 RID: 5782 RVA: 0x000A6DD0 File Offset: 0x000A4FD0
	[CompilerGenerated]
	private void <DropAllItems>g__drop|2_0(ItemDrop item)
	{
		Vector3 position = base.transform.position + Vector3.up + UnityEngine.Random.insideUnitSphere * 0.3f;
		Quaternion rotation = Quaternion.Euler(0f, (float)UnityEngine.Random.Range(0, 360), 0f);
		ItemDrop.OnCreateNew(UnityEngine.Object.Instantiate<GameObject>(item.gameObject, position, rotation));
	}

	// Token: 0x04001617 RID: 5655
	private const float updateDT = 2f;

	// Token: 0x04001618 RID: 5656
	public string m_name = "Fermentation barrel";

	// Token: 0x04001619 RID: 5657
	public float m_fermentationDuration = 2400f;

	// Token: 0x0400161A RID: 5658
	public GameObject m_fermentingObject;

	// Token: 0x0400161B RID: 5659
	public GameObject m_readyObject;

	// Token: 0x0400161C RID: 5660
	public GameObject m_topObject;

	// Token: 0x0400161D RID: 5661
	public EffectList m_addedEffects = new EffectList();

	// Token: 0x0400161E RID: 5662
	public EffectList m_tapEffects = new EffectList();

	// Token: 0x0400161F RID: 5663
	public EffectList m_spawnEffects = new EffectList();

	// Token: 0x04001620 RID: 5664
	public Switch m_addSwitch;

	// Token: 0x04001621 RID: 5665
	public Switch m_tapSwitch;

	// Token: 0x04001622 RID: 5666
	public float m_tapDelay = 1.5f;

	// Token: 0x04001623 RID: 5667
	public Transform m_outputPoint;

	// Token: 0x04001624 RID: 5668
	public Transform m_roofCheckPoint;

	// Token: 0x04001625 RID: 5669
	public List<Fermenter.ItemConversion> m_conversion = new List<Fermenter.ItemConversion>();

	// Token: 0x04001626 RID: 5670
	private ZNetView m_nview;

	// Token: 0x04001627 RID: 5671
	private float m_updateCoverTimer;

	// Token: 0x04001628 RID: 5672
	private bool m_exposed;

	// Token: 0x04001629 RID: 5673
	private bool m_hasRoof;

	// Token: 0x0400162A RID: 5674
	private string m_delayedTapItem = "";

	// Token: 0x0200035C RID: 860
	[Serializable]
	public class ItemConversion
	{
		// Token: 0x04002596 RID: 9622
		public ItemDrop m_from;

		// Token: 0x04002597 RID: 9623
		public ItemDrop m_to;

		// Token: 0x04002598 RID: 9624
		public int m_producedItems = 4;
	}

	// Token: 0x0200035D RID: 861
	private enum Status
	{
		// Token: 0x0400259A RID: 9626
		Empty,
		// Token: 0x0400259B RID: 9627
		Fermenting,
		// Token: 0x0400259C RID: 9628
		Exposed,
		// Token: 0x0400259D RID: 9629
		Ready
	}
}
