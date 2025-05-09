using System;
using UnityEngine;

// Token: 0x0200014C RID: 332
public class StealthSystem : MonoBehaviour
{
	// Token: 0x170000BA RID: 186
	// (get) Token: 0x06001436 RID: 5174 RVA: 0x00094A4E File Offset: 0x00092C4E
	public static StealthSystem instance
	{
		get
		{
			return StealthSystem.m_instance;
		}
	}

	// Token: 0x06001437 RID: 5175 RVA: 0x00094A55 File Offset: 0x00092C55
	private void Awake()
	{
		StealthSystem.m_instance = this;
	}

	// Token: 0x06001438 RID: 5176 RVA: 0x00094A5D File Offset: 0x00092C5D
	private void OnDestroy()
	{
		StealthSystem.m_instance = null;
	}

	// Token: 0x06001439 RID: 5177 RVA: 0x00094A68 File Offset: 0x00092C68
	public float GetLightFactor(Vector3 point)
	{
		float lightLevel = this.GetLightLevel(point);
		return Utils.LerpStep(this.m_minLightLevel, this.m_maxLightLevel, lightLevel);
	}

	// Token: 0x0600143A RID: 5178 RVA: 0x00094A90 File Offset: 0x00092C90
	public float GetLightLevel(Vector3 point)
	{
		if (Time.time - this.m_lastLightListUpdate > 1f)
		{
			this.m_lastLightListUpdate = Time.time;
			this.m_allLights = UnityEngine.Object.FindObjectsOfType<Light>();
		}
		float num = RenderSettings.ambientIntensity * RenderSettings.ambientLight.grayscale;
		foreach (Light light in this.m_allLights)
		{
			if (!(light == null))
			{
				if (light.type == LightType.Directional)
				{
					float num2 = 1f;
					if (light.shadows != LightShadows.None && (Physics.Raycast(point - light.transform.forward * 1000f, light.transform.forward, 1000f, this.m_shadowTestMask) || Physics.Raycast(point, -light.transform.forward, 1000f, this.m_shadowTestMask)))
					{
						num2 = 1f - light.shadowStrength;
					}
					float num3 = light.intensity * light.color.grayscale * num2;
					num += num3;
				}
				else
				{
					float num4 = Vector3.Distance(light.transform.position, point);
					if (num4 <= light.range)
					{
						float num5 = 1f;
						if (light.shadows != LightShadows.None)
						{
							Vector3 vector = point - light.transform.position;
							if (Physics.Raycast(light.transform.position, vector.normalized, vector.magnitude, this.m_shadowTestMask) || Physics.Raycast(point, -vector.normalized, vector.magnitude, this.m_shadowTestMask))
							{
								num5 = 1f - light.shadowStrength;
							}
						}
						float num6 = 1f - num4 / light.range;
						float num7 = light.intensity * light.color.grayscale * num6 * num5;
						num += num7;
					}
				}
			}
		}
		return num;
	}

	// Token: 0x040013F2 RID: 5106
	private static StealthSystem m_instance;

	// Token: 0x040013F3 RID: 5107
	public LayerMask m_shadowTestMask;

	// Token: 0x040013F4 RID: 5108
	public float m_minLightLevel = 0.2f;

	// Token: 0x040013F5 RID: 5109
	public float m_maxLightLevel = 1.6f;

	// Token: 0x040013F6 RID: 5110
	private Light[] m_allLights;

	// Token: 0x040013F7 RID: 5111
	private float m_lastLightListUpdate;

	// Token: 0x040013F8 RID: 5112
	private const float m_lightUpdateInterval = 1f;
}
