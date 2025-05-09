using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// Token: 0x0200007F RID: 127
public class KeySlider : KeyUI
{
	// Token: 0x06000847 RID: 2119 RVA: 0x000496B4 File Offset: 0x000478B4
	public void Awake()
	{
		this.m_slider = base.GetComponentInParent<Slider>();
		this.m_slider.maxValue = (float)(this.m_settings.Count - 1);
		this.m_slider.value = (float)this.m_defaultIndex;
		this.m_slider.wholeNumbers = true;
		foreach (KeySlider.SliderSetting sliderSetting in this.m_settings)
		{
			for (int i = 0; i < sliderSetting.m_keys.Count; i++)
			{
				sliderSetting.m_keys[i] = sliderSetting.m_keys[i].ToLower();
			}
		}
		this.m_slider.onValueChanged.AddListener(new UnityAction<float>(this.OnValueChanged));
	}

	// Token: 0x06000848 RID: 2120 RVA: 0x00049794 File Offset: 0x00047994
	public override void Update()
	{
		if (this.m_nameLabel)
		{
			this.m_nameLabel.text = Localization.instance.Localize(this.Selected().m_name);
		}
		if (KeySlider.m_lastActiveSlider == this && KeyUI.m_lastKeyUI == this)
		{
			this.SetToolTip();
		}
		if (ZInput.IsGamepadActive() && EventSystem.current.currentSelectedGameObject != base.gameObject && KeySlider.m_lastActiveSlider == this)
		{
			KeySlider.m_lastActiveSlider = null;
		}
		base.Update();
	}

	// Token: 0x06000849 RID: 2121 RVA: 0x00049825 File Offset: 0x00047A25
	public void OnValueChanged(float f)
	{
		base.OnValueChanged();
		this.m_manualSet = true;
	}

	// Token: 0x0600084A RID: 2122 RVA: 0x00049834 File Offset: 0x00047A34
	public override void OnPointerEnter(PointerEventData eventData)
	{
		if (KeySlider.m_lastActiveSlider != this || KeyUI.m_lastKeyUI != this)
		{
			KeySlider.m_lastActiveSlider = this;
			this.m_lastToolTipUpdateValue = -1f;
		}
		base.OnPointerEnter(eventData);
	}

	// Token: 0x0600084B RID: 2123 RVA: 0x00049868 File Offset: 0x00047A68
	public void SetValue(WorldModifierOption value)
	{
		for (int i = 0; i < this.m_settings.Count; i++)
		{
			if (this.m_settings[i].m_modifierValue == value)
			{
				this.m_slider.value = (float)i;
				base.OnValueChanged();
				return;
			}
		}
		Terminal.LogError(string.Format("Slider {0} missing value to set: {1}", this.m_modifier, value));
	}

	// Token: 0x0600084C RID: 2124 RVA: 0x000498D3 File Offset: 0x00047AD3
	public WorldModifierOption GetValue()
	{
		return this.m_settings[(int)this.m_slider.value].m_modifierValue;
	}

	// Token: 0x0600084D RID: 2125 RVA: 0x000498F4 File Offset: 0x00047AF4
	protected override void SetToolTip()
	{
		if (this.m_toolTipLabel)
		{
			if (KeySlider.m_lastActiveSlider != this || this.m_slider.value == this.m_lastToolTipUpdateValue)
			{
				return;
			}
			this.m_lastToolTipUpdateValue = this.m_slider.value;
			string text = "";
			if (this.m_toolTip.Length > 0)
			{
				text = text + this.m_toolTip + "\n\n";
			}
			if (this.Selected().m_name.Length > 0 && this.Selected().m_toolTip.Length > 0)
			{
				text = text + "<color=orange>" + this.Selected().m_name + "</color>\n";
			}
			text += this.Selected().m_toolTip;
			if (text.Length > 0)
			{
				this.m_toolTipLabel.text = Localization.instance.Localize(text);
				this.m_toolTipLabel.gameObject.SetActive(true);
			}
		}
	}

	// Token: 0x0600084E RID: 2126 RVA: 0x000499ED File Offset: 0x00047BED
	private KeySlider.SliderSetting Selected()
	{
		return this.m_settings[(int)this.m_slider.value];
	}

	// Token: 0x0600084F RID: 2127 RVA: 0x00049A08 File Offset: 0x00047C08
	public override void SetKeys(World world)
	{
		foreach (string text in this.m_settings[(int)this.m_slider.value].m_keys)
		{
			string text2 = text.ToLower();
			if (ZoneSystem.instance)
			{
				ZoneSystem.instance.SetGlobalKey(text2);
			}
			else if (!world.m_startingGlobalKeys.Contains(text2))
			{
				string b = text2.Split(' ', StringSplitOptions.None)[0].ToLower();
				for (int i = world.m_startingGlobalKeys.Count - 1; i >= 0; i--)
				{
					if (world.m_startingGlobalKeys[i].Split(' ', StringSplitOptions.None)[0].ToLower() == b)
					{
						world.m_startingGlobalKeys.RemoveAt(i);
					}
				}
				world.m_startingGlobalKeys.Add(text2);
			}
		}
	}

