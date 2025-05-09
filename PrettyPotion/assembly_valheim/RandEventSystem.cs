using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Rendering;

// Token: 0x0200012F RID: 303
public class RandEventSystem : MonoBehaviour
{
	// Token: 0x170000A5 RID: 165
	// (get) Token: 0x06001334 RID: 4916 RVA: 0x0008F049 File Offset: 0x0008D249
	public static RandEventSystem instance
	{
		get
		{
			return RandEventSystem.m_instance;
		}
	}

	// Token: 0x06001335 RID: 4917 RVA: 0x0008F050 File Offset: 0x0008D250
	private void Awake()
	{
		RandEventSystem.m_instance = this;
	}

	// Token: 0x06001336 RID: 4918 RVA: 0x0008F058 File Offset: 0x0008D258
	private void OnDestroy()
	{
		RandEventSystem.m_instance = null;
	}

	// Token: 0x06001337 RID: 4919 RVA: 0x0008F060 File Offset: 0x0008D260
	private void Start()
	{
		ZRoutedRpc.instance.Register<string, float, Vector3>("SetEvent", new Action<long, string, float, Vector3>(this.RPC_SetEvent));
	}

	// Token: 0x06001338 RID: 4920 RVA: 0x0008F080 File Offset: 0x0008D280
	private void FixedUpdate()
	{
		float fixedDeltaTime = Time.fixedDeltaTime;
		this.UpdateForcedEvents(fixedDeltaTime);
		this.UpdateRandomEvent(fixedDeltaTime);
		if (this.m_forcedEvent != null)
		{
			this.m_forcedEvent.Update(ZNet.instance.IsServer(), this.m_forcedEvent == this.m_activeEvent, true, fixedDeltaTime);
		}
		if (this.m_randomEvent != null && ZNet.instance.IsServer())
		{
			if (ZNet.instance.IsServer())
			{
				bool playerInArea = this.IsAnyPlayerInEventArea(this.m_randomEvent);
				if (this.m_randomEvent.Update(true, this.m_randomEvent == this.m_activeEvent, playerInArea, fixedDeltaTime))
				{
					this.SetRandomEvent(null, Vector3.zero);
				}
			}
			else
			{
				this.m_randomEvent.Update(ZNet.instance.IsServer(), this.m_randomEvent == this.m_activeEvent, true, fixedDeltaTime);
			}
		}
		if (this.m_forcedEvent != null)
		{
			this.SetActiveEvent(this.m_forcedEvent, false);
			return;
		}
		if (this.m_randomEvent == null || !Player.m_localPlayer)
		{
			this.SetActiveEvent(null, false);
			return;
		}
		if (this.IsInsideRandomEventArea(this.m_randomEvent, Player.m_localPlayer.transform.position))
		{
			this.SetActiveEvent(this.m_randomEvent, false);
			return;
		}
		this.SetActiveEvent(null, false);
	}

	// Token: 0x06001339 RID: 4921 RVA: 0x0008F1B4 File Offset: 0x0008D3B4
	private bool IsInsideRandomEventArea(RandomEvent re, Vector3 position)
	{
		return position.y <= 3000f && Utils.DistanceXZ(position, re.m_pos) < re.m_eventRange;
	}

