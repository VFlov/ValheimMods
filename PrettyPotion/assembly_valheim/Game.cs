using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Threading;
using SoftReferenceableAssets.SceneManagement;
using TMPro;
using UnityEngine;
using Valheim.SettingsGui;

// Token: 0x0200011A RID: 282
public class Game : MonoBehaviour
{
	// Token: 0x1700009D RID: 157
	// (get) Token: 0x06001217 RID: 4631 RVA: 0x00086D09 File Offset: 0x00084F09
	// (set) Token: 0x06001218 RID: 4632 RVA: 0x00086D10 File Offset: 0x00084F10
	public static Game instance { get; private set; }

	// Token: 0x06001219 RID: 4633 RVA: 0x00086D18 File Offset: 0x00084F18
	private void Awake()
	{
		Game.instance = this;
		foreach (GameObject gameObject in this.m_portalPrefabs)
		{
			this.PortalPrefabHash.Add(gameObject.name.GetStableHashCode());
		}
		if (!FejdStartup.AwakePlatforms())
		{
			return;
		}
		Settings.SetPlatformDefaultPrefs();
		GameplaySettings.SetControllerSpecificFirstTimeSettings();
		ZInput.Initialize();
		ZInput.WorkaroundEnabled = true;
		if (!global::Console.instance)
		{
			UnityEngine.Object.Instantiate<GameObject>(this.m_consolePrefab);
		}
		if (!ServerOptionsGUI.m_instance)
		{
			UnityEngine.Object.Instantiate<GameObject>(this.m_serverOptionPrefab);
		}
		Settings.ApplyStartupSettings();
		if (string.IsNullOrEmpty(Game.m_profileFilename))
		{
			this.m_playerProfile = new PlayerProfile(this.m_devProfileName, FileHelpers.FileSource.Local);
			this.m_playerProfile.SetName(this.m_devPlayerName);
			this.m_playerProfile.Load();
		}
		else
		{
			ZLog.Log("Loading player profile " + Game.m_profileFilename);
			this.m_playerProfile = new PlayerProfile(Game.m_profileFilename, Game.m_profileFileSource);
			this.m_playerProfile.Load();
		}
		base.InvokeRepeating("CollectResourcesCheckPeriodic", 3600f, 3600f);
		Gogan.LogEvent("Screen", "Enter", "InGame", 0L);
		Gogan.LogEvent("Game", "InputMode", ZInput.IsGamepadActive() ? "Gamepad" : "MK", 0L);
		ZLog.Log("isModded: " + Game.isModded.ToString());
	}

	// Token: 0x0600121A RID: 4634 RVA: 0x00086EAC File Offset: 0x000850AC
	private void OnDestroy()
	{
		Game.instance = null;
	}

	// Token: 0x0600121B RID: 4635 RVA: 0x00086EB4 File Offset: 0x000850B4
	private void Start()
	{
		Application.targetFrameRate = ((Settings.FPSLimit == 29 || Settings.FPSLimit > 360) ? -1 : Settings.FPSLimit);
		ZRoutedRpc.instance.Register("SleepStart", new Action<long>(this.SleepStart));
		ZRoutedRpc.instance.Register("SleepStop", new Action<long>(this.SleepStop));
		ZRoutedRpc.instance.Register<float>("Ping", new Action<long, float>(this.RPC_Ping));
		ZRoutedRpc.instance.Register<float>("Pong", new Action<long, float>(this.RPC_Pong));
		ZRoutedRpc.instance.Register<ZDOID, ZDOID>("RPC_SetConnection", new Action<long, ZDOID, ZDOID>(this.RPC_SetConnection));
		ZRoutedRpc.instance.Register<string, int, Vector3, bool>("RPC_DiscoverLocationResponse", new RoutedMethod<string, int, Vector3, bool>.Method(this.RPC_DiscoverLocationResponse));
		if (ZNet.instance.IsServer())
		{
			ZRoutedRpc.instance.Register<string, Vector3, string, int, bool, bool>("RPC_DiscoverClosestLocation", new RoutedMethod<string, Vector3, string, int, bool, bool>.Method(this.RPC_DiscoverClosestLocation));
			base.InvokeRepeating("UpdateSleeping", 2f, 2f);
			base.StartCoroutine("ConnectPortalsCoroutine");
		}
		if (!ZNet.instance.IsDedicated() && this.m_playerProfile.m_firstSpawn)
		{
			this.m_queuedIntro = true;
		}
	}

	// Token: 0x0600121C RID: 4636 RVA: 0x00086FED File Offset: 0x000851ED
	private void ShowIntro()
	{
		this.m_inIntro = true;
		TextViewer.instance.ShowText(TextViewer.Style.Intro, this.m_introTopic, this.m_introText, false);
	}

	// Token: 0x0600121D RID: 4637 RVA: 0x00087010 File Offset: 0x00085210
	private void ServerLog()
	{
		int peerConnections = ZNet.instance.GetPeerConnections();
		int num = ZDOMan.instance.NrOfObjects();
		int sentZDOs = ZDOMan.instance.GetSentZDOs();
		int recvZDOs = ZDOMan.instance.GetRecvZDOs();
		ZLog.Log(string.Concat(new string[]
		{
			" Connections ",
			peerConnections.ToString(),
			" ZDOS:",
			num.ToString(),
			"  sent:",
			sentZDOs.ToString(),
			" recv:",
			recvZDOs.ToString()
		}));
	}

	// Token: 0x0600121E RID: 4638 RVA: 0x000870A1 File Offset: 0x000852A1
	public void CollectResources(bool displayMessage = false)
	{
		if (displayMessage && Player.m_localPlayer)
		{
			Player.m_localPlayer.Message(MessageHud.MessageType.TopLeft, "Unloading unused assets", 0, null);
		}
		ZLog.Log("Unloading unused assets");
		Resources.UnloadUnusedAssets();
		this.m_lastCollectResources = DateTime.Now;
	}

	// Token: 0x0600121F RID: 4639 RVA: 0x000870DF File Offset: 0x000852DF
	public void CollectResourcesCheckPeriodic()
	{
		if (DateTime.Now - TimeSpan.FromSeconds(3599.0) > this.m_lastCollectResources)
		{
			this.CollectResources(true);
			return;
		}
		ZLog.Log("Skipping unloading unused assets");
	}

	// Token: 0x06001220 RID: 4640 RVA: 0x00087118 File Offset: 0x00085318
	public void CollectResourcesCheck()
	{
		if (DateTime.Now - TimeSpan.FromSeconds(1200.0) > this.m_lastCollectResources)
		{
			this.CollectResources(true);
			return;
		}
		ZLog.Log("Skipping unloading unused assets");
	}

	// Token: 0x06001221 RID: 4641 RVA: 0x00087154 File Offset: 0x00085354
	public void Logout(bool save = true, bool changeToStartScene = true)
	{
		if (this.m_shuttingDown)
		{
			return;
		}
		bool shouldExit = false;
		bool flag;
		save = ZNet.instance.EnoughDiskSpaceAvailable(out flag, true, delegate(bool exit)
		{
			shouldExit = exit;
			this.ContinueLogout(save, shouldExit, changeToStartScene);
		});
		if (!flag)
		{
			this.ContinueLogout(save, shouldExit, changeToStartScene);
		}
	}

	// Token: 0x06001222 RID: 4642 RVA: 0x000871C6 File Offset: 0x000853C6
	private void ContinueLogout(bool save, bool shouldExit, bool changeToStartScene)
	{
		if (!save && !shouldExit)
		{
			return;
		}
		this.Shutdown(save);
		if (changeToStartScene)
		{
			SceneManager.LoadScene(this.m_startScene, LoadSceneMode.Single);
		}
	}

	// Token: 0x06001223 RID: 4643 RVA: 0x000871E5 File Offset: 0x000853E5
	public bool IsShuttingDown()
	{
		return this.m_shuttingDown;
	}

