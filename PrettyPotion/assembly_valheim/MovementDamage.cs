using System;
using UnityEngine;

// Token: 0x02000025 RID: 37
public class MovementDamage : MonoBehaviour
{
	// Token: 0x060002F5 RID: 757 RVA: 0x0001A594 File Offset: 0x00018794
	private void Awake()
	{
		this.m_character = base.GetComponent<Character>();
		this.m_nview = base.GetComponent<ZNetView>();
		this.m_body = base.GetComponent<Rigidbody>();
		Aoe component = this.m_runDamageObject.GetComponent<Aoe>();
		if (component)
		{
			component.Setup(this.m_character, Vector3.zero, 0f, null, null, null);
		}
	}

	// Token: 0x060002F6 RID: 758 RVA: 0x0001A5F4 File Offset: 0x000187F4
	private void Update()
	{
		if (!this.m_nview.IsValid() || !this.m_nview.IsOwner())
		{
			this.m_runDamageObject.SetActive(false);
			return;
		}
		bool active = this.m_body.velocity.magnitude > this.m_speedTreshold;
		this.m_runDamageObject.SetActive(active);
	}

	// Token: 0x040003F3 RID: 1011
	public GameObject m_runDamageObject;

	// Token: 0x040003F4 RID: 1012
	public float m_speedTreshold = 6f;

	// Token: 0x040003F5 RID: 1013
	private Character m_character;

	// Token: 0x040003F6 RID: 1014
	private ZNetView m_nview;

	// Token: 0x040003F7 RID: 1015
	private Rigidbody m_body;
}
