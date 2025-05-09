using System;
using System.Collections.Generic;
using UnityEngine;

// Token: 0x020001C7 RID: 455
public class TerrainComp : MonoBehaviour
{
	// Token: 0x06001A58 RID: 6744 RVA: 0x000C38B8 File Offset: 0x000C1AB8
	private void Awake()
	{
		this.m_nview = base.GetComponent<ZNetView>();
		this.m_hmap = Heightmap.FindHeightmap(base.transform.position);
		if (this.m_hmap == null)
		{
			ZLog.LogWarning("Terrain compiler could not find hmap");
			return;
		}
		TerrainComp terrainComp = TerrainComp.FindTerrainCompiler(base.transform.position);
		if (terrainComp)
		{
			ZLog.LogWarning("Found another terrain compiler in this area, removing it");
			ZNetScene.instance.Destroy(terrainComp.gameObject);
		}
		TerrainComp.s_instances.Add(this);
		this.m_nview.Register<ZPackage>("ApplyOperation", new Action<long, ZPackage>(this.RPC_ApplyOperation));
		this.Initialize();
		this.CheckLoad();
	}

	// Token: 0x06001A59 RID: 6745 RVA: 0x000C3966 File Offset: 0x000C1B66
	private void OnDestroy()
	{
		TerrainComp.s_instances.Remove(this);
	}

	// Token: 0x06001A5A RID: 6746 RVA: 0x000C3974 File Offset: 0x000C1B74
	private void Update()
	{
		if (!this.m_nview.IsValid())
		{
			return;
		}
		this.CheckLoad();
	}

	// Token: 0x06001A5B RID: 6747 RVA: 0x000C398C File Offset: 0x000C1B8C
	private void Initialize()
	{
		this.m_initialized = true;
		this.m_width = this.m_hmap.m_width;
		this.m_size = (float)this.m_width * this.m_hmap.m_scale;
		int num = this.m_width + 1;
		this.m_modifiedHeight = new bool[num * num];
		this.m_levelDelta = new float[num * num];
		this.m_smoothDelta = new float[num * num];
		this.m_modifiedPaint = new bool[num * num];
		this.m_paintMask = new Color[num * num];
	}

	// Token: 0x06001A5C RID: 6748 RVA: 0x000C3A1C File Offset: 0x000C1C1C
	private void Save()
	{
		if (!this.m_initialized)
		{
			return;
		}
		if (!this.m_nview.IsValid() || !this.m_nview.IsOwner())
		{
			return;
		}
		ZPackage zpackage = new ZPackage();
		zpackage.Write(1);
		zpackage.Write(this.m_operations);
		zpackage.Write(this.m_lastOpPoint);
		zpackage.Write(this.m_lastOpRadius);
		zpackage.Write(this.m_modifiedHeight.Length);
		for (int i = 0; i < this.m_modifiedHeight.Length; i++)
		{
			zpackage.Write(this.m_modifiedHeight[i]);
			if (this.m_modifiedHeight[i])
			{
				zpackage.Write(this.m_levelDelta[i]);
				zpackage.Write(this.m_smoothDelta[i]);
			}
		}
		zpackage.Write(this.m_modifiedPaint.Length);
		for (int j = 0; j < this.m_modifiedPaint.Length; j++)
		{
			zpackage.Write(this.m_modifiedPaint[j]);
			if (this.m_modifiedPaint[j])
			{
				zpackage.Write(this.m_paintMask[j].r);
				zpackage.Write(this.m_paintMask[j].g);
				zpackage.Write(this.m_paintMask[j].b);
				zpackage.Write(this.m_paintMask[j].a);
			}
		}
		byte[] bytes = Utils.Compress(zpackage.GetArray());
		this.m_nview.GetZDO().Set(ZDOVars.s_TCData, bytes);
		this.m_lastDataRevision = this.m_nview.GetZDO().DataRevision;
	}

