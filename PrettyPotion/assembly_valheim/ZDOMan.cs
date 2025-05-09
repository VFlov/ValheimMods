using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using UnityEngine;

// Token: 0x020000D4 RID: 212
public class ZDOMan
{
	// Token: 0x17000076 RID: 118
	// (get) Token: 0x06000D34 RID: 3380 RVA: 0x00066596 File Offset: 0x00064796
	public static ZDOMan instance
	{
		get
		{
			return ZDOMan.s_instance;
		}
	}

	// Token: 0x06000D35 RID: 3381 RVA: 0x000665A0 File Offset: 0x000647A0
	public ZDOMan(int width)
	{
		ZDOMan.s_instance = this;
		ZRoutedRpc.instance.Register<ZPackage>("DestroyZDO", new Action<long, ZPackage>(this.RPC_DestroyZDO));
		ZRoutedRpc.instance.Register<ZDOID>("RequestZDO", new Action<long, ZDOID>(this.RPC_RequestZDO));
		this.m_width = width;
		this.m_halfWidth = this.m_width / 2;
		int num = ZoneSystem.instance.m_activeArea * 2 + 1;
		num *= num;
		for (int i = 0; i < num; i++)
		{
			this.m_tempNearObjectsForRemoval.Add(new List<ZDO>());
			this.m_tempNearObjectsForRemovalAreasActive.Add(false);
		}
		ZDOID.Reset();
		this.ResetSectorArray();
		ZDOExtraData.Init();
	}

	// Token: 0x06000D36 RID: 3382 RVA: 0x00066702 File Offset: 0x00064902
	private void ResetSectorArray()
	{
		this.m_objectsBySector = new List<ZDO>[this.m_width * this.m_width];
		this.m_objectsByOutsideSector.Clear();
	}

	// Token: 0x06000D37 RID: 3383 RVA: 0x00066728 File Offset: 0x00064928
	public void ShutDown()
	{
		if (!ZNet.instance.IsServer())
		{
			this.FlushClientObjects();
		}
		ZDOPool.Release(this.m_objectsByID);
		this.m_objectsByID.Clear();
		this.m_tempToSync.Clear();
		this.m_tempToSyncDistant.Clear();
		this.m_tempNearObjects.Clear();
		this.m_tempRemoveList.Clear();
		this.m_peers.Clear();
		this.ResetSectorArray();
		ZDOExtraData.Reset();
		Game.instance.CollectResources(false);
	}

	// Token: 0x06000D38 RID: 3384 RVA: 0x000667AC File Offset: 0x000649AC
	public void PrepareSave()
	{
		this.m_saveData = new ZDOMan.SaveData();
		this.m_saveData.m_sessionID = this.m_sessionID;
		this.m_saveData.m_nextUid = this.m_nextUid;
		Stopwatch stopwatch = Stopwatch.StartNew();
		this.m_saveData.m_zdos = this.GetSaveClone();
		ZLog.Log("PrepareSave: clone done in " + stopwatch.ElapsedMilliseconds.ToString() + "ms");
		stopwatch = Stopwatch.StartNew();
		ZDOExtraData.PrepareSave();
		ZLog.Log("PrepareSave: ZDOExtraData.PrepareSave done in " + stopwatch.ElapsedMilliseconds.ToString() + " ms");
	}

	// Token: 0x06000D39 RID: 3385 RVA: 0x0006684C File Offset: 0x00064A4C
	public void SaveAsync(BinaryWriter writer)
	{
		writer.Write(this.m_saveData.m_sessionID);
		writer.Write(this.m_saveData.m_nextUid);
		ZPackage zpackage = new ZPackage();
		writer.Write(this.m_saveData.m_zdos.Count);
		zpackage.SetWriter(writer);
		foreach (ZDO zdo in this.m_saveData.m_zdos)
		{
			zdo.Save(zpackage);
		}
		ZLog.Log("Saved " + this.m_saveData.m_zdos.Count.ToString() + " ZDOs");
		foreach (ZDO zdo2 in this.m_saveData.m_zdos)
		{
			zdo2.Reset();
		}
		this.m_saveData.m_zdos.Clear();
		this.m_saveData = null;
		ZDOExtraData.ClearSave();
	}

	// Token: 0x06000D3A RID: 3386 RVA: 0x00066974 File Offset: 0x00064B74
	private void FilterZDO(ZDO zdo, ref List<ZDO> zdos, ref List<ZDO> warningZDOs, ref List<ZDO> brokenZDOs)
	{
		if (ZDOMan.s_brokenPrefabsToFilterOut.Contains(zdo.GetPrefab()))
		{
			brokenZDOs.Add(zdo);
			return;
		}
		if (!ZNetScene.instance.HasPrefab(zdo.GetPrefab()))
		{
			warningZDOs.Add(zdo);
		}
		zdos.Add(zdo);
	}

	// Token: 0x06000D3B RID: 3387 RVA: 0x000669B4 File Offset: 0x00064BB4
	private void WarnAndRemoveBrokenZDOs(List<ZDO> warningZDOs, List<ZDO> brokenZDOs, int totalNumZDOs, int numZDOs)
	{
		if (warningZDOs.Count > 0)
		{
			string str = "Found ";
			int num = warningZDOs.Count;
			ZLog.LogWarning(str + num.ToString() + " ZDOs with unknown prefabs. Will load anyway.");
			Dictionary<int, int> dictionary = new Dictionary<int, int>();
			foreach (ZDO zdo in warningZDOs)
			{
				int prefab = zdo.GetPrefab();
				if (!dictionary.TryAdd(prefab, 1))
				{
					Dictionary<int, int> dictionary2 = dictionary;
					num = prefab;
					int num2 = dictionary2[num];
					dictionary2[num] = num2 + 1;
				}
			}
			foreach (KeyValuePair<int, int> keyValuePair in dictionary)
			{
				int num2;
				keyValuePair.Deconstruct(out num2, out num);
				int num3 = num2;
				int num4 = num;
				ZLog.LogWarning(string.Concat(new string[]
				{
					"    Hash ",
					num3.ToString(),
					" appeared ",
					num4.ToString(),
					" times."
				}));
			}
		}
		if (brokenZDOs.Count > 0)
		{
			string[] array = new string[7];
			array[0] = "Found ";
			int num5 = 1;
			int num = brokenZDOs.Count;
			array[num5] = num.ToString();
			array[2] = " ZDOs with prefabs not supported. Removing. ";
			array[3] = totalNumZDOs.ToString();
			array[4] = " => ";
			array[5] = numZDOs.ToString();
			array[6] = " ZDOs loaded.";
			ZLog.LogError(string.Concat(array));
			Dictionary<int, int> dictionary3 = new Dictionary<int, int>();
			foreach (ZDO zdo2 in brokenZDOs)
			{
				int prefab2 = zdo2.GetPrefab();
				if (!dictionary3.TryAdd(prefab2, 1))
				{
					Dictionary<int, int> dictionary4 = dictionary3;
					num = prefab2;
					int num2 = dictionary4[num];
					dictionary4[num] = num2 + 1;
				}
				ZDOPool.Release(zdo2);
			}
			foreach (KeyValuePair<int, int> keyValuePair in dictionary3)
			{
				int num2;
				keyValuePair.Deconstruct(out num2, out num);
				int num6 = num2;
				int num7 = num;
				ZLog.LogError(string.Concat(new string[]
				{
					"    Hash ",
					num6.ToString(),
					" filtered out ",
					num7.ToString(),
					" times."
				}));
			}
		}
	}

