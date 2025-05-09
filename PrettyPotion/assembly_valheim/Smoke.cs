using System;
using System.Collections.Generic;
using UnityEngine;

// Token: 0x02000065 RID: 101
public class Smoke : MonoBehaviour, IMonoUpdater
{
	// Token: 0x0600069C RID: 1692 RVA: 0x00037964 File Offset: 0x00035B64
	private void Awake()
	{
		Smoke.s_smoke.Add(this);
		this.m_added = true;
		this.m_body = base.GetComponent<Rigidbody>();
		this.m_body.maxDepenetrationVelocity = 1f;
		this.m_vel = Vector3.up + Quaternion.Euler(0f, (float)UnityEngine.Random.Range(0, 360), 0f) * Vector3.forward * this.m_randomVel;
		this.SetupParticle();
	}

	// Token: 0x0600069D RID: 1693 RVA: 0x000379E8 File Offset: 0x00035BE8
	private void SetupParticle()
	{
		float num = UnityEngine.Random.Range(0f, 360f);
		this.m_renderParticle = new ParticleSystem.Particle
		{
			angularVelocity = 0f,
			angularVelocity3D = Vector3.zero,
			axisOfRotation = new Vector3(0f, 0f, 1f),
			position = base.transform.position,
			randomSeed = (uint)UnityEngine.Random.Range(int.MinValue, int.MaxValue),
			remainingLifetime = this.m_ttl + this.m_fadetime,
			startLifetime = this.m_ttl,
			rotation = num,
			rotation3D = new Vector3(0f, 0f, num),
			velocity = Vector3.zero
		};
	}

	// Token: 0x0600069E RID: 1694 RVA: 0x00037ABC File Offset: 0x00035CBC
	public ParticleSystem.Particle GetParticleValues()
	{
		if (this.m_fadeTimer < 0f)
		{
			this.m_renderParticle.remainingLifetime = this.m_ttl - this.m_time;
		}
		else
		{
			this.m_renderParticle.remainingLifetime = this.m_fadetime - this.m_fadeTimer;
		}
		this.m_renderParticle.position = base.transform.position;
		return this.m_renderParticle;
	}

	// Token: 0x0600069F RID: 1695 RVA: 0x00037B24 File Offset: 0x00035D24
	public float GetAlpha()
	{
		float a = Utils.SmoothStep(0f, 1f, Mathf.Clamp01(this.m_time / 2f));
		float b = Utils.SmoothStep(0f, 1f, 1f - Mathf.Clamp01(this.m_fadeTimer / this.m_fadetime));
		return Mathf.Min(a, b);
	}

	// Token: 0x060006A0 RID: 1696 RVA: 0x00037B7F File Offset: 0x00035D7F
	private void OnEnable()
	{
		Smoke.Instances.Add(this);
		SmokeRenderer.Instance.RegisterSmoke(this);
	}

	// Token: 0x060006A1 RID: 1697 RVA: 0x00037B97 File Offset: 0x00035D97
	private void OnDisable()
	{
		SmokeRenderer.Instance.UnregisterSmoke(this);
		Smoke.Instances.Remove(this);
	}

	// Token: 0x060006A2 RID: 1698 RVA: 0x00037BB0 File Offset: 0x00035DB0
	private void OnDestroy()
	{
		if (this.m_added)
		{
			Smoke.s_smoke.Remove(this);
			this.m_added = false;
		}
	}

	// Token: 0x060006A3 RID: 1699 RVA: 0x00037BD0 File Offset: 0x00035DD0
	public void StartFadeOut()
	{
		if (this.m_fadeTimer >= 0f)
		{
			return;
		}
		if (this.m_added)
		{
			Smoke.s_smoke.Remove(this);
			this.m_added = false;
		}
		this.m_renderParticle.startLifetime = this.m_time + this.m_fadetime;
		this.m_fadeTimer = 0f;
	}

