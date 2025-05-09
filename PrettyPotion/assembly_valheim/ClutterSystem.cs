using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

// Token: 0x02000163 RID: 355
public class ClutterSystem : MonoBehaviour
{
	// Token: 0x170000BE RID: 190
	// (get) Token: 0x06001582 RID: 5506 RVA: 0x0009E1AD File Offset: 0x0009C3AD
	public static ClutterSystem instance
	{
		get
		{
			return ClutterSystem.m_instance;
		}
	}

	// Token: 0x06001583 RID: 5507 RVA: 0x0009E1B4 File Offset: 0x0009C3B4
	private void Awake()
	{
		ClutterSystem.m_instance = this;
		if (SystemInfo.graphicsDeviceType == GraphicsDeviceType.Null)
		{
			return;
		}
		this.ApplySettings();
		this.m_placeRayMask = LayerMask.GetMask(new string[]
		{
			"terrain"
		});
		this.m_grassRoot = new GameObject("grassroot");
		this.m_grassRoot.transform.SetParent(base.transform);
	}

	// Token: 0x06001584 RID: 5508 RVA: 0x0009E218 File Offset: 0x0009C418
	public void ApplySettings()
	{
		ClutterSystem.Quality @int = (ClutterSystem.Quality)PlatformPrefs.GetInt("ClutterQuality", 3);
		if (this.m_quality == @int)
		{
			return;
		}
		this.m_quality = @int;
		this.ClearAll();
	}

	// Token: 0x06001585 RID: 5509 RVA: 0x0009E248 File Offset: 0x0009C448
	private void LateUpdate()
	{
		if (!RenderGroupSystem.IsGroupActive(RenderGroup.Overworld))
		{
			this.ClearAll();
			return;
		}
		Camera mainCamera = Utils.GetMainCamera();
		if (mainCamera == null)
		{
			return;
		}
		Vector3 center = (!GameCamera.InFreeFly() && Player.m_localPlayer) ? Player.m_localPlayer.transform.position : mainCamera.transform.position;
		if (this.m_forceRebuild)
		{
			if (this.IsHeightmapReady())
			{
				this.m_forceRebuild = false;
				this.UpdateGrass(Time.deltaTime, true, center);
			}
		}
		else if (this.IsHeightmapReady())
		{
			this.UpdateGrass(Time.deltaTime, false, center);
		}
		Player localPlayer = Player.m_localPlayer;
		if (localPlayer != null)
		{
			this.m_oldPlayerPos = Vector3.Lerp(this.m_oldPlayerPos, localPlayer.transform.position, this.m_playerPushFade);
			Shader.SetGlobalVector("_PlayerPosition", localPlayer.transform.position);
			Shader.SetGlobalVector("_PlayerOldPosition", this.m_oldPlayerPos);
			return;
		}
		Shader.SetGlobalVector("_PlayerPosition", new Vector3(999999f, 999999f, 999999f));
		Shader.SetGlobalVector("_PlayerOldPosition", new Vector3(999999f, 999999f, 999999f));
	}

	// Token: 0x06001586 RID: 5510 RVA: 0x0009E388 File Offset: 0x0009C588
	public Vector2Int GetVegPatch(Vector3 point)
	{
		int x = Mathf.FloorToInt((point.x + this.m_grassPatchSize / 2f) / this.m_grassPatchSize);
		int y = Mathf.FloorToInt((point.z + this.m_grassPatchSize / 2f) / this.m_grassPatchSize);
		return new Vector2Int(x, y);
	}

	// Token: 0x06001587 RID: 5511 RVA: 0x0009E3DA File Offset: 0x0009C5DA
	public Vector3 GetVegPatchCenter(Vector2Int p)
	{
		return new Vector3((float)p.x * this.m_grassPatchSize, 0f, (float)p.y * this.m_grassPatchSize);
	}

