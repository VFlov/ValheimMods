using System;
using Splatform;
using UnityEngine;

// Token: 0x020001C5 RID: 453
public class TeleportWorld : MonoBehaviour, Hoverable, Interactable, TextReceiver
{
	// Token: 0x06001A44 RID: 6724 RVA: 0x000C3208 File Offset: 0x000C1408
	private void Awake()
	{
		this.m_nview = base.GetComponent<ZNetView>();
		if (this.m_nview.GetZDO() == null)
		{
			base.enabled = false;
			return;
		}
		this.m_hadTarget = this.HaveTarget();
		this.m_nview.Register<string, string>("RPC_SetTag", new Action<long, string, string>(this.RPC_SetTag));
		this.m_nview.Register<ZDOID>("RPC_SetConnected", new Action<long, ZDOID>(this.RPC_SetConnected));
		base.InvokeRepeating("UpdatePortal", 0.5f, 0.5f);
	}

	// Token: 0x06001A45 RID: 6725 RVA: 0x000C3290 File Offset: 0x000C1490
	public string GetHoverText()
	{
		string text = this.GetText().RemoveRichTextTags();
		string text2 = this.HaveTarget() ? "$piece_portal_connected" : "$piece_portal_unconnected";
		return Localization.instance.Localize(string.Concat(new string[]
		{
			"$piece_portal $piece_portal_tag:\"",
			text,
			"\"  [",
			text2,
			"]\n[<color=yellow><b>$KEY_Use</b></color>] $piece_portal_settag"
		}));
	}

	// Token: 0x06001A46 RID: 6726 RVA: 0x000C32F3 File Offset: 0x000C14F3
	public string GetHoverName()
	{
		return "Teleport";
	}

	// Token: 0x06001A47 RID: 6727 RVA: 0x000C32FC File Offset: 0x000C14FC
	public bool Interact(Humanoid human, bool hold, bool alt)
	{
		if (hold)
		{
			return false;
		}
		if (!PrivateArea.CheckAccess(base.transform.position, 0f, true, false))
		{
			human.Message(MessageHud.MessageType.Center, "$piece_noaccess", 0, null);
			return true;
		}
		TextInput.instance.RequestText(this, "$piece_portal_tag", 10);
		return true;
	}

	// Token: 0x06001A48 RID: 6728 RVA: 0x000C334A File Offset: 0x000C154A
	public bool UseItem(Humanoid user, ItemDrop.ItemData item)
	{
		return false;
	}

	// Token: 0x06001A49 RID: 6729 RVA: 0x000C3350 File Offset: 0x000C1550
	private void UpdatePortal()
	{
		if (!this.m_nview.IsValid() || this.m_proximityRoot == null)
		{
			return;
		}
		Player closestPlayer = Player.GetClosestPlayer(this.m_proximityRoot.position, this.m_activationRange);
		bool flag = this.HaveTarget();
		if (flag && !this.m_hadTarget)
		{
			this.m_connected.Create(base.transform.position, base.transform.rotation, null, 1f, -1);
		}
		this.m_hadTarget = flag;
		bool flag2 = false;
		if (closestPlayer)
		{
			flag2 = (closestPlayer.IsTeleportable() || this.m_allowAllItems);
		}
		this.m_target_found.SetActive(flag2 && this.TargetFound());
	}

	// Token: 0x06001A4A RID: 6730 RVA: 0x000C3408 File Offset: 0x000C1608
	private void Update()
	{
		this.m_colorAlpha = Mathf.MoveTowards(this.m_colorAlpha, this.m_hadTarget ? 1f : 0f, Time.deltaTime);
		this.m_model.material.SetColor("_EmissionColor", Color.Lerp(this.m_colorUnconnected, this.m_colorTargetfound, this.m_colorAlpha));
	}

	// Token: 0x06001A4B RID: 6731 RVA: 0x000C346C File Offset: 0x000C166C
	public void Teleport(Player player)
	{
		if (!this.TargetFound())
		{
			return;
		}
		if (ZoneSystem.instance.GetGlobalKey(GlobalKeys.NoPortals))
		{
			player.Message(MessageHud.MessageType.Center, "$msg_blocked", 0, null);
			return;
		}
		float num;
		if (ZoneSystem.instance.GetGlobalKey(GlobalKeys.NoBossPortals) && (RandEventSystem.instance.GetBossEvent() != null || (ZoneSystem.instance.GetGlobalKey(GlobalKeys.activeBosses, out num) && num > 0f)))
		{
			player.Message(MessageHud.MessageType.Center, "$msg_blockedbyboss", 0, null);
			return;
		}
		if (!this.m_allowAllItems && !player.IsTeleportable())
		{
			player.Message(MessageHud.MessageType.Center, "$msg_noteleport", 0, null);
			return;
		}
		ZLog.Log("Teleporting " + player.GetPlayerName());
		ZDO zdo = ZDOMan.instance.GetZDO(this.m_nview.GetZDO().GetConnectionZDOID(ZDOExtraData.ConnectionType.Portal));
		if (zdo == null)
		{
			return;
		}
		Vector3 position = zdo.GetPosition();
		Quaternion rotation = zdo.GetRotation();
		Vector3 a = rotation * Vector3.forward;
		Vector3 pos = position + a * this.m_exitDistance + Vector3.up;
		player.TeleportTo(pos, rotation, true);
		Game.instance.IncrementPlayerStat(PlayerStatType.PortalsUsed, 1f);
	}

