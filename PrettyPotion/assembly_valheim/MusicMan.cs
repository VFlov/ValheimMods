using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

// Token: 0x02000129 RID: 297
public class MusicMan : MonoBehaviour
{
	// Token: 0x170000A2 RID: 162
	// (get) Token: 0x060012B5 RID: 4789 RVA: 0x0008B4F5 File Offset: 0x000896F5
	public static MusicMan instance
	{
		get
		{
			return MusicMan.m_instance;
		}
	}

	// Token: 0x060012B6 RID: 4790 RVA: 0x0008B4FC File Offset: 0x000896FC
	private void Awake()
	{
		if (MusicMan.m_instance)
		{
			return;
		}
		MusicMan.m_instance = this;
		GameObject gameObject = new GameObject("music");
		gameObject.transform.SetParent(base.transform);
		this.m_musicSource = gameObject.AddComponent<AudioSource>();
		this.m_musicSource.loop = true;
		this.m_musicSource.spatialBlend = 0f;
		this.m_musicSource.outputAudioMixerGroup = this.m_musicMixer;
		this.m_musicSource.priority = 0;
		this.m_musicSource.bypassReverbZones = true;
		this.m_randomAmbientInterval = UnityEngine.Random.Range(this.m_randomMusicIntervalMin, this.m_randomMusicIntervalMax);
		MusicMan.m_masterMusicVolume = PlayerPrefs.GetFloat("MusicVolume", 1f);
		this.ApplySettings();
		foreach (MusicMan.NamedMusic namedMusic in this.m_music)
		{
			foreach (AudioClip audioClip in namedMusic.m_clips)
			{
				if (audioClip == null || !audioClip)
				{
					namedMusic.m_enabled = false;
					ZLog.LogWarning("Missing audio clip in music " + namedMusic.m_name);
					break;
				}
			}
		}
		foreach (MusicMan.NamedMusic namedMusic2 in this.m_music)
		{
			if (namedMusic2.m_enabled && namedMusic2.m_clips.Length != 0 && namedMusic2.m_clips[0] != null)
			{
				this.m_musicHashes.Add(namedMusic2.m_name.GetStableHashCode(), namedMusic2);
			}
		}
	}

	// Token: 0x060012B7 RID: 4791 RVA: 0x0008B6C4 File Offset: 0x000898C4
	public void ApplySettings()
	{
		foreach (MusicMan.NamedMusic namedMusic in this.m_music)
		{
			if (namedMusic.m_ambientMusic)
			{
				namedMusic.m_loop = Settings.ContinousMusic;
				if (!Settings.ContinousMusic && this.GetCurrentMusic() == namedMusic.m_name && this.m_musicSource.loop)
				{
					ZLog.Log("Stopping looping music because continous music is disabled");
					this.StopMusic();
				}
			}
		}
	}

	// Token: 0x060012B8 RID: 4792 RVA: 0x0008B75C File Offset: 0x0008995C
	private void OnDestroy()
	{
		if (MusicMan.m_instance == this)
		{
			MusicMan.m_instance = null;
		}
	}

	// Token: 0x060012B9 RID: 4793 RVA: 0x0008B774 File Offset: 0x00089974
	private void Update()
	{
		if (MusicMan.m_instance != this)
		{
			return;
		}
		float deltaTime = Time.deltaTime;
		this.UpdateCurrentMusic(deltaTime);
		this.UpdateCombatMusic(deltaTime);
		this.m_currentMusicVolMax = MusicVolume.UpdateProximityVolumes(this.m_musicSource);
		this.UpdateMusic(deltaTime);
	}

