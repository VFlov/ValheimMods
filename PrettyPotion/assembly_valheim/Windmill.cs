using System;
using UnityEngine;

// Token: 0x020001DF RID: 479
public class Windmill : MonoBehaviour
{
	// Token: 0x06001B8D RID: 7053 RVA: 0x000CE7D9 File Offset: 0x000CC9D9
	private void Start()
	{
		this.m_smelter = base.GetComponent<Smelter>();
		base.InvokeRepeating("CheckCover", 0.1f, 5f);
	}

	// Token: 0x06001B8E RID: 7054 RVA: 0x000CE7FC File Offset: 0x000CC9FC
	private void Update()
	{
		Quaternion to = Quaternion.LookRotation(-EnvMan.instance.GetWindDir());
		float powerOutput = this.GetPowerOutput();
		this.m_bom.rotation = Quaternion.RotateTowards(this.m_bom.rotation, to, this.m_bomRotationSpeed * powerOutput * Time.deltaTime);
		float num = powerOutput * this.m_propellerRotationSpeed;
		this.m_propAngle += num * Time.deltaTime;
		this.m_propeller.localRotation = Quaternion.Euler(0f, 0f, this.m_propAngle);
		if (this.m_smelter == null || this.m_smelter.IsActive())
		{
			this.m_grindStoneAngle += powerOutput * this.m_grindstoneRotationSpeed * Time.deltaTime;
		}
		this.m_grindstone.localRotation = Quaternion.Euler(0f, this.m_grindStoneAngle, 0f);
		this.m_propellerAOE.SetActive(Mathf.Abs(num) > this.m_minAOEPropellerSpeed);
		this.UpdateAudio(Time.deltaTime);
	}

	// Token: 0x06001B8F RID: 7055 RVA: 0x000CE908 File Offset: 0x000CCB08
	public float GetPowerOutput()
	{
		float num = Utils.LerpStep(this.m_minWindSpeed, 1f, EnvMan.instance.GetWindIntensity());
		return (1f - this.m_cover) * num;
	}

	// Token: 0x06001B90 RID: 7056 RVA: 0x000CE940 File Offset: 0x000CCB40
	private void CheckCover()
	{
		bool flag;
		Cover.GetCoverForPoint(this.m_propeller.transform.position, out this.m_cover, out flag, 0.5f);
	}

	// Token: 0x06001B91 RID: 7057 RVA: 0x000CE970 File Offset: 0x000CCB70
	private void UpdateAudio(float dt)
	{
		float powerOutput = this.GetPowerOutput();
		float target = Mathf.Lerp(this.m_minPitch, this.m_maxPitch, Mathf.Clamp01(powerOutput / this.m_maxPitchVel));
		float target2 = this.m_maxVol * Mathf.Clamp01(powerOutput / this.m_maxVolVel);
		foreach (AudioSource audioSource in this.m_sfxLoops)
		{
			audioSource.volume = Mathf.MoveTowards(audioSource.volume, target2, this.m_audioChangeSpeed * dt);
			audioSource.pitch = Mathf.MoveTowards(audioSource.pitch, target, this.m_audioChangeSpeed * dt);
		}
	}

	// Token: 0x04001C73 RID: 7283
	public Transform m_propeller;

	// Token: 0x04001C74 RID: 7284
	public Transform m_grindstone;

	// Token: 0x04001C75 RID: 7285
	public Transform m_bom;

	// Token: 0x04001C76 RID: 7286
	public AudioSource[] m_sfxLoops;

	// Token: 0x04001C77 RID: 7287
	public GameObject m_propellerAOE;

	// Token: 0x04001C78 RID: 7288
	public float m_minAOEPropellerSpeed = 5f;

	// Token: 0x04001C79 RID: 7289
	public float m_bomRotationSpeed = 10f;

	// Token: 0x04001C7A RID: 7290
	public float m_propellerRotationSpeed = 10f;

	// Token: 0x04001C7B RID: 7291
	public float m_grindstoneRotationSpeed = 10f;

	// Token: 0x04001C7C RID: 7292
	public float m_minWindSpeed = 0.1f;

	// Token: 0x04001C7D RID: 7293
	public float m_minPitch = 1f;

	// Token: 0x04001C7E RID: 7294
	public float m_maxPitch = 1.5f;

	// Token: 0x04001C7F RID: 7295
	public float m_maxPitchVel = 10f;

	// Token: 0x04001C80 RID: 7296
	public float m_maxVol = 1f;

	// Token: 0x04001C81 RID: 7297
	public float m_maxVolVel = 10f;

	// Token: 0x04001C82 RID: 7298
	public float m_audioChangeSpeed = 2f;

	// Token: 0x04001C83 RID: 7299
	private float m_cover;

	// Token: 0x04001C84 RID: 7300
	private float m_propAngle;

	// Token: 0x04001C85 RID: 7301
	private float m_grindStoneAngle;

	// Token: 0x04001C86 RID: 7302
	private Smelter m_smelter;
}
