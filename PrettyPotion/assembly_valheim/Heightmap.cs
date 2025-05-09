using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

// Token: 0x02000182 RID: 386
[ExecuteInEditMode]
public class Heightmap : MonoBehaviour, IMonoUpdater
{
	// Token: 0x06001724 RID: 5924 RVA: 0x000ABC3C File Offset: 0x000A9E3C
	private void Awake()
	{
		if (!this.m_isDistantLod)
		{
			Heightmap.s_heightmaps.Add(this);
		}
		if (Heightmap.s_shaderPropertyClearedMaskTex == 0)
		{
			Heightmap.s_shaderPropertyClearedMaskTex = Shader.PropertyToID("_ClearedMaskTex");
		}
		this.m_collider = base.GetComponent<MeshCollider>();
		this.m_meshFilter = base.GetComponent<MeshFilter>();
		if (!this.m_meshFilter)
		{
			this.m_meshFilter = base.gameObject.AddComponent<MeshFilter>();
		}
		this.m_meshRenderer = base.GetComponent<MeshRenderer>();
		if (!this.m_meshRenderer)
		{
			this.m_meshRenderer = base.gameObject.AddComponent<MeshRenderer>();
		}
		this.m_meshRenderer.motionVectorGenerationMode = MotionVectorGenerationMode.Camera;
		this.m_renderGroupSubscriber = base.GetComponent<RenderGroupSubscriber>();
		if (!this.m_renderGroupSubscriber)
		{
			this.m_renderGroupSubscriber = base.gameObject.AddComponent<RenderGroupSubscriber>();
		}
		this.m_renderGroupSubscriber.Group = RenderGroup.Overworld;
		if (this.m_material == null)
		{
			base.enabled = false;
		}
		this.UpdateShadowSettings();
	}

	// Token: 0x06001725 RID: 5925 RVA: 0x000ABD30 File Offset: 0x000A9F30
	private void OnDestroy()
	{
		if (!this.m_isDistantLod)
		{
			Heightmap.s_heightmaps.Remove(this);
		}
		if (this.m_materialInstance)
		{
			UnityEngine.Object.DestroyImmediate(this.m_materialInstance);
		}
		if (this.m_collisionMesh)
		{
			UnityEngine.Object.DestroyImmediate(this.m_collisionMesh);
		}
		if (this.m_renderMesh)
		{
			UnityEngine.Object.DestroyImmediate(this.m_renderMesh);
		}
		if (this.m_paintMask)
		{
			UnityEngine.Object.DestroyImmediate(this.m_paintMask);
		}
	}

	// Token: 0x06001726 RID: 5926 RVA: 0x000ABDB1 File Offset: 0x000A9FB1
	private void OnEnable()
	{
		Heightmap.Instances.Add(this);
		this.UpdateShadowSettings();
		if (this.m_isDistantLod && Application.isPlaying && !this.m_distantLodEditorHax)
		{
			return;
		}
		this.Regenerate();
	}

	// Token: 0x06001727 RID: 5927 RVA: 0x000ABDE2 File Offset: 0x000A9FE2
	private void OnDisable()
	{
		Heightmap.Instances.Remove(this);
	}

	// Token: 0x06001728 RID: 5928 RVA: 0x000ABDF0 File Offset: 0x000A9FF0
	public void CustomLateUpdate(float deltaTime)
	{
		if (!this.m_doLateUpdate)
		{
			return;
		}
		this.m_doLateUpdate = false;
		this.Regenerate();
	}

	// Token: 0x06001729 RID: 5929 RVA: 0x000ABE08 File Offset: 0x000AA008
	private void UpdateShadowSettings()
	{
		if (this.m_isDistantLod)
		{
			this.m_meshRenderer.shadowCastingMode = (Heightmap.EnableDistantTerrainShadows ? ShadowCastingMode.On : ShadowCastingMode.Off);
			this.m_meshRenderer.receiveShadows = false;
			return;
		}
		this.m_meshRenderer.shadowCastingMode = (Heightmap.EnableDistantTerrainShadows ? ShadowCastingMode.On : ShadowCastingMode.TwoSided);
		this.m_meshRenderer.receiveShadows = true;
	}

	// Token: 0x0600172A RID: 5930 RVA: 0x000ABE64 File Offset: 0x000AA064
	public static void ForceGenerateAll()
	{
		foreach (Heightmap heightmap in Heightmap.s_heightmaps)
		{
			if (heightmap.HaveQueuedRebuild())
			{
				ZLog.Log("Force generating hmap " + heightmap.transform.position.ToString());
				heightmap.Regenerate();
			}
		}
	}

	// Token: 0x0600172B RID: 5931 RVA: 0x000ABEE8 File Offset: 0x000AA0E8
	public void Poke(bool delayed)
	{
		if (delayed)
		{
			this.m_doLateUpdate = true;
			return;
		}
		this.Regenerate();
	}

	// Token: 0x0600172C RID: 5932 RVA: 0x000ABEFB File Offset: 0x000AA0FB
	public bool HaveQueuedRebuild()
	{
		return this.m_doLateUpdate;
	}

	// Token: 0x0600172D RID: 5933 RVA: 0x000ABF04 File Offset: 0x000AA104
	public void Regenerate()
	{
		this.m_doLateUpdate = false;
		if (!this.Generate())
		{
			return;
		}
		this.RebuildCollisionMesh();
		this.UpdateCornerDepths();
		this.m_materialInstance.SetTexture(Heightmap.s_shaderPropertyClearedMaskTex, this.m_paintMask);
		this.RebuildRenderMesh();
		Action clearConnectedWearNTearCache = this.m_clearConnectedWearNTearCache;
		if (clearConnectedWearNTearCache == null)
		{
			return;
		}
		clearConnectedWearNTearCache();
	}

	// Token: 0x0600172E RID: 5934 RVA: 0x000ABF5C File Offset: 0x000AA15C
	private void UpdateCornerDepths()
	{
		float num = 30f;
		this.m_oceanDepth[0] = this.GetHeight(0, this.m_width);
		this.m_oceanDepth[1] = this.GetHeight(this.m_width, this.m_width);
		this.m_oceanDepth[2] = this.GetHeight(this.m_width, 0);
		this.m_oceanDepth[3] = this.GetHeight(0, 0);
		this.m_oceanDepth[0] = Mathf.Max(0f, (float)((double)num - (double)this.m_oceanDepth[0]));
		this.m_oceanDepth[1] = Mathf.Max(0f, (float)((double)num - (double)this.m_oceanDepth[1]));
		this.m_oceanDepth[2] = Mathf.Max(0f, (float)((double)num - (double)this.m_oceanDepth[2]));
		this.m_oceanDepth[3] = Mathf.Max(0f, (float)((double)num - (double)this.m_oceanDepth[3]));
		this.m_materialInstance.SetFloatArray("_depth", this.m_oceanDepth);
	}

	// Token: 0x0600172F RID: 5935 RVA: 0x000AC055 File Offset: 0x000AA255
	public float[] GetOceanDepth()
	{
		return this.m_oceanDepth;
	}

