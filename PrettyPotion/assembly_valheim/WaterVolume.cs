using System;
using System.Collections.Generic;
using UnityEngine;

// Token: 0x020001DB RID: 475
public class WaterVolume : MonoBehaviour
{
	// Token: 0x06001B39 RID: 6969 RVA: 0x000CB750 File Offset: 0x000C9950
	private void Awake()
	{
		this.m_collider = base.GetComponent<Collider>();
		if (WaterVolume.s_createWaveTangents == null)
		{
			WaterVolume.s_createWaveTangents = new Vector2[]
			{
				new Vector2(-WaterVolume.s_createWaveDirections[0].y, WaterVolume.s_createWaveDirections[0].x),
				new Vector2(-WaterVolume.s_createWaveDirections[1].y, WaterVolume.s_createWaveDirections[1].x),
				new Vector2(-WaterVolume.s_createWaveDirections[2].y, WaterVolume.s_createWaveDirections[2].x),
				new Vector2(-WaterVolume.s_createWaveDirections[3].y, WaterVolume.s_createWaveDirections[3].x),
				new Vector2(-WaterVolume.s_createWaveDirections[4].y, WaterVolume.s_createWaveDirections[4].x),
				new Vector2(-WaterVolume.s_createWaveDirections[5].y, WaterVolume.s_createWaveDirections[5].x),
				new Vector2(-WaterVolume.s_createWaveDirections[6].y, WaterVolume.s_createWaveDirections[6].x),
				new Vector2(-WaterVolume.s_createWaveDirections[7].y, WaterVolume.s_createWaveDirections[7].x),
				new Vector2(-WaterVolume.s_createWaveDirections[8].y, WaterVolume.s_createWaveDirections[8].x),
				new Vector2(-WaterVolume.s_createWaveDirections[9].y, WaterVolume.s_createWaveDirections[9].x)
			};
		}
	}

	// Token: 0x06001B3A RID: 6970 RVA: 0x000CB944 File Offset: 0x000C9B44
	private void Start()
	{
		this.DetectWaterDepth();
		this.SetupMaterial();
	}

	// Token: 0x06001B3B RID: 6971 RVA: 0x000CB952 File Offset: 0x000C9B52
	private void OnEnable()
	{
		WaterVolume.Instances.Add(this);
	}

	// Token: 0x06001B3C RID: 6972 RVA: 0x000CB95F File Offset: 0x000C9B5F
	private void OnDisable()
	{
		WaterVolume.Instances.Remove(this);
	}

	// Token: 0x06001B3D RID: 6973 RVA: 0x000CB970 File Offset: 0x000C9B70
	private void DetectWaterDepth()
	{
		if (this.m_heightmap)
		{
			float[] oceanDepth = this.m_heightmap.GetOceanDepth();
			this.m_normalizedDepth[0] = Mathf.Clamp01(oceanDepth[0] / 10f);
			this.m_normalizedDepth[1] = Mathf.Clamp01(oceanDepth[1] / 10f);
			this.m_normalizedDepth[2] = Mathf.Clamp01(oceanDepth[2] / 10f);
			this.m_normalizedDepth[3] = Mathf.Clamp01(oceanDepth[3] / 10f);
			if (this.m_normalizedDepth[0].Equals(this.m_normalizedDepth[2]) && this.m_normalizedDepth[1].Equals(this.m_normalizedDepth[3]) && this.m_normalizedDepth[0].Equals(this.m_normalizedDepth[1]))
			{
				this.m_oneDepth = this.m_normalizedDepth[0];
				return;
			}
		}
		else
		{
			this.m_normalizedDepth[0] = this.m_forceDepth;
			this.m_normalizedDepth[1] = this.m_forceDepth;
			this.m_normalizedDepth[2] = this.m_forceDepth;
			this.m_normalizedDepth[3] = this.m_forceDepth;
			this.m_oneDepth = this.m_forceDepth;
		}
	}

	// Token: 0x06001B3E RID: 6974 RVA: 0x000CBA98 File Offset: 0x000C9C98
	public static void StaticUpdate()
	{
		WaterVolume.UpdateWaterTime(Time.deltaTime);
		if (EnvMan.instance)
		{
			EnvMan.instance.GetWindData(out WaterVolume.s_globalWind1, out WaterVolume.s_globalWind2, out WaterVolume.s_globalWindAlpha);
		}
	}

	// Token: 0x06001B3F RID: 6975 RVA: 0x000CBACC File Offset: 0x000C9CCC
	private static void UpdateWaterTime(float dt)
	{
		WaterVolume.s_wrappedDayTimeSeconds = ZNet.instance.GetWrappedDayTimeSeconds();
		float num = WaterVolume.s_wrappedDayTimeSeconds;
		WaterVolume.s_waterTime += dt;
		if (Mathf.Abs(num - WaterVolume.s_waterTime) > 10f)
		{
			WaterVolume.s_waterTime = num;
		}
		WaterVolume.s_waterTime = Mathf.Lerp(WaterVolume.s_waterTime, num, 0.05f);
	}

