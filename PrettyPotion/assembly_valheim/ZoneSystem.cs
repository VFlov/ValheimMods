using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using SoftReferenceableAssets;
using UnityEngine;

// Token: 0x02000154 RID: 340
public class ZoneSystem : MonoBehaviour
{
	// Token: 0x170000BC RID: 188
	// (get) Token: 0x0600148B RID: 5259 RVA: 0x00096348 File Offset: 0x00094548
	public static ZoneSystem instance
	{
		get
		{
			return ZoneSystem.m_instance;
		}
	}

	// Token: 0x0600148C RID: 5260 RVA: 0x00096350 File Offset: 0x00094550
	private ZoneSystem()
	{
	}

	// Token: 0x0600148D RID: 5261 RVA: 0x000964D0 File Offset: 0x000946D0
	private void Awake()
	{
		ZoneSystem.m_instance = this;
		this.m_terrainRayMask = LayerMask.GetMask(new string[]
		{
			"terrain"
		});
		this.m_blockRayMask = LayerMask.GetMask(new string[]
		{
			"Default",
			"static_solid",
			"Default_small",
			"piece"
		});
		this.m_solidRayMask = LayerMask.GetMask(new string[]
		{
			"Default",
			"static_solid",
			"Default_small",
			"piece",
			"terrain"
		});
		this.m_staticSolidRayMask = LayerMask.GetMask(new string[]
		{
			"static_solid",
			"terrain"
		});
		foreach (GameObject original in this.m_locationLists)
		{
			UnityEngine.Object.Instantiate<GameObject>(original);
		}
		ZLog.Log("Zonesystem Awake " + Time.frameCount.ToString());
	}

	// Token: 0x0600148E RID: 5262 RVA: 0x000965E8 File Offset: 0x000947E8
	private void Start()
	{
		ZLog.Log("Zonesystem Start " + Time.frameCount.ToString());
		this.UpdateWorldRates();
		this.SetupLocations();
		this.ValidateVegetation();
		ZRoutedRpc instance = ZRoutedRpc.instance;
		instance.m_onNewPeer = (Action<long>)Delegate.Combine(instance.m_onNewPeer, new Action<long>(this.OnNewPeer));
		if (ZNet.instance.IsServer())
		{
			ZRoutedRpc.instance.Register<string>("SetGlobalKey", new Action<long, string>(this.RPC_SetGlobalKey));
			ZRoutedRpc.instance.Register<string>("RemoveGlobalKey", new Action<long, string>(this.RPC_RemoveGlobalKey));
		}
		else
		{
			ZRoutedRpc.instance.Register<List<string>>("GlobalKeys", new Action<long, List<string>>(this.RPC_GlobalKeys));
			ZRoutedRpc.instance.Register<ZPackage>("LocationIcons", new Action<long, ZPackage>(this.RPC_LocationIcons));
		}
		this.m_startTime = (this.m_lastFixedTime = Time.fixedTime);
	}

	// Token: 0x0600148F RID: 5263 RVA: 0x000966D7 File Offset: 0x000948D7
	public void GenerateLocationsIfNeeded()
	{
		if (!this.LocationsGenerated)
		{
			this.GenerateLocations();
		}
	}

	// Token: 0x06001490 RID: 5264 RVA: 0x000966E8 File Offset: 0x000948E8
	private void SendGlobalKeys(long peer)
	{
		List<string> list = new List<string>(this.m_globalKeys);
		ZRoutedRpc.instance.InvokeRoutedRPC(peer, "GlobalKeys", new object[]
		{
			list
		});
		Player localPlayer = Player.m_localPlayer;
		if (localPlayer == null)
		{
			return;
		}
		localPlayer.UpdateEvents();
	}

	// Token: 0x06001491 RID: 5265 RVA: 0x0009672C File Offset: 0x0009492C
	private void RPC_GlobalKeys(long sender, List<string> keys)
	{
		ZLog.Log("client got keys " + keys.Count.ToString());
		this.ClearGlobalKeys();
		foreach (string keyStr in keys)
		{
			this.GlobalKeyAdd(keyStr, true);
		}
	}

	// Token: 0x06001492 RID: 5266 RVA: 0x000967A0 File Offset: 0x000949A0
	private void GlobalKeyAdd(string keyStr, bool canSaveToServerOptionKeys = true)
	{
		string text;
		GlobalKeys globalKeys;
		string keyValue = ZoneSystem.GetKeyValue(keyStr.ToLower(), out text, out globalKeys);
		bool flag = canSaveToServerOptionKeys && ZNet.World != null && globalKeys < GlobalKeys.NonServerOption;
		string str;
		if (this.m_globalKeysValues.TryGetValue(keyValue, out str))
		{
			string item = (keyValue + " " + str).TrimEnd();
			this.m_globalKeys.Remove(item);
			if (flag)
			{
				ZNet.World.m_startingGlobalKeys.Remove(item);
			}
		}
		string text2 = (keyValue + " " + text).TrimEnd();
		this.m_globalKeys.Add(text2);
		this.m_globalKeysValues[keyValue] = text;
		if (globalKeys != GlobalKeys.NonServerOption)
		{
			this.m_globalKeysEnums.Add(globalKeys);
		}
		Game.instance.GetPlayerProfile().m_knownWorldKeys.IncrementOrSet(text2, 1f);
		if (flag)
		{
			ZNet.World.m_startingGlobalKeys.Add(keyStr.ToLower());
		}
		this.UpdateWorldRates();
	}

	// Token: 0x06001493 RID: 5267 RVA: 0x00096890 File Offset: 0x00094A90
	private bool GlobalKeyRemove(string keyStr, bool canSaveToServerOptionKeys = true)
	{
		string text;
		GlobalKeys globalKeys;
		string keyValue = ZoneSystem.GetKeyValue(keyStr, out text, out globalKeys);
		string str;
		if (this.m_globalKeysValues.TryGetValue(keyValue, out str))
		{
			string item = (keyValue + " " + str).TrimEnd();
			if (canSaveToServerOptionKeys && ZNet.World != null && globalKeys < GlobalKeys.NonServerOption)
			{
				ZNet.World.m_startingGlobalKeys.Remove(item);
			}
			this.m_globalKeys.Remove(item);
			this.m_globalKeysValues.Remove(keyValue);
			if (globalKeys != GlobalKeys.NonServerOption)
			{
				this.m_globalKeysEnums.Remove(globalKeys);
			}
			this.UpdateWorldRates();
			return true;
		}
		return false;
	}

	// Token: 0x06001494 RID: 5268 RVA: 0x00096922 File Offset: 0x00094B22
	public void UpdateWorldRates()
	{
		Game.UpdateWorldRates(this.m_globalKeys, this.m_globalKeysValues);
	}

	// Token: 0x06001495 RID: 5269 RVA: 0x00096935 File Offset: 0x00094B35
	public void Reset()
	{
		this.ClearGlobalKeys();
		this.UpdateWorldRates();
	}

	// Token: 0x06001496 RID: 5270 RVA: 0x00096943 File Offset: 0x00094B43
	private void ClearGlobalKeys()
	{
		this.m_globalKeys.Clear();
		this.m_globalKeysEnums.Clear();
		this.m_globalKeysValues.Clear();
	}

	// Token: 0x06001497 RID: 5271 RVA: 0x00096968 File Offset: 0x00094B68
	public static string GetKeyValue(string key, out string value, out GlobalKeys gk)
	{
		int num = key.IndexOf(' ');
		value = "";
		string text;
		if (num > 0)
		{
			value = key.Substring(num + 1);
			text = key.Substring(0, num).ToLower();
		}
		else
		{
			text = key.ToLower();
		}
		if (!Enum.TryParse<GlobalKeys>(text, true, out gk))
		{
			gk = GlobalKeys.NonServerOption;
		}
		return text;
	}

	// Token: 0x06001498 RID: 5272 RVA: 0x000969BC File Offset: 0x00094BBC
	private void SendLocationIcons(long peer)
	{
		ZPackage zpackage = new ZPackage();
		this.tempIconList.Clear();
		this.GetLocationIcons(this.tempIconList);
		zpackage.Write(this.tempIconList.Count);
		foreach (KeyValuePair<Vector3, string> keyValuePair in this.tempIconList)
		{
			zpackage.Write(keyValuePair.Key);
			zpackage.Write(keyValuePair.Value);
		}
		ZRoutedRpc.instance.InvokeRoutedRPC(peer, "LocationIcons", new object[]
		{
			zpackage
		});
	}

	// Token: 0x06001499 RID: 5273 RVA: 0x00096A6C File Offset: 0x00094C6C
	private void RPC_LocationIcons(long sender, ZPackage pkg)
	{
		ZLog.Log("client got location icons");
		this.m_locationIcons.Clear();
		int num = pkg.ReadInt();
		for (int i = 0; i < num; i++)
		{
			Vector3 key = pkg.ReadVector3();
			string value = pkg.ReadString();
			this.m_locationIcons[key] = value;
		}
		ZLog.Log("Icons:" + num.ToString());
	}

	// Token: 0x0600149A RID: 5274 RVA: 0x00096AD2 File Offset: 0x00094CD2
	private void OnNewPeer(long peerID)
	{
		if (ZNet.instance.IsServer())
		{
			ZLog.Log("Server: New peer connected,sending global keys");
			this.SendGlobalKeys(peerID);
			this.SendLocationIcons(peerID);
		}
	}

	// Token: 0x0600149B RID: 5275 RVA: 0x00096AF8 File Offset: 0x00094CF8
	private void SetupLocations()
	{
		List<LocationList> allLocationLists = LocationList.GetAllLocationLists();
		allLocationLists.Sort((LocationList a, LocationList b) => a.m_sortOrder.CompareTo(b.m_sortOrder));
		foreach (LocationList locationList in allLocationLists)
		{
			this.m_locations.AddRange(locationList.m_locations);
			this.m_vegetation.AddRange(locationList.m_vegetation);
			foreach (EnvSetup env in locationList.m_environments)
			{
				EnvMan.instance.AppendEnvironment(env);
			}
			foreach (BiomeEnvSetup biomeEnv in locationList.m_biomeEnvironments)
			{
				EnvMan.instance.AppendBiomeSetup(biomeEnv);
			}
			ClutterSystem.instance.m_clutter.AddRange(locationList.m_clutter);
			ZLog.Log(string.Format("Added {0} locations, {1} vegetations, {2} environments, {3} biome env-setups, {4} clutter  from ", new object[]
			{
				locationList.m_locations.Count,
				locationList.m_vegetation.Count,
				locationList.m_environments.Count,
				locationList.m_biomeEnvironments.Count,
				locationList.m_clutter.Count
			}) + locationList.gameObject.scene.name);
			RandEventSystem.instance.m_events.AddRange(locationList.m_events);
		}
		foreach (ZoneSystem.ZoneLocation zoneLocation in this.m_locations)
		{
			if ((zoneLocation.m_enable || zoneLocation.m_prefab.IsValid) && Application.isPlaying)
			{
				zoneLocation.m_prefabName = zoneLocation.m_prefab.Name;
				int hash = zoneLocation.Hash;
				if (!this.m_locationsByHash.ContainsKey(hash))
				{
					this.m_locationsByHash.Add(hash, zoneLocation);
				}
			}
		}
		if (Settings.AssetMemoryUsagePolicy.HasFlag(AssetMemoryUsagePolicy.KeepAsynchronousLoadedBit))
		{
			ReferenceHolder referenceHolder = base.gameObject.AddComponent<ReferenceHolder>();
			foreach (ZoneSystem.ZoneLocation zoneLocation2 in this.m_locations)
			{
				if (zoneLocation2.m_enable)
				{
					zoneLocation2.m_prefab.Load();
					referenceHolder.HoldReferenceTo(zoneLocation2.m_prefab);
					zoneLocation2.m_prefab.Release();
				}
			}
		}
	}

	// Token: 0x0600149C RID: 5276 RVA: 0x00096E40 File Offset: 0x00095040
	public static void PrepareNetViews(GameObject root, List<ZNetView> views)
	{
		views.Clear();
		foreach (ZNetView znetView in root.GetComponentsInChildren<ZNetView>(true))
		{
			if (global::Utils.IsEnabledInheirarcy(znetView.gameObject, root))
			{
				views.Add(znetView);
			}
		}
	}

	// Token: 0x0600149D RID: 5277 RVA: 0x00096E84 File Offset: 0x00095084
	public static void PrepareRandomSpawns(GameObject root, List<RandomSpawn> randomSpawns)
	{
		randomSpawns.Clear();
		foreach (RandomSpawn randomSpawn in root.GetComponentsInChildren<RandomSpawn>(true))
		{
			if (global::Utils.IsEnabledInheirarcy(randomSpawn.gameObject, root))
			{
				randomSpawns.Add(randomSpawn);
				randomSpawn.Prepare();
			}
		}
	}

	// Token: 0x0600149E RID: 5278 RVA: 0x00096ECC File Offset: 0x000950CC
	private void OnDestroy()
	{
		this.ForceReleaseLoadedPrefabs();
		ZoneSystem.m_instance = null;
	}

	// Token: 0x0600149F RID: 5279 RVA: 0x00096EDC File Offset: 0x000950DC
	private void ValidateVegetation()
	{
		foreach (ZoneSystem.ZoneVegetation zoneVegetation in this.m_vegetation)
		{
			if (zoneVegetation.m_enable && zoneVegetation.m_prefab && zoneVegetation.m_prefab.GetComponent<ZNetView>() == null)
			{
				ZLog.LogError(string.Concat(new string[]
				{
					"Vegetation ",
					zoneVegetation.m_prefab.name,
					" [ ",
					zoneVegetation.m_name,
					"] is missing ZNetView"
				}));
			}
		}
	}

	// Token: 0x060014A0 RID: 5280 RVA: 0x00096F90 File Offset: 0x00095190
	public void PrepareSave()
	{
		this.m_tempGeneratedZonesSaveClone = new HashSet<Vector2i>(this.m_generatedZones);
		this.m_tempGlobalKeysSaveClone = new HashSet<string>(this.m_globalKeys);
		this.m_tempLocationsSaveClone = new List<ZoneSystem.LocationInstance>(this.m_locationInstances.Values);
		this.m_tempLocationsGeneratedSaveClone = this.LocationsGenerated;
	}

	// Token: 0x060014A1 RID: 5281 RVA: 0x00096FE4 File Offset: 0x000951E4
	public void SaveASync(BinaryWriter writer)
	{
		writer.Write(this.m_tempGeneratedZonesSaveClone.Count);
		foreach (Vector2i vector2i in this.m_tempGeneratedZonesSaveClone)
		{
			writer.Write(vector2i.x);
			writer.Write(vector2i.y);
		}
		writer.Write(0);
		writer.Write(this.m_locationVersion);
		this.m_tempGlobalKeysSaveClone.RemoveWhere(delegate(string x)
		{
			string text;
			GlobalKeys globalKeys;
			ZoneSystem.GetKeyValue(x, out text, out globalKeys);
			return globalKeys < GlobalKeys.NonServerOption;
		});
		writer.Write(this.m_tempGlobalKeysSaveClone.Count);
		foreach (string value in this.m_tempGlobalKeysSaveClone)
		{
			writer.Write(value);
		}
		writer.Write(this.m_tempLocationsGeneratedSaveClone);
		writer.Write(this.m_tempLocationsSaveClone.Count);
		foreach (ZoneSystem.LocationInstance locationInstance in this.m_tempLocationsSaveClone)
		{
			writer.Write(locationInstance.m_location.m_prefabName);
			writer.Write(locationInstance.m_position.x);
			writer.Write(locationInstance.m_position.y);
			writer.Write(locationInstance.m_position.z);
			writer.Write(locationInstance.m_placed);
		}
		this.m_tempGeneratedZonesSaveClone.Clear();
		this.m_tempGeneratedZonesSaveClone = null;
		this.m_tempGlobalKeysSaveClone.Clear();
		this.m_tempGlobalKeysSaveClone = null;
		this.m_tempLocationsSaveClone.Clear();
		this.m_tempLocationsSaveClone = null;
	}

