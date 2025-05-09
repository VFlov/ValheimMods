using System;
using TMPro;
using UnityEngine;

// Token: 0x02000045 RID: 69
public class TombStone : MonoBehaviour, Hoverable, Interactable
{
	// Token: 0x0600058D RID: 1421 RVA: 0x0002F408 File Offset: 0x0002D608
	private void Awake()
	{
		this.m_nview = base.GetComponent<ZNetView>();
		this.m_container = base.GetComponent<Container>();
		this.m_floating = base.GetComponent<Floating>();
		this.m_body = base.GetComponent<Rigidbody>();
		this.m_body.maxDepenetrationVelocity = 1f;
		this.m_body.solverIterations = 10;
		Container container = this.m_container;
		container.m_onTakeAllSuccess = (Action)Delegate.Combine(container.m_onTakeAllSuccess, new Action(this.OnTakeAllSuccess));
		if (this.m_nview.IsOwner() && this.m_nview.GetZDO().GetLong(ZDOVars.s_timeOfDeath, 0L) == 0L)
		{
			this.m_nview.GetZDO().Set(ZDOVars.s_timeOfDeath, ZNet.instance.GetTime().Ticks);
			this.m_nview.GetZDO().Set(ZDOVars.s_spawnPoint, base.transform.position);
		}
		base.InvokeRepeating("UpdateDespawn", TombStone.m_updateDt, TombStone.m_updateDt);
	}

	// Token: 0x0600058E RID: 1422 RVA: 0x0002F50C File Offset: 0x0002D70C
	private void Start()
	{
		string text = CensorShittyWords.FilterUGC(this.m_nview.GetZDO().GetString(ZDOVars.s_ownerName, ""), UGCType.CharacterName, this.GetOwner());
		base.GetComponent<Container>().m_name = text;
		this.m_worldText.text = text;
	}

	// Token: 0x0600058F RID: 1423 RVA: 0x0002F558 File Offset: 0x0002D758
	public string GetOwnerName()
	{
		return CensorShittyWords.FilterUGC(this.m_nview.GetZDO().GetString(ZDOVars.s_ownerName, ""), UGCType.CharacterName, this.GetOwner());
	}

	// Token: 0x06000590 RID: 1424 RVA: 0x0002F580 File Offset: 0x0002D780
	public string GetHoverText()
	{
		if (!this.m_nview.IsValid())
		{
			return "";
		}
		string str = this.m_text + " " + this.GetOwnerName();
		if (this.m_container.GetInventory().NrOfItems() == 0)
		{
			return "";
		}
		return Localization.instance.Localize(str + "\n[<color=yellow><b>$KEY_Use</b></color>] $piece_container_open");
	}

	// Token: 0x06000591 RID: 1425 RVA: 0x0002F5E4 File Offset: 0x0002D7E4
	public string GetHoverName()
	{
		return "";
	}

	// Token: 0x06000592 RID: 1426 RVA: 0x0002F5EC File Offset: 0x0002D7EC
	public bool Interact(Humanoid character, bool hold, bool alt)
	{
		if (hold)
		{
			return false;
		}
		if (this.m_container.GetInventory().NrOfItems() == 0)
		{
			return false;
		}
		if (!this.m_localOpened)
		{
			Game.instance.IncrementPlayerStat((this.GetOwnerName() == Game.instance.GetPlayerProfile().GetName()) ? PlayerStatType.TombstonesOpenedOwn : PlayerStatType.TombstonesOpenedOther, 1f);
		}
		if (this.IsOwner())
		{
			Player player = character as Player;
			if (this.EasyFitInInventory(player))
			{
				ZLog.Log("Grave should fit in inventory, loot all");
				this.m_container.TakeAll(character);
				if (!this.m_localOpened)
				{
					Game.instance.IncrementPlayerStat(PlayerStatType.TombstonesFit, 1f);
				}
				return true;
			}
		}
		this.m_localOpened = true;
		return this.m_container.Interact(character, false, false);
	}

	// Token: 0x06000593 RID: 1427 RVA: 0x0002F6AC File Offset: 0x0002D8AC
	private void OnTakeAllSuccess()
	{
		Player localPlayer = Player.m_localPlayer;
		if (localPlayer)
		{
			localPlayer.m_pickupEffects.Create(localPlayer.transform.position, Quaternion.identity, null, 1f, -1);
			localPlayer.Message(MessageHud.MessageType.Center, "$piece_tombstone_recovered", 0, null);
		}
	}

	// Token: 0x06000594 RID: 1428 RVA: 0x0002F6F8 File Offset: 0x0002D8F8
	private bool EasyFitInInventory(Player player)
	{
		int num = player.GetInventory().GetEmptySlots() - this.m_container.GetInventory().NrOfItems();
		if (num < 0)
		{
			foreach (ItemDrop.ItemData itemData in this.m_container.GetInventory().GetAllItems())
			{
				if (player.GetInventory().FindFreeStackSpace(itemData.m_shared.m_name, (float)itemData.m_worldLevel) >= itemData.m_stack)
				{
					num++;
				}
			}
			if (num < 0)
			{
				return false;
			}
		}
		return player.GetInventory().GetTotalWeight() + this.m_container.GetInventory().GetTotalWeight() <= player.GetMaxCarryWeight();
	}

