using System;
using UnityEngine;

// Token: 0x02000197 RID: 407
public class MenuShipMovement : MonoBehaviour
{
	// Token: 0x06001825 RID: 6181 RVA: 0x000B427D File Offset: 0x000B247D
	private void Start()
	{
		this.m_time = (float)UnityEngine.Random.Range(0, 10);
	}

	// Token: 0x06001826 RID: 6182 RVA: 0x000B4290 File Offset: 0x000B2490
	private void Update()
	{
		this.m_time += Time.deltaTime;
		base.transform.rotation = Quaternion.Euler(Mathf.Sin(this.m_time * this.m_freq) * this.m_xAngle, 0f, Mathf.Sin(this.m_time * 1.5341234f * this.m_freq) * this.m_zAngle);
	}

	// Token: 0x04001819 RID: 6169
	public float m_freq = 1f;

	// Token: 0x0400181A RID: 6170
	public float m_xAngle = 5f;

	// Token: 0x0400181B RID: 6171
	public float m_zAngle = 5f;

	// Token: 0x0400181C RID: 6172
	private float m_time;
}