	// Token: 0x060014A2 RID: 5282 RVA: 0x000971D0 File Offset: 0x000953D0
	public void Load(BinaryReader reader, int version)
	{
		this.m_generatedZones.Clear();
		int num = reader.ReadInt32();
		for (int i = 0; i < num; i++)
		{
			Vector2i item = default(Vector2i);
			item.x = reader.ReadInt32();
			item.y = reader.ReadInt32();
			this.m_generatedZones.Add(item);
		}
		if (version >= 13)
		{
			reader.ReadInt32();
			int num2 = (version >= 21) ? reader.ReadInt32() : 0;
			if (version >= 14)
			{
				this.ClearGlobalKeys();
				int num3 = reader.ReadInt32();
				for (int j = 0; j < num3; j++)
				{
					string keyStr = reader.ReadString();
					this.GlobalKeyAdd(keyStr, true);
				}
			}
			if (version >= 18)
			{
				if (version >= 20)
				{
					this.LocationsGenerated = reader.ReadBoolean();
				}
				this.m_locationInstances.Clear();
				int num4 = reader.ReadInt32();
				for (int k = 0; k < num4; k++)
				{
					string text = reader.ReadString();
					Vector3 zero = Vector3.zero;
					zero.x = reader.ReadSingle();
					zero.y = reader.ReadSingle();
					zero.z = reader.ReadSingle();
					bool generated = false;
					if (version >= 19)
					{
						generated = reader.ReadBoolean();
					}
					ZoneSystem.ZoneLocation location = this.GetLocation(text);
					if (location != null)
					{
						this.RegisterLocation(location, zero, generated);
					}
					else
					{
						ZLog.DevLog("Failed to find location " + text);
					}
				}
				ZLog.Log("Loaded " + num4.ToString() + " locations");
				if (num2 != this.m_locationVersion)
				{
					this.LocationsGenerated = false;
				}
			}
		}
	}

	// Token: 0x060014A3 RID: 5283 RVA: 0x00097358 File Offset: 0x00095558
	private void Update()
	{
		this.m_lastFixedTime = Time.fixedTime;
		if (ZNet.GetConnectionStatus() != ZNet.ConnectionStatus.Connected)
		{
			return;
		}
		if (!ZNet.instance.IsServer() || this.LocationsGenerated)
		{
			if (Terminal.m_showTests)
			{
				Dictionary<string, string> testList = Terminal.m_testList;
				string key = "Time";
				float num = Time.fixedTime;
				string str = num.ToString("0.00");
				string str2 = " / ";
				num = this.TimeSinceStart();
				testList[key] = str + str2 + num.ToString("0.00");
			}
			this.m_updateTimer += Time.deltaTime;
			if (this.m_updateTimer > 0.1f)
			{
				this.m_updateTimer = 0f;
				bool flag = this.CreateLocalZones(ZNet.instance.GetReferencePosition());
				this.UpdateTTL(0.1f);
				if (ZNet.instance.IsServer() && !flag)
				{
					this.CreateGhostZones(ZNet.instance.GetReferencePosition());
					foreach (ZNetPeer znetPeer in ZNet.instance.GetPeers())
					{
						this.CreateGhostZones(znetPeer.GetRefPos());
					}
				}
				this.UpdatePrefabLifetimes();
			}
			return;
		}
		if (TextViewer.IsShowingIntro())
		{
			float num;
			this.m_timeSlicedGenerationTimeBudget = ZoneSystem.GetGenerationTimeBudgetForTargetFrameRate(out num);
			return;
		}
		this.m_timeSlicedGenerationTimeBudget = 0.1f;
	}

	// Token: 0x060014A4 RID: 5284 RVA: 0x000974B4 File Offset: 0x000956B4
	private void UpdatePrefabLifetimes()
	{
		for (int i = 0; i < this.m_locationPrefabs.Count; i++)
		{
			this.m_locationPrefabs[i].m_iterationLifetime--;
			if (this.m_locationPrefabs[i].m_iterationLifetime <= 0)
			{
				this.m_tempLocationPrefabsToRelease.Add(i);
			}
		}
		for (int j = this.m_tempLocationPrefabsToRelease.Count - 1; j >= 0; j--)
		{
			int index = this.m_tempLocationPrefabsToRelease[j];
			this.m_locationPrefabs[index].Release();
			this.m_locationPrefabs.RemoveAt(index);
		}
		this.m_tempLocationPrefabsToRelease.Clear();
	}

	// Token: 0x060014A5 RID: 5285 RVA: 0x00097560 File Offset: 0x00095760
	private void ForceReleaseLoadedPrefabs()
	{
		foreach (ZoneSystem.LocationPrefabLoadData locationPrefabLoadData in this.m_locationPrefabs)
		{
			locationPrefabLoadData.Release();
		}
		this.m_locationPrefabs.Clear();
	}

	// Token: 0x060014A6 RID: 5286 RVA: 0x000975BC File Offset: 0x000957BC
	private bool CreateGhostZones(Vector3 refPoint)
	{
		Vector2i zone = ZoneSystem.GetZone(refPoint);
		GameObject gameObject;
		if (!this.IsZoneGenerated(zone) && this.SpawnZone(zone, ZoneSystem.SpawnMode.Ghost, out gameObject))
		{
			return true;
		}
		int num = this.m_activeArea + this.m_activeDistantArea;
		for (int i = zone.y - num; i <= zone.y + num; i++)
		{
			for (int j = zone.x - num; j <= zone.x + num; j++)
			{
				Vector2i zoneID = new Vector2i(j, i);
				GameObject gameObject2;
				if (!this.IsZoneGenerated(zoneID) && this.SpawnZone(zoneID, ZoneSystem.SpawnMode.Ghost, out gameObject2))
				{
					return true;
				}
			}
		}
		return false;
	}

	// Token: 0x060014A7 RID: 5287 RVA: 0x00097654 File Offset: 0x00095854
	private bool CreateLocalZones(Vector3 refPoint)
	{
		Vector2i zone = ZoneSystem.GetZone(refPoint);
		if (this.PokeLocalZone(zone))
		{
			return true;
		}
		for (int i = zone.y - this.m_activeArea; i <= zone.y + this.m_activeArea; i++)
		{
			for (int j = zone.x - this.m_activeArea; j <= zone.x + this.m_activeArea; j++)
			{
				Vector2i vector2i = new Vector2i(j, i);
				if (!(vector2i == zone) && this.PokeLocalZone(vector2i))
				{
					return true;
				}
			}
		}
		return false;
	}

	// Token: 0x060014A8 RID: 5288 RVA: 0x000976DC File Offset: 0x000958DC
	private bool PokeLocalZone(Vector2i zoneID)
	{
		ZoneSystem.ZoneData zoneData;
		if (this.m_zones.TryGetValue(zoneID, out zoneData))
		{
			zoneData.m_ttl = 0f;
			return false;
		}
		ZoneSystem.SpawnMode mode = (ZNet.instance.IsServer() && !this.IsZoneGenerated(zoneID)) ? ZoneSystem.SpawnMode.Full : ZoneSystem.SpawnMode.Client;
		GameObject root;
		if (this.SpawnZone(zoneID, mode, out root))
		{
			ZoneSystem.ZoneData zoneData2 = new ZoneSystem.ZoneData();
			zoneData2.m_root = root;
			this.m_zones.Add(zoneID, zoneData2);
			return true;
		}
		return false;
	}

	// Token: 0x060014A9 RID: 5289 RVA: 0x0009774C File Offset: 0x0009594C
	public bool IsZoneLoaded(Vector3 point)
	{
		Vector2i zone = ZoneSystem.GetZone(point);
		return this.IsZoneLoaded(zone);
	}

	// Token: 0x060014AA RID: 5290 RVA: 0x00097767 File Offset: 0x00095967
	public bool IsZoneLoaded(Vector2i zoneID)
	{
		return this.m_zones.ContainsKey(zoneID) && !this.m_loadingObjectsInZones.ContainsKey(zoneID);
	}

	// Token: 0x060014AB RID: 5291 RVA: 0x00097788 File Offset: 0x00095988
	public bool IsActiveAreaLoaded()
	{
		Vector2i zone = ZoneSystem.GetZone(ZNet.instance.GetReferencePosition());
		for (int i = zone.y - this.m_activeArea; i <= zone.y + this.m_activeArea; i++)
		{
			for (int j = zone.x - this.m_activeArea; j <= zone.x + this.m_activeArea; j++)
			{
				if (!this.m_zones.ContainsKey(new Vector2i(j, i)))
				{
					return false;
				}
			}
		}
		return true;
	}

	// Token: 0x060014AC RID: 5292 RVA: 0x00097804 File Offset: 0x00095A04
	private bool SpawnZone(Vector2i zoneID, ZoneSystem.SpawnMode mode, out GameObject root)
	{
		Vector3 zonePos = ZoneSystem.GetZonePos(zoneID);
		Heightmap componentInChildren = this.m_zonePrefab.GetComponentInChildren<Heightmap>();
		if (!HeightmapBuilder.instance.IsTerrainReady(zonePos, componentInChildren.m_width, componentInChildren.m_scale, componentInChildren.IsDistantLod, WorldGenerator.instance))
		{
			root = null;
			return false;
		}
		ZoneSystem.LocationInstance locationInstance;
		if (this.m_locationInstances.TryGetValue(zoneID, out locationInstance) && !locationInstance.m_placed && !this.PokeCanSpawnLocation(locationInstance.m_location, true))
		{
			root = null;
			return false;
		}
		root = UnityEngine.Object.Instantiate<GameObject>(this.m_zonePrefab, zonePos, Quaternion.identity);
		if ((mode == ZoneSystem.SpawnMode.Ghost || mode == ZoneSystem.SpawnMode.Full) && !this.IsZoneGenerated(zoneID))
		{
			Heightmap componentInChildren2 = root.GetComponentInChildren<Heightmap>();
			this.m_tempClearAreas.Clear();
			this.m_tempSpawnedObjects.Clear();
			this.PlaceLocations(zoneID, zonePos, root.transform, componentInChildren2, this.m_tempClearAreas, mode, this.m_tempSpawnedObjects);
			this.PlaceVegetation(zoneID, zonePos, root.transform, componentInChildren2, this.m_tempClearAreas, mode, this.m_tempSpawnedObjects);
			this.PlaceZoneCtrl(zoneID, zonePos, mode, this.m_tempSpawnedObjects);
			if (mode == ZoneSystem.SpawnMode.Ghost)
			{
				foreach (GameObject obj in this.m_tempSpawnedObjects)
				{
					UnityEngine.Object.Destroy(obj);
				}
				this.m_tempSpawnedObjects.Clear();
				UnityEngine.Object.Destroy(root);
				root = null;
			}
			this.SetZoneGenerated(zoneID);
		}
		return true;
	}

	// Token: 0x060014AD RID: 5293 RVA: 0x00097970 File Offset: 0x00095B70
	private void PlaceZoneCtrl(Vector2i zoneID, Vector3 zoneCenterPos, ZoneSystem.SpawnMode mode, List<GameObject> spawnedObjects)
	{
		if (mode == ZoneSystem.SpawnMode.Full || mode == ZoneSystem.SpawnMode.Ghost)
		{
			if (mode == ZoneSystem.SpawnMode.Ghost)
			{
				ZNetView.StartGhostInit();
			}
			GameObject gameObject = UnityEngine.Object.Instantiate<GameObject>(this.m_zoneCtrlPrefab, zoneCenterPos, Quaternion.identity);
			gameObject.GetComponent<ZNetView>();
			if (mode == ZoneSystem.SpawnMode.Ghost)
			{
				spawnedObjects.Add(gameObject);
				ZNetView.FinishGhostInit();
			}
		}
	}

	// Token: 0x060014AE RID: 5294 RVA: 0x000979B8 File Offset: 0x00095BB8
	private Vector3 GetRandomPointInRadius(Vector3 center, float radius)
	{
		float f = UnityEngine.Random.value * 3.1415927f * 2f;
		float num = UnityEngine.Random.Range(0f, radius);
		return center + new Vector3(Mathf.Sin(f) * num, 0f, Mathf.Cos(f) * num);
	}

