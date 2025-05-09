using System;
using System.Collections.Generic;
using UnityEngine;

// Token: 0x02000066 RID: 102
public class SmokeRenderer : MonoBehaviour
{
	// Token: 0x060006AD RID: 1709 RVA: 0x00037E62 File Offset: 0x00036062
	private void Awake()
	{
		if (SmokeRenderer.Instance == null)
		{
			SmokeRenderer.Instance = this;
			return;
		}
		UnityEngine.Object.Destroy(base.gameObject);
	}

	// Token: 0x060006AE RID: 1710 RVA: 0x00037E83 File Offset: 0x00036083
	public void RegisterSmoke(Smoke smoke)
	{
		this.AddSmokeToChunk(this.PositionToChunk(smoke.transform.position), smoke);
	}

	// Token: 0x060006AF RID: 1711 RVA: 0x00037E9D File Offset: 0x0003609D
	public void UnregisterSmoke(Smoke smoke)
	{
		this.RemoveSmokeFromChunk(smoke.RenderChunk, smoke);
	}

	// Token: 0x060006B0 RID: 1712 RVA: 0x00037EAC File Offset: 0x000360AC
	private Vector3Int PositionToChunk(Vector3 pos)
	{
		int x = Mathf.FloorToInt(pos.x / this.m_chunkSize);
		int y = Mathf.FloorToInt(pos.y / this.m_chunkSize);
		int z = Mathf.FloorToInt(pos.z / this.m_chunkSize);
		return new Vector3Int(x, y, z);
	}

	// Token: 0x060006B1 RID: 1713 RVA: 0x00037EF8 File Offset: 0x000360F8
	private Vector3 ChunkToWorld(Vector3Int chunk)
	{
		return new Vector3((float)chunk.x * this.m_chunkSize, (float)chunk.y * this.m_chunkSize, (float)chunk.z * this.m_chunkSize);
	}

	// Token: 0x060006B2 RID: 1714 RVA: 0x00037F2C File Offset: 0x0003612C
	private void AddSmokeToChunk(Vector3Int chunk, Smoke smoke)
	{
		if (!this.m_chunkedSmoke.ContainsKey(chunk))
		{
			this.m_chunkedSmoke.Add(chunk, new List<Smoke>());
			this.m_chunkedParticleSystems.Add(chunk, UnityEngine.Object.Instantiate<ParticleSystem>(this._particleSystemPrefab, this.ChunkToWorld(chunk), Quaternion.identity));
			this.m_chunkedParticles.Add(chunk, new ParticleSystem.Particle[100]);
		}
		if (!this.m_chunkedSmoke[chunk].Contains(smoke))
		{
			this.m_chunkedSmoke[chunk].Add(smoke);
		}
		smoke.RenderChunk = chunk;
	}

	// Token: 0x060006B3 RID: 1715 RVA: 0x00037FBB File Offset: 0x000361BB
	private void RemoveSmokeFromChunk(Vector3Int chunk, Smoke smoke)
	{
		if (this.m_chunkedSmoke.ContainsKey(chunk))
		{
			this.m_chunkedSmoke[chunk].Remove(smoke);
			if (this.m_chunkedSmoke[chunk].Count == 0)
			{
				this.CleanupChunk(chunk);
			}
		}
	}

	// Token: 0x060006B4 RID: 1716 RVA: 0x00037FF8 File Offset: 0x000361F8
	private void CleanupChunk(Vector3Int chunk)
	{
		this.m_chunkedParticles.Remove(chunk);
		this.m_chunkedSmoke.Remove(chunk);
		ParticleSystem particleSystem;
		this.m_chunkedParticleSystems.Remove(chunk, out particleSystem);
		if (particleSystem != null)
		{
			UnityEngine.Object.Destroy(particleSystem.gameObject);
		}
	}

