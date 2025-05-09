using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Token: 0x020000A9 RID: 169
public class UnifiedPopup : MonoBehaviour
{
	// Token: 0x14000008 RID: 8
	// (add) Token: 0x06000A8A RID: 2698 RVA: 0x0005A7A0 File Offset: 0x000589A0
	// (remove) Token: 0x06000A8B RID: 2699 RVA: 0x0005A7D4 File Offset: 0x000589D4
	public static event UnifiedPopup.PopupEnabledHandler OnPopupEnabled;

	// Token: 0x06000A8C RID: 2700 RVA: 0x0005A808 File Offset: 0x00058A08
	private void Awake()
	{
		if (this.buttonLeft != null)
		{
			this.buttonLeftText = this.buttonLeft.GetComponentInChildren<TMP_Text>();
		}
		if (this.buttonCenter != null)
		{
			this.buttonCenterText = this.buttonCenter.GetComponentInChildren<TMP_Text>();
		}
		if (this.buttonRight != null)
		{
			this.buttonRightText = this.buttonRight.GetComponentInChildren<TMP_Text>();
		}
		this.Hide();
	}

	// Token: 0x06000A8D RID: 2701 RVA: 0x0005A878 File Offset: 0x00058A78
	private void OnEnable()
	{
		if (UnifiedPopup.instance != null && UnifiedPopup.instance != this)
		{
			ZLog.LogError("Can't have more than one UnifiedPopup component enabled at the same time!");
			return;
		}
		UnifiedPopup.instance = this;
		UnifiedPopup.PopupEnabledHandler onPopupEnabled = UnifiedPopup.OnPopupEnabled;
		if (onPopupEnabled == null)
		{
			return;
		}
		onPopupEnabled();
	}

	// Token: 0x06000A8E RID: 2702 RVA: 0x0005A8B4 File Offset: 0x00058AB4
	private void OnDisable()
	{
		if (UnifiedPopup.instance == null)
		{
			ZLog.LogError("Instance of UnifiedPopup was already null! This may have happened because you had more than one UnifiedPopup component enabled at the same time, which isn't allowed!");
			return;
		}
		UnifiedPopup.instance = null;
	}

	// Token: 0x06000A8F RID: 2703 RVA: 0x0005A8D4 File Offset: 0x00058AD4
	private void LateUpdate()
	{
		while (this.popupStack.Count > 0 && this.popupStack.Peek() is LivePopupBase && (this.popupStack.Peek() as LivePopupBase).ShouldClose)
		{
			UnifiedPopup.Pop();
		}
		if (!UnifiedPopup.IsVisible())
		{
			this.wasClosedThisFrame = false;
		}
	}

	// Token: 0x06000A90 RID: 2704 RVA: 0x0005A92D File Offset: 0x00058B2D
	private static bool InstanceIsNullError()
	{
		if (UnifiedPopup.instance == null)
		{
			ZLog.LogError("Can't show popup when there is no enabled UnifiedPopup component in the scene!");
			return true;
		}
		return false;
	}

	// Token: 0x06000A91 RID: 2705 RVA: 0x0005A949 File Offset: 0x00058B49
	public static bool IsAvailable()
	{
		return UnifiedPopup.instance != null;
	}

	// Token: 0x06000A92 RID: 2706 RVA: 0x0005A956 File Offset: 0x00058B56
	public static void Push(PopupBase popup)
	{
		if (UnifiedPopup.InstanceIsNullError())
		{
			return;
		}
		UnifiedPopup.instance.popupStack.Push(popup);
		UnifiedPopup.instance.ShowTopmost();
	}

	// Token: 0x06000A93 RID: 2707 RVA: 0x0005A97C File Offset: 0x00058B7C
	public static void Pop()
	{
		if (UnifiedPopup.InstanceIsNullError())
		{
			return;
		}
		if (UnifiedPopup.instance.popupStack.Count <= 0)
		{
			ZLog.LogError("Push/pop mismatch! Tried to pop a popup element off the stack when it was empty!");
			return;
		}
		PopupBase popupBase = UnifiedPopup.instance.popupStack.Pop();
		if (popupBase is LivePopupBase)
		{
			UnifiedPopup.instance.StopCoroutine((popupBase as LivePopupBase).updateCoroutine);
		}
		if (UnifiedPopup.instance.popupStack.Count <= 0)
		{
			UnifiedPopup.instance.Hide();
			return;
		}
		UnifiedPopup.instance.ShowTopmost();
	}

