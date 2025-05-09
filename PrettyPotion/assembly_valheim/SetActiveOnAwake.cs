using System;
using UnityEngine;

// Token: 0x02000145 RID: 325
public class SetActiveOnAwake : MonoBehaviour
{
	// Token: 0x060013FF RID: 5119 RVA: 0x00093028 File Offset: 0x00091228
	private void Awake()
	{
		if (this.m_objectToSetActive != null)
		{
			this.m_objectToSetActive.SetActive(true);
		}
	}

	// Token: 0x040013C9 RID: 5065
	[SerializeField]
	private GameObject m_objectToSetActive;
}
