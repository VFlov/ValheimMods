using System;
using System.Collections.Generic;
using UnityEngine;

// Token: 0x0200017E RID: 382
public class Floating : MonoBehaviour, IWaterInteractable, IMonoUpdater
{
	// Token: 0x060016FE RID: 5886 RVA: 0x000AAD44 File Offset: 0x000A8F44
	private void Awake()
	{
		this.m_nview = base.GetComponent<ZNetView>();
		this.m_body = base.GetComponent<Rigidbody>();
		this.m_collider = base.GetComponentInChildren<Collider>();
		this.SetSurfaceEffect(false);
		Floating.s_waterVolumeMask = LayerMask.GetMask(new string[]
		{
			"WaterVolume"
		});
		base.InvokeRepeating("TerrainCheck", UnityEngine.Random.Range(10f, 30f), 30f);
	}

	// Token: 0x060016FF RID: 5887 RVA: 0x000AADB3 File Offset: 0x000A8FB3
	private void OnEnable()
	{
		Floating.Instances.Add(this);
	}

	// Token: 0x06001700 RID: 5888 RVA: 0x000AADC0 File Offset: 0x000A8FC0
	private void OnDisable()
	{
		Floating.Instances.Remove(this);
	}

	// Token: 0x06001701 RID: 5889 RVA: 0x000AADCE File Offset: 0x000A8FCE
	public Transform GetTransform()
	{
		if (this == null)
		{
			return null;
		}
		return base.transform;
	}

	// Token: 0x06001702 RID: 5890 RVA: 0x000AADE4 File Offset: 0x000A8FE4
	private void TerrainCheck()
	{
		if (!this.m_nview.IsValid() || !this.m_nview.IsOwner())
		{
			return;
		}
		float groundHeight = ZoneSystem.instance.GetGroundHeight(base.transform.position);
		if (base.transform.position.y - groundHeight < -1f)
		{
			Vector3 position = base.transform.position;
			position.y = groundHeight + 1f;
			base.transform.position = position;
			Rigidbody component = base.GetComponent<Rigidbody>();
			if (component)
			{
				component.velocity = Vector3.zero;
			}
			ZLog.Log("Moved up item " + base.gameObject.name);
		}
	}

	// Token: 0x06001703 RID: 5891 RVA: 0x000AAE98 File Offset: 0x000A9098
	public void CustomFixedUpdate(float fixedDeltaTime)
	{
		this.CheckBody();
		if (!this.m_body || !this.m_nview.IsValid() || !this.m_nview.IsOwner())
		{
			return;
		}
		if (!this.HaveLiquidLevel())
		{
			this.SetSurfaceEffect(false);
			return;
		}
		this.UpdateImpactEffect();
		float floatDepth = this.GetFloatDepth();
		if (floatDepth > 0f)
		{
			this.SetSurfaceEffect(false);
			return;
		}
		this.SetSurfaceEffect(true);
		Vector3 position = this.m_collider.ClosestPoint(base.transform.position + Vector3.down * 1000f);
		Vector3 worldCenterOfMass = this.m_body.worldCenterOfMass;
		float num = Mathf.Clamp01(Mathf.Abs(floatDepth) / this.m_forceDistance);
		Vector3 vector = this.m_force * num * (fixedDeltaTime * 50f) * Vector3.up;
		this.m_body.WakeUp();
		this.m_body.AddForceAtPosition(vector * this.m_balanceForceFraction, position, ForceMode.VelocityChange);
		this.m_body.AddForceAtPosition(vector, worldCenterOfMass, ForceMode.VelocityChange);
		this.m_body.velocity = this.m_body.velocity - this.m_damping * num * this.m_body.velocity;
		this.m_body.angularVelocity = this.m_body.angularVelocity - this.m_damping * num * this.m_body.angularVelocity;
	}