	// Token: 0x06000D3C RID: 3388 RVA: 0x00066C30 File Offset: 0x00064E30
	public void Load(BinaryReader reader, int version)
	{
		reader.ReadInt64();
		uint nextUid = reader.ReadUInt32();
		int num = reader.ReadInt32();
		ZDOPool.Release(this.m_objectsByID);
		this.m_objectsByID.Clear();
		this.ResetSectorArray();
		ZDOExtraData.Init();
		ZLog.Log(string.Concat(new string[]
		{
			"Loading ",
			num.ToString(),
			" zdos, my sessionID: ",
			this.m_sessionID.ToString(),
			", data version: ",
			version.ToString()
		}));
		List<ZDO> list = new List<ZDO>();
		list.Capacity = num;
		ZNetScene instance = ZNetScene.instance;
		List<ZDO> brokenZDOs = new List<ZDO>();
		List<ZDO> warningZDOs = new List<ZDO>();
		ZLog.Log("Loading in ZDOs");
		ZPackage zpackage = new ZPackage();
		if (version < 31)
		{
			for (int i = 0; i < num; i++)
			{
				ZDO zdo = ZDOPool.Create();
				zdo.m_uid = new ZDOID(reader);
				int count = reader.ReadInt32();
				byte[] data = reader.ReadBytes(count);
				zpackage.Load(data);
				zdo.LoadOldFormat(zpackage, version);
				zdo.SetOwner(0L);
				this.FilterZDO(zdo, ref list, ref warningZDOs, ref brokenZDOs);
			}
		}
		else
		{
			zpackage.SetReader(reader);
			for (int j = 0; j < num; j++)
			{
				ZDO zdo2 = ZDOPool.Create();
				zdo2.Load(zpackage, version);
				this.FilterZDO(zdo2, ref list, ref warningZDOs, ref brokenZDOs);
			}
			nextUid = (uint)(list.Count + 1);
		}
		this.WarnAndRemoveBrokenZDOs(warningZDOs, brokenZDOs, num, list.Count);
		ZLog.Log("Adding to Dictionary");
		foreach (ZDO zdo3 in list)
		{
			this.m_objectsByID.Add(zdo3.m_uid, zdo3);
			if (Game.instance.PortalPrefabHash.Contains(zdo3.GetPrefab()))
			{
				this.m_portalObjects.Add(zdo3);
			}
		}
		ZLog.Log("Adding to Sectors");
		foreach (ZDO zdo4 in list)
		{
			this.AddToSector(zdo4, zdo4.GetSector());
		}
		if (version < 31)
		{
			ZLog.Log("Converting Ships & Fishing-rods ownership");
			this.ConvertOwnerships(list);
			ZLog.Log("Converting & mapping CreationTime");
			this.ConvertCreationTime(list);
			ZLog.Log("Converting portals");
			this.ConvertPortals();
			ZLog.Log("Converting spawners");
			this.ConvertSpawners();
			ZLog.Log("Converting ZSyncTransforms");
			this.ConvertSyncTransforms();
			ZLog.Log("Converting ItemSeeds");
			this.ConvertSeed();
			ZLog.Log("Converting Dungeons");
			this.ConvertDungeonRooms(list);
		}
		else
		{
			ZLog.Log("Connecting Portals, Spawners & ZSyncTransforms");
			this.ConnectPortals();
			this.ConnectSpawners();
			this.ConnectSyncTransforms();
		}
		Game.instance.ConnectPortals();
		this.m_deadZDOs.Clear();
		if (version < 31)
		{
			int num2 = reader.ReadInt32();
			for (int k = 0; k < num2; k++)
			{
				reader.ReadInt64();
				reader.ReadUInt32();
				reader.ReadInt64();
			}
		}
		this.m_nextUid = nextUid;
	}

	// Token: 0x06000D3D RID: 3389 RVA: 0x00066F60 File Offset: 0x00065160
	public ZDO CreateNewZDO(Vector3 position, int prefabHash)
	{
		long sessionID = this.m_sessionID;
		uint nextUid = this.m_nextUid;
		this.m_nextUid = nextUid + 1U;
		ZDOID zdoid = new ZDOID(sessionID, nextUid);
		while (this.GetZDO(zdoid) != null)
		{
			long sessionID2 = this.m_sessionID;
			nextUid = this.m_nextUid;
			this.m_nextUid = nextUid + 1U;
			zdoid = new ZDOID(sessionID2, nextUid);
		}
		return this.CreateNewZDO(zdoid, position, prefabHash);
	}

	// Token: 0x06000D3E RID: 3390 RVA: 0x00066FC0 File Offset: 0x000651C0
	private ZDO CreateNewZDO(ZDOID uid, Vector3 position, int prefabHashIn = 0)
	{
		ZDO zdo = ZDOPool.Create(uid, position);
		zdo.SetOwnerInternal(this.m_sessionID);
		this.m_objectsByID.Add(uid, zdo);
		int item = (prefabHashIn != 0) ? prefabHashIn : zdo.GetPrefab();
		if (Game.instance.PortalPrefabHash.Contains(item))
		{
			this.m_portalObjects.Add(zdo);
		}
		return zdo;
	}

	// Token: 0x06000D3F RID: 3391 RVA: 0x0006701C File Offset: 0x0006521C
	public void AddToSector(ZDO zdo, Vector2i sector)
	{
		int num = this.SectorToIndex(sector);
		if (num >= 0)
		{
			if (this.m_objectsBySector[num] != null)
			{
				this.m_objectsBySector[num].Add(zdo);
				return;
			}
			List<ZDO> list = new List<ZDO>();
			list.Add(zdo);
			this.m_objectsBySector[num] = list;
			return;
		}
		else
		{
			List<ZDO> list2;
			if (this.m_objectsByOutsideSector.TryGetValue(sector, out list2))
			{
				list2.Add(zdo);
				return;
			}
			list2 = new List<ZDO>();
			list2.Add(zdo);
			this.m_objectsByOutsideSector.Add(sector, list2);
			return;
		}
	}

	// Token: 0x06000D40 RID: 3392 RVA: 0x00067098 File Offset: 0x00065298
	public void ZDOSectorInvalidated(ZDO zdo)
	{
		foreach (ZDOMan.ZDOPeer zdopeer in this.m_peers)
		{
			zdopeer.ZDOSectorInvalidated(zdo);
		}
	}

	// Token: 0x06000D41 RID: 3393 RVA: 0x000670EC File Offset: 0x000652EC
	public void RemoveFromSector(ZDO zdo, Vector2i sector)
	{
		int num = this.SectorToIndex(sector);
		List<ZDO> list;
		if (num >= 0)
		{
			if (this.m_objectsBySector[num] != null)
			{
				this.m_objectsBySector[num].Remove(zdo);
				return;
			}
		}
		else if (this.m_objectsByOutsideSector.TryGetValue(sector, out list))
		{
			list.Remove(zdo);
		}
	}

	// Token: 0x06000D42 RID: 3394 RVA: 0x00067138 File Offset: 0x00065338
	public ZDO GetZDO(ZDOID id)
	{
		if (id == ZDOID.None)
		{
			return null;
		}
		ZDO result;
		if (this.m_objectsByID.TryGetValue(id, out result))
		{
			return result;
		}
		return null;
	}

	// Token: 0x06000D43 RID: 3395 RVA: 0x00067168 File Offset: 0x00065368
	public void AddPeer(ZNetPeer netPeer)
	{
		ZDOMan.ZDOPeer zdopeer = new ZDOMan.ZDOPeer();
		zdopeer.m_peer = netPeer;
		this.m_peers.Add(zdopeer);
		zdopeer.m_peer.m_rpc.Register<ZPackage>("ZDOData", new Action<ZRpc, ZPackage>(this.RPC_ZDOData));
	}

	// Token: 0x06000D44 RID: 3396 RVA: 0x000671B0 File Offset: 0x000653B0
	public void RemovePeer(ZNetPeer netPeer)
	{
		ZDOMan.ZDOPeer zdopeer = this.FindPeer(netPeer);
		if (zdopeer != null)
		{
			this.m_peers.Remove(zdopeer);
			if (ZNet.instance.IsServer())
			{
				this.RemoveOrphanNonPersistentZDOS();
			}
		}
	}

	// Token: 0x06000D45 RID: 3397 RVA: 0x000671E8 File Offset: 0x000653E8
	private ZDOMan.ZDOPeer FindPeer(ZNetPeer netPeer)
	{
		foreach (ZDOMan.ZDOPeer zdopeer in this.m_peers)
		{
			if (zdopeer.m_peer == netPeer)
			{
				return zdopeer;
			}
		}
		return null;
	}

	// Token: 0x06000D46 RID: 3398 RVA: 0x00067244 File Offset: 0x00065444
	private ZDOMan.ZDOPeer FindPeer(ZRpc rpc)
	{
		foreach (ZDOMan.ZDOPeer zdopeer in this.m_peers)
		{
			if (zdopeer.m_peer.m_rpc == rpc)
			{
				return zdopeer;
			}
		}
		return null;
	}

	// Token: 0x06000D47 RID: 3399 RVA: 0x000672A8 File Offset: 0x000654A8
	public void Update(float dt)
	{
		if (ZNet.instance.IsServer())
		{
			this.ReleaseZDOS(dt);
		}
		this.SendZDOToPeers2(dt);
		this.SendDestroyed();
		this.UpdateStats(dt);
	}

