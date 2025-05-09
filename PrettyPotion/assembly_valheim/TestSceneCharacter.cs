using System;
using System.Threading;
using UnityEngine;

// Token: 0x020001CB RID: 459
public class TestSceneCharacter : MonoBehaviour
{
	// Token: 0x06001A89 RID: 6793 RVA: 0x000C57DE File Offset: 0x000C39DE
	private void Start()
	{
		this.m_body = base.GetComponent<Rigidbody>();
	}

	// Token: 0x06001A8A RID: 6794 RVA: 0x000C57EC File Offset: 0x000C39EC
	private void Update()
	{
		Thread.Sleep(30);
		this.HandleInput(Time.deltaTime);
	}

	// Token: 0x06001A8B RID: 6795 RVA: 0x000C5800 File Offset: 0x000C3A00
	private void HandleInput(float dt)
	{
		Camera mainCamera = Utils.GetMainCamera();
		if (mainCamera == null)
		{
			return;
		}
		Vector2 vector = Vector2.zero;
		vector = ZInput.GetMouseDelta();
		if (ZInput.GetKey(KeyCode.Mouse1, true) || Cursor.lockState != CursorLockMode.None)
		{
			this.m_lookYaw *= Quaternion.Euler(0f, vector.x, 0f);
			this.m_lookPitch = Mathf.Clamp(this.m_lookPitch - vector.y, -89f, 89f);
		}
		if (ZInput.GetKeyDown(KeyCode.F1, true))
		{
			if (Cursor.lockState == CursorLockMode.None)
			{
				Cursor.lockState = CursorLockMode.Locked;
			}
			else
			{
				Cursor.lockState = CursorLockMode.None;
			}
		}
		Vector3 a = Vector3.zero;
		if (ZInput.GetKey(KeyCode.A, true))
		{
			a -= base.transform.right * this.m_speed;
		}
		if (ZInput.GetKey(KeyCode.D, true))
		{
			a += base.transform.right * this.m_speed;
		}
		if (ZInput.GetKey(KeyCode.W, true))
		{
			a += base.transform.forward * this.m_speed;
		}
		if (ZInput.GetKey(KeyCode.S, true))
		{
			a -= base.transform.forward * this.m_speed;
		}
		if (ZInput.GetKeyDown(KeyCode.Space, true))
		{
			this.m_body.AddForce(Vector3.up * 10f, ForceMode.VelocityChange);
		}
		Vector3 force = a - this.m_body.velocity;
		force.y = 0f;
		this.m_body.AddForce(force, ForceMode.VelocityChange);
		base.transform.rotation = this.m_lookYaw;
		Quaternion rotation = this.m_lookYaw * Quaternion.Euler(this.m_lookPitch, 0f, 0f);
		mainCamera.transform.position = base.transform.position - rotation * Vector3.forward * this.m_cameraDistance;
		mainCamera.transform.LookAt(base.transform.position + Vector3.up);
	}

	// Token: 0x04001AED RID: 6893
	public float m_speed = 5f;

	// Token: 0x04001AEE RID: 6894
	public float m_cameraDistance = 10f;

	// Token: 0x04001AEF RID: 6895
	private Rigidbody m_body;

	// Token: 0x04001AF0 RID: 6896
	private Quaternion m_lookYaw = Quaternion.identity;

	// Token: 0x04001AF1 RID: 6897
	private float m_lookPitch;
}
