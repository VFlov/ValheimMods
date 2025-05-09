using System;
using System.Collections.Generic;
using UnityEngine;

// Token: 0x020001A9 RID: 425
public class RandomFlyingBird : MonoBehaviour, IMonoUpdater
{
	// Token: 0x060018F1 RID: 6385 RVA: 0x000BA4EC File Offset: 0x000B86EC
	private void Awake()
	{
		this.m_nview = base.GetComponent<ZNetView>();
		this.m_anim = base.GetComponentInChildren<ZSyncAnimation>();
		this.m_lodGroup = base.GetComponent<LODGroup>();
		if (!this.m_singleModel)
		{
			this.m_landedModel.SetActive(true);
			this.m_flyingModel.SetActive(true);
		}
		else
		{
			this.m_flyingModel.SetActive(true);
		}
		this.m_idleTargetTime = UnityEngine.Random.Range(this.m_randomIdleTimeMin, this.m_randomIdleTimeMax);
		this.m_spawnPoint = this.m_nview.GetZDO().GetVec3(ZDOVars.s_spawnPoint, base.transform.position);
		if (this.m_nview.IsOwner())
		{
			this.m_nview.GetZDO().Set(ZDOVars.s_spawnPoint, this.m_spawnPoint);
		}
		this.m_randomNoiseTimer = UnityEngine.Random.Range(this.m_randomNoiseIntervalMin, this.m_randomNoiseIntervalMax);
		if (this.m_nview.IsOwner())
		{
			this.RandomizeWaypoint(false);
		}
		if (this.m_lodGroup)
		{
			this.m_originalLocalRef = this.m_lodGroup.localReferencePoint;
		}
	}

	// Token: 0x060018F2 RID: 6386 RVA: 0x000BA5F9 File Offset: 0x000B87F9
	private void OnEnable()
	{
		if (this.m_nview != null)
		{
			RandomFlyingBird.Instances.Add(this);
		}
	}

	// Token: 0x060018F3 RID: 6387 RVA: 0x000BA614 File Offset: 0x000B8814
	private void OnDisable()
	{
		RandomFlyingBird.Instances.Remove(this);
	}

