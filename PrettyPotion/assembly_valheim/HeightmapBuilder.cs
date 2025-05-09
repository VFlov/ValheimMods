using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using UnityEngine;

// Token: 0x02000183 RID: 387
public class HeightmapBuilder
{
	// Token: 0x170000C6 RID: 198
	// (get) Token: 0x0600176D RID: 5997 RVA: 0x000AE024 File Offset: 0x000AC224
	public static HeightmapBuilder instance
	{
		get
		{
			if (HeightmapBuilder.hasBeenDisposed)
			{
				ZLog.LogWarning("Tried to get instance of heightmap builder after heightmap builder has been disposed!");
				return null;
			}
			if (HeightmapBuilder.m_instance == null)
			{
				HeightmapBuilder.m_instance = new HeightmapBuilder();
			}
			return HeightmapBuilder.m_instance;
		}
	}

	// Token: 0x0600176E RID: 5998 RVA: 0x000AE050 File Offset: 0x000AC250
	private HeightmapBuilder()
	{
		HeightmapBuilder.m_instance = this;
		this.m_builder = new Thread(new ThreadStart(this.BuildThread));
		this.m_builder.Start();
	}

	// Token: 0x0600176F RID: 5999 RVA: 0x000AE0AC File Offset: 0x000AC2AC
	public void Dispose()
	{
		if (HeightmapBuilder.hasBeenDisposed)
		{
			return;
		}
		HeightmapBuilder.hasBeenDisposed = true;
		if (this.m_builder != null)
		{
			ZLog.Log("Stopping build thread");
			this.m_lock.WaitOne();
			this.m_stop = true;
			this.m_lock.ReleaseMutex();
			this.m_builder.Join();
			this.m_builder = null;
		}
		if (this.m_lock != null)
		{
			this.m_lock.Close();
			this.m_lock = null;
		}
	}

	// Token: 0x06001770 RID: 6000 RVA: 0x000AE124 File Offset: 0x000AC324
	private void BuildThread()
	{
		ZLog.Log("Builder started");
		bool flag = false;
		while (!flag)
		{
			this.m_lock.WaitOne();
			bool flag2 = this.m_toBuild.Count > 0;
			this.m_lock.ReleaseMutex();
			if (flag2)
			{
				this.m_lock.WaitOne();
				HeightmapBuilder.HMBuildData hmbuildData = this.m_toBuild[0];
				this.m_lock.ReleaseMutex();
				new Stopwatch().Start();
				this.Build(hmbuildData);
				this.m_lock.WaitOne();
				this.m_toBuild.Remove(hmbuildData);
				this.m_ready.Add(hmbuildData);
				while (this.m_ready.Count > 16)
				{
					this.m_ready.RemoveAt(0);
				}
				this.m_lock.ReleaseMutex();
			}
			Thread.Sleep(10);
			this.m_lock.WaitOne();
			flag = this.m_stop;
			this.m_lock.ReleaseMutex();
		}
	}

