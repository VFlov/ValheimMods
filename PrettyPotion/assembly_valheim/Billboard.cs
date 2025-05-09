using System;
using UnityEngine;

// Token: 0x0200015D RID: 349
public class Billboard : MonoBehaviour
{
	// Token: 0x06001548 RID: 5448 RVA: 0x0009C247 File Offset: 0x0009A447
	private void Awake()
	{
		this.m_normal = base.transform.up;
	}

	// Token: 0x06001549 RID: 5449 RVA: 0x0009C25C File Offset: 0x0009A45C
	private void LateUpdate()
	{
		Camera mainCamera = Utils.GetMainCamera();
		if (mainCamera == null)
		{
			return;
		}
		Vector3 vector = mainCamera.transform.position;
		if (this.m_invert)
		{
			vector = base.transform.position - (vector - base.transform.position);
		}
		if (this.m_vertical)
		{
			vector.y = base.transform.position.y;
			base.transform.LookAt(vector, this.m_normal);
			return;
		}
		base.transform.LookAt(vector);
	}

	// Token: 0x040014B8 RID: 5304
	public bool m_vertical = true;

	// Token: 0x040014B9 RID: 5305
	public bool m_invert;

	// Token: 0x040014BA RID: 5306
	private Vector3 m_normal;
}
