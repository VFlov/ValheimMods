using System;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;

// Token: 0x020001E1 RID: 481
public class WorldGenerator
{
	// Token: 0x06001B9F RID: 7071 RVA: 0x000CEEE3 File Offset: 0x000CD0E3
	public static void Initialize(World world)
	{
		WorldGenerator instance = WorldGenerator.m_instance;
		if (instance != null)
		{
			instance.CleanCachedRiverData();
		}
		WorldGenerator.m_instance = new WorldGenerator(world);
	}

	// Token: 0x06001BA0 RID: 7072 RVA: 0x000CEF00 File Offset: 0x000CD100
	public static void Deitialize()
	{
		WorldGenerator.m_instance = null;
	}

	// Token: 0x170000CE RID: 206
	// (get) Token: 0x06001BA1 RID: 7073 RVA: 0x000CEF08 File Offset: 0x000CD108
	public static WorldGenerator instance
	{
		get
		{
			return WorldGenerator.m_instance;
		}
	}

	// Token: 0x06001BA2 RID: 7074 RVA: 0x000CEF10 File Offset: 0x000CD110
	private WorldGenerator(World world)
	{
		this.m_world = world;
		this.m_version = this.m_world.m_worldGenVersion;
		this.VersionSetup(this.m_version);
		UnityEngine.Random.State state = UnityEngine.Random.state;
		UnityEngine.Random.InitState(this.m_world.m_seed);
		if (WorldGenerator.m_noiseGen == null)
		{
			WorldGenerator.m_noiseGen = new FastNoise(this.m_world.m_seed);
			WorldGenerator.m_noiseGen.SetNoiseType(FastNoise.NoiseType.Cellular);
			WorldGenerator.m_noiseGen.SetCellularDistanceFunction(FastNoise.CellularDistanceFunction.Euclidean);
			WorldGenerator.m_noiseGen.SetCellularReturnType(FastNoise.CellularReturnType.Distance);
			WorldGenerator.m_noiseGen.SetFractalOctaves(2);
		}
		WorldGenerator.m_noiseGen.SetSeed(0);
		this.m_offset0 = (float)UnityEngine.Random.Range(-10000, 10000);
		this.m_offset1 = (float)UnityEngine.Random.Range(-10000, 10000);
		this.m_offset2 = (float)UnityEngine.Random.Range(-10000, 10000);
		this.m_offset3 = (float)UnityEngine.Random.Range(-10000, 10000);
		this.m_riverSeed = UnityEngine.Random.Range(int.MinValue, int.MaxValue);
		this.m_streamSeed = UnityEngine.Random.Range(int.MinValue, int.MaxValue);
		this.m_offset4 = (float)UnityEngine.Random.Range(-10000, 10000);
		if (!this.m_world.m_menu)
		{
			this.Pregenerate();
		}
		UnityEngine.Random.state = state;
	}

	// Token: 0x06001BA3 RID: 7075 RVA: 0x000CF0CC File Offset: 0x000CD2CC
	public void CleanCachedRiverData()
	{
		this.m_riverPoints.Clear();
		this.m_rivers.Clear();
		this.m_streams.Clear();
		this.m_cachedRiverPoints = null;
	}

	// Token: 0x06001BA4 RID: 7076 RVA: 0x000CF0F8 File Offset: 0x000CD2F8
	private void VersionSetup(int version)
	{
		ZLog.Log("Worldgenerator version setup:" + version.ToString());
		if (version <= 0)
		{
			this.m_minMountainDistance = 1500f;
		}
		if (version <= 1)
		{
			this.minDarklandNoise = 0.5f;
			this.maxMarshDistance = 8000f;
		}
	}

	// Token: 0x06001BA5 RID: 7077 RVA: 0x000CF144 File Offset: 0x000CD344
	private void Pregenerate()
	{
		this.FindLakes();
		this.m_rivers = this.PlaceRivers();
		this.m_streams = this.PlaceStreams();
	}

	// Token: 0x06001BA6 RID: 7078 RVA: 0x000CF164 File Offset: 0x000CD364
	public List<Vector2> GetLakes()
	{
		return this.m_lakes;
	}

	// Token: 0x06001BA7 RID: 7079 RVA: 0x000CF16C File Offset: 0x000CD36C
	public List<WorldGenerator.River> GetRivers()
	{
		return this.m_rivers;
	}

	// Token: 0x06001BA8 RID: 7080 RVA: 0x000CF174 File Offset: 0x000CD374
	public List<WorldGenerator.River> GetStreams()
	{
		return this.m_streams;
	}

	// Token: 0x06001BA9 RID: 7081 RVA: 0x000CF17C File Offset: 0x000CD37C
	private void FindLakes()
	{
		DateTime now = DateTime.Now;
		List<Vector2> list = new List<Vector2>();
		for (float num = -10000f; num <= 10000f; num = (float)((double)num + 128.0))
		{
			for (float num2 = -10000f; num2 <= 10000f; num2 = (float)((double)num2 + 128.0))
			{
				if (new Vector2(num2, num).magnitude <= 10000f && this.GetBaseHeight(num2, num, false) < 0.05f)
				{
					list.Add(new Vector2(num2, num));
				}
			}
		}
		this.m_lakes = this.MergePoints(list, 800f);
		DateTime.Now - now;
	}

	// Token: 0x06001BAA RID: 7082 RVA: 0x000CF224 File Offset: 0x000CD424
	private List<Vector2> MergePoints(List<Vector2> points, float range)
	{
		List<Vector2> list = new List<Vector2>();
		while (points.Count > 0)
		{
			Vector2 vector = points[0];
			points.RemoveAt(0);
			while (points.Count > 0)
			{
				int num = this.FindClosest(points, vector, range);
				if (num == -1)
				{
					break;
				}
				vector = (vector + points[num]) * 0.5f;
				points[num] = points[points.Count - 1];
				points.RemoveAt(points.Count - 1);
			}
			list.Add(vector);
		}
		return list;
	}

	// Token: 0x06001BAB RID: 7083 RVA: 0x000CF2B0 File Offset: 0x000CD4B0
	private int FindClosest(List<Vector2> points, Vector2 p, float maxDistance)
	{
		int result = -1;
		float num = 99999f;
		for (int i = 0; i < points.Count; i++)
		{
			if (!(points[i] == p))
			{
				float num2 = Vector2.Distance(p, points[i]);
				if (num2 < maxDistance && num2 < num)
				{
					result = i;
					num = num2;
				}
			}
		}
		return result;
	}

	// Token: 0x06001BAC RID: 7084 RVA: 0x000CF300 File Offset: 0x000CD500
	private List<WorldGenerator.River> PlaceStreams()
	{
		UnityEngine.Random.State state = UnityEngine.Random.state;
		UnityEngine.Random.InitState(this.m_streamSeed);
		List<WorldGenerator.River> list = new List<WorldGenerator.River>();
		int num = 0;
		DateTime now = DateTime.Now;
		for (int i = 0; i < 3000; i++)
		{
			Vector2 vector;
			float num2;
			Vector2 vector2;
			if (this.FindStreamStartPoint(100, 26f, 31f, out vector, out num2) && this.FindStreamEndPoint(100, 36f, 44f, vector, 80f, 200f, out vector2))
			{
				Vector2 vector3 = (vector + vector2) * 0.5f;
				float pregenerationHeight = this.GetPregenerationHeight(vector3.x, vector3.y);
				if (pregenerationHeight >= 26f && pregenerationHeight <= 44f)
				{
					WorldGenerator.River river = new WorldGenerator.River();
					river.p0 = vector;
					river.p1 = vector2;
					river.center = vector3;
					river.widthMax = 20f;
					river.widthMin = 20f;
					float num3 = Vector2.Distance(river.p0, river.p1);
					river.curveWidth = (float)((double)num3 / 15.0);
					river.curveWavelength = (float)((double)num3 / 20.0);
					list.Add(river);
					num++;
				}
			}
		}
		this.RenderRivers(list);
		UnityEngine.Random.state = state;
		DateTime.Now - now;
		return list;
	}

	// Token: 0x06001BAD RID: 7085 RVA: 0x000CF468 File Offset: 0x000CD668
	private bool FindStreamEndPoint(int iterations, float minHeight, float maxHeight, Vector2 start, float minLength, float maxLength, out Vector2 end)
	{
		float num = (float)(((double)maxLength - (double)minLength) / (double)iterations);
		float num2 = maxLength;
		for (int i = 0; i < iterations; i++)
		{
			num2 = (float)((double)num2 - (double)num);
			float f = UnityEngine.Random.Range(0f, 6.2831855f);
			Vector2 vector = start + new Vector2(Mathf.Sin(f), Mathf.Cos(f)) * num2;
			float pregenerationHeight = this.GetPregenerationHeight(vector.x, vector.y);
			if (pregenerationHeight > minHeight && pregenerationHeight < maxHeight)
			{
				end = vector;
				return true;
			}
		}
		end = Vector2.zero;
		return false;
	}

