using System;
using System.Collections.Generic;
using UnityEngine;

// Token: 0x020001B1 RID: 433
public class RuneStone : MonoBehaviour, Hoverable, Interactable
{
	// Token: 0x0600193E RID: 6462 RVA: 0x000BC765 File Offset: 0x000BA965
	public string GetHoverText()
	{
		return Localization.instance.Localize(this.m_name + "\n[<color=yellow><b>$KEY_Use</b></color>] $piece_rune_read");
	}

	// Token: 0x0600193F RID: 6463 RVA: 0x000BC781 File Offset: 0x000BA981
	public string GetHoverName()
	{
		return this.m_name;
	}

	// Token: 0x06001940 RID: 6464 RVA: 0x000BC78C File Offset: 0x000BA98C
	public bool Interact(Humanoid character, bool hold, bool alt)
	{
		if (hold)
		{
			return false;
		}
		Player player = character as Player;
		if (!string.IsNullOrEmpty(this.m_locationName))
		{
			Game.instance.DiscoverClosestLocation(this.m_locationName, base.transform.position, this.m_pinName, (int)this.m_pinType, this.m_showMap, false);
		}
		RuneStone.RandomRuneText randomText = this.GetRandomText();
		if (randomText != null)
		{
			if (randomText.m_label.Length > 0)
			{
				player.AddKnownText(randomText.m_label, randomText.m_text);
			}
			TextViewer.instance.ShowText(TextViewer.Style.Rune, randomText.m_topic, randomText.m_text, true);
		}
		else
		{
			if (this.m_label.Length > 0)
			{
				player.AddKnownText(this.m_label, this.m_text);
			}
			TextViewer.instance.ShowText(TextViewer.Style.Rune, this.m_topic, this.m_text, true);
		}
		return false;
	}

	// Token: 0x06001941 RID: 6465 RVA: 0x000BC85D File Offset: 0x000BAA5D
	public bool UseItem(Humanoid user, ItemDrop.ItemData item)
	{
		return false;
	}

	// Token: 0x06001942 RID: 6466 RVA: 0x000BC860 File Offset: 0x000BAA60
	private RuneStone.RandomRuneText GetRandomText()
	{
		if (this.m_randomTexts.Count == 0)
		{
			return null;
		}
		Vector3 position = base.transform.position;
		int seed = (int)position.x * (int)position.z;
		UnityEngine.Random.State state = UnityEngine.Random.state;
		UnityEngine.Random.InitState(seed);
		RuneStone.RandomRuneText result = this.m_randomTexts[UnityEngine.Random.Range(0, this.m_randomTexts.Count)];
		UnityEngine.Random.state = state;
		return result;
	}

	// Token: 0x040019A6 RID: 6566
	public string m_name = "Rune stone";

	// Token: 0x040019A7 RID: 6567
	public string m_topic = "";

	// Token: 0x040019A8 RID: 6568
	public string m_label = "";

	// Token: 0x040019A9 RID: 6569
	[TextArea]
	public string m_text = "";

	// Token: 0x040019AA RID: 6570
	public List<RuneStone.RandomRuneText> m_randomTexts;

	// Token: 0x040019AB RID: 6571
	public string m_locationName = "";

	// Token: 0x040019AC RID: 6572
	public string m_pinName = "Pin";

	// Token: 0x040019AD RID: 6573
	public Minimap.PinType m_pinType = Minimap.PinType.Boss;

	// Token: 0x040019AE RID: 6574
	public bool m_showMap;

	// Token: 0x0200037E RID: 894
	[Serializable]
	public class RandomRuneText
	{
		// Token: 0x04002664 RID: 9828
		public string m_topic = "";

		// Token: 0x04002665 RID: 9829
		public string m_label = "";

		// Token: 0x04002666 RID: 9830
		public string m_text = "";
	}
}
