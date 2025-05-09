using System;
using System.Collections.Generic;
using UnityEngine;

// Token: 0x0200000E RID: 14
public class NpcTalk : MonoBehaviour
{
	// Token: 0x060000FB RID: 251 RVA: 0x0000D070 File Offset: 0x0000B270
	private void Start()
	{
		this.m_character = base.GetComponentInChildren<Character>();
		this.m_monsterAI = base.GetComponent<MonsterAI>();
		this.m_animator = base.GetComponentInChildren<Animator>();
		this.m_nview = base.GetComponent<ZNetView>();
		MonsterAI monsterAI = this.m_monsterAI;
		monsterAI.m_onBecameAggravated = (Action<BaseAI.AggravatedReason>)Delegate.Combine(monsterAI.m_onBecameAggravated, new Action<BaseAI.AggravatedReason>(this.OnBecameAggravated));
		base.InvokeRepeating("RandomTalk", UnityEngine.Random.Range(this.m_randomTalkInterval / 5f, this.m_randomTalkInterval), this.m_randomTalkInterval);
	}

	// Token: 0x060000FC RID: 252 RVA: 0x0000D0FC File Offset: 0x0000B2FC
	private void Update()
	{
		if (this.m_monsterAI.GetTargetCreature() != null || this.m_monsterAI.GetStaticTarget() != null)
		{
			return;
		}
		if (!this.m_nview.IsValid())
		{
			return;
		}
		this.UpdateTarget();
		if (this.m_targetPlayer)
		{
			if (this.m_nview.IsOwner() && this.m_character.GetVelocity().magnitude < 0.5f)
			{
				Vector3 normalized = (this.m_targetPlayer.GetEyePoint() - this.m_character.GetEyePoint()).normalized;
				this.m_character.SetLookDir(normalized, 0f);
			}
			if (this.m_seeTarget)
			{
				float num = Vector3.Distance(this.m_targetPlayer.transform.position, base.transform.position);
				if (!this.m_didGreet && num < this.m_greetRange)
				{
					this.m_didGreet = true;
					this.QueueSay(this.m_randomGreets, "Greet", this.m_randomGreetFX);
				}
				if (this.m_didGreet && !this.m_didGoodbye && num > this.m_byeRange)
				{
					this.m_didGoodbye = true;
					this.QueueSay(this.m_randomGoodbye, "Greet", this.m_randomGoodbyeFX);
				}
			}
		}
		this.UpdateSayQueue();
	}

	// Token: 0x060000FD RID: 253 RVA: 0x0000D248 File Offset: 0x0000B448
	private void UpdateTarget()
	{
		if (Time.time - this.m_lastTargetUpdate > 1f)
		{
			this.m_lastTargetUpdate = Time.time;
			this.m_targetPlayer = null;
			Player closestPlayer = Player.GetClosestPlayer(base.transform.position, this.m_maxRange);
			if (closestPlayer == null)
			{
				return;
			}
			if (this.m_monsterAI.IsEnemy(closestPlayer))
			{
				return;
			}
			this.m_seeTarget = this.m_monsterAI.CanSeeTarget(closestPlayer);
			this.m_hearTarget = this.m_monsterAI.CanHearTarget(closestPlayer);
			if (!this.m_seeTarget && !this.m_hearTarget)
			{
				return;
			}
			this.m_targetPlayer = closestPlayer;
		}
	}

	// Token: 0x060000FE RID: 254 RVA: 0x0000D2E6 File Offset: 0x0000B4E6
	private void OnBecameAggravated(BaseAI.AggravatedReason reason)
	{
		this.QueueSay(this.m_aggravated, "Aggravated", null);
	}

	// Token: 0x060000FF RID: 255 RVA: 0x0000D2FC File Offset: 0x0000B4FC
	public void OnPrivateAreaAttacked(Character attacker)
	{
		if (attacker.IsPlayer() && this.m_monsterAI.IsAggravatable() && !this.m_monsterAI.IsAggravated() && Vector3.Distance(base.transform.position, attacker.transform.position) < this.m_maxRange)
		{
			this.QueueSay(this.m_privateAreaAlarm, "Angry", null);
		}
	}

	// Token: 0x06000100 RID: 256 RVA: 0x0000D360 File Offset: 0x0000B560
	private void RandomTalk()
	{
		if (Time.time - NpcTalk.m_lastTalkTime < this.m_minTalkInterval)
		{
			return;
		}
		if (UnityEngine.Random.Range(0f, 1f) > this.m_randomTalkChance)
		{
			return;
		}
		this.UpdateTarget();
		if (this.m_targetPlayer && this.m_seeTarget)
		{
			List<string> texts = this.InFactionBase() ? this.m_randomTalkInFactionBase : this.m_randomTalk;
			this.QueueSay(texts, "Talk", this.m_randomTalkFX);
		}
	}

