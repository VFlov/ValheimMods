using System;
using Splatform;
using TMPro;
using UnityEngine;

// Token: 0x020001BA RID: 442
public class Sign : MonoBehaviour, Hoverable, Interactable, TextReceiver
{
	// Token: 0x060019D5 RID: 6613 RVA: 0x000C0CFC File Offset: 0x000BEEFC
	private void Awake()
	{
		this.m_currentText = this.m_defaultText;
		this.m_nview = base.GetComponent<ZNetView>();
		if (this.m_nview.GetZDO() == null)
		{
			return;
		}
		this.UpdateText();
		base.InvokeRepeating("UpdateText", 2f, 2f);
	}

	// Token: 0x060019D6 RID: 6614 RVA: 0x000C0D4C File Offset: 0x000BEF4C
	public string GetHoverText()
	{
		string text = this.m_isViewable ? ("\"" + this.GetText().RemoveRichTextTags() + "\"") : "[TEXT HIDDEN, CHECK UGC SETTINGS]";
		if (!PrivateArea.CheckAccess(base.transform.position, 0f, false, false))
		{
			return text;
		}
		string str = "";
		return text + str + "\n" + Localization.instance.Localize(this.m_name + "\n[<color=yellow><b>$KEY_Use</b></color>] $piece_use");
	}

	// Token: 0x060019D7 RID: 6615 RVA: 0x000C0DCA File Offset: 0x000BEFCA
	public string GetHoverName()
	{
		return this.m_name;
	}

	// Token: 0x060019D8 RID: 6616 RVA: 0x000C0DD4 File Offset: 0x000BEFD4
	public bool Interact(Humanoid character, bool hold, bool alt)
	{
		if (hold)
		{
			return false;
		}
		if (!PrivateArea.CheckAccess(base.transform.position, 0f, true, false))
		{
			return false;
		}
		PrivilegeResult privilegeResult = PlatformManager.DistributionPlatform.PrivilegeProvider.CheckPrivilege(Privilege.ViewUserGeneratedContent);
		if (!privilegeResult.IsGranted())
		{
			if (PlatformManager.DistributionPlatform.UIProvider.ResolvePrivilege != null)
			{
				PlatformManager.DistributionPlatform.UIProvider.ResolvePrivilege.Open(Privilege.ViewUserGeneratedContent);
				if (!PlatformManager.DistributionPlatform.UIProvider.ResolvePrivilege.IsOpen)
				{
					ZLog.LogError(string.Format("{0} can't resolve the {1} privilege on this platform, which was denied with result {2}. Modifying sign text was blocked without meaningful feedback to the user!", "ResolvePrivilegeUI", Privilege.ViewUserGeneratedContent, privilegeResult));
				}
			}
			else
			{
				ZLog.LogError(string.Format("{0} is not available on this platform to resolve the {1} privilege, which was denied with result {2}. Modifying sign text was blocked without meaningful feedback to the user!", "ResolvePrivilegeUI", Privilege.ViewUserGeneratedContent, privilegeResult));
			}
			return false;
		}
		TextInput.instance.RequestText(this, "$piece_sign_input", this.m_characterLimit);
		return true;
	}

	// Token: 0x060019D9 RID: 6617 RVA: 0x000C0EB4 File Offset: 0x000BF0B4
	private void UpdateText()
	{
		uint dataRevision = this.m_nview.GetZDO().DataRevision;
		if (this.m_lastRevision == dataRevision)
		{
			return;
		}
		this.m_lastRevision = dataRevision;
		string @string = this.m_nview.GetZDO().GetString(ZDOVars.s_text, this.m_defaultText);
		PlatformUserID user = new PlatformUserID(this.m_nview.GetZDO().GetString(ZDOVars.s_author, ""));
		this.m_currentText = @string;
		this.m_authorDisplayName = this.m_nview.GetZDO().GetString(ZDOVars.s_authorDisplayName, "");
		if (user.IsValid)
		{
			RelationsManager.CheckPermissionAsync(user, Permission.ViewUserGeneratedContent, false, new CheckPermissionCompletedHandler(this.OnCheckPermissionCompleted));
			return;
		}
		this.OnCheckPermissionCompleted(RelationsManagerPermissionResult.Granted);
	}

	// Token: 0x060019DA RID: 6618 RVA: 0x000C0F6C File Offset: 0x000BF16C
	private void OnCheckPermissionCompleted(RelationsManagerPermissionResult result)
	{
		if (!result.IsGranted())
		{
			this.m_isViewable = false;
			this.m_textWidget.text = "ᚬᛏᛁᛚᛚᚴᛅᚾᚴᛚᛁᚴ";
			return;
		}
		this.m_isViewable = true;
		if (result == RelationsManagerPermissionResult.GrantedRequiresFiltering)
		{
			string text;
			CensorShittyWords.Filter(this.m_currentText, out text);
			this.m_textWidget.text = text;
			return;
		}
		this.m_textWidget.text = this.m_currentText;
	}

	// Token: 0x060019DB RID: 6619 RVA: 0x000C0FD0 File Offset: 0x000BF1D0
	public string GetText()
	{
		return this.m_textWidget.text;
	}

	// Token: 0x060019DC RID: 6620 RVA: 0x000C0FDD File Offset: 0x000BF1DD
	public bool UseItem(Humanoid user, ItemDrop.ItemData item)
	{
		return false;
	}

	// Token: 0x060019DD RID: 6621 RVA: 0x000C0FE0 File Offset: 0x000BF1E0
	public void SetText(string text)
	{
		if (!PrivateArea.CheckAccess(base.transform.position, 0f, true, false))
		{
			return;
		}
		this.m_nview.ClaimOwnership();
		this.m_nview.GetZDO().Set(ZDOVars.s_text, text);
		PlatformUserID platformUserID = PlatformManager.DistributionPlatform.LocalUser.PlatformUserID;
		this.m_nview.GetZDO().Set(ZDOVars.s_author, platformUserID.ToString());
		string value;
		if (ZNet.TryGetServerAssignedDisplayName(platformUserID, out value))
		{
			this.m_nview.GetZDO().Set(ZDOVars.s_authorDisplayName, value);
		}
		this.UpdateText();
	}

	// Token: 0x04001A63 RID: 6755
	public TextMeshProUGUI m_textWidget;

	// Token: 0x04001A64 RID: 6756
	public string m_name = "Sign";

	// Token: 0x04001A65 RID: 6757
	public string m_defaultText = "Sign";

	// Token: 0x04001A66 RID: 6758
	public string m_writtenBy = "Written by";

	// Token: 0x04001A67 RID: 6759
	public int m_characterLimit = 50;

	// Token: 0x04001A68 RID: 6760
	private ZNetView m_nview;

	// Token: 0x04001A69 RID: 6761
	private bool m_isViewable = true;

	// Token: 0x04001A6A RID: 6762
	private string m_authorDisplayName = "";

	// Token: 0x04001A6B RID: 6763
	private string m_currentText;

	// Token: 0x04001A6C RID: 6764
	private uint m_lastRevision = uint.MaxValue;
}