	// Token: 0x06000D48 RID: 3400 RVA: 0x000672D4 File Offset: 0x000654D4
	private void UpdateStats(float dt)
	{
		this.m_statTimer += dt;
		if (this.m_statTimer >= 1f)
		{
			this.m_statTimer = 0f;
			this.m_zdosSentLastSec = this.m_zdosSent;
			this.m_zdosRecvLastSec = this.m_zdosRecv;
			this.m_zdosRecv = 0;
			this.m_zdosSent = 0;
		}
	}

	// Token: 0x06000D49 RID: 3401 RVA: 0x00067330 File Offset: 0x00065530
	private void SendZDOToPeers2(float dt)
	{
		if (this.m_peers.Count == 0)
		{
			return;
		}
		this.m_sendTimer += dt;
		if (this.m_nextSendPeer < 0)
		{
			if (this.m_sendTimer > 0.05f)
			{
				this.m_nextSendPeer = 0;
				this.m_sendTimer = 0f;
				return;
			}
		}
		else
		{
			if (this.m_nextSendPeer < this.m_peers.Count)
			{
				this.SendZDOs(this.m_peers[this.m_nextSendPeer], false);
			}
			this.m_nextSendPeer++;
			if (this.m_nextSendPeer >= this.m_peers.Count)
			{
				this.m_nextSendPeer = -1;
			}
		}
	}

	// Token: 0x06000D4A RID: 3402 RVA: 0x000673D8 File Offset: 0x000655D8
	private void FlushClientObjects()
	{
		foreach (ZDOMan.ZDOPeer peer in this.m_peers)
		{
			this.SendAllZDOs(peer);
		}
	}

	// Token: 0x06000D4B RID: 3403 RVA: 0x0006742C File Offset: 0x0006562C
	private void ReleaseZDOS(float dt)
	{
		this.m_releaseZDOTimer += dt;
		if (this.m_releaseZDOTimer > 2f)
		{
			this.m_releaseZDOTimer = 0f;
			this.ReleaseNearbyZDOS(ZNet.instance.GetReferencePosition(), this.m_sessionID);
			foreach (ZDOMan.ZDOPeer zdopeer in this.m_peers)
			{
				this.ReleaseNearbyZDOS(zdopeer.m_peer.m_refPos, zdopeer.m_peer.m_uid);
			}
		}
	}

	// Token: 0x06000D4C RID: 3404 RVA: 0x000674D0 File Offset: 0x000656D0
	private bool IsInPeerActiveArea(Vector2i sector, long uid)
	{
		if (uid == this.m_sessionID)
		{
			return ZNetScene.InActiveArea(sector, ZNet.instance.GetReferencePosition());
		}
		ZNetPeer peer = ZNet.instance.GetPeer(uid);
		return peer != null && ZNetScene.InActiveArea(sector, peer.GetRefPos());
	}

	// Token: 0x06000D4D RID: 3405 RVA: 0x00067514 File Offset: 0x00065714
	private void ReleaseNearbyZDOS(Vector3 refPosition, long uid)
	{
		Vector2i zone = ZoneSystem.GetZone(refPosition);
		this.m_tempNearObjects.Clear();
		this.FindSectorObjects(zone, ZoneSystem.instance.m_activeArea, 0, this.m_tempNearObjects, null);
		int activatedArea = ZoneSystem.instance.m_activeArea - 1;
		foreach (ZDO zdo in this.m_tempNearObjects)
		{
			if (zdo.Persistent)
			{
				Vector2i sector = zdo.GetSector();
				if (zdo.GetOwner() == uid)
				{
					if (!ZNetScene.InActiveArea(sector, zone, activatedArea))
					{
						zdo.SetOwner(0L);
					}
				}
				else if ((!zdo.HasOwner() || !this.IsInPeerActiveArea(sector, zdo.GetOwner())) && ZNetScene.InActiveArea(sector, zone, activatedArea))
				{
					zdo.SetOwner(uid);
				}
			}
		}
	}

	// Token: 0x06000D4E RID: 3406 RVA: 0x000675F0 File Offset: 0x000657F0
	public void DestroyZDO(ZDO zdo)
	{
		if (!zdo.IsOwner())
		{
			return;
		}
		this.m_destroySendList.Add(zdo.m_uid);
	}

	// Token: 0x06000D4F RID: 3407 RVA: 0x0006760C File Offset: 0x0006580C
	private void SendDestroyed()
	{
		if (this.m_destroySendList.Count == 0)
		{
			return;
		}
		ZPackage zpackage = new ZPackage();
		zpackage.Write(this.m_destroySendList.Count);
		foreach (ZDOID id in this.m_destroySendList)
		{
			zpackage.Write(id);
		}
		this.m_destroySendList.Clear();
		ZRoutedRpc.instance.InvokeRoutedRPC(ZRoutedRpc.Everybody, "DestroyZDO", new object[]
		{
			zpackage
		});
	}

	// Token: 0x06000D50 RID: 3408 RVA: 0x000676B0 File Offset: 0x000658B0
	private void RPC_DestroyZDO(long sender, ZPackage pkg)
	{
		int num = pkg.ReadInt();
		for (int i = 0; i < num; i++)
		{
			ZDOID uid = pkg.ReadZDOID();
			this.HandleDestroyedZDO(uid);
		}
	}

	// Token: 0x06000D51 RID: 3409 RVA: 0x000676E0 File Offset: 0x000658E0
	private void HandleDestroyedZDO(ZDOID uid)
	{
		if (uid.UserID == this.m_sessionID && uid.ID >= this.m_nextUid)
		{
			this.m_nextUid = uid.ID + 1U;
		}
		ZDO zdo = this.GetZDO(uid);
		if (zdo == null)
		{
			return;
		}
		if (this.m_onZDODestroyed != null)
		{
			this.m_onZDODestroyed(zdo);
		}
		this.RemoveFromSector(zdo, zdo.GetSector());
		this.m_objectsByID.Remove(zdo.m_uid);
		if (Game.instance.PortalPrefabHash.Contains(zdo.GetPrefab()))
		{
			this.m_portalObjects.Remove(zdo);
		}
		ZDOPool.Release(zdo);
		foreach (ZDOMan.ZDOPeer zdopeer in this.m_peers)
		{
			zdopeer.m_zdos.Remove(uid);
		}
		if (ZNet.instance.IsServer())
		{
			long ticks = ZNet.instance.GetTime().Ticks;
			this.m_deadZDOs[uid] = ticks;
		}
	}

	// Token: 0x06000D52 RID: 3410 RVA: 0x000677FC File Offset: 0x000659FC
	private void SendAllZDOs(ZDOMan.ZDOPeer peer)
	{
		while (this.SendZDOs(peer, true))
		{
		}
	}

	// Token: 0x06000D53 RID: 3411 RVA: 0x00067808 File Offset: 0x00065A08
	private bool SendZDOs(ZDOMan.ZDOPeer peer, bool flush)
	{
		int sendQueueSize = peer.m_peer.m_socket.GetSendQueueSize();
		if (!flush && sendQueueSize > 10240)
		{
			return false;
		}
		int num = 10240 - sendQueueSize;
		if (num < 2048)
		{
			return false;
		}
		this.m_tempToSync.Clear();
		this.CreateSyncList(peer, this.m_tempToSync);
		if (this.m_tempToSync.Count == 0 && peer.m_invalidSector.Count == 0)
		{
			return false;
		}
		ZPackage zpackage = new ZPackage();
		bool flag = false;
		if (peer.m_invalidSector.Count > 0)
		{
			flag = true;
			zpackage.Write(peer.m_invalidSector.Count);
			foreach (ZDOID id in peer.m_invalidSector)
			{
				zpackage.Write(id);
			}
			peer.m_invalidSector.Clear();
		}
		else
		{
			zpackage.Write(0);
		}
		float time = Time.time;
		ZPackage zpackage2 = new ZPackage();
		bool flag2 = false;
		foreach (ZDO zdo in this.m_tempToSync)
		{
			if (zpackage.Size() > num)
			{
				break;
			}
			peer.m_forceSend.Remove(zdo.m_uid);
			if (!ZNet.instance.IsServer())
			{
				this.m_clientChangeQueue.Remove(zdo.m_uid);
			}
			zpackage.Write(zdo.m_uid);
			zpackage.Write(zdo.OwnerRevision);
			zpackage.Write(zdo.DataRevision);
			zpackage.Write(zdo.GetOwner());
			zpackage.Write(zdo.GetPosition());
			zpackage2.Clear();
			zdo.Serialize(zpackage2);
			zpackage.Write(zpackage2);
			peer.m_zdos[zdo.m_uid] = new ZDOMan.ZDOPeer.PeerZDOInfo(zdo.DataRevision, zdo.OwnerRevision, time);
			flag2 = true;
			this.m_zdosSent++;
		}
		zpackage.Write(ZDOID.None);
		if (flag2 || flag)
		{
			peer.m_peer.m_rpc.Invoke("ZDOData", new object[]
			{
				zpackage
			});
		}
		return flag2 || flag;
	}

