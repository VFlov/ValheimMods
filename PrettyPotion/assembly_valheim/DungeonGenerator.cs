using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using SoftReferenceableAssets;
using UnityEngine;

// Token: 0x02000170 RID: 368
public class DungeonGenerator : MonoBehaviour
{
	// Token: 0x06001619 RID: 5657 RVA: 0x000A2E88 File Offset: 0x000A1088
	private void Awake()
	{
		this.m_nview = base.GetComponent<ZNetView>();
		this.Load();
		if (this.m_loadedRooms.Length != 0)
		{
			this.LoadRoomPrefabsAsync();
		}
	}

	// Token: 0x0600161A RID: 5658 RVA: 0x000A2EAB File Offset: 0x000A10AB
	private void OnDestroy()
	{
		this.ReleaseHeldReferences();
	}

	// Token: 0x0600161B RID: 5659 RVA: 0x000A2EB4 File Offset: 0x000A10B4
	private void ReleaseHeldReferences()
	{
		for (int i = 0; i < this.m_heldReferences.Count; i++)
		{
			this.m_heldReferences[i].Release();
		}
		this.m_heldReferences.Clear();
		if (this.m_zdoSetToBeLoadingInZone != null)
		{
			ZoneSystem.instance.UnsetLoadingInZone(this.m_zdoSetToBeLoadingInZone);
			this.m_zdoSetToBeLoadingInZone = null;
		}
	}

	// Token: 0x0600161C RID: 5660 RVA: 0x000A2F12 File Offset: 0x000A1112
	public void Clear()
	{
		while (base.transform.childCount > 0)
		{
			UnityEngine.Object.DestroyImmediate(base.transform.GetChild(0).gameObject);
		}
	}

	// Token: 0x0600161D RID: 5661 RVA: 0x000A2F3C File Offset: 0x000A113C
	public void Generate(ZoneSystem.SpawnMode mode)
	{
		int seed = this.GetSeed();
		this.Generate(seed, mode);
	}

	// Token: 0x0600161E RID: 5662 RVA: 0x000A2F58 File Offset: 0x000A1158
	public int GetSeed()
	{
		if (this.m_hasGeneratedSeed)
		{
			return this.m_generatedSeed;
		}
		if (DungeonGenerator.m_forceSeed != -2147483648)
		{
			this.m_generatedSeed = DungeonGenerator.m_forceSeed;
			DungeonGenerator.m_forceSeed = int.MinValue;
		}
		else
		{
			int seed = WorldGenerator.instance.GetSeed();
			Vector3 position = base.transform.position;
			Vector2i zone = ZoneSystem.GetZone(base.transform.position);
			this.m_generatedSeed = seed + zone.x * 4271 + zone.y * -7187 + (int)position.x * -4271 + (int)position.y * 9187 + (int)position.z * -2134;
		}
		this.m_hasGeneratedSeed = true;
		return this.m_generatedSeed;
	}

	// Token: 0x0600161F RID: 5663 RVA: 0x000A3018 File Offset: 0x000A1218
	public void Generate(int seed, ZoneSystem.SpawnMode mode)
	{
		DateTime now = DateTime.Now;
		this.m_generatedSeed = seed;
		this.Clear();
		this.SetupColliders();
		this.SetupAvailableRooms();
		for (int i = 0; i < DungeonGenerator.m_availableRooms.Count; i++)
		{
			DungeonGenerator.m_availableRooms[i].m_prefab.Load();
		}
		if (ZoneSystem.instance)
		{
			Vector2i zone = ZoneSystem.GetZone(base.transform.position);
			this.m_zoneCenter = ZoneSystem.GetZonePos(zone);
			this.m_zoneCenter.y = base.transform.position.y - this.m_originalPosition.y;
		}
		Bounds bounds = new Bounds(this.m_zoneCenter, this.m_zoneSize);
		ZLog.Log(string.Format("Generating {0}, Seed: {1}, Bounds diff: {2} / {3}", new object[]
		{
			base.name,
			seed,
			bounds.min - base.transform.position,
			bounds.max - base.transform.position
		}));
		ZLog.Log("Available rooms:" + DungeonGenerator.m_availableRooms.Count.ToString());
		ZLog.Log("To place:" + this.m_maxRooms.ToString());
		DungeonGenerator.m_placedRooms.Clear();
		DungeonGenerator.m_openConnections.Clear();
		DungeonGenerator.m_doorConnections.Clear();
		UnityEngine.Random.State state = UnityEngine.Random.state;
		UnityEngine.Random.InitState(seed);
		this.GenerateRooms(mode);
		for (int j = 0; j < DungeonGenerator.m_availableRooms.Count; j++)
		{
			DungeonGenerator.m_availableRooms[j].m_prefab.Release();
		}
		this.Save();
		ZLog.Log("Placed " + DungeonGenerator.m_placedRooms.Count.ToString() + " rooms");
		UnityEngine.Random.state = state;
		SnapToGround.SnappAll();
		if (mode == ZoneSystem.SpawnMode.Ghost)
		{
			foreach (Room room in DungeonGenerator.m_placedRooms)
			{
				UnityEngine.Object.DestroyImmediate(room.gameObject);
			}
		}
		UnityEngine.Object.DestroyImmediate(this.m_colliderA);
		UnityEngine.Object.DestroyImmediate(this.m_colliderB);
		DungeonGenerator.m_placedRooms.Clear();
		DungeonGenerator.m_openConnections.Clear();
		DungeonGenerator.m_doorConnections.Clear();
		DateTime.Now - now;
	}

	// Token: 0x06001620 RID: 5664 RVA: 0x000A329C File Offset: 0x000A149C
	private void LoadRoomPrefabsAsync()
	{
		ZLog.Log("Loading room prefabs asynchronously");
		if (this.m_zdoSetToBeLoadingInZone == null)
		{
			this.m_zdoSetToBeLoadingInZone = this.m_nview.GetZDO();
			ZoneSystem.instance.SetLoadingInZone(this.m_zdoSetToBeLoadingInZone);
		}
		this.m_roomsToLoad = this.m_loadedRooms.Length;
		int num = this.m_loadedRooms.Length;
		for (int i = 0; i < num; i++)
		{
			this.m_heldReferences.Add(this.m_loadedRooms[i].m_roomData.m_prefab);
			this.m_loadedRooms[i].m_roomData.m_prefab.LoadAsync(new LoadedHandler(this.OnRoomLoaded));
		}
	}

