using System;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;
using UnityEngine.ParticleSystemJobs;

// Token: 0x0200006A RID: 106
[ExecuteAlways]
public class VortexParticles : MonoBehaviour
{
	// Token: 0x060006CD RID: 1741 RVA: 0x00038C20 File Offset: 0x00036E20
	private void Start()
	{
		this.ps = base.GetComponent<ParticleSystem>();
		if (this.ps == null)
		{
			ZLog.LogWarning("VortexParticles object '" + base.gameObject.name + "' is missing a particle system and disabled!");
			this.effectOn = false;
		}
	}

	// Token: 0x060006CE RID: 1742 RVA: 0x00038C70 File Offset: 0x00036E70
	private void Update()
	{
		if (this.ps.main.simulationSpace == ParticleSystemSimulationSpace.Local)
		{
			this.job.vortexCenter = this.centerOffset;
			this.job.upDir = new Vector3(0f, 1f, 0f);
		}
		else
		{
			this.job.vortexCenter = base.transform.position + this.centerOffset;
			this.job.upDir = base.transform.up;
		}
		this.job.pullStrength = this.pullStrength;
		this.job.vortexStrength = this.vortexStrength;
		this.job.lineAttraction = this.lineAttraction;
		this.job.useCustomData = this.useCustomData;
		this.job.deltaTime = Time.deltaTime;
		this.job.distanceStrengthFalloff = this.distanceStrengthFalloff;
	}

	// Token: 0x060006CF RID: 1743 RVA: 0x00038D60 File Offset: 0x00036F60
	private void OnParticleUpdateJobScheduled()
	{
		if (this.ps == null)
		{
			this.ps = base.GetComponent<ParticleSystem>();
			if (this.ps == null)
			{
				ZLog.LogWarning("VortexParticles object '" + base.gameObject.name + "' is missing a particle system and disabled!");
				this.effectOn = false;
			}
		}
		if (this.effectOn)
		{
			this.job.Schedule(this.ps, 1024, default(JobHandle));
		}
	}

	// Token: 0x040007F3 RID: 2035
	private ParticleSystem ps;

	// Token: 0x040007F4 RID: 2036
	private VortexParticles.VortexParticlesJob job;

	// Token: 0x040007F5 RID: 2037
	[SerializeField]
	private bool effectOn = true;

	// Token: 0x040007F6 RID: 2038
	[SerializeField]
	private Vector3 centerOffset;

	// Token: 0x040007F7 RID: 2039
	[SerializeField]
	private float pullStrength;

	// Token: 0x040007F8 RID: 2040
	[SerializeField]
	private float vortexStrength;

	// Token: 0x040007F9 RID: 2041
	[SerializeField]
	private bool lineAttraction;

	// Token: 0x040007FA RID: 2042
	[SerializeField]
	private bool useCustomData;

	// Token: 0x040007FB RID: 2043
	[SerializeField]
	private bool distanceStrengthFalloff;

	// Token: 0x02000263 RID: 611
	private struct VortexParticlesJob : IJobParticleSystemParallelFor
	{
		// Token: 0x06001F3F RID: 7999 RVA: 0x000E2B68 File Offset: 0x000E0D68
		public void Execute(ParticleSystemJobData particles, int i)
		{
			ParticleSystemNativeArray3 particleSystemNativeArray = particles.velocities;
			float x = particleSystemNativeArray.x[i];
			particleSystemNativeArray = particles.velocities;
			float y = particleSystemNativeArray.y[i];
			particleSystemNativeArray = particles.velocities;
			Vector3 a = new Vector3(x, y, particleSystemNativeArray.z[i]);
			particleSystemNativeArray = particles.positions;
			float x2 = particleSystemNativeArray.x[i];
			particleSystemNativeArray = particles.positions;
			float y2 = particleSystemNativeArray.y[i];
			particleSystemNativeArray = particles.positions;
			Vector3 vector = new Vector3(x2, y2, particleSystemNativeArray.z[i]);
			Vector3 a2 = this.vortexCenter;
			float num = this.useCustomData ? particles.customData1.x[i] : this.vortexStrength;
			if (this.lineAttraction)
			{
				a2.y = vector.y;
			}
			Vector3 vector2 = a2 - vector;
			if (this.distanceStrengthFalloff)
			{
				float num2 = Vector3.Magnitude(vector2);
				num *= -num2 / Mathf.Sqrt(num2);
			}
			vector2 = Vector3.Normalize(vector2);
			Vector3 a3 = Vector3.Cross(Vector3.Normalize(vector2), this.upDir);
			Vector3 vector3 = a + vector2 * this.pullStrength * this.deltaTime;
			vector3 += a3 * num * this.deltaTime;
			NativeArray<float> x3 = particles.velocities.x;
			NativeArray<float> y3 = particles.velocities.y;
			NativeArray<float> z = particles.velocities.z;
			x3[i] = vector3.x;
			y3[i] = vector3.y;
			z[i] = vector3.z;
		}

		// Token: 0x040020AE RID: 8366
		[ReadOnly]
		public Vector3 vortexCenter;

		// Token: 0x040020AF RID: 8367
		[ReadOnly]
		public float pullStrength;

		// Token: 0x040020B0 RID: 8368
		[ReadOnly]
		public Vector3 upDir;

		// Token: 0x040020B1 RID: 8369
		[ReadOnly]
		public float vortexStrength;

		// Token: 0x040020B2 RID: 8370
		[ReadOnly]
		public bool lineAttraction;

		// Token: 0x040020B3 RID: 8371
		[ReadOnly]
		public bool useCustomData;

		// Token: 0x040020B4 RID: 8372
		[ReadOnly]
		public float deltaTime;

		// Token: 0x040020B5 RID: 8373
		[ReadOnly]
		public bool distanceStrengthFalloff;
	}
}
