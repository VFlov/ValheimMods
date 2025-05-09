using System;
using System.Collections.Generic;

// Token: 0x0200013F RID: 319
public class WorldSaveComparer : IComparer<string>
{
	// Token: 0x060013E3 RID: 5091 RVA: 0x00092C30 File Offset: 0x00090E30
	public int Compare(string x, string y)
	{
		bool flag = true;
		int num = 0;
		string text;
		SaveFileType saveFileType;
		string a;
		DateTime? dateTime;
		if (!SaveSystem.GetSaveInfo(x, out text, out saveFileType, out a, out dateTime))
		{
			num++;
			flag = false;
		}
		string a2;
		if (!SaveSystem.GetSaveInfo(y, out text, out saveFileType, out a2, out dateTime))
		{
			num--;
			flag = false;
		}
		if (!flag)
		{
			return num;
		}
		if (a == ".fwl")
		{
			num--;
		}
		else if (a != ".db")
		{
			num++;
		}
		if (a2 == ".fwl")
		{
			num++;
		}
		else if (a2 != ".db")
		{
			num--;
		}
		return num;
	}
}