	// Token: 0x06001A5D RID: 6749 RVA: 0x000C3BA4 File Offset: 0x000C1DA4
	private void CheckLoad()
	{
		if (this.m_nview.GetZDO().DataRevision != this.m_lastDataRevision)
		{
			int operations = this.m_operations;
			if (this.Load())
			{
				this.m_hmap.Poke(false);
				if (ClutterSystem.instance)
				{
					if (this.m_operations == operations + 1)
					{
						ClutterSystem.instance.ResetGrass(this.m_lastOpPoint, this.m_lastOpRadius);
						return;
					}
					ClutterSystem.instance.ResetGrass(this.m_hmap.transform.position, (float)this.m_hmap.m_width * this.m_hmap.m_scale / 2f);
				}
			}
		}
	}

	// Token: 0x06001A5E RID: 6750 RVA: 0x000C3C50 File Offset: 0x000C1E50
	private bool Load()
	{
		this.m_lastDataRevision = this.m_nview.GetZDO().DataRevision;
		byte[] byteArray = this.m_nview.GetZDO().GetByteArray(ZDOVars.s_TCData, null);
		if (byteArray == null)
		{
			return false;
		}
		ZPackage zpackage = new ZPackage(Utils.Decompress(byteArray));
		zpackage.ReadInt();
		this.m_operations = zpackage.ReadInt();
		this.m_lastOpPoint = zpackage.ReadVector3();
		this.m_lastOpRadius = zpackage.ReadSingle();
		int num = zpackage.ReadInt();
		if (num != this.m_modifiedHeight.Length)
		{
			ZLog.LogWarning("Terrain data load error, height array missmatch");
			return false;
		}
		for (int i = 0; i < num; i++)
		{
			this.m_modifiedHeight[i] = zpackage.ReadBool();
			if (this.m_modifiedHeight[i])
			{
				this.m_levelDelta[i] = zpackage.ReadSingle();
				this.m_smoothDelta[i] = zpackage.ReadSingle();
			}
			else
			{
				this.m_levelDelta[i] = 0f;
				this.m_smoothDelta[i] = 0f;
			}
		}
		int num2 = zpackage.ReadInt();
		for (int j = 0; j < num2; j++)
		{
			this.m_modifiedPaint[j] = zpackage.ReadBool();
			if (this.m_modifiedPaint[j])
			{
				Color color = default(Color);
				color.r = zpackage.ReadSingle();
				color.g = zpackage.ReadSingle();
				color.b = zpackage.ReadSingle();
				color.a = zpackage.ReadSingle();
				this.m_paintMask[j] = color;
			}
			else
			{
				this.m_paintMask[j] = Color.black;
			}
		}
		if (num2 == this.m_width * this.m_width)
		{
			Color[] array = new Color[this.m_paintMask.Length];
			this.m_paintMask.CopyTo(array, 0);
			bool[] array2 = new bool[this.m_modifiedPaint.Length];
			this.m_modifiedPaint.CopyTo(array2, 0);
			int num3 = this.m_width + 1;
			for (int k = 0; k < this.m_paintMask.Length; k++)
			{
				int num4 = k / num3;
				int num5 = (k + 1) / num3;
				int num6 = k - num4;
				if (num4 == this.m_width)
				{
					num6 -= this.m_width;
				}
				if (k > 0 && (k - num4) % this.m_width == 0 && (k + 1 - num5) % this.m_width == 0)
				{
					num6--;
				}
				this.m_paintMask[k] = array[num6];
				this.m_modifiedPaint[k] = array2[num6];
			}
		}
		return true;
	}

	// Token: 0x06001A5F RID: 6751 RVA: 0x000C3EC8 File Offset: 0x000C20C8
	public static TerrainComp FindTerrainCompiler(Vector3 pos)
	{
		foreach (TerrainComp terrainComp in TerrainComp.s_instances)
		{
			float num = terrainComp.m_size / 2f;
			Vector3 position = terrainComp.transform.position;
			if (pos.x >= position.x - num && pos.x <= position.x + num && pos.z >= position.z - num && pos.z <= position.z + num)
			{
				return terrainComp;
			}
		}
		return null;
	}