	// Token: 0x060014AF RID: 5295 RVA: 0x00097A04 File Offset: 0x00095C04
	private void PlaceVegetation(Vector2i zoneID, Vector3 zoneCenterPos, Transform parent, Heightmap hmap, List<ZoneSystem.ClearArea> clearAreas, ZoneSystem.SpawnMode mode, List<GameObject> spawnedObjects)
	{
		UnityEngine.Random.State state = UnityEngine.Random.state;
		int seed = WorldGenerator.instance.GetSeed();
		int num = 1;
		foreach (ZoneSystem.ZoneVegetation zoneVegetation in this.m_vegetation)
		{
			num++;
			if (zoneVegetation.m_enable && hmap.HaveBiome(zoneVegetation.m_biome))
			{
				UnityEngine.Random.InitState(seed + zoneID.x * 4271 + zoneID.y * 9187 + zoneVegetation.m_prefab.name.GetStableHashCode());
				int num2 = 1;
				if (zoneVegetation.m_max < 1f)
				{
					if (UnityEngine.Random.value > zoneVegetation.m_max)
					{
						continue;
					}
				}
				else
				{
					num2 = UnityEngine.Random.Range((int)zoneVegetation.m_min, (int)zoneVegetation.m_max + 1);
				}
				bool flag = zoneVegetation.m_prefab.GetComponent<ZNetView>() != null;
				float num3 = Mathf.Cos(0.017453292f * zoneVegetation.m_maxTilt);
				float num4 = Mathf.Cos(0.017453292f * zoneVegetation.m_minTilt);
				float num5 = 32f - zoneVegetation.m_groupRadius;
				this.s_tempVeg.Clear();
				int num6 = zoneVegetation.m_forcePlacement ? (num2 * 50) : num2;
				int num7 = 0;
				for (int i = 0; i < num6; i++)
				{
					Vector3 vector = new Vector3(UnityEngine.Random.Range(zoneCenterPos.x - num5, zoneCenterPos.x + num5), 0f, UnityEngine.Random.Range(zoneCenterPos.z - num5, zoneCenterPos.z + num5));
					int num8 = UnityEngine.Random.Range(zoneVegetation.m_groupSizeMin, zoneVegetation.m_groupSizeMax + 1);
					bool flag2 = false;
					for (int j = 0; j < num8; j++)
					{
						Vector3 vector2 = (j == 0) ? vector : this.GetRandomPointInRadius(vector, zoneVegetation.m_groupRadius);
						float y = (float)UnityEngine.Random.Range(0, 360);
						float num9 = UnityEngine.Random.Range(zoneVegetation.m_scaleMin, zoneVegetation.m_scaleMax);
						float x = UnityEngine.Random.Range(-zoneVegetation.m_randTilt, zoneVegetation.m_randTilt);
						float z = UnityEngine.Random.Range(-zoneVegetation.m_randTilt, zoneVegetation.m_randTilt);
						if (!zoneVegetation.m_blockCheck || !this.IsBlocked(vector2))
						{
							Vector3 vector3;
							Heightmap.Biome biome;
							Heightmap.BiomeArea biomeArea;
							Heightmap heightmap;
							this.GetGroundData(ref vector2, out vector3, out biome, out biomeArea, out heightmap);
							if ((zoneVegetation.m_biome & biome) != Heightmap.Biome.None && (zoneVegetation.m_biomeArea & biomeArea) != (Heightmap.BiomeArea)0)
							{
								float y2;
								Vector3 vector4;
								if (zoneVegetation.m_snapToStaticSolid && this.GetStaticSolidHeight(vector2, out y2, out vector4))
								{
									vector2.y = y2;
									vector3 = vector4;
								}
								float num10 = vector2.y - 30f;
								if (num10 >= zoneVegetation.m_minAltitude && num10 <= zoneVegetation.m_maxAltitude)
								{
									if (zoneVegetation.m_minVegetation != zoneVegetation.m_maxVegetation)
									{
										float vegetationMask = heightmap.GetVegetationMask(vector2);
										if (vegetationMask > zoneVegetation.m_maxVegetation || vegetationMask < zoneVegetation.m_minVegetation)
										{
											goto IL_65D;
										}
									}
									if (zoneVegetation.m_minOceanDepth != zoneVegetation.m_maxOceanDepth)
									{
										float oceanDepth = heightmap.GetOceanDepth(vector2);
										if (oceanDepth < zoneVegetation.m_minOceanDepth || oceanDepth > zoneVegetation.m_maxOceanDepth)
										{
											goto IL_65D;
										}
									}
									if (vector3.y >= num3 && vector3.y <= num4)
									{
										if (zoneVegetation.m_terrainDeltaRadius > 0f)
										{
											float num11;
											Vector3 vector5;
											this.GetTerrainDelta(vector2, zoneVegetation.m_terrainDeltaRadius, out num11, out vector5);
											if (num11 > zoneVegetation.m_maxTerrainDelta || num11 < zoneVegetation.m_minTerrainDelta)
											{
												goto IL_65D;
											}
										}
										if (zoneVegetation.m_minDistanceFromCenter > 0f || zoneVegetation.m_maxDistanceFromCenter > 0f)
										{
											float num12 = global::Utils.LengthXZ(vector2);
											if ((zoneVegetation.m_minDistanceFromCenter > 0f && num12 < zoneVegetation.m_minDistanceFromCenter) || (zoneVegetation.m_maxDistanceFromCenter > 0f && num12 > zoneVegetation.m_maxDistanceFromCenter))
											{
												goto IL_65D;
											}
										}
										if (zoneVegetation.m_inForest)
										{
											float forestFactor = WorldGenerator.GetForestFactor(vector2);
											if (forestFactor < zoneVegetation.m_forestTresholdMin || forestFactor > zoneVegetation.m_forestTresholdMax)
											{
												goto IL_65D;
											}
										}
										if (zoneVegetation.m_surroundCheckVegetation)
										{
											float num13 = 0f;
											for (int k = 0; k < zoneVegetation.m_surroundCheckLayers; k++)
											{
												float num14 = (float)(k + 1) / (float)zoneVegetation.m_surroundCheckLayers * zoneVegetation.m_surroundCheckDistance;
												for (int l = 0; l < 6; l++)
												{
													float f = (float)l / 6f * 3.1415927f * 2f;
													float vegetationMask2 = heightmap.GetVegetationMask(vector2 + new Vector3(Mathf.Sin(f) * num14, 0f, Mathf.Cos(f) * num14));
													float num15 = (1f - num14) / (zoneVegetation.m_surroundCheckDistance * 2f);
													num13 += vegetationMask2 * num15;
												}
											}
											this.s_tempVeg.Add(num13);
											if (this.s_tempVeg.Count < 10)
											{
												goto IL_65D;
											}
											float num16 = this.s_tempVeg.Max();
											float num17 = this.s_tempVeg.Average();
											float num18 = num17 + (num16 - num17) * zoneVegetation.m_surroundBetterThanAverage;
											if (num13 < num18)
											{
												goto IL_65D;
											}
										}
										if (!this.InsideClearArea(clearAreas, vector2))
										{
											if (zoneVegetation.m_snapToWater)
											{
												vector2.y = 30f;
											}
											vector2.y += zoneVegetation.m_groundOffset;
											Quaternion rotation = Quaternion.identity;
											if (zoneVegetation.m_chanceToUseGroundTilt > 0f && UnityEngine.Random.value <= zoneVegetation.m_chanceToUseGroundTilt)
											{
												Quaternion rotation2 = Quaternion.Euler(0f, y, 0f);
												rotation = Quaternion.LookRotation(Vector3.Cross(vector3, rotation2 * Vector3.forward), vector3);
											}
											else
											{
												rotation = Quaternion.Euler(x, y, z);
											}
											if (flag)
											{
												if (mode == ZoneSystem.SpawnMode.Full || mode == ZoneSystem.SpawnMode.Ghost)
												{
													if (mode == ZoneSystem.SpawnMode.Ghost)
													{
														ZNetView.StartGhostInit();
													}
													GameObject gameObject = UnityEngine.Object.Instantiate<GameObject>(zoneVegetation.m_prefab, vector2, rotation);
													ZNetView component = gameObject.GetComponent<ZNetView>();
													if (num9 != gameObject.transform.localScale.x)
													{
														component.SetLocalScale(new Vector3(num9, num9, num9));
														foreach (Collider collider in gameObject.GetComponentsInChildren<Collider>())
														{
															collider.enabled = false;
															collider.enabled = true;
														}
													}
													if (mode == ZoneSystem.SpawnMode.Ghost)
													{
														spawnedObjects.Add(gameObject);
														ZNetView.FinishGhostInit();
													}
												}
											}
											else
											{
												GameObject gameObject2 = UnityEngine.Object.Instantiate<GameObject>(zoneVegetation.m_prefab, vector2, rotation);
												gameObject2.transform.localScale = new Vector3(num9, num9, num9);
												gameObject2.transform.SetParent(parent, true);
											}
											flag2 = true;
										}
									}
								}
							}
						}
						IL_65D:;
					}
					if (flag2)
					{
						num7++;
					}
					if (num7 >= num2)
					{
						break;
					}
				}
			}
		}
		UnityEngine.Random.state = state;
	}

	// Token: 0x060014B0 RID: 5296 RVA: 0x000980DC File Offset: 0x000962DC
	private bool InsideClearArea(List<ZoneSystem.ClearArea> areas, Vector3 point)
	{
		foreach (ZoneSystem.ClearArea clearArea in areas)
		{
			if (point.x > clearArea.m_center.x - clearArea.m_radius && point.x < clearArea.m_center.x + clearArea.m_radius && point.z > clearArea.m_center.z - clearArea.m_radius && point.z < clearArea.m_center.z + clearArea.m_radius)
			{
				return true;
			}
		}
		return false;
	}

	// Token: 0x060014B1 RID: 5297 RVA: 0x00098194 File Offset: 0x00096394
	private ZoneSystem.ZoneLocation GetLocation(int hash)
	{
		ZoneSystem.ZoneLocation result;
		if (this.m_locationsByHash.TryGetValue(hash, out result))
		{
			return result;
		}
		return null;
	}

	// Token: 0x060014B2 RID: 5298 RVA: 0x000981B4 File Offset: 0x000963B4
	private ZoneSystem.ZoneLocation GetLocation(string name)
	{
		foreach (ZoneSystem.ZoneLocation zoneLocation in this.m_locations)
		{
			if (!zoneLocation.m_prefab.IsValid)
			{
				if (zoneLocation.m_enable)
				{
					throw new NullReferenceException(string.Format("Location in list of locations was invalid! Asset: {0}", zoneLocation.m_prefab.m_assetID));
				}
			}
			else if (zoneLocation.m_prefab.Name == name)
			{
				return zoneLocation;
			}
		}
		return null;
	}

	// Token: 0x060014B3 RID: 5299 RVA: 0x00098250 File Offset: 0x00096450
	private void ClearNonPlacedLocations()
	{
		Dictionary<Vector2i, ZoneSystem.LocationInstance> dictionary = new Dictionary<Vector2i, ZoneSystem.LocationInstance>();
		foreach (KeyValuePair<Vector2i, ZoneSystem.LocationInstance> keyValuePair in this.m_locationInstances)
		{
			if (keyValuePair.Value.m_placed)
			{
				dictionary.Add(keyValuePair.Key, keyValuePair.Value);
			}
		}
		this.m_locationInstances = dictionary;
	}

	// Token: 0x060014B4 RID: 5300 RVA: 0x000982CC File Offset: 0x000964CC
	private void CheckLocationDuplicates()
	{
		ZLog.Log("Checking for location duplicates");
		Stopwatch stopwatch = new Stopwatch();
		stopwatch.Start();
		for (int i = 0; i < this.m_locations.Count; i++)
		{
			ZoneSystem.ZoneLocation zoneLocation = this.m_locations[i];
			if (zoneLocation.m_enable)
			{
				for (int j = i + 1; j < this.m_locations.Count; j++)
				{
					ZoneSystem.ZoneLocation zoneLocation2 = this.m_locations[j];
					if (zoneLocation2.m_enable)
					{
						if (zoneLocation.m_prefab.Name == zoneLocation2.m_prefab.Name)
						{
							string str = "Two locations have the same location prefab name ";
							SoftReference<GameObject> prefab = zoneLocation.m_prefab;
							ZLog.LogWarning(str + prefab.ToString());
						}
						if (zoneLocation.m_prefab == zoneLocation2.m_prefab)
						{
							ZLog.LogWarning(string.Format("Locations {0} and {1} point to the same location prefab", zoneLocation.m_prefab, zoneLocation2.m_prefab));
						}
					}
				}
			}
		}
		stopwatch.Stop();
		ZLog.Log(string.Format("Location duplicate check took {0} ms", stopwatch.Elapsed.TotalMilliseconds));
	}

	// Token: 0x060014B5 RID: 5301 RVA: 0x000983FD File Offset: 0x000965FD
	public void GenerateLocations()
	{
		if (this.m_generateLocationsCoroutine != null)
		{
			return;
		}
		if (!Application.isPlaying)
		{
			ZLog.Log("Setting up locations");
			this.SetupLocations();
		}
		this.m_generateLocationsCoroutine = base.StartCoroutine(this.GenerateLocationsTimeSliced());
	}

	// Token: 0x060014B6 RID: 5302 RVA: 0x00098431 File Offset: 0x00096631
	private IEnumerator GenerateLocationsTimeSliced()
	{
		this.m_estimatedGenerateLocationsCompletionTime = DateTime.MaxValue;
		yield return null;
		ZLog.Log("Setting up generating loading indicator");
		LoadingIndicator.SetProgress(0f);
		LoadingIndicator.SetProgressVisibility(true);
		LoadingIndicator.SetText("$menu_generating");
		Stopwatch timeSliceStopwatch = Stopwatch.StartNew();
		ZLog.Log("Generating locations");
		DateTime startTime = DateTime.UtcNow;
		this.ClearNonPlacedLocations();
		List<ZoneSystem.ZoneLocation> ordered = (from a in this.m_locations
		orderby a.m_prioritized descending
		select a).ToList<ZoneSystem.ZoneLocation>();
		int totalEstimatedIterationsLeft = 0;
		for (int j = ordered.Count - 1; j >= 0; j--)
		{
			ZoneSystem.ZoneLocation zoneLocation = ordered[j];
			if (!zoneLocation.m_enable || zoneLocation.m_quantity == 0)
			{
				ordered.RemoveAt(j);
			}
			else
			{
				totalEstimatedIterationsLeft += (zoneLocation.m_prioritized ? 200000 : 100000) * 20 / 2;
			}
		}
		int runIterations = 0;
		int num3;
		for (int i = 0; i < ordered.Count; i = num3 + 1)
		{
			ZoneSystem.ZoneLocation location = ordered[i];
			if (timeSliceStopwatch.Elapsed.TotalSeconds >= (double)this.m_timeSlicedGenerationTimeBudget)
			{
				yield return null;
				timeSliceStopwatch.Restart();
			}
			ZPackage iterationsPkg = new ZPackage();
			yield return this.GenerateLocationsTimeSliced(location, timeSliceStopwatch, iterationsPkg);
			runIterations += iterationsPkg.ReadInt();
			totalEstimatedIterationsLeft -= (location.m_prioritized ? 200000 : 100000) * 20 / 2;
			DateTime utcNow = DateTime.UtcNow;
			double totalSeconds = (utcNow - startTime).TotalSeconds;
			double num;
			if (runIterations == 0)
			{
				num = double.MaxValue;
			}
			else
			{
				num = totalSeconds / (double)runIterations;
			}
			double num2 = num * (double)totalEstimatedIterationsLeft;
			if (double.IsInfinity(num2))
			{
				this.m_estimatedGenerateLocationsCompletionTime = DateTime.MaxValue;
			}
			else
			{
				this.m_estimatedGenerateLocationsCompletionTime = utcNow.AddSeconds(num2);
			}
			LoadingIndicator.SetProgress((float)(i + 1) / (float)ordered.Count);
			location = null;
			iterationsPkg = null;
			num3 = i;
		}
		LoadingIndicator.SetProgress(1f);
		LoadingIndicator.SetProgressVisibility(false);
		this.LocationsGenerated = true;
		ZLog.Log(" Done generating locations, duration:" + (DateTime.UtcNow - startTime).TotalMilliseconds.ToString() + " ms");
		this.m_generateLocationsCoroutine = null;
		yield break;
	}

	// Token: 0x060014B7 RID: 5303 RVA: 0x00098440 File Offset: 0x00096640
	private int CountNrOfLocation(ZoneSystem.ZoneLocation location)
	{
		int num = 0;
		using (Dictionary<Vector2i, ZoneSystem.LocationInstance>.ValueCollection.Enumerator enumerator = this.m_locationInstances.Values.GetEnumerator())
		{
			while (enumerator.MoveNext())
			{
				if (enumerator.Current.m_location.m_prefab.Name == location.m_prefab.Name)
				{
					num++;
				}
			}
		}
		if (num > 0)
		{
			string str = "Old location found ";
			SoftReference<GameObject> prefab = location.m_prefab;
			ZLog.Log(str + prefab.ToString() + " x " + num.ToString());
		}
		return num;
	}

