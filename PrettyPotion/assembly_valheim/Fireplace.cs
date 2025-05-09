using System;
using System.Collections.Generic;
using UnityEngine;

// Token: 0x02000179 RID: 377
public class Fireplace : MonoBehaviour, Hoverable, Interactable, IHasHoverMenu
{
	// Token: 0x0600169F RID: 5791 RVA: 0x000A7594 File Offset: 0x000A5794
	public void Awake()
	{
		this.m_nview = base.gameObject.GetComponent<ZNetView>();
		this.m_piece = base.gameObject.GetComponent<Piece>();
		if (this.m_nview.GetZDO() == null)
		{
			return;
		}
		if (Fireplace.m_solidRayMask == 0)
		{
			Fireplace.m_solidRayMask = LayerMask.GetMask(new string[]
			{
				"Default",
				"static_solid",
				"Default_small",
				"piece",
				"terrain"
			});
		}
		if (this.m_nview.IsOwner() && this.m_nview.GetZDO().GetFloat(ZDOVars.s_fuel, -1f) == -1f)
		{
			this.m_nview.GetZDO().Set(ZDOVars.s_fuel, this.m_startFuel);
			if (this.m_startFuel > 0f)
			{
				this.m_fuelAddedEffects.Create(base.transform.position, base.transform.rotation, null, 1f, -1);
			}
		}
		Vector3 p = this.m_enabledObject ? this.m_enabledObject.transform.position : base.transform.position;
		p.y -= 15f;
		this.m_checkWaterLevel = Floating.IsUnderWater(p, ref this.m_previousWaterVolume);
		this.m_nview.Register("RPC_AddFuel", new Action<long>(this.RPC_AddFuel));
		this.m_nview.Register<float>("RPC_AddFuelAmount", new Action<long, float>(this.RPC_AddFuelAmount));
		this.m_nview.Register<float>("RPC_SetFuelAmount", new Action<long, float>(this.RPC_SetFuelAmount));
		this.m_nview.Register("RPC_ToggleOn", new Action<long>(this.RPC_ToggleOn));
		base.InvokeRepeating("UpdateFireplace", 0f, 2f);
		base.InvokeRepeating("CheckEnv", 4f, 4f);
		if (this.m_igniteInterval > 0f && this.m_igniteCapsuleRadius > 0f)
		{
			base.InvokeRepeating("UpdateIgnite", this.m_igniteInterval, this.m_igniteInterval);
		}
	}

	// Token: 0x060016A0 RID: 5792 RVA: 0x000A77A7 File Offset: 0x000A59A7
	private void Start()
	{
		if (this.m_playerBaseObject && this.m_piece)
		{
			this.m_playerBaseObject.SetActive(this.m_piece.IsPlacedByPlayer());
		}
	}

	// Token: 0x060016A1 RID: 5793 RVA: 0x000A77DC File Offset: 0x000A59DC
	private double GetTimeSinceLastUpdate()
	{
		DateTime time = ZNet.instance.GetTime();
		DateTime d = new DateTime(this.m_nview.GetZDO().GetLong(ZDOVars.s_lastTime, time.Ticks));
		TimeSpan timeSpan = time - d;
		this.m_nview.GetZDO().Set(ZDOVars.s_lastTime, time.Ticks);
		double num = timeSpan.TotalSeconds;
		if (num < 0.0)
		{
			num = 0.0;
		}
		return num;
	}

	// Token: 0x060016A2 RID: 5794 RVA: 0x000A785C File Offset: 0x000A5A5C
	private void UpdateFireplace()
	{
		if (!this.m_nview.IsValid())
		{
			return;
		}
		if (this.m_nview.IsOwner() && this.m_secPerFuel > 0f)
		{
			float num = this.m_nview.GetZDO().GetFloat(ZDOVars.s_fuel, 0f);
			double timeSinceLastUpdate = this.GetTimeSinceLastUpdate();
			bool flag = this.m_nview.GetZDO().GetInt(ZDOVars.s_state, 1) == 1;
			if (this.IsBurning() && !this.m_infiniteFuel && flag)
			{
				float num2 = (float)(timeSinceLastUpdate / (double)this.m_secPerFuel);
				num -= num2;
				if (num <= 0f)
				{
					num = 0f;
				}
				this.m_nview.GetZDO().Set(ZDOVars.s_fuel, num);
			}
		}
		this.UpdateState();
	}

