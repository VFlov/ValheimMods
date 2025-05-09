using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEngine.Rendering;

// Token: 0x02000166 RID: 358
public class CookingStation : MonoBehaviour, Interactable, Hoverable, IHasHoverMenuExtended
{
	// Token: 0x060015BA RID: 5562 RVA: 0x000A01DC File Offset: 0x0009E3DC
	private void Awake()
	{
		this.m_nview = base.gameObject.GetComponent<ZNetView>();
		if (this.m_nview.GetZDO() == null)
		{
			return;
		}
		this.m_ps = new ParticleSystem[this.m_slots.Length];
		this.m_as = new AudioSource[this.m_slots.Length];
		for (int i = 0; i < this.m_slots.Length; i++)
		{
			this.m_ps[i] = this.m_slots[i].GetComponent<ParticleSystem>();
			this.m_as[i] = this.m_slots[i].GetComponent<AudioSource>();
		}
		this.m_nview.Register<Vector3, int>("RPC_RemoveDoneItem", new Action<long, Vector3, int>(this.RPC_RemoveDoneItem));
		this.m_nview.Register<string>("RPC_AddItem", new Action<long, string>(this.RPC_AddItem));
		this.m_nview.Register("RPC_AddFuel", new Action<long>(this.RPC_AddFuel));
		this.m_nview.Register<int, string>("RPC_SetSlotVisual", new Action<long, int, string>(this.RPC_SetSlotVisual));
		if (this.m_addFoodSwitch)
		{
			this.m_addFoodSwitch.m_onUse = new Switch.Callback(this.OnAddFoodSwitch);
			this.m_addFoodSwitch.m_hoverText = this.HoverText();
		}
		if (this.m_addFuelSwitch)
		{
			this.m_addFuelSwitch.m_onUse = new Switch.Callback(this.OnAddFuelSwitch);
			this.m_addFuelSwitch.m_onHover = new Switch.TooltipCallback(this.OnHoverFuelSwitch);
		}
		WearNTear component = base.GetComponent<WearNTear>();
		if (component)
		{
			WearNTear wearNTear = component;
			wearNTear.m_onDestroyed = (Action)Delegate.Combine(wearNTear.m_onDestroyed, new Action(this.OnDestroyed));
		}
		this.m_cheapFireCheck = (this.m_fireCheckRadius == 0.25f);
		base.InvokeRepeating("UpdateCooking", 0f, 1f);
	}

	// Token: 0x060015BB RID: 5563 RVA: 0x000A03A4 File Offset: 0x0009E5A4
	private void DropAllItems()
	{
		if (this.m_fuelItem != null)
		{
			float fuel = this.GetFuel();
			for (int i = 0; i < (int)fuel; i++)
			{
				this.<DropAllItems>g__drop|1_0(this.m_fuelItem);
			}
			this.SetFuel(0f);
		}
		for (int j = 0; j < this.m_slots.Length; j++)
		{
			string text;
			float num;
			CookingStation.Status status;
			this.GetSlot(j, out text, out num, out status);
			if (text != "")
			{
				if (status == CookingStation.Status.Done)
				{
					this.<DropAllItems>g__drop|1_0(this.GetItemConversion(text).m_to);
				}
				else if (status == CookingStation.Status.Burnt)
				{
					this.<DropAllItems>g__drop|1_0(this.m_overCookedItem);
				}
				else if (status == CookingStation.Status.NotDone)
				{
					GameObject prefab = ZNetScene.instance.GetPrefab(text);
					if (prefab != null)
					{
						ItemDrop component = prefab.GetComponent<ItemDrop>();
						if (component)
						{
							this.<DropAllItems>g__drop|1_0(component);
						}
					}
				}
				this.SetSlot(j, "", 0f, CookingStation.Status.NotDone);
			}
		}
	}

	// Token: 0x060015BC RID: 5564 RVA: 0x000A0490 File Offset: 0x0009E690
	private void OnDestroyed()
	{
		if (this.m_nview.IsOwner())
		{
			this.DropAllItems();
		}
	}

