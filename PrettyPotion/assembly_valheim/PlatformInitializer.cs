using System;
using System.Threading;
using Splatform;
using UnityEngine;

// Token: 0x020000F2 RID: 242
public static class PlatformInitializer
{
	// Token: 0x17000084 RID: 132
	// (get) Token: 0x06000FD3 RID: 4051 RVA: 0x00076690 File Offset: 0x00074890
	public static bool PlatformInitialized
	{
		get
		{
			return PlatformInitializer.s_platformInitialized;
		}
	}

	// Token: 0x17000085 RID: 133
	// (get) Token: 0x06000FD4 RID: 4052 RVA: 0x00076697 File Offset: 0x00074897
	public static bool StartedSaveDataInitialization
	{
		get
		{
			return PlatformInitializer.s_startedStorageInitialization;
		}
	}

	// Token: 0x17000086 RID: 134
	// (get) Token: 0x06000FD5 RID: 4053 RVA: 0x0007669E File Offset: 0x0007489E
	public static bool SaveDataInitialized
	{
		get
		{
			return PlatformManager.DistributionPlatform.SaveDataProvider == null || !PlatformManager.DistributionPlatform.SaveDataProvider.IsEnabled || PlatformManager.DistributionPlatform.SaveDataProvider.IsInitialized;
		}
	}

	// Token: 0x17000087 RID: 135
	// (get) Token: 0x06000FD6 RID: 4054 RVA: 0x000766CE File Offset: 0x000748CE
	// (set) Token: 0x06000FD7 RID: 4055 RVA: 0x000766D5 File Offset: 0x000748D5
	public static bool AllowSaveDataInitialization
	{
		get
		{
			return PlatformInitializer.s_allowStorageInitialization;
		}
		set
		{
			PlatformInitializer.s_allowStorageInitialization = value;
			if (PlatformInitializer.s_allowStorageInitialization && !PlatformInitializer.s_startedStorageInitialization)
			{
				PlatformInitializer.InitializeSaveDataStorage();
			}
		}
	}

	// Token: 0x17000088 RID: 136
	// (get) Token: 0x06000FD8 RID: 4056 RVA: 0x000766F0 File Offset: 0x000748F0
	// (set) Token: 0x06000FD9 RID: 4057 RVA: 0x000766F7 File Offset: 0x000748F7
	public static bool InputDeviceRequired
	{
		get
		{
			return PlatformInitializer.s_inputDeviceRequired;
		}
		set
		{
			PlatformInitializer.s_inputDeviceRequired = value;
			if (PlatformManager.DistributionPlatform == null)
			{
				return;
			}
			if (PlatformManager.DistributionPlatform.InputDeviceManager == null)
			{
				return;
			}
			PlatformManager.DistributionPlatform.InputDeviceManager.SetInputDeviceRequiredForLocalUser(false, new CheckKeyboardMouseConnectedFunc(ZInput.CheckKeyboardMouseConnected), null, null);
		}
	}

	// Token: 0x17000089 RID: 137
	// (get) Token: 0x06000FDA RID: 4058 RVA: 0x00076732 File Offset: 0x00074932
	public static bool WaitingForInputDevice
	{
		get
		{
			return PlatformManager.DistributionPlatform != null && PlatformManager.DistributionPlatform.InputDeviceManager != null && !PlatformManager.DistributionPlatform.InputDeviceManager.HasInputDeviceAssociation(PlatformManager.DistributionPlatform.LocalUser.PlatformUserID);
		}
	}