	// Token: 0x06001B40 RID: 6976 RVA: 0x000CBB28 File Offset: 0x000C9D28
	public void UpdateMaterials()
	{
		this.m_waterSurface.material.SetFloat(WaterVolume.s_shaderWaterTime, WaterVolume.s_waterTime);
	}

	// Token: 0x06001B41 RID: 6977 RVA: 0x000CBB44 File Offset: 0x000C9D44
	private void SetupMaterial()
	{
		if (this.m_forceDepth >= 0f)
		{
			this.m_waterSurface.material.SetFloatArray(WaterVolume.s_shaderDepth, new float[]
			{
				this.m_forceDepth,
				this.m_forceDepth,
				this.m_forceDepth,
				this.m_forceDepth
			});
		}
		else
		{
			this.m_waterSurface.material.SetFloatArray(WaterVolume.s_shaderDepth, this.m_normalizedDepth);
		}
		this.m_waterSurface.material.SetFloat(WaterVolume.s_shaderUseGlobalWind, this.m_useGlobalWind ? 1f : 0f);
	}

	// Token: 0x06001B42 RID: 6978 RVA: 0x000CBBE3 File Offset: 0x000C9DE3
	public LiquidType GetLiquidType()
	{
		return LiquidType.Water;
	}

	// Token: 0x06001B43 RID: 6979 RVA: 0x000CBBE8 File Offset: 0x000C9DE8
	public float GetWaterSurface(Vector3 point, float waveFactor = 1f)
	{
		float num = 0f;
		if (this.m_useGlobalWind)
		{
			float waterTime = WaterVolume.s_wrappedDayTimeSeconds;
			float num2 = this.Depth(point);
			num = ((num2 == 0f) ? 0f : this.CalcWave(point, num2, waterTime, waveFactor));
		}
		float num3 = base.transform.position.y + num + this.m_surfaceOffset;
		if (this.m_forceDepth < 0f && Utils.LengthXZ(point) > 10500f)
		{
			num3 -= 100f;
		}
		return num3;
	}

	// Token: 0x06001B44 RID: 6980 RVA: 0x000CBC68 File Offset: 0x000C9E68
	private float TrochSin(float x, float k)
	{
		return Mathf.Sin(x - Mathf.Cos(x) * k) * 0.5f + 0.5f;
	}

	// Token: 0x06001B45 RID: 6981 RVA: 0x000CBC88 File Offset: 0x000C9E88
	private float CreateWave(Vector3 worldPos, float time, float waveSpeed, float waveLength, float waveHeight, Vector2 dir, Vector2 tangent, float sharpness)
	{
		Vector2 vector = -(worldPos.z * dir + worldPos.x * tangent);
		float num = time * waveSpeed;
		return (this.TrochSin(num + vector.y * waveLength, sharpness) * this.TrochSin(num * 0.123f + vector.x * 0.13123f * waveLength, sharpness) - 0.2f) * waveHeight;
	}

