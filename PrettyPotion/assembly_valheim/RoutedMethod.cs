using System;

// Token: 0x020000DE RID: 222
internal class RoutedMethod : RoutedMethodBase
{
	// Token: 0x06000E61 RID: 3681 RVA: 0x0006F38E File Offset: 0x0006D58E
	public RoutedMethod(Action<long> action)
	{
		this.m_action = action;
	}

	// Token: 0x06000E62 RID: 3682 RVA: 0x0006F39D File Offset: 0x0006D59D
	public void Invoke(long rpc, ZPackage pkg)
	{
		this.m_action(rpc);
	}

	// Token: 0x04000E77 RID: 3703
	private Action<long> m_action;
}