	// Token: 0x06001224 RID: 4644 RVA: 0x000871F0 File Offset: 0x000853F0
	private void OnApplicationQuit()
	{
		if (this.m_shuttingDown)
		{
			return;
		}
		ZLog.Log("Game - OnApplicationQuit");
		bool flag;
		bool saveWorld = ZNet.instance.EnoughDiskSpaceAvailable(out flag, false, null);
		this.Shutdown(saveWorld);
		HeightmapBuilder.instance.Dispose();
		Thread.Sleep(2000);
	}

	// Token: 0x06001225 RID: 4645 RVA: 0x0008723A File Offset: 0x0008543A
	private void Shutdown(bool saveWorld = true)
	{
		if (this.m_shuttingDown)
		{
			return;
		}
		ZLog.Log("Shutting down");
		this.m_shuttingDown = true;
		if (saveWorld)
		{
			this.SavePlayerProfile(true);
		}
		ZNetScene.instance.Shutdown();
		ZNet.instance.Shutdown(saveWorld);
	}

	// Token: 0x06001226 RID: 4646 RVA: 0x00087278 File Offset: 0x00085478
	public void SavePlayerProfile(bool setLogoutPoint)
	{
		this.m_saveTimer = 0f;
		if (Player.m_localPlayer)
		{
			this.m_playerProfile.SavePlayerData(Player.m_localPlayer);
			Minimap.instance.SaveMapData();
			if (setLogoutPoint)
			{
				this.m_playerProfile.SaveLogoutPoint();
			}
		}
		if (this.m_playerProfile.m_fileSource == FileHelpers.FileSource.Cloud)
		{
			ulong num = 1048576UL;
			if (FileHelpers.FileExistsCloud(this.m_playerProfile.GetPath()))
			{
				num += FileHelpers.GetFileSize(this.m_playerProfile.GetPath(), FileHelpers.FileSource.Cloud);
			}
			num *= 3UL;
			if (FileHelpers.OperationExceedsCloudCapacity(num))
			{
				string path = this.m_playerProfile.GetPath();
				this.m_playerProfile.m_fileSource = FileHelpers.FileSource.Local;
				string path2 = this.m_playerProfile.GetPath();
				if (FileHelpers.FileExistsCloud(path))
				{
					FileHelpers.FileCopyOutFromCloud(path, path2, true);
				}
				SaveSystem.InvalidateCache();
				ZLog.LogWarning("The character save operation may exceed the cloud save quota and it has therefore been moved to local storage!");
			}
		}
		this.m_playerProfile.Save();
	}

	// Token: 0x06001227 RID: 4647 RVA: 0x0008735C File Offset: 0x0008555C
	private Player SpawnPlayer(Vector3 spawnPoint, bool spawnValkyrie)
	{
		ZLog.DevLog("Spawning player:" + Time.frameCount.ToString());
		Player component = UnityEngine.Object.Instantiate<GameObject>(this.m_playerPrefab, spawnPoint, Quaternion.identity).GetComponent<Player>();
		component.SetLocalPlayer();
		this.m_playerProfile.LoadPlayerData(component);
		ZNet.instance.SetCharacterID(component.GetZDOID());
		component.OnSpawned(spawnValkyrie);
		return component;
	}

	// Token: 0x06001228 RID: 4648 RVA: 0x000873C8 File Offset: 0x000855C8
	private Bed FindBedNearby(Vector3 point, float maxDistance)
	{
		foreach (Bed bed in UnityEngine.Object.FindObjectsOfType<Bed>())
		{
			if (bed.IsCurrent())
			{
				return bed;
			}
		}
		return null;
	}

	// Token: 0x06001229 RID: 4649 RVA: 0x000873F8 File Offset: 0x000855F8
	private bool FindSpawnPoint(out Vector3 point, out bool usedLogoutPoint, float dt)
	{
		this.m_respawnWait += dt;
		usedLogoutPoint = false;
		if (!this.m_respawnAfterDeath && this.m_playerProfile.HaveLogoutPoint())
		{
			Vector3 logoutPoint = this.m_playerProfile.GetLogoutPoint();
			ZNet.instance.SetReferencePosition(logoutPoint);
			if (this.m_respawnWait <= this.m_respawnLoadDuration || !ZNetScene.instance.IsAreaReady(logoutPoint))
			{
				point = Vector3.zero;
				return false;
			}
			float num;
			if (!ZoneSystem.instance.GetGroundHeight(logoutPoint, out num))
			{
				string str = "Invalid spawn point, no ground ";
				Vector3 vector = logoutPoint;
				ZLog.Log(str + vector.ToString());
				this.m_respawnWait = 0f;
				this.m_playerProfile.ClearLoguoutPoint();
				point = Vector3.zero;
				return false;
			}
			this.m_playerProfile.ClearLoguoutPoint();
			point = logoutPoint;
			if (point.y < num)
			{
				point.y = num;
			}
			point.y += 0.25f;
			usedLogoutPoint = true;
			ZLog.Log("Spawned after " + this.m_respawnWait.ToString());
			return true;
		}
		else if (this.m_playerProfile.HaveCustomSpawnPoint())
		{
			Vector3 customSpawnPoint = this.m_playerProfile.GetCustomSpawnPoint();
			ZNet.instance.SetReferencePosition(customSpawnPoint);
			if (this.m_respawnWait <= this.m_respawnLoadDuration || !ZNetScene.instance.IsAreaReady(customSpawnPoint))
			{
				point = Vector3.zero;
				return false;
			}
			Bed bed = this.FindBedNearby(customSpawnPoint, 5f);
			if (bed != null)
			{
				ZLog.Log("Found bed at custom spawn point");
				point = bed.GetSpawnPoint();
				return true;
			}
			ZLog.Log("Failed to find bed at custom spawn point, using original");
			this.m_playerProfile.ClearCustomSpawnPoint();
			this.m_respawnWait = 0f;
			point = Vector3.zero;
			return false;
		}
		else
		{
			Vector3 a;
			if (ZoneSystem.instance.GetLocationIcon(this.m_StartLocation, out a))
			{
				point = a + Vector3.up * 2f;
				ZNet.instance.SetReferencePosition(point);
				return ZNetScene.instance.IsAreaReady(point);
			}
			ZNet.instance.SetReferencePosition(Vector3.zero);
			point = Vector3.zero;
			return false;
		}
	}

	// Token: 0x0600122A RID: 4650 RVA: 0x00087634 File Offset: 0x00085834
	public void RemoveCustomSpawnPoint(Vector3 point)
	{
		if (this.m_playerProfile.HaveCustomSpawnPoint())
		{
			Vector3 customSpawnPoint = this.m_playerProfile.GetCustomSpawnPoint();
			if (point == customSpawnPoint)
			{
				this.m_playerProfile.ClearCustomSpawnPoint();
			}
		}
	}

	// Token: 0x0600122B RID: 4651 RVA: 0x0008766E File Offset: 0x0008586E
	private static Vector3 GetPointOnCircle(float distance, float angle)
	{
		return new Vector3(Mathf.Sin(angle) * distance, 0f, Mathf.Cos(angle) * distance);
	}

	// Token: 0x0600122C RID: 4652 RVA: 0x0008768A File Offset: 0x0008588A
	public void RequestRespawn(float delay, bool afterDeath = false)
	{
		this.m_respawnAfterDeath = afterDeath;
		base.CancelInvoke("_RequestRespawn");
		base.Invoke("_RequestRespawn", delay);
	}

	// Token: 0x0600122D RID: 4653 RVA: 0x000876AC File Offset: 0x000858AC
	private void _RequestRespawn()
	{
		ZLog.Log("Starting respawn");
		if (Player.m_localPlayer)
		{
			this.m_playerProfile.SavePlayerData(Player.m_localPlayer);
		}
		if (Player.m_localPlayer)
		{
			ZNetScene.instance.Destroy(Player.m_localPlayer.gameObject);
			ZNet.instance.SetCharacterID(ZDOID.None);
		}
		this.m_respawnWait = 0f;
		this.m_requestRespawn = true;
		MusicMan.instance.TriggerMusic("respawn");
	}

