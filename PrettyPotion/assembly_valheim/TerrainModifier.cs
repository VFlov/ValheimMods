using System;
using System.Collections.Generic;
using UnityEngine;

// Token: 0x020001C8 RID: 456
[ExecuteInEditMode]
public class TerrainModifier : MonoBehaviour
{
	// Token: 0x06001A6F RID: 6767 RVA: 0x000C4CF8 File Offset: 0x000C2EF8
	private void Awake()
	{
		TerrainModifier.s_instances.Add(this);
		TerrainModifier.s_needsSorting = true;
		this.m_nview = base.GetComponent<ZNetView>();
		this.m_wasEnabled = base.enabled;
		if (base.enabled)
		{
			if (TerrainModifier.m_triggerOnPlaced)
			{
				this.OnPlaced();
			}
			this.PokeHeightmaps(true);
		}
		this.m_creationTime = this.GetCreationTime();
	}

	// Token: 0x06001A70 RID: 6768 RVA: 0x000C4D56 File Offset: 0x000C2F56
	private void OnDestroy()
	{
		TerrainModifier.s_instances.Remove(this);
		TerrainModifier.s_needsSorting = true;
		if (this.m_wasEnabled)
		{
			this.PokeHeightmaps(false);
		}
	}

	// Token: 0x06001A71 RID: 6769 RVA: 0x000C4D79 File Offset: 0x000C2F79
	public static void RemoveAll()
	{
		TerrainModifier.s_instances.Clear();
	}

	// Token: 0x06001A72 RID: 6770 RVA: 0x000C4D88 File Offset: 0x000C2F88
	private void PokeHeightmaps(bool forcedDelay = false)
	{
		bool delayed = !TerrainModifier.m_triggerOnPlaced || forcedDelay;
		foreach (Heightmap heightmap in Heightmap.GetAllHeightmaps())
		{
			if (heightmap.TerrainVSModifier(this))
			{
				heightmap.Poke(delayed);
			}
		}
		if (ClutterSystem.instance)
		{
			ClutterSystem.instance.ResetGrass(base.transform.position, this.GetRadius());
		}
	}

	// Token: 0x06001A73 RID: 6771 RVA: 0x000C4E18 File Offset: 0x000C3018
	public float GetRadius()
	{
		float num = 0f;
		if (this.m_level && this.m_levelRadius > num)
		{
			num = this.m_levelRadius;
		}
		if (this.m_smooth && this.m_smoothRadius > num)
		{
			num = this.m_smoothRadius;
		}
		if (this.m_paintCleared && this.m_paintRadius > num)
		{
			num = this.m_paintRadius;
		}
		return num;
	}

	// Token: 0x06001A74 RID: 6772 RVA: 0x000C4E74 File Offset: 0x000C3074
	public static void SetTriggerOnPlaced(bool trigger)
	{
		TerrainModifier.m_triggerOnPlaced = trigger;
	}

	// Token: 0x06001A75 RID: 6773 RVA: 0x000C4E7C File Offset: 0x000C307C
	private void OnPlaced()
	{
		this.RemoveOthers(base.transform.position, this.GetRadius() / 4f);
		this.m_onPlacedEffect.Create(base.transform.position, Quaternion.identity, null, 1f, -1);
		if (this.m_spawnOnPlaced)
		{
			if (!this.m_spawnAtMaxLevelDepth && Heightmap.AtMaxLevelDepth(base.transform.position + Vector3.up * this.m_levelOffset))
			{
				return;
			}
			if (UnityEngine.Random.value <= this.m_chanceToSpawn)
			{
				Vector3 b = UnityEngine.Random.insideUnitCircle * 0.2f;
				GameObject gameObject = UnityEngine.Object.Instantiate<GameObject>(this.m_spawnOnPlaced, base.transform.position + Vector3.up * 0.5f + b, Quaternion.identity);
				gameObject.GetComponent<ItemDrop>().m_itemData.m_stack = UnityEngine.Random.Range(1, this.m_maxSpawned + 1);
				gameObject.GetComponent<Rigidbody>().velocity = Vector3.up * 4f;
			}
		}
	}