	// Token: 0x06001704 RID: 5892 RVA: 0x000AB007 File Offset: 0x000A9207
	public bool HaveLiquidLevel()
	{
		return this.m_waterLevel > -10000f || this.m_tarLevel > -10000f;
	}

	// Token: 0x06001705 RID: 5893 RVA: 0x000AB025 File Offset: 0x000A9225
	private void SetSurfaceEffect(bool enabled)
	{
		if (this.m_surfaceEffects != null)
		{
			this.m_surfaceEffects.SetActive(enabled);
		}
	}

	// Token: 0x06001706 RID: 5894 RVA: 0x000AB044 File Offset: 0x000A9244
	private void UpdateImpactEffect()
	{
		this.CheckBody();
		if (!this.m_body || this.m_body.IsSleeping() || !this.m_impactEffects.HasEffects())
		{
			return;
		}
		Vector3 vector = this.m_collider.ClosestPoint(base.transform.position + Vector3.down * 1000f);
		float num = Mathf.Max(this.m_waterLevel, this.m_tarLevel);
		if (vector.y < num)
		{
			if (!this.m_wasInWater)
			{
				this.m_wasInWater = true;
				Vector3 basePos = vector;
				basePos.y = num;
				if (this.m_body.GetPointVelocity(vector).magnitude > 0.5f)
				{
					this.m_impactEffects.Create(basePos, Quaternion.identity, null, 1f, -1);
					return;
				}
			}
		}
		else
		{
			this.m_wasInWater = false;
		}
	}

	// Token: 0x06001707 RID: 5895 RVA: 0x000AB11C File Offset: 0x000A931C
	private float GetFloatDepth()
	{
		this.CheckBody();
		if (!this.m_body)
		{
			return 0f;
		}
		ref Vector3 worldCenterOfMass = this.m_body.worldCenterOfMass;
		float num = Mathf.Max(this.m_waterLevel, this.m_tarLevel);
		return worldCenterOfMass.y - num - this.m_waterLevelOffset;
	}

	// Token: 0x06001708 RID: 5896 RVA: 0x000AB16D File Offset: 0x000A936D
	public bool IsInTar()
	{
		this.CheckBody();
		return this.m_tarLevel > -10000f && this.m_body.worldCenterOfMass.y - this.m_tarLevel - this.m_waterLevelOffset < -0.2f;
	}

	// Token: 0x06001709 RID: 5897 RVA: 0x000AB1AC File Offset: 0x000A93AC
	public void SetLiquidLevel(float level, LiquidType type, Component liquidObj)
	{
		if (type != LiquidType.Water && type != LiquidType.Tar)
		{
			return;
		}
		if (type == LiquidType.Water)
		{
			this.m_waterLevel = level;
		}
		else
		{
			this.m_tarLevel = level;
		}
		if (!this.m_beenFloating && level > -10000f && this.GetFloatDepth() < 0f)
		{
			this.m_beenFloating = true;
		}
	}

	// Token: 0x0600170A RID: 5898 RVA: 0x000AB1F8 File Offset: 0x000A93F8
	private void CheckBody()
	{
		if (!this.m_body)
		{
			this.m_body = FloatingTerrain.GetBody(base.gameObject);
		}
	}

	// Token: 0x0600170B RID: 5899 RVA: 0x000AB218 File Offset: 0x000A9418
	public bool BeenFloating()
	{
		return this.m_beenFloating;
	}

	// Token: 0x0600170C RID: 5900 RVA: 0x000AB220 File Offset: 0x000A9420
	private void OnDrawGizmosSelected()
	{
		Gizmos.color = Color.blue;
		Gizmos.DrawWireCube(base.transform.position + Vector3.down * this.m_waterLevelOffset, new Vector3(1f, 0.05f, 1f));
	}