	// Token: 0x060012BA RID: 4794 RVA: 0x0008B7BC File Offset: 0x000899BC
	private void UpdateCurrentMusic(float dt)
	{
		string currentMusic = this.GetCurrentMusic();
		if (Game.instance != null)
		{
			if (Game.instance.InIntro(false) || (Player.m_localPlayer && Player.m_localPlayer.InIntro()))
			{
				this.StartMusic("intro");
				return;
			}
			if (currentMusic == "intro")
			{
				this.StopMusic();
			}
			if (Player.m_localPlayer == null)
			{
				this.StartMusic("respawn");
				return;
			}
			if (currentMusic == "respawn")
			{
				this.StopMusic();
			}
		}
		float target = (this.m_randomEventMusic == null) ? 0f : -80f;
		this.m_musicOnTopDuckVolume = Mathf.MoveTowards(this.m_musicOnTopDuckVolume, target, 80f * Time.deltaTime);
		this.m_musicMixer.audioMixer.SetFloat("Music_ontop_ducking", this.m_musicOnTopDuckVolume);
		if (this.HandleEventMusic(currentMusic))
		{
			return;
		}
		if (this.HandleLocationMusic(currentMusic))
		{
			return;
		}
		if (this.HandleSailingMusic(dt, currentMusic))
		{
			return;
		}
		if (this.HandleTriggerMusic(currentMusic))
		{
			return;
		}
		this.HandleEnvironmentMusic(dt, currentMusic);
	}

	// Token: 0x060012BB RID: 4795 RVA: 0x0008B8D0 File Offset: 0x00089AD0
	private bool HandleEnvironmentMusic(float dt, string currentMusic)
	{
		if (!EnvMan.instance)
		{
			return false;
		}
		MusicMan.NamedMusic environmentMusic = this.GetEnvironmentMusic();
		string currentMusic2 = this.GetCurrentMusic();
		if (environmentMusic == null || (this.m_currentMusic != null && environmentMusic.m_name != currentMusic2))
		{
			this.StopMusic();
			return true;
		}
		if (environmentMusic.m_name == currentMusic2)
		{
			return true;
		}
		if (!environmentMusic.m_loop)
		{
			if (Time.time - this.m_lastAmbientMusicTime < this.m_randomAmbientInterval)
			{
				return false;
			}
			this.m_randomAmbientInterval = UnityEngine.Random.Range(this.m_randomMusicIntervalMin, this.m_randomMusicIntervalMax);
			this.m_lastAmbientMusicTime = Time.time;
			ZLog.Log("Environment music starting at random ambient interval");
		}
		this.StartMusic(environmentMusic);
		return true;
	}

	// Token: 0x060012BC RID: 4796 RVA: 0x0008B980 File Offset: 0x00089B80
	private MusicMan.NamedMusic GetEnvironmentMusic()
	{
		string musicName;
		if (Player.m_localPlayer && Player.m_localPlayer.IsSafeInHome())
		{
			musicName = "home";
		}
		else
		{
			musicName = EnvMan.instance.GetAmbientMusic();
		}
		return this.FindMusic(musicName);
	}

	// Token: 0x060012BD RID: 4797 RVA: 0x0008B9C4 File Offset: 0x00089BC4
	private bool HandleTriggerMusic(string currentMusic)
	{
		if (this.m_triggerMusic != null)
		{
			this.StartMusic(this.m_triggerMusic);
			this.m_triggeredMusic = this.m_triggerMusic;
			this.m_triggerMusic = null;
			return true;
		}
		if (this.m_triggeredMusic != null)
		{
			if (currentMusic == this.m_triggeredMusic)
			{
				return true;
			}
			this.m_triggeredMusic = null;
		}
		return false;
	}

	// Token: 0x060012BE RID: 4798 RVA: 0x0008BA1B File Offset: 0x00089C1B
	public void LocationMusic(string name)
	{
		this.m_locationMusic = name;
	}

	// Token: 0x060012BF RID: 4799 RVA: 0x0008BA24 File Offset: 0x00089C24
	private bool HandleLocationMusic(string currentMusic)
	{
		if (this.m_lastLocationMusic != null && DateTime.Now > this.m_lastLocationMusicChange + TimeSpan.FromSeconds((double)this.m_repeatLocationMusicResetSeconds))
		{
			this.m_lastLocationMusic = null;
			this.m_lastLocationMusicChange = DateTime.Now;
		}
		if (this.m_locationMusic == null)
		{
			return false;
		}
		if (currentMusic == this.m_locationMusic && !this.m_musicSource.isPlaying)
		{
			this.m_locationMusic = null;
			return false;
		}
		if (currentMusic != this.m_locationMusic)
		{
			this.m_lastLocationMusicChange = DateTime.Now;
		}
		if (this.StartMusic(this.m_locationMusic))
		{
			this.m_lastLocationMusic = this.m_locationMusic;
		}
		else
		{
			ZLog.Log("Location music missing: " + this.m_locationMusic);
			this.m_locationMusic = null;
		}
		return true;
	}

