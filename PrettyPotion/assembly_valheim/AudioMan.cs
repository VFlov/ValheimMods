using System;
using System.Collections.Generic;
using CircularBuffer;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.Rendering;

// Token: 0x0200010C RID: 268
public class AudioMan : MonoBehaviour
{
	// Token: 0x17000098 RID: 152
	// (get) Token: 0x060010FB RID: 4347 RVA: 0x0007B97A File Offset: 0x00079B7A
	public static AudioMan instance
	{
		get
		{
			return AudioMan.m_instance;
		}
	}

	// Token: 0x060010FC RID: 4348 RVA: 0x0007B984 File Offset: 0x00079B84
	private void Awake()
	{
		if (AudioMan.m_instance != null)
		{
			ZLog.Log("Audioman already exist, destroying self");
			UnityEngine.Object.Destroy(base.gameObject);
			return;
		}
		AudioMan.m_instance = this;
		UnityEngine.Object.DontDestroyOnLoad(base.gameObject);
		GameObject gameObject = new GameObject("ocean_ambient_loop");
		gameObject.transform.SetParent(base.transform);
		this.m_oceanAmbientSource = gameObject.AddComponent<AudioSource>();
		this.m_oceanAmbientSource.loop = true;
		this.m_oceanAmbientSource.spatialBlend = 0.75f;
		this.m_oceanAmbientSource.outputAudioMixerGroup = this.m_ambientMixer;
		this.m_oceanAmbientSource.maxDistance = 128f;
		this.m_oceanAmbientSource.minDistance = 40f;
		this.m_oceanAmbientSource.spread = 90f;
		this.m_oceanAmbientSource.rolloffMode = AudioRolloffMode.Linear;
		this.m_oceanAmbientSource.clip = this.m_oceanAudio;
		this.m_oceanAmbientSource.bypassReverbZones = true;
		this.m_oceanAmbientSource.dopplerLevel = 0f;
		this.m_oceanAmbientSource.volume = 0f;
		this.m_oceanAmbientSource.priority = 0;
		this.m_oceanAmbientSource.Play();
		GameObject gameObject2 = new GameObject("ambient_loop");
		gameObject2.transform.SetParent(base.transform);
		this.m_ambientLoopSource = gameObject2.AddComponent<AudioSource>();
		this.m_ambientLoopSource.loop = true;
		this.m_ambientLoopSource.spatialBlend = 0f;
		this.m_ambientLoopSource.outputAudioMixerGroup = this.m_ambientMixer;
		this.m_ambientLoopSource.bypassReverbZones = true;
		this.m_ambientLoopSource.priority = 0;
		this.m_ambientLoopSource.volume = 0f;
		GameObject gameObject3 = new GameObject("wind_loop");
		gameObject3.transform.SetParent(base.transform);
		this.m_windLoopSource = gameObject3.AddComponent<AudioSource>();
		this.m_windLoopSource.loop = true;
		this.m_windLoopSource.spatialBlend = 0f;
		this.m_windLoopSource.outputAudioMixerGroup = this.m_ambientMixer;
		this.m_windLoopSource.bypassReverbZones = true;
		this.m_windLoopSource.clip = this.m_windAudio;
		this.m_windLoopSource.volume = 0f;
		this.m_windLoopSource.priority = 0;
		this.m_windLoopSource.Play();
		if (this.m_enableShieldDomeHum)
		{
			GameObject gameObject4 = UnityEngine.Object.Instantiate<GameObject>(this.m_shieldHumPrefab);
			gameObject4.transform.SetParent(base.transform);
			this.m_shieldHumSource = gameObject4.GetComponent<AudioSource>();
		}
		this.m_maxLavaLoops = this.GetLoopingMaxConcurrency(this.m_lavaLoopPrefab.GetComponent<ZSFX>());
	}

	// Token: 0x060010FD RID: 4349 RVA: 0x0007BC08 File Offset: 0x00079E08
	private void Start()
	{
		if (SystemInfo.graphicsDeviceType == GraphicsDeviceType.Null)
		{
			AudioListener.volume = 0f;
			return;
		}
		AudioListener.volume = PlayerPrefs.GetFloat("MasterVolume", AudioListener.volume);
		AudioMan.SetSFXVolume(PlayerPrefs.GetFloat("SfxVolume", AudioMan.GetSFXVolume()));
	}

	// Token: 0x060010FE RID: 4350 RVA: 0x0007BC45 File Offset: 0x00079E45
	private void OnApplicationQuit()
	{
		this.StopAllAudio();
	}

	// Token: 0x060010FF RID: 4351 RVA: 0x0007BC4D File Offset: 0x00079E4D
	private void OnDestroy()
	{
		if (AudioMan.m_instance == this)
		{
			AudioMan.m_instance = null;
		}
	}

