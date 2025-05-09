using System;
using System.Collections.Generic;
using UnityEngine;

// Token: 0x0200006B RID: 107
public class ZSFX : MonoBehaviour, IMonoUpdater
{
	// Token: 0x060006D1 RID: 1745 RVA: 0x00038DF2 File Offset: 0x00036FF2
	public void Awake()
	{
		this.m_delay = UnityEngine.Random.Range(this.m_minDelay, this.m_maxDelay);
		this.m_audioSource = base.GetComponent<AudioSource>();
		this.m_baseSpread = this.m_audioSource.spread;
	}

	// Token: 0x060006D2 RID: 1746 RVA: 0x00038E28 File Offset: 0x00037028
	private void OnEnable()
	{
		if (this.m_audioSource != null)
		{
			ZSFX.Instances.Add(this);
		}
	}

	// Token: 0x060006D3 RID: 1747 RVA: 0x00038E44 File Offset: 0x00037044
	private void OnDisable()
	{
		if (this.m_audioSource != null && this.m_playOnAwake && this.m_audioSource.loop)
		{
			this.m_time = 0f;
			this.m_delay = UnityEngine.Random.Range(this.m_minDelay, this.m_maxDelay);
			this.m_audioSource.Stop();
		}
		ZSFX.Instances.Remove(this);
	}

	// Token: 0x14000001 RID: 1
	// (add) Token: 0x060006D4 RID: 1748 RVA: 0x00038EB0 File Offset: 0x000370B0
	// (remove) Token: 0x060006D5 RID: 1749 RVA: 0x00038EE8 File Offset: 0x000370E8
	public event Action<ZSFX> OnDestroyingSfx = delegate(ZSFX <p0>)
	{
	};

	// Token: 0x060006D6 RID: 1750 RVA: 0x00038F1D File Offset: 0x0003711D
	private void OnDestroy()
	{
		this.OnDestroyingSfx(this);
	}

	// Token: 0x060006D7 RID: 1751 RVA: 0x00038F2C File Offset: 0x0003712C
	public void CustomUpdate(float dt, float time)
	{
		this.m_time += dt;
		if (this.m_delay >= 0f && this.m_time >= this.m_delay)
		{
			this.m_delay = -1f;
			if (this.m_playOnAwake)
			{
				this.Play();
			}
		}
		if (this.IsLooping())
		{
			this.m_concurrencyVolumeModifier = Mathf.MoveTowards(this.m_concurrencyVolumeModifier, (float)(this.m_disabledFromConcurrency ? 0 : 1), dt / 0.5f);
		}
		if (this.m_audioSource.isPlaying)
		{
			if (this.m_distanceReverb && this.m_audioSource.loop)
			{
				this.m_updateReverbTimer += dt;
				if (this.m_updateReverbTimer > 1f)
				{
					this.m_updateReverbTimer = 0f;
					this.UpdateReverb();
				}
			}
			if (this.m_fadeOutOnAwake && this.m_time > this.m_fadeOutDelay)
			{
				this.m_fadeOutOnAwake = false;
				this.FadeOut();
			}
			float vol = this.m_vol;
			float num = 1f;
			if (this.m_fadeOutTimer >= 0f)
			{
				this.m_fadeOutTimer += dt;
				if (this.m_fadeOutTimer >= this.m_fadeOutDuration)
				{
					this.m_audioSource.volume = 0f;
					this.Stop();
					return;
				}
				num = 1f - Mathf.Clamp01(this.m_fadeOutTimer / this.m_fadeOutDuration);
			}
			else if (this.m_fadeInTimer >= 0f)
			{
				this.m_fadeInTimer += dt;
				num = Mathf.Clamp01(this.m_fadeInTimer / this.m_fadeInDuration);
				if (this.m_fadeInTimer > this.m_fadeInDuration)
				{
					this.m_fadeInTimer = -1f;
				}
			}
			this.m_audioSource.volume = vol * num * this.m_concurrencyVolumeModifier * this.m_volumeModifier;
			float num2 = this.m_basePitch * this.m_pitchModifier;
			num2 -= num2 * this.m_reverbPitchModifier;
			this.m_audioSource.pitch = num2;
		}
	}

