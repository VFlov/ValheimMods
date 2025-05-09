using System;
using System.Collections.Generic;
using UnityEngine;

// Token: 0x0200018B RID: 395
public class ItemStand : MonoBehaviour, Interactable, Hoverable
{
	// Token: 0x060017A3 RID: 6051 RVA: 0x000AFE48 File Offset: 0x000AE048
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
		this.m_nview.Register("DropItem", new Action<long>(this.RPC_DropItem));
		this.m_nview.Register("RequestOwn", new Action<long>(this.RPC_RequestOwn));
		this.m_nview.Register("DestroyAttachment", new Action<long>(this.RPC_DestroyAttachment));
		this.m_nview.Register<string, int, int>("SetVisualItem", new Action<long, string, int, int>(this.RPC_SetVisualItem));
		base.InvokeRepeating("UpdateVisual", 1f, 4f);
	}

	// Token: 0x060017A4 RID: 6052 RVA: 0x000AFF3F File Offset: 0x000AE13F
	private void OnDestroyed()
	{
		if (this.m_nview.IsOwner())
		{
			this.DropItem();
		}
	}

	// Token: 0x060017A5 RID: 6053 RVA: 0x000AFF54 File Offset: 0x000AE154
	public string GetHoverText()
	{
		if (!Player.m_localPlayer)
		{
			return "";
		}
		if (!PrivateArea.CheckAccess(base.transform.position, 0f, false, false))
		{
			return Localization.instance.Localize(this.m_name + "\n$piece_noaccess");
		}
		if (this.HaveAttachment())
		{
			if (this.m_canBeRemoved)
			{
				return Localization.instance.Localize(this.m_name + " ( " + this.m_currentItemName + " )\n[<color=yellow><b>$KEY_Use</b></color>] $piece_itemstand_take");
			}
			if (!(this.m_guardianPower != null))
			{
				return "";
			}
			if (base.IsInvoking("DelayedPowerActivation"))
			{
				return "";
			}
			string tooltipString = this.m_guardianPower.GetTooltipString();
			if (this.IsGuardianPowerActive(Player.m_localPlayer))
			{
				return Localization.instance.Localize(string.Concat(new string[]
				{
					"<color=orange>",
					this.m_guardianPower.m_name,
					"</color>\n",
					tooltipString,
					"\n\n$guardianstone_hook_alreadyactive"
				}));
			}
			return Localization.instance.Localize(string.Concat(new string[]
			{
				"<color=orange>",
				this.m_guardianPower.m_name,
				"</color>\n",
				tooltipString,
				"\n\n[<color=yellow><b>$KEY_Use</b></color>] $guardianstone_hook_activate"
			}));
		}
		else
		{
			if (this.m_autoAttach && this.m_supportedItems.Count == 1)
			{
				return Localization.instance.Localize(this.m_name + "\n[<color=yellow><b>$KEY_Use</b></color>] $piece_itemstand_attach");
			}
			return Localization.instance.Localize(this.m_name + "\n[<color=yellow><b>1-8</b></color>] $piece_itemstand_attach");
		}
	}

	// Token: 0x060017A6 RID: 6054 RVA: 0x000B00F1 File Offset: 0x000AE2F1
	public string GetHoverName()
	{
		return this.m_name;
	}

	// Token: 0x060017A7 RID: 6055 RVA: 0x000B00FC File Offset: 0x000AE2FC
	public bool Interact(Humanoid user, bool hold, bool alt)
	{
		if (hold)
		{
			return false;
		}
		if (!PrivateArea.CheckAccess(base.transform.position, 0f, true, false))
		{
			return true;
		}
		if (!this.HaveAttachment())
		{
			if (this.m_autoAttach && this.m_supportedItems.Count == 1)
			{
				ItemDrop.ItemData item = user.GetInventory().GetItem(this.m_supportedItems[0].m_itemData.m_shared.m_name, -1, false);
				if (item != null)
				{
					this.UseItem(user, item);
					return true;
				}
				user.Message(MessageHud.MessageType.Center, "$piece_itemstand_missingitem", 0, null);
				return false;
			}
		}
		else
		{
			if (this.m_canBeRemoved)
			{
				this.m_nview.InvokeRPC("DropItem", Array.Empty<object>());
				return true;
			}
			if (this.m_guardianPower != null)
			{
				if (base.IsInvoking("DelayedPowerActivation"))
				{
					return false;
				}
				if (this.IsGuardianPowerActive(user))
				{
					return false;
				}
				user.Message(MessageHud.MessageType.Center, "$guardianstone_hook_power_activate ", 0, null);
				this.m_activatePowerEffects.Create(base.transform.position, base.transform.rotation, null, 1f, -1);
				this.m_activatePowerEffectsPlayer.Create(user.transform.position, Quaternion.identity, user.transform, 1f, -1);
				base.Invoke("DelayedPowerActivation", this.m_powerActivationDelay);
				return true;
			}
		}
		return false;
	}

	// Token: 0x060017A8 RID: 6056 RVA: 0x000B0251 File Offset: 0x000AE451
	private bool IsGuardianPowerActive(Humanoid user)
	{
		return (user as Player).GetGuardianPowerName() == this.m_guardianPower.name;
	}

	// Token: 0x060017A9 RID: 6057 RVA: 0x000B0270 File Offset: 0x000AE470
	private void DelayedPowerActivation()
	{
		Player localPlayer = Player.m_localPlayer;
		if (localPlayer == null)
		{
			return;
		}
		localPlayer.SetGuardianPower(this.m_guardianPower.name);
	}

	// Token: 0x060017AA RID: 6058 RVA: 0x000B02A0 File Offset: 0x000AE4A0
	public bool UseItem(Humanoid user, ItemDrop.ItemData item)
	{
		if (this.HaveAttachment())
		{
			return false;
		}
		if (!this.CanAttach(item))
		{
			user.Message(MessageHud.MessageType.Center, "$piece_itemstand_cantattach", 0, null);
			return true;
		}
		if (!this.m_nview.IsOwner())
		{
			this.m_nview.InvokeRPC("RequestOwn", Array.Empty<object>());
		}
		this.m_queuedItem = item;
		base.CancelInvoke("UpdateAttach");
		base.InvokeRepeating("UpdateAttach", 0f, 0.1f);
		return true;
	}

	// Token: 0x060017AB RID: 6059 RVA: 0x000B031A File Offset: 0x000AE51A
	private void RPC_DropItem(long sender)
	{
		if (!this.m_nview.IsOwner())
		{
			return;
		}
		if (!this.m_canBeRemoved)
		{
			return;
		}
		this.DropItem();
	}

	// Token: 0x060017AC RID: 6060 RVA: 0x000B0339 File Offset: 0x000AE539
	public void DestroyAttachment()
	{
		this.m_nview.InvokeRPC("DestroyAttachment", Array.Empty<object>());
	}

	// Token: 0x060017AD RID: 6061 RVA: 0x000B0350 File Offset: 0x000AE550
	public void RPC_DestroyAttachment(long sender)
	{
		if (!this.m_nview.IsOwner())
		{
			return;
		}
		if (!this.HaveAttachment())
		{
			return;
		}
		this.m_nview.GetZDO().Set(ZDOVars.s_item, "");
		this.m_nview.InvokeRPC(ZNetView.Everybody, "SetVisualItem", new object[]
		{
			"",
			0,
			0
		});
		this.m_destroyEffects.Create(this.m_dropSpawnPoint.position, Quaternion.identity, null, 1f, -1);
	}

	// Token: 0x060017AE RID: 6062 RVA: 0x000B03E8 File Offset: 0x000AE5E8
	private void DropItem()
	{
		if (!this.HaveAttachment())
		{
			return;
		}
		string @string = this.m_nview.GetZDO().GetString(ZDOVars.s_item, "");
		GameObject itemPrefab = ObjectDB.instance.GetItemPrefab(@string);
		if (itemPrefab)
		{
			Vector3 b = Vector3.zero;
			Quaternion rhs = Quaternion.identity;
			Transform transform = itemPrefab.transform.Find("attach");
			if (itemPrefab.transform.Find("attachobj") && transform)
			{
				rhs = transform.transform.localRotation;
				b = transform.transform.localPosition;
			}
			GameObject gameObject = UnityEngine.Object.Instantiate<GameObject>(itemPrefab, this.m_dropSpawnPoint.position + b, this.m_dropSpawnPoint.rotation * rhs);
			gameObject.GetComponent<ItemDrop>().LoadFromExternalZDO(this.m_nview.GetZDO());
			gameObject.GetComponent<Rigidbody>().velocity = Vector3.up * 4f;
			this.m_effects.Create(this.m_dropSpawnPoint.position, Quaternion.identity, null, 1f, -1);
		}
		this.m_nview.GetZDO().Set(ZDOVars.s_item, "");
		this.m_nview.InvokeRPC(ZNetView.Everybody, "SetVisualItem", new object[]
		{
			"",
			0,
			0
		});
	}

	// Token: 0x060017AF RID: 6063 RVA: 0x000B0551 File Offset: 0x000AE751
	public Transform GetAttach(ItemDrop.ItemData item)
	{
		return this.m_attachOther;
	}

	// Token: 0x060017B0 RID: 6064 RVA: 0x000B055C File Offset: 0x000AE75C
	private void UpdateAttach()
	{
		if (!this.m_nview.IsOwner())
		{
			return;
		}
		base.CancelInvoke("UpdateAttach");
		Player localPlayer = Player.m_localPlayer;
		if (this.m_queuedItem != null && localPlayer != null && localPlayer.GetInventory().ContainsItem(this.m_queuedItem) && !this.HaveAttachment())
		{
			ItemDrop.ItemData itemData = this.m_queuedItem.Clone();
			itemData.m_stack = 1;
			this.m_nview.GetZDO().Set(ZDOVars.s_item, this.m_queuedItem.m_dropPrefab.name);
			ItemDrop.SaveToZDO(itemData, this.m_nview.GetZDO());
			localPlayer.UnequipItem(this.m_queuedItem, true);
			localPlayer.GetInventory().RemoveOneItem(this.m_queuedItem);
			this.m_nview.InvokeRPC(ZNetView.Everybody, "SetVisualItem", new object[]
			{
				itemData.m_dropPrefab.name,
				itemData.m_variant,
				itemData.m_quality
			});
			Transform attach = this.GetAttach(this.m_queuedItem);
			this.m_effects.Create(attach.transform.position, Quaternion.identity, null, 1f, -1);
			Game.instance.IncrementPlayerStat(PlayerStatType.ItemStandUses, 1f);
		}
		this.m_queuedItem = null;
	}

	// Token: 0x060017B1 RID: 6065 RVA: 0x000B06B5 File Offset: 0x000AE8B5
	private void RPC_RequestOwn(long sender)
	{
		if (!this.m_nview.IsOwner())
		{
			return;
		}
		this.m_nview.GetZDO().SetOwner(sender);
	}

	// Token: 0x060017B2 RID: 6066 RVA: 0x000B06D8 File Offset: 0x000AE8D8
	private void UpdateVisual()
	{
		if (this.m_nview == null || !this.m_nview.IsValid())
		{
			return;
		}
		string @string = this.m_nview.GetZDO().GetString(ZDOVars.s_item, "");
		int @int = this.m_nview.GetZDO().GetInt(ZDOVars.s_variant, 0);
		int int2 = this.m_nview.GetZDO().GetInt(ZDOVars.s_quality, 1);
		this.SetVisualItem(@string, @int, int2);
	}

	// Token: 0x060017B3 RID: 6067 RVA: 0x000B0753 File Offset: 0x000AE953
	private void RPC_SetVisualItem(long sender, string itemName, int variant, int quality)
	{
		this.SetVisualItem(itemName, variant, quality);
	}

	// Token: 0x060017B4 RID: 6068 RVA: 0x000B0760 File Offset: 0x000AE960
	private void SetVisualItem(string itemName, int variant, int quality)
	{
		if (this.m_visualName == itemName && this.m_visualVariant == variant)
		{
			return;
		}
		this.m_visualName = itemName;
		this.m_visualVariant = variant;
		this.m_currentItemName = "";
		if (this.m_visualName == "")
		{
			UnityEngine.Object.Destroy(this.m_visualItem);
			return;
		}
		GameObject itemPrefab = ObjectDB.instance.GetItemPrefab(itemName);
		if (itemPrefab == null)
		{
			ZLog.LogWarning("Missing item prefab " + itemName);
			return;
		}
		GameObject attachPrefab = ItemStand.GetAttachPrefab(itemPrefab);
		if (attachPrefab == null)
		{
			ZLog.LogWarning("Failed to get attach prefab for item " + itemName);
			return;
		}
		ItemDrop component = itemPrefab.GetComponent<ItemDrop>();
		this.m_currentItemName = component.m_itemData.m_shared.m_name;
		Transform attach = this.GetAttach(component.m_itemData);
		GameObject attachGameObject = ItemStand.GetAttachGameObject(attachPrefab);
		this.m_visualItem = UnityEngine.Object.Instantiate<GameObject>(attachGameObject, attach.position, attach.rotation, attach);
		this.m_visualItem.transform.localPosition = attachPrefab.transform.localPosition;
		this.m_visualItem.transform.localRotation = attachPrefab.transform.localRotation;
		this.m_visualItem.transform.localScale = Vector3.Scale(attachPrefab.transform.localScale, component.m_itemData.GetScale((float)quality));
		if (this.m_horizontal)
		{
			this.m_visualItem.transform.localPosition += component.m_itemData.m_shared.m_ISHorizontalPos;
			this.m_visualItem.transform.localRotation *= Quaternion.Euler(component.m_itemData.m_shared.m_ISHorizontalRot);
			this.m_visualItem.transform.localScale = Vector3.Scale(this.m_visualItem.transform.localScale, component.m_itemData.m_shared.m_iSHorizontalScale);
		}
		IEquipmentVisual componentInChildren = this.m_visualItem.GetComponentInChildren<IEquipmentVisual>();
		if (componentInChildren != null)
		{
			componentInChildren.Setup(this.m_visualVariant);
		}
	}

	// Token: 0x060017B5 RID: 6069 RVA: 0x000B096C File Offset: 0x000AEB6C
	public static GameObject GetAttachPrefab(GameObject item)
	{
		Transform transform = item.transform.Find("attach");
		if (transform)
		{
			return transform.gameObject;
		}
		return null;
	}

	// Token: 0x060017B6 RID: 6070 RVA: 0x000B099C File Offset: 0x000AEB9C
	public static GameObject GetAttachGameObject(GameObject prefab)
	{
		Transform transform = prefab.transform.Find("attachobj");
		if (!(transform != null))
		{
			return prefab;
		}
		return transform.gameObject;
	}

	// Token: 0x060017B7 RID: 6071 RVA: 0x000B09CC File Offset: 0x000AEBCC
	private bool CanAttach(ItemDrop.ItemData item)
	{
		return !(ItemStand.GetAttachPrefab(item.m_dropPrefab) == null) && !this.IsUnsupported(item) && (this.IsSupported(item) || this.m_supportedTypes.Contains(item.m_shared.m_itemType));
	}

	// Token: 0x060017B8 RID: 6072 RVA: 0x000B0A20 File Offset: 0x000AEC20
	public bool IsUnsupported(ItemDrop.ItemData item)
	{
		using (List<ItemDrop>.Enumerator enumerator = this.m_unsupportedItems.GetEnumerator())
		{
			while (enumerator.MoveNext())
			{
				if (enumerator.Current.m_itemData.m_shared.m_name == item.m_shared.m_name)
				{
					return true;
				}
			}
		}
		return false;
	}

	// Token: 0x060017B9 RID: 6073 RVA: 0x000B0A94 File Offset: 0x000AEC94
	public bool IsSupported(ItemDrop.ItemData item)
	{
		if (this.m_supportedItems.Count == 0)
		{
			return true;
		}
		using (List<ItemDrop>.Enumerator enumerator = this.m_supportedItems.GetEnumerator())
		{
			while (enumerator.MoveNext())
			{
				if (enumerator.Current.m_itemData.m_shared.m_name == item.m_shared.m_name)
				{
					return true;
				}
			}
		}
		return false;
	}

	// Token: 0x060017BA RID: 6074 RVA: 0x000B0B18 File Offset: 0x000AED18
	public bool HaveAttachment()
	{
		return this.m_nview.IsValid() && this.m_nview.GetZDO().GetString(ZDOVars.s_item, "") != "";
	}

	// Token: 0x060017BB RID: 6075 RVA: 0x000B0B4D File Offset: 0x000AED4D
	public string GetAttachedItem()
	{
		if (!this.m_nview.IsValid())
		{
			return "";
		}
		return this.m_nview.GetZDO().GetString(ZDOVars.s_item, "");
	}

	// Token: 0x04001778 RID: 6008
	public ZNetView m_netViewOverride;

	// Token: 0x04001779 RID: 6009
	public string m_name = "";

	// Token: 0x0400177A RID: 6010
	public Transform m_attachOther;

	// Token: 0x0400177B RID: 6011
	public Transform m_dropSpawnPoint;

	// Token: 0x0400177C RID: 6012
	public bool m_canBeRemoved = true;

	// Token: 0x0400177D RID: 6013
	public bool m_autoAttach;

	// Token: 0x0400177E RID: 6014
	public bool m_horizontal;

	// Token: 0x0400177F RID: 6015
	public List<ItemDrop.ItemData.ItemType> m_supportedTypes = new List<ItemDrop.ItemData.ItemType>();

	// Token: 0x04001780 RID: 6016
	public List<ItemDrop> m_unsupportedItems = new List<ItemDrop>();

	// Token: 0x04001781 RID: 6017
	public List<ItemDrop> m_supportedItems = new List<ItemDrop>();

	// Token: 0x04001782 RID: 6018
	public EffectList m_effects = new EffectList();

	// Token: 0x04001783 RID: 6019
	public EffectList m_destroyEffects = new EffectList();

	// Token: 0x04001784 RID: 6020
	[Header("Guardian power")]
	public float m_powerActivationDelay = 2f;

	// Token: 0x04001785 RID: 6021
	public StatusEffect m_guardianPower;

	// Token: 0x04001786 RID: 6022
	public EffectList m_activatePowerEffects = new EffectList();

	// Token: 0x04001787 RID: 6023
	public EffectList m_activatePowerEffectsPlayer = new EffectList();

	// Token: 0x04001788 RID: 6024
	private string m_visualName = "";

	// Token: 0x04001789 RID: 6025
	private int m_visualVariant;

	// Token: 0x0400178A RID: 6026
	private GameObject m_visualItem;

	// Token: 0x0400178B RID: 6027
	[NonSerialized]
	public string m_currentItemName = "";

	// Token: 0x0400178C RID: 6028
	private ItemDrop.ItemData m_queuedItem;

	// Token: 0x0400178D RID: 6029
	private ZNetView m_nview;
}
