using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Valheim.SettingsGui;

// Token: 0x02000092 RID: 146
public class Settings : MonoBehaviour
{
	// Token: 0x17000042 RID: 66
	// (get) Token: 0x060009EA RID: 2538 RVA: 0x000570BF File Offset: 0x000552BF
	public static Settings instance
	{
		get
		{
			return Settings.m_instance;
		}
	}

	// Token: 0x060009EB RID: 2539 RVA: 0x000570C6 File Offset: 0x000552C6
	private void Awake()
	{
		Settings.m_instance = this;
		this.m_tabHandler = base.GetComponentInChildren<TabHandler>();
		this.SetAvailableTabs();
		ZInput.OnInputLayoutChanged += this.OnInputLayoutChanged;
		this.OnInputLayoutChanged();
	}

	// Token: 0x060009EC RID: 2540 RVA: 0x000570F7 File Offset: 0x000552F7
	private void Update()
	{
		if (!this.m_navigationBlocked && ZInput.GetKeyDown(KeyCode.Escape, true))
		{
			this.OnBack();
		}
	}

	// Token: 0x060009ED RID: 2541 RVA: 0x00057114 File Offset: 0x00055314
	private void SetAvailableTabs()
	{
		this.SettingsTabs = new List<SettingsBase>();
		foreach (TabHandler.Tab tab in this.m_tabHandler.m_tabs)
		{
			this.SettingsTabs.Add(tab.m_page.gameObject.GetComponent<SettingsBase>());
		}
		this.LoadTabSettings();
		this.m_tabHandler.ActiveTabChanged -= this.ActiveTabChanged;
		this.m_tabHandler.ActiveTabChanged += this.ActiveTabChanged;
	}

	// Token: 0x060009EE RID: 2542 RVA: 0x000571C0 File Offset: 0x000553C0
	private void OnInputLayoutChanged()
	{
		GameObject[] tabKeyHints = this.m_tabKeyHints;
		for (int i = 0; i < tabKeyHints.Length; i++)
		{
			tabKeyHints[i].SetActive(ZInput.GamepadActive);
		}
	}

	// Token: 0x060009EF RID: 2543 RVA: 0x000571EF File Offset: 0x000553EF
	private void ActiveTabChanged(int index)
	{
		this.SettingsTabs[index].FixBackButtonNavigation(this.m_backButton);
		this.SettingsTabs[index].FixOkButtonNavigation(this.m_okButton);
	}

	// Token: 0x060009F0 RID: 2544 RVA: 0x00057220 File Offset: 0x00055420
	private void LoadTabSettings()
	{
		foreach (SettingsBase settingsBase in this.SettingsTabs)
		{
			settingsBase.LoadSettings();
		}
	}

	// Token: 0x060009F1 RID: 2545 RVA: 0x00057270 File Offset: 0x00055470
	private void ResetTabSettings()
	{
		foreach (SettingsBase settingsBase in this.SettingsTabs)
		{
			settingsBase.ResetSettings();
		}
		ZInput.instance.Save();
	}

	// Token: 0x060009F2 RID: 2546 RVA: 0x000572CC File Offset: 0x000554CC
	private void SaveTabSettings()
	{
		this.m_tabsToSave = 0;
		foreach (SettingsBase settingsBase in this.SettingsTabs)
		{
			settingsBase.Saved = (Action)Delegate.Remove(settingsBase.Saved, new Action(this.TabSaved));
			settingsBase.Saved = (Action)Delegate.Combine(settingsBase.Saved, new Action(this.TabSaved));
			this.m_tabsToSave++;
		}
		foreach (SettingsBase settingsBase2 in this.SettingsTabs)
		{
			settingsBase2.SaveSettings();
		}
	}