	// Token: 0x0600122E RID: 4654 RVA: 0x00087730 File Offset: 0x00085930
	private void Update()
	{
		if (this.m_shuttingDown)
		{
			return;
		}
		bool flag = Settings.FPSLimit != 29 && Settings.FPSLimit <= 360;
		if (Settings.ReduceBackgroundUsage && !Application.isFocused)
		{
			Application.targetFrameRate = (flag ? Mathf.Min(30, Settings.FPSLimit) : 30);
		}
		else if (Game.IsPaused())
		{
			Application.targetFrameRate = (flag ? Mathf.Min(60, Settings.FPSLimit) : 60);
		}
		else
		{
			Application.targetFrameRate = (flag ? Settings.FPSLimit : -1);
		}
		if (Terminal.m_showTests)
		{
			Terminal.m_testList["fps limit"] = Application.targetFrameRate.ToString();
		}
		Game.UpdatePause();
		ZInput.Update(Time.unscaledDeltaTime);
		this.UpdateSaving(Time.unscaledDeltaTime);
		LightLod.UpdateLights(Time.deltaTime);
		if (this.m_queuedIntro && ZNet.GetConnectionStatus() == ZNet.ConnectionStatus.Connected)
		{
			float num2;
			float num = ZoneSystem.GetGenerationTimeBudgetForTargetFrameRate(out num2) / num2;
			if (ZoneSystem.instance.GetEstimatedGenerationCompletionTimeFromNow() < 45f * num)
			{
				this.m_queuedIntro = false;
				this.ShowIntro();
			}
		}
	}

	// Token: 0x0600122F RID: 4655 RVA: 0x0008783C File Offset: 0x00085A3C
	private void OnGUI()
	{
		ZInput.OnGUI();
	}

	// Token: 0x06001230 RID: 4656 RVA: 0x00087844 File Offset: 0x00085A44
	private void FixedUpdate()
	{
		if (ZNet.m_loadError)
		{
			this.Logout(true, true);
			ZLog.LogError("World load failed, exiting without save. Check backups!");
		}
		if (!this.m_haveSpawned && ZNet.GetConnectionStatus() == ZNet.ConnectionStatus.Connected)
		{
			this.m_haveSpawned = true;
			this.RequestRespawn(0f, false);
		}
		ZInput.FixedUpdate(Time.fixedDeltaTime);
		if (ZNet.GetConnectionStatus() != ZNet.ConnectionStatus.Connecting && ZNet.GetConnectionStatus() != ZNet.ConnectionStatus.Connected)
		{
			ZLog.Log("Lost connection to server:" + ZNet.GetConnectionStatus().ToString());
			this.Logout(true, true);
			return;
		}
		this.UpdateRespawn(Time.fixedDeltaTime);
	}

	// Token: 0x06001231 RID: 4657 RVA: 0x000878E0 File Offset: 0x00085AE0
	private void UpdateSaving(float dt)
	{
		if (Game.m_saveInterval - this.m_saveTimer > 30f && Game.m_saveInterval - (this.m_saveTimer + dt) <= 30f && MessageHud.instance && ZNet.instance.IsServer())
		{
			MessageHud.instance.MessageAll(MessageHud.MessageType.Center, "$msg_worldsavewarning " + 30f.ToString() + "s");
		}
		this.m_saveTimer += dt;
		if (this.m_saveTimer > Game.m_saveInterval)
		{
			bool flag;
			if (!ZNet.instance.EnoughDiskSpaceAvailable(out flag, false, null))
			{
				this.m_saveTimer -= 300f;
				return;
			}
			this.SavePlayerProfile(false);
			if (ZNet.instance)
			{
				ZNet.instance.Save(false, true, true);
			}
		}
	}

	// Token: 0x06001232 RID: 4658 RVA: 0x000879B4 File Offset: 0x00085BB4
	private void UpdateRespawn(float dt)
	{
		if (!this.m_requestRespawn)
		{
			return;
		}
		Vector3 vector;
		bool flag;
		if (!this.FindSpawnPoint(out vector, out flag, dt))
		{
			return;
		}
		if (!flag)
		{
			this.m_playerProfile.SetHomePoint(vector);
		}
		this.SpawnPlayer(vector, this.m_playerProfile.m_firstSpawn && this.m_inIntro);
		this.m_playerProfile.m_firstSpawn = false;
		this.m_inIntro = false;
		this.m_requestRespawn = false;
		if (this.m_firstSpawn)
		{
			this.m_firstSpawn = false;
			Chat.instance.SendText(Talker.Type.Shout, Localization.instance.Localize("$text_player_arrived"));
			Game.UpdateNoMap();
			JoinCode.Show(true);
			if (ZNet.m_loadError)
			{
				Player.m_localPlayer.Message(MessageHud.MessageType.Center, "World load error, saving disabled! Recover your .old file or backups!", 0, null);
				Hud.instance.m_betaText.GetComponent<TMP_Text>().text = "";
				Hud.instance.m_betaText.transform.GetChild(0).GetComponent<TMP_Text>().text = "WORLD SAVE DISABLED! (World load error)";
				Hud.instance.m_betaText.SetActive(true);
			}
		}
		Game.instance.CollectResourcesCheck();
	}

	// Token: 0x06001233 RID: 4659 RVA: 0x00087AC6 File Offset: 0x00085CC6
	public bool WaitingForRespawn()
	{
		return this.m_requestRespawn;
	}

	// Token: 0x06001234 RID: 4660 RVA: 0x00087ACE File Offset: 0x00085CCE
	public bool InIntro(bool includeQueued = false)
	{
		return this.m_inIntro || (includeQueued && this.m_queuedIntro);
	}

	// Token: 0x06001235 RID: 4661 RVA: 0x00087AE5 File Offset: 0x00085CE5
	public void SkipIntro()
	{
		this.m_queuedIntro = false;
		this.m_inIntro = false;
		this.RequestRespawn(0f, false);
		if (Valkyrie.m_instance)
		{
			Valkyrie.m_instance.DropPlayer(true);
		}
		TextViewer.instance.HideIntro();
	}

	// Token: 0x06001236 RID: 4662 RVA: 0x00087B22 File Offset: 0x00085D22
	public PlayerProfile GetPlayerProfile()
	{
		return this.m_playerProfile;
	}

	// Token: 0x06001237 RID: 4663 RVA: 0x00087B2A File Offset: 0x00085D2A
	public void IncrementPlayerStat(PlayerStatType stat, float amount = 1f)
	{
		this.m_playerProfile.IncrementStat(stat, amount);
	}

	// Token: 0x06001238 RID: 4664 RVA: 0x00087B39 File Offset: 0x00085D39
	public static void SetProfile(string filename, FileHelpers.FileSource fileSource)
	{
		Game.m_profileFilename = filename;
		Game.m_profileFileSource = fileSource;
	}

	// Token: 0x06001239 RID: 4665 RVA: 0x00087B47 File Offset: 0x00085D47
	private IEnumerator ConnectPortalsCoroutine()
	{
		for (;;)
		{
			this.ConnectPortals();
			yield return new WaitForSeconds(5f);
		}
		yield break;
	}