	// Token: 0x0600133A RID: 4922 RVA: 0x0008F1DC File Offset: 0x0008D3DC
	private void UpdateRandomEvent(float dt)
	{
		if (ZNet.instance.IsServer())
		{
			this.m_eventTimer += dt;
			if (Game.m_eventRate > 0f && this.m_eventTimer > this.m_eventIntervalMin * 60f * Game.m_eventRate)
			{
				this.m_eventTimer = 0f;
				if (UnityEngine.Random.Range(0f, 100f) <= this.m_eventChance / Game.m_eventRate)
				{
					this.StartRandomEvent();
				}
			}
			if (RandEventSystem.s_randomEventNeedsRefresh)
			{
				RandEventSystem.RefreshPlayerEventData();
			}
			Player.GetAllPlayers();
			foreach (RandomEvent randomEvent in this.m_events)
			{
				if (randomEvent.m_enabled && randomEvent.m_standaloneInterval > 0f && this.m_activeEvent != randomEvent)
				{
					randomEvent.m_time += dt;
					if (randomEvent.m_time > randomEvent.m_standaloneInterval * Game.m_eventRate)
					{
						if (this.HaveGlobalKeys(randomEvent, RandEventSystem.s_playerEventDatas))
						{
							List<Vector3> validEventPoints = this.GetValidEventPoints(randomEvent, RandEventSystem.s_playerEventDatas);
							if (validEventPoints.Count > 0 && UnityEngine.Random.Range(0f, 100f) <= randomEvent.m_standaloneChance / Game.m_eventRate)
							{
								this.SetRandomEvent(randomEvent, validEventPoints[UnityEngine.Random.Range(0, validEventPoints.Count)]);
							}
						}
						randomEvent.m_time = 0f;
					}
				}
			}
			this.m_sendTimer += dt;
			if (this.m_sendTimer > 2f)
			{
				this.m_sendTimer = 0f;
				this.SendCurrentRandomEvent();
			}
		}
	}

	// Token: 0x0600133B RID: 4923 RVA: 0x0008F38C File Offset: 0x0008D58C
	private void UpdateForcedEvents(float dt)
	{
		this.m_forcedEventUpdateTimer += dt;
		if (this.m_forcedEventUpdateTimer > 2f)
		{
			this.m_forcedEventUpdateTimer = 0f;
			string forcedEvent = this.GetForcedEvent();
			this.SetForcedEvent(forcedEvent);
		}
	}

	// Token: 0x0600133C RID: 4924 RVA: 0x0008F3D0 File Offset: 0x0008D5D0
	private void SetForcedEvent(string name)
	{
		if (this.m_forcedEvent != null && name != null && this.m_forcedEvent.m_name == name)
		{
			return;
		}
		if (this.m_forcedEvent != null)
		{
			if (this.m_forcedEvent == this.m_activeEvent)
			{
				this.SetActiveEvent(null, true);
			}
			this.m_forcedEvent.OnStop();
			this.m_forcedEvent = null;
		}
		RandomEvent @event = this.GetEvent(name);
		if (@event != null)
		{
			this.m_forcedEvent = @event.Clone();
			this.m_forcedEvent.OnStart();
		}
	}

	// Token: 0x0600133D RID: 4925 RVA: 0x0008F450 File Offset: 0x0008D650
	private string GetForcedEvent()
	{
		if (EnemyHud.instance != null)
		{
			Character activeBoss = EnemyHud.instance.GetActiveBoss();
			if (activeBoss != null && activeBoss.m_bossEvent.Length > 0)
			{
				return activeBoss.m_bossEvent;
			}
			string @event = EventZone.GetEvent();
			if (@event != null)
			{
				return @event;
			}
		}
		return null;
	}

	// Token: 0x0600133E RID: 4926 RVA: 0x0008F4A0 File Offset: 0x0008D6A0
	public string GetBossEvent()
	{
		if (EnemyHud.instance != null)
		{
			Character activeBoss = EnemyHud.instance.GetActiveBoss();
			if (activeBoss != null && activeBoss.m_bossEvent.Length > 0)
			{
				return activeBoss.m_bossEvent;
			}
		}
		return null;
	}

	// Token: 0x0600133F RID: 4927 RVA: 0x0008F4E0 File Offset: 0x0008D6E0
	private void SendCurrentRandomEvent()
	{
		if (this.m_randomEvent != null)
		{
			ZRoutedRpc.instance.InvokeRoutedRPC(ZRoutedRpc.Everybody, "SetEvent", new object[]
			{
				this.m_randomEvent.m_name,
				this.m_randomEvent.m_time,
				this.m_randomEvent.m_pos
			});
			return;
		}
		ZRoutedRpc.instance.InvokeRoutedRPC(ZRoutedRpc.Everybody, "SetEvent", new object[]
		{
			"",
			0f,
			Vector3.zero
		});
	}