	// Token: 0x060009F3 RID: 2547 RVA: 0x000573B0 File Offset: 0x000555B0
	private void ApplyAndClose()
	{
		ZInput.instance.Save();
		if (GameCamera.instance)
		{
			GameCamera.instance.ApplySettings();
		}
		if (CameraEffects.instance)
		{
			CameraEffects.instance.ApplySettings();
		}
		if (ClutterSystem.instance)
		{
			ClutterSystem.instance.ApplySettings();
		}
		if (MusicMan.instance)
		{
			MusicMan.instance.ApplySettings();
		}
		if (GameCamera.instance)
		{
			GameCamera.instance.ApplySettings();
		}
		if (KeyHints.instance)
		{
			KeyHints.instance.ApplySettings();
		}
		LightFlicker[] array = UnityEngine.Object.FindObjectsOfType<LightFlicker>();
		for (int i = 0; i < array.Length; i++)
		{
			array[i].ApplySettings();
		}
		PlayerPrefs.Save();
		UnityEngine.Object.Destroy(base.gameObject);
	}

	// Token: 0x060009F4 RID: 2548 RVA: 0x00057478 File Offset: 0x00055678
	private void TabSaved()
	{
		int num = this.m_tabsToSave - 1;
		this.m_tabsToSave = num;
		if (num > 0)
		{
			return;
		}
		this.ApplyAndClose();
	}

	// Token: 0x060009F5 RID: 2549 RVA: 0x000574A0 File Offset: 0x000556A0
	private void OnDestroy()
	{
		ZInput.OnInputLayoutChanged -= this.OnInputLayoutChanged;
		this.m_tabHandler.ActiveTabChanged -= this.ActiveTabChanged;
		Action settingsPopupDestroyed = this.SettingsPopupDestroyed;
		if (settingsPopupDestroyed != null)
		{
			settingsPopupDestroyed();
		}
		Settings.m_instance = null;
	}

	// Token: 0x060009F6 RID: 2550 RVA: 0x000574EC File Offset: 0x000556EC
	public void OnBack()
	{
		this.ResetTabSettings();
		UnityEngine.Object.Destroy(base.gameObject);
	}

	// Token: 0x060009F7 RID: 2551 RVA: 0x000574FF File Offset: 0x000556FF
	public void OnOk()
	{
		this.SaveTabSettings();
	}

	// Token: 0x060009F8 RID: 2552 RVA: 0x00057508 File Offset: 0x00055708
	public void BlockNavigation(bool block)
	{
		this.m_navigationBlocked = block;
		this.m_okButton.gameObject.SetActive(!block);
		this.m_backButton.gameObject.SetActive(!block);
		this.m_tabHandler.m_gamepadInput = !block;
		this.m_tabHandler.m_keybaordInput = !block;
		this.m_tabHandler.m_tabKeyInput = !block;
	}

	// Token: 0x060009F9 RID: 2553 RVA: 0x00057574 File Offset: 0x00055774
	public static void SetPlatformDefaultPrefs()
	{
		if (Settings.IsSteamRunningOnSteamDeck())
		{
			ZLog.Log("Running on Steam Deck!");
		}
		else
		{
			ZLog.Log("Using default prefs");
		}
		PlatformPrefs.PlatformDefaults[] array = new PlatformPrefs.PlatformDefaults[1];
		array[0] = new PlatformPrefs.PlatformDefaults("deck_", () => Settings.IsSteamRunningOnSteamDeck(), new Dictionary<string, PlatformPrefs>
		{
			{
				"GuiScale",
				1.15f
			},
			{
				"DOF",
				0
			},
			{
				"VSync",
				0
			},
			{
				"Bloom",
				1
			},
			{
				"SSAO",
				1
			},
			{
				"SunShafts",
				1
			},
			{
				"AntiAliasing",
				0
			},
			{
				"ChromaticAberration",
				1
			},
			{
				"MotionBlur",
				0
			},
			{
				"SoftPart",
				1
			},
			{
				"Tesselation",
				0
			},
			{
				"DistantShadows",
				1
			},
			{
				"ShadowQuality",
				0
			},
			{
				"LodBias",
				1
			},
			{
				"Lights",
				1
			},
			{
				"ClutterQuality",
				1
			},
			{
				"PointLights",
				1
			},
			{
				"PointLightShadows",
				1
			},
			{
				"FPSLimit",
				60
			}
		});
		PlatformPrefs.SetDefaults(array);
	}

