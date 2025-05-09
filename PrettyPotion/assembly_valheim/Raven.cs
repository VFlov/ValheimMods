using System;
using System.Collections.Generic;
using UnityEngine;

// Token: 0x020001AE RID: 430
public class Raven : MonoBehaviour, Hoverable, Interactable, IDestructible
{
	// Token: 0x06001909 RID: 6409 RVA: 0x000BB3F5 File Offset: 0x000B95F5
	public static bool IsInstantiated()
	{
		return Raven.m_instance != null;
	}

	// Token: 0x0600190A RID: 6410 RVA: 0x000BB404 File Offset: 0x000B9604
	private void Awake()
	{
		base.transform.position = new Vector3(0f, 100000f, 0f);
		Raven.m_instance = this;
		this.m_animator = this.m_visual.GetComponentInChildren<Animator>();
		this.m_collider = base.GetComponent<Collider>();
		base.InvokeRepeating("IdleEffect", UnityEngine.Random.Range(this.m_idleEffectIntervalMin, this.m_idleEffectIntervalMax), UnityEngine.Random.Range(this.m_idleEffectIntervalMin, this.m_idleEffectIntervalMax));
		base.InvokeRepeating("CheckSpawn", 1f, 1f);
	}

	// Token: 0x0600190B RID: 6411 RVA: 0x000BB495 File Offset: 0x000B9695
	private void OnDestroy()
	{
		if (Raven.m_instance == this)
		{
			Raven.m_instance = null;
		}
	}

	// Token: 0x0600190C RID: 6412 RVA: 0x000BB4AA File Offset: 0x000B96AA
	public string GetHoverText()
	{
		if (this.IsSpawned())
		{
			return Localization.instance.Localize(this.m_name + "\n[<color=yellow><b>$KEY_Use</b></color>] $raven_interact");
		}
		return "";
	}

	// Token: 0x0600190D RID: 6413 RVA: 0x000BB4D4 File Offset: 0x000B96D4
	public string GetHoverName()
	{
		return Localization.instance.Localize(this.m_name);
	}

	// Token: 0x0600190E RID: 6414 RVA: 0x000BB4E6 File Offset: 0x000B96E6
	public bool Interact(Humanoid character, bool hold, bool alt)
	{
		if (hold)
		{
			return false;
		}
		if (this.m_hasTalked && Chat.instance.IsDialogVisible(base.gameObject))
		{
			Chat.instance.ClearNpcText(base.gameObject);
		}
		else
		{
			this.Talk();
		}
		return false;
	}

	// Token: 0x0600190F RID: 6415 RVA: 0x000BB520 File Offset: 0x000B9720
	private void Talk()
	{
		if (!Player.m_localPlayer)
		{
			return;
		}
		if (this.m_currentText == null)
		{
			return;
		}
		if (this.m_currentText.m_key.Length > 0)
		{
			Player.m_localPlayer.SetSeenTutorial(this.m_currentText.m_key);
			Gogan.LogEvent("Game", "Raven", this.m_currentText.m_key, 0L);
		}
		else
		{
			Gogan.LogEvent("Game", "Raven", this.m_currentText.m_topic, 0L);
		}
		this.m_hasTalked = true;
		if (this.m_currentText.m_label.Length > 0)
		{
			Player.m_localPlayer.AddKnownText(this.m_currentText.m_label, this.m_currentText.m_text);
		}
		this.Say(this.m_currentText.m_topic, this.m_currentText.m_text, false, true, true);
		Game.instance.IncrementPlayerStat(PlayerStatType.RavenTalk, 1f);
	}

	// Token: 0x06001910 RID: 6416 RVA: 0x000BB610 File Offset: 0x000B9810
	private void Say(string topic, string text, bool showName, bool longTimeout, bool large)
	{
		if (topic.Length > 0)
		{
			text = "<color=orange>" + topic + "</color>\n" + text;
		}
		Chat.instance.SetNpcText(base.gameObject, Vector3.up * this.m_textOffset, this.m_textCullDistance, longTimeout ? this.m_longDialogVisibleTime : this.m_dialogVisibleTime, showName ? this.m_name : "", text, large);
		this.m_animator.SetTrigger("talk");
	}

	// Token: 0x06001911 RID: 6417 RVA: 0x000BB694 File Offset: 0x000B9894
	public bool UseItem(Humanoid user, ItemDrop.ItemData item)
	{
		return false;
	}

