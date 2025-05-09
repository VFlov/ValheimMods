using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;
using UnityEngine;

// Token: 0x020000AF RID: 175
public class ItemDrop : MonoBehaviour, Hoverable, Interactable
{
	// Token: 0x06000B0F RID: 2831 RVA: 0x0005DDD8 File Offset: 0x0005BFD8
	private void Awake()
	{
		if (!string.IsNullOrEmpty(base.name))
		{
			this.m_nameHash = base.name.GetStableHashCode();
		}
		this.m_myIndex = ItemDrop.s_instances.Count;
		ItemDrop.s_instances.Add(this);
		string prefabName = this.GetPrefabName(base.gameObject.name);
		GameObject itemPrefab = ObjectDB.instance.GetItemPrefab(prefabName);
		this.m_itemData.m_dropPrefab = itemPrefab;
		if (Application.isEditor)
		{
			this.m_itemData.m_shared = itemPrefab.GetComponent<ItemDrop>().m_itemData.m_shared;
		}
		this.m_floating = base.GetComponent<Floating>();
		this.m_body = base.GetComponent<Rigidbody>();
		this.m_piece = base.GetComponent<Piece>();
		this.m_wnt = base.GetComponent<WearNTear>();
		if (this.m_wnt)
		{
			this.m_wnt.enabled = false;
		}
		if (this.m_body)
		{
			this.m_body.maxDepenetrationVelocity = 1f;
		}
		this.m_spawnTime = Time.time;
		this.m_nview = base.GetComponent<ZNetView>();
		if (this.m_nview && this.m_nview.IsValid())
		{
			if (this.m_nview.IsOwner())
			{
				DateTime dateTime = new DateTime(this.m_nview.GetZDO().GetLong(ZDOVars.s_spawnTime, 0L));
				if (dateTime.Ticks == 0L)
				{
					this.m_nview.GetZDO().Set(ZDOVars.s_spawnTime, ZNet.instance.GetTime().Ticks);
				}
			}
			this.m_nview.Register("RPC_RequestOwn", new Action<long>(this.RPC_RequestOwn));
			this.m_nview.Register("RPC_MakePiece", new Action<long>(this.RPC_MakePiece));
			this.Load();
			if (this.m_nview.GetZDO().GetBool(ZDOVars.s_piece, false))
			{
				this.MakePiece(false);
			}
			else if (this.m_pieceEnableObj)
			{
				this.m_pieceEnableObj.SetActive(false);
			}
			base.InvokeRepeating("SlowUpdate", UnityEngine.Random.Range(1f, 2f), 10f);
		}
		this.SetQuality(this.m_itemData.m_quality);
	}

	// Token: 0x06000B10 RID: 2832 RVA: 0x0005E00C File Offset: 0x0005C20C
	private void OnDestroy()
	{
		ItemDrop.s_instances[this.m_myIndex] = ItemDrop.s_instances[ItemDrop.s_instances.Count - 1];
		ItemDrop.s_instances[this.m_myIndex].m_myIndex = this.m_myIndex;
		ItemDrop.s_instances.RemoveAt(ItemDrop.s_instances.Count - 1);
	}

	// Token: 0x06000B11 RID: 2833 RVA: 0x0005E070 File Offset: 0x0005C270
	private void Start()
	{
		this.Save();
		IEquipmentVisual componentInChildren = base.gameObject.GetComponentInChildren<IEquipmentVisual>();
		if (componentInChildren != null)
		{
			componentInChildren.Setup(this.m_itemData.m_variant);
		}
	}

	// Token: 0x06000B12 RID: 2834 RVA: 0x0005E0A4 File Offset: 0x0005C2A4
	public static void OnCreateNew(GameObject go)
	{
		ItemDrop component = go.GetComponent<ItemDrop>();
		if (component != null)
		{
			ItemDrop.OnCreateNew(component);
		}
	}

	// Token: 0x06000B13 RID: 2835 RVA: 0x0005E0C1 File Offset: 0x0005C2C1
	public static void OnCreateNew(ItemDrop item)
	{
		item.m_itemData.m_worldLevel = (int)((byte)Game.m_worldLevel);
	}

	// Token: 0x06000B14 RID: 2836 RVA: 0x0005E0D4 File Offset: 0x0005C2D4
	private double GetTimeSinceSpawned()
	{
		DateTime d = new DateTime(this.m_nview.GetZDO().GetLong(ZDOVars.s_spawnTime, 0L));
		return (ZNet.instance.GetTime() - d).TotalSeconds;
	}

	// Token: 0x06000B15 RID: 2837 RVA: 0x0005E118 File Offset: 0x0005C318
	private void SlowUpdate()
	{
		if (!this.m_nview.IsValid() || !this.m_nview.IsOwner())
		{
			return;
		}
		this.TerrainCheck();
		if (this.m_autoDestroy)
		{
			this.TimedDestruction();
		}
		if (ItemDrop.s_instances.Count > 200)
		{
			this.AutoStackItems();
		}
	}

	// Token: 0x06000B16 RID: 2838 RVA: 0x0005E16C File Offset: 0x0005C36C
	private void TerrainCheck()
	{
		float groundHeight = ZoneSystem.instance.GetGroundHeight(base.transform.position);
		if (base.transform.position.y - groundHeight < -0.5f)
		{
			Vector3 position = base.transform.position;
			position.y = groundHeight + 0.5f;
			base.transform.position = position;
			Rigidbody component = base.GetComponent<Rigidbody>();
			if (component)
			{
				component.velocity = Vector3.zero;
			}
		}
	}

	// Token: 0x06000B17 RID: 2839 RVA: 0x0005E1E8 File Offset: 0x0005C3E8
	private void TimedDestruction()
	{
		if (this.GetTimeSinceSpawned() < 3600.0)
		{
			return;
		}
		if (this.IsInsideBase())
		{
			return;
		}
		if (Player.IsPlayerInRange(base.transform.position, 25f))
		{
			return;
		}
		if (this.InTar())
		{
			return;
		}
		if (this.IsPiece())
		{
			return;
		}
		this.m_nview.Destroy();
	}

	// Token: 0x06000B18 RID: 2840 RVA: 0x0005E245 File Offset: 0x0005C445
	public bool IsPiece()
	{
		return !this.m_body && this.m_piece && this.m_wnt;
	}

	// Token: 0x06000B19 RID: 2841 RVA: 0x0005E270 File Offset: 0x0005C470
	public void MakePiece(bool sendRPC = false)
	{
		if (!this.m_piece)
		{
			ZLog.LogError("Missing piece script to make piece out of " + base.name);
		}
		if (this.m_body)
		{
			UnityEngine.Object.Destroy(this.m_body);
			Collider componentInChildren = base.GetComponentInChildren<Collider>();
			if (componentInChildren != null && componentInChildren != null)
			{
				componentInChildren.gameObject.layer = LayerMask.NameToLayer("piece");
			}
		}
		if (this.m_wnt)
		{
			this.m_wnt.enabled = true;
		}
		if (this.m_nview && this.m_nview.IsValid())
		{
			if (this.m_nview.IsOwner())
			{
				this.m_nview.GetZDO().Set(ZDOVars.s_piece, true);
			}
			if (sendRPC)
			{
				this.m_nview.InvokeRPC(ZNetView.Everybody, "RPC_MakePiece", Array.Empty<object>());
			}
		}
		if (this.m_pieceEnableObj)
		{
			this.m_pieceEnableObj.SetActive(true);
		}
		if (this.m_pieceDisabledObj)
		{
			this.m_pieceDisabledObj.SetActive(false);
		}
	}

	// Token: 0x06000B1A RID: 2842 RVA: 0x0005E384 File Offset: 0x0005C584
	public void RPC_MakePiece(long sender)
	{
		this.MakePiece(false);
	}

	// Token: 0x06000B1B RID: 2843 RVA: 0x0005E38D File Offset: 0x0005C58D
	private bool IsInsideBase()
	{
		return base.transform.position.y > 28f && EffectArea.IsPointInsideArea(base.transform.position, EffectArea.Type.PlayerBase, 0f);
	}

	// Token: 0x06000B1C RID: 2844 RVA: 0x0005E3C8 File Offset: 0x0005C5C8
	private void AutoStackItems()
	{
		if (this.m_itemData.m_shared.m_maxStackSize <= 1 || this.m_itemData.m_stack >= this.m_itemData.m_shared.m_maxStackSize)
		{
			return;
		}
		if (this.m_haveAutoStacked)
		{
			return;
		}
		this.m_haveAutoStacked = true;
		if (ItemDrop.s_itemMask == 0)
		{
			ItemDrop.s_itemMask = LayerMask.GetMask(new string[]
			{
				"item"
			});
		}
		bool flag = false;
		foreach (Collider collider in Physics.OverlapSphere(base.transform.position, 4f, ItemDrop.s_itemMask))
		{
			if (collider.attachedRigidbody)
			{
				ItemDrop component = collider.attachedRigidbody.GetComponent<ItemDrop>();
				if (!(component == null) && !(component == this) && component.m_itemData.m_shared.m_autoStack && !(component.m_nview == null) && component.m_nview.IsValid() && component.m_nview.IsOwner() && !(component.m_itemData.m_shared.m_name != this.m_itemData.m_shared.m_name) && component.m_itemData.m_quality == this.m_itemData.m_quality)
				{
					int num = this.m_itemData.m_shared.m_maxStackSize - this.m_itemData.m_stack;
					if (num == 0)
					{
						break;
					}
					if (component.m_itemData.m_stack <= num)
					{
						this.m_itemData.m_stack += component.m_itemData.m_stack;
						flag = true;
						component.m_nview.Destroy();
					}
				}
			}
		}
		if (flag)
		{
			this.Save();
		}
	}

	// Token: 0x06000B1D RID: 2845 RVA: 0x0005E594 File Offset: 0x0005C794
	public string GetHoverText()
	{
		this.Load();
		string str = this.m_itemData.m_shared.m_name;
		if (this.m_itemData.m_quality > 1)
		{
			str = str + "[" + this.m_itemData.m_quality.ToString() + "] ";
		}
		if (this.m_itemData.m_shared.m_itemType == ItemDrop.ItemData.ItemType.Consumable && this.IsPiece())
		{
			return Localization.instance.Localize(str + "\n[<color=yellow><b>$KEY_Use</b></color>] " + (this.m_itemData.m_shared.m_isDrink ? "$item_drink" : "$item_eat"));
		}
		if (this.m_itemData.m_stack > 1)
		{
			str = str + " x" + this.m_itemData.m_stack.ToString();
		}
		return Localization.instance.Localize(str + "\n[<color=yellow><b>$KEY_Use</b></color>] $inventory_pickup");
	}