	// Token: 0x060012C0 RID: 4800 RVA: 0x0008BAF0 File Offset: 0x00089CF0
	private bool HandleEventMusic(string currentMusic)
	{
		if (RandEventSystem.instance)
		{
			string musicOverride = RandEventSystem.instance.GetMusicOverride();
			if (musicOverride != null)
			{
				this.StartMusic(musicOverride);
				this.m_randomEventMusic = musicOverride;
				return true;
			}
			if (currentMusic == this.m_randomEventMusic)
			{
				this.m_randomEventMusic = null;
				this.StopMusic();
			}
		}
		return false;
	}

	// Token: 0x060012C1 RID: 4801 RVA: 0x0008BB44 File Offset: 0x00089D44
	private bool HandleCombatMusic(string currentMusic)
	{
		if (this.InCombat())
		{
			this.StartMusic("combat");
			return true;
		}
		if (currentMusic == "combat")
		{
			this.StopMusic();
		}
		return false;
	}

	// Token: 0x060012C2 RID: 4802 RVA: 0x0008BB70 File Offset: 0x00089D70
	private bool HandleSailingMusic(float dt, string currentMusic)
	{
		if (this.IsSailing())
		{
			this.m_notSailDuration = 0f;
			this.m_sailDuration += dt;
			if (this.m_sailDuration > this.m_sailMusicMinSailTime)
			{
				this.StartMusic(this.GetSailingMusic());
				return true;
			}
		}
		else
		{
			this.m_sailDuration = 0f;
			this.m_notSailDuration += dt;
			if (this.m_notSailDuration > this.m_sailMusicMinSailTime / 2f && currentMusic == this.GetSailingMusic() && currentMusic != EnvMan.instance.GetAmbientMusic())
			{
				this.StopMusic();
			}
		}
		return false;
	}

	// Token: 0x060012C3 RID: 4803 RVA: 0x0008BC0F File Offset: 0x00089E0F
	public string GetSailingMusic()
	{
		if (Player.m_localPlayer && Player.m_localPlayer.GetCurrentBiome() == Heightmap.Biome.AshLands)
		{
			return "sailing_ashlands";
		}
		return "sailing";
	}

	// Token: 0x060012C4 RID: 4804 RVA: 0x0008BC38 File Offset: 0x00089E38
	private bool IsSailing()
	{
		if (!Player.m_localPlayer)
		{
			return false;
		}
		Ship localShip = Ship.GetLocalShip();
		return localShip && localShip.GetSpeed() > this.m_sailMusicShipSpeedThreshold;
	}