	// Token: 0x06001BAE RID: 7086 RVA: 0x000CF500 File Offset: 0x000CD700
	private bool FindStreamStartPoint(int iterations, float minHeight, float maxHeight, out Vector2 p, out float starth)
	{
		for (int i = 0; i < iterations; i++)
		{
			float num = UnityEngine.Random.Range(-10000f, 10000f);
			float num2 = UnityEngine.Random.Range(-10000f, 10000f);
			float pregenerationHeight = this.GetPregenerationHeight(num, num2);
			if (pregenerationHeight > minHeight && pregenerationHeight < maxHeight)
			{
				p = new Vector2(num, num2);
				starth = pregenerationHeight;
				return true;
			}
		}
		p = Vector2.zero;
		starth = 0f;
		return false;
	}

	// Token: 0x06001BAF RID: 7087 RVA: 0x000CF574 File Offset: 0x000CD774
	private List<WorldGenerator.River> PlaceRivers()
	{
		UnityEngine.Random.State state = UnityEngine.Random.state;
		UnityEngine.Random.InitState(this.m_riverSeed);
		DateTime now = DateTime.Now;
		List<WorldGenerator.River> list = new List<WorldGenerator.River>();
		List<Vector2> list2 = new List<Vector2>(this.m_lakes);
		while (list2.Count > 1)
		{
			Vector2 vector = list2[0];
			int num = this.FindRandomRiverEnd(list, this.m_lakes, vector, 2000f, 0.4f, 128f);
			if (num == -1 && !this.HaveRiver(list, vector))
			{
				num = this.FindRandomRiverEnd(list, this.m_lakes, vector, 5000f, 0.4f, 128f);
			}
			if (num != -1)
			{
				WorldGenerator.River river = new WorldGenerator.River();
				river.p0 = vector;
				river.p1 = this.m_lakes[num];
				river.center = (river.p0 + river.p1) * 0.5f;
				river.widthMax = UnityEngine.Random.Range(60f, 100f);
				river.widthMin = UnityEngine.Random.Range(60f, river.widthMax);
				float num2 = Vector2.Distance(river.p0, river.p1);
				river.curveWidth = (float)((double)num2 / 15.0);
				river.curveWavelength = (float)((double)num2 / 20.0);
				list.Add(river);
			}
			else
			{
				list2.RemoveAt(0);
			}
		}
		this.RenderRivers(list);
		DateTime.Now - now;
		UnityEngine.Random.state = state;
		return list;
	}

	// Token: 0x06001BB0 RID: 7088 RVA: 0x000CF6FC File Offset: 0x000CD8FC
	private int FindClosestRiverEnd(List<WorldGenerator.River> rivers, List<Vector2> points, Vector2 p, float maxDistance, float heightLimit, float checkStep)
	{
		int result = -1;
		float num = 99999f;
		for (int i = 0; i < points.Count; i++)
		{
			if (!(points[i] == p))
			{
				float num2 = Vector2.Distance(p, points[i]);
				if (num2 < maxDistance && num2 < num && !this.HaveRiver(rivers, p, points[i]) && this.IsRiverAllowed(p, points[i], checkStep, heightLimit))
				{
					result = i;
					num = num2;
				}
			}
		}
		return result;
	}

	// Token: 0x06001BB1 RID: 7089 RVA: 0x000CF774 File Offset: 0x000CD974
	private int FindRandomRiverEnd(List<WorldGenerator.River> rivers, List<Vector2> points, Vector2 p, float maxDistance, float heightLimit, float checkStep)
	{
		List<int> list = new List<int>();
		for (int i = 0; i < points.Count; i++)
		{
			if (!(points[i] == p) && Vector2.Distance(p, points[i]) < maxDistance && !this.HaveRiver(rivers, p, points[i]) && this.IsRiverAllowed(p, points[i], checkStep, heightLimit))
			{
				list.Add(i);
			}
		}
		if (list.Count == 0)
		{
			return -1;
		}
		return list[UnityEngine.Random.Range(0, list.Count)];
	}

	// Token: 0x06001BB2 RID: 7090 RVA: 0x000CF800 File Offset: 0x000CDA00
	private bool HaveRiver(List<WorldGenerator.River> rivers, Vector2 p0)
	{
		foreach (WorldGenerator.River river in rivers)
		{
			if (river.p0 == p0 || river.p1 == p0)
			{
				return true;
			}
		}
		return false;
	}

	// Token: 0x06001BB3 RID: 7091 RVA: 0x000CF86C File Offset: 0x000CDA6C
	private bool HaveRiver(List<WorldGenerator.River> rivers, Vector2 p0, Vector2 p1)
	{
		foreach (WorldGenerator.River river in rivers)
		{
			if ((river.p0 == p0 && river.p1 == p1) || (river.p0 == p1 && river.p1 == p0))
			{
				return true;
			}
		}
		return false;
	}

	// Token: 0x06001BB4 RID: 7092 RVA: 0x000CF8F4 File Offset: 0x000CDAF4
	private bool IsRiverAllowed(Vector2 p0, Vector2 p1, float step, float heightLimit)
	{
		float num = Vector2.Distance(p0, p1);
		Vector2 normalized = (p1 - p0).normalized;
		bool flag = true;
		for (float num2 = step; num2 <= (float)((double)num - (double)step); num2 = (float)((double)num2 + (double)step))
		{
			Vector2 vector = p0 + normalized * num2;
			float baseHeight = this.GetBaseHeight(vector.x, vector.y, false);
			if (baseHeight > heightLimit)
			{
				return false;
			}
			if (baseHeight > 0.05f)
			{
				flag = false;
			}
		}
		return !flag;
	}

	// Token: 0x06001BB5 RID: 7093 RVA: 0x000CF974 File Offset: 0x000CDB74
	private void RenderRivers(List<WorldGenerator.River> rivers)
	{
		DateTime now = DateTime.Now;
		Dictionary<Vector2i, List<WorldGenerator.RiverPoint>> dictionary = new Dictionary<Vector2i, List<WorldGenerator.RiverPoint>>();
		foreach (WorldGenerator.River river in rivers)
		{
			float num = (float)((double)river.widthMin / 8.0);
			Vector2 normalized = (river.p1 - river.p0).normalized;
			Vector2 a = new Vector2(-normalized.y, normalized.x);
			float num2 = Vector2.Distance(river.p0, river.p1);
			for (float num3 = 0f; num3 <= num2; num3 = (float)((double)num3 + (double)num))
			{
				float num4 = (float)((double)num3 / (double)river.curveWavelength);
				float d = (float)(Math.Sin((double)num4) * Math.Sin((double)num4 * 0.634119987487793) * Math.Sin((double)num4 * 0.3341200053691864) * (double)river.curveWidth);
				float r = UnityEngine.Random.Range(river.widthMin, river.widthMax);
				Vector2 p = river.p0 + normalized * num3 + a * d;
				this.AddRiverPoint(dictionary, p, r, river);
			}
		}
		foreach (KeyValuePair<Vector2i, List<WorldGenerator.RiverPoint>> keyValuePair in dictionary)
		{
			WorldGenerator.RiverPoint[] collection;
			if (this.m_riverPoints.TryGetValue(keyValuePair.Key, out collection))
			{
				List<WorldGenerator.RiverPoint> list = new List<WorldGenerator.RiverPoint>(collection);
				list.AddRange(keyValuePair.Value);
				this.m_riverPoints[keyValuePair.Key] = list.ToArray();
			}
			else
			{
				WorldGenerator.RiverPoint[] value = keyValuePair.Value.ToArray();
				this.m_riverPoints.Add(keyValuePair.Key, value);
			}
		}
		DateTime.Now - now;
	}

	// Token: 0x06001BB6 RID: 7094 RVA: 0x000CFB98 File Offset: 0x000CDD98
	private void AddRiverPoint(Dictionary<Vector2i, List<WorldGenerator.RiverPoint>> riverPoints, Vector2 p, float r, WorldGenerator.River river)
	{
		Vector2i riverGrid = this.GetRiverGrid(p.x, p.y);
		int num = Mathf.CeilToInt((float)((double)r / 64.0));
		for (int i = riverGrid.y - num; i <= riverGrid.y + num; i++)
		{
			for (int j = riverGrid.x - num; j <= riverGrid.x + num; j++)
			{
				Vector2i grid = new Vector2i(j, i);
				if (this.InsideRiverGrid(grid, p, r))
				{
					this.AddRiverPoint(riverPoints, grid, p, r, river);
				}
			}
		}
	}