	// Token: 0x06001912 RID: 6418 RVA: 0x000BB698 File Offset: 0x000B9898
	private void IdleEffect()
	{
		if (!this.IsSpawned())
		{
			return;
		}
		this.m_idleEffect.Create(base.transform.position, base.transform.rotation, null, 1f, -1);
		base.CancelInvoke("IdleEffect");
		base.InvokeRepeating("IdleEffect", UnityEngine.Random.Range(this.m_idleEffectIntervalMin, this.m_idleEffectIntervalMax), UnityEngine.Random.Range(this.m_idleEffectIntervalMin, this.m_idleEffectIntervalMax));
	}

	// Token: 0x06001913 RID: 6419 RVA: 0x000BB70F File Offset: 0x000B990F
	private bool CanHide()
	{
		return Player.m_localPlayer == null || !Chat.instance.IsDialogVisible(base.gameObject);
	}

	// Token: 0x06001914 RID: 6420 RVA: 0x000BB738 File Offset: 0x000B9938
	private void Update()
	{
		this.m_timeSinceTeleport += Time.deltaTime;
		if (!this.IsAway() && !this.IsFlying() && Player.m_localPlayer)
		{
			Vector3 vector = Player.m_localPlayer.transform.position - base.transform.position;
			vector.y = 0f;
			vector.Normalize();
			float f = Vector3.SignedAngle(base.transform.forward, vector, Vector3.up);
			if (Mathf.Abs(f) > this.m_minRotationAngle)
			{
				this.m_animator.SetFloat("anglevel", this.m_rotateSpeed * Mathf.Sign(f), 0.4f, Time.deltaTime);
				base.transform.rotation = Quaternion.RotateTowards(base.transform.rotation, Quaternion.LookRotation(vector), Time.deltaTime * this.m_rotateSpeed);
			}
			else
			{
				this.m_animator.SetFloat("anglevel", 0f, 0.4f, Time.deltaTime);
			}
		}
		if (this.IsSpawned())
		{
			if (Player.m_localPlayer != null && !Chat.instance.IsDialogVisible(base.gameObject) && Vector3.Distance(Player.m_localPlayer.transform.position, base.transform.position) < this.m_autoTalkDistance)
			{
				this.m_randomTextTimer += Time.deltaTime;
				float num = this.m_hasTalked ? this.m_randomTextInterval : this.m_randomTextIntervalImportant;
				if (this.m_randomTextTimer >= num)
				{
					this.m_randomTextTimer = 0f;
					if (this.m_hasTalked)
					{
						this.Say("", this.m_randomTexts[UnityEngine.Random.Range(0, this.m_randomTexts.Count)], false, false, false);
					}
					else
					{
						this.Say("", this.m_randomTextsImportant[UnityEngine.Random.Range(0, this.m_randomTextsImportant.Count)], false, false, false);
					}
				}
			}
			if ((Player.m_localPlayer == null || Vector3.Distance(Player.m_localPlayer.transform.position, base.transform.position) > this.m_despawnDistance || this.EnemyNearby(base.transform.position) || RandEventSystem.InEvent() || this.m_currentText == null || this.m_groundObject == null || this.m_hasTalked) && this.CanHide())
			{
				bool forceTeleport = this.GetBestText() != null || this.m_groundObject == null;
				this.FlyAway(forceTeleport);
				this.RestartSpawnCheck(3f);
			}
			this.m_exclamation.SetActive(!this.m_hasTalked);
			return;
		}
		this.m_exclamation.SetActive(false);
	}

	// Token: 0x06001915 RID: 6421 RVA: 0x000BBA00 File Offset: 0x000B9C00
	private bool FindSpawnPoint(out Vector3 point, out GameObject landOn)
	{
		Vector3 position = Player.m_localPlayer.transform.position;
		Vector3 forward = Utils.GetMainCamera().transform.forward;
		forward.y = 0f;
		forward.Normalize();
		point = new Vector3(0f, -999f, 0f);
		landOn = null;
		bool result = false;
		for (int i = 0; i < 20; i++)
		{
			Vector3 a = Quaternion.Euler(0f, (float)UnityEngine.Random.Range(-30, 30), 0f) * forward;
			Vector3 vector = position + a * UnityEngine.Random.Range(this.m_spawnDistance - 5f, this.m_spawnDistance);
			float num;
			Vector3 vector2;
			GameObject gameObject;
			if (ZoneSystem.instance.GetSolidHeight(vector, out num, out vector2, out gameObject) && num > 30f && num > point.y && num < 2000f && vector2.y > 0.5f && Mathf.Abs(num - position.y) < 2f)
			{
				vector.y = num;
				point = vector;
				landOn = gameObject;
				result = true;
			}
		}
		return result;
	}