	// Token: 0x060006D8 RID: 1752 RVA: 0x0003910C File Offset: 0x0003730C
	public void FadeOut()
	{
		if (this.m_fadeOutTimer < 0f)
		{
			this.m_fadeOutTimer = 0f;
		}
	}

	// Token: 0x060006D9 RID: 1753 RVA: 0x00039126 File Offset: 0x00037326
	public void Stop()
	{
		if (this.m_audioSource != null)
		{
			this.m_audioSource.Stop();
		}
	}

	// Token: 0x060006DA RID: 1754 RVA: 0x00039141 File Offset: 0x00037341
	public bool IsPlaying()
	{
		return !(this.m_audioSource == null) && this.m_audioSource.isPlaying;
	}

	// Token: 0x060006DB RID: 1755 RVA: 0x00039160 File Offset: 0x00037360
	private void UpdateReverb()
	{
		Camera mainCamera = Utils.GetMainCamera();
		if (this.m_distanceReverb && this.m_audioSource.spatialBlend != 0f && mainCamera != null)
		{
			float num = Vector3.Distance(mainCamera.transform.position, base.transform.position);
			bool flag = Mister.InsideMister(base.transform.position, 0f);
			float num2 = this.m_useCustomReverbDistance ? this.m_customReverbDistance : 64f;
			float num3 = Mathf.Clamp01(num / num2);
			float b = Mathf.Clamp01(this.m_audioSource.maxDistance / num2) * Mathf.Clamp01(num / this.m_audioSource.maxDistance);
			float num4 = Mathf.Max(num3, b);
			if (flag)
			{
				num4 = Mathf.Lerp(num4, 0f, num3);
				this.m_reverbPitchModifier = 0.5f * num3;
			}
			this.m_audioSource.bypassReverbZones = false;
			this.m_audioSource.reverbZoneMix = num4;
			if (this.m_baseSpread < 120f)
			{
				float a = Mathf.Max(this.m_baseSpread, 45f);
				this.m_audioSource.spread = Mathf.Lerp(a, 120f, num4);
				return;
			}
		}
		else
		{
			this.m_audioSource.bypassReverbZones = true;
		}
	}

	// Token: 0x060006DC RID: 1756 RVA: 0x0003929C File Offset: 0x0003749C
	public void Play()
	{
		if (this.m_audioSource == null)
		{
			return;
		}
		if (this.m_audioClips.Length == 0)
		{
			return;
		}
		if (!this.m_audioSource.gameObject.activeInHierarchy)
		{
			return;
		}
		if (!AudioMan.instance.RequestPlaySound(this))
		{
			return;
		}
		if (this.m_audioSource.loop && this.m_disabledFromConcurrency)
		{
			this.m_concurrencyVolumeModifier = 0f;
		}
		if (ClosedCaptions.Valid && !this.m_audioSource.loop && this.m_closedCaptionToken.Length > 0)
		{
			ClosedCaptions.Instance.RegisterCaption(this, ClosedCaptions.CaptionType.Default);
		}
		int num = UnityEngine.Random.Range(0, this.m_audioClips.Length);
		this.m_audioSource.clip = this.m_audioClips[num];
		this.m_audioSource.pitch = UnityEngine.Random.Range(this.m_minPitch, this.m_maxPitch);
		this.m_basePitch = this.m_audioSource.pitch;
		if (this.m_randomPan)
		{
			this.m_audioSource.panStereo = UnityEngine.Random.Range(this.m_minPan, this.m_maxPan);
		}
		this.m_vol = UnityEngine.Random.Range(this.m_minVol, this.m_maxVol);
		if (this.m_fadeInDuration > 0f)
		{
			this.m_audioSource.volume = 0f;
			this.m_fadeInTimer = 0f;
		}
		else
		{
			this.m_audioSource.volume = this.m_vol;
		}
		this.UpdateReverb();
		this.m_audioSource.Play();
	}

