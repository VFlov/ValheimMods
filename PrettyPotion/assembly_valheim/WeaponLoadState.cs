using System;
using UnityEngine;

// Token: 0x020000B7 RID: 183
public class WeaponLoadState : MonoBehaviour
{
	// Token: 0x06000B7B RID: 2939 RVA: 0x00060FA3 File Offset: 0x0005F1A3
	private void Start()
	{
		this.m_owner = base.GetComponentInParent<Player>();
	}

	// Token: 0x06000B7C RID: 2940 RVA: 0x00060FB4 File Offset: 0x0005F1B4
	private void Update()
	{
		if (this.m_owner)
		{
			bool flag = this.m_owner.IsWeaponLoaded();
			this.m_unloaded.SetActive(!flag);
			this.m_loaded.SetActive(flag);
		}
	}

	// Token: 0x04000CC2 RID: 3266
	public GameObject m_unloaded;

	// Token: 0x04000CC3 RID: 3267
	public GameObject m_loaded;

	// Token: 0x04000CC4 RID: 3268
	private Player m_owner;
}