	// Token: 0x06001100 RID: 4352 RVA: 0x0007BC64 File Offset: 0x00079E64
	private void StopAllAudio()
	{
		AudioSource[] array = UnityEngine.Object.FindObjectsOfType(typeof(AudioSource)) as AudioSource[];
		for (int i = 0; i < array.Length; i++)
		{
			array[i].Stop();
		}
	}

	// Token: 0x06001101 RID: 4353 RVA: 0x0007BC9C File Offset: 0x00079E9C
	private void Update()
	{
		float deltaTime = Time.deltaTime;
		this.m_zoneSystemValid = (ZoneSystem.instance != null);
		this.m_envManValid = (EnvMan.instance != null);
		bool inMenu = this.InMenu();
		this.m_listenerPos = this.GetActiveAudioListener().transform.position;
		this.UpdateAmbientLoop(deltaTime);
		this.UpdateRandomAmbient(deltaTime, inMenu);
		this.UpdateLavaAmbient(deltaTime, inMenu);
		this.UpdateSnapshots(deltaTime, inMenu);
		this.UpdateLoopingConcurrency();
		this.UpdateShieldHum();
	}

	// Token: 0x06001102 RID: 4354 RVA: 0x0007BD1C File Offset: 0x00079F1C
	private void UpdateShieldHum()
	{
		if (!this.m_enableShieldDomeHum)
		{
			return;
		}
		if (ShieldGenerator.HasShields())
		{
			if (!this.m_shieldHumSource.isPlaying)
			{
				this.m_shieldHumSource.Play();
			}
			this.m_shieldHumSource.transform.position = ShieldGenerator.GetClosestShieldPoint(this.GetActiveAudioListener().transform.position);
			return;
		}
		this.m_shieldHumSource.Stop();
	}

	// Token: 0x06001103 RID: 4355 RVA: 0x0007BD84 File Offset: 0x00079F84
	private void FixedUpdate()
	{
		float fixedDeltaTime = Time.fixedDeltaTime;
		this.m_zoneSystemValid = (ZoneSystem.instance != null);
		this.m_envManValid = (EnvMan.instance != null);
		this.UpdateOceanAmbiance(fixedDeltaTime);
		this.UpdateWindAmbience(fixedDeltaTime);
	}

	// Token: 0x06001104 RID: 4356 RVA: 0x0007BDC8 File Offset: 0x00079FC8
	public static float GetSFXVolume()
	{
		if (AudioMan.m_instance == null)
		{
			return 1f;
		}
		float num;
		AudioMan.m_instance.m_masterMixer.GetFloat("SfxVol", out num);
		if (num <= -80f)
		{
			return 0f;
		}
		return Mathf.Pow(10f, num / 10f);
	}

	// Token: 0x06001105 RID: 4357 RVA: 0x0007BE20 File Offset: 0x0007A020
	public static void SetSFXVolume(float vol)
	{
		if (AudioMan.m_instance == null)
		{
			return;
		}
		float value = (vol > 0f) ? (Mathf.Log10(Mathf.Clamp(vol, 0.001f, 1f)) * 10f) : -80f;
		AudioMan.m_instance.m_masterMixer.SetFloat("SfxVol", value);
		AudioMan.m_instance.m_masterMixer.SetFloat("GuiVol", value);
	}

	// Token: 0x06001106 RID: 4358 RVA: 0x0007BE94 File Offset: 0x0007A094
	private void UpdateRandomAmbient(float dt, bool inMenu)
	{
		if (inMenu)
		{
			return;
		}
		this.m_randomAmbientTimer += dt;
		if (this.m_randomAmbientTimer > this.m_randomAmbientInterval)
		{
			this.m_randomAmbientTimer = 0f;
			if (UnityEngine.Random.value <= this.m_randomAmbientChance)
			{
				float num = 0f;
				AudioClip audioClip;
				if (this.SelectRandomAmbientClip(out audioClip, out num))
				{
					Vector3 randomAmbiencePoint = this.GetRandomAmbiencePoint();
					GameObject gameObject = UnityEngine.Object.Instantiate<GameObject>(this.m_randomAmbientPrefab, randomAmbiencePoint, Quaternion.identity, base.transform);
					ZSFX component = gameObject.GetComponent<ZSFX>();
					component.m_audioClips = new AudioClip[]
					{
						audioClip
					};
					component.Play();
					TimedDestruction component2 = gameObject.GetComponent<TimedDestruction>();
					if (num > 0f)
					{
						component.m_fadeOutDelay = 0f;
						component.m_fadeOutDuration = num;
						component.m_fadeOutOnAwake = true;
						component2.m_timeout = num + 2f;
					}
					else
					{
						component.m_fadeOutDelay = audioClip.length - 1f;
						component.m_fadeOutDuration = 1f;
						component.m_fadeOutOnAwake = true;
						component2.m_timeout = audioClip.length * 1.5f;
					}
					component2.Trigger();
				}
			}
		}
	}

