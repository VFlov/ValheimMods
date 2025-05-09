using System;
using System.Collections;
using UnityEngine;

// Token: 0x020001A8 RID: 424
public class Radiator : MonoBehaviour
{
	// Token: 0x060018ED RID: 6381 RVA: 0x000BA48B File Offset: 0x000B868B
	private void Start()
	{
		this.m_nview = base.GetComponentInParent<ZNetView>();
	}

	// Token: 0x060018EE RID: 6382 RVA: 0x000BA499 File Offset: 0x000B8699
	private void OnEnable()
	{
		base.StartCoroutine("UpdateLoop");
	}

	// Token: 0x060018EF RID: 6383 RVA: 0x000BA4A7 File Offset: 0x000B86A7
	private IEnumerator UpdateLoop()
	{
		for (;;)
		{
			yield return new WaitForSeconds(UnityEngine.Random.Range(this.m_rateMin, this.m_rateMax));
			if (this.m_nview.IsValid() && this.m_nview.IsOwner())
			{
				Vector3 onUnitSphere = UnityEngine.Random.onUnitSphere;
				Vector3 position = base.transform.position;
				if (onUnitSphere.y < 0f)
				{
					onUnitSphere.y = -onUnitSphere.y;
				}
				if (this.m_emitFrom)
				{
					position = this.m_emitFrom.ClosestPoint(this.m_emitFrom.transform.position + onUnitSphere * 1000f) + onUnitSphere * this.m_offset;
				}
				UnityEngine.Object.Instantiate<GameObject>(this.m_projectile, position, Quaternion.LookRotation(onUnitSphere, Vector3.up)).GetComponent<Projectile>().Setup(null, onUnitSphere * this.m_velocity, 0f, null, null, null);
			}
		}
		yield break;
	}

	// Token: 0x0400191D RID: 6429
	public GameObject m_projectile;

	// Token: 0x0400191E RID: 6430
	public Collider m_emitFrom;

	// Token: 0x0400191F RID: 6431
	public float m_rateMin = 2f;

	// Token: 0x04001920 RID: 6432
	public float m_rateMax = 5f;

	// Token: 0x04001921 RID: 6433
	public float m_velocity = 10f;

	// Token: 0x04001922 RID: 6434
	public float m_offset = 0.1f;

	// Token: 0x04001923 RID: 6435
	private ZNetView m_nview;
}
