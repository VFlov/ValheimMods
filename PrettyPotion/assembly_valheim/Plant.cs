using System;
using UnityEngine;

// Token: 0x020001A4 RID: 420
public class Plant : SlowUpdate, Hoverable
{
	// Token: 0x060018A7 RID: 6311 RVA: 0x000B836C File Offset: 0x000B656C
	public override void Awake()
	{
		base.Awake();
		this.m_nview = base.gameObject.GetComponent<ZNetView>();
		if (this.m_nview.GetZDO() == null)
		{
			return;
		}
		this.m_seed = this.m_nview.GetZDO().GetInt(ZDOVars.s_seed, 0);
		if (this.m_seed == 0)
		{
			this.m_seed = (int)((ulong)this.m_nview.GetZDO().m_uid.ID + (ulong)this.m_nview.GetZDO().m_uid.UserID);
			this.m_nview.GetZDO().Set(ZDOVars.s_seed, this.m_seed, true);
		}
		if (this.m_nview.IsOwner() && this.m_nview.GetZDO().GetLong(ZDOVars.s_plantTime, 0L) == 0L)
		{
			this.m_nview.GetZDO().Set(ZDOVars.s_plantTime, ZNet.instance.GetTime().Ticks);
		}
		this.m_spawnTime = Time.time;
	}

	// Token: 0x060018A8 RID: 6312 RVA: 0x000B8469 File Offset: 0x000B6669
	private void Start()
	{
		this.m_updateTime = Time.time + 10f;
	}

	// Token: 0x060018A9 RID: 6313 RVA: 0x000B847C File Offset: 0x000B667C
	public string GetHoverText()
	{
		switch (this.m_status)
		{
		case Plant.Status.Healthy:
			return Localization.instance.Localize(this.m_name + " ( $piece_plant_healthy )");
		case Plant.Status.NoSun:
			return Localization.instance.Localize(this.m_name + " ( $piece_plant_nosun )");
		case Plant.Status.NoSpace:
			return Localization.instance.Localize(this.m_name + " ( $piece_plant_nospace )");
		case Plant.Status.WrongBiome:
			return Localization.instance.Localize(this.m_name + " ( $piece_plant_wrongbiome )");
		case Plant.Status.NotCultivated:
			return Localization.instance.Localize(this.m_name + " ( $piece_plant_notcultivated )");
		case Plant.Status.NoAttachPiece:
			return Localization.instance.Localize(this.m_name + " ( $piece_plant_nowall )");
		case Plant.Status.TooHot:
			return Localization.instance.Localize(this.m_name + " ( $piece_plant_toohot )");
		case Plant.Status.TooCold:
			return Localization.instance.Localize(this.m_name + " ( $piece_plant_toocold )");
		default:
			return "";
		}
	}

	// Token: 0x060018AA RID: 6314 RVA: 0x000B8598 File Offset: 0x000B6798
	public string GetHoverName()
	{
		return Localization.instance.Localize(this.m_name);
	}

	// Token: 0x060018AB RID: 6315 RVA: 0x000B85AC File Offset: 0x000B67AC
	private double TimeSincePlanted()
	{
		DateTime d = new DateTime(this.m_nview.GetZDO().GetLong(ZDOVars.s_plantTime, ZNet.instance.GetTime().Ticks));
		return (ZNet.instance.GetTime() - d).TotalSeconds;
	}

	// Token: 0x060018AC RID: 6316 RVA: 0x000B8600 File Offset: 0x000B6800
	public override void SUpdate(float time, Vector2i referenceZone)
	{
		if (!this.m_nview.IsValid())
		{
			return;
		}
		if (time > this.m_updateTime)
		{
			return;
		}
		this.m_updateTime = time + 10f;
		double num = this.TimeSincePlanted();
		this.UpdateHealth(num);
		float growTime = this.GetGrowTime();
		if (this.m_healthyGrown)
		{
			bool flag = num > (double)(growTime * 0.5f);
			this.m_healthy.SetActive(!flag && this.m_status == Plant.Status.Healthy);
			this.m_unhealthy.SetActive(!flag && this.m_status > Plant.Status.Healthy);
			this.m_healthyGrown.SetActive(flag && this.m_status == Plant.Status.Healthy);
			this.m_unhealthyGrown.SetActive(flag && this.m_status > Plant.Status.Healthy);
		}
		else
		{
			this.m_healthy.SetActive(this.m_status == Plant.Status.Healthy);
			this.m_unhealthy.SetActive(this.m_status > Plant.Status.Healthy);
		}
		if (this.m_nview.IsOwner() && time - this.m_spawnTime > 10f && num > (double)growTime)
		{
			this.Grow();
		}
	}