	// Token: 0x06001730 RID: 5936 RVA: 0x000AC060 File Offset: 0x000AA260
	public float GetOceanDepth(Vector3 worldPos)
	{
		int num;
		int num2;
		this.WorldToVertex(worldPos, out num, out num2);
		float t = (float)((double)num / (double)((float)this.m_width));
		float t2 = (float)num2 / (float)this.m_width;
		float a = DUtils.Lerp(this.m_oceanDepth[3], this.m_oceanDepth[2], t);
		float b = DUtils.Lerp(this.m_oceanDepth[0], this.m_oceanDepth[1], t);
		return DUtils.Lerp(a, b, t2);
	}

	// Token: 0x06001731 RID: 5937 RVA: 0x000AC0C8 File Offset: 0x000AA2C8
	private void Initialize()
	{
		int num = this.m_width + 1;
		int num2 = num * num;
		if (this.m_heights.Count == num2)
		{
			return;
		}
		this.m_heights.Clear();
		for (int i = 0; i < num2; i++)
		{
			this.m_heights.Add(0f);
		}
		this.m_paintMask = new Texture2D(num, num);
		this.m_paintMask.name = "_Heightmap m_paintMask";
		this.m_paintMask.wrapMode = TextureWrapMode.Clamp;
		this.m_materialInstance = new Material(this.m_material);
		this.m_materialInstance.SetTexture(Heightmap.s_shaderPropertyClearedMaskTex, this.m_paintMask);
		this.m_meshRenderer.sharedMaterial = this.m_materialInstance;
	}

	// Token: 0x06001732 RID: 5938 RVA: 0x000AC17C File Offset: 0x000AA37C
	private bool Generate()
	{
		if (HeightmapBuilder.instance == null)
		{
			return false;
		}
		if (WorldGenerator.instance == null)
		{
			ZLog.LogError("The WorldGenerator instance was null");
			throw new NullReferenceException("The WorldGenerator instance was null");
		}
		this.Initialize();
		int num = this.m_width + 1;
		int num2 = num * num;
		Vector3 position = base.transform.position;
		if (this.m_buildData == null || this.m_buildData.m_baseHeights.Count != num2 || this.m_buildData.m_center != position || this.m_buildData.m_scale != this.m_scale || this.m_buildData.m_worldGen != WorldGenerator.instance)
		{
			this.m_buildData = HeightmapBuilder.instance.RequestTerrainSync(position, this.m_width, this.m_scale, this.m_isDistantLod, WorldGenerator.instance);
			this.m_cornerBiomes = this.m_buildData.m_cornerBiomes;
		}
		for (int i = 0; i < num2; i++)
		{
			this.m_heights[i] = this.m_buildData.m_baseHeights[i];
		}
		this.m_paintMask.SetPixels(this.m_buildData.m_baseMask);
		this.ApplyModifiers();
		return true;
	}

	// Token: 0x06001733 RID: 5939 RVA: 0x000AC2A0 File Offset: 0x000AA4A0
	private static float Distance(float x, float y, float rx, float ry)
	{
		float num = (float)((double)x - (double)rx);
		float num2 = (float)((double)y - (double)ry);
		float num3 = Mathf.Sqrt((float)((double)num * (double)num + (double)num2 * (double)num2));
		float num4 = (float)(1.4140000343322754 - (double)num3);
		return (float)((double)num4 * (double)num4 * (double)num4);
	}

	// Token: 0x06001734 RID: 5940 RVA: 0x000AC2E4 File Offset: 0x000AA4E4
	public bool HaveBiome(Heightmap.Biome biome)
	{
		return (this.m_cornerBiomes[0] & biome) != Heightmap.Biome.None || (this.m_cornerBiomes[1] & biome) != Heightmap.Biome.None || (this.m_cornerBiomes[2] & biome) != Heightmap.Biome.None || (this.m_cornerBiomes[3] & biome) > Heightmap.Biome.None;
	}

	// Token: 0x06001735 RID: 5941 RVA: 0x000AC31C File Offset: 0x000AA51C
	public Heightmap.Biome GetBiome(Vector3 point, float oceanLevel = 0.02f, bool waterAlwaysOcean = false)
	{
		if (this.m_isDistantLod || waterAlwaysOcean)
		{
			return WorldGenerator.instance.GetBiome(point.x, point.z, oceanLevel, waterAlwaysOcean);
		}
		if (this.m_cornerBiomes[0] == this.m_cornerBiomes[1] && this.m_cornerBiomes[0] == this.m_cornerBiomes[2] && this.m_cornerBiomes[0] == this.m_cornerBiomes[3])
		{
			return this.m_cornerBiomes[0];
		}
		float x = point.x;
		float z = point.z;
		this.WorldToNormalizedHM(point, out x, out z);
		for (int i = 1; i < Heightmap.s_tempBiomeWeights.Length; i++)
		{
			Heightmap.s_tempBiomeWeights[i] = 0f;
		}
		Heightmap.s_tempBiomeWeights[Heightmap.s_biomeToIndex[this.m_cornerBiomes[0]]] += Heightmap.Distance(x, z, 0f, 0f);
		Heightmap.s_tempBiomeWeights[Heightmap.s_biomeToIndex[this.m_cornerBiomes[1]]] += Heightmap.Distance(x, z, 1f, 0f);
		Heightmap.s_tempBiomeWeights[Heightmap.s_biomeToIndex[this.m_cornerBiomes[2]]] += Heightmap.Distance(x, z, 0f, 1f);
		Heightmap.s_tempBiomeWeights[Heightmap.s_biomeToIndex[this.m_cornerBiomes[3]]] += Heightmap.Distance(x, z, 1f, 1f);
		int num = Heightmap.s_biomeToIndex[Heightmap.Biome.None];
		float num2 = -99999f;
		for (int j = 1; j < Heightmap.s_tempBiomeWeights.Length; j++)
		{
			if (Heightmap.s_tempBiomeWeights[j] > num2)
			{
				num = j;
				num2 = Heightmap.s_tempBiomeWeights[j];
			}
		}
		return Heightmap.s_indexToBiome[num];
	}

	// Token: 0x06001736 RID: 5942 RVA: 0x000AC4D1 File Offset: 0x000AA6D1
	public Heightmap.BiomeArea GetBiomeArea()
	{
		if (!this.IsBiomeEdge())
		{
			return Heightmap.BiomeArea.Median;
		}
		return Heightmap.BiomeArea.Edge;
	}

	// Token: 0x06001737 RID: 5943 RVA: 0x000AC4DE File Offset: 0x000AA6DE
	public bool IsBiomeEdge()
	{
		return this.m_cornerBiomes[0] != this.m_cornerBiomes[1] || this.m_cornerBiomes[0] != this.m_cornerBiomes[2] || this.m_cornerBiomes[0] != this.m_cornerBiomes[3];
	}

