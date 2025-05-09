using System;
using System.Collections.Generic;
using UnityEngine;

// Token: 0x020001B8 RID: 440
public class ShipEffects : MonoBehaviour, IMonoUpdater
{
	// Token: 0x060019C6 RID: 6598 RVA: 0x000C0420 File Offset: 0x000BE620
	private void Awake()
	{
		ZNetView componentInParent = base.GetComponentInParent<ZNetView>();
		if (componentInParent && componentInParent.GetZDO() == null)
		{
			base.enabled = false;
			return;
		}
		this.m_body = base.GetComponentInParent<Rigidbody>();
		this.m_ship = base.GetComponentInParent<Ship>();
		if (this.m_speedWakeRoot)
		{
			this.m_wakeParticles = this.m_speedWakeRoot.GetComponentsInChildren<ParticleSystem>();
		}
		if (this.m_wakeSoundRoot)
		{
			foreach (AudioSource audioSource in this.m_wakeSoundRoot.GetComponentsInChildren<AudioSource>())
			{
				audioSource.pitch = UnityEngine.Random.Range(0.9f, 1.1f);
				this.m_wakeSounds.Add(new KeyValuePair<AudioSource, float>(audioSource, audioSource.volume));
			}
		}
		if (this.m_inWaterSoundRoot)
		{
			foreach (AudioSource audioSource2 in this.m_inWaterSoundRoot.GetComponentsInChildren<AudioSource>())
			{
				audioSource2.pitch = UnityEngine.Random.Range(0.9f, 1.1f);
				this.m_inWaterSounds.Add(new KeyValuePair<AudioSource, float>(audioSource2, audioSource2.volume));
			}
		}
		if (this.m_sailSound)
		{
			this.m_sailBaseVol = this.m_sailSound.volume;
			this.m_sailSound.pitch = UnityEngine.Random.Range(0.9f, 1.1f);
		}
	}

	// Token: 0x060019C7 RID: 6599 RVA: 0x000C056C File Offset: 0x000BE76C
	private void OnEnable()
	{
		ShipEffects.Instances.Add(this);
	}

	// Token: 0x060019C8 RID: 6600 RVA: 0x000C0579 File Offset: 0x000BE779
	private void OnDisable()
	{
		ShipEffects.Instances.Remove(this);
	}

	// Token: 0x060019C9 RID: 6601 RVA: 0x000C0588 File Offset: 0x000BE788
	public void CustomLateUpdate(float deltaTime)
	{
		if (!Floating.IsUnderWater(base.transform.position, ref this.m_previousWaterVolume))
		{
			this.m_shadow.gameObject.SetActive(false);
			this.SetWake(false, deltaTime);
			this.FadeSounds(this.m_inWaterSounds, false, deltaTime);
			return;
		}
		this.m_shadow.gameObject.SetActive(true);
		bool enabled = this.m_body.velocity.magnitude > this.m_minimumWakeVel;
		this.FadeSounds(this.m_inWaterSounds, true, deltaTime);
		this.SetWake(enabled, deltaTime);
		if (this.m_sailSound)
		{
			float target = this.m_ship.IsSailUp() ? this.m_sailBaseVol : 0f;
			ShipEffects.FadeSound(this.m_sailSound, target, this.m_sailFadeDuration, deltaTime);
		}
		if (this.m_splashEffects != null)
		{
			this.m_splashEffects.SetActive(this.m_ship.HasPlayerOnboard());
		}
	}

	// Token: 0x060019CA RID: 6602 RVA: 0x000C0678 File Offset: 0x000BE878
	private void SetWake(bool enabled, float dt)
	{
		ParticleSystem[] wakeParticles = this.m_wakeParticles;
		for (int i = 0; i < wakeParticles.Length; i++)
		{
			wakeParticles[i].emission.enabled = enabled;
		}
		this.FadeSounds(this.m_wakeSounds, enabled, dt);
	}

	// Token: 0x060019CB RID: 6603 RVA: 0x000C06BC File Offset: 0x000BE8BC
	private void FadeSounds(List<KeyValuePair<AudioSource, float>> sources, bool enabled, float dt)
	{
		foreach (KeyValuePair<AudioSource, float> keyValuePair in sources)
		{
			if (enabled)
			{
				ShipEffects.FadeSound(keyValuePair.Key, keyValuePair.Value, this.m_audioFadeDuration, dt);
			}
			else
			{
				ShipEffects.FadeSound(keyValuePair.Key, 0f, this.m_audioFadeDuration, dt);
			}
		}
	}

	// Token: 0x060019CC RID: 6604 RVA: 0x000C073C File Offset: 0x000BE93C
	private static void FadeSound(AudioSource source, float target, float fadeDuration, float dt)
	{
		float maxDelta = dt / fadeDuration;
		if (target > 0f)
		{
			if (!source.isPlaying)
			{
				source.Play();
			}
			source.volume = Mathf.MoveTowards(source.volume, target, maxDelta);
			return;
		}
		if (source.isPlaying)
		{
			source.volume = Mathf.MoveTowards(source.volume, 0f, maxDelta);
			if (source.volume <= 0f)
			{
				source.Stop();
			}
		}
	}

	// Token: 0x170000CA RID: 202
	// (get) Token: 0x060019CD RID: 6605 RVA: 0x000C07A9 File Offset: 0x000BE9A9
	public static List<IMonoUpdater> Instances { get; } = new List<IMonoUpdater>();

	// Token: 0x04001A3F RID: 6719
	public Transform m_shadow;

	// Token: 0x04001A40 RID: 6720
	public float m_offset = 0.01f;

	// Token: 0x04001A41 RID: 6721
	public float m_minimumWakeVel = 5f;

	// Token: 0x04001A42 RID: 6722
	public GameObject m_speedWakeRoot;

	// Token: 0x04001A43 RID: 6723
	public GameObject m_wakeSoundRoot;

	// Token: 0x04001A44 RID: 6724
	public GameObject m_inWaterSoundRoot;

	// Token: 0x04001A45 RID: 6725
	public float m_audioFadeDuration = 2f;

	// Token: 0x04001A46 RID: 6726
	public AudioSource m_sailSound;

	// Token: 0x04001A47 RID: 6727
	public float m_sailFadeDuration = 1f;

	// Token: 0x04001A48 RID: 6728
	public GameObject m_splashEffects;

	// Token: 0x04001A49 RID: 6729
	private ParticleSystem[] m_wakeParticles;

	// Token: 0x04001A4A RID: 6730
	private float m_sailBaseVol = 1f;

	// Token: 0x04001A4B RID: 6731
	private readonly List<KeyValuePair<AudioSource, float>> m_wakeSounds = new List<KeyValuePair<AudioSource, float>>();

	// Token: 0x04001A4C RID: 6732
	private readonly List<KeyValuePair<AudioSource, float>> m_inWaterSounds = new List<KeyValuePair<AudioSource, float>>();

	// Token: 0x04001A4D RID: 6733
	private WaterVolume m_previousWaterVolume;

	// Token: 0x04001A4E RID: 6734
	private Rigidbody m_body;

	// Token: 0x04001A4F RID: 6735
	private Ship m_ship;
}
