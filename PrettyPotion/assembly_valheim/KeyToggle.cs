using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// Token: 0x02000081 RID: 129
public class KeyToggle : KeyUI
{
	// Token: 0x0600085B RID: 2139 RVA: 0x00049E16 File Offset: 0x00048016
	public void Awake()
	{
		this.m_toggle = base.GetComponentInParent<Toggle>();
		this.m_toggle.isOn = this.m_defaultOn;
		this.m_toggle.onValueChanged.AddListener(delegate(bool f)
		{
			base.OnValueChanged();
		});
	}

	// Token: 0x0600085C RID: 2140 RVA: 0x00049E51 File Offset: 0x00048051
	public override void Update()
	{
		if (ZInput.IsGamepadActive() && EventSystem.current.currentSelectedGameObject == this.m_toggle.gameObject)
		{
			this.SetToolTip();
		}
		base.Update();
	}

	// Token: 0x0600085D RID: 2141 RVA: 0x00049E82 File Offset: 0x00048082
	protected override void SetToolTip()
	{
		if (this.m_toolTipLabel)
		{
			this.m_toolTipLabel.text = Localization.instance.Localize(this.m_toolTip);
		}
	}

	// Token: 0x0600085E RID: 2142 RVA: 0x00049EAC File Offset: 0x000480AC
	public override void SetKeys(World world)
	{
		if (this.m_toggle.isOn)
		{
			string text = this.m_enabledKey.ToLower();
			if (ZoneSystem.instance)
			{
				ZoneSystem.instance.SetGlobalKey(text);
				return;
			}
			if (!world.m_startingGlobalKeys.Contains(text))
			{
				world.m_startingGlobalKeys.Add(this.m_enabledKey.ToLower());
			}
		}
	}

	// Token: 0x0600085F RID: 2143 RVA: 0x00049F10 File Offset: 0x00048110
	public override bool TryMatch(World world, bool checkAllKeys = false)
	{
		return this.m_toggle.isOn = world.m_startingGlobalKeys.Contains(this.m_enabledKey.ToLower());
	}

	// Token: 0x06000860 RID: 2144 RVA: 0x00049F44 File Offset: 0x00048144
	public override bool TryMatch(List<string> keys, out string label, bool setToggle = true)
	{
		this.m_toggle.isOn = false;
		using (List<string>.Enumerator enumerator = keys.GetEnumerator())
		{
			while (enumerator.MoveNext())
			{
				if (enumerator.Current.ToLower() == this.m_enabledKey.ToLower())
				{
					if (setToggle)
					{
						this.m_toggle.isOn = true;
					}
					TMP_Text componentInChildren = base.GetComponentInChildren<TMP_Text>();
					label = ((componentInChildren != null) ? componentInChildren.text : this.m_enabledKey);
					return true;
				}
			}
		}
		label = null;
		return false;
	}

	// Token: 0x04000A20 RID: 2592
	private Toggle m_toggle;

	// Token: 0x04000A21 RID: 2593
	public TMP_Text m_toolTipLabel;

	// Token: 0x04000A22 RID: 2594
	public string m_toolTip;

	// Token: 0x04000A23 RID: 2595
	public bool m_defaultOn;

	// Token: 0x04000A24 RID: 2596
	public string m_enabledKey;
}
