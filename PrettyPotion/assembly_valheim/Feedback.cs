using System;
using GUIFramework;
using UnityEngine;
using UnityEngine.UI;

// Token: 0x02000076 RID: 118
public class Feedback : MonoBehaviour
{
	// Token: 0x0600077B RID: 1915 RVA: 0x00040303 File Offset: 0x0003E503
	private void Awake()
	{
		Feedback.m_instance = this;
	}

	// Token: 0x0600077C RID: 1916 RVA: 0x0004030B File Offset: 0x0003E50B
	private void OnDestroy()
	{
		if (Feedback.m_instance == this)
		{
			Feedback.m_instance = null;
		}
	}

	// Token: 0x0600077D RID: 1917 RVA: 0x00040320 File Offset: 0x0003E520
	public static bool IsVisible()
	{
		return Feedback.m_instance != null;
	}

	// Token: 0x0600077E RID: 1918 RVA: 0x00040330 File Offset: 0x0003E530
	private void LateUpdate()
	{
		this.m_sendButton.interactable = this.IsValid();
		if (Feedback.IsVisible() && (ZInput.GetKeyDown(KeyCode.Escape, true) || (ZInput.GetButtonDown("JoyMenu") && (!ZInput.GetButton("JoyLTrigger") || !ZInput.GetButton("JoyLBumper")))))
		{
			this.OnBack();
		}
	}

	// Token: 0x0600077F RID: 1919 RVA: 0x00040389 File Offset: 0x0003E589
	private bool IsValid()
	{
		return this.m_subject.text.Length != 0 && this.m_text.text.Length != 0;
	}

	// Token: 0x06000780 RID: 1920 RVA: 0x000403B4 File Offset: 0x0003E5B4
	public void OnBack()
	{
		UnityEngine.Object.Destroy(base.gameObject);
	}

	// Token: 0x06000781 RID: 1921 RVA: 0x000403C4 File Offset: 0x0003E5C4
	public void OnSend()
	{
		if (!this.IsValid())
		{
			return;
		}
		string category = this.GetCategory();
		Gogan.LogEvent("Feedback_" + category, this.m_subject.text, this.m_text.text, 0L);
		UnityEngine.Object.Destroy(base.gameObject);
	}

	// Token: 0x06000782 RID: 1922 RVA: 0x00040414 File Offset: 0x0003E614
	private string GetCategory()
	{
		if (this.m_catBug.isOn)
		{
			return "Bug";
		}
		if (this.m_catFeedback.isOn)
		{
			return "Feedback";
		}
		if (this.m_catIdea.isOn)
		{
			return "Idea";
		}
		return "";
	}

	// Token: 0x040008BA RID: 2234
	private static Feedback m_instance;

	// Token: 0x040008BB RID: 2235
	public GuiInputField m_subject;

	// Token: 0x040008BC RID: 2236
	public GuiInputField m_text;

	// Token: 0x040008BD RID: 2237
	public Button m_sendButton;

	// Token: 0x040008BE RID: 2238
	public Toggle m_catBug;

	// Token: 0x040008BF RID: 2239
	public Toggle m_catFeedback;

	// Token: 0x040008C0 RID: 2240
	public Toggle m_catIdea;
}