	// Token: 0x060015BD RID: 5565 RVA: 0x000A04A8 File Offset: 0x0009E6A8
	private void UpdateCooking()
	{
		if (!this.m_nview.IsValid())
		{
			return;
		}
		bool flag = (this.m_requireFire && this.IsFireLit()) || (this.m_useFuel && this.GetFuel() > 0f);
		if (this.m_nview.IsOwner())
		{
			float deltaTime = this.GetDeltaTime();
			if (flag)
			{
				this.UpdateFuel(deltaTime);
				for (int i = 0; i < this.m_slots.Length; i++)
				{
					string text;
					float num;
					CookingStation.Status status;
					this.GetSlot(i, out text, out num, out status);
					if (text != "" && status != CookingStation.Status.Burnt)
					{
						CookingStation.ItemConversion itemConversion = this.GetItemConversion(text);
						if (itemConversion == null)
						{
							this.SetSlot(i, "", 0f, CookingStation.Status.NotDone);
						}
						else
						{
							num += deltaTime;
							if (num > itemConversion.m_cookTime * 2f)
							{
								this.m_overcookedEffect.Create(this.m_slots[i].position, Quaternion.identity, null, 1f, -1);
								this.SetSlot(i, this.m_overCookedItem.name, num, CookingStation.Status.Burnt);
							}
							else if (num > itemConversion.m_cookTime && text == itemConversion.m_from.name)
							{
								this.m_doneEffect.Create(this.m_slots[i].position, Quaternion.identity, null, 1f, -1);
								this.SetSlot(i, itemConversion.m_to.name, num, CookingStation.Status.Done);
							}
							else
							{
								this.SetSlot(i, text, num, status);
							}
						}
					}
				}
			}
		}
		this.UpdateVisual(flag);
	}

	// Token: 0x060015BE RID: 5566 RVA: 0x000A0638 File Offset: 0x0009E838
	private float GetDeltaTime()
	{
		DateTime time = ZNet.instance.GetTime();
		DateTime d = new DateTime(this.m_nview.GetZDO().GetLong(ZDOVars.s_startTime, time.Ticks));
		float totalSeconds = (float)(time - d).TotalSeconds;
		this.m_nview.GetZDO().Set(ZDOVars.s_startTime, time.Ticks);
		return totalSeconds;
	}

	// Token: 0x060015BF RID: 5567 RVA: 0x000A06A0 File Offset: 0x0009E8A0
	private void UpdateFuel(float dt)
	{
		if (!this.m_useFuel)
		{
			return;
		}
		float num = dt / (float)this.m_secPerFuel;
		float num2 = this.GetFuel();
		num2 -= num;
		if (num2 < 0f)
		{
			num2 = 0f;
		}
		this.SetFuel(num2);
	}

	// Token: 0x060015C0 RID: 5568 RVA: 0x000A06E0 File Offset: 0x0009E8E0
	private void UpdateVisual(bool fireLit)
	{
		for (int i = 0; i < this.m_slots.Length; i++)
		{
			string item;
			float num;
			CookingStation.Status status;
			this.GetSlot(i, out item, out num, out status);
			this.SetSlotVisual(i, item, fireLit, status);
		}
		if (this.m_useFuel)
		{
			bool active = this.GetFuel() > 0f;
			if (this.m_haveFireObject)
			{
				this.m_haveFireObject.SetActive(fireLit);
			}
			if (this.m_haveFuelObject)
			{
				this.m_haveFuelObject.SetActive(active);
			}
		}
	}

	// Token: 0x060015C1 RID: 5569 RVA: 0x000A0761 File Offset: 0x0009E961
	private void RPC_SetSlotVisual(long sender, int slot, string item)
	{
		this.SetSlotVisual(slot, item, false, CookingStation.Status.NotDone);
	}