	// Token: 0x06000D54 RID: 3412 RVA: 0x00067A58 File Offset: 0x00065C58
	private void RPC_ZDOData(ZRpc rpc, ZPackage pkg)
	{
		ZDOMan.ZDOPeer zdopeer = this.FindPeer(rpc);
		if (zdopeer == null)
		{
			ZLog.Log("ZDO data from unkown host, ignoring");
			return;
		}
		float time = Time.time;
		int num = 0;
		ZPackage pkg2 = new ZPackage();
		int num2 = pkg.ReadInt();
		for (int i = 0; i < num2; i++)
		{
			ZDOID id = pkg.ReadZDOID();
			ZDO zdo = this.GetZDO(id);
			if (zdo != null)
			{
				zdo.InvalidateSector();
			}
		}
		for (;;)
		{
			ZDOID zdoid = pkg.ReadZDOID();
			if (zdoid.IsNone())
			{
				break;
			}
			num++;
			ushort num3 = pkg.ReadUShort();
			uint num4 = pkg.ReadUInt();
			long ownerInternal = pkg.ReadLong();
			Vector3 vector = pkg.ReadVector3();
			pkg.ReadPackage(ref pkg2);
			ZDO zdo2 = this.GetZDO(zdoid);
			bool flag = false;
			if (zdo2 != null)
			{
				if (num4 <= zdo2.DataRevision)
				{
					if (num3 > zdo2.OwnerRevision)
					{
						zdo2.SetOwnerInternal(ownerInternal);
						zdo2.OwnerRevision = num3;
						zdopeer.m_zdos[zdoid] = new ZDOMan.ZDOPeer.PeerZDOInfo(num4, num3, time);
						continue;
					}
					continue;
				}
			}
			else
			{
				zdo2 = this.CreateNewZDO(zdoid, vector, 0);
				flag = true;
			}
			zdo2.OwnerRevision = num3;
			zdo2.DataRevision = num4;
			zdo2.SetOwnerInternal(ownerInternal);
			zdo2.InternalSetPosition(vector);
			zdopeer.m_zdos[zdoid] = new ZDOMan.ZDOPeer.PeerZDOInfo(zdo2.DataRevision, zdo2.OwnerRevision, time);
			zdo2.Deserialize(pkg2);
			if (Game.instance.PortalPrefabHash.Contains(zdo2.GetPrefab()))
			{
				this.AddPortal(zdo2);
			}
			if (ZNet.instance.IsServer() && flag && this.m_deadZDOs.ContainsKey(zdoid))
			{
				zdo2.SetOwner(this.m_sessionID);
				this.DestroyZDO(zdo2);
			}
		}
		this.m_zdosRecv += num;
	}

	// Token: 0x06000D55 RID: 3413 RVA: 0x00067C20 File Offset: 0x00065E20
	public void FindSectorObjects(Vector2i sector, int area, int distantArea, List<ZDO> sectorObjects, List<ZDO> distantSectorObjects = null)
	{
		this.FindObjects(sector, sectorObjects);
		for (int i = 1; i <= area; i++)
		{
			for (int j = sector.x - i; j <= sector.x + i; j++)
			{
				this.FindObjects(new Vector2i(j, sector.y - i), sectorObjects);
				this.FindObjects(new Vector2i(j, sector.y + i), sectorObjects);
			}
			for (int k = sector.y - i + 1; k <= sector.y + i - 1; k++)
			{
				this.FindObjects(new Vector2i(sector.x - i, k), sectorObjects);
				this.FindObjects(new Vector2i(sector.x + i, k), sectorObjects);
			}
		}
		List<ZDO> objects = distantSectorObjects ?? sectorObjects;
		for (int l = area + 1; l <= area + distantArea; l++)
		{
			for (int m = sector.x - l; m <= sector.x + l; m++)
			{
				this.FindDistantObjects(new Vector2i(m, sector.y - l), objects);
				this.FindDistantObjects(new Vector2i(m, sector.y + l), objects);
			}
			for (int n = sector.y - l + 1; n <= sector.y + l - 1; n++)
			{
				this.FindDistantObjects(new Vector2i(sector.x - l, n), objects);
				this.FindDistantObjects(new Vector2i(sector.x + l, n), objects);
			}
		}
	}

	// Token: 0x06000D56 RID: 3414 RVA: 0x00067D9C File Offset: 0x00065F9C
	private void CreateSyncList(ZDOMan.ZDOPeer peer, List<ZDO> toSync)
	{
		if (ZNet.instance.IsServer())
		{
			Vector3 refPos = peer.m_peer.GetRefPos();
			Vector2i zone = ZoneSystem.GetZone(refPos);
			this.m_tempSectorObjects.Clear();
			this.m_tempToSyncDistant.Clear();
			this.FindSectorObjects(zone, ZoneSystem.instance.m_activeArea, ZoneSystem.instance.m_activeDistantArea, this.m_tempSectorObjects, this.m_tempToSyncDistant);
			foreach (ZDO zdo in this.m_tempSectorObjects)
			{
				if (peer.ShouldSend(zdo))
				{
					toSync.Add(zdo);
				}
			}
			this.ServerSortSendZDOS(toSync, refPos, peer);
			if (toSync.Count < 10)
			{
				foreach (ZDO zdo2 in this.m_tempToSyncDistant)
				{
					if (peer.ShouldSend(zdo2))
					{
						toSync.Add(zdo2);
					}
				}
			}
			this.AddForceSendZdos(peer, toSync);
			return;
		}
		this.m_tempRemoveList.Clear();
		foreach (ZDOID zdoid in this.m_clientChangeQueue)
		{
			ZDO zdo3 = this.GetZDO(zdoid);
			if (zdo3 != null && peer.ShouldSend(zdo3))
			{
				toSync.Add(zdo3);
			}
			else
			{
				this.m_tempRemoveList.Add(zdoid);
			}
		}
		foreach (ZDOID item in this.m_tempRemoveList)
		{
			this.m_clientChangeQueue.Remove(item);
		}
		this.ClientSortSendZDOS(toSync, peer);
		this.AddForceSendZdos(peer, toSync);
	}

	// Token: 0x06000D57 RID: 3415 RVA: 0x00067F94 File Offset: 0x00066194
	private void AddForceSendZdos(ZDOMan.ZDOPeer peer, List<ZDO> syncList)
	{
		if (peer.m_forceSend.Count <= 0)
		{
			return;
		}
		this.m_tempRemoveList.Clear();
		foreach (ZDOID zdoid in peer.m_forceSend)
		{
			ZDO zdo = this.GetZDO(zdoid);
			if (zdo != null && peer.ShouldSend(zdo))
			{
				syncList.Insert(0, zdo);
			}
			else
			{
				this.m_tempRemoveList.Add(zdoid);
			}
		}
		foreach (ZDOID item in this.m_tempRemoveList)
		{
			peer.m_forceSend.Remove(item);
		}
	}

	// Token: 0x06000D58 RID: 3416 RVA: 0x00068070 File Offset: 0x00066270
	private static int ServerSendCompare(ZDO x, ZDO y)
	{
		bool flag = x.Type == ZDO.ObjectType.Prioritized && x.HasOwner() && x.GetOwner() != ZDOMan.s_compareReceiver;
		bool flag2 = y.Type == ZDO.ObjectType.Prioritized && y.HasOwner() && y.GetOwner() != ZDOMan.s_compareReceiver;
		if (flag && flag2)
		{
			return Utils.CompareFloats(x.m_tempSortValue, y.m_tempSortValue);
		}
		if (flag != flag2)
		{
			if (!flag)
			{
				return 1;
			}
			return -1;
		}
		else
		{
			if (x.Type == y.Type)
			{
				return Utils.CompareFloats(x.m_tempSortValue, y.m_tempSortValue);
			}
			return ((int)y.Type).CompareTo((int)x.Type);
		}
	}

