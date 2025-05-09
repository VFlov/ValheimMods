using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

// Token: 0x0200012C RID: 300
public class Pathfinding : MonoBehaviour
{
	// Token: 0x170000A4 RID: 164
	// (get) Token: 0x060012E6 RID: 4838 RVA: 0x0008C8FB File Offset: 0x0008AAFB
	public static Pathfinding instance
	{
		get
		{
			return Pathfinding.m_instance;
		}
	}

	// Token: 0x060012E7 RID: 4839 RVA: 0x0008C902 File Offset: 0x0008AB02
	private void Awake()
	{
		Pathfinding.m_instance = this;
		this.SetupAgents();
		this.m_path = new NavMeshPath();
	}

	// Token: 0x060012E8 RID: 4840 RVA: 0x0008C91C File Offset: 0x0008AB1C
	private void ClearAgentSettings()
	{
		List<NavMeshBuildSettings> list = new List<NavMeshBuildSettings>();
		for (int i = 0; i < NavMesh.GetSettingsCount(); i++)
		{
			list.Add(NavMesh.GetSettingsByIndex(i));
		}
		foreach (NavMeshBuildSettings navMeshBuildSettings in list)
		{
			if (navMeshBuildSettings.agentTypeID != 0)
			{
				NavMesh.RemoveSettings(navMeshBuildSettings.agentTypeID);
			}
		}
	}

	// Token: 0x060012E9 RID: 4841 RVA: 0x0008C99C File Offset: 0x0008AB9C
	private void OnDestroy()
	{
		foreach (Pathfinding.NavMeshTile navMeshTile in this.m_tiles.Values)
		{
			this.ClearLinks(navMeshTile);
			if (navMeshTile.m_data)
			{
				NavMesh.RemoveNavMeshData(navMeshTile.m_instance);
			}
		}
		this.m_tiles.Clear();
		this.DestroyAllLinks();
	}

	// Token: 0x060012EA RID: 4842 RVA: 0x0008CA20 File Offset: 0x0008AC20
	private Pathfinding.AgentSettings AddAgent(Pathfinding.AgentType type, Pathfinding.AgentSettings copy = null)
	{
		while (type + 1 > (Pathfinding.AgentType)this.m_agentSettings.Count)
		{
			this.m_agentSettings.Add(null);
		}
		Pathfinding.AgentSettings agentSettings = new Pathfinding.AgentSettings(type);
		if (copy != null)
		{
			agentSettings.m_build.agentHeight = copy.m_build.agentHeight;
			agentSettings.m_build.agentClimb = copy.m_build.agentClimb;
			agentSettings.m_build.agentRadius = copy.m_build.agentRadius;
			agentSettings.m_build.agentSlope = copy.m_build.agentSlope;
		}
		this.m_agentSettings[(int)type] = agentSettings;
		return agentSettings;
	}

