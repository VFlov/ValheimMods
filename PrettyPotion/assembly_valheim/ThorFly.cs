using System;
using UnityEngine;

// Token: 0x020001CD RID: 461
public class ThorFly : MonoBehaviour
{
	// Token: 0x06001A90 RID: 6800 RVA: 0x000C5A5D File Offset: 0x000C3C5D
	private void Start()
	{
	}

	// Token: 0x06001A91 RID: 6801 RVA: 0x000C5A60 File Offset: 0x000C3C60
	private void Update()
	{
		base.transform.position = base.transform.position + base.transform.forward * this.m_speed * Time.deltaTime;
		this.m_timer += Time.deltaTime;
		if (this.m_timer > this.m_ttl)
		{
			UnityEngine.Object.Destroy(base.gameObject);
		}
	}

	// Token: 0x04001AF2 RID: 6898
	public float m_speed = 100f;

	// Token: 0x04001AF3 RID: 6899
	public float m_ttl = 10f;

	// Token: 0x04001AF4 RID: 6900
	private float m_timer;
}