	// Token: 0x06000A94 RID: 2708 RVA: 0x0005AA04 File Offset: 0x00058C04
	public static void SetFocus()
	{
		if (UnifiedPopup.instance.buttonCenter != null && UnifiedPopup.instance.buttonCenter.gameObject.activeInHierarchy)
		{
			UnifiedPopup.instance.buttonCenter.Select();
			return;
		}
		if (UnifiedPopup.instance.buttonRight != null && UnifiedPopup.instance.buttonRight.gameObject.activeInHierarchy)
		{
			UnifiedPopup.instance.buttonRight.Select();
			return;
		}
		if (UnifiedPopup.instance.buttonLeft != null && UnifiedPopup.instance.buttonLeft.gameObject.activeInHierarchy)
		{
			UnifiedPopup.instance.buttonLeft.Select();
		}
	}

	// Token: 0x06000A95 RID: 2709 RVA: 0x0005AAB8 File Offset: 0x00058CB8
	public static bool IsVisible()
	{
		return UnifiedPopup.IsAvailable() && UnifiedPopup.instance.popupUIParent.activeInHierarchy;
	}

	// Token: 0x06000A96 RID: 2710 RVA: 0x0005AAD2 File Offset: 0x00058CD2
	public static bool WasVisibleThisFrame()
	{
		return UnifiedPopup.IsVisible() || (UnifiedPopup.IsAvailable() && UnifiedPopup.instance.wasClosedThisFrame);
	}

	// Token: 0x06000A97 RID: 2711 RVA: 0x0005AAF0 File Offset: 0x00058CF0
	private void ShowTopmost()
	{
		this.Show(UnifiedPopup.instance.popupStack.Peek());
	}

	// Token: 0x06000A98 RID: 2712 RVA: 0x0005AB08 File Offset: 0x00058D08
	private void Show(PopupBase popup)
	{
		this.ResetUI();
		switch (popup.Type)
		{
		case PopupType.YesNo:
			this.ShowYesNo(popup as YesNoPopup);
			break;
		case PopupType.Warning:
			this.ShowWarning(popup as WarningPopup);
			break;
		case PopupType.Task:
			this.ShowTask(popup as TaskPopup);
			break;
		case PopupType.CancelableTask:
			this.ShowCancelableTask(popup as CancelableTaskPopup);
			break;
		}
		this.popupUIParent.SetActive(true);
		this.popupUIParent.transform.parent.SetAsLastSibling();
	}

	// Token: 0x06000A99 RID: 2713 RVA: 0x0005AB94 File Offset: 0x00058D94
	private void ResetUI()
	{
		this.buttonLeft.onClick.RemoveAllListeners();
		this.buttonCenter.onClick.RemoveAllListeners();
		this.buttonRight.onClick.RemoveAllListeners();
		this.buttonLeft.gameObject.SetActive(false);
		this.buttonCenter.gameObject.SetActive(false);
		this.buttonRight.gameObject.SetActive(false);
	}

	// Token: 0x06000A9A RID: 2714 RVA: 0x0005AC04 File Offset: 0x00058E04
	private void ShowYesNo(YesNoPopup popup)
	{
		this.headerText.text = popup.header;
		this.bodyText.text = popup.text;
		this.buttonRightText.text = Localization.instance.Localize(this.yesText);
		this.buttonRight.gameObject.SetActive(true);
		this.buttonRight.onClick.AddListener(delegate()
		{
			PopupButtonCallback yesCallback = popup.yesCallback;
			if (yesCallback == null)
			{
				return;
			}
			yesCallback();
		});
		this.buttonLeftText.text = Localization.instance.Localize(this.noText);
		this.buttonLeft.gameObject.SetActive(true);
		this.buttonLeft.onClick.AddListener(delegate()
		{
			PopupButtonCallback noCallback = popup.noCallback;
			if (noCallback == null)
			{
				return;
			}
			noCallback();
		});
	}

	// Token: 0x06000A9B RID: 2715 RVA: 0x0005ACDC File Offset: 0x00058EDC
	private void ShowWarning(WarningPopup popup)
	{
		this.headerText.text = popup.header;
		this.bodyText.text = popup.text;
		this.buttonCenterText.text = Localization.instance.Localize(this.okText);
		this.buttonCenter.gameObject.SetActive(true);
		this.buttonCenter.onClick.AddListener(delegate()
		{
			PopupButtonCallback okCallback = popup.okCallback;
			if (okCallback == null)
			{
				return;
			}
			okCallback();
		});
	}

