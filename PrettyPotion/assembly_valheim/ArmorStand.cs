using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

// Token: 0x02000157 RID: 343
public class ArmorStand : MonoBehaviour, IHasHoverMenuExtended
{
	// Token: 0x060014FE RID: 5374 RVA: 0x0009A3F8 File Offset: 0x000985F8
	private void Awake()
	{
		this.m_nview = (this.m_netViewOverride ? this.m_netViewOverride : base.gameObject.GetComponent<ZNetView>());
		if (this.m_nview.GetZDO() == null)
		{
			return;
		}
		WearNTear component = base.GetComponent<WearNTear>();
		if (component)
		{
			WearNTear wearNTear = component;
			wearNTear.m_onDestroyed = (Action)Delegate.Combine(wearNTear.m_onDestroyed, new Action(this.OnDestroyed));
		}
		this.m_nview.Register<int>("RPC_DropItem", new Action<long, int>(this.RPC_DropItem));
		this.m_nview.Register<string>("RPC_DropItemByName", new Action<long, string>(this.RPC_DropItemByName));
		this.m_nview.Register("RPC_RequestOwn", new Action<long>(this.RPC_RequestOwn));
		this.m_nview.Register<int>("RPC_DestroyAttachment", new Action<long, int>(this.RPC_DestroyAttachment));
		this.m_nview.Register<int, string, int>("RPC_SetVisualItem", new Action<long, int, string, int>(this.RPC_SetVisualItem));
		this.m_nview.Register<int>("RPC_SetPose", new Action<long, int>(this.RPC_SetPose));
		base.InvokeRepeating("UpdateVisual", 1f, 4f);
		this.SetPose(this.m_nview.GetZDO().GetInt(ZDOVars.s_pose, this.m_pose), false);
		using (List<ArmorStand.ArmorStandSlot>.Enumerator enumerator = this.m_slots.GetEnumerator())
		{
			while (enumerator.MoveNext())
			{
				ArmorStand.ArmorStandSlot item = enumerator.Current;
				if (item.m_switch.m_onUse == null)
				{
					Switch @switch = item.m_switch;
					@switch.m_onUse = (Switch.Callback)Delegate.Combine(@switch.m_onUse, new Switch.Callback(this.UseItem));
					Switch switch2 = item.m_switch;
					switch2.m_onHover = (Switch.TooltipCallback)Delegate.Combine(switch2.m_onHover, new Switch.TooltipCallback(delegate()
					{
						if (!PrivateArea.CheckAccess(this.transform.position, 0f, false, false))
						{
							return Localization.instance.Localize(this.m_name + "\n$piece_noaccess");
						}
						string str = (this.GetNrOfAttachedItems() > 0) ? "\n[<color=yellow><b>$KEY_Use</b></color>] $piece_itemstand_take" : "";
						return Localization.instance.Localize(item.m_switch.m_hoverText + "\n[<color=yellow><b>1-8</b></color>] $piece_itemstand_attach" + str);
					}));
				}
			}
		}
		if (this.m_changePoseSwitch != null && this.m_changePoseSwitch.gameObject.activeInHierarchy)
		{
			Switch changePoseSwitch = this.m_changePoseSwitch;
			changePoseSwitch.m_onUse = (Switch.Callback)Delegate.Combine(changePoseSwitch.m_onUse, new Switch.Callback(delegate(Switch caller, Humanoid user, ItemDrop.ItemData item)
			{
				if (!this.m_nview.IsOwner())
				{
					this.m_nview.InvokeRPC("RPC_RequestOwn", Array.Empty<object>());
				}
				if (!PrivateArea.CheckAccess(base.transform.position, 0f, false, false))
				{
					return false;
				}
				this.m_nview.InvokeRPC(ZNetView.Everybody, "RPC_SetPose", new object[]
				{
					(this.m_pose + 1 >= this.m_poseCount) ? 0 : (this.m_pose + 1)
				});
				return true;
			}));
			Switch changePoseSwitch2 = this.m_changePoseSwitch;
			changePoseSwitch2.m_onHover = (Switch.TooltipCallback)Delegate.Combine(changePoseSwitch2.m_onHover, new Switch.TooltipCallback(delegate()
			{
				if (!PrivateArea.CheckAccess(base.transform.position, 0f, false, false))
				{
					return Localization.instance.Localize(this.m_name + "\n$piece_noaccess");
				}
				return Localization.instance.Localize(this.m_name + "\n[<color=yellow><b>$KEY_Use</b></color>] Change pose ");
			}));
		}
	}

