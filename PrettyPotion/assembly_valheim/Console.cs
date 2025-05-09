using System;
using TMPro;
using UnityEngine;

// Token: 0x02000072 RID: 114
public class Console : Terminal
{
	// Token: 0x17000025 RID: 37
	// (get) Token: 0x06000736 RID: 1846 RVA: 0x0003B923 File Offset: 0x00039B23
	public static global::Console instance
	{
		get
		{
			return global::Console.m_instance;
		}
	}

	// Token: 0x06000737 RID: 1847 RVA: 0x0003B92C File Offset: 0x00039B2C
	public override void Awake()
	{
		base.LoadQuickSelect();
		base.Awake();
		global::Console.m_instance = this;
		base.AddString(string.Concat(new string[]
		{
			"Valheim ",
			global::Version.GetVersionString(false),
			" (network version ",
			34U.ToString(),
			")"
		}));
		base.AddString("");
		base.AddString("type \"help\" - for commands");
		base.AddString("");
		this.m_chatWindow.gameObject.SetActive(false);
	}

	// Token: 0x06000738 RID: 1848 RVA: 0x0003B9BC File Offset: 0x00039BBC
	public override void Update()
	{
		this.m_focused = false;
		if (ZNet.instance && ZNet.instance.InPasswordDialog())
		{
			this.m_chatWindow.gameObject.SetActive(false);
			return;
		}
		if (!this.IsConsoleEnabled())
		{
			return;
		}
		if (ZInput.GetButtonDown("Console") || (global::Console.IsVisible() && ZInput.GetKeyDown(KeyCode.Escape, true)) || (global::Console.IsVisible() && ZInput.GetButtonDown("JoyButtonB")) || (ZInput.GetButton("JoyLTrigger") && ZInput.GetButton("JoyLBumper") && ZInput.GetButtonDown("JoyStart")))
		{
			this.m_chatWindow.gameObject.SetActive(!this.m_chatWindow.gameObject.activeSelf);
			if (ZInput.IsGamepadActive())
			{
				base.AddString("Gamepad console controls:\n   A: Enter text when empty (only in big picture mode), or send text when not.\n   LB: Erase.\n   DPad up/down: Cycle history.\n   DPad right: Autocomplete.\n   DPad left: Show commands (help).\n   Left Stick: Scroll.\n   RStick + LStick: show/hide console.\n   X+DPad: Save quick select option.\n   Y+DPad: Load quick select option.");
			}
			if (this.m_chatWindow.gameObject.activeInHierarchy)
			{
				this.m_input.ActivateInputField();
			}
		}
		if (this.m_chatWindow.gameObject.activeInHierarchy)
		{
			this.m_focused = true;
		}
		if (this.m_focused)
		{
			if (ZInput.GetButtonDown("JoyTabLeft") && this.m_input.text.Length > 0)
			{
				this.m_input.text = this.m_input.text.Substring(0, this.m_input.text.Length - 1);
			}
			else if (ZInput.GetButtonDown("JoyDPadLeft"))
			{
				base.TryRunCommand("help", false, false);
			}
		}
		string text;
		if (global::Console.instance && Terminal.m_threadSafeConsoleLog.TryDequeue(out text))
		{
			global::Console.instance.AddString(text);
		}
		string msg;
		if (Player.m_localPlayer && Terminal.m_threadSafeMessages.TryDequeue(out msg))
		{
			Player.m_localPlayer.Message(MessageHud.MessageType.TopLeft, msg, 0, null);
		}
		base.Update();
		if (ZInput.GetButtonDown("JoyDPadLeft") && !ZInput.GetButton("JoyButtonX") && !ZInput.GetButton("JoyButtonY"))
		{
			base.TryRunCommand("help", false, false);
		}
	}

	// Token: 0x06000739 RID: 1849 RVA: 0x0003BBB6 File Offset: 0x00039DB6
	public static bool IsVisible()
	{
		return global::Console.m_instance && global::Console.m_instance.m_chatWindow.gameObject.activeInHierarchy;
	}

	// Token: 0x0600073A RID: 1850 RVA: 0x0003BBDA File Offset: 0x00039DDA
	public void Print(string text)
	{
		base.AddString(text);
	}

	// Token: 0x0600073B RID: 1851 RVA: 0x0003BBE3 File Offset: 0x00039DE3
	public bool IsConsoleEnabled()
	{
		return global::Console.m_consoleEnabled;
	}

	// Token: 0x0600073C RID: 1852 RVA: 0x0003BBEA File Offset: 0x00039DEA
	public static void SetConsoleEnabled(bool enabled)
	{
		global::Console.m_consoleEnabled = enabled;
	}

	// Token: 0x17000026 RID: 38
	// (get) Token: 0x0600073D RID: 1853 RVA: 0x0003BBF2 File Offset: 0x00039DF2
	protected override Terminal m_terminalInstance
	{
		get
		{
			return global::Console.m_instance;
		}
	}

	// Token: 0x04000882 RID: 2178
	private static global::Console m_instance;

	// Token: 0x04000883 RID: 2179
	private static bool m_consoleEnabled;

	// Token: 0x04000884 RID: 2180
	public TMP_Text m_devTest;
}
