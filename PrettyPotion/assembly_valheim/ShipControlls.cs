using System;
using UnityEngine;

// Token: 0x020001B7 RID: 439
public class ShipControlls : MonoBehaviour, Interactable, Hoverable, IDoodadController
{
	// Token: 0x060019B5 RID: 6581 RVA: 0x000C0068 File Offset: 0x000BE268
	private void Awake()
	{
		this.m_nview = this.m_ship.GetComponent<ZNetView>();
		this.m_nview.Register<long>("RequestControl", new Action<long, long>(this.RPC_RequestControl));
		this.m_nview.Register<long>("ReleaseControl", new Action<long, long>(this.RPC_ReleaseControl));
		this.m_nview.Register<bool>("RequestRespons", new Action<long, bool>(this.RPC_RequestRespons));
	}

	// Token: 0x060019B6 RID: 6582 RVA: 0x000C00DA File Offset: 0x000BE2DA
	public bool IsValid()
	{
		return this;
	}

	// Token: 0x060019B7 RID: 6583 RVA: 0x000C00E2 File Offset: 0x000BE2E2
	public bool UseItem(Humanoid user, ItemDrop.ItemData item)
	{
		return false;
	}

	// Token: 0x060019B8 RID: 6584 RVA: 0x000C00E8 File Offset: 0x000BE2E8
	public bool Interact(Humanoid character, bool repeat, bool alt)
	{
		if (repeat)
		{
			return false;
		}
		if (!this.m_nview.IsValid())
		{
			return false;
		}
		if (!this.InUseDistance(character))
		{
			return false;
		}
		Player player = character as Player;
		if (player == null || player.IsEncumbered())
		{
			return false;
		}
		if (player.GetStandingOnShip() != this.m_ship)
		{
			return false;
		}
		this.m_nview.InvokeRPC("RequestControl", new object[]
		{
			player.GetPlayerID()
		});
		return false;
	}

	// Token: 0x060019B9 RID: 6585 RVA: 0x000C0168 File Offset: 0x000BE368
	public Component GetControlledComponent()
	{
		return this.m_ship;
	}

	// Token: 0x060019BA RID: 6586 RVA: 0x000C0170 File Offset: 0x000BE370
	public Vector3 GetPosition()
	{
		return base.transform.position;
	}

	// Token: 0x060019BB RID: 6587 RVA: 0x000C017D File Offset: 0x000BE37D
	public void ApplyControlls(Vector3 moveDir, Vector3 lookDir, bool run, bool autoRun, bool block)
	{
		this.m_ship.ApplyControlls(moveDir);
	}

	// Token: 0x060019BC RID: 6588 RVA: 0x000C018B File Offset: 0x000BE38B
	public string GetHoverText()
	{
		if (!this.InUseDistance(Player.m_localPlayer))
		{
			return Localization.instance.Localize("<color=#888888>$piece_toofar</color>");
		}
		return Localization.instance.Localize("[<color=yellow><b>$KEY_Use</b></color>] " + this.m_hoverText);
	}

	// Token: 0x060019BD RID: 6589 RVA: 0x000C01C4 File Offset: 0x000BE3C4
	public string GetHoverName()
	{
		return Localization.instance.Localize(this.m_hoverText);
	}

	// Token: 0x060019BE RID: 6590 RVA: 0x000C01D8 File Offset: 0x000BE3D8
	private void RPC_RequestControl(long sender, long playerID)
	{
		if (!this.m_nview.IsOwner())
		{
			return;
		}
		if (!this.m_ship.IsPlayerInBoat(playerID))
		{
			return;
		}
		if (this.GetUser() == playerID || !this.HaveValidUser())
		{
			this.m_nview.GetZDO().Set(ZDOVars.s_user, playerID);
			this.m_nview.InvokeRPC(sender, "RequestRespons", new object[]
			{
				true
			});
			return;
		}
		this.m_nview.InvokeRPC(sender, "RequestRespons", new object[]
		{
			false
		});
	}

	// Token: 0x060019BF RID: 6591 RVA: 0x000C026A File Offset: 0x000BE46A
	private void RPC_ReleaseControl(long sender, long playerID)
	{
		if (!this.m_nview.IsOwner())
		{
			return;
		}
		if (this.GetUser() == playerID)
		{
			this.m_nview.GetZDO().Set(ZDOVars.s_user, 0L);
		}
	}

	// Token: 0x060019C0 RID: 6592 RVA: 0x000C029C File Offset: 0x000BE49C
	private void RPC_RequestRespons(long sender, bool granted)
	{
		if (!Player.m_localPlayer)
		{
			return;
		}
		if (granted)
		{
			Player.m_localPlayer.StartDoodadControl(this);
			if (this.m_attachPoint != null)
			{
				Player.m_localPlayer.AttachStart(this.m_attachPoint, null, false, false, true, this.m_attachAnimation, this.m_detachOffset, null);
				return;
			}
		}
		else
		{
			Player.m_localPlayer.Message(MessageHud.MessageType.Center, "$msg_inuse", 0, null);
		}
	}

	// Token: 0x060019C1 RID: 6593 RVA: 0x000C0308 File Offset: 0x000BE508
	public void OnUseStop(Player player)
	{
		if (!this.m_nview.IsValid())
		{
			return;
		}
		this.m_nview.InvokeRPC("ReleaseControl", new object[]
		{
			player.GetPlayerID()
		});
		if (this.m_attachPoint != null)
		{
			player.AttachStop();
		}
	}

	// Token: 0x060019C2 RID: 6594 RVA: 0x000C035C File Offset: 0x000BE55C
	public bool HaveValidUser()
	{
		long user = this.GetUser();
		return user != 0L && this.m_ship.IsPlayerInBoat(user);
	}

	// Token: 0x060019C3 RID: 6595 RVA: 0x000C0381 File Offset: 0x000BE581
	private long GetUser()
	{
		if (!this.m_nview.IsValid())
		{
			return 0L;
		}
		return this.m_nview.GetZDO().GetLong(ZDOVars.s_user, 0L);
	}

	// Token: 0x060019C4 RID: 6596 RVA: 0x000C03AA File Offset: 0x000BE5AA
	private bool InUseDistance(Humanoid human)
	{
		return Vector3.Distance(human.transform.position, this.m_attachPoint.position) < this.m_maxUseRange;
	}

	// Token: 0x04001A38 RID: 6712
	public string m_hoverText = "";

	// Token: 0x04001A39 RID: 6713
	public Ship m_ship;

	// Token: 0x04001A3A RID: 6714
	public float m_maxUseRange = 10f;

	// Token: 0x04001A3B RID: 6715
	public Transform m_attachPoint;

	// Token: 0x04001A3C RID: 6716
	public Vector3 m_detachOffset = new Vector3(0f, 0.5f, 0f);

	// Token: 0x04001A3D RID: 6717
	public string m_attachAnimation = "attach_chair";

	// Token: 0x04001A3E RID: 6718
	private ZNetView m_nview;
}
