using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityStandardAssets.ImageEffects;

// Token: 0x02000118 RID: 280
public class EnvMan : MonoBehaviour
{
	// Token: 0x1700009A RID: 154
	// (get) Token: 0x0600114C RID: 4428 RVA: 0x00080C46 File Offset: 0x0007EE46
	public static EnvMan instance
	{
		get
		{
			return EnvMan.s_instance;
		}
	}

	// Token: 0x0600114D RID: 4429 RVA: 0x00080C50 File Offset: 0x0007EE50
	private void Awake()
	{
		EnvMan.s_instance = this;
		foreach (EnvSetup env in this.m_environments)
		{
			this.InitializeEnvironment(env);
		}
		foreach (BiomeEnvSetup biome in this.m_biomes)
		{
			this.InitializeBiomeEnvSetup(biome);
		}
		this.m_currentEnv = this.GetDefaultEnv();
	}

	// Token: 0x0600114E RID: 4430 RVA: 0x00080CF8 File Offset: 0x0007EEF8
	private void OnDestroy()
	{
		EnvMan.s_instance = null;
	}

	// Token: 0x0600114F RID: 4431 RVA: 0x00080D00 File Offset: 0x0007EF00
	private void InitializeEnvironment(EnvSetup env)
	{
		this.SetParticleArrayEnabled(env.m_psystems, false);
		if (env.m_envObject)
		{
			env.m_envObject.SetActive(false);
		}
	}

	// Token: 0x06001150 RID: 4432 RVA: 0x00080D28 File Offset: 0x0007EF28
	private void InitializeBiomeEnvSetup(BiomeEnvSetup biome)
	{
		foreach (EnvEntry envEntry in biome.m_environments)
		{
			envEntry.m_env = this.GetEnv(envEntry.m_environment);
		}
	}

	// Token: 0x06001151 RID: 4433 RVA: 0x00080D88 File Offset: 0x0007EF88
	private void SetParticleArrayEnabled(GameObject[] psystems, bool enabled)
	{
		foreach (GameObject gameObject in psystems)
		{
			ParticleSystem[] componentsInChildren = gameObject.GetComponentsInChildren<ParticleSystem>();
			for (int j = 0; j < componentsInChildren.Length; j++)
			{
				componentsInChildren[j].emission.enabled = enabled;
			}
			MistEmitter componentInChildren = gameObject.GetComponentInChildren<MistEmitter>();
			if (componentInChildren)
			{
				componentInChildren.enabled = enabled;
			}
		}
	}

	// Token: 0x06001152 RID: 4434 RVA: 0x00080DF0 File Offset: 0x0007EFF0
	private float RescaleDayFraction(float fraction)
	{
		if (fraction >= 0.15f && fraction <= 0.85f)
		{
			float num = (fraction - 0.15f) / 0.7f;
			fraction = 0.25f + num * 0.5f;
		}
		else if (fraction < 0.5f)
		{
			fraction = fraction / 0.15f * 0.25f;
		}
		else
		{
			float num2 = (fraction - 0.85f) / 0.15f;
			fraction = 0.75f + num2 * 0.25f;
		}
		return fraction;
	}

	// Token: 0x06001153 RID: 4435 RVA: 0x00080E64 File Offset: 0x0007F064
	private void Update()
	{
		Vector3 windForce = EnvMan.instance.GetWindForce();
		this.m_cloudOffset += windForce * Time.deltaTime * 0.01f;
		Shader.SetGlobalVector(EnvMan.s_cloudOffset, this.m_cloudOffset);
		Shader.SetGlobalVector(EnvMan.s_netRefPos, ZNet.instance.GetReferencePosition());
	}

	// Token: 0x06001154 RID: 4436 RVA: 0x00080ED0 File Offset: 0x0007F0D0
	private void FixedUpdate()
	{
		if (Time.frameCount == EnvMan.s_lastFrame)
		{
			return;
		}
		EnvMan.s_lastFrame = Time.frameCount;
		this.UpdateTimeSkip(Time.fixedDeltaTime);
		this.m_totalSeconds = ZNet.instance.GetTimeSeconds();
		long num = (long)this.m_totalSeconds;
		double num2 = this.m_totalSeconds * 1000.0;
		long num3 = this.m_dayLengthSec * 1000L;
		float num4 = Mathf.Clamp01((float)(num2 % (double)num3 / 1000.0) / (float)this.m_dayLengthSec);
		num4 = this.RescaleDayFraction(num4);
		float smoothDayFraction = this.m_smoothDayFraction;
		float t = Mathf.LerpAngle(this.m_smoothDayFraction * 360f, num4 * 360f, 0.01f);
		this.m_smoothDayFraction = Mathf.Repeat(t, 360f) / 360f;
		if (this.m_debugTimeOfDay)
		{
			this.m_smoothDayFraction = this.m_debugTime;
		}
		float num5 = Mathf.Pow(Mathf.Max(1f - Mathf.Clamp01(this.m_smoothDayFraction / 0.25f), Mathf.Clamp01((this.m_smoothDayFraction - 0.75f) / 0.25f)), 0.5f);
		float num6 = Mathf.Pow(Mathf.Clamp01(1f - Mathf.Abs(this.m_smoothDayFraction - 0.5f) / 0.25f), 0.5f);
		float num7 = Mathf.Min(Mathf.Clamp01(1f - (this.m_smoothDayFraction - 0.26f) / -this.m_sunHorizonTransitionL), Mathf.Clamp01(1f - (this.m_smoothDayFraction - 0.26f) / this.m_sunHorizonTransitionH));
		float num8 = Mathf.Min(Mathf.Clamp01(1f - (this.m_smoothDayFraction - 0.74f) / -this.m_sunHorizonTransitionH), Mathf.Clamp01(1f - (this.m_smoothDayFraction - 0.74f) / this.m_sunHorizonTransitionL));
		float num9 = 1f / (num5 + num6 + num7 + num8);
		num5 *= num9;
		num6 *= num9;
		num7 *= num9;
		num8 *= num9;
		Heightmap.Biome biome = this.GetBiome();
		this.UpdateTriggers(smoothDayFraction, this.m_smoothDayFraction, biome, Time.fixedDeltaTime);
		this.UpdateEnvironment(num, biome);
		this.InterpolateEnvironment(Time.fixedDeltaTime);
		this.UpdateWind(num, Time.fixedDeltaTime);
		if (!string.IsNullOrEmpty(this.m_forceEnv))
		{
			EnvSetup env = this.GetEnv(this.m_forceEnv);
			if (env != null)
			{
				this.SetEnv(env, num6, num5, num7, num8, Time.fixedDeltaTime);
			}
		}
		else
		{
			this.SetEnv(this.m_currentEnv, num6, num5, num7, num8, Time.fixedDeltaTime);
		}
		EnvMan.s_isDay = this.CalculateDay();
		EnvMan.s_isDaylight = this.CalculateDaylight();
		EnvMan.s_isAfternoon = this.CalculateAfternoon();
		EnvMan.s_isCold = this.CalculateCold();
		EnvMan.s_isFreezing = this.CalculateFreezing();
		EnvMan.s_isNight = this.CalculateNight();
		EnvMan.s_isWet = this.CalculateWet();
		EnvMan.s_canSleep = this.CalculateCanSleep();
	}