	// Token: 0x060012C5 RID: 4805 RVA: 0x0008BC74 File Offset: 0x00089E74
	private void UpdateMusic(float dt)
	{
		if (this.m_queuedMusic != null || this.m_stopMusic)
		{
			if (!this.m_musicSource.isPlaying || this.m_currentMusicVol <= 0f)
			{
				if (this.m_musicSource.isPlaying && this.m_currentMusic != null && this.m_currentMusic.m_loop && this.m_currentMusic.m_resume)
				{
					this.m_currentMusic.m_lastPlayedTime = Time.time;
					this.m_currentMusic.m_savedPlaybackPos = this.m_musicSource.timeSamples;
					ZLog.Log("Stopped music " + this.m_currentMusic.m_name + " at " + this.m_currentMusic.m_savedPlaybackPos.ToString());
				}
				this.m_musicSource.Stop();
				this.m_stopMusic = false;
				this.m_currentMusic = null;
				if (this.m_queuedMusic != null)
				{
					this.m_musicSource.clip = this.m_queuedMusic.m_clips[UnityEngine.Random.Range(0, this.m_queuedMusic.m_clips.Length)];
					this.m_musicSource.loop = this.m_queuedMusic.m_loop;
					this.m_musicSource.volume = 0f;
					this.m_musicSource.timeSamples = 0;
					this.m_musicSource.Play();
					if (this.m_queuedMusic.m_loop && this.m_queuedMusic.m_resume && Time.time - this.m_queuedMusic.m_lastPlayedTime < this.m_musicSource.clip.length * 2f)
					{
						this.m_musicSource.timeSamples = this.m_queuedMusic.m_savedPlaybackPos;
						ZLog.Log("Resumed music " + this.m_queuedMusic.m_name + " at " + this.m_queuedMusic.m_savedPlaybackPos.ToString());
					}
					this.m_currentMusicVol = 0f;
					this.m_musicVolume = this.m_queuedMusic.m_volume;
					this.m_musicFadeTime = this.m_queuedMusic.m_fadeInTime;
					this.m_alwaysFadeout = this.m_queuedMusic.m_alwaysFadeout;
					this.m_currentMusic = this.m_queuedMusic;
					this.m_queuedMusic = null;
				}
			}
			else
			{
				float num = (this.m_queuedMusic != null) ? Mathf.Min(this.m_queuedMusic.m_fadeInTime, this.m_musicFadeTime) : this.m_musicFadeTime;
				this.m_currentMusicVol = Mathf.MoveTowards(this.m_currentMusicVol, 0f, dt / num);
				this.m_musicSource.volume = Utils.SmoothStep(0f, 1f, this.m_currentMusicVol) * this.m_musicVolume * MusicMan.m_masterMusicVolume;
			}
		}
		else if (this.m_musicSource.isPlaying)
		{
			float num2 = this.m_musicSource.clip.length - this.m_musicSource.time;
			if (this.m_alwaysFadeout && !this.m_musicSource.loop && num2 < this.m_musicFadeTime)
			{
				this.m_currentMusicVol = Mathf.MoveTowards(this.m_currentMusicVol, 0f, dt / this.m_musicFadeTime);
				this.m_musicSource.volume = Utils.SmoothStep(0f, 1f, this.m_currentMusicVol) * this.m_musicVolume * MusicMan.m_masterMusicVolume;
			}
			else
			{
				this.m_currentMusicVol = Mathf.MoveTowards(this.m_currentMusicVol, this.m_currentMusicVolMax, dt / this.m_musicFadeTime);
				this.m_musicSource.volume = Utils.SmoothStep(0f, 1f, this.m_currentMusicVol) * this.m_musicVolume * MusicMan.m_masterMusicVolume;
			}
			if (!Settings.ContinousMusic && num2 < this.m_musicFadeTime)
			{
				this.StopMusic();
				ZLog.Log("Music stopped after finishing, because continous music is disabled");
			}
		}
		else if (this.m_currentMusic != null && !this.m_musicSource.isPlaying)
		{
			this.m_currentMusic = null;
		}
		if (this.m_resetMusicTimer > 0f)
		{
			this.m_resetMusicTimer -= dt;
		}
		if (Terminal.m_showTests)
		{
			Terminal.m_testList["Music current"] = ((this.m_currentMusic == null) ? "NULL" : this.m_currentMusic.m_name);
			Terminal.m_testList["Music last started"] = ((this.m_lastStartedMusic == null) ? "NULL" : this.m_lastStartedMusic.m_name);
			Terminal.m_testList["Music queued"] = ((this.m_queuedMusic == null) ? "NULL" : this.m_queuedMusic.m_name);
			Terminal.m_testList["Music stopping"] = this.m_stopMusic.ToString();
			Terminal.m_testList["Music reset non continous"] = string.Format("{0} / {1}", this.m_resetMusicTimer, this.m_musicResetNonContinous);
			if (ZInput.GetKeyDown(KeyCode.N, true) && ZInput.GetKey(KeyCode.LeftShift, true) && this.m_musicSource != null && this.m_musicSource.isPlaying)
			{
				this.m_musicSource.time = this.m_musicSource.clip.length - 4f;
			}
		}
	}

	// Token: 0x060012C6 RID: 4806 RVA: 0x0008C16E File Offset: 0x0008A36E
	private void UpdateCombatMusic(float dt)
	{
		if (this.m_combatTimer > 0f)
		{
			this.m_combatTimer -= Time.deltaTime;
		}
	}