	// Token: 0x060012EB RID: 4843 RVA: 0x0008CAC0 File Offset: 0x0008ACC0
	private void SetupAgents()
	{
		this.ClearAgentSettings();
		Pathfinding.AgentSettings agentSettings = this.AddAgent(Pathfinding.AgentType.Humanoid, null);
		agentSettings.m_build.agentHeight = 1.8f;
		agentSettings.m_build.agentClimb = 0.3f;
		agentSettings.m_build.agentRadius = 0.4f;
		agentSettings.m_build.agentSlope = 85f;
		this.AddAgent(Pathfinding.AgentType.HumanoidNoSwim, agentSettings).m_canSwim = false;
		Pathfinding.AgentSettings agentSettings2 = this.AddAgent(Pathfinding.AgentType.HumanoidBig, agentSettings);
		agentSettings2.m_build.agentHeight = 2.5f;
		agentSettings2.m_build.agentClimb = 0.3f;
		agentSettings2.m_build.agentRadius = 0.5f;
		agentSettings2.m_build.agentSlope = 85f;
		Pathfinding.AgentSettings agentSettings3 = this.AddAgent(Pathfinding.AgentType.HumanoidBigNoSwim, null);
		agentSettings3.m_build.agentHeight = 2.5f;
		agentSettings3.m_build.agentClimb = 0.3f;
		agentSettings3.m_build.agentRadius = 0.5f;
		agentSettings3.m_build.agentSlope = 85f;
		agentSettings3.m_canSwim = false;
		this.AddAgent(Pathfinding.AgentType.HumanoidAvoidWater, agentSettings).m_avoidWater = true;
		Pathfinding.AgentSettings agentSettings4 = this.AddAgent(Pathfinding.AgentType.TrollSize, null);
		agentSettings4.m_build.agentHeight = 7f;
		agentSettings4.m_build.agentClimb = 0.6f;
		agentSettings4.m_build.agentRadius = 1f;
		agentSettings4.m_build.agentSlope = 85f;
		Pathfinding.AgentSettings agentSettings5 = this.AddAgent(Pathfinding.AgentType.Abomination, null);
		agentSettings5.m_build.agentHeight = 5f;
		agentSettings5.m_build.agentClimb = 0.6f;
		agentSettings5.m_build.agentRadius = 1.5f;
		agentSettings5.m_build.agentSlope = 85f;
		Pathfinding.AgentSettings agentSettings6 = this.AddAgent(Pathfinding.AgentType.SeekerQueen, null);
		agentSettings6.m_build.agentHeight = 7f;
		agentSettings6.m_build.agentClimb = 0.6f;
		agentSettings6.m_build.agentRadius = 1.5f;
		agentSettings6.m_build.agentSlope = 85f;
		Pathfinding.AgentSettings agentSettings7 = this.AddAgent(Pathfinding.AgentType.GoblinBruteSize, null);
		agentSettings7.m_build.agentHeight = 3.5f;
		agentSettings7.m_build.agentClimb = 0.3f;
		agentSettings7.m_build.agentRadius = 0.8f;
		agentSettings7.m_build.agentSlope = 85f;
		Pathfinding.AgentSettings agentSettings8 = this.AddAgent(Pathfinding.AgentType.HugeSize, null);
		agentSettings8.m_build.agentHeight = 10f;
		agentSettings8.m_build.agentClimb = 0.6f;
		agentSettings8.m_build.agentRadius = 2f;
		agentSettings8.m_build.agentSlope = 85f;
		Pathfinding.AgentSettings agentSettings9 = this.AddAgent(Pathfinding.AgentType.HorseSize, null);
		agentSettings9.m_build.agentHeight = 2.5f;
		agentSettings9.m_build.agentClimb = 0.3f;
		agentSettings9.m_build.agentRadius = 0.8f;
		agentSettings9.m_build.agentSlope = 85f;
		Pathfinding.AgentSettings agentSettings10 = this.AddAgent(Pathfinding.AgentType.Fish, null);
		agentSettings10.m_build.agentHeight = 0.5f;
		agentSettings10.m_build.agentClimb = 1f;
		agentSettings10.m_build.agentRadius = 0.5f;
		agentSettings10.m_build.agentSlope = 90f;
		agentSettings10.m_canSwim = true;
		agentSettings10.m_canWalk = false;
		agentSettings10.m_swimDepth = 0.4f;
		agentSettings10.m_areaMask = 12;
		Pathfinding.AgentSettings agentSettings11 = this.AddAgent(Pathfinding.AgentType.BigFish, null);
		agentSettings11.m_build.agentHeight = 1.5f;
		agentSettings11.m_build.agentClimb = 1f;
		agentSettings11.m_build.agentRadius = 1f;
		agentSettings11.m_build.agentSlope = 90f;
		agentSettings11.m_canSwim = true;
		agentSettings11.m_canWalk = false;
		agentSettings11.m_swimDepth = 1.5f;
		agentSettings11.m_areaMask = 12;
		NavMesh.SetAreaCost(0, this.m_defaultCost);
		NavMesh.SetAreaCost(3, this.m_waterCost);
	}

	// Token: 0x060012EC RID: 4844 RVA: 0x0008CE64 File Offset: 0x0008B064
	private Pathfinding.AgentSettings GetSettings(Pathfinding.AgentType agentType)
	{
		return this.m_agentSettings[(int)agentType];
	}

	// Token: 0x060012ED RID: 4845 RVA: 0x0008CE72 File Offset: 0x0008B072
	private int GetAgentID(Pathfinding.AgentType agentType)
	{
		return this.GetSettings(agentType).m_build.agentTypeID;
	}

	// Token: 0x060012EE RID: 4846 RVA: 0x0008CE88 File Offset: 0x0008B088
	private void Update()
	{
		if (this.IsBuilding())
		{
			return;
		}
		this.m_updatePathfindingTimer += Time.deltaTime;
		if (this.m_updatePathfindingTimer > 0.1f)
		{
			this.m_updatePathfindingTimer = 0f;
			this.UpdatePathfinding();
		}
		if (!this.IsBuilding())
		{
			this.DestroyQueuedNavmeshData();
		}
	}

	// Token: 0x060012EF RID: 4847 RVA: 0x0008CEDC File Offset: 0x0008B0DC
	private void DestroyAllLinks()
	{
		while (this.m_linkRemoveQueue.Count > 0 || this.m_tileRemoveQueue.Count > 0)
		{
			this.DestroyQueuedNavmeshData();
		}
	}