	// Token: 0x060014B8 RID: 5304 RVA: 0x000984EC File Offset: 0x000966EC
	private IEnumerator GenerateLocationsTimeSliced(ZoneSystem.ZoneLocation location, Stopwatch timeSliceStopwatch, ZPackage iterationsPkg)
	{
		DateTime t = DateTime.Now;
		int seed = WorldGenerator.instance.GetSeed() + location.m_prefab.Name.GetStableHashCode();
		UnityEngine.Random.State state = UnityEngine.Random.state;
		UnityEngine.Random.InitState(seed);
		int errorLocationInZone = 0;
		int errorCenterDistance = 0;
		int errorBiome = 0;
		int errorBiomeArea = 0;
		int errorAlt = 0;
		int errorForest = 0;
		int errorSimilar = 0;
		int errorNotSimilar = 0;
		int errorTerrainDelta = 0;
		int errorVegetation = 0;
		float maxRadius = Mathf.Max(location.m_exteriorRadius, location.m_interiorRadius);
		int attempts = location.m_prioritized ? 200000 : 100000;
		int iterations = 0;
		int placed = this.CountNrOfLocation(location);
		float maxRange = 10000f;
		if (location.m_centerFirst)
		{
			maxRange = location.m_minDistance;
		}
		if (!location.m_unique || placed <= 0)
		{
			this.s_tempVeg.Clear();
			int i = 0;
			while (i < attempts && placed < location.m_quantity)
			{
				if (timeSliceStopwatch.Elapsed.TotalSeconds >= (double)this.m_timeSlicedGenerationTimeBudget)
				{
					UnityEngine.Random.State insideState = UnityEngine.Random.state;
					UnityEngine.Random.state = state;
					yield return null;
					timeSliceStopwatch.Restart();
					state = UnityEngine.Random.state;
					UnityEngine.Random.state = insideState;
				}
				Vector2i zoneID = ZoneSystem.GetRandomZone(maxRange);
				if (location.m_centerFirst)
				{
					maxRange += 1f;
				}
				int num;
				if (this.m_locationInstances.ContainsKey(zoneID))
				{
					num = errorLocationInZone + 1;
					errorLocationInZone = num;
				}
				else if (!this.IsZoneGenerated(zoneID))
				{
					Vector3 zonePos = ZoneSystem.GetZonePos(zoneID);
					Heightmap.BiomeArea biomeArea = WorldGenerator.instance.GetBiomeArea(zonePos);
					if ((location.m_biomeArea & biomeArea) == (Heightmap.BiomeArea)0)
					{
						num = errorBiomeArea + 1;
						errorBiomeArea = num;
					}
					else
					{
						for (int j = 0; j < 20; j = num)
						{
							if (timeSliceStopwatch.Elapsed.TotalSeconds >= (double)this.m_timeSlicedGenerationTimeBudget)
							{
								UnityEngine.Random.State insideState = UnityEngine.Random.state;
								UnityEngine.Random.state = state;
								yield return null;
								timeSliceStopwatch.Restart();
								state = UnityEngine.Random.state;
								UnityEngine.Random.state = insideState;
							}
							num = iterations + 1;
							iterations = num;
							Vector3 randomPointInZone = ZoneSystem.GetRandomPointInZone(zoneID, maxRadius);
							float magnitude = randomPointInZone.magnitude;
							if (location.m_minDistance != 0f && magnitude < location.m_minDistance)
							{
								num = errorCenterDistance + 1;
								errorCenterDistance = num;
							}
							else if (location.m_maxDistance != 0f && magnitude > location.m_maxDistance)
							{
								num = errorCenterDistance + 1;
								errorCenterDistance = num;
							}
							else
							{
								Heightmap.Biome biome = WorldGenerator.instance.GetBiome(randomPointInZone);
								if ((location.m_biome & biome) == Heightmap.Biome.None)
								{
									num = errorBiome + 1;
									errorBiome = num;
								}
								else
								{
									Color color;
									randomPointInZone.y = WorldGenerator.instance.GetHeight(randomPointInZone.x, randomPointInZone.z, out color);
									float num2 = (float)((double)randomPointInZone.y - 30.0);
									if (num2 < location.m_minAltitude || num2 > location.m_maxAltitude)
									{
										num = errorAlt + 1;
										errorAlt = num;
									}
									else
									{
										if (location.m_inForest)
										{
											float forestFactor = WorldGenerator.GetForestFactor(randomPointInZone);
											if (forestFactor < location.m_forestTresholdMin || forestFactor > location.m_forestTresholdMax)
											{
												num = errorForest + 1;
												errorForest = num;
												goto IL_7F2;
											}
										}
										if (location.m_minDistanceFromCenter > 0f || location.m_maxDistanceFromCenter > 0f)
										{
											float num3 = global::Utils.LengthXZ(randomPointInZone);
											if ((location.m_minDistanceFromCenter > 0f && num3 < location.m_minDistanceFromCenter) || (location.m_maxDistanceFromCenter > 0f && num3 > location.m_maxDistanceFromCenter))
											{
												goto IL_7F2;
											}
										}
										float num4;
										Vector3 vector;
										WorldGenerator.instance.GetTerrainDelta(randomPointInZone, location.m_exteriorRadius, out num4, out vector);
										if (num4 > location.m_maxTerrainDelta || num4 < location.m_minTerrainDelta)
										{
											num = errorTerrainDelta + 1;
											errorTerrainDelta = num;
										}
										else if (location.m_minDistanceFromSimilar > 0f && this.HaveLocationInRange(location.m_prefab.Name, location.m_group, randomPointInZone, location.m_minDistanceFromSimilar, false))
										{
											num = errorSimilar + 1;
											errorSimilar = num;
										}
										else if (location.m_maxDistanceFromSimilar > 0f && !this.HaveLocationInRange(location.m_prefabName, location.m_groupMax, randomPointInZone, location.m_maxDistanceFromSimilar, true))
										{
											num = errorNotSimilar + 1;
											errorNotSimilar = num;
										}
										else
										{
											float a = color.a;
											if (location.m_minimumVegetation > 0f && a <= location.m_minimumVegetation)
											{
												num = errorVegetation + 1;
												errorVegetation = num;
											}
											else
											{
												if (location.m_maximumVegetation >= 1f || a < location.m_maximumVegetation)
												{
													if (location.m_surroundCheckVegetation)
													{
														float num5 = 0f;
														for (int k = 0; k < location.m_surroundCheckLayers; k++)
														{
															float num6 = (float)(k + 1) / (float)location.m_surroundCheckLayers * location.m_surroundCheckDistance;
															for (int l = 0; l < 6; l++)
															{
																float f = (float)l / 6f * 3.1415927f * 2f;
																Vector3 vector2 = randomPointInZone + new Vector3(Mathf.Sin(f) * num6, 0f, Mathf.Cos(f) * num6);
																Color color2;
																WorldGenerator.instance.GetHeight(vector2.x, vector2.z, out color2);
																float num7 = (location.m_surroundCheckDistance - num6) / (location.m_surroundCheckDistance * 2f);
																num5 += color2.a * num7;
															}
														}
														this.s_tempVeg.Add(num5);
														if (this.s_tempVeg.Count < 10)
														{
															goto IL_7F2;
														}
														float num8 = this.s_tempVeg.Max();
														float num9 = this.s_tempVeg.Average();
														float num10 = num9 + (num8 - num9) * location.m_surroundBetterThanAverage;
														if (num5 < num10)
														{
															goto IL_7F2;
														}
														ZLog.DevLog(string.Format("Surround check passed with a value of {0}, cutoff was {1}, max: {2}, average: {3}.", new object[]
														{
															num5,
															num10,
															num8,
															num9
														}));
													}
													this.RegisterLocation(location, randomPointInZone, false);
													num = placed + 1;
													placed = num;
													break;
												}
												num = errorVegetation + 1;
												errorVegetation = num;
											}
										}
									}
								}
							}
							IL_7F2:
							num = j + 1;
						}
						zoneID = default(Vector2i);
					}
				}
				num = i + 1;
				i = num;
			}
			if (placed < location.m_quantity)
			{
				ZLog.LogWarning(string.Concat(new string[]
				{
					"Failed to place all ",
					location.m_prefab.Name,
					", placed ",
					placed.ToString(),
					" out of ",
					location.m_quantity.ToString()
				}));
				ZLog.DevLog("errorLocationInZone " + errorLocationInZone.ToString());
				ZLog.DevLog("errorCenterDistance " + errorCenterDistance.ToString());
				ZLog.DevLog("errorBiome " + errorBiome.ToString());
				ZLog.DevLog("errorBiomeArea " + errorBiomeArea.ToString());
				ZLog.DevLog("errorAlt " + errorAlt.ToString());
				ZLog.DevLog("errorForest " + errorForest.ToString());
				ZLog.DevLog("errorSimilar " + errorSimilar.ToString());
				ZLog.DevLog("errorNotSimilar " + errorNotSimilar.ToString());
				ZLog.DevLog("errorTerrainDelta " + errorTerrainDelta.ToString());
				ZLog.DevLog("errorVegetation " + errorVegetation.ToString());
			}
		}
		UnityEngine.Random.state = state;
		DateTime.Now - t;
		iterationsPkg.Write(iterations);
		iterationsPkg.SetPos(0);
		yield break;
	}

	// Token: 0x060014B9 RID: 5305 RVA: 0x00098510 File Offset: 0x00096710
	public float GetEstimatedGenerationCompletionTimeFromNow()
	{
		if (this.m_generateLocationsCoroutine == null)
		{
			return 0f;
		}
		DateTime utcNow = DateTime.UtcNow;
		return (float)(this.m_estimatedGenerateLocationsCompletionTime - utcNow).TotalSeconds;
	}

	// Token: 0x060014BA RID: 5306 RVA: 0x00098548 File Offset: 0x00096748
	public static float GetGenerationTimeBudgetForTargetFrameRate(out float targetFrameTime)
	{
		float num;
		if (QualitySettings.vSyncCount > 0)
		{
			num = Screen.currentResolution.refreshRateRatio.denominator / Screen.currentResolution.refreshRateRatio.numerator * (float)QualitySettings.vSyncCount;
		}
		else
		{
			num = 1f / (float)Application.targetFrameRate;
			if (num < 0f)
			{
				num = 0f;
			}
		}
		targetFrameTime = Mathf.Clamp(num, 0.016666668f, 0.033333335f);
		float num2 = 0.006666667f;
		return targetFrameTime - num2;
	}

	// Token: 0x060014BB RID: 5307 RVA: 0x000985C8 File Offset: 0x000967C8
	private static Vector2i GetRandomZone(float range)
	{
		int num = (int)range / 64;
		Vector2i vector2i;
		do
		{
			vector2i = new Vector2i(UnityEngine.Random.Range(-num, num), UnityEngine.Random.Range(-num, num));
		}
		while (ZoneSystem.GetZonePos(vector2i).magnitude >= 10000f);
		return vector2i;
	}

	// Token: 0x060014BC RID: 5308 RVA: 0x00098608 File Offset: 0x00096808
	private static Vector3 GetRandomPointInZone(Vector2i zone, float locationRadius)
	{
		Vector3 zonePos = ZoneSystem.GetZonePos(zone);
		float x = UnityEngine.Random.Range(-32f + locationRadius, 32f - locationRadius);
		float z = UnityEngine.Random.Range(-32f + locationRadius, 32f - locationRadius);
		return zonePos + new Vector3(x, 0f, z);
	}

	// Token: 0x060014BD RID: 5309 RVA: 0x00098654 File Offset: 0x00096854
	private static Vector3 GetRandomPointInZone(float locationRadius)
	{
		Vector3 zonePos = ZoneSystem.GetZonePos(ZoneSystem.GetZone(new Vector3(UnityEngine.Random.Range(-10000f, 10000f), 0f, UnityEngine.Random.Range(-10000f, 10000f))));
		return new Vector3(UnityEngine.Random.Range(zonePos.x - 32f + locationRadius, zonePos.x + 32f - locationRadius), 0f, UnityEngine.Random.Range(zonePos.z - 32f + locationRadius, zonePos.z + 32f - locationRadius));
	}

	// Token: 0x060014BE RID: 5310 RVA: 0x000986E0 File Offset: 0x000968E0
	private void PlaceLocations(Vector2i zoneID, Vector3 zoneCenterPos, Transform parent, Heightmap hmap, List<ZoneSystem.ClearArea> clearAreas, ZoneSystem.SpawnMode mode, List<GameObject> spawnedObjects)
	{
		DateTime now = DateTime.Now;
		ZoneSystem.LocationInstance locationInstance;
		if (this.m_locationInstances.TryGetValue(zoneID, out locationInstance))
		{
			if (locationInstance.m_placed)
			{
				return;
			}
			Vector3 position = locationInstance.m_position;
			Vector3 vector;
			Heightmap.Biome biome;
			Heightmap.BiomeArea biomeArea;
			Heightmap heightmap;
			this.GetGroundData(ref position, out vector, out biome, out biomeArea, out heightmap);
			if (locationInstance.m_location.m_snapToWater)
			{
				position.y = 30f;
			}
			if (locationInstance.m_location.m_clearArea)
			{
				ZoneSystem.ClearArea item = new ZoneSystem.ClearArea(position, locationInstance.m_location.m_exteriorRadius);
				clearAreas.Add(item);
			}
			Quaternion rot = Quaternion.identity;
			if (locationInstance.m_location.m_slopeRotation)
			{
				float num;
				Vector3 vector2;
				this.GetTerrainDelta(position, locationInstance.m_location.m_exteriorRadius, out num, out vector2);
				Vector3 forward = new Vector3(vector2.x, 0f, vector2.z);
				forward.Normalize();
				rot = Quaternion.LookRotation(forward);
				Vector3 eulerAngles = rot.eulerAngles;
				eulerAngles.y = Mathf.Round(eulerAngles.y / 22.5f) * 22.5f;
				rot.eulerAngles = eulerAngles;
			}
			else if (locationInstance.m_location.m_randomRotation)
			{
				rot = Quaternion.Euler(0f, (float)UnityEngine.Random.Range(0, 16) * 22.5f, 0f);
			}
			int seed = WorldGenerator.instance.GetSeed() + zoneID.x * 4271 + zoneID.y * 9187;
			this.SpawnLocation(locationInstance.m_location, seed, position, rot, mode, spawnedObjects);
			locationInstance.m_placed = true;
			this.m_locationInstances[zoneID] = locationInstance;
			TimeSpan timeSpan = DateTime.Now - now;
			string[] array = new string[5];
			array[0] = "Placed locations in zone ";
			int num2 = 1;
			Vector2i vector2i = zoneID;
			array[num2] = vector2i.ToString();
			array[2] = "  duration ";
			array[3] = timeSpan.TotalMilliseconds.ToString();
			array[4] = " ms";
			ZLog.Log(string.Concat(array));
			if (locationInstance.m_location.m_unique)
			{
				this.RemoveUnplacedLocations(locationInstance.m_location);
			}
			if (locationInstance.m_location.m_iconPlaced)
			{
				this.SendLocationIcons(ZRoutedRpc.Everybody);
			}
		}
	}

