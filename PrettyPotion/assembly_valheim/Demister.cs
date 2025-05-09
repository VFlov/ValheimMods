using System;
using System.Collections.Generic;
using UnityEngine;

// Token: 0x02000167 RID: 359
public class Demister : MonoBehaviour
{
	// Token: 0x060015E5 RID: 5605 RVA: 0x000A1985 File Offset: 0x0009FB85
	private void Awake()
	{
		this.m_forceField = base.GetComponent<ParticleSystemForceField>();
		this.m_lastUpdatePosition = base.transform.position;
		if (this.m_disableForcefieldDelay > 0f)
		{
			base.Invoke("DisableForcefield", this.m_disableForcefieldDelay);
		}
	}

	// Token: 0x060015E6 RID: 5606 RVA: 0x000A19C2 File Offset: 0x0009FBC2
	private void OnEnable()
	{
		Demister.m_instances.Add(this);
	}

	// Token: 0x060015E7 RID: 5607 RVA: 0x000A19CF File Offset: 0x0009FBCF
	private void OnDisable()
	{
		Demister.m_instances.Remove(this);
	}

	// Token: 0x060015E8 RID: 5608 RVA: 0x000A19DD File Offset: 0x0009FBDD
	private void DisableForcefield()
	{
		this.m_forceField.enabled = false;
	}

	// Token: 0x060015E9 RID: 5609 RVA: 0x000A19EC File Offset: 0x0009FBEC
	public float GetMovedDistance()
	{
		Vector3 position = base.transform.position;
		if (position == this.m_lastUpdatePosition)
		{
			return 0f;
		}
		float a = Vector3.Distance(position, this.m_lastUpdatePosition);
		this.m_lastUpdatePosition = position;
		return Mathf.Min(a, 10f);
	}

	// Token: 0x060015EA RID: 5610 RVA: 0x000A1A36 File Offset: 0x0009FC36
	public static List<Demister> GetDemisters()
	{
		return Demister.m_instances;
	}

	// Token: 0x04001588 RID: 5512
	public float m_disableForcefieldDelay;

	// Token: 0x04001589 RID: 5513
	[NonSerialized]
	public ParticleSystemForceField m_forceField;

	// Token: 0x0400158A RID: 5514
	private Vector3 m_lastUpdatePosition;

	// Token: 0x0400158B RID: 5515
	private static List<Demister> m_instances = new List<Demister>();
}
