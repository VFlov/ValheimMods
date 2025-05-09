using System;
using System.Collections.Generic;

// Token: 0x020000BE RID: 190
public static class MonoUpdatersExtra
{
	// Token: 0x06000BAB RID: 2987 RVA: 0x00061AE0 File Offset: 0x0005FCE0
	public static void UpdateAI(this List<IUpdateAI> container, List<IUpdateAI> source, string profileScope, float deltaTime)
	{
		container.AddRange(source);
		foreach (IUpdateAI updateAI in container)
		{
			updateAI.UpdateAI(deltaTime);
		}
		container.Clear();
	}

	// Token: 0x06000BAC RID: 2988 RVA: 0x00061B3C File Offset: 0x0005FD3C
	public static void CustomFixedUpdate(this List<IMonoUpdater> container, List<IMonoUpdater> source, string profileScope, float deltaTime)
	{
		container.AddRange(source);
		foreach (IMonoUpdater monoUpdater in container)
		{
			monoUpdater.CustomFixedUpdate(deltaTime);
		}
		container.Clear();
	}

	// Token: 0x06000BAD RID: 2989 RVA: 0x00061B98 File Offset: 0x0005FD98
	public static void CustomUpdate(this List<IMonoUpdater> container, List<IMonoUpdater> source, string profileScope, float deltaTime, float time)
	{
		container.AddRange(source);
		foreach (IMonoUpdater monoUpdater in container)
		{
			monoUpdater.CustomUpdate(deltaTime, time);
		}
		container.Clear();
	}

	// Token: 0x06000BAE RID: 2990 RVA: 0x00061BF4 File Offset: 0x0005FDF4
	public static void CustomLateUpdate(this List<IMonoUpdater> container, List<IMonoUpdater> source, string profileScope, float deltaTime)
	{
		container.AddRange(source);
		foreach (IMonoUpdater monoUpdater in container)
		{
			monoUpdater.CustomLateUpdate(deltaTime);
		}
		container.Clear();
	}
}