	// Token: 0x060018AD RID: 6317 RVA: 0x000B871C File Offset: 0x000B691C
	private float GetGrowTime()
	{
		UnityEngine.Random.State state = UnityEngine.Random.state;
		UnityEngine.Random.InitState(this.m_seed);
		float value = UnityEngine.Random.value;
		UnityEngine.Random.state = state;
		return Mathf.Lerp(this.m_growTime, this.m_growTimeMax, value);
	}

	// Token: 0x060018AE RID: 6318 RVA: 0x000B8758 File Offset: 0x000B6958
	public GameObject Grow()
	{
		if (this.m_status != Plant.Status.Healthy)
		{
			if (this.m_destroyIfCantGrow)
			{
				this.Destroy();
			}
			return null;
		}
		float num = 11.25f;
		GameObject original = this.m_grownPrefabs[UnityEngine.Random.Range(0, this.m_grownPrefabs.Length)];
		Vector3 position = (this.m_attachDistance > 0f) ? this.m_attachPos : base.transform.position;
		Quaternion quaternion = (this.m_attachDistance > 0f) ? this.m_attachRot : Quaternion.Euler(base.transform.rotation.eulerAngles.x, base.transform.rotation.eulerAngles.y + UnityEngine.Random.Range(-num, num), base.transform.rotation.eulerAngles.z);
		GameObject gameObject = UnityEngine.Object.Instantiate<GameObject>(original, position, quaternion);
		if (this.m_attachDistance > 0f)
		{
			this.PlaceAgainst(gameObject, this.m_attachRot, this.m_attachPos, this.m_attachNormal);
		}
		string str = "Starting to grow plant with rotation: ";
		Quaternion quaternion2 = quaternion;
		ZLog.Log(str + quaternion2.ToString());
		ZNetView component = gameObject.GetComponent<ZNetView>();
		float num2 = UnityEngine.Random.Range(this.m_minScale, this.m_maxScale);
		component.SetLocalScale(new Vector3(num2, num2, num2));
		TreeBase component2 = gameObject.GetComponent<TreeBase>();
		if (component2 != null)
		{
			component2.Grow();
		}
		if (this.m_nview)
		{
			this.m_nview.Destroy();
			this.m_growEffect.Create(base.transform.position, quaternion, null, num2, -1);
		}
		return gameObject;
	}

	// Token: 0x060018AF RID: 6319 RVA: 0x000B88E8 File Offset: 0x000B6AE8
	public void UpdateHealth(double timeSincePlanted)
	{
		if (timeSincePlanted < 10.0)
		{
			this.m_status = Plant.Status.Healthy;
			return;
		}
		Heightmap heightmap = Heightmap.FindHeightmap(base.transform.position);
		if (heightmap)
		{
			Heightmap.Biome biome = heightmap.GetBiome(base.transform.position, 0.02f, false);
			if ((biome & this.m_biome) == Heightmap.Biome.None)
			{
				this.m_status = Plant.Status.WrongBiome;
				return;
			}
			if (this.m_needCultivatedGround && !heightmap.IsCultivated(base.transform.position))
			{
				this.m_status = Plant.Status.NotCultivated;
				return;
			}
			if (!this.m_tolerateHeat && biome == Heightmap.Biome.AshLands && !ShieldGenerator.IsInsideShield(base.transform.position))
			{
				this.m_status = Plant.Status.TooHot;
				return;
			}
			if (!this.m_tolerateCold && (biome == Heightmap.Biome.DeepNorth || biome == Heightmap.Biome.Mountain) && !ShieldGenerator.IsInsideShield(base.transform.position))
			{
				this.m_status = Plant.Status.TooCold;
				return;
			}
		}
		if (this.HaveRoof())
		{
			this.m_status = Plant.Status.NoSun;
			return;
		}
		if (!this.HaveGrowSpace())
		{
			this.m_status = Plant.Status.NoSpace;
			return;
		}
		if (this.m_attachDistance > 0f && !this.GetClosestAttachPosRot(out this.m_attachPos, out this.m_attachRot, out this.m_attachNormal))
		{
			this.m_status = Plant.Status.NoAttachPiece;
			return;
		}
		this.m_status = Plant.Status.Healthy;
	}

	// Token: 0x060018B0 RID: 6320 RVA: 0x000B8A1A File Offset: 0x000B6C1A
	public Collider GetClosestAttachObject()
	{
		return this.GetClosestAttachObject(base.transform.position);
	}