	// Token: 0x060014FF RID: 5375 RVA: 0x0009A678 File Offset: 0x00098878
	private void Update()
	{
		if (Player.m_localPlayer != null && this.m_cloths != null && this.m_cloths.Length != 0)
		{
			bool flag = Vector3.Distance(Player.m_localPlayer.transform.position, base.transform.position) > this.m_clothSimLodDistance * QualitySettings.lodBias;
			if (this.m_clothLodded != flag)
			{
				this.m_clothLodded = flag;
				foreach (Cloth cloth in this.m_cloths)
				{
					if (cloth)
					{
						cloth.enabled = !flag;
					}
				}
			}
		}
	}

	// Token: 0x06001500 RID: 5376 RVA: 0x0009A70C File Offset: 0x0009890C
	private void OnDestroyed()
	{
		if (this.m_nview.IsOwner())
		{
			for (int i = 0; i < this.m_slots.Count; i++)
			{
				this.DropItem(i);
			}
		}
	}

	// Token: 0x06001501 RID: 5377 RVA: 0x0009A744 File Offset: 0x00098944
	private void SetPose(int index, bool effect = true)
	{
		this.m_pose = index;
		this.m_poseAnimator.SetInteger("Pose", this.m_pose);
		if (effect)
		{
			this.m_effects.Create(base.transform.position, Quaternion.identity, null, 1f, -1);
		}
		if (this.m_nview.IsOwner())
		{
			this.m_nview.GetZDO().Set(ZDOVars.s_pose, this.m_pose, false);
		}
	}

	// Token: 0x06001502 RID: 5378 RVA: 0x0009A7BD File Offset: 0x000989BD
	public void RPC_SetPose(long sender, int index)
	{
		this.SetPose(index, true);
	}

	// Token: 0x06001503 RID: 5379 RVA: 0x0009A7C8 File Offset: 0x000989C8
	private bool UseItem(Switch caller, Humanoid user, ItemDrop.ItemData item)
	{
		if (!PrivateArea.CheckAccess(base.transform.position, 0f, false, false))
		{
			return true;
		}
		ArmorStand.ArmorStandSlot armorStandSlot = null;
		int num = -1;
		for (int i = 0; i < this.m_slots.Count; i++)
		{
			if (this.m_slots[i].m_switch == caller && ((item == null && !string.IsNullOrEmpty(this.m_slots[i].m_visualName)) || (item != null && this.CanAttach(this.m_slots[i], item))))
			{
				armorStandSlot = this.m_slots[i];
				num = i;
				break;
			}
		}
		if (item == null)
		{
			if (armorStandSlot == null || num < 0)
			{
				return false;
			}
			if (this.HaveAttachment(num))
			{
				this.m_nview.InvokeRPC("RPC_DropItemByName", new object[]
				{
					this.m_slots[num].m_switch.name
				});
				return true;
			}
			return false;
		}
		else
		{
			if (item.m_shared.m_itemType != ItemDrop.ItemData.ItemType.Legs && item.m_shared.m_itemType != ItemDrop.ItemData.ItemType.Chest)
			{
				int childCount = item.m_dropPrefab.transform.childCount;
				bool flag = false;
				for (int j = 0; j < childCount; j++)
				{
					Transform child = item.m_dropPrefab.transform.GetChild(j);
					if (child.gameObject.name == "attach" || child.gameObject.name == "attach_skin")
					{
						flag = true;
						break;
					}
				}
				if (!flag)
				{
					return false;
				}
			}
			if (num < 0)
			{
				user.Message(MessageHud.MessageType.Center, "$piece_armorstand_cantattach", 0, null);
				return true;
			}
			if (this.HaveAttachment(num))
			{
				return false;
			}
			if (!this.m_nview.IsOwner())
			{
				this.m_nview.InvokeRPC("RPC_RequestOwn", Array.Empty<object>());
			}
			this.m_queuedItem = item;
			this.m_queuedSlot = num;
			base.CancelInvoke("UpdateAttach");
			base.InvokeRepeating("UpdateAttach", 0f, 0.1f);
			return true;
		}
	}

	// Token: 0x06001504 RID: 5380 RVA: 0x0009A9B2 File Offset: 0x00098BB2
	public void DestroyAttachment(int index)
	{
		this.m_nview.InvokeRPC("RPC_DestroyAttachment", new object[]
		{
			index
		});
	}