	// Token: 0x06001340 RID: 4928 RVA: 0x0008F580 File Offset: 0x0008D780
	private void RPC_SetEvent(long sender, string eventName, float time, Vector3 pos)
	{
		if (ZNet.instance.IsServer())
		{
			return;
		}
		if (this.m_randomEvent == null || this.m_randomEvent.m_name != eventName)
		{
			this.SetRandomEventByName(eventName, pos);
		}
		if (this.m_randomEvent != null)
		{
			this.m_randomEvent.m_time = time;
			this.m_randomEvent.m_pos = pos;
		}
	}

	// Token: 0x06001341 RID: 4929 RVA: 0x0008F5DF File Offset: 0x0008D7DF
	public void ConsoleStartRandomEvent()
	{
		if (ZNet.instance.IsServer())
		{
			this.StartRandomEvent();
			return;
		}
		ZRoutedRpc.instance.InvokeRoutedRPC("startrandomevent", Array.Empty<object>());
	}

	// Token: 0x06001342 RID: 4930 RVA: 0x0008F608 File Offset: 0x0008D808
	private void RPC_ConsoleStartRandomEvent(long sender)
	{
		ZNetPeer peer = ZNet.instance.GetPeer(sender);
		if (!ZNet.instance.IsAdmin(peer.m_socket.GetHostName()))
		{
			ZNet.instance.RemotePrint(peer.m_rpc, "You are not admin");
			return;
		}
		this.StartRandomEvent();
	}

	// Token: 0x06001343 RID: 4931 RVA: 0x0008F654 File Offset: 0x0008D854
	public void ConsoleResetRandomEvent()
	{
		if (ZNet.instance.IsServer())
		{
			this.ResetRandomEvent();
			return;
		}
		ZRoutedRpc.instance.InvokeRoutedRPC("resetrandomevent", Array.Empty<object>());
	}

	// Token: 0x06001344 RID: 4932 RVA: 0x0008F680 File Offset: 0x0008D880
	private void RPC_ConsoleResetRandomEvent(long sender)
	{
		ZNetPeer peer = ZNet.instance.GetPeer(sender);
		if (!ZNet.instance.IsAdmin(peer.m_socket.GetHostName()))
		{
			ZNet.instance.RemotePrint(peer.m_rpc, "You are not admin");
			return;
		}
		this.ResetRandomEvent();
	}

	// Token: 0x06001345 RID: 4933 RVA: 0x0008F6CC File Offset: 0x0008D8CC
	public void StartRandomEvent()
	{
		if (!ZNet.instance.IsServer())
		{
			return;
		}
		List<KeyValuePair<RandomEvent, Vector3>> possibleRandomEvents = this.GetPossibleRandomEvents();
		Terminal.Log(string.Format("Possible events: {0}", possibleRandomEvents.Count));
		if (possibleRandomEvents.Count == 0)
		{
			return;
		}
		foreach (KeyValuePair<RandomEvent, Vector3> keyValuePair in possibleRandomEvents)
		{
			Terminal.Log("  " + keyValuePair.Key.m_name);
		}
		KeyValuePair<RandomEvent, Vector3> keyValuePair2 = possibleRandomEvents[UnityEngine.Random.Range(0, possibleRandomEvents.Count)];
		this.SetRandomEvent(keyValuePair2.Key, keyValuePair2.Value);
		Terminal.Log("Starting event: " + keyValuePair2.Key.m_name);
	}

	// Token: 0x06001346 RID: 4934 RVA: 0x0008F7A8 File Offset: 0x0008D9A8
	private RandomEvent GetEvent(string name)
	{
		if (string.IsNullOrEmpty(name))
		{
			return null;
		}
		foreach (RandomEvent randomEvent in this.m_events)
		{
			if (randomEvent.m_name == name && randomEvent.m_enabled)
			{
				return randomEvent;
			}
		}
		return null;
	}

	// Token: 0x06001347 RID: 4935 RVA: 0x0008F81C File Offset: 0x0008DA1C
	public void SetRandomEventByName(string name, Vector3 pos)
	{
		RandomEvent @event = this.GetEvent(name);
		this.SetRandomEvent(@event, pos);
	}