	// Token: 0x060012F0 RID: 4848 RVA: 0x0008CF04 File Offset: 0x0008B104
	private void DestroyQueuedNavmeshData()
	{
		if (this.m_linkRemoveQueue.Count > 0)
		{
			int num = Mathf.Min(this.m_linkRemoveQueue.Count, Mathf.Max(25, this.m_linkRemoveQueue.Count / 40));
			for (int i = 0; i < num; i++)
			{
				NavMesh.RemoveLink(this.m_linkRemoveQueue.Dequeue());
			}
			return;
		}
		if (this.m_tileRemoveQueue.Count > 0)
		{
			NavMesh.RemoveNavMeshData(this.m_tileRemoveQueue.Dequeue());
		}
	}

	// Token: 0x060012F1 RID: 4849 RVA: 0x0008CF80 File Offset: 0x0008B180
	private void UpdatePathfinding()
	{
		this.Buildtiles();
		this.TimeoutTiles();
	}

	// Token: 0x060012F2 RID: 4850 RVA: 0x0008CF8E File Offset: 0x0008B18E
	public bool HavePath(Vector3 from, Vector3 to, Pathfinding.AgentType agentType)
	{
		return this.GetPath(from, to, null, agentType, true, false, true);
	}

	// Token: 0x060012F3 RID: 4851 RVA: 0x0008CFA0 File Offset: 0x0008B1A0
	public bool FindValidPoint(out Vector3 point, Vector3 center, float range, Pathfinding.AgentType agentType)
	{
		this.PokePoint(center, agentType);
		Pathfinding.AgentSettings settings = this.GetSettings(agentType);
		NavMeshHit navMeshHit;
		if (NavMesh.SamplePosition(center, out navMeshHit, range, new NavMeshQueryFilter
		{
			agentTypeID = (int)settings.m_agentType,
			areaMask = settings.m_areaMask
		}))
		{
			point = navMeshHit.position;
			return true;
		}
		point = center;
		return false;
	}

	// Token: 0x060012F4 RID: 4852 RVA: 0x0008D004 File Offset: 0x0008B204
	private bool IsUnderTerrain(Vector3 p)
	{
		float num;
		return ZoneSystem.instance.GetGroundHeight(p, out num) && p.y < num - 1f;
	}

	// Token: 0x060012F5 RID: 4853 RVA: 0x0008D034 File Offset: 0x0008B234
	public bool GetPath(Vector3 from, Vector3 to, List<Vector3> path, Pathfinding.AgentType agentType, bool requireFullPath = false, bool cleanup = true, bool havePath = false)
	{
		if (path != null)
		{
			path.Clear();
		}
		this.PokeArea(from, agentType);
		this.PokeArea(to, agentType);
		Pathfinding.AgentSettings settings = this.GetSettings(agentType);
		if (!this.SnapToNavMesh(ref from, true, settings))
		{
			return false;
		}
		if (!this.SnapToNavMesh(ref to, !havePath, settings))
		{
			return false;
		}
		NavMeshQueryFilter filter = new NavMeshQueryFilter
		{
			agentTypeID = settings.m_build.agentTypeID,
			areaMask = settings.m_areaMask
		};
		if (NavMesh.CalculatePath(from, to, filter, this.m_path))
		{
			if (this.m_path.status == NavMeshPathStatus.PathPartial)
			{
				if (this.IsUnderTerrain(this.m_path.corners[0]) || this.IsUnderTerrain(this.m_path.corners[this.m_path.corners.Length - 1]))
				{
					return false;
				}
				if (requireFullPath)
				{
					return false;
				}
			}
			if (path != null)
			{
				path.AddRange(this.m_path.corners);
				if (cleanup)
				{
					this.CleanPath(path, settings);
				}
			}
			return true;
		}
		return false;
	}

	// Token: 0x060012F6 RID: 4854 RVA: 0x0008D138 File Offset: 0x0008B338
	private void CleanPath(List<Vector3> basePath, Pathfinding.AgentSettings settings)
	{
		if (basePath.Count <= 2)
		{
			return;
		}
		NavMeshQueryFilter filter = default(NavMeshQueryFilter);
		filter.agentTypeID = settings.m_build.agentTypeID;
		filter.areaMask = settings.m_areaMask;
		int num = 0;
		this.optPath.Clear();
		this.optPath.Add(basePath[num]);
		do
		{
			num = this.FindNextNode(basePath, filter, num);
			this.optPath.Add(basePath[num]);
		}
		while (num < basePath.Count - 1);
		this.tempPath.Clear();
		this.tempPath.Add(this.optPath[0]);
		for (int i = 1; i < this.optPath.Count - 1; i++)
		{
			Vector3 vector = this.optPath[i - 1];
			Vector3 vector2 = this.optPath[i];
			Vector3 vector3 = this.optPath[i + 1];
			Vector3 normalized = (vector3 - vector2).normalized;
			Vector3 normalized2 = (vector2 - vector).normalized;
			Vector3 vector4 = vector2 - (normalized + normalized2).normalized * Vector3.Distance(vector2, vector) * 0.33f;
			vector4.y = (vector2.y + vector.y) * 0.5f;
			Vector3 normalized3 = (vector4 - vector2).normalized;
			NavMeshHit navMeshHit;
			if (!NavMesh.Raycast(vector2 + normalized3 * 0.1f, vector4, out navMeshHit, filter) && !NavMesh.Raycast(vector4, vector, out navMeshHit, filter))
			{
				this.tempPath.Add(vector4);
			}
			this.tempPath.Add(vector2);
			Vector3 vector5 = vector2 + (normalized + normalized2).normalized * Vector3.Distance(vector2, vector3) * 0.33f;
			vector5.y = (vector2.y + vector3.y) * 0.5f;
			Vector3 normalized4 = (vector5 - vector2).normalized;
			if (!NavMesh.Raycast(vector2 + normalized4 * 0.1f, vector5, out navMeshHit, filter) && !NavMesh.Raycast(vector5, vector3, out navMeshHit, filter))
			{
				this.tempPath.Add(vector5);
			}
		}
		this.tempPath.Add(this.optPath[this.optPath.Count - 1]);
		basePath.Clear();
		basePath.AddRange(this.tempPath);
	}