	// Token: 0x06001BB7 RID: 7095 RVA: 0x000CFC24 File Offset: 0x000CDE24
	private void AddRiverPoint(Dictionary<Vector2i, List<WorldGenerator.RiverPoint>> riverPoints, Vector2i grid, Vector2 p, float r, WorldGenerator.River river)
	{
		List<WorldGenerator.RiverPoint> list;
		if (riverPoints.TryGetValue(grid, out list))
		{
			list.Add(new WorldGenerator.RiverPoint(p, r));
			return;
		}
		list = new List<WorldGenerator.RiverPoint>();
		list.Add(new WorldGenerator.RiverPoint(p, r));
		riverPoints.Add(grid, list);
	}

	// Token: 0x06001BB8 RID: 7096 RVA: 0x000CFC68 File Offset: 0x000CDE68
	public bool InsideRiverGrid(Vector2i grid, Vector2 p, float r)
	{
		Vector2 b = new Vector2((float)((double)grid.x * 64.0), (float)((double)grid.y * 64.0));
		Vector2 vector = p - b;
		return Math.Abs(vector.x) < (float)((double)r + 32.0) && Math.Abs(vector.y) < (float)((double)r + 32.0);
	}

	// Token: 0x06001BB9 RID: 7097 RVA: 0x000CFCE0 File Offset: 0x000CDEE0
	public Vector2i GetRiverGrid(float wx, float wy)
	{
		int x = Mathf.FloorToInt((float)(((double)wx + 32.0) / 64.0));
		int y = Mathf.FloorToInt((float)(((double)wy + 32.0) / 64.0));
		return new Vector2i(x, y);
	}

	// Token: 0x06001BBA RID: 7098 RVA: 0x000CFD2C File Offset: 0x000CDF2C
	private void GetRiverWeight(float wx, float wy, out float weight, out float width)
	{
		Vector2i riverGrid = this.GetRiverGrid(wx, wy);
		this.m_riverCacheLock.EnterReadLock();
		if (riverGrid == this.m_cachedRiverGrid)
		{
			if (this.m_cachedRiverPoints != null)
			{
				this.GetWeight(this.m_cachedRiverPoints, wx, wy, out weight, out width);
				this.m_riverCacheLock.ExitReadLock();
				return;
			}
			weight = 0f;
			width = 0f;
			this.m_riverCacheLock.ExitReadLock();
			return;
		}
		else
		{
			this.m_riverCacheLock.ExitReadLock();
			WorldGenerator.RiverPoint[] array;
			if (this.m_riverPoints.TryGetValue(riverGrid, out array))
			{
				this.GetWeight(array, wx, wy, out weight, out width);
				this.m_riverCacheLock.EnterWriteLock();
				this.m_cachedRiverGrid = riverGrid;
				this.m_cachedRiverPoints = array;
				this.m_riverCacheLock.ExitWriteLock();
				return;
			}
			this.m_riverCacheLock.EnterWriteLock();
			this.m_cachedRiverGrid = riverGrid;
			this.m_cachedRiverPoints = null;
			this.m_riverCacheLock.ExitWriteLock();
			weight = 0f;
			width = 0f;
			return;
		}
	}

	// Token: 0x06001BBB RID: 7099 RVA: 0x000CFE1C File Offset: 0x000CE01C
	private void GetWeight(WorldGenerator.RiverPoint[] points, float wx, float wy, out float weight, out float width)
	{
		Vector2 b = new Vector2(wx, wy);
		weight = 0f;
		width = 0f;
		float num = 0f;
		float num2 = 0f;
		foreach (WorldGenerator.RiverPoint riverPoint in points)
		{
			float num3 = Vector2.SqrMagnitude(riverPoint.p - b);
			if (num3 < riverPoint.w2)
			{
				float num4 = (float)Math.Sqrt((double)num3);
				float num5 = (float)(1.0 - (double)num4 / (double)riverPoint.w);
				if (num5 > weight)
				{
					weight = num5;
				}
				num = (float)((double)num + (double)riverPoint.w * (double)num5);
				num2 = (float)((double)num2 + (double)num5);
			}
		}
		if (num2 > 0f)
		{
			width = (float)((double)num / (double)num2);
		}
	}

	// Token: 0x06001BBC RID: 7100 RVA: 0x000CFEE0 File Offset: 0x000CE0E0
	private void GenerateBiomes()
	{
		this.m_biomes = new List<Heightmap.Biome>();
		int num = 400000000;
		for (int i = 0; i < num; i++)
		{
			this.m_biomes[i] = Heightmap.Biome.Meadows;
		}
	}

	// Token: 0x06001BBD RID: 7101 RVA: 0x000CFF18 File Offset: 0x000CE118
	public Heightmap.BiomeArea GetBiomeArea(Vector3 point)
	{
		Heightmap.Biome biome = this.GetBiome(point);
		Heightmap.Biome biome2 = this.GetBiome(point - new Vector3(-64f, 0f, -64f));
		Heightmap.Biome biome3 = this.GetBiome(point - new Vector3(64f, 0f, -64f));
		Heightmap.Biome biome4 = this.GetBiome(point - new Vector3(64f, 0f, 64f));
		Heightmap.Biome biome5 = this.GetBiome(point - new Vector3(-64f, 0f, 64f));
		Heightmap.Biome biome6 = this.GetBiome(point - new Vector3(-64f, 0f, 0f));
		Heightmap.Biome biome7 = this.GetBiome(point - new Vector3(64f, 0f, 0f));
		Heightmap.Biome biome8 = this.GetBiome(point - new Vector3(0f, 0f, -64f));
		Heightmap.Biome biome9 = this.GetBiome(point - new Vector3(0f, 0f, 64f));
		if (biome == biome2 && biome == biome3 && biome == biome4 && biome == biome5 && biome == biome6 && biome == biome7 && biome == biome8 && biome == biome9)
		{
			return Heightmap.BiomeArea.Median;
		}
		return Heightmap.BiomeArea.Edge;
	}

	// Token: 0x06001BBE RID: 7102 RVA: 0x000D0062 File Offset: 0x000CE262
	public Heightmap.Biome GetBiome(Vector3 point)
	{
		return this.GetBiome(point.x, point.z, 0.02f, false);
	}

	// Token: 0x06001BBF RID: 7103 RVA: 0x000D007C File Offset: 0x000CE27C
	public static bool IsAshlands(float x, float y)
	{
		double num = (double)WorldGenerator.WorldAngle(x, y) * 100.0;
		return (double)DUtils.Length(x, (float)((double)y + (double)WorldGenerator.ashlandsYOffset)) > (double)WorldGenerator.ashlandsMinDistance + num;
	}

	// Token: 0x06001BC0 RID: 7104 RVA: 0x000D00B8 File Offset: 0x000CE2B8
	public static float GetAshlandsOceanGradient(float x, float y)
	{
		double num = (double)WorldGenerator.WorldAngle(x, y + WorldGenerator.ashlandsYOffset) * 100.0;
		return (float)(((double)DUtils.Length(x, y + WorldGenerator.ashlandsYOffset) - ((double)WorldGenerator.ashlandsMinDistance + num)) / 300.0);
	}

	// Token: 0x06001BC1 RID: 7105 RVA: 0x000D0100 File Offset: 0x000CE300
	public static float GetAshlandsOceanGradient(Vector2 pos)
	{
		return WorldGenerator.GetAshlandsOceanGradient(pos.x, pos.y);
	}

	// Token: 0x06001BC2 RID: 7106 RVA: 0x000D0113 File Offset: 0x000CE313
	public static float GetAshlandsOceanGradient(Vector3 pos)
	{
		return WorldGenerator.GetAshlandsOceanGradient(pos.x, pos.z);
	}

	// Token: 0x06001BC3 RID: 7107 RVA: 0x000D0128 File Offset: 0x000CE328
	public static bool IsDeepnorth(float x, float y)
	{
		float num = (float)((double)WorldGenerator.WorldAngle(x, y) * 100.0);
		return new Vector2(x, (float)((double)y + 4000.0)).magnitude > (float)(12000.0 + (double)num);
	}

