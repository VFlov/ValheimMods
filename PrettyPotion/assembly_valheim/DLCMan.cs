using System;
using System.Collections.Generic;
using Steamworks;
using UnityEngine;

// Token: 0x02000113 RID: 275
public class DLCMan : MonoBehaviour
{
	// Token: 0x17000099 RID: 153
	// (get) Token: 0x0600113E RID: 4414 RVA: 0x00080857 File Offset: 0x0007EA57
	public static DLCMan instance
	{
		get
		{
			return DLCMan.m_instance;
		}
	}

	// Token: 0x0600113F RID: 4415 RVA: 0x0008085E File Offset: 0x0007EA5E
	private void Awake()
	{
		DLCMan.m_instance = this;
		this.CheckDLCsSTEAM();
	}

	// Token: 0x06001140 RID: 4416 RVA: 0x0008086C File Offset: 0x0007EA6C
	private void OnDestroy()
	{
		if (DLCMan.m_instance == this)
		{
			DLCMan.m_instance = null;
		}
	}

	// Token: 0x06001141 RID: 4417 RVA: 0x00080884 File Offset: 0x0007EA84
	public bool IsDLCInstalled(string name)
	{
		if (name.Length == 0)
		{
			return true;
		}
		foreach (DLCMan.DLCInfo dlcinfo in this.m_dlcs)
		{
			if (dlcinfo.m_name == name)
			{
				return dlcinfo.m_installed;
			}
		}
		ZLog.LogWarning("DLC " + name + " not registered in DLCMan");
		return false;
	}

	// Token: 0x06001142 RID: 4418 RVA: 0x0008090C File Offset: 0x0007EB0C
	private void CheckDLCsSTEAM()
	{
		if (!SteamManager.Initialized)
		{
			ZLog.Log("Steam not initialized");
			return;
		}
		ZLog.Log("Checking for installed DLCs");
		foreach (DLCMan.DLCInfo dlcinfo in this.m_dlcs)
		{
			dlcinfo.m_installed = this.IsDLCInstalled(dlcinfo);
			ZLog.Log("DLC:" + dlcinfo.m_name + " installed:" + dlcinfo.m_installed.ToString());
		}
	}

	// Token: 0x06001143 RID: 4419 RVA: 0x000809A8 File Offset: 0x0007EBA8
	private bool IsDLCInstalled(DLCMan.DLCInfo dlc)
	{
		foreach (uint id in dlc.m_steamAPPID)
		{
			if (this.IsDLCInstalled(id))
			{
				return true;
			}
		}
		return false;
	}

	// Token: 0x06001144 RID: 4420 RVA: 0x000809DC File Offset: 0x0007EBDC
	private bool IsDLCInstalled(uint id)
	{
		AppId_t x = new AppId_t(id);
		int dlccount = SteamApps.GetDLCCount();
		for (int i = 0; i < dlccount; i++)
		{
			AppId_t appId_t;
			bool flag;
			string text;
			if (SteamApps.BGetDLCDataByIndex(i, out appId_t, out flag, out text, 200) && x == appId_t)
			{
				ZLog.Log("DLC installed:" + id.ToString());
				return SteamApps.BIsDlcInstalled(appId_t);
			}
		}
		return false;
	}

	// Token: 0x0400106A RID: 4202
	private static DLCMan m_instance;

	// Token: 0x0400106B RID: 4203
	public List<DLCMan.DLCInfo> m_dlcs = new List<DLCMan.DLCInfo>();

	// Token: 0x0200030F RID: 783
	[Serializable]
	public class DLCInfo
	{
		// Token: 0x040023BD RID: 9149
		public string m_name = "DLC";

		// Token: 0x040023BE RID: 9150
		public uint[] m_steamAPPID = new uint[0];

		// Token: 0x040023BF RID: 9151
		[NonSerialized]
		public bool m_installed;
	}
}
