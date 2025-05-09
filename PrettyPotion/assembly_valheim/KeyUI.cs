using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

// Token: 0x02000080 RID: 128
public abstract class KeyUI : MonoBehaviour, IPointerEnterHandler, IEventSystemHandler
{
	// Token: 0x06000853 RID: 2131 RVA: 0x00049DBF File Offset: 0x00047FBF
	public virtual void OnPointerEnter(PointerEventData eventData)
	{
		KeyUI.m_lastKeyUI = this;
		this.SetToolTip();
	}

	// Token: 0x06000854 RID: 2132 RVA: 0x00049DCD File Offset: 0x00047FCD
	public void OnValueChanged()
	{
		ServerOptionsGUI.m_instance.OnCustomValueChanged(this);
	}

	// Token: 0x06000855 RID: 2133 RVA: 0x00049DDA File Offset: 0x00047FDA
	public virtual void Update()
	{
		if (KeyUI.m_lastKeyUI != this && EventSystem.current.currentSelectedGameObject == base.gameObject && ZInput.IsGamepadActive())
		{
			this.OnPointerEnter(null);
		}
	}

	// Token: 0x06000856 RID: 2134
	public abstract bool TryMatch(World world, bool checkAllKeys = false);

	// Token: 0x06000857 RID: 2135
	public abstract bool TryMatch(List<string> keys, out string label, bool setElement = true);

	// Token: 0x06000858 RID: 2136
	public abstract void SetKeys(World world);

	// Token: 0x06000859 RID: 2137
	protected abstract void SetToolTip();

	// Token: 0x04000A1F RID: 2591
	public static KeyUI m_lastKeyUI;
}