	// Token: 0x060012F7 RID: 4855 RVA: 0x0008D3D0 File Offset: 0x0008B5D0
	private int FindNextNode(List<Vector3> path, NavMeshQueryFilter filter, int start)
	{
		for (int i = start + 2; i < path.Count; i++)
		{
			NavMeshHit navMeshHit;
			if (NavMesh.Raycast(path[start], path[i], out navMeshHit, filter))
			{
				return i - 1;
			}
		}
		return path.Count - 1;
	}

	// Token: 0x060012F8 RID: 4856 RVA: 0x0008D414 File Offset: 0x0008B614
	private bool SnapToNavMesh(ref Vector3 point, bool extendedSearchArea, Pathfinding.AgentSettings settings)
	{
		if (ZoneSystem.instance)
		{
			float num;
			if (ZoneSystem.instance.GetGroundHeight(point, out num) && point.y < num)
			{
				point.y = num;
			}
			if (settings.m_canSwim)
			{
				point.y = Mathf.Max(30f - settings.m_swimDepth, point.y);
			}
		}
		NavMeshQueryFilter filter = default(NavMeshQueryFilter);
		filter.agentTypeID = settings.m_build.agentTypeID;
		filter.areaMask = settings.m_areaMask;
		NavMeshHit navMeshHit;
		if (extendedSearchArea)
		{
			if (NavMesh.SamplePosition(point, out navMeshHit, 1.5f, filter))
			{
				point = navMeshHit.position;
				return true;
			}
			if (NavMesh.SamplePosition(point, out navMeshHit, 3f, filter))
			{
				point = navMeshHit.position;
				return true;
			}
			if (NavMesh.SamplePosition(point, out navMeshHit, 6f, filter))
			{
				point = navMeshHit.position;
				return true;
			}
			if (NavMesh.SamplePosition(point, out navMeshHit, 12f, filter))
			{
				point = navMeshHit.position;
				return true;
			}
		}
		else if (NavMesh.SamplePosition(point, out navMeshHit, 1f, filter))
		{
			point = navMeshHit.position;
			return true;
		}
		return false;
	}

	// Token: 0x060012F9 RID: 4857 RVA: 0x0008D558 File Offset: 0x0008B758
	private void TimeoutTiles()
	{
		float realtimeSinceStartup = Time.realtimeSinceStartup;
		foreach (KeyValuePair<Vector3Int, Pathfinding.NavMeshTile> keyValuePair in this.m_tiles)
		{
			if (realtimeSinceStartup - keyValuePair.Value.m_pokeTime > this.m_tileTimeout)
			{
				this.ClearLinks(keyValuePair.Value);
				if (keyValuePair.Value.m_instance.valid)
				{
					this.m_tileRemoveQueue.Enqueue(keyValuePair.Value.m_instance);
				}
				this.m_tiles.Remove(keyValuePair.Key);
				break;
			}
		}
	}

	// Token: 0x060012FA RID: 4858 RVA: 0x0008D60C File Offset: 0x0008B80C
	private void PokeArea(Vector3 point, Pathfinding.AgentType agentType)
	{
		Vector3Int tile = this.GetTile(point, agentType);
		this.PokeTile(tile);
		for (int i = -1; i <= 1; i++)
		{
			for (int j = -1; j <= 1; j++)
			{
				if (j != 0 || i != 0)
				{
					Vector3Int tileID = new Vector3Int(tile.x + j, tile.y + i, tile.z);
					this.PokeTile(tileID);
				}
			}
		}
	}