	// Token: 0x0600123A RID: 4666 RVA: 0x00087B58 File Offset: 0x00085D58
	public void ConnectPortals()
	{
		this.ClearCurrentlyConnectingPortals();
		List<ZDO> portals = ZDOMan.instance.GetPortals();
		int num = 0;
		foreach (ZDO zdo in portals)
		{
			ZDOID connectionZDOID = zdo.GetConnectionZDOID(ZDOExtraData.ConnectionType.Portal);
			string @string = zdo.GetString(ZDOVars.s_tag, "");
			if (!connectionZDOID.IsNone())
			{
				ZDO zdo2 = ZDOMan.instance.GetZDO(connectionZDOID);
				if (zdo2 == null || zdo2.GetString(ZDOVars.s_tag, "") != @string)
				{
					this.SetConnection(zdo, ZDOID.None, false);
				}
			}
		}
		foreach (ZDO zdo3 in portals)
		{
			if (!this.IsCurrentlyConnectingPortal(zdo3) && zdo3.GetConnectionZDOID(ZDOExtraData.ConnectionType.Portal).IsNone())
			{
				string string2 = zdo3.GetString(ZDOVars.s_tag, "");
				ZDO zdo4 = this.FindRandomUnconnectedPortal(portals, zdo3, string2);
				if (zdo4 != null)
				{
					this.AddToCurrentlyConnectingPortals(zdo3, zdo4);
					this.SetConnection(zdo3, zdo4.m_uid, false);
					this.SetConnection(zdo4, zdo3.m_uid, false);
					num++;
					string str = "Connected portals ";
					ZDO zdo5 = zdo3;
					string str2 = (zdo5 != null) ? zdo5.ToString() : null;
					string str3 = " <-> ";
					ZDO zdo6 = zdo4;
					ZLog.Log(str + str2 + str3 + ((zdo6 != null) ? zdo6.ToString() : null));
				}
			}
		}
		if (num > 0)
		{
			ZLog.Log("[ Connected " + num.ToString() + " portals ]");
		}
	}

	// Token: 0x0600123B RID: 4667 RVA: 0x00087D10 File Offset: 0x00085F10
	private void ForceSetConnection(ZDO portal, ZDOID connection)
	{
		if (portal.GetConnectionZDOID(ZDOExtraData.ConnectionType.Portal) != connection)
		{
			this.SetConnection(portal, connection, true);
		}
	}

	// Token: 0x0600123C RID: 4668 RVA: 0x00087D2C File Offset: 0x00085F2C
	private void SetConnection(ZDO portal, ZDOID connection, bool forceImmediateConnection = false)
	{
		long owner = portal.GetOwner();
		bool flag = ZNet.instance.GetPeer(owner) != null;
		if (owner == 0L || !flag || forceImmediateConnection)
		{
			portal.SetOwner(ZDOMan.GetSessionID());
			portal.SetConnection(ZDOExtraData.ConnectionType.Portal, connection);
			ZDOMan.instance.ForceSendZDO(portal.m_uid);
			return;
		}
		ZRoutedRpc.instance.InvokeRoutedRPC(owner, "RPC_SetConnection", new object[]
		{
			portal.m_uid,
			connection
		});
	}

	// Token: 0x0600123D RID: 4669 RVA: 0x00087DB0 File Offset: 0x00085FB0
	private void RPC_SetConnection(long sender, ZDOID portalID, ZDOID connectionID)
	{
		ZDO zdo = ZDOMan.instance.GetZDO(portalID);
		if (zdo == null)
		{
			return;
		}
		zdo.SetOwner(ZDOMan.GetSessionID());
		zdo.SetConnection(ZDOExtraData.ConnectionType.Portal, connectionID);
		ZDOMan.instance.ForceSendZDO(portalID);
	}

	// Token: 0x0600123E RID: 4670 RVA: 0x00087DEC File Offset: 0x00085FEC
	private ZDO FindRandomUnconnectedPortal(List<ZDO> portals, ZDO skip, string tag)
	{
		List<ZDO> list = new List<ZDO>();
		foreach (ZDO zdo in portals)
		{
			if (zdo != skip && !(zdo.GetString(ZDOVars.s_tag, "") != tag) && !(zdo.GetConnectionZDOID(ZDOExtraData.ConnectionType.Portal) != ZDOID.None) && !this.IsCurrentlyConnectingPortal(zdo))
			{
				list.Add(zdo);
			}
		}
		if (list.Count == 0)
		{
			return null;
		}
		return list[UnityEngine.Random.Range(0, list.Count)];
	}

	// Token: 0x0600123F RID: 4671 RVA: 0x00087E94 File Offset: 0x00086094
	private void AddToCurrentlyConnectingPortals(ZDO portalA, ZDO portalB)
	{
		this.m_currentlyConnectingPortals.Add(new Game.ConnectingPortals
		{
			PortalA = portalA,
			PortalB = portalB
		});
	}

	// Token: 0x06001240 RID: 4672 RVA: 0x00087EC8 File Offset: 0x000860C8
	private void ClearCurrentlyConnectingPortals()
	{
		foreach (Game.ConnectingPortals connectingPortals in this.m_currentlyConnectingPortals)
		{
			this.ForceSetConnection(connectingPortals.PortalA, connectingPortals.PortalB.m_uid);
			this.ForceSetConnection(connectingPortals.PortalB, connectingPortals.PortalA.m_uid);
		}
		this.m_currentlyConnectingPortals.Clear();
	}

	// Token: 0x06001241 RID: 4673 RVA: 0x00087F50 File Offset: 0x00086150
	private bool IsCurrentlyConnectingPortal(ZDO zdo)
	{
		foreach (Game.ConnectingPortals connectingPortals in this.m_currentlyConnectingPortals)
		{
			if (zdo == connectingPortals.PortalA || zdo == connectingPortals.PortalB)
			{
				return true;
			}
		}
		return false;
	}

	// Token: 0x06001242 RID: 4674 RVA: 0x00087FB8 File Offset: 0x000861B8
	private void UpdateSleeping()
	{
		if (!ZNet.instance.IsServer())
		{
			return;
		}
		if (this.m_sleeping)
		{
			if (!EnvMan.instance.IsTimeSkipping())
			{
				this.m_lastSleepTime = ZNet.instance.GetTimeSeconds();
				this.m_sleeping = false;
				ZRoutedRpc.instance.InvokeRoutedRPC(ZRoutedRpc.Everybody, "SleepStop", Array.Empty<object>());
				return;
			}
		}
		else if (!EnvMan.instance.IsTimeSkipping())
		{
			if (!EnvMan.IsAfternoon() && !EnvMan.IsNight())
			{
				return;
			}
			if (!this.EverybodyIsTryingToSleep() || ZNet.instance.GetTimeSeconds() - this.m_lastSleepTime < 10.0)
			{
				return;
			}
			EnvMan.instance.SkipToMorning();
			this.m_sleeping = true;
			ZRoutedRpc.instance.InvokeRoutedRPC(ZRoutedRpc.Everybody, "SleepStart", Array.Empty<object>());
		}
	}

	// Token: 0x06001243 RID: 4675 RVA: 0x00088084 File Offset: 0x00086284
	private bool EverybodyIsTryingToSleep()
	{
		List<ZDO> allCharacterZDOS = ZNet.instance.GetAllCharacterZDOS();
		if (allCharacterZDOS.Count == 0)
		{
			return false;
		}
		using (List<ZDO>.Enumerator enumerator = allCharacterZDOS.GetEnumerator())
		{
			while (enumerator.MoveNext())
			{
				if (!enumerator.Current.GetBool(ZDOVars.s_inBed, false))
				{
					return false;
				}
			}
		}
		return true;
	}

	// Token: 0x06001244 RID: 4676 RVA: 0x000880F4 File Offset: 0x000862F4
	private void SleepStart(long sender)
	{
		Player localPlayer = Player.m_localPlayer;
		if (localPlayer)
		{
			localPlayer.SetSleeping(true);
		}
	}

	// Token: 0x06001245 RID: 4677 RVA: 0x00088118 File Offset: 0x00086318
	private void SleepStop(long sender)
	{
		Player localPlayer = Player.m_localPlayer;
		if (localPlayer)
		{
			localPlayer.SetSleeping(false);
			localPlayer.AttachStop();
		}
		if (this.m_saveTimer > 60f)
		{
			bool flag;
			if (!ZNet.instance.EnoughDiskSpaceAvailable(out flag, false, null))
			{
				this.m_saveTimer -= 300f;
				return;
			}
			this.SavePlayerProfile(false);
			if (ZNet.instance)
			{
				ZNet.instance.Save(false, false, true);
				return;
			}
		}
		else
		{
			ZLog.Log("Saved recently, skipping sleep save.");
		}
	}

