using System;
using UnityEngine;

// Token: 0x02000124 RID: 292
public class InstantiatePrefab : MonoBehaviour
{
	// Token: 0x0600129C RID: 4764 RVA: 0x0008B068 File Offset: 0x00089268
	private void Awake()
	{
		if (this.m_attach)
		{
			UnityEngine.Object.Instantiate<GameObject>(this.m_prefab, base.transform).transform.SetAsFirstSibling();
			return;
		}
		UnityEngine.Object.Instantiate<GameObject>(this.m_prefab);
	}

	// Token: 0x04001255 RID: 4693
	public GameObject m_prefab;

	// Token: 0x04001256 RID: 4694
	public bool m_attach = true;

	// Token: 0x04001257 RID: 4695
	public bool m_moveToTop;
}
