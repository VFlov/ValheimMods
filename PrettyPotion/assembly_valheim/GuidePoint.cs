using System;
using UnityEngine;

// Token: 0x02000181 RID: 385
public class GuidePoint : MonoBehaviour
{
	// Token: 0x06001720 RID: 5920 RVA: 0x000ABBBC File Offset: 0x000A9DBC
	private void Start()
	{
		if (!Raven.IsInstantiated())
		{
			UnityEngine.Object.Instantiate<GameObject>(this.m_ravenPrefab, new Vector3(0f, 0f, 0f), Quaternion.identity);
		}
		this.m_text.m_static = true;
		this.m_text.m_guidePoint = this;
		Raven.RegisterStaticText(this.m_text);
	}

	// Token: 0x06001721 RID: 5921 RVA: 0x000ABC18 File Offset: 0x000A9E18
	private void OnDestroy()
	{
		Raven.UnregisterStaticText(this.m_text);
	}

	// Token: 0x06001722 RID: 5922 RVA: 0x000ABC25 File Offset: 0x000A9E25
	private void OnDrawGizmos()
	{
	}

	// Token: 0x04001718 RID: 5912
	public Raven.RavenText m_text = new Raven.RavenText();

	// Token: 0x04001719 RID: 5913
	public GameObject m_ravenPrefab;
}