	// Token: 0x06000595 RID: 1429 RVA: 0x0002F7C8 File Offset: 0x0002D9C8
	public bool UseItem(Humanoid user, ItemDrop.ItemData item)
	{
		return false;
	}

	// Token: 0x06000596 RID: 1430 RVA: 0x0002F7CC File Offset: 0x0002D9CC
	public void Setup(string ownerName, long ownerUID)
	{
		this.m_nview.GetZDO().Set(ZDOVars.s_ownerName, ownerName);
		this.m_nview.GetZDO().Set(ZDOVars.s_owner, ownerUID);
		if (this.m_body)
		{
			this.m_body.velocity = new Vector3(0f, this.m_spawnUpVel, 0f);
		}
	}

	// Token: 0x06000597 RID: 1431 RVA: 0x0002F832 File Offset: 0x0002DA32
	private long GetOwner()
	{
		if (this.m_nview.IsValid())
		{
			return this.m_nview.GetZDO().GetLong(ZDOVars.s_owner, 0L);
		}
		return 0L;
	}

	// Token: 0x06000598 RID: 1432 RVA: 0x0002F85C File Offset: 0x0002DA5C
	private bool IsOwner()
	{
		long owner = this.GetOwner();
		long playerID = Game.instance.GetPlayerProfile().GetPlayerID();
		return owner == playerID;
	}

	// Token: 0x06000599 RID: 1433 RVA: 0x0002F884 File Offset: 0x0002DA84
	private void UpdateDespawn()
	{
		if (!this.m_nview.IsValid())
		{
			return;
		}
		if (this.m_floater != null)
		{
			this.UpdateFloater();
		}
		if (!this.m_nview.IsOwner())
		{
			return;
		}
		this.PositionCheck();
		if (!this.m_container.IsInUse() && this.m_container.GetInventory().NrOfItems() <= 0)
		{
			this.GiveBoost();
			this.m_removeEffect.Create(base.transform.position, base.transform.rotation, null, 1f, -1);
			this.m_nview.Destroy();
		}
	}

	// Token: 0x0600059A RID: 1434 RVA: 0x0002F924 File Offset: 0x0002DB24
	private void GiveBoost()
	{
		if (this.m_lootStatusEffect == null)
		{
			return;
		}
		Player player = this.FindOwner();
		if (player)
		{
			player.GetSEMan().AddStatusEffect(this.m_lootStatusEffect.NameHash(), true, 0, 0f);
		}
	}

	// Token: 0x0600059B RID: 1435 RVA: 0x0002F970 File Offset: 0x0002DB70
	private Player FindOwner()
	{
		long owner = this.GetOwner();
		if (owner == 0L)
		{
			return null;
		}
		return Player.GetPlayer(owner);
	}

	// Token: 0x0600059C RID: 1436 RVA: 0x0002F990 File Offset: 0x0002DB90
	private void PositionCheck()
	{
		if (!this.m_body)
		{
			this.m_body = FloatingTerrain.GetBody(base.gameObject);
		}
		Vector3 vec = this.m_nview.GetZDO().GetVec3(ZDOVars.s_spawnPoint, base.transform.position);
		if (Utils.DistanceXZ(vec, base.transform.position) > 4f)
		{
			ZLog.Log("Tombstone moved too far from spawn position, reseting position");
			base.transform.position = vec;
			this.m_body.position = vec;
			this.m_body.velocity = Vector3.zero;
		}
		float groundHeight = ZoneSystem.instance.GetGroundHeight(base.transform.position);
		if (base.transform.position.y < groundHeight - 1f)
		{
			Vector3 position = base.transform.position;
			position.y = groundHeight + 0.5f;
			base.transform.position = position;
			this.m_body.position = position;
			this.m_body.velocity = Vector3.zero;
		}
	}

	// Token: 0x0600059D RID: 1437 RVA: 0x0002FA98 File Offset: 0x0002DC98
	private void UpdateFloater()
	{
		if (this.m_nview.IsOwner())
		{
			bool flag = this.m_floating.BeenFloating();
			this.m_nview.GetZDO().Set(ZDOVars.s_inWater, flag);
			this.m_floater.SetActive(flag);
			return;
		}
		bool @bool = this.m_nview.GetZDO().GetBool(ZDOVars.s_inWater, false);
		this.m_floater.SetActive(@bool);
	}

	// Token: 0x0400062F RID: 1583
	private static float m_updateDt = 2f;

	// Token: 0x04000630 RID: 1584
	public string m_text = "$piece_tombstone";

	// Token: 0x04000631 RID: 1585
	public GameObject m_floater;

	// Token: 0x04000632 RID: 1586
	public TMP_Text m_worldText;

	// Token: 0x04000633 RID: 1587
	public float m_spawnUpVel = 5f;

	// Token: 0x04000634 RID: 1588
	public StatusEffect m_lootStatusEffect;

	// Token: 0x04000635 RID: 1589
	public EffectList m_removeEffect = new EffectList();

	// Token: 0x04000636 RID: 1590
	private Container m_container;

	// Token: 0x04000637 RID: 1591
	private ZNetView m_nview;

	// Token: 0x04000638 RID: 1592
	private Floating m_floating;

	// Token: 0x04000639 RID: 1593
	private Rigidbody m_body;

	// Token: 0x0400063A RID: 1594
	private bool m_localOpened;
}
