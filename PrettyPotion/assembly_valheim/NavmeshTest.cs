using System;
using System.Collections.Generic;
using UnityEngine;

// Token: 0x0200012A RID: 298
public class NavmeshTest : MonoBehaviour
{
	// Token: 0x060012D3 RID: 4819 RVA: 0x0008C3DA File Offset: 0x0008A5DA
	private void Awake()
	{
	}

	// Token: 0x060012D4 RID: 4820 RVA: 0x0008C3DC File Offset: 0x0008A5DC
	private void Update()
	{
		if (Pathfinding.instance.GetPath(base.transform.position, this.m_target.position, this.m_path, this.m_agentType, false, this.m_cleanPath, false))
		{
			this.m_havePath = true;
			return;
		}
		this.m_havePath = false;
	}

	// Token: 0x060012D5 RID: 4821 RVA: 0x0008C430 File Offset: 0x0008A630
	private void OnDrawGizmos()
	{
		if (this.m_target == null)
		{
			return;
		}
		if (this.m_havePath)
		{
			Gizmos.color = Color.yellow;
			for (int i = 0; i < this.m_path.Count - 1; i++)
			{
				Vector3 a = this.m_path[i];
				Vector3 a2 = this.m_path[i + 1];
				Gizmos.DrawLine(a + Vector3.up * 0.2f, a2 + Vector3.up * 0.2f);
			}
			foreach (Vector3 a3 in this.m_path)
			{
				Gizmos.DrawSphere(a3 + Vector3.up * 0.2f, 0.1f);
			}
			Gizmos.color = Color.green;
			Gizmos.DrawSphere(base.transform.position, 0.3f);
			Gizmos.DrawSphere(this.m_target.position, 0.3f);
			return;
		}
		Gizmos.color = Color.red;
		Gizmos.DrawLine(base.transform.position + Vector3.up * 0.2f, this.m_target.position + Vector3.up * 0.2f);
		Gizmos.DrawSphere(base.transform.position, 0.3f);
		Gizmos.DrawSphere(this.m_target.position, 0.3f);
	}

	// Token: 0x0400128F RID: 4751
	public Transform m_target;

	// Token: 0x04001290 RID: 4752
	public Pathfinding.AgentType m_agentType = Pathfinding.AgentType.Humanoid;

	// Token: 0x04001291 RID: 4753
	public bool m_cleanPath = true;

	// Token: 0x04001292 RID: 4754
	private List<Vector3> m_path = new List<Vector3>();

	// Token: 0x04001293 RID: 4755
	private bool m_havePath;
}