	// Token: 0x06001A76 RID: 6774 RVA: 0x000C4F9C File Offset: 0x000C319C
	private static void GetModifiers(Vector3 point, float range, List<TerrainModifier> modifiers, TerrainModifier ignore = null)
	{
		foreach (TerrainModifier terrainModifier in TerrainModifier.s_instances)
		{
			if (!(terrainModifier == ignore) && Utils.DistanceXZ(point, terrainModifier.transform.position) < range)
			{
				modifiers.Add(terrainModifier);
			}
		}
	}

	// Token: 0x06001A77 RID: 6775 RVA: 0x000C500C File Offset: 0x000C320C
	public static Piece FindClosestModifierPieceInRange(Vector3 point, float range)
	{
		float num = 999999f;
		TerrainModifier terrainModifier = null;
		foreach (TerrainModifier terrainModifier2 in TerrainModifier.s_instances)
		{
			if (!(terrainModifier2.m_nview == null))
			{
				float num2 = Utils.DistanceXZ(point, terrainModifier2.transform.position);
				if (num2 <= range && num2 <= num)
				{
					num = num2;
					terrainModifier = terrainModifier2;
				}
			}
		}
		if (terrainModifier)
		{
			return terrainModifier.GetComponent<Piece>();
		}
		return null;
	}

	// Token: 0x06001A78 RID: 6776 RVA: 0x000C50A0 File Offset: 0x000C32A0
	private void RemoveOthers(Vector3 point, float range)
	{
		List<TerrainModifier> list = new List<TerrainModifier>();
		TerrainModifier.GetModifiers(point, range, list, this);
		int num = 0;
		foreach (TerrainModifier terrainModifier in list)
		{
			if ((this.m_level || !terrainModifier.m_level) && (!this.m_paintCleared || this.m_paintType != TerrainModifier.PaintType.Reset || (terrainModifier.m_paintCleared && terrainModifier.m_paintType == TerrainModifier.PaintType.Reset)) && terrainModifier.m_nview && terrainModifier.m_nview.IsValid())
			{
				num++;
				terrainModifier.m_nview.ClaimOwnership();
				terrainModifier.m_nview.Destroy();
			}
		}
	}

	// Token: 0x06001A79 RID: 6777 RVA: 0x000C5160 File Offset: 0x000C3360
	private static int SortByModifiers(TerrainModifier a, TerrainModifier b)
	{
		if (a.m_playerModifiction != b.m_playerModifiction)
		{
			return a.m_playerModifiction.CompareTo(b.m_playerModifiction);
		}
		if (a.m_sortOrder != b.m_sortOrder)
		{
			return a.m_sortOrder.CompareTo(b.m_sortOrder);
		}
		if (a.m_creationTime != b.m_creationTime)
		{
			return a.m_creationTime.CompareTo(b.m_creationTime);
		}
		return a.transform.position.sqrMagnitude.CompareTo(b.transform.position.sqrMagnitude);
	}

	// Token: 0x06001A7A RID: 6778 RVA: 0x000C51FB File Offset: 0x000C33FB
	public static List<TerrainModifier> GetAllInstances()
	{
		if (TerrainModifier.s_needsSorting)
		{
			TerrainModifier.s_instances.Sort(new Comparison<TerrainModifier>(TerrainModifier.SortByModifiers));
			TerrainModifier.s_needsSorting = false;
		}
		return TerrainModifier.s_instances;
	}

	// Token: 0x06001A7B RID: 6779 RVA: 0x000C5228 File Offset: 0x000C3428
	private void OnDrawGizmosSelected()
	{
		Gizmos.matrix = Matrix4x4.TRS(base.transform.position + Vector3.up * this.m_levelOffset, Quaternion.identity, new Vector3(1f, 0f, 1f));
		if (this.m_level)
		{
			Gizmos.color = Color.green;
			Gizmos.DrawWireSphere(Vector3.zero, this.m_levelRadius);
		}
		if (this.m_smooth)
		{
			Gizmos.color = Color.blue;
			Gizmos.DrawWireSphere(Vector3.zero, this.m_smoothRadius);
		}
		if (this.m_paintCleared)
		{
			Gizmos.color = Color.yellow;
			Gizmos.DrawWireSphere(Vector3.zero, this.m_paintRadius);
		}
		Gizmos.matrix = Matrix4x4.identity;
	}

