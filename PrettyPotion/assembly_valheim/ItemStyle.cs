using System;
using UnityEngine;

// Token: 0x02000020 RID: 32
public class ItemStyle : MonoBehaviour, IEquipmentVisual
{
	// Token: 0x060002E1 RID: 737 RVA: 0x00019DC7 File Offset: 0x00017FC7
	public void Setup(int style)
	{
		MaterialMan.instance.SetValue<int>(base.gameObject, ShaderProps._Style, style);
	}
}