	// Token: 0x06001155 RID: 4437 RVA: 0x000811B3 File Offset: 0x0007F3B3
	private int GetCurrentDay()
	{
		return (int)(this.m_totalSeconds / (double)this.m_dayLengthSec);
	}

	// Token: 0x06001156 RID: 4438 RVA: 0x000811C4 File Offset: 0x0007F3C4
	private void UpdateTriggers(float oldDayFraction, float newDayFraction, Heightmap.Biome biome, float dt)
	{
		if (Player.m_localPlayer == null || biome == Heightmap.Biome.None)
		{
			return;
		}
		EnvSetup currentEnvironment = this.GetCurrentEnvironment();
		if (currentEnvironment == null)
		{
			return;
		}
		this.UpdateAmbientMusic(biome, currentEnvironment, dt);
		if (oldDayFraction > 0.2f && oldDayFraction < 0.25f && newDayFraction > 0.25f && newDayFraction < 0.3f)
		{
			this.OnMorning(biome, currentEnvironment);
		}
		if (oldDayFraction > 0.7f && oldDayFraction < 0.75f && newDayFraction > 0.75f && newDayFraction < 0.8f)
		{
			this.OnEvening(biome, currentEnvironment);
		}
	}

	// Token: 0x06001157 RID: 4439 RVA: 0x00081248 File Offset: 0x0007F448
	private void UpdateAmbientMusic(Heightmap.Biome biome, EnvSetup currentEnv, float dt)
	{
		this.m_ambientMusicTimer += dt;
		if (this.m_ambientMusicTimer > 2f)
		{
			this.m_ambientMusicTimer = 0f;
			this.m_ambientMusic = null;
			BiomeEnvSetup biomeEnvSetup = this.GetBiomeEnvSetup(biome);
			if (EnvMan.IsDay())
			{
				if (currentEnv.m_musicDay.Length > 0)
				{
					this.m_ambientMusic = currentEnv.m_musicDay;
					return;
				}
				if (biomeEnvSetup.m_musicDay.Length > 0)
				{
					this.m_ambientMusic = biomeEnvSetup.m_musicDay;
					return;
				}
			}
			else
			{
				if (currentEnv.m_musicNight.Length > 0)
				{
					this.m_ambientMusic = currentEnv.m_musicNight;
					return;
				}
				if (biomeEnvSetup.m_musicNight.Length > 0)
				{
					this.m_ambientMusic = biomeEnvSetup.m_musicNight;
				}
			}
		}
	}

	// Token: 0x06001158 RID: 4440 RVA: 0x000812FF File Offset: 0x0007F4FF
	public string GetAmbientMusic()
	{
		return this.m_ambientMusic;
	}

	// Token: 0x06001159 RID: 4441 RVA: 0x00081308 File Offset: 0x0007F508
	private void OnMorning(Heightmap.Biome biome, EnvSetup currentEnv)
	{
		string text = "morning";
		if (currentEnv.m_musicMorning.Length > 0)
		{
			if (currentEnv.m_musicMorning == currentEnv.m_musicDay)
			{
				text = "-";
			}
			else
			{
				text = currentEnv.m_musicMorning;
			}
		}
		else
		{
			BiomeEnvSetup biomeEnvSetup = this.GetBiomeEnvSetup(biome);
			if (biomeEnvSetup.m_musicMorning.Length > 0)
			{
				if (biomeEnvSetup.m_musicMorning == biomeEnvSetup.m_musicDay)
				{
					text = "-";
				}
				else
				{
					text = biomeEnvSetup.m_musicMorning;
				}
			}
		}
		if (text != "-")
		{
			MusicMan.instance.TriggerMusic(text);
		}
		Player.m_localPlayer.Message(MessageHud.MessageType.Center, Localization.instance.Localize("$msg_newday", new string[]
		{
			this.GetCurrentDay().ToString()
		}), 0, null);
	}

	// Token: 0x0600115A RID: 4442 RVA: 0x000813D0 File Offset: 0x0007F5D0
	private void OnEvening(Heightmap.Biome biome, EnvSetup currentEnv)
	{
		string text = "evening";
		if (currentEnv.m_musicEvening.Length > 0)
		{
			if (currentEnv.m_musicEvening == currentEnv.m_musicNight)
			{
				text = "-";
			}
			else
			{
				text = currentEnv.m_musicEvening;
			}
		}
		else
		{
			BiomeEnvSetup biomeEnvSetup = this.GetBiomeEnvSetup(biome);
			if (biomeEnvSetup.m_musicEvening.Length > 0)
			{
				if (biomeEnvSetup.m_musicEvening == biomeEnvSetup.m_musicNight)
				{
					text = "-";
				}
				else
				{
					text = biomeEnvSetup.m_musicEvening;
				}
			}
		}
		if (text != "-")
		{
			MusicMan.instance.TriggerMusic(text);
		}
		MusicMan.instance.TriggerMusic(text);
	}

	// Token: 0x0600115B RID: 4443 RVA: 0x00081470 File Offset: 0x0007F670
	public void SetForceEnvironment(string env)
	{
		if (this.m_forceEnv == env)
		{
			return;
		}
		ZLog.Log("Setting forced environment " + env);
		this.m_forceEnv = env;
		this.FixedUpdate();
		if (ReflectionUpdate.instance)
		{
			ReflectionUpdate.instance.UpdateReflection();
		}
	}

	// Token: 0x0600115C RID: 4444 RVA: 0x000814C0 File Offset: 0x0007F6C0
	private EnvSetup SelectWeightedEnvironment(List<EnvEntry> environments)
	{
		float num = 0f;
		foreach (EnvEntry envEntry in environments)
		{
			if (!envEntry.m_ashlandsOverride && !envEntry.m_deepnorthOverride)
			{
				num += envEntry.m_weight;
			}
		}
		float num2 = UnityEngine.Random.Range(0f, num);
		float num3 = 0f;
		foreach (EnvEntry envEntry2 in environments)
		{
			if (!envEntry2.m_ashlandsOverride && !envEntry2.m_deepnorthOverride)
			{
				num3 += envEntry2.m_weight;
				if (num3 >= num2)
				{
					return envEntry2.m_env;
				}
			}
		}
		EnvEntry envEntry3 = environments[environments.Count - 1];
		if (envEntry3.m_ashlandsOverride || envEntry3.m_deepnorthOverride)
		{
			return null;
		}
		return envEntry3.m_env;
	}