	// Token: 0x060006A4 RID: 1700 RVA: 0x00037C29 File Offset: 0x00035E29
	public static int GetTotalSmoke()
	{
		return Smoke.s_smoke.Count;
	}

	// Token: 0x060006A5 RID: 1701 RVA: 0x00037C35 File Offset: 0x00035E35
	public static void FadeOldest()
	{
		if (Smoke.s_smoke.Count == 0)
		{
			return;
		}
		Smoke.s_smoke[0].StartFadeOut();
	}

	// Token: 0x060006A6 RID: 1702 RVA: 0x00037C54 File Offset: 0x00035E54
	public static void FadeMostDistant()
	{
		if (Smoke.s_smoke.Count == 0)
		{
			return;
		}
		Camera mainCamera = Utils.GetMainCamera();
		if (mainCamera == null)
		{
			return;
		}
		Vector3 position = mainCamera.transform.position;
		int num = -1;
		float num2 = 0f;
		for (int i = 0; i < Smoke.s_smoke.Count; i++)
		{
			float num3 = Vector3.Distance(Smoke.s_smoke[i].transform.position, position);
			if (num3 > num2)
			{
				num = i;
				num2 = num3;
			}
		}
		if (num != -1)
		{
			Smoke.s_smoke[num].StartFadeOut();
		}
	}

	// Token: 0x060006A7 RID: 1703 RVA: 0x00037CE8 File Offset: 0x00035EE8
	public void CustomUpdate(float deltaTime, float time)
	{
		this.m_time += deltaTime;
		if (this.m_time > this.m_ttl && this.m_fadeTimer < 0f)
		{
			this.StartFadeOut();
		}
		float num = 1f - Mathf.Clamp01(this.m_time / this.m_ttl);
		this.m_body.mass = num * num;
		Vector3 velocity = this.m_body.velocity;
		Vector3 vel = this.m_vel;
		vel.y *= num;
		Vector3 a = vel - velocity;
		this.m_body.AddForce(a * (this.m_force * deltaTime), ForceMode.VelocityChange);
		if (this.m_fadeTimer >= 0f)
		{
			this.m_fadeTimer += deltaTime;
			Mathf.Clamp01(this.m_fadeTimer / this.m_fadetime);
			if (this.m_fadeTimer >= this.m_fadetime)
			{
				UnityEngine.Object.Destroy(base.gameObject);
			}
		}
	}

	// Token: 0x17000019 RID: 25
	// (get) Token: 0x060006A8 RID: 1704 RVA: 0x00037DD3 File Offset: 0x00035FD3
	// (set) Token: 0x060006A9 RID: 1705 RVA: 0x00037DDB File Offset: 0x00035FDB
	public Vector3Int RenderChunk { get; set; } = Vector3Int.zero;

	// Token: 0x1700001A RID: 26
	// (get) Token: 0x060006AA RID: 1706 RVA: 0x00037DE4 File Offset: 0x00035FE4
	public static List<IMonoUpdater> Instances { get; } = new List<IMonoUpdater>();

	// Token: 0x040007B3 RID: 1971
	public Vector3 m_vel = Vector3.up;

	// Token: 0x040007B4 RID: 1972
	public float m_randomVel = 0.1f;

	// Token: 0x040007B5 RID: 1973
	public float m_force = 0.1f;

	// Token: 0x040007B6 RID: 1974
	public float m_ttl = 10f;

	// Token: 0x040007B7 RID: 1975
	public float m_fadetime = 3f;

	// Token: 0x040007B8 RID: 1976
	private Rigidbody m_body;

	// Token: 0x040007B9 RID: 1977
	private float m_time;

	// Token: 0x040007BA RID: 1978
	private float m_fadeTimer = -1f;

	// Token: 0x040007BB RID: 1979
	private bool m_added;

	// Token: 0x040007BD RID: 1981
	private ParticleSystem.Particle m_renderParticle;

	// Token: 0x040007BE RID: 1982
	private static readonly List<Smoke> s_smoke = new List<Smoke>();
}