	// Token: 0x06001621 RID: 5665 RVA: 0x000A334C File Offset: 0x000A154C
	private void OnRoomLoaded(AssetID assetID, LoadResult result)
	{
		if (result != LoadResult.Succeeded)
		{
			return;
		}
		if (this == null || base.gameObject == null)
		{
			return;
		}
		this.m_roomsToLoad--;
		if (this.m_roomsToLoad > 0)
		{
			return;
		}
		this.Spawn();
		this.ReleaseHeldReferences();
	}

	// Token: 0x06001622 RID: 5666 RVA: 0x000A339C File Offset: 0x000A159C
	private void Spawn()
	{
		ZLog.Log("Spawning dungeon");
		for (int i = 0; i < this.m_loadedRooms.Length; i++)
		{
			this.PlaceRoom(this.m_loadedRooms[i].m_roomData, this.m_loadedRooms[i].m_position, this.m_loadedRooms[i].m_rotation, null, ZoneSystem.SpawnMode.Client);
		}
		SnapToGround.SnappAll();
		this.m_loadedRooms = null;
	}

	// Token: 0x06001623 RID: 5667 RVA: 0x000A3410 File Offset: 0x000A1610
	private void GenerateRooms(ZoneSystem.SpawnMode mode)
	{
		switch (this.m_algorithm)
		{
		case DungeonGenerator.Algorithm.Dungeon:
			this.GenerateDungeon(mode);
			return;
		case DungeonGenerator.Algorithm.CampGrid:
			this.GenerateCampGrid(mode);
			return;
		case DungeonGenerator.Algorithm.CampRadial:
			this.GenerateCampRadial(mode);
			return;
		default:
			return;
		}
	}

	// Token: 0x06001624 RID: 5668 RVA: 0x000A344E File Offset: 0x000A164E
	private void GenerateDungeon(ZoneSystem.SpawnMode mode)
	{
		this.PlaceStartRoom(mode);
		this.PlaceRooms(mode);
		this.PlaceEndCaps(mode);
		this.PlaceDoors(mode);
	}

	// Token: 0x06001625 RID: 5669 RVA: 0x000A346C File Offset: 0x000A166C
	private void GenerateCampGrid(ZoneSystem.SpawnMode mode)
	{
		float num = Mathf.Cos(0.017453292f * this.m_maxTilt);
		Vector3 a = base.transform.position + new Vector3((float)(-(float)this.m_gridSize) * this.m_tileWidth * 0.5f, 0f, (float)(-(float)this.m_gridSize) * this.m_tileWidth * 0.5f);
		for (int i = 0; i < this.m_gridSize; i++)
		{
			for (int j = 0; j < this.m_gridSize; j++)
			{
				if (UnityEngine.Random.value <= this.m_spawnChance)
				{
					Vector3 pos = a + new Vector3((float)j * this.m_tileWidth, 0f, (float)i * this.m_tileWidth);
					DungeonDB.RoomData randomWeightedRoom = this.GetRandomWeightedRoom(false);
					if (randomWeightedRoom != null)
					{
						if (ZoneSystem.instance)
						{
							Vector3 vector;
							Heightmap.Biome biome;
							Heightmap.BiomeArea biomeArea;
							Heightmap heightmap;
							ZoneSystem.instance.GetGroundData(ref pos, out vector, out biome, out biomeArea, out heightmap);
							if (vector.y < num)
							{
								goto IL_FF;
							}
						}
						Quaternion rot = Quaternion.Euler(0f, (float)UnityEngine.Random.Range(0, 16) * 22.5f, 0f);
						this.PlaceRoom(randomWeightedRoom, pos, rot, null, mode);
					}
				}
				IL_FF:;
			}
		}
	}

	// Token: 0x06001626 RID: 5670 RVA: 0x000A3598 File Offset: 0x000A1798
	private void GenerateCampRadial(ZoneSystem.SpawnMode mode)
	{
		float num = UnityEngine.Random.Range(this.m_campRadiusMin, this.m_campRadiusMax);
		float num2 = Mathf.Cos(0.017453292f * this.m_maxTilt);
		int num3 = UnityEngine.Random.Range(this.m_minRooms, this.m_maxRooms);
		int num4 = num3 * 20;
		int num5 = 0;
		for (int i = 0; i < num4; i++)
		{
			Vector3 vector = base.transform.position + Quaternion.Euler(0f, (float)UnityEngine.Random.Range(0, 360), 0f) * Vector3.forward * UnityEngine.Random.Range(0f, num - this.m_perimeterBuffer);
			DungeonDB.RoomData randomWeightedRoom = this.GetRandomWeightedRoom(false);
			if (randomWeightedRoom != null)
			{
				if (ZoneSystem.instance)
				{
					Vector3 vector2;
					Heightmap.Biome biome;
					Heightmap.BiomeArea biomeArea;
					Heightmap heightmap;
					ZoneSystem.instance.GetGroundData(ref vector, out vector2, out biome, out biomeArea, out heightmap);
					if (vector2.y < num2 || vector.y - 30f < this.m_minAltitude)
					{
						goto IL_119;
					}
				}
				Quaternion campRoomRotation = this.GetCampRoomRotation(randomWeightedRoom, vector);
				if (!this.TestCollision(randomWeightedRoom.RoomInPrefab, vector, campRoomRotation))
				{
					this.PlaceRoom(randomWeightedRoom, vector, campRoomRotation, null, mode);
					num5++;
					if (num5 >= num3)
					{
						break;
					}
				}
			}
			IL_119:;
		}
		if (this.m_perimeterSections > 0)
		{
			this.PlaceWall(num, this.m_perimeterSections, mode);
		}
	}

	// Token: 0x06001627 RID: 5671 RVA: 0x000A36E4 File Offset: 0x000A18E4
	private Quaternion GetCampRoomRotation(DungeonDB.RoomData room, Vector3 pos)
	{
		if (room.RoomInPrefab.m_faceCenter)
		{
			Vector3 vector = base.transform.position - pos;
			vector.y = 0f;
			if (vector == Vector3.zero)
			{
				vector = Vector3.forward;
			}
			vector.Normalize();
			float y = Mathf.Round(global::Utils.YawFromDirection(vector) / 22.5f) * 22.5f;
			return Quaternion.Euler(0f, y, 0f);
		}
		return Quaternion.Euler(0f, (float)UnityEngine.Random.Range(0, 16) * 22.5f, 0f);
	}

