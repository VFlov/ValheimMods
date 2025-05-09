using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEngine;

// Token: 0x020000E9 RID: 233
public class ZRpc : IDisposable
{
	// Token: 0x06000EE4 RID: 3812 RVA: 0x00070CF1 File Offset: 0x0006EEF1
	public ZRpc(ISocket socket)
	{
		this.m_socket = socket;
	}

	// Token: 0x06000EE5 RID: 3813 RVA: 0x00070D16 File Offset: 0x0006EF16
	public void Dispose()
	{
		this.m_socket.Dispose();
	}

	// Token: 0x06000EE6 RID: 3814 RVA: 0x00070D23 File Offset: 0x0006EF23
	public ISocket GetSocket()
	{
		return this.m_socket;
	}

	// Token: 0x06000EE7 RID: 3815 RVA: 0x00070D2C File Offset: 0x0006EF2C
	public ZRpc.ErrorCode Update(float dt)
	{
		if (!this.m_socket.IsConnected())
		{
			return ZRpc.ErrorCode.Disconnected;
		}
		for (ZPackage zpackage = this.m_socket.Recv(); zpackage != null; zpackage = this.m_socket.Recv())
		{
			this.m_recvPackages++;
			this.m_recvData += zpackage.Size();
			try
			{
				this.HandlePackage(zpackage);
			}
			catch (EndOfStreamException ex)
			{
				ZLog.LogError("EndOfStreamException in ZRpc::HandlePackage: Assume incompatible version: " + ex.Message);
				return ZRpc.ErrorCode.IncompatibleVersion;
			}
			catch (Exception ex2)
			{
				string str = "Exception in ZRpc::HandlePackage: ";
				Exception ex3 = ex2;
				ZLog.Log(str + ((ex3 != null) ? ex3.ToString() : null));
			}
		}
		this.UpdatePing(dt);
		return ZRpc.ErrorCode.Success;
	}

	// Token: 0x06000EE8 RID: 3816 RVA: 0x00070DF0 File Offset: 0x0006EFF0
	private void UpdatePing(float dt)
	{
		this.m_pingTimer += dt;
		if (this.m_pingTimer > ZRpc.m_pingInterval)
		{
			this.m_pingTimer = 0f;
			this.m_pkg.Clear();
			this.m_pkg.Write(0);
			this.m_pkg.Write(true);
			this.SendPackage(this.m_pkg);
		}
		this.m_timeSinceLastPing += dt;
		if (this.m_timeSinceLastPing > ZRpc.m_timeout)
		{
			ZLog.LogWarning("ZRpc timeout detected");
			this.m_socket.Close();
		}
	}

	// Token: 0x06000EE9 RID: 3817 RVA: 0x00070E84 File Offset: 0x0006F084
	private void ReceivePing(ZPackage package)
	{
		if (package.ReadBool())
		{
			this.m_pkg.Clear();
			this.m_pkg.Write(0);
			this.m_pkg.Write(false);
			this.SendPackage(this.m_pkg);
			return;
		}
		this.m_timeSinceLastPing = 0f;
	}

	// Token: 0x06000EEA RID: 3818 RVA: 0x00070ED4 File Offset: 0x0006F0D4
	public float GetTimeSinceLastPing()
	{
		return this.m_timeSinceLastPing;
	}

	// Token: 0x06000EEB RID: 3819 RVA: 0x00070EDC File Offset: 0x0006F0DC
	public bool IsConnected()
	{
		return this.m_socket.IsConnected();
	}

	// Token: 0x06000EEC RID: 3820 RVA: 0x00070EEC File Offset: 0x0006F0EC
	private void HandlePackage(ZPackage package)
	{
		int num = package.ReadInt();
		if (num == 0)
		{
			this.ReceivePing(package);
			return;
		}
		ZRpc.RpcMethodBase rpcMethodBase2;
		if (ZRpc.m_DEBUG)
		{
			package.ReadString();
			ZRpc.RpcMethodBase rpcMethodBase;
			if (this.m_functions.TryGetValue(num, out rpcMethodBase))
			{
				rpcMethodBase.Invoke(this, package);
				return;
			}
		}
		else if (this.m_functions.TryGetValue(num, out rpcMethodBase2))
		{
			rpcMethodBase2.Invoke(this, package);
		}
	}

