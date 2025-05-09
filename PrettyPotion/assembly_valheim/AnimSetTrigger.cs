using System;
using UnityEngine;

// Token: 0x02000010 RID: 16
public class AnimSetTrigger : StateMachineBehaviour
{
	// Token: 0x0600010A RID: 266 RVA: 0x0000D671 File Offset: 0x0000B871
	public override void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
	{
		if (!string.IsNullOrEmpty(this.TriggerOnEnter))
		{
			if (this.TriggerOnEnterEnable)
			{
				animator.SetTrigger(this.TriggerOnEnter);
				return;
			}
			animator.ResetTrigger(this.TriggerOnEnter);
		}
	}

	// Token: 0x0600010B RID: 267 RVA: 0x0000D6A1 File Offset: 0x0000B8A1
	public override void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
	{
		if (!string.IsNullOrEmpty(this.TriggerOnExit))
		{
			if (this.TriggerOnExitEnable)
			{
				animator.SetTrigger(this.TriggerOnExit);
				return;
			}
			animator.ResetTrigger(this.TriggerOnExit);
		}
	}

	// Token: 0x0400021E RID: 542
	public string TriggerOnEnter;

	// Token: 0x0400021F RID: 543
	public bool TriggerOnEnterEnable = true;

	// Token: 0x04000220 RID: 544
	public string TriggerOnExit;

	// Token: 0x04000221 RID: 545
	public bool TriggerOnExitEnable = true;
}