	// Token: 0x06000850 RID: 2128 RVA: 0x00049B00 File Offset: 0x00047D00
	public override bool TryMatch(World world, bool checkAllKeys = false)
	{
		for (int i = 0; i < this.m_settings.Count; i++)
		{
			bool flag = false;
			KeySlider.SliderSetting sliderSetting = this.m_settings[i];
			if (sliderSetting.m_keys.Count == 0)
			{
				this.m_slider.value = (float)i;
				flag = true;
				if (world.m_startingGlobalKeys.Count == 0)
				{
					return true;
				}
			}
			else
			{
				foreach (string text in sliderSetting.m_keys)
				{
					string text2;
					GlobalKeys globalKeys;
					ZoneSystem.GetKeyValue(text, out text2, out globalKeys);
					if (!world.m_startingGlobalKeys.Contains(text.ToLower()))
					{
						flag = true;
						break;
					}
				}
				if (checkAllKeys)
				{
					foreach (string text3 in world.m_startingGlobalKeys)
					{
						GlobalKeys globalKeys;
						string text4;
						GlobalKeys globalKeys2;
						if (Enum.TryParse<GlobalKeys>(ZoneSystem.GetKeyValue(text3, out text4, out globalKeys), true, out globalKeys2) && globalKeys2 < GlobalKeys.NonServerOption && !sliderSetting.m_keys.Contains(text3.ToLower()))
						{
							flag = true;
							break;
						}
					}
				}
				if (!flag)
				{
					this.m_slider.value = (float)i;
					return true;
				}
			}
		}
		return false;
	}

	// Token: 0x06000851 RID: 2129 RVA: 0x00049C58 File Offset: 0x00047E58
	public override bool TryMatch(List<string> keys, out string label, bool setSlider = true)
	{
		for (int i = 0; i < keys.Count; i++)
		{
			keys[i] = keys[i].ToLower();
		}
		int num = 0;
		int num2 = -1;
		int num3 = 0;
		for (int j = 0; j < this.m_settings.Count; j++)
		{
			bool flag = false;
			KeySlider.SliderSetting sliderSetting = this.m_settings[j];
			if (sliderSetting.m_keys.Count == 0)
			{
				num = j;
				flag = true;
			}
			foreach (string text in sliderSetting.m_keys)
			{
				string text2;
				GlobalKeys globalKeys;
				ZoneSystem.GetKeyValue(text, out text2, out globalKeys);
				if (!keys.Contains(text.ToLower()))
				{
					flag = true;
					break;
				}
			}
			if (!flag && sliderSetting.m_keys.Count >= num3)
			{
				num2 = j;
				num3 = sliderSetting.m_keys.Count;
			}
		}
		if (num2 >= 0)
		{
			if (setSlider)
			{
				this.m_slider.value = (float)num2;
			}
			label = this.m_modifier.GetDisplayString() + ": " + this.m_settings[num2].m_name;
			return true;
		}
		if (setSlider)
		{
			this.m_slider.value = (float)num;
		}
		label = null;
		return false;
	}

	// Token: 0x04000A15 RID: 2581
	private Slider m_slider;

	// Token: 0x04000A16 RID: 2582
	public TMP_Text m_nameLabel;

	// Token: 0x04000A17 RID: 2583
	public TMP_Text m_toolTipLabel;

	// Token: 0x04000A18 RID: 2584
	public string m_toolTip;

	// Token: 0x04000A19 RID: 2585
	public int m_defaultIndex;

	// Token: 0x04000A1A RID: 2586
	public WorldModifiers m_modifier;

	// Token: 0x04000A1B RID: 2587
	public List<KeySlider.SliderSetting> m_settings;

	// Token: 0x04000A1C RID: 2588
	[HideInInspector]
	public bool m_manualSet;

	// Token: 0x04000A1D RID: 2589
	private float m_lastToolTipUpdateValue = -1f;

	// Token: 0x04000A1E RID: 2590
	public static KeySlider m_lastActiveSlider;

	// Token: 0x02000284 RID: 644
	[Serializable]
	public class SliderSetting
	{
		// Token: 0x040021D5 RID: 8661
		public string m_name;

		// Token: 0x040021D6 RID: 8662
		public string m_toolTip;

		// Token: 0x040021D7 RID: 8663
		public WorldModifierOption m_modifierValue;

		// Token: 0x040021D8 RID: 8664
		public List<string> m_keys = new List<string>();
	}
}