	// Token: 0x060015C2 RID: 5570 RVA: 0x000A0770 File Offset: 0x0009E970
	private void SetSlotVisual(int i, string item, bool fireLit, CookingStation.Status status)
	{
		if (item == "")
		{
			this.m_ps[i].emission.enabled = false;
			if (this.m_burntPS.Length != 0)
			{
				this.m_burntPS[i].emission.enabled = false;
			}
			if (this.m_donePS.Length != 0)
			{
				this.m_donePS[i].emission.enabled = false;
			}
			this.m_as[i].mute = true;
			if (this.m_slots[i].childCount > 0)
			{
				UnityEngine.Object.Destroy(this.m_slots[i].GetChild(0).gameObject);
				return;
			}
		}
		else
		{
			this.m_ps[i].emission.enabled = (fireLit && status != CookingStation.Status.Burnt);
			if (this.m_burntPS.Length != 0)
			{
				this.m_burntPS[i].emission.enabled = (fireLit && status == CookingStation.Status.Burnt);
			}
			if (this.m_donePS.Length != 0)
			{
				this.m_donePS[i].emission.enabled = (fireLit && status == CookingStation.Status.Done);
			}
			this.m_as[i].mute = !fireLit;
			if (this.m_slots[i].childCount == 0 || this.m_slots[i].GetChild(0).name != item)
			{
				if (this.m_slots[i].childCount > 0)
				{
					UnityEngine.Object.Destroy(this.m_slots[i].GetChild(0).gameObject);
				}
				Component component = ObjectDB.instance.GetItemPrefab(item).transform.Find("attach");
				Transform transform = this.m_slots[i];
				GameObject gameObject = UnityEngine.Object.Instantiate<GameObject>(component.gameObject, transform.position, transform.rotation, transform);
				gameObject.name = item;
				Renderer[] componentsInChildren = gameObject.GetComponentsInChildren<Renderer>();
				for (int j = 0; j < componentsInChildren.Length; j++)
				{
					componentsInChildren[j].shadowCastingMode = ShadowCastingMode.Off;
				}
			}
		}
	}

	// Token: 0x060015C3 RID: 5571 RVA: 0x000A0964 File Offset: 0x0009EB64
	private void RPC_RemoveDoneItem(long sender, Vector3 userPoint, int amount)
	{
		for (int i = 0; i < this.m_slots.Length; i++)
		{
			string text;
			float num;
			CookingStation.Status status;
			this.GetSlot(i, out text, out num, out status);
			if (text != "" && this.IsItemDone(text))
			{
				for (int j = 0; j < amount; j++)
				{
					this.SpawnItem(text, i, userPoint);
				}
				this.SetSlot(i, "", 0f, CookingStation.Status.NotDone);
				this.m_nview.InvokeRPC(ZNetView.Everybody, "RPC_SetSlotVisual", new object[]
				{
					i,
					""
				});
				return;
			}
		}
	}

	// Token: 0x060015C4 RID: 5572 RVA: 0x000A0A04 File Offset: 0x0009EC04
	private bool HaveDoneItem()
	{
		for (int i = 0; i < this.m_slots.Length; i++)
		{
			string text;
			float num;
			CookingStation.Status status;
			this.GetSlot(i, out text, out num, out status);
			if (text != "" && this.IsItemDone(text))
			{
				return true;
			}
		}
		return false;
	}

	// Token: 0x060015C5 RID: 5573 RVA: 0x000A0A4C File Offset: 0x0009EC4C
	private bool IsItemDone(string itemName)
	{
		if (itemName == this.m_overCookedItem.name)
		{
			return true;
		}
		CookingStation.ItemConversion itemConversion = this.GetItemConversion(itemName);
		return itemConversion != null && itemName == itemConversion.m_to.name;
	}

	// Token: 0x060015C6 RID: 5574 RVA: 0x000A0A94 File Offset: 0x0009EC94
	private void SpawnItem(string name, int slot, Vector3 userPoint)
	{
		GameObject itemPrefab = ObjectDB.instance.GetItemPrefab(name);
		Vector3 vector;
		Vector3 a;
		if (this.m_spawnPoint != null)
		{
			vector = this.m_spawnPoint.position;
			a = this.m_spawnPoint.forward;
		}
		else
		{
			Vector3 position = this.m_slots[slot].position;
			Vector3 vector2 = userPoint - position;
			vector2.y = 0f;
			vector2.Normalize();
			vector = position + vector2 * 0.5f;
			a = vector2;
		}
		Quaternion rotation = Quaternion.Euler(0f, (float)UnityEngine.Random.Range(0, 360), 0f);
		GameObject gameObject = UnityEngine.Object.Instantiate<GameObject>(itemPrefab, vector, rotation);
		ItemDrop.OnCreateNew(gameObject);
		gameObject.GetComponent<Rigidbody>().velocity = a * this.m_spawnForce;
		this.m_pickEffector.Create(vector, Quaternion.identity, null, 1f, -1);
	}