	// Token: 0x06001588 RID: 5512 RVA: 0x0009E404 File Offset: 0x0009C604
	private bool IsHeightmapReady()
	{
		Camera mainCamera = Utils.GetMainCamera();
		return mainCamera && !Heightmap.HaveQueuedRebuild(mainCamera.transform.position, this.m_distance);
	}

	// Token: 0x06001589 RID: 5513 RVA: 0x0009E43C File Offset: 0x0009C63C
	private void UpdateGrass(float dt, bool rebuildAll, Vector3 center)
	{
		if (this.m_quality == ClutterSystem.Quality.Off)
		{
			return;
		}
		this.GeneratePatches(rebuildAll, center);
		this.TimeoutPatches(dt);
	}

	// Token: 0x0600158A RID: 5514 RVA: 0x0009E458 File Offset: 0x0009C658
	private void GeneratePatches(bool rebuildAll, Vector3 center)
	{
		bool flag = false;
		Vector2Int vegPatch = this.GetVegPatch(center);
		this.GeneratePatch(center, vegPatch, ref flag, rebuildAll);
		int num = Mathf.CeilToInt((this.m_distance - this.m_grassPatchSize / 2f) / this.m_grassPatchSize);
		for (int i = 1; i <= num; i++)
		{
			for (int j = vegPatch.x - i; j <= vegPatch.x + i; j++)
			{
				this.GeneratePatch(center, new Vector2Int(j, vegPatch.y - i), ref flag, rebuildAll);
				this.GeneratePatch(center, new Vector2Int(j, vegPatch.y + i), ref flag, rebuildAll);
			}
			for (int k = vegPatch.y - i + 1; k <= vegPatch.y + i - 1; k++)
			{
				this.GeneratePatch(center, new Vector2Int(vegPatch.x - i, k), ref flag, rebuildAll);
				this.GeneratePatch(center, new Vector2Int(vegPatch.x + i, k), ref flag, rebuildAll);
			}
		}
	}

	// Token: 0x0600158B RID: 5515 RVA: 0x0009E558 File Offset: 0x0009C758
	private void GeneratePatch(Vector3 camPos, Vector2Int p, ref bool generated, bool rebuildAll)
	{
		if (Utils.DistanceXZ(this.GetVegPatchCenter(p), camPos) > this.m_distance)
		{
			return;
		}
		ClutterSystem.PatchData patchData;
		if (this.m_patches.TryGetValue(p, out patchData) && !patchData.m_reset)
		{
			patchData.m_timer = 0f;
			return;
		}
		if (rebuildAll || !generated || this.m_menuHack)
		{
			ClutterSystem.PatchData patchData2 = this.GenerateVegPatch(p, this.m_grassPatchSize);
			if (patchData2 != null)
			{
				ClutterSystem.PatchData patchData3;
				if (this.m_patches.TryGetValue(p, out patchData3))
				{
					foreach (GameObject obj in patchData3.m_objects)
					{
						UnityEngine.Object.Destroy(obj);
					}
					this.FreePatch(patchData3);
					this.m_patches.Remove(p);
				}
				this.m_patches.Add(p, patchData2);
				generated = true;
			}
		}
	}

	// Token: 0x0600158C RID: 5516 RVA: 0x0009E638 File Offset: 0x0009C838
	private void TimeoutPatches(float dt)
	{
		this.m_tempToRemovePair.Clear();
		foreach (KeyValuePair<Vector2Int, ClutterSystem.PatchData> item in this.m_patches)
		{
			item.Value.m_timer += dt;
			if (item.Value.m_timer >= 2f)
			{
				this.m_tempToRemovePair.Add(item);
			}
		}
		foreach (KeyValuePair<Vector2Int, ClutterSystem.PatchData> keyValuePair in this.m_tempToRemovePair)
		{
			foreach (GameObject obj in keyValuePair.Value.m_objects)
			{
				UnityEngine.Object.Destroy(obj);
			}
			this.m_patches.Remove(keyValuePair.Key);
			this.FreePatch(keyValuePair.Value);
		}
	}