	// Token: 0x06001628 RID: 5672 RVA: 0x000A3780 File Offset: 0x000A1980
	private void PlaceWall(float radius, int sections, ZoneSystem.SpawnMode mode)
	{
		float num = Mathf.Cos(0.017453292f * this.m_maxTilt);
		int num2 = 0;
		int num3 = sections * 20;
		for (int i = 0; i < num3; i++)
		{
			DungeonDB.RoomData randomWeightedRoom = this.GetRandomWeightedRoom(true);
			if (randomWeightedRoom != null)
			{
				Vector3 vector = base.transform.position + Quaternion.Euler(0f, (float)UnityEngine.Random.Range(0, 360), 0f) * Vector3.forward * radius;
				Quaternion campRoomRotation = this.GetCampRoomRotation(randomWeightedRoom, vector);
				if (ZoneSystem.instance)
				{
					Vector3 vector2;
					Heightmap.Biome biome;
					Heightmap.BiomeArea biomeArea;
					Heightmap heightmap;
					ZoneSystem.instance.GetGroundData(ref vector, out vector2, out biome, out biomeArea, out heightmap);
					if (vector2.y < num || vector.y - 30f < this.m_minAltitude)
					{
						goto IL_E2;
					}
				}
				if (!this.TestCollision(randomWeightedRoom.RoomInPrefab, vector, campRoomRotation))
				{
					this.PlaceRoom(randomWeightedRoom, vector, campRoomRotation, null, mode);
					num2++;
					if (num2 >= sections)
					{
						break;
					}
				}
			}
			IL_E2:;
		}
	}

	// Token: 0x06001629 RID: 5673 RVA: 0x000A387C File Offset: 0x000A1A7C
	private void Save()
	{
		if (this.m_nview == null)
		{
			return;
		}
		ZDO zdo = this.m_nview.GetZDO();
		DungeonGenerator.saveStream.SetLength(0L);
		DungeonGenerator.saveWriter.Write(DungeonGenerator.m_placedRooms.Count);
		for (int i = 0; i < DungeonGenerator.m_placedRooms.Count; i++)
		{
			Room room = DungeonGenerator.m_placedRooms[i];
			DungeonGenerator.saveWriter.Write(room.GetHash());
			DungeonGenerator.saveWriter.Write(room.transform.position);
			DungeonGenerator.saveWriter.Write(room.transform.rotation);
		}
		zdo.Set(ZDOVars.s_roomData, DungeonGenerator.saveStream.ToArray());
		int num;
		if (zdo.GetInt(ZDOVars.s_rooms, out num))
		{
			zdo.RemoveInt(ZDOVars.s_rooms);
			for (int j = 0; j < num; j++)
			{
				string text = "room" + j.ToString();
				zdo.RemoveInt(text);
				zdo.RemoveVec3(text + "_pos");
				zdo.RemoveQuaternion(text + "_rot");
				zdo.RemoveInt(text + "_seed");
			}
			ZLog.Log(string.Format("Cleaned up old dungeon data format for {0} rooms.", num));
		}
	}

	// Token: 0x0600162A RID: 5674 RVA: 0x000A39CC File Offset: 0x000A1BCC
	private void Load()
	{
		if (this.m_nview == null)
		{
			return;
		}
		DateTime now = DateTime.Now;
		ZLog.Log("Loading dungeon");
		ZDO zdo = this.m_nview.GetZDO();
		int num = 0;
		byte[] buffer;
		if (zdo.GetByteArray(ZDOVars.s_roomData, out buffer))
		{
			BinaryReader binaryReader = new BinaryReader(new MemoryStream(buffer));
			int num2 = binaryReader.ReadInt32();
			this.m_loadedRooms = new DungeonGenerator.RoomPlacementData[num2];
			for (int i = 0; i < num2; i++)
			{
				int hash = binaryReader.ReadInt32();
				Vector3 position = binaryReader.ReadVector3();
				Quaternion rotation = binaryReader.ReadQuaternion();
				DungeonDB.RoomData room = DungeonDB.instance.GetRoom(hash);
				if (room == null)
				{
					ZLog.LogWarning("Missing room:" + hash.ToString());
				}
				else
				{
					this.m_loadedRooms[num++] = new DungeonGenerator.RoomPlacementData(room, position, rotation);
				}
			}
			ZLog.Log(string.Format("Dungeon loaded with {0} rooms in {1} ms.", num2, (DateTime.Now - now).TotalMilliseconds));
		}
		else
		{
			int @int = zdo.GetInt("rooms", 0);
			this.m_loadedRooms = new DungeonGenerator.RoomPlacementData[@int];
			for (int j = 0; j < @int; j++)
			{
				string text = "room" + j.ToString();
				int int2 = zdo.GetInt(text, 0);
				Vector3 vec = zdo.GetVec3(text + "_pos", Vector3.zero);
				Quaternion quaternion = zdo.GetQuaternion(text + "_rot", Quaternion.identity);
				DungeonDB.RoomData room2 = DungeonDB.instance.GetRoom(int2);
				if (room2 == null)
				{
					ZLog.LogWarning("Missing room:" + int2.ToString());
				}
				else
				{
					this.m_loadedRooms[num++] = new DungeonGenerator.RoomPlacementData(room2, vec, quaternion);
				}
			}
			ZLog.Log(string.Format("Dungeon loaded with {0} rooms from old format in {1} ms.", @int, (DateTime.Now - now).TotalMilliseconds));
		}
		if (num < this.m_loadedRooms.Length)
		{
			DungeonGenerator.RoomPlacementData[] array = new DungeonGenerator.RoomPlacementData[num];
			Array.Copy(this.m_loadedRooms, array, num);
			this.m_loadedRooms = array;
		}
	}

	// Token: 0x0600162B RID: 5675 RVA: 0x000A3C00 File Offset: 0x000A1E00
	private void SetupAvailableRooms()
	{
		DungeonGenerator.m_availableRooms.Clear();
		foreach (DungeonDB.RoomData roomData in DungeonDB.GetRooms())
		{
			if ((roomData.m_theme & this.m_themes) != Room.Theme.None && roomData.m_enabled)
			{
				DungeonGenerator.m_availableRooms.Add(roomData);
			}
		}
	}