	// Token: 0x060015C7 RID: 5575 RVA: 0x000A0B6C File Offset: 0x0009ED6C
	public string GetHoverText()
	{
		if (this.m_addFoodSwitch != null)
		{
			return "";
		}
		return Localization.instance.Localize(this.HoverText());
	}

	// Token: 0x060015C8 RID: 5576 RVA: 0x000A0B92 File Offset: 0x0009ED92
	private string HoverText()
	{
		return this.m_name + "\n[<color=yellow><b>$KEY_Use</b></color>] " + this.m_addItemTooltip + (ZInput.GamepadActive ? "" : ("\n[<color=yellow><b>1-8</b></color>] " + this.m_addItemTooltip));
	}

	// Token: 0x060015C9 RID: 5577 RVA: 0x000A0BC8 File Offset: 0x0009EDC8
	public string GetHoverName()
	{
		return this.m_name;
	}

	// Token: 0x060015CA RID: 5578 RVA: 0x000A0BD0 File Offset: 0x0009EDD0
	private bool OnAddFuelSwitch(Switch sw, Humanoid user, ItemDrop.ItemData item)
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

	// Token: 0x060015CB RID: 5579 RVA: 0x000A0CE8 File Offset: 0x0009EEE8
	private void RPC_AddFuel(long sender)
	{
		if (!this.m_nview.IsOwner())
		{
			return;
		}
		ZLog.Log("Add fuel");
		float fuel = this.GetFuel();
		this.SetFuel(fuel + 1f);
		this.m_fuelAddedEffects.Create(base.transform.position, base.transform.rotation, base.transform, 1f, -1);
	}

	// Token: 0x060015CC RID: 5580 RVA: 0x000A0D50 File Offset: 0x0009EF50
	private string OnHoverFuelSwitch()
	{
		float fuel = this.GetFuel();
		Localization instance = Localization.instance;
		string[] array = new string[9];
		array[0] = this.m_name;
		array[1] = " (";
		array[2] = this.m_fuelItem.m_itemData.m_shared.m_name;
		array[3] = " ";
		array[4] = Mathf.Ceil(fuel).ToString();
		array[5] = "/";
		int num = 6;
		int maxFuel = this.m_maxFuel;
		array[num] = maxFuel.ToString();
		array[7] = ")\n[<color=yellow><b>$KEY_Use</b></color>] $piece_smelter_add ";
		array[8] = this.m_fuelItem.m_itemData.m_shared.m_name;
		return instance.Localize(string.Concat(array));
	}

	// Token: 0x060015CD RID: 5581 RVA: 0x000A0DF5 File Offset: 0x0009EFF5
	private bool OnAddFoodSwitch(Switch caller, Humanoid user, ItemDrop.ItemData item)
	{
		ZLog.Log("add food switch");
		if (item != null)
		{
			return this.OnUseItem(user, item);
		}
		return this.OnInteract(user);
	}

	// Token: 0x060015CE RID: 5582 RVA: 0x000A0E14 File Offset: 0x0009F014
	public bool Interact(Humanoid user, bool hold, bool alt)
	{
		return !hold && !(this.m_addFoodSwitch != null) && this.OnInteract(user);
	}

