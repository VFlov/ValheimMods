using System;
using UnityEngine;

// Token: 0x02000158 RID: 344
public class AutoJumpLedge : MonoBehaviour
{
	// Token: 0x06001519 RID: 5401 RVA: 0x0009B498 File Offset: 0x00099698
	private void OnTriggerStay(Collider collider)
	{
		Character component = collider.GetComponent<Character>();
		if (component)
		{
			component.OnAutoJump(base.transform.forward, this.m_upVel, this.m_forwardVel);
		}
	}

	// Token: 0x04001490 RID: 5264
	public bool m_forwardOnly = true;

	// Token: 0x04001491 RID: 5265
	public float m_upVel = 1f;

	// Token: 0x04001492 RID: 5266
	public float m_forwardVel = 1f;
}