	// Token: 0x06001BC4 RID: 7108 RVA: 0x000D0174 File Offset: 0x000CE374
	public Heightmap.Biome GetBiome(float wx, float wy, float oceanLevel = 0.02f, bool waterAlwaysOcean = false)
	{
		if (this.m_world.m_menu)
		{
			if (this.GetBaseHeight(wx, wy, true) >= 0.4f)
			{
				return Heightmap.Biome.Mountain;
			}
			return Heightmap.Biome.BlackForest;
		}
		else
		{
			float num = DUtils.Length(wx, wy);
			float baseHeight = this.GetBaseHeight(wx, wy, false);
			float num2 = (float)((double)WorldGenerator.WorldAngle(wx, wy) * 100.0);
			if (waterAlwaysOcean && this.GetHeight(wx, wy) <= oceanLevel)
			{
				return Heightmap.Biome.Ocean;
			}
			if (WorldGenerator.IsAshlands(wx, wy))
			{
				return Heightmap.Biome.AshLands;
			}
			if (!waterAlwaysOcean && baseHeight <= oceanLevel)
			{
				return Heightmap.Biome.Ocean;
			}
			if (WorldGenerator.IsDeepnorth(wx, wy))
			{
				if (baseHeight > 0.4f)
				{
					return Heightmap.Biome.Mountain;
				}
				return Heightmap.Biome.DeepNorth;
			}
			else
			{
				if (baseHeight > 0.4f)
				{
					return Heightmap.Biome.Mountain;
				}
				if (DUtils.PerlinNoise((double)((float)((double)this.m_offset0 + (double)wx)) * 0.0010000000474974513, (double)((float)((double)this.m_offset0 + (double)wy)) * 0.0010000000474974513) > 0.6f && num > 2000f && num < this.maxMarshDistance && baseHeight > 0.05f && baseHeight < 0.25f)
				{
					return Heightmap.Biome.Swamp;
				}
				if (DUtils.PerlinNoise((double)((float)((double)this.m_offset4 + (double)wx)) * 0.0010000000474974513, (double)((float)((double)this.m_offset4 + (double)wy)) * 0.0010000000474974513) > this.minDarklandNoise && num > (float)(6000.0 + (double)num2) && num < 10000f)
				{
					return Heightmap.Biome.Mistlands;
				}
				if (DUtils.PerlinNoise((double)((float)((double)this.m_offset1 + (double)wx)) * 0.0010000000474974513, (double)((float)((double)this.m_offset1 + (double)wy)) * 0.0010000000474974513) > 0.4f && num > (float)(3000.0 + (double)num2) && num < 8000f)
				{
					return Heightmap.Biome.Plains;
				}
				if (DUtils.PerlinNoise((double)((float)((double)this.m_offset2 + (double)wx)) * 0.0010000000474974513, (double)((float)((double)this.m_offset2 + (double)wy)) * 0.0010000000474974513) > 0.4f && num > (float)(600.0 + (double)num2) && num < 6000f)
				{
					return Heightmap.Biome.BlackForest;
				}
				if (num > (float)(5000.0 + (double)num2))
				{
					return Heightmap.Biome.BlackForest;
				}
				return Heightmap.Biome.Meadows;
			}
		}
	}

	// Token: 0x06001BC5 RID: 7109 RVA: 0x000D0381 File Offset: 0x000CE581
	public static float WorldAngle(float wx, float wy)
	{
		return (float)Math.Sin((double)((float)((double)((float)Math.Atan2((double)wx, (double)wy)) * 20.0)));
	}

	// Token: 0x06001BC6 RID: 7110 RVA: 0x000D03A0 File Offset: 0x000CE5A0
	private float GetBaseHeight(float wx, float wy, bool menuTerrain)
	{
		if (menuTerrain)
		{
			double num = (double)wx;
			double num2 = (double)wy;
			num += 100000.0 + (double)this.m_offset0;
			num2 += 100000.0 + (double)this.m_offset1;
			float num3 = 0f;
			num3 = (float)((double)num3 + (double)DUtils.PerlinNoise(num * 0.0020000000949949026 * 0.5, num2 * 0.0020000000949949026 * 0.5) * (double)DUtils.PerlinNoise(num * 0.003000000026077032 * 0.5, num2 * 0.003000000026077032 * 0.5) * 1.0);
			num3 = (float)((double)num3 + (double)DUtils.PerlinNoise(num * 0.0020000000949949026 * 1.0, num2 * 0.0020000000949949026 * 1.0) * (double)DUtils.PerlinNoise(num * 0.003000000026077032 * 1.0, num2 * 0.003000000026077032 * 1.0) * (double)num3 * 0.8999999761581421);
			num3 = (float)((double)num3 + (double)DUtils.PerlinNoise(num * 0.004999999888241291 * 1.0, num2 * 0.004999999888241291 * 1.0) * (double)DUtils.PerlinNoise(num * 0.009999999776482582 * 1.0, num2 * 0.009999999776482582 * 1.0) * 0.5 * (double)num3);
			return (float)((double)num3 - 0.07000000029802322);
		}
		float num4 = DUtils.Length(wx, wy);
		double num5 = (double)wx;
		double num6 = (double)wy;
		num5 += 100000.0 + (double)this.m_offset0;
		num6 += 100000.0 + (double)this.m_offset1;
		float num7 = 0f;
		num7 = (float)((double)num7 + (double)DUtils.PerlinNoise(num5 * 0.0020000000949949026 * 0.5, num6 * 0.0020000000949949026 * 0.5) * (double)DUtils.PerlinNoise(num5 * 0.003000000026077032 * 0.5, num6 * 0.003000000026077032 * 0.5) * 1.0);
		num7 = (float)((double)num7 + (double)DUtils.PerlinNoise(num5 * 0.0020000000949949026 * 1.0, num6 * 0.0020000000949949026 * 1.0) * (double)DUtils.PerlinNoise(num5 * 0.003000000026077032 * 1.0, num6 * 0.003000000026077032 * 1.0) * (double)num7 * 0.8999999761581421);
		num7 = (float)((double)num7 + (double)DUtils.PerlinNoise(num5 * 0.004999999888241291 * 1.0, num6 * 0.004999999888241291 * 1.0) * (double)DUtils.PerlinNoise(num5 * 0.009999999776482582 * 1.0, num6 * 0.009999999776482582 * 1.0) * 0.5 * (double)num7);
		num7 = (float)((double)num7 - 0.07000000029802322);
		double num8 = (double)DUtils.PerlinNoise(num5 * 0.0020000000949949026 * 0.25 + 0.12300000339746475, num6 * 0.0020000000949949026 * 0.25 + 0.15123000741004944);
		float num9 = DUtils.PerlinNoise(num5 * 0.0020000000949949026 * 0.25 + 0.32100000977516174, num6 * 0.0020000000949949026 * 0.25 + 0.23100000619888306);
		float v = Mathf.Abs((float)(num8 - (double)num9));
		float num10 = (float)(1.0 - (double)DUtils.LerpStep(0.02f, 0.12f, v));
		num10 = (float)((double)num10 * (double)DUtils.SmoothStep(744f, 1000f, num4));
		num7 = (float)((double)num7 * (1.0 - (double)num10));
		if (num4 > 10000f)
		{
			float t = DUtils.LerpStep(10000f, 10500f, num4);
			num7 = DUtils.Lerp(num7, -0.2f, t);
			float num11 = 10490f;
			if (num4 > num11)
			{
				float t2 = Utils.LerpStep(num11, 10500f, num4);
				num7 = DUtils.Lerp(num7, -2f, t2);
			}
			return num7;
		}
		if (num4 < this.m_minMountainDistance && num7 > 0.28f)
		{
			float t3 = (float)DUtils.Clamp01(((double)num7 - 0.2800000011920929) / 0.09999999403953552);
			num7 = DUtils.Lerp(DUtils.Lerp(0.28f, 0.38f, t3), num7, DUtils.LerpStep((float)((double)this.m_minMountainDistance - 400.0), this.m_minMountainDistance, num4));
		}
		return num7;
	}

	// Token: 0x06001BC7 RID: 7111 RVA: 0x000D08B4 File Offset: 0x000CEAB4
	private float AddRivers(float wx, float wy, float h)
	{
		float num;
		float v;
		this.GetRiverWeight(wx, wy, out num, out v);
		if (num <= 0f)
		{
			return h;
		}
		float t = DUtils.LerpStep(20f, 60f, v);
		float num2 = DUtils.Lerp(0.14f, 0.12f, t);
		float num3 = DUtils.Lerp(0.139f, 0.128f, t);
		if (h > num2)
		{
			h = DUtils.Lerp(h, num2, num);
		}
		if (h > num3)
		{
			float t2 = DUtils.LerpStep(0.85f, 1f, num);
			h = DUtils.Lerp(h, num3, t2);
		}
		return h;
	}

	// Token: 0x06001BC8 RID: 7112 RVA: 0x000D0940 File Offset: 0x000CEB40
	public float GetHeight(float wx, float wy)
	{
		Heightmap.Biome biome = this.GetBiome(wx, wy, 0.02f, false);
		Color color;
		return this.GetBiomeHeight(biome, wx, wy, out color, false);
	}

	// Token: 0x06001BC9 RID: 7113 RVA: 0x000D0968 File Offset: 0x000CEB68
	public float GetHeight(float wx, float wy, out Color mask)
	{
		Heightmap.Biome biome = this.GetBiome(wx, wy, 0.02f, false);
		return this.GetBiomeHeight(biome, wx, wy, out mask, false);
	}

	// Token: 0x06001BCA RID: 7114 RVA: 0x000D0990 File Offset: 0x000CEB90
	public float GetPregenerationHeight(float wx, float wy)
	{
		Heightmap.Biome biome = this.GetBiome(wx, wy, 0.02f, false);
		Color color;
		return this.GetBiomeHeight(biome, wx, wy, out color, true);
	}

