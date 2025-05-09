using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

// Token: 0x02000057 RID: 87
public class LightFlicker : MonoBehaviour, IMonoUpdater
{
	// Token: 0x06000623 RID: 1571 RVA: 0x00033F7C File Offset: 0x0003217C
	private void Awake()
	{
		this.m_light = base.GetComponent<Light>();
		this.m_baseIntensity = this.m_light.intensity;
		this.m_basePosition = base.transform.localPosition;
		this.m_flickerOffset = UnityEngine.Random.Range(0f, 10f);
		if (Settings.ReduceFlashingLights)
		{
			this.m_light.intensity = 0f;
		}
		this.m_reducedFlashing = (Settings.ReduceFlashingLights && this.m_flashingLightsSetting == LightFlicker.LightFlashSettings.OnIncludeFade);
		this.m_multiplier = (Settings.ReduceFlashingLights ? this.m_accessibilityBrightnessMultiplier : 1f);
		this.m_smoothFlicker = (Settings.ReduceFlashingLights && this.m_flashingLightsSetting == LightFlicker.LightFlashSettings.SmoothedFlicker);
	}

	// Token: 0x06000624 RID: 1572 RVA: 0x00034030 File Offset: 0x00032230
	public void ApplySettings()
	{
		if (!base.enabled)
		{
			return;
		}
		LightFlicker.Instances.Remove(this);
		this.m_reducedFlashing = (Settings.ReduceFlashingLights && this.m_flashingLightsSetting == LightFlicker.LightFlashSettings.OnIncludeFade);
		this.m_multiplier = (Settings.ReduceFlashingLights ? this.m_accessibilityBrightnessMultiplier : 1f);
		this.m_smoothFlicker = (Settings.ReduceFlashingLights && this.m_flashingLightsSetting == LightFlicker.LightFlashSettings.SmoothedFlicker);
		this.m_light.intensity = 0f;
		if (!Settings.ReduceFlashingLights)
		{
			LightFlicker.Instances.Add(this);
			return;
		}
		if (this.m_flashingLightsSetting == LightFlicker.LightFlashSettings.Off)
		{
			this.m_light.intensity = 0f;
			return;
		}
		if (this.m_flashingLightsSetting == LightFlicker.LightFlashSettings.AlwaysOn)
		{
			this.m_light.intensity = 1f;
			return;
		}
		LightFlicker.Instances.Add(this);
	}

	// Token: 0x06000625 RID: 1573 RVA: 0x00034100 File Offset: 0x00032300
	private void OnEnable()
	{
		this.m_time = 0f;
		if (!this.m_light)
		{
			return;
		}
		this.ApplySettings();
	}

	// Token: 0x06000626 RID: 1574 RVA: 0x00034121 File Offset: 0x00032321
	private void OnDisable()
	{
		LightFlicker.Instances.Remove(this);
	}