	// Token: 0x060015CF RID: 5583 RVA: 0x000A0E34 File Offset: 0x0009F034
	private bool OnInteract(Humanoid user)
	{
		if (this.HaveDoneItem())
		{
			Player.m_localPlayer.RaiseSkill(Skills.SkillType.Cooking, 0.6f);
			int num = 1;
			float skillFactor = Player.m_localPlayer.GetSkillFactor(Skills.SkillType.Cooking);
			if (UnityEngine.Random.value < skillFactor * InventoryGui.instance.m_craftBonusChance)
			{
				num += InventoryGui.instance.m_craftBonusAmount;
				DamageText.instance.ShowText(DamageText.TextType.Bonus, base.transform.position + Vector3.up, "+" + InventoryGui.instance.m_craftBonusAmount.ToString(), true);
				InventoryGui.instance.m_craftBonusEffect.Create(base.transform.position, Quaternion.identity, null, 1f, -1);
				ZLog.Log("Bonus food cooking station!");
			}
			this.m_nview.InvokeRPC("RPC_RemoveDoneItem", new object[]
			{
				user.transform.position,
				num
			});
			return true;
		}
		ItemDrop.ItemData itemData = this.FindCookableItem(user.GetInventory());
		if (itemData == null)
		{
			CookingStation.ItemMessage itemMessage = this.FindIncompatibleItem(user.GetInventory());
			if (itemMessage != null)
			{
				user.Message(MessageHud.MessageType.Center, itemMessage.m_message + " " + itemMessage.m_item.m_itemData.m_shared.m_name, 0, null);
			}
			else
			{
				user.Message(MessageHud.MessageType.Center, "$msg_nocookitems", 0, null);
			}
			return false;
		}
		if (this.OnUseItem(user, itemData))
		{
			Player.m_localPlayer.RaiseSkill(Skills.SkillType.Cooking, 0.4f);
			return true;
		}
		return false;
	}

	// Token: 0x060015D0 RID: 5584 RVA: 0x000A0FA9 File Offset: 0x0009F1A9
	public bool UseItem(Humanoid user, ItemDrop.ItemData item)
	{
		return !(this.m_addFoodSwitch != null) && this.OnUseItem(user, item);
	}

	// Token: 0x060015D1 RID: 5585 RVA: 0x000A0FC4 File Offset: 0x0009F1C4
	private bool OnUseItem(Humanoid user, ItemDrop.ItemData item)
	{
		if (this.m_requireFire && !this.IsFireLit())
		{
			user.Message(MessageHud.MessageType.Center, "$msg_needfire", 0, null);
			user.UseIemBlockkMessage();
			return false;
		}
		if (this.GetFreeSlot() == -1)
		{
			user.Message(MessageHud.MessageType.Center, "$msg_nocookroom", 0, null);
			user.UseIemBlockkMessage();
			return false;
		}
		return this.CookItem(user, item);
	}

	// Token: 0x060015D2 RID: 5586 RVA: 0x000A1020 File Offset: 0x0009F220
	private bool IsFireLit()
	{
		if (this.m_fireCheckPoints != null && this.m_fireCheckPoints.Length != 0)
		{
			foreach (Transform transform in this.m_fireCheckPoints)
			{
				if (this.m_cheapFireCheck)
				{
					if (!EffectArea.IsPointPlus025InsideBurningArea(transform.position))
					{
						return false;
					}
				}
				else if (!EffectArea.IsPointInsideArea(transform.position, EffectArea.Type.Burning, this.m_fireCheckRadius))
				{
					return false;
				}
			}
			return true;
		}
		if (this.m_cheapFireCheck)
		{
			return EffectArea.IsPointPlus025InsideBurningArea(base.transform.position);
		}
		return EffectArea.IsPointInsideArea(base.transform.position, EffectArea.Type.Burning, this.m_fireCheckRadius);
	}

	// Token: 0x060015D3 RID: 5587 RVA: 0x000A10C0 File Offset: 0x0009F2C0
	private ItemDrop.ItemData FindCookableItem(Inventory inventory)
	{
		foreach (CookingStation.ItemConversion itemConversion in this.m_conversion)
		{
			ItemDrop.ItemData item = inventory.GetItem(itemConversion.m_from.m_itemData.m_shared.m_name, -1, false);
			if (item != null)
			{
				return item;
			}
		}
		return null;
	}

	// Token: 0x060015D4 RID: 5588 RVA: 0x000A1134 File Offset: 0x0009F334
	private CookingStation.ItemMessage FindIncompatibleItem(Inventory inventory)
	{
		foreach (CookingStation.ItemMessage itemMessage in this.m_incompatibleItems)
		{
			if (inventory.GetItem(itemMessage.m_item.m_itemData.m_shared.m_name, -1, false) != null)
			{
				return itemMessage;
			}
		}
		return null;
	}