	// Token: 0x06001107 RID: 4359 RVA: 0x0007BFB0 File Offset: 0x0007A1B0
	private void UpdateLavaAmbient(float dt, bool inMenu)
	{
		this.ScanForLava();
		this.UpdateLavaAmbientLoops();
		if (inMenu)
		{
			return;
		}
		if (this.m_validLavaPositions.Size == 0 || !this.m_envManValid || EnvMan.instance.GetCurrentBiome() != Heightmap.Biome.AshLands)
		{
			return;
		}
		this.m_lavaAmbientTimer += dt;
		if (this.m_lavaAmbientTimer < this.m_lavaNoiseInterval)
		{
			return;
		}
		this.m_lavaAmbientTimer = 0f;
		if (UnityEngine.Random.value > this.m_lavaNoiseChance)
		{
			return;
		}
		int i = 0;
		Vector3 vector = Vector3.zero;
		while (i < 5)
		{
			vector = this.m_validLavaPositions[UnityEngine.Random.Range(0, this.m_validLavaPositions.Size - 1)];
			float num = vector.DistanceTo(this.GetActiveAudioListener().transform.position);
			if (num > this.m_lavaNoiseMinDistance && num < this.m_lavaNoiseMaxDistance)
			{
				break;
			}
			i++;
		}
		if (i != 5)
		{
			GameObject gameObject = UnityEngine.Object.Instantiate<GameObject>(this.m_randomAmbientPrefab, vector, Quaternion.identity, base.transform);
			ZSFX component = gameObject.GetComponent<ZSFX>();
			AudioClip audioClip = this.m_randomLavaNoises[UnityEngine.Random.Range(0, this.m_randomLavaNoises.Count - 1)];
			component.m_audioClips = new AudioClip[]
			{
				audioClip
			};
			component.Play();
			TimedDestruction component2 = gameObject.GetComponent<TimedDestruction>();
			component2.m_timeout = audioClip.length;
			component2.Trigger();
		}
	}

	// Token: 0x06001108 RID: 4360 RVA: 0x0007C0F8 File Offset: 0x0007A2F8
	private void UpdateLavaAmbientLoops()
	{
		if (Time.frameCount % 24 != 0)
		{
			return;
		}
		if (this.m_ambientLavaLoops.Count < this.m_maxLavaLoops && this.m_validLavaPositions.Size > 0 && this.m_envManValid && EnvMan.instance.GetCurrentBiome() == Heightmap.Biome.AshLands)
		{
			Vector3 vector = this.m_validLavaPositions[UnityEngine.Random.Range(0, this.m_validLavaPositions.Size - 1)];
			float num = float.PositiveInfinity;
			foreach (ZSFX zsfx3 in this.m_ambientLavaLoops)
			{
				Vector3 position = zsfx3.transform.position;
				float num2 = position.DistanceTo(vector);
				if (num2 < num && zsfx3.transform.position != position)
				{
					num = num2;
				}
			}
			if (num <= this.m_minDistanceBetweenLavaLoops)
			{
				return;
			}
			ZSFX component = UnityEngine.Object.Instantiate<GameObject>(this.m_lavaLoopPrefab, vector, Quaternion.identity).GetComponent<ZSFX>();
			component.OnDestroyingSfx += delegate(ZSFX zsfx)
			{
				if (this.m_ambientLavaLoops.Contains(zsfx))
				{
					this.m_ambientLavaLoops.Remove(zsfx);
				}
			};
			this.m_ambientLavaLoops.Add(component);
		}
		for (int i = this.m_ambientLavaLoops.Count - 1; i >= 0; i--)
		{
			ZSFX zsfx2 = this.m_ambientLavaLoops[i];
			if (zsfx2.gameObject.transform.position.DistanceTo(this.m_listenerPos) >= this.m_maxLavaLoopDistance)
			{
				zsfx2.GetComponent<TimedDestruction>().Trigger();
				this.m_ambientLavaLoops.Remove(zsfx2);
			}
		}
	}

	// Token: 0x06001109 RID: 4361 RVA: 0x0007C298 File Offset: 0x0007A498
	private void ScanForLava()
	{
		if (Time.frameCount % 12 != 0 || !this.m_envManValid || EnvMan.instance.GetCurrentBiome() != Heightmap.Biome.AshLands || !this.m_zoneSystemValid)
		{
			return;
		}
		Vector2 vector = UnityEngine.Random.insideUnitCircle.normalized;
		vector *= UnityEngine.Random.Range(2f, this.m_lavaScanRadius);
		Vector3 item = this.m_listenerPos + new Vector3(vector.x, 0f, vector.y);
		if (ZoneSystem.instance.IsLava(ref item, false))
		{
			this.m_validLavaPositions.PushFront(item);
		}
	}

