using System;
using System.Collections.Generic;
using UnityEngine;

// Token: 0x020001D2 RID: 466
public class Trap : MonoBehaviour, Hoverable, Interactable
{
	// Token: 0x06001AB2 RID: 6834 RVA: 0x000C65B4 File Offset: 0x000C47B4
	private void Awake()
	{
		this.m_nview = base.GetComponent<ZNetView>();
		this.m_aoe = this.m_AOE.GetComponent<Aoe>();
		this.m_piece = base.GetComponent<Piece>();
		if (!this.m_aoe)
		{
			ZLog.LogError("Trap '" + base.gameObject.name + "' is missing AOE!");
		}
		this.m_aoe.gameObject.SetActive(false);
		if (this.m_nview)
		{
			this.m_nview.Register<int>("RPC_RequestStateChange", new Action<long, int>(this.RPC_RequestStateChange));
			this.m_nview.Register<int, long>("RPC_OnStateChanged", new Action<long, int, long>(this.RPC_OnStateChanged));
			if (this.m_startsArmed && this.m_nview.IsValid() && this.m_nview.GetZDO().GetInt(ZDOVars.s_state, -1) == -1)
			{
				this.m_nview.GetZDO().Set(ZDOVars.s_state, 1, false);
			}
			this.UpdateState();
		}
	}

	// Token: 0x06001AB3 RID: 6835 RVA: 0x000C66BC File Offset: 0x000C48BC
	private void Update()
	{
		if (this.m_nview.IsValid() && this.m_nview.IsOwner())
		{
			if (this.IsActive() && !this.IsCoolingDown())
			{
				this.RequestStateChange(Trap.TrapState.Unarmed);
			}
			if (this.m_onReceiveOwnershipActions.Count > 0)
			{
				for (int i = 0; i < this.m_onReceiveOwnershipActions.Count; i++)
				{
					Action action = this.m_onReceiveOwnershipActions[i];
					if (action != null)
					{
						action();
					}
				}
				this.m_onReceiveOwnershipActions.Clear();
			}
		}
	}

	// Token: 0x06001AB4 RID: 6836 RVA: 0x000C6740 File Offset: 0x000C4940
	public bool IsArmed()
	{
		return this.m_nview.IsValid() && this.m_nview.GetZDO().GetInt(ZDOVars.s_state, 0) == 1;
	}

	// Token: 0x06001AB5 RID: 6837 RVA: 0x000C676A File Offset: 0x000C496A
	public bool IsActive()
	{
		return this.m_nview.IsValid() && this.m_nview.GetZDO().GetInt(ZDOVars.s_state, 0) == 2;
	}

	// Token: 0x06001AB6 RID: 6838 RVA: 0x000C6794 File Offset: 0x000C4994
	public bool IsCoolingDown()
	{
		return this.m_nview.IsValid() && (double)(this.m_nview.GetZDO().GetFloat(ZDOVars.s_triggered, 0f) + (float)this.m_rearmCooldown) > ZNet.instance.GetTimeSeconds();
	}

	// Token: 0x06001AB7 RID: 6839 RVA: 0x000C67D4 File Offset: 0x000C49D4
	public string GetHoverText()
	{
		if (!this.m_nview.IsValid())
		{
			return "";
		}
		if (!PrivateArea.CheckAccess(base.transform.position, 0f, false, false))
		{
			return Localization.instance.Localize(this.m_name + "\n$piece_noaccess");
		}
		if (this.IsArmed())
		{
			return Localization.instance.Localize(this.m_name + " ($piece_trap_armed)");
		}
		if (this.IsCoolingDown())
		{
			return Localization.instance.Localize(this.m_name);
		}
		return Localization.instance.Localize(this.m_name + "\n[<color=yellow><b>$KEY_Use</b></color>] $piece_trap_arm");
	}

	// Token: 0x06001AB8 RID: 6840 RVA: 0x000C687E File Offset: 0x000C4A7E
	public string GetHoverName()
	{
		return this.m_name;
	}

