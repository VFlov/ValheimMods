using System;
using System.Collections.Generic;
using UnityEngine;

// Token: 0x02000165 RID: 357
public class Container : MonoBehaviour, Hoverable, Interactable
{
	// Token: 0x0600159D RID: 5533 RVA: 0x0009F3C0 File Offset: 0x0009D5C0
	private void Awake()
	{
		this.m_nview = (this.m_rootObjectOverride ? this.m_rootObjectOverride.GetComponent<ZNetView>() : base.GetComponent<ZNetView>());
		if (this.m_nview.GetZDO() == null)
		{
			return;
		}
		this.m_inventory = new Inventory(this.m_name, this.m_bkg, this.m_width, this.m_height);
		Inventory inventory = this.m_inventory;
		inventory.m_onChanged = (Action)Delegate.Combine(inventory.m_onChanged, new Action(this.OnContainerChanged));
		this.m_piece = base.GetComponent<Piece>();
		if (this.m_nview)
		{
			this.m_nview.Register<long>("RequestOpen", new Action<long, long>(this.RPC_RequestOpen));
			this.m_nview.Register<bool>("OpenRespons", new Action<long, bool>(this.RPC_OpenRespons));
			this.m_nview.Register<long>("RPC_RequestStack", new Action<long, long>(this.RPC_RequestStack));
			this.m_nview.Register<bool>("RPC_StackResponse", new Action<long, bool>(this.RPC_StackResponse));
			this.m_nview.Register<long>("RequestTakeAll", new Action<long, long>(this.RPC_RequestTakeAll));
			this.m_nview.Register<bool>("TakeAllRespons", new Action<long, bool>(this.RPC_TakeAllRespons));
		}
		WearNTear wearNTear = this.m_rootObjectOverride ? this.m_rootObjectOverride.GetComponent<WearNTear>() : base.GetComponent<WearNTear>();
		if (wearNTear)
		{
			WearNTear wearNTear2 = wearNTear;
			wearNTear2.m_onDestroyed = (Action)Delegate.Combine(wearNTear2.m_onDestroyed, new Action(this.OnDestroyed));
		}
		Destructible destructible = this.m_rootObjectOverride ? this.m_rootObjectOverride.GetComponent<Destructible>() : base.GetComponent<Destructible>();
		if (destructible)
		{
			Destructible destructible2 = destructible;
			destructible2.m_onDestroyed = (Action)Delegate.Combine(destructible2.m_onDestroyed, new Action(this.OnDestroyed));
		}
		if (this.m_nview.IsOwner() && !this.m_nview.GetZDO().GetBool(ZDOVars.s_addedDefaultItems, false))
		{
			this.AddDefaultItems();
			this.m_nview.GetZDO().Set(ZDOVars.s_addedDefaultItems, true);
		}
		base.InvokeRepeating("CheckForChanges", 0f, 1f);
	}

	// Token: 0x0600159E RID: 5534 RVA: 0x0009F5FC File Offset: 0x0009D7FC
	private void AddDefaultItems()
	{
		foreach (ItemDrop.ItemData item in this.m_defaultItems.GetDropListItems())
		{
			this.m_inventory.AddItem(item);
		}
	}

	// Token: 0x0600159F RID: 5535 RVA: 0x0009F65C File Offset: 0x0009D85C
	private void DropAllItems(GameObject lootContainerPrefab)
	{
		while (this.m_inventory.NrOfItems() > 0)
		{
			Vector3 position = base.transform.position + UnityEngine.Random.insideUnitSphere * 1f;
			UnityEngine.Object.Instantiate<GameObject>(lootContainerPrefab, position, UnityEngine.Random.rotation).GetComponent<Container>().GetInventory().MoveAll(this.m_inventory);
		}
	}

	// Token: 0x060015A0 RID: 5536 RVA: 0x0009F6BC File Offset: 0x0009D8BC
	private void DropAllItems()
	{
		List<ItemDrop.ItemData> allItems = this.m_inventory.GetAllItems();
		int num = 1;
		foreach (ItemDrop.ItemData item in allItems)
		{
			Vector3 position = base.transform.position + Vector3.up * 0.5f + UnityEngine.Random.insideUnitSphere * 0.3f;
			Quaternion rotation = Quaternion.Euler(0f, (float)UnityEngine.Random.Range(0, 360), 0f);
			ItemDrop.DropItem(item, 0, position, rotation);
			num++;
		}
		this.m_inventory.RemoveAll();
		this.Save();
	}

