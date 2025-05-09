using System;
using System.Collections.Generic;
using GUIFramework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// Token: 0x0200009D RID: 157
public class UIGamePad : MonoBehaviour
{
	// Token: 0x06000A62 RID: 2658 RVA: 0x0005A310 File Offset: 0x00058510
	private void Start()
	{
		this.m_group = base.GetComponentInParent<UIGroupHandler>();
		this.m_button = base.GetComponent<Button>();
		this.m_submit = base.GetComponent<ISubmitHandler>();
		this.m_toggle = base.GetComponent<Toggle>();
		if (this.m_hint)
		{
			this.m_hint.SetActive(false);
		}
	}

	// Token: 0x06000A63 RID: 2659 RVA: 0x0005A368 File Offset: 0x00058568
	private bool IsInteractive()
	{
		if (this.m_button != null && !this.m_button.IsInteractable())
		{
			return false;
		}
		if (this.m_toggle)
		{
			if (!this.m_toggle.IsInteractable())
			{
				return false;
			}
			if (this.m_toggle.group && !this.m_toggle.group.allowSwitchOff && this.m_toggle.isOn)
			{
				return false;
			}
		}
		return (this.alternativeGroupHandler != null && this.alternativeGroupHandler.IsActive) || ((!this.m_group || this.m_group.IsActive) && (!(this.m_submit is GuiInputField) || (this.m_submit as GuiInputField).interactable));
	}

	// Token: 0x06000A64 RID: 2660 RVA: 0x0005A43C File Offset: 0x0005863C
	private void Update()
	{
		bool flag = this.IsInteractive();
		if (this.m_hint)
		{
			bool flag2 = ZInput.IsGamepadActive();
			bool flag3 = this.alternativeGroupHandler != null && this.alternativeGroupHandler.IsActive;
			this.m_hint.SetActive(flag && (flag2 || flag3));
		}
		if (flag && Time.frameCount - UIGamePad.m_lastInteractFrame >= 2 && this.ButtonPressed())
		{
			UIGamePad.m_lastInteractFrame = Time.frameCount;
			ZLog.Log("Button pressed " + base.gameObject.name + "  frame:" + Time.frameCount.ToString());
			if (this.m_button != null)
			{
				this.m_button.OnSubmit(null);
			}
			else if (this.m_submit != null)
			{
				this.m_submit.OnSubmit(null);
			}
			else if (this.m_toggle != null)
			{
				this.m_toggle.OnSubmit(null);
			}
		}
		this.m_blockedByLastFrame = this.m_blockNextFrame;
		this.m_blockNextFrame = false;
		if (this.m_blockingElements != null)
		{
			using (List<GameObject>.Enumerator enumerator = this.m_blockingElements.GetEnumerator())
			{
				while (enumerator.MoveNext())
				{
					if (enumerator.Current.gameObject.activeInHierarchy)
					{
						this.m_blockNextFrame = true;
					}
				}
			}
		}
	}

	// Token: 0x06000A65 RID: 2661 RVA: 0x0005A5A8 File Offset: 0x000587A8
	public bool ButtonPressed()
	{
		if (this.IsBlocked())
		{
			return false;
		}
		if (this.m_blockingElements != null)
		{
			using (List<GameObject>.Enumerator enumerator = this.m_blockingElements.GetEnumerator())
			{
				while (enumerator.MoveNext())
				{
					if (enumerator.Current.gameObject.activeInHierarchy)
					{
						return false;
					}
				}
			}
		}
		return (!string.IsNullOrEmpty(this.m_zinputKey) && ZInput.GetButtonDown(this.m_zinputKey)) || (this.m_keyCode != KeyCode.None && ZInput.GetKeyDown(this.m_keyCode, true));
	}

	// Token: 0x06000A66 RID: 2662 RVA: 0x0005A64C File Offset: 0x0005884C
	public bool IsBlocked()
	{
		return this.m_blockedByLastFrame || this.m_blockNextFrame || (global::Console.instance && global::Console.IsVisible());
	}

	// Token: 0x04000BF4 RID: 3060
	public KeyCode m_keyCode;

	// Token: 0x04000BF5 RID: 3061
	public string m_zinputKey;

	// Token: 0x04000BF6 RID: 3062
	public GameObject m_hint;

	// Token: 0x04000BF7 RID: 3063
	[global::Tooltip("The hotkey won't activate if any of these gameobjects are visible")]
	public List<GameObject> m_blockingElements;

	// Token: 0x04000BF8 RID: 3064
	private Button m_button;

	// Token: 0x04000BF9 RID: 3065
	private ISubmitHandler m_submit;

	// Token: 0x04000BFA RID: 3066
	private Toggle m_toggle;

	// Token: 0x04000BFB RID: 3067
	private UIGroupHandler m_group;

	// Token: 0x04000BFC RID: 3068
	[SerializeField]
	private UIGroupHandler alternativeGroupHandler;

	// Token: 0x04000BFD RID: 3069
	private bool m_blockedByLastFrame;

	// Token: 0x04000BFE RID: 3070
	private bool m_blockNextFrame;

	// Token: 0x04000BFF RID: 3071
	private static int m_lastInteractFrame;
}