	// Token: 0x060009FA RID: 2554 RVA: 0x00057724 File Offset: 0x00055924
	public static bool IsSteamRunningOnSteamDeck()
	{
		string environmentVariable = Environment.GetEnvironmentVariable("SteamDeck");
		return !string.IsNullOrEmpty(environmentVariable) && environmentVariable != "0";
	}

	// Token: 0x060009FB RID: 2555 RVA: 0x00057754 File Offset: 0x00055954
	public static void ApplyStartupSettings()
	{
		Settings.ReduceBackgroundUsage = (PlatformPrefs.GetInt("ReduceBackgroundUsage", 0) == 1);
		Settings.ContinousMusic = (PlatformPrefs.GetInt("ContinousMusic", 1) == 1);
		Settings.ReduceFlashingLights = (PlatformPrefs.GetInt("ReduceFlashingLights", 0) == 1);
		Raven.m_tutorialsEnabled = (PlatformPrefs.GetInt("TutorialsEnabled", 1) == 1);
		Settings.ClosedCaptions = (PlatformPrefs.GetInt("ClosedCaptions", 0) == 1);
		Settings.DirectionalSoundIndicators = (PlatformPrefs.GetInt("DirectionalSoundIndicators", 0) == 1);
		GraphicsModeManager.Initialize();
	}

	// Token: 0x060009FC RID: 2556 RVA: 0x000577D8 File Offset: 0x000559D8
	public static void ApplyQualitySettings()
	{
		bool startUp = Settings.m_startUp;
		QualitySettings.vSyncCount = ((PlatformPrefs.GetInt("VSync", 0) == 1) ? 1 : 0);
		QualitySettings.softParticles = (PlatformPrefs.GetInt("SoftPart", 1) == 1);
		if (PlatformPrefs.GetInt("Tesselation", 1) == 1)
		{
			Shader.EnableKeyword("TESSELATION_ON");
		}
		else
		{
			Shader.DisableKeyword("TESSELATION_ON");
		}
		switch (PlatformPrefs.GetInt("LodBias", 2))
		{
		case 0:
			QualitySettings.lodBias = 1f;
			break;
		case 1:
			QualitySettings.lodBias = 1.5f;
			break;
		case 2:
			QualitySettings.lodBias = 2f;
			break;
		case 3:
			QualitySettings.lodBias = 5f;
			break;
		}
		switch (PlatformPrefs.GetInt("Lights", 2))
		{
		case 0:
			QualitySettings.pixelLightCount = 2;
			break;
		case 1:
			QualitySettings.pixelLightCount = 4;
			break;
		case 2:
			QualitySettings.pixelLightCount = 8;
			break;
		}
		LightLod.m_lightLimit = Settings.GetPointLightLimit(PlatformPrefs.GetInt("PointLights", 3));
		LightLod.m_shadowLimit = Settings.GetPointLightShadowLimit(PlatformPrefs.GetInt("PointLightShadows", 2));
		Settings.FPSLimit = PlatformPrefs.GetInt("FPSLimit", -1);
		float @float = PlatformPrefs.GetFloat("RenderScale", 1f);
		if (@float <= 0f)
		{
			VirtualFrameBuffer.m_autoRenderScale = true;
		}
		else
		{
			VirtualFrameBuffer.m_autoRenderScale = false;
			VirtualFrameBuffer.m_global3DRenderScale = Mathf.Clamp01(@float);
		}
		Settings.ApplyShadowQuality();
		Settings.m_startUp = false;
	}

