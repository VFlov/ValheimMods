using System;
using System.Collections.Generic;

// Token: 0x0200013E RID: 318
public class SaveWithBackupsComparer : IComparer<SaveWithBackups>
{
	// Token: 0x060013E1 RID: 5089 RVA: 0x00092BD4 File Offset: 0x00090DD4
	public int Compare(SaveWithBackups x, SaveWithBackups y)
	{
		if (x.IsDeleted || y.IsDeleted)
		{
			return 0 + (x.IsDeleted ? -1 : 0) + (y.IsDeleted ? 1 : 0);
		}
		return DateTime.Compare(y.PrimaryFile.LastModified, x.PrimaryFile.LastModified);
	}
}
