using System;

// Token: 0x020000E1 RID: 225
internal class RoutedMethod<T, U, V> : RoutedMethodBase
{
	// Token: 0x06000E67 RID: 3687 RVA: 0x0006F413 File Offset: 0x0006D613
	public RoutedMethod(Action<long, T, U, V> action)
	{
		this.m_action = action;
	}

	// Token: 0x06000E68 RID: 3688 RVA: 0x0006F422 File Offset: 0x0006D622
	public void Invoke(long rpc, ZPackage pkg)
	{
		this.m_action.DynamicInvoke(ZNetView.Deserialize(rpc, this.m_action.Method.GetParameters(), pkg));
	}

	// Token: 0x04000E7A RID: 3706
	private Action<long, T, U, V> m_action;
}