	// Token: 0x06001246 RID: 4678 RVA: 0x0008819C File Offset: 0x0008639C
	public void DiscoverClosestLocation(string name, Vector3 point, string pinName, int pinType, bool showMap = true, bool discoverAll = false)
	{
		ZLog.Log("DiscoverClosestLocation");
		ZRoutedRpc.instance.InvokeRoutedRPC("RPC_DiscoverClosestLocation", new object[]
		{
			name,
			point,
			pinName,
			pinType,
			showMap,
			discoverAll
		});
	}

	// Token: 0x06001247 RID: 4679 RVA: 0x000881F8 File Offset: 0x000863F8
	private void RPC_DiscoverClosestLocation(long sender, string name, Vector3 point, string pinName, int pinType, bool showMap, bool discoverAll)
	{
		if (discoverAll && ZoneSystem.instance.FindLocations(name, ref this.m_tempLocations))
		{
			ZLog.Log(string.Format("Found {0} locations of type {1}", this.m_tempLocations.Count, name));
			using (List<ZoneSystem.LocationInstance>.Enumerator enumerator = this.m_tempLocations.GetEnumerator())
			{
				while (enumerator.MoveNext())
				{
					ZoneSystem.LocationInstance locationInstance = enumerator.Current;
					ZRoutedRpc.instance.InvokeRoutedRPC(sender, "RPC_DiscoverLocationResponse", new object[]
					{
						pinName,
						pinType,
						locationInstance.m_position,
						showMap
					});
				}
				return;
			}
		}
		ZoneSystem.LocationInstance locationInstance2;
		if (!discoverAll && ZoneSystem.instance.FindClosestLocation(name, point, out locationInstance2))
		{
			ZLog.Log("Found location of type " + name);
			ZRoutedRpc.instance.InvokeRoutedRPC(sender, "RPC_DiscoverLocationResponse", new object[]
			{
				pinName,
				pinType,
				locationInstance2.m_position,
				showMap
			});
			return;
		}
		ZLog.LogWarning("Failed to find location of type " + name);
	}

	// Token: 0x06001248 RID: 4680 RVA: 0x00088334 File Offset: 0x00086534
	private void RPC_DiscoverLocationResponse(long sender, string pinName, int pinType, Vector3 pos, bool showMap)
	{
		Minimap.instance.DiscoverLocation(pos, (Minimap.PinType)pinType, pinName, showMap);
		if (Player.m_localPlayer && Minimap.instance.m_mode == Minimap.MapMode.None)
		{
			Player.m_localPlayer.SetLookDir(pos - Player.m_localPlayer.transform.position, 3.5f);
		}
	}

	// Token: 0x06001249 RID: 4681 RVA: 0x0008838F File Offset: 0x0008658F
	public void Ping()
	{
		if (global::Console.instance)
		{
			global::Console.instance.Print("Ping sent to server");
		}
		ZRoutedRpc.instance.InvokeRoutedRPC("Ping", new object[]
		{
			Time.time
		});
	}

	// Token: 0x0600124A RID: 4682 RVA: 0x000883CE File Offset: 0x000865CE
	private void RPC_Ping(long sender, float time)
	{
		ZRoutedRpc.instance.InvokeRoutedRPC(sender, "Pong", new object[]
		{
			time
		});
	}

	// Token: 0x0600124B RID: 4683 RVA: 0x000883F0 File Offset: 0x000865F0
	private void RPC_Pong(long sender, float time)
	{
		float num = Time.time - time;
		string text = "Got ping reply from server: " + ((int)(num * 1000f)).ToString() + " ms";
		ZLog.Log(text);
		if (global::Console.instance)
		{
			global::Console.instance.Print(text);
		}
		if (Chat.instance)
		{
			Chat.instance.AddString(text);
		}
	}

	// Token: 0x0600124C RID: 4684 RVA: 0x00088459 File Offset: 0x00086659
	public void SetForcePlayerDifficulty(int players)
	{
		this.m_forcePlayers = players;
	}

	// Token: 0x0600124D RID: 4685 RVA: 0x00088464 File Offset: 0x00086664
	public int GetPlayerDifficulty(Vector3 pos)
	{
		if (this.m_forcePlayers > 0)
		{
			return this.m_forcePlayers;
		}
		int num = Player.GetPlayersInRangeXZ(pos, this.m_difficultyScaleRange);
		if (num < 1)
		{
			num = 1;
		}
		if (num > this.m_difficultyScaleMaxPlayers)
		{
			num = this.m_difficultyScaleMaxPlayers;
		}
		return num;
	}

	// Token: 0x0600124E RID: 4686 RVA: 0x000884A8 File Offset: 0x000866A8
	public float GetDifficultyDamageScalePlayer(Vector3 pos)
	{
		int playerDifficulty = this.GetPlayerDifficulty(pos);
		return 1f + (float)(playerDifficulty - 1) * this.m_damageScalePerPlayer;
	}

	// Token: 0x0600124F RID: 4687 RVA: 0x000884D0 File Offset: 0x000866D0
	public float GetDifficultyDamageScaleEnemy(Vector3 pos)
	{
		int playerDifficulty = this.GetPlayerDifficulty(pos);
		float num = 1f + (float)(playerDifficulty - 1) * this.m_healthScalePerPlayer;
		return 1f / num;
	}

	// Token: 0x06001250 RID: 4688 RVA: 0x00088500 File Offset: 0x00086700
	private static void UpdatePause()
	{
		if (Game.m_pauseFrom != Game.m_pauseTarget)
		{
			if (DateTime.Now >= Game.m_pauseEnd)
			{
				Game.m_pauseFrom = Game.m_pauseTarget;
				Game.m_timeScale = Game.m_pauseTarget;
			}
			else
			{
				Game.m_timeScale = Mathf.SmoothStep(Game.m_pauseFrom, Game.m_pauseTarget, (float)((DateTime.Now - Game.m_pauseStart).TotalSeconds / (Game.m_pauseEnd - Game.m_pauseStart).TotalSeconds));
			}
		}
		if (Time.timeScale > 0f)
		{
			Game.m_pauseRotateFade = 0f;
		}
		Time.timeScale = (Game.IsPaused() ? 0f : ((ZNet.instance.GetPeerConnections() > 0) ? 1f : Game.m_timeScale));
		if (Game.IsPaused())
		{
			Game.m_pauseTimer += Time.fixedUnscaledDeltaTime;
		}
		else if (Game.m_pauseTimer > 0f)
		{
			Game.m_pauseTimer = 0f;
		}
		if (Game.IsPaused() && Menu.IsVisible() && Player.m_localPlayer)
		{
			if (Game.m_pauseRotateFade < 1f)
			{
				Mathf.Min(1f, Game.m_pauseRotateFade += 0.05f * Time.unscaledDeltaTime);
			}
			Transform eye = Player.m_localPlayer.m_eye;
			Vector3 forward = Player.m_localPlayer.m_eye.forward;
			float num = Vector3.Dot(forward, Vector3.up);
			float num2 = Vector3.Dot(forward, Vector3.down);
			float num3 = Mathf.Max(0.05f, 1f - ((num > num2) ? num : num2));
			eye.Rotate(Vector3.up, Time.unscaledDeltaTime * Mathf.Cos(Time.realtimeSinceStartup * 0.3f) * 5f * Game.m_pauseRotateFade * num3);
			Player.m_localPlayer.SetLookDir(eye.forward, 0f);
			Game.m_collectTimer += Time.fixedUnscaledDeltaTime;
			if (Game.m_collectTimer > 5f && DateTime.Now > ZInput.instance.GetLastInputTimer() + TimeSpan.FromSeconds(5.0))
			{
				Game.instance.CollectResourcesCheck();
				Game.m_collectTimer = -1000f;
				return;
			}
		}
		else if (Game.m_collectTimer != 0f)
		{
			Game.m_collectTimer = 0f;
		}
	}

	// Token: 0x06001251 RID: 4689 RVA: 0x00088746 File Offset: 0x00086946
	public static bool IsPaused()
	{
		return Game.m_pause && Game.CanPause();
	}