	// Token: 0x06000D59 RID: 3417 RVA: 0x00068120 File Offset: 0x00066320
	private void ServerSortSendZDOS(List<ZDO> objects, Vector3 refPos, ZDOMan.ZDOPeer peer)
	{
		float time = Time.time;
		foreach (ZDO zdo in objects)
		{
			Vector3 position = zdo.GetPosition();
			zdo.m_tempSortValue = Vector3.Distance(position, refPos);
			float num = 100f;
			ZDOMan.ZDOPeer.PeerZDOInfo peerZDOInfo;
			if (peer.m_zdos.TryGetValue(zdo.m_uid, out peerZDOInfo))
			{
				num = Mathf.Clamp(time - peerZDOInfo.m_syncTime, 0f, 100f);
			}
			zdo.m_tempSortValue -= num * 1.5f;
		}
		ZDOMan.s_compareReceiver = peer.m_peer.m_uid;
		objects.Sort(new Comparison<ZDO>(ZDOMan.ServerSendCompare));
	}

	// Token: 0x06000D5A RID: 3418 RVA: 0x000681F0 File Offset: 0x000663F0
	private static int ClientSendCompare(ZDO x, ZDO y)
	{
		if (x.Type == y.Type)
		{
			return Utils.CompareFloats(x.m_tempSortValue, y.m_tempSortValue);
		}
		if (x.Type == ZDO.ObjectType.Prioritized)
		{
			return -1;
		}
		if (y.Type == ZDO.ObjectType.Prioritized)
		{
			return 1;
		}
		return Utils.CompareFloats(x.m_tempSortValue, y.m_tempSortValue);
	}

	// Token: 0x06000D5B RID: 3419 RVA: 0x00068244 File Offset: 0x00066444
	private void ClientSortSendZDOS(List<ZDO> objects, ZDOMan.ZDOPeer peer)
	{
		float time = Time.time;
		foreach (ZDO zdo in objects)
		{
			zdo.m_tempSortValue = 0f;
			float num = 100f;
			ZDOMan.ZDOPeer.PeerZDOInfo peerZDOInfo;
			if (peer.m_zdos.TryGetValue(zdo.m_uid, out peerZDOInfo))
			{
				num = Mathf.Clamp(time - peerZDOInfo.m_syncTime, 0f, 100f);
			}
			zdo.m_tempSortValue -= num * 1.5f;
		}
		objects.Sort(new Comparison<ZDO>(ZDOMan.ClientSendCompare));
	}

	// Token: 0x06000D5C RID: 3420 RVA: 0x000682F8 File Offset: 0x000664F8
	public static long GetSessionID()
	{
		return ZDOMan.s_instance.m_sessionID;
	}

	// Token: 0x06000D5D RID: 3421 RVA: 0x00068304 File Offset: 0x00066504
	private int SectorToIndex(Vector2i s)
	{
		int num = s.x + this.m_halfWidth;
		int num2 = s.y + this.m_halfWidth;
		if (num < 0 || num2 < 0 || num >= this.m_width || num2 >= this.m_width)
		{
			return -1;
		}
		return num2 * this.m_width + num;
	}

	// Token: 0x06000D5E RID: 3422 RVA: 0x00068354 File Offset: 0x00066554
	private void FindObjects(Vector2i sector, List<ZDO> objects)
	{
		int num = this.SectorToIndex(sector);
		List<ZDO> collection;
		if (num >= 0)
		{
			if (this.m_objectsBySector[num] != null)
			{
				objects.AddRange(this.m_objectsBySector[num]);
				return;
			}
		}
		else if (this.m_objectsByOutsideSector.TryGetValue(sector, out collection))
		{
			objects.AddRange(collection);
		}
	}

	// Token: 0x06000D5F RID: 3423 RVA: 0x000683A0 File Offset: 0x000665A0
	private void FindDistantObjects(Vector2i sector, List<ZDO> objects)
	{
		int num = this.SectorToIndex(sector);
		if (num >= 0)
		{
			List<ZDO> list = this.m_objectsBySector[num];
			if (list == null)
			{
				return;
			}
			using (List<ZDO>.Enumerator enumerator = list.GetEnumerator())
			{
				while (enumerator.MoveNext())
				{
					ZDO zdo = enumerator.Current;
					if (zdo.Distant)
					{
						objects.Add(zdo);
					}
				}
				return;
			}
		}
		List<ZDO> list2;
		if (!this.m_objectsByOutsideSector.TryGetValue(sector, out list2))
		{
			return;
		}
		foreach (ZDO zdo2 in list2)
		{
			if (zdo2.Distant)
			{
				objects.Add(zdo2);
			}
		}
	}

	// Token: 0x06000D60 RID: 3424 RVA: 0x0006846C File Offset: 0x0006666C
	private void RemoveOrphanNonPersistentZDOS()
	{
		foreach (KeyValuePair<ZDOID, ZDO> keyValuePair in this.m_objectsByID)
		{
			ZDO value = keyValuePair.Value;
			if (!value.Persistent && (!value.HasOwner() || !this.IsPeerConnected(value.GetOwner())))
			{
				string str = "Destroying abandoned non persistent zdo ";
				ZDOID uid = value.m_uid;
				ZLog.Log(str + uid.ToString() + " owner " + value.GetOwner().ToString());
				value.SetOwner(this.m_sessionID);
				this.DestroyZDO(value);
			}
		}
	}

	// Token: 0x06000D61 RID: 3425 RVA: 0x0006852C File Offset: 0x0006672C
	private bool IsPeerConnected(long uid)
	{
		if (this.m_sessionID == uid)
		{
			return true;
		}
		using (List<ZDOMan.ZDOPeer>.Enumerator enumerator = this.m_peers.GetEnumerator())
		{
			while (enumerator.MoveNext())
			{
				if (enumerator.Current.m_peer.m_uid == uid)
				{
					return true;
				}
			}
		}
		return false;
	}

	// Token: 0x06000D62 RID: 3426 RVA: 0x00068598 File Offset: 0x00066798
	private static bool InvalidZDO(ZDO zdo)
	{
		return !zdo.IsValid();
	}

	// Token: 0x06000D63 RID: 3427 RVA: 0x000685A4 File Offset: 0x000667A4
	public bool GetAllZDOsWithPrefabIterative(string prefab, List<ZDO> zdos, ref int index)
	{
		int stableHashCode = prefab.GetStableHashCode();
		if (index >= this.m_objectsBySector.Length)
		{
			foreach (List<ZDO> list in this.m_objectsByOutsideSector.Values)
			{
				foreach (ZDO zdo in list)
				{
					if (zdo.GetPrefab() == stableHashCode)
					{
						zdos.Add(zdo);
					}
				}
			}
			zdos.RemoveAll(new Predicate<ZDO>(ZDOMan.InvalidZDO));
			return true;
		}
		int num = 0;
		while (index < this.m_objectsBySector.Length)
		{
			List<ZDO> list2 = this.m_objectsBySector[index];
			if (list2 != null)
			{
				foreach (ZDO zdo2 in list2)
				{
					if (zdo2.GetPrefab() == stableHashCode)
					{
						zdos.Add(zdo2);
					}
				}
				num++;
				if (num > 400)
				{
					break;
				}
			}
			index++;
		}
		return false;
	}

	// Token: 0x06000D64 RID: 3428 RVA: 0x000686E4 File Offset: 0x000668E4
	private List<ZDO> GetSaveClone()
	{
		List<ZDO> list = new List<ZDO>();
		for (int i = 0; i < this.m_objectsBySector.Length; i++)
		{
			if (this.m_objectsBySector[i] != null)
			{
				foreach (ZDO zdo in this.m_objectsBySector[i])
				{
					if (zdo.Persistent)
					{
						list.Add(zdo.Clone());
					}
				}
			}
		}
		foreach (List<ZDO> list2 in this.m_objectsByOutsideSector.Values)
		{
			foreach (ZDO zdo2 in list2)
			{
				if (zdo2.Persistent)
				{
					list.Add(zdo2.Clone());
				}
			}
		}
		return list;
	}

	// Token: 0x06000D65 RID: 3429 RVA: 0x000687F8 File Offset: 0x000669F8
	public List<ZDO> GetPortals()
	{
		return this.m_portalObjects;
	}

	// Token: 0x06000D66 RID: 3430 RVA: 0x00068800 File Offset: 0x00066A00
	public int NrOfObjects()
	{
		return this.m_objectsByID.Count;
	}

	// Token: 0x06000D67 RID: 3431 RVA: 0x0006880D File Offset: 0x00066A0D
	public int GetSentZDOs()
	{
		return this.m_zdosSentLastSec;
	}

