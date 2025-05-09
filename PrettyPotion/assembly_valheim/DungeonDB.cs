using System;
using System.Collections.Generic;
using SoftReferenceableAssets;
using UnityEngine;

// Token: 0x0200016F RID: 367
public class DungeonDB : MonoBehaviour
{
	// Token: 0x170000BF RID: 191
	// (get) Token: 0x0600160F RID: 5647 RVA: 0x000A2BD2 File Offset: 0x000A0DD2
	public static DungeonDB instance
	{
		get
		{
			return DungeonDB.m_instance;
		}
	}

	// Token: 0x06001610 RID: 5648 RVA: 0x000A2BDC File Offset: 0x000A0DDC
	private void Awake()
	{
		DungeonDB.m_instance = this;
		ZLog.Log("DungeonDB Awake " + Time.frameCount.ToString());
	}

	// Token: 0x06001611 RID: 5649 RVA: 0x000A2C0B File Offset: 0x000A0E0B
	public bool SkipSaving()
	{
		return this.m_error;
	}

	// Token: 0x06001612 RID: 5650 RVA: 0x000A2C14 File Offset: 0x000A0E14
	private void Start()
	{
		ZLog.Log("DungeonDB Start " + Time.frameCount.ToString());
		this.SetupRooms();
		this.GenerateHashList();
		this.LoadRooms();
	}

	// Token: 0x06001613 RID: 5651 RVA: 0x000A2C50 File Offset: 0x000A0E50
	private void SetupRooms()
	{
		foreach (GameObject original in this.m_roomLists)
		{
			UnityEngine.Object.Instantiate<GameObject>(original);
		}
		foreach (RoomList roomList in RoomList.GetAllRoomLists())
		{
			this.m_rooms.AddRange(roomList.m_rooms);
		}
	}

	// Token: 0x06001614 RID: 5652 RVA: 0x000A2CEC File Offset: 0x000A0EEC
	public static List<DungeonDB.RoomData> GetRooms()
	{
		return DungeonDB.m_instance.m_rooms;
	}

	// Token: 0x06001615 RID: 5653 RVA: 0x000A2CF8 File Offset: 0x000A0EF8
	public DungeonDB.RoomData GetRoom(int hash)
	{
		DungeonDB.RoomData result;
		if (this.m_roomByHash.TryGetValue(hash, out result))
		{
			return result;
		}
		return null;
	}

	// Token: 0x06001616 RID: 5654 RVA: 0x000A2D18 File Offset: 0x000A0F18
	private void GenerateHashList()
	{
		this.m_roomByHash.Clear();
		foreach (DungeonDB.RoomData roomData in this.m_rooms)
		{
			int hash = roomData.Hash;
			if (this.m_roomByHash.ContainsKey(hash))
			{
				ZLog.LogError("Room with name " + roomData.m_prefab.Name + " already registered");
			}
			else
			{
				this.m_roomByHash.Add(hash, roomData);
			}
		}
	}

	// Token: 0x06001617 RID: 5655 RVA: 0x000A2DB4 File Offset: 0x000A0FB4
	private void LoadRooms()
	{
		if (Settings.AssetMemoryUsagePolicy.HasFlag(AssetMemoryUsagePolicy.KeepAsynchronousLoadedBit))
		{
			ReferenceHolder referenceHolder = base.gameObject.AddComponent<ReferenceHolder>();
			foreach (DungeonDB.RoomData roomData in this.m_rooms)
			{
				if (roomData.m_enabled)
				{
					roomData.m_prefab.Load();
					referenceHolder.HoldReferenceTo(roomData.m_prefab);
					roomData.m_prefab.Release();
				}
			}
		}
	}

	// Token: 0x040015B7 RID: 5559
	private static DungeonDB m_instance;

	// Token: 0x040015B8 RID: 5560
	public List<string> m_roomScenes = new List<string>();

	// Token: 0x040015B9 RID: 5561
	public List<GameObject> m_roomLists = new List<GameObject>();

	// Token: 0x040015BA RID: 5562
	private List<DungeonDB.RoomData> m_rooms = new List<DungeonDB.RoomData>();

	// Token: 0x040015BB RID: 5563
	private Dictionary<int, DungeonDB.RoomData> m_roomByHash = new Dictionary<int, DungeonDB.RoomData>();

	// Token: 0x040015BC RID: 5564
	private bool m_error;

	// Token: 0x02000354 RID: 852
	[Serializable]
	public class RoomData
	{
		// Token: 0x170001D4 RID: 468
		// (get) Token: 0x060022AC RID: 8876 RVA: 0x000EF1D4 File Offset: 0x000ED3D4
		public Room RoomInPrefab
		{
			get
			{
				if (this.m_loadedRoom == null)
				{
					if (this.m_prefab.Asset != null)
					{
						this.m_loadedRoom = this.m_prefab.Asset.GetComponent<Room>();
					}
					else
					{
						Debug.LogError(string.Format("Room {0} wasn't loaded!", this.m_prefab));
					}
				}
				return this.m_loadedRoom;
			}
		}

		// Token: 0x170001D5 RID: 469
		// (get) Token: 0x060022AD RID: 8877 RVA: 0x000EF23A File Offset: 0x000ED43A
		public int Hash
		{
			get
			{
				return this.m_prefab.Name.GetStableHashCode();
			}
		}

		// Token: 0x04002568 RID: 9576
		public SoftReference<GameObject> m_prefab;

		// Token: 0x04002569 RID: 9577
		public bool m_enabled;

		// Token: 0x0400256A RID: 9578
		[BitMask(typeof(Room.Theme))]
		public Room.Theme m_theme;

		// Token: 0x0400256B RID: 9579
		[NonSerialized]
		private Room m_loadedRoom;
	}
}