	// Token: 0x06001BCB RID: 7115 RVA: 0x000D09B8 File Offset: 0x000CEBB8
	public float GetBiomeHeight(Heightmap.Biome biome, float wx, float wy, out Color mask, bool preGeneration = false)
	{
		float num;
		if (preGeneration)
		{
			num = WorldGenerator.GetHeightMultiplier();
		}
		else
		{
			num = (float)((double)WorldGenerator.GetHeightMultiplier() * this.CreateAshlandsGap(wx, wy) * this.CreateDeepNorthGap(wx, wy));
		}
		mask = Color.black;
		if (this.m_world.m_menu)
		{
			if (biome == Heightmap.Biome.Mountain)
			{
				return (float)((double)this.GetSnowMountainHeight(wx, wy, true) * (double)num);
			}
			return (float)((double)this.GetMenuHeight(wx, wy) * (double)num);
		}
		else
		{
			if (DUtils.Length(wx, wy) > 10500f)
			{
				return -2f * WorldGenerator.GetHeightMultiplier();
			}
			if (biome <= Heightmap.Biome.Plains)
			{
				switch (biome)
				{
				case Heightmap.Biome.Meadows:
					return (float)((double)this.GetMeadowsHeight(wx, wy) * (double)num);
				case Heightmap.Biome.Swamp:
					return (float)((double)this.GetMarshHeight(wx, wy) * (double)num);
				case Heightmap.Biome.Meadows | Heightmap.Biome.Swamp:
					break;
				case Heightmap.Biome.Mountain:
					return (float)((double)this.GetSnowMountainHeight(wx, wy, false) * (double)num);
				default:
					if (biome == Heightmap.Biome.BlackForest)
					{
						return (float)((double)this.GetForestHeight(wx, wy) * (double)num);
					}
					if (biome == Heightmap.Biome.Plains)
					{
						return (float)((double)this.GetPlainsHeight(wx, wy) * (double)num);
					}
					break;
				}
			}
			else if (biome <= Heightmap.Biome.DeepNorth)
			{
				if (biome != Heightmap.Biome.AshLands)
				{
					if (biome == Heightmap.Biome.DeepNorth)
					{
						return (float)((double)this.GetDeepNorthHeight(wx, wy) * (double)num);
					}
				}
				else
				{
					if (preGeneration)
					{
						return (float)((double)this.GetAshlandsHeightPregenerate(wx, wy) * (double)num);
					}
					return (float)((double)this.GetAshlandsHeight(wx, wy, out mask, false) * (double)num);
				}
			}
			else
			{
				if (biome == Heightmap.Biome.Ocean)
				{
					return (float)((double)this.GetOceanHeight(wx, wy) * (double)num);
				}
				if (biome == Heightmap.Biome.Mistlands)
				{
					if (preGeneration)
					{
						return (float)((double)this.GetForestHeight(wx, wy) * (double)num);
					}
					return (float)((double)this.GetMistlandsHeight(wx, wy, out mask) * (double)num);
				}
			}
			return 0f;
		}
	}

	// Token: 0x06001BCC RID: 7116 RVA: 0x000D0B48 File Offset: 0x000CED48
	private float GetMarshHeight(float wx, float wy)
	{
		float wx2 = wx;
		float wy2 = wy;
		float num = 0.137f;
		wx = (float)((double)wx + 100000.0);
		wy = (float)((double)wy + 100000.0);
		double num2 = (double)wx;
		double num3 = (double)wy;
		float num4 = (float)((double)DUtils.PerlinNoise(num2 * 0.03999999910593033, num3 * 0.03999999910593033) * (double)DUtils.PerlinNoise(num2 * 0.07999999821186066, num3 * 0.07999999821186066));
		num = (float)((double)num + (double)num4 * 0.029999999329447746);
		num = this.AddRivers(wx2, wy2, num);
		num = (float)((double)num + (double)DUtils.PerlinNoise(num2 * 0.10000000149011612, num3 * 0.10000000149011612) * 0.009999999776482582);
		return (float)((double)num + (double)DUtils.PerlinNoise(num2 * 0.4000000059604645, num3 * 0.4000000059604645) * 0.003000000026077032);
	}

	// Token: 0x06001BCD RID: 7117 RVA: 0x000D0C38 File Offset: 0x000CEE38
	private float GetMeadowsHeight(float wx, float wy)
	{
		float wx2 = wx;
		float wy2 = wy;
		float baseHeight = this.GetBaseHeight(wx, wy, false);
		wx = (float)((double)wx + 100000.0 + (double)this.m_offset3);
		wy = (float)((double)wy + 100000.0 + (double)this.m_offset3);
		double num = (double)wx;
		double num2 = (double)wy;
		float num3 = (float)((double)DUtils.PerlinNoise(num * 0.009999999776482582, num2 * 0.009999999776482582) * (double)DUtils.PerlinNoise(num * 0.019999999552965164, num2 * 0.019999999552965164));
		num3 = (float)((double)num3 + (double)DUtils.PerlinNoise(num * 0.05000000074505806, num2 * 0.05000000074505806) * (double)DUtils.PerlinNoise(num * 0.10000000149011612, num2 * 0.10000000149011612) * (double)num3 * 0.5);
		float num4 = baseHeight;
		num4 = (float)((double)num4 + (double)num3 * 0.10000000149011612);
		float num5 = 0.15f;
		float num6 = (float)((double)num4 - (double)num5);
		float num7 = (float)DUtils.Clamp01((double)baseHeight / 0.4000000059604645);
		if (num6 > 0f)
		{
			num4 = (float)((double)num4 - (double)num6 * ((1.0 - (double)num7) * 0.75));
		}
		num4 = this.AddRivers(wx2, wy2, num4);
		num4 = (float)((double)num4 + (double)DUtils.PerlinNoise(num * 0.10000000149011612, num2 * 0.10000000149011612) * 0.009999999776482582);
		return (float)((double)num4 + (double)DUtils.PerlinNoise(num * 0.4000000059604645, num2 * 0.4000000059604645) * 0.003000000026077032);
	}

	// Token: 0x06001BCE RID: 7118 RVA: 0x000D0DE0 File Offset: 0x000CEFE0
	private float GetForestHeight(float wx, float wy)
	{
		float wx2 = wx;
		float wy2 = wy;
		float num = this.GetBaseHeight(wx, wy, false);
		wx = (float)((double)wx + 100000.0 + (double)this.m_offset3);
		wy = (float)((double)wy + 100000.0 + (double)this.m_offset3);
		double num2 = (double)wx;
		double num3 = (double)wy;
		float num4 = (float)((double)DUtils.PerlinNoise(num2 * 0.009999999776482582, num3 * 0.009999999776482582) * (double)DUtils.PerlinNoise(num2 * 0.019999999552965164, num3 * 0.019999999552965164));
		num4 = (float)((double)num4 + (double)DUtils.PerlinNoise(num2 * 0.05000000074505806, num3 * 0.05000000074505806) * (double)DUtils.PerlinNoise(num2 * 0.10000000149011612, num3 * 0.10000000149011612) * (double)num4 * 0.5);
		num = (float)((double)num + (double)num4 * 0.10000000149011612);
		num = this.AddRivers(wx2, wy2, num);
		num = (float)((double)num + (double)DUtils.PerlinNoise(num2 * 0.10000000149011612, num3 * 0.10000000149011612) * 0.009999999776482582);
		return (float)((double)num + (double)DUtils.PerlinNoise(num2 * 0.4000000059604645, num3 * 0.4000000059604645) * 0.003000000026077032);
	}

	// Token: 0x06001BCF RID: 7119 RVA: 0x000D0F34 File Offset: 0x000CF134
	private float GetMistlandsHeight(float wx, float wy, out Color mask)
	{
		float wx2 = wx;
		float wy2 = wy;
		float num = this.GetBaseHeight(wx, wy, false);
		wx = (float)((double)wx + 100000.0 + (double)this.m_offset3);
		wy = (float)((double)wy + 100000.0 + (double)this.m_offset3);
		double num2 = (double)wx;
		double num3 = (double)wy;
		float num4 = DUtils.PerlinNoise(num2 * 0.019999999552965164 * 0.699999988079071, num3 * 0.019999999552965164 * 0.699999988079071) * DUtils.PerlinNoise(num2 * 0.03999999910593033 * 0.699999988079071, num3 * 0.03999999910593033 * 0.699999988079071);
		num4 = (float)((double)num4 + (double)DUtils.PerlinNoise(num2 * 0.029999999329447746 * 0.699999988079071, num3 * 0.029999999329447746 * 0.699999988079071) * (double)DUtils.PerlinNoise(num2 * 0.05000000074505806 * 0.699999988079071, num3 * 0.05000000074505806 * 0.699999988079071) * (double)num4 * 0.5);
		num4 = ((num4 > 0f) ? ((float)Math.Pow((double)num4, 1.5)) : num4);
		num = (float)((double)num + (double)num4 * 0.4000000059604645);
		num = this.AddRivers(wx2, wy2, num);
		float num5 = (float)DUtils.Clamp01((double)num4 * 7.0);
		num = (float)((double)num + (double)DUtils.PerlinNoise(num2 * 0.10000000149011612, num3 * 0.10000000149011612) * 0.029999999329447746 * (double)num5);
		num = (float)((double)num + (double)DUtils.PerlinNoise(num2 * 0.4000000059604645, num3 * 0.4000000059604645) * 0.009999999776482582 * (double)num5);
		float num6 = (float)(1.0 - (double)num5 * 1.2000000476837158);
		num6 = (float)((double)num6 - (1.0 - (double)DUtils.LerpStep(0.1f, 0.3f, num5)));
		float a = (float)((double)num + (double)DUtils.PerlinNoise(num2 * 0.4000000059604645, num3 * 0.4000000059604645) * 0.0020000000949949026);
		float num7 = num;
		num7 = (float)((double)num7 * 400.0);
		num7 = Mathf.Ceil(num7);
		num7 = (float)((double)num7 / 400.0);
		num = DUtils.Lerp(a, num7, num5);
		mask = new Color(0f, 0f, 0f, num6);
		return num;
	}