	// Token: 0x06001771 RID: 6001 RVA: 0x000AE21C File Offset: 0x000AC41C
	private void Build(HeightmapBuilder.HMBuildData data)
	{
		int num = data.m_width + 1;
		int num2 = num * num;
		Vector3 vector = data.m_center + new Vector3((float)data.m_width * data.m_scale * -0.5f, 0f, (float)data.m_width * data.m_scale * -0.5f);
		WorldGenerator worldGen = data.m_worldGen;
		data.m_cornerBiomes = new Heightmap.Biome[4];
		data.m_cornerBiomes[0] = worldGen.GetBiome(vector.x, vector.z, 0.02f, false);
		data.m_cornerBiomes[1] = worldGen.GetBiome((float)((double)vector.x + (double)data.m_width * (double)data.m_scale), vector.z, 0.02f, false);
		data.m_cornerBiomes[2] = worldGen.GetBiome(vector.x, (float)((double)vector.z + (double)data.m_width * (double)data.m_scale), 0.02f, false);
		data.m_cornerBiomes[3] = worldGen.GetBiome((float)((double)vector.x + (double)data.m_width * (double)data.m_scale), (float)((double)vector.z + (double)data.m_width * (double)data.m_scale), 0.02f, false);
		Heightmap.Biome biome = data.m_cornerBiomes[0];
		Heightmap.Biome biome2 = data.m_cornerBiomes[1];
		Heightmap.Biome biome3 = data.m_cornerBiomes[2];
		Heightmap.Biome biome4 = data.m_cornerBiomes[3];
		data.m_baseHeights = new List<float>(num * num);
		for (int i = 0; i < num2; i++)
		{
			data.m_baseHeights.Add(0f);
		}
		int num3 = num * num;
		data.m_baseMask = new Color[num3];
		for (int j = 0; j < num3; j++)
		{
			data.m_baseMask[j] = new Color(0f, 0f, 0f, 0f);
		}
		for (int k = 0; k < num; k++)
		{
			float wy = (float)((double)vector.z + (double)k * (double)data.m_scale);
			float t = DUtils.SmoothStep(0f, 1f, (float)((double)k / (double)data.m_width));
			for (int l = 0; l < num; l++)
			{
				float wx = (float)((double)vector.x + (double)l * (double)data.m_scale);
				float t2 = DUtils.SmoothStep(0f, 1f, (float)((double)l / (double)data.m_width));
				Color color = Color.black;
				float value;
				if (data.m_distantLod)
				{
					Heightmap.Biome biome5 = worldGen.GetBiome(wx, wy, 0.02f, false);
					value = worldGen.GetBiomeHeight(biome5, wx, wy, out color, false);
				}
				else if (biome3 == biome && biome2 == biome && biome4 == biome)
				{
					value = worldGen.GetBiomeHeight(biome, wx, wy, out color, false);
				}
				else
				{
					Color[] array = new Color[4];
					float biomeHeight = worldGen.GetBiomeHeight(biome, wx, wy, out array[0], false);
					float biomeHeight2 = worldGen.GetBiomeHeight(biome2, wx, wy, out array[1], false);
					float biomeHeight3 = worldGen.GetBiomeHeight(biome3, wx, wy, out array[2], false);
					float biomeHeight4 = worldGen.GetBiomeHeight(biome4, wx, wy, out array[3], false);
					float a = DUtils.Lerp(biomeHeight, biomeHeight2, t2);
					float b = DUtils.Lerp(biomeHeight3, biomeHeight4, t2);
					value = DUtils.Lerp(a, b, t);
					Color a2 = Color.Lerp(array[0], array[1], t2);
					Color b2 = Color.Lerp(array[2], array[3], t2);
					color = Color.Lerp(a2, b2, t);
				}
				data.m_baseHeights[k * num + l] = value;
				data.m_baseMask[k * num + l] = color;
			}
		}
		if (data.m_distantLod)
		{
			for (int m = 0; m < 4; m++)
			{
				List<float> list = new List<float>(data.m_baseHeights);
				for (int n = 1; n < num - 1; n++)
				{
					for (int num4 = 1; num4 < num - 1; num4++)
					{
						float num5 = list[n * num + num4];
						float num6 = list[(n - 1) * num + num4];
						float num7 = list[(n + 1) * num + num4];
						float num8 = list[n * num + num4 - 1];
						float num9 = list[n * num + num4 + 1];
						if (Mathf.Abs(num5 - num6) > 10f)
						{
							num5 = (num5 + num6) * 0.5f;
						}
						if (Mathf.Abs(num5 - num7) > 10f)
						{
							num5 = (num5 + num7) * 0.5f;
						}
						if (Mathf.Abs(num5 - num8) > 10f)
						{
							num5 = (num5 + num8) * 0.5f;
						}
						if (Mathf.Abs(num5 - num9) > 10f)
						{
							num5 = (num5 + num9) * 0.5f;
						}
						data.m_baseHeights[n * num + num4] = num5;
					}
				}
			}
		}
	}

	// Token: 0x06001772 RID: 6002 RVA: 0x000AE710 File Offset: 0x000AC910
	public HeightmapBuilder.HMBuildData RequestTerrainSync(Vector3 center, int width, float scale, bool distantLod, WorldGenerator worldGen)
	{
		HeightmapBuilder.HMBuildData hmbuildData;
		do
		{
			hmbuildData = this.RequestTerrain(center, width, scale, distantLod, worldGen);
		}
		while (hmbuildData == null);
		return hmbuildData;
	}

