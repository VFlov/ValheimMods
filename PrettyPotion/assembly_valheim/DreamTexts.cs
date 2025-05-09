using System;
using System.Collections.Generic;
using UnityEngine;

// Token: 0x0200016D RID: 365
public class DreamTexts : MonoBehaviour
{
	// Token: 0x06001609 RID: 5641 RVA: 0x000A2900 File Offset: 0x000A0B00
	public DreamTexts.DreamText GetRandomDreamText()
	{
		List<DreamTexts.DreamText> list = new List<DreamTexts.DreamText>();
		foreach (DreamTexts.DreamText dreamText in this.m_texts)
		{
			if (this.HaveGlobalKeys(dreamText))
			{
				list.Add(dreamText);
			}
		}
		if (list.Count == 0)
		{
			return null;
		}
		DreamTexts.DreamText dreamText2 = list[UnityEngine.Random.Range(0, list.Count)];
		if (UnityEngine.Random.value <= dreamText2.m_chanceToDream)
		{
			return dreamText2;
		}
		return null;
	}

	// Token: 0x0600160A RID: 5642 RVA: 0x000A2990 File Offset: 0x000A0B90
	private bool HaveGlobalKeys(DreamTexts.DreamText dream)
	{
		foreach (string name in dream.m_trueKeys)
		{
			if (!ZoneSystem.instance.GetGlobalKey(name))
			{
				return false;
			}
		}
		foreach (string name2 in dream.m_falseKeys)
		{
			if (ZoneSystem.instance.GetGlobalKey(name2))
			{
				return false;
			}
		}
		return true;
	}

	// Token: 0x040015B3 RID: 5555
	public List<DreamTexts.DreamText> m_texts = new List<DreamTexts.DreamText>();

	// Token: 0x02000353 RID: 851
	[Serializable]
	public class DreamText
	{
		// Token: 0x04002564 RID: 9572
		public string m_text = "Fluffy sheep";

		// Token: 0x04002565 RID: 9573
		public float m_chanceToDream = 0.1f;

		// Token: 0x04002566 RID: 9574
		public List<string> m_trueKeys = new List<string>();

		// Token: 0x04002567 RID: 9575
		public List<string> m_falseKeys = new List<string>();
	}
}
