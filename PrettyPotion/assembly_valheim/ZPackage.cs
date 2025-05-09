using System;
using System.IO;
using System.Security.Cryptography;
using UnityEngine;

// Token: 0x020000E7 RID: 231
public class ZPackage
{
	// Token: 0x06000E94 RID: 3732 RVA: 0x000700A8 File Offset: 0x0006E2A8
	public ZPackage()
	{
		this.m_writer = new BinaryWriter(this.m_stream);
		this.m_reader = new BinaryReader(this.m_stream);
	}

	// Token: 0x06000E95 RID: 3733 RVA: 0x000700E0 File Offset: 0x0006E2E0
	public ZPackage(string base64String)
	{
		this.m_writer = new BinaryWriter(this.m_stream);
		this.m_reader = new BinaryReader(this.m_stream);
		if (string.IsNullOrEmpty(base64String))
		{
			return;
		}
		byte[] array = Convert.FromBase64String(base64String);
		this.m_stream.Write(array, 0, array.Length);
		this.m_stream.Position = 0L;
	}

	// Token: 0x06000E96 RID: 3734 RVA: 0x00070150 File Offset: 0x0006E350
	public ZPackage(byte[] data)
	{
		this.m_writer = new BinaryWriter(this.m_stream);
		this.m_reader = new BinaryReader(this.m_stream);
		this.m_stream.Write(data, 0, data.Length);
		this.m_stream.Position = 0L;
	}

	// Token: 0x06000E97 RID: 3735 RVA: 0x000701B0 File Offset: 0x0006E3B0
	public ZPackage(byte[] data, int dataSize)
	{
		this.m_writer = new BinaryWriter(this.m_stream);
		this.m_reader = new BinaryReader(this.m_stream);
		this.m_stream.Write(data, 0, dataSize);
		this.m_stream.Position = 0L;
	}

	// Token: 0x06000E98 RID: 3736 RVA: 0x0007020B File Offset: 0x0006E40B
	public void SetReader(BinaryReader reader)
	{
		this.m_reader = reader;
	}

	// Token: 0x06000E99 RID: 3737 RVA: 0x00070214 File Offset: 0x0006E414
	public void SetWriter(BinaryWriter writer)
	{
		this.m_writer = writer;
	}

	// Token: 0x06000E9A RID: 3738 RVA: 0x0007021D File Offset: 0x0006E41D
	public void Load(byte[] data)
	{
		this.Clear();
		this.m_stream.Write(data, 0, data.Length);
		this.m_stream.Position = 0L;
	}

	// Token: 0x06000E9B RID: 3739 RVA: 0x00070244 File Offset: 0x0006E444
	public void Write(ZPackage pkg)
	{
		byte[] array = pkg.GetArray();
		this.m_writer.Write(array.Length);
		this.m_writer.Write(array);
	}

	// Token: 0x06000E9C RID: 3740 RVA: 0x00070274 File Offset: 0x0006E474
	public void WriteCompressed(ZPackage pkg)
	{
		byte[] array = Utils.Compress(pkg.GetArray());
		this.m_writer.Write(array.Length);
		this.m_writer.Write(array);
	}

	// Token: 0x06000E9D RID: 3741 RVA: 0x000702A7 File Offset: 0x0006E4A7
	public void Write(byte[] array)
	{
		this.m_writer.Write(array.Length);
		this.m_writer.Write(array);
	}

	// Token: 0x06000E9E RID: 3742 RVA: 0x000702C3 File Offset: 0x0006E4C3
	public void Write(byte data)
	{
		this.m_writer.Write(data);
	}

	// Token: 0x06000E9F RID: 3743 RVA: 0x000702D1 File Offset: 0x0006E4D1
	public void Write(sbyte data)
	{
		this.m_writer.Write(data);
	}

	// Token: 0x06000EA0 RID: 3744 RVA: 0x000702DF File Offset: 0x0006E4DF
	public void Write(char data)
	{
		this.m_writer.Write(data);
	}

