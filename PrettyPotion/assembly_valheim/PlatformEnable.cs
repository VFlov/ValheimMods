using System;
using UnityEngine;

// Token: 0x02000089 RID: 137
public class PlatformEnable : MonoBehaviour
{
	// Token: 0x06000962 RID: 2402 RVA: 0x000525AB File Offset: 0x000507AB
	private void Awake()
	{
		base.gameObject.SetActive(this.m_enabledPlatforms.HasFlag(global::Version.GetPlatform()));
	}

	// Token: 0x04000B0E RID: 2830
	public Platforms m_enabledPlatforms = Platforms.All;
}
