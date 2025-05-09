using System;
using UnityEngine;

// Token: 0x0200000F RID: 15
public class AnimationObjectToggle : MonoBehaviour
{
	// Token: 0x06000106 RID: 262 RVA: 0x0000D618 File Offset: 0x0000B818
	private GameObject GetGameObject(string objectName)
	{
		if (this.m_parentTransform == null)
		{
			return base.transform.Find(objectName).gameObject;
		}
		return this.m_parentTransform.Find(objectName).gameObject;
	}

	// Token: 0x06000107 RID: 263 RVA: 0x0000D64B File Offset: 0x0000B84B
	private void HideObject(string objectName)
	{
		this.GetGameObject(objectName).SetActive(false);
	}

	// Token: 0x06000108 RID: 264 RVA: 0x0000D65A File Offset: 0x0000B85A
	private void ShowObject(string objectName)
	{
		this.GetGameObject(objectName).SetActive(true);
	}

	// Token: 0x0400021D RID: 541
	public Transform m_parentTransform;
}
