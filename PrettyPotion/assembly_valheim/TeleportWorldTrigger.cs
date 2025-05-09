using System;
using UnityEngine;

// Token: 0x020001C6 RID: 454
public class TeleportWorldTrigger : MonoBehaviour
{
	// Token: 0x06001A55 RID: 6741 RVA: 0x000C385E File Offset: 0x000C1A5E
	private void Awake()
	{
		this.m_teleportWorld = base.GetComponentInParent<TeleportWorld>();
	}

	// Token: 0x06001A56 RID: 6742 RVA: 0x000C386C File Offset: 0x000C1A6C
	private void OnTriggerEnter(Collider colliderIn)
	{
		Player component = colliderIn.GetComponent<Player>();
		if (component == null)
		{
			return;
		}
		if (Player.m_localPlayer != component)
		{
			return;
		}
		ZLog.Log("Teleportation TRIGGER");
		this.m_teleportWorld.Teleport(component);
	}

	// Token: 0x04001ABA RID: 6842
	private TeleportWorld m_teleportWorld;
}