	// Token: 0x06001738 RID: 5944 RVA: 0x000AC51C File Offset: 0x000AA71C
	private void ApplyModifiers()
	{
		List<TerrainModifier> allInstances = TerrainModifier.GetAllInstances();
		float[] array = null;
		float[] array2 = null;
		foreach (TerrainModifier terrainModifier in allInstances)
		{
			if (terrainModifier.enabled && this.TerrainVSModifier(terrainModifier))
			{
				if (terrainModifier.m_playerModifiction && array == null)
				{
					array = this.m_heights.ToArray();
					array2 = this.m_heights.ToArray();
				}
				this.ApplyModifier(terrainModifier, array, array2);
			}
		}
		TerrainComp terrainComp = this.m_isDistantLod ? null : TerrainComp.FindTerrainCompiler(base.transform.position);
		if (terrainComp)
		{
			if (array == null)
			{
				array = this.m_heights.ToArray();
				array2 = this.m_heights.ToArray();
			}
			terrainComp.ApplyToHeightmap(this.m_paintMask, this.m_heights, array, array2, this);
		}
		this.m_paintMask.Apply();
	}

	// Token: 0x06001739 RID: 5945 RVA: 0x000AC60C File Offset: 0x000AA80C
	private void ApplyModifier(TerrainModifier modifier, float[] baseHeights, float[] levelOnly)
	{
		if (modifier.m_level)
		{
			this.LevelTerrain(modifier.transform.position + Vector3.up * modifier.m_levelOffset, modifier.m_levelRadius, modifier.m_square, baseHeights, levelOnly, modifier.m_playerModifiction);
		}
		if (modifier.m_smooth)
		{
			this.SmoothTerrain2(modifier.transform.position + Vector3.up * modifier.m_levelOffset, modifier.m_smoothRadius, modifier.m_square, levelOnly, modifier.m_smoothPower, modifier.m_playerModifiction);
		}
		if (modifier.m_paintCleared)
		{
			this.PaintCleared(modifier.transform.position, modifier.m_paintRadius, modifier.m_paintType, modifier.m_paintHeightCheck, false);
		}
	}

	// Token: 0x0600173A RID: 5946 RVA: 0x000AC6D0 File Offset: 0x000AA8D0
	public bool CheckTerrainModIsContained(TerrainModifier modifier)
	{
		Vector3 position = modifier.transform.position;
		float num = modifier.GetRadius() + 0.1f;
		Vector3 position2 = base.transform.position;
		float num2 = (float)this.m_width * this.m_scale * 0.5f;
		return position.x + num <= position2.x + num2 && position.x - num >= position2.x - num2 && position.z + num <= position2.z + num2 && position.z - num >= position2.z - num2;
	}

	// Token: 0x0600173B RID: 5947 RVA: 0x000AC768 File Offset: 0x000AA968
	public bool TerrainVSModifier(TerrainModifier modifier)
	{
		Vector3 position = modifier.transform.position;
		float num = modifier.GetRadius() + 4f;
		Vector3 position2 = base.transform.position;
		float num2 = (float)this.m_width * this.m_scale * 0.5f;
		return position.x + num >= position2.x - num2 && position.x - num <= position2.x + num2 && position.z + num >= position2.z - num2 && position.z - num <= position2.z + num2;
	}

	// Token: 0x0600173C RID: 5948 RVA: 0x000AC800 File Offset: 0x000AAA00
	private Vector3 CalcVertex(int x, int y)
	{
		int num = this.m_width + 1;
		Vector3 a = new Vector3((float)((double)this.m_width * (double)this.m_scale * -0.5), 0f, (float)((double)this.m_width * (double)this.m_scale * -0.5));
		float y2 = this.m_heights[y * num + x];
		return a + new Vector3((float)((double)x * (double)this.m_scale), y2, (float)((double)y * (double)this.m_scale));
	}

	// Token: 0x0600173D RID: 5949 RVA: 0x000AC888 File Offset: 0x000AAA88
	private Color GetBiomeColor(float ix, float iy)
	{
		if (this.m_cornerBiomes[0] == this.m_cornerBiomes[1] && this.m_cornerBiomes[0] == this.m_cornerBiomes[2] && this.m_cornerBiomes[0] == this.m_cornerBiomes[3])
		{
			return Heightmap.GetBiomeColor(this.m_cornerBiomes[0]);
		}
		Color32 biomeColor = Heightmap.GetBiomeColor(this.m_cornerBiomes[0]);
		Color32 biomeColor2 = Heightmap.GetBiomeColor(this.m_cornerBiomes[1]);
		Color32 biomeColor3 = Heightmap.GetBiomeColor(this.m_cornerBiomes[2]);
		Color32 biomeColor4 = Heightmap.GetBiomeColor(this.m_cornerBiomes[3]);
		Color32 a = Color32.Lerp(biomeColor, biomeColor2, ix);
		Color32 b = Color32.Lerp(biomeColor3, biomeColor4, ix);
		return Color32.Lerp(a, b, iy);
	}

	// Token: 0x0600173E RID: 5950 RVA: 0x000AC934 File Offset: 0x000AAB34
	public static Color32 GetBiomeColor(Heightmap.Biome biome)
	{
		if (biome <= Heightmap.Biome.Plains)
		{
			switch (biome)
			{
			case Heightmap.Biome.Meadows:
			case Heightmap.Biome.Meadows | Heightmap.Biome.Swamp:
				break;
			case Heightmap.Biome.Swamp:
				return new Color32(byte.MaxValue, 0, 0, 0);
			case Heightmap.Biome.Mountain:
				return new Color32(0, byte.MaxValue, 0, 0);
			default:
				if (biome == Heightmap.Biome.BlackForest)
				{
					return new Color32(0, 0, byte.MaxValue, 0);
				}
				if (biome == Heightmap.Biome.Plains)
				{
					return new Color32(0, 0, 0, byte.MaxValue);
				}
				break;
			}
		}
		else
		{
			if (biome == Heightmap.Biome.AshLands)
			{
				return new Color32(byte.MaxValue, 0, 0, byte.MaxValue);
			}
			if (biome == Heightmap.Biome.DeepNorth)
			{
				return new Color32(0, byte.MaxValue, 0, 0);
			}
			if (biome == Heightmap.Biome.Mistlands)
			{
				return new Color32(0, 0, byte.MaxValue, byte.MaxValue);
			}
		}
		return new Color32(0, 0, 0, 0);
	}