	// Token: 0x060016A3 RID: 5795 RVA: 0x000A7924 File Offset: 0x000A5B24
	private void CheckEnv()
	{
		this.CheckUnderTerrain();
		if (this.m_enabledObjectLow != null && this.m_enabledObjectHigh != null)
		{
			this.CheckWet();
		}
	}

	// Token: 0x060016A4 RID: 5796 RVA: 0x000A7950 File Offset: 0x000A5B50
	private void CheckUnderTerrain()
	{
		this.m_blocked = false;
		if (this.m_disableCoverCheck)
		{
			return;
		}
		float num;
		if (Heightmap.GetHeight(base.transform.position, out num) && num > base.transform.position.y + this.m_checkTerrainOffset)
		{
			this.m_blocked = true;
			return;
		}
		RaycastHit raycastHit;
		if (Physics.Raycast(base.transform.position + Vector3.up * this.m_coverCheckOffset, Vector3.up, out raycastHit, 0.5f, Fireplace.m_solidRayMask))
		{
			this.m_blocked = true;
			return;
		}
		if (this.m_smokeSpawner && this.m_smokeSpawner.IsBlocked())
		{
			this.m_blocked = true;
			return;
		}
	}

	// Token: 0x060016A5 RID: 5797 RVA: 0x000A7A08 File Offset: 0x000A5C08
	private void CheckWet()
	{
		this.m_wet = false;
		bool flag = EnvMan.instance.GetWindIntensity() >= 0.8f;
		bool flag2 = EnvMan.IsWet();
		if (flag || flag2)
		{
			float num;
			bool flag3;
			Cover.GetCoverForPoint(base.transform.position + Vector3.up * this.m_coverCheckOffset, out num, out flag3, 0.5f);
			if (flag && num < 0.7f)
			{
				this.m_wet = true;
				return;
			}
			if (flag2 && !flag3)
			{
				this.m_wet = true;
			}
		}
	}

	// Token: 0x060016A6 RID: 5798 RVA: 0x000A7A8C File Offset: 0x000A5C8C
	private void UpdateState()
	{
		float @float = this.m_nview.GetZDO().GetFloat(ZDOVars.s_fuel, 0f);
		bool flag = @float >= this.m_halfThreshold;
		bool flag2 = @float <= 0f;
		if (this.m_lowWetOverHalf)
		{
			bool flag3 = !this.m_wet;
		}
		if (this.IsBurning())
		{
			if (this.m_enabledObject)
			{
				this.m_enabledObject.SetActive(true);
			}
			if (this.m_enabledObjectHigh && this.m_enabledObjectLow)
			{
				if (this.m_enabledObjectHigh.activeSelf != !this.m_wet)
				{
					this.m_enabledObjectHigh.SetActive(!this.m_wet);
				}
				if (this.m_enabledObjectLow.activeSelf != this.m_wet)
				{
					this.m_enabledObjectLow.SetActive(this.m_wet);
				}
			}
			if (this.m_canTurnOff && this.m_wet && this.m_nview.IsOwner() && this.m_nview.GetZDO().GetInt(ZDOVars.s_state, 1) == 1)
			{
				this.m_nview.InvokeRPC("RPC_ToggleOn", Array.Empty<object>());
			}
		}
		else
		{
			if (this.m_enabledObject)
			{
				this.m_enabledObject.SetActive(false);
			}
			if (this.m_enabledObjectHigh && this.m_enabledObjectLow)
			{
				if (this.m_enabledObjectLow.activeSelf)
				{
					this.m_enabledObjectLow.SetActive(false);
				}
				if (this.m_enabledObjectHigh.activeSelf)
				{
					this.m_enabledObjectHigh.SetActive(false);
				}
			}
		}
		if (this.m_fullObject && this.m_halfObject)
		{
			this.m_fullObject.SetActive(flag);
			this.m_halfObject.SetActive(!flag);
		}
		if (this.m_emptyObject)
		{
			if (flag2)
			{
				if (this.m_fullObject && this.m_fullObject.activeSelf)
				{
					this.m_fullObject.SetActive(false);
				}
				if (this.m_halfObject && this.m_halfObject.activeSelf)
				{
					this.m_halfObject.SetActive(false);
				}
			}
			if (this.m_emptyObject.activeSelf != flag2)
			{
				this.m_emptyObject.SetActive(flag2);
			}
		}
	}