	// Token: 0x06000B1E RID: 2846 RVA: 0x0005E675 File Offset: 0x0005C875
	public string GetHoverName()
	{
		return this.m_itemData.m_shared.m_name;
	}

	// Token: 0x06000B1F RID: 2847 RVA: 0x0005E688 File Offset: 0x0005C888
	private string GetPrefabName(string name)
	{
		char[] anyOf = new char[]
		{
			'(',
			' '
		};
		int num = name.IndexOfAny(anyOf);
		string result;
		if (num >= 0)
		{
			result = name.Substring(0, num);
		}
		else
		{
			result = name;
		}
		return result;
	}

	// Token: 0x06000B20 RID: 2848 RVA: 0x0005E6C0 File Offset: 0x0005C8C0
	public bool Interact(Humanoid character, bool repeat, bool alt)
	{
		if (repeat)
		{
			return false;
		}
		if (this.InTar())
		{
			character.Message(MessageHud.MessageType.Center, "$hud_itemstucktar", 0, null);
			return true;
		}
		if (alt || this.m_itemData.m_shared.m_itemType != ItemDrop.ItemData.ItemType.Consumable || !this.IsPiece())
		{
			this.Pickup(character);
			return true;
		}
		if (Player.m_localPlayer.CanConsumeItem(this.m_itemData, false))
		{
			this.Eat();
			return true;
		}
		return false;
	}

	// Token: 0x06000B21 RID: 2849 RVA: 0x0005E730 File Offset: 0x0005C930
	public bool InTar()
	{
		if (this.m_body == null)
		{
			return false;
		}
		if (this.m_floating != null)
		{
			return this.m_floating.IsInTar();
		}
		Vector3 worldCenterOfMass = this.m_body.worldCenterOfMass;
		float liquidLevel = Floating.GetLiquidLevel(worldCenterOfMass, 1f, LiquidType.Tar);
		return worldCenterOfMass.y < liquidLevel;
	}

	// Token: 0x06000B22 RID: 2850 RVA: 0x0005E787 File Offset: 0x0005C987
	public bool UseItem(Humanoid user, ItemDrop.ItemData item)
	{
		return false;
	}

	// Token: 0x06000B23 RID: 2851 RVA: 0x0005E78C File Offset: 0x0005C98C
	public void SetStack(int stack)
	{
		if (!this.m_nview.IsValid() || !this.m_nview.IsOwner())
		{
			return;
		}
		this.m_itemData.m_stack = stack;
		if (this.m_itemData.m_stack > this.m_itemData.m_shared.m_maxStackSize)
		{
			this.m_itemData.m_stack = this.m_itemData.m_shared.m_maxStackSize;
		}
		this.Save();
	}

	// Token: 0x06000B24 RID: 2852 RVA: 0x0005E800 File Offset: 0x0005CA00
	public void Pickup(Humanoid character)
	{
		if (!this.m_nview.IsValid())
		{
			return;
		}
		if (this.CanPickup(true))
		{
			this.Load();
			character.Pickup(base.gameObject, true, true);
			this.Save();
			return;
		}
		this.m_pickupRequester = character;
		base.CancelInvoke("PickupUpdate");
		float num = 0.05f;
		base.InvokeRepeating("PickupUpdate", num, num);
		this.RequestOwn();
	}

	// Token: 0x06000B25 RID: 2853 RVA: 0x0005E86C File Offset: 0x0005CA6C
	public void RequestOwn()
	{
		if (Time.time - this.m_lastOwnerRequest < this.m_ownerRetryTimeout)
		{
			return;
		}
		if (this.m_nview.IsOwner())
		{
			return;
		}
		this.m_lastOwnerRequest = Time.time;
		this.m_ownerRetryTimeout = Mathf.Min(0.2f * Mathf.Pow(2f, (float)this.m_ownerRetryCounter), 30f);
		this.m_ownerRetryCounter++;
		this.m_nview.InvokeRPC("RPC_RequestOwn", Array.Empty<object>());
	}

	// Token: 0x06000B26 RID: 2854 RVA: 0x0005E8F4 File Offset: 0x0005CAF4
	public bool RemoveOne()
	{
		if (!this.CanPickup(true))
		{
			this.RequestOwn();
			return false;
		}
		if (this.m_itemData.m_stack <= 1)
		{
			this.m_nview.Destroy();
			return true;
		}
		this.m_itemData.m_stack--;
		this.Save();
		return true;
	}

	// Token: 0x06000B27 RID: 2855 RVA: 0x0005E947 File Offset: 0x0005CB47
	public void OnPlayerDrop()
	{
		this.m_autoPickup = false;
	}

	// Token: 0x06000B28 RID: 2856 RVA: 0x0005E950 File Offset: 0x0005CB50
	public bool CanEat()
	{
		return this.m_nview == null || !this.m_nview.IsValid() || this.m_nview.IsOwner();
	}

	// Token: 0x06000B29 RID: 2857 RVA: 0x0005E97A File Offset: 0x0005CB7A
	public void EatUpdate()
	{
		if (!this.m_nview.IsValid())
		{
			return;
		}
		if (this.CanEat())
		{
			base.CancelInvoke("EatUpdate");
			this.Eat();
		}
	}

	// Token: 0x06000B2A RID: 2858 RVA: 0x0005E9A4 File Offset: 0x0005CBA4
	public bool Eat()
	{
		if (!Player.m_localPlayer.CanConsumeItem(this.m_itemData, false))
		{
			return false;
		}
		if (!this.CanEat())
		{
			base.CancelInvoke("EatUpdate");
			float num = 0.05f;
			base.InvokeRepeating("EatUpdate", num, num);
			this.RequestOwn();
			return false;
		}
		if (this.m_itemData.m_shared.m_consumeStatusEffect)
		{
			Player.m_localPlayer.GetSEMan().AddStatusEffect(this.m_itemData.m_shared.m_consumeStatusEffect, true, 0, 0f);
		}
		if (this.m_itemData.m_shared.m_food > 0f)
		{
			Player.m_localPlayer.EatFood(this.m_itemData);
		}
		this.m_wnt.Remove(true);
		return true;
	}

	// Token: 0x06000B2B RID: 2859 RVA: 0x0005EA6C File Offset: 0x0005CC6C
	public bool CanPickup(bool autoPickupDelay = true)
	{
		if (this.m_nview == null || !this.m_nview.IsValid())
		{
			return true;
		}
		if (autoPickupDelay && (double)(Time.time - this.m_spawnTime) < 0.5)
		{
			return false;
		}
		if (this.m_nview.IsOwner())
		{
			this.m_ownerRetryCounter = 0;
			this.m_ownerRetryTimeout = 0f;
		}
		return this.m_nview.IsOwner();
	}

	// Token: 0x06000B2C RID: 2860 RVA: 0x0005EAE0 File Offset: 0x0005CCE0
	private void RPC_RequestOwn(long uid)
	{
		ZLog.Log(string.Concat(new string[]
		{
			"Player ",
			uid.ToString(),
			" wants to pickup ",
			base.gameObject.name,
			"   im: ",
			ZDOMan.GetSessionID().ToString()
		}));
		if (this.m_nview.IsOwner())
		{
			this.m_nview.GetZDO().SetOwner(uid);
			return;
		}
		if (this.m_nview.GetZDO().GetOwner() == uid)
		{
			ZLog.Log("  but they are already the owner");
			return;
		}
		ZLog.Log("  but neither I nor the requesting player are the owners");
	}

	// Token: 0x06000B2D RID: 2861 RVA: 0x0005EB84 File Offset: 0x0005CD84
	private void PickupUpdate()
	{
		if (!this.m_nview.IsValid())
		{
			return;
		}
		if (this.CanPickup(true))
		{
			ZLog.Log("Im finally the owner");
			base.CancelInvoke("PickupUpdate");
			this.Load();
			(this.m_pickupRequester as Player).Pickup(base.gameObject, true, true);
			this.Save();
			return;
		}
		ZLog.Log("Im still nto the owner");
	}

	// Token: 0x06000B2E RID: 2862 RVA: 0x0005EBF0 File Offset: 0x0005CDF0
	private void Save()
	{
		if (this.m_nview == null || !this.m_nview.IsValid())
		{
			return;
		}
		if (this.m_nview.IsOwner())
		{
			ItemDrop.SaveToZDO(this.m_itemData, this.m_nview.GetZDO());
		}
	}

	// Token: 0x06000B2F RID: 2863 RVA: 0x0005EC3C File Offset: 0x0005CE3C
	public void Load()
	{
		if (this.m_nview == null || !this.m_nview.IsValid())
		{
			return;
		}
		ZDO zdo = this.m_nview.GetZDO();
		if (zdo.DataRevision == this.m_loadedRevision)
		{
			return;
		}
		this.m_loadedRevision = zdo.DataRevision;
		ItemDrop.LoadFromZDO(this.m_itemData, zdo);
		this.SetQuality(this.m_itemData.m_quality);
	}

	// Token: 0x06000B30 RID: 2864 RVA: 0x0005ECA9 File Offset: 0x0005CEA9
	public void LoadFromExternalZDO(ZDO zdo)
	{
		ItemDrop.LoadFromZDO(this.m_itemData, zdo);
		ItemDrop.SaveToZDO(this.m_itemData, this.m_nview.GetZDO());
		this.SetQuality(this.m_itemData.m_quality);
	}