	// Token: 0x06001BD0 RID: 7120 RVA: 0x000D11CC File Offset: 0x000CF3CC
	private float GetPlainsHeight(float wx, float wy)
	{
		float wx2 = wx;
		float wy2 = wy;
		float baseHeight = this.GetBaseHeight(wx, wy, false);
		wx = (float)((double)wx + 100000.0 + (double)this.m_offset3);
		wy = (float)((double)wy + 100000.0 + (double)this.m_offset3);
		double num = (double)wx;
		double num2 = (double)wy;
		float num3 = (float)((double)DUtils.PerlinNoise(num * 0.009999999776482582, num2 * 0.009999999776482582) * (double)DUtils.PerlinNoise(num * 0.019999999552965164, num2 * 0.019999999552965164));
		num3 = (float)((double)num3 + (double)DUtils.PerlinNoise(num * 0.05000000074505806, num2 * 0.05000000074505806) * (double)DUtils.PerlinNoise(num * 0.10000000149011612, num2 * 0.10000000149011612) * (double)num3 * 0.5);
		float num4 = baseHeight;
		num4 = (float)((double)num4 + (double)num3 * 0.10000000149011612);
		float num5 = 0.15f;
		float num6 = num4 - num5;
		float num7 = (float)DUtils.Clamp01((double)baseHeight / 0.4000000059604645);
		if (num6 > 0f)
		{
			num4 = (float)((double)num4 - (double)num6 * (1.0 - (double)num7) * 0.75);
		}
		num4 = this.AddRivers(wx2, wy2, num4);
		num4 = (float)((double)num4 + (double)DUtils.PerlinNoise(num * 0.10000000149011612, num2 * 0.10000000149011612) * 0.009999999776482582);
		return (float)((double)num4 + (double)DUtils.PerlinNoise(num * 0.4000000059604645, num2 * 0.4000000059604645) * 0.003000000026077032);
	}

	// Token: 0x06001BD1 RID: 7121 RVA: 0x000D1370 File Offset: 0x000CF570
	private float GetMenuHeight(float wx, float wy)
	{
		double baseHeight = (double)this.GetBaseHeight(wx, wy, true);
		wx = (float)((double)wx + 100000.0 + (double)this.m_offset3);
		wy = (float)((double)wy + 100000.0 + (double)this.m_offset3);
		double num = (double)wx;
		double num2 = (double)wy;
		float num3 = DUtils.PerlinNoise(num * 0.009999999776482582, num2 * 0.009999999776482582) * DUtils.PerlinNoise(num * 0.019999999552965164, num2 * 0.019999999552965164);
		num3 = (float)((double)num3 + (double)DUtils.PerlinNoise(num * 0.05000000074505806, num2 * 0.05000000074505806) * (double)DUtils.PerlinNoise(num * 0.10000000149011612, num2 * 0.10000000149011612) * (double)num3 * 0.5);
		return (float)((double)((float)((double)((float)(baseHeight + (double)num3 * 0.10000000149011612)) + (double)DUtils.PerlinNoise(num * 0.10000000149011612, num2 * 0.10000000149011612) * 0.009999999776482582)) + (double)DUtils.PerlinNoise(num * 0.4000000059604645, num2 * 0.4000000059604645) * 0.003000000026077032);
	}

	// Token: 0x06001BD2 RID: 7122 RVA: 0x000D14A0 File Offset: 0x000CF6A0
	private float GetAshlandsHeightPregenerate(float wx, float wy)
	{
		float wx2 = wx;
		float wy2 = wy;
		float num = this.GetBaseHeight(wx, wy, false);
		wx = (float)((double)wx + 100000.0 + (double)this.m_offset3);
		wy = (float)((double)wy + 100000.0 + (double)this.m_offset3);
		double num2 = (double)wx;
		double num3 = (double)wy;
		float num4 = (float)((double)DUtils.PerlinNoise(num2 * 0.009999999776482582, num3 * 0.009999999776482582) * (double)DUtils.PerlinNoise(num2 * 0.019999999552965164, num3 * 0.019999999552965164));
		num4 = (float)((double)num4 + (double)DUtils.PerlinNoise(num2 * 0.05000000074505806, num3 * 0.05000000074505806) * (double)DUtils.PerlinNoise(num2 * 0.10000000149011612, num3 * 0.10000000149011612) * (double)num4 * 0.5);
		num = (float)((double)num + (double)num4 * 0.10000000149011612);
		num = (float)((double)num + 0.10000000149011612);
		num = (float)((double)num + (double)DUtils.PerlinNoise(num2 * 0.10000000149011612, num3 * 0.10000000149011612) * 0.009999999776482582);
		num = (float)((double)num + (double)DUtils.PerlinNoise(num2 * 0.4000000059604645, num3 * 0.4000000059604645) * 0.003000000026077032);
		return this.AddRivers(wx2, wy2, num);
	}

	// Token: 0x06001BD3 RID: 7123 RVA: 0x000D1604 File Offset: 0x000CF804
	public float GetAshlandsHeight(float wx, float wy, out Color mask, bool cheap = false)
	{
		double num = (double)wx;
		double num2 = (double)wy;
		double a = (double)this.GetBaseHeight((float)num, (float)num2, false);
		double num3 = (double)WorldGenerator.WorldAngle((float)num, (float)num2) * 100.0;
		double num4 = DUtils.Length(num, num2 + (double)WorldGenerator.ashlandsYOffset - (double)WorldGenerator.ashlandsYOffset * 0.3) - ((double)WorldGenerator.ashlandsMinDistance + num3);
		num4 = Math.Abs(num4) / 1000.0;
		num4 = 1.0 - DUtils.Clamp01(num4);
		num4 = DUtils.MathfLikeSmoothStep(0.1, 1.0, num4);
		double num5 = Math.Abs(num);
		num5 = 1.0 - DUtils.Clamp01(num5 / 7500.0);
		num4 *= num5;
		double num6 = DUtils.Length(num, num2) - 10150.0;
		num6 = 1.0 - DUtils.Clamp01(num6 / 600.0);
		num += (double)(100000f + this.m_offset3);
		num2 += (double)(100000f + this.m_offset3);
		double num7 = 0.0;
		double num8 = 1.0;
		double num9 = 0.33000001311302185;
		int num10 = cheap ? 2 : 5;
		for (int i = 0; i < num10; i++)
		{
			num7 += num8 * DUtils.MathfLikeSmoothStep(0.0, 1.0, WorldGenerator.m_noiseGen.GetCellular(num * num9, num2 * num9));
			num9 *= 2.0;
			num8 *= 0.5;
		}
		num7 = DUtils.Remap(num7, -1.0, 1.0, 0.0, 1.0);
		double num11 = DUtils.Lerp(num4, DUtils.BlendOverlay(num4, num7), 0.5);
		double num12 = (double)(DUtils.PerlinNoise(num * 0.009999999776482582, num2 * 0.009999999776482582) * DUtils.PerlinNoise(num * 0.019999999552965164, num2 * 0.019999999552965164));
		num12 += (double)(DUtils.PerlinNoise(num * 0.05000000074505806, num2 * 0.05000000074505806) * DUtils.PerlinNoise(num * 0.10000000149011612, num2 * 0.10000000149011612)) * num12 * 0.5;
		double num13 = DUtils.Lerp(a, 0.15000000596046448, 0.75);
		num13 += num11 * 0.5;
		num13 = DUtils.Lerp(-1.0, num13, DUtils.MathfLikeSmoothStep(0.0, 1.0, num6));
		double num14 = 0.15;
		double num15 = 0.0;
		double num16 = 1.0;
		double num17 = 8.0;
		int num18 = cheap ? 2 : 3;
		for (int j = 0; j < num18; j++)
		{
			num15 += num16 * WorldGenerator.m_noiseGen.GetCellular(num * num17, num2 * num17);
			num17 *= 2.0;
			num16 *= 0.5;
		}
		num15 = DUtils.Remap(num15, -1.0, 1.0, 0.0, 1.0);
		num15 = DUtils.Clamp01(Math.Pow(num15, 4.0) * 2.0);
		double num19 = WorldGenerator.m_noiseGen.GetSimplexFractal(num * 0.075, num2 * 0.075);
		num19 = DUtils.Remap(num19, -1.0, 1.0, 0.0, 1.0);
		num19 = Math.Pow(num19, 1.399999976158142);
		num13 *= num19;
		double num20 = DUtils.Fbm(new Vector2((float)(num * 0.009999999776482582), (float)(num2 * 0.009999999776482582)), 3, 2.0, 0.5);
		num20 *= DUtils.Clamp01(DUtils.Remap(num4, 0.0, 0.5, 0.5, 1.0));
		num20 = DUtils.LerpStep(0.699999988079071, 1.0, num20);
		num20 = Math.Pow(num20, 2.0);
		double num21 = DUtils.BlendOverlay(num20, num15);
		num21 *= DUtils.Clamp01((num13 - num14 - 0.02) / 0.01);
		double num22 = (double)DUtils.PerlinNoise(num * 0.05 + 5124.0, num2 * 0.05 + 5000.0);
		num22 = Math.Pow(num22, 2.0);
		num22 = DUtils.Remap(num22, 0.0, 1.0, 0.009999999776482582, 0.054999999701976776);
		double b = (double)Mathf.Clamp((float)(num13 - num22), (float)(num14 + 0.009999999776482582), 5000f);
		num13 = DUtils.Lerp(num13, b, num21);
		mask = new Color(0f, 0f, 0f, (float)num21);
		return (float)num13;
	}