	// Token: 0x060009FD RID: 2557 RVA: 0x00057939 File Offset: 0x00055B39
	public static int GetPointLightLimit(int level)
	{
		switch (level)
		{
		case 0:
			return 4;
		case 1:
			return 15;
		case 3:
			return -1;
		}
		return 40;
	}

	// Token: 0x060009FE RID: 2558 RVA: 0x0005795C File Offset: 0x00055B5C
	public static int GetPointLightShadowLimit(int level)
	{
		switch (level)
		{
		case 0:
			return 0;
		case 1:
			return 1;
		case 3:
			return -1;
		}
		return 3;
	}

	// Token: 0x060009FF RID: 2559 RVA: 0x00057980 File Offset: 0x00055B80
	public static void ApplyShadowQuality()
	{
		int @int = PlatformPrefs.GetInt("ShadowQuality", 2);
		int int2 = PlatformPrefs.GetInt("DistantShadows", 1);
		switch (@int)
		{
		case 0:
			QualitySettings.shadowCascades = 2;
			QualitySettings.shadowDistance = 80f;
			QualitySettings.shadowResolution = ShadowResolution.Low;
			break;
		case 1:
			QualitySettings.shadowCascades = 3;
			QualitySettings.shadowDistance = 120f;
			QualitySettings.shadowResolution = ShadowResolution.Medium;
			break;
		case 2:
			QualitySettings.shadowCascades = 4;
			QualitySettings.shadowDistance = 150f;
			QualitySettings.shadowResolution = ShadowResolution.High;
			break;
		}
		Heightmap.EnableDistantTerrainShadows = (int2 == 1);
	}

	// Token: 0x14000006 RID: 6
	// (add) Token: 0x06000A00 RID: 2560 RVA: 0x00057A08 File Offset: 0x00055C08
	// (remove) Token: 0x06000A01 RID: 2561 RVA: 0x00057A40 File Offset: 0x00055C40
	public event Action SettingsPopupDestroyed;

	// Token: 0x04000B74 RID: 2932
	private static Settings m_instance;

	// Token: 0x04000B75 RID: 2933
	private static bool m_startUp = true;

	// Token: 0x04000B76 RID: 2934
	public const bool c_vsyncRequiresRestart = false;

	// Token: 0x04000B77 RID: 2935
	public const float c_renderScaleDefault = 1f;

	// Token: 0x04000B78 RID: 2936
	public static int FPSLimit = -1;

	// Token: 0x04000B79 RID: 2937
	public static bool ReduceBackgroundUsage = false;

	// Token: 0x04000B7A RID: 2938
	public static bool ContinousMusic = true;

	// Token: 0x04000B7B RID: 2939
	public static bool ReduceFlashingLights = false;

	// Token: 0x04000B7C RID: 2940
	public static bool ClosedCaptions = false;

	// Token: 0x04000B7D RID: 2941
	public static bool DirectionalSoundIndicators = false;

	// Token: 0x04000B7E RID: 2942
	public static AssetMemoryUsagePolicy AssetMemoryUsagePolicy = AssetMemoryUsagePolicy.KeepSynchronousOnlyLoaded;

	// Token: 0x04000B7F RID: 2943
	[SerializeField]
	private GameObject[] m_tabKeyHints;

	// Token: 0x04000B80 RID: 2944
	[SerializeField]
	private GameObject m_settingsPanel;

	// Token: 0x04000B81 RID: 2945
	[SerializeField]
	private TabHandler m_tabHandler;

	// Token: 0x04000B82 RID: 2946
	[SerializeField]
	private Button m_backButton;

	// Token: 0x04000B83 RID: 2947
	[SerializeField]
	private Button m_okButton;

	// Token: 0x04000B84 RID: 2948
	private bool m_navigationBlocked;

	// Token: 0x04000B85 RID: 2949
	private List<SettingsBase> SettingsTabs;

	// Token: 0x04000B86 RID: 2950
	private int m_tabsToSave;

	// Token: 0x04000B88 RID: 2952
	public Action<string, int> SharedSettingsChanged;
}
