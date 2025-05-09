using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

// Token: 0x0200002C RID: 44
public class RandomAnimation : MonoBehaviour
{
	// Token: 0x06000467 RID: 1127 RVA: 0x00028365 File Offset: 0x00026565
	private void Start()
	{
		this.m_anim = base.GetComponentInChildren<Animator>();
		this.m_nview = base.GetComponent<ZNetView>();
	}

	// Token: 0x06000468 RID: 1128 RVA: 0x00028380 File Offset: 0x00026580
	private void FixedUpdate()
	{
		if (this.m_nview != null && !this.m_nview.IsValid())
		{
			return;
		}
		float fixedDeltaTime = Time.fixedDeltaTime;
		foreach (RandomAnimation.RandomValue randomValue in this.m_values)
		{
			this.m_sb.Clear();
			this.m_sb.Append("RA_");
			this.m_sb.Append(randomValue.m_name);
			if (this.m_nview == null || this.m_nview.IsOwner())
			{
				randomValue.m_timer += fixedDeltaTime;
				if (randomValue.m_timer > randomValue.m_interval)
				{
					randomValue.m_timer = 0f;
					randomValue.m_value = UnityEngine.Random.Range(0, randomValue.m_values);
					if (this.m_nview)
					{
						this.m_nview.GetZDO().Set(this.m_sb.ToString(), randomValue.m_value);
					}
					if (!randomValue.m_floatValue)
					{
						this.m_anim.SetInteger(randomValue.m_name, randomValue.m_value);
					}
				}
			}
			if (this.m_nview && !this.m_nview.IsOwner())
			{
				int @int = this.m_nview.GetZDO().GetInt(this.m_sb.ToString(), 0);
				if (@int != randomValue.m_value)
				{
					randomValue.m_value = @int;
					if (!randomValue.m_floatValue)
					{
						this.m_anim.SetInteger(randomValue.m_name, randomValue.m_value);
					}
				}
			}
			if (randomValue.m_floatValue)
			{
				if (randomValue.m_hashValues.Count != randomValue.m_values)
				{
					randomValue.m_hashValues.Resize(randomValue.m_values);
					for (int i = 0; i < randomValue.m_values; i++)
					{
						this.m_sb.Clear();
						this.m_sb.Append(randomValue.m_name);
						this.m_sb.Append(i.ToString());
						randomValue.m_hashValues[i] = ZSyncAnimation.GetHash(this.m_sb.ToString());
					}
				}
				for (int j = 0; j < randomValue.m_values; j++)
				{
					float num = this.m_anim.GetFloat(randomValue.m_hashValues[j]);
					if (j == randomValue.m_value)
					{
						num = Mathf.MoveTowards(num, 1f, fixedDeltaTime / randomValue.m_floatTransition);
					}
					else
					{
						num = Mathf.MoveTowards(num, 0f, fixedDeltaTime / randomValue.m_floatTransition);
					}
					this.m_anim.SetFloat(randomValue.m_hashValues[j], num);
				}
			}
		}
	}

	// Token: 0x04000501 RID: 1281
	public List<RandomAnimation.RandomValue> m_values = new List<RandomAnimation.RandomValue>();

	// Token: 0x04000502 RID: 1282
	private Animator m_anim;

	// Token: 0x04000503 RID: 1283
	private ZNetView m_nview;

	// Token: 0x04000504 RID: 1284
	private readonly StringBuilder m_sb = new StringBuilder();

	// Token: 0x0200023F RID: 575
	[Serializable]
	public class RandomValue
	{
		// Token: 0x04001FBB RID: 8123
		public string m_name;

		// Token: 0x04001FBC RID: 8124
		public int m_values;

		// Token: 0x04001FBD RID: 8125
		public float m_interval;

		// Token: 0x04001FBE RID: 8126
		public bool m_floatValue;

		// Token: 0x04001FBF RID: 8127
		public float m_floatTransition = 1f;

		// Token: 0x04001FC0 RID: 8128
		[NonSerialized]
		public float m_timer;

		// Token: 0x04001FC1 RID: 8129
		[NonSerialized]
		public int m_value;

		// Token: 0x04001FC2 RID: 8130
		[NonSerialized]
		public List<int> m_hashValues = new List<int>();
	}
}