	// Token: 0x0600110A RID: 4362 RVA: 0x0007C330 File Offset: 0x0007A530
	private Vector3 GetRandomAmbiencePoint()
	{
		float f = UnityEngine.Random.value * 3.1415927f * 2f;
		float num = UnityEngine.Random.Range(this.m_randomMinDistance, this.m_randomMaxDistance);
		return this.m_listenerPos + new Vector3(Mathf.Sin(f) * num, 0f, Mathf.Cos(f) * num);
	}

	// Token: 0x0600110B RID: 4363 RVA: 0x0007C388 File Offset: 0x0007A588
	private bool SelectRandomAmbientClip(out AudioClip clip, out float fadeoutDuration)
	{
		fadeoutDuration = 0f;
		clip = null;
		if (!this.m_envManValid)
		{
			return false;
		}
		EnvSetup currentEnvironment = EnvMan.instance.GetCurrentEnvironment();
		AudioMan.BiomeAmbients biomeAmbients;
		if (currentEnvironment != null && !string.IsNullOrEmpty(currentEnvironment.m_ambientList))
		{
			biomeAmbients = this.GetAmbients(currentEnvironment.m_ambientList);
		}
		else
		{
			biomeAmbients = this.GetBiomeAmbients(EnvMan.instance.GetCurrentBiome());
		}
		if (biomeAmbients == null)
		{
			return false;
		}
		fadeoutDuration = biomeAmbients.m_forceFadeout;
		List<AudioClip> list = new List<AudioClip>(biomeAmbients.m_randomAmbientClips);
		List<AudioClip> collection = EnvMan.IsDaylight() ? biomeAmbients.m_randomAmbientClipsDay : biomeAmbients.m_randomAmbientClipsNight;
		list.AddRange(collection);
		if (list.Count == 0)
		{
			return false;
		}
		clip = list[UnityEngine.Random.Range(0, list.Count)];
		return true;
	}

	// Token: 0x0600110C RID: 4364 RVA: 0x0007C43C File Offset: 0x0007A63C
	private void UpdateAmbientLoop(float dt)
	{
		if (!this.m_envManValid)
		{
			this.m_ambientLoopSource.Stop();
			return;
		}
		if (this.m_queuedAmbientLoop || this.m_stopAmbientLoop)
		{
			if (this.m_ambientLoopSource.isPlaying && this.m_ambientLoopSource.volume > 0f)
			{
				this.m_ambientLoopSource.volume = Mathf.MoveTowards(this.m_ambientLoopSource.volume, 0f, dt / this.m_ambientFadeTime);
				return;
			}
			this.m_ambientLoopSource.Stop();
			this.m_stopAmbientLoop = false;
			if (this.m_queuedAmbientLoop)
			{
				this.m_ambientLoopSource.clip = this.m_queuedAmbientLoop;
				this.m_ambientLoopSource.volume = 0f;
				this.m_ambientLoopSource.Play();
				this.m_ambientVol = this.m_queuedAmbientVol;
				this.m_queuedAmbientLoop = null;
				return;
			}
		}
		else if (this.m_ambientLoopSource.isPlaying)
		{
			this.m_ambientLoopSource.volume = Mathf.MoveTowards(this.m_ambientLoopSource.volume, this.m_ambientVol, dt / this.m_ambientFadeTime);
		}
	}

	// Token: 0x0600110D RID: 4365 RVA: 0x0007C555 File Offset: 0x0007A755
	public void SetIndoor(bool indoor)
	{
		this.m_indoor = indoor;
	}

	// Token: 0x0600110E RID: 4366 RVA: 0x0007C55E File Offset: 0x0007A75E
	private bool InMenu()
	{
		return FejdStartup.instance != null || Menu.IsVisible() || (Game.instance && Game.instance.WaitingForRespawn()) || TextViewer.IsShowingIntro();
	}

	// Token: 0x0600110F RID: 4367 RVA: 0x0007C593 File Offset: 0x0007A793
	private void UpdateSnapshots(float dt, bool inMenu)
	{
		if (inMenu)
		{
			this.SetSnapshot(AudioMan.Snapshot.Menu);
			return;
		}
		if (this.m_indoor)
		{
			this.SetSnapshot(AudioMan.Snapshot.Indoor);
			return;
		}
		this.SetSnapshot(AudioMan.Snapshot.Default);
	}