	// Token: 0x060018B1 RID: 6321 RVA: 0x000B8A30 File Offset: 0x000B6C30
	public Collider GetClosestAttachObject(Vector3 from)
	{
		if (Plant.m_pieceMask == 0)
		{
			Plant.m_pieceMask = LayerMask.GetMask(new string[]
			{
				"piece"
			});
		}
		int num = Physics.OverlapSphereNonAlloc(from, this.m_attachDistance, Plant.s_hits, Plant.m_pieceMask);
		Collider result = null;
		float num2 = float.MaxValue;
		for (int i = 0; i < num; i++)
		{
			Collider collider = Plant.s_hits[i];
			float num3 = Vector3.Distance(from, collider.bounds.center);
			if (num3 < num2)
			{
				Piece componentInParent = collider.GetComponentInParent<Piece>();
				if (componentInParent != null && !componentInParent.m_noVines)
				{
					result = collider;
					num2 = num3;
				}
			}
		}
		return result;
	}

	// Token: 0x060018B2 RID: 6322 RVA: 0x000B8ACA File Offset: 0x000B6CCA
	public bool GetClosestAttachPosRot(out Vector3 pos, out Quaternion rot, out Vector3 normal)
	{
		return this.GetClosestAttachPosRot(base.transform.position, out pos, out rot, out normal);
	}

	// Token: 0x060018B3 RID: 6323 RVA: 0x000B8AE0 File Offset: 0x000B6CE0
	public bool GetClosestAttachPosRot(Vector3 from, out Vector3 pos, out Quaternion rot, out Vector3 normal)
	{
		Collider closestAttachObject = this.GetClosestAttachObject(from);
		if (closestAttachObject != null)
		{
			if (Plant.m_pieceMask == 0)
			{
				Plant.m_pieceMask = LayerMask.GetMask(new string[]
				{
					"piece"
				});
			}
			if (from.y < closestAttachObject.bounds.min.y)
			{
				from.y += closestAttachObject.bounds.min.y - from.y + 0.01f;
			}
			if (from.y > closestAttachObject.bounds.max.y)
			{
				from.y += closestAttachObject.bounds.max.y - from.y - 0.01f;
			}
			Vector3 a = closestAttachObject.ClosestPoint(from);
			RaycastHit raycastHit;
			if (Physics.Raycast(from, a - from, out raycastHit, 50f, Plant.m_pieceMask) && raycastHit.collider && !raycastHit.collider.attachedRigidbody)
			{
				pos = raycastHit.point;
				rot = Quaternion.Euler(0f, 90f, 0f) * Quaternion.LookRotation(raycastHit.normal);
				normal = raycastHit.normal;
				Terminal.Log("Plant found grow normal: " + raycastHit.normal.ToString());
				return true;
			}
			Terminal.Log("Plant ray didn't hit any valid colliders");
		}
		pos = (normal = Vector3.zero);
		rot = Quaternion.identity;
		Terminal.Log("Plant found no attach obj.");
		return false;
	}

	// Token: 0x060018B4 RID: 6324 RVA: 0x000B8C94 File Offset: 0x000B6E94
	public void PlaceAgainst(GameObject obj, Quaternion rot, Vector3 hitPos, Vector3 hitNormal)
	{
		obj.transform.position = hitPos + hitNormal * 50f;
		obj.transform.rotation = rot;
		Vector3 b = Vector3.zero;
		float num = 999999f;
		foreach (Collider collider in obj.GetComponentsInChildren<Collider>())
		{
			if (!collider.isTrigger && collider.enabled)
			{
				MeshCollider meshCollider = collider as MeshCollider;
				if (!(meshCollider != null) || meshCollider.convex)
				{
					Vector3 vector = collider.ClosestPoint(hitPos);
					float num2 = Vector3.Distance(vector, hitPos);
					if (num2 < num)
					{
						b = vector;
						num = num2;
					}
				}
			}
		}
		Vector3 b2 = obj.transform.position - b;
		obj.transform.position = hitPos + b2;
		obj.transform.rotation = rot;
	}

	// Token: 0x060018B5 RID: 6325 RVA: 0x000B8D74 File Offset: 0x000B6F74
	private void Destroy()
	{
		IDestructible component = base.GetComponent<IDestructible>();
		if (component != null)
		{
			HitData hitData = new HitData();
			hitData.m_damage.m_damage = 9999f;
			component.Damage(hitData);
		}
	}

	// Token: 0x060018B6 RID: 6326 RVA: 0x000B8DA8 File Offset: 0x000B6FA8
	private bool HaveRoof()
	{
		if (Plant.m_roofMask == 0)
		{
			Plant.m_roofMask = LayerMask.GetMask(new string[]
			{
				"Default",
				"static_solid",
				"piece"
			});
		}
		return Physics.Raycast(base.transform.position, Vector3.up, 100f, Plant.m_roofMask);
	}