	// Token: 0x060018F4 RID: 6388 RVA: 0x000BA624 File Offset: 0x000B8824
	public void CustomFixedUpdate(float dt)
	{
		if (!this.m_nview.IsValid())
		{
			return;
		}
		bool flag = EnvMan.IsDaylight();
		this.m_randomNoiseTimer -= dt;
		if (this.m_randomNoiseTimer <= 0f)
		{
			if (flag || !this.m_noNoiseAtNight)
			{
				this.m_randomNoise.Create(base.transform.position, Quaternion.identity, base.transform, 1f, -1);
			}
			this.m_randomNoiseTimer = UnityEngine.Random.Range(this.m_randomNoiseIntervalMin, this.m_randomNoiseIntervalMax);
		}
		bool @bool = this.m_nview.GetZDO().GetBool(ZDOVars.s_landed, false);
		if (!this.m_singleModel)
		{
			this.m_landedModel.SetActive(@bool);
			this.m_flyingModel.SetActive(!@bool);
		}
		this.SetVisible(this.m_nview.HasOwner());
		if (!this.m_nview.IsOwner())
		{
			return;
		}
		this.m_flyTimer += dt;
		this.m_modeTimer += dt;
		if (this.m_singleModel)
		{
			this.m_anim.SetBool(RandomFlyingBird.s_flying, !@bool);
		}
		if (@bool)
		{
			Vector3 forward = base.transform.forward;
			forward.y = 0f;
			forward.Normalize();
			base.transform.rotation = Quaternion.LookRotation(forward, Vector3.up);
			if (this.m_randomIdles > 0)
			{
				this.m_idleTimer += Time.fixedDeltaTime;
				if (this.m_idleTimer > this.m_idleTargetTime)
				{
					this.m_idleTargetTime = UnityEngine.Random.Range(this.m_randomIdleTimeMin, this.m_randomIdleTimeMax);
					this.m_idleTimer = 0f;
					this.m_anim.SetFloat(RandomFlyingBird.s_idle, (float)UnityEngine.Random.Range(0, this.m_randomIdles));
				}
			}
			this.m_landedTimer += dt;
			if (((flag || !this.m_noRandomFlightAtNight) && this.m_landedTimer > this.m_landDuration) || this.DangerNearby(base.transform.position))
			{
				this.m_nview.GetZDO().Set(ZDOVars.s_landed, false);
				this.RandomizeWaypoint(false);
				return;
			}
		}
		else
		{
			if (this.m_flapping)
			{
				if (this.m_modeTimer > this.m_flapDuration)
				{
					this.m_modeTimer = 0f;
					this.m_flapping = false;
				}
			}
			else if (this.m_modeTimer > this.m_sailDuration)
			{
				this.m_flapping = true;
				this.m_modeTimer = 0f;
			}
			this.m_anim.SetBool(RandomFlyingBird.s_flapping, this.m_flapping);
			Vector3 vector = Vector3.Normalize(this.m_waypoint - base.transform.position);
			float num = this.m_groundwp ? (this.m_turnRate * 4f) : this.m_turnRate;
			Vector3 vector2 = Vector3.RotateTowards(base.transform.forward, vector, num * 0.017453292f * dt, 1f);
			float num2 = Vector3.SignedAngle(base.transform.forward, vector, Vector3.up);
			Vector3 a = Vector3.Cross(vector2, Vector3.up);
			Vector3 a2 = Vector3.up;
			if (num2 > 0f)
			{
				a2 += -a * 1.5f * Utils.LerpStep(0f, 45f, num2);
			}
			else
			{
				a2 += a * 1.5f * Utils.LerpStep(0f, 45f, -num2);
			}
			float num3 = this.m_speed;
			bool flag2 = false;
			if (this.m_groundwp)
			{
				float num4 = Vector3.Distance(base.transform.position, this.m_waypoint);
				if (num4 < 5f)
				{
					num3 *= Mathf.Clamp(num4 / 5f, 0.2f, 1f);
					vector2.y = 0f;
					vector2.Normalize();
					a2 = Vector3.up;
					flag2 = true;
				}
				if (num4 < 0.2f)
				{
					base.transform.position = this.m_waypoint;
					this.m_nview.GetZDO().Set(ZDOVars.s_landed, true);
					this.m_landedTimer = 0f;
					this.m_flapping = true;
					this.m_modeTimer = 0f;
				}
			}
			else if (this.m_flyTimer >= this.m_wpDuration)
			{
				bool ground = UnityEngine.Random.value < this.m_landChance;
				this.RandomizeWaypoint(ground);
			}
			Quaternion to = Quaternion.LookRotation(vector2, a2.normalized);
			base.transform.rotation = Quaternion.RotateTowards(base.transform.rotation, to, 200f * dt);
			if (flag2)
			{
				base.transform.position += vector * num3 * dt;
				return;
			}
			base.transform.position += base.transform.forward * num3 * dt;
		}
	}

	// Token: 0x060018F5 RID: 6389 RVA: 0x000BAB00 File Offset: 0x000B8D00
	private void RandomizeWaypoint(bool ground)
	{
		this.m_flyTimer = 0f;
		Vector3 waypoint;
		if (ground && this.FindLandingPoint(out waypoint))
		{
			this.m_waypoint = waypoint;
			this.m_groundwp = true;
			return;
		}
		Vector2 vector = UnityEngine.Random.insideUnitCircle * this.m_flyRange;
		this.m_waypoint = this.m_spawnPoint + new Vector3(vector.x, 0f, vector.y);
		float num;
		if (ZoneSystem.instance.GetSolidHeight(this.m_waypoint, out num, 1000))
		{
			float num2 = 32f;
			if (num < num2)
			{
				num = num2;
			}
			this.m_waypoint.y = num + UnityEngine.Random.Range(this.m_minAlt, this.m_maxAlt);
		}
		this.m_groundwp = false;
	}

	// Token: 0x060018F6 RID: 6390 RVA: 0x000BABB8 File Offset: 0x000B8DB8
	private bool FindLandingPoint(out Vector3 waypoint)
	{
		waypoint = new Vector3(0f, -999f, 0f);
		bool result = false;
		for (int i = 0; i < 10; i++)
		{
			Vector2 vector = UnityEngine.Random.insideUnitCircle * this.m_flyRange;
			Vector3 vector2 = this.m_spawnPoint + new Vector3(vector.x, 0f, vector.y);
			float num;
			if (ZoneSystem.instance.GetSolidHeight(vector2, out num, 1000) && num > 30f && num > waypoint.y)
			{
				vector2.y = num;
				if (!this.DangerNearby(vector2))
				{
					waypoint = vector2;
					result = true;
				}
			}
		}
		return result;
	}

	// Token: 0x060018F7 RID: 6391 RVA: 0x000BAC64 File Offset: 0x000B8E64
	private bool DangerNearby(Vector3 p)
	{
		return Player.IsPlayerInRange(p, this.m_avoidDangerDistance);
	}

