using System;
using UnityEngine;

// Token: 0x02000175 RID: 373
public class EventZone : MonoBehaviour
{
	// Token: 0x0600166E RID: 5742 RVA: 0x000A5E5C File Offset: 0x000A405C
	private void OnTriggerStay(Collider collider)
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
		EventZone.m_triggered = this;
	}

	// Token: 0x0600166F RID: 5743 RVA: 0x000A5E90 File Offset: 0x000A4090
	private void OnTriggerExit(Collider collider)
	{
		if (EventZone.m_triggered != this)
		{
			return;
		}
		Player component = collider.GetComponent<Player>();
		if (component == null)
		{
			return;
		}
		if (Player.m_localPlayer != component)
		{
			return;
		}
		EventZone.m_triggered = null;
	}

	// Token: 0x06001670 RID: 5744 RVA: 0x000A5ED0 File Offset: 0x000A40D0
	public static string GetEvent()
	{
		if (EventZone.m_triggered && EventZone.m_triggered.m_event.Length > 0)
		{
			return EventZone.m_triggered.m_event;
		}
		return null;
	}

	// Token: 0x0400160F RID: 5647
	public string m_event = "";

	// Token: 0x04001610 RID: 5648
	private static EventZone m_triggered;
}