	// Token: 0x06001252 RID: 4690 RVA: 0x00088756 File Offset: 0x00086956
	public static void Pause()
	{
		Game.m_pause = true;
	}

	// Token: 0x06001253 RID: 4691 RVA: 0x0008875E File Offset: 0x0008695E
	public static void Unpause()
	{
		Game.m_pause = false;
		Game.m_timeScale = 1f;
	}

	// Token: 0x06001254 RID: 4692 RVA: 0x00088770 File Offset: 0x00086970
	public static void PauseToggle()
	{
		if (Game.IsPaused())
		{
			Game.Unpause();
			return;
		}
		Game.Pause();
	}

	// Token: 0x06001255 RID: 4693 RVA: 0x00088784 File Offset: 0x00086984
	private static bool CanPause()
	{
		return (!ZNet.instance.IsServer() || ZNet.instance.GetPeerConnections() <= 0) && Player.m_localPlayer && ZNet.instance && ((Player.m_debugMode && !ZNet.instance.IsServer() && global::Console.instance && global::Console.instance.IsCheatsEnabled()) || (ZNet.instance.IsServer() && ZNet.instance.GetPeerConnections() == 0));
	}

	// Token: 0x06001256 RID: 4694 RVA: 0x0008880C File Offset: 0x00086A0C
	public static void FadeTimeScale(float timeScale = 0f, float transitionSec = 0f)
	{
		if (timeScale != 1f && !Game.CanPause())
		{
			return;
		}
		timeScale = Mathf.Clamp(timeScale, 0f, 100f);
		if (transitionSec == 0f)
		{
			Game.m_timeScale = timeScale;
			return;
		}
		Game.m_pauseFrom = Time.timeScale;
		Game.m_pauseTarget = timeScale;
		Game.m_pauseStart = DateTime.Now;
		Game.m_pauseEnd = DateTime.Now + TimeSpan.FromSeconds((double)transitionSec);
	}

	// Token: 0x06001257 RID: 4695 RVA: 0x0008887C File Offset: 0x00086A7C
	public int ScaleDrops(GameObject drop, int amount)
	{
		if (Game.m_resourceRate != 1f)
		{
			ItemDrop component = drop.GetComponent<ItemDrop>();
			if (component != null)
			{
				return this.ScaleDrops(component.m_itemData, amount);
			}
		}
		return amount;
	}

	// Token: 0x06001258 RID: 4696 RVA: 0x000888B0 File Offset: 0x00086AB0
	public int ScaleDrops(ItemDrop.ItemData data, int amount)
	{
		if (Game.m_resourceRate != 1f && !this.m_nonScaledDropTypes.Contains(data.m_shared.m_itemType))
		{
			amount = (int)Mathf.Clamp(Mathf.Round((float)amount * Game.m_resourceRate), 1f, (float)((data.m_shared.m_maxStackSize > 1) ? data.m_shared.m_maxStackSize : 1000));
		}
		return amount;
	}

	// Token: 0x06001259 RID: 4697 RVA: 0x00088920 File Offset: 0x00086B20
	public int ScaleDrops(GameObject drop, int randomMin, int randomMax)
	{
		if (Game.m_resourceRate != 1f)
		{
			ItemDrop component = drop.GetComponent<ItemDrop>();
			if (component != null)
			{
				return this.ScaleDrops(component.m_itemData, randomMin, randomMax);
			}
		}
		return UnityEngine.Random.Range(randomMin, randomMax);
	}

	// Token: 0x0600125A RID: 4698 RVA: 0x0008895C File Offset: 0x00086B5C
	public int ScaleDrops(ItemDrop.ItemData data, int randomMin, int randomMax)
	{
		if (Game.m_resourceRate != 1f && !this.m_nonScaledDropTypes.Contains(data.m_shared.m_itemType))
		{
			return Mathf.Min(this.ScaleDrops(randomMin, randomMax), (data.m_shared.m_maxStackSize > 1) ? data.m_shared.m_maxStackSize : 10000);
		}
		return UnityEngine.Random.Range(randomMin, randomMax);
	}

	// Token: 0x0600125B RID: 4699 RVA: 0x000889C2 File Offset: 0x00086BC2
	public int ScaleDrops(int randomMin, int randomMax)
	{
		return (int)Mathf.Max(1f, Mathf.Round(UnityEngine.Random.Range((float)randomMin, (float)randomMax) * Game.m_resourceRate));
	}

	// Token: 0x0600125C RID: 4700 RVA: 0x000889E4 File Offset: 0x00086BE4
	public int ScaleDropsInverse(GameObject drop, int randomMin, int randomMax)
	{
		if (Game.m_resourceRate != 1f)
		{
			ItemDrop component = drop.GetComponent<ItemDrop>();
			if (component != null)
			{
				return this.ScaleDropsInverse(component.m_itemData, randomMin, randomMax);
			}
		}
		return UnityEngine.Random.Range(randomMin, randomMax);
	}

	// Token: 0x0600125D RID: 4701 RVA: 0x00088A20 File Offset: 0x00086C20
	public int ScaleDropsInverse(ItemDrop.ItemData data, int randomMin, int randomMax)
	{
		if (Game.m_resourceRate != 1f && !this.m_nonScaledDropTypes.Contains(data.m_shared.m_itemType))
		{
			return this.ScaleDropsInverse(randomMin, Mathf.Min(randomMax, (data.m_shared.m_maxStackSize > 1) ? data.m_shared.m_maxStackSize : 1000));
		}
		return UnityEngine.Random.Range(randomMin, randomMax);
	}

	// Token: 0x0600125E RID: 4702 RVA: 0x00088A86 File Offset: 0x00086C86
	public int ScaleDropsInverse(int randomMin, int randomMax)
	{
		return (int)Mathf.Max(1f, Mathf.Round(UnityEngine.Random.Range((float)randomMin, (float)randomMax) / Game.m_resourceRate));
	}

	// Token: 0x0600125F RID: 4703 RVA: 0x00088AA8 File Offset: 0x00086CA8
	public static void UpdateWorldRates(HashSet<string> globalKeys, Dictionary<string, string> globalKeysValues)
	{
		Game.<>c__DisplayClass77_0 CS$<>8__locals1;
		CS$<>8__locals1.globalKeysValues = globalKeysValues;
		CS$<>8__locals1.playerKeys = (Player.m_localPlayer ? Player.m_localPlayer.GetUniqueKeys() : null);
		Game.<UpdateWorldRates>g__trySetScalarKey|77_0(GlobalKeys.PlayerDamage, out Game.m_playerDamageRate, 1f, 100f, ref CS$<>8__locals1);
		Game.<UpdateWorldRates>g__trySetScalarKey|77_0(GlobalKeys.EnemyDamage, out Game.m_enemyDamageRate, 1f, 100f, ref CS$<>8__locals1);
		Game.<UpdateWorldRates>g__trySetScalarKey|77_0(GlobalKeys.ResourceRate, out Game.m_resourceRate, 1f, 100f, ref CS$<>8__locals1);
		Game.<UpdateWorldRates>g__trySetScalarKey|77_0(GlobalKeys.StaminaRate, out Game.m_staminaRate, 1f, 100f, ref CS$<>8__locals1);
		Game.<UpdateWorldRates>g__trySetScalarKey|77_0(GlobalKeys.MoveStaminaRate, out Game.m_moveStaminaRate, 1f, 100f, ref CS$<>8__locals1);
		Game.<UpdateWorldRates>g__trySetScalarKey|77_0(GlobalKeys.StaminaRegenRate, out Game.m_staminaRegenRate, 1f, 100f, ref CS$<>8__locals1);
		Game.<UpdateWorldRates>g__trySetScalarKey|77_0(GlobalKeys.SkillGainRate, out Game.m_skillGainRate, 1f, 100f, ref CS$<>8__locals1);
		Game.<UpdateWorldRates>g__trySetScalarKey|77_0(GlobalKeys.SkillReductionRate, out Game.m_skillReductionRate, 1f, 100f, ref CS$<>8__locals1);
		Game.<UpdateWorldRates>g__trySetScalarKey|77_0(GlobalKeys.EnemySpeedSize, out Game.m_enemySpeedSize, 1f, 100f, ref CS$<>8__locals1);
		Game.<UpdateWorldRates>g__trySetScalarKey|77_0(GlobalKeys.EnemyLevelUpRate, out Game.m_enemyLevelUpRate, 1f, 100f, ref CS$<>8__locals1);
		Game.<UpdateWorldRates>g__trySetIntKey|77_1(GlobalKeys.WorldLevel, out Game.m_worldLevel, 0, ref CS$<>8__locals1);
		Game.<UpdateWorldRates>g__trySetScalarKey|77_0(GlobalKeys.EventRate, out Game.m_eventRate, 1f, 100f, ref CS$<>8__locals1);
		Game.<UpdateWorldRates>g__trySetScalarKeyPlayer|77_2(PlayerKeys.DamageTaken, out Game.m_localDamgeTakenRate, 1f, 100f, ref CS$<>8__locals1);
		Game.m_worldLevel = Mathf.Clamp(Game.m_worldLevel, 0, 10);
		Game.UpdateNoMap();
		Game.m_serverOptionsSummary = ServerOptionsGUI.GetWorldModifierSummary(globalKeys, false, ", ");
		PlayerProfile playerProfile = Game.instance.GetPlayerProfile();
		for (int i = 0; i < globalKeys.Count; i++)
		{
			GlobalKeys globalKeys2 = (GlobalKeys)i;
			if (!ZoneSystem.instance.GetGlobalKey(globalKeys2))
			{
				playerProfile.m_knownWorldKeys.IncrementOrSet(string.Format("{0} {1}", globalKeys2, "default"), 1f);
			}
		}
	}