	// Token: 0x06001AB9 RID: 6841 RVA: 0x000C6888 File Offset: 0x000C4A88
	public bool Interact(Humanoid character, bool hold, bool alt)
	{
		if (hold)
		{
			return false;
		}
		if (!PrivateArea.CheckAccess(base.transform.position, 0f, true, false))
		{
			return true;
		}
		if (this.IsArmed())
		{
			return false;
		}
		if (this.IsCoolingDown())
		{
			Player.m_localPlayer.Message(MessageHud.MessageType.Center, Localization.instance.Localize("$piece_trap_cooldown"), 0, null);
			return true;
		}
		this.RequestStateChange(Trap.TrapState.Armed);
		return true;
	}

	// Token: 0x06001ABA RID: 6842 RVA: 0x000C68ED File Offset: 0x000C4AED
	public bool UseItem(Humanoid user, ItemDrop.ItemData item)
	{
		return false;
	}

	// Token: 0x06001ABB RID: 6843 RVA: 0x000C68F0 File Offset: 0x000C4AF0
	private void RequestStateChange(Trap.TrapState newState)
	{
		if (!this.m_nview.IsValid())
		{
			return;
		}
		if (this.m_nview.IsOwner())
		{
			this.RPC_RequestStateChange(ZNet.GetUID(), (int)newState);
			return;
		}
		this.m_nview.InvokeRPC("RPC_RequestStateChange", new object[]
		{
			(int)newState
		});
	}

	// Token: 0x06001ABC RID: 6844 RVA: 0x000C6944 File Offset: 0x000C4B44
	private void RPC_OnStateChanged(long uid, int value, long idOfClientModifyingState)
	{
		if (!this.m_nview.IsValid())
		{
			return;
		}
		this.m_onReceiveOwnershipActions.Clear();
		switch (value)
		{
		case 1:
			if (idOfClientModifyingState == ZNet.GetUID())
			{
				this.m_armEffects.Create(base.transform.position, base.transform.rotation, null, 1f, -1);
				Game.instance.IncrementPlayerStat(PlayerStatType.TrapArmed, 1f);
			}
			this.m_tempTriggeringHumanoid = null;
			goto IL_148;
		case 2:
			if (idOfClientModifyingState != ZNet.GetUID())
			{
				goto IL_148;
			}
			if (this.m_nview.IsOwner())
			{
				this.TriggerTrap();
			}
			else
			{
				this.m_onReceiveOwnershipActions.Add(new Action(this.TriggerTrap));
			}
			if (!(this.m_tempTriggeringHumanoid != null) || this.m_tempTriggeringHumanoid.GetZDOID().ID != Player.m_localPlayer.GetZDOID().ID)
			{
				goto IL_148;
			}
			if (this.m_nview.IsOwner())
			{
				Game.instance.IncrementPlayerStat(PlayerStatType.TrapTriggered, 1f);
				goto IL_148;
			}
			this.m_onReceiveOwnershipActions.Add(delegate
			{
				Game.instance.IncrementPlayerStat(PlayerStatType.TrapTriggered, 1f);
			});
			goto IL_148;
		}
		this.m_tempTriggeringHumanoid = null;
		IL_148:
		this.UpdateState((Trap.TrapState)value);
	}

	// Token: 0x06001ABD RID: 6845 RVA: 0x000C6AA0 File Offset: 0x000C4CA0
	private void TriggerTrap()
	{
		if (this.m_tempTriggeringHumanoid != null)
		{
			this.m_tempTriggeringHumanoid.transform.position = this.m_tempTriggeredPosition;
			Physics.SyncTransforms();
			if (this.m_forceStagger)
			{
				this.m_tempTriggeringHumanoid.Stagger(Vector3.zero);
			}
		}
		this.m_nview.GetZDO().Set(ZDOVars.s_state, 2, false);
		this.m_nview.GetZDO().Set(ZDOVars.s_triggered, (float)ZNet.instance.GetTimeSeconds());
		UnityEngine.Object.Instantiate<GameObject>(this.m_aoe.gameObject, base.transform).SetActive(true);
		this.m_triggerEffects.Create(base.transform.position, base.transform.rotation, null, 1f, -1);
	}

	// Token: 0x06001ABE RID: 6846 RVA: 0x000C6B6C File Offset: 0x000C4D6C
	private void RPC_RequestStateChange(long senderID, int value)
	{
		if (!this.m_nview.IsValid())
		{
			return;
		}
		if (!this.m_nview.IsOwner())
		{
			return;
		}
		if (this.m_nview.GetZDO().GetInt(ZDOVars.s_state, 0) == value)
		{
			return;
		}
		if (value != 2)
		{
			this.m_nview.GetZDO().Set(ZDOVars.s_state, value, false);
		}
		else if (senderID != ZNet.GetUID())
		{
			this.m_nview.GetZDO().SetOwner(senderID);
		}
		this.m_nview.InvokeRPC(ZNetView.Everybody, "RPC_OnStateChanged", new object[]
		{
			value,
			senderID
		});
	}

