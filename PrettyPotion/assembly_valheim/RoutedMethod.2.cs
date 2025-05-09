using System;

// Token: 0x020000DF RID: 223
internal class RoutedMethod<T> : RoutedMethodBase
{
	// Token: 0x06000E63 RID: 3683 RVA: 0x0006F3AB File Offset: 0x0006D5AB
	public RoutedMethod(Action<long, T> action)
	{
		this.m_action = action;
	}

	// Token: 0x06000E64 RID: 3684 RVA: 0x0006F3BA File Offset: 0x0006D5BA
	public void Invoke(long rpc, ZPackage pkg)
	{
		this.m_action.DynamicInvoke(ZNetView.Deserialize(rpc, this.m_action.Method.GetParameters(), pkg));
	}

	// Token: 0x04000E78 RID: 3704
	private Action<long, T> m_action;
}
