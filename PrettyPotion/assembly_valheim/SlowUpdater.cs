using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Token: 0x02000147 RID: 327
public class SlowUpdater : MonoBehaviour
{
	// Token: 0x06001407 RID: 5127 RVA: 0x000930FD File Offset: 0x000912FD
	private void Awake()
	{
		base.StartCoroutine("UpdateLoop");
	}

	// Token: 0x06001408 RID: 5128 RVA: 0x0009310B File Offset: 0x0009130B
	private IEnumerator UpdateLoop()
	{
		for (;;)
		{
			List<SlowUpdate> instances = SlowUpdate.GetAllInstaces();
			int index = 0;
			while (index < instances.Count)
			{
				float time = Time.time;
				Vector2i zone = ZoneSystem.GetZone(ZNet.instance.GetReferencePosition());
				int num = 0;
				while (num < 100 && instances.Count != 0 && index < instances.Count)
				{
					instances[index].SUpdate(time, zone);
					int num2 = index + 1;
					index = num2;
					num++;
				}
				yield return null;
			}
			yield return new WaitForSeconds(0.1f);
			instances = null;
		}
		yield break;
	}

	// Token: 0x040013CC RID: 5068
	private const int m_updatesPerFrame = 100;
}