	// Token: 0x06000D68 RID: 3432 RVA: 0x00068815 File Offset: 0x00066A15
	public int GetRecvZDOs()
	{
		return this.m_zdosRecvLastSec;
	}

	// Token: 0x06000D69 RID: 3433 RVA: 0x0006881D File Offset: 0x00066A1D
	public int GetClientChangeQueue()
	{
		return this.m_clientChangeQueue.Count;
	}

	// Token: 0x06000D6A RID: 3434 RVA: 0x0006882A File Offset: 0x00066A2A
	public void GetAverageStats(out float sentZdos, out float recvZdos)
	{
		sentZdos = (float)this.m_zdosSentLastSec / 20f;
		recvZdos = (float)this.m_zdosRecvLastSec / 20f;
	}

	// Token: 0x06000D6B RID: 3435 RVA: 0x0006884A File Offset: 0x00066A4A
	public void RequestZDO(ZDOID id)
	{
		ZRoutedRpc.instance.InvokeRoutedRPC("RequestZDO", new object[]
		{
			id
		});
	}

	// Token: 0x06000D6C RID: 3436 RVA: 0x0006886A File Offset: 0x00066A6A
	private void RPC_RequestZDO(long sender, ZDOID id)
	{
		ZDOMan.ZDOPeer peer = this.GetPeer(sender);
		if (peer == null)
		{
			return;
		}
		peer.ForceSendZDO(id);
	}

	// Token: 0x06000D6D RID: 3437 RVA: 0x00068880 File Offset: 0x00066A80
	private ZDOMan.ZDOPeer GetPeer(long uid)
	{
		foreach (ZDOMan.ZDOPeer zdopeer in this.m_peers)
		{
			if (zdopeer.m_peer.m_uid == uid)
			{
				return zdopeer;
			}
		}
		return null;
	}

	// Token: 0x06000D6E RID: 3438 RVA: 0x000688E4 File Offset: 0x00066AE4
	public void ForceSendZDO(ZDOID id)
	{
		foreach (ZDOMan.ZDOPeer zdopeer in this.m_peers)
		{
			zdopeer.ForceSendZDO(id);
		}
	}

	// Token: 0x06000D6F RID: 3439 RVA: 0x00068938 File Offset: 0x00066B38
	public void ForceSendZDO(long peerID, ZDOID id)
	{
		if (ZNet.instance.IsServer())
		{
			ZDOMan.ZDOPeer peer = this.GetPeer(peerID);
			if (peer != null)
			{
				peer.ForceSendZDO(id);
				return;
			}
		}
		else
		{
			foreach (ZDOMan.ZDOPeer zdopeer in this.m_peers)
			{
				zdopeer.ForceSendZDO(id);
			}
		}
	}

	// Token: 0x06000D70 RID: 3440 RVA: 0x000689A8 File Offset: 0x00066BA8
	public void ClientChanged(ZDOID id)
	{
		this.m_clientChangeQueue.Add(id);
	}

	// Token: 0x06000D71 RID: 3441 RVA: 0x000689B7 File Offset: 0x00066BB7
	private void AddPortal(ZDO zdo)
	{
		if (!this.m_portalObjects.Contains(zdo))
		{
			this.m_portalObjects.Add(zdo);
		}
	}

	// Token: 0x06000D72 RID: 3442 RVA: 0x000689D4 File Offset: 0x00066BD4
	private void ConvertOwnerships(List<ZDO> zdos)
	{
		foreach (ZDO zdo in zdos)
		{
			ZDOID zdoid = zdo.GetZDOID(ZDOVars.s_zdoidUser);
			if (zdoid != ZDOID.None)
			{
				zdo.SetOwnerInternal(ZDOMan.GetSessionID());
				zdo.Set(ZDOVars.s_user, zdoid.UserID);
			}
			ZDOID zdoid2 = zdo.GetZDOID(ZDOVars.s_zdoidRodOwner);
			if (zdoid2 != ZDOID.None)
			{
				zdo.SetOwnerInternal(ZDOMan.GetSessionID());
				zdo.Set(ZDOVars.s_rodOwner, zdoid2.UserID);
			}
		}
	}

	// Token: 0x06000D73 RID: 3443 RVA: 0x00068A88 File Offset: 0x00066C88
	private void ConvertCreationTime(List<ZDO> zdos)
	{
		if (!ZDOExtraData.HasTimeCreated())
		{
			return;
		}
		List<int> list = new List<int>
		{
			"cultivate".GetStableHashCode(),
			"raise".GetStableHashCode(),
			"path".GetStableHashCode(),
			"paved_road".GetStableHashCode(),
			"HeathRockPillar".GetStableHashCode(),
			"HeathRockPillar_frac".GetStableHashCode(),
			"ship_construction".GetStableHashCode(),
			"replant".GetStableHashCode(),
			"digg".GetStableHashCode(),
			"mud_road".GetStableHashCode(),
			"LevelTerrain".GetStableHashCode(),
			"digg_v2".GetStableHashCode()
		};
		int num = 0;
		foreach (ZDO zdo in zdos)
		{
			if (list.Contains(zdo.GetPrefab()))
			{
				num++;
				long timeCreated = ZDOExtraData.GetTimeCreated(zdo.m_uid);
				zdo.SetOwner(ZDOMan.GetSessionID());
				zdo.Set(ZDOVars.s_terrainModifierTimeCreated, timeCreated);
			}
		}
		if (num > 0)
		{
			UnityEngine.Debug.Log("Converted " + num.ToString() + " Creation Times.");
		}
	}

	// Token: 0x06000D74 RID: 3444 RVA: 0x00068BF8 File Offset: 0x00066DF8
	private void ConvertPortals()
	{
		UnityEngine.Debug.Log("ConvertPortals => Make sure all " + this.m_portalObjects.Count.ToString() + " portals are in a good state.");
		int num = 0;
		foreach (ZDO zdo in this.m_portalObjects)
		{
			string @string = zdo.GetString(ZDOVars.s_tag, "");
			ZDOID zdoid = zdo.GetZDOID(ZDOVars.s_toRemoveTarget);
			zdo.RemoveZDOID(ZDOVars.s_toRemoveTarget);
			if (!(zdoid == ZDOID.None))
			{
				ZDO zdo2 = this.GetZDO(zdoid);
				if (zdo2 != null)
				{
					ZDOID zdoid2 = zdo2.GetZDOID(ZDOVars.s_toRemoveTarget);
					string string2 = zdo2.GetString(ZDOVars.s_tag, "");
					zdo2.RemoveZDOID(ZDOVars.s_toRemoveTarget);
					if (@string == string2 && zdoid == zdo2.m_uid && zdoid2 == zdo.m_uid)
					{
						zdo.SetOwner(ZDOMan.GetSessionID());
						zdo2.SetOwner(ZDOMan.GetSessionID());
						num++;
						zdo.SetConnection(ZDOExtraData.ConnectionType.Portal, zdo2.m_uid);
						zdo2.SetConnection(ZDOExtraData.ConnectionType.Portal, zdo.m_uid);
						ZDOMan.instance.ForceSendZDO(zdo.m_uid);
						ZDOMan.instance.ForceSendZDO(zdo2.m_uid);
					}
				}
			}
		}
		if (num > 0)
		{
			UnityEngine.Debug.Log("ConvertPortals => fixed " + num.ToString() + " portals.");
		}
	}

	// Token: 0x06000D75 RID: 3445 RVA: 0x00068D9C File Offset: 0x00066F9C
	private void ConnectPortals()
	{
		List<ZDOID> allConnectionZDOIDs = ZDOExtraData.GetAllConnectionZDOIDs(ZDOExtraData.ConnectionType.Portal);
		List<ZDOID> allConnectionZDOIDs2 = ZDOExtraData.GetAllConnectionZDOIDs(ZDOExtraData.ConnectionType.Portal | ZDOExtraData.ConnectionType.Target);
		int num = 0;
		foreach (ZDOID zdoid in allConnectionZDOIDs)
		{
			ZDO zdo = this.GetZDO(zdoid);
			if (zdo != null)
			{
				ZDOConnectionHashData connectionHashData = zdo.GetConnectionHashData(ZDOExtraData.ConnectionType.Portal);
				if (connectionHashData != null)
				{
					foreach (ZDOID zdoid2 in allConnectionZDOIDs2)
					{
						if (!(zdoid2 == zdoid) && ZDOExtraData.GetConnectionType(zdoid2) == ZDOExtraData.ConnectionType.None)
						{
							ZDO zdo2 = this.GetZDO(zdoid2);
							if (zdo2 != null)
							{
								ZDOConnectionHashData connectionHashData2 = ZDOExtraData.GetConnectionHashData(zdoid2, ZDOExtraData.ConnectionType.Portal | ZDOExtraData.ConnectionType.Target);
								if (connectionHashData2 != null && connectionHashData.m_hash == connectionHashData2.m_hash)
								{
									num++;
									zdo.SetOwner(ZDOMan.GetSessionID());
									zdo2.SetOwner(ZDOMan.GetSessionID());
									zdo.SetConnection(ZDOExtraData.ConnectionType.Portal, zdoid2);
									zdo2.SetConnection(ZDOExtraData.ConnectionType.Portal, zdoid);
									break;
								}
							}
						}
					}
				}
			}
		}
		if (num > 0)
		{
			UnityEngine.Debug.Log("ConnectPortals => Connected " + num.ToString() + " portals.");
		}
	}