	// Token: 0x0600158D RID: 5517 RVA: 0x0009E764 File Offset: 0x0009C964
	public void ClearAll()
	{
		foreach (KeyValuePair<Vector2Int, ClutterSystem.PatchData> keyValuePair in this.m_patches)
		{
			foreach (GameObject obj in keyValuePair.Value.m_objects)
			{
				UnityEngine.Object.Destroy(obj);
			}
			this.FreePatch(keyValuePair.Value);
		}
		this.m_patches.Clear();
		this.m_forceRebuild = true;
	}

	// Token: 0x0600158E RID: 5518 RVA: 0x0009E814 File Offset: 0x0009CA14
	public void ResetGrass(Vector3 center, float radius)
	{
		float num = this.m_grassPatchSize / 2f;
		foreach (KeyValuePair<Vector2Int, ClutterSystem.PatchData> keyValuePair in this.m_patches)
		{
			Vector3 center2 = keyValuePair.Value.center;
			if (center2.x + num >= center.x - radius && center2.x - num <= center.x + radius && center2.z + num >= center.z - radius && center2.z - num <= center.z + radius)
			{
				keyValuePair.Value.m_reset = true;
				this.m_forceRebuild = true;
			}
		}
	}

	// Token: 0x0600158F RID: 5519 RVA: 0x0009E8D8 File Offset: 0x0009CAD8
	public bool GetGroundInfo(Vector3 p, out Vector3 point, out Vector3 normal, out Heightmap hmap, out Heightmap.Biome biome)
	{
		RaycastHit raycastHit;
		if (Physics.Raycast(p + Vector3.up * 500f, Vector3.down, out raycastHit, 1000f, this.m_placeRayMask))
		{
			point = raycastHit.point;
			normal = raycastHit.normal;
			hmap = raycastHit.collider.GetComponent<Heightmap>();
			biome = hmap.GetBiome(point, 0.02f, false);
			return true;
		}
		point = p;
		normal = Vector3.up;
		hmap = null;
		biome = Heightmap.Biome.Meadows;
		return false;
	}

	// Token: 0x06001590 RID: 5520 RVA: 0x0009E970 File Offset: 0x0009CB70
	private Heightmap.Biome GetPatchBiomes(Vector3 center, float halfSize)
	{
		Heightmap.Biome biome = Heightmap.FindBiomeClutter(new Vector3(center.x - halfSize, 0f, center.z - halfSize));
		Heightmap.Biome biome2 = Heightmap.FindBiomeClutter(new Vector3(center.x + halfSize, 0f, center.z - halfSize));
		Heightmap.Biome biome3 = Heightmap.FindBiomeClutter(new Vector3(center.x - halfSize, 0f, center.z + halfSize));
		Heightmap.Biome biome4 = Heightmap.FindBiomeClutter(new Vector3(center.x + halfSize, 0f, center.z + halfSize));
		if (biome == Heightmap.Biome.None || biome2 == Heightmap.Biome.None || biome3 == Heightmap.Biome.None || biome4 == Heightmap.Biome.None)
		{
			return Heightmap.Biome.None;
		}
		return biome | biome2 | biome3 | biome4;
	}