	// Token: 0x06001A4C RID: 6732 RVA: 0x000C358C File Offset: 0x000C178C
	public string GetText()
	{
		ZDO zdo = this.m_nview.GetZDO();
		if (zdo == null)
		{
			return "";
		}
		return CensorShittyWords.FilterUGC(zdo.GetString(ZDOVars.s_tag, ""), UGCType.Text, new PlatformUserID(zdo.GetString(ZDOVars.s_tagauthor, "")), 0L);
	}

	// Token: 0x06001A4D RID: 6733 RVA: 0x000C35DC File Offset: 0x000C17DC
	private void GetTagSignature(out string tagRaw, out string authorId)
	{
		ZDO zdo = this.m_nview.GetZDO();
		tagRaw = zdo.GetString(ZDOVars.s_tag, "");
		authorId = zdo.GetString(ZDOVars.s_tagauthor, "");
	}

	// Token: 0x06001A4E RID: 6734 RVA: 0x000C361C File Offset: 0x000C181C
	public void SetText(string text)
	{
		if (!this.m_nview.IsValid())
		{
			return;
		}
		this.m_nview.InvokeRPC("RPC_SetTag", new object[]
		{
			text,
			PlatformManager.DistributionPlatform.LocalUser.PlatformUserID.ToString()
		});
	}

	// Token: 0x06001A4F RID: 6735 RVA: 0x000C3674 File Offset: 0x000C1874
	private void RPC_SetTag(long sender, string tag, string authorId)
	{
		if (!this.m_nview.IsValid() || !this.m_nview.IsOwner())
		{
			return;
		}
		string a;
		string a2;
		this.GetTagSignature(out a, out a2);
		if (a == tag && a2 == authorId)
		{
			return;
		}
		ZDO zdo = this.m_nview.GetZDO();
		zdo.UpdateConnection(ZDOExtraData.ConnectionType.Portal, ZDOID.None);
		ZDOID connectionZDOID = zdo.GetConnectionZDOID(ZDOExtraData.ConnectionType.Portal);
		this.SetConnectedPortal(connectionZDOID);
		zdo.Set(ZDOVars.s_tag, tag);
		zdo.Set(ZDOVars.s_tagauthor, authorId);
	}

	// Token: 0x06001A50 RID: 6736 RVA: 0x000C36F8 File Offset: 0x000C18F8
	private void SetConnectedPortal(ZDOID targetID)
	{
		ZDO zdo = ZDOMan.instance.GetZDO(targetID);
		if (zdo == null)
		{
			return;
		}
		long owner = zdo.GetOwner();
		if (owner == 0L)
		{
			zdo.SetOwner(ZDOMan.GetSessionID());
			zdo.SetConnection(ZDOExtraData.ConnectionType.Portal, ZDOID.None);
			return;
		}
		this.m_nview.InvokeRPC(owner, "RPC_SetConnected", new object[]
		{
			targetID
		});
	}

	// Token: 0x06001A51 RID: 6737 RVA: 0x000C3758 File Offset: 0x000C1958
	private void RPC_SetConnected(long sender, ZDOID portalID)
	{
		ZDO zdo = ZDOMan.instance.GetZDO(portalID);
		if (zdo == null || !zdo.IsOwner())
		{
			return;
		}
		zdo.UpdateConnection(ZDOExtraData.ConnectionType.Portal, ZDOID.None);
	}

	// Token: 0x06001A52 RID: 6738 RVA: 0x000C3789 File Offset: 0x000C1989
	private bool HaveTarget()
	{
		return !(this.m_nview == null) && this.m_nview.GetZDO() != null && this.m_nview.GetZDO().GetConnectionZDOID(ZDOExtraData.ConnectionType.Portal) != ZDOID.None;
	}

	// Token: 0x06001A53 RID: 6739 RVA: 0x000C37C4 File Offset: 0x000C19C4
	private bool TargetFound()
	{
		if (this.m_nview == null || this.m_nview.GetZDO() == null)
		{
			return false;
		}
		ZDOID connectionZDOID = this.m_nview.GetZDO().GetConnectionZDOID(ZDOExtraData.ConnectionType.Portal);
		if (connectionZDOID == ZDOID.None)
		{
			return false;
		}
		if (ZDOMan.instance.GetZDO(connectionZDOID) == null)
		{
			ZDOMan.instance.RequestZDO(connectionZDOID);
			return false;
		}
		return true;
	}

	// Token: 0x04001AAE RID: 6830
	public float m_activationRange = 5f;

	// Token: 0x04001AAF RID: 6831
	public float m_exitDistance = 1f;

	// Token: 0x04001AB0 RID: 6832
	public Transform m_proximityRoot;

	// Token: 0x04001AB1 RID: 6833
	[ColorUsage(true, true)]
	public Color m_colorUnconnected = Color.white;

	// Token: 0x04001AB2 RID: 6834
	[ColorUsage(true, true)]
	public Color m_colorTargetfound = Color.white;

	// Token: 0x04001AB3 RID: 6835
	public EffectFade m_target_found;

	// Token: 0x04001AB4 RID: 6836
	public MeshRenderer m_model;

	// Token: 0x04001AB5 RID: 6837
	public EffectList m_connected;

	// Token: 0x04001AB6 RID: 6838
	public bool m_allowAllItems;

	// Token: 0x04001AB7 RID: 6839
	private ZNetView m_nview;

	// Token: 0x04001AB8 RID: 6840
	private bool m_hadTarget;

	// Token: 0x04001AB9 RID: 6841
	private float m_colorAlpha;
}