	// Token: 0x060015A1 RID: 5537 RVA: 0x0009F77C File Offset: 0x0009D97C
	private void OnDestroyed()
	{
		if (this.m_nview.IsOwner())
		{
			if (this.m_destroyedLootPrefab)
			{
				this.DropAllItems(this.m_destroyedLootPrefab);
				return;
			}
			this.DropAllItems();
		}
	}

	// Token: 0x060015A2 RID: 5538 RVA: 0x0009F7AC File Offset: 0x0009D9AC
	private void CheckForChanges()
	{
		if (!this.m_nview.IsValid())
		{
			return;
		}
		if (!this.Load())
		{
			return;
		}
		this.UpdateUseVisual();
		if (this.m_autoDestroyEmpty && this.m_nview.IsOwner() && !this.IsInUse() && this.m_inventory.NrOfItems() == 0)
		{
			this.m_nview.Destroy();
		}
	}

	// Token: 0x060015A3 RID: 5539 RVA: 0x0009F80C File Offset: 0x0009DA0C
	private void UpdateUseVisual()
	{
		bool flag;
		if (this.m_nview.IsOwner())
		{
			flag = this.m_inUse;
			this.m_nview.GetZDO().Set(ZDOVars.s_inUse, this.m_inUse ? 1 : 0, false);
		}
		else
		{
			flag = (this.m_nview.GetZDO().GetInt(ZDOVars.s_inUse, 0) == 1);
		}
		if (this.m_open)
		{
			this.m_open.SetActive(flag);
		}
		if (this.m_closed)
		{
			this.m_closed.SetActive(!flag);
		}
	}

	// Token: 0x060015A4 RID: 5540 RVA: 0x0009F8A0 File Offset: 0x0009DAA0
	public string GetHoverText()
	{
		if (this.m_checkGuardStone && !PrivateArea.CheckAccess(base.transform.position, 0f, false, false))
		{
			return Localization.instance.Localize(this.m_name + "\n$piece_noaccess");
		}
		string text;
		if (this.m_inventory.NrOfItems() == 0)
		{
			text = this.m_name + " ( $piece_container_empty )";
		}
		else
		{
			text = this.m_name;
		}
		text += "\n[<color=yellow><b>$KEY_Use</b></color>] $piece_container_open $msg_stackall_hover";
		return Localization.instance.Localize(text);
	}

	// Token: 0x060015A5 RID: 5541 RVA: 0x0009F927 File Offset: 0x0009DB27
	public string GetHoverName()
	{
		return this.m_name;
	}

	// Token: 0x060015A6 RID: 5542 RVA: 0x0009F930 File Offset: 0x0009DB30
	public bool Interact(Humanoid character, bool hold, bool alt)
	{
		if (hold)
		{
			return false;
		}
		if (this.m_checkGuardStone && !PrivateArea.CheckAccess(base.transform.position, 0f, true, false))
		{
			return true;
		}
		long playerID = Game.instance.GetPlayerProfile().GetPlayerID();
		if (!this.CheckAccess(playerID))
		{
			character.Message(MessageHud.MessageType.Center, "$msg_cantopen", 0, null);
			return true;
		}
		this.m_nview.InvokeRPC("RequestOpen", new object[]
		{
			playerID
		});
		return true;
	}

	// Token: 0x060015A7 RID: 5543 RVA: 0x0009F9AE File Offset: 0x0009DBAE
	public bool UseItem(Humanoid user, ItemDrop.ItemData item)
	{
		return false;
	}

	// Token: 0x060015A8 RID: 5544 RVA: 0x0009F9B1 File Offset: 0x0009DBB1
	public bool CanBeRemoved()
	{
		return this.m_privacy != Container.PrivacySetting.Private || this.GetInventory().NrOfItems() <= 0;
	}