	// Token: 0x0600162C RID: 5676 RVA: 0x000A3C78 File Offset: 0x000A1E78
	public SoftReference<GameObject>[] GetAvailableRoomPrefabs()
	{
		this.SetupAvailableRooms();
		SoftReference<GameObject>[] array = new SoftReference<GameObject>[DungeonGenerator.m_availableRooms.Count];
		for (int i = 0; i < DungeonGenerator.m_availableRooms.Count; i++)
		{
			array[i] = DungeonGenerator.m_availableRooms[i].m_prefab;
		}
		return array;
	}

	// Token: 0x0600162D RID: 5677 RVA: 0x000A3CC8 File Offset: 0x000A1EC8
	private DungeonGenerator.DoorDef FindDoorType(string type)
	{
		List<DungeonGenerator.DoorDef> list = new List<DungeonGenerator.DoorDef>();
		foreach (DungeonGenerator.DoorDef doorDef in this.m_doorTypes)
		{
			if (doorDef.m_connectionType == type)
			{
				list.Add(doorDef);
			}
		}
		if (list.Count == 0)
		{
			return null;
		}
		return list[UnityEngine.Random.Range(0, list.Count)];
	}

	// Token: 0x0600162E RID: 5678 RVA: 0x000A3D4C File Offset: 0x000A1F4C
	private void PlaceDoors(ZoneSystem.SpawnMode mode)
	{
		int num = 0;
		foreach (RoomConnection roomConnection in DungeonGenerator.m_doorConnections)
		{
			DungeonGenerator.DoorDef doorDef = this.FindDoorType(roomConnection.m_type);
			if (doorDef == null)
			{
				ZLog.Log("No door type for connection:" + roomConnection.m_type);
			}
			else if ((doorDef.m_chance <= 0f || UnityEngine.Random.value <= doorDef.m_chance) && (doorDef.m_chance > 0f || UnityEngine.Random.value <= this.m_doorChance))
			{
				GameObject obj = UnityEngine.Object.Instantiate<GameObject>(doorDef.m_prefab, roomConnection.transform.position, roomConnection.transform.rotation);
				if (mode == ZoneSystem.SpawnMode.Ghost)
				{
					UnityEngine.Object.Destroy(obj);
				}
				num++;
			}
		}
		ZLog.Log("placed " + num.ToString() + " doors");
	}

	// Token: 0x0600162F RID: 5679 RVA: 0x000A3E48 File Offset: 0x000A2048
	private void PlaceEndCaps(ZoneSystem.SpawnMode mode)
	{
		for (int i = 0; i < DungeonGenerator.m_openConnections.Count; i++)
		{
			RoomConnection roomConnection = DungeonGenerator.m_openConnections[i];
			RoomConnection roomConnection2 = null;
			for (int j = 0; j < DungeonGenerator.m_openConnections.Count; j++)
			{
				if (j != i && roomConnection.TestContact(DungeonGenerator.m_openConnections[j]))
				{
					roomConnection2 = DungeonGenerator.m_openConnections[j];
					break;
				}
			}
			if (roomConnection2 != null)
			{
				if (roomConnection.m_type != roomConnection2.m_type)
				{
					this.FindDividers(DungeonGenerator.m_tempRooms);
					if (DungeonGenerator.m_tempRooms.Count > 0)
					{
						DungeonDB.RoomData weightedRoom = this.GetWeightedRoom(DungeonGenerator.m_tempRooms);
						RoomConnection[] connections = weightedRoom.RoomInPrefab.GetConnections();
						Vector3 vector;
						Quaternion rot;
						this.CalculateRoomPosRot(connections[0], roomConnection.transform.position, roomConnection.transform.rotation, out vector, out rot);
						bool flag = false;
						foreach (Room room in DungeonGenerator.m_placedRooms)
						{
							if (room.m_divider && Vector3.Distance(room.transform.position, vector) < 0.5f)
							{
								flag = true;
								break;
							}
						}
						if (!flag)
						{
							this.PlaceRoom(weightedRoom, vector, rot, roomConnection, mode);
							ZLog.Log(string.Concat(new string[]
							{
								"Cyclic detected. Door missmatch for cyclic room '",
								roomConnection.m_type,
								"'-'",
								roomConnection2.m_type,
								"', placing divider: ",
								weightedRoom.m_prefab.Name
							}));
						}
					}
					else
					{
						ZLog.LogWarning(string.Concat(new string[]
						{
							"Cyclic detected. Door missmatch for cyclic room '",
							roomConnection.m_type,
							"'-'",
							roomConnection2.m_type,
							"', but no dividers defined!"
						}));
					}
				}
				else
				{
					ZLog.Log("cyclic detected and door types match, cool");
				}
			}
			else
			{
				this.FindEndCaps(roomConnection, DungeonGenerator.m_tempRooms);
				bool flag2 = false;
				if (this.m_alternativeFunctionality)
				{
					for (int k = 0; k < 5; k++)
					{
						DungeonDB.RoomData weightedRoom2 = this.GetWeightedRoom(DungeonGenerator.m_tempRooms);
						if (this.PlaceRoom(roomConnection, weightedRoom2, mode))
						{
							flag2 = true;
							break;
						}
					}
				}
				IOrderedEnumerable<DungeonDB.RoomData> orderedEnumerable = from item in DungeonGenerator.m_tempRooms
				orderby item.RoomInPrefab.m_endCapPrio descending
				select item;
				if (!flag2)
				{
					foreach (DungeonDB.RoomData roomData in orderedEnumerable)
					{
						if (this.PlaceRoom(roomConnection, roomData, mode))
						{
							flag2 = true;
							break;
						}
					}
				}
				if (!flag2)
				{
					ZLog.LogWarning("Failed to place end cap " + roomConnection.name + " " + roomConnection.transform.parent.gameObject.name);
				}
			}
		}
	}

	// Token: 0x06001630 RID: 5680 RVA: 0x000A4144 File Offset: 0x000A2344
	private void FindDividers(List<DungeonDB.RoomData> rooms)
	{
		rooms.Clear();
		foreach (DungeonDB.RoomData roomData in DungeonGenerator.m_availableRooms)
		{
			if (roomData.RoomInPrefab.m_divider)
			{
				rooms.Add(roomData);
			}
		}
		rooms.Shuffle(true);
	}