	// Token: 0x06001110 RID: 4368 RVA: 0x0007C5B8 File Offset: 0x0007A7B8
	private void SetSnapshot(AudioMan.Snapshot snapshot)
	{
		if (this.m_currentSnapshot == snapshot)
		{
			return;
		}
		this.m_currentSnapshot = snapshot;
		switch (snapshot)
		{
		case AudioMan.Snapshot.Default:
			this.m_masterMixer.FindSnapshot("Default").TransitionTo(this.m_snapshotTransitionTime);
			return;
		case AudioMan.Snapshot.Menu:
			this.m_masterMixer.FindSnapshot("Menu").TransitionTo(this.m_snapshotTransitionTime);
			return;
		case AudioMan.Snapshot.Indoor:
			this.m_masterMixer.FindSnapshot("Indoor").TransitionTo(this.m_snapshotTransitionTime);
			return;
		default:
			return;
		}
	}

	// Token: 0x06001111 RID: 4369 RVA: 0x0007C63C File Offset: 0x0007A83C
	public void StopAmbientLoop()
	{
		this.m_queuedAmbientLoop = null;
		this.m_stopAmbientLoop = true;
	}

	// Token: 0x06001112 RID: 4370 RVA: 0x0007C64C File Offset: 0x0007A84C
	public void QueueAmbientLoop(AudioClip clip, float vol)
	{
		if (this.m_queuedAmbientLoop == clip && this.m_queuedAmbientVol == vol)
		{
			return;
		}
		if (this.m_queuedAmbientLoop == null && this.m_ambientLoopSource.clip == clip && this.m_ambientVol == vol)
		{
			return;
		}
		this.m_queuedAmbientLoop = clip;
		this.m_queuedAmbientVol = vol;
		this.m_stopAmbientLoop = false;
	}

	// Token: 0x06001113 RID: 4371 RVA: 0x0007C6B4 File Offset: 0x0007A8B4
	private void UpdateWindAmbience(float dt)
	{
		if (!this.m_zoneSystemValid || !this.m_envManValid)
		{
			this.m_windLoopSource.volume = 0f;
			return;
		}
		float num = EnvMan.instance.GetWindIntensity();
		num = Mathf.Pow(num, this.m_windIntensityPower);
		num += num * Mathf.Sin(Time.time) * Mathf.Sin(Time.time * 1.54323f) * Mathf.Sin(Time.time * 2.31237f) * this.m_windVariation;
		this.m_windLoopSource.volume = Mathf.Lerp(this.m_windMinVol, this.m_windMaxVol, num);
		this.m_windLoopSource.pitch = Mathf.Lerp(this.m_windMinPitch, this.m_windMaxPitch, num);
	}

	// Token: 0x06001114 RID: 4372 RVA: 0x0007C76C File Offset: 0x0007A96C
	private void UpdateOceanAmbiance(float dt)
	{
		if (!this.m_zoneSystemValid || !this.m_envManValid)
		{
			this.m_oceanAmbientSource.volume = 0f;
			return;
		}
		this.m_oceanUpdateTimer += dt;
		if (this.m_oceanUpdateTimer > 2f)
		{
			this.m_oceanUpdateTimer = 0f;
			this.m_haveOcean = this.FindAverageOceanPoint(out this.m_avgOceanPoint);
		}
		if (this.m_haveOcean)
		{
			float windIntensity = EnvMan.instance.GetWindIntensity();
			float target = Mathf.Lerp(this.m_oceanVolumeMin, this.m_oceanVolumeMax, windIntensity);
			this.m_oceanAmbientSource.volume = Mathf.MoveTowards(this.m_oceanAmbientSource.volume, target, this.m_oceanFadeSpeed * dt);
			this.m_oceanAmbientSource.transform.position = Vector3.Lerp(this.m_oceanAmbientSource.transform.position, this.m_avgOceanPoint, this.m_oceanMoveSpeed);
			return;
		}
		this.m_oceanAmbientSource.volume = Mathf.MoveTowards(this.m_oceanAmbientSource.volume, 0f, this.m_oceanFadeSpeed * dt);
	}

	// Token: 0x06001115 RID: 4373 RVA: 0x0007C878 File Offset: 0x0007AA78
	private bool FindAverageOceanPoint(out Vector3 point)
	{
		Vector3 vector = Vector3.zero;
		int num = 0;
		Vector2i zone = ZoneSystem.GetZone(this.m_listenerPos);
		for (int i = -1; i <= 1; i++)
		{
			for (int j = -1; j <= 1; j++)
			{
				Vector2i id = zone;
				id.x += j;
				id.y += i;
				Vector3 zonePos = ZoneSystem.GetZonePos(id);
				if (this.IsOceanZone(zonePos))
				{
					num++;
					vector += zonePos;
				}
			}
		}
		if (num > 0)
		{
			vector /= (float)num;
			point = vector;
			point.y = 30f;
			return true;
		}
		point = Vector3.zero;
		return false;
	}

	// Token: 0x06001116 RID: 4374 RVA: 0x0007C920 File Offset: 0x0007AB20
	private bool IsOceanZone(Vector3 centerPos)
	{
		float groundHeight = ZoneSystem.instance.GetGroundHeight(centerPos);
		return 30f - groundHeight > this.m_oceanDepthTreshold;
	}