	// Token: 0x060012FB RID: 4859 RVA: 0x0008D670 File Offset: 0x0008B870
	private void PokePoint(Vector3 point, Pathfinding.AgentType agentType)
	{
		Vector3Int tile = this.GetTile(point, agentType);
		this.PokeTile(tile);
	}

	// Token: 0x060012FC RID: 4860 RVA: 0x0008D68D File Offset: 0x0008B88D
	private void PokeTile(Vector3Int tileID)
	{
		this.GetNavTile(tileID).m_pokeTime = Time.realtimeSinceStartup;
	}

	// Token: 0x060012FD RID: 4861 RVA: 0x0008D6A0 File Offset: 0x0008B8A0
	private void Buildtiles()
	{
		if (this.UpdateAsyncBuild())
		{
			return;
		}
		Pathfinding.NavMeshTile navMeshTile = null;
		float num = 0f;
		foreach (KeyValuePair<Vector3Int, Pathfinding.NavMeshTile> keyValuePair in this.m_tiles)
		{
			float num2 = keyValuePair.Value.m_pokeTime - keyValuePair.Value.m_buildTime;
			if (num2 > this.m_updateInterval && (navMeshTile == null || num2 > num))
			{
				navMeshTile = keyValuePair.Value;
				num = num2;
			}
		}
		if (navMeshTile != null)
		{
			this.BuildTile(navMeshTile);
			navMeshTile.m_buildTime = Time.realtimeSinceStartup;
		}
	}

	// Token: 0x060012FE RID: 4862 RVA: 0x0008D74C File Offset: 0x0008B94C
	private void BuildTile(Pathfinding.NavMeshTile tile)
	{
		DateTime now = DateTime.Now;
		List<NavMeshBuildSource> list = new List<NavMeshBuildSource>();
		List<NavMeshBuildMarkup> markups = new List<NavMeshBuildMarkup>();
		Pathfinding.AgentType z = (Pathfinding.AgentType)tile.m_tile.z;
		Pathfinding.AgentSettings settings = this.GetSettings(z);
		Bounds includedWorldBounds = new Bounds(tile.m_center, new Vector3(this.m_tileSize, 6000f, this.m_tileSize));
		Bounds localBounds = new Bounds(Vector3.zero, new Vector3(this.m_tileSize, 6000f, this.m_tileSize));
		int defaultArea = settings.m_canWalk ? 0 : 1;
		NavMeshBuilder.CollectSources(includedWorldBounds, this.m_layers.value, NavMeshCollectGeometry.PhysicsColliders, defaultArea, markups, list);
		if (settings.m_avoidWater)
		{
			List<NavMeshBuildSource> list2 = new List<NavMeshBuildSource>();
			NavMeshBuilder.CollectSources(includedWorldBounds, this.m_waterLayers.value, NavMeshCollectGeometry.PhysicsColliders, 1, markups, list2);
			using (List<NavMeshBuildSource>.Enumerator enumerator = list2.GetEnumerator())
			{
				while (enumerator.MoveNext())
				{
					NavMeshBuildSource item = enumerator.Current;
					item.transform *= Matrix4x4.Translate(Vector3.down * 0.2f);
					list.Add(item);
				}
				goto IL_1AE;
			}
		}
		if (settings.m_canSwim)
		{
			List<NavMeshBuildSource> list3 = new List<NavMeshBuildSource>();
			NavMeshBuilder.CollectSources(includedWorldBounds, this.m_waterLayers.value, NavMeshCollectGeometry.PhysicsColliders, 3, markups, list3);
			if (settings.m_swimDepth != 0f)
			{
				using (List<NavMeshBuildSource>.Enumerator enumerator = list3.GetEnumerator())
				{
					while (enumerator.MoveNext())
					{
						NavMeshBuildSource item2 = enumerator.Current;
						item2.transform *= Matrix4x4.Translate(Vector3.down * settings.m_swimDepth);
						list.Add(item2);
					}
					goto IL_1AE;
				}
			}
			list.AddRange(list3);
		}
		IL_1AE:
		if (tile.m_data == null)
		{
			tile.m_data = new NavMeshData();
			tile.m_data.position = tile.m_center;
		}
		this.m_buildOperation = NavMeshBuilder.UpdateNavMeshDataAsync(tile.m_data, settings.m_build, list, localBounds);
		this.m_buildTile = tile;
	}

	// Token: 0x060012FF RID: 4863 RVA: 0x0008D970 File Offset: 0x0008BB70
	private bool IsBuilding()
	{
		return this.m_buildOperation != null && !this.m_buildOperation.isDone;
	}