	// Token: 0x06001631 RID: 5681 RVA: 0x000A41B0 File Offset: 0x000A23B0
	private void FindEndCaps(RoomConnection connection, List<DungeonDB.RoomData> rooms)
	{
		rooms.Clear();
		foreach (DungeonDB.RoomData roomData in DungeonGenerator.m_availableRooms)
		{
			if (roomData.RoomInPrefab.m_endCap && roomData.RoomInPrefab.HaveConnection(connection))
			{
				rooms.Add(roomData);
			}
		}
		rooms.Shuffle(true);
	}

	// Token: 0x06001632 RID: 5682 RVA: 0x000A422C File Offset: 0x000A242C
	private DungeonDB.RoomData FindEndCap(RoomConnection connection)
	{
		DungeonGenerator.m_tempRooms.Clear();
		foreach (DungeonDB.RoomData roomData in DungeonGenerator.m_availableRooms)
		{
			if (roomData.RoomInPrefab.m_endCap && roomData.RoomInPrefab.HaveConnection(connection))
			{
				DungeonGenerator.m_tempRooms.Add(roomData);
			}
		}
		if (DungeonGenerator.m_tempRooms.Count == 0)
		{
			return null;
		}
		return DungeonGenerator.m_tempRooms[UnityEngine.Random.Range(0, DungeonGenerator.m_tempRooms.Count)];
	}

	// Token: 0x06001633 RID: 5683 RVA: 0x000A42D0 File Offset: 0x000A24D0
	private void PlaceRooms(ZoneSystem.SpawnMode mode)
	{
		for (int i = 0; i < this.m_maxRooms; i++)
		{
			this.PlaceOneRoom(mode);
			if (this.CheckRequiredRooms() && DungeonGenerator.m_placedRooms.Count > this.m_minRooms)
			{
				ZLog.Log("All required rooms have been placed, stopping generation");
				return;
			}
		}
	}

	// Token: 0x06001634 RID: 5684 RVA: 0x000A431C File Offset: 0x000A251C
	private void PlaceStartRoom(ZoneSystem.SpawnMode mode)
	{
		DungeonDB.RoomData roomData = this.FindStartRoom();
		RoomConnection entrance = roomData.RoomInPrefab.GetEntrance();
		Quaternion rotation = base.transform.rotation;
		Vector3 pos;
		Quaternion rot;
		this.CalculateRoomPosRot(entrance, base.transform.position, rotation, out pos, out rot);
		this.PlaceRoom(roomData, pos, rot, entrance, mode);
	}

	// Token: 0x06001635 RID: 5685 RVA: 0x000A436C File Offset: 0x000A256C
	private bool PlaceOneRoom(ZoneSystem.SpawnMode mode)
	{
		RoomConnection openConnection = this.GetOpenConnection();
		if (openConnection == null)
		{
			return false;
		}
		for (int i = 0; i < 10; i++)
		{
			DungeonDB.RoomData roomData = this.m_alternativeFunctionality ? this.GetRandomWeightedRoom(openConnection) : this.GetRandomRoom(openConnection);
			if (roomData == null)
			{
				break;
			}
			if (this.PlaceRoom(openConnection, roomData, mode))
			{
				return true;
			}
		}
		return false;
	}

	// Token: 0x06001636 RID: 5686 RVA: 0x000A43C4 File Offset: 0x000A25C4
	private void CalculateRoomPosRot(RoomConnection roomCon, Vector3 exitPos, Quaternion exitRot, out Vector3 pos, out Quaternion rot)
	{
		Quaternion rhs = Quaternion.Inverse(roomCon.transform.localRotation);
		rot = exitRot * rhs;
		Vector3 localPosition = roomCon.transform.localPosition;
		pos = exitPos - rot * localPosition;
	}

	// Token: 0x06001637 RID: 5687 RVA: 0x000A4418 File Offset: 0x000A2618
	private bool PlaceRoom(RoomConnection connection, DungeonDB.RoomData roomData, ZoneSystem.SpawnMode mode)
	{
		SoftReference<GameObject> prefab = roomData.m_prefab;
		prefab.Load();
		Room component = prefab.Asset.GetComponent<Room>();
		Quaternion quaternion = connection.transform.rotation;
		quaternion *= Quaternion.Euler(0f, 180f, 0f);
		RoomConnection connection2 = component.GetConnection(connection);
		if (connection2.transform.parent.gameObject != component.gameObject)
		{
			ZLog.LogWarning(string.Concat(new string[]
			{
				"Connection '",
				component.name,
				"->",
				connection2.name,
				"' is not placed as a direct child of room!"
			}));
		}
		Vector3 pos;
		Quaternion rot;
		this.CalculateRoomPosRot(connection2, connection.transform.position, quaternion, out pos, out rot);
		if (component.m_size.x != 0 && component.m_size.z != 0 && this.TestCollision(component, pos, rot))
		{
			prefab.Release();
			return false;
		}
		this.PlaceRoom(roomData, pos, rot, connection, mode);
		if (!component.m_endCap)
		{
			if (connection.m_allowDoor && (!connection.m_doorOnlyIfOtherAlsoAllowsDoor || connection2.m_allowDoor))
			{
				DungeonGenerator.m_doorConnections.Add(connection);
			}
			DungeonGenerator.m_openConnections.Remove(connection);
		}
		prefab.Release();
		return true;
	}