	// Token: 0x06001591 RID: 5521 RVA: 0x0009EA14 File Offset: 0x0009CC14
	private ClutterSystem.PatchData GenerateVegPatch(Vector2Int patchID, float size)
	{
		Vector3 vegPatchCenter = this.GetVegPatchCenter(patchID);
		float num = size / 2f;
		Heightmap.Biome patchBiomes = this.GetPatchBiomes(vegPatchCenter, num);
		if (patchBiomes == Heightmap.Biome.None)
		{
			return null;
		}
		UnityEngine.Random.State state = UnityEngine.Random.state;
		ClutterSystem.PatchData patchData = this.AllocatePatch();
		patchData.center = vegPatchCenter;
		for (int i = 0; i < this.m_clutter.Count; i++)
		{
			ClutterSystem.Clutter clutter = this.m_clutter[i];
			if (clutter.m_enabled && (patchBiomes & clutter.m_biome) != Heightmap.Biome.None)
			{
				InstanceRenderer instanceRenderer = null;
				UnityEngine.Random.InitState(patchID.x * (patchID.y * 1374) + i * 9321);
				Vector3 b = new Vector3(clutter.m_fractalOffset, 0f, 0f);
				float num2 = Mathf.Cos(0.017453292f * clutter.m_maxTilt);
				float num3 = Mathf.Cos(0.017453292f * clutter.m_minTilt);
				ClutterSystem.Quality quality = this.m_quality;
				int num4;
				if (quality != ClutterSystem.Quality.Low)
				{
					if (quality != ClutterSystem.Quality.Med)
					{
						num4 = clutter.m_amount;
					}
					else
					{
						num4 = clutter.m_amount / 2;
					}
				}
				else
				{
					num4 = clutter.m_amount / 4;
				}
				num4 = (int)((float)num4 * this.m_amountScale);
				int j = 0;
				while (j < num4)
				{
					Vector3 vector = new Vector3(UnityEngine.Random.Range(vegPatchCenter.x - num, vegPatchCenter.x + num), 0f, UnityEngine.Random.Range(vegPatchCenter.z - num, vegPatchCenter.z + num));
					float num5 = (float)UnityEngine.Random.Range(0, 360);
					if (!clutter.m_inForest)
					{
						goto IL_189;
					}
					float forestFactor = WorldGenerator.GetForestFactor(vector);
					if (forestFactor >= clutter.m_forestTresholdMin && forestFactor <= clutter.m_forestTresholdMax)
					{
						goto IL_189;
					}
					IL_442:
					j++;
					continue;
					IL_189:
					if (clutter.m_fractalScale > 0f)
					{
						float num6 = Utils.Fbm(vector * 0.01f * clutter.m_fractalScale + b, 3, 1.6f, 0.7f);
						if (num6 < clutter.m_fractalTresholdMin || num6 > clutter.m_fractalTresholdMax)
						{
							goto IL_442;
						}
					}
					Vector3 vector2;
					Vector3 vector3;
					Heightmap heightmap;
					Heightmap.Biome biome;
					if (!this.GetGroundInfo(vector, out vector2, out vector3, out heightmap, out biome) || (clutter.m_biome & biome) == Heightmap.Biome.None)
					{
						goto IL_442;
					}
					float num7 = vector2.y - this.m_waterLevel;
					if (num7 < clutter.m_minAlt || num7 > clutter.m_maxAlt || vector3.y < num2 || vector3.y > num3)
					{
						goto IL_442;
					}
					if (clutter.m_minOceanDepth != clutter.m_maxOceanDepth)
					{
						float oceanDepth = heightmap.GetOceanDepth(vector);
						if (oceanDepth < clutter.m_minOceanDepth || oceanDepth > clutter.m_maxOceanDepth)
						{
							goto IL_442;
						}
					}
					if (clutter.m_minVegetation != clutter.m_maxVegetation)
					{
						float vegetationMask = heightmap.GetVegetationMask(vector2);
						if (vegetationMask > clutter.m_maxVegetation || vegetationMask < clutter.m_minVegetation)
						{
							goto IL_442;
						}
					}
					if (!clutter.m_onCleared || !clutter.m_onUncleared)
					{
						bool flag = heightmap.IsCleared(vector2);
						if ((clutter.m_onCleared && !flag) || (clutter.m_onUncleared && flag))
						{
							goto IL_442;
						}
					}
					vector = vector2;
					if (clutter.m_snapToWater)
					{
						vector.y = this.m_waterLevel;
					}
					if (clutter.m_randomOffset != 0f)
					{
						vector.y += UnityEngine.Random.Range(-clutter.m_randomOffset, clutter.m_randomOffset);
					}
					Quaternion quaternion = Quaternion.identity;
					if (clutter.m_terrainTilt)
					{
						quaternion = Quaternion.AngleAxis(num5, vector3);
					}
					else
					{
						quaternion = Quaternion.Euler(0f, num5, 0f);
					}
					if (clutter.m_instanced)
					{
						if (instanceRenderer == null)
						{
							GameObject gameObject = UnityEngine.Object.Instantiate<GameObject>(clutter.m_prefab, vegPatchCenter, Quaternion.identity, this.m_grassRoot.transform);
							instanceRenderer = gameObject.GetComponent<InstanceRenderer>();
							if (instanceRenderer.m_lodMaxDistance > this.m_distance - this.m_grassPatchSize / 2f)
							{
								instanceRenderer.m_lodMaxDistance = this.m_distance - this.m_grassPatchSize / 2f;
							}
							patchData.m_objects.Add(gameObject);
						}
						float scale = UnityEngine.Random.Range(clutter.m_scaleMin, clutter.m_scaleMax);
						instanceRenderer.AddInstance(vector, quaternion, scale);
						goto IL_442;
					}
					GameObject item = UnityEngine.Object.Instantiate<GameObject>(clutter.m_prefab, vector, quaternion, this.m_grassRoot.transform);
					patchData.m_objects.Add(item);
					goto IL_442;
				}
			}
		}
		UnityEngine.Random.state = state;
		return patchData;
	}