	// Token: 0x06001505 RID: 5381 RVA: 0x0009A9D4 File Offset: 0x00098BD4
	public void RPC_DestroyAttachment(long sender, int index)
	{
		if (!this.m_nview.IsOwner())
		{
			return;
		}
		if (!this.HaveAttachment(index))
		{
			return;
		}
		this.m_nview.GetZDO().Set(index.ToString() + "_item", "");
		this.m_nview.InvokeRPC(ZNetView.Everybody, "RPC_SetVisualItem", new object[]
		{
			index,
			"",
			0
		});
		this.m_destroyEffects.Create(this.m_dropSpawnPoint.position, Quaternion.identity, null, 1f, -1);
	}

	// Token: 0x06001506 RID: 5382 RVA: 0x0009AA78 File Offset: 0x00098C78
	private void RPC_DropItemByName(long sender, string name)
	{
		if (!this.m_nview.IsOwner())
		{
			return;
		}
		for (int i = 0; i < this.m_slots.Count; i++)
		{
			if (this.m_slots[i].m_switch.name == name)
			{
				this.DropItem(i);
			}
		}
	}

	// Token: 0x06001507 RID: 5383 RVA: 0x0009AACE File Offset: 0x00098CCE
	private void RPC_DropItem(long sender, int index)
	{
		if (!this.m_nview.IsOwner())
		{
			return;
		}
		this.DropItem(index);
	}

	// Token: 0x06001508 RID: 5384 RVA: 0x0009AAE8 File Offset: 0x00098CE8
	private void DropItem(int index)
	{
		if (!this.HaveAttachment(index))
		{
			return;
		}
		string @string = this.m_nview.GetZDO().GetString(index.ToString() + "_item", "");
		GameObject itemPrefab = ObjectDB.instance.GetItemPrefab(@string);
		if (itemPrefab)
		{
			GameObject gameObject = UnityEngine.Object.Instantiate<GameObject>(itemPrefab, this.m_dropSpawnPoint.position, this.m_dropSpawnPoint.rotation);
			ItemDrop component = gameObject.GetComponent<ItemDrop>();
			ItemDrop.LoadFromZDO(index, component.m_itemData, this.m_nview.GetZDO());
			gameObject.GetComponent<Rigidbody>().velocity = Vector3.up * 4f;
			this.m_destroyEffects.Create(this.m_dropSpawnPoint.position, Quaternion.identity, null, 1f, -1);
		}
		this.m_nview.GetZDO().Set(index.ToString() + "_item", "");
		this.m_nview.InvokeRPC(ZNetView.Everybody, "RPC_SetVisualItem", new object[]
		{
			index,
			"",
			0
		});
		this.UpdateSupports();
		this.m_cloths = base.GetComponentsInChildren<Cloth>();
	}

	// Token: 0x06001509 RID: 5385 RVA: 0x0009AC20 File Offset: 0x00098E20
	private void UpdateAttach()
	{
		if (this.m_nview.IsOwner())
		{
			base.CancelInvoke("UpdateAttach");
			Player localPlayer = Player.m_localPlayer;
			if (this.m_queuedItem != null && localPlayer != null && localPlayer.GetInventory().ContainsItem(this.m_queuedItem) && !this.HaveAttachment(this.m_queuedSlot))
			{
				ItemDrop.ItemData itemData = this.m_queuedItem.Clone();
				itemData.m_stack = 1;
				this.m_nview.GetZDO().Set(this.m_queuedSlot.ToString() + "_item", this.m_queuedItem.m_dropPrefab.name);
				ItemDrop.SaveToZDO(this.m_queuedSlot, itemData, this.m_nview.GetZDO());
				localPlayer.UnequipItem(this.m_queuedItem, true);
				localPlayer.GetInventory().RemoveOneItem(this.m_queuedItem);
				this.m_nview.InvokeRPC(ZNetView.Everybody, "RPC_SetVisualItem", new object[]
				{
					this.m_queuedSlot,
					itemData.m_dropPrefab.name,
					itemData.m_variant
				});
				this.m_effects.Create(base.transform.position, Quaternion.identity, null, 1f, -1);
				Game.instance.IncrementPlayerStat(PlayerStatType.ArmorStandUses, 1f);
			}
			this.m_queuedItem = null;
		}
	}

	// Token: 0x0600150A RID: 5386 RVA: 0x0009AD8A File Offset: 0x00098F8A
	private void RPC_RequestOwn(long sender)
	{
		if (!this.m_nview.IsOwner())
		{
			return;
		}
		this.m_nview.GetZDO().SetOwner(sender);
	}

	// Token: 0x0600150B RID: 5387 RVA: 0x0009ADAC File Offset: 0x00098FAC
	private void UpdateVisual()
	{
		if (this.m_nview == null || !this.m_nview.IsValid())
		{
			return;
		}
		for (int i = 0; i < this.m_slots.Count; i++)
		{
			string @string = this.m_nview.GetZDO().GetString(i.ToString() + "_item", "");
			int @int = this.m_nview.GetZDO().GetInt(i.ToString() + "_variant", 0);
			this.SetVisualItem(i, @string, @int);
		}
	}

