using System;
using UnityEngine;

// Token: 0x0200004F RID: 79
public class EffectFade : MonoBehaviour
{
	// Token: 0x06000603 RID: 1539 RVA: 0x00032C34 File Offset: 0x00030E34
	private void Awake()
	{
		this.m_particles = base.gameObject.GetComponentsInChildren<ParticleSystem>();
		this.m_light = base.gameObject.GetComponentInChildren<Light>();
		this.m_audioSource = base.gameObject.GetComponentInChildren<AudioSource>();
		if (this.m_light)
		{
			this.m_lightBaseIntensity = this.m_light.intensity;
			this.m_light.intensity = 0f;
		}
		if (this.m_audioSource)
		{
			this.m_baseVolume = this.m_audioSource.volume;
			this.m_audioSource.volume = 0f;
		}
		this.SetActive(false);
	}

	// Token: 0x06000604 RID: 1540 RVA: 0x00032CD8 File Offset: 0x00030ED8
	private void Update()
	{
		this.m_intensity = Mathf.MoveTowards(this.m_intensity, this.m_active ? 1f : 0f, Time.deltaTime / this.m_fadeDuration);
		if (this.m_light)
		{
			this.m_light.intensity = this.m_intensity * this.m_lightBaseIntensity;
			this.m_light.enabled = (this.m_light.intensity > 0f);
		}
		if (this.m_audioSource)
		{
			this.m_audioSource.volume = this.m_intensity * this.m_baseVolume;
		}
	}

	// Token: 0x06000605 RID: 1541 RVA: 0x00032D80 File Offset: 0x00030F80
	public void SetActive(bool active)
	{
		if (this.m_active == active)
		{
			return;
		}
		this.m_active = active;
		ParticleSystem[] particles = this.m_particles;
		for (int i = 0; i < particles.Length; i++)
		{
			particles[i].emission.enabled = active;
		}
	}

	// Token: 0x040006C9 RID: 1737
	public float m_fadeDuration = 1f;

	// Token: 0x040006CA RID: 1738
	private ParticleSystem[] m_particles;

	// Token: 0x040006CB RID: 1739
	private Light m_light;

	// Token: 0x040006CC RID: 1740
	private AudioSource m_audioSource;

	// Token: 0x040006CD RID: 1741
	private float m_baseVolume;

	// Token: 0x040006CE RID: 1742
	private float m_lightBaseIntensity;

	// Token: 0x040006CF RID: 1743
	private bool m_active = true;

	// Token: 0x040006D0 RID: 1744
	private float m_intensity;
}
