using System;
using System.Collections.Generic;
using UnityEngine;

// Token: 0x02000146 RID: 326
public class SlowUpdate : MonoBehaviour
{
	// Token: 0x06001401 RID: 5121 RVA: 0x0009304C File Offset: 0x0009124C
	public virtual void Awake()
	{
		SlowUpdate.m_allInstances.Add(this);
		this.m_myIndex = SlowUpdate.m_allInstances.Count - 1;
	}

	// Token: 0x06001402 RID: 5122 RVA: 0x0009306C File Offset: 0x0009126C
	public virtual void OnDestroy()
	{
		if (this.m_myIndex != -1)
		{
			SlowUpdate.m_allInstances[this.m_myIndex] = SlowUpdate.m_allInstances[SlowUpdate.m_allInstances.Count - 1];
			SlowUpdate.m_allInstances[this.m_myIndex].m_myIndex = this.m_myIndex;
			SlowUpdate.m_allInstances.RemoveAt(SlowUpdate.m_allInstances.Count - 1);
		}
	}

	// Token: 0x06001403 RID: 5123 RVA: 0x000930D9 File Offset: 0x000912D9
	public virtual void SUpdate(float time, Vector2i referenceZone)
	{
	}

	// Token: 0x06001404 RID: 5124 RVA: 0x000930DB File Offset: 0x000912DB
	public static List<SlowUpdate> GetAllInstaces()
	{
		return SlowUpdate.m_allInstances;
	}

	// Token: 0x040013CA RID: 5066
	private static List<SlowUpdate> m_allInstances = new List<SlowUpdate>();

	// Token: 0x040013CB RID: 5067
	private int m_myIndex = -1;
}
