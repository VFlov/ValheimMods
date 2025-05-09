using System;
using UnityEngine;

// Token: 0x0200015B RID: 347
public class Bed : MonoBehaviour, Hoverable, Interactable
{
	// Token: 0x06001528 RID: 5416 RVA: 0x0009B81B File Offset: 0x00099A1B
	private void Awake()
	{
		this.m_nview = base.GetComponent<ZNetView>();
		if (this.m_nview.GetZDO() == null)
		{
			return;
		}
		this.m_nview.Register<long, string>("SetOwner", new Action<long, long, string>(this.RPC_SetOwner));
	}

	// Token: 0x06001529 RID: 5417 RVA: 0x0009B854 File Offset: 0x00099A54
	public string GetHoverText()
	{
		string ownerName = this.GetOwnerName();
		if (ownerName == "")
		{
			return Localization.instance.Localize("$piece_bed_unclaimed\n[<color=yellow><b>$KEY_Use</b></color>] $piece_bed_claim");
		}
		string text = ownerName + "'s $piece_bed";
		if (!this.IsMine())
		{
			return Localization.instance.Localize(text);
		}
		if (this.IsCurrent())
		{
			return Localization.instance.Localize(text + "\n[<color=yellow><b>$KEY_Use</b></color>] $piece_bed_sleep");
		}
		return Localization.instance.Localize(text + "\n[<color=yellow><b>$KEY_Use</b></color>] $piece_bed_setspawn");
	}

	// Token: 0x0600152A RID: 5418 RVA: 0x0009B8D8 File Offset: 0x00099AD8
	public string GetHoverName()
	{
		return Localization.instance.Localize("$piece_bed");
	}

	// Token: 0x0600152B RID: 5419 RVA: 0x0009B8EC File Offset: 0x00099AEC
	public bool Interact(Humanoid human, bool repeat, bool alt)
	{
		if (repeat)
		{
			return false;
		}
		if (this.m_nview.GetZDO() == null)
		{
			return false;
		}
		long playerID = Game.instance.GetPlayerProfile().GetPlayerID();
		bool owner = this.GetOwner() != 0L;
		Player human2 = human as Player;
		if (!owner)
		{
			ZLog.Log("Has no creator");
			if (!this.CheckExposure(human2))
			{
				return false;
			}
			this.SetOwner(playerID, Game.instance.GetPlayerProfile().GetName());
			Game.instance.GetPlayerProfile().SetCustomSpawnPoint(this.GetSpawnPoint());
			human.Message(MessageHud.MessageType.Center, "$msg_spawnpointset", 0, null);
		}
		else if (this.IsMine())
		{
			ZLog.Log("Is mine");
			if (this.IsCurrent())
			{
				ZLog.Log("is current spawnpoint");
				if (!EnvMan.CanSleep())
				{
					human.Message(MessageHud.MessageType.Center, "$msg_cantsleep", 0, null);
					return false;
				}
				if (!this.CheckEnemies(human2))
				{
					return false;
				}
				if (!this.CheckExposure(human2))
				{
					return false;
				}
				if (!this.CheckFire(human2))
				{
					return false;
				}
				if (!this.CheckWet(human2))
				{
					return false;
				}
				human.AttachStart(this.m_spawnPoint, base.gameObject, true, true, false, "attach_bed", new Vector3(0f, 0.5f, 0f), null);
				return false;
			}
			else
			{
				ZLog.Log("Not current spawn point");
				if (!this.CheckExposure(human2))
				{
					return false;
				}
				Game.instance.GetPlayerProfile().SetCustomSpawnPoint(this.GetSpawnPoint());
				human.Message(MessageHud.MessageType.Center, "$msg_spawnpointset", 0, null);
			}
		}
		return false;
	}

	// Token: 0x0600152C RID: 5420 RVA: 0x0009BA53 File Offset: 0x00099C53
	private bool CheckWet(Player human)
	{
		if (human.GetSEMan().HaveStatusEffect(SEMan.s_statusEffectWet))
		{
			human.Message(MessageHud.MessageType.Center, "$msg_bedwet", 0, null);
			return false;
		}
		return true;
	}

