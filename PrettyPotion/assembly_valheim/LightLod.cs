using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Token: 0x0200018F RID: 399
public class LightLod : MonoBehaviour
{
	// Token: 0x060017CC RID: 6092 RVA: 0x000B115C File Offset: 0x000AF35C
	private void Awake()
	{
		this.m_light = base.GetComponent<Light>();
		this.m_baseRange = this.m_light.range;
		this.m_baseShadowStrength = this.m_light.shadowStrength;
		if (this.m_shadowLod && this.m_light.shadows == LightShadows.None)
		{
			this.m_shadowLod = false;
		}
		if (this.m_lightLod)
		{
			this.m_light.range = 0f;
			this.m_light.enabled = false;
		}
		if (this.m_shadowLod)
		{
			this.m_light.shadowStrength = 0f;
			this.m_light.shadows = LightShadows.None;
		}
		LightLod.m_lights.Add(this);
	}

	// Token: 0x060017CD RID: 6093 RVA: 0x000B1207 File Offset: 0x000AF407
	private void OnEnable()
	{
		base.StartCoroutine("UpdateLoop");
	}

	// Token: 0x060017CE RID: 6094 RVA: 0x000B1215 File Offset: 0x000AF415
	private void OnDisable()
	{
		base.StopCoroutine("UpdateLoop");
	}

	// Token: 0x060017CF RID: 6095 RVA: 0x000B1222 File Offset: 0x000AF422
	private void OnDestroy()
	{
		LightLod.m_lights.Remove(this);
	}

	// Token: 0x060017D0 RID: 6096 RVA: 0x000B1230 File Offset: 0x000AF430
	private IEnumerator UpdateLoop()
	{
		for (;;)
		{
			if (Utils.GetMainCamera() != null && this.m_light)
			{
				Vector3 lightReferencePoint = LightLod.GetLightReferencePoint();
				float distance = Vector3.Distance(lightReferencePoint, base.transform.position);
				if (this.m_lightLod)
				{
					if (distance < this.m_lightDistance)
					{
						if (this.m_lightPrio >= LightLod.m_lightLimit)
						{
							if (LightLod.m_lightLimit >= 0)
							{
								goto IL_18D;
							}
						}
						while (this.m_light)
						{
							if (this.m_light.range >= this.m_baseRange && this.m_light.enabled)
							{
								break;
							}
							this.m_light.enabled = true;
							this.m_light.range = Mathf.Min(this.m_baseRange, this.m_light.range + Time.deltaTime * this.m_baseRange);
							yield return null;
						}
						goto IL_1BF;
					}
					IL_18D:
					while (this.m_light && (this.m_light.range > 0f || this.m_light.enabled))
					{
						this.m_light.range = Mathf.Max(0f, this.m_light.range - Time.deltaTime * this.m_baseRange);
						if (this.m_light.range <= 0f)
						{
							this.m_light.enabled = false;
						}
						yield return null;
					}
				}
				IL_1BF:
				if (this.m_shadowLod)
				{
					if (distance < this.m_shadowDistance)
					{
						if (this.m_lightPrio >= LightLod.m_shadowLimit)
						{
							if (LightLod.m_shadowLimit >= 0)
							{
								goto IL_2E0;
							}
						}
						while (this.m_light)
						{
							if (this.m_light.shadowStrength >= this.m_baseShadowStrength && this.m_light.shadows != LightShadows.None)
							{
								break;
							}
							this.m_light.shadows = LightShadows.Soft;
							this.m_light.shadowStrength = Mathf.Min(this.m_baseShadowStrength, this.m_light.shadowStrength + Time.deltaTime * this.m_baseShadowStrength);
							yield return null;
						}
						goto IL_312;
					}
					IL_2E0:
					while (this.m_light && (this.m_light.shadowStrength > 0f || this.m_light.shadows != LightShadows.None))
					{
						this.m_light.shadowStrength = Mathf.Max(0f, this.m_light.shadowStrength - Time.deltaTime * this.m_baseShadowStrength);
						if (this.m_light.shadowStrength <= 0f)
						{
							this.m_light.shadows = LightShadows.None;
						}
						yield return null;
					}
				}
			}
			IL_312:
			yield return LightLod.s_waitFor1Sec;
		}
		yield break;
	}

