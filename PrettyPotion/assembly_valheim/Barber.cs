using System;
using UnityEngine;

// Token: 0x02000159 RID: 345
public class Barber : MonoBehaviour, Hoverable, Interactable
{
	// Token: 0x0600151B RID: 5403 RVA: 0x0009B4F8 File Offset: 0x000996F8
	public string GetHoverText()
	{
		if (Time.time - Barber.m_lastSitTime < 2f)
		{
			return "";
		}
		if (!this.InUseDistance(Player.m_localPlayer))
		{
			return Localization.instance.Localize("<color=#888888>$piece_toofar</color>");
		}
		return Localization.instance.Localize(this.m_name + "\n[<color=yellow><b>$KEY_Use</b></color>] $piece_use");
	}

	// Token: 0x0600151C RID: 5404 RVA: 0x0009B554 File Offset: 0x00099754
	public string GetHoverName()
	{
		return this.m_name;
	}

	// Token: 0x0600151D RID: 5405 RVA: 0x0009B55C File Offset: 0x0009975C
	private void Update()
	{
		if (!PlayerCustomizaton.IsBarberGuiVisible() && Player.m_localPlayer && this.m_attachPoint && Player.m_localPlayer.GetAttachPoint() == this.m_attachPoint && !InventoryGui.IsVisible() && !Minimap.IsOpen() && !Game.IsPaused())
		{
			PlayerCustomizaton.ShowBarberGui();
		}
	}

	// Token: 0x0600151E RID: 5406 RVA: 0x0009B5BC File Offset: 0x000997BC
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
		if (Time.time - Barber.m_lastSitTime < 2f)
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
			player.AttachStart(this.m_attachPoint, null, false, false, false, this.m_attachAnimation, this.m_detachOffset, this.m_cameraPosition);
			PlayerCustomizaton.ShowBarberGui();
			Barber.m_lastSitTime = Time.time;
		}
		return false;
	}

	// Token: 0x0600151F RID: 5407 RVA: 0x0009B678 File Offset: 0x00099878
	public bool UseItem(Humanoid user, ItemDrop.ItemData item)
	{
		return false;
	}

	// Token: 0x06001520 RID: 5408 RVA: 0x0009B67B File Offset: 0x0009987B
	private bool InUseDistance(Humanoid human)
	{
		return Vector3.Distance(human.transform.position, this.m_attachPoint.position) < this.m_useDistance;
	}

	// Token: 0x04001493 RID: 5267
	public string m_name = "Chair";

	// Token: 0x04001494 RID: 5268
	public float m_useDistance = 2f;

	// Token: 0x04001495 RID: 5269
	public Transform m_attachPoint;

	// Token: 0x04001496 RID: 5270
	public Vector3 m_detachOffset = new Vector3(0f, 0.5f, 0f);

	// Token: 0x04001497 RID: 5271
	public string m_attachAnimation = "attach_chair";

	// Token: 0x04001498 RID: 5272
	public Transform m_cameraPosition;

	// Token: 0x04001499 RID: 5273
	private const float m_minSitDelay = 2f;

	// Token: 0x0400149A RID: 5274
	private static float m_lastSitTime;
}