	// Token: 0x06001BD4 RID: 7124 RVA: 0x000D1B84 File Offset: 0x000CFD84
	private float GetEdgeHeight(float wx, float wy)
	{
		float num = DUtils.Length(wx, wy);
		float num2 = 10490f;
		if (num > num2)
		{
			float num3 = DUtils.LerpStep(num2, 10500f, num);
			return (float)(-2.0 * (double)num3);
		}
		float t = DUtils.LerpStep(10000f, 10100f, num);
		float num4 = this.GetBaseHeight(wx, wy, false);
		num4 = DUtils.Lerp(num4, 0f, t);
		return this.AddRivers(wx, wy, num4);
	}

	// Token: 0x06001BD5 RID: 7125 RVA: 0x000D1BFF File Offset: 0x000CFDFF
	private float GetOceanHeight(float wx, float wy)
	{
		return this.GetBaseHeight(wx, wy, false);
	}

	// Token: 0x06001BD6 RID: 7126 RVA: 0x000D1C0C File Offset: 0x000CFE0C
	private float BaseHeightTilt(float wx, float wy)
	{
		float baseHeight = this.GetBaseHeight((float)((double)wx - 1.0), wy, false);
		double baseHeight2 = (double)this.GetBaseHeight((float)((double)wx + 1.0), wy, false);
		float baseHeight3 = this.GetBaseHeight(wx, (float)((double)wy - 1.0), false);
		float baseHeight4 = this.GetBaseHeight(wx, (float)((double)wy + 1.0), false);
		return (float)((double)Mathf.Abs((float)(baseHeight2 - (double)baseHeight)) + (double)Mathf.Abs((float)((double)baseHeight3 - (double)baseHeight4)));
	}

	// Token: 0x06001BD7 RID: 7127 RVA: 0x000D1C8C File Offset: 0x000CFE8C
	private float GetSnowMountainHeight(float wx, float wy, bool menu)
	{
		float wx2 = wx;
		float wy2 = wy;
		float num = this.GetBaseHeight(wx, wy, menu);
		float num2 = this.BaseHeightTilt(wx, wy);
		wx = (float)((double)wx + 100000.0 + (double)this.m_offset3);
		wy = (float)((double)wy + 100000.0 + (double)this.m_offset3);
		double num3 = (double)wx;
		double num4 = (double)wy;
		float num5 = (float)((double)num - 0.4000000059604645);
		num = (float)((double)num + (double)num5);
		float num6 = (float)((double)DUtils.PerlinNoise(num3 * 0.009999999776482582, num4 * 0.009999999776482582) * (double)DUtils.PerlinNoise(num3 * 0.019999999552965164, num4 * 0.019999999552965164));
		num6 = (float)((double)num6 + (double)DUtils.PerlinNoise(num3 * 0.05000000074505806, num4 * 0.05000000074505806) * (double)DUtils.PerlinNoise(num3 * 0.10000000149011612, num4 * 0.10000000149011612) * (double)num6 * 0.5);
		num = (float)((double)num + (double)num6 * 0.20000000298023224);
		num = this.AddRivers(wx2, wy2, num);
		num = (float)((double)num + (double)DUtils.PerlinNoise(num3 * 0.10000000149011612, num4 * 0.10000000149011612) * 0.009999999776482582);
		num = (float)((double)num + (double)DUtils.PerlinNoise(num3 * 0.4000000059604645, num4 * 0.4000000059604645) * 0.003000000026077032);
		return (float)((double)num + (double)DUtils.PerlinNoise(num3 * 0.20000000298023224, num4 * 0.20000000298023224) * 2.0 * (double)num2);
	}

	// Token: 0x06001BD8 RID: 7128 RVA: 0x000D1E38 File Offset: 0x000D0038
	private float GetDeepNorthHeight(float wx, float wy)
	{
		float wx2 = wx;
		float wy2 = wy;
		float num = this.GetBaseHeight(wx, wy, false);
		wx = (float)((double)wx + 100000.0 + (double)this.m_offset3);
		wy = (float)((double)wy + 100000.0 + (double)this.m_offset3);
		double num2 = (double)wx;
		double num3 = (double)wy;
		float num4 = Mathf.Max(0f, (float)((double)num - 0.4000000059604645));
		num = (float)((double)num + (double)num4);
		float num5 = (float)((double)DUtils.PerlinNoise(num2 * 0.009999999776482582, num3 * 0.009999999776482582) * (double)DUtils.PerlinNoise(num2 * 0.019999999552965164, num3 * 0.019999999552965164));
		num5 = (float)((double)num5 + (double)DUtils.PerlinNoise(num2 * 0.05000000074505806, num3 * 0.05000000074505806) * (double)DUtils.PerlinNoise(num2 * 0.10000000149011612, num3 * 0.10000000149011612) * (double)num5 * 0.5);
		num = (float)((double)num + (double)num5 * 0.20000000298023224);
		num = (float)((double)num * 1.2000000476837158);
		num = this.AddRivers(wx2, wy2, num);
		num = (float)((double)num + (double)DUtils.PerlinNoise((double)(wx * 0.1f), (double)(wy * 0.1f)) * 0.009999999776482582);
		return (float)((double)num + (double)DUtils.PerlinNoise((double)(wx * 0.4f), (double)(wy * 0.4f)) * 0.003000000026077032);
	}

	// Token: 0x06001BD9 RID: 7129 RVA: 0x000D1FAC File Offset: 0x000D01AC
	private double CreateAshlandsGap(float wx, float wy)
	{
		double num = (double)WorldGenerator.WorldAngle(wx, wy) * 100.0;
		double num2 = (double)DUtils.Length(wx, wy + WorldGenerator.ashlandsYOffset) - ((double)WorldGenerator.ashlandsMinDistance + num);
		num2 = DUtils.Clamp01(Math.Abs(num2) / 400.0);
		return DUtils.MathfLikeSmoothStep(0.0, 1.0, (double)((float)num2));
	}

	// Token: 0x06001BDA RID: 7130 RVA: 0x000D2018 File Offset: 0x000D0218
	private double CreateDeepNorthGap(float wx, float wy)
	{
		double num = (double)WorldGenerator.WorldAngle(wx, wy) * 100.0;
		double num2 = (double)DUtils.Length(wx, wy + 4000f) - (12000.0 + num);
		num2 = DUtils.Clamp01(Math.Abs(num2) / 400.0);
		return DUtils.MathfLikeSmoothStep(0.0, 1.0, (double)((float)num2));
	}

	// Token: 0x06001BDB RID: 7131 RVA: 0x000D2085 File Offset: 0x000D0285
	public static bool InForest(Vector3 pos)
	{
		return WorldGenerator.GetForestFactor(pos) < 1.15f;
	}

	// Token: 0x06001BDC RID: 7132 RVA: 0x000D2094 File Offset: 0x000D0294
	public static float GetForestFactor(Vector3 pos)
	{
		float d = 0.4f;
		return DUtils.Fbm(pos * 0.01f * d, 3, 1.6f, 0.7f);
	}