	// Token: 0x06001638 RID: 5688 RVA: 0x000A455C File Offset: 0x000A275C
	private Room PlaceRoom(DungeonDB.RoomData roomData, Vector3 pos, Quaternion rot, RoomConnection fromConnection, ZoneSystem.SpawnMode mode)
	{
		roomData.m_prefab.Load();
		Room component = roomData.m_prefab.Asset.GetComponent<Room>();
		ZNetView[] enabledComponentsInChildren = global::Utils.GetEnabledComponentsInChildren<ZNetView>(roomData.m_prefab.Asset);
		RandomSpawn[] enabledComponentsInChildren2 = global::Utils.GetEnabledComponentsInChildren<RandomSpawn>(roomData.m_prefab.Asset);
		for (int i = 0; i < enabledComponentsInChildren2.Length; i++)
		{
			enabledComponentsInChildren2[i].Prepare();
		}
		Vector3 vector = pos;
		if (this.m_useCustomInteriorTransform)
		{
			vector = pos - base.transform.position;
		}
		int seed = (int)vector.x * 4271 + (int)vector.y * 9187 + (int)vector.z * 2134;
		UnityEngine.Random.State state = UnityEngine.Random.state;
		UnityEngine.Random.InitState(seed);
		Vector3 position = component.transform.position;
		Quaternion quaternion = Quaternion.Inverse(component.transform.rotation);
		if (mode == ZoneSystem.SpawnMode.Full || mode == ZoneSystem.SpawnMode.Ghost)
		{
			UnityEngine.Random.InitState(seed);
			foreach (RandomSpawn randomSpawn in enabledComponentsInChildren2)
			{
				Vector3 point = quaternion * (randomSpawn.gameObject.transform.position - position);
				Vector3 pos2 = pos + rot * point;
				randomSpawn.Randomize(pos2, null, this);
			}
			foreach (ZNetView znetView in enabledComponentsInChildren)
			{
				if (znetView.gameObject.activeSelf)
				{
					Vector3 point2 = quaternion * (znetView.gameObject.transform.position - position);
					Vector3 position2 = pos + rot * point2;
					Quaternion rhs = quaternion * znetView.gameObject.transform.rotation;
					Quaternion rotation = rot * rhs;
					GameObject gameObject = UnityEngine.Object.Instantiate<GameObject>(znetView.gameObject, position2, rotation);
					gameObject.HoldReferenceTo(roomData.m_prefab);
					if (mode == ZoneSystem.SpawnMode.Ghost)
					{
						UnityEngine.Object.Destroy(gameObject);
					}
				}
			}
		}
		else
		{
			UnityEngine.Random.InitState(seed);
			foreach (RandomSpawn randomSpawn2 in enabledComponentsInChildren2)
			{
				Vector3 point3 = quaternion * (randomSpawn2.gameObject.transform.position - position);
				Vector3 pos3 = pos + rot * point3;
				randomSpawn2.Randomize(pos3, null, this);
			}
		}
		ZNetView[] array2 = enabledComponentsInChildren;
		for (int j = 0; j < array2.Length; j++)
		{
			array2[j].gameObject.SetActive(false);
		}
		Room component2 = SoftReferenceableAssets.Utils.Instantiate(roomData.m_prefab, pos, rot, base.transform).GetComponent<Room>();
		component2.gameObject.name = roomData.m_prefab.Name;
		if (mode != ZoneSystem.SpawnMode.Client)
		{
			component2.m_placeOrder = (fromConnection ? (fromConnection.m_placeOrder + 1) : 0);
			component2.m_seed = seed;
			DungeonGenerator.m_placedRooms.Add(component2);
			this.AddOpenConnections(component2, fromConnection);
		}
		UnityEngine.Random.state = state;
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
		roomData.m_prefab.Release();
		return component2;
	}

	// Token: 0x06001639 RID: 5689 RVA: 0x000A48AC File Offset: 0x000A2AAC
	private void AddOpenConnections(Room newRoom, RoomConnection skipConnection)
	{
		RoomConnection[] connections = newRoom.GetConnections();
		if (skipConnection != null)
		{
			foreach (RoomConnection roomConnection in connections)
			{
				if (!roomConnection.m_entrance && Vector3.Distance(roomConnection.transform.position, skipConnection.transform.position) >= 0.1f)
				{
					roomConnection.m_placeOrder = newRoom.m_placeOrder;
					DungeonGenerator.m_openConnections.Add(roomConnection);
				}
			}
			return;
		}
		RoomConnection[] array = connections;
		for (int i = 0; i < array.Length; i++)
		{
			array[i].m_placeOrder = newRoom.m_placeOrder;
		}
		DungeonGenerator.m_openConnections.AddRange(connections);
	}

	// Token: 0x0600163A RID: 5690 RVA: 0x000A4948 File Offset: 0x000A2B48
	private void SetupColliders()
	{
		if (this.m_colliderA != null)
		{
			return;
		}
		BoxCollider[] componentsInChildren = base.gameObject.GetComponentsInChildren<BoxCollider>();
		for (int i = 0; i < componentsInChildren.Length; i++)
		{
			UnityEngine.Object.DestroyImmediate(componentsInChildren[i]);
		}
		this.m_colliderA = base.gameObject.AddComponent<BoxCollider>();
		this.m_colliderB = base.gameObject.AddComponent<BoxCollider>();
	}

	// Token: 0x0600163B RID: 5691 RVA: 0x000A49A8 File Offset: 0x000A2BA8
	public void Derp()
	{
	}

	// Token: 0x0600163C RID: 5692 RVA: 0x000A49AC File Offset: 0x000A2BAC
	private bool IsInsideDungeon(Room room, Vector3 pos, Quaternion rot)
	{
		Bounds bounds = new Bounds(this.m_zoneCenter, this.m_zoneSize);
		Vector3 vector = room.m_size;
		vector *= 0.5f;
		return bounds.Contains(pos + rot * new Vector3(vector.x, vector.y, -vector.z)) && bounds.Contains(pos + rot * new Vector3(-vector.x, vector.y, -vector.z)) && bounds.Contains(pos + rot * new Vector3(vector.x, vector.y, vector.z)) && bounds.Contains(pos + rot * new Vector3(-vector.x, vector.y, vector.z)) && bounds.Contains(pos + rot * new Vector3(vector.x, -vector.y, -vector.z)) && bounds.Contains(pos + rot * new Vector3(-vector.x, -vector.y, -vector.z)) && bounds.Contains(pos + rot * new Vector3(vector.x, -vector.y, vector.z)) && bounds.Contains(pos + rot * new Vector3(-vector.x, -vector.y, vector.z));
	}

