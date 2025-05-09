using System;
using System.Collections.Generic;

// Token: 0x0200013D RID: 317
public class SaveFileComparer : IComparer<SaveFile>
{
	// Token: 0x060013DF RID: 5087 RVA: 0x00092BB6 File Offset: 0x00090DB6
	public int Compare(SaveFile x, SaveFile y)
	{
		return DateTime.Compare(y.LastModified, x.LastModified);
	}
}
