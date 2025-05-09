using System;
using UnityEngine;

// Token: 0x02000037 RID: 55
public class SE_Harpooned : StatusEffect
{
	// Token: 0x060004EC RID: 1260 RVA: 0x0002B813 File Offset: 0x00029A13
	public override void Setup(Character character)
	{
		base.Setup(character);
	}

	// Token: 0x060004ED RID: 1261 RVA: 0x0002B81C File Offset: 0x00029A1C
	public override void SetAttacker(Character attacker)
	{
		ZLog.Log("Setting attacker " + attacker.m_name);
		this.m_attacker = attacker;
		this.m_time = 0f;
		if (this.m_character.IsBoss())
		{
			this.m_broken = true;
			return;
		}
		float num = Vector3.Distance(this.m_attacker.transform.position, this.m_character.transform.position);
		if (num > this.m_maxDistance)
		{
			this.m_attacker.Message(MessageHud.MessageType.Center, "$msg_harpoon_targettoofar", 0, null);
			this.m_broken = true;
			return;
		}
		this.m_baseDistance = num;
		this.m_attacker.Message(MessageHud.MessageType.Center, this.m_character.m_name + " $msg_harpoon_harpooned", 0, null);
		foreach (GameObject gameObject in this.m_startEffectInstances)
		{
			if (gameObject)
			{
				LineConnect component = gameObject.GetComponent<LineConnect>();
				if (component)
				{
					component.SetPeer(this.m_attacker.GetComponent<ZNetView>());
					this.m_line = component;
				}
			}
		}
	}

	// Token: 0x060004EE RID: 1262 RVA: 0x0002B928 File Offset: 0x00029B28
	public override void UpdateStatusEffect(float dt)
	{
		base.UpdateStatusEffect(dt);
		if (!this.m_attacker)
		{
			return;
		}
		Rigidbody component = this.m_character.GetComponent<Rigidbody>();
		if (component)
		{
			float num = Vector3.Distance(this.m_attacker.transform.position, this.m_character.transform.position);
			if (this.m_character.GetStandingOnShip() == null && !this.m_character.IsAttached())
			{
				float num2 = Utils.Pull(component, this.m_attacker.transform.position, this.m_baseDistance, this.m_pullSpeed, this.m_pullForce, this.m_smoothDistance, true, true, this.m_forcePower);
				this.m_drainStaminaTimer += dt;
				if (this.m_drainStaminaTimer > this.m_staminaDrainInterval && num2 > 0f)
				{
					this.m_drainStaminaTimer = 0f;
					float stamina = this.m_staminaDrain * num2 * this.m_character.GetMass();
					this.m_attacker.UseStamina(stamina);
				}
			}
			if (this.m_line)
			{
				this.m_line.SetSlack((1f - Utils.LerpStep(this.m_baseDistance / 2f, this.m_baseDistance, num)) * this.m_maxLineSlack);
			}
			if (num - this.m_baseDistance > this.m_breakDistance)
			{
				this.m_broken = true;
				this.m_attacker.Message(MessageHud.MessageType.Center, "$msg_harpoon_linebroke", 0, null);
			}
			if (!this.m_attacker.HaveStamina(0f))
			{
				this.m_broken = true;
				this.m_attacker.Message(MessageHud.MessageType.Center, this.m_character.m_name + " $msg_harpoon_released", 0, null);
			}
		}
	}

	// Token: 0x060004EF RID: 1263 RVA: 0x0002BAD8 File Offset: 0x00029CD8
	public override bool IsDone()
	{
		if (base.IsDone())
		{
			return true;
		}
		if (this.m_broken)
		{
			return true;
		}
		if (!this.m_attacker)
		{
			return true;
		}
		if (this.m_time > 2f && (this.m_attacker.IsBlocking() || this.m_attacker.InAttack()))
		{
			this.m_attacker.Message(MessageHud.MessageType.Center, this.m_character.m_name + " released", 0, null);
			return true;
		}
		return false;
	}

	// Token: 0x0400057A RID: 1402
	[Header("SE_Harpooned")]
	public float m_pullForce;

	// Token: 0x0400057B RID: 1403
	public float m_forcePower = 2f;

	// Token: 0x0400057C RID: 1404
	public float m_pullSpeed = 5f;

	// Token: 0x0400057D RID: 1405
	public float m_smoothDistance = 2f;

	// Token: 0x0400057E RID: 1406
	public float m_maxLineSlack = 0.3f;

	// Token: 0x0400057F RID: 1407
	public float m_breakDistance = 4f;

	// Token: 0x04000580 RID: 1408
	public float m_maxDistance = 30f;

	// Token: 0x04000581 RID: 1409
	public float m_staminaDrain = 10f;

	// Token: 0x04000582 RID: 1410
	public float m_staminaDrainInterval = 0.1f;

	// Token: 0x04000583 RID: 1411
	private bool m_broken;

	// Token: 0x04000584 RID: 1412
	private Character m_attacker;

	// Token: 0x04000585 RID: 1413
	private float m_baseDistance = 999999f;

	// Token: 0x04000586 RID: 1414
	private LineConnect m_line;

	// Token: 0x04000587 RID: 1415
	private float m_drainStaminaTimer;
}