	// Token: 0x060015A9 RID: 5545 RVA: 0x0009F9CC File Offset: 0x0009DBCC
	private bool CheckAccess(long playerID)
	{
		switch (this.m_privacy)
		{
		case Container.PrivacySetting.Private:
			return this.m_piece.GetCreator() == playerID;
		case Container.PrivacySetting.Group:
			return false;
		case Container.PrivacySetting.Public:
			return true;
		default:
			return false;
		}
	}

	// Token: 0x060015AA RID: 5546 RVA: 0x0009FA08 File Offset: 0x0009DC08
	public bool IsOwner()
	{
		return this.m_nview.IsOwner();
	}

	// Token: 0x060015AB RID: 5547 RVA: 0x0009FA15 File Offset: 0x0009DC15
	public bool IsInUse()
	{
		return this.m_inUse;
	}

	// Token: 0x060015AC RID: 5548 RVA: 0x0009FA20 File Offset: 0x0009DC20
	public void SetInUse(bool inUse)
	{
		if (!this.m_nview.IsOwner())
		{
			return;
		}
		if (this.m_inUse == inUse)
		{
			return;
		}
		this.m_inUse = inUse;
		this.UpdateUseVisual();
		if (inUse)
		{
			this.m_openEffects.Create(base.transform.position, base.transform.rotation, null, 1f, -1);
			return;
		}
		this.m_closeEffects.Create(base.transform.position, base.transform.rotation, null, 1f, -1);
	}

	// Token: 0x060015AD RID: 5549 RVA: 0x0009FAA8 File Offset: 0x0009DCA8
	public Inventory GetInventory()
	{
		return this.m_inventory;
	}

	// Token: 0x060015AE RID: 5550 RVA: 0x0009FAB0 File Offset: 0x0009DCB0
	private void RPC_RequestOpen(long uid, long playerID)
	{
		ZLog.Log(string.Concat(new string[]
		{
			"Player ",
			uid.ToString(),
			" wants to open ",
			base.gameObject.name,
			"   im: ",
			ZDOMan.GetSessionID().ToString()
		}));
		if (!this.m_nview.IsOwner())
		{
			ZLog.Log("  but im not the owner");
			return;
		}
		if ((this.IsInUse() || (this.m_wagon && this.m_wagon.InUse())) && uid != ZNet.GetUID())
		{
			ZLog.Log("  in use");
			this.m_nview.InvokeRPC(uid, "OpenRespons", new object[]
			{
				false
			});
			return;
		}
		if (!this.CheckAccess(playerID))
		{
			ZLog.Log("  not yours");
			this.m_nview.InvokeRPC(uid, "OpenRespons", new object[]
			{
				false
			});
			return;
		}
		ZDOMan.instance.ForceSendZDO(uid, this.m_nview.GetZDO().m_uid);
		this.m_nview.GetZDO().SetOwner(uid);
		this.m_nview.InvokeRPC(uid, "OpenRespons", new object[]
		{
			true
		});
	}

	// Token: 0x060015AF RID: 5551 RVA: 0x0009FBFA File Offset: 0x0009DDFA
	private void RPC_OpenRespons(long uid, bool granted)
	{
		if (!Player.m_localPlayer)
		{
			return;
		}
		if (granted)
		{
			InventoryGui.instance.Show(this, 1);
			return;
		}
		Player.m_localPlayer.Message(MessageHud.MessageType.Center, "$msg_inuse", 0, null);
	}

	// Token: 0x060015B0 RID: 5552 RVA: 0x0009FC2C File Offset: 0x0009DE2C
	public void StackAll()
	{
		long playerID = Game.instance.GetPlayerProfile().GetPlayerID();
		this.m_nview.InvokeRPC("RPC_RequestStack", new object[]
		{
			playerID
		});
	}

