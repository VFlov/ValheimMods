using System;
using UnityEngine;

// Token: 0x020001D0 RID: 464
public class Tracker : MonoBehaviour
{
	// Token: 0x06001A9E RID: 6814 RVA: 0x000C5C34 File Offset: 0x000C3E34
	private void Awake()
	{
		ZNetView component = base.GetComponent<ZNetView>();
		if (component && component.IsOwner())
		{
			this.m_active = true;
			ZNet.instance.SetReferencePosition(base.transform.position);
		}
	}

	// Token: 0x06001A9F RID: 6815 RVA: 0x000C5C74 File Offset: 0x000C3E74
	public void SetActive(bool active)
	{
		this.m_active = active;
	}

	// Token: 0x06001AA0 RID: 6816 RVA: 0x000C5C7D File Offset: 0x000C3E7D
	private void OnDestroy()
	{
		this.m_active = false;
	}

	// Token: 0x06001AA1 RID: 6817 RVA: 0x000C5C86 File Offset: 0x000C3E86
	private void FixedUpdate()
	{
		if (this.m_active)
		{
			ZNet.instance.SetReferencePosition(base.transform.position);
		}
	}

	// Token: 0x04001B00 RID: 6912
	private bool m_active;
}