	// Token: 0x06001A60 RID: 6752 RVA: 0x000C3F78 File Offset: 0x000C2178
	public void ApplyToHeightmap(Texture2D clearedMask, List<float> heights, float[] baseHeights, float[] levelOnlyHeights, Heightmap hm)
	{
		if (!this.m_initialized)
		{
			return;
		}
		int num = this.m_width + 1;
		for (int i = 0; i < num; i++)
		{
			for (int j = 0; j < num; j++)
			{
				int num2 = i * num + j;
				float num3 = this.m_levelDelta[num2];
				float num4 = this.m_smoothDelta[num2];
				if (num3 != 0f || num4 != 0f)
				{
					float num5 = heights[num2];
					float num6 = baseHeights[num2];
					float value = num5 + num3 + num4;
					value = Mathf.Clamp(value, num6 - 8f, num6 + 8f);
					heights[num2] = value;
				}
			}
		}
		for (int k = 0; k < num; k++)
		{
			for (int l = 0; l < num; l++)
			{
				int num7 = k * num + l;
				if (this.m_modifiedPaint[num7])
				{
					clearedMask.SetPixel(l, k, this.m_paintMask[num7]);
				}
			}
		}
	}

	// Token: 0x06001A61 RID: 6753 RVA: 0x000C405C File Offset: 0x000C225C
	public void ApplyOperation(TerrainOp modifier)
	{
		ZPackage zpackage = new ZPackage();
		zpackage.Write(modifier.transform.position);
		modifier.m_settings.Serialize(zpackage);
		this.m_nview.InvokeRPC("ApplyOperation", new object[]
		{
			zpackage
		});
	}

	// Token: 0x06001A62 RID: 6754 RVA: 0x000C40A8 File Offset: 0x000C22A8
	private void RPC_ApplyOperation(long sender, ZPackage pkg)
	{
		if (!this.m_nview.IsOwner())
		{
			return;
		}
		TerrainOp.Settings settings = new TerrainOp.Settings();
		Vector3 pos = pkg.ReadVector3();
		settings.Deserialize(pkg);
		this.DoOperation(pos, settings);
	}

	// Token: 0x06001A63 RID: 6755 RVA: 0x000C40E0 File Offset: 0x000C22E0
	private void DoOperation(Vector3 pos, TerrainOp.Settings modifier)
	{
		if (!this.m_initialized)
		{
			return;
		}
		this.InternalDoOperation(pos, modifier);
		this.Save();
		this.m_hmap.Poke(false);
		if (ClutterSystem.instance)
		{
			ClutterSystem.instance.ResetGrass(pos, modifier.GetRadius());
		}
	}

	// Token: 0x06001A64 RID: 6756 RVA: 0x000C4130 File Offset: 0x000C2330
	private void InternalDoOperation(Vector3 pos, TerrainOp.Settings modifier)
	{
		if (modifier.m_level)
		{
			this.LevelTerrain(pos + Vector3.up * modifier.m_levelOffset, modifier.m_levelRadius, modifier.m_square);
		}
		if (modifier.m_raise)
		{
			this.RaiseTerrain(pos, modifier.m_raiseRadius, modifier.m_raiseDelta, modifier.m_square, modifier.m_raisePower);
		}
		if (modifier.m_smooth)
		{
			this.SmoothTerrain(pos + Vector3.up * modifier.m_levelOffset, modifier.m_smoothRadius, modifier.m_square, modifier.m_smoothPower);
		}
		if (modifier.m_paintCleared)
		{
			this.PaintCleared(pos, modifier.m_paintRadius, modifier.m_paintType, modifier.m_paintHeightCheck, false);
		}
		this.m_operations++;
		this.m_lastOpPoint = pos;
		this.m_lastOpRadius = modifier.GetRadius();
	}

	// Token: 0x06001A65 RID: 6757 RVA: 0x000C4210 File Offset: 0x000C2410
	private void LevelTerrain(Vector3 worldPos, float radius, bool square)
	{
		int num;
		int num2;
		this.m_hmap.WorldToVertex(worldPos, out num, out num2);
		Vector3 vector = worldPos - base.transform.position;
		float num3 = radius / this.m_hmap.m_scale;
		int num4 = Mathf.CeilToInt(num3);
		int num5 = this.m_width + 1;
		Vector2 a = new Vector2((float)num, (float)num2);
		for (int i = num2 - num4; i <= num2 + num4; i++)
		{
			for (int j = num - num4; j <= num + num4; j++)
			{
				if ((square || Vector2.Distance(a, new Vector2((float)j, (float)i)) <= num3) && j >= 0 && i >= 0 && j < num5 && i < num5)
				{
					float height = this.m_hmap.GetHeight(j, i);
					float num6 = vector.y - height;
					int num7 = i * num5 + j;
					num6 += this.m_smoothDelta[num7];
					this.m_smoothDelta[num7] = 0f;
					this.m_levelDelta[num7] += num6;
					this.m_levelDelta[num7] = Mathf.Clamp(this.m_levelDelta[num7], -8f, 8f);
					this.m_modifiedHeight[num7] = true;
				}
			}
		}
	}

