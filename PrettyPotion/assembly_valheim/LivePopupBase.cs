using System;
using System.Collections;
using TMPro;
using UnityEngine;

// Token: 0x020000A4 RID: 164
public abstract class LivePopupBase : PopupBase
{
	// Token: 0x06000A77 RID: 2679 RVA: 0x0005A6B9 File Offset: 0x000588B9
	public LivePopupBase(RetrieveFromStringSource headerRetrievalFunc, RetrieveFromStringSource textRetrievalFunc, RetrieveFromBoolSource isActiveRetrievalFunc)
	{
		this.headerRetrievalFunc = headerRetrievalFunc;
		this.textRetrievalFunc = textRetrievalFunc;
		this.shouldCloseRetrievalFunc = isActiveRetrievalFunc;
	}

	// Token: 0x06000A78 RID: 2680 RVA: 0x0005A6D6 File Offset: 0x000588D6
	protected void SetUpdateRoutine(IEnumerator updateRoutine)
	{
		this.updateRoutine = updateRoutine;
	}

	// Token: 0x06000A79 RID: 2681 RVA: 0x0005A6DF File Offset: 0x000588DF
	public void SetUpdateCoroutineReference(Coroutine updateCoroutine)
	{
		this.updateCoroutine = updateCoroutine;
	}

	// Token: 0x06000A7A RID: 2682 RVA: 0x0005A6E8 File Offset: 0x000588E8
	public void SetTextReferences(TextMeshProUGUI headerText, TextMeshProUGUI bodyText)
	{
		this.headerText = headerText;
		this.bodyText = bodyText;
	}

	// Token: 0x17000048 RID: 72
	// (get) Token: 0x06000A7B RID: 2683 RVA: 0x0005A6F8 File Offset: 0x000588F8
	// (set) Token: 0x06000A7C RID: 2684 RVA: 0x0005A700 File Offset: 0x00058900
	public IEnumerator updateRoutine { get; private set; }

	// Token: 0x17000049 RID: 73
	// (get) Token: 0x06000A7D RID: 2685 RVA: 0x0005A709 File Offset: 0x00058909
	// (set) Token: 0x06000A7E RID: 2686 RVA: 0x0005A711 File Offset: 0x00058911
	public Coroutine updateCoroutine { get; private set; }

	// Token: 0x1700004A RID: 74
	// (get) Token: 0x06000A7F RID: 2687 RVA: 0x0005A71A File Offset: 0x0005891A
	// (set) Token: 0x06000A80 RID: 2688 RVA: 0x0005A722 File Offset: 0x00058922
	public bool ShouldClose { get; protected set; }

	// Token: 0x04000C07 RID: 3079
	protected TextMeshProUGUI headerText;

	// Token: 0x04000C08 RID: 3080
	protected TextMeshProUGUI bodyText;

	// Token: 0x04000C09 RID: 3081
	public readonly RetrieveFromStringSource headerRetrievalFunc;

	// Token: 0x04000C0A RID: 3082
	public readonly RetrieveFromStringSource textRetrievalFunc;

	// Token: 0x04000C0B RID: 3083
	public readonly RetrieveFromBoolSource shouldCloseRetrievalFunc;
}