	// Token: 0x060015B1 RID: 5553 RVA: 0x0009FC68 File Offset: 0x0009DE68
	private void RPC_RequestStack(long uid, long playerID)
	{
		ZLog.Log(string.Concat(new string[]
		{
			"Player ",
			uid.ToString(),
			" wants to stack all in ",
			base.gameObject.name,
			"   im: ",
			ZDOMan.GetSessionID().ToString()
		}));
		if (!this.m_nview.IsOwner())
		{
			ZLog.Log("  but im not the owner");
			return;
		}
		if ((this.IsInUse() || (this.m_wagon && this.m_wagon.InUse())) && uid != ZNet.GetUID())
		{
			ZLog.Log("  in use");
			this.m_nview.InvokeRPC(uid, "RPC_StackResponse", new object[]
			{
				false
			});
			return;
		}
		if (!this.CheckAccess(playerID))
		{
			ZLog.Log("  not yours");
			this.m_nview.InvokeRPC(uid, "RPC_StackResponse", new object[]
			{
				false
			});
			return;
		}
		ZDOMan.instance.ForceSendZDO(uid, this.m_nview.GetZDO().m_uid);
		this.m_nview.GetZDO().SetOwner(uid);
		this.m_nview.InvokeRPC(uid, "RPC_StackResponse", new object[]
		{
			true
		});
	}

	// Token: 0x060015B2 RID: 5554 RVA: 0x0009FDB4 File Offset: 0x0009DFB4
	private void RPC_StackResponse(long uid, bool granted)
	{
		if (!Player.m_localPlayer)
		{
			return;
		}
		if (granted)
		{
			if (this.m_inventory.StackAll(Player.m_localPlayer.GetInventory(), true) > 0)
			{
				InventoryGui.instance.m_moveItemEffects.Create(base.transform.position, Quaternion.identity, null, 1f, -1);
				return;
			}
		}
		else
		{
			Player.m_localPlayer.Message(MessageHud.MessageType.Center, "$msg_inuse", 0, null);
		}
	}

	// Token: 0x060015B3 RID: 5555 RVA: 0x0009FE24 File Offset: 0x0009E024
	public bool TakeAll(Humanoid character)
	{
		if (this.m_checkGuardStone && !PrivateArea.CheckAccess(base.transform.position, 0f, true, false))
		{
			return false;
		}
		long playerID = Game.instance.GetPlayerProfile().GetPlayerID();
		if (!this.CheckAccess(playerID))
		{
			character.Message(MessageHud.MessageType.Center, "$msg_cantopen", 0, null);
			return false;
		}
		this.m_nview.InvokeRPC("RequestTakeAll", new object[]
		{
			playerID
		});
		return true;
	}

	// Token: 0x060015B4 RID: 5556 RVA: 0x0009FEA0 File Offset: 0x0009E0A0
	private void RPC_RequestTakeAll(long uid, long playerID)
	{
		ZLog.Log(string.Concat(new string[]
		{
			"Player ",
			uid.ToString(),
			" wants to takeall from ",
			base.gameObject.name,
			"   im: ",
			ZDOMan.GetSessionID().ToString()
		}));
		if (!this.m_nview.IsOwner())
		{
			ZLog.Log("  but im not the owner");
			return;
		}
		if ((this.IsInUse() || (this.m_wagon && this.m_wagon.InUse())) && uid != ZNet.GetUID())
		{
			ZLog.Log("  in use");
			this.m_nview.InvokeRPC(uid, "TakeAllRespons", new object[]
			{
				false
			});
			return;
		}
		if (!this.CheckAccess(playerID))
		{
			ZLog.Log("  not yours");
			this.m_nview.InvokeRPC(uid, "TakeAllRespons", new object[]
			{
				false
			});
			return;
		}
		if (Time.time - this.m_lastTakeAllTime < 2f)
		{
			return;
		}
		this.m_lastTakeAllTime = Time.time;
		this.m_nview.InvokeRPC(uid, "TakeAllRespons", new object[]
		{
			true
		});
	}

	// Token: 0x060015B5 RID: 5557 RVA: 0x0009FFE0 File Offset: 0x0009E1E0
	private void RPC_TakeAllRespons(long uid, bool granted)
	{
		if (!Player.m_localPlayer)
		{
			return;
		}
		if (granted)
		{
			this.m_nview.ClaimOwnership();
			ZDOMan.instance.ForceSendZDO(uid, this.m_nview.GetZDO().m_uid);
			Player.m_localPlayer.GetInventory().MoveAll(this.m_inventory);
			if (this.m_onTakeAllSuccess != null)
			{
				this.m_onTakeAllSuccess();
				return;
			}
		}
		else
		{
			Player.m_localPlayer.Message(MessageHud.MessageType.Center, "$msg_inuse", 0, null);
		}
	}

