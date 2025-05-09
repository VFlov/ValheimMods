using System;

// Token: 0x020000E0 RID: 224
internal class RoutedMethod<T, U> : RoutedMethodBase
{
	// Token: 0x06000E65 RID: 3685 RVA: 0x0006F3DF File Offset: 0x0006D5DF
	public RoutedMethod(Action<long, T, U> action)
	{
		this.m_action = action;
	}

	// Token: 0x06000E66 RID: 3686 RVA: 0x0006F3EE File Offset: 0x0006D5EE
	public void Invoke(long rpc, ZPackage pkg)
	{
		this.m_action.DynamicInvoke(ZNetView.Deserialize(rpc, this.m_action.Method.GetParameters(), pkg));
	}

	// Token: 0x04000E79 RID: 3705
	private Action<long, T, U> m_action;
}
