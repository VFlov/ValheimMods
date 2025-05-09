using System;
using UnityEngine;

// Token: 0x02000043 RID: 67
public class Talker : MonoBehaviour
{
	// Token: 0x06000560 RID: 1376 RVA: 0x0002DE39 File Offset: 0x0002C039
	private void Awake()
	{
		this.m_nview = base.GetComponent<ZNetView>();
		this.m_character = base.GetComponent<Character>();
		this.m_nview.Register<int, UserInfo, string>("Say", new Action<long, int, UserInfo, string>(this.RPC_Say));
	}

	// Token: 0x06000561 RID: 1377 RVA: 0x0002DE70 File Offset: 0x0002C070
	public void Say(Talker.Type type, string text)
	{
		ZLog.Log("Saying " + type.ToString() + "  " + text);
		Chat.CheckPermissionsAndSendChatMessageRPCsAsync(delegate(long user, bool filterText)
		{
			UserInfo userInfo;
			string text2;
			Chat.GetChatMessageData(text, filterText, out userInfo, out text2);
			this.m_nview.InvokeRPC(user, "Say", new object[]
			{
				(int)type,
				userInfo,
				text2
			});
		});
	}

	// Token: 0x06000562 RID: 1378 RVA: 0x0002DED4 File Offset: 0x0002C0D4
	private void RPC_Say(long sender, int ctype, UserInfo user, string text)
	{
		if (Player.m_localPlayer == null)
		{
			return;
		}
		float num = 0f;
		switch (ctype)
		{
		case 0:
			num = this.m_visperDistance;
			break;
		case 1:
			num = this.m_normalDistance;
			break;
		case 2:
			num = this.m_shoutDistance;
			break;
		}
		if (Vector3.Distance(base.transform.position, Player.m_localPlayer.transform.position) < num && Chat.instance)
		{
			Vector3 headPoint = this.m_character.GetHeadPoint();
			Chat.instance.OnNewChatMessage(base.gameObject, sender, headPoint, (Talker.Type)ctype, user, text);
		}
	}

	// Token: 0x0400060A RID: 1546
	public float m_visperDistance = 4f;

	// Token: 0x0400060B RID: 1547
	public float m_normalDistance = 15f;

	// Token: 0x0400060C RID: 1548
	public float m_shoutDistance = 70f;

	// Token: 0x0400060D RID: 1549
	private ZNetView m_nview;

	// Token: 0x0400060E RID: 1550
	private Character m_character;

	// Token: 0x02000248 RID: 584
	public enum Type
	{
		// Token: 0x04001FF9 RID: 8185
		Whisper,
		// Token: 0x04001FFA RID: 8186
		Normal,
		// Token: 0x04001FFB RID: 8187
		Shout,
		// Token: 0x04001FFC RID: 8188
		Ping
	}
}