	// Token: 0x0600173F RID: 5951 RVA: 0x000AC9F0 File Offset: 0x000AABF0
	private void RebuildCollisionMesh()
	{
		if (this.m_collisionMesh == null)
		{
			this.m_collisionMesh = new Mesh();
			this.m_collisionMesh.name = "___Heightmap m_collisionMesh";
		}
		int num = this.m_width + 1;
		float num2 = -999999f;
		float num3 = 999999f;
		Heightmap.s_tempVertices.Clear();
		for (int i = 0; i < num; i++)
		{
			for (int j = 0; j < num; j++)
			{
				Vector3 vector = this.CalcVertex(j, i);
				Heightmap.s_tempVertices.Add(vector);
				if (vector.y > num2)
				{
					num2 = vector.y;
				}
				if (vector.y < num3)
				{
					num3 = vector.y;
				}
			}
		}
		this.m_collisionMesh.SetVertices(Heightmap.s_tempVertices);
		int num4 = (num - 1) * (num - 1) * 6;
		if ((ulong)this.m_collisionMesh.GetIndexCount(0) != (ulong)((long)num4))
		{
			Heightmap.s_tempIndices.Clear();
			for (int k = 0; k < num - 1; k++)
			{
				for (int l = 0; l < num - 1; l++)
				{
					int item = k * num + l;
					int item2 = k * num + l + 1;
					int item3 = (k + 1) * num + l + 1;
					int item4 = (k + 1) * num + l;
					Heightmap.s_tempIndices.Add(item);
					Heightmap.s_tempIndices.Add(item4);
					Heightmap.s_tempIndices.Add(item2);
					Heightmap.s_tempIndices.Add(item2);
					Heightmap.s_tempIndices.Add(item4);
					Heightmap.s_tempIndices.Add(item3);
				}
			}
			this.m_collisionMesh.SetIndices(Heightmap.s_tempIndices.ToArray(), MeshTopology.Triangles, 0);
		}
		if (this.m_collider)
		{
			this.m_collider.sharedMesh = this.m_collisionMesh;
		}
		float num5 = (float)this.m_width * this.m_scale * 0.5f;
		this.m_bounds.SetMinMax(base.transform.position + new Vector3(-num5, num3, -num5), base.transform.position + new Vector3(num5, num2, num5));
		this.m_boundingSphere.position = this.m_bounds.center;
		this.m_boundingSphere.radius = Vector3.Distance(this.m_boundingSphere.position, this.m_bounds.max);
	}

	// Token: 0x06001740 RID: 5952 RVA: 0x000ACC44 File Offset: 0x000AAE44
	private void RebuildRenderMesh()
	{
		if (this.m_renderMesh == null)
		{
			this.m_renderMesh = new Mesh();
			this.m_renderMesh.name = "___Heightmap m_renderMesh";
		}
		WorldGenerator instance = WorldGenerator.instance;
		int num = this.m_width + 1;
		Vector3 vector = base.transform.position + new Vector3((float)((double)this.m_width * (double)this.m_scale * -0.5), 0f, (float)((double)this.m_width * (double)this.m_scale * -0.5));
		Heightmap.s_tempVertices.Clear();
		Heightmap.s_tempUVs.Clear();
		Heightmap.s_tempIndices.Clear();
		Heightmap.s_tempColors.Clear();
		for (int i = 0; i < num; i++)
		{
			float iy = DUtils.SmoothStep(0f, 1f, (float)((double)i / (double)this.m_width));
			for (int j = 0; j < num; j++)
			{
				float ix = DUtils.SmoothStep(0f, 1f, (float)((double)j / (double)this.m_width));
				Heightmap.s_tempUVs.Add(new Vector2((float)((double)j / (double)this.m_width), (float)((double)i / (double)this.m_width)));
				if (this.m_isDistantLod)
				{
					float wx = (float)((double)vector.x + (double)j * (double)this.m_scale);
					float wy = (float)((double)vector.z + (double)i * (double)this.m_scale);
					Heightmap.Biome biome = instance.GetBiome(wx, wy, 0.02f, false);
					Heightmap.s_tempColors.Add(Heightmap.GetBiomeColor(biome));
				}
				else
				{
					Heightmap.s_tempColors.Add(this.GetBiomeColor(ix, iy));
				}
			}
		}
		this.m_collisionMesh.GetVertices(Heightmap.s_tempVertices);
		this.m_collisionMesh.GetIndices(Heightmap.s_tempIndices, 0);
		this.m_renderMesh.Clear();
		this.m_renderMesh.SetVertices(Heightmap.s_tempVertices);
		this.m_renderMesh.SetColors(Heightmap.s_tempColors);
		this.m_renderMesh.SetUVs(0, Heightmap.s_tempUVs);
		this.m_renderMesh.SetIndices(Heightmap.s_tempIndices, MeshTopology.Triangles, 0, true, 0);
		this.m_renderMesh.RecalculateNormals();
		this.m_renderMesh.RecalculateTangents();
		this.m_renderMesh.RecalculateBounds();
		this.m_meshFilter.mesh = this.m_renderMesh;
	}

	// Token: 0x06001741 RID: 5953 RVA: 0x000ACE9C File Offset: 0x000AB09C
	private void SmoothTerrain2(Vector3 worldPos, float radius, bool square, float[] levelOnlyHeights, float power, bool playerModifiction)
	{
		int num;
		int num2;
		this.WorldToVertex(worldPos, out num, out num2);
		float b = (float)((double)(worldPos.y - base.transform.position.y));
		float num3 = (float)((double)(radius / this.m_scale));
		int num4 = Mathf.CeilToInt(num3);
		Vector2 a = new Vector2((float)num, (float)num2);
		int num5 = this.m_width + 1;
		for (int i = num2 - num4; i <= num2 + num4; i++)
		{
			for (int j = num - num4; j <= num + num4; j++)
			{
				float num6 = Vector2.Distance(a, new Vector2((float)j, (float)i));
				if (num6 <= num3)
				{
					float num7 = num6 / num3;
					if (j >= 0 && i >= 0 && j < num5 && i < num5)
					{
						if (power == 3f)
						{
							num7 = (float)((double)num7 * (double)num7 * (double)num7);
						}
						else
						{
							num7 = Mathf.Pow(num7, power);
						}
						float height = this.GetHeight(j, i);
						float t = (float)(1.0 - (double)num7);
						float num8 = DUtils.Lerp(height, b, t);
						if (playerModifiction)
						{
							float num9 = levelOnlyHeights[i * num5 + j];
							num8 = Mathf.Clamp(num8, (float)((double)num9 - 1.0), (float)((double)num9 + 1.0));
						}
						this.SetHeight(j, i, num8);
					}
				}
			}
		}
	}

	// Token: 0x06001742 RID: 5954 RVA: 0x000ACFFC File Offset: 0x000AB1FC
	private bool AtMaxWorldLevelDepth(Vector3 worldPos)
	{
		float num;
		this.GetWorldHeight(worldPos, out num);
		float num2;
		this.GetWorldBaseHeight(worldPos, out num2);
		return Mathf.Max(-(float)((double)num - (double)num2), 0f) >= 7.95f;
	}

	// Token: 0x06001743 RID: 5955 RVA: 0x000AD038 File Offset: 0x000AB238
	private bool GetWorldBaseHeight(Vector3 worldPos, out float height)
	{
		int num;
		int num2;
		this.WorldToVertex(worldPos, out num, out num2);
		int num3 = this.m_width + 1;
		if (num < 0 || num2 < 0 || num >= num3 || num2 >= num3)
		{
			height = 0f;
			return false;
		}
		height = (float)((double)this.m_buildData.m_baseHeights[num2 * num3 + num] + (double)base.transform.position.y);
		return true;
	}

