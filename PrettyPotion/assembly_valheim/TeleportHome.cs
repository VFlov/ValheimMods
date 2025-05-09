using System;
using UnityEngine;

// Token: 0x020001C4 RID: 452
public class TeleportHome : MonoBehaviour
{
	// Token: 0x06001A42 RID: 6722 RVA: 0x000C31C4 File Offset: 0x000C13C4
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
		Game.instance.RequestRespawn(0f, false);
	}
}