	// Token: 0x06001ABF RID: 6847 RVA: 0x000C6C14 File Offset: 0x000C4E14
	private void UpdateState(Trap.TrapState state)
	{
		this.m_piece.m_randomTarget = (state == Trap.TrapState.Unarmed);
		this.m_visualArmed.SetActive(state == Trap.TrapState.Armed);
		this.m_visualUnarmed.SetActive(state != Trap.TrapState.Armed);
	}

	// Token: 0x06001AC0 RID: 6848 RVA: 0x000C6C48 File Offset: 0x000C4E48
	private void UpdateState()
	{
		if (!this.m_nview || !this.m_nview.IsValid())
		{
			return;
		}
		Trap.TrapState @int = (Trap.TrapState)this.m_nview.GetZDO().GetInt(ZDOVars.s_state, 0);
		this.UpdateState(@int);
	}

	// Token: 0x06001AC1 RID: 6849 RVA: 0x000C6C90 File Offset: 0x000C4E90
	private void OnTriggerEnter(Collider collider)
	{
		Humanoid humanoid = null;
		Player componentInParent = collider.GetComponentInParent<Player>();
		if (componentInParent != null)
		{
			if (!this.m_triggeredByPlayers)
			{
				return;
			}
			if (componentInParent != Player.m_localPlayer)
			{
				return;
			}
		}
		else if (collider.GetComponentInParent<MonsterAI>() != null)
		{
			if (!this.m_triggeredByEnemies)
			{
				return;
			}
			humanoid = collider.GetComponentInParent<Humanoid>();
			if (humanoid != null && !humanoid.IsOwner())
			{
				return;
			}
		}
		if (this.IsArmed())
		{
			if (humanoid == null)
			{
				humanoid = collider.GetComponentInParent<Humanoid>();
			}
			if (humanoid != null)
			{
				this.m_tempTriggeringHumanoid = humanoid;
				this.m_tempTriggeredPosition = humanoid.transform.position;
			}
			this.RequestStateChange(Trap.TrapState.Active);
		}
	}

	// Token: 0x04001B1D RID: 6941
	public string m_name = "Trap";

	// Token: 0x04001B1E RID: 6942
	public GameObject m_AOE;

	// Token: 0x04001B1F RID: 6943
	public Collider m_trigger;

	// Token: 0x04001B20 RID: 6944
	public int m_rearmCooldown = 60;

	// Token: 0x04001B21 RID: 6945
	public GameObject m_visualArmed;

	// Token: 0x04001B22 RID: 6946
	public GameObject m_visualUnarmed;

	// Token: 0x04001B23 RID: 6947
	public bool m_triggeredByEnemies;

	// Token: 0x04001B24 RID: 6948
	public bool m_triggeredByPlayers;

	// Token: 0x04001B25 RID: 6949
	public bool m_forceStagger = true;

	// Token: 0x04001B26 RID: 6950
	public EffectList m_triggerEffects;

	// Token: 0x04001B27 RID: 6951
	public EffectList m_armEffects;

	// Token: 0x04001B28 RID: 6952
	public bool m_startsArmed;

	// Token: 0x04001B29 RID: 6953
	private ZNetView m_nview;

	// Token: 0x04001B2A RID: 6954
	private Aoe m_aoe;

	// Token: 0x04001B2B RID: 6955
	private Piece m_piece;

	// Token: 0x04001B2C RID: 6956
	private Humanoid m_tempTriggeringHumanoid;

	// Token: 0x04001B2D RID: 6957
	private Vector3 m_tempTriggeredPosition = Vector3.zero;

	// Token: 0x04001B2E RID: 6958
	private List<Action> m_onReceiveOwnershipActions = new List<Action>();

	// Token: 0x0200038E RID: 910
	private enum TrapState
	{
		// Token: 0x040026A7 RID: 9895
		Unarmed,
		// Token: 0x040026A8 RID: 9896
		Armed,
		// Token: 0x040026A9 RID: 9897
		Active
	}
}