	// Token: 0x06001260 RID: 4704 RVA: 0x00088C7C File Offset: 0x00086E7C
	public static void UpdateNoMap()
	{
		Game.m_noMap = ((ZoneSystem.instance && ZoneSystem.instance.GetGlobalKey(GlobalKeys.NoMap)) || (Player.m_localPlayer != null && PlayerPrefs.GetFloat("mapenabled_" + Player.m_localPlayer.GetPlayerName(), 1f) == 0f));
		Minimap.instance.SetMapMode(Game.m_noMap ? Minimap.MapMode.None : Minimap.MapMode.Small);
	}

	// Token: 0x06001261 RID: 4705 RVA: 0x00088CF8 File Offset: 0x00086EF8
	public GameObject CheckDropConversion(HitData hitData, ItemDrop itemDrop, GameObject dropPrefab, ref int dropCount)
	{
		if (hitData == null)
		{
			return dropPrefab;
		}
		HitData.DamageType majorityDamageType = hitData.m_damage.GetMajorityDamageType();
		HitData.HitType hitType = hitData.m_hitType;
		bool flag = majorityDamageType == HitData.DamageType.Fire && ZoneSystem.instance.GetGlobalKey(GlobalKeys.Fire);
		foreach (Game.ItemConversion itemConversion in this.m_damageTypeDropConversions)
		{
			if ((itemConversion.m_hitType == HitData.HitType.Undefined || itemConversion.m_hitType == hitType || flag) && itemConversion.m_damageType == majorityDamageType)
			{
				using (List<ItemDrop>.Enumerator enumerator2 = itemConversion.m_items.GetEnumerator())
				{
					while (enumerator2.MoveNext())
					{
						if (!(enumerator2.Current.m_itemData.m_shared.m_name != itemDrop.m_itemData.m_shared.m_name))
						{
							dropCount *= itemConversion.m_multiplier;
							return itemConversion.m_result.gameObject;
						}
					}
				}
			}
		}
		return dropPrefab;
	}

	// Token: 0x1700009E RID: 158
	// (get) Token: 0x06001262 RID: 4706 RVA: 0x00088E1C File Offset: 0x0008701C
	// (set) Token: 0x06001263 RID: 4707 RVA: 0x00088E24 File Offset: 0x00087024
	public List<int> PortalPrefabHash { get; private set; } = new List<int>();

	// Token: 0x06001266 RID: 4710 RVA: 0x00089064 File Offset: 0x00087264
	[CompilerGenerated]
	internal static void <UpdateWorldRates>g__trySetScalarKey|77_0(GlobalKeys key, out float value, float defaultValue = 1f, float multiplier = 100f, ref Game.<>c__DisplayClass77_0 A_4)
	{
		value = defaultValue;
		string s;
		float num;
		if (A_4.globalKeysValues.TryGetValue(key.ToString().ToLower(), out s) && float.TryParse(s, NumberStyles.Any, CultureInfo.InvariantCulture, out num))
		{
			value = num / multiplier;
		}
	}

	// Token: 0x06001267 RID: 4711 RVA: 0x000890B0 File Offset: 0x000872B0
	[CompilerGenerated]
	internal static void <UpdateWorldRates>g__trySetIntKey|77_1(GlobalKeys key, out int value, int defaultValue = 1, ref Game.<>c__DisplayClass77_0 A_3)
	{
		value = defaultValue;
		string s;
		int num;
		if (A_3.globalKeysValues.TryGetValue(key.ToString().ToLower(), out s) && int.TryParse(s, NumberStyles.Any, CultureInfo.InvariantCulture, out num))
		{
			value = num;
		}
	}

	// Token: 0x06001268 RID: 4712 RVA: 0x000890F8 File Offset: 0x000872F8
	[CompilerGenerated]
	internal static void <UpdateWorldRates>g__trySetScalarKeyPlayer|77_2(PlayerKeys key, out float value, float defaultValue = 1f, float multiplier = 100f, ref Game.<>c__DisplayClass77_0 A_4)
	{
		if (A_4.playerKeys == null)
		{
			value = defaultValue;
			return;
		}
		value = defaultValue;
		foreach (string text in A_4.playerKeys)
		{
			string[] array = text.Split(' ', StringSplitOptions.None);
			float num;
			if (array.Length >= 2 && array[0].ToLower() == key.ToString().ToLower() && float.TryParse(array[1], NumberStyles.Any, CultureInfo.InvariantCulture, out num))
			{
				value = num / multiplier;
				break;
			}
		}
	}

	// Token: 0x04001167 RID: 4455
	public static readonly string messageForModders = "While we don't officially support mods in Valheim at this time. We ask that you please set the following isModded value to true in your mod. This will place a small text in the menu to inform the player that their game is modded and help us solving support issues. Thank you for your help!";

	// Token: 0x04001168 RID: 4456
	public static bool isModded = false;

	// Token: 0x0400116B RID: 4459
	public const int m_backgroundFPS = 30;

	// Token: 0x0400116C RID: 4460
	public const int m_menuFPS = 60;

	// Token: 0x0400116D RID: 4461
	public const int m_minimumFPSLimit = 30;

	// Token: 0x0400116E RID: 4462
	public const int m_maximumFPSLimit = 360;

	// Token: 0x0400116F RID: 4463
	public GameObject m_playerPrefab;

	// Token: 0x04001170 RID: 4464
	public List<GameObject> m_portalPrefabs;

	// Token: 0x04001171 RID: 4465
	public GameObject m_consolePrefab;

	// Token: 0x04001172 RID: 4466
	public GameObject m_serverOptionPrefab;

	// Token: 0x04001173 RID: 4467
	public SceneReference m_startScene;

	// Token: 0x04001174 RID: 4468
	[Header("Player Startup")]
	public string m_devWorldName = "DevWorld";

	// Token: 0x04001175 RID: 4469
	public string m_devWorldSeed = "";

	// Token: 0x04001176 RID: 4470
	public string m_devProfileName = "Developer";

	// Token: 0x04001177 RID: 4471
	public string m_devPlayerName = "Odev";

	// Token: 0x04001178 RID: 4472
	public string m_StartLocation = "StartTemple";