	// Token: 0x06001B46 RID: 6982 RVA: 0x000CBCFC File Offset: 0x000C9EFC
	private float CalcWave(Vector3 worldPos, float depth, Vector4 wind, float waterTime, float waveFactor)
	{
		WaterVolume.s_createWaveDirections[0].x = wind.x;
		WaterVolume.s_createWaveDirections[0].y = wind.z;
		WaterVolume.s_createWaveDirections[0].Normalize();
		WaterVolume.s_createWaveTangents[0].x = -WaterVolume.s_createWaveDirections[0].y;
		WaterVolume.s_createWaveTangents[0].y = WaterVolume.s_createWaveDirections[0].x;
		float w = wind.w;
		float num = Mathf.Lerp(0f, w, depth);
		float time = waterTime / 20f;
		float num2 = this.CreateWave(worldPos, time, 10f, 0.04f, 8f, WaterVolume.s_createWaveDirections[0], WaterVolume.s_createWaveTangents[0], 0.5f);
		float num3 = this.CreateWave(worldPos, time, 14.123f, 0.08f, 6f, WaterVolume.s_createWaveDirections[1], WaterVolume.s_createWaveTangents[1], 0.5f);
		float num4 = this.CreateWave(worldPos, time, 22.312f, 0.1f, 4f, WaterVolume.s_createWaveDirections[2], WaterVolume.s_createWaveTangents[2], 0.5f);
		float num5 = this.CreateWave(worldPos, time, 31.42f, 0.2f, 2f, WaterVolume.s_createWaveDirections[3], WaterVolume.s_createWaveTangents[3], 0.5f);
		float num6 = this.CreateWave(worldPos, time, 35.42f, 0.4f, 1f, WaterVolume.s_createWaveDirections[4], WaterVolume.s_createWaveTangents[4], 0.5f);
		float num7 = this.CreateWave(worldPos, time, 38.1223f, 1f, 0.8f, WaterVolume.s_createWaveDirections[5], WaterVolume.s_createWaveTangents[5], 0.7f);
		float num8 = this.CreateWave(worldPos, time, 41.1223f, 1.2f, 0.6f * waveFactor, WaterVolume.s_createWaveDirections[6], WaterVolume.s_createWaveTangents[6], 0.8f);
		float num9 = this.CreateWave(worldPos, time, 51.5123f, 1.3f, 0.4f * waveFactor, WaterVolume.s_createWaveDirections[7], WaterVolume.s_createWaveTangents[7], 0.9f);
		float num10 = this.CreateWave(worldPos, time, 54.2f, 1.3f, 0.3f * waveFactor, WaterVolume.s_createWaveDirections[8], WaterVolume.s_createWaveTangents[8], 0.9f);
		float num11 = this.CreateWave(worldPos, time, 56.123f, 1.5f, 0.2f * waveFactor, WaterVolume.s_createWaveDirections[9], WaterVolume.s_createWaveTangents[9], 0.9f);
		return (num2 + num3 + num4 + num5 + num6 + num7 + num8 + num9 + num10 + num11) * num;
	}

	// Token: 0x06001B47 RID: 6983 RVA: 0x000CBFD4 File Offset: 0x000CA1D4
	public float CalcWave(Vector3 worldPos, float depth, float waterTime, float waveFactor)
	{
		if (WaterVolume.s_globalWindAlpha == 0f)
		{
			return this.CalcWave(worldPos, depth, WaterVolume.s_globalWind1, waterTime, waveFactor);
		}
		float a = this.CalcWave(worldPos, depth, WaterVolume.s_globalWind1, waterTime, waveFactor);
		float b = this.CalcWave(worldPos, depth, WaterVolume.s_globalWind2, waterTime, waveFactor);
		return Mathf.LerpUnclamped(a, b, WaterVolume.s_globalWindAlpha);
	}

	// Token: 0x06001B48 RID: 6984 RVA: 0x000CC02C File Offset: 0x000CA22C
	public float Depth(Vector3 point)
	{
		if (this.m_oneDepth > -1f)
		{
			return this.m_oneDepth;
		}
		Vector3 vector = base.transform.InverseTransformPoint(point);
		Vector3 size = this.m_collider.bounds.size;
		float t = (vector.x + size.x / 2f) / size.x;
		float t2 = (vector.z + size.z / 2f) / size.z;
		float a = Mathf.Lerp(this.m_normalizedDepth[3], this.m_normalizedDepth[2], t);
		float b = Mathf.Lerp(this.m_normalizedDepth[0], this.m_normalizedDepth[1], t);
		return Mathf.Lerp(a, b, t2);
	}

	// Token: 0x06001B49 RID: 6985 RVA: 0x000CC0D8 File Offset: 0x000CA2D8
	private void OnTriggerEnter(Collider triggerCollider)
	{
		IWaterInteractable component = triggerCollider.attachedRigidbody.GetComponent<IWaterInteractable>();
		if (component == null)
		{
			return;
		}
		component.Increment(LiquidType.Water);
		if (!this.m_inWater.Contains(component))
		{
			this.m_inWater.Add(component);
		}
	}

	// Token: 0x06001B4A RID: 6986 RVA: 0x000CC118 File Offset: 0x000CA318
	public void UpdateFloaters()
	{
		int count = this.m_inWater.Count;
		if (count == 0)
		{
			return;
		}
		WaterVolume.s_inWaterRemoveIndices.Clear();
		for (int i = 0; i < count; i++)
		{
			IWaterInteractable waterInteractable = this.m_inWater[i];
			if (waterInteractable == null)
			{
				WaterVolume.s_inWaterRemoveIndices.Add(i);
			}
			else
			{
				Transform transform = waterInteractable.GetTransform();
				if (transform)
				{
					float waterSurface = this.GetWaterSurface(transform.position, 1f);
					waterInteractable.SetLiquidLevel(waterSurface, LiquidType.Water, this);
				}
				else
				{
					WaterVolume.s_inWaterRemoveIndices.Add(i);
				}
			}
		}
		for (int j = WaterVolume.s_inWaterRemoveIndices.Count - 1; j >= 0; j--)
		{
			this.m_inWater.RemoveAt(WaterVolume.s_inWaterRemoveIndices[j]);
		}
	}

