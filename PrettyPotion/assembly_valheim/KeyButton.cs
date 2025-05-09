using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// Token: 0x0200007C RID: 124
public class KeyButton : MonoBehaviour, IPointerEnterHandler, IEventSystemHandler
{
	// Token: 0x06000832 RID: 2098 RVA: 0x00048A1F File Offset: 0x00046C1F
	public void Awake()
	{
		this.m_button = base.GetComponentInParent<Button>();
		this.m_button.onClick.AddListener(new UnityAction(this.OnClick));
	}

	// Token: 0x06000833 RID: 2099 RVA: 0x00048A49 File Offset: 0x00046C49
	private void Update()
	{
		if (ZInput.IsGamepadActive() && EventSystem.current.currentSelectedGameObject == this.m_button.gameObject)
		{
			this.UpdateTooltip();
		}
	}

	// Token: 0x06000834 RID: 2100 RVA: 0x00048A74 File Offset: 0x00046C74
	public void OnPointerEnter(PointerEventData eventData)
	{
		this.UpdateTooltip();
	}

	// Token: 0x06000835 RID: 2101 RVA: 0x00048A7C File Offset: 0x00046C7C
	private void UpdateTooltip()
	{
		KeyUI.m_lastKeyUI = null;
		if (this.m_toolTipLabel)
		{
			this.m_toolTipLabel.text = Localization.instance.Localize(this.m_toolTip);
		}
	}

	// Token: 0x06000836 RID: 2102 RVA: 0x00048AAC File Offset: 0x00046CAC
	public void SetKeys(World world)
	{
		foreach (string text in this.m_keys)
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
		ServerOptionsGUI.m_instance.OnPresetButton(this);
	}

	// Token: 0x06000837 RID: 2103 RVA: 0x00048B9C File Offset: 0x00046D9C
	public bool TryMatch(List<string> keys)
	{
		for (int i = 0; i < keys.Count; i++)
		{
			keys[i] = keys[i].ToLower();
		}
		for (int j = 0; j < this.m_keys.Count; j++)
		{
			if (!keys.Contains(this.m_keys[j].ToLower()))
			{
				return false;
			}
		}
		return true;
	}

	// Token: 0x06000838 RID: 2104 RVA: 0x00048BFF File Offset: 0x00046DFF
	public string GetName()
	{
		return base.gameObject.GetComponentInChildren<TMP_Text>().text;
	}

	// Token: 0x06000839 RID: 2105 RVA: 0x00048C11 File Offset: 0x00046E11
	private void OnClick()
	{
		ServerOptionsGUI.m_instance.OnPresetButton(this);
	}

	// Token: 0x040009EC RID: 2540
	private Button m_button;

	// Token: 0x040009ED RID: 2541
	public TMP_Text m_toolTipLabel;

	// Token: 0x040009EE RID: 2542
	public string m_toolTip;

	// Token: 0x040009EF RID: 2543
	public WorldPresets m_preset;

	// Token: 0x040009F0 RID: 2544
	public List<string> m_keys;
}