	// Token: 0x06000B31 RID: 2865 RVA: 0x0005ECE0 File Offset: 0x0005CEE0
	public static void SaveToZDO(ItemDrop.ItemData itemData, ZDO zdo)
	{
		zdo.Set(ZDOVars.s_durability, itemData.m_durability);
		zdo.Set(ZDOVars.s_stack, itemData.m_stack, false);
		zdo.Set(ZDOVars.s_quality, itemData.m_quality, false);
		zdo.Set(ZDOVars.s_variant, itemData.m_variant, false);
		zdo.Set(ZDOVars.s_crafterID, itemData.m_crafterID);
		zdo.Set(ZDOVars.s_crafterName, itemData.m_crafterName);
		zdo.Set(ZDOVars.s_dataCount, itemData.m_customData.Count, false);
		int num = 0;
		foreach (KeyValuePair<string, string> keyValuePair in itemData.m_customData)
		{
			zdo.Set(string.Format("data_{0}", num), keyValuePair.Key);
			zdo.Set(string.Format("data__{0}", num++), keyValuePair.Value);
		}
		zdo.Set(ZDOVars.s_worldLevel, itemData.m_worldLevel, false);
		zdo.Set(ZDOVars.s_pickedUp, itemData.m_pickedUp);
	}

	// Token: 0x06000B32 RID: 2866 RVA: 0x0005EE10 File Offset: 0x0005D010
	private static void LoadFromZDO(ItemDrop.ItemData itemData, ZDO zdo)
	{
		itemData.m_durability = zdo.GetFloat(ZDOVars.s_durability, itemData.m_durability);
		itemData.m_stack = zdo.GetInt(ZDOVars.s_stack, itemData.m_stack);
		itemData.m_quality = zdo.GetInt(ZDOVars.s_quality, itemData.m_quality);
		itemData.m_variant = zdo.GetInt(ZDOVars.s_variant, itemData.m_variant);
		itemData.m_crafterID = zdo.GetLong(ZDOVars.s_crafterID, itemData.m_crafterID);
		itemData.m_crafterName = zdo.GetString(ZDOVars.s_crafterName, itemData.m_crafterName);
		int @int = zdo.GetInt(ZDOVars.s_dataCount, 0);
		itemData.m_customData.Clear();
		for (int i = 0; i < @int; i++)
		{
			itemData.m_customData[zdo.GetString(string.Format("data_{0}", i), "")] = zdo.GetString(string.Format("data__{0}", i), "");
		}
		itemData.m_worldLevel = (int)((byte)zdo.GetInt(ZDOVars.s_worldLevel, itemData.m_worldLevel));
		itemData.m_pickedUp = zdo.GetBool(ZDOVars.s_pickedUp, itemData.m_pickedUp);
	}

	// Token: 0x06000B33 RID: 2867 RVA: 0x0005EF3C File Offset: 0x0005D13C
	public static void SaveToZDO(int index, ItemDrop.ItemData itemData, ZDO zdo)
	{
		zdo.Set(index.ToString() + "_durability", itemData.m_durability);
		zdo.Set(index.ToString() + "_stack", itemData.m_stack);
		zdo.Set(index.ToString() + "_quality", itemData.m_quality);
		zdo.Set(index.ToString() + "_variant", itemData.m_variant);
		zdo.Set(index.ToString() + "_crafterID", itemData.m_crafterID);
		zdo.Set(index.ToString() + "_crafterName", itemData.m_crafterName);
		zdo.Set(index.ToString() + "_dataCount", itemData.m_customData.Count);
		int num = 0;
		foreach (KeyValuePair<string, string> keyValuePair in itemData.m_customData)
		{
			zdo.Set(string.Format("{0}_data_{1}", index, num), keyValuePair.Key);
			zdo.Set(string.Format("{0}_data__{1}", index, num++), keyValuePair.Value);
		}
		zdo.Set(index.ToString() + "_worldLevel", itemData.m_worldLevel);
		zdo.Set(index.ToString() + "_pickedUp", itemData.m_pickedUp);
	}

	// Token: 0x06000B34 RID: 2868 RVA: 0x0005F0E0 File Offset: 0x0005D2E0
	public static void LoadFromZDO(int index, ItemDrop.ItemData itemData, ZDO zdo)
	{
		itemData.m_durability = zdo.GetFloat(index.ToString() + "_durability", itemData.m_durability);
		itemData.m_stack = zdo.GetInt(index.ToString() + "_stack", itemData.m_stack);
		itemData.m_quality = zdo.GetInt(index.ToString() + "_quality", itemData.m_quality);
		itemData.m_variant = zdo.GetInt(index.ToString() + "_variant", itemData.m_variant);
		itemData.m_crafterID = zdo.GetLong(index.ToString() + "_crafterID", itemData.m_crafterID);
		itemData.m_crafterName = zdo.GetString(index.ToString() + "_crafterName", itemData.m_crafterName);
		int @int = zdo.GetInt(index.ToString() + "_dataCount", 0);
		for (int i = 0; i < @int; i++)
		{
			itemData.m_customData[zdo.GetString(string.Format("{0}_data_{1}", index, i), "")] = zdo.GetString(string.Format("{0}_data__{1}", index, i), "");
		}
		itemData.m_worldLevel = (int)((byte)zdo.GetInt(index.ToString() + "_worldLevel", itemData.m_worldLevel));
		itemData.m_pickedUp = zdo.GetBool(index.ToString() + "_pickedUp", itemData.m_pickedUp);
	}

	// Token: 0x06000B35 RID: 2869 RVA: 0x0005F278 File Offset: 0x0005D478
	public static ItemDrop DropItem(ItemDrop.ItemData item, int amount, Vector3 position, Quaternion rotation)
	{
		ItemDrop component = UnityEngine.Object.Instantiate<GameObject>(item.m_dropPrefab, position, rotation).GetComponent<ItemDrop>();
		component.m_itemData = item.Clone();
		if (component.m_itemData.m_quality > 1)
		{
			component.SetQuality(component.m_itemData.m_quality);
		}
		if (amount > 0)
		{
			component.m_itemData.m_stack = amount;
		}
		if (component.m_onDrop != null)
		{
			component.m_onDrop(component);
		}
		component.Save();
		return component;
	}

	// Token: 0x06000B36 RID: 2870 RVA: 0x0005F2EE File Offset: 0x0005D4EE
	public void SetQuality(int quality)
	{
		this.m_itemData.m_quality = quality;
		base.transform.localScale = this.m_itemData.GetScale();
	}

	// Token: 0x06000B37 RID: 2871 RVA: 0x0005F312 File Offset: 0x0005D512
	public int NameHash()
	{
		return this.m_nameHash;
	}

	// Token: 0x04000C5A RID: 3162
	private static List<ItemDrop> s_instances = new List<ItemDrop>();

	// Token: 0x04000C5B RID: 3163
	private int m_myIndex = -1;

	// Token: 0x04000C5C RID: 3164
	public bool m_autoPickup = true;

	// Token: 0x04000C5D RID: 3165
	public bool m_autoDestroy = true;

	// Token: 0x04000C5E RID: 3166
	public ItemDrop.ItemData m_itemData = new ItemDrop.ItemData();

	// Token: 0x04000C5F RID: 3167
	[HideInInspector]
	public Action<ItemDrop> m_onDrop;

	// Token: 0x04000C60 RID: 3168
	public GameObject m_pieceEnableObj;

	// Token: 0x04000C61 RID: 3169
	public GameObject m_pieceDisabledObj;

	// Token: 0x04000C62 RID: 3170
	private int m_nameHash;

	// Token: 0x04000C63 RID: 3171
	private Floating m_floating;

	// Token: 0x04000C64 RID: 3172
	private Rigidbody m_body;

	// Token: 0x04000C65 RID: 3173
	private ZNetView m_nview;

	// Token: 0x04000C66 RID: 3174
	private Character m_pickupRequester;

	// Token: 0x04000C67 RID: 3175
	private float m_lastOwnerRequest;

	// Token: 0x04000C68 RID: 3176
	private int m_ownerRetryCounter;

	// Token: 0x04000C69 RID: 3177
	private float m_ownerRetryTimeout;

	// Token: 0x04000C6A RID: 3178
	private float m_spawnTime;

	// Token: 0x04000C6B RID: 3179
	private Piece m_piece;

	// Token: 0x04000C6C RID: 3180
	private WearNTear m_wnt;

	// Token: 0x04000C6D RID: 3181
	private uint m_loadedRevision = uint.MaxValue;

	// Token: 0x04000C6E RID: 3182
	private const double c_AutoDestroyTimeout = 3600.0;

	// Token: 0x04000C6F RID: 3183
	private const double c_AutoPickupDelay = 0.5;

	// Token: 0x04000C70 RID: 3184
	private const float c_AutoDespawnBaseMinAltitude = -2f;

	// Token: 0x04000C71 RID: 3185
	private const int c_AutoStackThreshold = 200;

	// Token: 0x04000C72 RID: 3186
	private const float c_AutoStackRange = 4f;

	// Token: 0x04000C73 RID: 3187
	private bool m_haveAutoStacked;

	// Token: 0x04000C74 RID: 3188
	private static int s_itemMask = 0;

	// Token: 0x020002D4 RID: 724
	[Serializable]
	public class ItemData
	{
		// Token: 0x06002128 RID: 8488 RVA: 0x000E9E40 File Offset: 0x000E8040
		public ItemDrop.ItemData Clone()
		{
			ItemDrop.ItemData itemData = base.MemberwiseClone() as ItemDrop.ItemData;
			itemData.m_customData = new Dictionary<string, string>(this.m_customData);
			return itemData;
		}

		// Token: 0x06002129 RID: 8489 RVA: 0x000E9E60 File Offset: 0x000E8060
		public bool IsEquipable()
		{
			return this.m_shared.m_itemType == ItemDrop.ItemData.ItemType.Tool || this.m_shared.m_itemType == ItemDrop.ItemData.ItemType.OneHandedWeapon || this.m_shared.m_itemType == ItemDrop.ItemData.ItemType.TwoHandedWeapon || this.m_shared.m_itemType == ItemDrop.ItemData.ItemType.TwoHandedWeaponLeft || this.m_shared.m_itemType == ItemDrop.ItemData.ItemType.Bow || this.m_shared.m_itemType == ItemDrop.ItemData.ItemType.Shield || this.m_shared.m_itemType == ItemDrop.ItemData.ItemType.Helmet || this.m_shared.m_itemType == ItemDrop.ItemData.ItemType.Chest || this.m_shared.m_itemType == ItemDrop.ItemData.ItemType.Legs || this.m_shared.m_itemType == ItemDrop.ItemData.ItemType.Shoulder || this.m_shared.m_itemType == ItemDrop.ItemData.ItemType.Ammo || this.m_shared.m_itemType == ItemDrop.ItemData.ItemType.Torch || this.m_shared.m_itemType == ItemDrop.ItemData.ItemType.Utility;
		}