	// Token: 0x0600115D RID: 4445 RVA: 0x000815CC File Offset: 0x0007F7CC
	private string GetEnvironmentOverride()
	{
		if (!string.IsNullOrEmpty(this.m_debugEnv))
		{
			return this.m_debugEnv;
		}
		if (Player.m_localPlayer != null && Player.m_localPlayer.InIntro())
		{
			return this.m_introEnvironment;
		}
		string envOverride = RandEventSystem.instance.GetEnvOverride();
		if (!string.IsNullOrEmpty(envOverride))
		{
			return envOverride;
		}
		string environment = EnvZone.GetEnvironment();
		if (!string.IsNullOrEmpty(environment))
		{
			return environment;
		}
		return null;
	}

	// Token: 0x0600115E RID: 4446 RVA: 0x00081634 File Offset: 0x0007F834
	private void UpdateEnvironment(long sec, Heightmap.Biome biome)
	{
		string environmentOverride = this.GetEnvironmentOverride();
		if (!string.IsNullOrEmpty(environmentOverride))
		{
			this.m_environmentPeriod = -1L;
			this.m_currentBiome = this.GetBiome();
			this.QueueEnvironment(environmentOverride);
			return;
		}
		long num = sec / this.m_environmentDuration;
		Vector3 position = Utils.GetMainCamera().transform.position;
		bool flag = WorldGenerator.IsAshlands(position.x, position.z);
		bool flag2 = WorldGenerator.IsDeepnorth(position.x, position.y);
		bool flag3 = flag || flag2;
		if (Player.m_localPlayer && Player.m_localPlayer.InInterior())
		{
			this.m_dirLight.renderMode = LightRenderMode.ForceVertex;
		}
		else
		{
			this.m_dirLight.renderMode = LightRenderMode.ForcePixel;
		}
		if (this.m_environmentPeriod != num || this.m_currentBiome != biome || flag3 != this.m_inAshlandsOrDeepnorth)
		{
			this.m_environmentPeriod = num;
			this.m_currentBiome = biome;
			this.m_inAshlandsOrDeepnorth = flag3;
			UnityEngine.Random.State state = UnityEngine.Random.state;
			UnityEngine.Random.InitState((int)num);
			List<EnvEntry> availableEnvironments = this.GetAvailableEnvironments(biome);
			if (availableEnvironments != null && availableEnvironments.Count > 0)
			{
				EnvSetup envSetup = this.SelectWeightedEnvironment(availableEnvironments);
				foreach (EnvEntry envEntry in availableEnvironments)
				{
					if (envEntry.m_ashlandsOverride && flag)
					{
						envSetup = envEntry.m_env;
					}
					if (envEntry.m_deepnorthOverride && flag2)
					{
						envSetup = envEntry.m_env;
					}
				}
				if (envSetup != null)
				{
					this.QueueEnvironment(envSetup);
				}
			}
			UnityEngine.Random.state = state;
		}
	}

	// Token: 0x0600115F RID: 4447 RVA: 0x000817C0 File Offset: 0x0007F9C0
	private BiomeEnvSetup GetBiomeEnvSetup(Heightmap.Biome biome)
	{
		foreach (BiomeEnvSetup biomeEnvSetup in this.m_biomes)
		{
			if (biomeEnvSetup.m_biome == biome)
			{
				return biomeEnvSetup;
			}
		}
		return null;
	}

	// Token: 0x06001160 RID: 4448 RVA: 0x0008181C File Offset: 0x0007FA1C
	private List<EnvEntry> GetAvailableEnvironments(Heightmap.Biome biome)
	{
		BiomeEnvSetup biomeEnvSetup = this.GetBiomeEnvSetup(biome);
		if (biomeEnvSetup != null)
		{
			return biomeEnvSetup.m_environments;
		}
		return null;
	}

	// Token: 0x06001161 RID: 4449 RVA: 0x0008183C File Offset: 0x0007FA3C
	private Heightmap.Biome GetBiome()
	{
		Camera mainCamera = Utils.GetMainCamera();
		if (mainCamera == null)
		{
			return Heightmap.Biome.None;
		}
		Vector3 position = mainCamera.transform.position;
		if (this.m_cachedHeightmap == null || !this.m_cachedHeightmap.IsPointInside(position, 0f))
		{
			this.m_cachedHeightmap = Heightmap.FindHeightmap(position);
		}
		if (this.m_cachedHeightmap)
		{
			bool flag = WorldGenerator.IsAshlands(position.x, position.z);
			bool flag2 = WorldGenerator.IsDeepnorth(position.x, position.y);
			return this.m_cachedHeightmap.GetBiome(position, this.m_oceanLevelEnvCheckAshlandsDeepnorth, flag || flag2);
		}
		return Heightmap.Biome.None;
	}

	// Token: 0x06001162 RID: 4450 RVA: 0x000818DC File Offset: 0x0007FADC
	private void InterpolateEnvironment(float dt)
	{
		if (this.m_nextEnv != null)
		{
			this.m_transitionTimer += dt;
			float num = Mathf.Clamp01(this.m_transitionTimer / this.m_transitionDuration);
			this.m_currentEnv = this.InterpolateEnvironment(this.m_prevEnv, this.m_nextEnv, num);
			if (num >= 1f)
			{
				this.m_currentEnv = this.m_nextEnv;
				this.m_prevEnv = null;
				this.m_nextEnv = null;
			}
		}
	}

	// Token: 0x06001163 RID: 4451 RVA: 0x00081950 File Offset: 0x0007FB50
	private void QueueEnvironment(string name)
	{
		if (this.m_currentEnv.m_name == name)
		{
			return;
		}
		if (this.m_nextEnv != null && this.m_nextEnv.m_name == name)
		{
			return;
		}
		EnvSetup env = this.GetEnv(name);
		if (env != null)
		{
			this.QueueEnvironment(env);
		}
	}

	// Token: 0x06001164 RID: 4452 RVA: 0x000819A0 File Offset: 0x0007FBA0
	private void QueueEnvironment(EnvSetup env)
	{
		if (Terminal.m_showTests)
		{
			Terminal.Log(string.Format("Queuing environment: {0} (biome: {1})", env.m_name, this.m_currentBiome));
			Terminal.m_testList["Env"] = string.Format("{0} (biome: {1})", env.m_name, this.m_currentBiome);
		}
		if (this.m_firstEnv)
		{
			this.m_firstEnv = false;
			this.m_currentEnv = env;
			return;
		}
		this.m_prevEnv = this.m_currentEnv.Clone();
		this.m_nextEnv = env;
		this.m_transitionTimer = 0f;
	}