	// Token: 0x06001592 RID: 5522 RVA: 0x0009EE92 File Offset: 0x0009D092
	private ClutterSystem.PatchData AllocatePatch()
	{
		if (this.m_freePatches.Count > 0)
		{
			return this.m_freePatches.Pop();
		}
		return new ClutterSystem.PatchData();
	}

	// Token: 0x06001593 RID: 5523 RVA: 0x0009EEB3 File Offset: 0x0009D0B3
	private void FreePatch(ClutterSystem.PatchData patch)
	{
		patch.center = Vector3.zero;
		patch.m_objects.Clear();
		patch.m_timer = 0f;
		patch.m_reset = false;
		this.m_freePatches.Push(patch);
	}

	// Token: 0x0400152A RID: 5418
	private static ClutterSystem m_instance;

	// Token: 0x0400152B RID: 5419
	private int m_placeRayMask;

	// Token: 0x0400152C RID: 5420
	public List<ClutterSystem.Clutter> m_clutter = new List<ClutterSystem.Clutter>();

	// Token: 0x0400152D RID: 5421
	public float m_grassPatchSize = 8f;

	// Token: 0x0400152E RID: 5422
	public float m_distance = 40f;

	// Token: 0x0400152F RID: 5423
	public float m_waterLevel = 27f;

	// Token: 0x04001530 RID: 5424
	public float m_playerPushFade = 0.05f;

	// Token: 0x04001531 RID: 5425
	public float m_amountScale = 1f;

	// Token: 0x04001532 RID: 5426
	public bool m_menuHack;

	// Token: 0x04001533 RID: 5427
	private Dictionary<Vector2Int, ClutterSystem.PatchData> m_patches = new Dictionary<Vector2Int, ClutterSystem.PatchData>();

	// Token: 0x04001534 RID: 5428
	private Stack<ClutterSystem.PatchData> m_freePatches = new Stack<ClutterSystem.PatchData>();

	// Token: 0x04001535 RID: 5429
	private GameObject m_grassRoot;

	// Token: 0x04001536 RID: 5430
	private Vector3 m_oldPlayerPos = Vector3.zero;

	// Token: 0x04001537 RID: 5431
	private List<Vector2Int> m_tempToRemove = new List<Vector2Int>();