	// Token: 0x060015D5 RID: 5589 RVA: 0x000A11A8 File Offset: 0x0009F3A8
	private bool CookItem(Humanoid user, ItemDrop.ItemData item)
	{
		string name = item.m_dropPrefab.name;
		if (!this.m_nview.HasOwner())
		{
			this.m_nview.ClaimOwnership();
		}
		foreach (CookingStation.ItemMessage itemMessage in this.m_incompatibleItems)
		{
			if (itemMessage.m_item.m_itemData.m_shared.m_name == item.m_shared.m_name)
			{
				user.Message(MessageHud.MessageType.Center, itemMessage.m_message + " " + itemMessage.m_item.m_itemData.m_shared.m_name, 0, null);
				return true;
			}
		}
		if (!this.IsItemAllowed(item))
		{
			return false;
		}
		if (this.GetFreeSlot() == -1)
		{
			return false;
		}
		user.GetInventory().RemoveOneItem(item);
		this.m_nview.InvokeRPC("RPC_AddItem", new object[]
		{
			name
		});
		return true;
	}

	// Token: 0x060015D6 RID: 5590 RVA: 0x000A12B4 File Offset: 0x0009F4B4
	private void RPC_AddItem(long sender, string itemName)
	{
		if (!this.IsItemAllowed(itemName))
		{
			return;
		}
		int freeSlot = this.GetFreeSlot();
		if (freeSlot == -1)
		{
			return;
		}
		this.SetSlot(freeSlot, itemName, 0f, CookingStation.Status.NotDone);
		this.m_nview.InvokeRPC(ZNetView.Everybody, "RPC_SetSlotVisual", new object[]
		{
			freeSlot,
			itemName
		});
		this.m_addEffect.Create(this.m_slots[freeSlot].position, Quaternion.identity, null, 1f, -1);
	}

	// Token: 0x060015D7 RID: 5591 RVA: 0x000A1334 File Offset: 0x0009F534
	private void SetSlot(int slot, string itemName, float cookedTime, CookingStation.Status status)
	{
		if (!this.m_nview.IsValid())
		{
			return;
		}
		this.m_nview.GetZDO().Set("slot" + slot.ToString(), itemName);
		this.m_nview.GetZDO().Set("slot" + slot.ToString(), cookedTime);
		this.m_nview.GetZDO().Set("slotstatus" + slot.ToString(), (int)status);
	}

	// Token: 0x060015D8 RID: 5592 RVA: 0x000A13B8 File Offset: 0x0009F5B8
	private void GetSlot(int slot, out string itemName, out float cookedTime, out CookingStation.Status status)
	{
		if (!this.m_nview.IsValid())
		{
			itemName = "";
			status = CookingStation.Status.NotDone;
			cookedTime = 0f;
			return;
		}
		itemName = this.m_nview.GetZDO().GetString("slot" + slot.ToString(), "");
		cookedTime = this.m_nview.GetZDO().GetFloat("slot" + slot.ToString(), 0f);
		status = (CookingStation.Status)this.m_nview.GetZDO().GetInt("slotstatus" + slot.ToString(), 0);
	}

	// Token: 0x060015D9 RID: 5593 RVA: 0x000A145C File Offset: 0x0009F65C
	private bool IsEmpty()
	{
		for (int i = 0; i < this.m_slots.Length; i++)
		{
			if (this.m_nview.GetZDO().GetString("slot" + i.ToString(), "") != "")
			{
				return false;
			}
		}
		return true;
	}

	// Token: 0x060015DA RID: 5594 RVA: 0x000A14B4 File Offset: 0x0009F6B4
	private int GetFreeSlot()
	{
		for (int i = 0; i < this.m_slots.Length; i++)
		{
			if (this.m_nview.GetZDO().GetString("slot" + i.ToString(), "") == "")
			{
				return i;
			}
		}
		return -1;
	}

