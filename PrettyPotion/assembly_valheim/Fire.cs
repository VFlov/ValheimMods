using System;
using System.Collections.Generic;
using UnityEngine;

// Token: 0x02000178 RID: 376
public class Fire : MonoBehaviour
{
	// Token: 0x06001697 RID: 5783 RVA: 0x000A6E38 File Offset: 0x000A5038
	private void Awake()
	{
		this.m_nview = base.GetComponent<ZNetView>();
		if (Fire.s_dotMask == 0)
		{
			Fire.s_dotMask = LayerMask.GetMask(new string[]
			{
				"Default",
				"static_solid",
				"Default_small",
				"piece",
				"piece_nonsolid",
				"character",
				"character_net",
				"character_ghost",
				"hitbox",
				"character_noenv",
				"vehicle"
			});
			Fire.s_solidMask = LayerMask.GetMask(new string[]
			{
				"Default",
				"static_solid",
				"Default_small",
				"piece"
			});
			Fire.s_terrainMask = LayerMask.GetMask(new string[]
			{
				"terrain"
			});
			Fire.s_smokeRayMask = LayerMask.GetMask(new string[]
			{
				"smoke"
			});
		}
		base.InvokeRepeating("Dot", this.m_dotInterval, this.m_dotInterval);
		if (this.m_terrainHitSpawn && (this.m_terrainHitBiomes == Heightmap.Biome.All || this.m_terrainHitBiomes.HasFlag(WorldGenerator.instance.GetBiome(base.transform.position))))
		{
			base.Invoke("HitTerrain", this.m_terrainHitDelay);
		}
		base.InvokeRepeating("UpdateFire", UnityEngine.Random.Range(this.m_updateRate / 2f, this.m_updateRate), this.m_updateRate);
		Fire.s_fires.Add(this);
	}

	// Token: 0x06001698 RID: 5784 RVA: 0x000A6FC7 File Offset: 0x000A51C7
	private void OnDestroy()
	{
		Fire.s_fires.Remove(this);
	}

	// Token: 0x06001699 RID: 5785 RVA: 0x000A6FD8 File Offset: 0x000A51D8
	private void Dot()
	{
		if (!this.m_nview.IsValid() || !this.m_nview.IsOwner())
		{
			return;
		}
		Fire.m_destructibles.Clear();
		int num = Physics.OverlapSphereNonAlloc(base.transform.position, this.m_dotRadius, Fire.m_colliders, Fire.s_dotMask);
		for (int i = 0; i < num; i++)
		{
			GameObject gameObject = Projectile.FindHitObject(Fire.m_colliders[i]);
			IDestructible component = gameObject.GetComponent<IDestructible>();
			if (UnityEngine.Random.Range(0f, 1f) < this.m_fuelBurnChance)
			{
				Fireplace component2 = gameObject.GetComponent<Fireplace>();
				if (component2 != null)
				{
					component2.AddFuel(-this.m_fuelBurnAmount);
				}
			}
			if (gameObject.GetComponent<Character>())
			{
				this.DoDamage(component, Fire.m_colliders[i]);
			}
			else
			{
				WearNTear component3 = gameObject.GetComponent<WearNTear>();
				if ((component3 == null || component3.m_burnable) && component != null)
				{
					Fire.m_destructibles.Add(new KeyValuePair<IDestructible, Collider>(component, Fire.m_colliders[i]));
				}
			}
		}
		if (Fire.m_destructibles.Count > 0)
		{
			KeyValuePair<IDestructible, Collider> keyValuePair = Fire.m_destructibles[UnityEngine.Random.Range(0, Fire.m_destructibles.Count)];
			this.DoDamage(keyValuePair.Key, keyValuePair.Value);
		}
	}

	// Token: 0x0600169A RID: 5786 RVA: 0x000A710C File Offset: 0x000A530C
	private void DoDamage(IDestructible toHit, Collider collider)
	{
		HitData hitData = new HitData();
		hitData.m_hitCollider = collider;
		hitData.m_damage.m_fire = this.m_fireDamage;
		hitData.m_damage.m_chop = this.m_chopDamage;
		hitData.m_toolTier = this.m_toolTier;
		hitData.m_point = (base.transform.position + collider.bounds.center) * 0.5f;
		hitData.m_dodgeable = false;
		hitData.m_blockable = false;
		hitData.m_hitType = HitData.HitType.CinderFire;
		this.m_hitEffect.Create(hitData.m_point, Quaternion.identity, null, 1f, -1);
		toHit.Damage(hitData);
	}

	// Token: 0x0600169B RID: 5787 RVA: 0x000A71C0 File Offset: 0x000A53C0
	private void HitTerrain()
	{
		RaycastHit raycastHit;
		if (Physics.Raycast(base.transform.position, Vector3.down, out raycastHit, this.m_terrainMaxDist, Fire.s_terrainMask))
		{
			Heightmap component = raycastHit.collider.GetComponent<Heightmap>();
			if (component != null && !component.IsLava(raycastHit.point, 0.6f) && ((this.m_terrainCheckCultivated && !component.IsCultivated(raycastHit.point)) || (this.m_terrainCheckCleared && !component.IsCleared(raycastHit.point)) || (!this.m_terrainCheckCleared && !this.m_terrainCheckCultivated)))
			{
				UnityEngine.Object.Instantiate<GameObject>(this.m_terrainHitSpawn, raycastHit.point, Quaternion.identity);
			}
		}
	}