	// Token: 0x060006B5 RID: 1717 RVA: 0x00038044 File Offset: 0x00036244
	private void TransferSmokeBetweenChunks()
	{
		this.m_chunkedSmokeToMove.Clear();
		foreach (Vector3Int vector3Int in this.m_chunkedSmoke.Keys)
		{
			foreach (Smoke smoke in this.m_chunkedSmoke[vector3Int])
			{
				Vector3Int vector3Int2 = this.PositionToChunk(smoke.transform.position);
				if (vector3Int2 != vector3Int)
				{
					this.m_chunkedSmokeToMove.Add(new Tuple<Vector3Int, Vector3Int, Smoke>(vector3Int, vector3Int2, smoke));
				}
			}
		}
		foreach (Tuple<Vector3Int, Vector3Int, Smoke> tuple in this.m_chunkedSmokeToMove)
		{
			this.RemoveSmokeFromChunk(tuple.Item1, tuple.Item3);
			this.AddSmokeToChunk(tuple.Item2, tuple.Item3);
		}
	}

	// Token: 0x060006B6 RID: 1718 RVA: 0x00038178 File Offset: 0x00036378
	private void LateUpdate()
	{
		this.TransferSmokeBetweenChunks();
		foreach (Vector3Int key in this.m_chunkedParticleSystems.Keys)
		{
			ParticleSystem particleSystem = this.m_chunkedParticleSystems[key];
			List<Smoke> list = this.m_chunkedSmoke[key];
			ParticleSystem.Particle[] array = this.m_chunkedParticles[key];
			if (list.Count > particleSystem.particleCount)
			{
				particleSystem.Emit(list.Count - particleSystem.particleCount);
			}
			for (int i = 0; i < list.Count; i++)
			{
				Smoke smoke = list[i];
				array[i] = smoke.GetParticleValues();
				array[i].startColor = this.m_smokeColor * new Color(1f, 1f, 1f, smoke.GetAlpha());
				array[i].startSize = this.m_smokeBallSize;
			}
			for (int j = list.Count; j < particleSystem.particleCount; j++)
			{
				array[j].remainingLifetime = -1f;
			}
			particleSystem.SetParticles(array, particleSystem.particleCount);
		}
	}

	// Token: 0x060006B7 RID: 1719 RVA: 0x000382E4 File Offset: 0x000364E4
	private void OnDrawGizmosSelected()
	{
		foreach (Vector3Int vector3Int in this.m_chunkedSmoke.Keys)
		{
			Vector3 a = this.ChunkToWorld(vector3Int);
			Color a2 = new Color(0.43f, 1f, 0f, 0.26f);
			Color red = Color.red;
			Gizmos.color = Color.Lerp(a2, red, (float)this.m_chunkedSmoke[vector3Int].Count / 100f * 0.33f);
			Gizmos.DrawWireCube(a + Vector3.one * this.m_chunkSize * 0.5f, this.m_chunkSize * Vector3.one);
		}
	}

	// Token: 0x040007C0 RID: 1984
	[SerializeField]
	private ParticleSystem _particleSystemPrefab;

	// Token: 0x040007C1 RID: 1985
	[SerializeField]
	private Color m_smokeColor;

	// Token: 0x040007C2 RID: 1986
	[SerializeField]
	private float m_smokeBallSize = 4f;

	// Token: 0x040007C3 RID: 1987
	[Header("Chunking")]
	[SerializeField]
	private float m_chunkSize = 10f;

	// Token: 0x040007C4 RID: 1988
	private Dictionary<Vector3Int, ParticleSystem> m_chunkedParticleSystems = new Dictionary<Vector3Int, ParticleSystem>();

	// Token: 0x040007C5 RID: 1989
	private Dictionary<Vector3Int, List<Smoke>> m_chunkedSmoke = new Dictionary<Vector3Int, List<Smoke>>();

	// Token: 0x040007C6 RID: 1990
	private Dictionary<Vector3Int, ParticleSystem.Particle[]> m_chunkedParticles = new Dictionary<Vector3Int, ParticleSystem.Particle[]>();

	// Token: 0x040007C7 RID: 1991
	private List<Tuple<Vector3Int, Vector3Int, Smoke>> m_chunkedSmokeToMove = new List<Tuple<Vector3Int, Vector3Int, Smoke>>();

	// Token: 0x040007C8 RID: 1992
	public static SmokeRenderer Instance;

	// Token: 0x040007C9 RID: 1993
	private const int c_MaxChunkParticleCount = 100;
}