	// Token: 0x06001165 RID: 4453 RVA: 0x00081A38 File Offset: 0x0007FC38
	private EnvSetup InterpolateEnvironment(EnvSetup a, EnvSetup b, float i)
	{
		EnvSetup envSetup = a.Clone();
		envSetup.m_name = b.m_name;
		if (i >= 0.5f)
		{
			envSetup.m_isFreezingAtNight = b.m_isFreezingAtNight;
			envSetup.m_isFreezing = b.m_isFreezing;
			envSetup.m_isCold = b.m_isCold;
			envSetup.m_isColdAtNight = b.m_isColdAtNight;
			envSetup.m_isColdAtNight = b.m_isColdAtNight;
		}
		envSetup.m_ambColorDay = Color.Lerp(a.m_ambColorDay, b.m_ambColorDay, i);
		envSetup.m_ambColorNight = Color.Lerp(a.m_ambColorNight, b.m_ambColorNight, i);
		envSetup.m_fogColorDay = Color.Lerp(a.m_fogColorDay, b.m_fogColorDay, i);
		envSetup.m_fogColorEvening = Color.Lerp(a.m_fogColorEvening, b.m_fogColorEvening, i);
		envSetup.m_fogColorMorning = Color.Lerp(a.m_fogColorMorning, b.m_fogColorMorning, i);
		envSetup.m_fogColorNight = Color.Lerp(a.m_fogColorNight, b.m_fogColorNight, i);
		envSetup.m_fogColorSunDay = Color.Lerp(a.m_fogColorSunDay, b.m_fogColorSunDay, i);
		envSetup.m_fogColorSunEvening = Color.Lerp(a.m_fogColorSunEvening, b.m_fogColorSunEvening, i);
		envSetup.m_fogColorSunMorning = Color.Lerp(a.m_fogColorSunMorning, b.m_fogColorSunMorning, i);
		envSetup.m_fogColorSunNight = Color.Lerp(a.m_fogColorSunNight, b.m_fogColorSunNight, i);
		envSetup.m_fogDensityDay = Mathf.Lerp(a.m_fogDensityDay, b.m_fogDensityDay, i);
		envSetup.m_fogDensityEvening = Mathf.Lerp(a.m_fogDensityEvening, b.m_fogDensityEvening, i);
		envSetup.m_fogDensityMorning = Mathf.Lerp(a.m_fogDensityMorning, b.m_fogDensityMorning, i);
		envSetup.m_fogDensityNight = Mathf.Lerp(a.m_fogDensityNight, b.m_fogDensityNight, i);
		envSetup.m_sunColorDay = Color.Lerp(a.m_sunColorDay, b.m_sunColorDay, i);
		envSetup.m_sunColorEvening = Color.Lerp(a.m_sunColorEvening, b.m_sunColorEvening, i);
		envSetup.m_sunColorMorning = Color.Lerp(a.m_sunColorMorning, b.m_sunColorMorning, i);
		envSetup.m_sunColorNight = Color.Lerp(a.m_sunColorNight, b.m_sunColorNight, i);
		envSetup.m_lightIntensityDay = Mathf.Lerp(a.m_lightIntensityDay, b.m_lightIntensityDay, i);
		envSetup.m_lightIntensityNight = Mathf.Lerp(a.m_lightIntensityNight, b.m_lightIntensityNight, i);
		envSetup.m_sunAngle = Mathf.Lerp(a.m_sunAngle, b.m_sunAngle, i);
		envSetup.m_windMin = Mathf.Lerp(a.m_windMin, b.m_windMin, i);
		envSetup.m_windMax = Mathf.Lerp(a.m_windMax, b.m_windMax, i);
		envSetup.m_rainCloudAlpha = Mathf.Lerp(a.m_rainCloudAlpha, b.m_rainCloudAlpha, i);
		envSetup.m_ambientLoop = ((i > 0.75f) ? b.m_ambientLoop : a.m_ambientLoop);
		envSetup.m_ambientVol = ((i > 0.75f) ? b.m_ambientVol : a.m_ambientVol);
		envSetup.m_musicEvening = b.m_musicEvening;
		envSetup.m_musicMorning = b.m_musicMorning;
		envSetup.m_musicDay = b.m_musicDay;
		envSetup.m_musicNight = b.m_musicNight;
		return envSetup;
	}

	// Token: 0x06001166 RID: 4454 RVA: 0x00081D48 File Offset: 0x0007FF48
	private void SetEnv(EnvSetup env, float dayInt, float nightInt, float morningInt, float eveningInt, float dt)
	{
		Camera mainCamera = Utils.GetMainCamera();
		if (mainCamera == null)
		{
			return;
		}
		this.m_dirLight.transform.rotation = Quaternion.Euler(-90f + env.m_sunAngle, 0f, 0f) * Quaternion.Euler(0f, -90f, 0f) * Quaternion.Euler(-90f + 360f * this.m_smoothDayFraction, 0f, 0f);
		Vector3 v = -this.m_dirLight.transform.forward;
		this.m_dirLight.intensity = env.m_lightIntensityDay * dayInt;
		this.m_dirLight.intensity += env.m_lightIntensityNight * nightInt;
		if (nightInt > 0f)
		{
			this.m_dirLight.transform.rotation = this.m_dirLight.transform.rotation * Quaternion.Euler(180f, 0f, 0f);
		}
		this.m_dirLight.transform.position = mainCamera.transform.position - this.m_dirLight.transform.forward * 3000f;
		this.m_dirLight.color = new Color(0f, 0f, 0f, 0f);
		this.m_dirLight.color += env.m_sunColorNight * nightInt;
		if (dayInt > 0f)
		{
			this.m_dirLight.color += env.m_sunColorDay * dayInt;
			this.m_dirLight.color += env.m_sunColorMorning * morningInt;
			this.m_dirLight.color += env.m_sunColorEvening * eveningInt;
		}
		RenderSettings.fogColor = new Color(0f, 0f, 0f, 0f);
		RenderSettings.fogColor += env.m_fogColorNight * nightInt;
		RenderSettings.fogColor += env.m_fogColorDay * dayInt;
		RenderSettings.fogColor += env.m_fogColorMorning * morningInt;
		RenderSettings.fogColor += env.m_fogColorEvening * eveningInt;
		this.m_sunFogColor = new Color(0f, 0f, 0f, 0f);
		this.m_sunFogColor += env.m_fogColorSunNight * nightInt;
		if (dayInt > 0f)
		{
			this.m_sunFogColor += env.m_fogColorSunDay * dayInt;
			this.m_sunFogColor += env.m_fogColorSunMorning * morningInt;
			this.m_sunFogColor += env.m_fogColorSunEvening * eveningInt;
		}
		this.m_sunFogColor = Color.Lerp(RenderSettings.fogColor, this.m_sunFogColor, Mathf.Clamp01(Mathf.Max(nightInt, dayInt) * 3f));
		RenderSettings.fogDensity = 0f;
		RenderSettings.fogDensity += env.m_fogDensityNight * nightInt;
		RenderSettings.fogDensity += env.m_fogDensityDay * dayInt;
		RenderSettings.fogDensity += env.m_fogDensityMorning * morningInt;
		RenderSettings.fogDensity += env.m_fogDensityEvening * eveningInt;
		RenderSettings.ambientMode = AmbientMode.Flat;
		RenderSettings.ambientLight = Color.Lerp(env.m_ambColorNight, env.m_ambColorDay, dayInt);
		SunShafts component = mainCamera.GetComponent<SunShafts>();
		if (component)
		{
			component.sunColor = this.m_dirLight.color;
		}
		if (env.m_envObject != this.m_currentEnvObject)
		{
			if (this.m_currentEnvObject)
			{
				this.m_currentEnvObject.SetActive(false);
				this.m_currentEnvObject = null;
			}
			if (env.m_envObject)
			{
				this.m_currentEnvObject = env.m_envObject;
				this.m_currentEnvObject.SetActive(true);
			}
		}
		if (env.m_psystems != this.m_currentPSystems)
		{
			if (this.m_currentPSystems != null)
			{
				this.SetParticleArrayEnabled(this.m_currentPSystems, false);
				this.m_currentPSystems = null;
			}
			if (env.m_psystems != null && (!env.m_psystemsOutsideOnly || (Player.m_localPlayer && !Player.m_localPlayer.InShelter())))
			{
				this.SetParticleArrayEnabled(env.m_psystems, true);
				this.m_currentPSystems = env.m_psystems;
			}
		}
		this.m_clouds.material.SetFloat(EnvMan.s_rain, env.m_rainCloudAlpha);
		if (env.m_ambientLoop)
		{
			AudioMan.instance.QueueAmbientLoop(env.m_ambientLoop, env.m_ambientVol);
		}
		else
		{
			AudioMan.instance.StopAmbientLoop();
		}
		Shader.SetGlobalVector(EnvMan.s_skyboxSunDir, v);
		Shader.SetGlobalVector(EnvMan.s_skyboxSunDir, v);
		Shader.SetGlobalVector(EnvMan.s_sunDir, -this.m_dirLight.transform.forward);
		Shader.SetGlobalColor(EnvMan.s_sunFogColor, this.m_sunFogColor);
		Shader.SetGlobalColor(EnvMan.s_sunColor, this.m_dirLight.color * this.m_dirLight.intensity);
		Shader.SetGlobalColor(EnvMan.s_ambientColor, RenderSettings.ambientLight);
		float num = Shader.GetGlobalFloat(EnvMan.s_wet);
		num = Mathf.MoveTowards(num, env.m_isWet ? 1f : 0f, dt / this.m_wetTransitionDuration);
		Shader.SetGlobalFloat(EnvMan.s_wet, num);
	}

