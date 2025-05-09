using System;
using UnityEngine;
using UnityEngine.Serialization;

// Token: 0x02000160 RID: 352
public class Chair : MonoBehaviour, Hoverable, Interactable
{
	// Token: 0x06001567 RID: 5479 RVA: 0x0009D870 File Offset: 0x0009BA70
	public string GetHoverText()
	{
		if (Time.time - Chair.m_lastSitTime < 2f)
		{
			return "";
		}
		if (!this.InUseDistance(Player.m_localPlayer))
		{
			return Localization.instance.Localize("<color=#888888>$piece_toofar</color>");
		}
		return Localization.instance.Localize(this.m_name + "\n[<color=yellow><b>$KEY_Use</b></color>] $piece_use");
	}

	// Token: 0x06001568 RID: 5480 RVA: 0x0009D8CC File Offset: 0x0009BACC
	public string GetHoverName()
	{
		return this.m_name;
	}

	// Token: 0x06001569 RID: 5481 RVA: 0x0009D8D4 File Offset: 0x0009BAD4
	public bool Interact(Humanoid human, bool hold, bool alt)
	{
		if (hold)
		{
			return false;
		}
		Player player = human as Player;
		if (!this.InUseDistance(player))
		{
			return false;
		}
		if (Time.time - Chair.m_lastSitTime < 2f)
		{
			return false;
		}
		Player closestPlayer = Player.GetClosestPlayer(this.m_attachPoint.position, 0.1f);
		if (closestPlayer != null && closestPlayer != Player.m_localPlayer)
		{
			Player.m_localPlayer.Message(MessageHud.MessageType.Center, "$msg_blocked", 0, null);
			return false;
		}
		if (player)
		{
			if (player.IsEncumbered())
			{
				return false;
			}
			player.AttachStart(this.m_attachPoint, null, false, false, this.m_inShip, this.m_attachAnimation, this.m_detachOffset, null);
			Chair.m_lastSitTime = Time.time;
		}
		return false;
	}

	// Token: 0x0600156A RID: 5482 RVA: 0x0009D98B File Offset: 0x0009BB8B
	public bool UseItem(Humanoid user, ItemDrop.ItemData item)
	{
		return false;
	}

	// Token: 0x0600156B RID: 5483 RVA: 0x0009D98E File Offset: 0x0009BB8E
	private bool InUseDistance(Humanoid human)
	{
		return Vector3.Distance(human.transform.position, this.m_attachPoint.position) < this.m_useDistance;
	}

	// Token: 0x04001507 RID: 5383
	public string m_name = "Chair";

	// Token: 0x04001508 RID: 5384
	public float m_useDistance = 2f;

	// Token: 0x04001509 RID: 5385
	public Transform m_attachPoint;

	// Token: 0x0400150A RID: 5386
	public Vector3 m_detachOffset = new Vector3(0f, 0.5f, 0f);

	// Token: 0x0400150B RID: 5387
	public string m_attachAnimation = "attach_chair";

	// Token: 0x0400150C RID: 5388
	[FormerlySerializedAs("m_onShip")]
	public bool m_inShip;

	// Token: 0x0400150D RID: 5389
	private const float m_minSitDelay = 2f;

	// Token: 0x0400150E RID: 5390
	private static float m_lastSitTime;
}
