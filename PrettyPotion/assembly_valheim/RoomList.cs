using System;
using System.Collections.Generic;
using UnityEngine;

// Token: 0x02000135 RID: 309
public class RoomList : MonoBehaviour
{
	// Token: 0x06001383 RID: 4995 RVA: 0x00090A3C File Offset: 0x0008EC3C
	private void Awake()
	{
		RoomList.s_allRoomLists.Add(this);
	}

	// Token: 0x06001384 RID: 4996 RVA: 0x00090A49 File Offset: 0x0008EC49
	private void OnDestroy()
	{
		RoomList.s_allRoomLists.Remove(this);
	}

	// Token: 0x06001385 RID: 4997 RVA: 0x00090A57 File Offset: 0x0008EC57
	public static List<RoomList> GetAllRoomLists()
	{
		return RoomList.s_allRoomLists;
	}

	// Token: 0x0400137B RID: 4987
	private static List<RoomList> s_allRoomLists = new List<RoomList>();

	// Token: 0x0400137C RID: 4988
	public List<DungeonDB.RoomData> m_rooms = new List<DungeonDB.RoomData>();
}