	// Token: 0x06001117 RID: 4375 RVA: 0x0007C94C File Offset: 0x0007AB4C
	private AudioMan.BiomeAmbients GetAmbients(string name)
	{
		foreach (AudioMan.BiomeAmbients biomeAmbients in this.m_randomAmbients)
		{
			if (biomeAmbients.m_name == name)
			{
				return biomeAmbients;
			}
		}
		return null;
	}

	// Token: 0x06001118 RID: 4376 RVA: 0x0007C9B0 File Offset: 0x0007ABB0
	private AudioMan.BiomeAmbients GetBiomeAmbients(Heightmap.Biome biome)
	{
		foreach (AudioMan.BiomeAmbients biomeAmbients in this.m_randomAmbients)
		{
			if ((biomeAmbients.m_biome & biome) != Heightmap.Biome.None)
			{
				return biomeAmbients;
			}
		}
		return null;
	}

	// Token: 0x06001119 RID: 4377 RVA: 0x0007CA10 File Offset: 0x0007AC10
	public bool RequestPlaySound(ZSFX sfx)
	{
		if (sfx.IsLooping())
		{
			this.RegisterLoopingSound(sfx);
			return true;
		}
		if (sfx.m_maxConcurrentSources <= 0)
		{
			return true;
		}
		int hash = sfx.m_hash;
		float time = Time.time;
		Vector3 position = sfx.gameObject.transform.position;
		int num = 0;
		foreach (AudioMan.SoundHash soundHash in this.m_soundList)
		{
			if (hash == soundHash.hash)
			{
				if (time - soundHash.playTime < this.m_concurrencyThreshold && Vector3.Distance(soundHash.position, position) < sfx.GetConcurrencyDistance())
				{
					num++;
				}
				if (num >= sfx.m_maxConcurrentSources)
				{
					return false;
				}
			}
		}
		this.m_soundList.PushFront(new AudioMan.SoundHash(hash, time, position));
		return true;
	}

	// Token: 0x0600111A RID: 4378 RVA: 0x0007CAF4 File Offset: 0x0007ACF4
	private void RegisterLoopingSound(ZSFX sfx)
	{
		if (this.GetLoopingMaxConcurrency(sfx) < 1)
		{
			return;
		}
		int num = 0;
		using (List<ZSFX>.Enumerator enumerator = this.m_loopingSfx.GetEnumerator())
		{
			while (enumerator.MoveNext())
			{
				if (enumerator.Current.m_hash == sfx.m_hash)
				{
					num++;
				}
			}
		}
		if (num > sfx.m_maxConcurrentSources)
		{
			sfx.ConcurrencyDisable();
		}
		this.m_loopingSfx.Add(sfx);
		sfx.OnDestroyingSfx += delegate(ZSFX zsfx)
		{
			this.m_loopingSfx.Remove(zsfx);
		};
	}

	// Token: 0x0600111B RID: 4379 RVA: 0x0007CB8C File Offset: 0x0007AD8C
	private int GetLoopingMaxConcurrency(ZSFX sfx)
	{
		if (sfx.m_maxConcurrentSources < 0)
		{
			return -1;
		}
		if (sfx.m_maxConcurrentSources != 0)
		{
			return sfx.m_maxConcurrentSources;
		}
		return this.m_forcedMaxConcurrentLoops;
	}

	// Token: 0x0600111C RID: 4380 RVA: 0x0007CBB0 File Offset: 0x0007ADB0
	private void UpdateLoopingConcurrency()
	{
		if (Time.frameCount % 16 != 0)
		{
			return;
		}
		Vector3 position = Utils.GetMainCamera().transform.position;
		this.m_checkedHashes.Clear();
		foreach (ZSFX zsfx in this.m_loopingSfx)
		{
			if (!this.m_checkedHashes.Contains(zsfx.m_hash))
			{
				this.m_checkedHashes.Add(zsfx.m_hash);
				int maxConcurrentSources = zsfx.m_maxConcurrentSources;
				this.m_tmpSameSfx.Clear();
				this.m_tmpSameSfxDistance.Clear();
				foreach (ZSFX zsfx2 in this.m_loopingSfx)
				{
					if (zsfx.m_hash == zsfx2.m_hash)
					{
						float distance = Vector3.Distance(zsfx2.gameObject.transform.position, position);
						Utils.InsertSortNoAlloc<ZSFX>(this.m_tmpSameSfx, zsfx2, this.m_tmpSameSfxDistance, distance);
					}
				}
				for (int i = 0; i < this.m_tmpSameSfx.Count; i++)
				{
					if (i > maxConcurrentSources)
					{
						this.m_tmpSameSfx[i].ConcurrencyDisable();
					}
					else
					{
						this.m_tmpSameSfx[i].ConcurrencyEnable();
					}
				}
			}
		}
	}