	// Token: 0x06001348 RID: 4936 RVA: 0x0008F839 File Offset: 0x0008DA39
	public void ResetRandomEvent()
	{
		this.SetRandomEvent(null, Vector3.zero);
	}

	// Token: 0x06001349 RID: 4937 RVA: 0x0008F847 File Offset: 0x0008DA47
	public bool HaveEvent(string name)
	{
		return this.GetEvent(name) != null;
	}

	// Token: 0x0600134A RID: 4938 RVA: 0x0008F854 File Offset: 0x0008DA54
	private void SetRandomEvent(RandomEvent ev, Vector3 pos)
	{
		if (this.m_randomEvent != null)
		{
			if (this.m_randomEvent == this.m_activeEvent)
			{
				this.SetActiveEvent(null, true);
			}
			this.m_randomEvent.OnStop();
			this.m_randomEvent = null;
		}
		if (ev != null)
		{
			this.m_randomEvent = ev.Clone();
			this.m_randomEvent.m_pos = pos;
			this.m_randomEvent.OnStart();
			ZLog.Log("Random event set:" + ev.m_name);
			if (Player.m_localPlayer)
			{
				Player.m_localPlayer.ShowTutorial("randomevent", false);
			}
		}
		if (ZNet.instance.IsServer())
		{
			this.SendCurrentRandomEvent();
		}
	}

	// Token: 0x0600134B RID: 4939 RVA: 0x0008F8FC File Offset: 0x0008DAFC
	private bool IsAnyPlayerInEventArea(RandomEvent re)
	{
		foreach (ZDO zdo in ZNet.instance.GetAllCharacterZDOS())
		{
			if (this.IsInsideRandomEventArea(re, zdo.GetPosition()))
			{
				return true;
			}
		}
		return false;
	}

	// Token: 0x0600134C RID: 4940 RVA: 0x0008F964 File Offset: 0x0008DB64
	private List<KeyValuePair<RandomEvent, Vector3>> GetPossibleRandomEvents()
	{
		this.m_lastPossibleEvents.Clear();
		RandEventSystem.RefreshPlayerEventData();
		foreach (RandomEvent randomEvent in this.m_events)
		{
			if (randomEvent.m_enabled && randomEvent.m_random && this.HaveGlobalKeys(randomEvent, RandEventSystem.s_playerEventDatas))
			{
				List<Vector3> validEventPoints = this.GetValidEventPoints(randomEvent, RandEventSystem.s_playerEventDatas);
				if (validEventPoints.Count != 0)
				{
					Vector3 value = validEventPoints[UnityEngine.Random.Range(0, validEventPoints.Count)];
					this.m_lastPossibleEvents.Add(new KeyValuePair<RandomEvent, Vector3>(randomEvent, value));
				}
			}
		}
		return this.m_lastPossibleEvents;
	}

	// Token: 0x0600134D RID: 4941 RVA: 0x0008FA20 File Offset: 0x0008DC20
	private List<Vector3> GetValidEventPoints(RandomEvent ev, List<RandEventSystem.PlayerEventData> characters)
	{
		this.points.Clear();
		bool globalKey = ZoneSystem.instance.GetGlobalKey(GlobalKeys.PlayerEvents);
		foreach (RandEventSystem.PlayerEventData playerEventData in characters)
		{
			if (this.InValidBiome(ev, playerEventData.position) && this.CheckBase(ev, playerEventData) && playerEventData.position.y <= 3000f && (!globalKey || playerEventData.possibleEvents.Contains(ev.m_name)))
			{
				this.points.Add(playerEventData.position);
			}
		}
		return this.points;
	}