	// Token: 0x060014BF RID: 5311 RVA: 0x000988FC File Offset: 0x00096AFC
	private void RemoveUnplacedLocations(ZoneSystem.ZoneLocation location)
	{
		List<Vector2i> list = new List<Vector2i>();
		foreach (KeyValuePair<Vector2i, ZoneSystem.LocationInstance> keyValuePair in this.m_locationInstances)
		{
			if (keyValuePair.Value.m_location == location && !keyValuePair.Value.m_placed)
			{
				list.Add(keyValuePair.Key);
			}
		}
		foreach (Vector2i key in list)
		{
			this.m_locationInstances.Remove(key);
		}
		ZLog.DevLog("Removed " + list.Count.ToString() + " unplaced locations of type " + location.m_prefab.Name);
	}

	// Token: 0x060014C0 RID: 5312 RVA: 0x000989EC File Offset: 0x00096BEC
	public bool TestSpawnLocation(string name, Vector3 pos, bool disableSave = true)
	{
		if (!ZNet.instance.IsServer())
		{
			return false;
		}
		ZoneSystem.ZoneLocation location = this.GetLocation(name);
		if (location == null)
		{
			ZLog.Log("Missing location:" + name);
			global::Console.instance.Print("Missing location:" + name);
			return false;
		}
		if (!location.m_prefab.IsValid)
		{
			ZLog.Log("Missing prefab in location:" + name);
			global::Console.instance.Print("Missing location:" + name);
			return false;
		}
		float num = Mathf.Max(location.m_exteriorRadius, location.m_interiorRadius);
		Vector3 zonePos = ZoneSystem.GetZonePos(ZoneSystem.GetZone(pos));
		pos.x = Mathf.Clamp(pos.x, zonePos.x - 32f + num, zonePos.x + 32f - num);
		pos.z = Mathf.Clamp(pos.z, zonePos.z - 32f + num, zonePos.z + 32f - num);
		string[] array = new string[6];
		array[0] = "radius ";
		array[1] = num.ToString();
		array[2] = "  ";
		int num2 = 3;
		Vector3 vector = zonePos;
		array[num2] = vector.ToString();
		array[4] = " ";
		int num3 = 5;
		vector = pos;
		array[num3] = vector.ToString();
		ZLog.Log(string.Concat(array));
		MessageHud.instance.ShowMessage(MessageHud.MessageType.Center, "Location spawned, " + (disableSave ? "world saving DISABLED until restart" : "CAUTION! world saving is ENABLED, use normal location command to disable it!"), 0, null, false);
		this.m_didZoneTest = disableSave;
		float y = (float)UnityEngine.Random.Range(0, 16) * 22.5f;
		List<GameObject> spawnedGhostObjects = new List<GameObject>();
		this.SpawnLocation(location, UnityEngine.Random.Range(0, 99999), pos, Quaternion.Euler(0f, y, 0f), ZoneSystem.SpawnMode.Full, spawnedGhostObjects);
		return true;
	}

	// Token: 0x060014C1 RID: 5313 RVA: 0x00098BAC File Offset: 0x00096DAC
	private bool PokeCanSpawnLocation(ZoneSystem.ZoneLocation location, bool isFirstSpawn)
	{
		ZoneSystem.LocationPrefabLoadData locationPrefabLoadData = null;
		for (int i = 0; i < this.m_locationPrefabs.Count; i++)
		{
			if (this.m_locationPrefabs[i].PrefabAssetID == location.m_prefab.m_assetID)
			{
				locationPrefabLoadData = this.m_locationPrefabs[i];
				break;
			}
		}
		if (locationPrefabLoadData == null)
		{
			locationPrefabLoadData = new ZoneSystem.LocationPrefabLoadData(location.m_prefab, isFirstSpawn);
			this.m_locationPrefabs.Add(locationPrefabLoadData);
		}
		locationPrefabLoadData.m_iterationLifetime = this.GetLocationPrefabLifetime();
		return locationPrefabLoadData.IsLoaded;
	}

	// Token: 0x060014C2 RID: 5314 RVA: 0x00098C34 File Offset: 0x00096E34
	public int GetLocationPrefabLifetime()
	{
		int num = 2 * (this.m_activeArea + this.m_activeDistantArea) + 1;
		int num2 = num * num;
		int num3 = ZNet.instance.IsServer() ? (ZNet.instance.GetPeers().Count + 1) : 1;
		return num2 * num3;
	}

	// Token: 0x060014C3 RID: 5315 RVA: 0x00098C78 File Offset: 0x00096E78
	public bool ShouldDelayProxyLocationSpawning(int hash)
	{
		ZoneSystem.ZoneLocation location = this.GetLocation(hash);
		if (location == null)
		{
			ZLog.LogWarning("Missing location:" + hash.ToString());
			return false;
		}
		return !this.PokeCanSpawnLocation(location, false);
	}

	// Token: 0x060014C4 RID: 5316 RVA: 0x00098CB4 File Offset: 0x00096EB4
	public GameObject SpawnProxyLocation(int hash, int seed, Vector3 pos, Quaternion rot)
	{
		ZoneSystem.ZoneLocation location = this.GetLocation(hash);
		if (location == null)
		{
			ZLog.LogWarning("Missing location:" + hash.ToString());
			return null;
		}
		List<GameObject> spawnedGhostObjects = new List<GameObject>();
		return this.SpawnLocation(location, seed, pos, rot, ZoneSystem.SpawnMode.Client, spawnedGhostObjects);
	}

	// Token: 0x060014C5 RID: 5317 RVA: 0x00098CF8 File Offset: 0x00096EF8
	private GameObject SpawnLocation(ZoneSystem.ZoneLocation location, int seed, Vector3 pos, Quaternion rot, ZoneSystem.SpawnMode mode, List<GameObject> spawnedGhostObjects)
	{
		location.m_prefab.Load();
		ZNetView[] enabledComponentsInChildren = global::Utils.GetEnabledComponentsInChildren<ZNetView>(location.m_prefab.Asset);
		RandomSpawn[] enabledComponentsInChildren2 = global::Utils.GetEnabledComponentsInChildren<RandomSpawn>(location.m_prefab.Asset);
		for (int i = 0; i < enabledComponentsInChildren2.Length; i++)
		{
			enabledComponentsInChildren2[i].Prepare();
		}
		Location component = location.m_prefab.Asset.GetComponent<Location>();
		Vector3 b = Vector3.zero;
		Vector3 vector = Vector3.zero;
		if (component.m_interiorTransform && component.m_generator)
		{
			b = component.m_interiorTransform.localPosition;
			vector = component.m_generator.transform.localPosition;
		}
		Vector3 position = location.m_prefab.Asset.transform.position;
		Quaternion rotation = location.m_prefab.Asset.transform.rotation;
		location.m_prefab.Asset.transform.position = Vector3.zero;
		location.m_prefab.Asset.transform.rotation = Quaternion.identity;
		UnityEngine.Random.InitState(seed);
		bool flag = component && component.m_useCustomInteriorTransform && component.m_interiorTransform && component.m_generator;
		Vector3 localPosition = Vector3.zero;
		Vector3 localPosition2 = Vector3.zero;
		Quaternion localRotation = Quaternion.identity;
		if (flag)
		{
			localPosition = component.m_generator.transform.localPosition;
			localPosition2 = component.m_interiorTransform.localPosition;
			localRotation = component.m_interiorTransform.localRotation;
			Vector3 zonePos = ZoneSystem.GetZonePos(ZoneSystem.GetZone(pos));
			component.m_generator.transform.localPosition = Vector3.zero;
			Vector3 vector2 = zonePos + b + vector - pos;
			Vector3 localPosition3 = (Matrix4x4.Rotate(Quaternion.Inverse(rot)) * Matrix4x4.Translate(vector2)).GetColumn(3);
			localPosition3.y = component.m_interiorTransform.localPosition.y;
			component.m_interiorTransform.localPosition = localPosition3;
			component.m_interiorTransform.localRotation = Quaternion.Inverse(rot);
		}
		if (component && component.m_generator && component.m_useCustomInteriorTransform != component.m_generator.m_useCustomInteriorTransform)
		{
			ZLog.LogWarning(component.name + " & " + component.m_generator.name + " don't have matching m_useCustomInteriorTransform()! If one has it the other should as well!");
		}
		GameObject gameObject = null;
		if (mode == ZoneSystem.SpawnMode.Full || mode == ZoneSystem.SpawnMode.Ghost)
		{
			UnityEngine.Random.InitState(seed);
			foreach (RandomSpawn randomSpawn in enabledComponentsInChildren2)
			{
				Vector3 position2 = randomSpawn.gameObject.transform.position;
				Vector3 pos2 = pos + rot * position2;
				randomSpawn.Randomize(pos2, component, null);
			}
			WearNTear.m_randomInitialDamage = component.m_applyRandomDamage;
			foreach (ZNetView znetView in enabledComponentsInChildren)
			{
				if (znetView.gameObject.activeSelf)
				{
					Vector3 position3 = znetView.gameObject.transform.position;
					Vector3 position4 = pos + rot * position3;
					Quaternion rotation2 = znetView.gameObject.transform.rotation;
					Quaternion rotation3 = rot * rotation2;
					if (mode == ZoneSystem.SpawnMode.Ghost)
					{
						ZNetView.StartGhostInit();
					}
					GameObject gameObject2 = UnityEngine.Object.Instantiate<GameObject>(znetView.gameObject, position4, rotation3);
					gameObject2.HoldReferenceTo(location.m_prefab);
					DungeonGenerator component2 = gameObject2.GetComponent<DungeonGenerator>();
					if (component2)
					{
						if (flag)
						{
							component2.m_originalPosition = vector;
						}
						component2.Generate(mode);
					}
					if (mode == ZoneSystem.SpawnMode.Ghost)
					{
						spawnedGhostObjects.Add(gameObject2);
						ZNetView.FinishGhostInit();
					}
				}
			}
			WearNTear.m_randomInitialDamage = false;
			RandomSpawn[] array = enabledComponentsInChildren2;
			for (int j = 0; j < array.Length; j++)
			{
				array[j].Reset();
			}
			ZNetView[] array2 = enabledComponentsInChildren;
			for (int j = 0; j < array2.Length; j++)
			{
				array2[j].gameObject.SetActive(true);
			}
			location.m_prefab.Asset.transform.position = position;
			location.m_prefab.Asset.transform.rotation = rotation;
			if (flag)
			{
				component.m_generator.transform.localPosition = localPosition;
				component.m_interiorTransform.localPosition = localPosition2;
				component.m_interiorTransform.localRotation = localRotation;
			}
			this.CreateLocationProxy(location, seed, pos, rot, mode, spawnedGhostObjects);
		}
		else
		{
			UnityEngine.Random.InitState(seed);
			foreach (RandomSpawn randomSpawn2 in enabledComponentsInChildren2)
			{
				Vector3 position5 = randomSpawn2.gameObject.transform.position;
				Vector3 pos3 = pos + rot * position5;
				randomSpawn2.Randomize(pos3, component, null);
			}
			ZNetView[] array2 = enabledComponentsInChildren;
			for (int j = 0; j < array2.Length; j++)
			{
				array2[j].gameObject.SetActive(false);
			}
			gameObject = SoftReferenceableAssets.Utils.Instantiate(location.m_prefab, pos, rot);
			gameObject.SetActive(true);
			RandomSpawn[] array = enabledComponentsInChildren2;
			for (int j = 0; j < array.Length; j++)
			{
				array[j].Reset();
			}
			array2 = enabledComponentsInChildren;
			for (int j = 0; j < array2.Length; j++)
			{
				array2[j].gameObject.SetActive(true);
			}
			location.m_prefab.Asset.transform.position = position;
			location.m_prefab.Asset.transform.rotation = rotation;
			if (flag)
			{
				component.m_generator.transform.localPosition = localPosition;
				component.m_interiorTransform.localPosition = localPosition2;
				component.m_interiorTransform.localRotation = localRotation;
			}
		}
		location.m_prefab.Release();
		SnapToGround.SnappAll();
		return gameObject;
	}

	// Token: 0x060014C6 RID: 5318 RVA: 0x000992A4 File Offset: 0x000974A4
	private void CreateLocationProxy(ZoneSystem.ZoneLocation location, int seed, Vector3 pos, Quaternion rotation, ZoneSystem.SpawnMode mode, List<GameObject> spawnedGhostObjects)
	{
		if (mode == ZoneSystem.SpawnMode.Ghost)
		{
			ZNetView.StartGhostInit();
		}
		GameObject gameObject = UnityEngine.Object.Instantiate<GameObject>(this.m_locationProxyPrefab, pos, rotation);
		LocationProxy component = gameObject.GetComponent<LocationProxy>();
		bool spawnNow = mode == ZoneSystem.SpawnMode.Full;
		component.SetLocation(location.m_prefab.Name, seed, spawnNow);
		if (mode == ZoneSystem.SpawnMode.Ghost)
		{
			spawnedGhostObjects.Add(gameObject);
			ZNetView.FinishGhostInit();
		}
	}

	// Token: 0x060014C7 RID: 5319 RVA: 0x000992FC File Offset: 0x000974FC
	private void RegisterLocation(ZoneSystem.ZoneLocation location, Vector3 pos, bool generated)
	{
		ZoneSystem.LocationInstance value = default(ZoneSystem.LocationInstance);
		value.m_location = location;
		value.m_position = pos;
		value.m_placed = generated;
		Vector2i zone = ZoneSystem.GetZone(pos);
		if (this.m_locationInstances.ContainsKey(zone))
		{
			string str = "Location already exist in zone ";
			Vector2i vector2i = zone;
			ZLog.LogWarning(str + vector2i.ToString());
			return;
		}
		this.m_locationInstances.Add(zone, value);
	}

	// Token: 0x060014C8 RID: 5320 RVA: 0x0009936C File Offset: 0x0009756C
	private bool HaveLocationInRange(string prefabName, string group, Vector3 p, float radius, bool maxGroup = false)
	{
		foreach (ZoneSystem.LocationInstance locationInstance in this.m_locationInstances.Values)
		{
			if ((locationInstance.m_location.m_prefab.Name == prefabName || (!maxGroup && group.Length > 0 && group == locationInstance.m_location.m_group) || (maxGroup && group.Length > 0 && group == locationInstance.m_location.m_groupMax)) && Vector3.Distance(locationInstance.m_position, p) < radius)
			{
				return true;
			}
		}
		return false;
	}

	// Token: 0x060014C9 RID: 5321 RVA: 0x0009942C File Offset: 0x0009762C
	public bool GetLocationIcon(string name, out Vector3 pos)
	{
		if (ZNet.instance.IsServer())
		{
			using (Dictionary<Vector2i, ZoneSystem.LocationInstance>.Enumerator enumerator = this.m_locationInstances.GetEnumerator())
			{
				while (enumerator.MoveNext())
				{
					KeyValuePair<Vector2i, ZoneSystem.LocationInstance> keyValuePair = enumerator.Current;
					if ((keyValuePair.Value.m_location.m_iconAlways || (keyValuePair.Value.m_location.m_iconPlaced && keyValuePair.Value.m_placed)) && keyValuePair.Value.m_location.m_prefab.Name == name)
					{
						pos = keyValuePair.Value.m_position;
						return true;
					}
				}
				goto IL_F6;
			}
		}
		foreach (KeyValuePair<Vector3, string> keyValuePair2 in this.m_locationIcons)
		{
			if (keyValuePair2.Value == name)
			{
				pos = keyValuePair2.Key;
				return true;
			}
		}
		IL_F6:
		pos = Vector3.zero;
		return false;
	}