	// Token: 0x06001773 RID: 6003 RVA: 0x000AE730 File Offset: 0x000AC930
	private HeightmapBuilder.HMBuildData RequestTerrain(Vector3 center, int width, float scale, bool distantLod, WorldGenerator worldGen)
	{
		this.m_lock.WaitOne();
		for (int i = 0; i < this.m_ready.Count; i++)
		{
			HeightmapBuilder.HMBuildData hmbuildData = this.m_ready[i];
			if (hmbuildData.IsEqual(center, width, scale, distantLod, worldGen))
			{
				this.m_ready.RemoveAt(i);
				this.m_lock.ReleaseMutex();
				return hmbuildData;
			}
		}
		for (int j = 0; j < this.m_toBuild.Count; j++)
		{
			if (this.m_toBuild[j].IsEqual(center, width, scale, distantLod, worldGen))
			{
				this.m_lock.ReleaseMutex();
				return null;
			}
		}
		this.m_toBuild.Add(new HeightmapBuilder.HMBuildData(center, width, scale, distantLod, worldGen));
		this.m_lock.ReleaseMutex();
		return null;
	}

	// Token: 0x06001774 RID: 6004 RVA: 0x000AE7F4 File Offset: 0x000AC9F4
	public bool IsTerrainReady(Vector3 center, int width, float scale, bool distantLod, WorldGenerator worldGen)
	{
		this.m_lock.WaitOne();
		for (int i = 0; i < this.m_ready.Count; i++)
		{
			if (this.m_ready[i].IsEqual(center, width, scale, distantLod, worldGen))
			{
				this.m_lock.ReleaseMutex();
				return true;
			}
		}
		for (int j = 0; j < this.m_toBuild.Count; j++)
		{
			if (this.m_toBuild[j].IsEqual(center, width, scale, distantLod, worldGen))
			{
				this.m_lock.ReleaseMutex();
				return false;
			}
		}
		this.m_toBuild.Add(new HeightmapBuilder.HMBuildData(center, width, scale, distantLod, worldGen));
		this.m_lock.ReleaseMutex();
		return false;
	}

	// Token: 0x04001746 RID: 5958
	private static bool hasBeenDisposed;

	// Token: 0x04001747 RID: 5959
	private static HeightmapBuilder m_instance;

	// Token: 0x04001748 RID: 5960
	private const int m_maxReadyQueue = 16;

	// Token: 0x04001749 RID: 5961
	private List<HeightmapBuilder.HMBuildData> m_toBuild = new List<HeightmapBuilder.HMBuildData>();

	// Token: 0x0400174A RID: 5962
	private List<HeightmapBuilder.HMBuildData> m_ready = new List<HeightmapBuilder.HMBuildData>();

	// Token: 0x0400174B RID: 5963
	private Thread m_builder;

	// Token: 0x0400174C RID: 5964
	private Mutex m_lock = new Mutex();

	// Token: 0x0400174D RID: 5965
	private bool m_stop;

	// Token: 0x02000362 RID: 866
	public class HMBuildData
	{
		// Token: 0x060022B7 RID: 8887 RVA: 0x000EF2BE File Offset: 0x000ED4BE
		public HMBuildData(Vector3 center, int width, float scale, bool distantLod, WorldGenerator worldGen)
		{
			this.m_center = center;
			this.m_width = width;
			this.m_scale = scale;
			this.m_distantLod = distantLod;
			this.m_worldGen = worldGen;
		}

		// Token: 0x060022B8 RID: 8888 RVA: 0x000EF2EB File Offset: 0x000ED4EB
		public bool IsEqual(Vector3 center, int width, float scale, bool distantLod, WorldGenerator worldGen)
		{
			return this.m_center == center && this.m_width == width && this.m_scale == scale && this.m_distantLod == distantLod && this.m_worldGen == worldGen;
		}

		// Token: 0x040025B3 RID: 9651
		public Vector3 m_center;

		// Token: 0x040025B4 RID: 9652
		public int m_width;

		// Token: 0x040025B5 RID: 9653
		public float m_scale;

		// Token: 0x040025B6 RID: 9654
		public bool m_distantLod;

		// Token: 0x040025B7 RID: 9655
		public bool m_menu;

		// Token: 0x040025B8 RID: 9656
		public WorldGenerator m_worldGen;

		// Token: 0x040025B9 RID: 9657
		public Heightmap.Biome[] m_cornerBiomes;

		// Token: 0x040025BA RID: 9658
		public List<float> m_baseHeights;

		// Token: 0x040025BB RID: 9659
		public Color[] m_baseMask;
	}
}