		// Token: 0x0600212A RID: 8490 RVA: 0x000E9F3C File Offset: 0x000E813C
		public bool IsWeapon()
		{
			return this.m_shared.m_itemType == ItemDrop.ItemData.ItemType.OneHandedWeapon || this.m_shared.m_itemType == ItemDrop.ItemData.ItemType.Bow || this.m_shared.m_itemType == ItemDrop.ItemData.ItemType.TwoHandedWeapon || this.m_shared.m_itemType == ItemDrop.ItemData.ItemType.TwoHandedWeaponLeft || this.m_shared.m_itemType == ItemDrop.ItemData.ItemType.Torch;
		}

		// Token: 0x0600212B RID: 8491 RVA: 0x000E9F94 File Offset: 0x000E8194
		public bool IsTwoHanded()
		{
			return this.m_shared.m_itemType == ItemDrop.ItemData.ItemType.TwoHandedWeapon || this.m_shared.m_itemType == ItemDrop.ItemData.ItemType.TwoHandedWeaponLeft || this.m_shared.m_itemType == ItemDrop.ItemData.ItemType.Bow;
		}

		// Token: 0x0600212C RID: 8492 RVA: 0x000E9FC4 File Offset: 0x000E81C4
		public bool HavePrimaryAttack()
		{
			return !string.IsNullOrEmpty(this.m_shared.m_attack.m_attackAnimation);
		}

		// Token: 0x0600212D RID: 8493 RVA: 0x000E9FDE File Offset: 0x000E81DE
		public bool HaveSecondaryAttack()
		{
			return !string.IsNullOrEmpty(this.m_shared.m_secondaryAttack.m_attackAnimation);
		}

		// Token: 0x0600212E RID: 8494 RVA: 0x000E9FF8 File Offset: 0x000E81F8
		public float GetArmor()
		{
			return this.GetArmor(this.m_quality, (float)this.m_worldLevel);
		}

		// Token: 0x0600212F RID: 8495 RVA: 0x000EA00D File Offset: 0x000E820D
		public float GetArmor(int quality, float worldLevel)
		{
			return this.m_shared.m_armor + (float)Mathf.Max(0, quality - 1) * this.m_shared.m_armorPerLevel + worldLevel * (float)Game.instance.m_worldLevelGearBaseAC;
		}

		// Token: 0x06002130 RID: 8496 RVA: 0x000EA03F File Offset: 0x000E823F
		public bool TryGetArmorDifference(out float difference)
		{
			if (Player.m_localPlayer != null)
			{
				return Player.m_localPlayer.TryGetArmorDifference(this, out difference);
			}
			difference = 0f;
			return false;
		}

		// Token: 0x06002131 RID: 8497 RVA: 0x000EA063 File Offset: 0x000E8263
		public int GetValue()
		{
			return this.m_shared.m_value * this.m_stack;
		}

		// Token: 0x06002132 RID: 8498 RVA: 0x000EA078 File Offset: 0x000E8278
		public float GetWeight(int stackOverride = -1)
		{
			int num = (stackOverride >= 0) ? stackOverride : this.m_stack;
			float num2 = this.m_shared.m_weight * (float)num;
			if (this.m_shared.m_scaleWeightByQuality != 0f && this.m_quality != 1)
			{
				num2 += num2 * (float)(this.m_quality - 1) * this.m_shared.m_scaleWeightByQuality;
			}
			return num2;
		}

		// Token: 0x06002133 RID: 8499 RVA: 0x000EA0D8 File Offset: 0x000E82D8
		public float GetNonStackedWeight()
		{
			float num = this.m_shared.m_weight;
			if (this.m_shared.m_scaleWeightByQuality != 0f && this.m_quality != 1)
			{
				num += num * (float)(this.m_quality - 1) * this.m_shared.m_scaleWeightByQuality;
			}
			return num;
		}

		// Token: 0x06002134 RID: 8500 RVA: 0x000EA127 File Offset: 0x000E8327
		public HitData.DamageTypes GetDamage()
		{
			return this.GetDamage(this.m_quality, (float)this.m_worldLevel);
		}

		// Token: 0x06002135 RID: 8501 RVA: 0x000EA13C File Offset: 0x000E833C
		public float GetDurabilityPercentage()
		{
			float maxDurability = this.GetMaxDurability();
			if (maxDurability == 0f)
			{
				return 1f;
			}
			return Mathf.Clamp01(this.m_durability / maxDurability);
		}

		// Token: 0x06002136 RID: 8502 RVA: 0x000EA16B File Offset: 0x000E836B
		public float GetMaxDurability()
		{
			return this.GetMaxDurability(this.m_quality);
		}

		// Token: 0x06002137 RID: 8503 RVA: 0x000EA179 File Offset: 0x000E8379
		public float GetMaxDurability(int quality)
		{
			return this.m_shared.m_maxDurability + (float)Mathf.Max(0, quality - 1) * this.m_shared.m_durabilityPerLevel;
		}

		// Token: 0x06002138 RID: 8504 RVA: 0x000EA1A0 File Offset: 0x000E83A0
		public HitData.DamageTypes GetDamage(int quality, float worldLevel)
		{
			HitData.DamageTypes damages = this.m_shared.m_damages;
			if (quality > 1)
			{
				damages.Add(this.m_shared.m_damagesPerLevel, quality - 1);
			}
			if (worldLevel > 0f)
			{
				damages.IncreaseEqually(worldLevel * (float)Game.instance.m_worldLevelGearBaseDamage, true);
			}
			return damages;
		}

		// Token: 0x06002139 RID: 8505 RVA: 0x000EA1F0 File Offset: 0x000E83F0
		public float GetBaseBlockPower()
		{
			return this.GetBaseBlockPower(this.m_quality);
		}

		// Token: 0x0600213A RID: 8506 RVA: 0x000EA1FE File Offset: 0x000E83FE
		public float GetBaseBlockPower(int quality)
		{
			return this.m_shared.m_blockPower + (float)Mathf.Max(0, quality - 1) * this.m_shared.m_blockPowerPerLevel;
		}

		// Token: 0x0600213B RID: 8507 RVA: 0x000EA222 File Offset: 0x000E8422
		public float GetBlockPower(float skillFactor)
		{
			return this.GetBlockPower(this.m_quality, skillFactor);
		}

		// Token: 0x0600213C RID: 8508 RVA: 0x000EA231 File Offset: 0x000E8431
		public float GetBlockPower(int quality, float skillFactor)
		{
			float baseBlockPower = this.GetBaseBlockPower(quality);
			return baseBlockPower + baseBlockPower * skillFactor * 0.5f;
		}

		// Token: 0x0600213D RID: 8509 RVA: 0x000EA244 File Offset: 0x000E8444
		public float GetBlockPowerTooltip(int quality)
		{
			if (Player.m_localPlayer == null)
			{
				return 0f;
			}
			float skillFactor = Player.m_localPlayer.GetSkillFactor(Skills.SkillType.Blocking);
			return this.GetBlockPower(quality, skillFactor);
		}

		// Token: 0x0600213E RID: 8510 RVA: 0x000EA278 File Offset: 0x000E8478
		public float GetDrawStaminaDrain()
		{
			if (this.m_shared.m_attack.m_drawStaminaDrain <= 0f)
			{
				return 0f;
			}
			float drawStaminaDrain = this.m_shared.m_attack.m_drawStaminaDrain;
			float skillFactor = Player.m_localPlayer.GetSkillFactor(this.m_shared.m_skillType);
			return drawStaminaDrain - drawStaminaDrain * 0.33f * skillFactor;
		}

		// Token: 0x0600213F RID: 8511 RVA: 0x000EA2D4 File Offset: 0x000E84D4
		public float GetDrawEitrDrain()
		{
			if (this.m_shared.m_attack.m_drawEitrDrain <= 0f)
			{
				return 0f;
			}
			float drawEitrDrain = this.m_shared.m_attack.m_drawEitrDrain;
			float skillFactor = Player.m_localPlayer.GetSkillFactor(this.m_shared.m_skillType);
			return drawEitrDrain - drawEitrDrain * 0.33f * skillFactor;
		}

		// Token: 0x06002140 RID: 8512 RVA: 0x000EA330 File Offset: 0x000E8530
		public float GetWeaponLoadingTime()
		{
			if (this.m_shared.m_attack.m_requiresReload)
			{
				float skillFactor = Player.m_localPlayer.GetSkillFactor(this.m_shared.m_skillType);
				return Mathf.Lerp(this.m_shared.m_attack.m_reloadTime, this.m_shared.m_attack.m_reloadTime * 0.5f, skillFactor);
			}
			return 1f;
		}

		// Token: 0x06002141 RID: 8513 RVA: 0x000EA397 File Offset: 0x000E8597
		public float GetDeflectionForce()
		{
			return this.GetDeflectionForce(this.m_quality);
		}

		// Token: 0x06002142 RID: 8514 RVA: 0x000EA3A5 File Offset: 0x000E85A5
		public float GetDeflectionForce(int quality)
		{
			return this.m_shared.m_deflectionForce + (float)Mathf.Max(0, quality - 1) * this.m_shared.m_deflectionForcePerLevel;
		}

		// Token: 0x06002143 RID: 8515 RVA: 0x000EA3C9 File Offset: 0x000E85C9
		public Vector3 GetScale()
		{
			return this.GetScale((float)this.m_quality);
		}

		// Token: 0x06002144 RID: 8516 RVA: 0x000EA3D8 File Offset: 0x000E85D8
		public Vector3 GetScale(float quality)
		{
			float num = 1f + (quality - 1f) * this.m_shared.m_scaleByQuality;
			return new Vector3(num, num, num);
		}

		// Token: 0x06002145 RID: 8517 RVA: 0x000EA3FA File Offset: 0x000E85FA
		public string GetTooltip(int stackOverride = -1)
		{
			return ItemDrop.ItemData.GetTooltip(this, this.m_quality, false, (float)this.m_worldLevel, stackOverride);
		}

