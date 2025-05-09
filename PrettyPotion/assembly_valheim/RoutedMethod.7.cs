using System;

// Token: 0x020000E4 RID: 228
public class RoutedMethod<T, U, V, B, K, M> : RoutedMethodBase
{
	// Token: 0x06000E6D RID: 3693 RVA: 0x0006F4AF File Offset: 0x0006D6AF
	public RoutedMethod(RoutedMethod<T, U, V, B, K, M>.Method action)
	{
		this.m_action = action;
	}

	// Token: 0x06000E6E RID: 3694 RVA: 0x0006F4BE File Offset: 0x0006D6BE
	public void Invoke(long rpc, ZPackage pkg)
	{
		this.m_action.DynamicInvoke(ZNetView.Deserialize(rpc, this.m_action.Method.GetParameters(), pkg));
	}

	// Token: 0x04000E7D RID: 3709
	private RoutedMethod<T, U, V, B, K, M>.Method m_action;

	// Token: 0x020002ED RID: 749
	// (Invoke) Token: 0x06002198 RID: 8600
	public delegate void Method(long sender, T p0, U p1, V p2, B p3, K p4, M p5);
}
