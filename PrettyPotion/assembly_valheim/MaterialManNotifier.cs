using System;
using UnityEngine;

// Token: 0x02000128 RID: 296
public class MaterialManNotifier : MonoBehaviour
{
	// Token: 0x060012B3 RID: 4787 RVA: 0x0008B4DB File Offset: 0x000896DB
	private void OnDestroy()
	{
		MaterialMan.instance.UnregisterRenderers(base.gameObject);
	}
}
