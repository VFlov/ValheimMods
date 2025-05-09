using System;
using UnityEngine;

// Token: 0x0200016C RID: 364
public class Door : MonoBehaviour, Hoverable, Interactable
{
	// Token: 0x060015FD RID: 5629 RVA: 0x000A2248 File Offset: 0x000A0448
	private void Awake()
	{
		this.m_nview = base.GetComponent<ZNetView>();
		if (this.m_nview.GetZDO() == null)
		{
			return;
		}
		this.m_animator = base.GetComponentInChildren<Animator>();
		if (this.m_nview)
		{
			this.m_nview.Register<bool>("UseDoor", new Action<long, bool>(this.RPC_UseDoor));
		}
		base.InvokeRepeating("UpdateState", 0f, 0.2f);
	}

	// Token: 0x060015FE RID: 5630 RVA: 0x000A22BC File Offset: 0x000A04BC
	private void UpdateState()
	{
		if (!this.m_nview.IsValid())
		{
			return;
		}
		uint dataRevision = this.m_nview.GetZDO().DataRevision;
		if (this.m_lastDataRevision == dataRevision)
		{
			return;
		}
		this.m_lastDataRevision = dataRevision;
		int @int = this.m_nview.GetZDO().GetInt(ZDOVars.s_state, 0);
		this.SetState(@int);
	}

	// Token: 0x060015FF RID: 5631 RVA: 0x000A2318 File Offset: 0x000A0518
	private void SetState(int state)
	{
		if (this.m_animator.GetInteger("state") != state)
		{
			if (state != 0)
			{
				this.m_openEffects.Create(base.transform.position, base.transform.rotation, null, 1f, -1);
			}
			else
			{
				this.m_closeEffects.Create(base.transform.position, base.transform.rotation, null, 1f, -1);
			}
			this.m_animator.SetInteger("state", state);
		}
		if (this.m_openEnable)
		{
			this.m_openEnable.SetActive(state != 0);
		}
	}

	// Token: 0x06001600 RID: 5632 RVA: 0x000A23BC File Offset: 0x000A05BC
	private bool CanInteract()
	{
		return ((!(this.m_keyItem != null) && !this.m_canNotBeClosed) || this.m_nview.GetZDO().GetInt(ZDOVars.s_state, 0) == 0) && (this.m_animator.GetCurrentAnimatorStateInfo(0).IsTag("open") || this.m_animator.GetCurrentAnimatorStateInfo(0).IsTag("closed"));
	}

	// Token: 0x06001601 RID: 5633 RVA: 0x000A2430 File Offset: 0x000A0630
	public string GetHoverText()
	{
		if (!this.m_nview.IsValid())
		{
			return "";
		}
		if (this.m_canNotBeClosed && !this.CanInteract())
		{
			return "";
		}
		if (this.m_checkGuardStone && !PrivateArea.CheckAccess(base.transform.position, 0f, false, false))
		{
			return Localization.instance.Localize(this.m_name + "\n$piece_noaccess");
		}
		if (!this.CanInteract())
		{
			return Localization.instance.Localize(this.m_name);
		}
		if (this.m_nview.GetZDO().GetInt(ZDOVars.s_state, 0) != 0)
		{
			return Localization.instance.Localize(this.m_name + "\n[<color=yellow><b>$KEY_Use</b></color>] " + (this.m_invertedOpenClosedText ? "$piece_door_open" : "$piece_door_close"));
		}
		return Localization.instance.Localize(this.m_name + "\n[<color=yellow><b>$KEY_Use</b></color>] " + (this.m_invertedOpenClosedText ? "$piece_door_close" : "$piece_door_open"));
	}

	// Token: 0x06001602 RID: 5634 RVA: 0x000A2530 File Offset: 0x000A0730
	public string GetHoverName()
	{
		return this.m_name;
	}