	// Token: 0x06000EA1 RID: 3745 RVA: 0x000702ED File Offset: 0x0006E4ED
	public void Write(bool data)
	{
		this.m_writer.Write(data);
	}

	// Token: 0x06000EA2 RID: 3746 RVA: 0x000702FB File Offset: 0x0006E4FB
	public void Write(int data)
	{
		this.m_writer.Write(data);
	}

	// Token: 0x06000EA3 RID: 3747 RVA: 0x00070309 File Offset: 0x0006E509
	public void Write(uint data)
	{
		this.m_writer.Write(data);
	}

	// Token: 0x06000EA4 RID: 3748 RVA: 0x00070317 File Offset: 0x0006E517
	public void Write(short data)
	{
		this.m_writer.Write(data);
	}

	// Token: 0x06000EA5 RID: 3749 RVA: 0x00070325 File Offset: 0x0006E525
	public void Write(ushort data)
	{
		this.m_writer.Write(data);
	}

	// Token: 0x06000EA6 RID: 3750 RVA: 0x00070333 File Offset: 0x0006E533
	public void Write(long data)
	{
		this.m_writer.Write(data);
	}

	// Token: 0x06000EA7 RID: 3751 RVA: 0x00070341 File Offset: 0x0006E541
	public void Write(ulong data)
	{
		this.m_writer.Write(data);
	}

	// Token: 0x06000EA8 RID: 3752 RVA: 0x0007034F File Offset: 0x0006E54F
	public void Write(float data)
	{
		this.m_writer.Write(data);
	}

	// Token: 0x06000EA9 RID: 3753 RVA: 0x0007035D File Offset: 0x0006E55D
	public void Write(double data)
	{
		this.m_writer.Write(data);
	}

	// Token: 0x06000EAA RID: 3754 RVA: 0x0007036B File Offset: 0x0006E56B
	public void Write(string data)
	{
		this.m_writer.Write(data);
	}

	// Token: 0x06000EAB RID: 3755 RVA: 0x00070379 File Offset: 0x0006E579
	public void Write(ZDOID id)
	{
		this.m_writer.Write(id.UserID);
		this.m_writer.Write(id.ID);
	}

	// Token: 0x06000EAC RID: 3756 RVA: 0x0007039F File Offset: 0x0006E59F
	public void Write(Vector3 v3)
	{
		this.m_writer.Write(v3.x);
		this.m_writer.Write(v3.y);
		this.m_writer.Write(v3.z);
	}

	// Token: 0x06000EAD RID: 3757 RVA: 0x000703D4 File Offset: 0x0006E5D4
	public void Write(Vector2i v2)
	{
		this.m_writer.Write(v2.x);
		this.m_writer.Write(v2.y);
	}

	// Token: 0x06000EAE RID: 3758 RVA: 0x000703F8 File Offset: 0x0006E5F8
	public void Write(Vector2s v2)
	{
		this.m_writer.Write(v2.x);
		this.m_writer.Write(v2.y);
	}

	// Token: 0x06000EAF RID: 3759 RVA: 0x0007041C File Offset: 0x0006E61C
	public void Write(Quaternion q)
	{
		this.m_writer.Write(q.x);
		this.m_writer.Write(q.y);
		this.m_writer.Write(q.z);
		this.m_writer.Write(q.w);
	}

	// Token: 0x06000EB0 RID: 3760 RVA: 0x0007046D File Offset: 0x0006E66D
	public void WriteNumItems(int numItems)
	{
		if (numItems < 128)
		{
			this.m_writer.Write((byte)numItems);
			return;
		}
		this.m_writer.Write((byte)(numItems >> 8 | 128));
		this.m_writer.Write((byte)numItems);
	}

	// Token: 0x06000EB1 RID: 3761 RVA: 0x000704A7 File Offset: 0x0006E6A7
	public ZDOID ReadZDOID()
	{
		return new ZDOID(this.m_reader.ReadInt64(), this.m_reader.ReadUInt32());
	}