	// Token: 0x060017D1 RID: 6097 RVA: 0x000B1240 File Offset: 0x000AF440
	private static Vector3 GetLightReferencePoint()
	{
		Camera mainCamera = Utils.GetMainCamera();
		if (GameCamera.InFreeFly() || Player.m_localPlayer == null)
		{
			return mainCamera.transform.position;
		}
		return Player.m_localPlayer.transform.position;
	}

	// Token: 0x060017D2 RID: 6098 RVA: 0x000B127C File Offset: 0x000AF47C
	public static void UpdateLights(float dt)
	{
		if (LightLod.m_lightLimit < 0 && LightLod.m_shadowLimit < 0)
		{
			return;
		}
		LightLod.m_updateTimer += dt;
		if (LightLod.m_updateTimer < 1f)
		{
			return;
		}
		LightLod.m_updateTimer = 0f;
		if (Utils.GetMainCamera() == null)
		{
			return;
		}
		Vector3 lightReferencePoint = LightLod.GetLightReferencePoint();
		LightLod.m_sortedLights.Clear();
		foreach (LightLod lightLod in LightLod.m_lights)
		{
			if (lightLod.enabled && lightLod.m_light && lightLod.m_light.type == LightType.Point)
			{
				lightLod.m_cameraDistanceOuter = Vector3.Distance(lightReferencePoint, lightLod.transform.position) - lightLod.m_lightDistance * 0.25f;
				LightLod.m_sortedLights.Add(lightLod);
			}
		}
		LightLod.m_sortedLights.Sort((LightLod a, LightLod b) => a.m_cameraDistanceOuter.CompareTo(b.m_cameraDistanceOuter));
		for (int i = 0; i < LightLod.m_sortedLights.Count; i++)
		{
			LightLod.m_sortedLights[i].m_lightPrio = i;
		}
	}

	// Token: 0x060017D3 RID: 6099 RVA: 0x000B13BC File Offset: 0x000AF5BC
	private void OnDrawGizmosSelected()
	{
		if (this.m_lightLod)
		{
			Gizmos.color = Color.yellow;
			Gizmos.DrawWireSphere(base.transform.position, this.m_lightDistance);
		}
		if (this.m_shadowLod)
		{
			Gizmos.color = Color.grey;
			Gizmos.DrawWireSphere(base.transform.position, this.m_shadowDistance);
		}
	}

	// Token: 0x040017A2 RID: 6050
	private static HashSet<LightLod> m_lights = new HashSet<LightLod>();

	// Token: 0x040017A3 RID: 6051
	private static List<LightLod> m_sortedLights = new List<LightLod>();

	// Token: 0x040017A4 RID: 6052
	public static int m_lightLimit = -1;

	// Token: 0x040017A5 RID: 6053
	public static int m_shadowLimit = -1;

	// Token: 0x040017A6 RID: 6054
	public bool m_lightLod = true;

	// Token: 0x040017A7 RID: 6055
	public float m_lightDistance = 40f;

	// Token: 0x040017A8 RID: 6056
	public bool m_shadowLod = true;

	// Token: 0x040017A9 RID: 6057
	public float m_shadowDistance = 20f;

	// Token: 0x040017AA RID: 6058
	private const float m_lightSizeWeight = 0.25f;

	// Token: 0x040017AB RID: 6059
	private static float m_updateTimer = 0f;

	// Token: 0x040017AC RID: 6060
	private static readonly WaitForSeconds s_waitFor1Sec = new WaitForSeconds(1f);

	// Token: 0x040017AD RID: 6061
	private int m_lightPrio;

	// Token: 0x040017AE RID: 6062
	private float m_cameraDistanceOuter;

	// Token: 0x040017AF RID: 6063
	private Light m_light;

	// Token: 0x040017B0 RID: 6064
	private float m_baseRange;

	// Token: 0x040017B1 RID: 6065
	private float m_baseShadowStrength;
}