	// Token: 0x0600169C RID: 5788 RVA: 0x000A726C File Offset: 0x000A546C
	private void UpdateFire()
	{
		if (!this.m_nview.IsValid() || !this.m_nview.IsOwner())
		{
			return;
		}
		if (!this.m_roof)
		{
			WearNTear.RoofCheck(base.transform.position, out this.m_roof);
		}
		if (!this.m_roof && EnvMan.IsWet())
		{
			ZNetScene.instance.Destroy(base.gameObject);
		}
		if (this.m_roof)
		{
			this.m_smokeHits = Physics.OverlapSphereNonAlloc(base.transform.position + Vector3.up * this.m_smokeOxygenCheckHeight, this.m_smokeOxygenCheckRadius, Fire.s_hits, Fire.s_smokeRayMask);
			this.m_smokeHits -= this.m_oxygenSmokeTolerance;
			if (this.m_smokeHits > 0)
			{
				this.m_suffocating += (float)this.m_smokeHits * this.m_smokeSuffocationPerHit;
				Terminal.Log(string.Format("Fire suffocation in interior with {0} smoke hits", this.m_smokeHits));
			}
			else
			{
				this.m_suffocating = Mathf.Max(0f, this.m_suffocating - 1f);
			}
		}
		else
		{
			this.m_inSmoke = Physics.CheckSphere(base.transform.position + Vector3.up * this.m_smokeCheckHeight, this.m_smokeCheckRadius, Fire.s_smokeRayMask);
			if (this.m_inSmoke)
			{
				this.m_suffocating += 1f;
				Terminal.Log("Fire in direct smoke");
			}
			else
			{
				this.m_suffocating = Mathf.Max(0f, this.m_suffocating - 1f);
			}
		}
		if (this.m_suffocating >= this.m_maxSmoke && (this.m_smokeDieChance >= 1f || UnityEngine.Random.Range(0f, 1f) < this.m_smokeDieChance))
		{
			Terminal.Log("Fire suffocated");
			ZNetScene.instance.Destroy(base.gameObject);
		}
	}

	// Token: 0x0400162B RID: 5675
	public static List<Fire> s_fires = new List<Fire>();

	// Token: 0x0400162C RID: 5676
	private static Collider[] m_colliders = new Collider[128];

	// Token: 0x0400162D RID: 5677
	private static List<KeyValuePair<IDestructible, Collider>> m_destructibles = new List<KeyValuePair<IDestructible, Collider>>();

	// Token: 0x0400162E RID: 5678
	public float m_dotInterval = 1f;

	// Token: 0x0400162F RID: 5679
	public float m_dotRadius = 1f;

	// Token: 0x04001630 RID: 5680
	public float m_fireDamage = 10f;

	// Token: 0x04001631 RID: 5681
	public float m_chopDamage = 10f;

	// Token: 0x04001632 RID: 5682
	public short m_toolTier = 2;

	// Token: 0x04001633 RID: 5683
	public int m_spread = 4;

	// Token: 0x04001634 RID: 5684
	public float m_updateRate = 2f;

	// Token: 0x04001635 RID: 5685
	[Header("Terrain hit")]
	public float m_terrainHitDelay;

	// Token: 0x04001636 RID: 5686
	public float m_terrainMaxDist;

	// Token: 0x04001637 RID: 5687
	public bool m_terrainCheckCultivated;

	// Token: 0x04001638 RID: 5688
	public bool m_terrainCheckCleared;

	// Token: 0x04001639 RID: 5689
	public GameObject m_terrainHitSpawn;

	// Token: 0x0400163A RID: 5690
	public Heightmap.Biome m_terrainHitBiomes = Heightmap.Biome.All;

	// Token: 0x0400163B RID: 5691
	[Header("Burn fuel from fireplaces")]
	public float m_fuelBurnChance = 0.5f;

	// Token: 0x0400163C RID: 5692
	public float m_fuelBurnAmount = 0.1f;

	// Token: 0x0400163D RID: 5693
	[Header("Smoke")]
	public SmokeSpawner m_smokeSpawner;

	// Token: 0x0400163E RID: 5694
	public float m_smokeCheckHeight = 0.25f;

	// Token: 0x0400163F RID: 5695
	public float m_smokeCheckRadius = 0.5f;

	// Token: 0x04001640 RID: 5696
	public float m_smokeOxygenCheckHeight = 1.25f;

	// Token: 0x04001641 RID: 5697
	public float m_smokeOxygenCheckRadius = 1.5f;

	// Token: 0x04001642 RID: 5698
	public float m_smokeSuffocationPerHit = 0.2f;

	// Token: 0x04001643 RID: 5699
	public int m_oxygenSmokeTolerance = 2;

	// Token: 0x04001644 RID: 5700
	public int m_oxygenInteriorChecks = 5;

	// Token: 0x04001645 RID: 5701
	public float m_smokeDieChance = 0.5f;

	// Token: 0x04001646 RID: 5702
	public float m_maxSmoke = 3f;

	// Token: 0x04001647 RID: 5703
	[Header("Effects")]
	public EffectList m_hitEffect;

	// Token: 0x04001648 RID: 5704
	private static int s_dotMask = 0;

	// Token: 0x04001649 RID: 5705
	private static int s_solidMask = 0;

	// Token: 0x0400164A RID: 5706
	private static int s_terrainMask = 0;

	// Token: 0x0400164B RID: 5707
	private static int s_smokeRayMask = 0;

	// Token: 0x0400164C RID: 5708
	private static readonly RaycastHit[] s_raycastHits = new RaycastHit[32];

	// Token: 0x0400164D RID: 5709
	private static readonly Collider[] s_hits = new Collider[32];

	// Token: 0x0400164E RID: 5710
	private int m_smokeHits;

	// Token: 0x0400164F RID: 5711
	private bool m_inSmoke;

	// Token: 0x04001650 RID: 5712
	private GameObject m_roof;

	// Token: 0x04001651 RID: 5713
	private ZNetView m_nview;

	// Token: 0x04001652 RID: 5714
	private float m_suffocating;
}
