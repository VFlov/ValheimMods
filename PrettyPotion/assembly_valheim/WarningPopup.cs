using System;

// Token: 0x020000A6 RID: 166
public class WarningPopup : FixedPopupBase
{
	// Token: 0x06000A83 RID: 2691 RVA: 0x0005A749 File Offset: 0x00058949
	public WarningPopup(string header, string text, PopupButtonCallback okCallback, bool localizeText = true) : base(header, text, localizeText)
	{
		this.okCallback = okCallback;
	}

	// Token: 0x1700004C RID: 76
	// (get) Token: 0x06000A84 RID: 2692 RVA: 0x0005A75C File Offset: 0x0005895C
	public override PopupType Type
	{
		get
		{
			return PopupType.Warning;
		}
	}

	// Token: 0x04000C11 RID: 3089
	public readonly PopupButtonCallback okCallback;
}