	// Token: 0x060014CA RID: 5322 RVA: 0x0009955C File Offset: 0x0009775C
	public void GetLocationIcons(Dictionary<Vector3, string> icons)
	{
		if (ZNet.instance.IsServer())
		{
			using (Dictionary<Vector2i, ZoneSystem.LocationInstance>.ValueCollection.Enumerator enumerator = this.m_locationInstances.Values.GetEnumerator())
			{
				while (enumerator.MoveNext())
				{
					ZoneSystem.LocationInstance locationInstance = enumerator.Current;
					if (locationInstance.m_location.m_iconAlways || (locationInstance.m_location.m_iconPlaced && locationInstance.m_placed))
					{
						icons[locationInstance.m_position] = locationInstance.m_location.m_prefab.Name;
					}
				}
				return;
			}
		}
		foreach (KeyValuePair<Vector3, string> keyValuePair in this.m_locationIcons)
		{
			icons.Add(keyValuePair.Key, keyValuePair.Value);
		}
	}

	// Token: 0x060014CB RID: 5323 RVA: 0x00099648 File Offset: 0x00097848
	private void GetTerrainDelta(Vector3 center, float radius, out float delta, out Vector3 slopeDirection)
	{
		int num = 10;
		float num2 = -999999f;
		float num3 = 999999f;
		Vector3 b = center;
		Vector3 a = center;
		for (int i = 0; i < num; i++)
		{
			Vector2 vector = UnityEngine.Random.insideUnitCircle * radius;
			Vector3 vector2 = center + new Vector3(vector.x, 0f, vector.y);
			float groundHeight = this.GetGroundHeight(vector2);
			if (groundHeight < num3)
			{
				num3 = groundHeight;
				a = vector2;
			}
			if (groundHeight > num2)
			{
				num2 = groundHeight;
				b = vector2;
			}
		}
		delta = num2 - num3;
		slopeDirection = Vector3.Normalize(a - b);
	}

	// Token: 0x060014CC RID: 5324 RVA: 0x000996E0 File Offset: 0x000978E0
	public bool IsBlocked(Vector3 p)
	{
		p.y += 2000f;
		return Physics.Raycast(p, Vector3.down, 10000f, this.m_blockRayMask);
	}

	// Token: 0x060014CD RID: 5325 RVA: 0x00099710 File Offset: 0x00097910
	public float GetGroundHeight(Vector3 p)
	{
		Vector3 origin = p;
		origin.y = 6000f;
		RaycastHit raycastHit;
		if (Physics.Raycast(origin, Vector3.down, out raycastHit, 10000f, this.m_terrainRayMask))
		{
			return raycastHit.point.y;
		}
		return p.y;
	}

	// Token: 0x060014CE RID: 5326 RVA: 0x00099758 File Offset: 0x00097958
	public bool GetGroundHeight(Vector3 p, out float height)
	{
		p.y = 6000f;
		RaycastHit raycastHit;
		if (Physics.Raycast(p, Vector3.down, out raycastHit, 10000f, this.m_terrainRayMask))
		{
			height = raycastHit.point.y;
			return true;
		}
		height = 0f;
		return false;
	}

	// Token: 0x060014CF RID: 5327 RVA: 0x000997A4 File Offset: 0x000979A4
	public float GetSolidHeight(Vector3 p)
	{
		Vector3 origin = p;
		origin.y += 1000f;
		RaycastHit raycastHit;
		if (Physics.Raycast(origin, Vector3.down, out raycastHit, 2000f, this.m_solidRayMask))
		{
			return raycastHit.point.y;
		}
		return p.y;
	}

	// Token: 0x060014D0 RID: 5328 RVA: 0x000997F0 File Offset: 0x000979F0
	public bool GetSolidHeight(Vector3 p, out float height, int heightMargin = 1000)
	{
		p.y += (float)heightMargin;
		RaycastHit raycastHit;
		if (Physics.Raycast(p, Vector3.down, out raycastHit, 2000f, this.m_solidRayMask) && !raycastHit.collider.attachedRigidbody)
		{
			height = raycastHit.point.y;
			return true;
		}
		height = 0f;
		return false;
	}

	// Token: 0x060014D1 RID: 5329 RVA: 0x00099850 File Offset: 0x00097A50
	public bool GetSolidHeight(Vector3 p, float radius, out float height, Transform ignore)
	{
		height = p.y - 1000f;
		p.y += 1000f;
		int num;
		if (radius <= 0f)
		{
			num = Physics.RaycastNonAlloc(p, Vector3.down, this.rayHits, 2000f, this.m_solidRayMask);
		}
		else
		{
			num = Physics.SphereCastNonAlloc(p, radius, Vector3.down, this.rayHits, 2000f, this.m_solidRayMask);
		}
		ZoneSystem.s_rayHits.Clear();
		ZoneSystem.s_rayHitsHeight.Clear();
		for (int i = 0; i < num; i++)
		{
			RaycastHit item = this.rayHits[i];
			float y = item.point.y;
			global::Utils.InsertSortNoAlloc<RaycastHit>(ZoneSystem.s_rayHits, item, ZoneSystem.s_rayHitsHeight, y);
		}
		for (int j = ZoneSystem.s_rayHits.Count - 1; j >= 0; j--)
		{
			RaycastHit raycastHit = this.rayHits[j];
			Collider collider = raycastHit.collider;
			if (!(collider.attachedRigidbody != null) && (!(ignore != null) || !global::Utils.IsParent(collider.transform, ignore)))
			{
				height = Mathf.Max(height, ZoneSystem.s_rayHitsHeight[j]);
				return true;
			}
		}
		return false;
	}

	// Token: 0x060014D2 RID: 5330 RVA: 0x00099980 File Offset: 0x00097B80
	public bool GetSolidHeight(Vector3 p, out float height, out Vector3 normal, out GameObject go)
	{
		p.y += 1000f;
		RaycastHit raycastHit;
		if (Physics.Raycast(p, Vector3.down, out raycastHit, 2000f, this.m_solidRayMask) && !raycastHit.collider.attachedRigidbody)
		{
			height = raycastHit.point.y;
			normal = raycastHit.normal;
			go = raycastHit.collider.gameObject;
			return true;
		}
		height = 0f;
		normal = Vector3.zero;
		go = null;
		return false;
	}

	// Token: 0x060014D3 RID: 5331 RVA: 0x00099A10 File Offset: 0x00097C10
	public bool GetStaticSolidHeight(Vector3 p, out float height, out Vector3 normal)
	{
		p.y += 1000f;
		RaycastHit raycastHit;
		if (Physics.Raycast(p, Vector3.down, out raycastHit, 2000f, this.m_staticSolidRayMask) && !raycastHit.collider.attachedRigidbody)
		{
			height = raycastHit.point.y;
			normal = raycastHit.normal;
			return true;
		}
		height = 0f;
		normal = Vector3.zero;
		return false;
	}

	// Token: 0x060014D4 RID: 5332 RVA: 0x00099A8C File Offset: 0x00097C8C
	public bool FindFloor(Vector3 p, out float height)
	{
		RaycastHit raycastHit;
		if (Physics.Raycast(p + Vector3.up * 1f, Vector3.down, out raycastHit, 1000f, this.m_solidRayMask))
		{
			height = raycastHit.point.y;
			return true;
		}
		height = 0f;
		return false;
	}

	// Token: 0x060014D5 RID: 5333 RVA: 0x00099AE0 File Offset: 0x00097CE0
	public float GetGroundOffset(Vector3 position)
	{
		Vector3 vector;
		Heightmap.Biome biome;
		Heightmap.BiomeArea biomeArea;
		Heightmap heightmap;
		this.GetGroundData(ref position, out vector, out biome, out biomeArea, out heightmap);
		if (heightmap)
		{
			return heightmap.GetHeightOffset(position);
		}
		return 0f;
	}

	// Token: 0x060014D6 RID: 5334 RVA: 0x00099B14 File Offset: 0x00097D14
	public static bool IsLavaPreHeightmap(Vector3 position, float lavaValue = 0.6f)
	{
		if (WorldGenerator.instance.GetBiome(position.x, position.z, 0.02f, false) != Heightmap.Biome.AshLands)
		{
			return false;
		}
		Color color;
		WorldGenerator.instance.GetBiomeHeight(Heightmap.Biome.AshLands, position.x, position.z, out color, false);
		return color.a > lavaValue;
	}

	// Token: 0x060014D7 RID: 5335 RVA: 0x00099B68 File Offset: 0x00097D68
	public bool IsLava(Vector3 position, bool defaultTrue = false)
	{
		Vector3 vector;
		Heightmap.Biome biome;
		Heightmap.BiomeArea biomeArea;
		Heightmap heightmap;
		this.GetGroundData(ref position, out vector, out biome, out biomeArea, out heightmap);
		if (!heightmap)
		{
			return defaultTrue;
		}
		return heightmap.IsLava(position, 0.6f);
	}

	// Token: 0x060014D8 RID: 5336 RVA: 0x00099B9C File Offset: 0x00097D9C
	public bool IsLava(ref Vector3 position, bool defaultTrue = false)
	{
		Vector3 vector;
		Heightmap.Biome biome;
		Heightmap.BiomeArea biomeArea;
		Heightmap heightmap;
		this.GetGroundData(ref position, out vector, out biome, out biomeArea, out heightmap);
		if (!heightmap)
		{
			return defaultTrue;
		}
		return heightmap.IsLava(position, 0.6f);
	}

	// Token: 0x060014D9 RID: 5337 RVA: 0x00099BD4 File Offset: 0x00097DD4
	public void GetGroundData(ref Vector3 p, out Vector3 normal, out Heightmap.Biome biome, out Heightmap.BiomeArea biomeArea, out Heightmap hmap)
	{
		biome = Heightmap.Biome.None;
		biomeArea = Heightmap.BiomeArea.Everything;
		hmap = null;
		RaycastHit raycastHit;
		if (Physics.Raycast(p + Vector3.up * 5000f, Vector3.down, out raycastHit, 10000f, this.m_terrainRayMask))
		{
			p.y = raycastHit.point.y;
			normal = raycastHit.normal;
			Heightmap component = raycastHit.collider.GetComponent<Heightmap>();
			if (component)
			{
				biome = component.GetBiome(raycastHit.point, 0.02f, false);
				biomeArea = component.GetBiomeArea();
				hmap = component;
			}
			return;
		}
		normal = Vector3.up;
	}

	// Token: 0x060014DA RID: 5338 RVA: 0x00099C84 File Offset: 0x00097E84
	private void UpdateTTL(float dt)
	{
		foreach (KeyValuePair<Vector2i, ZoneSystem.ZoneData> keyValuePair in this.m_zones)
		{
			keyValuePair.Value.m_ttl += dt;
		}
		foreach (KeyValuePair<Vector2i, ZoneSystem.ZoneData> keyValuePair2 in this.m_zones)
		{
			if (keyValuePair2.Value.m_ttl > this.m_zoneTTL && !ZNetScene.instance.HaveInstanceInSector(keyValuePair2.Key))
			{
				UnityEngine.Object.Destroy(keyValuePair2.Value.m_root);
				this.m_zones.Remove(keyValuePair2.Key);
				break;
			}
		}
	}

	// Token: 0x060014DB RID: 5339 RVA: 0x00099D6C File Offset: 0x00097F6C
	public bool FindClosestLocation(string name, Vector3 point, out ZoneSystem.LocationInstance closest)
	{
		float num = 999999f;
		closest = default(ZoneSystem.LocationInstance);
		bool result = false;
		foreach (ZoneSystem.LocationInstance locationInstance in this.m_locationInstances.Values)
		{
			float num2 = Vector3.Distance(locationInstance.m_position, point);
			if (locationInstance.m_location.m_prefab.Name == name && num2 < num)
			{
				num = num2;
				closest = locationInstance;
				result = true;
			}
		}
		return result;
	}

	// Token: 0x060014DC RID: 5340 RVA: 0x00099E04 File Offset: 0x00098004
	public bool FindLocations(string name, ref List<ZoneSystem.LocationInstance> locations)
	{
		locations.Clear();
		foreach (ZoneSystem.LocationInstance locationInstance in this.m_locationInstances.Values)
		{
			if (locationInstance.m_location.m_prefab.Name == name)
			{
				locations.Add(locationInstance);
			}
		}
		return locations.Count > 0;
	}

	// Token: 0x060014DD RID: 5341 RVA: 0x00099E88 File Offset: 0x00098088
	public static Vector2i GetZone(Vector3 point)
	{
		int x = global::Utils.FloorToInt((float)(((double)point.x + 32.0) / 64.0));
		int y = global::Utils.FloorToInt((float)(((double)point.z + 32.0) / 64.0));
		return new Vector2i(x, y);
	}

	// Token: 0x060014DE RID: 5342 RVA: 0x00099EDE File Offset: 0x000980DE
	public static Vector3 GetZonePos(Vector2i id)
	{
		return new Vector3((float)((double)id.x * 64.0), 0f, (float)((double)id.y * 64.0));
	}

	// Token: 0x060014DF RID: 5343 RVA: 0x00099F0E File Offset: 0x0009810E
	private void SetZoneGenerated(Vector2i zoneID)
	{
		this.m_generatedZones.Add(zoneID);
	}

	// Token: 0x060014E0 RID: 5344 RVA: 0x00099F1D File Offset: 0x0009811D
	private bool IsZoneGenerated(Vector2i zoneID)
	{
		return this.m_generatedZones.Contains(zoneID);
	}

	// Token: 0x060014E1 RID: 5345 RVA: 0x00099F2C File Offset: 0x0009812C
	public bool IsZoneReadyForType(Vector2i zoneID, ZDO.ObjectType objectType)
	{
		if (this.m_loadingObjectsInZones.Count <= 0)
		{
			return true;
		}
		if (!this.m_loadingObjectsInZones.ContainsKey(zoneID))
		{
			return true;
		}
		foreach (ZDO zdo in this.m_loadingObjectsInZones[zoneID])
		{
			if (objectType < zdo.Type)
			{
				return false;
			}
		}
		return true;
	}

	// Token: 0x060014E2 RID: 5346 RVA: 0x00099FB0 File Offset: 0x000981B0
	public void SetLoadingInZone(ZDO zdo)
	{
		Vector2i sector = zdo.GetSector();
		if (this.m_loadingObjectsInZones.ContainsKey(sector))
		{
			this.m_loadingObjectsInZones[sector].Add(zdo);
			return;
		}
		List<ZDO> list = new List<ZDO>();
		list.Add(zdo);
		this.m_loadingObjectsInZones.Add(sector, list);
	}

	// Token: 0x060014E3 RID: 5347 RVA: 0x0009A000 File Offset: 0x00098200
	public void UnsetLoadingInZone(ZDO zdo)
	{
		Vector2i sector = zdo.GetSector();
		this.m_loadingObjectsInZones[sector].Remove(zdo);
		if (this.m_loadingObjectsInZones[sector].Count <= 0)
		{
			this.m_loadingObjectsInZones.Remove(sector);
		}
	}

