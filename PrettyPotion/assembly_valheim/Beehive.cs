using System;
using UnityEngine;

// Token: 0x0200015C RID: 348
public class Beehive : MonoBehaviour, Hoverable, Interactable
{
	// Token: 0x06001539 RID: 5433 RVA: 0x0009BC9C File Offset: 0x00099E9C
	private void Awake()
	{
		this.m_nview = base.GetComponent<ZNetView>();
		this.m_collider = base.GetComponentInChildren<Collider>();
		this.m_piece = base.GetComponent<Piece>();
		if (this.m_nview.GetZDO() == null)
		{
			return;
		}
		if (this.m_nview.IsOwner() && this.m_nview.GetZDO().GetLong(ZDOVars.s_lastTime, 0L) == 0L)
		{
			this.m_nview.GetZDO().Set(ZDOVars.s_lastTime, ZNet.instance.GetTime().Ticks);
		}
		this.m_nview.Register("RPC_Extract", new Action<long>(this.RPC_Extract));
		base.InvokeRepeating("UpdateBees", 0f, 10f);
	}

	// Token: 0x0600153A RID: 5434 RVA: 0x0009BD5C File Offset: 0x00099F5C
	public string GetHoverText()
	{
		if (!PrivateArea.CheckAccess(base.transform.position, 0f, false, false))
		{
			return Localization.instance.Localize(this.m_name + "\n$piece_noaccess");
		}
		int honeyLevel = this.GetHoneyLevel();
		if (honeyLevel > 0)
		{
			return Localization.instance.Localize(string.Format("{0} ( {1} x {2} )\n[<color=yellow><b>$KEY_Use</b></color>] {3}", new object[]
			{
				this.m_name,
				this.m_honeyItem.m_itemData.m_shared.m_name,
				honeyLevel,
				this.m_extractText
			}));
		}
		return Localization.instance.Localize(this.m_name + " ( $piece_container_empty )\n[<color=yellow><b>$KEY_Use</b></color>] " + this.m_checkText);
	}

	// Token: 0x0600153B RID: 5435 RVA: 0x0009BE16 File Offset: 0x0009A016
	public string GetHoverName()
	{
		return this.m_name;
	}

	// Token: 0x0600153C RID: 5436 RVA: 0x0009BE20 File Offset: 0x0009A020
	public bool Interact(Humanoid character, bool repeat, bool alt)
	{
		if (repeat)
		{
			return false;
		}
		if (!PrivateArea.CheckAccess(base.transform.position, 0f, true, false))
		{
			return true;
		}
		if (this.GetHoneyLevel() > 0)
		{
			this.Extract();
			Game.instance.IncrementPlayerStat(PlayerStatType.BeesHarvested, 1f);
		}
		else
		{
			if (!this.CheckBiome())
			{
				character.Message(MessageHud.MessageType.Center, this.m_areaText, 0, null);
				return true;
			}
			if (!this.HaveFreeSpace())
			{
				character.Message(MessageHud.MessageType.Center, this.m_freespaceText, 0, null);
				return true;
			}
			if (!EnvMan.IsDaylight() && this.m_effectOnlyInDaylight)
			{
				character.Message(MessageHud.MessageType.Center, this.m_sleepText, 0, null);
				return true;
			}
			character.Message(MessageHud.MessageType.Center, this.m_happyText, 0, null);
		}
		return true;
	}

	// Token: 0x0600153D RID: 5437 RVA: 0x0009BED1 File Offset: 0x0009A0D1
	public bool UseItem(Humanoid user, ItemDrop.ItemData item)
	{
		return false;
	}

	// Token: 0x0600153E RID: 5438 RVA: 0x0009BED4 File Offset: 0x0009A0D4
	private void Extract()
	{
		this.m_nview.InvokeRPC("RPC_Extract", Array.Empty<object>());
	}

	// Token: 0x0600153F RID: 5439 RVA: 0x0009BEEC File Offset: 0x0009A0EC
	private void RPC_Extract(long caller)
	{
		int honeyLevel = this.GetHoneyLevel();
		if (honeyLevel > 0)
		{
			this.m_spawnEffect.Create(this.m_spawnPoint.position, Quaternion.identity, null, 1f, -1);
			for (int i = 0; i < honeyLevel; i++)
			{
				Vector2 vector = UnityEngine.Random.insideUnitCircle * 0.5f;
				Vector3 position = this.m_spawnPoint.position + new Vector3(vector.x, 0.25f * (float)i, vector.y);
				ItemDrop component = UnityEngine.Object.Instantiate<ItemDrop>(this.m_honeyItem, position, Quaternion.identity).GetComponent<ItemDrop>();
				if (component != null)
				{
					component.SetStack(Game.instance.ScaleDrops(this.m_honeyItem.m_itemData, 1));
				}
			}
			this.ResetLevel();
		}
	}

