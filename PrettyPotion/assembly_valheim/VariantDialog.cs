using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// Token: 0x020000AA RID: 170
public class VariantDialog : MonoBehaviour
{
	// Token: 0x06000AA0 RID: 2720 RVA: 0x0005AE80 File Offset: 0x00059080
	public void Setup(ItemDrop.ItemData item)
	{
		base.gameObject.SetActive(true);
		foreach (GameObject obj in this.m_elements)
		{
			UnityEngine.Object.Destroy(obj);
		}
		this.m_elements.Clear();
		for (int i = 0; i < item.m_shared.m_variants; i++)
		{
			Sprite sprite = item.m_shared.m_icons[i];
			int num = i / this.m_gridWidth;
			int num2 = i % this.m_gridWidth;
			GameObject gameObject = UnityEngine.Object.Instantiate<GameObject>(this.m_elementPrefab, Vector3.zero, Quaternion.identity, this.m_listRoot);
			gameObject.SetActive(true);
			(gameObject.transform as RectTransform).anchoredPosition = new Vector2((float)num2 * this.m_spacing, (float)(-(float)num) * this.m_spacing);
			Button component = gameObject.transform.Find("Button").GetComponent<Button>();
			int buttonIndex = i;
			component.onClick.AddListener(delegate()
			{
				this.OnClicked(buttonIndex);
			});
			component.GetComponent<Image>().sprite = sprite;
			this.m_elements.Add(gameObject);
		}
	}

	// Token: 0x06000AA1 RID: 2721 RVA: 0x0005AFD0 File Offset: 0x000591D0
	public void OnClose()
	{
		base.gameObject.SetActive(false);
	}

	// Token: 0x06000AA2 RID: 2722 RVA: 0x0005AFDE File Offset: 0x000591DE
	private void OnClicked(int index)
	{
		ZLog.Log("Clicked button " + index.ToString());
		base.gameObject.SetActive(false);
		this.m_selected(index);
	}

	// Token: 0x04000C24 RID: 3108
	public Transform m_listRoot;

	// Token: 0x04000C25 RID: 3109
	public GameObject m_elementPrefab;

	// Token: 0x04000C26 RID: 3110
	public float m_spacing = 70f;

	// Token: 0x04000C27 RID: 3111
	public int m_gridWidth = 5;

	// Token: 0x04000C28 RID: 3112
	private List<GameObject> m_elements = new List<GameObject>();

	// Token: 0x04000C29 RID: 3113
	public Action<int> m_selected;
}
