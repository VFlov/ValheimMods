using System;
using System.Collections;

// Token: 0x020000A8 RID: 168
public class CancelableTaskPopup : LivePopupBase
{
	// Token: 0x06000A87 RID: 2695 RVA: 0x0005A76D File Offset: 0x0005896D
	public CancelableTaskPopup(RetrieveFromStringSource headerRetrievalFunc, RetrieveFromStringSource textRetrievalFunc, RetrieveFromBoolSource shouldCloseRetrievalFunc, PopupButtonCallback cancelCallback) : base(headerRetrievalFunc, textRetrievalFunc, shouldCloseRetrievalFunc)
	{
		base.SetUpdateRoutine(this.UpdateRoutine());
		this.cancelCallback = cancelCallback;
	}

	// Token: 0x06000A88 RID: 2696 RVA: 0x0005A78C File Offset: 0x0005898C
	private IEnumerator UpdateRoutine()
	{
		while (!this.shouldCloseRetrievalFunc())
		{
			this.headerText.text = this.headerRetrievalFunc();
			this.bodyText.text = this.textRetrievalFunc();
			yield return null;
		}
		base.ShouldClose = true;
		yield break;
	}

	// Token: 0x1700004E RID: 78
	// (get) Token: 0x06000A89 RID: 2697 RVA: 0x0005A79B File Offset: 0x0005899B
	public override PopupType Type
	{
		get
		{
			return PopupType.CancelableTask;
		}
	}

	// Token: 0x04000C12 RID: 3090
	public readonly PopupButtonCallback cancelCallback;
}
