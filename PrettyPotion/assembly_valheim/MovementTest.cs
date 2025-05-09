using System;
using UnityEngine;

// Token: 0x0200019B RID: 411
public class MovementTest : MonoBehaviour
{
	// Token: 0x06001858 RID: 6232 RVA: 0x000B5F28 File Offset: 0x000B4128
	private void Start()
	{
		this.m_body = base.GetComponent<Rigidbody>();
		this.m_center = base.transform.position;
	}

	// Token: 0x06001859 RID: 6233 RVA: 0x000B5F48 File Offset: 0x000B4148
	private void FixedUpdate()
	{
		this.m_timer += Time.fixedDeltaTime;
		float num = 5f;
		Vector3 vector = this.m_center + new Vector3(Mathf.Sin(this.m_timer * this.m_speed) * num, 0f, Mathf.Cos(this.m_timer * this.m_speed) * num);
		this.m_vel = (vector - this.m_body.position) / Time.fixedDeltaTime;
		this.m_body.position = vector;
		this.m_body.velocity = this.m_vel;
	}

	// Token: 0x0400184A RID: 6218
	public float m_speed = 10f;

	// Token: 0x0400184B RID: 6219
	private float m_timer;

	// Token: 0x0400184C RID: 6220
	private Rigidbody m_body;

	// Token: 0x0400184D RID: 6221
	private Vector3 m_center;

	// Token: 0x0400184E RID: 6222
	private Vector3 m_vel;
}
