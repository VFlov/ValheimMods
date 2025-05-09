using System;
using UnityEngine;

// Token: 0x02000051 RID: 81
public class EmitterRotation : MonoBehaviour
{
	// Token: 0x0600060A RID: 1546 RVA: 0x00033066 File Offset: 0x00031266
	private void Start()
	{
		this.m_lastPos = base.transform.position;
		this.m_ps = base.GetComponentInChildren<ParticleSystem>();
	}

	// Token: 0x0600060B RID: 1547 RVA: 0x00033088 File Offset: 0x00031288
	private void Update()
	{
		if (!this.m_ps.emission.enabled)
		{
			return;
		}
		Vector3 position = base.transform.position;
		Vector3 vector = position - this.m_lastPos;
		this.m_lastPos = position;
		float t = Mathf.Clamp01(vector.magnitude / Time.deltaTime / this.m_maxSpeed);
		if (vector == Vector3.zero)
		{
			vector = Vector3.up;
		}
		Quaternion a = Quaternion.LookRotation(Vector3.up);
		Quaternion b = Quaternion.LookRotation(vector);
		Quaternion to = Quaternion.Lerp(a, b, t);
		base.transform.rotation = Quaternion.RotateTowards(base.transform.rotation, to, Time.deltaTime * this.m_rotSpeed);
	}

	// Token: 0x040006D2 RID: 1746
	public float m_maxSpeed = 10f;

	// Token: 0x040006D3 RID: 1747
	public float m_rotSpeed = 90f;

	// Token: 0x040006D4 RID: 1748
	private Vector3 m_lastPos;

	// Token: 0x040006D5 RID: 1749
	private ParticleSystem m_ps;
}