		// Token: 0x06002146 RID: 8518 RVA: 0x000EA411 File Offset: 0x000E8611
		public Sprite GetIcon()
		{
			return this.m_shared.m_icons[this.m_variant];
		}

		// Token: 0x06002147 RID: 8519 RVA: 0x000EA428 File Offset: 0x000E8628
		private static void AddHandedTip(ItemDrop.ItemData item, StringBuilder text)
		{
			ItemDrop.ItemData.ItemType itemType = item.m_shared.m_itemType;
			if (itemType <= ItemDrop.ItemData.ItemType.TwoHandedWeapon)
			{
				switch (itemType)
				{
				case ItemDrop.ItemData.ItemType.OneHandedWeapon:
				case ItemDrop.ItemData.ItemType.Shield:
					break;
				case ItemDrop.ItemData.ItemType.Bow:
					goto IL_48;
				default:
					if (itemType != ItemDrop.ItemData.ItemType.TwoHandedWeapon)
					{
						return;
					}
					goto IL_48;
				}
			}
			else if (itemType != ItemDrop.ItemData.ItemType.Torch)
			{
				if (itemType != ItemDrop.ItemData.ItemType.Tool && itemType != ItemDrop.ItemData.ItemType.TwoHandedWeaponLeft)
				{
					return;
				}
				goto IL_48;
			}
			text.Append("\n$item_onehanded");
			return;
			IL_48:
			text.Append("\n$item_twohanded");
		}

		// Token: 0x06002148 RID: 8520 RVA: 0x000EA48C File Offset: 0x000E868C
		private static void AddBlockTooltip(ItemDrop.ItemData item, int qualityLevel, StringBuilder text)
		{
			float baseBlockPower = item.GetBaseBlockPower(qualityLevel);
			if (baseBlockPower > 1f)
			{
				text.AppendFormat("\n$item_blockarmor: <color=orange>{0}</color> <color=yellow>({1})</color>", baseBlockPower, item.GetBlockPowerTooltip(qualityLevel).ToString("0"));
			}
			float deflectionForce = item.GetDeflectionForce(qualityLevel);
			if (deflectionForce > 1f)
			{
				text.AppendFormat("\n$item_blockforce: <color=orange>{0}</color>", deflectionForce);
			}
			if (item.m_shared.m_timedBlockBonus > 1f)
			{
				text.AppendFormat("\n$item_parrybonus: <color=orange>{0}x</color>", item.m_shared.m_timedBlockBonus);
			}
			string damageModifiersTooltipString = SE_Stats.GetDamageModifiersTooltipString(item.m_shared.m_damageModifiers);
			if (damageModifiersTooltipString.Length > 0)
			{
				text.Append(damageModifiersTooltipString);
			}
		}

		// Token: 0x06002149 RID: 8521 RVA: 0x000EA544 File Offset: 0x000E8744
		public static string GetTooltip(ItemDrop.ItemData item, int qualityLevel, bool crafting, float worldLevel, int stackOverride = -1)
		{
			ItemDrop.ItemData.<>c__DisplayClass35_0 CS$<>8__locals1;
			CS$<>8__locals1.player = Player.m_localPlayer;
			ItemDrop.ItemData.m_stringBuilder.Clear();
			ItemDrop.ItemData.m_stringBuilder.Append(item.m_shared.m_description);
			ItemDrop.ItemData.m_stringBuilder.Append("\n");
			if (item.m_shared.m_dlc.Length > 0)
			{
				ItemDrop.ItemData.m_stringBuilder.Append("\n<color=#00FFFF>$item_dlc</color>");
			}
			if (item.m_worldLevel > 0)
			{
				ItemDrop.ItemData.m_stringBuilder.Append("\n<color=orange>$item_newgameplusitem " + ((item.m_worldLevel != 1) ? item.m_worldLevel.ToString() : "") + "</color>");
			}
			ItemDrop.ItemData.AddHandedTip(item, ItemDrop.ItemData.m_stringBuilder);
			if (item.m_crafterID != 0L)
			{
				ItemDrop.ItemData.m_stringBuilder.AppendFormat("\n$item_crafter: <color=orange>{0}</color>", CensorShittyWords.FilterUGC(item.m_crafterName, UGCType.CharacterName, item.m_crafterID));
			}
			if (!item.m_shared.m_teleportable && !ZoneSystem.instance.GetGlobalKey(GlobalKeys.TeleportAll))
			{
				ItemDrop.ItemData.m_stringBuilder.Append("\n<color=orange>$item_noteleport</color>");
			}
			if (item.m_shared.m_value > 0)
			{
				ItemDrop.ItemData.m_stringBuilder.AppendFormat("\n$item_value: <color=orange>{0} ({1} $item_total)</color>", item.m_shared.m_value, item.GetValue());
			}
			if (item.m_shared.m_maxStackSize > 1)
			{
				ItemDrop.ItemData.m_stringBuilder.AppendFormat("\n$item_weight: <color=orange>{0} ({1} $item_total)</color>", item.GetNonStackedWeight().ToString("0.0"), item.GetWeight(stackOverride).ToString("0.0"));
			}
			else
			{
				ItemDrop.ItemData.m_stringBuilder.AppendFormat("\n$item_weight: <color=orange>{0}</color>", item.GetWeight(-1).ToString("0.0"));
			}
			if (item.m_shared.m_maxQuality > 1 && !crafting)
			{
				ItemDrop.ItemData.m_stringBuilder.AppendFormat("\n$item_quality: <color=orange>{0}</color>", qualityLevel);
			}
			if (item.m_shared.m_useDurability)
			{
				if (crafting)
				{
					float maxDurability = item.GetMaxDurability(qualityLevel);
					ItemDrop.ItemData.m_stringBuilder.AppendFormat("\n$item_durability: <color=orange>{0}</color>", maxDurability);
				}
				else
				{
					float maxDurability2 = item.GetMaxDurability(qualityLevel);
					float durability = item.m_durability;
					ItemDrop.ItemData.m_stringBuilder.AppendFormat("\n$item_durability: <color=orange>{0}%</color> <color=yellow>({1}/{2})</color>", (item.GetDurabilityPercentage() * 100f).ToString("0"), durability.ToString("0"), maxDurability2.ToString("0"));
				}
				if (item.m_shared.m_canBeReparied && !crafting)
				{
					Recipe recipe = ObjectDB.instance.GetRecipe(item);
					if (recipe != null)
					{
						int minStationLevel = recipe.m_minStationLevel;
						ItemDrop.ItemData.m_stringBuilder.AppendFormat("\n$item_repairlevel: <color=orange>{0}</color>", minStationLevel.ToString());
					}
				}
			}
			switch (item.m_shared.m_itemType)
			{
			case ItemDrop.ItemData.ItemType.Consumable:
				ItemDrop.ItemData.<GetTooltip>g__printConsumable|35_0(item, ref CS$<>8__locals1);
				break;
			case ItemDrop.ItemData.ItemType.OneHandedWeapon:
			case ItemDrop.ItemData.ItemType.Bow:
			case ItemDrop.ItemData.ItemType.TwoHandedWeapon:
			case ItemDrop.ItemData.ItemType.Torch:
			case ItemDrop.ItemData.ItemType.TwoHandedWeaponLeft:
			{
				ItemDrop.ItemData.m_stringBuilder.Append(item.GetDamage(qualityLevel, worldLevel).GetTooltipString(item.m_shared.m_skillType));
				if (item.m_shared.m_attack.m_damageMultiplierByTotalHealthMissing > 0f)
				{
					ItemDrop.ItemData.m_stringBuilder.AppendFormat("\n$item_damagemultipliertotal: <color=orange>{0}%</color>", item.m_shared.m_attack.m_damageMultiplierByTotalHealthMissing * 100f);
				}
				if (item.m_shared.m_attack.m_damageMultiplierPerMissingHP > 0f)
				{
					ItemDrop.ItemData.m_stringBuilder.AppendFormat("\n$item_damagemultiplierhp: <color=orange>{0}%</color>", item.m_shared.m_attack.m_damageMultiplierPerMissingHP * 100f);
				}
				if (item.m_shared.m_attack.m_attackStamina > 0f)
				{
					ItemDrop.ItemData.m_stringBuilder.AppendFormat("\n$item_staminause: <color=orange>{0}</color>", item.m_shared.m_attack.m_attackStamina);
				}
				if (item.m_shared.m_attack.m_attackEitr > 0f)
				{
					ItemDrop.ItemData.m_stringBuilder.AppendFormat("\n$item_eitruse: <color=orange>{0}</color>", item.m_shared.m_attack.m_attackEitr);
				}
				if (item.m_shared.m_attack.m_attackHealth > 0f)
				{
					ItemDrop.ItemData.m_stringBuilder.AppendFormat("\n$item_healthuse: <color=orange>{0}</color>", item.m_shared.m_attack.m_attackHealth);
				}
				if (item.m_shared.m_attack.m_attackHealthReturnHit > 0f)
				{
					ItemDrop.ItemData.m_stringBuilder.AppendFormat("\n$item_healthhitreturn: <color=orange>{0}</color>", item.m_shared.m_attack.m_attackHealthReturnHit);
				}
				if (item.m_shared.m_attack.m_attackHealthPercentage > 0f)
				{
					ItemDrop.ItemData.m_stringBuilder.AppendFormat("\n$item_healthuse: <color=orange>{0}%</color>", item.m_shared.m_attack.m_attackHealthPercentage.ToString("0.0"));
				}
				if (item.m_shared.m_attack.m_drawStaminaDrain > 0f)
				{
					ItemDrop.ItemData.m_stringBuilder.AppendFormat("\n$item_staminahold: <color=orange>{0}</color>/s", item.m_shared.m_attack.m_drawStaminaDrain);
				}
				ItemDrop.ItemData.AddBlockTooltip(item, qualityLevel, ItemDrop.ItemData.m_stringBuilder);
				if (item.m_shared.m_attackForce > 0f)
				{
					ItemDrop.ItemData.m_stringBuilder.AppendFormat("\n$item_knockback: <color=orange>{0}</color>", item.m_shared.m_attackForce);
				}
				if (item.m_shared.m_backstabBonus > 1f)
				{
					ItemDrop.ItemData.m_stringBuilder.AppendFormat("\n$item_backstab: <color=orange>{0}x</color>", item.m_shared.m_backstabBonus);
				}
				if (item.m_shared.m_tamedOnly)
				{
					ItemDrop.ItemData.m_stringBuilder.AppendFormat("\n<color=orange>$item_tamedonly</color>", Array.Empty<object>());
				}
				string projectileTooltip = item.GetProjectileTooltip(qualityLevel);
				if (projectileTooltip.Length > 0 && item.m_shared.m_projectileToolTip)
				{
					ItemDrop.ItemData.m_stringBuilder.Append("\n\n");
					ItemDrop.ItemData.m_stringBuilder.Append(projectileTooltip);
				}
				break;
			}
			case ItemDrop.ItemData.ItemType.Shield:
				ItemDrop.ItemData.AddBlockTooltip(item, qualityLevel, ItemDrop.ItemData.m_stringBuilder);
				break;
			case ItemDrop.ItemData.ItemType.Helmet:
			case ItemDrop.ItemData.ItemType.Chest:
			case ItemDrop.ItemData.ItemType.Legs:
			case ItemDrop.ItemData.ItemType.Shoulder:
			{
				ItemDrop.ItemData.m_stringBuilder.AppendFormat("\n$item_armor: <color=orange>{0}</color>", item.GetArmor(qualityLevel, worldLevel));
				string damageModifiersTooltipString = SE_Stats.GetDamageModifiersTooltipString(item.m_shared.m_damageModifiers);
				if (damageModifiersTooltipString.Length > 0)
				{
					ItemDrop.ItemData.m_stringBuilder.Append(damageModifiersTooltipString);
				}
				break;
			}
			case ItemDrop.ItemData.ItemType.Ammo:
			case ItemDrop.ItemData.ItemType.AmmoNonEquipable:
				ItemDrop.ItemData.m_stringBuilder.Append(item.GetDamage(qualityLevel, worldLevel).GetTooltipString(item.m_shared.m_skillType));
				ItemDrop.ItemData.m_stringBuilder.AppendFormat("\n$item_knockback: <color=orange>{0}</color>", item.m_shared.m_attackForce);
				break;
			}
			float skillLevel = Player.m_localPlayer.GetSkillLevel(item.m_shared.m_skillType);
			string statusEffectTooltip = item.GetStatusEffectTooltip(qualityLevel, skillLevel);
			if (statusEffectTooltip.Length > 0)
			{
				ItemDrop.ItemData.m_stringBuilder.Append("\n\n");
				ItemDrop.ItemData.m_stringBuilder.Append(statusEffectTooltip);
			}
			string chainTooltip = item.GetChainTooltip(qualityLevel, skillLevel);
			if (chainTooltip.Length > 0)
			{
				ItemDrop.ItemData.m_stringBuilder.Append("\n\n");
				ItemDrop.ItemData.m_stringBuilder.Append(chainTooltip);
			}
			if (item.m_shared.m_eitrRegenModifier > 0f && CS$<>8__locals1.player != null)
			{
				float equipmentEitrRegenModifier = CS$<>8__locals1.player.GetEquipmentEitrRegenModifier();
				ItemDrop.ItemData.m_stringBuilder.AppendFormat("\n$item_eitrregen_modifier: <color=orange>{0}%</color> ($item_total:<color=yellow>{1}%</color>)", (item.m_shared.m_eitrRegenModifier * 100f).ToString("+0;-0"), (equipmentEitrRegenModifier * 100f).ToString("+0;-0"));
			}
			if (CS$<>8__locals1.player != null)
			{
				CS$<>8__locals1.player.AppendEquipmentModifierTooltips(item, ItemDrop.ItemData.m_stringBuilder);
			}
			string setStatusEffectTooltip = item.GetSetStatusEffectTooltip(qualityLevel, skillLevel);
			if (setStatusEffectTooltip.Length > 0)
			{
				ItemDrop.ItemData.m_stringBuilder.AppendFormat("\n\n$item_seteffect (<color=orange>{0}</color> $item_parts):<color=orange>{1}</color>\n{2}", item.m_shared.m_setSize, item.m_shared.m_setStatusEffect.m_name, setStatusEffectTooltip);
			}
			if (item.m_shared.m_appendToolTip && !ItemDrop.ItemData.<GetTooltip>g__printConsumable|35_0(item.m_shared.m_appendToolTip.m_itemData, ref CS$<>8__locals1))
			{
				return ItemDrop.ItemData.m_stringBuilder.ToString() + "\n\n" + ItemDrop.ItemData.GetTooltip(item.m_shared.m_appendToolTip.m_itemData, qualityLevel, crafting, worldLevel, -1);
			}
			return ItemDrop.ItemData.m_stringBuilder.ToString();
		}

