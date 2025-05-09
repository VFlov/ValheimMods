using System;
using UnityEngine;

// Token: 0x02000161 RID: 353
public class Cinder : MonoBehaviour
{
	// Token: 0x0600156D RID: 5485 RVA: 0x0009DA04 File Offset: 0x0009BC04
	private void Awake()
	{
		this.m_nview = base.GetComponent<ZNetView>();
		if (Cinder.m_raymask == 0)
		{
			Cinder.m_raymask = LayerMask.GetMask(new string[]
			{
				"Default",
				"static_solid",
				"Default_small",
				"piece",
				"piece_nonsolid",
				"terrain",
				"character",
				"character_net",
				"character_ghost",
				"hitbox",
				"character_noenv",
				"vehicle"
			});
		}
		if (!this.m_nview.IsValid() || !this.m_nview.IsOwner())
		{
			return;
		}
		Vector3 vector = base.transform.position;
		vector -= EnvMan.instance.GetWindForce() * this.m_windStrength * 10f;
		base.transform.position = vector;
	}

	// Token: 0x0600156E RID: 5486 RVA: 0x0009DAF4 File Offset: 0x0009BCF4
	private void FixedUpdate()
	{
		if (this.m_haveHit)
		{
			return;
		}
		if (!this.m_nview.IsValid() || !this.m_nview.IsOwner())
		{
			return;
		}
		float fixedDeltaTime = Time.fixedDeltaTime;
		this.m_vel += EnvMan.instance.GetWindForce() * (fixedDeltaTime * this.m_windStrength);
		this.m_vel += Vector3.down * (this.m_gravity * fixedDeltaTime);
		float d = Mathf.Pow(this.m_vel.magnitude, 2f) * this.m_drag * Time.fixedDeltaTime;
		this.m_vel += d * -this.m_vel.normalized;
		Vector3 position = base.transform.position;
		Vector3 vector = position + this.m_vel * fixedDeltaTime;
		base.transform.position = vector;
		RaycastHit raycastHit;
		if (Physics.Raycast(position, this.m_vel.normalized, out raycastHit, Vector3.Distance(position, vector), Cinder.m_raymask))
		{
			this.OnHit(raycastHit.collider, raycastHit.point, raycastHit.normal);
		}
		ShieldGenerator.CheckObjectInsideShield(this);
	}

	// Token: 0x0600156F RID: 5487 RVA: 0x0009DC30 File Offset: 0x0009BE30
	private void OnHit(Collider collider, Vector3 point, Vector3 normal)
	{
		this.m_hitEffects.Create(point, Quaternion.identity, null, 1f, -1);
		bool flag;
		if (Cinder.CanBurn(collider, point, out flag, this.m_chanceToIgniteGrass))
		{
			GameObject gameObject;
			if (flag)
			{
				gameObject = UnityEngine.Object.Instantiate<GameObject>(this.m_firePrefab, point + normal * 0.1f, Quaternion.identity);
			}
			else
			{
				gameObject = UnityEngine.Object.Instantiate<GameObject>(this.m_houseFirePrefab, point + normal * 0.1f, Quaternion.identity);
			}
			CinderSpawner component = gameObject.GetComponent<CinderSpawner>();
			if (component != null)
			{
				component.Setup(this.GetSpread(), collider.gameObject);
			}
		}
		this.m_haveHit = true;
		base.transform.position = point;
		base.InvokeRepeating("DestroyNow", 0.25f, 1f);
	}

	// Token: 0x06001570 RID: 5488 RVA: 0x0009DCF4 File Offset: 0x0009BEF4
	private void OnShieldHit()
	{
	}

	// Token: 0x06001571 RID: 5489 RVA: 0x0009DCF6 File Offset: 0x0009BEF6
	private void DestroyNow()
	{
		if (!this.m_nview.IsValid() || !this.m_nview.IsOwner())
		{
			return;
		}
		this.m_nview.Destroy();
	}

	// Token: 0x06001572 RID: 5490 RVA: 0x0009DD20 File Offset: 0x0009BF20
	public static bool CanBurn(Collider collider, Vector3 point, out bool isTerrain, float chanceToIgniteGrass = 0f)
	{
		isTerrain = false;
		if (point.y < 30f)
		{
			return false;
		}
		if (Floating.GetLiquidLevel(point, 1f, LiquidType.All) > point.y)
		{
			return false;
		}
		Piece componentInParent = collider.gameObject.GetComponentInParent<Piece>();
		if (componentInParent != null && Player.IsPlacementGhost(componentInParent.gameObject))
		{
			return false;
		}
		WearNTear componentInParent2 = collider.gameObject.GetComponentInParent<WearNTear>();
		if (componentInParent2 != null)
		{
			if (componentInParent2.m_burnable && !componentInParent2.IsWet())
			{
				return true;
			}
		}
		else
		{
			if (collider.gameObject.GetComponentInParent<TreeBase>() != null)
			{
				return true;
			}
			if (collider.gameObject.GetComponentInParent<TreeLog>() != null)
			{
				return true;
			}
		}
		if (EnvMan.IsWet())
		{
			return false;
		}
		if (chanceToIgniteGrass > 0f)
		{
			Heightmap component = collider.GetComponent<Heightmap>();
			if (component)
			{
				if (component.IsCleared(point))
				{
					return false;
				}
				Heightmap.Biome biome = component.GetBiome(point, 0.02f, false);
				if (biome == Heightmap.Biome.Mountain || biome == Heightmap.Biome.DeepNorth)
				{
					return false;
				}
				isTerrain = true;
				return UnityEngine.Random.value <= chanceToIgniteGrass;
			}
		}
		return false;
	}

	// Token: 0x06001573 RID: 5491 RVA: 0x0009DE07 File Offset: 0x0009C007
	public void Setup(Vector3 vel, int spread)
	{
		this.m_vel = vel;
		this.m_nview.GetZDO().Set(ZDOVars.s_spread, spread, false);
	}

	// Token: 0x06001574 RID: 5492 RVA: 0x0009DE27 File Offset: 0x0009C027
	private int GetSpread()
	{
		return this.m_nview.GetZDO().GetInt(ZDOVars.s_spread, this.m_spread);
	}

	// Token: 0x0400150F RID: 5391
	public GameObject m_firePrefab;

	// Token: 0x04001510 RID: 5392
	public GameObject m_houseFirePrefab;

	// Token: 0x04001511 RID: 5393
	public float m_gravity = 10f;

	// Token: 0x04001512 RID: 5394
	public float m_drag;

	// Token: 0x04001513 RID: 5395
	public float m_windStrength;

	// Token: 0x04001514 RID: 5396
	public int m_spread = 4;

	// Token: 0x04001515 RID: 5397
	[Range(0f, 1f)]
	public float m_chanceToIgniteGrass = 0.1f;

	// Token: 0x04001516 RID: 5398
	public EffectList m_hitEffects;

	// Token: 0x04001517 RID: 5399
	private Vector3 m_vel;

	// Token: 0x04001518 RID: 5400
	private static int m_raymask;

	// Token: 0x04001519 RID: 5401
	private ZNetView m_nview;

	// Token: 0x0400151A RID: 5402
	private bool m_haveHit;
}
