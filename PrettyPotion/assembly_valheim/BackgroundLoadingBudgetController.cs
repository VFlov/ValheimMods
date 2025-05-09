using System;
using System.Collections.Generic;
using UnityEngine;

// Token: 0x0200010D RID: 269
public class BackgroundLoadingBudgetController
{
	// Token: 0x06001121 RID: 4385 RVA: 0x0007CF79 File Offset: 0x0007B179
	[RuntimeInitializeOnLoadMethod]
	private static void OnLoad()
	{
		BackgroundLoadingBudgetController.ApplyBudget();
	}

	// Token: 0x06001122 RID: 4386 RVA: 0x0007CF80 File Offset: 0x0007B180
	public static ThreadPriority RequestLoadingBudget(ThreadPriority priority)
	{
		BackgroundLoadingBudgetController.AddRequest(priority);
		BackgroundLoadingBudgetController.ApplyBudget();
		return priority;
	}

	// Token: 0x06001123 RID: 4387 RVA: 0x0007CF8E File Offset: 0x0007B18E
	public static ThreadPriority UpdateLoadingBudgetRequest(ThreadPriority oldPriority, ThreadPriority newPriority)
	{
		BackgroundLoadingBudgetController.RemoveRequest(oldPriority);
		BackgroundLoadingBudgetController.AddRequest(newPriority);
		BackgroundLoadingBudgetController.ApplyBudget();
		return newPriority;
	}

	// Token: 0x06001124 RID: 4388 RVA: 0x0007CFA2 File Offset: 0x0007B1A2
	public static void ReleaseLoadingBudgetRequest(ThreadPriority priority)
	{
		BackgroundLoadingBudgetController.RemoveRequest(priority);
		BackgroundLoadingBudgetController.ApplyBudget();
	}

	// Token: 0x06001125 RID: 4389 RVA: 0x0007CFB0 File Offset: 0x0007B1B0
	private static void AddRequest(ThreadPriority priority)
	{
		int num = BackgroundLoadingBudgetController.m_budgetRequests.BinarySearch(priority);
		if (num < 0)
		{
			num = ~num;
		}
		BackgroundLoadingBudgetController.m_budgetRequests.Insert(num, priority);
	}

	// Token: 0x06001126 RID: 4390 RVA: 0x0007CFDC File Offset: 0x0007B1DC
	private static void RemoveRequest(ThreadPriority priority)
	{
		int num = BackgroundLoadingBudgetController.m_budgetRequests.BinarySearch(priority);
		if (num >= 0)
		{
			BackgroundLoadingBudgetController.m_budgetRequests.RemoveAt(num);
			return;
		}
		ZLog.LogError(string.Format("Failed to remove loading budget request {0}", priority));
	}

	// Token: 0x06001127 RID: 4391 RVA: 0x0007D01C File Offset: 0x0007B21C
	private static void ApplyBudget()
	{
		ThreadPriority threadPriority = (BackgroundLoadingBudgetController.m_budgetRequests.Count <= 0 || BackgroundLoadingBudgetController.m_budgetRequests[BackgroundLoadingBudgetController.m_budgetRequests.Count - 1] < ThreadPriority.Low) ? ThreadPriority.Low : BackgroundLoadingBudgetController.m_budgetRequests[BackgroundLoadingBudgetController.m_budgetRequests.Count - 1];
		Application.backgroundLoadingPriority = threadPriority;
		ZLog.Log(string.Format("Set background loading budget to {0}", threadPriority));
	}

	// Token: 0x04001044 RID: 4164
	private const ThreadPriority c_defaultBudget = ThreadPriority.Low;

	// Token: 0x04001045 RID: 4165
	private static List<ThreadPriority> m_budgetRequests = new List<ThreadPriority>();
}
