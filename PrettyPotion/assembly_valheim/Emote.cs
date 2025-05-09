using System;

// Token: 0x0200001A RID: 26
public class Emote : Attribute
{
	// Token: 0x0600026E RID: 622 RVA: 0x00015F1C File Offset: 0x0001411C
	public static void DoEmote(Emotes emote)
	{
		Emote attributeOfType = emote.GetAttributeOfType<Emote>();
		if (Player.m_localPlayer && Player.m_localPlayer.StartEmote(emote.ToString().ToLower(), attributeOfType == null || attributeOfType.OneShot) && attributeOfType != null && attributeOfType.FaceLookDirection)
		{
			Player.m_localPlayer.FaceLookDirection();
		}
	}

	// Token: 0x04000376 RID: 886
	public bool OneShot = true;

	// Token: 0x04000377 RID: 887
	public bool FaceLookDirection;
}