	// Token: 0x06000EED RID: 3821 RVA: 0x00070F4C File Offset: 0x0006F14C
	public void Register(string name, ZRpc.RpcMethod.Method f)
	{
		int stableHashCode = name.GetStableHashCode();
		this.m_functions.Remove(stableHashCode);
		this.m_functions.Add(stableHashCode, new ZRpc.RpcMethod(f));
	}

	// Token: 0x06000EEE RID: 3822 RVA: 0x00070F80 File Offset: 0x0006F180
	public void Register<T>(string name, Action<ZRpc, T> f)
	{
		int stableHashCode = name.GetStableHashCode();
		this.m_functions.Remove(stableHashCode);
		this.m_functions.Add(stableHashCode, new ZRpc.RpcMethod<T>(f));
	}

	// Token: 0x06000EEF RID: 3823 RVA: 0x00070FB4 File Offset: 0x0006F1B4
	public void Register<T, U>(string name, Action<ZRpc, T, U> f)
	{
		int stableHashCode = name.GetStableHashCode();
		this.m_functions.Remove(stableHashCode);
		this.m_functions.Add(stableHashCode, new ZRpc.RpcMethod<T, U>(f));
	}

	// Token: 0x06000EF0 RID: 3824 RVA: 0x00070FE8 File Offset: 0x0006F1E8
	public void Register<T, U, V>(string name, Action<ZRpc, T, U, V> f)
	{
		int stableHashCode = name.GetStableHashCode();
		this.m_functions.Remove(stableHashCode);
		this.m_functions.Add(stableHashCode, new ZRpc.RpcMethod<T, U, V>(f));
	}

	// Token: 0x06000EF1 RID: 3825 RVA: 0x0007101C File Offset: 0x0006F21C
	public void Register<T, U, V, W>(string name, ZRpc.RpcMethod<T, U, V, W>.Method f)
	{
		int stableHashCode = name.GetStableHashCode();
		this.m_functions.Remove(stableHashCode);
		this.m_functions.Add(stableHashCode, new ZRpc.RpcMethod<T, U, V, W>(f));
	}

	// Token: 0x06000EF2 RID: 3826 RVA: 0x00071050 File Offset: 0x0006F250
	public void Unregister(string name)
	{
		int stableHashCode = name.GetStableHashCode();
		this.m_functions.Remove(stableHashCode);
	}

	// Token: 0x06000EF3 RID: 3827 RVA: 0x00071074 File Offset: 0x0006F274
	public void Invoke(string method, params object[] parameters)
	{
		if (!this.IsConnected())
		{
			return;
		}
		this.m_pkg.Clear();
		int stableHashCode = method.GetStableHashCode();
		this.m_pkg.Write(stableHashCode);
		if (ZRpc.m_DEBUG)
		{
			this.m_pkg.Write(method);
		}
		ZRpc.Serialize(parameters, ref this.m_pkg);
		this.SendPackage(this.m_pkg);
	}

	// Token: 0x06000EF4 RID: 3828 RVA: 0x000710D3 File Offset: 0x0006F2D3
	private void SendPackage(ZPackage pkg)
	{
		this.m_sentPackages++;
		this.m_sentData += pkg.Size();
		this.m_socket.Send(this.m_pkg);
	}