	// Token: 0x06001300 RID: 4864 RVA: 0x0008D98C File Offset: 0x0008BB8C
	private bool UpdateAsyncBuild()
	{
		if (this.m_buildOperation == null)
		{
			return false;
		}
		if (!this.m_buildOperation.isDone)
		{
			return true;
		}
		if (!this.m_buildTile.m_instance.valid)
		{
			this.m_buildTile.m_instance = NavMesh.AddNavMeshData(this.m_buildTile.m_data);
		}
		this.RebuildLinks(this.m_buildTile);
		this.m_buildOperation = null;
		this.m_buildTile = null;
		return true;
	}

	// Token: 0x06001301 RID: 4865 RVA: 0x0008D9FA File Offset: 0x0008BBFA
	private void ClearLinks(Pathfinding.NavMeshTile tile)
	{
		this.ClearLinks(tile.m_links1);
		this.ClearLinks(tile.m_links2);
	}

	// Token: 0x06001302 RID: 4866 RVA: 0x0008DA14 File Offset: 0x0008BC14
	private void ClearLinks(List<KeyValuePair<Vector3, NavMeshLinkInstance>> links)
	{
		foreach (KeyValuePair<Vector3, NavMeshLinkInstance> keyValuePair in links)
		{
			this.m_linkRemoveQueue.Enqueue(keyValuePair.Value);
		}
		links.Clear();
	}

	// Token: 0x06001303 RID: 4867 RVA: 0x0008DA74 File Offset: 0x0008BC74
	private void RebuildLinks(Pathfinding.NavMeshTile tile)
	{
		Pathfinding.AgentType z = (Pathfinding.AgentType)tile.m_tile.z;
		Pathfinding.AgentSettings settings = this.GetSettings(z);
		float num = this.m_tileSize / 2f;
		this.ConnectAlongEdge(tile.m_links1, tile.m_center + new Vector3(num, 0f, num), tile.m_center + new Vector3(num, 0f, -num), this.m_linkWidth, settings);
		this.ConnectAlongEdge(tile.m_links2, tile.m_center + new Vector3(-num, 0f, num), tile.m_center + new Vector3(num, 0f, num), this.m_linkWidth, settings);
	}

	// Token: 0x06001304 RID: 4868 RVA: 0x0008DB28 File Offset: 0x0008BD28
	private void ConnectAlongEdge(List<KeyValuePair<Vector3, NavMeshLinkInstance>> links, Vector3 p0, Vector3 p1, float step, Pathfinding.AgentSettings settings)
	{
		Vector3 normalized = (p1 - p0).normalized;
		Vector3 a = Vector3.Cross(Vector3.up, normalized);
		float num = Vector3.Distance(p0, p1);
		bool canSwim = settings.m_canSwim;
		this.tempStitchPoints.Clear();
		for (float num2 = step / 2f; num2 <= num; num2 += step)
		{
			Vector3 p2 = p0 + normalized * num2;
			this.FindGround(p2, canSwim, this.tempStitchPoints, settings);
		}
		if (this.CompareLinks(this.tempStitchPoints, links))
		{
			return;
		}
		this.ClearLinks(links);
		foreach (Vector3 vector in this.tempStitchPoints)
		{
			NavMeshLinkInstance value = NavMesh.AddLink(new NavMeshLinkData
			{
				startPosition = vector - a * 0.1f,
				endPosition = vector + a * 0.1f,
				width = step,
				costModifier = this.m_linkCost,
				bidirectional = true,
				agentTypeID = settings.m_build.agentTypeID,
				area = 2
			});
			if (value.valid)
			{
				links.Add(new KeyValuePair<Vector3, NavMeshLinkInstance>(vector, value));
			}
		}
	}

	// Token: 0x06001305 RID: 4869 RVA: 0x0008DC98 File Offset: 0x0008BE98
	private bool CompareLinks(List<Vector3> tempStitchPoints, List<KeyValuePair<Vector3, NavMeshLinkInstance>> links)
	{
		if (tempStitchPoints.Count != links.Count)
		{
			return false;
		}
		for (int i = 0; i < tempStitchPoints.Count; i++)
		{
			if (tempStitchPoints[i] != links[i].Key)
			{
				return false;
			}
		}
		return true;
	}

	// Token: 0x06001306 RID: 4870 RVA: 0x0008DCE8 File Offset: 0x0008BEE8
	private bool SnapToNearestGround(Vector3 p, out Vector3 pos, float range)
	{
		RaycastHit raycastHit;
		if (Physics.Raycast(p + Vector3.up, Vector3.down, out raycastHit, range + 1f, this.m_layers.value | this.m_waterLayers.value))
		{
			pos = raycastHit.point;
			return true;
		}
		if (Physics.Raycast(p + Vector3.up * range, Vector3.down, out raycastHit, range, this.m_layers.value | this.m_waterLayers.value))
		{
			pos = raycastHit.point;
			return true;
		}
		pos = p;
		return false;
	}