	// Token: 0x06001167 RID: 4455 RVA: 0x000822F8 File Offset: 0x000804F8
	public float GetDayFraction()
	{
		return this.m_smoothDayFraction;
	}

	// Token: 0x06001168 RID: 4456 RVA: 0x00082300 File Offset: 0x00080500
	public int GetDay()
	{
		return this.GetDay(ZNet.instance.GetTimeSeconds());
	}

	// Token: 0x06001169 RID: 4457 RVA: 0x00082312 File Offset: 0x00080512
	public int GetDay(double time)
	{
		return (int)(time / (double)this.m_dayLengthSec);
	}

	// Token: 0x0600116A RID: 4458 RVA: 0x0008231E File Offset: 0x0008051E
	public double GetMorningStartSec(int day)
	{
		return (double)((float)((long)day * this.m_dayLengthSec) + (float)this.m_dayLengthSec * 0.15f);
	}

	// Token: 0x0600116B RID: 4459 RVA: 0x0008233C File Offset: 0x0008053C
	private void UpdateTimeSkip(float dt)
	{
		if (!ZNet.instance.IsServer())
		{
			return;
		}
		if (this.m_skipTime)
		{
			double num = ZNet.instance.GetTimeSeconds();
			num += (double)dt * this.m_timeSkipSpeed;
			if (num >= this.m_skipToTime)
			{
				num = this.m_skipToTime;
				this.m_skipTime = false;
			}
			ZNet.instance.SetNetTime(num);
		}
	}

	// Token: 0x0600116C RID: 4460 RVA: 0x00082397 File Offset: 0x00080597
	public bool IsTimeSkipping()
	{
		return this.m_skipTime;
	}

	// Token: 0x0600116D RID: 4461 RVA: 0x000823A0 File Offset: 0x000805A0
	public void SkipToMorning()
	{
		double timeSeconds = ZNet.instance.GetTimeSeconds();
		double time = timeSeconds - (double)((float)this.m_dayLengthSec * 0.15f);
		int day = this.GetDay(time);
		double morningStartSec = this.GetMorningStartSec(day + 1);
		this.m_skipTime = true;
		this.m_skipToTime = morningStartSec;
		double num = morningStartSec - timeSeconds;
		this.m_timeSkipSpeed = num / 12.0;
		ZLog.Log(string.Concat(new string[]
		{
			"Time ",
			timeSeconds.ToString(),
			", day:",
			day.ToString(),
			"    nextm:",
			morningStartSec.ToString(),
			"  skipspeed:",
			this.m_timeSkipSpeed.ToString()
		}));
	}

	// Token: 0x0600116E RID: 4462 RVA: 0x0008245C File Offset: 0x0008065C
	public static bool IsFreezing()
	{
		return EnvMan.s_isFreezing;
	}

	// Token: 0x0600116F RID: 4463 RVA: 0x00082463 File Offset: 0x00080663
	public static bool IsCold()
	{
		return EnvMan.s_isCold;
	}

	// Token: 0x06001170 RID: 4464 RVA: 0x0008246A File Offset: 0x0008066A
	public static bool IsWet()
	{
		return EnvMan.s_isWet;
	}

	// Token: 0x06001171 RID: 4465 RVA: 0x00082471 File Offset: 0x00080671
	public static bool CanSleep()
	{
		return EnvMan.s_canSleep;
	}

	// Token: 0x06001172 RID: 4466 RVA: 0x00082478 File Offset: 0x00080678
	public static bool IsDay()
	{
		return EnvMan.s_isDay;
	}

	// Token: 0x06001173 RID: 4467 RVA: 0x0008247F File Offset: 0x0008067F
	public static bool IsAfternoon()
	{
		return EnvMan.s_isAfternoon;
	}

	// Token: 0x06001174 RID: 4468 RVA: 0x00082486 File Offset: 0x00080686
	public static bool IsNight()
	{
		return EnvMan.s_isNight;
	}

	// Token: 0x06001175 RID: 4469 RVA: 0x0008248D File Offset: 0x0008068D
	public static bool IsDaylight()
	{
		return EnvMan.s_isDaylight;
	}

	// Token: 0x06001176 RID: 4470 RVA: 0x00082494 File Offset: 0x00080694
	private bool CalculateFreezing()
	{
		EnvSetup currentEnvironment = this.GetCurrentEnvironment();
		return currentEnvironment != null && (currentEnvironment.m_isFreezing || (currentEnvironment.m_isFreezingAtNight && !EnvMan.IsDay()));
	}

	// Token: 0x06001177 RID: 4471 RVA: 0x000824CC File Offset: 0x000806CC
	private bool CalculateCold()
	{
		EnvSetup currentEnvironment = this.GetCurrentEnvironment();
		return currentEnvironment != null && (currentEnvironment.m_isCold || (currentEnvironment.m_isColdAtNight && !EnvMan.IsDay()));
	}

