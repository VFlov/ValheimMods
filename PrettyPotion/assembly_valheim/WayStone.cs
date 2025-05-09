using System;
using UnityEngine;

// Token: 0x020001DC RID: 476
public class WayStone : MonoBehaviour, Hoverable, Interactable
{
	// Token: 0x06001B51 RID: 6993 RVA: 0x000CC4FF File Offset: 0x000CA6FF
	private void Awake()
	{
		this.m_activeObject.SetActive(false);
	}

	// Token: 0x06001B52 RID: 6994 RVA: 0x000CC50D File Offset: 0x000CA70D
	public string GetHoverText()
	{
		if (this.m_activeObject.activeSelf)
		{
			return "Activated waystone";
		}
		return Localization.instance.Localize("Waystone\n[<color=yellow><b>$KEY_Use</b></color>] Activate");
	}

	// Token: 0x06001B53 RID: 6995 RVA: 0x000CC531 File Offset: 0x000CA731
	public string GetHoverName()
	{
		return "Waystone";
	}

	// Token: 0x06001B54 RID: 6996 RVA: 0x000CC538 File Offset: 0x000CA738
	public bool Interact(Humanoid character, bool hold, bool alt)
	{
		if (hold)
		{
			return false;
		}
		if (!this.m_activeObject.activeSelf)
		{
			character.Message(MessageHud.MessageType.Center, this.m_activateMessage, 0, null);
			this.m_activeObject.SetActive(true);
			this.m_activeEffect.Create(base.gameObject.transform.position, base.gameObject.transform.rotation, null, 1f, -1);
		}
		return true;
	}

	// Token: 0x06001B55 RID: 6997 RVA: 0x000CC5A6 File Offset: 0x000CA7A6
	public bool UseItem(Humanoid user, ItemDrop.ItemData item)
	{
		return false;
	}

	// Token: 0x06001B56 RID: 6998 RVA: 0x000CC5AC File Offset: 0x000CA7AC
	private void FixedUpdate()
	{
		if (this.m_activeObject.activeSelf && Game.instance != null)
		{
			Vector3 forward = this.GetSpawnPoint() - base.transform.position;
			forward.y = 0f;
			forward.Normalize();
			this.m_activeObject.transform.rotation = Quaternion.LookRotation(forward);
		}
	}

	// Token: 0x06001B57 RID: 6999 RVA: 0x000CC614 File Offset: 0x000CA814
	private Vector3 GetSpawnPoint()
	{
		PlayerProfile playerProfile = Game.instance.GetPlayerProfile();
		if (playerProfile.HaveCustomSpawnPoint())
		{
			return playerProfile.GetCustomSpawnPoint();
		}
		return playerProfile.GetHomePoint();
	}

	// Token: 0x04001C12 RID: 7186
	[TextArea]
	public string m_activateMessage = "You touch the cold stone surface and you think of home.";

	// Token: 0x04001C13 RID: 7187
	public GameObject m_activeObject;

	// Token: 0x04001C14 RID: 7188
	public EffectList m_activeEffect;
}