	// Token: 0x060016A7 RID: 5799 RVA: 0x000A7CD4 File Offset: 0x000A5ED4
	public string GetHoverText()
	{
		if (!this.m_nview.IsValid() || this.m_infiniteFuel)
		{
			return "";
		}
		float @float = this.m_nview.GetZDO().GetFloat(ZDOVars.s_fuel, 0f);
		string text = this.m_name;
		if (this.m_canRefill)
		{
			text += string.Format("\n( $piece_fire_fuel {0}/{1} )\n[<color=yellow><b>$KEY_Use</b></color>] $piece_use {2}\n[<color=yellow><b>1-8</b></color>] $piece_useitem", Mathf.Ceil(@float), (int)this.m_maxFuel, this.m_fuelItem.m_itemData.m_shared.m_name);
		}
		else if (this.m_canTurnOff && @float > 0f)
		{
			text += "\n[<color=yellow><b>$KEY_Use</b></color>] $piece_use";
		}
		return Localization.instance.Localize(text);
	}

	// Token: 0x060016A8 RID: 5800 RVA: 0x000A7D8C File Offset: 0x000A5F8C
	public string GetHoverName()
	{
		return this.m_name;
	}

	// Token: 0x060016A9 RID: 5801 RVA: 0x000A7D94 File Offset: 0x000A5F94
	public void AddFuel(float fuel)
	{
		if (!this.m_nview || !this.m_nview.IsValid())
		{
			return;
		}
		float @float = this.m_nview.GetZDO().GetFloat(ZDOVars.s_fuel, 0f);
		if ((fuel < 0f && @float > 0f) || (fuel > 0f && @float < this.m_maxFuel))
		{
			this.m_nview.InvokeRPC("RPC_AddFuelAmount", new object[]
			{
				fuel
			});
		}
	}

	// Token: 0x060016AA RID: 5802 RVA: 0x000A7E18 File Offset: 0x000A6018
	public bool Interact(Humanoid user, bool hold, bool alt)
	{
		if (hold)
		{
			if (this.m_holdRepeatInterval <= 0f)
			{
				return false;
			}
			if (Time.time - this.m_lastUseTime < this.m_holdRepeatInterval)
			{
				return false;
			}
		}
		if (!this.m_nview.HasOwner())
		{
			this.m_nview.ClaimOwnership();
		}
		float @float = this.m_nview.GetZDO().GetFloat(ZDOVars.s_fuel, 0f);
		if (this.m_canTurnOff && !hold && !alt && @float > 0f)
		{
			this.m_nview.InvokeRPC("RPC_ToggleOn", Array.Empty<object>());
			return true;
		}
		if (this.m_canRefill)
		{
			Inventory inventory = user.GetInventory();
			if (inventory != null)
			{
				if (this.m_infiniteFuel)
				{
					return false;
				}
				if (!inventory.HaveItem(this.m_fuelItem.m_itemData.m_shared.m_name, true))
				{
					user.Message(MessageHud.MessageType.Center, "$msg_outof " + this.m_fuelItem.m_itemData.m_shared.m_name, 0, null);
					return false;
				}
				if ((float)Mathf.CeilToInt(@float) >= this.m_maxFuel)
				{
					user.Message(MessageHud.MessageType.Center, Localization.instance.Localize("$msg_cantaddmore", new string[]
					{
						this.m_fuelItem.m_itemData.m_shared.m_name
					}), 0, null);
					return false;
				}
				user.Message(MessageHud.MessageType.Center, Localization.instance.Localize("$msg_fireadding", new string[]
				{
					this.m_fuelItem.m_itemData.m_shared.m_name
				}), 0, null);
				inventory.RemoveItem(this.m_fuelItem.m_itemData.m_shared.m_name, 1, -1, true);
				this.m_nview.InvokeRPC("RPC_AddFuel", Array.Empty<object>());
				return true;
			}
		}
		return false;
	}