	// Token: 0x06000D76 RID: 3446 RVA: 0x00068EE4 File Offset: 0x000670E4
	private void ConvertSpawners()
	{
		List<ZDOID> allZDOIDsWithHash = ZDOExtraData.GetAllZDOIDsWithHash(ZDOExtraData.Type.Long, "spawn_id_u".GetStableHashCode());
		if (allZDOIDsWithHash.Count > 0)
		{
			UnityEngine.Debug.Log("ConvertSpawners => Will try and convert " + allZDOIDsWithHash.Count.ToString() + " spawners.");
		}
		int num = 0;
		int num2 = 0;
		foreach (ZDO zdo in from id in allZDOIDsWithHash
		select this.GetZDO(id))
		{
			zdo.SetOwner(ZDOMan.GetSessionID());
			ZDOID zdoid = zdo.GetZDOID(ZDOVars.s_toRemoveSpawnID);
			zdo.RemoveZDOID(ZDOVars.s_toRemoveSpawnID);
			ZDO zdo2 = this.GetZDO(zdoid);
			if (zdo2 != null)
			{
				num++;
				zdo.SetConnection(ZDOExtraData.ConnectionType.Spawned, zdo2.m_uid);
			}
			else
			{
				num2++;
				zdo.SetConnection(ZDOExtraData.ConnectionType.Spawned, ZDOID.None);
			}
		}
		if (num > 0 || num2 > 0)
		{
			UnityEngine.Debug.Log(string.Concat(new string[]
			{
				"ConvertSpawners => Converted ",
				num.ToString(),
				" spawners, and ",
				num2.ToString(),
				" 'done' spawners."
			}));
		}
	}

	// Token: 0x06000D77 RID: 3447 RVA: 0x0006901C File Offset: 0x0006721C
	private void ConnectSpawners()
	{
		List<ZDOID> allConnectionZDOIDs = ZDOExtraData.GetAllConnectionZDOIDs(ZDOExtraData.ConnectionType.Spawned);
		List<ZDOID> allConnectionZDOIDs2 = ZDOExtraData.GetAllConnectionZDOIDs(ZDOExtraData.ConnectionType.Portal | ZDOExtraData.ConnectionType.SyncTransform | ZDOExtraData.ConnectionType.Target);
		int num = 0;
		int num2 = 0;
		foreach (ZDOID zdoid in allConnectionZDOIDs)
		{
			ZDO zdo = this.GetZDO(zdoid);
			if (zdo != null)
			{
				zdo.SetOwner(ZDOMan.GetSessionID());
				bool flag = false;
				ZDOConnectionHashData connectionHashData = zdo.GetConnectionHashData(ZDOExtraData.ConnectionType.Spawned);
				if (connectionHashData != null)
				{
					foreach (ZDOID zdoid2 in allConnectionZDOIDs2)
					{
						if (!(zdoid2 == zdoid))
						{
							ZDOConnectionHashData connectionHashData2 = ZDOExtraData.GetConnectionHashData(zdoid2, ZDOExtraData.ConnectionType.Portal | ZDOExtraData.ConnectionType.SyncTransform | ZDOExtraData.ConnectionType.Target);
							if (connectionHashData2 != null && connectionHashData.m_hash == connectionHashData2.m_hash)
							{
								flag = true;
								num++;
								zdo.SetConnection(ZDOExtraData.ConnectionType.Spawned, zdoid2);
								break;
							}
						}
					}
				}
				if (!flag)
				{
					num2++;
					zdo.SetConnection(ZDOExtraData.ConnectionType.Spawned, ZDOID.None);
				}
			}
		}
		if (num > 0 || num2 > 0)
		{
			UnityEngine.Debug.Log(string.Concat(new string[]
			{
				"ConnectSpawners => Connected ",
				num.ToString(),
				" spawners and ",
				num2.ToString(),
				" 'done' spawners."
			}));
		}
	}

	// Token: 0x06000D78 RID: 3448 RVA: 0x00069178 File Offset: 0x00067378
	private void ConvertSyncTransforms()
	{
		List<ZDOID> allZDOIDsWithHash = ZDOExtraData.GetAllZDOIDsWithHash(ZDOExtraData.Type.Long, "parentID_u".GetStableHashCode());
		if (allZDOIDsWithHash.Count > 0)
		{
			UnityEngine.Debug.Log("ConvertSyncTransforms => Will try and convert " + allZDOIDsWithHash.Count.ToString() + " SyncTransforms.");
		}
		int num = 0;
		foreach (ZDO zdo in allZDOIDsWithHash.Select(new Func<ZDOID, ZDO>(this.GetZDO)))
		{
			zdo.SetOwner(ZDOMan.GetSessionID());
			ZDOID zdoid = zdo.GetZDOID(ZDOVars.s_toRemoveParentID);
			zdo.RemoveZDOID(ZDOVars.s_toRemoveParentID);
			ZDO zdo2 = this.GetZDO(zdoid);
			if (zdo2 != null)
			{
				num++;
				zdo.SetConnection(ZDOExtraData.ConnectionType.SyncTransform, zdo2.m_uid);
			}
		}
		if (num > 0)
		{
			UnityEngine.Debug.Log("ConvertSyncTransforms => Converted " + num.ToString() + " SyncTransforms.");
		}
	}

	// Token: 0x06000D79 RID: 3449 RVA: 0x00069270 File Offset: 0x00067470
	private void ConvertSeed()
	{
		IEnumerable<ZDOID> allZDOIDsWithHash = ZDOExtraData.GetAllZDOIDsWithHash(ZDOExtraData.Type.Int, ZDOVars.s_leftItem);
		int num = 0;
		foreach (ZDO zdo in allZDOIDsWithHash.Select(new Func<ZDOID, ZDO>(this.GetZDO)))
		{
			num++;
			int hashCode = zdo.m_uid.GetHashCode();
			zdo.Set(ZDOVars.s_seed, hashCode, true);
		}
		if (num > 0)
		{
			UnityEngine.Debug.Log("ConvertSeed => Converted " + num.ToString() + " ZDOs.");
		}
	}

	// Token: 0x06000D7A RID: 3450 RVA: 0x00069310 File Offset: 0x00067510
	private void ConvertDungeonRooms(List<ZDO> zdos)
	{
		MemoryStream memoryStream = new MemoryStream();
		BinaryWriter binaryWriter = new BinaryWriter(memoryStream);
		foreach (ZDO zdo in zdos)
		{
			int @int = zdo.GetInt(ZDOVars.s_rooms, 0);
			zdo.RemoveInt(ZDOVars.s_rooms);
			if (@int != 0)
			{
				memoryStream.SetLength(0L);
				binaryWriter.Write(@int);
				for (int i = 0; i < @int; i++)
				{
					string text = "room" + i.ToString();
					int int2 = zdo.GetInt(text, 0);
					Vector3 vec = zdo.GetVec3(text + "_pos", Vector3.zero);
					Quaternion quaternion = zdo.GetQuaternion(text + "_rot", Quaternion.identity);
					zdo.RemoveInt(text);
					zdo.RemoveVec3(text + "_pos");
					zdo.RemoveQuaternion(text + "_rot");
					zdo.RemoveInt(text + "_seed");
					binaryWriter.Write(int2);
					binaryWriter.Write(vec);
					binaryWriter.Write(quaternion);
				}
				zdo.Set(ZDOVars.s_roomData, memoryStream.ToArray());
				ZLog.Log(string.Format("Cleaned up old dungeon data format for {0} rooms.", @int));
			}
		}
	}