	// Token: 0x0600150C RID: 5388 RVA: 0x0009AE3E File Offset: 0x0009903E
	private void RPC_SetVisualItem(long sender, int index, string itemName, int variant)
	{
		this.SetVisualItem(index, itemName, variant);
	}

	// Token: 0x0600150D RID: 5389 RVA: 0x0009AE4C File Offset: 0x0009904C
	private void SetVisualItem(int index, string itemName, int variant)
	{
		ArmorStand.ArmorStandSlot armorStandSlot = this.m_slots[index];
		if (armorStandSlot.m_visualName == itemName && armorStandSlot.m_visualVariant == variant)
		{
			return;
		}
		armorStandSlot.m_visualName = itemName;
		armorStandSlot.m_visualVariant = variant;
		armorStandSlot.m_currentItemName = "";
		if (armorStandSlot.m_visualName == "")
		{
			this.m_visEquipment.SetItem(armorStandSlot.m_slot, "", 0);
			return;
		}
		GameObject itemPrefab = ObjectDB.instance.GetItemPrefab(itemName);
		if (itemPrefab == null)
		{
			ZLog.LogWarning("Missing item prefab " + itemName);
			return;
		}
		ItemDrop component = itemPrefab.GetComponent<ItemDrop>();
		armorStandSlot.m_currentItemName = component.m_itemData.m_shared.m_name;
		ItemDrop component2 = itemPrefab.GetComponent<ItemDrop>();
		if (component2 != null)
		{
			if (component2.m_itemData.m_dropPrefab == null)
			{
				component2.m_itemData.m_dropPrefab = itemPrefab.gameObject;
			}
			this.m_visEquipment.SetItem(armorStandSlot.m_slot, component2.m_itemData.m_dropPrefab.name, armorStandSlot.m_visualVariant);
			this.UpdateSupports();
			this.m_cloths = base.GetComponentsInChildren<Cloth>();
		}
	}

	// Token: 0x0600150E RID: 5390 RVA: 0x0009AF6C File Offset: 0x0009916C
	private void UpdateSupports()
	{
		foreach (ArmorStand.ArmorStandSupport armorStandSupport in this.m_supports)
		{
			foreach (GameObject gameObject in armorStandSupport.m_supports)
			{
				gameObject.SetActive(false);
			}
		}
		foreach (ArmorStand.ArmorStandSlot armorStandSlot in this.m_slots)
		{
			if (armorStandSlot.m_item != null)
			{
				foreach (ArmorStand.ArmorStandSupport armorStandSupport2 in this.m_supports)
				{
					using (List<ItemDrop>.Enumerator enumerator4 = armorStandSupport2.m_items.GetEnumerator())
					{
						while (enumerator4.MoveNext())
						{
							if (enumerator4.Current.m_itemData.m_shared.m_name == armorStandSlot.m_currentItemName)
							{
								foreach (GameObject gameObject2 in armorStandSupport2.m_supports)
								{
									gameObject2.SetActive(true);
								}
							}
						}
					}
				}
			}
		}
	}

	// Token: 0x0600150F RID: 5391 RVA: 0x0009B120 File Offset: 0x00099320
	private GameObject GetAttachPrefab(GameObject item)
	{
		Transform transform = item.transform.Find("attach_skin");
		if (transform)
		{
			return transform.gameObject;
		}
		transform = item.transform.Find("attach");
		if (transform)
		{
			return transform.gameObject;
		}
		return null;
	}

	// Token: 0x06001510 RID: 5392 RVA: 0x0009B170 File Offset: 0x00099370
	private bool CanAttach(ArmorStand.ArmorStandSlot slot, ItemDrop.ItemData item)
	{
		return slot.m_supportedTypes.Count == 0 || slot.m_supportedTypes.Contains((item.m_shared.m_attachOverride != ItemDrop.ItemData.ItemType.None) ? item.m_shared.m_attachOverride : item.m_shared.m_itemType);
	}

	// Token: 0x06001511 RID: 5393 RVA: 0x0009B1BC File Offset: 0x000993BC
	public bool HaveAttachment(int index)
	{
		return this.m_nview.IsValid() && this.m_nview.GetZDO().GetString(index.ToString() + "_item", "") != "";
	}

	// Token: 0x06001512 RID: 5394 RVA: 0x0009B208 File Offset: 0x00099408
	public string GetAttachedItem(int index)
	{
		if (!this.m_nview.IsValid())
		{
			return "";
		}
		return this.m_nview.GetZDO().GetString(index.ToString() + "_item", "");
	}