	// Token: 0x060016AB RID: 5803 RVA: 0x000A7FD0 File Offset: 0x000A61D0
	public bool UseItem(Humanoid user, ItemDrop.ItemData item)
	{
		if (!this.m_canRefill)
		{
			return false;
		}
		if (!(item.m_shared.m_name == this.m_fuelItem.m_itemData.m_shared.m_name) || this.m_infiniteFuel)
		{
			int i = 0;
			while (i < this.m_fireworkItemList.Length)
			{
				if (item.m_shared.m_name == this.m_fireworkItemList[i].m_fireworkItem.m_itemData.m_shared.m_name)
				{
					if (!this.IsBurning())
					{
						user.Message(MessageHud.MessageType.Center, "$msg_firenotburning", 0, null);
						return true;
					}
					if (user.GetInventory().CountItems(this.m_fireworkItemList[i].m_fireworkItem.m_itemData.m_shared.m_name, -1, true) < this.m_fireworkItemList[i].m_fireworkItemCount)
					{
						user.Message(MessageHud.MessageType.Center, "$msg_toofew " + this.m_fireworkItemList[i].m_fireworkItem.m_itemData.m_shared.m_name, 0, null);
						return true;
					}
					user.GetInventory().RemoveItem(item.m_shared.m_name, this.m_fireworkItemList[i].m_fireworkItemCount, -1, true);
					user.Message(MessageHud.MessageType.Center, Localization.instance.Localize("$msg_throwinfire", new string[]
					{
						item.m_shared.m_name
					}), 0, null);
					float x = UnityEngine.Random.Range(-this.m_fireworksMaxRandomAngle, this.m_fireworksMaxRandomAngle);
					float z = UnityEngine.Random.Range(-this.m_fireworksMaxRandomAngle, this.m_fireworksMaxRandomAngle);
					Quaternion baseRot = Quaternion.Euler(x, 0f, z);
					this.m_fireworkItemList[i].m_fireworksEffects.Create(base.transform.position, baseRot, null, 1f, -1);
					this.m_fuelAddedEffects.Create(base.transform.position, base.transform.rotation, null, 1f, -1);
					return true;
				}
				else
				{
					i++;
				}
			}
			return false;
		}
		if ((float)Mathf.CeilToInt(this.m_nview.GetZDO().GetFloat(ZDOVars.s_fuel, 0f)) >= this.m_maxFuel)
		{
			user.Message(MessageHud.MessageType.Center, Localization.instance.Localize("$msg_cantaddmore", new string[]
			{
				item.m_shared.m_name
			}), 0, null);
			return true;
		}
		Inventory inventory = user.GetInventory();
		user.Message(MessageHud.MessageType.Center, Localization.instance.Localize("$msg_fireadding", new string[]
		{
			item.m_shared.m_name
		}), 0, null);
		inventory.RemoveItem(item, 1);
		this.m_nview.InvokeRPC("RPC_AddFuel", Array.Empty<object>());
		return true;
	}

	// Token: 0x060016AC RID: 5804 RVA: 0x000A827C File Offset: 0x000A647C
	private void RPC_AddFuel(long sender)
	{
		if (this.m_nview.IsOwner())
		{
			float num = this.m_nview.GetZDO().GetFloat(ZDOVars.s_fuel, 0f);
			if ((float)Mathf.CeilToInt(num) >= this.m_maxFuel)
			{
				return;
			}
			num = Mathf.Clamp(num, 0f, this.m_maxFuel);
			num += 1f;
			num = Mathf.Clamp(num, 0f, this.m_maxFuel);
			this.m_nview.GetZDO().Set(ZDOVars.s_fuel, num);
			this.m_fuelAddedEffects.Create(base.transform.position, base.transform.rotation, null, 1f, -1);
			this.UpdateState();
		}
	}

