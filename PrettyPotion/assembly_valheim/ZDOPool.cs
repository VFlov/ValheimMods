using System;
using System.Collections.Generic;
using UnityEngine;

// Token: 0x020000D5 RID: 213
public static class ZDOPool
{
	// Token: 0x06000D7E RID: 3454 RVA: 0x000695AE File Offset: 0x000677AE
	public static ZDO Create(ZDOID id, Vector3 position)
	{
		ZDO zdo = ZDOPool.Get();
		zdo.Initialize(id, position);
		return zdo;
	}

	// Token: 0x06000D7F RID: 3455 RVA: 0x000695BD File Offset: 0x000677BD
	public static ZDO Create()
	{
		return ZDOPool.Get();
	}

	// Token: 0x06000D80 RID: 3456 RVA: 0x000695C4 File Offset: 0x000677C4
	public static void Release(Dictionary<ZDOID, ZDO> objects)
	{
		foreach (ZDO zdo in objects.Values)
		{
			ZDOPool.Release(zdo);
		}
	}

	// Token: 0x06000D81 RID: 3457 RVA: 0x00069614 File Offset: 0x00067814
	public static void Release(ZDO zdo)
	{
		zdo.Reset();
		ZDOPool.s_free.Push(zdo);
		ZDOPool.s_active--;
	}

	// Token: 0x06000D82 RID: 3458 RVA: 0x00069634 File Offset: 0x00067834
	private static ZDO Get()
	{
		if (ZDOPool.s_free.Count <= 0)
		{
			for (int i = 0; i < 64; i++)
			{
				ZDO item = new ZDO();
				ZDOPool.s_free.Push(item);
			}
		}
		ZDOPool.s_active++;
		ZDO zdo = ZDOPool.s_free.Pop();
		zdo.Init();
		return zdo;
	}

	// Token: 0x06000D83 RID: 3459 RVA: 0x00069688 File Offset: 0x00067888
	public static int GetPoolSize()
	{
		return ZDOPool.s_free.Count;
	}

	// Token: 0x06000D84 RID: 3460 RVA: 0x00069694 File Offset: 0x00067894
	public static int GetPoolActive()
	{
		return ZDOPool.s_active;
	}

	// Token: 0x06000D85 RID: 3461 RVA: 0x0006969B File Offset: 0x0006789B
	public static int GetPoolTotal()
	{
		return ZDOPool.s_active + ZDOPool.s_free.Count;
	}

	// Token: 0x04000D70 RID: 3440
	private const int c_BatchSize = 64;

	// Token: 0x04000D71 RID: 3441
	private static readonly Stack<ZDO> s_free = new Stack<ZDO>();

	// Token: 0x04000D72 RID: 3442
	private static int s_active;
}