	// Token: 0x060015B6 RID: 5558 RVA: 0x000A005E File Offset: 0x0009E25E
	private void OnContainerChanged()
	{
		if (this.m_loading)
		{
			return;
		}
		if (!this.IsOwner())
		{
			return;
		}
		this.Save();
	}

	// Token: 0x060015B7 RID: 5559 RVA: 0x000A0078 File Offset: 0x0009E278
	private void Save()
	{
		ZPackage zpackage = new ZPackage();
		this.m_inventory.Save(zpackage);
		string @base = zpackage.GetBase64();
		this.m_nview.GetZDO().Set(ZDOVars.s_items, @base);
		this.m_lastRevision = this.m_nview.GetZDO().DataRevision;
		this.m_lastDataString = @base;
	}

	// Token: 0x060015B8 RID: 5560 RVA: 0x000A00D4 File Offset: 0x0009E2D4
	private bool Load()
	{
		if (this.m_nview.GetZDO().DataRevision == this.m_lastRevision)
		{
			return false;
		}
		this.m_lastRevision = this.m_nview.GetZDO().DataRevision;
		string @string = this.m_nview.GetZDO().GetString(ZDOVars.s_items, "");
		if (@string == this.m_lastDataString)
		{
			this.m_lastDataString = @string;
			return true;
		}
		if (string.IsNullOrEmpty(@string))
		{
			this.m_lastDataString = @string;
			return true;
		}
		ZPackage pkg = new ZPackage(@string);
		this.m_loading = true;
		this.m_inventory.Load(pkg);
		this.m_loading = false;
		this.m_lastDataString = @string;
		return true;
	}

	// Token: 0x04001552 RID: 5458
	private float m_lastTakeAllTime;

	// Token: 0x04001553 RID: 5459
	public Action m_onTakeAllSuccess;

	// Token: 0x04001554 RID: 5460
	public string m_name = "Container";

	// Token: 0x04001555 RID: 5461
	public Sprite m_bkg;

	// Token: 0x04001556 RID: 5462
	public int m_width = 3;

	// Token: 0x04001557 RID: 5463
	public int m_height = 2;

	// Token: 0x04001558 RID: 5464
	public Container.PrivacySetting m_privacy = Container.PrivacySetting.Public;

	// Token: 0x04001559 RID: 5465
	public bool m_checkGuardStone;

	// Token: 0x0400155A RID: 5466
	public bool m_autoDestroyEmpty;

	// Token: 0x0400155B RID: 5467
	public DropTable m_defaultItems = new DropTable();

	// Token: 0x0400155C RID: 5468
	public GameObject m_open;

	// Token: 0x0400155D RID: 5469
	public GameObject m_closed;

	// Token: 0x0400155E RID: 5470
	public EffectList m_openEffects = new EffectList();

	// Token: 0x0400155F RID: 5471
	public EffectList m_closeEffects = new EffectList();

	// Token: 0x04001560 RID: 5472
	public ZNetView m_rootObjectOverride;

	// Token: 0x04001561 RID: 5473
	public Vagon m_wagon;

	// Token: 0x04001562 RID: 5474
	public GameObject m_destroyedLootPrefab;

	// Token: 0x04001563 RID: 5475
	private Inventory m_inventory;

	// Token: 0x04001564 RID: 5476
	private ZNetView m_nview;

	// Token: 0x04001565 RID: 5477
	private Piece m_piece;

	// Token: 0x04001566 RID: 5478
	private bool m_inUse;

	// Token: 0x04001567 RID: 5479
	private bool m_loading;

	// Token: 0x04001568 RID: 5480
	private uint m_lastRevision = uint.MaxValue;

	// Token: 0x04001569 RID: 5481
	private string m_lastDataString;

	// Token: 0x0200034E RID: 846
	public enum PrivacySetting
	{
		// Token: 0x04002556 RID: 9558
		Private,
		// Token: 0x04002557 RID: 9559
		Group,
		// Token: 0x04002558 RID: 9560
		Public
	}
}