	// Token: 0x0600134E RID: 4942 RVA: 0x0008FAD8 File Offset: 0x0008DCD8
	private static void RefreshPlayerEventData()
	{
		RandEventSystem.s_randomEventNeedsRefresh = false;
		RandEventSystem.s_playerEventDatas.Clear();
		RandEventSystem.PlayerEventData item;
		if (SystemInfo.graphicsDeviceType != GraphicsDeviceType.Null && RandEventSystem.RetrievePlayerEventData(ZNet.instance.m_serverSyncedPlayerData, ZNet.instance.GetReferencePosition(), out item))
		{
			RandEventSystem.s_playerEventDatas.Add(item);
		}
		foreach (ZNetPeer znetPeer in ZNet.instance.GetPeers())
		{
			RandEventSystem.PlayerEventData item2;
			if (znetPeer.IsReady() && RandEventSystem.RetrievePlayerEventData(znetPeer.m_serverSyncedPlayerData, znetPeer.m_refPos, out item2))
			{
				RandEventSystem.s_playerEventDatas.Add(item2);
			}
		}
	}

	// Token: 0x0600134F RID: 4943 RVA: 0x0008FB90 File Offset: 0x0008DD90
	public static void SetRandomEventsNeedsRefresh()
	{
		RandEventSystem.s_randomEventNeedsRefresh = true;
	}

	// Token: 0x06001350 RID: 4944 RVA: 0x0008FB98 File Offset: 0x0008DD98
	private static bool RetrievePlayerEventData(Dictionary<string, string> playerData, Vector3 position, out RandEventSystem.PlayerEventData eventData)
	{
		eventData = default(RandEventSystem.PlayerEventData);
		string text;
		if (!playerData.TryGetValue("possibleEvents", out text))
		{
			return false;
		}
		string s;
		if (!playerData.TryGetValue("baseValue", out s) || !int.TryParse(s, out eventData.baseValue))
		{
			return false;
		}
		eventData.position = position;
		eventData.possibleEvents = new HashSet<string>();
		string[] array = text.Split(',', StringSplitOptions.None);
		for (int i = 0; i < array.Length; i++)
		{
			eventData.possibleEvents.Add(array[i]);
		}
		return true;
	}

	// Token: 0x06001351 RID: 4945 RVA: 0x0008FC16 File Offset: 0x0008DE16
	private bool InValidBiome(RandomEvent ev, Vector3 point)
	{
		return ev.m_biome == Heightmap.Biome.None || (WorldGenerator.instance.GetBiome(point) & ev.m_biome) != Heightmap.Biome.None;
	}

	// Token: 0x06001352 RID: 4946 RVA: 0x0008FC39 File Offset: 0x0008DE39
	private bool CheckBase(RandomEvent ev, RandEventSystem.PlayerEventData player)
	{
		return !ev.m_nearBaseOnly || player.baseValue >= 3;
	}

	// Token: 0x06001353 RID: 4947 RVA: 0x0008FC54 File Offset: 0x0008DE54
	private bool HaveGlobalKeys(RandomEvent ev, List<RandEventSystem.PlayerEventData> players)
	{
		if (ZoneSystem.instance.GetGlobalKey(GlobalKeys.PlayerEvents) && (ev.m_altRequiredKnownItems.Count > 0 || ev.m_altRequiredNotKnownItems.Count > 0 || ev.m_altNotRequiredPlayerKeys.Count > 0 || ev.m_altRequiredPlayerKeysAny.Count > 0 || ev.m_altRequiredPlayerKeysAll.Count > 0))
		{
			using (List<RandEventSystem.PlayerEventData>.Enumerator enumerator = players.GetEnumerator())
			{
				while (enumerator.MoveNext())
				{
					if (enumerator.Current.possibleEvents.Contains(ev.m_name))
					{
						return true;
					}
				}
			}
			return false;
		}
		foreach (string name in ev.m_requiredGlobalKeys)
		{
			if (!ZoneSystem.instance.GetGlobalKey(name))
			{
				return false;
			}
		}
		foreach (string name2 in ev.m_notRequiredGlobalKeys)
		{
			if (ZoneSystem.instance.GetGlobalKey(name2))
			{
				return false;
			}
		}
		return true;
	}