	// Token: 0x060016AD RID: 5805 RVA: 0x000A8338 File Offset: 0x000A6538
	private void RPC_ToggleOn(long sender)
	{
		if (this.m_nview.IsOwner())
		{
			bool flag = this.m_nview.GetZDO().GetInt(ZDOVars.s_state, 1) == 1;
			this.m_nview.GetZDO().Set(ZDOVars.s_state, flag ? 2 : 1, false);
			this.m_toggleOnEffects.Create(base.transform.position, Quaternion.identity, null, 1f, flag ? 2 : 1);
		}
		this.UpdateState();
	}

	// Token: 0x060016AE RID: 5806 RVA: 0x000A83B8 File Offset: 0x000A65B8
	private void RPC_AddFuelAmount(long sender, float amount)
	{
		if (this.m_nview.IsOwner())
		{
			float num = this.m_nview.GetZDO().GetFloat(ZDOVars.s_fuel, 0f);
			num = Mathf.Clamp(num + amount, 0f, this.m_maxFuel);
			this.m_nview.GetZDO().Set(ZDOVars.s_fuel, num);
			this.m_fuelAddedEffects.Create(base.transform.position, base.transform.rotation, null, 1f, -1);
			this.UpdateState();
		}
	}

	// Token: 0x060016AF RID: 5807 RVA: 0x000A8448 File Offset: 0x000A6648
	public void SetFuel(float fuel)
	{
		if (!this.m_nview || !this.m_nview.IsValid())
		{
			return;
		}
		float @float = this.m_nview.GetZDO().GetFloat(ZDOVars.s_fuel, 0f);
		fuel = Mathf.Clamp(fuel, 0f, this.m_maxFuel);
		if (fuel != @float)
		{
			this.m_nview.InvokeRPC("RPC_SetFuelAmount", new object[]
			{
				fuel
			});
		}
	}

	// Token: 0x060016B0 RID: 5808 RVA: 0x000A84C4 File Offset: 0x000A66C4
	private void RPC_SetFuelAmount(long sender, float fuel)
	{
		if (this.m_nview.IsOwner())
		{
			this.m_nview.GetZDO().Set(ZDOVars.s_fuel, fuel);
			this.m_fuelAddedEffects.Create(base.transform.position, base.transform.rotation, null, 1f, -1);
			this.UpdateState();
		}
	}

	// Token: 0x060016B1 RID: 5809 RVA: 0x000A8523 File Offset: 0x000A6723
	public bool CanBeRemoved()
	{
		return !this.IsBurning();
	}

	// Token: 0x060016B2 RID: 5810 RVA: 0x000A8530 File Offset: 0x000A6730
	public bool IsBurning()
	{
		return !this.m_blocked && this.m_nview.GetZDO().GetInt(ZDOVars.s_state, 1) == 1 && (!this.m_checkWaterLevel || !Floating.IsUnderWater(this.m_enabledObject ? this.m_enabledObject.transform.position : base.transform.position, ref this.m_previousWaterVolume)) && (this.m_nview.GetZDO().GetFloat(ZDOVars.s_fuel, 0f) > 0f || this.m_infiniteFuel);
	}

	// Token: 0x060016B3 RID: 5811 RVA: 0x000A85D0 File Offset: 0x000A67D0
	private void OnDrawGizmosSelected()
	{
		Gizmos.color = Color.white;
		Gizmos.DrawWireSphere(base.transform.position + Vector3.up * this.m_coverCheckOffset, 0.5f);
		Gizmos.color = Color.yellow;
		Gizmos.DrawWireCube(base.transform.position + Vector3.up * this.m_checkTerrainOffset, new Vector3(1f, 0.01f, 1f));
		Gizmos.color = Color.red;
		Utils.DrawGizmoCapsule(base.transform.position + this.m_igniteCapsuleStart, base.transform.position + this.m_igniteCapsuleEnd, this.m_igniteCapsuleRadius);
	}