	// Token: 0x060006DD RID: 1757 RVA: 0x00039408 File Offset: 0x00037608
	public void GenerateHash()
	{
		this.m_hash = Guid.NewGuid().GetHashCode();
	}

	// Token: 0x060006DE RID: 1758 RVA: 0x0003942E File Offset: 0x0003762E
	public float GetConcurrencyDistance()
	{
		if (!this.m_ignoreConcurrencyDistance)
		{
			return Mathf.Max(1f, this.m_audioSource.minDistance);
		}
		return float.PositiveInfinity;
	}

	// Token: 0x060006DF RID: 1759 RVA: 0x00039453 File Offset: 0x00037653
	public void ConcurrencyDisable()
	{
		this.m_disabledFromConcurrency = true;
	}

	// Token: 0x060006E0 RID: 1760 RVA: 0x0003945C File Offset: 0x0003765C
	public void ConcurrencyEnable()
	{
		this.m_disabledFromConcurrency = false;
	}

	// Token: 0x060006E1 RID: 1761 RVA: 0x00039468 File Offset: 0x00037668
	public float GetVolumeModifierByDistance(float distance)
	{
		float result;
		switch (this.m_audioSource.rolloffMode)
		{
		case AudioRolloffMode.Logarithmic:
		{
			float f = distance / this.m_audioSource.minDistance;
			result = 1f * (1f / (1f + 1f * Mathf.Log(f)));
			break;
		}
		default:
			result = Mathf.InverseLerp(this.m_audioSource.maxDistance, this.m_audioSource.minDistance, distance);
			break;
		case AudioRolloffMode.Custom:
		{
			float time = Mathf.InverseLerp(this.m_audioSource.minDistance, this.m_audioSource.maxDistance, distance);
			result = this.m_audioSource.GetCustomCurve(AudioSourceCurveType.CustomRolloff).Evaluate(time);
			break;
		}
		}
		return result;
	}

	// Token: 0x060006E2 RID: 1762 RVA: 0x00039518 File Offset: 0x00037718
	public bool IsLooping()
	{
		return this.m_audioSource.loop;
	}

	// Token: 0x060006E3 RID: 1763 RVA: 0x00039525 File Offset: 0x00037725
	public void SetVolumeModifier(float v)
	{
		this.m_volumeModifier = v;
	}

	// Token: 0x060006E4 RID: 1764 RVA: 0x0003952E File Offset: 0x0003772E
	public float GetVolumeModifier()
	{
		return this.m_volumeModifier;
	}

	// Token: 0x060006E5 RID: 1765 RVA: 0x00039536 File Offset: 0x00037736
	public void SetPitchModifier(float p)
	{
		this.m_pitchModifier = p;
	}

	// Token: 0x060006E6 RID: 1766 RVA: 0x0003953F File Offset: 0x0003773F
	public float GetPitchModifier()
	{
		return this.m_pitchModifier;
	}

	// Token: 0x1700001C RID: 28
	// (get) Token: 0x060006E7 RID: 1767 RVA: 0x00039547 File Offset: 0x00037747
	public static List<IMonoUpdater> Instances { get; } = new List<IMonoUpdater>();

	// Token: 0x040007FD RID: 2045
	public bool m_playOnAwake = true;

	// Token: 0x040007FE RID: 2046
	[Header("Captions")]
	[global::Tooltip("If set, will create an entry in the Closed Captions display.")]
	public string m_closedCaptionToken = "";

	// Token: 0x040007FF RID: 2047
	[global::Tooltip("Appended after the first token, usually used for actions - $enemyname $attack for example.")]
	public string m_secondaryCaptionToken = "";

	// Token: 0x04000800 RID: 2048
	[global::Tooltip("Sorted in order of importance. If too many captions are pushed, low importance ones will be removed first.")]
	public ClosedCaptions.CaptionType m_captionType;

