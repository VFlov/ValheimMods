using System;

// Token: 0x020000C4 RID: 196
public class ServerJoinDataPlayFabUser : ServerJoinData
{
	// Token: 0x06000BC4 RID: 3012 RVA: 0x00061EEE File Offset: 0x000600EE
	public ServerJoinDataPlayFabUser(string remotePlayerId)
	{
		this.m_remotePlayerId = remotePlayerId;
		this.m_serverName = this.ToString();
	}

	// Token: 0x06000BC5 RID: 3013 RVA: 0x00061F09 File Offset: 0x00060109
	public override bool IsValid()
	{
		return this.m_remotePlayerId != null;
	}

	// Token: 0x06000BC6 RID: 3014 RVA: 0x00061F14 File Offset: 0x00060114
	public override string GetDataName()
	{
		return "PlayFab user";
	}

	// Token: 0x06000BC7 RID: 3015 RVA: 0x00061F1C File Offset: 0x0006011C
	public override bool Equals(object obj)
	{
		ServerJoinDataPlayFabUser serverJoinDataPlayFabUser = obj as ServerJoinDataPlayFabUser;
		return serverJoinDataPlayFabUser != null && base.Equals(obj) && this.ToString() == serverJoinDataPlayFabUser.ToString();
	}

	// Token: 0x06000BC8 RID: 3016 RVA: 0x00061F4F File Offset: 0x0006014F
	public override int GetHashCode()
	{
		return (1688301347 * -1521134295 + base.GetHashCode()) * -1521134295 + this.ToString().GetHashCode();
	}

	// Token: 0x06000BC9 RID: 3017 RVA: 0x00061F75 File Offset: 0x00060175
	public static bool operator ==(ServerJoinDataPlayFabUser left, ServerJoinDataPlayFabUser right)
	{
		if (left == null || right == null)
		{
			return left == null && right == null;
		}
		return left.Equals(right);
	}

	// Token: 0x06000BCA RID: 3018 RVA: 0x00061F8E File Offset: 0x0006018E
	public static bool operator !=(ServerJoinDataPlayFabUser left, ServerJoinDataPlayFabUser right)
	{
		return !(left == right);
	}

	// Token: 0x06000BCB RID: 3019 RVA: 0x00061F9A File Offset: 0x0006019A
	public override string ToString()
	{
		return this.m_remotePlayerId;
	}

	// Token: 0x17000054 RID: 84
	// (get) Token: 0x06000BCC RID: 3020 RVA: 0x00061FA2 File Offset: 0x000601A2
	// (set) Token: 0x06000BCD RID: 3021 RVA: 0x00061FAA File Offset: 0x000601AA
	public string m_remotePlayerId { get; private set; }

	// Token: 0x04000CE7 RID: 3303
	public const string typeName = "PlayFab user";
}