	// Token: 0x06001916 RID: 6422 RVA: 0x000BBB24 File Offset: 0x000B9D24
	private bool EnemyNearby(Vector3 point)
	{
		return LootSpawner.IsMonsterInRange(point, this.m_enemyCheckDistance);
	}

	// Token: 0x06001917 RID: 6423 RVA: 0x000BBB34 File Offset: 0x000B9D34
	private bool InState(string name)
	{
		return this.m_animator.isInitialized && (this.m_animator.GetCurrentAnimatorStateInfo(0).IsTag(name) || this.m_animator.GetNextAnimatorStateInfo(0).IsTag(name));
	}

	// Token: 0x06001918 RID: 6424 RVA: 0x000BBB84 File Offset: 0x000B9D84
	private Raven.RavenText GetBestText()
	{
		Raven.RavenText ravenText = this.GetTempText();
		Raven.RavenText closestStaticText = this.GetClosestStaticText(this.m_spawnDistance);
		if (closestStaticText != null && (ravenText == null || closestStaticText.m_priority >= ravenText.m_priority))
		{
			ravenText = closestStaticText;
		}
		return ravenText;
	}

	// Token: 0x06001919 RID: 6425 RVA: 0x000BBBBC File Offset: 0x000B9DBC
	private Raven.RavenText GetTempText()
	{
		foreach (Raven.RavenText ravenText in Raven.m_tempTexts)
		{
			if (ravenText.m_munin == this.m_isMunin)
			{
				return ravenText;
			}
		}
		return null;
	}

	// Token: 0x0600191A RID: 6426 RVA: 0x000BBC1C File Offset: 0x000B9E1C
	private Raven.RavenText GetClosestStaticText(float maxDistance)
	{
		if (Player.m_localPlayer == null)
		{
			return null;
		}
		Raven.RavenText ravenText = null;
		float num = 9999f;
		bool flag = false;
		Vector3 position = Player.m_localPlayer.transform.position;
		foreach (Raven.RavenText ravenText2 in Raven.m_staticTexts)
		{
			if (ravenText2.m_munin == this.m_isMunin && ravenText2.m_guidePoint)
			{
				float num2 = Vector3.Distance(position, ravenText2.m_guidePoint.transform.position);
				if (num2 < maxDistance)
				{
					bool flag2 = ravenText2.m_key.Length > 0 && Player.m_localPlayer.HaveSeenTutorial(ravenText2.m_key);
					if (ravenText2.m_alwaysSpawn || !flag2)
					{
						if (ravenText == null)
						{
							ravenText = ravenText2;
							num = num2;
							flag = flag2;
						}
						else if (flag2 == flag)
						{
							if (ravenText2.m_priority == ravenText.m_priority || flag2)
							{
								if (num2 < num)
								{
									ravenText = ravenText2;
									num = num2;
									flag = flag2;
								}
							}
							else if (ravenText2.m_priority > ravenText.m_priority)
							{
								ravenText = ravenText2;
								num = num2;
								flag = flag2;
							}
						}
						else if (!flag2 && flag)
						{
							ravenText = ravenText2;
							num = num2;
							flag = flag2;
						}
					}
				}
			}
		}
		return ravenText;
	}

	// Token: 0x0600191B RID: 6427 RVA: 0x000BBD74 File Offset: 0x000B9F74
	private void RemoveSeendTempTexts()
	{
		for (int i = 0; i < Raven.m_tempTexts.Count; i++)
		{
			if (Player.m_localPlayer.HaveSeenTutorial(Raven.m_tempTexts[i].m_key))
			{
				Raven.m_tempTexts.RemoveAt(i);
				return;
			}
		}
	}

	// Token: 0x0600191C RID: 6428 RVA: 0x000BBDC0 File Offset: 0x000B9FC0
	private void FlyAway(bool forceTeleport = false)
	{
		Chat.instance.ClearNpcText(base.gameObject);
		if (forceTeleport || this.IsUnderRoof())
		{
			this.m_animator.SetTrigger("poff");
			this.m_timeSinceTeleport = 0f;
			return;
		}
		this.m_animator.SetTrigger("flyaway");
	}

	// Token: 0x0600191D RID: 6429 RVA: 0x000BBE14 File Offset: 0x000BA014
	private void CheckSpawn()
	{
		if (Player.m_localPlayer == null)
		{
			return;
		}
		this.RemoveSeendTempTexts();
		Raven.RavenText bestText = this.GetBestText();
		if (this.IsSpawned() && this.CanHide() && bestText != null && bestText != this.m_currentText)
		{
			this.FlyAway(true);
			this.m_currentText = null;
		}
		if (this.IsAway() && bestText != null)
		{
			if (this.EnemyNearby(base.transform.position))
			{
				return;
			}
			if (RandEventSystem.InEvent())
			{
				return;
			}
			bool forceTeleport = this.m_timeSinceTeleport < 6f;
			this.Spawn(bestText, forceTeleport);
		}
	}