	// Token: 0x0600163D RID: 5693 RVA: 0x000A4B64 File Offset: 0x000A2D64
	private bool TestCollision(Room room, Vector3 pos, Quaternion rot)
	{
		if (!this.IsInsideDungeon(room, pos, rot))
		{
			return true;
		}
		this.m_colliderA.size = new Vector3((float)room.m_size.x - 0.1f, (float)room.m_size.y - 0.1f, (float)room.m_size.z - 0.1f);
		foreach (Room room2 in DungeonGenerator.m_placedRooms)
		{
			this.m_colliderB.size = room2.m_size;
			Vector3 vector;
			float num;
			if (Physics.ComputePenetration(this.m_colliderA, pos, rot, this.m_colliderB, room2.transform.position, room2.transform.rotation, out vector, out num))
			{
				return true;
			}
		}
		return false;
	}

	// Token: 0x0600163E RID: 5694 RVA: 0x000A4C50 File Offset: 0x000A2E50
	private DungeonDB.RoomData GetRandomWeightedRoom(bool perimeterRoom)
	{
		DungeonGenerator.m_tempRooms.Clear();
		float num = 0f;
		foreach (DungeonDB.RoomData roomData in DungeonGenerator.m_availableRooms)
		{
			if (!roomData.RoomInPrefab.m_entrance && !roomData.RoomInPrefab.m_endCap && !roomData.RoomInPrefab.m_divider && roomData.RoomInPrefab.m_perimeter == perimeterRoom)
			{
				num += roomData.RoomInPrefab.m_weight;
				DungeonGenerator.m_tempRooms.Add(roomData);
			}
		}
		if (DungeonGenerator.m_tempRooms.Count == 0)
		{
			return null;
		}
		float num2 = UnityEngine.Random.Range(0f, num);
		float num3 = 0f;
		foreach (DungeonDB.RoomData roomData2 in DungeonGenerator.m_tempRooms)
		{
			num3 += roomData2.RoomInPrefab.m_weight;
			if (num2 <= num3)
			{
				return roomData2;
			}
		}
		return DungeonGenerator.m_tempRooms[0];
	}

	// Token: 0x0600163F RID: 5695 RVA: 0x000A4D84 File Offset: 0x000A2F84
	private DungeonDB.RoomData GetRandomWeightedRoom(RoomConnection connection)
	{
		DungeonGenerator.m_tempRooms.Clear();
		foreach (DungeonDB.RoomData roomData in DungeonGenerator.m_availableRooms)
		{
			if (!roomData.RoomInPrefab.m_entrance && !roomData.RoomInPrefab.m_endCap && !roomData.RoomInPrefab.m_divider && (!connection || (roomData.RoomInPrefab.HaveConnection(connection) && connection.m_placeOrder >= roomData.RoomInPrefab.m_minPlaceOrder)))
			{
				DungeonGenerator.m_tempRooms.Add(roomData);
			}
		}
		if (DungeonGenerator.m_tempRooms.Count == 0)
		{
			return null;
		}
		return this.GetWeightedRoom(DungeonGenerator.m_tempRooms);
	}

	// Token: 0x06001640 RID: 5696 RVA: 0x000A4E50 File Offset: 0x000A3050
	private DungeonDB.RoomData GetWeightedRoom(List<DungeonDB.RoomData> rooms)
	{
		float num = 0f;
		foreach (DungeonDB.RoomData roomData in rooms)
		{
			num += roomData.RoomInPrefab.m_weight;
		}
		float num2 = UnityEngine.Random.Range(0f, num);
		float num3 = 0f;
		foreach (DungeonDB.RoomData roomData2 in rooms)
		{
			num3 += roomData2.RoomInPrefab.m_weight;
			if (num2 <= num3)
			{
				return roomData2;
			}
		}
		return DungeonGenerator.m_tempRooms[0];
	}

	// Token: 0x06001641 RID: 5697 RVA: 0x000A4F20 File Offset: 0x000A3120
	private DungeonDB.RoomData GetRandomRoom(RoomConnection connection)
	{
		DungeonGenerator.m_tempRooms.Clear();
		foreach (DungeonDB.RoomData roomData in DungeonGenerator.m_availableRooms)
		{
			if (!roomData.RoomInPrefab.m_entrance && !roomData.RoomInPrefab.m_endCap && !roomData.RoomInPrefab.m_divider && (!connection || (roomData.RoomInPrefab.HaveConnection(connection) && connection.m_placeOrder >= roomData.RoomInPrefab.m_minPlaceOrder)))
			{
				DungeonGenerator.m_tempRooms.Add(roomData);
			}
		}
		if (DungeonGenerator.m_tempRooms.Count == 0)
		{
			return null;
		}
		return DungeonGenerator.m_tempRooms[UnityEngine.Random.Range(0, DungeonGenerator.m_tempRooms.Count)];
	}

	// Token: 0x06001642 RID: 5698 RVA: 0x000A4FF8 File Offset: 0x000A31F8
	private RoomConnection GetOpenConnection()
	{
		if (DungeonGenerator.m_openConnections.Count == 0)
		{
			return null;
		}
		return DungeonGenerator.m_openConnections[UnityEngine.Random.Range(0, DungeonGenerator.m_openConnections.Count)];
	}

	// Token: 0x06001643 RID: 5699 RVA: 0x000A5024 File Offset: 0x000A3224
	private DungeonDB.RoomData FindStartRoom()
	{
		DungeonGenerator.m_tempRooms.Clear();
		foreach (DungeonDB.RoomData roomData in DungeonGenerator.m_availableRooms)
		{
			if (roomData.RoomInPrefab.m_entrance)
			{
				DungeonGenerator.m_tempRooms.Add(roomData);
			}
		}
		return DungeonGenerator.m_tempRooms[UnityEngine.Random.Range(0, DungeonGenerator.m_tempRooms.Count)];
	}

	// Token: 0x06001644 RID: 5700 RVA: 0x000A50AC File Offset: 0x000A32AC
	private bool CheckRequiredRooms()
	{
		if (this.m_minRequiredRooms == 0 || this.m_requiredRooms.Count == 0)
		{
			return false;
		}
		int num = 0;
		foreach (Room room in DungeonGenerator.m_placedRooms)
		{
			if (this.m_requiredRooms.Contains(room.gameObject.name))
			{
				num++;
			}
		}
		return num >= this.m_minRequiredRooms;
	}

	// Token: 0x06001645 RID: 5701 RVA: 0x000A5138 File Offset: 0x000A3338
	private void OnDrawGizmos()
	{
		Gizmos.color = new Color(0f, 1.5f, 0f, 0.5f);
		Gizmos.DrawWireCube(this.m_zoneCenter, new Vector3(this.m_zoneSize.x, this.m_zoneSize.y, this.m_zoneSize.z));
		Gizmos.matrix = Matrix4x4.identity;
	}