	// Token: 0x06001744 RID: 5956 RVA: 0x000AD0A0 File Offset: 0x000AB2A0
	private bool GetWorldHeight(Vector3 worldPos, out float height)
	{
		int num;
		int num2;
		this.WorldToVertex(worldPos, out num, out num2);
		int num3 = this.m_width + 1;
		if (num < 0 || num2 < 0 || num >= num3 || num2 >= num3)
		{
			height = 0f;
			return false;
		}
		height = (float)((double)this.m_heights[num2 * num3 + num] + (double)base.transform.position.y);
		return true;
	}

	// Token: 0x06001745 RID: 5957 RVA: 0x000AD104 File Offset: 0x000AB304
	public static bool AtMaxLevelDepth(Vector3 worldPos)
	{
		Heightmap heightmap = Heightmap.FindHeightmap(worldPos);
		return heightmap && heightmap.AtMaxWorldLevelDepth(worldPos);
	}

	// Token: 0x06001746 RID: 5958 RVA: 0x000AD12C File Offset: 0x000AB32C
	public static bool GetHeight(Vector3 worldPos, out float height)
	{
		Heightmap heightmap = Heightmap.FindHeightmap(worldPos);
		if (heightmap && heightmap.GetWorldHeight(worldPos, out height))
		{
			return true;
		}
		height = 0f;
		return false;
	}

	// Token: 0x06001747 RID: 5959 RVA: 0x000AD15C File Offset: 0x000AB35C
	private void PaintCleared(Vector3 worldPos, float radius, TerrainModifier.PaintType paintType, bool heightCheck, bool apply)
	{
		worldPos.x -= 0.5f;
		worldPos.z -= 0.5f;
		float num = worldPos.y - base.transform.position.y;
		int num2;
		int num3;
		this.WorldToVertexMask(worldPos, out num2, out num3);
		float num4 = radius / this.m_scale;
		int num5 = Mathf.CeilToInt(num4);
		Vector2 a = new Vector2((float)num2, (float)num3);
		for (int i = num3 - num5; i <= num3 + num5; i++)
		{
			for (int j = num2 - num5; j <= num2 + num5; j++)
			{
				if (j >= 0 && i >= 0 && j < this.m_paintMask.width + 1 && i < this.m_paintMask.height + 1 && (!heightCheck || this.GetHeight(j, i) <= num))
				{
					float num6 = Vector2.Distance(a, new Vector2((float)j, (float)i));
					float num7 = 1f - Mathf.Clamp01(num6 / num4);
					num7 = Mathf.Pow(num7, 0.1f);
					Color color = this.m_paintMask.GetPixel(j, i);
					float a2 = color.a;
					switch (paintType)
					{
					case TerrainModifier.PaintType.Dirt:
						color = Color.Lerp(color, Heightmap.m_paintMaskDirt, num7);
						break;
					case TerrainModifier.PaintType.Cultivate:
						color = Color.Lerp(color, Heightmap.m_paintMaskCultivated, num7);
						break;
					case TerrainModifier.PaintType.Paved:
						color = Color.Lerp(color, Heightmap.m_paintMaskPaved, num7);
						break;
					case TerrainModifier.PaintType.Reset:
						color = Color.Lerp(color, Heightmap.m_paintMaskNothing, num7);
						break;
					case TerrainModifier.PaintType.ClearVegetation:
						color = Color.Lerp(color, Heightmap.m_paintMaskClearVegetation, num7);
						break;
					}
					if (paintType != TerrainModifier.PaintType.ClearVegetation)
					{
						color.a = a2;
					}
					this.m_paintMask.SetPixel(j, i, color);
				}
			}
		}
		if (apply)
		{
			this.m_paintMask.Apply();
		}
	}

	// Token: 0x06001748 RID: 5960 RVA: 0x000AD33C File Offset: 0x000AB53C
	public float GetVegetationMask(Vector3 worldPos)
	{
		worldPos.x -= 0.5f;
		worldPos.z -= 0.5f;
		int x;
		int y;
		this.WorldToVertexMask(worldPos, out x, out y);
		return this.m_paintMask.GetPixel(x, y).a;
	}

	// Token: 0x06001749 RID: 5961 RVA: 0x000AD388 File Offset: 0x000AB588
	public bool IsCleared(Vector3 worldPos)
	{
		worldPos.x = (float)((double)worldPos.x - 0.5);
		worldPos.z = (float)((double)worldPos.z - 0.5);
		int x;
		int y;
		this.WorldToVertexMask(worldPos, out x, out y);
		Color pixel = this.m_paintMask.GetPixel(x, y);
		return pixel.r > 0.5f || pixel.g > 0.5f || pixel.b > 0.5f;
	}

	// Token: 0x0600174A RID: 5962 RVA: 0x000AD40C File Offset: 0x000AB60C
	public bool IsCultivated(Vector3 worldPos)
	{
		int x;
		int y;
		this.WorldToVertexMask(worldPos, out x, out y);
		return this.m_paintMask.GetPixel(x, y).g > 0.5f;
	}

	// Token: 0x0600174B RID: 5963 RVA: 0x000AD43D File Offset: 0x000AB63D
	public bool IsLava(Vector3 worldPos, float lavaValue = 0.6f)
	{
		return this.GetBiome(worldPos, 0.02f, false) == Heightmap.Biome.AshLands && !this.IsBiomeEdge() && this.GetVegetationMask(worldPos) > lavaValue;
	}

	// Token: 0x0600174C RID: 5964 RVA: 0x000AD467 File Offset: 0x000AB667
	public float GetLava(Vector3 worldPos)
	{
		if (this.GetBiome(worldPos, 0.02f, false) != Heightmap.Biome.AshLands || this.IsBiomeEdge())
		{
			return 0f;
		}
		return this.GetVegetationMask(worldPos);
	}

	// Token: 0x0600174D RID: 5965 RVA: 0x000AD490 File Offset: 0x000AB690
	public float GetHeightOffset(Vector3 worldPos)
	{
		if (this.GetBiome(worldPos, 0.02f, false) != Heightmap.Biome.AshLands)
		{
			return 0f;
		}
		if (this.IsBiomeEdge())
		{
			return Heightmap.GetGroundMaterialOffset(FootStep.GroundMaterial.Ashlands);
		}
		float vegetationMask = this.GetVegetationMask(worldPos);
		return Mathf.Lerp(Heightmap.GetGroundMaterialOffset(FootStep.GroundMaterial.Ashlands), Heightmap.GetGroundMaterialOffset(FootStep.GroundMaterial.Lava), vegetationMask);
	}

	// Token: 0x0600174E RID: 5966 RVA: 0x000AD4EC File Offset: 0x000AB6EC
	public void WorldToVertex(Vector3 worldPos, out int x, out int y)
	{
		Vector3 vector = worldPos - base.transform.position;
		int num = this.m_width / 2;
		x = Mathf.FloorToInt(vector.x / this.m_scale + 0.5f) + num;
		y = Mathf.FloorToInt(vector.z / this.m_scale + 0.5f) + num;
	}

