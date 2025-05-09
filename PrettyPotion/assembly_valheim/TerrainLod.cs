using System;
using System.Collections.Generic;
using UnityEngine;

// Token: 0x0200014D RID: 333
public class TerrainLod : MonoBehaviour
{
	// Token: 0x0600143C RID: 5180 RVA: 0x00094CBD File Offset: 0x00092EBD
	private void OnEnable()
	{
		this.CreateMeshes();
	}

	// Token: 0x0600143D RID: 5181 RVA: 0x00094CC5 File Offset: 0x00092EC5
	private void OnDisable()
	{
		this.ResetMeshes();
	}

	// Token: 0x0600143E RID: 5182 RVA: 0x00094CD0 File Offset: 0x00092ED0
	private void CreateMeshes()
	{
		float num = this.m_terrainSize / (float)this.m_regionsPerAxis;
		float num2 = Mathf.Round(this.m_vertexDistance);
		int width = Mathf.RoundToInt(num / num2);
		for (int i = 0; i < this.m_regionsPerAxis; i++)
		{
			for (int j = 0; j < this.m_regionsPerAxis; j++)
			{
				Vector3 offset = new Vector3(((float)i * 2f - (float)this.m_regionsPerAxis + 1f) * this.m_terrainSize * 0.5f / (float)this.m_regionsPerAxis, 0f, ((float)j * 2f - (float)this.m_regionsPerAxis + 1f) * this.m_terrainSize * 0.5f / (float)this.m_regionsPerAxis);
				this.CreateMesh(num2, width, offset);
			}
		}
	}

	// Token: 0x0600143F RID: 5183 RVA: 0x00094D94 File Offset: 0x00092F94
	private void CreateMesh(float scale, int width, Vector3 offset)
	{
		GameObject gameObject = new GameObject("lodMesh");
		gameObject.transform.position = offset;
		gameObject.transform.SetParent(base.transform);
		Heightmap heightmap = gameObject.AddComponent<Heightmap>();
		this.m_heightmaps.Add(new TerrainLod.HeightmapWithOffset(heightmap, offset));
		heightmap.m_scale = scale;
		heightmap.m_width = width;
		heightmap.m_material = this.m_material;
		heightmap.IsDistantLod = true;
		heightmap.enabled = true;
	}

	// Token: 0x06001440 RID: 5184 RVA: 0x00094E08 File Offset: 0x00093008
	private void ResetMeshes()
	{
		for (int i = 0; i < this.m_heightmaps.Count; i++)
		{
			UnityEngine.Object.Destroy(this.m_heightmaps[i].m_heightmap.gameObject);
		}
		this.m_heightmaps.Clear();
		this.m_lastPoint = new Vector3(99999f, 0f, 99999f);
		this.m_heightmapState = TerrainLod.HeightmapState.Done;
	}

	// Token: 0x06001441 RID: 5185 RVA: 0x00094E72 File Offset: 0x00093072
	private void Update()
	{
		this.UpdateHeightmaps();
	}

	// Token: 0x06001442 RID: 5186 RVA: 0x00094E7A File Offset: 0x0009307A
	private void UpdateHeightmaps()
	{
		if (ZNet.GetConnectionStatus() != ZNet.ConnectionStatus.Connected)
		{
			return;
		}
		if (!this.NeedsRebuild())
		{
			return;
		}
		if (!this.IsAllTerrainReady())
		{
			return;
		}
		this.RebuildAllHeightmaps();
	}

	// Token: 0x06001443 RID: 5187 RVA: 0x00094EA0 File Offset: 0x000930A0
	private void RebuildAllHeightmaps()
	{
		for (int i = 0; i < this.m_heightmaps.Count; i++)
		{
			this.RebuildHeightmap(this.m_heightmaps[i]);
		}
		this.m_heightmapState = TerrainLod.HeightmapState.Done;
	}

	// Token: 0x06001444 RID: 5188 RVA: 0x00094EDC File Offset: 0x000930DC
	private bool IsAllTerrainReady()
	{
		int num = 0;
		for (int i = 0; i < this.m_heightmaps.Count; i++)
		{
			if (this.IsTerrainReady(this.m_heightmaps[i]))
			{
				num++;
			}
		}
		return num == this.m_heightmaps.Count;
	}