	// Token: 0x06001513 RID: 5395 RVA: 0x0009B244 File Offset: 0x00099444
	public int GetNrOfAttachedItems()
	{
		int num = 0;
		using (List<ArmorStand.ArmorStandSlot>.Enumerator enumerator = this.m_slots.GetEnumerator())
		{
			while (enumerator.MoveNext())
			{
				if (enumerator.Current.m_currentItemName.Length > 0)
				{
					num++;
				}
			}
		}
		return num;
	}

	// Token: 0x06001514 RID: 5396 RVA: 0x0009B2A4 File Offset: 0x000994A4
	public bool TryGetItems(Player player, Switch switchRef, out List<string> items)
	{
		items = new List<string>();
		ArmorStand.ArmorStandSlot armorStandSlot = this.m_slots.FirstOrDefault((ArmorStand.ArmorStandSlot t) => t.m_switch == switchRef);
		if (armorStandSlot == null)
		{
			return false;
		}
		items.Add("type");
		items.AddRange(from type in armorStandSlot.m_supportedTypes
		select type.ToString());
		return true;
	}

	// Token: 0x06001515 RID: 5397 RVA: 0x0009B320 File Offset: 0x00099520
	public bool CanUseItems(Player player, Switch switchRef, bool sendErrorMessage = true)
	{
		return this.m_slots.FirstOrDefault((ArmorStand.ArmorStandSlot t) => t.m_switch == switchRef) != null;
	}

	// Token: 0x0400147D RID: 5245
	public ZNetView m_netViewOverride;

	// Token: 0x0400147E RID: 5246
	private ZNetView m_nview;

	// Token: 0x0400147F RID: 5247
	public List<ArmorStand.ArmorStandSlot> m_slots = new List<ArmorStand.ArmorStandSlot>();

	// Token: 0x04001480 RID: 5248
	public List<ArmorStand.ArmorStandSupport> m_supports = new List<ArmorStand.ArmorStandSupport>();

	// Token: 0x04001481 RID: 5249
	public Switch m_changePoseSwitch;

	// Token: 0x04001482 RID: 5250
	public Animator m_poseAnimator;

	// Token: 0x04001483 RID: 5251
	public string m_name = "";

	// Token: 0x04001484 RID: 5252
	public Transform m_dropSpawnPoint;

	// Token: 0x04001485 RID: 5253
	public VisEquipment m_visEquipment;

	// Token: 0x04001486 RID: 5254
	public EffectList m_effects = new EffectList();

	// Token: 0x04001487 RID: 5255
	public EffectList m_destroyEffects = new EffectList();

	// Token: 0x04001488 RID: 5256
	public int m_poseCount = 3;

	// Token: 0x04001489 RID: 5257
	public int m_startPose;

	// Token: 0x0400148A RID: 5258
	private int m_pose;

	// Token: 0x0400148B RID: 5259
	public float m_clothSimLodDistance = 10f;

	// Token: 0x0400148C RID: 5260
	private bool m_clothLodded;

	// Token: 0x0400148D RID: 5261
	private Cloth[] m_cloths;

	// Token: 0x0400148E RID: 5262
	private ItemDrop.ItemData m_queuedItem;

	// Token: 0x0400148F RID: 5263
	private int m_queuedSlot;

	// Token: 0x02000342 RID: 834
	[Serializable]
	public class ArmorStandSlot
	{
		// Token: 0x04002517 RID: 9495
		public Switch m_switch;

		// Token: 0x04002518 RID: 9496
		public VisSlot m_slot;

		// Token: 0x04002519 RID: 9497
		public List<ItemDrop.ItemData.ItemType> m_supportedTypes = new List<ItemDrop.ItemData.ItemType>();

		// Token: 0x0400251A RID: 9498
		[HideInInspector]
		public ItemDrop.ItemData m_item;

		// Token: 0x0400251B RID: 9499
		[HideInInspector]
		public string m_visualName = "";

		// Token: 0x0400251C RID: 9500
		[HideInInspector]
		public int m_visualVariant;

		// Token: 0x0400251D RID: 9501
		[HideInInspector]
		public string m_currentItemName = "";
	}

	// Token: 0x02000343 RID: 835
	[Serializable]
	public class ArmorStandSupport
	{
		// Token: 0x0400251E RID: 9502
		public List<ItemDrop> m_items = new List<ItemDrop>();

		// Token: 0x0400251F RID: 9503
		public List<GameObject> m_supports = new List<GameObject>();
	}
}