	// Token: 0x06001354 RID: 4948 RVA: 0x0008FDAC File Offset: 0x0008DFAC
	public bool PlayerIsReadyForEvent(Player player, RandomEvent ev)
	{
		foreach (ItemDrop itemDrop in ev.m_altRequiredNotKnownItems)
		{
			if (player.IsMaterialKnown(itemDrop.m_itemData.m_shared.m_name))
			{
				return false;
			}
		}
		foreach (string name in ev.m_altNotRequiredPlayerKeys)
		{
			if (player.HaveUniqueKey(name))
			{
				return false;
			}
		}
		foreach (ItemDrop itemDrop2 in ev.m_altRequiredKnownItems)
		{
			if (player.IsMaterialKnown(itemDrop2.m_itemData.m_shared.m_name))
			{
				return true;
			}
		}
		foreach (string name2 in ev.m_altRequiredPlayerKeysAny)
		{
			if (player.HaveUniqueKey(name2))
			{
				return true;
			}
		}
		foreach (string name3 in ev.m_altRequiredPlayerKeysAll)
		{
			if (!player.HaveUniqueKey(name3))
			{
				return false;
			}
		}
		return ev.m_altRequiredKnownItems.Count <= 0 && ev.m_altRequiredPlayerKeysAny.Count <= 0;
	}

	// Token: 0x06001355 RID: 4949 RVA: 0x0008FF78 File Offset: 0x0008E178
	public List<SpawnSystem.SpawnData> GetCurrentSpawners()
	{
		if (this.m_activeEvent != null && this.m_activeEvent.m_time > this.m_activeEvent.m_spawnerDelay)
		{
			return this.m_activeEvent.m_spawn;
		}
		return null;
	}

	// Token: 0x06001356 RID: 4950 RVA: 0x0008FFA7 File Offset: 0x0008E1A7
	public string GetEnvOverride()
	{
		if (this.m_activeEvent != null && !string.IsNullOrEmpty(this.m_activeEvent.m_forceEnvironment) && this.m_activeEvent.InEventBiome())
		{
			return this.m_activeEvent.m_forceEnvironment;
		}
		return null;
	}

	// Token: 0x06001357 RID: 4951 RVA: 0x0008FFDD File Offset: 0x0008E1DD
	public string GetMusicOverride()
	{
		if (this.m_activeEvent != null && !string.IsNullOrEmpty(this.m_activeEvent.m_forceMusic))
		{
			return this.m_activeEvent.m_forceMusic;
		}
		return null;
	}

	// Token: 0x06001358 RID: 4952 RVA: 0x00090008 File Offset: 0x0008E208
	private void SetActiveEvent(RandomEvent ev, bool end = false)
	{
		if (ev != null && this.m_activeEvent != null && ev.m_name == this.m_activeEvent.m_name)
		{
			return;
		}
		if (this.m_activeEvent != null)
		{
			this.m_activeEvent.OnDeactivate(end);
			this.m_activeEvent = null;
		}
		if (ev != null)
		{
			this.m_activeEvent = ev;
			if (this.m_activeEvent != null)
			{
				this.m_activeEvent.OnActivate();
			}
		}
	}

	// Token: 0x06001359 RID: 4953 RVA: 0x00090071 File Offset: 0x0008E271
	public static bool InEvent()
	{
		return !(RandEventSystem.m_instance == null) && RandEventSystem.m_instance.m_activeEvent != null;
	}

	// Token: 0x0600135A RID: 4954 RVA: 0x0009008F File Offset: 0x0008E28F
	public static bool HaveActiveEvent()
	{
		return !(RandEventSystem.m_instance == null) && (RandEventSystem.m_instance.m_activeEvent != null || RandEventSystem.m_instance.m_randomEvent != null || RandEventSystem.m_instance.m_activeEvent != null);
	}

	// Token: 0x0600135B RID: 4955 RVA: 0x000900C9 File Offset: 0x0008E2C9
	public RandomEvent GetCurrentRandomEvent()
	{
		return this.m_randomEvent;
	}

	// Token: 0x0600135C RID: 4956 RVA: 0x000900D1 File Offset: 0x0008E2D1
	public RandomEvent GetActiveEvent()
	{
		return this.m_activeEvent;
	}

