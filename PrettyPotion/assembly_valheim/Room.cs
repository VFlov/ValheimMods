using System;
using System.Collections.Generic;
using UnityEngine;

// Token: 0x02000171 RID: 369
public class Room : MonoBehaviour
{
	// Token: 0x06001648 RID: 5704 RVA: 0x000A52E2 File Offset: 0x000A34E2
	private void Awake()
	{
		if (this.m_musicPrefab)
		{
			UnityEngine.Object.Instantiate<MusicVolume>(this.m_musicPrefab, base.transform).m_sizeFromRoom = this;
		}
	}

	// Token: 0x06001649 RID: 5705 RVA: 0x000A5308 File Offset: 0x000A3508
	private void OnDrawGizmos()
	{
		Gizmos.color = new Color(0.5f, 0.5f, 0.5f, 0.5f);
		Gizmos.matrix = Matrix4x4.TRS(base.transform.position, base.transform.rotation, new Vector3(1f, 1f, 1f));
		Gizmos.DrawWireCube(Vector3.zero, new Vector3((float)this.m_size.x, (float)this.m_size.y, (float)this.m_size.z));
		Gizmos.matrix = Matrix4x4.identity;
	}

	// Token: 0x0600164A RID: 5706 RVA: 0x000A53A4 File Offset: 0x000A35A4
	public int GetHash()
	{
		return Utils.GetPrefabName(base.gameObject).GetStableHashCode();
	}

	// Token: 0x0600164B RID: 5707 RVA: 0x000A53B6 File Offset: 0x000A35B6
	private void OnEnable()
	{
		this.m_roomConnections = null;
	}

	// Token: 0x0600164C RID: 5708 RVA: 0x000A53BF File Offset: 0x000A35BF
	public RoomConnection[] GetConnections()
	{
		if (this.m_roomConnections == null)
		{
			this.m_roomConnections = base.GetComponentsInChildren<RoomConnection>(false);
		}
		return this.m_roomConnections;
	}

	// Token: 0x0600164D RID: 5709 RVA: 0x000A53DC File Offset: 0x000A35DC
	public RoomConnection GetConnection(RoomConnection other)
	{
		RoomConnection[] connections = this.GetConnections();
		Room.tempConnections.Clear();
		foreach (RoomConnection roomConnection in connections)
		{
			if (roomConnection.m_type == other.m_type)
			{
				Room.tempConnections.Add(roomConnection);
			}
		}
		if (Room.tempConnections.Count == 0)
		{
			return null;
		}
		return Room.tempConnections[UnityEngine.Random.Range(0, Room.tempConnections.Count)];
	}

	// Token: 0x0600164E RID: 5710 RVA: 0x000A5454 File Offset: 0x000A3654
	public RoomConnection GetEntrance()
	{
		RoomConnection[] connections = this.GetConnections();
		ZLog.Log("Connections " + connections.Length.ToString());
		foreach (RoomConnection roomConnection in connections)
		{
			if (roomConnection.m_entrance)
			{
				return roomConnection;
			}
		}
		return null;
	}

	// Token: 0x0600164F RID: 5711 RVA: 0x000A54A4 File Offset: 0x000A36A4
	public bool HaveConnection(RoomConnection other)
	{
		RoomConnection[] connections = this.GetConnections();
		for (int i = 0; i < connections.Length; i++)
		{
			if (connections[i].m_type == other.m_type)
			{
				return true;
			}
		}
		return false;
	}

	// Token: 0x06001650 RID: 5712 RVA: 0x000A54E0 File Offset: 0x000A36E0
	public override string ToString()
	{
		return string.Format("{0}, Enabled: {1}, {2}, {3}", new object[]
		{
			base.name,
			this.m_enabled,
			this.m_theme,
			this.m_entrance ? "Entrance" : (this.m_endCap ? "EndCap" : "Room")
		});
	}

	// Token: 0x040015E4 RID: 5604
	private static List<RoomConnection> tempConnections = new List<RoomConnection>();

	// Token: 0x040015E5 RID: 5605
	public Vector3Int m_size = new Vector3Int(8, 4, 8);

	// Token: 0x040015E6 RID: 5606
	[BitMask(typeof(Room.Theme))]
	public Room.Theme m_theme = Room.Theme.Crypt;

	// Token: 0x040015E7 RID: 5607
	public bool m_enabled = true;

	// Token: 0x040015E8 RID: 5608
	public bool m_entrance;

	// Token: 0x040015E9 RID: 5609
	public bool m_endCap;

	// Token: 0x040015EA RID: 5610
	public bool m_divider;

	// Token: 0x040015EB RID: 5611
	public int m_endCapPrio;

	// Token: 0x040015EC RID: 5612
	public int m_minPlaceOrder;

	// Token: 0x040015ED RID: 5613
	public float m_weight = 1f;

	// Token: 0x040015EE RID: 5614
	public bool m_faceCenter;

	// Token: 0x040015EF RID: 5615
	public bool m_perimeter;

	// Token: 0x040015F0 RID: 5616
	[NonSerialized]
	public int m_placeOrder;

	// Token: 0x040015F1 RID: 5617
	[NonSerialized]
	public int m_seed;

	// Token: 0x040015F2 RID: 5618
	public MusicVolume m_musicPrefab;

	// Token: 0x040015F3 RID: 5619
	private RoomConnection[] m_roomConnections;

	// Token: 0x02000359 RID: 857
	public enum Theme
	{
		// Token: 0x04002579 RID: 9593
		None,
		// Token: 0x0400257A RID: 9594
		Crypt,
		// Token: 0x0400257B RID: 9595
		SunkenCrypt,
		// Token: 0x0400257C RID: 9596
		Cave = 4,
		// Token: 0x0400257D RID: 9597
		ForestCrypt = 8,
		// Token: 0x0400257E RID: 9598
		GoblinCamp = 16,
		// Token: 0x0400257F RID: 9599
		MeadowsVillage = 32,
		// Token: 0x04002580 RID: 9600
		MeadowsFarm = 64,
		// Token: 0x04002581 RID: 9601
		DvergerTown = 128,
		// Token: 0x04002582 RID: 9602
		DvergerBoss = 256,
		// Token: 0x04002583 RID: 9603
		ForestCryptHildir = 512,
		// Token: 0x04002584 RID: 9604
		CaveHildir = 1024,
		// Token: 0x04002585 RID: 9605
		PlainsFortHildir = 2048,
		// Token: 0x04002586 RID: 9606
		AshlandRuins = 4096,
		// Token: 0x04002587 RID: 9607
		FortressRuins = 8192
	}
}
