using System;
using UnityEngine;

// Token: 0x02000174 RID: 372
public class EnvZone : MonoBehaviour
{
	// Token: 0x06001668 RID: 5736 RVA: 0x000A5D10 File Offset: 0x000A3F10
	private void Awake()
	{
		if (this.m_exteriorMesh)
		{
			this.m_exteriorMesh.forceRenderingOff = true;
		}
	}

	// Token: 0x06001669 RID: 5737 RVA: 0x000A5D2C File Offset: 0x000A3F2C
	private void OnTriggerStay(Collider collider)
	{
		Player component = collider.GetComponent<Player>();
		if (component == null)
		{
			return;
		}
		if (Player.m_localPlayer != component)
		{
			return;
		}
		if (this.m_force && string.IsNullOrEmpty(EnvMan.instance.m_debugEnv))
		{
			EnvMan.instance.SetForceEnvironment(this.m_environment);
		}
		EnvZone.s_triggered = this;
		if (this.m_exteriorMesh)
		{
			this.m_exteriorMesh.forceRenderingOff = false;
		}
	}

	// Token: 0x0600166A RID: 5738 RVA: 0x000A5DA0 File Offset: 0x000A3FA0
	private void OnTriggerExit(Collider collider)
	{
		if (EnvZone.s_triggered != this)
		{
			return;
		}
		Player component = collider.GetComponent<Player>();
		if (component == null)
		{
			return;
		}
		if (Player.m_localPlayer != component)
		{
			return;
		}
		if (this.m_force)
		{
			EnvMan.instance.SetForceEnvironment("");
		}
		EnvZone.s_triggered = null;
	}

	// Token: 0x0600166B RID: 5739 RVA: 0x000A5DF7 File Offset: 0x000A3FF7
	public static string GetEnvironment()
	{
		if (EnvZone.s_triggered && !EnvZone.s_triggered.m_force)
		{
			return EnvZone.s_triggered.m_environment;
		}
		return null;
	}

	// Token: 0x0600166C RID: 5740 RVA: 0x000A5E1D File Offset: 0x000A401D
	private void Update()
	{
		if (this.m_exteriorMesh)
		{
			this.m_exteriorMesh.forceRenderingOff = (EnvZone.s_triggered != this);
		}
	}

	// Token: 0x0400160B RID: 5643
	public string m_environment = "";

	// Token: 0x0400160C RID: 5644
	public bool m_force = true;

	// Token: 0x0400160D RID: 5645
	public MeshRenderer m_exteriorMesh;

	// Token: 0x0400160E RID: 5646
	private static EnvZone s_triggered;
}