	// Token: 0x06001540 RID: 5440 RVA: 0x0009BFB0 File Offset: 0x0009A1B0
	private float GetTimeSinceLastUpdate()
	{
		DateTime d = new DateTime(this.m_nview.GetZDO().GetLong(ZDOVars.s_lastTime, ZNet.instance.GetTime().Ticks));
		DateTime time = ZNet.instance.GetTime();
		TimeSpan timeSpan = time - d;
		this.m_nview.GetZDO().Set(ZDOVars.s_lastTime, time.Ticks);
		double num = timeSpan.TotalSeconds;
		if (num < 0.0)
		{
			num = 0.0;
		}
		return (float)num;
	}

	// Token: 0x06001541 RID: 5441 RVA: 0x0009C03B File Offset: 0x0009A23B
	private void ResetLevel()
	{
		this.m_nview.GetZDO().Set(ZDOVars.s_level, 0, false);
	}

	// Token: 0x06001542 RID: 5442 RVA: 0x0009C054 File Offset: 0x0009A254
	private void IncreseLevel(int i)
	{
		int num = this.GetHoneyLevel();
		num += i;
		num = Mathf.Clamp(num, 0, this.m_maxHoney);
		this.m_nview.GetZDO().Set(ZDOVars.s_level, num, false);
	}

	// Token: 0x06001543 RID: 5443 RVA: 0x0009C091 File Offset: 0x0009A291
	private int GetHoneyLevel()
	{
		return this.m_nview.GetZDO().GetInt(ZDOVars.s_level, 0);
	}

	// Token: 0x06001544 RID: 5444 RVA: 0x0009C0AC File Offset: 0x0009A2AC
	private void UpdateBees()
	{
		bool flag = this.CheckBiome() && this.HaveFreeSpace();
		bool active = flag && (!this.m_effectOnlyInDaylight || EnvMan.IsDaylight());
		this.m_beeEffect.SetActive(active);
		if (this.m_nview.IsOwner() && flag)
		{
			float timeSinceLastUpdate = this.GetTimeSinceLastUpdate();
			float num = this.m_nview.GetZDO().GetFloat(ZDOVars.s_product, 0f);
			num += timeSinceLastUpdate;
			if (num > this.m_secPerUnit)
			{
				int i = (int)(num / this.m_secPerUnit);
				this.IncreseLevel(i);
				num = 0f;
			}
			this.m_nview.GetZDO().Set(ZDOVars.s_product, num);
		}
	}

	// Token: 0x06001545 RID: 5445 RVA: 0x0009C15C File Offset: 0x0009A35C
	private bool HaveFreeSpace()
	{
		if (this.m_maxCover <= 0f)
		{
			return true;
		}
		float num;
		bool flag;
		Cover.GetCoverForPoint(this.m_coverPoint.position, out num, out flag, 0.5f);
		return num < this.m_maxCover;
	}

	// Token: 0x06001546 RID: 5446 RVA: 0x0009C19A File Offset: 0x0009A39A
	private bool CheckBiome()
	{
		return (Heightmap.FindBiome(base.transform.position) & this.m_biome) > Heightmap.Biome.None;
	}

	// Token: 0x040014A0 RID: 5280
	public string m_name = "";

	// Token: 0x040014A1 RID: 5281
	public Transform m_coverPoint;

	// Token: 0x040014A2 RID: 5282
	public Transform m_spawnPoint;

	// Token: 0x040014A3 RID: 5283
	public GameObject m_beeEffect;

	// Token: 0x040014A4 RID: 5284
	public bool m_effectOnlyInDaylight = true;

	// Token: 0x040014A5 RID: 5285
	public float m_maxCover = 0.25f;

	// Token: 0x040014A6 RID: 5286
	[BitMask(typeof(Heightmap.Biome))]
	public Heightmap.Biome m_biome;

	// Token: 0x040014A7 RID: 5287
	public float m_secPerUnit = 10f;

	// Token: 0x040014A8 RID: 5288
	public int m_maxHoney = 4;

	// Token: 0x040014A9 RID: 5289
	public ItemDrop m_honeyItem;

	// Token: 0x040014AA RID: 5290
	public EffectList m_spawnEffect = new EffectList();

	// Token: 0x040014AB RID: 5291
	[Header("Texts")]
	public string m_extractText = "$piece_beehive_extract";

	// Token: 0x040014AC RID: 5292
	public string m_checkText = "$piece_beehive_check";

	// Token: 0x040014AD RID: 5293
	public string m_areaText = "$piece_beehive_area";

	// Token: 0x040014AE RID: 5294
	public string m_freespaceText = "$piece_beehive_freespace";

	// Token: 0x040014AF RID: 5295
	public string m_sleepText = "$piece_beehive_sleep";

	// Token: 0x040014B0 RID: 5296
	public string m_happyText = "$piece_beehive_happy";

	// Token: 0x040014B1 RID: 5297
	public string m_notConnectedText;

	// Token: 0x040014B2 RID: 5298
	public string m_blockedText;

	// Token: 0x040014B3 RID: 5299
	private ZNetView m_nview;

	// Token: 0x040014B4 RID: 5300
	private Collider m_collider;

	// Token: 0x040014B5 RID: 5301
	private Piece m_piece;

	// Token: 0x040014B6 RID: 5302
	private ZNetView m_connectedObject;

	// Token: 0x040014B7 RID: 5303
	private Piece m_blockingPiece;
}