	// Token: 0x06000A9C RID: 2716 RVA: 0x0005AD6A File Offset: 0x00058F6A
	private void ShowTask(TaskPopup popup)
	{
		this.headerText.text = popup.header;
		this.bodyText.text = popup.text;
	}

	// Token: 0x06000A9D RID: 2717 RVA: 0x0005AD90 File Offset: 0x00058F90
	private void ShowCancelableTask(CancelableTaskPopup popup)
	{
		popup.SetTextReferences(this.headerText, this.bodyText);
		popup.SetUpdateCoroutineReference(base.StartCoroutine(popup.updateRoutine));
		this.buttonCenterText.text = Localization.instance.Localize(this.cancelText);
		this.buttonCenter.gameObject.SetActive(true);
		this.buttonCenter.onClick.AddListener(delegate()
		{
			PopupButtonCallback cancelCallback = popup.cancelCallback;
			if (cancelCallback != null)
			{
				cancelCallback();
			}
			this.StopCoroutine(popup.updateCoroutine);
		});
	}

	// Token: 0x06000A9E RID: 2718 RVA: 0x0005AE2C File Offset: 0x0005902C
	private void Hide()
	{
		this.wasClosedThisFrame = true;
		this.popupUIParent.SetActive(false);
	}

	// Token: 0x04000C13 RID: 3091
	private static UnifiedPopup instance;

	// Token: 0x04000C14 RID: 3092
	[Header("References")]
	[global::Tooltip("A reference to the parent object of the rest of the popup. This is what gets enabled and disabled to show and hide the popup.")]
	[SerializeField]
	private GameObject popupUIParent;

	// Token: 0x04000C15 RID: 3093
	[global::Tooltip("A reference to the left button of the popup, assigned to escape on keyboards and B on controllers. This usually gets assigned to \"back\", \"no\" or similar in dual-action popups. It's not necessary to assign buttons to any Unity Events - that is done automatically.")]
	[SerializeField]
	private Button buttonLeft;

	// Token: 0x04000C16 RID: 3094
	[global::Tooltip("A reference to the center button of the popup, assigned to enter on keyboards and A on controllers. This usually gets assigned to \"Ok\" or similar in single-action popups. It's not necessary to assign buttons to any Unity Events - that is done automatically.")]
	[SerializeField]
	private Button buttonCenter;

	// Token: 0x04000C17 RID: 3095
	[global::Tooltip("A reference to the right button of the popup, assigned to enter on keyboards and A on controllers. This usually gets assigned to \"yes\", \"accept\" or similar in dual-action popups. It's not necessary to assign buttons to any Unity Events - that is done automatically.")]
	[SerializeField]
	private Button buttonRight;

	// Token: 0x04000C18 RID: 3096
	[global::Tooltip("A reference to the header text of the popup.")]
	[SerializeField]
	private TextMeshProUGUI headerText;

	// Token: 0x04000C19 RID: 3097
	[global::Tooltip("A reference to the body text of the popup.")]
	[SerializeField]
	private TextMeshProUGUI bodyText;

	// Token: 0x04000C1A RID: 3098
	[Header("Button text")]
	[SerializeField]
	private string yesText = "$menu_yes";

	// Token: 0x04000C1B RID: 3099
	[SerializeField]
	private string noText = "$menu_no";

	// Token: 0x04000C1C RID: 3100
	[SerializeField]
	private string okText = "$menu_ok";

	// Token: 0x04000C1D RID: 3101
	[SerializeField]
	private string cancelText = "$menu_cancel";

	// Token: 0x04000C1E RID: 3102
	private TMP_Text buttonLeftText;

	// Token: 0x04000C1F RID: 3103
	private TMP_Text buttonCenterText;

	// Token: 0x04000C20 RID: 3104
	private TMP_Text buttonRightText;

	// Token: 0x04000C21 RID: 3105
	private bool wasClosedThisFrame;

	// Token: 0x04000C22 RID: 3106
	private Stack<PopupBase> popupStack = new Stack<PopupBase>();

	// Token: 0x020002C9 RID: 713
	// (Invoke) Token: 0x06002102 RID: 8450
	public delegate void PopupEnabledHandler();
}