	// Token: 0x060014E4 RID: 5348 RVA: 0x0009A048 File Offset: 0x00098248
	public bool SkipSaving()
	{
		return this.m_error || this.m_didZoneTest;
	}

	// Token: 0x060014E5 RID: 5349 RVA: 0x0009A05A File Offset: 0x0009825A
	public float TimeSinceStart()
	{
		return this.m_lastFixedTime - this.m_startTime;
	}

	// Token: 0x060014E6 RID: 5350 RVA: 0x0009A069 File Offset: 0x00098269
	public void ResetGlobalKeys()
	{
		this.ClearGlobalKeys();
		this.SetStartingGlobalKeys(false);
		this.SendGlobalKeys(ZRoutedRpc.Everybody);
	}

	// Token: 0x060014E7 RID: 5351 RVA: 0x0009A084 File Offset: 0x00098284
	public void ResetWorldKeys()
	{
		for (int i = 0; i < 31; i++)
		{
			GlobalKeys globalKeys = (GlobalKeys)i;
			this.RemoveGlobalKey(globalKeys.ToString());
		}
	}

	// Token: 0x060014E8 RID: 5352 RVA: 0x0009A0B4 File Offset: 0x000982B4
	public void SetStartingGlobalKeys(bool send = true)
	{
		for (int i = 0; i < 31; i++)
		{
			GlobalKeys globalKeys = (GlobalKeys)i;
			this.GlobalKeyRemove(globalKeys.ToString(), false);
		}
		string text = null;
		this.m_tempKeys.Clear();
		this.m_tempKeys.AddRange(ZNet.World.m_startingGlobalKeys);
		foreach (string text2 in this.m_tempKeys)
		{
			string text3;
			GlobalKeys globalKeys2;
			ZoneSystem.GetKeyValue(text2.ToLower(), out text3, out globalKeys2);
			if (globalKeys2 == GlobalKeys.Preset)
			{
				text = text3;
			}
			this.GlobalKeyAdd(text2, false);
		}
		if (text != null)
		{
			ServerOptionsGUI.m_instance.SetPreset(ZNet.World, text);
		}
		if (send)
		{
			this.SendGlobalKeys(ZRoutedRpc.Everybody);
		}
	}

	// Token: 0x060014E9 RID: 5353 RVA: 0x0009A18C File Offset: 0x0009838C
	public void SetGlobalKey(GlobalKeys key, float value)
	{
		this.SetGlobalKey(string.Format("{0} {1}", key, value.ToString(CultureInfo.InvariantCulture)));
	}

	// Token: 0x060014EA RID: 5354 RVA: 0x0009A1B0 File Offset: 0x000983B0
	public void SetGlobalKey(GlobalKeys key)
	{
		this.SetGlobalKey(key.ToString());
	}

	// Token: 0x060014EB RID: 5355 RVA: 0x0009A1C5 File Offset: 0x000983C5
	public void SetGlobalKey(string name)
	{
		ZRoutedRpc.instance.InvokeRoutedRPC("SetGlobalKey", new object[]
		{
			name
		});
	}

	// Token: 0x060014EC RID: 5356 RVA: 0x0009A1E0 File Offset: 0x000983E0
	public bool GetGlobalKey(GlobalKeys key)
	{
		return this.m_globalKeysEnums.Contains(key);
	}

	// Token: 0x060014ED RID: 5357 RVA: 0x0009A1EE File Offset: 0x000983EE
	public bool GetGlobalKey(GlobalKeys key, out string value)
	{
		return this.m_globalKeysValues.TryGetValue(key.ToString().ToLower(), out value);
	}

	// Token: 0x060014EE RID: 5358 RVA: 0x0009A210 File Offset: 0x00098410
	public bool GetGlobalKey(GlobalKeys key, out float value)
	{
		string s;
		if (this.m_globalKeysValues.TryGetValue(key.ToString().ToLower(), out s) && float.TryParse(s, NumberStyles.Any, CultureInfo.InvariantCulture, out value))
		{
			return true;
		}
		value = 0f;
		return false;
	}

	// Token: 0x060014EF RID: 5359 RVA: 0x0009A25C File Offset: 0x0009845C
	public bool GetGlobalKey(string name)
	{
		string text;
		return this.GetGlobalKey(name, out text);
	}

	// Token: 0x060014F0 RID: 5360 RVA: 0x0009A272 File Offset: 0x00098472
	public bool GetGlobalKey(string name, out string value)
	{
		return this.m_globalKeysValues.TryGetValue(name.ToLower(), out value);
	}

	// Token: 0x060014F1 RID: 5361 RVA: 0x0009A286 File Offset: 0x00098486
	public bool GetGlobalKeyExact(string fullLine)
	{
		return this.m_globalKeys.Contains(fullLine);
	}

	// Token: 0x060014F2 RID: 5362 RVA: 0x0009A294 File Offset: 0x00098494
	public bool CheckKey(string key, GameKeyType type = GameKeyType.Global, bool trueWhenKeySet = true)
	{
		if (type == GameKeyType.Global)
		{
			return ZoneSystem.instance.GetGlobalKey(key) == trueWhenKeySet;
		}
		if (type != GameKeyType.Player)
		{
			ZLog.LogError("Unknown GameKeyType type");
			return false;
		}
		return Player.m_localPlayer && Player.m_localPlayer.HaveUniqueKey(key) == trueWhenKeySet;
	}

	// Token: 0x060014F3 RID: 5363 RVA: 0x0009A2E1 File Offset: 0x000984E1
	private void RPC_SetGlobalKey(long sender, string name)
	{
		if (this.m_globalKeys.Contains(name))
		{
			return;
		}
		this.GlobalKeyAdd(name, true);
		this.SendGlobalKeys(ZRoutedRpc.Everybody);
	}

	// Token: 0x060014F4 RID: 5364 RVA: 0x0009A305 File Offset: 0x00098505
	public void RemoveGlobalKey(GlobalKeys key)
	{
		this.RemoveGlobalKey(key.ToString());
	}

	// Token: 0x060014F5 RID: 5365 RVA: 0x0009A31A File Offset: 0x0009851A
	public void RemoveGlobalKey(string name)
	{
		ZRoutedRpc.instance.InvokeRoutedRPC("RemoveGlobalKey", new object[]
		{
			name
		});
	}

	// Token: 0x060014F6 RID: 5366 RVA: 0x0009A335 File Offset: 0x00098535
	private void RPC_RemoveGlobalKey(long sender, string name)
	{
		if (!this.GlobalKeyRemove(name, true))
		{
			return;
		}
		this.SendGlobalKeys(ZRoutedRpc.Everybody);
	}

	// Token: 0x060014F7 RID: 5367 RVA: 0x0009A34D File Offset: 0x0009854D
	public List<string> GetGlobalKeys()
	{
		return new List<string>(this.m_globalKeys);
	}

	// Token: 0x060014F8 RID: 5368 RVA: 0x0009A35A File Offset: 0x0009855A
	public Dictionary<Vector2i, ZoneSystem.LocationInstance>.ValueCollection GetLocationList()
	{
		return this.m_locationInstances.Values;
	}

	// Token: 0x170000BD RID: 189
	// (get) Token: 0x060014F9 RID: 5369 RVA: 0x0009A367 File Offset: 0x00098567
	// (set) Token: 0x060014FA RID: 5370 RVA: 0x0009A36F File Offset: 0x0009856F
	private bool LocationsGenerated
	{
		get
		{
			return this.m_locationsGenerated;
		}
		set
		{
			this.m_locationsGenerated = value;
			if (this.m_locationsGenerated)
			{
				Action generateLocationsCompleted = this.m_generateLocationsCompleted;
				if (generateLocationsCompleted != null)
				{
					generateLocationsCompleted();
				}
				this.m_generateLocationsCompleted = null;
			}
		}
	}

	// Token: 0x1400000D RID: 13
	// (add) Token: 0x060014FB RID: 5371 RVA: 0x0009A398 File Offset: 0x00098598
	// (remove) Token: 0x060014FC RID: 5372 RVA: 0x0009A3C3 File Offset: 0x000985C3
	public event Action GenerateLocationsCompleted
	{
		add
		{
			if (this.m_locationsGenerated)
			{
				if (value != null)
				{
					value();
				}
				return;
			}
			this.m_generateLocationsCompleted = (Action)Delegate.Combine(this.m_generateLocationsCompleted, value);
		}
		remove
		{
			this.m_generateLocationsCompleted = (Action)Delegate.Remove(this.m_generateLocationsCompleted, value);
		}
	}

	// Token: 0x04001436 RID: 5174
	private Dictionary<Vector3, string> tempIconList = new Dictionary<Vector3, string>();

	// Token: 0x04001437 RID: 5175
	private List<float> s_tempVeg = new List<float>();

	// Token: 0x04001438 RID: 5176
	private RaycastHit[] rayHits = new RaycastHit[200];

	// Token: 0x04001439 RID: 5177
	private static readonly List<RaycastHit> s_rayHits = new List<RaycastHit>(64);

	// Token: 0x0400143A RID: 5178
	private static readonly List<float> s_rayHitsHeight = new List<float>(64);

	// Token: 0x0400143B RID: 5179
	private List<string> m_tempKeys = new List<string>();

	// Token: 0x0400143C RID: 5180
	private static ZoneSystem m_instance;

	// Token: 0x0400143D RID: 5181
	[HideInInspector]
	public List<Heightmap.Biome> m_biomeFolded = new List<Heightmap.Biome>();

	// Token: 0x0400143E RID: 5182
	[HideInInspector]
	public List<Heightmap.Biome> m_vegetationFolded = new List<Heightmap.Biome>();

	// Token: 0x0400143F RID: 5183
	[HideInInspector]
	public List<Heightmap.Biome> m_locationFolded = new List<Heightmap.Biome>();

	// Token: 0x04001440 RID: 5184
	[NonSerialized]
	public bool m_drawLocations;

	// Token: 0x04001441 RID: 5185
	[NonSerialized]
	public string m_drawLocationsFilter = "";

	// Token: 0x04001442 RID: 5186
	[global::Tooltip("Zones to load around center sector")]
	public int m_activeArea = 1;

	// Token: 0x04001443 RID: 5187
	public int m_activeDistantArea = 1;

	// Token: 0x04001444 RID: 5188
	[global::Tooltip("Zone size, should match netscene sector size")]
	public float m_zoneSize = 64f;

	// Token: 0x04001445 RID: 5189
	[global::Tooltip("Time before destroying inactive zone")]
	public float m_zoneTTL = 4f;

	// Token: 0x04001446 RID: 5190
	[global::Tooltip("Time before spawning active zone")]
	public float m_zoneTTS = 4f;

	// Token: 0x04001447 RID: 5191
	public GameObject m_zonePrefab;

	// Token: 0x04001448 RID: 5192
	public GameObject m_zoneCtrlPrefab;

	// Token: 0x04001449 RID: 5193
	public GameObject m_locationProxyPrefab;

	// Token: 0x0400144A RID: 5194
	public float m_waterLevel = 30f;

	// Token: 0x0400144B RID: 5195
	public const float c_WaterLevel = 30f;

	// Token: 0x0400144C RID: 5196
	public const float c_ZoneSize = 64f;

	// Token: 0x0400144D RID: 5197
	public const double c_ZoneSizeDouble = 64.0;

	// Token: 0x0400144E RID: 5198
	public const float c_ZoneHalfSize = 32f;

	// Token: 0x0400144F RID: 5199
	public const double c_ZoneHalfSizeDouble = 32.0;

	// Token: 0x04001450 RID: 5200
	[Header("Versions")]
	public int m_pgwVersion = 53;

	// Token: 0x04001451 RID: 5201
	public int m_locationVersion = 1;

	// Token: 0x04001452 RID: 5202
	[Header("Generation data")]
	public List<string> m_locationScenes = new List<string>();

	// Token: 0x04001453 RID: 5203
	public List<GameObject> m_locationLists = new List<GameObject>();

	// Token: 0x04001454 RID: 5204
	public List<ZoneSystem.ZoneVegetation> m_vegetation = new List<ZoneSystem.ZoneVegetation>();

	// Token: 0x04001455 RID: 5205
	public List<ZoneSystem.ZoneLocation> m_locations = new List<ZoneSystem.ZoneLocation>();

	// Token: 0x04001456 RID: 5206
	private Dictionary<int, ZoneSystem.ZoneLocation> m_locationsByHash = new Dictionary<int, ZoneSystem.ZoneLocation>();

	// Token: 0x04001457 RID: 5207
	private bool m_error;

	// Token: 0x04001458 RID: 5208
	public bool m_didZoneTest;

	// Token: 0x04001459 RID: 5209
	private int m_terrainRayMask;

	// Token: 0x0400145A RID: 5210
	private int m_blockRayMask;

	// Token: 0x0400145B RID: 5211
	private int m_solidRayMask;

	// Token: 0x0400145C RID: 5212
	private int m_staticSolidRayMask;

	// Token: 0x0400145D RID: 5213
	private float m_updateTimer;

	// Token: 0x0400145E RID: 5214
	private float m_startTime;

	// Token: 0x0400145F RID: 5215
	private float m_lastFixedTime;

	// Token: 0x04001460 RID: 5216
	private Dictionary<Vector2i, ZoneSystem.ZoneData> m_zones = new Dictionary<Vector2i, ZoneSystem.ZoneData>();

	// Token: 0x04001461 RID: 5217
	private HashSet<Vector2i> m_generatedZones = new HashSet<Vector2i>();

	// Token: 0x04001462 RID: 5218
	private Dictionary<Vector2i, List<ZDO>> m_loadingObjectsInZones = new Dictionary<Vector2i, List<ZDO>>();

	// Token: 0x04001463 RID: 5219
	private Coroutine m_generateLocationsCoroutine;

	// Token: 0x04001464 RID: 5220
	private DateTime m_estimatedGenerateLocationsCompletionTime;

	// Token: 0x04001465 RID: 5221
	private float m_timeSlicedGenerationTimeBudget = 0.01f;

	// Token: 0x04001466 RID: 5222
	private bool m_locationsGenerated;

	// Token: 0x04001467 RID: 5223
	[HideInInspector]
	public Dictionary<Vector2i, ZoneSystem.LocationInstance> m_locationInstances = new Dictionary<Vector2i, ZoneSystem.LocationInstance>();

	// Token: 0x04001468 RID: 5224
	private Dictionary<Vector3, string> m_locationIcons = new Dictionary<Vector3, string>();

	// Token: 0x04001469 RID: 5225
	private HashSet<string> m_globalKeys = new HashSet<string>();

	// Token: 0x0400146A RID: 5226
	public HashSet<GlobalKeys> m_globalKeysEnums = new HashSet<GlobalKeys>();

	// Token: 0x0400146B RID: 5227
	public Dictionary<string, string> m_globalKeysValues = new Dictionary<string, string>();

	// Token: 0x0400146C RID: 5228
	private HashSet<Vector2i> m_tempGeneratedZonesSaveClone;

	// Token: 0x0400146D RID: 5229
	private HashSet<string> m_tempGlobalKeysSaveClone;

	// Token: 0x0400146E RID: 5230
	private List<ZoneSystem.LocationInstance> m_tempLocationsSaveClone;