	// Token: 0x04001538 RID: 5432
	private List<KeyValuePair<Vector2Int, ClutterSystem.PatchData>> m_tempToRemovePair = new List<KeyValuePair<Vector2Int, ClutterSystem.PatchData>>();

	// Token: 0x04001539 RID: 5433
	private ClutterSystem.Quality m_quality = ClutterSystem.Quality.High;

	// Token: 0x0400153A RID: 5434
	private bool m_forceRebuild;

	// Token: 0x0200034A RID: 842
	[Serializable]
	public class Clutter
	{
		// Token: 0x0400252D RID: 9517
		public string m_name = "";

		// Token: 0x0400252E RID: 9518
		public bool m_enabled = true;

		// Token: 0x0400252F RID: 9519
		[BitMask(typeof(Heightmap.Biome))]
		public Heightmap.Biome m_biome;

		// Token: 0x04002530 RID: 9520
		public bool m_instanced;

		// Token: 0x04002531 RID: 9521
		public GameObject m_prefab;

		// Token: 0x04002532 RID: 9522
		public int m_amount = 80;

		// Token: 0x04002533 RID: 9523
		public bool m_onUncleared = true;

		// Token: 0x04002534 RID: 9524
		public bool m_onCleared;

		// Token: 0x04002535 RID: 9525
		public float m_minVegetation;

		// Token: 0x04002536 RID: 9526
		public float m_maxVegetation;

		// Token: 0x04002537 RID: 9527
		public float m_scaleMin = 1f;

		// Token: 0x04002538 RID: 9528
		public float m_scaleMax = 1f;

		// Token: 0x04002539 RID: 9529
		public float m_maxTilt = 18f;

		// Token: 0x0400253A RID: 9530
		public float m_minTilt;

		// Token: 0x0400253B RID: 9531
		public float m_maxAlt = 1000f;

		// Token: 0x0400253C RID: 9532
		public float m_minAlt = 27f;

		// Token: 0x0400253D RID: 9533
		public bool m_snapToWater;

		// Token: 0x0400253E RID: 9534
		public bool m_terrainTilt;

		// Token: 0x0400253F RID: 9535
		public float m_randomOffset;

		// Token: 0x04002540 RID: 9536
		[Header("Ocean depth ")]
		public float m_minOceanDepth;

		// Token: 0x04002541 RID: 9537
		public float m_maxOceanDepth;

		// Token: 0x04002542 RID: 9538
		[Header("Forest fractal 0-1 inside forest")]
		public bool m_inForest;

		// Token: 0x04002543 RID: 9539
		public float m_forestTresholdMin;

		// Token: 0x04002544 RID: 9540
		public float m_forestTresholdMax = 1f;

		// Token: 0x04002545 RID: 9541
		[Header("Fractal placement (m_fractalScale > 0 == enabled) ")]
		public float m_fractalScale;

		// Token: 0x04002546 RID: 9542
		public float m_fractalOffset;

		// Token: 0x04002547 RID: 9543
		public float m_fractalTresholdMin = 0.5f;

		// Token: 0x04002548 RID: 9544
		public float m_fractalTresholdMax = 1f;
	}

	// Token: 0x0200034B RID: 843
	private class PatchData
	{
		// Token: 0x04002549 RID: 9545
		public Vector3 center;

		// Token: 0x0400254A RID: 9546
		public List<GameObject> m_objects = new List<GameObject>();

		// Token: 0x0400254B RID: 9547
		public float m_timer;

		// Token: 0x0400254C RID: 9548
		public bool m_reset;
	}

	// Token: 0x0200034C RID: 844
	public enum Quality
	{
		// Token: 0x0400254E RID: 9550
		Off,
		// Token: 0x0400254F RID: 9551
		Low,
		// Token: 0x04002550 RID: 9552
		Med,
		// Token: 0x04002551 RID: 9553
		High
	}
}