	// Token: 0x06001BDD RID: 7133 RVA: 0x000D20C8 File Offset: 0x000D02C8
	public void GetTerrainDelta(Vector3 center, float radius, out float delta, out Vector3 slopeDirection)
	{
		int num = 10;
		float num2 = -999999f;
		float num3 = 999999f;
		Vector3 b = center;
		Vector3 a = center;
		for (int i = 0; i < num; i++)
		{
			Vector2 vector = UnityEngine.Random.insideUnitCircle * radius;
			Vector3 vector2 = center + new Vector3(vector.x, 0f, vector.y);
			float height = this.GetHeight(vector2.x, vector2.z);
			if (height < num3)
			{
				num3 = height;
				a = vector2;
			}
			if (height > num2)
			{
				num2 = height;
				b = vector2;
			}
		}
		delta = (float)((double)num2 - (double)num3);
		slopeDirection = Vector3.Normalize(a - b);
	}

	// Token: 0x06001BDE RID: 7134 RVA: 0x000D216F File Offset: 0x000D036F
	public int GetSeed()
	{
		return this.m_world.m_seed;
	}

	// Token: 0x06001BDF RID: 7135 RVA: 0x000D217C File Offset: 0x000D037C
	public static float GetHeightMultiplier()
	{
		return 200f;
	}

	// Token: 0x04001C99 RID: 7321
	private const float m_waterTreshold = 0.05f;

	// Token: 0x04001C9A RID: 7322
	private static WorldGenerator m_instance = null;

	// Token: 0x04001C9B RID: 7323
	private World m_world;

	// Token: 0x04001C9C RID: 7324
	private int m_version;

	// Token: 0x04001C9D RID: 7325
	private float m_offset0;

	// Token: 0x04001C9E RID: 7326
	private float m_offset1;

	// Token: 0x04001C9F RID: 7327
	private float m_offset2;

	// Token: 0x04001CA0 RID: 7328
	private float m_offset3;

	// Token: 0x04001CA1 RID: 7329
	private float m_offset4;

	// Token: 0x04001CA2 RID: 7330
	private int m_riverSeed;

	// Token: 0x04001CA3 RID: 7331
	private int m_streamSeed;

	// Token: 0x04001CA4 RID: 7332
	private List<Vector2> m_lakes;

	// Token: 0x04001CA5 RID: 7333
	private List<WorldGenerator.River> m_rivers = new List<WorldGenerator.River>();

	// Token: 0x04001CA6 RID: 7334
	private List<WorldGenerator.River> m_streams = new List<WorldGenerator.River>();

	// Token: 0x04001CA7 RID: 7335
	private Dictionary<Vector2i, WorldGenerator.RiverPoint[]> m_riverPoints = new Dictionary<Vector2i, WorldGenerator.RiverPoint[]>();

	// Token: 0x04001CA8 RID: 7336
	private WorldGenerator.RiverPoint[] m_cachedRiverPoints;

	// Token: 0x04001CA9 RID: 7337
	private Vector2i m_cachedRiverGrid = new Vector2i(-999999, -999999);

	// Token: 0x04001CAA RID: 7338
	private ReaderWriterLockSlim m_riverCacheLock = new ReaderWriterLockSlim();

	// Token: 0x04001CAB RID: 7339
	private List<Heightmap.Biome> m_biomes = new List<Heightmap.Biome>();

	// Token: 0x04001CAC RID: 7340
	private static FastNoise m_noiseGen;

	// Token: 0x04001CAD RID: 7341
	private const float c_HeightMultiplier = 200f;

	// Token: 0x04001CAE RID: 7342
	private const float riverGridSize = 64f;

	// Token: 0x04001CAF RID: 7343
	private const float minRiverWidth = 60f;

	// Token: 0x04001CB0 RID: 7344
	private const float maxRiverWidth = 100f;

	// Token: 0x04001CB1 RID: 7345
	private const float minRiverCurveWidth = 50f;

	// Token: 0x04001CB2 RID: 7346
	private const float maxRiverCurveWidth = 80f;

	// Token: 0x04001CB3 RID: 7347
	private const float minRiverCurveWaveLength = 50f;

	// Token: 0x04001CB4 RID: 7348
	private const float maxRiverCurveWaveLength = 70f;

	// Token: 0x04001CB5 RID: 7349
	private const int streams = 3000;

	// Token: 0x04001CB6 RID: 7350
	private const float streamWidth = 20f;

	// Token: 0x04001CB7 RID: 7351
	private const float meadowsMaxDistance = 5000f;

	// Token: 0x04001CB8 RID: 7352
	private const float minDeepForestNoise = 0.4f;

	// Token: 0x04001CB9 RID: 7353
	private const float minDeepForestDistance = 600f;

	// Token: 0x04001CBA RID: 7354
	private const float maxDeepForestDistance = 6000f;

	// Token: 0x04001CBB RID: 7355
	private const float deepForestForestFactorMax = 0.9f;

	// Token: 0x04001CBC RID: 7356
	private const float marshBiomeScale = 0.001f;

	// Token: 0x04001CBD RID: 7357
	private const float minMarshNoise = 0.6f;

	// Token: 0x04001CBE RID: 7358
	private const float minMarshDistance = 2000f;

	// Token: 0x04001CBF RID: 7359
	private float maxMarshDistance = 6000f;

	// Token: 0x04001CC0 RID: 7360
	private const float minMarshHeight = 0.05f;

	// Token: 0x04001CC1 RID: 7361
	private const float maxMarshHeight = 0.25f;

	// Token: 0x04001CC2 RID: 7362
	private const float heathBiomeScale = 0.001f;

	// Token: 0x04001CC3 RID: 7363
	private const float minHeathNoise = 0.4f;

	// Token: 0x04001CC4 RID: 7364
	private const float minHeathDistance = 3000f;

	// Token: 0x04001CC5 RID: 7365
	private const float maxHeathDistance = 8000f;

	// Token: 0x04001CC6 RID: 7366
	private const float darklandBiomeScale = 0.001f;

	// Token: 0x04001CC7 RID: 7367
	private float minDarklandNoise = 0.4f;

	// Token: 0x04001CC8 RID: 7368
	private const float minDarklandDistance = 6000f;

	// Token: 0x04001CC9 RID: 7369
	private const float maxDarklandDistance = 10000f;

	// Token: 0x04001CCA RID: 7370
	private const float oceanBiomeScale = 0.0005f;

	// Token: 0x04001CCB RID: 7371
	private const float oceanBiomeMinNoise = 0.4f;

	// Token: 0x04001CCC RID: 7372
	private const float oceanBiomeMaxNoise = 0.6f;

	// Token: 0x04001CCD RID: 7373
	private const float oceanBiomeMinDistance = 1000f;

	// Token: 0x04001CCE RID: 7374
	private const float oceanBiomeMinDistanceBuffer = 256f;

	// Token: 0x04001CCF RID: 7375
	private float m_minMountainDistance = 1000f;

	// Token: 0x04001CD0 RID: 7376
	private const float mountainBaseHeightMin = 0.4f;

	// Token: 0x04001CD1 RID: 7377
	private const float deepNorthMinDistance = 12000f;

	// Token: 0x04001CD2 RID: 7378
	private const float deepNorthYOffset = 4000f;

	// Token: 0x04001CD3 RID: 7379
	public static readonly float ashlandsMinDistance = 12000f;

	// Token: 0x04001CD4 RID: 7380
	public static readonly float ashlandsYOffset = -4000f;

	// Token: 0x04001CD5 RID: 7381
	public const float worldSize = 10000f;

	// Token: 0x04001CD6 RID: 7382
	public const float waterEdge = 10500f;

	// Token: 0x0200039D RID: 925
	public class River
	{
		// Token: 0x040026E7 RID: 9959
		public Vector2 p0;

		// Token: 0x040026E8 RID: 9960
		public Vector2 p1;

		// Token: 0x040026E9 RID: 9961
		public Vector2 center;

		// Token: 0x040026EA RID: 9962
		public float widthMin;

		// Token: 0x040026EB RID: 9963
		public float widthMax;

		// Token: 0x040026EC RID: 9964
		public float curveWidth;

		// Token: 0x040026ED RID: 9965
		public float curveWavelength;
	}

	// Token: 0x0200039E RID: 926
	public struct RiverPoint
	{
		// Token: 0x0600232C RID: 9004 RVA: 0x000F1695 File Offset: 0x000EF895
		public RiverPoint(Vector2 p_p, float p_w)
		{
			this.p = p_p;
			this.w = p_w;
			this.w2 = (float)((double)p_w * (double)p_w);
		}

		// Token: 0x040026EE RID: 9966
		public Vector2 p;

		// Token: 0x040026EF RID: 9967
		public float w;

		// Token: 0x040026F0 RID: 9968
		public float w2;
	}
}
