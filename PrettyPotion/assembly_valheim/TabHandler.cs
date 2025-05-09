using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

// Token: 0x02000096 RID: 150
public class TabHandler : MonoBehaviour
{
	// Token: 0x06000A24 RID: 2596 RVA: 0x00058C69 File Offset: 0x00056E69
	private void Start()
	{
		this.Init(true);
	}

	// Token: 0x06000A25 RID: 2597 RVA: 0x00058C74 File Offset: 0x00056E74
	public void Init(bool forceSelect = false)
	{
		int num = -1;
		for (int i = 0; i < this.m_tabs.Count; i++)
		{
			TabHandler.Tab tab = this.m_tabs[i];
			if (tab.m_button)
			{
				tab.m_button.onClick.AddListener(delegate()
				{
					this.OnClick(tab.m_button);
				});
				Transform transform = tab.m_button.gameObject.transform.Find("Selected");
				if (transform)
				{
					TMP_Text componentInChildren = transform.GetComponentInChildren<TMP_Text>();
					TMP_Text componentInChildren2 = tab.m_button.GetComponentInChildren<TMP_Text>();
					string text = null;
					if (componentInChildren2 != null)
					{
						text = componentInChildren2.text;
					}
					else
					{
						TextMeshProUGUI componentInChildren3 = tab.m_button.GetComponentInChildren<TextMeshProUGUI>();
						if (componentInChildren3 != null)
						{
							text = componentInChildren3.text;
						}
					}
					if (componentInChildren != null)
					{
						componentInChildren.text = text;
					}
					else
					{
						TextMeshProUGUI componentInChildren4 = transform.GetComponentInChildren<TextMeshProUGUI>();
						if (componentInChildren4 != null)
						{
							componentInChildren4.text = text;
						}
					}
				}
				if (tab.m_default)
				{
					num = i;
				}
			}
		}
		if (num >= 0)
		{
			this.SetActiveTab(num, forceSelect, true);
		}
		this.gamePad = base.GetComponent<UIGamePad>();
	}

	// Token: 0x06000A26 RID: 2598 RVA: 0x00058DD0 File Offset: 0x00056FD0
	private void Update()
	{
		if (UnifiedPopup.IsVisible())
		{
			return;
		}
		int num = 0;
		if (this.m_gamepadInput && (this.gamePad == null || !this.gamePad.IsBlocked()))
		{
			if (!string.IsNullOrEmpty(this.m_gamepadNavigateLeft) && ZInput.GetButtonDown(this.m_gamepadNavigateLeft))
			{
				num = -1;
			}
			else if (!string.IsNullOrEmpty(this.m_gamepadNavigateRight) && ZInput.GetButtonDown(this.m_gamepadNavigateRight))
			{
				num = 1;
			}
		}
		if (this.m_keybaordInput)
		{
			if (!string.IsNullOrEmpty(this.m_keyboardNavigateLeft) && ZInput.GetButtonDown(this.m_keyboardNavigateLeft))
			{
				num = -1;
			}
			else if (!string.IsNullOrEmpty(this.m_keyboardNavigateRight) && ZInput.GetButtonDown(this.m_keyboardNavigateRight))
			{
				num = 1;
			}
		}
		if (this.m_tabKeyInput && ZInput.GetKeyDown(KeyCode.Tab, true))
		{
			num = 1;
		}
		if (num == 0)
		{
			return;
		}
		int num2 = this.m_selected + num;
		if (!this.m_cycling)
		{
			this.SetActiveTab(Math.Max(0, Math.Min(this.m_tabs.Count - 1, num2)), false, true);
			return;
		}
		if (num2 < 0)
		{
			num2 = this.m_tabs.Count - 1;
		}
		else if (num2 > this.m_tabs.Count - 1)
		{
			num2 = 0;
		}
		if (!this.m_tabs[num2].m_button)
		{
			for (int i = num2 + num; i <= this.m_tabs.Count; i += num)
			{
				if (i == num2)
				{
					return;
				}
				if (i >= this.m_tabs.Count)
				{
					i = 0;
				}
				if (this.m_tabs[i].m_button)
				{
					this.SetActiveTab(i, false, true);
					return;
				}
			}
			return;
		}
		this.SetActiveTab(num2, false, true);
	}

	// Token: 0x06000A27 RID: 2599 RVA: 0x00058F6B File Offset: 0x0005716B
	private void OnClick(Button button)
	{
		this.SetActiveTab(button);
	}

	// Token: 0x06000A28 RID: 2600 RVA: 0x00058F74 File Offset: 0x00057174
	private void SetActiveTab(Button button)
	{
		for (int i = 0; i < this.m_tabs.Count; i++)
		{
			if (!(this.m_tabs[i].m_button == null) && !(this.m_tabs[i].m_button != button))
			{
				this.SetActiveTab(i, false, true);
				return;
			}
		}
	}