	// Token: 0x06001307 RID: 4871 RVA: 0x0008DD8C File Offset: 0x0008BF8C
	private void FindGround(Vector3 p, bool testWater, List<Vector3> hits, Pathfinding.AgentSettings settings)
	{
		p.y = 6000f;
		int layerMask = testWater ? (this.m_layers.value | this.m_waterLayers.value) : this.m_layers.value;
		float agentHeight = settings.m_build.agentHeight;
		float y = p.y;
		int num = Physics.RaycastNonAlloc(p, Vector3.down, this.tempHitArray, 10000f, layerMask);
		for (int i = 0; i < num; i++)
		{
			Vector3 point = this.tempHitArray[i].point;
			if (Mathf.Abs(point.y - y) >= agentHeight)
			{
				y = point.y;
				if ((1 << this.tempHitArray[i].collider.gameObject.layer & this.m_waterLayers) != 0)
				{
					point.y -= settings.m_swimDepth;
				}
				hits.Add(point);
			}
		}
	}

	// Token: 0x06001308 RID: 4872 RVA: 0x0008DE84 File Offset: 0x0008C084
	private Pathfinding.NavMeshTile GetNavTile(Vector3 point, Pathfinding.AgentType agent)
	{
		Vector3Int tile = this.GetTile(point, agent);
		return this.GetNavTile(tile);
	}

	// Token: 0x06001309 RID: 4873 RVA: 0x0008DEA4 File Offset: 0x0008C0A4
	private Pathfinding.NavMeshTile GetNavTile(Vector3Int tile)
	{
		if (tile == this.m_cachedTileID)
		{
			return this.m_cachedTile;
		}
		Pathfinding.NavMeshTile navMeshTile;
		if (this.m_tiles.TryGetValue(tile, out navMeshTile))
		{
			this.m_cachedTileID = tile;
			this.m_cachedTile = navMeshTile;
			return navMeshTile;
		}
		navMeshTile = new Pathfinding.NavMeshTile();
		navMeshTile.m_tile = tile;
		navMeshTile.m_center = this.GetTilePos(tile);
		this.m_tiles.Add(tile, navMeshTile);
		this.m_cachedTileID = tile;
		this.m_cachedTile = navMeshTile;
		return navMeshTile;
	}

	// Token: 0x0600130A RID: 4874 RVA: 0x0008DF1C File Offset: 0x0008C11C
	private Vector3Int GetTile(Vector3 point, Pathfinding.AgentType agent)
	{
		int x = Mathf.FloorToInt((point.x + this.m_tileSize / 2f) / this.m_tileSize);
		int y = Mathf.FloorToInt((point.z + this.m_tileSize / 2f) / this.m_tileSize);
		return new Vector3Int(x, y, (int)agent);
	}

	// Token: 0x0600130B RID: 4875 RVA: 0x0008DF6F File Offset: 0x0008C16F
	public Vector3 GetTilePos(Vector3Int id)
	{
		return new Vector3((float)id.x * this.m_tileSize, 2500f, (float)id.y * this.m_tileSize);
	}

	// Token: 0x0400129A RID: 4762
	private List<Vector3> tempPath = new List<Vector3>();

	// Token: 0x0400129B RID: 4763
	private List<Vector3> optPath = new List<Vector3>();

	// Token: 0x0400129C RID: 4764
	private List<Vector3> tempStitchPoints = new List<Vector3>();

	// Token: 0x0400129D RID: 4765
	private RaycastHit[] tempHitArray = new RaycastHit[255];

	// Token: 0x0400129E RID: 4766
	private static Pathfinding m_instance;

	// Token: 0x0400129F RID: 4767
	public LayerMask m_layers;

	// Token: 0x040012A0 RID: 4768
	public LayerMask m_waterLayers;

	// Token: 0x040012A1 RID: 4769
	private Dictionary<Vector3Int, Pathfinding.NavMeshTile> m_tiles = new Dictionary<Vector3Int, Pathfinding.NavMeshTile>();

	// Token: 0x040012A2 RID: 4770
	public float m_tileSize = 32f;

	// Token: 0x040012A3 RID: 4771
	public float m_defaultCost = 1f;

	// Token: 0x040012A4 RID: 4772
	public float m_waterCost = 4f;

	// Token: 0x040012A5 RID: 4773
	public float m_linkCost = 10f;

	// Token: 0x040012A6 RID: 4774
	public float m_linkWidth = 1f;

	// Token: 0x040012A7 RID: 4775
	public float m_updateInterval = 5f;

	// Token: 0x040012A8 RID: 4776
	public float m_tileTimeout = 30f;

	// Token: 0x040012A9 RID: 4777
	private const float m_tileHeight = 6000f;

	// Token: 0x040012AA RID: 4778
	private const float m_tileY = 2500f;

	// Token: 0x040012AB RID: 4779
	private float m_updatePathfindingTimer;