	// Token: 0x060012C7 RID: 4807 RVA: 0x0008C18F File Offset: 0x0008A38F
	public void ResetCombatTimer()
	{
		this.m_combatTimer = this.m_combatMusicTimeout;
	}

	// Token: 0x060012C8 RID: 4808 RVA: 0x0008C19D File Offset: 0x0008A39D
	private bool InCombat()
	{
		return this.m_combatTimer > 0f;
	}

	// Token: 0x060012C9 RID: 4809 RVA: 0x0008C1AC File Offset: 0x0008A3AC
	public void TriggerMusic(string name)
	{
		this.m_triggerMusic = name;
	}

	// Token: 0x060012CA RID: 4810 RVA: 0x0008C1B8 File Offset: 0x0008A3B8
	private bool StartMusic(string name)
	{
		if (this.GetCurrentMusic() == name)
		{
			return true;
		}
		MusicMan.NamedMusic music = this.FindMusic(name);
		return this.StartMusic(music);
	}

	// Token: 0x060012CB RID: 4811 RVA: 0x0008C1E4 File Offset: 0x0008A3E4
	private bool StartMusic(MusicMan.NamedMusic music)
	{
		if (music != null && this.GetCurrentMusic() == music.m_name)
		{
			return true;
		}
		if (music == this.m_lastStartedMusic && !Settings.ContinousMusic && this.m_resetMusicTimer > 0f)
		{
			return false;
		}
		this.m_lastStartedMusic = music;
		this.m_resetMusicTimer = this.m_musicResetNonContinous + ((music != null && music.m_clips.Length != 0) ? music.m_clips[0].length : 0f);
		if (music != null)
		{
			this.m_queuedMusic = music;
			this.m_stopMusic = false;
			ZLog.Log("Starting music " + music.m_name);
			return true;
		}
		this.StopMusic();
		return false;
	}

	// Token: 0x060012CC RID: 4812 RVA: 0x0008C28B File Offset: 0x0008A48B
	private MusicMan.NamedMusic FindMusic(string musicName)
	{
		if (string.IsNullOrEmpty(musicName))
		{
			return null;
		}
		return this.m_musicHashes.GetValueOrDefault(musicName.GetStableHashCode());
	}

	// Token: 0x060012CD RID: 4813 RVA: 0x0008C2A8 File Offset: 0x0008A4A8
	public bool IsPlaying()
	{
		return this.m_musicSource.isPlaying;
	}

	// Token: 0x060012CE RID: 4814 RVA: 0x0008C2B5 File Offset: 0x0008A4B5
	private string GetCurrentMusic()
	{
		if (this.m_stopMusic)
		{
			return "";
		}
		if (this.m_queuedMusic != null)
		{
			return this.m_queuedMusic.m_name;
		}
		if (this.m_currentMusic != null)
		{
			return this.m_currentMusic.m_name;
		}
		return "";
	}

	// Token: 0x060012CF RID: 4815 RVA: 0x0008C2F2 File Offset: 0x0008A4F2
	private void StopMusic()
	{
		this.m_queuedMusic = null;
		this.m_stopMusic = true;
	}

	// Token: 0x060012D0 RID: 4816 RVA: 0x0008C302 File Offset: 0x0008A502
	public void Reset()
	{
		this.StopMusic();
		this.m_combatTimer = 0f;
		this.m_randomEventMusic = null;
		this.m_triggerMusic = null;
		this.m_locationMusic = null;
	}

	// Token: 0x0400126B RID: 4715
	private string m_triggeredMusic = "";

	// Token: 0x0400126C RID: 4716
	private static MusicMan m_instance;

	// Token: 0x0400126D RID: 4717
	public static float m_masterMusicVolume = 1f;

	// Token: 0x0400126E RID: 4718
	public AudioMixerGroup m_musicMixer;

	// Token: 0x0400126F RID: 4719
	public List<MusicMan.NamedMusic> m_music = new List<MusicMan.NamedMusic>();