	// Token: 0x06001A66 RID: 6758 RVA: 0x000C4360 File Offset: 0x000C2560
	private void RaiseTerrain(Vector3 worldPos, float radius, float delta, bool square, float power)
	{
		int num;
		int num2;
		this.m_hmap.WorldToVertex(worldPos, out num, out num2);
		Vector3 vector = worldPos - base.transform.position;
		float num3 = radius / this.m_hmap.m_scale;
		int num4 = Mathf.CeilToInt(num3);
		int num5 = this.m_width + 1;
		Vector2 a = new Vector2((float)num, (float)num2);
		for (int i = num2 - num4; i <= num2 + num4; i++)
		{
			for (int j = num - num4; j <= num + num4; j++)
			{
				if (j >= 0 && i >= 0 && j < num5 && i < num5)
				{
					float num6 = 1f;
					if (!square)
					{
						float num7 = Vector2.Distance(a, new Vector2((float)j, (float)i));
						if (num7 > num3)
						{
							goto IL_191;
						}
						if (power > 0f)
						{
							num6 = num7 / num3;
							num6 = 1f - num6;
							if (power != 1f)
							{
								num6 = Mathf.Pow(num6, power);
							}
						}
					}
					float height = this.m_hmap.GetHeight(j, i);
					float num8 = delta * num6;
					float num9 = vector.y + num8;
					if (delta >= 0f || num9 <= height)
					{
						if (delta > 0f)
						{
							if (num9 < height)
							{
								goto IL_191;
							}
							if (num9 > height + num8)
							{
								num9 = height + num8;
							}
						}
						int num10 = i * num5 + j;
						float num11 = num9 - height + this.m_smoothDelta[num10];
						this.m_smoothDelta[num10] = 0f;
						this.m_levelDelta[num10] += num11;
						this.m_levelDelta[num10] = Mathf.Clamp(this.m_levelDelta[num10], -8f, 8f);
						this.m_modifiedHeight[num10] = true;
					}
				}
				IL_191:;
			}
		}
	}

	// Token: 0x06001A67 RID: 6759 RVA: 0x000C4520 File Offset: 0x000C2720
	private void SmoothTerrain(Vector3 worldPos, float radius, bool square, float power)
	{
		int num;
		int num2;
		this.m_hmap.WorldToVertex(worldPos, out num, out num2);
		float b = worldPos.y - base.transform.position.y;
		float num3 = radius / this.m_hmap.m_scale;
		int num4 = Mathf.CeilToInt(num3);
		Vector2 a = new Vector2((float)num, (float)num2);
		int num5 = this.m_width + 1;
		for (int i = num2 - num4; i <= num2 + num4; i++)
		{
			for (int j = num - num4; j <= num + num4; j++)
			{
				float num6 = Vector2.Distance(a, new Vector2((float)j, (float)i));
				if (num6 <= num3 && j >= 0 && i >= 0 && j < num5 && i < num5)
				{
					float num7 = num6 / num3;
					if (power == 3f)
					{
						num7 = num7 * num7 * num7;
					}
					else
					{
						num7 = Mathf.Pow(num7, power);
					}
					float height = this.m_hmap.GetHeight(j, i);
					float t = 1f - num7;
					float num8 = Mathf.Lerp(height, b, t) - height;
					int num9 = i * num5 + j;
					this.m_smoothDelta[num9] += num8;
					this.m_smoothDelta[num9] = Mathf.Clamp(this.m_smoothDelta[num9], -1f, 1f);
					this.m_modifiedHeight[num9] = true;
				}
			}
		}
	}