	// Token: 0x06001445 RID: 5189 RVA: 0x00094F28 File Offset: 0x00093128
	private bool IsTerrainReady(TerrainLod.HeightmapWithOffset heightmapWithOffset)
	{
		Heightmap heightmap = heightmapWithOffset.m_heightmap;
		Vector3 offset = heightmapWithOffset.m_offset;
		if (heightmapWithOffset.m_state == TerrainLod.HeightmapState.ReadyToRebuild)
		{
			return true;
		}
		if (HeightmapBuilder.instance.IsTerrainReady(this.m_lastPoint + offset, heightmap.m_width, heightmap.m_scale, heightmap.IsDistantLod, WorldGenerator.instance))
		{
			heightmapWithOffset.m_state = TerrainLod.HeightmapState.ReadyToRebuild;
			return true;
		}
		return false;
	}

	// Token: 0x06001446 RID: 5190 RVA: 0x00094F88 File Offset: 0x00093188
	private void RebuildHeightmap(TerrainLod.HeightmapWithOffset heightmapWithOffset)
	{
		Heightmap heightmap = heightmapWithOffset.m_heightmap;
		Vector3 offset = heightmapWithOffset.m_offset;
		heightmap.transform.position = this.m_lastPoint + offset;
		heightmap.Regenerate();
		heightmapWithOffset.m_state = TerrainLod.HeightmapState.Done;
	}

	// Token: 0x06001447 RID: 5191 RVA: 0x00094FC8 File Offset: 0x000931C8
	private bool NeedsRebuild()
	{
		if (this.m_heightmapState == TerrainLod.HeightmapState.NeedsRebuild)
		{
			return true;
		}
		Camera mainCamera = Utils.GetMainCamera();
		if (mainCamera == null)
		{
			return false;
		}
		Vector3 position = mainCamera.transform.position;
		if (Utils.DistanceXZ(position, this.m_lastPoint) > this.m_updateStepDistance && this.m_heightmapState == TerrainLod.HeightmapState.Done)
		{
			for (int i = 0; i < this.m_heightmaps.Count; i++)
			{
				this.m_heightmaps[i].m_state = TerrainLod.HeightmapState.NeedsRebuild;
			}
			this.m_lastPoint = new Vector3(Mathf.Round(position.x / this.m_vertexDistance) * this.m_vertexDistance, 0f, Mathf.Round(position.z / this.m_vertexDistance) * this.m_vertexDistance);
			this.m_heightmapState = TerrainLod.HeightmapState.NeedsRebuild;
			return true;
		}
		return false;
	}

	// Token: 0x040013F9 RID: 5113
	[SerializeField]
	private float m_updateStepDistance = 256f;

	// Token: 0x040013FA RID: 5114
	[SerializeField]
	private float m_terrainSize = 2400f;

	// Token: 0x040013FB RID: 5115
	[SerializeField]
	private int m_regionsPerAxis = 3;

	// Token: 0x040013FC RID: 5116
	[SerializeField]
	private float m_vertexDistance = 10f;

	// Token: 0x040013FD RID: 5117
	[SerializeField]
	private Material m_material;

	// Token: 0x040013FE RID: 5118
	private List<TerrainLod.HeightmapWithOffset> m_heightmaps = new List<TerrainLod.HeightmapWithOffset>();

	// Token: 0x040013FF RID: 5119
	private Vector3 m_lastPoint = new Vector3(99999f, 0f, 99999f);

	// Token: 0x04001400 RID: 5120
	private TerrainLod.HeightmapState m_heightmapState = TerrainLod.HeightmapState.Done;

	// Token: 0x02000335 RID: 821
	private enum HeightmapState
	{
		// Token: 0x04002480 RID: 9344
		NeedsRebuild,
		// Token: 0x04002481 RID: 9345
		ReadyToRebuild,
		// Token: 0x04002482 RID: 9346
		Done
	}

	// Token: 0x02000336 RID: 822
	private class HeightmapWithOffset
	{
		// Token: 0x0600226A RID: 8810 RVA: 0x000EDBF7 File Offset: 0x000EBDF7
		public HeightmapWithOffset(Heightmap heightmap, Vector3 offset)
		{
			this.m_heightmap = heightmap;
			this.m_offset = offset;
			this.m_state = TerrainLod.HeightmapState.NeedsRebuild;
		}

		// Token: 0x04002483 RID: 9347
		public Heightmap m_heightmap;

		// Token: 0x04002484 RID: 9348
		public Vector3 m_offset;

		// Token: 0x04002485 RID: 9349
		public TerrainLod.HeightmapState m_state;
	}
}
