using System;
using UnityEngine;

// Token: 0x02000162 RID: 354
public class CinderSpawner : MonoBehaviour
{
	// Token: 0x06001576 RID: 5494 RVA: 0x0009DE6C File Offset: 0x0009C06C
	private void Awake()
	{
		this.m_nview = base.GetComponentInParent<ZNetView>();
		if (this.m_cinderInterval > 0f)
		{
			base.InvokeRepeating("UpdateSpawnCinder", this.m_cinderInterval, this.m_cinderInterval);
		}
		if (this.m_spawnOnAwake)
		{
			this.SpawnCinder();
		}
		if (this.m_spawnOnProjectileHit)
		{
			Projectile component = base.GetComponent<Projectile>();
			if (component != null)
			{
				Projectile projectile = component;
				projectile.m_onHit = (OnProjectileHit)Delegate.Combine(projectile.m_onHit, new OnProjectileHit(delegate(Collider collider, Vector3 point, bool water)
				{
					this.SpawnCinder();
				}));
			}
		}
		this.m_fireplace = base.GetComponent<Fireplace>();
	}

	// Token: 0x06001577 RID: 5495 RVA: 0x0009DEF7 File Offset: 0x0009C0F7
	private void FixedUpdate()
	{
		if (this.m_hasAttachObj && !this.m_attachObj)
		{
			this.DestroyNow();
		}
	}

	// Token: 0x06001578 RID: 5496 RVA: 0x0009DF14 File Offset: 0x0009C114
	public void Setup(int spread, GameObject attachObj)
	{
		this.m_nview.GetZDO().Set(ZDOVars.s_spread, spread, false);
		this.m_hasAttachObj = (attachObj != null);
		this.m_attachObj = attachObj;
	}

	// Token: 0x06001579 RID: 5497 RVA: 0x0009DF41 File Offset: 0x0009C141
	private int GetSpread()
	{
		return this.m_nview.GetZDO().GetInt(ZDOVars.s_spread, this.m_spread);
	}

	// Token: 0x0600157A RID: 5498 RVA: 0x0009DF60 File Offset: 0x0009C160
	private void UpdateSpawnCinder()
	{
		if (!this.m_nview.IsValid() || !this.m_nview.IsOwner())
		{
			return;
		}
		if (this.m_fireplace && !this.m_fireplace.IsBurning())
		{
			return;
		}
		if (!this.CanSpawnCinder())
		{
			return;
		}
		if (this.GetSpread() <= 0)
		{
			return;
		}
		if (UnityEngine.Random.value > this.m_cinderChance)
		{
			return;
		}
		this.SpawnCinder();
	}

	// Token: 0x0600157B RID: 5499 RVA: 0x0009DFCC File Offset: 0x0009C1CC
	public void SpawnCinder()
	{
		if (!this.m_nview.IsValid() || !this.m_nview.IsOwner())
		{
			return;
		}
		if (!this.CanSpawnCinder())
		{
			return;
		}
		if (ShieldGenerator.IsInsideShield(base.transform.position))
		{
			return;
		}
		for (int i = 0; i < this.m_instancesPerSpawn; i++)
		{
			Vector3 insideUnitSphere = UnityEngine.Random.insideUnitSphere;
			insideUnitSphere.y = Mathf.Abs(insideUnitSphere.y * 2f);
			insideUnitSphere.Normalize();
			UnityEngine.Object.Instantiate<GameObject>(this.m_cinderPrefab, base.transform.position + insideUnitSphere * this.m_spawnOffset, Quaternion.identity).GetComponent<Cinder>().Setup(insideUnitSphere * this.m_cinderVel, this.GetSpread() - 1);
		}
	}

	// Token: 0x0600157C RID: 5500 RVA: 0x0009E090 File Offset: 0x0009C290
	public bool CanSpawnCinder()
	{
		return CinderSpawner.CanSpawnCinder(base.transform, ref this.m_biome);
	}

	// Token: 0x0600157D RID: 5501 RVA: 0x0009E0A4 File Offset: 0x0009C2A4
	public static bool CanSpawnCinder(Transform transform, ref Heightmap.Biome biome)
	{
		if (biome == Heightmap.Biome.None)
		{
			Vector3 position = transform.position;
			Vector3 vector;
			Heightmap.Biome biome2;
			Heightmap.BiomeArea biomeArea;
			Heightmap heightmap;
			ZoneSystem.instance.GetGroundData(ref position, out vector, out biome2, out biomeArea, out heightmap);
			if (heightmap != null)
			{
				biome = heightmap.GetBiome(transform.position, 0.02f, false);
			}
		}
		return biome == Heightmap.Biome.AshLands || ZoneSystem.instance.GetGlobalKey(GlobalKeys.Fire);
	}

	// Token: 0x0600157E RID: 5502 RVA: 0x0009E101 File Offset: 0x0009C301
	private void DestroyNow()
	{
		if (!this.m_nview.IsValid() || !this.m_nview.IsOwner())
		{
			return;
		}
		this.m_nview.Destroy();
	}

	// Token: 0x0600157F RID: 5503 RVA: 0x0009E129 File Offset: 0x0009C329
	private void OnDrawGizmosSelected()
	{
		Gizmos.color = Color.cyan;
		Gizmos.DrawWireSphere(base.transform.position + this.m_spawnOffsetPoint, 0.05f);
	}

	// Token: 0x0400151B RID: 5403
	public GameObject m_cinderPrefab;

	// Token: 0x0400151C RID: 5404
	public float m_cinderInterval = 2f;

	// Token: 0x0400151D RID: 5405
	public float m_cinderChance = 0.1f;

	// Token: 0x0400151E RID: 5406
	public float m_cinderVel = 5f;

	// Token: 0x0400151F RID: 5407
	public float m_spawnOffset = 1f;

	// Token: 0x04001520 RID: 5408
	public Vector3 m_spawnOffsetPoint;

	// Token: 0x04001521 RID: 5409
	public int m_spread = 4;

	// Token: 0x04001522 RID: 5410
	public int m_instancesPerSpawn = 1;

	// Token: 0x04001523 RID: 5411
	public bool m_spawnOnAwake;

	// Token: 0x04001524 RID: 5412
	public bool m_spawnOnProjectileHit;

	// Token: 0x04001525 RID: 5413
	private ZNetView m_nview;

	// Token: 0x04001526 RID: 5414
	private Heightmap.Biome m_biome;

	// Token: 0x04001527 RID: 5415
	private GameObject m_attachObj;

	// Token: 0x04001528 RID: 5416
	private bool m_hasAttachObj;

	// Token: 0x04001529 RID: 5417
	private Fireplace m_fireplace;
}
