using System;
using System.Collections.Generic;
using UnityEngine;

// Token: 0x020001AC RID: 428
public class RandomSpawn : MonoBehaviour
{
	// Token: 0x06001901 RID: 6401 RVA: 0x000BAFCC File Offset: 0x000B91CC
	public void Randomize(Vector3 pos, Location loc = null, DungeonGenerator dg = null)
	{
		bool spawned = UnityEngine.Random.Range(0f, 100f) <= this.m_chanceToSpawn;
		if (dg != null && this.m_dungeonRequireTheme != Room.Theme.None && !dg.m_themes.HasFlag(this.m_dungeonRequireTheme))
		{
			spawned = false;
		}
		if (loc != null && this.m_requireBiome != Heightmap.Biome.None)
		{
			if (loc.m_biome == Heightmap.Biome.None)
			{
				loc.m_biome = WorldGenerator.instance.GetBiome(pos);
			}
			if (!this.m_requireBiome.HasFlag(loc.m_biome))
			{
				spawned = false;
			}
		}
		if (this.m_notInLava && ZoneSystem.instance && ZoneSystem.IsLavaPreHeightmap(pos, 0.6f))
		{
			spawned = false;
		}
		if (pos.y < (float)this.m_minElevation || pos.y > (float)this.m_maxElevation)
		{
			spawned = false;
		}
		this.SetSpawned(spawned);
	}

	// Token: 0x06001902 RID: 6402 RVA: 0x000BB0B7 File Offset: 0x000B92B7
	public void Reset()
	{
		this.SetSpawned(true);
	}

	// Token: 0x06001903 RID: 6403 RVA: 0x000BB0C0 File Offset: 0x000B92C0
	private void SetSpawned(bool doSpawn)
	{
		if (!doSpawn)
		{
			base.gameObject.SetActive(false);
			using (List<ZNetView>.Enumerator enumerator = this.m_childNetViews.GetEnumerator())
			{
				while (enumerator.MoveNext())
				{
					ZNetView znetView = enumerator.Current;
					znetView.gameObject.SetActive(false);
				}
				goto IL_62;
			}
		}
		if (this.m_nview == null)
		{
			base.gameObject.SetActive(true);
		}
		IL_62:
		if (this.m_OffObject != null)
		{
			this.m_OffObject.SetActive(!doSpawn);
		}
	}

	// Token: 0x06001904 RID: 6404 RVA: 0x000BB15C File Offset: 0x000B935C
	public void Prepare()
	{
		this.m_nview = base.GetComponent<ZNetView>();
		this.m_childNetViews = new List<ZNetView>();
		foreach (ZNetView znetView in base.gameObject.GetComponentsInChildren<ZNetView>(true))
		{
			if (Utils.IsEnabledInheirarcy(znetView.gameObject, base.gameObject))
			{
				this.m_childNetViews.Add(znetView);
			}
		}
	}

	// Token: 0x04001956 RID: 6486
	public GameObject m_OffObject;

	// Token: 0x04001957 RID: 6487
	[Range(0f, 100f)]
	public float m_chanceToSpawn = 50f;

	// Token: 0x04001958 RID: 6488
	public Room.Theme m_dungeonRequireTheme;

	// Token: 0x04001959 RID: 6489
	public Heightmap.Biome m_requireBiome;

	// Token: 0x0400195A RID: 6490
	public bool m_notInLava;

	// Token: 0x0400195B RID: 6491
	[Header("Elevation span (water is 30)")]
	public int m_minElevation = -10000;

	// Token: 0x0400195C RID: 6492
	public int m_maxElevation = 10000;

	// Token: 0x0400195D RID: 6493
	private List<ZNetView> m_childNetViews;

	// Token: 0x0400195E RID: 6494
	private ZNetView m_nview;
}
