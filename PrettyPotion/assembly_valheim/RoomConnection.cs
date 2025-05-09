using System;
using UnityEngine;

// Token: 0x02000172 RID: 370
public class RoomConnection : MonoBehaviour
{
	// Token: 0x06001653 RID: 5715 RVA: 0x000A5584 File Offset: 0x000A3784
	private void OnDrawGizmos()
	{
		if (this.m_entrance)
		{
			Gizmos.color = Color.white;
		}
		else
		{
			Gizmos.color = new Color(1f, 1f, 0f, 1f);
		}
		Gizmos.matrix = Matrix4x4.TRS(base.transform.position, base.transform.rotation, new Vector3(1f, 1f, 1f));
		Gizmos.DrawCube(Vector3.zero, new Vector3(2f, 0.02f, 0.2f));
		Gizmos.DrawCube(new Vector3(0f, 0f, 0.35f), new Vector3(0.2f, 0.02f, 0.5f));
		Gizmos.matrix = Matrix4x4.identity;
	}

	// Token: 0x06001654 RID: 5716 RVA: 0x000A564C File Offset: 0x000A384C
	public bool TestContact(RoomConnection other)
	{
		return Vector3.Distance(base.transform.position, other.transform.position) < 0.1f;
	}

	// Token: 0x040015F4 RID: 5620
	public string m_type = "";

	// Token: 0x040015F5 RID: 5621
	public bool m_entrance;

	// Token: 0x040015F6 RID: 5622
	public bool m_allowDoor = true;

	// Token: 0x040015F7 RID: 5623
	public bool m_doorOnlyIfOtherAlsoAllowsDoor;

	// Token: 0x040015F8 RID: 5624
	[NonSerialized]
	public int m_placeOrder;
}