	// Token: 0x06000EF5 RID: 3829 RVA: 0x00071108 File Offset: 0x0006F308
	public static void Serialize(object[] parameters, ref ZPackage pkg)
	{
		foreach (object obj in parameters)
		{
			if (obj is int)
			{
				pkg.Write((int)obj);
			}
			else if (obj is uint)
			{
				pkg.Write((uint)obj);
			}
			else if (obj is long)
			{
				pkg.Write((long)obj);
			}
			else if (obj is float)
			{
				pkg.Write((float)obj);
			}
			else if (obj is double)
			{
				pkg.Write((double)obj);
			}
			else if (obj is bool)
			{
				pkg.Write((bool)obj);
			}
			else if (obj is string)
			{
				pkg.Write((string)obj);
			}
			else if (obj is ZPackage)
			{
				pkg.Write((ZPackage)obj);
			}
			else
			{
				if (obj is List<string>)
				{
					List<string> list = obj as List<string>;
					pkg.Write(list.Count);
					using (List<string>.Enumerator enumerator = list.GetEnumerator())
					{
						while (enumerator.MoveNext())
						{
							string data = enumerator.Current;
							pkg.Write(data);
						}
						goto IL_207;
					}
				}
				if (obj is Vector3)
				{
					pkg.Write(((Vector3)obj).x);
					pkg.Write(((Vector3)obj).y);
					pkg.Write(((Vector3)obj).z);
				}
				else if (obj is Quaternion)
				{
					pkg.Write(((Quaternion)obj).x);
					pkg.Write(((Quaternion)obj).y);
					pkg.Write(((Quaternion)obj).z);
					pkg.Write(((Quaternion)obj).w);
				}
				else if (obj is ZDOID)
				{
					pkg.Write((ZDOID)obj);
				}
				else if (obj is HitData)
				{
					(obj as HitData).Serialize(ref pkg);
				}
				else if (obj is ISerializableParameter)
				{
					(obj as ISerializableParameter).Serialize(ref pkg);
				}
			}
			IL_207:;
		}
	}

	// Token: 0x06000EF6 RID: 3830 RVA: 0x0007133C File Offset: 0x0006F53C
	public static object[] Deserialize(ZRpc rpc, ParameterInfo[] paramInfo, ZPackage pkg)
	{
		List<object> list = new List<object>();
		list.Add(rpc);
		ZRpc.Deserialize(paramInfo, pkg, ref list);
		return list.ToArray();
	}

	// Token: 0x06000EF7 RID: 3831 RVA: 0x00071368 File Offset: 0x0006F568
	public static void Deserialize(ParameterInfo[] paramInfo, ZPackage pkg, ref List<object> parameters)
	{
		for (int i = 1; i < paramInfo.Length; i++)
		{
			ParameterInfo parameterInfo = paramInfo[i];
			if (parameterInfo.ParameterType == typeof(int))
			{
				parameters.Add(pkg.ReadInt());
			}
			else if (parameterInfo.ParameterType == typeof(uint))
			{
				parameters.Add(pkg.ReadUInt());
			}
			else if (parameterInfo.ParameterType == typeof(long))
			{
				parameters.Add(pkg.ReadLong());
			}
			else if (parameterInfo.ParameterType == typeof(float))
			{
				parameters.Add(pkg.ReadSingle());
			}
			else if (parameterInfo.ParameterType == typeof(double))
			{
				parameters.Add(pkg.ReadDouble());
			}
			else if (parameterInfo.ParameterType == typeof(bool))
			{
				parameters.Add(pkg.ReadBool());
			}
			else if (parameterInfo.ParameterType == typeof(string))
			{
				parameters.Add(pkg.ReadString());
			}
			else if (parameterInfo.ParameterType == typeof(ZPackage))
			{
				parameters.Add(pkg.ReadPackage());
			}
			else if (parameterInfo.ParameterType == typeof(List<string>))
			{
				int num = pkg.ReadInt();
				List<string> list = new List<string>(num);
				for (int j = 0; j < num; j++)
				{
					list.Add(pkg.ReadString());
				}
				parameters.Add(list);
			}
			else if (parameterInfo.ParameterType == typeof(Vector3))
			{
				Vector3 vector = new Vector3(pkg.ReadSingle(), pkg.ReadSingle(), pkg.ReadSingle());
				parameters.Add(vector);
			}
			else if (parameterInfo.ParameterType == typeof(Quaternion))
			{
				Quaternion quaternion = new Quaternion(pkg.ReadSingle(), pkg.ReadSingle(), pkg.ReadSingle(), pkg.ReadSingle());
				parameters.Add(quaternion);
			}
			else if (parameterInfo.ParameterType == typeof(ZDOID))
			{
				parameters.Add(pkg.ReadZDOID());
			}
			else if (parameterInfo.ParameterType == typeof(HitData))
			{
				HitData hitData = new HitData();
				hitData.Deserialize(ref pkg);
				parameters.Add(hitData);
			}
			else if (typeof(ISerializableParameter).IsAssignableFrom(parameterInfo.ParameterType))
			{
				ISerializableParameter serializableParameter = (ISerializableParameter)Activator.CreateInstance(parameterInfo.ParameterType);
				serializableParameter.Deserialize(ref pkg);
				parameters.Add(serializableParameter);
			}
			else if (typeof(ISerializableParameter).IsAssignableFrom(parameterInfo.ParameterType))
			{
				ISerializableParameter serializableParameter2 = (ISerializableParameter)Activator.CreateInstance(parameterInfo.ParameterType);
				serializableParameter2.Deserialize(ref pkg);
				parameters.Add(serializableParameter2);
			}
		}
	}