	// Token: 0x0600111D RID: 4381 RVA: 0x0007CD44 File Offset: 0x0007AF44
	public AudioListener GetActiveAudioListener()
	{
		if (this.m_activeAudioListener && this.m_activeAudioListener.isActiveAndEnabled)
		{
			return this.m_activeAudioListener;
		}
		AudioListener[] array = UnityEngine.Object.FindObjectsOfType<AudioListener>(false);
		this.m_activeAudioListener = Array.Find<AudioListener>(array, (AudioListener l) => l.enabled);
		return this.m_activeAudioListener;
	}

	// Token: 0x04001003 RID: 4099
	private bool m_zoneSystemValid;

	// Token: 0x04001004 RID: 4100
	private bool m_envManValid;

	// Token: 0x04001005 RID: 4101
	private readonly List<ZSFX> m_loopingSfx = new List<ZSFX>();

	// Token: 0x04001006 RID: 4102
	private readonly List<int> m_checkedHashes = new List<int>();

	// Token: 0x04001007 RID: 4103
	private readonly List<ZSFX> m_tmpSameSfx = new List<ZSFX>();

	// Token: 0x04001008 RID: 4104
	private readonly List<float> m_tmpSameSfxDistance = new List<float>();

	// Token: 0x04001009 RID: 4105
	private AudioListener m_activeAudioListener;

	// Token: 0x0400100A RID: 4106
	private static AudioMan m_instance;

	// Token: 0x0400100B RID: 4107
	[Header("Mixers")]
	public AudioMixerGroup m_ambientMixer;

	// Token: 0x0400100C RID: 4108
	public AudioMixerGroup m_guiMixer;

	// Token: 0x0400100D RID: 4109
	public AudioMixer m_masterMixer;

	// Token: 0x0400100E RID: 4110
	public float m_snapshotTransitionTime = 2f;

	// Token: 0x0400100F RID: 4111
	[Header("Wind")]
	public AudioClip m_windAudio;

	// Token: 0x04001010 RID: 4112
	public float m_windMinVol;

	// Token: 0x04001011 RID: 4113
	public float m_windMaxVol = 1f;

	// Token: 0x04001012 RID: 4114
	public float m_windMinPitch = 0.5f;

	// Token: 0x04001013 RID: 4115
	public float m_windMaxPitch = 1.5f;

	// Token: 0x04001014 RID: 4116
	public float m_windVariation = 0.2f;

	// Token: 0x04001015 RID: 4117
	public float m_windIntensityPower = 1.5f;

	// Token: 0x04001016 RID: 4118
	[Header("Ocean")]
	public AudioClip m_oceanAudio;

	// Token: 0x04001017 RID: 4119
	public float m_oceanVolumeMax = 1f;

	// Token: 0x04001018 RID: 4120
	public float m_oceanVolumeMin = 1f;

	// Token: 0x04001019 RID: 4121
	public float m_oceanFadeSpeed = 0.1f;

	// Token: 0x0400101A RID: 4122
	public float m_oceanMoveSpeed = 0.1f;

	// Token: 0x0400101B RID: 4123
	public float m_oceanDepthTreshold = 10f;

	// Token: 0x0400101C RID: 4124
	[Header("Random ambients")]
	public float m_ambientFadeTime = 2f;

	// Token: 0x0400101D RID: 4125
	[Min(1f)]
	public float m_randomAmbientInterval = 5f;

	// Token: 0x0400101E RID: 4126
	[Range(0f, 1f)]
	public float m_randomAmbientChance = 0.5f;

	// Token: 0x0400101F RID: 4127
	public float m_randomMinDistance = 5f;

	// Token: 0x04001020 RID: 4128
	public float m_randomMaxDistance = 20f;

	// Token: 0x04001021 RID: 4129
	public List<AudioMan.BiomeAmbients> m_randomAmbients = new List<AudioMan.BiomeAmbients>();

	// Token: 0x04001022 RID: 4130
	public GameObject m_randomAmbientPrefab;

	// Token: 0x04001023 RID: 4131
	[Header("Lava Ambience")]
	[Min(10f)]
	public float m_lavaScanRadius = 40f;

	// Token: 0x04001024 RID: 4132
	[Min(0f)]
	public float m_lavaNoiseMinDistance = 2f;

	// Token: 0x04001025 RID: 4133
	[Min(10f)]
	public float m_lavaNoiseMaxDistance = 10f;

	// Token: 0x04001026 RID: 4134
	[Min(1f)]
	public float m_lavaNoiseInterval = 2.5f;

	// Token: 0x04001027 RID: 4135
	[Range(0f, 1f)]
	public float m_lavaNoiseChance = 0.25f;

	// Token: 0x04001028 RID: 4136
	public List<AudioClip> m_randomLavaNoises;

