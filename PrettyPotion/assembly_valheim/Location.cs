using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

// Token: 0x02000192 RID: 402
public class Location : MonoBehaviour
{
	// Token: 0x060017FF RID: 6143 RVA: 0x000B3344 File Offset: 0x000B1544
	private void Awake()
	{
		Location.s_allLocations.Add(this);
		if (this.m_hasInterior)
		{
			Vector3 zoneCenter = this.GetZoneCenter();
			Vector3 position = new Vector3(zoneCenter.x, base.transform.position.y + 5000f, zoneCenter.z);
			GameObject gameObject = UnityEngine.Object.Instantiate<GameObject>(this.m_interiorPrefab, position, Quaternion.identity, base.transform);
			gameObject.transform.localScale = new Vector3(64f, 500f, 64f);
			gameObject.GetComponent<EnvZone>().m_environment = this.m_interiorEnvironment;
		}
	}

	// Token: 0x06001800 RID: 6144 RVA: 0x000B33DA File Offset: 0x000B15DA
	private Vector3 GetZoneCenter()
	{
		return ZoneSystem.GetZonePos(ZoneSystem.GetZone(base.transform.position));
	}

	// Token: 0x06001801 RID: 6145 RVA: 0x000B33F1 File Offset: 0x000B15F1
	private void OnDestroy()
	{
		Location.s_allLocations.Remove(this);
	}

	// Token: 0x06001802 RID: 6146 RVA: 0x000B3400 File Offset: 0x000B1600
	private void OnDrawGizmos()
	{
		Gizmos.color = new Color(0.8f, 0.8f, 0.8f, 0.5f);
		Gizmos.matrix = Matrix4x4.TRS(base.transform.position + new Vector3(0f, -0.01f, 0f), Quaternion.identity, new Vector3(1f, 0.001f, 1f));
		Gizmos.DrawSphere(Vector3.zero, this.m_exteriorRadius);
		Utils.DrawGizmoCircle(base.transform.position, this.m_noBuildRadiusOverride, 32);
		Gizmos.matrix = Matrix4x4.identity;
		Utils.DrawGizmoCircle(base.transform.position, this.m_exteriorRadius, 32);
		if (this.m_hasInterior)
		{
			Utils.DrawGizmoCircle(base.transform.position + new Vector3(0f, 5000f, 0f), this.m_interiorRadius, 32);
			Utils.DrawGizmoCircle(base.transform.position, this.m_interiorRadius, 32);
			Gizmos.matrix = Matrix4x4.TRS(base.transform.position + new Vector3(0f, 5000f, 0f), Quaternion.identity, new Vector3(1f, 0.001f, 1f));
			Gizmos.DrawSphere(Vector3.zero, this.m_interiorRadius);
			Gizmos.matrix = Matrix4x4.identity;
		}
	}

	// Token: 0x06001803 RID: 6147 RVA: 0x000B3571 File Offset: 0x000B1771
	public float GetMaxRadius()
	{
		if (!this.m_hasInterior)
		{
			return this.m_exteriorRadius;
		}
		return Mathf.Max(this.m_exteriorRadius, this.m_interiorRadius);
	}

	// Token: 0x06001804 RID: 6148 RVA: 0x000B3594 File Offset: 0x000B1794
	public bool IsInside(Vector3 point, float radius, bool buildCheck = false)
	{
		float num = (buildCheck && this.m_noBuildRadiusOverride > 0f) ? this.m_noBuildRadiusOverride : this.GetMaxRadius();
		return Utils.DistanceXZ(base.transform.position, point) < num + radius;
	}

	// Token: 0x06001805 RID: 6149 RVA: 0x000B35D8 File Offset: 0x000B17D8
	public static bool IsInsideLocation(Vector3 point, float distance)
	{
		using (List<Location>.Enumerator enumerator = Location.s_allLocations.GetEnumerator())
		{
			while (enumerator.MoveNext())
			{
				if (enumerator.Current.IsInside(point, distance, false))
				{
					return true;
				}
			}
		}
		return false;
	}

	// Token: 0x06001806 RID: 6150 RVA: 0x000B3634 File Offset: 0x000B1834
	public static Location GetLocation(Vector3 point, bool checkDungeons = true)
	{
		if (Character.InInterior(point))
		{
			return Location.GetZoneLocation(point);
		}
		foreach (Location location in Location.s_allLocations)
		{
			if (location.IsInside(point, 0f, false))
			{
				return location;
			}
		}
		return null;
	}