	// Token: 0x04001270 RID: 4720
	public float m_musicResetNonContinous = 120f;

	// Token: 0x04001271 RID: 4721
	private readonly Dictionary<int, MusicMan.NamedMusic> m_musicHashes = new Dictionary<int, MusicMan.NamedMusic>();

	// Token: 0x04001272 RID: 4722
	[Header("Combat")]
	public float m_combatMusicTimeout = 4f;

	// Token: 0x04001273 RID: 4723
	[Header("Sailing")]
	public float m_sailMusicShipSpeedThreshold = 3f;

	// Token: 0x04001274 RID: 4724
	public float m_sailMusicMinSailTime = 20f;

	// Token: 0x04001275 RID: 4725
	[Header("Ambient music")]
	public float m_randomMusicIntervalMin = 300f;

	// Token: 0x04001276 RID: 4726
	public float m_randomMusicIntervalMax = 500f;

	// Token: 0x04001277 RID: 4727
	private MusicMan.NamedMusic m_queuedMusic;

	// Token: 0x04001278 RID: 4728
	private MusicMan.NamedMusic m_currentMusic;

	// Token: 0x04001279 RID: 4729
	private MusicMan.NamedMusic m_lastStartedMusic;

	// Token: 0x0400127A RID: 4730
	private float m_musicVolume = 1f;

	// Token: 0x0400127B RID: 4731
	private float m_musicFadeTime = 3f;

	// Token: 0x0400127C RID: 4732
	private bool m_alwaysFadeout;

	// Token: 0x0400127D RID: 4733
	private bool m_stopMusic;

	// Token: 0x0400127E RID: 4734
	private string m_randomEventMusic;

	// Token: 0x0400127F RID: 4735
	private float m_lastAmbientMusicTime;

	// Token: 0x04001280 RID: 4736
	private float m_randomAmbientInterval;

	// Token: 0x04001281 RID: 4737
	private string m_triggerMusic;

	// Token: 0x04001282 RID: 4738
	private string m_locationMusic;

	// Token: 0x04001283 RID: 4739
	public string m_lastLocationMusic;

	// Token: 0x04001284 RID: 4740
	private DateTime m_lastLocationMusicChange;

	// Token: 0x04001285 RID: 4741
	public int m_repeatLocationMusicResetSeconds = 300;

	// Token: 0x04001286 RID: 4742
	private float m_combatTimer;

	// Token: 0x04001287 RID: 4743
	private float m_resetMusicTimer;

	// Token: 0x04001288 RID: 4744
	private AudioSource m_musicSource;

	// Token: 0x04001289 RID: 4745
	private float m_currentMusicVol;

	// Token: 0x0400128A RID: 4746
	public float m_currentMusicVolMax = 1f;

	// Token: 0x0400128B RID: 4747
	private float m_sailDuration;

	// Token: 0x0400128C RID: 4748
	private float m_notSailDuration;

	// Token: 0x0400128D RID: 4749
	private float m_musicOnTopDuckVolume;

	// Token: 0x0400128E RID: 4750
	private const string c_Duckmusic = "Music_ontop_ducking";

	// Token: 0x02000322 RID: 802
	[Serializable]
	public class NamedMusic
	{
		// Token: 0x040023F4 RID: 9204
		public string m_name = "";

		// Token: 0x040023F5 RID: 9205
		public AudioClip[] m_clips;

		// Token: 0x040023F6 RID: 9206
		public float m_volume = 1f;

		// Token: 0x040023F7 RID: 9207
		public float m_fadeInTime = 3f;

		// Token: 0x040023F8 RID: 9208
		public bool m_alwaysFadeout;

		// Token: 0x040023F9 RID: 9209
		public bool m_loop;

		// Token: 0x040023FA RID: 9210
		public bool m_resume;

		// Token: 0x040023FB RID: 9211
		public bool m_enabled = true;

		// Token: 0x040023FC RID: 9212
		public bool m_ambientMusic;

		// Token: 0x040023FD RID: 9213
		[NonSerialized]
		public int m_savedPlaybackPos;

		// Token: 0x040023FE RID: 9214
		[NonSerialized]
		public float m_lastPlayedTime;
	}
}