	// Token: 0x060018B7 RID: 6327 RVA: 0x000B8E08 File Offset: 0x000B7008
	private bool HaveGrowSpace()
	{
		if (Plant.m_spaceMask == 0)
		{
			Plant.m_spaceMask = LayerMask.GetMask(new string[]
			{
				"Default",
				"static_solid",
				"Default_small",
				"piece",
				"piece_nonsolid"
			});
		}
		int num = Physics.OverlapSphereNonAlloc(base.transform.position, this.m_growRadius, Plant.s_colliders, Plant.m_spaceMask);
		for (int i = 0; i < num; i++)
		{
			Plant component = Plant.s_colliders[i].GetComponent<Plant>();
			if (!component || (!(component == this) && component.GetStatus() == Plant.Status.Healthy))
			{
				return false;
			}
		}
		if (this.m_growRadiusVines > 0f)
		{
			num = Physics.OverlapSphereNonAlloc(base.transform.position, this.m_growRadiusVines, Plant.s_colliders, Plant.m_spaceMask);
			for (int j = 0; j < num; j++)
			{
				if (Plant.s_colliders[j].GetComponentInParent<Vine>() != null)
				{
					return false;
				}
			}
		}
		return true;
	}

	// Token: 0x060018B8 RID: 6328 RVA: 0x000B8EFA File Offset: 0x000B70FA
	public Plant.Status GetStatus()
	{
		return this.m_status;
	}

	// Token: 0x040018DB RID: 6363
	private static Collider[] s_colliders = new Collider[30];

	// Token: 0x040018DC RID: 6364
	private static Collider[] s_hits = new Collider[10];

	// Token: 0x040018DD RID: 6365
	public string m_name = "Plant";

	// Token: 0x040018DE RID: 6366
	public float m_growTime = 10f;

	// Token: 0x040018DF RID: 6367
	public float m_growTimeMax = 2000f;

	// Token: 0x040018E0 RID: 6368
	public GameObject[] m_grownPrefabs = new GameObject[0];

	// Token: 0x040018E1 RID: 6369
	public float m_minScale = 1f;

	// Token: 0x040018E2 RID: 6370
	public float m_maxScale = 1f;

	// Token: 0x040018E3 RID: 6371
	public float m_growRadius = 1f;

	// Token: 0x040018E4 RID: 6372
	public float m_growRadiusVines;

	// Token: 0x040018E5 RID: 6373
	public bool m_needCultivatedGround;

	// Token: 0x040018E6 RID: 6374
	public bool m_destroyIfCantGrow;

	// Token: 0x040018E7 RID: 6375
	public bool m_tolerateHeat;

	// Token: 0x040018E8 RID: 6376
	public bool m_tolerateCold;

	// Token: 0x040018E9 RID: 6377
	[SerializeField]
	private GameObject m_healthy;

	// Token: 0x040018EA RID: 6378
	[SerializeField]
	private GameObject m_unhealthy;

	// Token: 0x040018EB RID: 6379
	[SerializeField]
	private GameObject m_healthyGrown;

	// Token: 0x040018EC RID: 6380
	[SerializeField]
	private GameObject m_unhealthyGrown;

	// Token: 0x040018ED RID: 6381
	[BitMask(typeof(Heightmap.Biome))]
	public Heightmap.Biome m_biome;

	// Token: 0x040018EE RID: 6382
	public EffectList m_growEffect = new EffectList();

	// Token: 0x040018EF RID: 6383
	[Header("Attach to buildpiece (Vines)")]
	public float m_attachDistance;

	// Token: 0x040018F0 RID: 6384
	private Plant.Status m_status;

	// Token: 0x040018F1 RID: 6385
	private ZNetView m_nview;

	// Token: 0x040018F2 RID: 6386
	private float m_updateTime;

	// Token: 0x040018F3 RID: 6387
	private float m_spawnTime;

	// Token: 0x040018F4 RID: 6388
	private int m_seed;

	// Token: 0x040018F5 RID: 6389
	private Vector3 m_attachPos;

	// Token: 0x040018F6 RID: 6390
	private Vector3 m_attachNormal;

	// Token: 0x040018F7 RID: 6391
	private Quaternion m_attachRot;

	// Token: 0x040018F8 RID: 6392
	private Collider m_attachCollider;

	// Token: 0x040018F9 RID: 6393
	private static int m_spaceMask = 0;

	// Token: 0x040018FA RID: 6394
	private static int m_roofMask = 0;

	// Token: 0x040018FB RID: 6395
	private static int m_pieceMask = 0;

	// Token: 0x0200037A RID: 890
	public enum Status
	{
		// Token: 0x0400264F RID: 9807
		Healthy,
		// Token: 0x04002650 RID: 9808
		NoSun,
		// Token: 0x04002651 RID: 9809
		NoSpace,
		// Token: 0x04002652 RID: 9810
		WrongBiome,
		// Token: 0x04002653 RID: 9811
		NotCultivated,
		// Token: 0x04002654 RID: 9812
		NoAttachPiece,
		// Token: 0x04002655 RID: 9813
		TooHot,
		// Token: 0x04002656 RID: 9814
		TooCold
	}
}