	// Token: 0x06001807 RID: 6151 RVA: 0x000B36A4 File Offset: 0x000B18A4
	public static Location GetZoneLocation(Vector2i zone)
	{
		foreach (Location location in Location.s_allLocations)
		{
			if (zone == ZoneSystem.GetZone(location.transform.position))
			{
				return location;
			}
		}
		return null;
	}

	// Token: 0x06001808 RID: 6152 RVA: 0x000B3710 File Offset: 0x000B1910
	public static Location GetZoneLocation(Vector3 point)
	{
		Vector2i zone = ZoneSystem.GetZone(point);
		foreach (Location location in Location.s_allLocations)
		{
			if (zone == ZoneSystem.GetZone(location.transform.position))
			{
				return location;
			}
		}
		return null;
	}

	// Token: 0x06001809 RID: 6153 RVA: 0x000B3784 File Offset: 0x000B1984
	public static bool IsInsideNoBuildLocation(Vector3 point)
	{
		foreach (Location location in Location.s_allLocations)
		{
			if (location.m_noBuild && location.IsInside(point, 0f, true))
			{
				return true;
			}
		}
		return false;
	}

	// Token: 0x0600180A RID: 6154 RVA: 0x000B37F0 File Offset: 0x000B19F0
	public static bool IsInsideActiveBossDungeon(Vector3 point)
	{
		if (EnemyHud.instance != null)
		{
			Character activeBoss = EnemyHud.instance.GetActiveBoss();
			if (activeBoss != null && activeBoss.m_bossEvent.Length > 0)
			{
				Vector2i zone = ZoneSystem.GetZone(point);
				Vector2i zone2 = ZoneSystem.GetZone(activeBoss.transform.position);
				if (zone == zone2)
				{
					return true;
				}
			}
		}
		return false;
	}

	// Token: 0x040017E7 RID: 6119
	[FormerlySerializedAs("m_radius")]
	public float m_exteriorRadius = 20f;

	// Token: 0x040017E8 RID: 6120
	public bool m_noBuild = true;

	// Token: 0x040017E9 RID: 6121
	public float m_noBuildRadiusOverride;

	// Token: 0x040017EA RID: 6122
	public bool m_clearArea = true;

	// Token: 0x040017EB RID: 6123
	public string m_discoverLabel = "";

	// Token: 0x040017EC RID: 6124
	[Header("Other")]
	public bool m_applyRandomDamage;

	// Token: 0x040017ED RID: 6125
	[Header("Interior")]
	public bool m_hasInterior;

	// Token: 0x040017EE RID: 6126
	public float m_interiorRadius = 20f;

	// Token: 0x040017EF RID: 6127
	public string m_interiorEnvironment = "";

	// Token: 0x040017F0 RID: 6128
	public Transform m_interiorTransform;

	// Token: 0x040017F1 RID: 6129
	[global::Tooltip("Makes the dungeon entrance start at the given interior transform (including rotation) rather than straight above the entrance, which gives the dungeon much more room to fill out the entire zone. Must use together with DungeonGenerator.m_useCustomInteriorTransform to make sure seeds are deterministic.")]
	public bool m_useCustomInteriorTransform;

	// Token: 0x040017F2 RID: 6130
	public DungeonGenerator m_generator;

	// Token: 0x040017F3 RID: 6131
	public GameObject m_interiorPrefab;

	// Token: 0x040017F4 RID: 6132
	[Header("Spawners")]
	public int m_enemyMinLevelOverride = -1;

	// Token: 0x040017F5 RID: 6133
	public int m_enemyMaxLevelOverride = -1;

	// Token: 0x040017F6 RID: 6134
	public float m_enemyLevelUpOverride = -1f;

	// Token: 0x040017F7 RID: 6135
	[global::Tooltip("Exludes CreatureSpawners of specified groups for level up override values above.")]
	public List<int> m_excludeEnemyLevelOverrideGroups = new List<int>();

	// Token: 0x040017F8 RID: 6136
	[global::Tooltip("Blocks any CreatureSpawner that is set to given SpawnGroups of these IDs.")]
	public List<int> m_blockSpawnGroups = new List<int>();

	// Token: 0x040017F9 RID: 6137
	private static List<Location> s_allLocations = new List<Location>();

	// Token: 0x040017FA RID: 6138
	public Heightmap.Biome m_biome;
}
