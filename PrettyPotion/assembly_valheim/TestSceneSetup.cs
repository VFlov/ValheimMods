using System;
using UnityEngine;

// Token: 0x020001CC RID: 460
public class TestSceneSetup : MonoBehaviour
{
	// Token: 0x06001A8D RID: 6797 RVA: 0x000C5A47 File Offset: 0x000C3C47
	private void Awake()
	{
		WorldGenerator.Initialize(World.GetMenuWorld());
	}

	// Token: 0x06001A8E RID: 6798 RVA: 0x000C5A53 File Offset: 0x000C3C53
	private void Update()
	{
	}
}
