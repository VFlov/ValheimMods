using System;
using System.Collections.Generic;
using UnityEngine;

// Token: 0x02000058 RID: 88
[ExecuteInEditMode]
public class LineAttach : MonoBehaviour, IMonoUpdater
{
	// Token: 0x0600062B RID: 1579 RVA: 0x000343EB File Offset: 0x000325EB
	private void Start()
	{
		this.m_lineRenderer = base.GetComponent<LineRenderer>();
	}

	// Token: 0x0600062C RID: 1580 RVA: 0x000343F9 File Offset: 0x000325F9
	private void OnEnable()
	{
		LineAttach.Instances.Add(this);
	}

	// Token: 0x0600062D RID: 1581 RVA: 0x00034406 File Offset: 0x00032606
	private void OnDisable()
	{
		LineAttach.Instances.Remove(this);
	}

	// Token: 0x0600062E RID: 1582 RVA: 0x00034414 File Offset: 0x00032614
	public void CustomLateUpdate(float deltaTime)
	{
		for (int i = 0; i < this.m_attachments.Count; i++)
		{
			Transform transform = this.m_attachments[i];
			if (transform)
			{
				this.m_lineRenderer.SetPosition(i, base.transform.InverseTransformPoint(transform.position));
			}
		}
	}

	// Token: 0x17000014 RID: 20
	// (get) Token: 0x0600062F RID: 1583 RVA: 0x00034469 File Offset: 0x00032669
	public static List<IMonoUpdater> Instances { get; } = new List<IMonoUpdater>();

	// Token: 0x0400072C RID: 1836
	public List<Transform> m_attachments = new List<Transform>();

	// Token: 0x0400072D RID: 1837
	private LineRenderer m_lineRenderer;
}