	// Token: 0x0600174F RID: 5967 RVA: 0x000AD54C File Offset: 0x000AB74C
	public void WorldToVertexMask(Vector3 worldPos, out int x, out int y)
	{
		Vector3 vector = worldPos - base.transform.position;
		int num = (this.m_width + 1) / 2;
		x = Mathf.FloorToInt(vector.x / this.m_scale + 0.5f) + num;
		y = Mathf.FloorToInt(vector.z / this.m_scale + 0.5f) + num;
	}

	// Token: 0x06001750 RID: 5968 RVA: 0x000AD5B0 File Offset: 0x000AB7B0
	private void WorldToNormalizedHM(Vector3 worldPos, out float x, out float y)
	{
		float num = (float)this.m_width * this.m_scale;
		Vector3 vector = worldPos - base.transform.position;
		x = vector.x / num + 0.5f;
		y = vector.z / num + 0.5f;
	}

	// Token: 0x06001751 RID: 5969 RVA: 0x000AD600 File Offset: 0x000AB800
	private void LevelTerrain(Vector3 worldPos, float radius, bool square, float[] baseHeights, float[] levelOnly, bool playerModifiction)
	{
		int num;
		int num2;
		this.WorldToVertexMask(worldPos, out num, out num2);
		Vector3 vector = worldPos - base.transform.position;
		float num3 = (float)((double)radius / (double)this.m_scale);
		int num4 = Mathf.CeilToInt(num3);
		int num5 = this.m_width + 1;
		Vector2 a = new Vector2((float)num, (float)num2);
		for (int i = num2 - num4; i <= num2 + num4; i++)
		{
			for (int j = num - num4; j <= num + num4; j++)
			{
				if ((square || Vector2.Distance(a, new Vector2((float)j, (float)i)) <= num3) && j >= 0 && i >= 0 && j < num5 && i < num5)
				{
					float num6 = vector.y;
					if (playerModifiction)
					{
						float num7 = baseHeights[i * num5 + j];
						num6 = Mathf.Clamp(num6, (float)((double)num7 - 8.0), (float)((double)num7 + 8.0));
						levelOnly[i * num5 + j] = num6;
					}
					this.SetHeight(j, i, num6);
				}
			}
		}
	}

	// Token: 0x06001752 RID: 5970 RVA: 0x000AD710 File Offset: 0x000AB910
	public Color GetPaintMask(int x, int y)
	{
		if (x < 0 || y < 0 || x >= this.m_paintMask.width || y >= this.m_paintMask.height)
		{
			return Color.black;
		}
		return this.m_paintMask.GetPixel(x, y);
	}

	// Token: 0x06001753 RID: 5971 RVA: 0x000AD749 File Offset: 0x000AB949
	public Texture2D GetPaintMask()
	{
		return this.m_paintMask;
	}

	// Token: 0x06001754 RID: 5972 RVA: 0x000AD751 File Offset: 0x000AB951
	private void SetPaintMask(int x, int y, Color paint)
	{
		if (x < 0 || y < 0 || x >= this.m_width || y >= this.m_width)
		{
			return;
		}
		this.m_paintMask.SetPixel(x, y, paint);
	}

	// Token: 0x06001755 RID: 5973 RVA: 0x000AD77C File Offset: 0x000AB97C
	public float GetHeight(int x, int y)
	{
		int num = this.m_width + 1;
		if (x < 0 || y < 0 || x >= num || y >= num)
		{
			return 0f;
		}
		return this.m_heights[y * num + x];
	}

	// Token: 0x06001756 RID: 5974 RVA: 0x000AD7B8 File Offset: 0x000AB9B8
	public void SetHeight(int x, int y, float h)
	{
		int num = this.m_width + 1;
		if (x < 0 || y < 0 || x >= num || y >= num)
		{
			return;
		}
		this.m_heights[y * num + x] = h;
	}

	// Token: 0x06001757 RID: 5975 RVA: 0x000AD7F0 File Offset: 0x000AB9F0
	public bool IsPointInside(Vector3 point, float radius = 0f)
	{
		float num = (float)((double)this.m_width * (double)this.m_scale * 0.5);
		Vector3 position = base.transform.position;
		return (float)((double)point.x + (double)radius) >= (float)((double)position.x - (double)num) && (float)((double)point.x - (double)radius) <= (float)((double)position.x + (double)num) && (float)((double)point.z + (double)radius) >= (float)((double)position.z - (double)num) && (float)((double)point.z - (double)radius) <= (float)((double)position.z + (double)num);
	}

	// Token: 0x06001758 RID: 5976 RVA: 0x000AD887 File Offset: 0x000ABA87
	public static List<Heightmap> GetAllHeightmaps()
	{
		return Heightmap.s_heightmaps;
	}

	// Token: 0x06001759 RID: 5977 RVA: 0x000AD890 File Offset: 0x000ABA90
	public static Heightmap FindHeightmap(Vector3 point)
	{
		foreach (Heightmap heightmap in Heightmap.s_heightmaps)
		{
			if (heightmap.IsPointInside(point, 0f))
			{
				return heightmap;
			}
		}
		return null;
	}

	// Token: 0x0600175A RID: 5978 RVA: 0x000AD8F0 File Offset: 0x000ABAF0
	public static void FindHeightmap(Vector3 point, float radius, List<Heightmap> heightmaps)
	{
		foreach (Heightmap heightmap in Heightmap.s_heightmaps)
		{
			if (heightmap.IsPointInside(point, radius))
			{
				heightmaps.Add(heightmap);
			}
		}
	}

	// Token: 0x0600175B RID: 5979 RVA: 0x000AD94C File Offset: 0x000ABB4C
	public static Heightmap.Biome FindBiome(Vector3 point)
	{
		Heightmap heightmap = Heightmap.FindHeightmap(point);
		if (!heightmap)
		{
			return Heightmap.Biome.None;
		}
		return heightmap.GetBiome(point, 0.02f, false);
	}

	// Token: 0x0600175C RID: 5980 RVA: 0x000AD978 File Offset: 0x000ABB78
	public static bool HaveQueuedRebuild(Vector3 point, float radius)
	{
		Heightmap.s_tempHmaps.Clear();
		Heightmap.FindHeightmap(point, radius, Heightmap.s_tempHmaps);
		using (List<Heightmap>.Enumerator enumerator = Heightmap.s_tempHmaps.GetEnumerator())
		{
			while (enumerator.MoveNext())
			{
				if (enumerator.Current.HaveQueuedRebuild())
				{
					return true;
				}
			}
		}
		return false;
	}

	// Token: 0x0600175D RID: 5981 RVA: 0x000AD9E8 File Offset: 0x000ABBE8
	public static void UpdateTerrainAlpha()
	{
		if (Player.m_localPlayer == null)
		{
			return;
		}
		List<Heightmap> list = new List<Heightmap>();
		Heightmap.FindHeightmap(Player.m_localPlayer.transform.position, 150f, list);
		bool flag = false;
		using (List<Heightmap>.Enumerator enumerator = list.GetEnumerator())
		{
			while (enumerator.MoveNext())
			{
				if (Heightmap.UpdateTerrainAlpha(enumerator.Current))
				{
					flag = true;
				}
			}
		}
		if (!flag)
		{
			global::Console.instance.Print("Nothing to update");
			return;
		}
		global::Console.instance.Print("Updated terrain alpha");
	}

