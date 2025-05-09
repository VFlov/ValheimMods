using System;

// Token: 0x020000A5 RID: 165
public class YesNoPopup : FixedPopupBase
{
	// Token: 0x06000A81 RID: 2689 RVA: 0x0005A72B File Offset: 0x0005892B
	public YesNoPopup(string header, string text, PopupButtonCallback yesCallback, PopupButtonCallback noCallback, bool localizeText = true) : base(header, text, localizeText)
	{
		this.yesCallback = yesCallback;
		this.noCallback = noCallback;
	}

	// Token: 0x1700004B RID: 75
	// (get) Token: 0x06000A82 RID: 2690 RVA: 0x0005A746 File Offset: 0x00058946
	public override PopupType Type
	{
		get
		{
			return PopupType.YesNo;
		}
	}

	// Token: 0x04000C0F RID: 3087
	public readonly PopupButtonCallback yesCallback;

	// Token: 0x04000C10 RID: 3088
	public readonly PopupButtonCallback noCallback;
}
