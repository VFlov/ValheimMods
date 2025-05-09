using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

// Token: 0x02000091 RID: 145
public class ServerOptionsGUI : MonoBehaviour
{
	// Token: 0x060009DA RID: 2522 RVA: 0x0005625A File Offset: 0x0005445A
	public void Awake()
	{
		ServerOptionsGUI.m_instance = this;
		ServerOptionsGUI.m_modifiers = this.m_modifiersRoot.transform.GetComponentsInChildren<KeyUI>();
		ServerOptionsGUI.m_presets = this.m_presetsRoot.transform.GetComponentsInChildren<KeyButton>();
	}

	// Token: 0x060009DB RID: 2523 RVA: 0x0005628C File Offset: 0x0005448C
	private void Update()
	{
		if (ZNet.instance != null)
		{
			base.gameObject.SetActive(false);
			return;
		}
		this.m_toolTipPanel.gameObject.SetActive(this.m_toolTipText.text.Length > 0);
		if (EventSystem.current.currentSelectedGameObject == null)
		{
			EventSystem.current.SetSelectedGameObject(this.m_doneButton);
		}
	}

	// Token: 0x060009DC RID: 2524 RVA: 0x000562F8 File Offset: 0x000544F8
	public void ReadKeys(World world)
	{
		if (world == null)
		{
			return;
		}
		KeyUI[] modifiers = ServerOptionsGUI.m_modifiers;
		for (int i = 0; i < modifiers.Length; i++)
		{
			modifiers[i].TryMatch(world, false);
		}
	}

	// Token: 0x060009DD RID: 2525 RVA: 0x00056328 File Offset: 0x00054528
	public void SetKeys(World world)
	{
		if (world == null)
		{
			return;
		}
		string text = "";
		bool flag = false;
		foreach (KeyUI keyUI in ServerOptionsGUI.m_modifiers)
		{
			keyUI.SetKeys(world);
			KeySlider keySlider = keyUI as KeySlider;
			if (keySlider != null && keySlider.m_manualSet)
			{
				flag = true;
			}
		}
		if (flag)
		{
			KeyUI[] modifiers = ServerOptionsGUI.m_modifiers;
			for (int i = 0; i < modifiers.Length; i++)
			{
				KeySlider keySlider2 = modifiers[i] as KeySlider;
				if (keySlider2 != null)
				{
					if (text.Length > 0)
					{
						text += ":";
					}
					text = text + keySlider2.m_modifier.ToString().ToLower() + "_" + keySlider2.GetValue().ToString().ToLower();
				}
			}
		}
		else if (text.Length == 0 && this.m_preset > WorldPresets.Custom)
		{
			text = this.m_preset.ToString().ToLower();
		}
		if (text.Length > 0)
		{
			string text2 = GlobalKeys.Preset.ToString().ToLower() + " " + text;
			if (ZoneSystem.instance)
			{
				ZoneSystem.instance.SetGlobalKey(text2);
			}
			else
			{
				world.m_startingGlobalKeys.Add(text2);
			}
			Terminal.Log("Saving modifier preset: " + text);
		}
	}

	// Token: 0x060009DE RID: 2526 RVA: 0x0005647C File Offset: 0x0005467C
	public void OnPresetButton(KeyButton button)
	{
		foreach (KeyUI keyUI in ServerOptionsGUI.m_modifiers)
		{
			string text;
			keyUI.TryMatch(button.m_keys, out text, true);
			KeySlider keySlider = keyUI as KeySlider;
			if (keySlider != null)
			{
				keySlider.m_manualSet = false;
			}
		}
		this.m_preset = button.m_preset;
	}