	// Token: 0x0600175E RID: 5982 RVA: 0x000ADA8C File Offset: 0x000ABC8C
	public static bool UpdateTerrainAlpha(Heightmap hmap)
	{
		HeightmapBuilder.HMBuildData hmbuildData = HeightmapBuilder.instance.RequestTerrainSync(hmap.transform.position, hmap.m_width, hmap.m_scale, hmap.IsDistantLod, WorldGenerator.instance);
		int num = 0;
		for (int i = 0; i < hmap.m_width; i++)
		{
			for (int j = 0; j < hmap.m_width; j++)
			{
				int num2 = i * hmap.m_width + j;
				float a = hmbuildData.m_baseMask[num2].a;
				Color paintMask = hmap.GetPaintMask(j, i);
				if (a != paintMask.a)
				{
					paintMask.a = a;
					hmap.SetPaintMask(j, i, paintMask);
					num++;
				}
			}
		}
		if (num > 0)
		{
			hmap.GetAndCreateTerrainCompiler().UpdatePaintMask(hmap);
		}
		return num > 0;
	}

	// Token: 0x0600175F RID: 5983 RVA: 0x000ADB4C File Offset: 0x000ABD4C
	public FootStep.GroundMaterial GetGroundMaterial(Vector3 groundNormal, Vector3 point, float lavaValue = 0.6f)
	{
		float num = Mathf.Acos(Mathf.Clamp01(groundNormal.y)) * 57.29578f;
		Heightmap.Biome biome = this.GetBiome(point, 0.02f, false);
		if (biome == Heightmap.Biome.Mountain || biome == Heightmap.Biome.DeepNorth)
		{
			if (num < 40f && !this.IsCleared(point))
			{
				return FootStep.GroundMaterial.Snow;
			}
		}
		else if (biome == Heightmap.Biome.Swamp)
		{
			if (num < 40f)
			{
				return FootStep.GroundMaterial.Mud;
			}
		}
		else if (biome == Heightmap.Biome.Meadows || biome == Heightmap.Biome.BlackForest)
		{
			if (num < 25f)
			{
				return FootStep.GroundMaterial.Grass;
			}
		}
		else if (biome == Heightmap.Biome.AshLands)
		{
			if (this.IsLava(point, lavaValue))
			{
				return FootStep.GroundMaterial.Lava;
			}
			return FootStep.GroundMaterial.Ashlands;
		}
		return FootStep.GroundMaterial.GenericGround;
	}

	// Token: 0x06001760 RID: 5984 RVA: 0x000ADBDD File Offset: 0x000ABDDD
	public static float GetGroundMaterialOffset(FootStep.GroundMaterial material)
	{
		if (material == FootStep.GroundMaterial.Snow)
		{
			return 0.1f;
		}
		if (material == FootStep.GroundMaterial.Ashlands)
		{
			return 0.1f;
		}
		if (material != FootStep.GroundMaterial.Lava)
		{
			return 0f;
		}
		return 0.8f;
	}

	// Token: 0x06001761 RID: 5985 RVA: 0x000ADC10 File Offset: 0x000ABE10
	public static Heightmap.Biome FindBiomeClutter(Vector3 point)
	{
		if (ZoneSystem.instance && !ZoneSystem.instance.IsZoneLoaded(point))
		{
			return Heightmap.Biome.None;
		}
		Heightmap heightmap = Heightmap.FindHeightmap(point);
		if (heightmap)
		{
			return heightmap.GetBiome(point, 0.02f, false);
		}
		return Heightmap.Biome.None;
	}

	// Token: 0x06001762 RID: 5986 RVA: 0x000ADC58 File Offset: 0x000ABE58
	public void Clear()
	{
		this.m_heights.Clear();
		this.m_paintMask = null;
		this.m_materialInstance = null;
		this.m_buildData = null;
		if (this.m_collisionMesh)
		{
			this.m_collisionMesh.Clear();
		}
		if (this.m_renderMesh)
		{
			this.m_renderMesh.Clear();
		}
		if (this.m_collider)
		{
			this.m_collider.sharedMesh = null;
		}
	}

	// Token: 0x06001763 RID: 5987 RVA: 0x000ADCD0 File Offset: 0x000ABED0
	public TerrainComp GetAndCreateTerrainCompiler()
	{
		TerrainComp terrainComp = TerrainComp.FindTerrainCompiler(base.transform.position);
		if (terrainComp)
		{
			return terrainComp;
		}
		return UnityEngine.Object.Instantiate<GameObject>(this.m_terrainCompilerPrefab, base.transform.position, Quaternion.identity).GetComponent<TerrainComp>();
	}

	// Token: 0x170000C3 RID: 195
	// (get) Token: 0x06001764 RID: 5988 RVA: 0x000ADD18 File Offset: 0x000ABF18
	// (set) Token: 0x06001765 RID: 5989 RVA: 0x000ADD20 File Offset: 0x000ABF20
	public bool IsDistantLod
	{
		get
		{
			return this.m_isDistantLod;
		}
		set
		{
			if (this.m_isDistantLod == value)
			{
				return;
			}
			if (value)
			{
				Heightmap.s_heightmaps.Remove(this);
			}
			else
			{
				Heightmap.s_heightmaps.Add(this);
			}
			this.m_isDistantLod = value;
			this.UpdateShadowSettings();
		}
	}

	// Token: 0x1400000E RID: 14
	// (add) Token: 0x06001766 RID: 5990 RVA: 0x000ADD58 File Offset: 0x000ABF58
	// (remove) Token: 0x06001767 RID: 5991 RVA: 0x000ADD90 File Offset: 0x000ABF90
	public event Action m_clearConnectedWearNTearCache;

	// Token: 0x170000C4 RID: 196
	// (get) Token: 0x06001768 RID: 5992 RVA: 0x000ADDC5 File Offset: 0x000ABFC5
	// (set) Token: 0x06001769 RID: 5993 RVA: 0x000ADDCC File Offset: 0x000ABFCC
	public static bool EnableDistantTerrainShadows
	{
		get
		{
			return Heightmap.s_enableDistantTerrainShadows;
		}
		set
		{
			if (Heightmap.s_enableDistantTerrainShadows == value)
			{
				return;
			}
			Heightmap.s_enableDistantTerrainShadows = value;
			foreach (IMonoUpdater monoUpdater in Heightmap.Instances)
			{
				((Heightmap)monoUpdater).UpdateShadowSettings();
			}
		}
	}

	// Token: 0x170000C5 RID: 197
	// (get) Token: 0x0600176A RID: 5994 RVA: 0x000ADE30 File Offset: 0x000AC030
	public static List<IMonoUpdater> Instances { get; } = new List<IMonoUpdater>();

