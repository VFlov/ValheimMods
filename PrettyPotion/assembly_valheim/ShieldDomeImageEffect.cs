using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;
using Valheim.SettingsGui;

// Token: 0x02000063 RID: 99
[RequireComponent(typeof(Camera))]
[ExecuteAlways]
public class ShieldDomeImageEffect : MonoBehaviour
{
	// Token: 0x06000691 RID: 1681 RVA: 0x0003733C File Offset: 0x0003553C
	private void Awake()
	{
		this.m_effectMaterial = new Material(Shader.Find("Hidden/ShieldDomePass"));
		this.m_gradientTex = new Texture2D(256, 1, TextureFormat.RGB24, false);
		this.m_gradientTex.wrapMode = TextureWrapMode.Clamp;
		for (int i = 0; i < 256; i++)
		{
			Color color = this.m_shieldColorGradient.Evaluate((float)i / 256f);
			this.m_gradientTex.SetPixel(i, 0, color);
		}
		this.m_gradientTex.Apply();
		ShieldDomeImageEffect.s_staticGradient = this.m_shieldColorGradient;
		ShieldDomeImageEffect.Smoothing = this.m_smoothing;
	}

	// Token: 0x06000692 RID: 1682 RVA: 0x000373D0 File Offset: 0x000355D0
	[ImageEffectAllowedInSceneView]
	private void OnRenderImage(RenderTexture src, RenderTexture dest)
	{
		if (this.m_shieldDomes == null || this.m_shieldDomes.Length == 0)
		{
			Graphics.Blit(src, dest);
			return;
		}
		if (this.m_cam == null)
		{
			this.m_cam = Camera.main;
		}
		Vector3 direction = this.m_cam.ViewportPointToRay(new Vector3(0f, 1f, 0f)).direction;
		Vector3 direction2 = this.m_cam.ViewportPointToRay(new Vector3(1f, 1f, 0f)).direction;
		Vector3 direction3 = this.m_cam.ViewportPointToRay(new Vector3(0f, 0f, 0f)).direction;
		Vector3 direction4 = this.m_cam.ViewportPointToRay(new Vector3(1f, 0f, 0f)).direction;
		this.m_effectMaterial.SetVector(ShieldDomeImageEffect.Uniforms._TopLeft, direction);
		this.m_effectMaterial.SetVector(ShieldDomeImageEffect.Uniforms._TopRight, direction2);
		this.m_effectMaterial.SetVector(ShieldDomeImageEffect.Uniforms._BottomLeft, direction3);
		this.m_effectMaterial.SetVector(ShieldDomeImageEffect.Uniforms._BottomRight, direction4);
		this.m_effectMaterial.SetFloat(ShieldDomeImageEffect.Uniforms._Smoothing, this.m_smoothing);
		this.m_effectMaterial.SetFloat(ShieldDomeImageEffect.Uniforms._DepthFade, this.m_depthFadeDistance);
		this.m_effectMaterial.SetFloat(ShieldDomeImageEffect.Uniforms._EdgeGlow, this.m_edgeGlowDistance);
		this.m_effectMaterial.SetFloat(ShieldDomeImageEffect.Uniforms._DrawDistance, this.m_drawDistance);
		this.m_effectMaterial.SetTexture(ShieldDomeImageEffect.Uniforms._ShieldColorGradient, this.m_gradientTex);
		this.m_effectMaterial.SetFloat(ShieldDomeImageEffect.Uniforms._ShieldTime, Time.time);
		this.m_effectMaterial.SetTexture(ShieldDomeImageEffect.Uniforms._NoiseTexture, this.m_noiseTexture);
		this.m_effectMaterial.SetTexture(ShieldDomeImageEffect.Uniforms._CurlNoiseTexture, this.m_curlNoiseTexture);
		this.m_effectMaterial.SetFloat(ShieldDomeImageEffect.Uniforms._NoiseSize, this.m_noiseSize);
		this.m_effectMaterial.SetFloat(ShieldDomeImageEffect.Uniforms._CurlNoiseSize, this.m_curlSize);
		this.m_effectMaterial.SetFloat(ShieldDomeImageEffect.Uniforms._CurlNoiseStrength, this.m_curlStrength);
		int value = 32 + GraphicsModeManager.CurrentDeviceQualitySettings.Lod * 32;
		this.m_effectMaterial.SetInt(ShieldDomeImageEffect.Uniforms._MaxSteps, value);
		this.m_effectMaterial.SetFloat(ShieldDomeImageEffect.Uniforms._SurfaceDistance, this.m_surfaceDistance);
		this.m_effectMaterial.SetFloat(ShieldDomeImageEffect.Uniforms._NormalBias, this.m_normalBias);
		this.m_effectMaterial.SetFloat(ShieldDomeImageEffect.Uniforms._RefractStrength, this.m_refractStrength);
		this.PrepareComputeBuffer();
		Graphics.Blit(src, dest, this.m_effectMaterial, 0);
	}