		// Token: 0x0600214A RID: 8522 RVA: 0x000EADA4 File Offset: 0x000E8FA4
		public static string GetDurationString(float time)
		{
			int num = Mathf.CeilToInt(time);
			int num2 = (int)((float)num / 60f);
			int num3 = Mathf.Max(0, num - num2 * 60);
			if (num2 > 0 && num3 > 0)
			{
				return num2.ToString() + "m " + num3.ToString() + "s";
			}
			if (num2 > 0)
			{
				return num2.ToString() + "m ";
			}
			return num3.ToString() + "s";
		}

		// Token: 0x0600214B RID: 8523 RVA: 0x000EAE1C File Offset: 0x000E901C
		private string GetStatusEffectTooltip(int quality, float skillLevel)
		{
			if (this.m_shared.m_attackStatusEffect)
			{
				this.m_shared.m_attackStatusEffect.SetLevel(quality, skillLevel);
				string text = (this.m_shared.m_attackStatusEffectChance < 1f) ? string.Format("$item_chancetoapplyse <color=orange>{0}%</color>\n", this.m_shared.m_attackStatusEffectChance * 100f) : "";
				return string.Concat(new string[]
				{
					text,
					"<color=orange>",
					this.m_shared.m_attackStatusEffect.m_name,
					"</color>\n",
					this.m_shared.m_attackStatusEffect.GetTooltipString()
				});
			}
			if (this.m_shared.m_consumeStatusEffect)
			{
				this.m_shared.m_consumeStatusEffect.SetLevel(quality, skillLevel);
				return "<color=orange>" + this.m_shared.m_consumeStatusEffect.m_name + "</color>\n" + this.m_shared.m_consumeStatusEffect.GetTooltipString();
			}
			if (this.m_shared.m_equipStatusEffect)
			{
				this.m_shared.m_equipStatusEffect.SetLevel(quality, skillLevel);
				return "<color=orange>" + this.m_shared.m_equipStatusEffect.m_name + "</color>\n" + this.m_shared.m_equipStatusEffect.GetTooltipString();
			}
			return "";
		}

		// Token: 0x0600214C RID: 8524 RVA: 0x000EAF80 File Offset: 0x000E9180
		private string GetChainTooltip(int quality, float skillLevel)
		{
			if (this.m_shared.m_attack.m_spawnOnHitChance > 0f && this.m_shared.m_attack.m_spawnOnHit != null)
			{
				return ((this.m_shared.m_attack.m_spawnOnHitChance < 1f) ? string.Format("$item_chancetoapplyse <color=orange>{0}%</color>\n", this.m_shared.m_attack.m_spawnOnHitChance * 100f) : "") + "<color=orange>" + this.<GetChainTooltip>g__getName|38_0(true) + "</color>";
			}
			if (this.m_shared.m_secondaryAttack.m_spawnOnHitChance > 0f && this.m_shared.m_secondaryAttack.m_spawnOnHit != null)
			{
				return ((this.m_shared.m_secondaryAttack.m_spawnOnHitChance < 1f) ? string.Format("$item_chancetoapplyse <color=orange>{0}%</color>\n", this.m_shared.m_secondaryAttack.m_spawnOnHitChance * 100f) : "") + "<color=orange>" + this.<GetChainTooltip>g__getName|38_0(false) + "</color>";
			}
			return "";
		}

		// Token: 0x0600214D RID: 8525 RVA: 0x000EB0A4 File Offset: 0x000E92A4
		private string GetEquipStatusEffectTooltip(int quality, float skillLevel)
		{
			if (this.m_shared.m_equipStatusEffect)
			{
				StatusEffect equipStatusEffect = this.m_shared.m_equipStatusEffect;
				this.m_shared.m_equipStatusEffect.SetLevel(quality, skillLevel);
				if (equipStatusEffect != null)
				{
					return equipStatusEffect.GetTooltipString();
				}
			}
			return "";
		}

		// Token: 0x0600214E RID: 8526 RVA: 0x000EB0F8 File Offset: 0x000E92F8
		private string GetSetStatusEffectTooltip(int quality, float skillLevel)
		{
			if (this.m_shared.m_setStatusEffect)
			{
				StatusEffect setStatusEffect = this.m_shared.m_setStatusEffect;
				this.m_shared.m_setStatusEffect.SetLevel(quality, skillLevel);
				if (setStatusEffect != null)
				{
					return setStatusEffect.GetTooltipString();
				}
			}
			return "";
		}

		// Token: 0x0600214F RID: 8527 RVA: 0x000EB14C File Offset: 0x000E934C
		private string GetProjectileTooltip(int itemQuality)
		{
			string text = "";
			if (this.m_shared.m_attack.m_attackProjectile)
			{
				IProjectile component = this.m_shared.m_attack.m_attackProjectile.GetComponent<IProjectile>();
				if (component != null)
				{
					text += component.GetTooltipString(itemQuality);
				}
			}
			if (this.m_shared.m_spawnOnHit)
			{
				IProjectile component2 = this.m_shared.m_spawnOnHit.GetComponent<IProjectile>();
				if (component2 != null)
				{
					text += component2.GetTooltipString(itemQuality);
				}
			}
			return text;
		}

		// Token: 0x06002150 RID: 8528 RVA: 0x000EB1D2 File Offset: 0x000E93D2
		public override string ToString()
		{
			return string.Format("{0}: stack: {1}, quality: {2}, Shared: {3}", new object[]
			{
				"ItemData",
				this.m_stack,
				this.m_quality,
				this.m_shared
			});
		}