	// Token: 0x0600170D RID: 5901 RVA: 0x000AB270 File Offset: 0x000A9470
	public static float GetLiquidLevel(Vector3 p, float waveFactor = 1f, LiquidType type = LiquidType.All)
	{
		if (Floating.s_waterVolumeMask == 0)
		{
			Floating.s_waterVolumeMask = LayerMask.GetMask(new string[]
			{
				"WaterVolume"
			});
		}
		float num = -10000f;
		int num2 = Physics.OverlapSphereNonAlloc(p, 0f, Floating.s_tempColliderArray, Floating.s_waterVolumeMask);
		for (int i = 0; i < num2; i++)
		{
			Collider collider = Floating.s_tempColliderArray[i];
			int instanceID = collider.GetInstanceID();
			WaterVolume component;
			if (!Floating.s_waterVolumeCache.TryGetValue(instanceID, out component))
			{
				component = collider.GetComponent<WaterVolume>();
				Floating.s_waterVolumeCache[instanceID] = component;
			}
			if (component)
			{
				if (type == LiquidType.All || component.GetLiquidType() == type)
				{
					num = Mathf.Max(num, component.GetWaterSurface(p, waveFactor));
				}
			}
			else
			{
				LiquidSurface component2;
				if (!Floating.s_liquidSurfaceCache.TryGetValue(instanceID, out component2))
				{
					component2 = collider.GetComponent<LiquidSurface>();
					Floating.s_liquidSurfaceCache[instanceID] = component2;
				}
				if (component2 && (type == LiquidType.All || component2.GetLiquidType() == type))
				{
					num = Mathf.Max(num, component2.GetSurface(p));
				}
			}
		}
		return num;
	}

	// Token: 0x0600170E RID: 5902 RVA: 0x000AB378 File Offset: 0x000A9578
	public static float GetWaterLevel(Vector3 p, ref WaterVolume previousAndOut)
	{
		if (previousAndOut != null && previousAndOut.gameObject.GetComponent<Collider>().bounds.Contains(p))
		{
			return previousAndOut.GetWaterSurface(p, 1f);
		}
		float num = -10000f;
		int num2 = Physics.OverlapSphereNonAlloc(p, 0f, Floating.s_tempColliderArray, Floating.s_waterVolumeMask);
		for (int i = 0; i < num2; i++)
		{
			Collider collider = Floating.s_tempColliderArray[i];
			int instanceID = collider.GetInstanceID();
			WaterVolume component;
			if (!Floating.s_waterVolumeCache.TryGetValue(instanceID, out component))
			{
				component = collider.GetComponent<WaterVolume>();
				Floating.s_waterVolumeCache[instanceID] = component;
			}
			if (component)
			{
				if (component.GetLiquidType() == LiquidType.Water)
				{
					float waterSurface = component.GetWaterSurface(p, 1f);
					if (waterSurface > num)
					{
						num = waterSurface;
						previousAndOut = component;
					}
				}
			}
			else
			{
				LiquidSurface component2;
				if (!Floating.s_liquidSurfaceCache.TryGetValue(instanceID, out component2))
				{
					component2 = collider.GetComponent<LiquidSurface>();
					Floating.s_liquidSurfaceCache[instanceID] = component2;
				}
				if (component2 && component2.GetLiquidType() == LiquidType.Water)
				{
					num = Mathf.Max(num, component2.GetSurface(p));
				}
			}
		}
		return num;
	}