	// Token: 0x06001B4B RID: 6987 RVA: 0x000CC1D4 File Offset: 0x000CA3D4
	private void OnTriggerExit(Collider triggerCollider)
	{
		IWaterInteractable component = triggerCollider.attachedRigidbody.GetComponent<IWaterInteractable>();
		if (component == null)
		{
			return;
		}
		if (component.Decrement(LiquidType.Water) == 0)
		{
			component.SetLiquidLevel(-10000f, LiquidType.Water, this);
		}
		this.m_inWater.Remove(component);
	}

	// Token: 0x06001B4C RID: 6988 RVA: 0x000CC214 File Offset: 0x000CA414
	private void OnDestroy()
	{
		foreach (IWaterInteractable waterInteractable in this.m_inWater)
		{
			if (waterInteractable != null && waterInteractable.Decrement(LiquidType.Water) == 0)
			{
				waterInteractable.SetLiquidLevel(-10000f, LiquidType.Water, this);
			}
		}
		this.m_inWater.Clear();
	}

	// Token: 0x06001B4D RID: 6989 RVA: 0x000CC284 File Offset: 0x000CA484
	private void OnDrawGizmosSelected()
	{
		Gizmos.color = Color.yellow;
		Gizmos.DrawWireCube(base.transform.position + Vector3.up * this.m_surfaceOffset, new Vector3(2f, 0.05f, 2f));
	}

	// Token: 0x170000CD RID: 205
	// (get) Token: 0x06001B4E RID: 6990 RVA: 0x000CC2D4 File Offset: 0x000CA4D4
	public static List<WaterVolume> Instances { get; } = new List<WaterVolume>();

	// Token: 0x04001BFC RID: 7164
	private Collider m_collider;

	// Token: 0x04001BFD RID: 7165
	private readonly float[] m_normalizedDepth = new float[4];

	// Token: 0x04001BFE RID: 7166
	private readonly List<IWaterInteractable> m_inWater = new List<IWaterInteractable>();

	// Token: 0x04001BFF RID: 7167
	private float m_oneDepth = -1f;

	// Token: 0x04001C00 RID: 7168
	public MeshRenderer m_waterSurface;

	// Token: 0x04001C01 RID: 7169
	public Heightmap m_heightmap;

	// Token: 0x04001C02 RID: 7170
	public float m_forceDepth = -1f;

	// Token: 0x04001C03 RID: 7171
	public float m_surfaceOffset;

	// Token: 0x04001C04 RID: 7172
	public bool m_useGlobalWind = true;

	// Token: 0x04001C05 RID: 7173
	private const bool c_MenuWater = false;

	// Token: 0x04001C06 RID: 7174
	private static float s_waterTime = 0f;

	// Token: 0x04001C07 RID: 7175
	private static readonly int s_shaderWaterTime = Shader.PropertyToID("_WaterTime");

	// Token: 0x04001C08 RID: 7176
	private static readonly int s_shaderDepth = Shader.PropertyToID("_depth");

	// Token: 0x04001C09 RID: 7177
	private static readonly int s_shaderUseGlobalWind = Shader.PropertyToID("_UseGlobalWind");

	// Token: 0x04001C0A RID: 7178
	private static Vector4 s_globalWind1 = new Vector4(1f, 0f, 0f, 0f);

	// Token: 0x04001C0B RID: 7179
	private static Vector4 s_globalWind2 = new Vector4(1f, 0f, 0f, 0f);

	// Token: 0x04001C0C RID: 7180
	private static float s_globalWindAlpha = 0f;

	// Token: 0x04001C0D RID: 7181
	private static float s_wrappedDayTimeSeconds = 0f;

	// Token: 0x04001C0E RID: 7182
	private static readonly List<int> s_inWaterRemoveIndices = new List<int>();

	// Token: 0x04001C0F RID: 7183
	private static readonly Vector2[] s_createWaveDirections = new Vector2[]
	{
		new Vector2(1.0312f, 0.312f).normalized,
		new Vector2(1.0312f, 0.312f).normalized,
		new Vector2(-0.123f, 1.12f).normalized,
		new Vector2(0.423f, 0.124f).normalized,
		new Vector2(0.123f, -0.64f).normalized,
		new Vector2(-0.523f, -0.64f).normalized,
		new Vector2(0.223f, 0.74f).normalized,
		new Vector2(0.923f, -0.24f).normalized,
		new Vector2(-0.323f, 0.44f).normalized,
		new Vector2(0.5312f, -0.812f).normalized
	};

	// Token: 0x04001C10 RID: 7184
	private static Vector2[] s_createWaveTangents = null;
}