	// Token: 0x060018F8 RID: 6392 RVA: 0x000BAC74 File Offset: 0x000B8E74
	private void SetVisible(bool visible)
	{
		if (this.m_lodGroup == null)
		{
			return;
		}
		if (this.m_lodVisible == visible)
		{
			return;
		}
		this.m_lodVisible = visible;
		if (this.m_lodVisible)
		{
			this.m_lodGroup.localReferencePoint = this.m_originalLocalRef;
			return;
		}
		this.m_lodGroup.localReferencePoint = new Vector3(999999f, 999999f, 999999f);
	}

	// Token: 0x170000C8 RID: 200
	// (get) Token: 0x060018F9 RID: 6393 RVA: 0x000BACDA File Offset: 0x000B8EDA
	public static List<IMonoUpdater> Instances { get; } = new List<IMonoUpdater>();

	// Token: 0x04001924 RID: 6436
	public float m_flyRange = 20f;

	// Token: 0x04001925 RID: 6437
	public float m_minAlt = 5f;

	// Token: 0x04001926 RID: 6438
	public float m_maxAlt = 20f;

	// Token: 0x04001927 RID: 6439
	public float m_speed = 10f;

	// Token: 0x04001928 RID: 6440
	public float m_turnRate = 10f;

	// Token: 0x04001929 RID: 6441
	public float m_wpDuration = 4f;

	// Token: 0x0400192A RID: 6442
	public float m_flapDuration = 2f;

	// Token: 0x0400192B RID: 6443
	public float m_sailDuration = 4f;

	// Token: 0x0400192C RID: 6444
	public float m_landChance = 0.5f;

	// Token: 0x0400192D RID: 6445
	public float m_landDuration = 2f;

	// Token: 0x0400192E RID: 6446
	public float m_avoidDangerDistance = 4f;

	// Token: 0x0400192F RID: 6447
	public bool m_noRandomFlightAtNight = true;

	// Token: 0x04001930 RID: 6448
	public float m_randomNoiseIntervalMin = 3f;

	// Token: 0x04001931 RID: 6449
	public float m_randomNoiseIntervalMax = 6f;

	// Token: 0x04001932 RID: 6450
	public bool m_noNoiseAtNight = true;

	// Token: 0x04001933 RID: 6451
	public EffectList m_randomNoise = new EffectList();

	// Token: 0x04001934 RID: 6452
	public int m_randomIdles;

	// Token: 0x04001935 RID: 6453
	public float m_randomIdleTimeMin = 1f;

	// Token: 0x04001936 RID: 6454
	public float m_randomIdleTimeMax = 4f;

	// Token: 0x04001937 RID: 6455
	public bool m_singleModel;

	// Token: 0x04001938 RID: 6456
	public GameObject m_flyingModel;

	// Token: 0x04001939 RID: 6457
	public GameObject m_landedModel;

	// Token: 0x0400193A RID: 6458
	private Vector3 m_spawnPoint;

	// Token: 0x0400193B RID: 6459
	private Vector3 m_waypoint;

	// Token: 0x0400193C RID: 6460
	private bool m_groundwp;

	// Token: 0x0400193D RID: 6461
	private float m_flyTimer;

	// Token: 0x0400193E RID: 6462
	private float m_modeTimer;

	// Token: 0x0400193F RID: 6463
	private float m_idleTimer;

	// Token: 0x04001940 RID: 6464
	private float m_idleTargetTime = 1f;

	// Token: 0x04001941 RID: 6465
	private float m_randomNoiseTimer;

	// Token: 0x04001942 RID: 6466
	private ZSyncAnimation m_anim;

	// Token: 0x04001943 RID: 6467
	private bool m_flapping = true;

	// Token: 0x04001944 RID: 6468
	private float m_landedTimer;

	// Token: 0x04001945 RID: 6469
	private static readonly int s_flapping = ZSyncAnimation.GetHash("flapping");

	// Token: 0x04001946 RID: 6470
	private static readonly int s_flying = ZSyncAnimation.GetHash("flying");

	// Token: 0x04001947 RID: 6471
	private static readonly int s_idle = ZSyncAnimation.GetHash("idle");

	// Token: 0x04001948 RID: 6472
	private ZNetView m_nview;

	// Token: 0x04001949 RID: 6473
	protected LODGroup m_lodGroup;

	// Token: 0x0400194A RID: 6474
	private Vector3 m_originalLocalRef;

	// Token: 0x0400194B RID: 6475
	private bool m_lodVisible = true;
}