	// Token: 0x06000627 RID: 1575 RVA: 0x00034130 File Offset: 0x00032330
	public void CustomUpdate(float deltaTime, float time)
	{
		if (!this.m_light)
		{
			ZLog.LogError("Light was null! This should never happen!");
			return;
		}
		if (!this.m_light.enabled)
		{
			return;
		}
		this.m_time += deltaTime;
		float num = this.m_flickerOffset + time * this.m_flickerSpeed;
		this.m_targetIntensity = 1f;
		if (!this.m_reducedFlashing)
		{
			this.m_targetIntensity += MathF.Sin(num) * MathF.Sin(num * 0.56436f) * MathF.Cos(num * 0.758348f) * this.m_flickerIntensity;
		}
		if (this.m_fadeInDuration > 0f)
		{
			this.m_targetIntensity *= Utils.LerpStep(0f, this.m_fadeInDuration, this.m_time);
		}
		if (this.m_ttl > 0f)
		{
			if (this.m_time > this.m_ttl)
			{
				UnityEngine.Object.Destroy(base.gameObject);
				return;
			}
			float l = this.m_ttl - this.m_fadeDuration;
			this.m_targetIntensity *= 1f - Utils.LerpStep(l, this.m_ttl, this.m_time);
		}
		if (this.m_smoothFlicker)
		{
			float h = (this.m_time > this.m_ttl - this.m_fadeDuration && this.m_ttl > 0f) ? 0.075f : 0.15f;
			this.m_smoothedIntensity = Utils.LerpSmooth(this.m_smoothedIntensity, this.m_targetIntensity, deltaTime, h);
		}
		else
		{
			this.m_smoothedIntensity = this.m_targetIntensity;
		}
		this.m_light.intensity = this.m_baseIntensity * this.m_smoothedIntensity * this.m_multiplier;
		this.m_offset.x = MathF.Sin(num) * MathF.Sin(num * 0.56436f);
		this.m_offset.y = MathF.Sin(num * 0.56436f) * MathF.Sin(num * 0.688742f);
		this.m_offset.z = MathF.Cos(num * 0.758348f) * MathF.Cos(num * 0.4563696f);
		this.m_offset *= this.m_movement;
		base.transform.localPosition = this.m_basePosition + this.m_offset;
	}

	// Token: 0x17000013 RID: 19
	// (get) Token: 0x06000628 RID: 1576 RVA: 0x0003436D File Offset: 0x0003256D
	public static List<IMonoUpdater> Instances { get; } = new List<IMonoUpdater>();

	// Token: 0x04000718 RID: 1816
	private Vector3 m_offset;

	// Token: 0x04000719 RID: 1817
	public float m_flickerIntensity = 0.1f;

	// Token: 0x0400071A RID: 1818
	public float m_flickerSpeed = 10f;

	// Token: 0x0400071B RID: 1819
	public float m_movement = 0.1f;

	// Token: 0x0400071C RID: 1820
	public float m_ttl;

	// Token: 0x0400071D RID: 1821
	public float m_fadeDuration = 0.2f;

	// Token: 0x0400071E RID: 1822
	public float m_fadeInDuration;

	// Token: 0x0400071F RID: 1823
	[FormerlySerializedAs("m_flashingLightingsAccessibility")]
	[Header("Accessibility")]
	public LightFlicker.LightFlashSettings m_flashingLightsSetting;

	// Token: 0x04000720 RID: 1824
	[Range(0f, 1f)]
	public float m_accessibilityBrightnessMultiplier = 1f;

	// Token: 0x04000721 RID: 1825
	private Light m_light;

	// Token: 0x04000722 RID: 1826
	private float m_baseIntensity = 1f;

	// Token: 0x04000723 RID: 1827
	private Vector3 m_basePosition = Vector3.zero;

	// Token: 0x04000724 RID: 1828
	private float m_time;

	// Token: 0x04000725 RID: 1829
	private float m_flickerOffset;

	// Token: 0x04000726 RID: 1830
	private float m_smoothedIntensity;

	// Token: 0x04000727 RID: 1831
	private float m_targetIntensity;

	// Token: 0x04000728 RID: 1832
	private bool m_reducedFlashing;

	// Token: 0x04000729 RID: 1833
	private bool m_smoothFlicker;

	// Token: 0x0400072A RID: 1834
	private float m_multiplier = 1f;

	// Token: 0x02000252 RID: 594
	public enum LightFlashSettings
	{
		// Token: 0x0400201B RID: 8219
		[InspectorName("Unchanged")]
		Default,
		// Token: 0x0400201C RID: 8220
		[InspectorName("Remove Flicker, Keep Fade")]
		OnIncludeFade,
		// Token: 0x0400201D RID: 8221
		[InspectorName("Disable light")]
		Off,
		// Token: 0x0400201E RID: 8222
		AlwaysOn,
		// Token: 0x0400201F RID: 8223
		SmoothedFlicker
	}
}
