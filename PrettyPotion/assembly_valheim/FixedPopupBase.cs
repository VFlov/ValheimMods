using System;

// Token: 0x020000A1 RID: 161
public abstract class FixedPopupBase : PopupBase
{
	// Token: 0x06000A6E RID: 2670 RVA: 0x0005A683 File Offset: 0x00058883
	public FixedPopupBase(string header, string text, bool localizeText = true)
	{
		this.header = (localizeText ? Localization.instance.Localize(header) : header);
		this.text = (localizeText ? Localization.instance.Localize(text) : text);
	}

	// Token: 0x04000C05 RID: 3077
	public readonly string header;

	// Token: 0x04000C06 RID: 3078
	public readonly string text;
}