	// Token: 0x0600135D RID: 4957 RVA: 0x000900DC File Offset: 0x0008E2DC
	public void PrepareSave()
	{
		this.m_tempSaveEventTimer = this.m_eventTimer;
		if (this.m_randomEvent != null)
		{
			this.m_tempSaveRandomEvent = this.m_randomEvent.m_name;
			this.m_tempSaveRandomEventTime = this.m_randomEvent.m_time;
			this.m_tempSaveRandomEventPos = this.m_randomEvent.m_pos;
			return;
		}
		this.m_tempSaveRandomEvent = "";
		this.m_tempSaveRandomEventTime = 0f;
		this.m_tempSaveRandomEventPos = Vector3.zero;
	}

	// Token: 0x0600135E RID: 4958 RVA: 0x00090154 File Offset: 0x0008E354
	public void SaveAsync(BinaryWriter writer)
	{
		writer.Write(this.m_tempSaveEventTimer);
		writer.Write(this.m_tempSaveRandomEvent);
		writer.Write(this.m_tempSaveRandomEventTime);
		writer.Write(this.m_tempSaveRandomEventPos.x);
		writer.Write(this.m_tempSaveRandomEventPos.y);
		writer.Write(this.m_tempSaveRandomEventPos.z);
	}

	// Token: 0x0600135F RID: 4959 RVA: 0x000901B8 File Offset: 0x0008E3B8
	public void Load(BinaryReader reader, int version)
	{
		this.m_eventTimer = reader.ReadSingle();
		if (version >= 25)
		{
			string text = reader.ReadString();
			float time = reader.ReadSingle();
			Vector3 pos;
			pos.x = reader.ReadSingle();
			pos.y = reader.ReadSingle();
			pos.z = reader.ReadSingle();
			if (!string.IsNullOrEmpty(text))
			{
				this.SetRandomEventByName(text, pos);
				if (this.m_randomEvent != null)
				{
					this.m_randomEvent.m_time = time;
					this.m_randomEvent.m_pos = pos;
				}
			}
		}
	}

	// Token: 0x04001338 RID: 4920
	private List<Vector3> points = new List<Vector3>();

	// Token: 0x04001339 RID: 4921
	private static RandEventSystem m_instance;

	// Token: 0x0400133A RID: 4922
	private List<KeyValuePair<RandomEvent, Vector3>> m_lastPossibleEvents = new List<KeyValuePair<RandomEvent, Vector3>>();

	// Token: 0x0400133B RID: 4923
	private static List<RandEventSystem.PlayerEventData> s_playerEventDatas = new List<RandEventSystem.PlayerEventData>();

	// Token: 0x0400133C RID: 4924
	private static bool s_randomEventNeedsRefresh = true;

	// Token: 0x0400133D RID: 4925
	public float m_eventIntervalMin = 1f;

	// Token: 0x0400133E RID: 4926
	public float m_eventChance = 25f;

	// Token: 0x0400133F RID: 4927
	private float m_eventTimer;

	// Token: 0x04001340 RID: 4928
	private float m_sendTimer;

	// Token: 0x04001341 RID: 4929
	public List<RandomEvent> m_events = new List<RandomEvent>();

	// Token: 0x04001342 RID: 4930
	private RandomEvent m_randomEvent;

	// Token: 0x04001343 RID: 4931
	private float m_forcedEventUpdateTimer;

	// Token: 0x04001344 RID: 4932
	private RandomEvent m_forcedEvent;

	// Token: 0x04001345 RID: 4933
	private RandomEvent m_activeEvent;

	// Token: 0x04001346 RID: 4934
	private float m_tempSaveEventTimer;

	// Token: 0x04001347 RID: 4935
	private string m_tempSaveRandomEvent;

	// Token: 0x04001348 RID: 4936
	private float m_tempSaveRandomEventTime;

	// Token: 0x04001349 RID: 4937
	private Vector3 m_tempSaveRandomEventPos;

	// Token: 0x0400134A RID: 4938
	public const string PossibleEventsKey = "possibleEvents";

	// Token: 0x02000329 RID: 809
	public struct PlayerEventData
	{
		// Token: 0x0400242A RID: 9258
		public Vector3 position;

		// Token: 0x0400242B RID: 9259
		public HashSet<string> possibleEvents;

		// Token: 0x0400242C RID: 9260
		public int baseValue;
	}
}