	// Token: 0x060016B4 RID: 5812 RVA: 0x000A8698 File Offset: 0x000A6898
	private void UpdateIgnite()
	{
		if (!this.m_nview.IsValid() || !this.m_nview.IsOwner())
		{
			return;
		}
		if (!this.m_firePrefab)
		{
			return;
		}
		if (!this.CanIgnite())
		{
			return;
		}
		if (!this.IsBurning())
		{
			return;
		}
		int num = Physics.OverlapCapsuleNonAlloc(base.transform.position + this.m_igniteCapsuleStart, base.transform.position + this.m_igniteCapsuleEnd, this.m_igniteCapsuleRadius, Fireplace.s_tempColliders);
		for (int i = 0; i < num; i++)
		{
			Collider collider = Fireplace.s_tempColliders[i];
			bool flag;
			if (!(collider.gameObject == base.gameObject) && (!(collider.transform.parent != null) || !(collider.transform.parent.gameObject == base.gameObject)) && !collider.isTrigger && UnityEngine.Random.Range(0f, 1f) <= this.m_igniteChance && Cinder.CanBurn(collider, collider.transform.position, out flag, 0f))
			{
				CinderSpawner component = UnityEngine.Object.Instantiate<GameObject>(this.m_firePrefab, collider.transform.position + Utils.RandomVector3(-0.1f, 0.1f), Quaternion.identity).GetComponent<CinderSpawner>();
				if (component != null)
				{
					component.Setup(this.m_igniteSpread, collider.gameObject);
				}
			}
		}
	}

	// Token: 0x060016B5 RID: 5813 RVA: 0x000A8806 File Offset: 0x000A6A06
	public bool TryGetItems(Player player, out List<string> items)
	{
		items = new List<string>();
		if (this.m_infiniteFuel)
		{
			return false;
		}
		if (!this.CanUseItems(player, true))
		{
			return true;
		}
		items.Add(this.m_fuelItem.m_itemData.m_shared.m_name);
		return true;
	}

	// Token: 0x060016B6 RID: 5814 RVA: 0x000A8844 File Offset: 0x000A6A44
	public bool CanUseItems(Player player, bool sendErrorMessage = true)
	{
		if (this.m_infiniteFuel)
		{
			return false;
		}
		if (!player.GetInventory().HaveItem(this.m_fuelItem.m_itemData.m_shared.m_name, true))
		{
			if (sendErrorMessage)
			{
				player.Message(MessageHud.MessageType.Center, "$msg_outof " + this.m_fuelItem.m_itemData.m_shared.m_name, 0, null);
			}
			return false;
		}
		if ((float)Mathf.CeilToInt(this.m_nview.GetZDO().GetFloat(ZDOVars.s_fuel, 0f)) < this.m_maxFuel)
		{
			return true;
		}
		if (sendErrorMessage)
		{
			player.Message(MessageHud.MessageType.Center, Localization.instance.Localize("$msg_cantaddmore", new string[]
			{
				this.m_fuelItem.m_itemData.m_shared.m_name
			}), 0, null);
		}
		return false;
	}

	// Token: 0x060016B7 RID: 5815 RVA: 0x000A890F File Offset: 0x000A6B0F
	public bool CanIgnite()
	{
		return CinderSpawner.CanSpawnCinder(base.transform, ref this.m_biome);
	}

	// Token: 0x04001653 RID: 5715
	private ZNetView m_nview;

	// Token: 0x04001654 RID: 5716
	private Piece m_piece;

	// Token: 0x04001655 RID: 5717
	[Header("Fire")]
	public string m_name = "Fire";

	// Token: 0x04001656 RID: 5718
	public float m_startFuel = 3f;

	// Token: 0x04001657 RID: 5719
	public float m_maxFuel = 10f;

	// Token: 0x04001658 RID: 5720
	public float m_secPerFuel = 3f;