	// Token: 0x06001178 RID: 4472 RVA: 0x00082504 File Offset: 0x00080704
	private bool CalculateWet()
	{
		EnvSetup currentEnvironment = this.GetCurrentEnvironment();
		return currentEnvironment != null && currentEnvironment.m_isWet;
	}

	// Token: 0x06001179 RID: 4473 RVA: 0x00082523 File Offset: 0x00080723
	private bool CalculateCanSleep()
	{
		return (EnvMan.IsAfternoon() || EnvMan.IsNight()) && (Player.m_localPlayer == null || ZNet.instance.GetTimeSeconds() > Player.m_localPlayer.m_wakeupTime + this.m_sleepCooldownSeconds);
	}

	// Token: 0x0600117A RID: 4474 RVA: 0x00082564 File Offset: 0x00080764
	private bool CalculateDay()
	{
		float dayFraction = this.GetDayFraction();
		return dayFraction >= 0.25f && dayFraction <= 0.75f;
	}

	// Token: 0x0600117B RID: 4475 RVA: 0x00082590 File Offset: 0x00080790
	private bool CalculateAfternoon()
	{
		float dayFraction = this.GetDayFraction();
		return dayFraction >= 0.5f && dayFraction <= 0.75f;
	}

	// Token: 0x0600117C RID: 4476 RVA: 0x000825BC File Offset: 0x000807BC
	private bool CalculateNight()
	{
		float dayFraction = this.GetDayFraction();
		return dayFraction <= 0.25f || dayFraction >= 0.75f;
	}

	// Token: 0x0600117D RID: 4477 RVA: 0x000825E8 File Offset: 0x000807E8
	private bool CalculateDaylight()
	{
		EnvSetup currentEnvironment = this.GetCurrentEnvironment();
		return (currentEnvironment == null || !currentEnvironment.m_alwaysDark) && EnvMan.IsDay();
	}

	// Token: 0x0600117E RID: 4478 RVA: 0x0008260E File Offset: 0x0008080E
	public Heightmap.Biome GetCurrentBiome()
	{
		return this.m_currentBiome;
	}

	// Token: 0x0600117F RID: 4479 RVA: 0x00082616 File Offset: 0x00080816
	public bool IsEnvironment(string name)
	{
		return this.GetCurrentEnvironment().m_name == name;
	}

	// Token: 0x06001180 RID: 4480 RVA: 0x0008262C File Offset: 0x0008082C
	public bool IsEnvironment(List<string> names)
	{
		EnvSetup currentEnvironment = this.GetCurrentEnvironment();
		return names.Contains(currentEnvironment.m_name);
	}

	// Token: 0x06001181 RID: 4481 RVA: 0x0008264C File Offset: 0x0008084C
	public EnvSetup GetCurrentEnvironment()
	{
		if (!string.IsNullOrEmpty(this.m_forceEnv))
		{
			EnvSetup env = this.GetEnv(this.m_forceEnv);
			if (env != null)
			{
				return env;
			}
		}
		return this.m_currentEnv;
	}

	// Token: 0x06001182 RID: 4482 RVA: 0x0008267E File Offset: 0x0008087E
	public Color GetSunFogColor()
	{
		return this.m_sunFogColor;
	}

	// Token: 0x06001183 RID: 4483 RVA: 0x00082686 File Offset: 0x00080886
	public Vector3 GetSunDirection()
	{
		return this.m_dirLight.transform.forward;
	}

	// Token: 0x06001184 RID: 4484 RVA: 0x00082698 File Offset: 0x00080898
	private EnvSetup GetEnv(string name)
	{
		foreach (EnvSetup envSetup in this.m_environments)
		{
			if (envSetup.m_name == name)
			{
				return envSetup;
			}
		}
		return null;
	}

	// Token: 0x06001185 RID: 4485 RVA: 0x000826FC File Offset: 0x000808FC
	private EnvSetup GetDefaultEnv()
	{
		foreach (EnvSetup envSetup in this.m_environments)
		{
			if (envSetup.m_default)
			{
				return envSetup;
			}
		}
		return null;
	}

	// Token: 0x06001186 RID: 4486 RVA: 0x00082758 File Offset: 0x00080958
	public void SetDebugWind(float angle, float intensity)
	{
		this.m_debugWind = true;
		this.m_debugWindAngle = angle;
		this.m_debugWindIntensity = Mathf.Clamp01(intensity);
	}

	// Token: 0x06001187 RID: 4487 RVA: 0x00082774 File Offset: 0x00080974
	public void ResetDebugWind()
	{
		this.m_debugWind = false;
	}

	// Token: 0x06001188 RID: 4488 RVA: 0x0008277D File Offset: 0x0008097D
	public Vector3 GetWindForce()
	{
		return this.GetWindDir() * this.m_wind.w;
	}

	// Token: 0x06001189 RID: 4489 RVA: 0x00082795 File Offset: 0x00080995
	public Vector3 GetWindDir()
	{
		return new Vector3(this.m_wind.x, this.m_wind.y, this.m_wind.z);
	}

	// Token: 0x0600118A RID: 4490 RVA: 0x000827BD File Offset: 0x000809BD
	public float GetWindIntensity()
	{
		return this.m_wind.w;
	}

	// Token: 0x0600118B RID: 4491 RVA: 0x000827CC File Offset: 0x000809CC
	private void UpdateWind(long timeSec, float dt)
	{
		if (this.m_debugWind)
		{
			float f = 0.017453292f * this.m_debugWindAngle;
			Vector3 dir = new Vector3(Mathf.Sin(f), 0f, Mathf.Cos(f));
			this.SetTargetWind(dir, this.m_debugWindIntensity);
		}
		else
		{
			EnvSetup currentEnvironment = this.GetCurrentEnvironment();
			if (currentEnvironment != null)
			{
				UnityEngine.Random.State state = UnityEngine.Random.state;
				float f2 = 0f;
				float num = 0.5f;
				this.AddWindOctave(timeSec, 1, ref f2, ref num);
				this.AddWindOctave(timeSec, 2, ref f2, ref num);
				this.AddWindOctave(timeSec, 4, ref f2, ref num);
				this.AddWindOctave(timeSec, 8, ref f2, ref num);
				UnityEngine.Random.state = state;
				Vector3 dir2 = new Vector3(Mathf.Sin(f2), 0f, Mathf.Cos(f2));
				num = Mathf.Lerp(currentEnvironment.m_windMin, currentEnvironment.m_windMax, num);
				if (Player.m_localPlayer && !Player.m_localPlayer.InInterior())
				{
					float num2 = Utils.LengthXZ(Player.m_localPlayer.transform.position);
					if (num2 > 10500f - this.m_edgeOfWorldWidth)
					{
						float num3 = Utils.LerpStep(10500f - this.m_edgeOfWorldWidth, 10500f, num2);
						num3 = 1f - Mathf.Pow(1f - num3, 2f);
						dir2 = Player.m_localPlayer.transform.position.normalized;
						num = Mathf.Lerp(num, 1f, num3);
					}
					else
					{
						Ship localShip = Ship.GetLocalShip();
						if (localShip && localShip.IsWindControllActive())
						{
							dir2 = localShip.transform.forward;
						}
					}
				}
				this.SetTargetWind(dir2, num);
			}
		}
		this.UpdateWindTransition(dt);
	}

