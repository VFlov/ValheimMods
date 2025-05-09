using System;
using UnityEngine;
using UnityEngine.UI;

// Token: 0x0200008E RID: 142
[RequireComponent(typeof(ScrollRect))]
public class ScrollRectEnsureVisible : MonoBehaviour
{
	// Token: 0x06000987 RID: 2439 RVA: 0x000532D1 File Offset: 0x000514D1
	private void Awake()
	{
		if (!this.mInitialized)
		{
			this.Initialize();
		}
	}

	// Token: 0x06000988 RID: 2440 RVA: 0x000532E4 File Offset: 0x000514E4
	private void Initialize()
	{
		this.mScrollRect = base.GetComponent<ScrollRect>();
		this.mScrollTransform = (this.mScrollRect.transform as RectTransform);
		this.mContent = this.mScrollRect.content;
		this.Reset();
		this.mInitialized = true;
	}

	// Token: 0x06000989 RID: 2441 RVA: 0x00053334 File Offset: 0x00051534
	public void CenterOnItem(RectTransform target)
	{
		if (!this.mInitialized)
		{
			this.Initialize();
		}
		Vector3 worldPointInWidget = this.GetWorldPointInWidget(this.mScrollTransform, this.GetWidgetWorldPoint(target));
		Vector3 vector = this.GetWorldPointInWidget(this.mScrollTransform, this.GetWidgetWorldPoint(this.maskTransform)) - worldPointInWidget;
		vector.z = 0f;
		if (!this.mScrollRect.horizontal)
		{
			vector.x = 0f;
		}
		if (!this.mScrollRect.vertical)
		{
			vector.y = 0f;
		}
		Vector2 b = new Vector2(vector.x / (this.mContent.rect.size.x - this.mScrollTransform.rect.size.x), vector.y / (this.mContent.rect.size.y - this.mScrollTransform.rect.size.y));
		Vector2 vector2 = this.mScrollRect.normalizedPosition - b;
		if (this.mScrollRect.movementType != ScrollRect.MovementType.Unrestricted)
		{
			vector2.x = Mathf.Clamp01(vector2.x);
			vector2.y = Mathf.Clamp01(vector2.y);
		}
		this.mScrollRect.normalizedPosition = vector2;
	}

	// Token: 0x0600098A RID: 2442 RVA: 0x0005348C File Offset: 0x0005168C
	private void Reset()
	{
		if (this.maskTransform == null)
		{
			Mask componentInChildren = base.GetComponentInChildren<Mask>(true);
			if (componentInChildren)
			{
				this.maskTransform = componentInChildren.rectTransform;
			}
			if (this.maskTransform == null)
			{
				RectMask2D componentInChildren2 = base.GetComponentInChildren<RectMask2D>(true);
				if (componentInChildren2)
				{
					this.maskTransform = componentInChildren2.rectTransform;
				}
			}
		}
	}

	// Token: 0x0600098B RID: 2443 RVA: 0x000534F0 File Offset: 0x000516F0
	private Vector3 GetWidgetWorldPoint(RectTransform target)
	{
		Vector3 b = new Vector3((0.5f - target.pivot.x) * target.rect.size.x, (0.5f - target.pivot.y) * target.rect.size.y, 0f);
		Vector3 position = target.localPosition + b;
		return target.parent.TransformPoint(position);
	}

	// Token: 0x0600098C RID: 2444 RVA: 0x0005356C File Offset: 0x0005176C
	private Vector3 GetWorldPointInWidget(RectTransform target, Vector3 worldPoint)
	{
		return target.InverseTransformPoint(worldPoint);
	}

	// Token: 0x04000B2B RID: 2859
	private RectTransform maskTransform;

	// Token: 0x04000B2C RID: 2860
	private ScrollRect mScrollRect;

	// Token: 0x04000B2D RID: 2861
	private RectTransform mScrollTransform;

	// Token: 0x04000B2E RID: 2862
	private RectTransform mContent;

	// Token: 0x04000B2F RID: 2863
	private bool mInitialized;
}