	// Token: 0x06001A7C RID: 6780 RVA: 0x000C52E8 File Offset: 0x000C34E8
	public ZDOID GetZDOID()
	{
		if (this.m_nview && this.m_nview.GetZDO() != null)
		{
			return this.m_nview.GetZDO().m_uid;
		}
		return ZDOID.None;
	}

	// Token: 0x06001A7D RID: 6781 RVA: 0x000C531C File Offset: 0x000C351C
	private long GetCreationTime()
	{
		long num = 0L;
		if (this.m_nview && this.m_nview.GetZDO() != null)
		{
			this.m_nview.GetZDO().GetPrefab();
			ZDO zdo = this.m_nview.GetZDO();
			ZDOID uid = zdo.m_uid;
			num = zdo.GetLong(ZDOVars.s_terrainModifierTimeCreated, 0L);
			if (num == 0L)
			{
				num = ZDOExtraData.GetTimeCreated(uid);
				if (num != 0L)
				{
					zdo.Set(ZDOVars.s_terrainModifierTimeCreated, num);
					Debug.LogError("CreationTime should already be set for " + this.m_nview.name + "  Prefab: " + this.m_nview.GetZDO().GetPrefab().ToString());
				}
			}
		}
		return num;
	}

	// Token: 0x04001ACB RID: 6859
	private static bool m_triggerOnPlaced = false;

	// Token: 0x04001ACC RID: 6860
	public int m_sortOrder;

	// Token: 0x04001ACD RID: 6861
	public bool m_useTerrainCompiler;

	// Token: 0x04001ACE RID: 6862
	public bool m_playerModifiction;

	// Token: 0x04001ACF RID: 6863
	public float m_levelOffset;

	// Token: 0x04001AD0 RID: 6864
	[Header("Level")]
	public bool m_level;

	// Token: 0x04001AD1 RID: 6865
	public float m_levelRadius = 2f;

	// Token: 0x04001AD2 RID: 6866
	public bool m_square = true;

	// Token: 0x04001AD3 RID: 6867
	[Header("Smooth")]
	public bool m_smooth;

	// Token: 0x04001AD4 RID: 6868
	public float m_smoothRadius = 2f;

	// Token: 0x04001AD5 RID: 6869
	public float m_smoothPower = 3f;

	// Token: 0x04001AD6 RID: 6870
	[Header("Paint")]
	public bool m_paintCleared = true;

	// Token: 0x04001AD7 RID: 6871
	public bool m_paintHeightCheck;

	// Token: 0x04001AD8 RID: 6872
	public TerrainModifier.PaintType m_paintType;

	// Token: 0x04001AD9 RID: 6873
	public float m_paintRadius = 2f;

	// Token: 0x04001ADA RID: 6874
	[Header("Effects")]
	public EffectList m_onPlacedEffect = new EffectList();

	// Token: 0x04001ADB RID: 6875
	[Header("Spawn items")]
	public GameObject m_spawnOnPlaced;

	// Token: 0x04001ADC RID: 6876
	public float m_chanceToSpawn = 1f;

	// Token: 0x04001ADD RID: 6877
	public int m_maxSpawned = 1;

	// Token: 0x04001ADE RID: 6878
	public bool m_spawnAtMaxLevelDepth = true;

	// Token: 0x04001ADF RID: 6879
	private bool m_wasEnabled;

	// Token: 0x04001AE0 RID: 6880
	private long m_creationTime;

	// Token: 0x04001AE1 RID: 6881
	private ZNetView m_nview;

	// Token: 0x04001AE2 RID: 6882
	private static readonly List<TerrainModifier> s_instances = new List<TerrainModifier>();

	// Token: 0x04001AE3 RID: 6883
	private static bool s_needsSorting = false;

	// Token: 0x04001AE4 RID: 6884
	private static bool s_delayedPokeHeightmaps = false;

	// Token: 0x04001AE5 RID: 6885
	private static int s_lastFramePoked = 0;

	// Token: 0x02000388 RID: 904
	public enum PaintType
	{
		// Token: 0x04002680 RID: 9856
		Dirt,
		// Token: 0x04002681 RID: 9857
		Cultivate,
		// Token: 0x04002682 RID: 9858
		Paved,
		// Token: 0x04002683 RID: 9859
		Reset,
		// Token: 0x04002684 RID: 9860
		ClearVegetation
	}
}
