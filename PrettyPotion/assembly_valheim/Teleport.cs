using System;
using UnityEngine;

// Token: 0x020001C3 RID: 451
public class Teleport : MonoBehaviour, Hoverable, Interactable
{
	// Token: 0x06001A3A RID: 6714 RVA: 0x000C3057 File Offset: 0x000C1257
	public string GetHoverText()
	{
		return Localization.instance.Localize("[<color=yellow><b>$KEY_Use</b></color>] " + this.m_hoverText);
	}

	// Token: 0x06001A3B RID: 6715 RVA: 0x000C3073 File Offset: 0x000C1273
	public string GetHoverName()
	{
		return "";
	}

	// Token: 0x06001A3C RID: 6716 RVA: 0x000C307C File Offset: 0x000C127C
	private void OnTriggerEnter(Collider collider)
	{
		Player component = collider.GetComponent<Player>();
		if (component == null)
		{
			return;
		}
		if (Player.m_localPlayer != component)
		{
			return;
		}
		this.Interact(component, false, false);
	}

	// Token: 0x06001A3D RID: 6717 RVA: 0x000C30B4 File Offset: 0x000C12B4
	public bool Interact(Humanoid character, bool hold, bool alt)
	{
		if (hold)
		{
			return false;
		}
		if (this.m_targetPoint == null)
		{
			return false;
		}
		if (ZoneSystem.instance.GetGlobalKey(GlobalKeys.NoBossPortals) && character.InInterior() && Location.IsInsideActiveBossDungeon(character.transform.position))
		{
			character.Message(MessageHud.MessageType.Center, "$msg_blockedbyboss", 0, null);
			return false;
		}
		if (character.TeleportTo(this.m_targetPoint.GetTeleportPoint(), this.m_targetPoint.transform.rotation, false))
		{
			Game.instance.IncrementPlayerStat(character.InInterior() ? PlayerStatType.PortalDungeonOut : PlayerStatType.PortalDungeonIn, 1f);
			if (this.m_enterText.Length > 0)
			{
				MessageHud.instance.ShowBiomeFoundMsg(this.m_enterText, false);
			}
			return true;
		}
		return false;
	}

	// Token: 0x06001A3E RID: 6718 RVA: 0x000C3171 File Offset: 0x000C1371
	private Vector3 GetTeleportPoint()
	{
		return base.transform.position + base.transform.forward - base.transform.up;
	}

	// Token: 0x06001A3F RID: 6719 RVA: 0x000C319E File Offset: 0x000C139E
	public bool UseItem(Humanoid user, ItemDrop.ItemData item)
	{
		return false;
	}

	// Token: 0x06001A40 RID: 6720 RVA: 0x000C31A1 File Offset: 0x000C13A1
	private void OnDrawGizmos()
	{
	}

	// Token: 0x04001AAB RID: 6827
	public string m_hoverText = "$location_enter";

	// Token: 0x04001AAC RID: 6828
	public string m_enterText = "";

	// Token: 0x04001AAD RID: 6829
	public Teleport m_targetPoint;
}