	// Token: 0x06000A29 RID: 2601 RVA: 0x00058FD4 File Offset: 0x000571D4
	public void SetActiveTab(int index, bool forceSelect = false, bool invokeOnClick = true)
	{
		if (!forceSelect && this.m_selected == index)
		{
			return;
		}
		this.m_selected = (this.m_tabs[index].m_button ? index : this.m_selected);
		for (int i = 0; i < this.m_tabs.Count; i++)
		{
			TabHandler.Tab tab = this.m_tabs[i];
			bool flag = i == index;
			if (tab.m_page != null)
			{
				tab.m_page.gameObject.SetActive(flag);
			}
			if (tab.m_button)
			{
				tab.m_button.interactable = !flag;
				Transform transform = tab.m_button.gameObject.transform.Find("Selected");
				if (transform)
				{
					transform.gameObject.SetActive(i == this.m_selected);
				}
				if (flag && invokeOnClick)
				{
					UnityEvent onClick = tab.m_onClick;
					if (onClick != null)
					{
						onClick.Invoke();
					}
				}
			}
		}
		if (ZInput.IsGamepadActive())
		{
			EffectList setActiveTabEffects = this.m_setActiveTabEffects;
			if (setActiveTabEffects != null)
			{
				setActiveTabEffects.Create((Player.m_localPlayer != null) ? Player.m_localPlayer.transform.position : Vector3.zero, Quaternion.identity, null, 1f, -1);
			}
		}
		Action<int> activeTabChanged = this.ActiveTabChanged;
		if (activeTabChanged == null)
		{
			return;
		}
		activeTabChanged(index);
	}

	// Token: 0x06000A2A RID: 2602 RVA: 0x00059124 File Offset: 0x00057324
	public void SetActiveTabWithoutInvokingOnClick(int index)
	{
		this.m_selected = (this.m_tabs[index].m_button ? index : this.m_selected);
		for (int i = 0; i < this.m_tabs.Count; i++)
		{
			TabHandler.Tab tab = this.m_tabs[i];
			bool flag = i == index;
			if (tab.m_page != null)
			{
				tab.m_page.gameObject.SetActive(flag);
			}
			if (tab.m_button)
			{
				tab.m_button.interactable = !flag;
				Transform transform = tab.m_button.gameObject.transform.Find("Selected");
				if (transform)
				{
					transform.gameObject.SetActive(i == this.m_selected);
				}
			}
		}
		Action<int> activeTabChanged = this.ActiveTabChanged;
		if (activeTabChanged == null)
		{
			return;
		}
		activeTabChanged(index);
	}

	// Token: 0x06000A2B RID: 2603 RVA: 0x00059209 File Offset: 0x00057409
	public int GetActiveTab()
	{
		return this.m_selected;
	}

	// Token: 0x14000007 RID: 7
	// (add) Token: 0x06000A2C RID: 2604 RVA: 0x00059214 File Offset: 0x00057414
	// (remove) Token: 0x06000A2D RID: 2605 RVA: 0x0005924C File Offset: 0x0005744C
	public event Action<int> ActiveTabChanged;

	// Token: 0x04000BB0 RID: 2992
	public bool m_cycling = true;

	// Token: 0x04000BB1 RID: 2993
	public bool m_tabKeyInput = true;

	// Token: 0x04000BB2 RID: 2994
	public bool m_keybaordInput;

	// Token: 0x04000BB3 RID: 2995
	public string m_keyboardNavigateLeft = "TabLeft";

	// Token: 0x04000BB4 RID: 2996
	public string m_keyboardNavigateRight = "TabRight";

	// Token: 0x04000BB5 RID: 2997
	public bool m_gamepadInput;

	// Token: 0x04000BB6 RID: 2998
	public string m_gamepadNavigateLeft = "JoyTabLeft";

	// Token: 0x04000BB7 RID: 2999
	public string m_gamepadNavigateRight = "JoyTabRight";

	// Token: 0x04000BB8 RID: 3000
	public List<TabHandler.Tab> m_tabs = new List<TabHandler.Tab>();

	// Token: 0x04000BB9 RID: 3001
	[Header("Effects")]
	public EffectList m_setActiveTabEffects = new EffectList();

	// Token: 0x04000BBA RID: 3002
	private int m_selected;

	// Token: 0x04000BBB RID: 3003
	private UIGamePad gamePad;

	// Token: 0x020002BF RID: 703
	[Serializable]
	public class Tab
	{
		// Token: 0x040022BD RID: 8893
		public Button m_button;

		// Token: 0x040022BE RID: 8894
		public RectTransform m_page;

		// Token: 0x040022BF RID: 8895
		public bool m_default;

		// Token: 0x040022C0 RID: 8896
		public UnityEvent m_onClick;
	}
}
