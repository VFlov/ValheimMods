using System;

// Token: 0x020000B8 RID: 184
public interface ISerializableParameter
{
	// Token: 0x06000B7E RID: 2942
	void Serialize(ref ZPackage pkg);

	// Token: 0x06000B7F RID: 2943
	void Deserialize(ref ZPackage pkg);
}
