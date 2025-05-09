using System;
using System.IO;
using UnityEngine;

// Token: 0x02000144 RID: 324
public class ServerCtrl
{
	// Token: 0x170000B8 RID: 184
	// (get) Token: 0x060013F9 RID: 5113 RVA: 0x00092F92 File Offset: 0x00091192
	public static ServerCtrl instance
	{
		get
		{
			return ServerCtrl.m_instance;
		}
	}

	// Token: 0x060013FA RID: 5114 RVA: 0x00092F99 File Offset: 0x00091199
	public static void Initialize()
	{
		if (ServerCtrl.m_instance == null)
		{
			ServerCtrl.m_instance = new ServerCtrl();
		}
	}

	// Token: 0x060013FB RID: 5115 RVA: 0x00092FAC File Offset: 0x000911AC
	private ServerCtrl()
	{
		this.ClearExitFile();
	}

	// Token: 0x060013FC RID: 5116 RVA: 0x00092FBA File Offset: 0x000911BA
	public void Update(float dt)
	{
		this.CheckExit(dt);
	}

	// Token: 0x060013FD RID: 5117 RVA: 0x00092FC3 File Offset: 0x000911C3
	private void CheckExit(float dt)
	{
		this.m_checkTimer += dt;
		if (this.m_checkTimer > 2f)
		{
			this.m_checkTimer = 0f;
			if (File.Exists("server_exit.drp"))
			{
				Application.Quit();
			}
		}
	}

	// Token: 0x060013FE RID: 5118 RVA: 0x00092FFC File Offset: 0x000911FC
	private void ClearExitFile()
	{
		try
		{
			File.Delete("server_exit.drp");
		}
		catch
		{
		}
	}

	// Token: 0x040013C7 RID: 5063
	private static ServerCtrl m_instance;

	// Token: 0x040013C8 RID: 5064
	private float m_checkTimer;
}