	// Token: 0x060009DF RID: 2527 RVA: 0x000564CC File Offset: 0x000546CC
	public void SetPreset(World world, string combinedString)
	{
		WorldPresets preset;
		if (Enum.TryParse<WorldPresets>(combinedString, true, out preset))
		{
			this.SetPreset(world, preset);
			return;
		}
		foreach (string text in combinedString.Split(':', StringSplitOptions.None))
		{
			string[] array2 = text.Split('_', StringSplitOptions.None);
			WorldModifiers preset2;
			WorldModifierOption value;
			if (array2.Length == 2 && Enum.TryParse<WorldModifiers>(array2[0], true, out preset2) && Enum.TryParse<WorldModifierOption>(array2[1], true, out value))
			{
				this.SetPreset(world, preset2, value);
			}
			else
			{
				Terminal.LogError("Invalid preset string data '" + text + "'");
			}
		}
	}

	// Token: 0x060009E0 RID: 2528 RVA: 0x0005655C File Offset: 0x0005475C
	public void SetPreset(World world, WorldPresets preset)
	{
		Terminal.Log(string.Format("Setting World preset: {0}", preset));
		foreach (KeyButton keyButton in ServerOptionsGUI.m_presets)
		{
			if (keyButton.m_preset == preset)
			{
				keyButton.SetKeys(world);
				ZoneSystem instance = ZoneSystem.instance;
				if (instance != null)
				{
					instance.UpdateWorldRates();
				}
				this.m_preset = preset;
				return;
			}
		}
		Terminal.LogError(string.Format("Missing settings for preset: {0}", preset));
	}

	// Token: 0x060009E1 RID: 2529 RVA: 0x000565D4 File Offset: 0x000547D4
	public void SetPreset(World world, WorldModifiers preset, WorldModifierOption value)
	{
		Terminal.Log(string.Format("Setting WorldModifiers preset: '{0}' to '{1}'", preset, value));
		KeyUI[] modifiers = ServerOptionsGUI.m_modifiers;
		int i = 0;
		while (i < modifiers.Length)
		{
			KeySlider keySlider = modifiers[i] as KeySlider;
			if (keySlider != null && keySlider.m_modifier == preset)
			{
				keySlider.SetValue(value);
				keySlider.SetKeys(world);
				ZoneSystem instance = ZoneSystem.instance;
				if (instance == null)
				{
					return;
				}
				instance.UpdateWorldRates();
				return;
			}
			else
			{
				i++;
			}
		}
		Terminal.LogError(string.Format("Missing settings for preset: '{0}' to '{1}'", preset, value));
	}

	// Token: 0x060009E2 RID: 2530 RVA: 0x0005665E File Offset: 0x0005485E
	public void OnCustomValueChanged(KeyUI element)
	{
		this.m_preset = WorldPresets.Custom;
	}

	// Token: 0x060009E3 RID: 2531 RVA: 0x00056668 File Offset: 0x00054868
	public static void Initizalize()
	{
		KeyUI[] modifiers = ServerOptionsGUI.m_modifiers;
		for (int i = 0; i < modifiers.Length; i++)
		{
			KeySlider keySlider = modifiers[i] as KeySlider;
			if (keySlider != null)
			{
				if (keySlider.m_modifier == WorldModifiers.Default)
				{
					ZLog.LogError(string.Format("Modifier {0} is setup without a defined modifier", keySlider.m_nameLabel));
				}
				List<WorldModifierOption> list = new List<WorldModifierOption>();
				ServerOptionsGUI.m_modifierSetups[keySlider.m_modifier] = list;
				foreach (KeySlider.SliderSetting sliderSetting in keySlider.m_settings)
				{
					if (sliderSetting.m_modifierValue == WorldModifierOption.Default)
					{
						ZLog.LogError(string.Format("Modifier setting {0} in {1} is setup without a modifier option", sliderSetting.m_name, keySlider.m_nameLabel));
					}
					list.Add(sliderSetting.m_modifierValue);
				}
			}
		}
	}