	// Token: 0x06000EB2 RID: 3762 RVA: 0x000704C4 File Offset: 0x0006E6C4
	public bool ReadBool()
	{
		return this.m_reader.ReadBoolean();
	}

	// Token: 0x06000EB3 RID: 3763 RVA: 0x000704D1 File Offset: 0x0006E6D1
	public char ReadChar()
	{
		return this.m_reader.ReadChar();
	}

	// Token: 0x06000EB4 RID: 3764 RVA: 0x000704DE File Offset: 0x0006E6DE
	public byte ReadByte()
	{
		return this.m_reader.ReadByte();
	}

	// Token: 0x06000EB5 RID: 3765 RVA: 0x000704EC File Offset: 0x0006E6EC
	public int ReadNumItems()
	{
		int num = (int)this.m_reader.ReadByte();
		if ((num & 128) != 0)
		{
			num = ((num & 127) << 8 | (int)this.m_reader.ReadByte());
		}
		return num;
	}

	// Token: 0x06000EB6 RID: 3766 RVA: 0x00070522 File Offset: 0x0006E722
	public sbyte ReadSByte()
	{
		return this.m_reader.ReadSByte();
	}

	// Token: 0x06000EB7 RID: 3767 RVA: 0x0007052F File Offset: 0x0006E72F
	public short ReadShort()
	{
		return this.m_reader.ReadInt16();
	}

	// Token: 0x06000EB8 RID: 3768 RVA: 0x0007053C File Offset: 0x0006E73C
	public ushort ReadUShort()
	{
		return this.m_reader.ReadUInt16();
	}

	// Token: 0x06000EB9 RID: 3769 RVA: 0x00070549 File Offset: 0x0006E749
	public int ReadInt()
	{
		return this.m_reader.ReadInt32();
	}

	// Token: 0x06000EBA RID: 3770 RVA: 0x00070556 File Offset: 0x0006E756
	public uint ReadUInt()
	{
		return this.m_reader.ReadUInt32();
	}

	// Token: 0x06000EBB RID: 3771 RVA: 0x00070563 File Offset: 0x0006E763
	public long ReadLong()
	{
		return this.m_reader.ReadInt64();
	}

	// Token: 0x06000EBC RID: 3772 RVA: 0x00070570 File Offset: 0x0006E770
	public ulong ReadULong()
	{
		return this.m_reader.ReadUInt64();
	}

	// Token: 0x06000EBD RID: 3773 RVA: 0x0007057D File Offset: 0x0006E77D
	public float ReadSingle()
	{
		return this.m_reader.ReadSingle();
	}

	// Token: 0x06000EBE RID: 3774 RVA: 0x0007058A File Offset: 0x0006E78A
	public double ReadDouble()
	{
		return this.m_reader.ReadDouble();
	}

	// Token: 0x06000EBF RID: 3775 RVA: 0x00070597 File Offset: 0x0006E797
	public string ReadString()
	{
		return this.m_reader.ReadString();
	}

	// Token: 0x06000EC0 RID: 3776 RVA: 0x000705A4 File Offset: 0x0006E7A4
	public Vector3 ReadVector3()
	{
		return new Vector3
		{
			x = this.m_reader.ReadSingle(),
			y = this.m_reader.ReadSingle(),
			z = this.m_reader.ReadSingle()
		};
	}

	// Token: 0x06000EC1 RID: 3777 RVA: 0x000705F0 File Offset: 0x0006E7F0
	public Vector2i ReadVector2i()
	{
		return new Vector2i
		{
			x = this.m_reader.ReadInt32(),
			y = this.m_reader.ReadInt32()
		};
	}

	// Token: 0x06000EC2 RID: 3778 RVA: 0x0007062C File Offset: 0x0006E82C
	public Vector2s ReadVector2s()
	{
		return new Vector2s
		{
			x = this.m_reader.ReadInt16(),
			y = this.m_reader.ReadInt16()
		};
	}

