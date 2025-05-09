using System;
using System.Collections.Generic;
using UnityEngine;

// Token: 0x0200004A RID: 74
public class AnimationEffect : MonoBehaviour
{
	// Token: 0x060005DD RID: 1501 RVA: 0x00031D9E File Offset: 0x0002FF9E
	private void Start()
	{
		this.m_animator = base.GetComponent<Animator>();
	}

	// Token: 0x060005DE RID: 1502 RVA: 0x00031DAC File Offset: 0x0002FFAC
	public void Effect(AnimationEvent e)
	{
		string stringParameter = e.stringParameter;
		GameObject gameObject = e.objectReferenceParameter as GameObject;
		if (gameObject == null)
		{
			return;
		}
		Transform transform = null;
		if (stringParameter.Length > 0)
		{
			transform = Utils.FindChild(base.transform, stringParameter, Utils.IterativeSearchType.DepthFirst);
		}
		if (transform == null)
		{
			transform = (this.m_effectRoot ? this.m_effectRoot : base.transform);
		}
		UnityEngine.Object.Instantiate<GameObject>(gameObject, transform.position, transform.rotation);
	}

	// Token: 0x060005DF RID: 1503 RVA: 0x00031E28 File Offset: 0x00030028
	public void Attach(AnimationEvent e)
	{
		string stringParameter = e.stringParameter;
		GameObject gameObject = e.objectReferenceParameter as GameObject;
		bool flag = e.intParameter < 0;
		int intParameter = e.intParameter;
		bool flag2 = intParameter == 10 || intParameter == -10;
		if (gameObject == null)
		{
			return;
		}
		if (stringParameter == "")
		{
			ZLog.LogWarning("No joint name specified for Attach in animation " + e.animatorClipInfo.clip.name);
			return;
		}
		Transform transform = Utils.FindChild(base.transform, stringParameter, Utils.IterativeSearchType.DepthFirst);
		if (transform == null)
		{
			ZLog.LogWarning("Failed to find attach joint " + stringParameter + " for animation " + e.animatorClipInfo.clip.name);
			return;
		}
		this.ClearAttachment(transform);
		GameObject gameObject2 = UnityEngine.Object.Instantiate<GameObject>(gameObject, transform.position, transform.rotation);
		Vector3 localScale = gameObject2.transform.localScale;
		gameObject2.transform.SetParent(transform, true);
		if (flag2)
		{
			gameObject2.transform.localScale = localScale;
		}
		if (flag)
		{
			return;
		}
		if (this.m_attachments == null)
		{
			this.m_attachments = new List<GameObject>();
		}
		this.m_attachments.Add(gameObject2);
		this.m_attachStateHash = e.animatorStateInfo.fullPathHash;
		base.CancelInvoke("UpdateAttachments");
		base.InvokeRepeating("UpdateAttachments", 0.1f, 0.1f);
	}

	// Token: 0x060005E0 RID: 1504 RVA: 0x00031F94 File Offset: 0x00030194
	private void ClearAttachment(Transform parent)
	{
		if (this.m_attachments == null)
		{
			return;
		}
		foreach (GameObject gameObject in this.m_attachments)
		{
			if (gameObject && gameObject.transform.parent == parent)
			{
				this.m_attachments.Remove(gameObject);
				UnityEngine.Object.Destroy(gameObject);
				break;
			}
		}
	}

	// Token: 0x060005E1 RID: 1505 RVA: 0x0003201C File Offset: 0x0003021C
	public void RemoveAttachments()
	{
		if (this.m_attachments == null)
		{
			return;
		}
		foreach (GameObject obj in this.m_attachments)
		{
			UnityEngine.Object.Destroy(obj);
		}
		this.m_attachments.Clear();
	}

	// Token: 0x060005E2 RID: 1506 RVA: 0x00032080 File Offset: 0x00030280
	private void UpdateAttachments()
	{
		if (this.m_attachments != null && this.m_attachments.Count > 0)
		{
			if (this.m_attachStateHash != this.m_animator.GetCurrentAnimatorStateInfo(0).fullPathHash && this.m_attachStateHash != this.m_animator.GetNextAnimatorStateInfo(0).fullPathHash)
			{
				this.RemoveAttachments();
				return;
			}
		}
		else
		{
			base.CancelInvoke("UpdateAttachments");
		}
	}

	// Token: 0x0400069B RID: 1691
	public Transform m_effectRoot;

	// Token: 0x0400069C RID: 1692
	private Animator m_animator;

	// Token: 0x0400069D RID: 1693
	private List<GameObject> m_attachments;

	// Token: 0x0400069E RID: 1694
	private int m_attachStateHash;
}