	// Token: 0x0600118C RID: 4492 RVA: 0x00082975 File Offset: 0x00080B75
	private void AddWindOctave(long timeSec, int octave, ref float angle, ref float intensity)
	{
		UnityEngine.Random.InitState((int)(timeSec / (this.m_windPeriodDuration / (long)octave)));
		angle += UnityEngine.Random.value * (6.2831855f / (float)octave);
		intensity += -(0.5f / (float)octave) + UnityEngine.Random.value / (float)octave;
	}

	// Token: 0x0600118D RID: 4493 RVA: 0x000829B4 File Offset: 0x00080BB4
	private void SetTargetWind(Vector3 dir, float intensity)
	{
		if (this.m_windTransitionTimer >= 0f)
		{
			return;
		}
		intensity = Mathf.Clamp(intensity, 0.05f, 1f);
		if (Mathf.Approximately(dir.x, this.m_windDir1.x) && Mathf.Approximately(dir.y, this.m_windDir1.y) && Mathf.Approximately(dir.z, this.m_windDir1.z) && Mathf.Approximately(intensity, this.m_windDir1.w))
		{
			return;
		}
		this.m_windTransitionTimer = 0f;
		this.m_windDir2 = new Vector4(dir.x, dir.y, dir.z, intensity);
	}

	// Token: 0x0600118E RID: 4494 RVA: 0x00082A68 File Offset: 0x00080C68
	private void UpdateWindTransition(float dt)
	{
		if (this.m_windTransitionTimer >= 0f)
		{
			this.m_windTransitionTimer += dt;
			float num = Mathf.Clamp01(this.m_windTransitionTimer / this.m_windTransitionDuration);
			Shader.SetGlobalVector(EnvMan.s_globalWind1, this.m_windDir1);
			Shader.SetGlobalVector(EnvMan.s_globalWind2, this.m_windDir2);
			Shader.SetGlobalFloat(EnvMan.s_globalWindAlpha, num);
			this.m_wind = Vector4.Lerp(this.m_windDir1, this.m_windDir2, num);
			if (num >= 1f)
			{
				this.m_windDir1 = this.m_windDir2;
				this.m_windTransitionTimer = -1f;
			}
		}
		else
		{
			Shader.SetGlobalVector(EnvMan.s_globalWind1, this.m_windDir1);
			Shader.SetGlobalFloat(EnvMan.s_globalWindAlpha, 0f);
			this.m_wind = this.m_windDir1;
		}
		Shader.SetGlobalVector(EnvMan.s_globalWindForce, this.GetWindForce());
	}

	// Token: 0x0600118F RID: 4495 RVA: 0x00082B4C File Offset: 0x00080D4C
	public void GetWindData(out Vector4 wind1, out Vector4 wind2, out float alpha)
	{
		wind1 = this.m_windDir1;
		wind2 = this.m_windDir2;
		if (this.m_windTransitionTimer >= 0f)
		{
			alpha = Mathf.Clamp01(this.m_windTransitionTimer / this.m_windTransitionDuration);
			return;
		}
		alpha = 0f;
	}

	// Token: 0x06001190 RID: 4496 RVA: 0x00082B9C File Offset: 0x00080D9C
	public void AppendEnvironment(EnvSetup env)
	{
		EnvSetup env2 = this.GetEnv(env.m_name);
		if (env2 != null)
		{
			ZLog.LogError("Environment with name " + env.m_name + " is defined multiple times and will be overwritten! Check locationlists & gamemain.");
			this.m_environments.Remove(env2);
		}
		this.m_environments.Add(env);
		this.InitializeEnvironment(env);
	}

	// Token: 0x06001191 RID: 4497 RVA: 0x00082BF4 File Offset: 0x00080DF4
	public void AppendBiomeSetup(BiomeEnvSetup biomeEnv)
	{
		BiomeEnvSetup biomeEnvSetup = this.GetBiomeEnvSetup(biomeEnv.m_biome);
		if (biomeEnvSetup != null)
		{
			biomeEnvSetup.m_environments.AddRange(biomeEnv.m_environments);
			if (!string.IsNullOrEmpty(biomeEnv.m_musicDay) || !string.IsNullOrEmpty(biomeEnv.m_musicEvening) || !string.IsNullOrEmpty(biomeEnv.m_musicMorning) || !string.IsNullOrEmpty(biomeEnv.m_musicNight))
			{
				ZLog.LogError(string.Concat(new string[]
				{
					"EnvSetup ",
					biomeEnv.m_name,
					" sets music, but is already defined previously in ",
					biomeEnvSetup.m_name,
					", only settings from first loaded envsetup per biome will be used!"
				}));
			}
		}
		this.m_biomes.Add(biomeEnv);
		this.InitializeBiomeEnvSetup(biomeEnv);
	}

	// Token: 0x06001192 RID: 4498 RVA: 0x00082CA4 File Offset: 0x00080EA4
	public bool CheckInteriorBuildingOverride()
	{
		string b = this.GetCurrentEnvironment().m_name.ToLower();
		using (List<string>.Enumerator enumerator = this.m_interiorBuildingOverrideEnvironments.GetEnumerator())
		{
			while (enumerator.MoveNext())
			{
				if (enumerator.Current.ToLower() == b)
				{
					return true;
				}
			}
		}
		return false;
	}

	// Token: 0x040010A3 RID: 4259
	private static int s_lastFrame = int.MaxValue;

	// Token: 0x040010A4 RID: 4260
	private static EnvMan s_instance;

	// Token: 0x040010A5 RID: 4261
	private static bool s_isDay;

	// Token: 0x040010A6 RID: 4262
	private static bool s_isDaylight;

	// Token: 0x040010A7 RID: 4263
	private static bool s_isAfternoon;

	// Token: 0x040010A8 RID: 4264
	private static bool s_isCold;

	// Token: 0x040010A9 RID: 4265
	private static bool s_isFreezing;

	// Token: 0x040010AA RID: 4266
	private static bool s_isNight;

	// Token: 0x040010AB RID: 4267
	private static bool s_isWet;

	// Token: 0x040010AC RID: 4268
	private static bool s_canSleep;

	// Token: 0x040010AD RID: 4269
	public Light m_dirLight;

	// Token: 0x040010AE RID: 4270
	public bool m_debugTimeOfDay;

	// Token: 0x040010AF RID: 4271
	[Range(0f, 1f)]
	public float m_debugTime = 0.5f;