	// Token: 0x04000801 RID: 2049
	[global::Tooltip("Don't draw a caption if the sound is quieter than this (takes distance fading into account)")]
	public float m_minimumCaptionVolume = 0.3f;

	// Token: 0x04000802 RID: 2050
	[Header("Clips")]
	public AudioClip[] m_audioClips = new AudioClip[0];

	// Token: 0x04000803 RID: 2051
	[Header("Audio System")]
	[global::Tooltip("How many of the same sound can play in a small area? Uses the min distance of 3D sounds, or 1 meter, whichever is higher")]
	public int m_maxConcurrentSources;

	// Token: 0x04000804 RID: 2052
	[global::Tooltip("Ignore the distance check, don't play sound if any other of the same sound were played recently")]
	public bool m_ignoreConcurrencyDistance;

	// Token: 0x04000805 RID: 2053
	[Header("Random")]
	public float m_maxPitch = 1f;

	// Token: 0x04000806 RID: 2054
	public float m_minPitch = 1f;

	// Token: 0x04000807 RID: 2055
	public float m_maxVol = 1f;

	// Token: 0x04000808 RID: 2056
	public float m_minVol = 1f;

	// Token: 0x04000809 RID: 2057
	[Header("Fade")]
	public float m_fadeInDuration;

	// Token: 0x0400080A RID: 2058
	public float m_fadeOutDuration;

	// Token: 0x0400080B RID: 2059
	public float m_fadeOutDelay;

	// Token: 0x0400080C RID: 2060
	public bool m_fadeOutOnAwake;

	// Token: 0x0400080D RID: 2061
	[Header("Pan")]
	public bool m_randomPan;

	// Token: 0x0400080E RID: 2062
	public float m_minPan = -1f;

	// Token: 0x0400080F RID: 2063
	public float m_maxPan = 1f;

	// Token: 0x04000810 RID: 2064
	[Header("Delay")]
	public float m_maxDelay;

	// Token: 0x04000811 RID: 2065
	public float m_minDelay;

	// Token: 0x04000812 RID: 2066
	[Header("Reverb")]
	public bool m_distanceReverb = true;

	// Token: 0x04000813 RID: 2067
	public bool m_useCustomReverbDistance;

	// Token: 0x04000814 RID: 2068
	public float m_customReverbDistance = 10f;

	// Token: 0x04000815 RID: 2069
	[HideInInspector]
	public int m_hash;

	// Token: 0x04000816 RID: 2070
	private const float m_globalReverbDistance = 64f;

	// Token: 0x04000817 RID: 2071
	private const float m_minReverbSpread = 45f;

	// Token: 0x04000818 RID: 2072
	private const float m_maxReverbSpread = 120f;

	// Token: 0x04000819 RID: 2073
	private float m_delay;

	// Token: 0x0400081A RID: 2074
	private float m_time;

	// Token: 0x0400081B RID: 2075
	private float m_fadeOutTimer = -1f;

	// Token: 0x0400081C RID: 2076
	private float m_fadeInTimer = -1f;

	// Token: 0x0400081D RID: 2077
	private float m_vol = 1f;

	// Token: 0x0400081E RID: 2078
	private float m_concurrencyVolumeModifier = 1f;

	// Token: 0x0400081F RID: 2079
	private float m_volumeModifier = 1f;

	// Token: 0x04000820 RID: 2080
	private float m_pitchModifier = 1f;

	// Token: 0x04000821 RID: 2081
	private float m_reverbPitchModifier;

	// Token: 0x04000822 RID: 2082
	private bool m_disabledFromConcurrency;

	// Token: 0x04000823 RID: 2083
	private float m_baseSpread;

	// Token: 0x04000824 RID: 2084
	private float m_basePitch;

	// Token: 0x04000825 RID: 2085
	private float m_updateReverbTimer;

	// Token: 0x04000826 RID: 2086
	private AudioSource m_audioSource;
}