	// Token: 0x0600191E RID: 6430 RVA: 0x000BBEA3 File Offset: 0x000BA0A3
	public DestructibleType GetDestructibleType()
	{
		return DestructibleType.Character;
	}

	// Token: 0x0600191F RID: 6431 RVA: 0x000BBEA6 File Offset: 0x000BA0A6
	public void Damage(HitData hit)
	{
		if (!this.IsSpawned())
		{
			return;
		}
		this.FlyAway(true);
		this.RestartSpawnCheck(4f);
		Game.instance.IncrementPlayerStat(PlayerStatType.RavenHits, 1f);
	}

	// Token: 0x06001920 RID: 6432 RVA: 0x000BBED4 File Offset: 0x000BA0D4
	private void RestartSpawnCheck(float delay)
	{
		base.CancelInvoke("CheckSpawn");
		base.InvokeRepeating("CheckSpawn", delay, 1f);
	}

	// Token: 0x06001921 RID: 6433 RVA: 0x000BBEF2 File Offset: 0x000BA0F2
	private bool IsSpawned()
	{
		return this.InState("visible");
	}

	// Token: 0x06001922 RID: 6434 RVA: 0x000BBEFF File Offset: 0x000BA0FF
	public bool IsAway()
	{
		return this.InState("away");
	}

	// Token: 0x06001923 RID: 6435 RVA: 0x000BBF0C File Offset: 0x000BA10C
	public bool IsFlying()
	{
		return this.InState("flying");
	}

	// Token: 0x06001924 RID: 6436 RVA: 0x000BBF1C File Offset: 0x000BA11C
	private void Spawn(Raven.RavenText text, bool forceTeleport)
	{
		if (Utils.GetMainCamera() == null || !Raven.m_tutorialsEnabled)
		{
			return;
		}
		if (text.m_static)
		{
			this.m_groundObject = text.m_guidePoint.gameObject;
			base.transform.position = text.m_guidePoint.transform.position;
		}
		else
		{
			Vector3 position;
			GameObject groundObject;
			if (!this.FindSpawnPoint(out position, out groundObject))
			{
				return;
			}
			base.transform.position = position;
			this.m_groundObject = groundObject;
		}
		this.m_currentText = text;
		this.m_hasTalked = false;
		this.m_randomTextTimer = 99999f;
		if (this.m_currentText.m_key.Length > 0 && Player.m_localPlayer.HaveSeenTutorial(this.m_currentText.m_key))
		{
			this.m_hasTalked = true;
		}
		Vector3 forward = Player.m_localPlayer.transform.position - base.transform.position;
		forward.y = 0f;
		forward.Normalize();
		base.transform.rotation = Quaternion.LookRotation(forward);
		if (forceTeleport)
		{
			this.m_animator.SetTrigger("teleportin");
		}
		else if (text.m_static)
		{
			if (this.IsUnderRoof())
			{
				this.m_animator.SetTrigger("teleportin");
			}
			else
			{
				this.m_animator.SetTrigger("flyin");
			}
		}
		else
		{
			this.m_animator.SetTrigger("flyin");
		}
		Game.instance.IncrementPlayerStat(PlayerStatType.RavenAppear, 1f);
	}

	// Token: 0x06001925 RID: 6437 RVA: 0x000BC090 File Offset: 0x000BA290
	private bool IsUnderRoof()
	{
		return Physics.Raycast(base.transform.position + Vector3.up * 0.2f, Vector3.up, 20f, LayerMask.GetMask(new string[]
		{
			"Default",
			"static_solid",
			"piece"
		}));
	}

	// Token: 0x06001926 RID: 6438 RVA: 0x000BC0EE File Offset: 0x000BA2EE
	public static void RegisterStaticText(Raven.RavenText text)
	{
		Raven.m_staticTexts.Add(text);
	}

	// Token: 0x06001927 RID: 6439 RVA: 0x000BC0FB File Offset: 0x000BA2FB
	public static void UnregisterStaticText(Raven.RavenText text)
	{
		Raven.m_staticTexts.Remove(text);
	}

