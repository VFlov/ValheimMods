using System;

// Token: 0x020000A7 RID: 167
public class TaskPopup : FixedPopupBase
{
	// Token: 0x06000A85 RID: 2693 RVA: 0x0005A75F File Offset: 0x0005895F
	public TaskPopup(string header, string text, bool localizeText = true) : base(header, text, localizeText)
	{
	}

	// Token: 0x1700004D RID: 77
	// (get) Token: 0x06000A86 RID: 2694 RVA: 0x0005A76A File Offset: 0x0005896A
	public override PopupType Type
	{
		get
		{
			return PopupType.Task;
		}
	}
}
