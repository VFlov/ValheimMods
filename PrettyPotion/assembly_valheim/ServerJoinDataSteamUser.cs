using System;
using Steamworks;

// Token: 0x020000C3 RID: 195
public class ServerJoinDataSteamUser : ServerJoinData
{
	// Token: 0x06000BB9 RID: 3001 RVA: 0x00061DC4 File Offset: 0x0005FFC4
	public ServerJoinDataSteamUser(ulong joinUserID)
	{
		this.m_joinUserID = new CSteamID(joinUserID);
		this.m_serverName = this.ToString();
	}

	// Token: 0x06000BBA RID: 3002 RVA: 0x00061DE4 File Offset: 0x0005FFE4
	public ServerJoinDataSteamUser(CSteamID joinUserID)
	{
		this.m_joinUserID = joinUserID;
		this.m_serverName = this.ToString();
	}

	// Token: 0x06000BBB RID: 3003 RVA: 0x00061E00 File Offset: 0x00060000
	public override bool IsValid()
	{
		return this.m_joinUserID.IsValid();
	}

	// Token: 0x06000BBC RID: 3004 RVA: 0x00061E1B File Offset: 0x0006001B
	public override string GetDataName()
	{
		return "Steam user";
	}

	// Token: 0x06000BBD RID: 3005 RVA: 0x00061E24 File Offset: 0x00060024
	public override bool Equals(object obj)
	{
		ServerJoinDataSteamUser serverJoinDataSteamUser = obj as ServerJoinDataSteamUser;
		return serverJoinDataSteamUser != null && base.Equals(obj) && this.m_joinUserID.Equals(serverJoinDataSteamUser.m_joinUserID);
	}

	// Token: 0x06000BBE RID: 3006 RVA: 0x00061E5C File Offset: 0x0006005C
	public override int GetHashCode()
	{
		return (-995281327 * -1521134295 + base.GetHashCode()) * -1521134295 + this.m_joinUserID.GetHashCode();
	}

	// Token: 0x06000BBF RID: 3007 RVA: 0x00061E96 File Offset: 0x00060096
	public static bool operator ==(ServerJoinDataSteamUser left, ServerJoinDataSteamUser right)
	{
		if (left == null || right == null)
		{
			return left == null && right == null;
		}
		return left.Equals(right);
	}

	// Token: 0x06000BC0 RID: 3008 RVA: 0x00061EAF File Offset: 0x000600AF
	public static bool operator !=(ServerJoinDataSteamUser left, ServerJoinDataSteamUser right)
	{
		return !(left == right);
	}

	// Token: 0x06000BC1 RID: 3009 RVA: 0x00061EBC File Offset: 0x000600BC
	public override string ToString()
	{
		return this.m_joinUserID.ToString();
	}

	// Token: 0x17000053 RID: 83
	// (get) Token: 0x06000BC2 RID: 3010 RVA: 0x00061EDD File Offset: 0x000600DD
	// (set) Token: 0x06000BC3 RID: 3011 RVA: 0x00061EE5 File Offset: 0x000600E5
	public CSteamID m_joinUserID { get; private set; }

	// Token: 0x04000CE5 RID: 3301
	public const string typeName = "Steam user";
}