	// Token: 0x060015DB RID: 5595 RVA: 0x000A1509 File Offset: 0x0009F709
	private bool IsItemAllowed(ItemDrop.ItemData item)
	{
		return this.IsItemAllowed(item.m_dropPrefab.name);
	}

	// Token: 0x060015DC RID: 5596 RVA: 0x000A151C File Offset: 0x0009F71C
	private bool IsItemAllowed(string itemName)
	{
		using (List<CookingStation.ItemConversion>.Enumerator enumerator = this.m_conversion.GetEnumerator())
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

	// Token: 0x060015DD RID: 5597 RVA: 0x000A1588 File Offset: 0x0009F788
	private CookingStation.ItemConversion GetItemConversion(string itemName)
	{
		foreach (CookingStation.ItemConversion itemConversion in this.m_conversion)
		{
			if (itemConversion.m_from.gameObject.name == itemName || itemConversion.m_to.gameObject.name == itemName)
			{
				return itemConversion;
			}
		}
		return null;
	}

	// Token: 0x060015DE RID: 5598 RVA: 0x000A160C File Offset: 0x0009F80C
	private void SetFuel(float fuel)
	{
		this.m_nview.GetZDO().Set(ZDOVars.s_fuel, fuel);
	}

	// Token: 0x060015DF RID: 5599 RVA: 0x000A1624 File Offset: 0x0009F824
	private float GetFuel()
	{
		return this.m_nview.GetZDO().GetFloat(ZDOVars.s_fuel, 0f);
	}

	// Token: 0x060015E0 RID: 5600 RVA: 0x000A1640 File Offset: 0x0009F840
	public bool TryGetItems(Player player, Switch switchRef, out List<string> items)
	{
		items = new List<string>();
		if (!switchRef && this.m_addFoodSwitch)
		{
			return false;
		}
		if (!this.CanUseItems(player, switchRef, true))
		{
			return true;
		}
		if (switchRef == null || switchRef == this.m_addFoodSwitch)
		{
			items.AddRange(from conversion in this.m_conversion
			select conversion.m_from.m_itemData.m_shared.m_name);
		}
		else if (switchRef == this.m_addFuelSwitch)
		{
			items.Add(this.m_fuelItem.m_itemData.m_shared.m_name);
		}
		return true;
	}

	// Token: 0x060015E1 RID: 5601 RVA: 0x000A16F0 File Offset: 0x0009F8F0
	public bool CanUseItems(Player player, Switch switchRef, bool sendErrorMessage = true)
	{
		if (switchRef == null || switchRef == this.m_addFoodSwitch)
		{
			if (this.m_requireFire && !this.IsFireLit())
			{
				if (sendErrorMessage)
				{
					player.Message(MessageHud.MessageType.Center, "$msg_needfire", 0, null);
				}
				return false;
			}
			if (this.GetFreeSlot() == -1)
			{
				if (sendErrorMessage)
				{
					player.Message(MessageHud.MessageType.Center, "$msg_nocookroom", 0, null);
				}
				return false;
			}
			if (this.FindCookableItem(player.GetInventory()) != null)
			{
				return true;
			}
			if (sendErrorMessage)
			{
				player.Message(MessageHud.MessageType.Center, "$msg_nocookitems", 0, null);
			}
			return false;
		}
		else
		{
			if (switchRef != this.m_addFuelSwitch)
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

	// Token: 0x060015E2 RID: 5602 RVA: 0x000A17FC File Offset: 0x0009F9FC
	private void OnDrawGizmosSelected()
	{
		if (this.m_requireFire)
		{
			if (this.m_fireCheckPoints != null && this.m_fireCheckPoints.Length != 0)
			{
				foreach (Transform transform in this.m_fireCheckPoints)
				{
					Gizmos.color = Color.red;
					Gizmos.DrawWireSphere(transform.position, this.m_fireCheckRadius);
				}
				return;
			}
			Gizmos.color = Color.red;
			Gizmos.DrawWireSphere(base.transform.position, this.m_fireCheckRadius);
		}
	}

	// Token: 0x060015E4 RID: 5604 RVA: 0x000A1920 File Offset: 0x0009FB20
	[CompilerGenerated]
	private void <DropAllItems>g__drop|1_0(ItemDrop item)
	{
		Vector3 position = base.transform.position + Vector3.up + UnityEngine.Random.insideUnitSphere * 0.3f;
		Quaternion rotation = Quaternion.Euler(0f, (float)UnityEngine.Random.Range(0, 360), 0f);
		ItemDrop.OnCreateNew(UnityEngine.Object.Instantiate<GameObject>(item.gameObject, position, rotation));
	}

	// Token: 0x0400156A RID: 5482
	public Switch m_addFoodSwitch;

	// Token: 0x0400156B RID: 5483
	public Switch m_addFuelSwitch;

	// Token: 0x0400156C RID: 5484
	public EffectList m_addEffect = new EffectList();

	// Token: 0x0400156D RID: 5485
	public EffectList m_doneEffect = new EffectList();

	// Token: 0x0400156E RID: 5486
	public EffectList m_overcookedEffect = new EffectList();

	// Token: 0x0400156F RID: 5487
	public EffectList m_pickEffector = new EffectList();

	// Token: 0x04001570 RID: 5488
	public string m_addItemTooltip = "$piece_cstand_cook";

	// Token: 0x04001571 RID: 5489
	public Transform m_spawnPoint;

	// Token: 0x04001572 RID: 5490
	public float m_spawnForce = 5f;

	// Token: 0x04001573 RID: 5491
	public ItemDrop m_overCookedItem;

	// Token: 0x04001574 RID: 5492
	public List<CookingStation.ItemConversion> m_conversion = new List<CookingStation.ItemConversion>();

	// Token: 0x04001575 RID: 5493
	public List<CookingStation.ItemMessage> m_incompatibleItems = new List<CookingStation.ItemMessage>();

	// Token: 0x04001576 RID: 5494
	public Transform[] m_slots;

	// Token: 0x04001577 RID: 5495
	public ParticleSystem[] m_donePS;

	// Token: 0x04001578 RID: 5496
	public ParticleSystem[] m_burntPS;

	// Token: 0x04001579 RID: 5497
	public string m_name = "";

	// Token: 0x0400157A RID: 5498
	public bool m_requireFire = true;

	// Token: 0x0400157B RID: 5499
	public Transform[] m_fireCheckPoints;

	// Token: 0x0400157C RID: 5500
	public float m_fireCheckRadius = 0.25f;

	// Token: 0x0400157D RID: 5501
	public bool m_useFuel;

	// Token: 0x0400157E RID: 5502
	public ItemDrop m_fuelItem;

	// Token: 0x0400157F RID: 5503
	public int m_maxFuel = 10;

	// Token: 0x04001580 RID: 5504
	public int m_secPerFuel = 5000;

	// Token: 0x04001581 RID: 5505
	public EffectList m_fuelAddedEffects = new EffectList();

	// Token: 0x04001582 RID: 5506
	public GameObject m_haveFuelObject;

	// Token: 0x04001583 RID: 5507
	public GameObject m_haveFireObject;

	// Token: 0x04001584 RID: 5508
	private bool m_cheapFireCheck;

	// Token: 0x04001585 RID: 5509
	private ZNetView m_nview;

	// Token: 0x04001586 RID: 5510
	private ParticleSystem[] m_ps;

	// Token: 0x04001587 RID: 5511
	private AudioSource[] m_as;

	// Token: 0x0200034F RID: 847
	[Serializable]
	public class ItemConversion
	{
		// Token: 0x04002559 RID: 9561
		public ItemDrop m_from;

		// Token: 0x0400255A RID: 9562
		public ItemDrop m_to;

		// Token: 0x0400255B RID: 9563
		public float m_cookTime = 10f;
	}

	// Token: 0x02000350 RID: 848
	[Serializable]
	public class ItemMessage
	{
		// Token: 0x0400255C RID: 9564
		public ItemDrop m_item;

		// Token: 0x0400255D RID: 9565
		public string m_message;
	}

	// Token: 0x02000351 RID: 849
	private enum Status
	{
		// Token: 0x0400255F RID: 9567
		NotDone,
		// Token: 0x04002560 RID: 9568
		Done,
		// Token: 0x04002561 RID: 9569
		Burnt
	}
}