	// Token: 0x06000EF8 RID: 3832 RVA: 0x000716A5 File Offset: 0x0006F8A5
	public static void SetLongTimeout(bool enable)
	{
		if (enable)
		{
			ZRpc.m_timeout = 90f;
		}
		else
		{
			ZRpc.m_timeout = 30f;
		}
		ZLog.Log(string.Format("ZRpc timeout set to {0}s ", ZRpc.m_timeout));
	}

	// Token: 0x04000E9F RID: 3743
	private ISocket m_socket;

	// Token: 0x04000EA0 RID: 3744
	private ZPackage m_pkg = new ZPackage();

	// Token: 0x04000EA1 RID: 3745
	private Dictionary<int, ZRpc.RpcMethodBase> m_functions = new Dictionary<int, ZRpc.RpcMethodBase>();

	// Token: 0x04000EA2 RID: 3746
	private int m_sentPackages;

	// Token: 0x04000EA3 RID: 3747
	private int m_sentData;

	// Token: 0x04000EA4 RID: 3748
	private int m_recvPackages;

	// Token: 0x04000EA5 RID: 3749
	private int m_recvData;

	// Token: 0x04000EA6 RID: 3750
	private float m_pingTimer;

	// Token: 0x04000EA7 RID: 3751
	private float m_timeSinceLastPing;

	// Token: 0x04000EA8 RID: 3752
	private static float m_pingInterval = 1f;

	// Token: 0x04000EA9 RID: 3753
	private static float m_timeout = 30f;

	// Token: 0x04000EAA RID: 3754
	private static bool m_DEBUG = false;

	// Token: 0x020002EF RID: 751
	public enum ErrorCode
	{
		// Token: 0x04002370 RID: 9072
		Success,
		// Token: 0x04002371 RID: 9073
		Disconnected,
		// Token: 0x04002372 RID: 9074
		IncompatibleVersion
	}

	// Token: 0x020002F0 RID: 752
	private interface RpcMethodBase
	{
		// Token: 0x0600219E RID: 8606
		void Invoke(ZRpc rpc, ZPackage pkg);
	}

	// Token: 0x020002F1 RID: 753
	public class RpcMethod : ZRpc.RpcMethodBase
	{
		// Token: 0x0600219F RID: 8607 RVA: 0x000EBB2C File Offset: 0x000E9D2C
		public RpcMethod(ZRpc.RpcMethod.Method action)
		{
			this.m_action = action;
		}