	// Token: 0x0400146F RID: 5231
	private bool m_tempLocationsGeneratedSaveClone;

	// Token: 0x04001470 RID: 5232
	private List<ZoneSystem.ClearArea> m_tempClearAreas = new List<ZoneSystem.ClearArea>();

	// Token: 0x04001471 RID: 5233
	private List<GameObject> m_tempSpawnedObjects = new List<GameObject>();

	// Token: 0x04001472 RID: 5234
	private List<int> m_tempLocationPrefabsToRelease = new List<int>();

	// Token: 0x04001473 RID: 5235
	private List<ZoneSystem.LocationPrefabLoadData> m_locationPrefabs = new List<ZoneSystem.LocationPrefabLoadData>();

	// Token: 0x04001474 RID: 5236
	private Action m_generateLocationsCompleted;

	// Token: 0x02000338 RID: 824
	private class ZoneData
	{
		// Token: 0x0400248D RID: 9357
		public GameObject m_root;

		// Token: 0x0400248E RID: 9358
		public float m_ttl;
	}

	// Token: 0x02000339 RID: 825
	private class ClearArea
	{
		// Token: 0x0600226C RID: 8812 RVA: 0x000EDC1C File Offset: 0x000EBE1C
		public ClearArea(Vector3 p, float r)
		{
			this.m_center = p;
			this.m_radius = r;
		}

		// Token: 0x0400248F RID: 9359
		public Vector3 m_center;

		// Token: 0x04002490 RID: 9360
		public float m_radius;
	}

	// Token: 0x0200033A RID: 826
	[Serializable]
	public class ZoneVegetation
	{
		// Token: 0x0600226D RID: 8813 RVA: 0x000EDC32 File Offset: 0x000EBE32
		public ZoneSystem.ZoneVegetation Clone()
		{
			return base.MemberwiseClone() as ZoneSystem.ZoneVegetation;
		}

		// Token: 0x04002491 RID: 9361
		public string m_name = "veg";

		// Token: 0x04002492 RID: 9362
		public GameObject m_prefab;

		// Token: 0x04002493 RID: 9363
		public bool m_enable = true;

		// Token: 0x04002494 RID: 9364
		public float m_min;

		// Token: 0x04002495 RID: 9365
		public float m_max = 10f;

		// Token: 0x04002496 RID: 9366
		public bool m_forcePlacement;

		// Token: 0x04002497 RID: 9367
		public float m_scaleMin = 1f;

		// Token: 0x04002498 RID: 9368
		public float m_scaleMax = 1f;

		// Token: 0x04002499 RID: 9369
		public float m_randTilt;

		// Token: 0x0400249A RID: 9370
		public float m_chanceToUseGroundTilt;

		// Token: 0x0400249B RID: 9371
		[BitMask(typeof(Heightmap.Biome))]
		public Heightmap.Biome m_biome;

		// Token: 0x0400249C RID: 9372
		[BitMask(typeof(Heightmap.BiomeArea))]
		public Heightmap.BiomeArea m_biomeArea = Heightmap.BiomeArea.Everything;

		// Token: 0x0400249D RID: 9373
		public bool m_blockCheck = true;

		// Token: 0x0400249E RID: 9374
		public bool m_snapToStaticSolid;

		// Token: 0x0400249F RID: 9375
		public float m_minAltitude = -1000f;

		// Token: 0x040024A0 RID: 9376
		public float m_maxAltitude = 1000f;

		// Token: 0x040024A1 RID: 9377
		public float m_minVegetation;

		// Token: 0x040024A2 RID: 9378
		public float m_maxVegetation;

		// Token: 0x040024A3 RID: 9379
		[Header("Samples points around and choses the highest vegetation")]
		[global::Tooltip("Samples points around the placement point and choses the point with most total vegetation value")]
		public bool m_surroundCheckVegetation;

		// Token: 0x040024A4 RID: 9380
		[global::Tooltip("How far to check surroundings")]
		public float m_surroundCheckDistance = 20f;

		// Token: 0x040024A5 RID: 9381
		[global::Tooltip("How many layers of circles to sample. (If distance is large you should have more layers)")]
		public int m_surroundCheckLayers = 2;

		// Token: 0x040024A6 RID: 9382
		[global::Tooltip("How much better than the average an accepted point will be. (Procentually between average and best)")]
		public float m_surroundBetterThanAverage;

		// Token: 0x040024A7 RID: 9383
		[Space(10f)]
		public float m_minOceanDepth;

		// Token: 0x040024A8 RID: 9384
		public float m_maxOceanDepth;

		// Token: 0x040024A9 RID: 9385
		public float m_minTilt;

		// Token: 0x040024AA RID: 9386
		public float m_maxTilt = 90f;

		// Token: 0x040024AB RID: 9387
		public float m_terrainDeltaRadius;

		// Token: 0x040024AC RID: 9388
		public float m_maxTerrainDelta = 2f;

		// Token: 0x040024AD RID: 9389
		public float m_minTerrainDelta;

		// Token: 0x040024AE RID: 9390
		public bool m_snapToWater;

		// Token: 0x040024AF RID: 9391
		public float m_groundOffset;

		// Token: 0x040024B0 RID: 9392
		public int m_groupSizeMin = 1;

		// Token: 0x040024B1 RID: 9393
		public int m_groupSizeMax = 1;

		// Token: 0x040024B2 RID: 9394
		public float m_groupRadius;

		// Token: 0x040024B3 RID: 9395
		[Header("Distance from center")]
		public float m_minDistanceFromCenter;

		// Token: 0x040024B4 RID: 9396
		public float m_maxDistanceFromCenter;

		// Token: 0x040024B5 RID: 9397
		[Header("Forest fractal 0-1 inside forest")]
		public bool m_inForest;

		// Token: 0x040024B6 RID: 9398
		public float m_forestTresholdMin;

		// Token: 0x040024B7 RID: 9399
		public float m_forestTresholdMax = 1f;

		// Token: 0x040024B8 RID: 9400
		[HideInInspector]
		public bool m_foldout;
	}

	// Token: 0x0200033B RID: 827
	[Serializable]
	public class ZoneLocation
	{
		// Token: 0x0600226F RID: 8815 RVA: 0x000EDCEB File Offset: 0x000EBEEB
		public ZoneSystem.ZoneLocation Clone()
		{
			return base.MemberwiseClone() as ZoneSystem.ZoneLocation;
		}

		// Token: 0x170001C9 RID: 457
		// (get) Token: 0x06002270 RID: 8816 RVA: 0x000EDCF8 File Offset: 0x000EBEF8
		public int Hash
		{
			get
			{
				return this.m_prefab.Name.GetStableHashCode();
			}
		}

		// Token: 0x040024B9 RID: 9401
		public string m_name;

		// Token: 0x040024BA RID: 9402
		public bool m_enable = true;

		// Token: 0x040024BB RID: 9403
		[HideInInspector]
		public string m_prefabName;

		// Token: 0x040024BC RID: 9404
		public SoftReference<GameObject> m_prefab;

		// Token: 0x040024BD RID: 9405
		[BitMask(typeof(Heightmap.Biome))]
		public Heightmap.Biome m_biome;

		// Token: 0x040024BE RID: 9406
		[BitMask(typeof(Heightmap.BiomeArea))]
		public Heightmap.BiomeArea m_biomeArea = Heightmap.BiomeArea.Everything;

		// Token: 0x040024BF RID: 9407
		public int m_quantity;

		// Token: 0x040024C0 RID: 9408
		public bool m_prioritized;

		// Token: 0x040024C1 RID: 9409
		public bool m_centerFirst;

		// Token: 0x040024C2 RID: 9410
		public bool m_unique;

		// Token: 0x040024C3 RID: 9411
		public string m_group = "";

		// Token: 0x040024C4 RID: 9412
		public float m_minDistanceFromSimilar;

		// Token: 0x040024C5 RID: 9413
		public string m_groupMax = "";

		// Token: 0x040024C6 RID: 9414
		public float m_maxDistanceFromSimilar;

		// Token: 0x040024C7 RID: 9415
		public bool m_iconAlways;

		// Token: 0x040024C8 RID: 9416
		public bool m_iconPlaced;

		// Token: 0x040024C9 RID: 9417
		public bool m_randomRotation = true;

		// Token: 0x040024CA RID: 9418
		public bool m_slopeRotation;

		// Token: 0x040024CB RID: 9419
		public bool m_snapToWater;

		// Token: 0x040024CC RID: 9420
		public float m_interiorRadius;

		// Token: 0x040024CD RID: 9421
		public float m_exteriorRadius;

		// Token: 0x040024CE RID: 9422
		public bool m_clearArea;

		// Token: 0x040024CF RID: 9423
		public float m_minTerrainDelta;

		// Token: 0x040024D0 RID: 9424
		public float m_maxTerrainDelta = 2f;

		// Token: 0x040024D1 RID: 9425
		public float m_minimumVegetation;

		// Token: 0x040024D2 RID: 9426
		public float m_maximumVegetation = 1f;

		// Token: 0x040024D3 RID: 9427
		[Header("Samples points around and choses the highest vegetation")]
		[global::Tooltip("Samples points around the placement point and choses the point with most total vegetation value")]
		public bool m_surroundCheckVegetation;

		// Token: 0x040024D4 RID: 9428
		[global::Tooltip("How far to check surroundings")]
		public float m_surroundCheckDistance = 20f;

		// Token: 0x040024D5 RID: 9429
		[global::Tooltip("How many layers of circles to sample. (If distance is large you should have more layers)")]
		public int m_surroundCheckLayers = 2;

		// Token: 0x040024D6 RID: 9430
		[global::Tooltip("How much better than the average an accepted point will be. (Procentually between average and best)")]
		public float m_surroundBetterThanAverage;

		// Token: 0x040024D7 RID: 9431
		[Header("Forest fractal 0-1 inside forest")]
		public bool m_inForest;

		// Token: 0x040024D8 RID: 9432
		public float m_forestTresholdMin;

		// Token: 0x040024D9 RID: 9433
		public float m_forestTresholdMax = 1f;

		// Token: 0x040024DA RID: 9434
		[Header("Distance from center")]
		public float m_minDistanceFromCenter;

		// Token: 0x040024DB RID: 9435
		public float m_maxDistanceFromCenter;

		// Token: 0x040024DC RID: 9436
		[Space(10f)]
		public float m_minDistance;

		// Token: 0x040024DD RID: 9437
		public float m_maxDistance;

		// Token: 0x040024DE RID: 9438
		public float m_minAltitude = -1000f;

		// Token: 0x040024DF RID: 9439
		public float m_maxAltitude = 1000f;

		// Token: 0x040024E0 RID: 9440
		[HideInInspector]
		public bool m_foldout;
	}

	// Token: 0x0200033C RID: 828
	public struct LocationInstance
	{
		// Token: 0x040024E1 RID: 9441
		public ZoneSystem.ZoneLocation m_location;

		// Token: 0x040024E2 RID: 9442
		public Vector3 m_position;

		// Token: 0x040024E3 RID: 9443
		public bool m_placed;
	}

	// Token: 0x0200033D RID: 829
	private class LocationPrefabLoadData
	{
		// Token: 0x170001CA RID: 458
		// (get) Token: 0x06002272 RID: 8818 RVA: 0x000EDD93 File Offset: 0x000EBF93
		// (set) Token: 0x06002273 RID: 8819 RVA: 0x000EDD9B File Offset: 0x000EBF9B
		public bool IsLoaded { get; private set; }

		// Token: 0x170001CB RID: 459
		// (get) Token: 0x06002274 RID: 8820 RVA: 0x000EDDA4 File Offset: 0x000EBFA4
		public AssetID PrefabAssetID
		{
			get
			{
				return this.m_prefab.m_assetID;
			}
		}

		// Token: 0x06002275 RID: 8821 RVA: 0x000EDDB1 File Offset: 0x000EBFB1
		public LocationPrefabLoadData(SoftReference<GameObject> prefab, bool isFirstSpawn)
		{
			this.m_prefab = prefab;
			this.m_isFirstSpawn = isFirstSpawn;
			this.m_roomsToLoad = 0;
			this.m_prefab.LoadAsync(new LoadedHandler(this.OnPrefabLoaded));
		}

		// Token: 0x06002276 RID: 8822 RVA: 0x000EDDE8 File Offset: 0x000EBFE8
		public void Release()
		{
			if (!this.m_prefab.IsValid)
			{
				return;
			}
			this.m_prefab.Release();
			this.m_prefab.m_assetID = default(AssetID);
			if (this.m_possibleRooms == null)
			{
				return;
			}
			for (int i = 0; i < this.m_possibleRooms.Length; i++)
			{
				this.m_possibleRooms[i].Release();
			}
			this.m_possibleRooms = null;
		}

		// Token: 0x06002277 RID: 8823 RVA: 0x000EDE54 File Offset: 0x000EC054
		private void OnPrefabLoaded(AssetID assetID, LoadResult result)
		{
			if (result != LoadResult.Succeeded)
			{
				return;
			}
			if (!this.m_prefab.IsValid)
			{
				return;
			}
			if (!this.m_isFirstSpawn)
			{
				this.IsLoaded = true;
				return;
			}
			DungeonGenerator[] enabledComponentsInChildren = global::Utils.GetEnabledComponentsInChildren<DungeonGenerator>(this.m_prefab.Asset);
			if (enabledComponentsInChildren.Length == 0)
			{
				this.IsLoaded = true;
				return;
			}
			if (enabledComponentsInChildren.Length > 1)
			{
				ZLog.LogWarning("Location " + this.m_prefab.Asset.name + " has more than one dungeon generator! The preloading code only works for one dungeon generator per location.");
			}
			this.m_possibleRooms = enabledComponentsInChildren[0].GetAvailableRoomPrefabs();
			this.m_roomsToLoad = this.m_possibleRooms.Length;
			for (int i = 0; i < this.m_possibleRooms.Length; i++)
			{
				this.m_possibleRooms[i].LoadAsync(new LoadedHandler(this.OnRoomLoaded));
			}
		}

		// Token: 0x06002278 RID: 8824 RVA: 0x000EDF16 File Offset: 0x000EC116
		private void OnRoomLoaded(AssetID assetID, LoadResult result)
		{
			if (result != LoadResult.Succeeded)
			{
				return;
			}
			this.m_roomsToLoad--;
			if (this.m_possibleRooms == null)
			{
				return;
			}
			if (this.m_roomsToLoad > 0)
			{
				return;
			}
			this.IsLoaded = true;
		}

		// Token: 0x040024E4 RID: 9444
		private SoftReference<GameObject> m_prefab;

		// Token: 0x040024E5 RID: 9445
		private SoftReference<GameObject>[] m_possibleRooms;

		// Token: 0x040024E6 RID: 9446
		private int m_roomsToLoad;

		// Token: 0x040024E7 RID: 9447
		private bool m_isFirstSpawn;

		// Token: 0x040024E8 RID: 9448
		public int m_iterationLifetime;
	}

	// Token: 0x0200033E RID: 830
	public enum SpawnMode
	{
		// Token: 0x040024EB RID: 9451
		Full,
		// Token: 0x040024EC RID: 9452
		Client,
		// Token: 0x040024ED RID: 9453
		Ghost
	}
}
