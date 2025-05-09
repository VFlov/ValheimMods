using System;
using System.Collections.Generic;
using UnityEngine;

// Token: 0x020000D9 RID: 217
internal static class ShuffleClass
{
	// Token: 0x06000D91 RID: 3473 RVA: 0x0006A1CC File Offset: 0x000683CC
	public static void Shuffle<T>(this IList<T> list, bool useUnityRandom = false)
	{
		int i = list.Count;
		while (i > 1)
		{
			i--;
			int index = useUnityRandom ? UnityEngine.Random.Range(0, i) : ShuffleClass.rng.Next(i + 1);
			T value = list[index];
			list[index] = list[i];
			list[i] = value;
		}
	}

	// Token: 0x04000E29 RID: 3625
	private static System.Random rng = new System.Random();
}
