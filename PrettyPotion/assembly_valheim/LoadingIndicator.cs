using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Token: 0x02000082 RID: 130
public class LoadingIndicator : MonoBehaviour
{
	// Token: 0x17000034 RID: 52
	// (get) Token: 0x06000863 RID: 2147 RVA: 0x00049FF0 File Offset: 0x000481F0
	public static bool IsCompletelyInvisible
	{
		get
		{
			return LoadingIndicator.s_instance == null || (LoadingIndicator.s_instance.m_spinnerVisibility == 0f && LoadingIndicator.s_instance.m_progressVisibility == 0f);
		}
	}

	// Token: 0x06000864 RID: 2148 RVA: 0x0004A028 File Offset: 0x00048228
	private void Awake()
	{
		ZLog.Log("Initializing loading indicator instance");
		if (LoadingIndicator.s_instance == null)
		{
			LoadingIndicator.s_instance = this;
		}
		else
		{
			ZLog.LogWarning("Loading indicator instance already set up! Not setting the instance.");
		}
		this.m_visible = this.m_visibleInitially;
		this.m_spinnerVisibility = (this.m_visible ? 1f : 0f);
		this.m_progressVisibility = ((this.m_visible && this.m_showProgressIndicator) ? 1f : 0f);
		this.m_text.text = "";
		this.m_progressIndicatorOriginalColor = this.m_progressIndicator.color;
		this.m_spinnerOriginalColor = this.m_spinner.color;
		this.m_backgroundOriginalColor = this.m_background.color;
		this.m_textOriginalColor = this.m_text.color;
		this.UpdateGUIVisibility();
	}

	// Token: 0x06000865 RID: 2149 RVA: 0x0004A100 File Offset: 0x00048300
	private void OnDestroy()
	{
		ZLog.Log("Destroying loading indicator instance");
		if (LoadingIndicator.s_instance == this)
		{
			LoadingIndicator.s_instance = null;
			return;
		}
		ZLog.LogWarning("Loading indicator instance did not match! Not removing the instance.");
	}

	// Token: 0x06000866 RID: 2150 RVA: 0x0004A12C File Offset: 0x0004832C
	private void LateUpdate()
	{
		float num = Mathf.Min(Time.deltaTime, this.m_maxDeltaTime);
		float num2 = this.m_visible ? 1f : 0f;
		float num3 = (this.m_visible && this.m_showProgressIndicator) ? 1f : 0f;
		bool flag = false;
		if (this.m_spinnerVisibility != num2)
		{
			if (this.m_visibilityFadeTime <= 0f)
			{
				this.m_spinnerVisibility = num2;
			}
			else
			{
				this.m_spinnerVisibility = Mathf.MoveTowards(this.m_spinnerVisibility, num2, num / this.m_visibilityFadeTime);
			}
			flag = true;
		}
		if (this.m_progressVisibility != num3)
		{
			if (this.m_visibilityFadeTime <= 0f)
			{
				this.m_progressVisibility = num3;
			}
			else
			{
				this.m_progressVisibility = Mathf.MoveTowards(this.m_progressVisibility, num3, num / this.m_visibilityFadeTime);
			}
			flag = true;
		}
		if (flag)
		{
			this.UpdateGUIVisibility();
		}
		float target = (this.m_progress < 1f) ? this.m_progress : 1.05f;
		this.m_progressIndicator.fillAmount = Mathf.Min(1f, Mathf.SmoothDamp(this.m_progressIndicator.fillAmount, target, ref this.m_progressSmoothVelocity, 0.2f, float.PositiveInfinity, num));
	}

	// Token: 0x06000867 RID: 2151 RVA: 0x0004A254 File Offset: 0x00048454
	private void UpdateGUIVisibility()
	{
		Color color = this.m_spinnerOriginalColor;
		color.a *= this.m_spinnerVisibility;
		this.m_spinner.color = color;
		color = this.m_progressIndicatorOriginalColor;
		color.a *= this.m_progressVisibility;
		this.m_progressIndicator.color = color;
		color = this.m_backgroundOriginalColor;
		color.a *= this.m_progressVisibility;
		this.m_background.color = color;
		color = this.m_textOriginalColor;
		color.a *= this.m_progressVisibility;
		this.m_text.color = color;
	}

	// Token: 0x06000868 RID: 2152 RVA: 0x0004A2F1 File Offset: 0x000484F1
	public static void SetVisibility(bool visible)
	{
		if (LoadingIndicator.s_instance == null)
		{
			return;
		}
		LoadingIndicator.s_instance.m_visible = visible;
	}

	// Token: 0x06000869 RID: 2153 RVA: 0x0004A30C File Offset: 0x0004850C
	public static void SetProgressVisibility(bool visible)
	{
		if (LoadingIndicator.s_instance == null)
		{
			return;
		}
		LoadingIndicator.s_instance.m_showProgressIndicator = visible;
	}

	// Token: 0x0600086A RID: 2154 RVA: 0x0004A327 File Offset: 0x00048527
	public static void SetProgress(float progress)
	{
		if (LoadingIndicator.s_instance == null)
		{
			ZLog.LogError("No instance when setting progress! Ignoring.");
			return;
		}
		LoadingIndicator.s_instance.m_progress = progress;
	}

	// Token: 0x0600086B RID: 2155 RVA: 0x0004A34C File Offset: 0x0004854C
	public static void SetText(string progressText)
	{
		if (LoadingIndicator.s_instance == null)
		{
			ZLog.LogError("No instance when setting text! Ignoring.");
			return;
		}
		if (progressText == null)
		{
			ZLog.LogError("Progress text was null!");
			return;
		}
		LoadingIndicator.s_instance.m_text.text = Localization.instance.Localize(progressText);
	}

	// Token: 0x04000A25 RID: 2597
	public static LoadingIndicator s_instance;

	// Token: 0x04000A26 RID: 2598
	[SerializeField]
	public bool m_showProgressIndicator = true;

	// Token: 0x04000A27 RID: 2599
	[SerializeField]
	private bool m_visibleInitially;

	// Token: 0x04000A28 RID: 2600
	[SerializeField]
	private float m_visibilityFadeTime = 0.2f;

	// Token: 0x04000A29 RID: 2601
	[SerializeField]
	private float m_maxDeltaTime = 0.033333335f;

	// Token: 0x04000A2A RID: 2602
	[SerializeField]
	private Image m_spinner;

	// Token: 0x04000A2B RID: 2603
	[SerializeField]
	private Image m_progressIndicator;

	// Token: 0x04000A2C RID: 2604
	[SerializeField]
	private Image m_background;

	// Token: 0x04000A2D RID: 2605
	[SerializeField]
	private TMP_Text m_text;

	// Token: 0x04000A2E RID: 2606
	private bool m_visible;

	// Token: 0x04000A2F RID: 2607
	private float m_progress;

	// Token: 0x04000A30 RID: 2608
	private float m_spinnerVisibility;

	// Token: 0x04000A31 RID: 2609
	private float m_progressVisibility;

	// Token: 0x04000A32 RID: 2610
	private float m_progressSmoothVelocity;

	// Token: 0x04000A33 RID: 2611
	private Color m_progressIndicatorOriginalColor;

	// Token: 0x04000A34 RID: 2612
	private Color m_spinnerOriginalColor;

	// Token: 0x04000A35 RID: 2613
	private Color m_backgroundOriginalColor;

	// Token: 0x04000A36 RID: 2614
	private Color m_textOriginalColor;
}