	// Token: 0x04001659 RID: 5721
	public bool m_infiniteFuel;

	// Token: 0x0400165A RID: 5722
	public bool m_disableCoverCheck;

	// Token: 0x0400165B RID: 5723
	public float m_checkTerrainOffset = 0.2f;

	// Token: 0x0400165C RID: 5724
	public float m_coverCheckOffset = 0.5f;

	// Token: 0x0400165D RID: 5725
	private const float m_minimumOpenSpace = 0.5f;

	// Token: 0x0400165E RID: 5726
	public float m_holdRepeatInterval = 0.2f;

	// Token: 0x0400165F RID: 5727
	public float m_halfThreshold = 0.5f;

	// Token: 0x04001660 RID: 5728
	public bool m_canTurnOff;

	// Token: 0x04001661 RID: 5729
	public bool m_canRefill = true;

	// Token: 0x04001662 RID: 5730
	public bool m_lowWetOverHalf = true;

	// Token: 0x04001663 RID: 5731
	public GameObject m_enabledObject;

	// Token: 0x04001664 RID: 5732
	public GameObject m_enabledObjectLow;

	// Token: 0x04001665 RID: 5733
	public GameObject m_enabledObjectHigh;

	// Token: 0x04001666 RID: 5734
	public GameObject m_fullObject;

	// Token: 0x04001667 RID: 5735
	public GameObject m_halfObject;

	// Token: 0x04001668 RID: 5736
	public GameObject m_emptyObject;

	// Token: 0x04001669 RID: 5737
	public GameObject m_playerBaseObject;

	// Token: 0x0400166A RID: 5738
	public ItemDrop m_fuelItem;

	// Token: 0x0400166B RID: 5739
	public SmokeSpawner m_smokeSpawner;

	// Token: 0x0400166C RID: 5740
	public EffectList m_fuelAddedEffects = new EffectList();

	// Token: 0x0400166D RID: 5741
	public EffectList m_toggleOnEffects = new EffectList();

	// Token: 0x0400166E RID: 5742
	[Header("Fireworks")]
	[Range(0f, 60f)]
	public float m_fireworksMaxRandomAngle = 5f;

	// Token: 0x0400166F RID: 5743
	public Fireplace.FireworkItem[] m_fireworkItemList;

	// Token: 0x04001670 RID: 5744
	[Header("Ignite Pieces")]
	public float m_igniteInterval;

	// Token: 0x04001671 RID: 5745
	public float m_igniteChance;

	// Token: 0x04001672 RID: 5746
	public int m_igniteSpread = 4;

	// Token: 0x04001673 RID: 5747
	public float m_igniteCapsuleRadius;

	// Token: 0x04001674 RID: 5748
	public Vector3 m_igniteCapsuleStart;

	// Token: 0x04001675 RID: 5749
	public Vector3 m_igniteCapsuleEnd;

	// Token: 0x04001676 RID: 5750
	public GameObject m_firePrefab;

	// Token: 0x04001677 RID: 5751
	private bool m_blocked;

	// Token: 0x04001678 RID: 5752
	private bool m_wet;

	// Token: 0x04001679 RID: 5753
	private Heightmap.Biome m_biome;

	// Token: 0x0400167A RID: 5754
	private float m_lastUseTime;

	// Token: 0x0400167B RID: 5755
	private bool m_checkWaterLevel;

	// Token: 0x0400167C RID: 5756
	private WaterVolume m_previousWaterVolume;

	// Token: 0x0400167D RID: 5757
	private static int m_solidRayMask = 0;

	// Token: 0x0400167E RID: 5758
	private static Collider[] s_tempColliders = new Collider[20];

	// Token: 0x0200035E RID: 862
	[Serializable]
	public struct FireworkItem
	{
		// Token: 0x0400259E RID: 9630
		public ItemDrop m_fireworkItem;

		// Token: 0x0400259F RID: 9631
		public int m_fireworkItemCount;

		// Token: 0x040025A0 RID: 9632
		public EffectList m_fireworksEffects;
	}
}