	// Token: 0x04001179 RID: 4473
	private static DateTime m_pauseStart;

	// Token: 0x0400117A RID: 4474
	private static DateTime m_pauseEnd;

	// Token: 0x0400117B RID: 4475
	private static float m_pauseFrom;

	// Token: 0x0400117C RID: 4476
	private static float m_pauseTarget;

	// Token: 0x0400117D RID: 4477
	private static float m_timeScale = 1f;

	// Token: 0x0400117E RID: 4478
	private static float m_pauseRotateFade;

	// Token: 0x0400117F RID: 4479
	private static float m_pauseTimer;

	// Token: 0x04001180 RID: 4480
	private static float m_collectTimer;

	// Token: 0x04001181 RID: 4481
	private static bool m_pause;

	// Token: 0x04001182 RID: 4482
	private static string m_profileFilename = null;

	// Token: 0x04001183 RID: 4483
	private static FileHelpers.FileSource m_profileFileSource = FileHelpers.FileSource.Local;

	// Token: 0x04001184 RID: 4484
	private PlayerProfile m_playerProfile;

	// Token: 0x04001185 RID: 4485
	private bool m_requestRespawn;

	// Token: 0x04001186 RID: 4486
	private bool m_respawnAfterDeath;

	// Token: 0x04001187 RID: 4487
	private float m_respawnWait;

	// Token: 0x04001188 RID: 4488
	public float m_respawnLoadDuration = 8f;

	// Token: 0x04001189 RID: 4489
	public float m_fadeTimeDeath = 9.5f;

	// Token: 0x0400118A RID: 4490
	public float m_fadeTimeSleep = 3f;

	// Token: 0x0400118B RID: 4491
	private bool m_haveSpawned;

	// Token: 0x0400118C RID: 4492
	private bool m_firstSpawn = true;

	// Token: 0x0400118D RID: 4493
	private bool m_shuttingDown;

	// Token: 0x0400118E RID: 4494
	private bool m_inIntro;

	// Token: 0x0400118F RID: 4495
	private bool m_queuedIntro;

	// Token: 0x04001190 RID: 4496
	private Vector3 m_randomStartPoint = Vector3.zero;

	// Token: 0x04001191 RID: 4497
	private UnityEngine.Random.State m_spawnRandomState;

	// Token: 0x04001192 RID: 4498
	private List<ZoneSystem.LocationInstance> m_tempLocations = new List<ZoneSystem.LocationInstance>();

	// Token: 0x04001193 RID: 4499
	private double m_lastSleepTime;

	// Token: 0x04001194 RID: 4500
	private bool m_sleeping;

	// Token: 0x04001195 RID: 4501
	private List<Game.ConnectingPortals> m_currentlyConnectingPortals = new List<Game.ConnectingPortals>();

	// Token: 0x04001196 RID: 4502
	private const float m_collectResourcesInterval = 1200f;

	// Token: 0x04001197 RID: 4503
	private const float m_collectResourcesIntervalPeriodic = 3600f;

	// Token: 0x04001198 RID: 4504
	private DateTime m_lastCollectResources = DateTime.Now;

	// Token: 0x04001199 RID: 4505
	[NonSerialized]
	public float m_saveTimer;

	// Token: 0x0400119A RID: 4506
	public static float m_saveInterval = 1800f;

	// Token: 0x0400119B RID: 4507
	private const float m_preSaveWarning = 30f;

	// Token: 0x0400119C RID: 4508
	[Header("Intro")]
	public string m_introTopic = "";

	// Token: 0x0400119D RID: 4509
	[TextArea]
	public string m_introText = "";

	// Token: 0x0400119E RID: 4510
	[Header("Diffuculty scaling")]
	public float m_difficultyScaleRange = 100f;

	// Token: 0x0400119F RID: 4511
	public int m_difficultyScaleMaxPlayers = 5;

	// Token: 0x040011A0 RID: 4512
	public float m_damageScalePerPlayer = 0.04f;

	// Token: 0x040011A1 RID: 4513
	public float m_healthScalePerPlayer = 0.3f;

	// Token: 0x040011A2 RID: 4514
	private int m_forcePlayers;

	// Token: 0x040011A3 RID: 4515
	[Header("Misc")]
	public float m_ashDamage = 5f;

	// Token: 0x040011A4 RID: 4516
	public List<Game.ItemConversion> m_damageTypeDropConversions = new List<Game.ItemConversion>();

	// Token: 0x040011A5 RID: 4517
	[Header("World Level Rates")]
	public List<ItemDrop.ItemData.ItemType> m_nonScaledDropTypes = new List<ItemDrop.ItemData.ItemType>();

	// Token: 0x040011A6 RID: 4518
	public int m_worldLevelEnemyBaseAC = 100;

	// Token: 0x040011A7 RID: 4519
	public float m_worldLevelEnemyHPMultiplier = 2f;

	// Token: 0x040011A8 RID: 4520
	public int m_worldLevelEnemyBaseDamage = 85;

	// Token: 0x040011A9 RID: 4521
	public int m_worldLevelGearBaseAC = 38;

	// Token: 0x040011AA RID: 4522
	public int m_worldLevelGearBaseDamage = 120;

	// Token: 0x040011AB RID: 4523
	public float m_worldLevelEnemyLevelUpExponent = 1.15f;

	// Token: 0x040011AC RID: 4524
	public float m_worldLevelEnemyMoveSpeedMultiplier = 0.2f;

	// Token: 0x040011AD RID: 4525
	public int m_worldLevelPieceBaseDamage = 100;

	// Token: 0x040011AE RID: 4526
	public float m_worldLevelPieceHPMultiplier = 1f;

	// Token: 0x040011AF RID: 4527
	public float m_worldLevelMineHPMultiplier = 2f;

	// Token: 0x040011B0 RID: 4528
	public static float m_playerDamageRate = 1f;

	// Token: 0x040011B1 RID: 4529
	public static float m_enemyDamageRate = 1f;

	// Token: 0x040011B2 RID: 4530
	public static float m_enemyLevelUpRate = 1f;

	// Token: 0x040011B3 RID: 4531
	public static float m_localDamgeTakenRate = 1f;

	// Token: 0x040011B4 RID: 4532
	public static float m_resourceRate = 1f;

	// Token: 0x040011B5 RID: 4533
	public static float m_eventRate = 1f;

	// Token: 0x040011B6 RID: 4534
	public static float m_staminaRate = 1f;

	// Token: 0x040011B7 RID: 4535
	public static float m_moveStaminaRate = 1f;

	// Token: 0x040011B8 RID: 4536
	public static float m_staminaRegenRate = 1f;

	// Token: 0x040011B9 RID: 4537
	public static float m_skillGainRate = 1f;

	// Token: 0x040011BA RID: 4538
	public static float m_skillReductionRate = 1f;

	// Token: 0x040011BB RID: 4539
	public static float m_enemySpeedSize = 1f;

	// Token: 0x040011BC RID: 4540
	public static int m_worldLevel = 0;

	// Token: 0x040011BD RID: 4541
	public static string m_serverOptionsSummary = "";

	// Token: 0x040011BE RID: 4542
	public static bool m_noMap = false;

	// Token: 0x040011BF RID: 4543
	public const string m_keyDefaultString = "default";

	// Token: 0x0200031A RID: 794
	private struct ConnectingPortals
	{
		// Token: 0x040023DE RID: 9182
		public ZDO PortalA;

		// Token: 0x040023DF RID: 9183
		public ZDO PortalB;
	}

	// Token: 0x0200031B RID: 795
	[Serializable]
	public class ItemConversion
	{
		// Token: 0x040023E0 RID: 9184
		public HitData.DamageType m_damageType;

		// Token: 0x040023E1 RID: 9185
		public HitData.HitType m_hitType;

		// Token: 0x040023E2 RID: 9186
		public List<ItemDrop> m_items;

		// Token: 0x040023E3 RID: 9187
		public ItemDrop m_result;

		// Token: 0x040023E4 RID: 9188
		public int m_multiplier = 1;
	}
}