		// Token: 0x06002152 RID: 8530 RVA: 0x000EB224 File Offset: 0x000E9424
		[CompilerGenerated]
		internal static bool <GetTooltip>g__printConsumable|35_0(ItemDrop.ItemData item, ref ItemDrop.ItemData.<>c__DisplayClass35_0 A_1)
		{
			if (item.m_shared.m_food > 0f || item.m_shared.m_foodStamina > 0f || item.m_shared.m_foodEitr > 0f)
			{
				float maxHealth = A_1.player.GetMaxHealth();
				float maxStamina = A_1.player.GetMaxStamina();
				float maxEitr = A_1.player.GetMaxEitr();
				if (item.m_shared.m_food > 0f)
				{
					ItemDrop.ItemData.m_stringBuilder.AppendFormat("\n$item_food_health: <color=#ff8080ff>{0}</color>  ($item_current:<color=yellow>{1}</color>)", item.m_shared.m_food, maxHealth.ToString("0"));
				}
				if (item.m_shared.m_foodStamina > 0f)
				{
					ItemDrop.ItemData.m_stringBuilder.AppendFormat("\n$item_food_stamina: <color=#ffff80ff>{0}</color>  ($item_current:<color=yellow>{1}</color>)", item.m_shared.m_foodStamina, maxStamina.ToString("0"));
				}
				if (item.m_shared.m_foodEitr > 0f)
				{
					ItemDrop.ItemData.m_stringBuilder.AppendFormat("\n$item_food_eitr: <color=#9090ffff>{0}</color>  ($item_current:<color=yellow>{1}</color>)", item.m_shared.m_foodEitr, maxEitr.ToString("0"));
				}
				ItemDrop.ItemData.m_stringBuilder.AppendFormat("\n$item_food_duration: <color=orange>{0}</color>", ItemDrop.ItemData.GetDurationString(item.m_shared.m_foodBurnTime));
				if (item.m_shared.m_foodRegen > 0f)
				{
					ItemDrop.ItemData.m_stringBuilder.AppendFormat("\n$item_food_regen: <color=orange>{0} hp/tick</color>", item.m_shared.m_foodRegen);
				}
				return true;
			}
			return false;
		}

		// Token: 0x06002153 RID: 8531 RVA: 0x000EB3A0 File Offset: 0x000E95A0
		[CompilerGenerated]
		private string <GetChainTooltip>g__getName|38_0(bool primary)
		{
			GameObject gameObject = primary ? this.m_shared.m_attack.m_spawnOnHit : this.m_shared.m_secondaryAttack.m_spawnOnHit;
			Aoe component = gameObject.GetComponent<Aoe>();
			if (component != null)
			{
				return component.m_name;
			}
			ItemDrop component2 = gameObject.GetComponent<ItemDrop>();
			if (component2 != null)
			{
				return component2.m_itemData.m_shared.m_name;
			}
			return gameObject.name;
		}

		// Token: 0x040022FD RID: 8957
		private static StringBuilder m_stringBuilder = new StringBuilder(256);

		// Token: 0x040022FE RID: 8958
		public int m_stack = 1;

		// Token: 0x040022FF RID: 8959
		public float m_durability = 100f;

		// Token: 0x04002300 RID: 8960
		public int m_quality = 1;

		// Token: 0x04002301 RID: 8961
		public int m_variant;

		// Token: 0x04002302 RID: 8962
		public int m_worldLevel = Game.m_worldLevel;

		// Token: 0x04002303 RID: 8963
		public bool m_pickedUp;

		// Token: 0x04002304 RID: 8964
		public ItemDrop.ItemData.SharedData m_shared;

		// Token: 0x04002305 RID: 8965
		[NonSerialized]
		public long m_crafterID;

		// Token: 0x04002306 RID: 8966
		[NonSerialized]
		public string m_crafterName = "";

		// Token: 0x04002307 RID: 8967
		[NonSerialized]
		public Dictionary<string, string> m_customData = new Dictionary<string, string>();

		// Token: 0x04002308 RID: 8968
		[NonSerialized]
		public Vector2i m_gridPos = Vector2i.zero;

		// Token: 0x04002309 RID: 8969
		[NonSerialized]
		public bool m_equipped;

		// Token: 0x0400230A RID: 8970
		[NonSerialized]
		public GameObject m_dropPrefab;

		// Token: 0x0400230B RID: 8971
		[NonSerialized]
		public float m_lastAttackTime;

		// Token: 0x0400230C RID: 8972
		[NonSerialized]
		public GameObject m_lastProjectile;

		// Token: 0x020003DB RID: 987
		public enum ItemType
		{
			// Token: 0x04002774 RID: 10100
			None,
			// Token: 0x04002775 RID: 10101
			Material,
			// Token: 0x04002776 RID: 10102
			Consumable,
			// Token: 0x04002777 RID: 10103
			OneHandedWeapon,
			// Token: 0x04002778 RID: 10104
			Bow,
			// Token: 0x04002779 RID: 10105
			Shield,
			// Token: 0x0400277A RID: 10106
			Helmet,
			// Token: 0x0400277B RID: 10107
			Chest,
			// Token: 0x0400277C RID: 10108
			Ammo = 9,
			// Token: 0x0400277D RID: 10109
			Customization,
			// Token: 0x0400277E RID: 10110
			Legs,
			// Token: 0x0400277F RID: 10111
			Hands,
			// Token: 0x04002780 RID: 10112
			Trophy,
			// Token: 0x04002781 RID: 10113
			TwoHandedWeapon,
			// Token: 0x04002782 RID: 10114
			Torch,
			// Token: 0x04002783 RID: 10115
			Misc,
			// Token: 0x04002784 RID: 10116
			Shoulder,
			// Token: 0x04002785 RID: 10117
			Utility,
			// Token: 0x04002786 RID: 10118
			Tool,
			// Token: 0x04002787 RID: 10119
			Attach_Atgeir,
			// Token: 0x04002788 RID: 10120
			Fish,
			// Token: 0x04002789 RID: 10121
			TwoHandedWeaponLeft,
			// Token: 0x0400278A RID: 10122
			AmmoNonEquipable
		}

		// Token: 0x020003DC RID: 988
		public enum AnimationState
		{
			// Token: 0x0400278C RID: 10124
			Unarmed,
			// Token: 0x0400278D RID: 10125
			OneHanded,
			// Token: 0x0400278E RID: 10126
			TwoHandedClub,
			// Token: 0x0400278F RID: 10127
			Bow,
			// Token: 0x04002790 RID: 10128
			Shield,
			// Token: 0x04002791 RID: 10129
			Torch,
			// Token: 0x04002792 RID: 10130
			LeftTorch,
			// Token: 0x04002793 RID: 10131
			Atgeir,
			// Token: 0x04002794 RID: 10132
			TwoHandedAxe,
			// Token: 0x04002795 RID: 10133
			FishingRod,
			// Token: 0x04002796 RID: 10134
			Crossbow,
			// Token: 0x04002797 RID: 10135
			Knives,
			// Token: 0x04002798 RID: 10136
			Staves,
			// Token: 0x04002799 RID: 10137
			Greatsword,
			// Token: 0x0400279A RID: 10138
			MagicItem,
			// Token: 0x0400279B RID: 10139
			DualAxes,
			// Token: 0x0400279C RID: 10140
			Feaster,
			// Token: 0x0400279D RID: 10141
			Scythe
		}

		// Token: 0x020003DD RID: 989
		public enum AiTarget
		{
			// Token: 0x0400279F RID: 10143
			Enemy,
			// Token: 0x040027A0 RID: 10144
			FriendHurt,
			// Token: 0x040027A1 RID: 10145
			Friend
		}

		// Token: 0x020003DE RID: 990
		public enum HelmetHairType
		{
			// Token: 0x040027A3 RID: 10147
			Default,
			// Token: 0x040027A4 RID: 10148
			Hidden,
			// Token: 0x040027A5 RID: 10149
			HiddenHat,
			// Token: 0x040027A6 RID: 10150
			HiddenHood,
			// Token: 0x040027A7 RID: 10151
			HiddenNeck,
			// Token: 0x040027A8 RID: 10152
			HiddenScarf
		}

		// Token: 0x020003DF RID: 991
		public enum AccessoryType
		{
			// Token: 0x040027AA RID: 10154
			Hair,
			// Token: 0x040027AB RID: 10155
			Beard
		}

		// Token: 0x020003E0 RID: 992
		[Serializable]
		public class HelmetHairSettings
		{
			// Token: 0x040027AC RID: 10156
			public ItemDrop.ItemData.HelmetHairType m_setting;

			// Token: 0x040027AD RID: 10157
			public ItemDrop m_hairPrefab;
		}

		// Token: 0x020003E1 RID: 993
		[Serializable]
		public class SharedData
		{
			// Token: 0x060023F4 RID: 9204 RVA: 0x000F2DF4 File Offset: 0x000F0FF4
			public override string ToString()
			{
				return string.Format("{0}: {1}, max stack: {2}, attacks: {3} / {4}", new object[]
				{
					"SharedData",
					this.m_name,
					this.m_maxStackSize,
					this.m_attack,
					this.m_secondaryAttack
				});
			}

			// Token: 0x040027AE RID: 10158
			public string m_name = "";

			// Token: 0x040027AF RID: 10159
			public string m_dlc = "";

			// Token: 0x040027B0 RID: 10160
			public ItemDrop.ItemData.ItemType m_itemType = ItemDrop.ItemData.ItemType.Misc;

			// Token: 0x040027B1 RID: 10161
			public Sprite[] m_icons = Array.Empty<Sprite>();

			// Token: 0x040027B2 RID: 10162
			public ItemDrop.ItemData.ItemType m_attachOverride;

			// Token: 0x040027B3 RID: 10163
			[TextArea]
			public string m_description = "";

			// Token: 0x040027B4 RID: 10164
			public int m_maxStackSize = 1;

			// Token: 0x040027B5 RID: 10165
			public bool m_autoStack = true;

			// Token: 0x040027B6 RID: 10166
			public int m_maxQuality = 1;

			// Token: 0x040027B7 RID: 10167
			public float m_scaleByQuality;

			// Token: 0x040027B8 RID: 10168
			public float m_weight = 1f;

			// Token: 0x040027B9 RID: 10169
			public float m_scaleWeightByQuality;

			// Token: 0x040027BA RID: 10170
			public int m_value;

			// Token: 0x040027BB RID: 10171
			public bool m_teleportable = true;

