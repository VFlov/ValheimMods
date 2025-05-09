using System;

// Token: 0x020000E2 RID: 226
public class RoutedMethod<T, U, V, B> : RoutedMethodBase
{
	// Token: 0x06000E69 RID: 3689 RVA: 0x0006F447 File Offset: 0x0006D647
	public RoutedMethod(RoutedMethod<T, U, V, B>.Method action)
	{
		this.m_action = action;
	}

	// Token: 0x06000E6A RID: 3690 RVA: 0x0006F456 File Offset: 0x0006D656
	public void Invoke(long rpc, ZPackage pkg)
	{
		this.m_action.DynamicInvoke(ZNetView.Deserialize(rpc, this.m_action.Method.GetParameters(), pkg));
	}

	// Token: 0x04000E7B RID: 3707
	private RoutedMethod<T, U, V, B>.Method m_action;

	// Token: 0x020002EB RID: 747
	// (Invoke) Token: 0x06002190 RID: 8592
	public delegate void Method(long sender, T p0, U p1, V p2, B p3);
}