	// Token: 0x06001A68 RID: 6760 RVA: 0x000C4690 File Offset: 0x000C2890
	private void PaintCleared(Vector3 worldPos, float radius, TerrainModifier.PaintType paintType, bool heightCheck, bool apply)
	{
		worldPos.x -= 0.5f;
		worldPos.z -= 0.5f;
		float num = worldPos.y - base.transform.position.y;
		int num2;
		int num3;
		this.m_hmap.WorldToVertexMask(worldPos, out num2, out num3);
		float num4 = radius / this.m_hmap.m_scale;
		int num5 = Mathf.CeilToInt(num4);
		Vector2 a = new Vector2((float)num2, (float)num3);
		for (int i = num3 - num5; i <= num3 + num5; i++)
		{
			for (int j = num2 - num5; j <= num2 + num5; j++)
			{
				float num6 = Vector2.Distance(a, new Vector2((float)j, (float)i));
				int num7 = this.m_width + 1;
				if (j >= 0 && i >= 0 && j < num7 && i < num7 && (!heightCheck || this.m_hmap.GetHeight(j, i) <= num))
				{
					float num8 = 1f - Mathf.Clamp01(num6 / num4);
					num8 = Mathf.Pow(num8, 0.1f);
					Color color = this.m_hmap.GetPaintMask(j, i);
					float a2 = color.a;
					switch (paintType)
					{
					case TerrainModifier.PaintType.Dirt:
						color = Color.Lerp(color, Heightmap.m_paintMaskDirt, num8);
						break;
					case TerrainModifier.PaintType.Cultivate:
						color = Color.Lerp(color, Heightmap.m_paintMaskCultivated, num8);
						break;
					case TerrainModifier.PaintType.Paved:
						color = Color.Lerp(color, Heightmap.m_paintMaskPaved, num8);
						break;
					case TerrainModifier.PaintType.Reset:
						color = Color.Lerp(color, Heightmap.m_paintMaskNothing, num8);
						break;
					}
					color.a = a2;
					this.m_modifiedPaint[i * num7 + j] = true;
					this.m_paintMask[i * num7 + j] = color;
				}
			}
		}
	}

	// Token: 0x06001A69 RID: 6761 RVA: 0x000C485C File Offset: 0x000C2A5C
	public bool IsOwner()
	{
		return this.m_nview.IsValid() && this.m_nview.IsOwner();
	}

	// Token: 0x06001A6A RID: 6762 RVA: 0x000C4878 File Offset: 0x000C2A78
	public void UpdatePaintMask(Heightmap hmap)
	{
		for (int i = 0; i < this.m_width; i++)
		{
			for (int j = 0; j < this.m_width; j++)
			{
				int num = i * this.m_width + j;
				if (this.m_modifiedPaint[num])
				{
					Color color = this.m_paintMask[num];
					color.a = hmap.GetPaintMask(j, i).a;
					this.m_paintMask[num] = color;
				}
			}
		}
		this.Save();
		hmap.Poke(false);
	}