	// Token: 0x06000101 RID: 257 RVA: 0x0000D3E0 File Offset: 0x0000B5E0
	private void QueueSay(List<string> texts, string trigger, EffectList effect)
	{
		if (texts.Count == 0)
		{
			return;
		}
		if (this.m_queuedTexts.Count >= 3)
		{
			return;
		}
		NpcTalk.QueuedSay queuedSay = new NpcTalk.QueuedSay();
		queuedSay.text = texts[UnityEngine.Random.Range(0, texts.Count)];
		queuedSay.trigger = trigger;
		queuedSay.m_effect = effect;
		this.m_queuedTexts.Enqueue(queuedSay);
	}

	// Token: 0x06000102 RID: 258 RVA: 0x0000D440 File Offset: 0x0000B640
	private void UpdateSayQueue()
	{
		if (this.m_queuedTexts.Count == 0)
		{
			return;
		}
		if (Time.time - NpcTalk.m_lastTalkTime < this.m_minTalkInterval)
		{
			return;
		}
		NpcTalk.QueuedSay queuedSay = this.m_queuedTexts.Dequeue();
		this.Say(queuedSay.text, queuedSay.trigger);
		if (queuedSay.m_effect != null)
		{
			queuedSay.m_effect.Create(base.transform.position, Quaternion.identity, null, 1f, -1);
		}
	}

	// Token: 0x06000103 RID: 259 RVA: 0x0000D4B8 File Offset: 0x0000B6B8
	private void Say(string text, string trigger)
	{
		NpcTalk.m_lastTalkTime = Time.time;
		Chat.instance.SetNpcText(base.gameObject, Vector3.up * this.m_offset, 20f, this.m_hideDialogDelay, "", text, false);
		if (trigger.Length > 0)
		{
			this.m_animator.SetTrigger(trigger);
		}
	}

	// Token: 0x06000104 RID: 260 RVA: 0x0000D516 File Offset: 0x0000B716
	private bool InFactionBase()
	{
		return PrivateArea.InsideFactionArea(base.transform.position, this.m_character.GetFaction());
	}

	// Token: 0x040001FE RID: 510
	private float m_lastTargetUpdate;

	// Token: 0x040001FF RID: 511
	public string m_name = "Haldor";

	// Token: 0x04000200 RID: 512
	public float m_maxRange = 15f;

	// Token: 0x04000201 RID: 513
	public float m_greetRange = 10f;

	// Token: 0x04000202 RID: 514
	public float m_byeRange = 15f;

	// Token: 0x04000203 RID: 515
	public float m_offset = 2f;

	// Token: 0x04000204 RID: 516
	public float m_minTalkInterval = 1.5f;

	// Token: 0x04000205 RID: 517
	private const int m_maxQueuedTexts = 3;

	// Token: 0x04000206 RID: 518
	public float m_hideDialogDelay = 5f;

	// Token: 0x04000207 RID: 519
	public float m_randomTalkInterval = 10f;

	// Token: 0x04000208 RID: 520
	public float m_randomTalkChance = 1f;

	// Token: 0x04000209 RID: 521
	public List<string> m_randomTalk = new List<string>();

	// Token: 0x0400020A RID: 522
	public List<string> m_randomTalkInFactionBase = new List<string>();

	// Token: 0x0400020B RID: 523
	public List<string> m_randomGreets = new List<string>();

	// Token: 0x0400020C RID: 524
	public List<string> m_randomGoodbye = new List<string>();

	// Token: 0x0400020D RID: 525
	public List<string> m_privateAreaAlarm = new List<string>();

	// Token: 0x0400020E RID: 526
	public List<string> m_aggravated = new List<string>();

	// Token: 0x0400020F RID: 527
	public EffectList m_randomTalkFX = new EffectList();

	// Token: 0x04000210 RID: 528
	public EffectList m_randomGreetFX = new EffectList();

	// Token: 0x04000211 RID: 529
	public EffectList m_randomGoodbyeFX = new EffectList();

	// Token: 0x04000212 RID: 530
	private bool m_didGreet;

	// Token: 0x04000213 RID: 531
	private bool m_didGoodbye;

	// Token: 0x04000214 RID: 532
	private MonsterAI m_monsterAI;

	// Token: 0x04000215 RID: 533
	private Animator m_animator;

	// Token: 0x04000216 RID: 534
	private Character m_character;

	// Token: 0x04000217 RID: 535
	private ZNetView m_nview;

	// Token: 0x04000218 RID: 536
	private Player m_targetPlayer;

	// Token: 0x04000219 RID: 537
	private bool m_seeTarget;

	// Token: 0x0400021A RID: 538
	private bool m_hearTarget;

	// Token: 0x0400021B RID: 539
	private Queue<NpcTalk.QueuedSay> m_queuedTexts = new Queue<NpcTalk.QueuedSay>();

	// Token: 0x0400021C RID: 540
	private static float m_lastTalkTime;

	// Token: 0x0200022A RID: 554
	private class QueuedSay
	{
		// Token: 0x04001F41 RID: 8001
		public string text;

		// Token: 0x04001F42 RID: 8002
		public string trigger;

		// Token: 0x04001F43 RID: 8003
		public EffectList m_effect;
	}
}
