using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Token: 0x0200007B RID: 123
public class JoinCode : MonoBehaviour
{
	// Token: 0x06000827 RID: 2087 RVA: 0x0004864D File Offset: 0x0004684D
	public static void Show(bool firstSpawn = false)
	{
		if (JoinCode.m_instance != null)
		{
			JoinCode.m_instance.Activate(firstSpawn);
		}
	}

	// Token: 0x06000828 RID: 2088 RVA: 0x00048667 File Offset: 0x00046867
	public static void Hide()
	{
		if (JoinCode.m_instance != null)
		{
			JoinCode.m_instance.Deactivate();
		}
	}

	// Token: 0x06000829 RID: 2089 RVA: 0x00048680 File Offset: 0x00046880
	private void Start()
	{
		JoinCode.m_instance = this;
		this.m_textAlpha = this.m_text.color.a;
		this.m_darkenAlpha = this.m_darken.GetAlpha();
		this.Deactivate();
	}

	// Token: 0x0600082A RID: 2090 RVA: 0x000486B8 File Offset: 0x000468B8
	private void Init()
	{
		if (this.m_initialized)
		{
			return;
		}
		if (ZNet.m_onlineBackend == OnlineBackendType.PlayFab)
		{
			this.m_joinCode = ZPlayFabMatchmaking.JoinCode;
			this.m_root.SetActive(this.m_joinCode.Length > 0);
		}
		else
		{
			this.m_root.SetActive(false);
		}
		this.m_initialized = true;
	}

	// Token: 0x0600082B RID: 2091 RVA: 0x00048710 File Offset: 0x00046910
	private void Activate(bool firstSpawn)
	{
		this.Init();
		if (ZNet.m_onlineBackend == OnlineBackendType.PlayFab)
		{
			this.m_joinCode = ZPlayFabMatchmaking.JoinCode;
		}
		this.ResetAlpha();
		this.m_root.SetActive(this.m_joinCode.Length > 0);
		this.m_inMenu = !firstSpawn;
		this.m_isVisible = (firstSpawn ? this.m_firstShowDuration : 0f);
	}

	// Token: 0x0600082C RID: 2092 RVA: 0x00048775 File Offset: 0x00046975
	public void Deactivate()
	{
		this.m_root.SetActive(false);
		this.m_inMenu = false;
		this.m_isVisible = 0f;
	}

	// Token: 0x0600082D RID: 2093 RVA: 0x00048798 File Offset: 0x00046998
	private void ResetAlpha()
	{
		Color color = this.m_text.color;
		color.a = this.m_textAlpha;
		this.m_text.color = color;
		this.m_darken.SetAlpha(this.m_darkenAlpha);
	}

	// Token: 0x0600082E RID: 2094 RVA: 0x000487DC File Offset: 0x000469DC
	private void Update()
	{
		if (this.m_inMenu || this.m_isVisible > 0f)
		{
			this.m_btn.gameObject.GetComponentInChildren<TMP_Text>().text = Localization.instance.Localize("$menu_joincode", new string[]
			{
				this.m_joinCode
			});
			if (this.m_inMenu)
			{
				if (Settings.instance == null && (Menu.instance == null || (!Menu.instance.m_logoutDialog.gameObject.activeSelf && !Menu.instance.PlayerListActive)) && this.m_inputBlocked)
				{
					this.m_inputBlocked = false;
					return;
				}
				this.m_inputBlocked = (Settings.instance != null || (Menu.instance != null && (Menu.instance.m_logoutDialog.gameObject.activeSelf || Menu.instance.PlayerListActive)));
				if (this.m_inputBlocked)
				{
					return;
				}
				if (Settings.instance == null && (ZInput.GetButtonDown("JoyButtonX") || ZInput.GetKeyDown(KeyCode.J, true)))
				{
					this.CopyJoinCodeToClipboard();
					return;
				}
			}
			else
			{
				this.m_isVisible -= Time.deltaTime;
				if (this.m_isVisible < 0f)
				{
					JoinCode.Hide();
					return;
				}
				if (this.m_isVisible < this.m_fadeOutDuration)
				{
					float t = this.m_isVisible / this.m_fadeOutDuration;
					float a = Mathf.Lerp(0f, this.m_textAlpha, t);
					float alpha = Mathf.Lerp(0f, this.m_darkenAlpha, t);
					Color color = this.m_text.color;
					color.a = a;
					this.m_text.color = color;
					this.m_darken.SetAlpha(alpha);
				}
			}
		}
	}

	// Token: 0x0600082F RID: 2095 RVA: 0x0004899E File Offset: 0x00046B9E
	public void OnClick()
	{
		this.CopyJoinCodeToClipboard();
	}

	// Token: 0x06000830 RID: 2096 RVA: 0x000489A8 File Offset: 0x00046BA8
	private void CopyJoinCodeToClipboard()
	{
		Gogan.LogEvent("Screen", "CopyToClipboard", "JoinCode", 0L);
		GUIUtility.systemCopyBuffer = this.m_joinCode;
		if (MessageHud.instance != null)
		{
			MessageHud.instance.ShowMessage(MessageHud.MessageType.TopLeft, "$menu_joincode_copied", 0, null, false);
		}
	}

	// Token: 0x040009DE RID: 2526
	private static JoinCode m_instance;

	// Token: 0x040009DF RID: 2527
	public GameObject m_root;

	// Token: 0x040009E0 RID: 2528
	public Button m_btn;

	// Token: 0x040009E1 RID: 2529
	public TMP_Text m_text;

	// Token: 0x040009E2 RID: 2530
	public CanvasRenderer m_darken;

	// Token: 0x040009E3 RID: 2531
	public float m_firstShowDuration = 7f;

	// Token: 0x040009E4 RID: 2532
	public float m_fadeOutDuration = 3f;

	// Token: 0x040009E5 RID: 2533
	private bool m_initialized;

	// Token: 0x040009E6 RID: 2534
	private string m_joinCode = "";

	// Token: 0x040009E7 RID: 2535
	private float m_textAlpha;

	// Token: 0x040009E8 RID: 2536
	private float m_darkenAlpha;

	// Token: 0x040009E9 RID: 2537
	private float m_isVisible;

	// Token: 0x040009EA RID: 2538
	private bool m_inMenu;

	// Token: 0x040009EB RID: 2539
	private bool m_inputBlocked;
}