	// Token: 0x06000693 RID: 1683 RVA: 0x00037678 File Offset: 0x00035878
	private void PrepareComputeBuffer()
	{
		if (this.m_shieldDomes != null && this.m_shieldDomes.Length != 0)
		{
			if (this.m_shieldDomeBuffer == null || this.m_shieldDomeBuffer.count != this.m_shieldDomes.Length)
			{
				ComputeBuffer shieldDomeBuffer = this.m_shieldDomeBuffer;
				if (shieldDomeBuffer != null)
				{
					shieldDomeBuffer.Release();
				}
				this.m_shieldDomeBuffer = new ComputeBuffer(this.m_shieldDomes.Length, this.m_ShieldDomeStride, ComputeBufferType.Structured);
			}
			this.m_shieldDomeBuffer.SetData(this.m_shieldDomes);
			this.m_effectMaterial.SetBuffer(ShieldDomeImageEffect.Uniforms._DomeBuffer, this.m_shieldDomeBuffer);
			this.m_effectMaterial.SetInt(ShieldDomeImageEffect.Uniforms._DomeCount, this.m_shieldDomes.Length);
			return;
		}
		ComputeBuffer shieldDomeBuffer2 = this.m_shieldDomeBuffer;
		if (shieldDomeBuffer2 == null)
		{
			return;
		}
		shieldDomeBuffer2.Release();
	}

	// Token: 0x06000694 RID: 1684 RVA: 0x00037730 File Offset: 0x00035930
	public void SetShieldData(ShieldGenerator shield, Vector3 position, float radius, float fuelFactor, float lastHitTime)
	{
		this.m_shieldDomeData[shield] = new ShieldDomeImageEffect.ShieldDome
		{
			position = position,
			radius = radius,
			fuelFactor = fuelFactor,
			lastHitTime = lastHitTime
		};
		this.updateShieldData();
	}

	// Token: 0x06000695 RID: 1685 RVA: 0x0003777A File Offset: 0x0003597A
	public void RemoveShield(ShieldGenerator shield)
	{
		if (this.m_shieldDomeData.Remove(shield))
		{
			this.updateShieldData();
		}
	}

	// Token: 0x06000696 RID: 1686 RVA: 0x00037790 File Offset: 0x00035990
	private void updateShieldData()
	{
		if (this.m_shieldDomes == null || this.m_shieldDomes.Length != this.m_shieldDomeData.Count)
		{
			this.m_shieldDomes = new ShieldDomeImageEffect.ShieldDome[this.m_shieldDomeData.Count];
		}
		int num = 0;
		foreach (KeyValuePair<ShieldGenerator, ShieldDomeImageEffect.ShieldDome> keyValuePair in this.m_shieldDomeData)
		{
			this.m_shieldDomes[num++] = keyValuePair.Value;
		}
	}

	// Token: 0x06000697 RID: 1687 RVA: 0x00037828 File Offset: 0x00035A28
	private void OnDestroy()
	{
		if (Application.isPlaying)
		{
			UnityEngine.Object.Destroy(this.m_effectMaterial);
			return;
		}
		UnityEngine.Object.DestroyImmediate(this.m_effectMaterial);
	}

	// Token: 0x06000698 RID: 1688 RVA: 0x00037848 File Offset: 0x00035A48
	public static Color GetDomeColor(float fuelFactor)
	{
		return ShieldDomeImageEffect.s_staticGradient.Evaluate(fuelFactor);
	}

	// Token: 0x0400079B RID: 1947
	private int m_ShieldDomeStride = 24;

	// Token: 0x0400079C RID: 1948
	private Material m_effectMaterial;

	// Token: 0x0400079D RID: 1949
	private Camera m_cam;

	// Token: 0x0400079E RID: 1950
	private ComputeBuffer m_shieldDomeBuffer;

	// Token: 0x0400079F RID: 1951
	private Texture2D m_gradientTex;

	// Token: 0x040007A0 RID: 1952
	private static Gradient s_staticGradient;

	// Token: 0x040007A1 RID: 1953
	public static float Smoothing;

	// Token: 0x040007A2 RID: 1954
	[Min(0.1f)]
	public float m_smoothing = 0.25f;

	// Token: 0x040007A3 RID: 1955
	public float m_depthFadeDistance = 3f;

	// Token: 0x040007A4 RID: 1956
	[Min(0.25f)]
	public float m_edgeGlowDistance = 1f;

	// Token: 0x040007A5 RID: 1957
	public Gradient m_shieldColorGradient;

	// Token: 0x040007A6 RID: 1958
	[Range(-10f, 10f)]
	public float m_refractStrength = 1f;