	// Token: 0x06001A6B RID: 6763 RVA: 0x000C48F8 File Offset: 0x000C2AF8
	public static void UpgradeTerrain()
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
				if (TerrainComp.UpgradeTerrain(enumerator.Current))
				{
					flag = true;
				}
			}
		}
		if (!flag)
		{
			global::Console.instance.Print("Nothing to optimize");
			return;
		}
		global::Console.instance.Print("Optimized terrain");
	}

	// Token: 0x06001A6C RID: 6764 RVA: 0x000C499C File Offset: 0x000C2B9C
	public static bool UpgradeTerrain(Heightmap hmap)
	{
		List<TerrainModifier> allInstances = TerrainModifier.GetAllInstances();
		int num = 0;
		List<TerrainModifier> list = new List<TerrainModifier>();
		foreach (TerrainModifier terrainModifier in allInstances)
		{
			ZNetView component = terrainModifier.GetComponent<ZNetView>();
			if (!(component == null) && component.IsValid() && component.IsOwner() && terrainModifier.m_playerModifiction)
			{
				if (!hmap.CheckTerrainModIsContained(terrainModifier))
				{
					num++;
				}
				else
				{
					list.Add(terrainModifier);
				}
			}
		}
		if (list.Count == 0)
		{
			return false;
		}
		TerrainComp andCreateTerrainCompiler = hmap.GetAndCreateTerrainCompiler();
		if (!andCreateTerrainCompiler.IsOwner())
		{
			global::Console.instance.Print("Skipping terrain at " + hmap.transform.position.ToString() + " ( another player is currently the owner )");
			return false;
		}
		int num2 = andCreateTerrainCompiler.m_width + 1;
		float[] array = new float[andCreateTerrainCompiler.m_modifiedHeight.Length];
		for (int i = 0; i < num2; i++)
		{
			for (int j = 0; j < num2; j++)
			{
				array[i * num2 + j] = hmap.GetHeight(j, i);
			}
		}
		Color[] array2 = new Color[andCreateTerrainCompiler.m_paintMask.Length];
		for (int k = 0; k < num2; k++)
		{
			for (int l = 0; l < num2; l++)
			{
				array2[k * num2 + l] = hmap.GetPaintMask(l, k);
			}
		}
		foreach (TerrainModifier terrainModifier2 in list)
		{
			terrainModifier2.enabled = false;
			terrainModifier2.GetComponent<ZNetView>().Destroy();
		}
		hmap.Poke(false);
		int num3 = 0;
		for (int m = 0; m < num2; m++)
		{
			for (int n = 0; n < num2; n++)
			{
				int num4 = m * num2 + n;
				float num5 = array[num4];
				float height = hmap.GetHeight(n, m);
				float num6 = num5 - height;
				if (Mathf.Abs(num6) >= 0.001f)
				{
					andCreateTerrainCompiler.m_modifiedHeight[num4] = true;
					andCreateTerrainCompiler.m_levelDelta[num4] += num6;
					num3++;
				}
			}
		}
		int num7 = 0;
		for (int num8 = 0; num8 < num2; num8++)
		{
			for (int num9 = 0; num9 < num2; num9++)
			{
				int num10 = num8 * num2 + num9;
				Color color = array2[num10];
				Color paintMask = hmap.GetPaintMask(num9, num8);
				if (!(color == paintMask))
				{
					andCreateTerrainCompiler.m_modifiedPaint[num10] = true;
					andCreateTerrainCompiler.m_paintMask[num10] = color;
					num7++;
				}
			}
		}
		andCreateTerrainCompiler.Save();
		hmap.Poke(false);
		if (ClutterSystem.instance)
		{
			ClutterSystem.instance.ResetGrass(hmap.transform.position, (float)hmap.m_width * hmap.m_scale / 2f);
		}
		global::Console.instance.Print(string.Concat(new string[]
		{
			"Operations optimized:",
			list.Count.ToString(),
			"  height changes:",
			num3.ToString(),
			"  paint changes:",
			num7.ToString()
		}));
		return true;
	}

	// Token: 0x04001ABB RID: 6843
	private const int terrainCompVersion = 1;

	// Token: 0x04001ABC RID: 6844
	private static readonly List<TerrainComp> s_instances = new List<TerrainComp>();

	// Token: 0x04001ABD RID: 6845
	private bool m_initialized;

	// Token: 0x04001ABE RID: 6846
	private int m_width;

	// Token: 0x04001ABF RID: 6847
	private float m_size;

	// Token: 0x04001AC0 RID: 6848
	private int m_operations;

	// Token: 0x04001AC1 RID: 6849
	private bool[] m_modifiedHeight;

	// Token: 0x04001AC2 RID: 6850
	private float[] m_levelDelta;

	// Token: 0x04001AC3 RID: 6851
	private float[] m_smoothDelta;

	// Token: 0x04001AC4 RID: 6852
	private bool[] m_modifiedPaint;

	// Token: 0x04001AC5 RID: 6853
	private Color[] m_paintMask;

	// Token: 0x04001AC6 RID: 6854
	private Heightmap m_hmap;

	// Token: 0x04001AC7 RID: 6855
	private ZNetView m_nview;

	// Token: 0x04001AC8 RID: 6856
	private uint m_lastDataRevision = uint.MaxValue;

	// Token: 0x04001AC9 RID: 6857
	private Vector3 m_lastOpPoint;

	// Token: 0x04001ACA RID: 6858
	private float m_lastOpRadius;
}
