using System;
using UnityEngine;

// Token: 0x02000193 RID: 403
public class LodFadeInOut : MonoBehaviour
{
	// Token: 0x0600180D RID: 6157 RVA: 0x000B38D0 File Offset: 0x000B1AD0
	private void Awake()
	{
		Camera mainCamera = Utils.GetMainCamera();
		if (mainCamera == null)
		{
			return;
		}
		if (Vector3.Distance(mainCamera.transform.position, base.transform.position) > 20f)
		{
			this.m_lodGroup = base.GetComponent<LODGroup>();
			if (this.m_lodGroup)
			{
				this.m_originalLocalRef = this.m_lodGroup.localReferencePoint;
				this.m_lodGroup.localReferencePoint = new Vector3(999999f, 999999f, 999999f);
				base.Invoke("FadeIn", UnityEngine.Random.Range(0.1f, 0.3f));
			}
		}
	}

	// Token: 0x0600180E RID: 6158 RVA: 0x000B3972 File Offset: 0x000B1B72
	private void FadeIn()
	{
		this.m_lodGroup.localReferencePoint = this.m_originalLocalRef;
	}

	// Token: 0x040017FB RID: 6139
	private Vector3 m_originalLocalRef;

	// Token: 0x040017FC RID: 6140
	private LODGroup m_lodGroup;

	// Token: 0x040017FD RID: 6141
	private const float m_minTriggerDistance = 20f;
}