	// Token: 0x040007A7 RID: 1959
	private ShieldDomeImageEffect.ShieldDome[] m_shieldDomes;

	// Token: 0x040007A8 RID: 1960
	private Dictionary<ShieldGenerator, ShieldDomeImageEffect.ShieldDome> m_shieldDomeData = new Dictionary<ShieldGenerator, ShieldDomeImageEffect.ShieldDome>();

	// Token: 0x040007A9 RID: 1961
	[Header("Textures")]
	public Texture2D m_noiseTexture;

	// Token: 0x040007AA RID: 1962
	public float m_noiseSize = 15f;

	// Token: 0x040007AB RID: 1963
	public Texture3D m_curlNoiseTexture;

	// Token: 0x040007AC RID: 1964
	public float m_curlSize;

	// Token: 0x040007AD RID: 1965
	public float m_curlStrength;

	// Token: 0x040007AE RID: 1966
	[Header("Quality")]
	[Range(0f, 0.2f)]
	public float m_surfaceDistance = 0.001f;

	// Token: 0x040007AF RID: 1967
	[Range(0f, 0.5f)]
	public float m_normalBias = 0.1f;

	// Token: 0x040007B0 RID: 1968
	public float m_drawDistance = 100f;

	// Token: 0x02000260 RID: 608
	[Serializable]
	public struct ShieldDome
	{
		// Token: 0x04002092 RID: 8338
		public Vector3 position;

		// Token: 0x04002093 RID: 8339
		public float radius;

		// Token: 0x04002094 RID: 8340
		[FormerlySerializedAs("health")]
		[Range(0f, 1f)]
		public float fuelFactor;

		// Token: 0x04002095 RID: 8341
		public float lastHitTime;
	}

	// Token: 0x02000261 RID: 609
	private static class Uniforms
	{
		// Token: 0x04002096 RID: 8342
		internal static readonly int _TopLeft = Shader.PropertyToID("_TopLeft");

		// Token: 0x04002097 RID: 8343
		internal static readonly int _TopRight = Shader.PropertyToID("_TopRight");

		// Token: 0x04002098 RID: 8344
		internal static readonly int _BottomLeft = Shader.PropertyToID("_BottomLeft");

		// Token: 0x04002099 RID: 8345
		internal static readonly int _BottomRight = Shader.PropertyToID("_BottomRight");

		// Token: 0x0400209A RID: 8346
		internal static readonly int _Smoothing = Shader.PropertyToID("_Smoothing");

		// Token: 0x0400209B RID: 8347
		internal static readonly int _DepthFade = Shader.PropertyToID("_DepthFade");

		// Token: 0x0400209C RID: 8348
		internal static readonly int _EdgeGlow = Shader.PropertyToID("_EdgeGlow");

		// Token: 0x0400209D RID: 8349
		internal static readonly int _DrawDistance = Shader.PropertyToID("_DrawDistance");

		// Token: 0x0400209E RID: 8350
		internal static readonly int _DomeBuffer = Shader.PropertyToID("_DomeBuffer");

		// Token: 0x0400209F RID: 8351
		internal static readonly int _DomeCount = Shader.PropertyToID("_DomeCount");

		// Token: 0x040020A0 RID: 8352
		internal static readonly int _MaxSteps = Shader.PropertyToID("_MaxSteps");

		// Token: 0x040020A1 RID: 8353
		internal static readonly int _SurfaceDistance = Shader.PropertyToID("_SurfaceDistance");

		// Token: 0x040020A2 RID: 8354
		internal static readonly int _NormalBias = Shader.PropertyToID("_NormalBias");

		// Token: 0x040020A3 RID: 8355
		internal static readonly int _RefractStrength = Shader.PropertyToID("_RefractStrength");

		// Token: 0x040020A4 RID: 8356
		internal static readonly int _ShieldColorGradient = Shader.PropertyToID("_ShieldColorGradient");

		// Token: 0x040020A5 RID: 8357
		internal static readonly int _ShieldTime = Shader.PropertyToID("_ShieldTime");

		// Token: 0x040020A6 RID: 8358
		internal static readonly int _NoiseTexture = Shader.PropertyToID("_NoiseTexture");

		// Token: 0x040020A7 RID: 8359
		internal static readonly int _CurlNoiseTexture = Shader.PropertyToID("_CurlNoiseTexture");

		// Token: 0x040020A8 RID: 8360
		internal static readonly int _NoiseSize = Shader.PropertyToID("_NoiseSize");

		// Token: 0x040020A9 RID: 8361
		internal static readonly int _CurlNoiseSize = Shader.PropertyToID("_CurlNoiseSize");

		// Token: 0x040020AA RID: 8362
		internal static readonly int _CurlNoiseStrength = Shader.PropertyToID("_CurlNoiseStrength");
	}
}
