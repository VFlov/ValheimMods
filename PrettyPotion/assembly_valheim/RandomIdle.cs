using System;
using UnityEngine;

// Token: 0x0200002D RID: 45
public class RandomIdle : StateMachineBehaviour
{
	// Token: 0x0600046A RID: 1130 RVA: 0x0002866C File Offset: 0x0002686C
	public override void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
	{
		int randomIdle = this.GetRandomIdle(animator);
		animator.SetFloat(this.m_valueName, (float)randomIdle);
		this.m_last = stateInfo.normalizedTime % 1f;
	}

	// Token: 0x0600046B RID: 1131 RVA: 0x000286A4 File Offset: 0x000268A4
	public override void OnStateUpdate(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
	{
		float num = stateInfo.normalizedTime % 1f;
		if (num < this.m_last)
		{
			int randomIdle = this.GetRandomIdle(animator);
			animator.SetFloat(this.m_valueName, (float)randomIdle);
		}
		this.m_last = num;
	}

	// Token: 0x0600046C RID: 1132 RVA: 0x000286E8 File Offset: 0x000268E8
	private int GetRandomIdle(Animator animator)
	{
		if (!this.m_haveSetup)
		{
			this.m_haveSetup = true;
			this.m_baseAI = animator.GetComponentInParent<BaseAI>();
			this.m_character = animator.GetComponentInParent<Character>();
		}
		if (this.m_baseAI && this.m_alertedIdle >= 0 && this.m_baseAI.IsAlerted())
		{
			return this.m_alertedIdle;
		}
		return UnityEngine.Random.Range(0, (this.m_animationsWhenTamed > 0 && this.m_character != null && this.m_character.IsTamed()) ? this.m_animationsWhenTamed : this.m_animations);
	}

	// Token: 0x04000505 RID: 1285
	public int m_animations = 4;

	// Token: 0x04000506 RID: 1286
	public int m_animationsWhenTamed;

	// Token: 0x04000507 RID: 1287
	public string m_valueName = "";

	// Token: 0x04000508 RID: 1288
	public int m_alertedIdle = -1;

	// Token: 0x04000509 RID: 1289
	private float m_last;

	// Token: 0x0400050A RID: 1290
	private bool m_haveSetup;

	// Token: 0x0400050B RID: 1291
	private BaseAI m_baseAI;

	// Token: 0x0400050C RID: 1292
	private Character m_character;
}