	// Token: 0x06000D7B RID: 3451 RVA: 0x00069490 File Offset: 0x00067690
	private void ConnectSyncTransforms()
	{
		List<ZDOID> allConnectionZDOIDs = ZDOExtraData.GetAllConnectionZDOIDs(ZDOExtraData.ConnectionType.SyncTransform);
		List<ZDOID> allConnectionZDOIDs2 = ZDOExtraData.GetAllConnectionZDOIDs(ZDOExtraData.ConnectionType.SyncTransform | ZDOExtraData.ConnectionType.Target);
		int num = 0;
		foreach (ZDOID zid in allConnectionZDOIDs)
		{
			ZDOConnectionHashData connectionHashData = ZDOExtraData.GetConnectionHashData(zid, ZDOExtraData.ConnectionType.SyncTransform);
			if (connectionHashData != null)
			{
				foreach (ZDOID zdoid in allConnectionZDOIDs2)
				{
					ZDOConnectionHashData connectionHashData2 = ZDOExtraData.GetConnectionHashData(zdoid, ZDOExtraData.ConnectionType.SyncTransform | ZDOExtraData.ConnectionType.Target);
					if (connectionHashData2 != null && connectionHashData.m_hash == connectionHashData2.m_hash)
					{
						num++;
						ZDOExtraData.SetConnection(zid, ZDOExtraData.ConnectionType.SyncTransform, zdoid);
						break;
					}
				}
			}
		}
		if (num > 0)
		{
			UnityEngine.Debug.Log("ConnectSyncTransforms => Connected " + num.ToString() + " SyncTransforms.");
		}
	}

	// Token: 0x04000D4F RID: 3407
	public Action<ZDO> m_onZDODestroyed;

	// Token: 0x04000D50 RID: 3408
	private readonly long m_sessionID = Utils.GenerateUID();

	// Token: 0x04000D51 RID: 3409
	private uint m_nextUid = 1U;

	// Token: 0x04000D52 RID: 3410
	private readonly List<ZDO> m_portalObjects = new List<ZDO>();

	// Token: 0x04000D53 RID: 3411
	private readonly Dictionary<Vector2i, List<ZDO>> m_objectsByOutsideSector = new Dictionary<Vector2i, List<ZDO>>();

	// Token: 0x04000D54 RID: 3412
	private readonly List<ZDOMan.ZDOPeer> m_peers = new List<ZDOMan.ZDOPeer>();

	// Token: 0x04000D55 RID: 3413
	private readonly Dictionary<ZDOID, long> m_deadZDOs = new Dictionary<ZDOID, long>();

	// Token: 0x04000D56 RID: 3414
	private readonly List<ZDOID> m_destroySendList = new List<ZDOID>();

	// Token: 0x04000D57 RID: 3415
	private readonly HashSet<ZDOID> m_clientChangeQueue = new HashSet<ZDOID>();

	// Token: 0x04000D58 RID: 3416
	private readonly Dictionary<ZDOID, ZDO> m_objectsByID = new Dictionary<ZDOID, ZDO>();

	// Token: 0x04000D59 RID: 3417
	private List<ZDO>[] m_objectsBySector;

	// Token: 0x04000D5A RID: 3418
	private readonly int m_width;

	// Token: 0x04000D5B RID: 3419
	private readonly int m_halfWidth;

	// Token: 0x04000D5C RID: 3420
	private float m_sendTimer;

	// Token: 0x04000D5D RID: 3421
	private const float c_SendFPS = 20f;

	// Token: 0x04000D5E RID: 3422
	private float m_releaseZDOTimer;

	// Token: 0x04000D5F RID: 3423
	private int m_zdosSent;

	// Token: 0x04000D60 RID: 3424
	private int m_zdosRecv;

	// Token: 0x04000D61 RID: 3425
	private int m_zdosSentLastSec;

	// Token: 0x04000D62 RID: 3426
	private int m_zdosRecvLastSec;

	// Token: 0x04000D63 RID: 3427
	private float m_statTimer;

	// Token: 0x04000D64 RID: 3428
	private ZDOMan.SaveData m_saveData;

	// Token: 0x04000D65 RID: 3429
	private int m_nextSendPeer = -1;

	// Token: 0x04000D66 RID: 3430
	private readonly List<ZDO> m_tempToSync = new List<ZDO>();

	// Token: 0x04000D67 RID: 3431
	private readonly List<ZDO> m_tempToSyncDistant = new List<ZDO>();

	// Token: 0x04000D68 RID: 3432
	private readonly List<ZDO> m_tempNearObjects = new List<ZDO>();

	// Token: 0x04000D69 RID: 3433
	private readonly List<ZDOID> m_tempRemoveList = new List<ZDOID>();

	// Token: 0x04000D6A RID: 3434
	private readonly List<ZDO> m_tempSectorObjects = new List<ZDO>();

	// Token: 0x04000D6B RID: 3435
	private readonly List<List<ZDO>> m_tempNearObjectsForRemoval = new List<List<ZDO>>();

	// Token: 0x04000D6C RID: 3436
	private readonly List<bool> m_tempNearObjectsForRemovalAreasActive = new List<bool>();

	// Token: 0x04000D6D RID: 3437
	private static ZDOMan s_instance;

	// Token: 0x04000D6E RID: 3438
	private static long s_compareReceiver = 0L;

	// Token: 0x04000D6F RID: 3439
	private static readonly List<int> s_brokenPrefabsToFilterOut = new List<int>
	{
		1332933305,
		-1334479845
	};

	// Token: 0x020002E0 RID: 736
	private class ZDOPeer
	{
		// Token: 0x0600216C RID: 8556 RVA: 0x000EB4FC File Offset: 0x000E96FC
		public void ZDOSectorInvalidated(ZDO zdo)
		{
			if (zdo.GetOwner() == this.m_peer.m_uid)
			{
				return;
			}
			if (this.m_zdos.ContainsKey(zdo.m_uid) && !ZNetScene.InActiveArea(zdo.GetSector(), this.m_peer.GetRefPos()))
			{
				this.m_invalidSector.Add(zdo.m_uid);
				this.m_zdos.Remove(zdo.m_uid);
			}
		}

		// Token: 0x0600216D RID: 8557 RVA: 0x000EB56C File Offset: 0x000E976C
		public void ForceSendZDO(ZDOID id)
		{
			this.m_forceSend.Add(id);
		}

		// Token: 0x0600216E RID: 8558 RVA: 0x000EB57C File Offset: 0x000E977C
		public bool ShouldSend(ZDO zdo)
		{
			ZDOMan.ZDOPeer.PeerZDOInfo peerZDOInfo;
			return !this.m_zdos.TryGetValue(zdo.m_uid, out peerZDOInfo) || zdo.OwnerRevision > peerZDOInfo.m_ownerRevision || zdo.DataRevision > peerZDOInfo.m_dataRevision;
		}

		// Token: 0x04002339 RID: 9017
		public ZNetPeer m_peer;

		// Token: 0x0400233A RID: 9018
		public readonly Dictionary<ZDOID, ZDOMan.ZDOPeer.PeerZDOInfo> m_zdos = new Dictionary<ZDOID, ZDOMan.ZDOPeer.PeerZDOInfo>();

		// Token: 0x0400233B RID: 9019
		public readonly HashSet<ZDOID> m_forceSend = new HashSet<ZDOID>();

		// Token: 0x0400233C RID: 9020
		public readonly HashSet<ZDOID> m_invalidSector = new HashSet<ZDOID>();

		// Token: 0x0400233D RID: 9021
		public int m_sendIndex;

		// Token: 0x020003E3 RID: 995
		public struct PeerZDOInfo
		{
			// Token: 0x060023F6 RID: 9206 RVA: 0x000F307D File Offset: 0x000F127D
			public PeerZDOInfo(uint dataRevision, ushort ownerRevision, float syncTime)
			{
				this.m_dataRevision = dataRevision;
				this.m_ownerRevision = ownerRevision;
				this.m_syncTime = syncTime;
			}

			// Token: 0x04002822 RID: 10274
			public readonly uint m_dataRevision;

			// Token: 0x04002823 RID: 10275
			public readonly ushort m_ownerRevision;

			// Token: 0x04002824 RID: 10276
			public readonly float m_syncTime;
		}
	}

	// Token: 0x020002E1 RID: 737
	private class SaveData
	{
		// Token: 0x0400233E RID: 9022
		public long m_sessionID;

		// Token: 0x0400233F RID: 9023
		public uint m_nextUid = 1U;

		// Token: 0x04002340 RID: 9024
		public List<ZDO> m_zdos;
	}
}
