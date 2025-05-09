using System;
using UnityEngine;

// Token: 0x02000180 RID: 384
public class FloatingTerrainDummy : MonoBehaviour
{
	// Token: 0x0600171E RID: 5918 RVA: 0x000ABB93 File Offset: 0x000A9D93
	private void OnCollisionStay(Collision collision)
	{
		if (!this.m_parent)
		{
			UnityEngine.Object.Destroy(this);
		}
		this.m_parent.OnDummyCollision(collision);
	}

	// Token: 0x04001717 RID: 5911
	public FloatingTerrain m_parent;
}