	// Token: 0x040010B0 RID: 4272
	public string m_debugEnv = "";

	// Token: 0x040010B1 RID: 4273
	public bool m_debugWind;

	// Token: 0x040010B2 RID: 4274
	[Range(0f, 360f)]
	public float m_debugWindAngle;

	// Token: 0x040010B3 RID: 4275
	[Range(0f, 1f)]
	public float m_debugWindIntensity = 1f;

	// Token: 0x040010B4 RID: 4276
	public float m_sunHorizonTransitionH = 0.08f;

	// Token: 0x040010B5 RID: 4277
	public float m_sunHorizonTransitionL = 0.02f;

	// Token: 0x040010B6 RID: 4278
	public long m_dayLengthSec = 1200L;

	// Token: 0x040010B7 RID: 4279
	public float m_transitionDuration = 2f;

	// Token: 0x040010B8 RID: 4280
	public long m_environmentDuration = 20L;

	// Token: 0x040010B9 RID: 4281
	public long m_windPeriodDuration = 10L;

	// Token: 0x040010BA RID: 4282
	public float m_windTransitionDuration = 5f;

	// Token: 0x040010BB RID: 4283
	public List<EnvSetup> m_environments = new List<EnvSetup>();

	// Token: 0x040010BC RID: 4284
	public List<string> m_interiorBuildingOverrideEnvironments = new List<string>();

	// Token: 0x040010BD RID: 4285
	public List<BiomeEnvSetup> m_biomes = new List<BiomeEnvSetup>();

	// Token: 0x040010BE RID: 4286
	public string m_introEnvironment = "ThunderStorm";

	// Token: 0x040010BF RID: 4287
	public float m_edgeOfWorldWidth = 500f;

	// Token: 0x040010C0 RID: 4288
	[Header("Music")]
	public float m_randomMusicIntervalMin = 60f;

	// Token: 0x040010C1 RID: 4289
	public float m_randomMusicIntervalMax = 200f;

	// Token: 0x040010C2 RID: 4290
	[Header("Other")]
	public MeshRenderer m_clouds;

	// Token: 0x040010C3 RID: 4291
	public MeshRenderer m_rainClouds;

	// Token: 0x040010C4 RID: 4292
	public MeshRenderer m_rainCloudsDownside;

	// Token: 0x040010C5 RID: 4293
	public float m_wetTransitionDuration = 15f;

	// Token: 0x040010C6 RID: 4294
	public double m_sleepCooldownSeconds = 30.0;

	// Token: 0x040010C7 RID: 4295
	public float m_oceanLevelEnvCheckAshlandsDeepnorth = 20f;

	// Token: 0x040010C8 RID: 4296
	private bool m_skipTime;

	// Token: 0x040010C9 RID: 4297
	private double m_skipToTime;

	// Token: 0x040010CA RID: 4298
	private double m_timeSkipSpeed = 1.0;

	// Token: 0x040010CB RID: 4299
	private const double c_TimeSkipDuration = 12.0;

	// Token: 0x040010CC RID: 4300
	private double m_totalSeconds;

	// Token: 0x040010CD RID: 4301
	private float m_smoothDayFraction;

	// Token: 0x040010CE RID: 4302
	private Color m_sunFogColor = Color.white;

	// Token: 0x040010CF RID: 4303
	private GameObject[] m_currentPSystems;

	// Token: 0x040010D0 RID: 4304
	private GameObject m_currentEnvObject;

	// Token: 0x040010D1 RID: 4305
	private const float c_MorningL = 0.15f;

	// Token: 0x040010D2 RID: 4306
	private Vector4 m_windDir1 = new Vector4(0f, 0f, -1f, 0f);

	// Token: 0x040010D3 RID: 4307
	private Vector4 m_windDir2 = new Vector4(0f, 0f, -1f, 0f);

	// Token: 0x040010D4 RID: 4308
	private Vector4 m_wind = new Vector4(0f, 0f, -1f, 0f);

	// Token: 0x040010D5 RID: 4309
	private float m_windTransitionTimer = -1f;

	// Token: 0x040010D6 RID: 4310
	private Vector3 m_cloudOffset = Vector3.zero;

	// Token: 0x040010D7 RID: 4311
	private string m_forceEnv = "";

	// Token: 0x040010D8 RID: 4312
	private EnvSetup m_currentEnv;

	// Token: 0x040010D9 RID: 4313
	private EnvSetup m_prevEnv;

	// Token: 0x040010DA RID: 4314
	private EnvSetup m_nextEnv;

	// Token: 0x040010DB RID: 4315
	private string m_ambientMusic;

	// Token: 0x040010DC RID: 4316
	private float m_ambientMusicTimer;

	// Token: 0x040010DD RID: 4317
	private Heightmap m_cachedHeightmap;

	// Token: 0x040010DE RID: 4318
	private Heightmap.Biome m_currentBiome;

	// Token: 0x040010DF RID: 4319
	private bool m_inAshlandsOrDeepnorth;

	// Token: 0x040010E0 RID: 4320
	private long m_environmentPeriod;

	// Token: 0x040010E1 RID: 4321
	private float m_transitionTimer;

	// Token: 0x040010E2 RID: 4322
	private bool m_firstEnv = true;

	// Token: 0x040010E3 RID: 4323
	private static readonly int s_netRefPos = Shader.PropertyToID("_NetRefPos");

	// Token: 0x040010E4 RID: 4324
	private static readonly int s_skyboxSunDir = Shader.PropertyToID("_SkyboxSunDir");

	// Token: 0x040010E5 RID: 4325
	private static readonly int s_sunDir = Shader.PropertyToID("_SunDir");

	// Token: 0x040010E6 RID: 4326
	private static readonly int s_sunFogColor = Shader.PropertyToID("_SunFogColor");

	// Token: 0x040010E7 RID: 4327
	private static readonly int s_wet = Shader.PropertyToID("_Wet");

	// Token: 0x040010E8 RID: 4328
	private static readonly int s_sunColor = Shader.PropertyToID("_SunColor");

	// Token: 0x040010E9 RID: 4329
	private static readonly int s_ambientColor = Shader.PropertyToID("_AmbientColor");

	// Token: 0x040010EA RID: 4330
	private static readonly int s_globalWind1 = Shader.PropertyToID("_GlobalWind1");

	// Token: 0x040010EB RID: 4331
	private static readonly int s_globalWind2 = Shader.PropertyToID("_GlobalWind2");

	// Token: 0x040010EC RID: 4332
	private static readonly int s_globalWindAlpha = Shader.PropertyToID("_GlobalWindAlpha");

	// Token: 0x040010ED RID: 4333
	private static readonly int s_cloudOffset = Shader.PropertyToID("_CloudOffset");

	// Token: 0x040010EE RID: 4334
	private static readonly int s_globalWindForce = Shader.PropertyToID("_GlobalWindForce");

	// Token: 0x040010EF RID: 4335
	private static readonly int s_rain = Shader.PropertyToID("_Rain");
}
