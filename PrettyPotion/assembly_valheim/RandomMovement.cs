using System;
using UnityEngine;

// Token: 0x020001AA RID: 426
public class RandomMovement : MonoBehaviour
{
	// Token: 0x060018FC RID: 6396 RVA: 0x000BAE07 File Offset: 0x000B9007
	private void Start()
	{
		this.m_basePosition = base.transform.localPosition;
	}

	// Token: 0x060018FD RID: 6397 RVA: 0x000BAE1C File Offset: 0x000B901C
	private void Update()
	{
		float num = Time.time * this.m_frequency;
		Vector3 b = new Vector3(Mathf.Sin(num) * Mathf.Sin(num * 0.56436f), Mathf.Sin(num * 0.56436f) * Mathf.Sin(num * 0.688742f), Mathf.Cos(num * 0.758348f) * Mathf.Cos(num * 0.4563696f)) * this.m_movement;
		base.transform.localPosition = this.m_basePosition + b;
	}

	// Token: 0x0400194D RID: 6477
	public float m_frequency = 10f;

	// Token: 0x0400194E RID: 6478
	public float m_movement = 0.1f;

	// Token: 0x0400194F RID: 6479
	private Vector3 m_basePosition = Vector3.zero;
}
