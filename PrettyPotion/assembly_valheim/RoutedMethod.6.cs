using System;

// Token: 0x020000E3 RID: 227
public class RoutedMethod<T, U, V, B, K> : RoutedMethodBase
{
	// Token: 0x06000E6B RID: 3691 RVA: 0x0006F47B File Offset: 0x0006D67B
	public RoutedMethod(RoutedMethod<T, U, V, B, K>.Method action)
	{
		this.m_action = action;
	}

	// Token: 0x06000E6C RID: 3692 RVA: 0x0006F48A File Offset: 0x0006D68A
	public void Invoke(long rpc, ZPackage pkg)
	{
		this.m_action.DynamicInvoke(ZNetView.Deserialize(rpc, this.m_action.Method.GetParameters(), pkg));
	}

	// Token: 0x04000E7C RID: 3708
	private RoutedMethod<T, U, V, B, K>.Method m_action;

	// Token: 0x020002EC RID: 748
	// (Invoke) Token: 0x06002194 RID: 8596
	public delegate void Method(long sender, T p0, U p1, V p2, B p3, K p4);
}