			// Token: 0x040027BC RID: 10172
			public bool m_questItem;

			// Token: 0x040027BD RID: 10173
			public float m_equipDuration = 1f;

			// Token: 0x040027BE RID: 10174
			public int m_variants;

			// Token: 0x040027BF RID: 10175
			public Vector2Int m_trophyPos = Vector2Int.zero;

			// Token: 0x040027C0 RID: 10176
			public PieceTable m_buildPieces;

			// Token: 0x040027C1 RID: 10177
			public bool m_centerCamera;

			// Token: 0x040027C2 RID: 10178
			public string m_setName = "";

			// Token: 0x040027C3 RID: 10179
			public int m_setSize;

			// Token: 0x040027C4 RID: 10180
			public StatusEffect m_setStatusEffect;

			// Token: 0x040027C5 RID: 10181
			public StatusEffect m_equipStatusEffect;

			// Token: 0x040027C6 RID: 10182
			[Header("Stat modifiers")]
			public float m_eitrRegenModifier;

			// Token: 0x040027C7 RID: 10183
			public float m_movementModifier;

			// Token: 0x040027C8 RID: 10184
			public float m_homeItemsStaminaModifier;

			// Token: 0x040027C9 RID: 10185
			public float m_heatResistanceModifier;

			// Token: 0x040027CA RID: 10186
			public float m_jumpStaminaModifier;

			// Token: 0x040027CB RID: 10187
			public float m_attackStaminaModifier;

			// Token: 0x040027CC RID: 10188
			public float m_blockStaminaModifier;

			// Token: 0x040027CD RID: 10189
			public float m_dodgeStaminaModifier;

			// Token: 0x040027CE RID: 10190
			public float m_swimStaminaModifier;

			// Token: 0x040027CF RID: 10191
			public float m_sneakStaminaModifier;

			// Token: 0x040027D0 RID: 10192
			public float m_runStaminaModifier;

			// Token: 0x040027D1 RID: 10193
			[Header("Food settings")]
			public float m_food;

			// Token: 0x040027D2 RID: 10194
			public float m_foodStamina;

			// Token: 0x040027D3 RID: 10195
			public float m_foodEitr;

			// Token: 0x040027D4 RID: 10196
			public float m_foodBurnTime;

			// Token: 0x040027D5 RID: 10197
			public float m_foodRegen;

			// Token: 0x040027D6 RID: 10198
			public float m_foodEatAnimTime = 1f;

			// Token: 0x040027D7 RID: 10199
			public bool m_isDrink;

			// Token: 0x040027D8 RID: 10200
			[Header("Armor settings")]
			public Material m_armorMaterial;

			// Token: 0x040027D9 RID: 10201
			public ItemDrop.ItemData.HelmetHairType m_helmetHideHair = ItemDrop.ItemData.HelmetHairType.Hidden;

			// Token: 0x040027DA RID: 10202
			public ItemDrop.ItemData.HelmetHairType m_helmetHideBeard;

			// Token: 0x040027DB RID: 10203
			public List<ItemDrop.ItemData.HelmetHairSettings> m_helmetHairSettings = new List<ItemDrop.ItemData.HelmetHairSettings>();

			// Token: 0x040027DC RID: 10204
			public List<ItemDrop.ItemData.HelmetHairSettings> m_helmetBeardSettings = new List<ItemDrop.ItemData.HelmetHairSettings>();

			// Token: 0x040027DD RID: 10205
			public float m_armor = 10f;

			// Token: 0x040027DE RID: 10206
			public float m_armorPerLevel = 1f;

			// Token: 0x040027DF RID: 10207
			public List<HitData.DamageModPair> m_damageModifiers = new List<HitData.DamageModPair>();

			// Token: 0x040027E0 RID: 10208
			[Header("Shield settings")]
			public float m_blockPower = 10f;

			// Token: 0x040027E1 RID: 10209
			public float m_blockPowerPerLevel;

			// Token: 0x040027E2 RID: 10210
			public float m_deflectionForce;

			// Token: 0x040027E3 RID: 10211
			public float m_deflectionForcePerLevel;

			// Token: 0x040027E4 RID: 10212
			public float m_timedBlockBonus = 1.5f;

			// Token: 0x040027E5 RID: 10213
			[Header("Weapon")]
			public ItemDrop.ItemData.AnimationState m_animationState = ItemDrop.ItemData.AnimationState.OneHanded;

			// Token: 0x040027E6 RID: 10214
			public Skills.SkillType m_skillType = Skills.SkillType.Swords;

			// Token: 0x040027E7 RID: 10215
			public int m_toolTier;

			// Token: 0x040027E8 RID: 10216
			public HitData.DamageTypes m_damages;

			// Token: 0x040027E9 RID: 10217
			public HitData.DamageTypes m_damagesPerLevel;

			// Token: 0x040027EA RID: 10218
			public float m_attackForce = 30f;

			// Token: 0x040027EB RID: 10219
			public float m_backstabBonus = 4f;

			// Token: 0x040027EC RID: 10220
			public bool m_dodgeable;

			// Token: 0x040027ED RID: 10221
			public bool m_blockable;

			// Token: 0x040027EE RID: 10222
			public bool m_tamedOnly;

			// Token: 0x040027EF RID: 10223
			public bool m_alwaysRotate;

			// Token: 0x040027F0 RID: 10224
			public StatusEffect m_attackStatusEffect;

			// Token: 0x040027F1 RID: 10225
			public float m_attackStatusEffectChance = 1f;

			// Token: 0x040027F2 RID: 10226
			public GameObject m_spawnOnHit;

			// Token: 0x040027F3 RID: 10227
			public GameObject m_spawnOnHitTerrain;

			// Token: 0x040027F4 RID: 10228
			public bool m_projectileToolTip = true;

			// Token: 0x040027F5 RID: 10229
			[Header("Ammo")]
			public string m_ammoType = "";

			// Token: 0x040027F6 RID: 10230
			[Header("Attacks")]
			public Attack m_attack;

			// Token: 0x040027F7 RID: 10231
			public Attack m_secondaryAttack;

			// Token: 0x040027F8 RID: 10232
			[Header("ItemStand")]
			public Vector3 m_ISHorizontalPos;

			// Token: 0x040027F9 RID: 10233
			public Vector3 m_ISHorizontalRot;

			// Token: 0x040027FA RID: 10234
			public Vector3 m_iSHorizontalScale = Vector3.one;

			// Token: 0x040027FB RID: 10235
			[Header("Durability")]
			public bool m_useDurability;

			// Token: 0x040027FC RID: 10236
			public bool m_destroyBroken = true;

			// Token: 0x040027FD RID: 10237
			public bool m_canBeReparied = true;

			// Token: 0x040027FE RID: 10238
			public float m_maxDurability = 100f;

			// Token: 0x040027FF RID: 10239
			public float m_durabilityPerLevel = 50f;

			// Token: 0x04002800 RID: 10240
			public float m_useDurabilityDrain = 1f;

			// Token: 0x04002801 RID: 10241
			public float m_durabilityDrain;

			// Token: 0x04002802 RID: 10242
			public Skills.SkillType m_placementDurabilitySkill;

			// Token: 0x04002803 RID: 10243
			public float m_placementDurabilityMax = 0.5f;

			// Token: 0x04002804 RID: 10244
			[Header("AI")]
			public float m_aiAttackRange = 2f;

			// Token: 0x04002805 RID: 10245
			public float m_aiAttackRangeMin;

			// Token: 0x04002806 RID: 10246
			public float m_aiAttackInterval = 2f;

			// Token: 0x04002807 RID: 10247
			public float m_aiAttackMaxAngle = 5f;

			// Token: 0x04002808 RID: 10248
			public bool m_aiInvertAngleCheck;

			// Token: 0x04002809 RID: 10249
			public bool m_aiWhenFlying = true;

			// Token: 0x0400280A RID: 10250
			public float m_aiWhenFlyingAltitudeMin;

			// Token: 0x0400280B RID: 10251
			public float m_aiWhenFlyingAltitudeMax = 999999f;

			// Token: 0x0400280C RID: 10252
			public bool m_aiWhenWalking = true;

			// Token: 0x0400280D RID: 10253
			public bool m_aiWhenSwiming = true;

			// Token: 0x0400280E RID: 10254
			public bool m_aiPrioritized;

			// Token: 0x0400280F RID: 10255
			public bool m_aiInDungeonOnly;

			// Token: 0x04002810 RID: 10256
			public bool m_aiInMistOnly;

			// Token: 0x04002811 RID: 10257
			[Range(0f, 1f)]
			public float m_aiMaxHealthPercentage = 1f;

			// Token: 0x04002812 RID: 10258
			[Range(0f, 1f)]
			public float m_aiMinHealthPercentage;

			// Token: 0x04002813 RID: 10259
			public ItemDrop.ItemData.AiTarget m_aiTargetType;

			// Token: 0x04002814 RID: 10260
			[Header("Effects")]
			public EffectList m_hitEffect = new EffectList();

			// Token: 0x04002815 RID: 10261
			public EffectList m_hitTerrainEffect = new EffectList();

			// Token: 0x04002816 RID: 10262
			public EffectList m_blockEffect = new EffectList();

			// Token: 0x04002817 RID: 10263
			public EffectList m_startEffect = new EffectList();

			// Token: 0x04002818 RID: 10264
			public EffectList m_holdStartEffect = new EffectList();

			// Token: 0x04002819 RID: 10265
			public EffectList m_equipEffect = new EffectList();

			// Token: 0x0400281A RID: 10266
			public EffectList m_unequipEffect = new EffectList();

			// Token: 0x0400281B RID: 10267
			public EffectList m_triggerEffect = new EffectList();

			// Token: 0x0400281C RID: 10268
			public EffectList m_trailStartEffect = new EffectList();

			// Token: 0x0400281D RID: 10269
			public EffectList m_buildEffect = new EffectList();

			// Token: 0x0400281E RID: 10270
			public EffectList m_destroyEffect = new EffectList();

			// Token: 0x0400281F RID: 10271
			[Header("Consumable")]
			public StatusEffect m_consumeStatusEffect;

			// Token: 0x04002820 RID: 10272
			public ItemDrop m_appendToolTip;
		}
	}
}
