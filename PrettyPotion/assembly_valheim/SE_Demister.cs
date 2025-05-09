using System;
using UnityEngine;

// Token: 0x02000034 RID: 52
public class SE_Demister : StatusEffect
{
	// Token: 0x060004DE RID: 1246 RVA: 0x0002B040 File Offset: 0x00029240
	public override void Setup(Character character)
	{
		base.Setup(character);
		if (this.m_coverRayMask == 0)
		{
			this.m_coverRayMask = LayerMask.GetMask(new string[]
			{
				"Default",
				"static_solid",
				"Default_small",
				"piece",
				"terrain"
			});
		}
	}

	// Token: 0x060004DF RID: 1247 RVA: 0x0002B098 File Offset: 0x00029298
	private bool IsUnderRoof()
	{
		RaycastHit raycastHit;
		return Physics.Raycast(this.m_character.GetCenterPoint(), Vector3.up, out raycastHit, 4f, this.m_coverRayMask);
	}

	// Token: 0x060004E0 RID: 1248 RVA: 0x0002B0CC File Offset: 0x000292CC
	public override void UpdateStatusEffect(float dt)
	{
		base.UpdateStatusEffect(dt);
		if (!this.m_ballInstance)
		{
			Vector3 position = this.m_character.GetCenterPoint() + this.m_character.transform.forward * 0.5f;
			this.m_ballInstance = UnityEngine.Object.Instantiate<GameObject>(this.m_ballPrefab, position, Quaternion.identity);
			return;
		}
		Character character = this.m_character;
		bool flag = this.IsUnderRoof();
		Vector3 position2 = this.m_character.transform.position;
		Vector3 vector = this.m_ballInstance.transform.position;
		Vector3 vector2 = flag ? this.m_offsetInterior : this.m_offset;
		float d = flag ? this.m_noiseDistanceInterior : this.m_noiseDistance;
		Vector3 vector3 = position2 + this.m_character.transform.TransformVector(vector2);
		float num = Time.time * this.m_noiseSpeed;
		vector3 += new Vector3(Mathf.Sin(num * 4f), Mathf.Sin(num * 2f) * this.m_noiseDistanceYScale, Mathf.Cos(num * 5f)) * d;
		float num2 = Vector3.Distance(vector3, vector);
		if (num2 > this.m_maxDistance * 2f)
		{
			vector = vector3;
		}
		else if (num2 > this.m_maxDistance)
		{
			Vector3 normalized = (vector - vector3).normalized;
			vector = vector3 + normalized * this.m_maxDistance;
		}
		Vector3 normalized2 = (vector3 - vector).normalized;
		this.m_ballVel += normalized2 * this.m_ballAcceleration * dt;
		if (this.m_ballVel.magnitude > this.m_ballMaxSpeed)
		{
			this.m_ballVel = this.m_ballVel.normalized * this.m_ballMaxSpeed;
		}
		if (!flag)
		{
			Vector3 velocity = this.m_character.GetVelocity();
			this.m_ballVel += velocity * this.m_characterVelocityFactor * dt;
		}
		this.m_ballVel -= this.m_ballVel * this.m_ballFriction;
		Vector3 position3 = vector + this.m_ballVel * dt;
		this.m_ballInstance.transform.position = position3;
		Quaternion quaternion = this.m_ballInstance.transform.rotation;
		quaternion *= Quaternion.Euler(this.m_rotationSpeed, 0f, this.m_rotationSpeed * 0.5321f);
		this.m_ballInstance.transform.rotation = quaternion;
	}

	// Token: 0x060004E1 RID: 1249 RVA: 0x0002B36C File Offset: 0x0002956C
	private void RemoveEffects()
	{
		if (this.m_ballInstance != null)
		{
			ZNetView component = this.m_ballInstance.GetComponent<ZNetView>();
			if (component.IsValid())
			{
				component.ClaimOwnership();
				component.Destroy();
			}
		}
	}

	// Token: 0x060004E2 RID: 1250 RVA: 0x0002B3A7 File Offset: 0x000295A7
	protected override void OnApplicationQuit()
	{
		base.OnApplicationQuit();
		this.m_ballInstance = null;
	}

	// Token: 0x060004E3 RID: 1251 RVA: 0x0002B3B6 File Offset: 0x000295B6
	public override void Stop()
	{
		base.Stop();
		this.RemoveEffects();
	}

	// Token: 0x060004E4 RID: 1252 RVA: 0x0002B3C4 File Offset: 0x000295C4
	public override void OnDestroy()
	{
		base.OnDestroy();
		this.RemoveEffects();
	}

	// Token: 0x0400055C RID: 1372
	[Header("SE_Demister")]
	public GameObject m_ballPrefab;

	// Token: 0x0400055D RID: 1373
	public Vector3 m_offset = new Vector3(0f, 2f, 0f);

	// Token: 0x0400055E RID: 1374
	public Vector3 m_offsetInterior = new Vector3(0.5f, 1.8f, 0f);

	// Token: 0x0400055F RID: 1375
	public float m_maxDistance = 50f;

	// Token: 0x04000560 RID: 1376
	public float m_ballAcceleration = 4f;

	// Token: 0x04000561 RID: 1377
	public float m_ballMaxSpeed = 10f;

	// Token: 0x04000562 RID: 1378
	public float m_ballFriction = 0.1f;

	// Token: 0x04000563 RID: 1379
	public float m_noiseDistance = 1f;

	// Token: 0x04000564 RID: 1380
	public float m_noiseDistanceInterior = 0.2f;

	// Token: 0x04000565 RID: 1381
	public float m_noiseDistanceYScale = 1f;

	// Token: 0x04000566 RID: 1382
	public float m_noiseSpeed = 1f;

	// Token: 0x04000567 RID: 1383
	public float m_characterVelocityFactor = 1f;

	// Token: 0x04000568 RID: 1384
	public float m_rotationSpeed = 1f;

	// Token: 0x04000569 RID: 1385
	private int m_coverRayMask;

	// Token: 0x0400056A RID: 1386
	private GameObject m_ballInstance;

	// Token: 0x0400056B RID: 1387
	private Vector3 m_ballVel = new Vector3(0f, 0f, 0f);
}