	// Token: 0x06000FDB RID: 4059 RVA: 0x0007676C File Offset: 0x0007496C
	[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
	private static void InitializePlatform()
	{
		PlatformInitializer.SetMainThreadName();
		PlatformInitializer.ParseArguments();
		PlatformConfiguration platformConfiguration = default(PlatformConfiguration);
		SteamManager.Initialize();
		platformConfiguration.SetBool("managesteamruntime", false);
		platformConfiguration.SetUIntArray("acceptedappids", new uint[]
		{
			1223920U,
			892970U
		});
		Splatform.Logger.SetLogHandler(new LogHandler(PlatformInitializer.OnSplatformLog));
		PlatformManager.InitializeAsync(platformConfiguration, new InitializeCompletedHandler(PlatformInitializer.OnInitializeCompleted));
	}

	// Token: 0x06000FDC RID: 4060 RVA: 0x000767E3 File Offset: 0x000749E3
	private static void SetMainThreadName()
	{
		if (string.IsNullOrEmpty(Thread.CurrentThread.Name))
		{
			Thread.CurrentThread.Name = "MainValheimThread";
		}
	}

	// Token: 0x06000FDD RID: 4061 RVA: 0x00076808 File Offset: 0x00074A08
	private static void ParseArguments()
	{
		foreach (string text in Environment.GetCommandLineArgs())
		{
		}
	}

	// Token: 0x06000FDE RID: 4062 RVA: 0x00076830 File Offset: 0x00074A30
	private static void OnInitializeCompleted(bool succeeded)
	{
		if (!succeeded)
		{
			ZLog.LogError("Failed to initialize platform!");
			Application.Quit();
			return;
		}
		SuspendManager.Initialize();
		PlatformInitializer.s_platformInitialized = true;
		ZLog.Log("Initialized platform!");
		PlatformManager.DistributionPlatform.LocalUser.SignedIn += PlatformInitializer.OnLoginCompleted;
	}

	// Token: 0x06000FDF RID: 4063 RVA: 0x00076880 File Offset: 0x00074A80
	private static void OnLoginCompleted()
	{
		PlatformManager.DistributionPlatform.LocalUser.SignedIn -= PlatformInitializer.OnLoginCompleted;
		MatchmakingManager.Initialize();
		if (PlatformInitializer.s_allowStorageInitialization)
		{
			PlatformInitializer.InitializeSaveDataStorage();
		}
	}

	// Token: 0x06000FE0 RID: 4064 RVA: 0x000768B0 File Offset: 0x00074AB0
	private static void InitializeSaveDataStorage()
	{
		PlatformInitializer.s_startedStorageInitialization = true;
		if (PlatformManager.DistributionPlatform.SaveDataProvider == null)
		{
			return;
		}
		PlatformManager.DistributionPlatform.SaveDataProvider.InitializeAsync(delegate(bool succeeded)
		{
			if (succeeded)
			{
				if (FileHelpers.LocalStorageSupported)
				{
					string[] files = FileHelpers.GetFiles(FileHelpers.FileSource.Local, Utils.GetSaveDataPath(FileHelpers.FileSource.Local), null, null);
					string text = "All files in local storage save data:";
					for (int i = 0; i < files.Length; i++)
					{
						text += string.Format("\n{0} ({1})", files[i], FileHelpers.GetFileSize(files[i], FileHelpers.FileSource.Local));
					}
					ZLog.Log(text);
				}
				else
				{
					ZLog.Log("Local storage is not supported");
				}
				if (FileHelpers.CloudStorageSupported && FileHelpers.CloudStorageEnabled)
				{
					string[] files = FileHelpers.GetFiles(FileHelpers.FileSource.Cloud, Utils.GetSaveDataPath(FileHelpers.FileSource.Cloud), null, null);
					string text = "All files in platform save data:";
					for (int j = 0; j < files.Length; j++)
					{
						text += string.Format("\n{0} ({1})", files[j], FileHelpers.GetFileSize(files[j], FileHelpers.FileSource.Cloud));
					}
					ZLog.Log(text);
					return;
				}
				ZLog.Log("Cloud storage is not supported or enabled");
			}
		});
	}

	// Token: 0x06000FE1 RID: 4065 RVA: 0x00076900 File Offset: 0x00074B00
	private static void OnSplatformLog(LogType logType, object message)
	{
		switch (logType)
		{
		case LogType.Error:
			ZLog.LogError(message);
			return;
		case LogType.Warning:
			ZLog.LogWarning(message);
			return;
		case LogType.Log:
			ZLog.Log(message);
			return;
		}
		ZLog.LogError(string.Format("Log type {0} not implemented! Log message:\n{1}", logType, message));
	}

	// Token: 0x04000F42 RID: 3906
	private static bool s_platformInitialized = false;

	// Token: 0x04000F43 RID: 3907
	private static bool s_loginFinished = false;

	// Token: 0x04000F44 RID: 3908
	private static bool s_startedStorageInitialization = false;

	// Token: 0x04000F45 RID: 3909
	private static bool s_allowStorageInitialization = true;

	// Token: 0x04000F46 RID: 3910
	private static bool s_inputDeviceRequired = false;
}