	// Token: 0x060009E4 RID: 2532 RVA: 0x00056748 File Offset: 0x00054948
	public static string GetWorldModifierSummary(IEnumerable<string> keys, bool alwaysShort = false, string divider = ", ")
	{
		string text = "";
		string text2 = "";
		ServerOptionsGUI.m_tempKeys.Clear();
		ServerOptionsGUI.m_tempKeys.AddRange(keys);
		if (ServerOptionsGUI.m_tempKeys.Count == 0)
		{
			return "";
		}
		if (ServerOptionsGUI.m_presets == null)
		{
			ZLog.LogWarning("Can't get world modifier summary until prefab has been initiated.");
			return "Error!";
		}
		KeyButton keyButton = null;
		int num = 0;
		foreach (KeyButton keyButton2 in ServerOptionsGUI.m_presets)
		{
			if (keyButton2.m_preset != WorldPresets.Default && keyButton2.TryMatch(ServerOptionsGUI.m_tempKeys) && keyButton2.m_keys.Count > num)
			{
				keyButton = keyButton2;
				num = keyButton2.m_keys.Count;
			}
		}
		if (keyButton != null)
		{
			text2 = keyButton.m_preset.GetDisplayString();
			foreach (string item in keyButton.m_keys)
			{
				ServerOptionsGUI.m_tempKeys.Remove(item);
			}
		}
		KeyUI[] modifiers = ServerOptionsGUI.m_modifiers;
		for (int i = 0; i < modifiers.Length; i++)
		{
			string str;
			if (modifiers[i].TryMatch(ServerOptionsGUI.m_tempKeys, out str, false))
			{
				if (text.Length > 0)
				{
					text += divider;
				}
				text += str;
			}
		}
		if (alwaysShort)
		{
			if (text.Length > 0)
			{
				if (text2.Length > 0)
				{
					text2 += "+";
				}
				else
				{
					text2 = "$menu_modifier_custom";
				}
			}
			return text2;
		}
		if (text.Length > 0)
		{
			if (text2.Length <= 0)
			{
				return text;
			}
			text2 = text2 + divider + text;
		}
		return text2;
	}

	// Token: 0x060009E5 RID: 2533 RVA: 0x000568F0 File Offset: 0x00054AF0
	public static bool TryConvertModifierKeysToCompactKVP<T>(ICollection<string> keys, out T result) where T : IDictionary<string, string>, new()
	{
		result = Activator.CreateInstance<T>();
		foreach (string text in keys)
		{
			int num = text.IndexOf(' ');
			string text2;
			string text3;
			if (num >= 0)
			{
				text2 = text.Substring(0, num);
				text3 = text.Substring(num + 1);
			}
			else
			{
				text2 = text;
				text3 = null;
			}
			GlobalKeys globalKeys;
			if (!Enum.TryParse<GlobalKeys>(text2, true, out globalKeys) || globalKeys.ToString().ToLower() != text2.ToLower())
			{
				ZLog.LogError("Failed to parse key " + text + " as GlobalKeys!");
				return false;
			}
			int num5;
			if (globalKeys == GlobalKeys.Preset)
			{
				string text4 = "";
				int[] array = text3.AllIndicesOf(':');
				for (int i = 0; i < array.Length + 1; i++)
				{
					int num2;
					if (i > 0)
					{
						text4 += ":";
						num2 = array[i - 1] + 1;
					}
					else
					{
						num2 = 0;
					}
					int num3 = text3.IndexOf('_', num2);
					int num4 = (i >= array.Length) ? text3.Length : array[i];
					if (num3 >= num4)
					{
						ZLog.LogError("Failed to parse value " + text3 + "'s subkey as WorldModifiers and WorldModifierOption: separator index in wrong location!");
					}
					if (num3 < 0)
					{
						string text5 = text3.Substring(num2, num4 - num2);
						WorldPresets worldPresets;
						if (!Enum.TryParse<WorldPresets>(text5, true, out worldPresets) || worldPresets.ToString().ToLower() != text5.ToLower())
						{
							ZLog.LogError(string.Concat(new string[]
							{
								"Failed to parse value ",
								text3,
								"'s subvalue ",
								text5,
								" as WorldPresets: Value enum couldn't be parsed!"
							}));
							return false;
						}
						string str = text4;
						num5 = (int)worldPresets;
						text4 = str + num5.ToString();
					}
					else
					{
						string text6 = text3.Substring(num2, num3 - num2);
						string text7 = text3.Substring(num3 + 1, num4 - (num3 + 1));
						WorldModifiers worldModifiers;
						if (!Enum.TryParse<WorldModifiers>(text6, true, out worldModifiers) || worldModifiers.ToString().ToLower() != text6.ToLower())
						{
							ZLog.LogError(string.Concat(new string[]
							{
								"Failed to parse value ",
								text3,
								"'s subkey ",
								text6,
								" as WorldModifiers: Key enum couldn't be parsed!"
							}));
							return false;
						}
						WorldModifierOption worldModifierOption;
						if (!Enum.TryParse<WorldModifierOption>(text7, true, out worldModifierOption) || worldModifierOption.ToString().ToLower() != text7.ToLower())
						{
							ZLog.LogError(string.Concat(new string[]
							{
								"Failed to parse value ",
								text3,
								"'s subvalue ",
								text7,
								" as WorldModifierOption: Value enum couldn't be parsed!"
							}));
							return false;
						}
						string str2 = text4;
						num5 = (int)worldModifiers;
						string str3 = num5.ToString();
						string str4 = "_";
						num5 = (int)worldModifierOption;
						text4 = str2 + str3 + str4 + num5.ToString();
					}
				}
				text3 = text4;
			}
			num5 = (int)globalKeys;
			result[num5.ToString()] = text3;
		}
		return true;
	}