	// Token: 0x06000EC3 RID: 3779 RVA: 0x00070668 File Offset: 0x0006E868
	public Quaternion ReadQuaternion()
	{
		return new Quaternion
		{
			x = this.m_reader.ReadSingle(),
			y = this.m_reader.ReadSingle(),
			z = this.m_reader.ReadSingle(),
			w = this.m_reader.ReadSingle()
		};
	}

	// Token: 0x06000EC4 RID: 3780 RVA: 0x000706C8 File Offset: 0x0006E8C8
	public ZPackage ReadCompressedPackage()
	{
		int count = this.m_reader.ReadInt32();
		return new ZPackage(Utils.Decompress(this.m_reader.ReadBytes(count)));
	}

	// Token: 0x06000EC5 RID: 3781 RVA: 0x000706F8 File Offset: 0x0006E8F8
	public ZPackage ReadPackage()
	{
		int count = this.m_reader.ReadInt32();
		return new ZPackage(this.m_reader.ReadBytes(count));
	}

	// Token: 0x06000EC6 RID: 3782 RVA: 0x00070724 File Offset: 0x0006E924
	public void ReadPackage(ref ZPackage pkg)
	{
		int count = this.m_reader.ReadInt32();
		byte[] array = this.m_reader.ReadBytes(count);
		pkg.Clear();
		pkg.m_stream.Write(array, 0, array.Length);
		pkg.m_stream.Position = 0L;
	}

	// Token: 0x06000EC7 RID: 3783 RVA: 0x00070770 File Offset: 0x0006E970
	public byte[] ReadByteArray()
	{
		int count = this.m_reader.ReadInt32();
		return this.m_reader.ReadBytes(count);
	}

	// Token: 0x06000EC8 RID: 3784 RVA: 0x00070795 File Offset: 0x0006E995
	public byte[] ReadByteArray(int num)
	{
		return this.m_reader.ReadBytes(num);
	}

	// Token: 0x06000EC9 RID: 3785 RVA: 0x000707A3 File Offset: 0x0006E9A3
	public string GetBase64()
	{
		return Convert.ToBase64String(this.GetArray());
	}

	// Token: 0x06000ECA RID: 3786 RVA: 0x000707B0 File Offset: 0x0006E9B0
	public byte[] GetArray()
	{
		this.m_writer.Flush();
		this.m_stream.Flush();
		return this.m_stream.ToArray();
	}

	// Token: 0x06000ECB RID: 3787 RVA: 0x000707D3 File Offset: 0x0006E9D3
	public void SetPos(int pos)
	{
		this.m_stream.Position = (long)pos;
	}

	// Token: 0x06000ECC RID: 3788 RVA: 0x000707E2 File Offset: 0x0006E9E2
	public int GetPos()
	{
		return (int)this.m_stream.Position;
	}

	// Token: 0x06000ECD RID: 3789 RVA: 0x000707F0 File Offset: 0x0006E9F0
	public int Size()
	{
		this.m_writer.Flush();
		this.m_stream.Flush();
		return (int)this.m_stream.Length;
	}

	// Token: 0x06000ECE RID: 3790 RVA: 0x00070814 File Offset: 0x0006EA14
	public void Clear()
	{
		this.m_writer.Flush();
		this.m_stream.SetLength(0L);
		this.m_stream.Position = 0L;
	}

	// Token: 0x06000ECF RID: 3791 RVA: 0x0007083C File Offset: 0x0006EA3C
	public byte[] GenerateHash()
	{
		byte[] array = this.GetArray();
		return SHA512.Create().ComputeHash(array);
	}

	// Token: 0x04000E94 RID: 3732
	private MemoryStream m_stream = new MemoryStream();

	// Token: 0x04000E95 RID: 3733
	private BinaryWriter m_writer;

	// Token: 0x04000E96 RID: 3734
	private BinaryReader m_reader;
}