	// Token: 0x0400171A RID: 5914
	private static readonly Dictionary<Heightmap.Biome, int> s_biomeToIndex = new Dictionary<Heightmap.Biome, int>
	{
		{
			Heightmap.Biome.None,
			0
		},
		{
			Heightmap.Biome.Meadows,
			1
		},
		{
			Heightmap.Biome.Swamp,
			2
		},
		{
			Heightmap.Biome.Mountain,
			3
		},
		{
			Heightmap.Biome.BlackForest,
			4
		},
		{
			Heightmap.Biome.Plains,
			5
		},
		{
			Heightmap.Biome.AshLands,
			6
		},
		{
			Heightmap.Biome.DeepNorth,
			7
		},
		{
			Heightmap.Biome.Ocean,
			8
		},
		{
			Heightmap.Biome.Mistlands,
			9
		}
	};

	// Token: 0x0400171B RID: 5915
	private static readonly Heightmap.Biome[] s_indexToBiome = new Heightmap.Biome[]
	{
		Heightmap.Biome.None,
		Heightmap.Biome.Meadows,
		Heightmap.Biome.Swamp,
		Heightmap.Biome.Mountain,
		Heightmap.Biome.BlackForest,
		Heightmap.Biome.Plains,
		Heightmap.Biome.AshLands,
		Heightmap.Biome.DeepNorth,
		Heightmap.Biome.Ocean,
		Heightmap.Biome.Mistlands
	};

	// Token: 0x0400171C RID: 5916
	private static readonly float[] s_tempBiomeWeights = new float[Enum.GetValues(typeof(Heightmap.Biome)).Length];

	// Token: 0x0400171D RID: 5917
	public GameObject m_terrainCompilerPrefab;

	// Token: 0x0400171E RID: 5918
	public int m_width = 32;

	// Token: 0x0400171F RID: 5919
	public float m_scale = 1f;

	// Token: 0x04001720 RID: 5920
	public Material m_material;

	// Token: 0x04001721 RID: 5921
	public const float c_LevelMaxDelta = 8f;

	// Token: 0x04001722 RID: 5922
	public const float c_SmoothMaxDelta = 1f;

	// Token: 0x04001723 RID: 5923
	[SerializeField]
	private bool m_isDistantLod;

	// Token: 0x04001724 RID: 5924
	private ShadowCastingMode m_shadowMode = ShadowCastingMode.ShadowsOnly;

	// Token: 0x04001725 RID: 5925
	private bool m_receiveShadows;

	// Token: 0x04001726 RID: 5926
	public bool m_distantLodEditorHax;

	// Token: 0x04001727 RID: 5927
	private static readonly List<Heightmap> s_tempHmaps = new List<Heightmap>();

	// Token: 0x04001728 RID: 5928
	private readonly List<float> m_heights = new List<float>();

	// Token: 0x04001729 RID: 5929
	private HeightmapBuilder.HMBuildData m_buildData;

	// Token: 0x0400172A RID: 5930
	private Texture2D m_paintMask;

	// Token: 0x0400172B RID: 5931
	private Material m_materialInstance;

	// Token: 0x0400172C RID: 5932
	private MeshCollider m_collider;

	// Token: 0x0400172D RID: 5933
	private MeshFilter m_meshFilter;

	// Token: 0x0400172E RID: 5934
	private MeshRenderer m_meshRenderer;

	// Token: 0x0400172F RID: 5935
	private RenderGroupSubscriber m_renderGroupSubscriber;

	// Token: 0x04001730 RID: 5936
	private readonly float[] m_oceanDepth = new float[4];

	// Token: 0x04001731 RID: 5937
	private Heightmap.Biome[] m_cornerBiomes = new Heightmap.Biome[]
	{
		Heightmap.Biome.Meadows,
		Heightmap.Biome.Meadows,
		Heightmap.Biome.Meadows,
		Heightmap.Biome.Meadows
	};

	// Token: 0x04001732 RID: 5938
	private Bounds m_bounds;

	// Token: 0x04001733 RID: 5939
	private BoundingSphere m_boundingSphere;

	// Token: 0x04001734 RID: 5940
	private Mesh m_collisionMesh;

	// Token: 0x04001735 RID: 5941
	private Mesh m_renderMesh;

	// Token: 0x04001736 RID: 5942
	private bool m_doLateUpdate;

	// Token: 0x04001738 RID: 5944
	private static readonly List<Heightmap> s_heightmaps = new List<Heightmap>();

	// Token: 0x04001739 RID: 5945
	private static readonly List<Vector3> s_tempVertices = new List<Vector3>();

	// Token: 0x0400173A RID: 5946
	private static readonly List<Vector2> s_tempUVs = new List<Vector2>();

	// Token: 0x0400173B RID: 5947
	private static readonly List<int> s_tempIndices = new List<int>();

	// Token: 0x0400173C RID: 5948
	private static readonly List<Color32> s_tempColors = new List<Color32>();

	// Token: 0x0400173D RID: 5949
	public static Color m_paintMaskDirt = new Color(1f, 0f, 0f, 1f);

	// Token: 0x0400173E RID: 5950
	public static Color m_paintMaskCultivated = new Color(0f, 1f, 0f, 1f);

	// Token: 0x0400173F RID: 5951
	public static Color m_paintMaskPaved = new Color(0f, 0f, 1f, 1f);

	// Token: 0x04001740 RID: 5952
	public static Color m_paintMaskNothing = new Color(0f, 0f, 0f, 1f);

	// Token: 0x04001741 RID: 5953
	public static Color m_paintMaskClearVegetation = new Color(0f, 0f, 0f, 0f);

	// Token: 0x04001742 RID: 5954
	private static bool s_enableDistantTerrainShadows = false;

	// Token: 0x04001743 RID: 5955
	private static int s_shaderPropertyClearedMaskTex = 0;

	// Token: 0x04001744 RID: 5956
	public const RenderGroup c_RenderGroup = RenderGroup.Overworld;

	// Token: 0x02000360 RID: 864
	[Flags]
	public enum Biome
	{
		// Token: 0x040025A4 RID: 9636
		None = 0,
		// Token: 0x040025A5 RID: 9637
		Meadows = 1,
		// Token: 0x040025A6 RID: 9638
		Swamp = 2,
		// Token: 0x040025A7 RID: 9639
		Mountain = 4,
		// Token: 0x040025A8 RID: 9640
		BlackForest = 8,
		// Token: 0x040025A9 RID: 9641
		Plains = 16,
		// Token: 0x040025AA RID: 9642
		AshLands = 32,
		// Token: 0x040025AB RID: 9643
		DeepNorth = 64,
		// Token: 0x040025AC RID: 9644
		Ocean = 256,
		// Token: 0x040025AD RID: 9645
		Mistlands = 512,
		// Token: 0x040025AE RID: 9646
		All = 895
	}

	// Token: 0x02000361 RID: 865
	[Flags]
	public enum BiomeArea
	{
		// Token: 0x040025B0 RID: 9648
		Edge = 1,
		// Token: 0x040025B1 RID: 9649
		Median = 2,
		// Token: 0x040025B2 RID: 9650
		Everything = 3
	}
}