	// Token: 0x060009E6 RID: 2534 RVA: 0x00056C24 File Offset: 0x00054E24
	public static bool TryConvertCompactKVPToModifierKeys<T>(IDictionary<string, string> kvps, out T result) where T : ICollection<string>, new()
	{
		GlobalKeys[] array = new GlobalKeys[kvps.Count];
		string[] array2 = new string[kvps.Count];
		int num = 0;
		result = Activator.CreateInstance<T>();
		foreach (KeyValuePair<string, string> keyValuePair in kvps)
		{
			int num2;
			if (!int.TryParse(keyValuePair.Key, out num2))
			{
				ZLog.LogError(string.Concat(new string[]
				{
					"Failed to parse key ",
					keyValuePair.Key,
					" as GlobalKeys: ",
					keyValuePair.Key,
					" could not be parsed as an integer!"
				}));
				return false;
			}
			if (!Enum.IsDefined(typeof(GlobalKeys), num2))
			{
				ZLog.LogError(string.Format("Failed to parse key {0} as {1}: {2} is out of range!", keyValuePair.Key, "GlobalKeys", num2));
			}
			array[num] = (GlobalKeys)num2;
			array2[num] = keyValuePair.Value;
			num++;
		}
		for (int i = 0; i < array.Length; i++)
		{
			GlobalKeys globalKeys = array[i];
			string text = array2[i];
			if (string.IsNullOrEmpty(text))
			{
				result.Add(globalKeys.ToString());
			}
			else
			{
				if (globalKeys == GlobalKeys.Preset)
				{
					string text2 = "";
					int[] array3 = array2[i].AllIndicesOf(':');
					for (int j = 0; j < array3.Length + 1; j++)
					{
						int num3;
						if (j > 0)
						{
							text2 += ":";
							num3 = array3[j - 1] + 1;
						}
						else
						{
							num3 = 0;
						}
						int num4 = text.IndexOf('_', num3);
						int num5 = (j >= array3.Length) ? text.Length : array3[j];
						if (num4 >= num5)
						{
							ZLog.LogError("Failed to parse value " + text + "'s subkey as WorldModifiers and WorldModifierOption: separator index in wrong location!");
						}
						if (num4 < 0)
						{
							string text3 = text.Substring(num3, num5 - num3);
							int num6;
							if (!int.TryParse(text3, out num6))
							{
								ZLog.LogError(string.Concat(new string[]
								{
									"Failed to parse value ",
									text3,
									" as WorldPresets: ",
									text3,
									" could not be parsed as an integer!"
								}));
								return false;
							}
							if (!Enum.IsDefined(typeof(WorldPresets), num6))
							{
								ZLog.LogError(string.Format("Failed to parse value {0} as {1}: {2} is out of range!", text3, "WorldPresets", num6));
							}
							string str = text2;
							WorldPresets worldPresets = (WorldPresets)num6;
							text2 = str + worldPresets.ToString();
						}
						else
						{
							string text4 = text.Substring(num3, num4 - num3);
							string text5 = text.Substring(num4 + 1, num5 - (num4 + 1));
							int num7;
							if (!int.TryParse(text4, out num7))
							{
								ZLog.LogError(string.Concat(new string[]
								{
									"Failed to parse value ",
									text4,
									" as WorldModifiers: ",
									text4,
									" could not be parsed as an integer!"
								}));
								return false;
							}
							if (!Enum.IsDefined(typeof(WorldModifiers), num7))
							{
								ZLog.LogError(string.Format("Failed to parse value {0} as {1}: {2} is out of range!", text4, "WorldModifiers", num7));
							}
							int num8;
							if (!int.TryParse(text5, out num8))
							{
								ZLog.LogError(string.Concat(new string[]
								{
									"Failed to parse value ",
									text5,
									" as WorldModifierOption: ",
									text5,
									" could not be parsed as an integer!"
								}));
								return false;
							}
							if (!Enum.IsDefined(typeof(WorldModifierOption), num8))
							{
								ZLog.LogError(string.Format("Failed to parse value {0} as {1}: {2} is out of range!", text5, "WorldModifierOption", num8));
							}
							string str2 = text2;
							WorldModifiers worldModifiers = (WorldModifiers)num7;
							string str3 = worldModifiers.ToString();
							string str4 = "_";
							WorldModifierOption worldModifierOption = (WorldModifierOption)num8;
							text2 = str2 + str3 + str4 + worldModifierOption.ToString();
						}
					}
					text = text2;
				}
				result.Add(array[i].ToString() + " " + text);
			}
		}
		return true;
	}