	// Token: 0x040012AC RID: 4780
	private Queue<Vector3Int> m_queuedAreas = new Queue<Vector3Int>();

	// Token: 0x040012AD RID: 4781
	private Queue<NavMeshLinkInstance> m_linkRemoveQueue = new Queue<NavMeshLinkInstance>();

	// Token: 0x040012AE RID: 4782
	private Queue<NavMeshDataInstance> m_tileRemoveQueue = new Queue<NavMeshDataInstance>();

	// Token: 0x040012AF RID: 4783
	private Vector3Int m_cachedTileID = new Vector3Int(-9999999, -9999999, -9999999);

	// Token: 0x040012B0 RID: 4784
	private Pathfinding.NavMeshTile m_cachedTile;

	// Token: 0x040012B1 RID: 4785
	private List<Pathfinding.AgentSettings> m_agentSettings = new List<Pathfinding.AgentSettings>();

	// Token: 0x040012B2 RID: 4786
	private AsyncOperation m_buildOperation;

	// Token: 0x040012B3 RID: 4787
	private Pathfinding.NavMeshTile m_buildTile;

	// Token: 0x040012B4 RID: 4788
	private List<KeyValuePair<Pathfinding.NavMeshTile, Pathfinding.NavMeshTile>> m_edgeBuildQueue = new List<KeyValuePair<Pathfinding.NavMeshTile, Pathfinding.NavMeshTile>>();

	// Token: 0x040012B5 RID: 4789
	private NavMeshPath m_path;

	// Token: 0x02000323 RID: 803
	private class NavMeshTile
	{
		// Token: 0x040023FF RID: 9215
		public Vector3Int m_tile;

		// Token: 0x04002400 RID: 9216
		public Vector3 m_center;

		// Token: 0x04002401 RID: 9217
		public float m_pokeTime = -1000f;

		// Token: 0x04002402 RID: 9218
		public float m_buildTime = -1000f;

		// Token: 0x04002403 RID: 9219
		public NavMeshData m_data;

		// Token: 0x04002404 RID: 9220
		public NavMeshDataInstance m_instance;

		// Token: 0x04002405 RID: 9221
		public List<KeyValuePair<Vector3, NavMeshLinkInstance>> m_links1 = new List<KeyValuePair<Vector3, NavMeshLinkInstance>>();

		// Token: 0x04002406 RID: 9222
		public List<KeyValuePair<Vector3, NavMeshLinkInstance>> m_links2 = new List<KeyValuePair<Vector3, NavMeshLinkInstance>>();
	}

	// Token: 0x02000324 RID: 804
	public enum AgentType
	{
		// Token: 0x04002408 RID: 9224
		Humanoid = 1,
		// Token: 0x04002409 RID: 9225
		TrollSize,
		// Token: 0x0400240A RID: 9226
		HugeSize,
		// Token: 0x0400240B RID: 9227
		HorseSize,
		// Token: 0x0400240C RID: 9228
		HumanoidNoSwim,
		// Token: 0x0400240D RID: 9229
		HumanoidAvoidWater,
		// Token: 0x0400240E RID: 9230
		Fish,
		// Token: 0x0400240F RID: 9231
		HumanoidBig,
		// Token: 0x04002410 RID: 9232
		BigFish,
		// Token: 0x04002411 RID: 9233
		GoblinBruteSize,
		// Token: 0x04002412 RID: 9234
		HumanoidBigNoSwim,
		// Token: 0x04002413 RID: 9235
		Abomination,
		// Token: 0x04002414 RID: 9236
		SeekerQueen
	}

	// Token: 0x02000325 RID: 805
	public enum AreaType
	{
		// Token: 0x04002416 RID: 9238
		Default,
		// Token: 0x04002417 RID: 9239
		NotWalkable,
		// Token: 0x04002418 RID: 9240
		Jump,
		// Token: 0x04002419 RID: 9241
		Water
	}

	// Token: 0x02000326 RID: 806
	private class AgentSettings
	{
		// Token: 0x06002238 RID: 8760 RVA: 0x000ECF24 File Offset: 0x000EB124
		public AgentSettings(Pathfinding.AgentType type)
		{
			this.m_agentType = type;
			this.m_build = NavMesh.CreateSettings();
		}

		// Token: 0x0400241A RID: 9242
		public Pathfinding.AgentType m_agentType;

		// Token: 0x0400241B RID: 9243
		public NavMeshBuildSettings m_build;

		// Token: 0x0400241C RID: 9244
		public bool m_canWalk = true;

		// Token: 0x0400241D RID: 9245
		public bool m_avoidWater;

		// Token: 0x0400241E RID: 9246
		public bool m_canSwim = true;

		// Token: 0x0400241F RID: 9247
		public float m_swimDepth;

		// Token: 0x04002420 RID: 9248
		public int m_areaMask = -1;
	}
}