	// Token: 0x06001928 RID: 6440 RVA: 0x000BC10C File Offset: 0x000BA30C
	public static void AddTempText(string key, string topic, string text, string label, bool munin)
	{
		if (key.Length > 0)
		{
			using (List<Raven.RavenText>.Enumerator enumerator = Raven.m_tempTexts.GetEnumerator())
			{
				while (enumerator.MoveNext())
				{
					if (enumerator.Current.m_key == key)
					{
						return;
					}
				}
			}
		}
		Raven.RavenText ravenText = new Raven.RavenText();
		ravenText.m_key = key;
		ravenText.m_topic = topic;
		ravenText.m_label = label;
		ravenText.m_text = text;
		ravenText.m_static = false;
		ravenText.m_munin = munin;
		Raven.m_tempTexts.Add(ravenText);
	}

	// Token: 0x0400196F RID: 6511
	public GameObject m_visual;

	// Token: 0x04001970 RID: 6512
	public GameObject m_exclamation;

	// Token: 0x04001971 RID: 6513
	public string m_name = "Name";

	// Token: 0x04001972 RID: 6514
	public bool m_isMunin;

	// Token: 0x04001973 RID: 6515
	public bool m_autoTalk = true;

	// Token: 0x04001974 RID: 6516
	public float m_idleEffectIntervalMin = 10f;

	// Token: 0x04001975 RID: 6517
	public float m_idleEffectIntervalMax = 20f;

	// Token: 0x04001976 RID: 6518
	public float m_spawnDistance = 15f;

	// Token: 0x04001977 RID: 6519
	public float m_despawnDistance = 20f;

	// Token: 0x04001978 RID: 6520
	public float m_autoTalkDistance = 3f;

	// Token: 0x04001979 RID: 6521
	public float m_enemyCheckDistance = 10f;

	// Token: 0x0400197A RID: 6522
	public float m_rotateSpeed = 10f;

	// Token: 0x0400197B RID: 6523
	public float m_minRotationAngle = 15f;

	// Token: 0x0400197C RID: 6524
	public float m_dialogVisibleTime = 10f;

	// Token: 0x0400197D RID: 6525
	public float m_longDialogVisibleTime = 10f;

	// Token: 0x0400197E RID: 6526
	public float m_dontFlyDistance = 3f;

	// Token: 0x0400197F RID: 6527
	public float m_textOffset = 1.5f;

	// Token: 0x04001980 RID: 6528
	public float m_textCullDistance = 20f;

	// Token: 0x04001981 RID: 6529
	public float m_randomTextInterval = 30f;

	// Token: 0x04001982 RID: 6530
	public float m_randomTextIntervalImportant = 10f;

	// Token: 0x04001983 RID: 6531
	public List<string> m_randomTextsImportant = new List<string>();

	// Token: 0x04001984 RID: 6532
	public List<string> m_randomTexts = new List<string>();

	// Token: 0x04001985 RID: 6533
	public EffectList m_idleEffect = new EffectList();

	// Token: 0x04001986 RID: 6534
	public EffectList m_despawnEffect = new EffectList();

	// Token: 0x04001987 RID: 6535
	private Raven.RavenText m_currentText;

	// Token: 0x04001988 RID: 6536
	private GameObject m_groundObject;

	// Token: 0x04001989 RID: 6537
	private Animator m_animator;

	// Token: 0x0400198A RID: 6538
	private Collider m_collider;

	// Token: 0x0400198B RID: 6539
	private bool m_hasTalked;

	// Token: 0x0400198C RID: 6540
	private float m_randomTextTimer = 9999f;

	// Token: 0x0400198D RID: 6541
	private float m_timeSinceTeleport = 9999f;

	// Token: 0x0400198E RID: 6542
	private static List<Raven.RavenText> m_tempTexts = new List<Raven.RavenText>();

	// Token: 0x0400198F RID: 6543
	private static List<Raven.RavenText> m_staticTexts = new List<Raven.RavenText>();

	// Token: 0x04001990 RID: 6544
	private static Raven m_instance = null;

	// Token: 0x04001991 RID: 6545
	public static bool m_tutorialsEnabled = true;

	// Token: 0x0200037D RID: 893
	[Serializable]
	public class RavenText
	{
		// Token: 0x0400265B RID: 9819
		public bool m_alwaysSpawn = true;

		// Token: 0x0400265C RID: 9820
		public bool m_munin;

		// Token: 0x0400265D RID: 9821
		public int m_priority;

		// Token: 0x0400265E RID: 9822
		public string m_key = "";

		// Token: 0x0400265F RID: 9823
		public string m_topic = "";

		// Token: 0x04002660 RID: 9824
		public string m_label = "";

		// Token: 0x04002661 RID: 9825
		[TextArea]
		public string m_text = "";

		// Token: 0x04002662 RID: 9826
		[NonSerialized]
		public bool m_static;

		// Token: 0x04002663 RID: 9827
		[NonSerialized]
		public GuidePoint m_guidePoint;
	}
}