	// Token: 0x04001029 RID: 4137
	public GameObject m_lavaLoopPrefab;

	// Token: 0x0400102A RID: 4138
	[Space(16f)]
	public int m_maxLavaLoops;

	// Token: 0x0400102B RID: 4139
	public float m_minDistanceBetweenLavaLoops = 10f;

	// Token: 0x0400102C RID: 4140
	public float m_maxLavaLoopDistance = 40f;

	// Token: 0x0400102D RID: 4141
	[Header("Shield Dome Hum")]
	public bool m_enableShieldDomeHum = true;

	// Token: 0x0400102E RID: 4142
	public GameObject m_shieldHumPrefab;

	// Token: 0x0400102F RID: 4143
	[Header("ZSFX Settings")]
	[Min(0f)]
	[global::Tooltip("How soon a sound trying to play after the same one counts as concurrent")]
	public float m_concurrencyThreshold = 0.2f;

	// Token: 0x04001030 RID: 4144
	[Min(0f)]
	[global::Tooltip("Automatically makes sure no looping sounds are playing more than this many at a time. ZSFX components that have a max concurrency value set will use that instead.")]
	public int m_forcedMaxConcurrentLoops = 5;

	// Token: 0x04001031 RID: 4145
	private AudioSource m_oceanAmbientSource;

	// Token: 0x04001032 RID: 4146
	private AudioSource m_ambientLoopSource;

	// Token: 0x04001033 RID: 4147
	private AudioSource m_windLoopSource;

	// Token: 0x04001034 RID: 4148
	private AudioSource m_shieldHumSource;

	// Token: 0x04001035 RID: 4149
	private AudioClip m_queuedAmbientLoop;

	// Token: 0x04001036 RID: 4150
	private float m_queuedAmbientVol;

	// Token: 0x04001037 RID: 4151
	private float m_ambientVol;

	// Token: 0x04001038 RID: 4152
	private float m_randomAmbientTimer;

	// Token: 0x04001039 RID: 4153
	private bool m_stopAmbientLoop;

	// Token: 0x0400103A RID: 4154
	private bool m_indoor;

	// Token: 0x0400103B RID: 4155
	private float m_oceanUpdateTimer;

	// Token: 0x0400103C RID: 4156
	private bool m_haveOcean;

	// Token: 0x0400103D RID: 4157
	private Vector3 m_avgOceanPoint = Vector3.zero;

	// Token: 0x0400103E RID: 4158
	private Vector3 m_listenerPos = Vector3.zero;

	// Token: 0x0400103F RID: 4159
	private float m_lavaAmbientTimer;

	// Token: 0x04001040 RID: 4160
	private CircularBuffer<Vector3> m_validLavaPositions = new CircularBuffer<Vector3>(128);

	// Token: 0x04001041 RID: 4161
	private List<ZSFX> m_ambientLavaLoops = new List<ZSFX>();

	// Token: 0x04001042 RID: 4162
	private AudioMan.Snapshot m_currentSnapshot;

	// Token: 0x04001043 RID: 4163
	private readonly CircularBuffer<AudioMan.SoundHash> m_soundList = new CircularBuffer<AudioMan.SoundHash>(512);

	// Token: 0x0200030A RID: 778
	[Serializable]
	public class BiomeAmbients
	{
		// Token: 0x040023AC RID: 9132
		public string m_name = "";

		// Token: 0x040023AD RID: 9133
		public float m_forceFadeout = 3f;

		// Token: 0x040023AE RID: 9134
		[BitMask(typeof(Heightmap.Biome))]
		public Heightmap.Biome m_biome;

		// Token: 0x040023AF RID: 9135
		public List<AudioClip> m_randomAmbientClips = new List<AudioClip>();

		// Token: 0x040023B0 RID: 9136
		public List<AudioClip> m_randomAmbientClipsDay = new List<AudioClip>();

		// Token: 0x040023B1 RID: 9137
		public List<AudioClip> m_randomAmbientClipsNight = new List<AudioClip>();
	}

	// Token: 0x0200030B RID: 779
	private enum Snapshot
	{
		// Token: 0x040023B3 RID: 9139
		Default,
		// Token: 0x040023B4 RID: 9140
		Menu,
		// Token: 0x040023B5 RID: 9141
		Indoor
	}

	// Token: 0x0200030C RID: 780
	private class SoundHash
	{
		// Token: 0x060021F2 RID: 8690 RVA: 0x000EC4F5 File Offset: 0x000EA6F5
		public SoundHash(int h, float pt, Vector3 pos)
		{
			this.hash = h;
			this.playTime = pt;
			this.position = pos;
		}

		// Token: 0x040023B6 RID: 9142
		public int hash;

		// Token: 0x040023B7 RID: 9143
		public float playTime;

		// Token: 0x040023B8 RID: 9144
		public Vector3 position;
	}
}