	// Token: 0x060009E7 RID: 2535 RVA: 0x00057038 File Offset: 0x00055238
	private static bool TryMatch(List<string> keys, List<string> others)
	{
		if (others.Count != keys.Count)
		{
			return false;
		}
		for (int i = 0; i < keys.Count; i++)
		{
			keys[i] = keys[i].ToLower();
		}
		for (int j = 0; j < others.Count; j++)
		{
			if (!keys.Contains(others[j].ToLower()))
			{
				return false;
			}
		}
		return true;
	}

	// Token: 0x04000B69 RID: 2921
	private static List<string> m_tempKeys = new List<string>();

	// Token: 0x04000B6A RID: 2922
	public static ServerOptionsGUI m_instance;

	// Token: 0x04000B6B RID: 2923
	public RectTransform m_toolTipPanel;

	// Token: 0x04000B6C RID: 2924
	public TMP_Text m_toolTipText;

	// Token: 0x04000B6D RID: 2925
	public GameObject m_modifiersRoot;

	// Token: 0x04000B6E RID: 2926
	public GameObject m_presetsRoot;

	// Token: 0x04000B6F RID: 2927
	public GameObject m_doneButton;

	// Token: 0x04000B70 RID: 2928
	private WorldPresets m_preset;

	// Token: 0x04000B71 RID: 2929
	private static KeyUI[] m_modifiers;

	// Token: 0x04000B72 RID: 2930
	private static KeyButton[] m_presets;

	// Token: 0x04000B73 RID: 2931
	private static Dictionary<WorldModifiers, List<WorldModifierOption>> m_modifierSetups = new Dictionary<WorldModifiers, List<WorldModifierOption>>();
}