	// Token: 0x0600170F RID: 5903 RVA: 0x000AB498 File Offset: 0x000A9698
	public static bool IsUnderWater(Vector3 p, ref WaterVolume previousAndOut)
	{
		if (previousAndOut != null && previousAndOut.gameObject.GetComponent<Collider>().bounds.Contains(p))
		{
			return previousAndOut.GetWaterSurface(p, 1f) > p.y;
		}
		float num = -10000f;
		previousAndOut = null;
		int num2 = Physics.OverlapSphereNonAlloc(p, 0f, Floating.s_tempColliderArray, Floating.s_waterVolumeMask);
		for (int i = 0; i < num2; i++)
		{
			Collider collider = Floating.s_tempColliderArray[i];
			int instanceID = collider.GetInstanceID();
			WaterVolume component;
			if (!Floating.s_waterVolumeCache.TryGetValue(instanceID, out component))
			{
				component = collider.GetComponent<WaterVolume>();
				Floating.s_waterVolumeCache[instanceID] = component;
			}
			if (component)
			{
				if (component.GetLiquidType() == LiquidType.Water)
				{
					float waterSurface = component.GetWaterSurface(p, 1f);
					if (waterSurface > num)
					{
						num = waterSurface;
						previousAndOut = component;
					}
				}
			}
			else
			{
				LiquidSurface component2;
				if (!Floating.s_liquidSurfaceCache.TryGetValue(instanceID, out component2))
				{
					component2 = collider.GetComponent<LiquidSurface>();
					Floating.s_liquidSurfaceCache[instanceID] = component2;
				}
				if (component2 && component2.GetLiquidType() == LiquidType.Water)
				{
					num = Mathf.Max(num, component2.GetSurface(p));
				}
			}
		}
		return num > p.y;
	}

	// Token: 0x06001710 RID: 5904 RVA: 0x000AB5CC File Offset: 0x000A97CC
	public int Increment(LiquidType type)
	{
		int[] liquids = this.m_liquids;
		int num = liquids[(int)type] + 1;
		liquids[(int)type] = num;
		return num;
	}

	// Token: 0x06001711 RID: 5905 RVA: 0x000AB5F0 File Offset: 0x000A97F0
	public int Decrement(LiquidType type)
	{
		int[] liquids = this.m_liquids;
		int num = liquids[(int)type] - 1;
		liquids[(int)type] = num;
		return num;
	}

	// Token: 0x170000C2 RID: 194
	// (get) Token: 0x06001712 RID: 5906 RVA: 0x000AB611 File Offset: 0x000A9811
	public static List<IMonoUpdater> Instances { get; } = new List<IMonoUpdater>();

	// Token: 0x040016F1 RID: 5873
	public float m_waterLevelOffset;

	// Token: 0x040016F2 RID: 5874
	public float m_forceDistance = 1f;

	// Token: 0x040016F3 RID: 5875
	public float m_force = 0.5f;

	// Token: 0x040016F4 RID: 5876
	public float m_balanceForceFraction = 0.02f;

	// Token: 0x040016F5 RID: 5877
	public float m_damping = 0.05f;

	// Token: 0x040016F6 RID: 5878
	public EffectList m_impactEffects = new EffectList();

	// Token: 0x040016F7 RID: 5879
	public GameObject m_surfaceEffects;

	// Token: 0x040016F8 RID: 5880
	private static int s_waterVolumeMask = 0;

	// Token: 0x040016F9 RID: 5881
	private static readonly Collider[] s_tempColliderArray = new Collider[256];

	// Token: 0x040016FA RID: 5882
	private static readonly Dictionary<int, WaterVolume> s_waterVolumeCache = new Dictionary<int, WaterVolume>();

	// Token: 0x040016FB RID: 5883
	private static readonly Dictionary<int, LiquidSurface> s_liquidSurfaceCache = new Dictionary<int, LiquidSurface>();

	// Token: 0x040016FC RID: 5884
	private float m_waterLevel = -10000f;

	// Token: 0x040016FD RID: 5885
	private float m_tarLevel = -10000f;

	// Token: 0x040016FE RID: 5886
	private bool m_beenFloating;

	// Token: 0x040016FF RID: 5887
	private bool m_wasInWater = true;

	// Token: 0x04001700 RID: 5888
	private const float c_MinImpactEffectVelocity = 0.5f;

	// Token: 0x04001701 RID: 5889
	private Rigidbody m_body;

	// Token: 0x04001702 RID: 5890
	private Collider m_collider;

	// Token: 0x04001703 RID: 5891
	private ZNetView m_nview;

	// Token: 0x04001704 RID: 5892
	private readonly int[] m_liquids = new int[2];
}