		// Token: 0x060021A0 RID: 8608 RVA: 0x000EBB3B File Offset: 0x000E9D3B
		public void Invoke(ZRpc rpc, ZPackage pkg)
		{
			this.m_action(rpc);
		}

		// Token: 0x04002373 RID: 9075
		private ZRpc.RpcMethod.Method m_action;

		// Token: 0x020003E4 RID: 996
		// (Invoke) Token: 0x060023F8 RID: 9208
		public delegate void Method(ZRpc RPC);
	}

	// Token: 0x020002F2 RID: 754
	private class RpcMethod<T> : ZRpc.RpcMethodBase
	{
		// Token: 0x060021A1 RID: 8609 RVA: 0x000EBB49 File Offset: 0x000E9D49
		public RpcMethod(Action<ZRpc, T> action)
		{
			this.m_action = action;
		}

		// Token: 0x060021A2 RID: 8610 RVA: 0x000EBB58 File Offset: 0x000E9D58
		public void Invoke(ZRpc rpc, ZPackage pkg)
		{
			this.m_action.DynamicInvoke(ZRpc.Deserialize(rpc, this.m_action.Method.GetParameters(), pkg));
		}

		// Token: 0x04002374 RID: 9076
		private Action<ZRpc, T> m_action;
	}

	// Token: 0x020002F3 RID: 755
	private class RpcMethod<T, U> : ZRpc.RpcMethodBase
	{
		// Token: 0x060021A3 RID: 8611 RVA: 0x000EBB7D File Offset: 0x000E9D7D
		public RpcMethod(Action<ZRpc, T, U> action)
		{
			this.m_action = action;
		}

		// Token: 0x060021A4 RID: 8612 RVA: 0x000EBB8C File Offset: 0x000E9D8C
		public void Invoke(ZRpc rpc, ZPackage pkg)
		{
			this.m_action.DynamicInvoke(ZRpc.Deserialize(rpc, this.m_action.Method.GetParameters(), pkg));
		}

		// Token: 0x04002375 RID: 9077
		private Action<ZRpc, T, U> m_action;
	}

	// Token: 0x020002F4 RID: 756
	private class RpcMethod<T, U, V> : ZRpc.RpcMethodBase
	{
		// Token: 0x060021A5 RID: 8613 RVA: 0x000EBBB1 File Offset: 0x000E9DB1
		public RpcMethod(Action<ZRpc, T, U, V> action)
		{
			this.m_action = action;
		}

		// Token: 0x060021A6 RID: 8614 RVA: 0x000EBBC0 File Offset: 0x000E9DC0
		public void Invoke(ZRpc rpc, ZPackage pkg)
		{
			this.m_action.DynamicInvoke(ZRpc.Deserialize(rpc, this.m_action.Method.GetParameters(), pkg));
		}

		// Token: 0x04002376 RID: 9078
		private Action<ZRpc, T, U, V> m_action;
	}

	// Token: 0x020002F5 RID: 757
	public class RpcMethod<T, U, V, B> : ZRpc.RpcMethodBase
	{
		// Token: 0x060021A7 RID: 8615 RVA: 0x000EBBE5 File Offset: 0x000E9DE5
		public RpcMethod(ZRpc.RpcMethod<T, U, V, B>.Method action)
		{
			this.m_action = action;
		}

		// Token: 0x060021A8 RID: 8616 RVA: 0x000EBBF4 File Offset: 0x000E9DF4
		public void Invoke(ZRpc rpc, ZPackage pkg)
		{
			this.m_action.DynamicInvoke(ZRpc.Deserialize(rpc, this.m_action.Method.GetParameters(), pkg));
		}

		// Token: 0x04002377 RID: 9079
		private ZRpc.RpcMethod<T, U, V, B>.Method m_action;

		// Token: 0x020003E5 RID: 997
		// (Invoke) Token: 0x060023FC RID: 9212
		public delegate void Method(ZRpc RPC, T p0, U p1, V p2, B p3);
	}
}
