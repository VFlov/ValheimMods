using System;
using System.Collections.Generic;
using UnityEngine;

// Token: 0x020000CE RID: 206
public static class ZDODataHelper
{
	// Token: 0x06000CB8 RID: 3256 RVA: 0x000650EC File Offset: 0x000632EC
	public static void WriteData<TType>(ZPackage pkg, List<KeyValuePair<int, TType>> data, Action<TType> func)
	{
		if (data.Count == 0)
		{
			return;
		}
		if (data.Count > 100)
		{
			Debug.LogWarning("Writing a lot of data; " + data.Count.ToString() + " items, is not optimal. Perhaps use a byte array or two instead?");
		}
		pkg.WriteNumItems(data.Count);
		foreach (KeyValuePair<int, TType> keyValuePair in data)
		{
			pkg.Write(keyValuePair.Key);
			func(keyValuePair.Value);
		}
	}
}
