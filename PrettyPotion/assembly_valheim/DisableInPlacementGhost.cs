using System;
using System.Collections.Generic;
using UnityEngine;

// Token: 0x0200016B RID: 363
public class DisableInPlacementGhost : MonoBehaviour
{
	// Token: 0x060015FB RID: 5627 RVA: 0x000A21A0 File Offset: 0x000A03A0
	private void Start()
	{
		if (!Player.IsPlacementGhost(base.gameObject))
		{
			return;
		}
		foreach (Behaviour behaviour in this.m_components)
		{
			behaviour.enabled = false;
		}
		foreach (GameObject gameObject in this.m_objects)
		{
			gameObject.SetActive(false);
		}
	}

	// Token: 0x040015A5 RID: 5541
	public List<Behaviour> m_components;

	// Token: 0x040015A6 RID: 5542
	public List<GameObject> m_objects;
}
