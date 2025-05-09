using System;
using System.Collections.Generic;
using UnityEngine;

// Token: 0x020001BD RID: 445
[ExecuteInEditMode]
public class SnapToGround : MonoBehaviour
{
	// Token: 0x06001A0E RID: 6670 RVA: 0x000C265C File Offset: 0x000C085C
	private void Awake()
	{
		SnapToGround.m_allSnappers.Add(this);
		this.m_inList = true;
	}

	// Token: 0x06001A0F RID: 6671 RVA: 0x000C2670 File Offset: 0x000C0870
	private void OnDestroy()
	{
		if (this.m_inList)
		{
			SnapToGround.m_allSnappers.Remove(this);
			this.m_inList = false;
		}
	}

	// Token: 0x06001A10 RID: 6672 RVA: 0x000C2690 File Offset: 0x000C0890
	public void Snap()
	{
		if (ZoneSystem.instance == null)
		{
			return;
		}
		float groundHeight = ZoneSystem.instance.GetGroundHeight(base.transform.position);
		Vector3 position = base.transform.position;
		position.y = groundHeight + this.m_offset;
		base.transform.position = position;
		ZNetView component = base.GetComponent<ZNetView>();
		if (component != null && component.IsOwner())
		{
			component.GetZDO().SetPosition(position);
		}
	}

	// Token: 0x06001A11 RID: 6673 RVA: 0x000C270C File Offset: 0x000C090C
	public bool HaveUnsnapped()
	{
		return SnapToGround.m_allSnappers.Count > 0;
	}

	// Token: 0x06001A12 RID: 6674 RVA: 0x000C271C File Offset: 0x000C091C
	public static void SnappAll()
	{
		if (SnapToGround.m_allSnappers.Count == 0)
		{
			return;
		}
		Heightmap.ForceGenerateAll();
		foreach (SnapToGround snapToGround in SnapToGround.m_allSnappers)
		{
			snapToGround.Snap();
			snapToGround.m_inList = false;
		}
		SnapToGround.m_allSnappers.Clear();
	}

	// Token: 0x04001A91 RID: 6801
	public float m_offset;

	// Token: 0x04001A92 RID: 6802
	private static List<SnapToGround> m_allSnappers = new List<SnapToGround>();

	// Token: 0x04001A93 RID: 6803
	private bool m_inList;
}