	// Token: 0x06001603 RID: 5635 RVA: 0x000A2538 File Offset: 0x000A0738
	public bool Interact(Humanoid character, bool hold, bool alt)
	{
		if (hold)
		{
			return false;
		}
		if (!this.CanInteract())
		{
			return false;
		}
		if (this.m_checkGuardStone && !PrivateArea.CheckAccess(base.transform.position, 0f, true, false))
		{
			return true;
		}
		if (this.m_keyItem != null)
		{
			if (!this.HaveKey(character, true))
			{
				this.m_lockedEffects.Create(base.transform.position, base.transform.rotation, null, 1f, -1);
				if (Game.m_worldLevel > 0 && this.HaveKey(character, false))
				{
					character.Message(MessageHud.MessageType.Center, Localization.instance.Localize("$msg_ng_the_x") + this.m_keyItem.m_itemData.m_shared.m_name + Localization.instance.Localize("$msg_ng_x_is_too_low"), 0, null);
				}
				else
				{
					character.Message(MessageHud.MessageType.Center, Localization.instance.Localize("$msg_door_needkey", new string[]
					{
						this.m_keyItem.m_itemData.m_shared.m_name
					}), 0, null);
				}
				return true;
			}
			character.Message(MessageHud.MessageType.Center, Localization.instance.Localize("$msg_door_usingkey", new string[]
			{
				this.m_keyItem.m_itemData.m_shared.m_name
			}), 0, null);
		}
		Vector3 normalized = (character.transform.position - base.transform.position).normalized;
		Game.instance.IncrementPlayerStat((this.m_nview.GetZDO().GetInt(ZDOVars.s_state, 0) == 0) ? PlayerStatType.DoorsOpened : PlayerStatType.DoorsClosed, 1f);
		this.Open(normalized);
		return true;
	}

	// Token: 0x06001604 RID: 5636 RVA: 0x000A26DC File Offset: 0x000A08DC
	private void Open(Vector3 userDir)
	{
		bool flag = Vector3.Dot(base.transform.forward, userDir) < 0f;
		this.m_nview.InvokeRPC("UseDoor", new object[]
		{
			flag
		});
	}

	// Token: 0x06001605 RID: 5637 RVA: 0x000A2724 File Offset: 0x000A0924
	public bool UseItem(Humanoid user, ItemDrop.ItemData item)
	{
		if (!(this.m_keyItem != null) || !(this.m_keyItem.m_itemData.m_shared.m_name == item.m_shared.m_name))
		{
			return false;
		}
		if (!this.CanInteract())
		{
			return false;
		}
		if (this.m_checkGuardStone && !PrivateArea.CheckAccess(base.transform.position, 0f, true, false))
		{
			return true;
		}
		user.Message(MessageHud.MessageType.Center, Localization.instance.Localize("$msg_door_usingkey", new string[]
		{
			this.m_keyItem.m_itemData.m_shared.m_name
		}), 0, null);
		Vector3 normalized = (user.transform.position - base.transform.position).normalized;
		this.Open(normalized);
		return true;
	}

	// Token: 0x06001606 RID: 5638 RVA: 0x000A27FD File Offset: 0x000A09FD
	private bool HaveKey(Humanoid player, bool matchWorldLevel = true)
	{
		return this.m_keyItem == null || player.GetInventory().HaveItem(this.m_keyItem.m_itemData.m_shared.m_name, matchWorldLevel);
	}

	// Token: 0x06001607 RID: 5639 RVA: 0x000A2830 File Offset: 0x000A0A30
	private void RPC_UseDoor(long uid, bool forward)
	{
		if (!this.CanInteract())
		{
			return;
		}
		if (this.m_nview.GetZDO().GetInt(ZDOVars.s_state, 0) == 0)
		{
			if (forward)
			{
				this.m_nview.GetZDO().Set(ZDOVars.s_state, 1, false);
			}
			else
			{
				this.m_nview.GetZDO().Set(ZDOVars.s_state, -1, false);
			}
		}
		else
		{
			this.m_nview.GetZDO().Set(ZDOVars.s_state, 0, false);
		}
		this.UpdateState();
	}

	// Token: 0x040015A7 RID: 5543
	public string m_name = "door";

	// Token: 0x040015A8 RID: 5544
	public ItemDrop m_keyItem;

	// Token: 0x040015A9 RID: 5545
	public bool m_canNotBeClosed;

	// Token: 0x040015AA RID: 5546
	public bool m_invertedOpenClosedText;

	// Token: 0x040015AB RID: 5547
	public bool m_checkGuardStone = true;

	// Token: 0x040015AC RID: 5548
	public GameObject m_openEnable;

	// Token: 0x040015AD RID: 5549
	public EffectList m_openEffects = new EffectList();

	// Token: 0x040015AE RID: 5550
	public EffectList m_closeEffects = new EffectList();

	// Token: 0x040015AF RID: 5551
	public EffectList m_lockedEffects = new EffectList();

	// Token: 0x040015B0 RID: 5552
	private ZNetView m_nview;

	// Token: 0x040015B1 RID: 5553
	private Animator m_animator;

	// Token: 0x040015B2 RID: 5554
	private uint m_lastDataRevision = uint.MaxValue;
}
