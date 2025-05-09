using System;
using Splatform;

// Token: 0x020000C8 RID: 200
public class UserInfo : ISerializableParameter
{
	// Token: 0x06000C05 RID: 3077 RVA: 0x00062A78 File Offset: 0x00060C78
	public static UserInfo GetLocalUser()
	{
		return new UserInfo
		{
			Name = Game.instance.GetPlayerProfile().GetName(),
			UserId = PlatformManager.DistributionPlatform.LocalUser.PlatformUserID
		};
	}

	// Token: 0x06000C06 RID: 3078 RVA: 0x00062AA9 File Offset: 0x00060CA9
	public void Deserialize(ref ZPackage pkg)
	{
		this.Name = pkg.ReadString();
		this.UserId = new PlatformUserID(pkg.ReadString());
	}

	// Token: 0x06000C07 RID: 3079 RVA: 0x00062ACA File Offset: 0x00060CCA
	public void Serialize(ref ZPackage pkg)
	{
		pkg.Write(this.Name);
		pkg.Write(this.UserId.ToString());
	}

	// Token: 0x06000C08 RID: 3080 RVA: 0x00062AF1 File Offset: 0x00060CF1
	public string GetDisplayName()
	{
		return CensorShittyWords.FilterUGC(this.Name, UGCType.CharacterName, this.UserId, 0L);
	}

	// Token: 0x04000D02 RID: 3330
	public string Name;

	// Token: 0x04000D03 RID: 3331
	public PlatformUserID UserId;
}
