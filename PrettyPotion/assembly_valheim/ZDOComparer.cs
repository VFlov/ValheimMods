using System;
using System.Collections.Generic;

// Token: 0x020000CC RID: 204
internal class ZDOComparer : IEqualityComparer<ZDO>
{
	// Token: 0x06000C2E RID: 3118 RVA: 0x000633AE File Offset: 0x000615AE
	public bool Equals(ZDO a, ZDO b)
	{
		return a == b;
	}

	// Token: 0x06000C2F RID: 3119 RVA: 0x000633B4 File Offset: 0x000615B4
	public int GetHashCode(ZDO a)
	{
		return a.GetHashCode();
	}
}
