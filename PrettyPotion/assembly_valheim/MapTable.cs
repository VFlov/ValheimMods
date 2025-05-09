using System;
using UnityEngine;

// Token: 0x02000195 RID: 405
public class MapTable : MonoBehaviour
{
	// Token: 0x06001818 RID: 6168 RVA: 0x000B3D7C File Offset: 0x000B1F7C
	private void Start()
	{
		this.m_nview = base.GetComponent<ZNetView>();
		this.m_nview.Register<ZPackage>("MapData", new Action<long, ZPackage>(this.RPC_MapData));
		Switch readSwitch = this.m_readSwitch;
		readSwitch.m_onUse = (Switch.Callback)Delegate.Combine(readSwitch.m_onUse, new Switch.Callback(this.OnRead));
		Switch readSwitch2 = this.m_readSwitch;
		readSwitch2.m_onHover = (Switch.TooltipCallback)Delegate.Combine(readSwitch2.m_onHover, new Switch.TooltipCallback(this.GetReadHoverText));
		Switch writeSwitch = this.m_writeSwitch;
		writeSwitch.m_onUse = (Switch.Callback)Delegate.Combine(writeSwitch.m_onUse, new Switch.Callback(this.OnWrite));
		Switch writeSwitch2 = this.m_writeSwitch;
		writeSwitch2.m_onHover = (Switch.TooltipCallback)Delegate.Combine(writeSwitch2.m_onHover, new Switch.TooltipCallback(this.GetWriteHoverText));
	}

	// Token: 0x06001819 RID: 6169 RVA: 0x000B3E50 File Offset: 0x000B2050
	private string GetReadHoverText()
	{
		if (!PrivateArea.CheckAccess(base.transform.position, 0f, false, false))
		{
			return Localization.instance.Localize(this.m_name + "\n$piece_noaccess");
		}
		return Localization.instance.Localize(this.m_name + "\n[<color=yellow><b>$KEY_Use</b></color>] $piece_readmap ");
	}

	// Token: 0x0600181A RID: 6170 RVA: 0x000B3EAC File Offset: 0x000B20AC
	private string GetWriteHoverText()
	{
		if (!PrivateArea.CheckAccess(base.transform.position, 0f, false, false))
		{
			return Localization.instance.Localize(this.m_name + "\n$piece_noaccess");
		}
		return Localization.instance.Localize(this.m_name + "\n[<color=yellow><b>$KEY_Use</b></color>] $piece_writemap ");
	}

	// Token: 0x0600181B RID: 6171 RVA: 0x000B3F07 File Offset: 0x000B2107
	private bool OnRead(Switch caller, Humanoid user, ItemDrop.ItemData item)
	{
		return this.OnRead(caller, user, item, true);
	}

	// Token: 0x0600181C RID: 6172 RVA: 0x000B3F14 File Offset: 0x000B2114
	private bool OnRead(Switch caller, Humanoid user, ItemDrop.ItemData item, bool showMessage)
	{
		if (item != null)
		{
			return false;
		}
		float realtimeSinceStartup = Time.realtimeSinceStartup;
		byte[] byteArray = this.m_nview.GetZDO().GetByteArray(ZDOVars.s_data, null);
		if (byteArray != null)
		{
			byte[] dataArray = Utils.Decompress(byteArray);
			bool flag = Minimap.instance.AddSharedMapData(dataArray);
			if (showMessage)
			{
				if (flag)
				{
					user.Message(MessageHud.MessageType.Center, "$msg_mapsynced", 0, null);
				}
				else
				{
					user.Message(MessageHud.MessageType.Center, "$msg_alreadysynced", 0, null);
				}
			}
		}
		else if (showMessage)
		{
			user.Message(MessageHud.MessageType.Center, "$msg_mapnodata", 0, null);
		}
		return false;
	}

	// Token: 0x0600181D RID: 6173 RVA: 0x000B3F94 File Offset: 0x000B2194
	private bool OnWrite(Switch caller, Humanoid user, ItemDrop.ItemData item)
	{
		this.OnRead(caller, user, item, false);
		if (item != null)
		{
			return false;
		}
		if (!this.m_nview.IsValid())
		{
			return false;
		}
		if (!PrivateArea.CheckAccess(base.transform.position, 0f, true, false))
		{
			return true;
		}
		byte[] array = this.m_nview.GetZDO().GetByteArray(ZDOVars.s_data, null);
		if (array != null)
		{
			array = Utils.Decompress(array);
		}
		ZPackage mapData = this.GetMapData(array);
		this.m_nview.InvokeRPC("MapData", new object[]
		{
			mapData
		});
		user.Message(MessageHud.MessageType.Center, "$msg_mapsaved", 0, null);
		this.m_writeEffects.Create(base.transform.position, base.transform.rotation, null, 1f, -1);
		return true;
	}

	// Token: 0x0600181E RID: 6174 RVA: 0x000B4058 File Offset: 0x000B2258
	private void RPC_MapData(long sender, ZPackage pkg)
	{
		if (!this.m_nview.IsOwner())
		{
			return;
		}
		byte[] array = pkg.GetArray();
		this.m_nview.GetZDO().Set(ZDOVars.s_data, array);
	}

	// Token: 0x0600181F RID: 6175 RVA: 0x000B4090 File Offset: 0x000B2290
	private ZPackage GetMapData(byte[] currentMapData)
	{
		byte[] array = Utils.Compress(Minimap.instance.GetSharedMapData(currentMapData));
		ZLog.Log("Compressed map data:" + array.Length.ToString());
		return new ZPackage(array);
	}

	// Token: 0x06001820 RID: 6176 RVA: 0x000B40CE File Offset: 0x000B22CE
	public bool UseItem(Humanoid user, ItemDrop.ItemData item)
	{
		return false;
	}

	// Token: 0x0400180D RID: 6157
	public string m_name = "$piece_maptable";

	// Token: 0x0400180E RID: 6158
	public Switch m_readSwitch;

	// Token: 0x0400180F RID: 6159
	public Switch m_writeSwitch;

	// Token: 0x04001810 RID: 6160
	public EffectList m_writeEffects = new EffectList();

	// Token: 0x04001811 RID: 6161
	private ZNetView m_nview;
}