	// Token: 0x0600152D RID: 5421 RVA: 0x0009BA78 File Offset: 0x00099C78
	private bool CheckEnemies(Player human)
	{
		if (human.IsSensed())
		{
			human.Message(MessageHud.MessageType.Center, "$msg_bedenemiesnearby", 0, null);
			return false;
		}
		return true;
	}

	// Token: 0x0600152E RID: 5422 RVA: 0x0009BA94 File Offset: 0x00099C94
	private bool CheckExposure(Player human)
	{
		float num;
		bool flag;
		Cover.GetCoverForPoint(this.GetSpawnPoint(), out num, out flag, 0.5f);
		if (!flag)
		{
			human.Message(MessageHud.MessageType.Center, "$msg_bedneedroof", 0, null);
			return false;
		}
		if (num < 0.8f)
		{
			human.Message(MessageHud.MessageType.Center, "$msg_bedtooexposed", 0, null);
			return false;
		}
		ZLog.Log("exporeusre check " + num.ToString() + "  " + flag.ToString());
		return true;
	}

	// Token: 0x0600152F RID: 5423 RVA: 0x0009BB03 File Offset: 0x00099D03
	private bool CheckFire(Player human)
	{
		if (!EffectArea.IsPointInsideArea(base.transform.position, EffectArea.Type.Heat, 0f))
		{
			human.Message(MessageHud.MessageType.Center, "$msg_bednofire", 0, null);
			return false;
		}
		return true;
	}

	// Token: 0x06001530 RID: 5424 RVA: 0x0009BB33 File Offset: 0x00099D33
	public bool UseItem(Humanoid user, ItemDrop.ItemData item)
	{
		return false;
	}

	// Token: 0x06001531 RID: 5425 RVA: 0x0009BB36 File Offset: 0x00099D36
	public bool IsCurrent()
	{
		return this.IsMine() && Vector3.Distance(this.GetSpawnPoint(), Game.instance.GetPlayerProfile().GetCustomSpawnPoint()) < 1f;
	}

	// Token: 0x06001532 RID: 5426 RVA: 0x0009BB63 File Offset: 0x00099D63
	public Vector3 GetSpawnPoint()
	{
		return this.m_spawnPoint.position;
	}

	// Token: 0x06001533 RID: 5427 RVA: 0x0009BB70 File Offset: 0x00099D70
	private bool IsMine()
	{
		long playerID = Game.instance.GetPlayerProfile().GetPlayerID();
		long owner = this.GetOwner();
		return playerID == owner;
	}

	// Token: 0x06001534 RID: 5428 RVA: 0x0009BB96 File Offset: 0x00099D96
	private void SetOwner(long uid, string name)
	{
		this.m_nview.InvokeRPC("SetOwner", new object[]
		{
			uid,
			name
		});
	}

	// Token: 0x06001535 RID: 5429 RVA: 0x0009BBBB File Offset: 0x00099DBB
	private void RPC_SetOwner(long sender, long uid, string name)
	{
		if (!this.m_nview.IsOwner())
		{
			return;
		}
		this.m_nview.GetZDO().Set(ZDOVars.s_owner, uid);
		this.m_nview.GetZDO().Set(ZDOVars.s_ownerName, name);
	}

	// Token: 0x06001536 RID: 5430 RVA: 0x0009BBF7 File Offset: 0x00099DF7
	private long GetOwner()
	{
		if (this.m_nview.GetZDO() == null)
		{
			return 0L;
		}
		return this.m_nview.GetZDO().GetLong(ZDOVars.s_owner, 0L);
	}

	// Token: 0x06001537 RID: 5431 RVA: 0x0009BC20 File Offset: 0x00099E20
	private string GetOwnerName()
	{
		if (this.m_nview.GetZDO() == null)
		{
			return "";
		}
		if (!this.IsMine())
		{
			return CensorShittyWords.FilterUGC(this.m_nview.GetZDO().GetString(ZDOVars.s_ownerName, ""), UGCType.CharacterName, this.GetOwner());
		}
		return this.m_nview.GetZDO().GetString(ZDOVars.s_ownerName, "");
	}

	// Token: 0x0400149D RID: 5277
	public Transform m_spawnPoint;

	// Token: 0x0400149E RID: 5278
	public float m_monsterCheckRadius = 20f;

	// Token: 0x0400149F RID: 5279
	private ZNetView m_nview;
}
