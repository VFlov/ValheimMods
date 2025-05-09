using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

// Token: 0x0200005D RID: 93
public static class ExtensionMethods
{
	// Token: 0x06000673 RID: 1651 RVA: 0x00036234 File Offset: 0x00034434
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static void Swap<T>(this List<T> list, int indexA, int indexB)
	{
		T value = list[indexB];
		T value2 = list[indexA];
		list[indexA] = value;
		list[indexB] = value2;
	}
}