	// Token: 0x040015BD RID: 5565
	private static MemoryStream saveStream = new MemoryStream();

	// Token: 0x040015BE RID: 5566
	private static BinaryWriter saveWriter = new BinaryWriter(DungeonGenerator.saveStream);

	// Token: 0x040015BF RID: 5567
	public static int m_forceSeed = int.MinValue;

	// Token: 0x040015C0 RID: 5568
	public DungeonGenerator.Algorithm m_algorithm;

	// Token: 0x040015C1 RID: 5569
	public int m_maxRooms = 3;

	// Token: 0x040015C2 RID: 5570
	public int m_minRooms = 20;

	// Token: 0x040015C3 RID: 5571
	public int m_minRequiredRooms;

	// Token: 0x040015C4 RID: 5572
	public List<string> m_requiredRooms = new List<string>();

	// Token: 0x040015C5 RID: 5573
	[global::Tooltip("Rooms and endcaps will be placed using weights.")]
	public bool m_alternativeFunctionality;

	// Token: 0x040015C6 RID: 5574
	[BitMask(typeof(Room.Theme))]
	public Room.Theme m_themes = Room.Theme.Crypt;

	// Token: 0x040015C7 RID: 5575
	[Header("Dungeon")]
	public List<DungeonGenerator.DoorDef> m_doorTypes = new List<DungeonGenerator.DoorDef>();

	// Token: 0x040015C8 RID: 5576
	[Range(0f, 1f)]
	public float m_doorChance = 0.5f;

	// Token: 0x040015C9 RID: 5577
	[Header("Camp")]
	public float m_maxTilt = 10f;

	// Token: 0x040015CA RID: 5578
	public float m_tileWidth = 8f;

	// Token: 0x040015CB RID: 5579
	public int m_gridSize = 4;

	// Token: 0x040015CC RID: 5580
	public float m_spawnChance = 1f;

	// Token: 0x040015CD RID: 5581
	[Header("Camp radial")]
	public float m_campRadiusMin = 15f;

	// Token: 0x040015CE RID: 5582
	public float m_campRadiusMax = 30f;

	// Token: 0x040015CF RID: 5583
	public float m_minAltitude = 1f;

	// Token: 0x040015D0 RID: 5584
	public int m_perimeterSections;

	// Token: 0x040015D1 RID: 5585
	public float m_perimeterBuffer = 2f;

	// Token: 0x040015D2 RID: 5586
	[Header("Misc")]
	public Vector3 m_zoneCenter = new Vector3(0f, 0f, 0f);

	// Token: 0x040015D3 RID: 5587
	public Vector3 m_zoneSize = new Vector3(64f, 64f, 64f);

	// Token: 0x040015D4 RID: 5588
	[global::Tooltip("Makes the dungeon entrance start at the given interior transform (including rotation) rather than straight above the entrance, which gives the dungeon much more room to fill out the entire zone. Must use together with Location.m_useCustomInteriorTransform to make sure seeds are deterministic.")]
	public bool m_useCustomInteriorTransform;

	// Token: 0x040015D5 RID: 5589
	[HideInInspector]
	public int m_generatedSeed;

	// Token: 0x040015D6 RID: 5590
	private bool m_hasGeneratedSeed;

	// Token: 0x040015D7 RID: 5591
	private ZDO m_zdoSetToBeLoadingInZone;

	// Token: 0x040015D8 RID: 5592
	private int m_roomsToLoad;

	// Token: 0x040015D9 RID: 5593
	private DungeonGenerator.RoomPlacementData[] m_loadedRooms;

	// Token: 0x040015DA RID: 5594
	private List<IReferenceCounted> m_heldReferences = new List<IReferenceCounted>();

	// Token: 0x040015DB RID: 5595
	private static List<Room> m_placedRooms = new List<Room>();

	// Token: 0x040015DC RID: 5596
	private static List<RoomConnection> m_openConnections = new List<RoomConnection>();

	// Token: 0x040015DD RID: 5597
	private static List<RoomConnection> m_doorConnections = new List<RoomConnection>();

	// Token: 0x040015DE RID: 5598
	private static List<DungeonDB.RoomData> m_availableRooms = new List<DungeonDB.RoomData>();

	// Token: 0x040015DF RID: 5599
	private static List<DungeonDB.RoomData> m_tempRooms = new List<DungeonDB.RoomData>();

	// Token: 0x040015E0 RID: 5600
	private BoxCollider m_colliderA;

	// Token: 0x040015E1 RID: 5601
	private BoxCollider m_colliderB;

	// Token: 0x040015E2 RID: 5602
	private ZNetView m_nview;

	// Token: 0x040015E3 RID: 5603
	[HideInInspector]
	public Vector3 m_originalPosition;

	// Token: 0x02000355 RID: 853
	[Serializable]
	public class DoorDef
	{
		// Token: 0x0400256C RID: 9580
		public GameObject m_prefab;

		// Token: 0x0400256D RID: 9581
		public string m_connectionType = "";

		// Token: 0x0400256E RID: 9582
		[global::Tooltip("Will use default door chance set in DungeonGenerator if set to zero to default to old behaviour")]
		[Range(0f, 1f)]
		public float m_chance;
	}

	// Token: 0x02000356 RID: 854
	private struct RoomPlacementData
	{
		// Token: 0x060022B0 RID: 8880 RVA: 0x000EF267 File Offset: 0x000ED467
		public RoomPlacementData(DungeonDB.RoomData roomData, Vector3 position, Quaternion rotation)
		{
			this.m_roomData = roomData;
			this.m_position = position;
			this.m_rotation = rotation;
		}

		// Token: 0x0400256F RID: 9583
		public DungeonDB.RoomData m_roomData;

		// Token: 0x04002570 RID: 9584
		public Vector3 m_position;

		// Token: 0x04002571 RID: 9585
		public Quaternion m_rotation;
	}

	// Token: 0x02000357 RID: 855
	public enum Algorithm
	{
		// Token: 0x04002573 RID: 9587
		Dungeon,
		// Token: 0x04002574 RID: 9588
		CampGrid,
		// Token: 0x04002575 RID: 9589
		CampRadial
	}
}
