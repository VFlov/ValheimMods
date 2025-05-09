using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

// Token: 0x020001A6 RID: 422
public class PrivateArea : MonoBehaviour, Hoverable, Interactable
{
	// Token: 0x060018C0 RID: 6336 RVA: 0x000B91B4 File Offset: 0x000B73B4
	private void Awake()
	{
		if (this.m_areaMarker)
		{
			this.m_areaMarker.m_radius = this.m_radius;
		}
		this.m_nview = base.GetComponent<ZNetView>();
		if (!this.m_nview.IsValid())
		{
			return;
		}
		WearNTear component = base.GetComponent<WearNTear>();
		component.m_onDamaged = (Action)Delegate.Combine(component.m_onDamaged, new Action(this.OnDamaged));
		this.m_piece = base.GetComponent<Piece>();
		if (this.m_areaMarker)
		{
			this.m_areaMarker.gameObject.SetActive(false);
		}
		if (this.m_inRangeEffect)
		{
			this.m_inRangeEffect.SetActive(false);
		}
		PrivateArea.m_allAreas.Add(this);
		base.InvokeRepeating("UpdateStatus", 0f, 1f);
		this.m_nview.Register<long>("ToggleEnabled", new Action<long, long>(this.RPC_ToggleEnabled));
		this.m_nview.Register<long, string>("TogglePermitted", new Action<long, long, string>(this.RPC_TogglePermitted));
		this.m_nview.Register("FlashShield", new Action<long>(this.RPC_FlashShield));
		if (this.m_enabledByDefault && this.m_nview.IsOwner())
		{
			this.m_nview.GetZDO().Set(ZDOVars.s_enabled, true);
		}
	}

	// Token: 0x060018C1 RID: 6337 RVA: 0x000B9302 File Offset: 0x000B7502
	private void OnDestroy()
	{
		PrivateArea.m_allAreas.Remove(this);
	}

	// Token: 0x060018C2 RID: 6338 RVA: 0x000B9310 File Offset: 0x000B7510
	private void UpdateStatus()
	{
		bool flag = this.IsEnabled();
		this.m_enabledEffect.SetActive(flag);
		this.m_flashAvailable = true;
		foreach (Material material in this.m_model.materials)
		{
			if (flag)
			{
				material.EnableKeyword("_EMISSION");
			}
			else
			{
				material.DisableKeyword("_EMISSION");
			}
		}
	}

	// Token: 0x060018C3 RID: 6339 RVA: 0x000B9370 File Offset: 0x000B7570
	public string GetHoverText()
	{
		if (!this.m_nview.IsValid())
		{
			return "";
		}
		if (Player.m_localPlayer == null)
		{
			return "";
		}
		if (this.m_ownerFaction != Character.Faction.Players)
		{
			return Localization.instance.Localize(this.m_name);
		}
		this.ShowAreaMarker();
		StringBuilder stringBuilder = new StringBuilder(256);
		if (this.m_piece.IsCreator())
		{
			if (this.IsEnabled())
			{
				stringBuilder.Append(this.m_name + " ( $piece_guardstone_active )");
				stringBuilder.Append("\n$piece_guardstone_owner:" + this.GetCreatorName());
				stringBuilder.Append("\n[<color=yellow><b>$KEY_Use</b></color>] $piece_guardstone_deactivate");
			}
			else
			{
				stringBuilder.Append(this.m_name + " ($piece_guardstone_inactive )");
				stringBuilder.Append("\n$piece_guardstone_owner:" + this.GetCreatorName());
				stringBuilder.Append("\n[<color=yellow><b>$KEY_Use</b></color>] $piece_guardstone_activate");
			}
		}
		else if (this.IsEnabled())
		{
			stringBuilder.Append(this.m_name + " ( $piece_guardstone_active )");
			stringBuilder.Append("\n$piece_guardstone_owner:" + this.GetCreatorName());
		}
		else
		{
			stringBuilder.Append(this.m_name + " ( $piece_guardstone_inactive )");
			stringBuilder.Append("\n$piece_guardstone_owner:" + this.GetCreatorName());
			if (this.IsPermitted(Player.m_localPlayer.GetPlayerID()))
			{
				stringBuilder.Append("\n[<color=yellow><b>$KEY_Use</b></color>] $piece_guardstone_remove");
			}
			else
			{
				stringBuilder.Append("\n[<color=yellow><b>$KEY_Use</b></color>] $piece_guardstone_add");
			}
		}
		this.AddUserList(stringBuilder);
		return Localization.instance.Localize(stringBuilder.ToString());
	}

	// Token: 0x060018C4 RID: 6340 RVA: 0x000B950C File Offset: 0x000B770C
	private void AddUserList(StringBuilder text)
	{
		List<KeyValuePair<long, string>> permittedPlayers = this.GetPermittedPlayers();
		text.Append("\n$piece_guardstone_additional: ");
		for (int i = 0; i < permittedPlayers.Count; i++)
		{
			text.Append(CensorShittyWords.FilterUGC(permittedPlayers[i].Value, UGCType.CharacterName, this.m_piece.GetCreator()));
			if (i != permittedPlayers.Count - 1)
			{
				text.Append(", ");
			}
		}
	}

	// Token: 0x060018C5 RID: 6341 RVA: 0x000B957C File Offset: 0x000B777C
	private void RemovePermitted(long playerID)
	{
		List<KeyValuePair<long, string>> permittedPlayers = this.GetPermittedPlayers();
		if (permittedPlayers.RemoveAll((KeyValuePair<long, string> x) => x.Key == playerID) > 0)
		{
			this.SetPermittedPlayers(permittedPlayers);
			this.m_removedPermittedEffect.Create(base.transform.position, base.transform.rotation, null, 1f, -1);
		}
	}

	// Token: 0x060018C6 RID: 6342 RVA: 0x000B95E4 File Offset: 0x000B77E4
	private bool IsPermitted(long playerID)
	{
		foreach (KeyValuePair<long, string> keyValuePair in this.GetPermittedPlayers())
		{
			if (keyValuePair.Key == playerID)
			{
				return true;
			}
		}
		return false;
	}

	// Token: 0x060018C7 RID: 6343 RVA: 0x000B9644 File Offset: 0x000B7844
	private void AddPermitted(long playerID, string playerName)
	{
		List<KeyValuePair<long, string>> permittedPlayers = this.GetPermittedPlayers();
		foreach (KeyValuePair<long, string> keyValuePair in permittedPlayers)
		{
			if (keyValuePair.Key == playerID)
			{
				return;
			}
		}
		permittedPlayers.Add(new KeyValuePair<long, string>(playerID, playerName));
		this.SetPermittedPlayers(permittedPlayers);
		this.m_addPermittedEffect.Create(base.transform.position, base.transform.rotation, null, 1f, -1);
	}

	// Token: 0x060018C8 RID: 6344 RVA: 0x000B96DC File Offset: 0x000B78DC
	private void SetPermittedPlayers(List<KeyValuePair<long, string>> users)
	{
		this.m_nview.GetZDO().Set(ZDOVars.s_permitted, users.Count, false);
		for (int i = 0; i < users.Count; i++)
		{
			KeyValuePair<long, string> keyValuePair = users[i];
			this.m_nview.GetZDO().Set("pu_id" + i.ToString(), keyValuePair.Key);
			this.m_nview.GetZDO().Set("pu_name" + i.ToString(), keyValuePair.Value);
		}
	}

	// Token: 0x060018C9 RID: 6345 RVA: 0x000B9770 File Offset: 0x000B7970
	private List<KeyValuePair<long, string>> GetPermittedPlayers()
	{
		List<KeyValuePair<long, string>> list = new List<KeyValuePair<long, string>>();
		int @int = this.m_nview.GetZDO().GetInt(ZDOVars.s_permitted, 0);
		for (int i = 0; i < @int; i++)
		{
			long @long = this.m_nview.GetZDO().GetLong("pu_id" + i.ToString(), 0L);
			string @string = this.m_nview.GetZDO().GetString("pu_name" + i.ToString(), "");
			if (@long != 0L)
			{
				list.Add(new KeyValuePair<long, string>(@long, @string));
			}
		}
		return list;
	}

	// Token: 0x060018CA RID: 6346 RVA: 0x000B9804 File Offset: 0x000B7A04
	public string GetHoverName()
	{
		return this.m_name;
	}

	// Token: 0x060018CB RID: 6347 RVA: 0x000B980C File Offset: 0x000B7A0C
	public bool Interact(Humanoid human, bool hold, bool alt)
	{
		if (hold)
		{
			return false;
		}
		if (this.m_ownerFaction != Character.Faction.Players)
		{
			return false;
		}
		Player player = human as Player;
		if (this.m_piece.IsCreator())
		{
			this.m_nview.InvokeRPC("ToggleEnabled", new object[]
			{
				player.GetPlayerID()
			});
			return true;
		}
		if (this.IsEnabled())
		{
			return false;
		}
		this.m_nview.InvokeRPC("TogglePermitted", new object[]
		{
			player.GetPlayerID(),
			player.GetPlayerName()
		});
		return true;
	}

	// Token: 0x060018CC RID: 6348 RVA: 0x000B989A File Offset: 0x000B7A9A
	public bool UseItem(Humanoid user, ItemDrop.ItemData item)
	{
		return false;
	}

	// Token: 0x060018CD RID: 6349 RVA: 0x000B989D File Offset: 0x000B7A9D
	private void RPC_TogglePermitted(long uid, long playerID, string name)
	{
		if (!this.m_nview.IsOwner())
		{
			return;
		}
		if (this.IsEnabled())
		{
			return;
		}
		if (this.IsPermitted(playerID))
		{
			this.RemovePermitted(playerID);
			return;
		}
		this.AddPermitted(playerID, name);
	}

	// Token: 0x060018CE RID: 6350 RVA: 0x000B98D0 File Offset: 0x000B7AD0
	private void RPC_ToggleEnabled(long uid, long playerID)
	{
		ZLog.Log("Toggle enabled from " + playerID.ToString() + "  creator is " + this.m_piece.GetCreator().ToString());
		if (!this.m_nview.IsOwner())
		{
			return;
		}
		if (this.m_piece.GetCreator() != playerID)
		{
			return;
		}
		this.SetEnabled(!this.IsEnabled());
	}

	// Token: 0x060018CF RID: 6351 RVA: 0x000B9937 File Offset: 0x000B7B37
	private bool IsEnabled()
	{
		return this.m_nview.IsValid() && this.m_nview.GetZDO().GetBool(ZDOVars.s_enabled, false);
	}

	// Token: 0x060018D0 RID: 6352 RVA: 0x000B9960 File Offset: 0x000B7B60
	private void SetEnabled(bool enabled)
	{
		this.m_nview.GetZDO().Set(ZDOVars.s_enabled, enabled);
		this.UpdateStatus();
		if (enabled)
		{
			this.m_activateEffect.Create(base.transform.position, base.transform.rotation, null, 1f, -1);
			return;
		}
		this.m_deactivateEffect.Create(base.transform.position, base.transform.rotation, null, 1f, -1);
	}

	// Token: 0x060018D1 RID: 6353 RVA: 0x000B99DF File Offset: 0x000B7BDF
	public void Setup(string name)
	{
		this.m_nview.GetZDO().Set(ZDOVars.s_creatorName, name);
	}

	// Token: 0x060018D2 RID: 6354 RVA: 0x000B99F8 File Offset: 0x000B7BF8
	public void PokeAllAreasInRange()
	{
		foreach (PrivateArea privateArea in PrivateArea.m_allAreas)
		{
			if (!(privateArea == this) && this.IsInside(privateArea.transform.position, 0f))
			{
				privateArea.StartInRangeEffect();
			}
		}
	}

	// Token: 0x060018D3 RID: 6355 RVA: 0x000B9A6C File Offset: 0x000B7C6C
	private void StartInRangeEffect()
	{
		this.m_inRangeEffect.SetActive(true);
		base.CancelInvoke("StopInRangeEffect");
		base.Invoke("StopInRangeEffect", 0.2f);
	}

	// Token: 0x060018D4 RID: 6356 RVA: 0x000B9A95 File Offset: 0x000B7C95
	private void StopInRangeEffect()
	{
		this.m_inRangeEffect.SetActive(false);
	}

	// Token: 0x060018D5 RID: 6357 RVA: 0x000B9AA4 File Offset: 0x000B7CA4
	public void PokeConnectionEffects()
	{
		List<PrivateArea> connectedAreas = this.GetConnectedAreas(false);
		this.StartConnectionEffects();
		foreach (PrivateArea privateArea in connectedAreas)
		{
			privateArea.StartConnectionEffects();
		}
	}

	// Token: 0x060018D6 RID: 6358 RVA: 0x000B9AFC File Offset: 0x000B7CFC
	private void StartConnectionEffects()
	{
		List<PrivateArea> list = new List<PrivateArea>();
		foreach (PrivateArea privateArea in PrivateArea.m_allAreas)
		{
			if (!(privateArea == this) && this.IsInside(privateArea.transform.position, 0f))
			{
				list.Add(privateArea);
			}
		}
		Vector3 vector = base.transform.position + Vector3.up * 1.4f;
		if (this.m_connectionInstances.Count != list.Count)
		{
			this.StopConnectionEffects();
			for (int i = 0; i < list.Count; i++)
			{
				GameObject item = UnityEngine.Object.Instantiate<GameObject>(this.m_connectEffect, vector, Quaternion.identity, base.transform);
				this.m_connectionInstances.Add(item);
			}
		}
		if (this.m_connectionInstances.Count == 0)
		{
			return;
		}
		for (int j = 0; j < list.Count; j++)
		{
			Vector3 vector2 = list[j].transform.position + Vector3.up * 1.4f - vector;
			Quaternion rotation = Quaternion.LookRotation(vector2.normalized);
			GameObject gameObject = this.m_connectionInstances[j];
			gameObject.transform.position = vector;
			gameObject.transform.rotation = rotation;
			gameObject.transform.localScale = new Vector3(1f, 1f, vector2.magnitude);
		}
		base.CancelInvoke("StopConnectionEffects");
		base.Invoke("StopConnectionEffects", 0.3f);
	}

	// Token: 0x060018D7 RID: 6359 RVA: 0x000B9CB0 File Offset: 0x000B7EB0
	private void StopConnectionEffects()
	{
		foreach (GameObject obj in this.m_connectionInstances)
		{
			UnityEngine.Object.Destroy(obj);
		}
		this.m_connectionInstances.Clear();
	}

	// Token: 0x060018D8 RID: 6360 RVA: 0x000B9D0C File Offset: 0x000B7F0C
	private string GetCreatorName()
	{
		return CensorShittyWords.FilterUGC(this.m_nview.GetZDO().GetString(ZDOVars.s_creatorName, ""), UGCType.CharacterName, this.m_piece.GetCreator());
	}

	// Token: 0x060018D9 RID: 6361 RVA: 0x000B9D3C File Offset: 0x000B7F3C
	public static bool OnObjectDamaged(Vector3 point, Character attacker, bool destroyed)
	{
		foreach (PrivateArea privateArea in PrivateArea.m_allAreas)
		{
			if (privateArea.IsEnabled() && privateArea.IsInside(point, 0f))
			{
				privateArea.OnObjectDamaged(attacker, destroyed);
				return true;
			}
		}
		return false;
	}

	// Token: 0x060018DA RID: 6362 RVA: 0x000B9DAC File Offset: 0x000B7FAC
	public static bool CheckAccess(Vector3 point, float radius = 0f, bool flash = true, bool wardCheck = false)
	{
		List<PrivateArea> list = new List<PrivateArea>();
		bool flag = true;
		if (wardCheck)
		{
			flag = true;
			using (List<PrivateArea>.Enumerator enumerator = PrivateArea.m_allAreas.GetEnumerator())
			{
				while (enumerator.MoveNext())
				{
					PrivateArea privateArea = enumerator.Current;
					if (privateArea.IsEnabled() && privateArea.IsInside(point, radius) && !privateArea.HaveLocalAccess())
					{
						flag = false;
						list.Add(privateArea);
					}
				}
				goto IL_B8;
			}
		}
		flag = false;
		foreach (PrivateArea privateArea2 in PrivateArea.m_allAreas)
		{
			if (privateArea2.IsEnabled() && privateArea2.IsInside(point, radius))
			{
				if (privateArea2.HaveLocalAccess())
				{
					flag = true;
				}
				else
				{
					list.Add(privateArea2);
				}
			}
		}
		IL_B8:
		if (!flag && list.Count > 0)
		{
			if (flash)
			{
				foreach (PrivateArea privateArea3 in list)
				{
					privateArea3.FlashShield(false);
				}
			}
			return false;
		}
		return true;
	}

	// Token: 0x060018DB RID: 6363 RVA: 0x000B9EDC File Offset: 0x000B80DC
	private bool HaveLocalAccess()
	{
		return this.m_piece.IsCreator() || this.IsPermitted(Player.m_localPlayer.GetPlayerID());
	}

	// Token: 0x060018DC RID: 6364 RVA: 0x000B9F02 File Offset: 0x000B8102
	private List<PrivateArea> GetConnectedAreas(bool forceUpdate = false)
	{
		if (Time.time - this.m_connectionUpdateTime > this.m_updateConnectionsInterval || forceUpdate)
		{
			this.GetAllConnectedAreas(this.m_connectedAreas);
			this.m_connectionUpdateTime = Time.time;
		}
		return this.m_connectedAreas;
	}

	// Token: 0x060018DD RID: 6365 RVA: 0x000B9F3C File Offset: 0x000B813C
	private void GetAllConnectedAreas(List<PrivateArea> areas)
	{
		Queue<PrivateArea> queue = new Queue<PrivateArea>();
		queue.Enqueue(this);
		foreach (PrivateArea privateArea in PrivateArea.m_allAreas)
		{
			privateArea.m_tempChecked = false;
		}
		this.m_tempChecked = true;
		while (queue.Count > 0)
		{
			PrivateArea privateArea2 = queue.Dequeue();
			foreach (PrivateArea privateArea3 in PrivateArea.m_allAreas)
			{
				if (!privateArea3.m_tempChecked && privateArea3.IsEnabled() && privateArea3.IsInside(privateArea2.transform.position, 0f))
				{
					privateArea3.m_tempChecked = true;
					queue.Enqueue(privateArea3);
					areas.Add(privateArea3);
				}
			}
		}
	}

	// Token: 0x060018DE RID: 6366 RVA: 0x000BA02C File Offset: 0x000B822C
	private void OnObjectDamaged(Character attacker, bool destroyed)
	{
		this.FlashShield(false);
		if (this.m_ownerFaction != Character.Faction.Players)
		{
			List<Character> list = new List<Character>();
			Character.GetCharactersInRange(base.transform.position, this.m_radius * 2f, list);
			foreach (Character character in list)
			{
				if (character.GetFaction() == this.m_ownerFaction)
				{
					MonsterAI component = character.GetComponent<MonsterAI>();
					if (component)
					{
						component.OnPrivateAreaAttacked(attacker, destroyed);
					}
					NpcTalk component2 = character.GetComponent<NpcTalk>();
					if (component2)
					{
						component2.OnPrivateAreaAttacked(attacker);
					}
				}
			}
		}
	}

	// Token: 0x060018DF RID: 6367 RVA: 0x000BA0E8 File Offset: 0x000B82E8
	private void FlashShield(bool flashConnected)
	{
		if (!this.m_flashAvailable)
		{
			return;
		}
		this.m_flashAvailable = false;
		this.m_nview.InvokeRPC(ZNetView.Everybody, "FlashShield", Array.Empty<object>());
		if (flashConnected)
		{
			foreach (PrivateArea privateArea in this.GetConnectedAreas(false))
			{
				if (privateArea.m_nview.IsValid())
				{
					privateArea.m_nview.InvokeRPC(ZNetView.Everybody, "FlashShield", Array.Empty<object>());
				}
			}
		}
	}

	// Token: 0x060018E0 RID: 6368 RVA: 0x000BA18C File Offset: 0x000B838C
	private void RPC_FlashShield(long uid)
	{
		this.m_flashEffect.Create(base.transform.position, Quaternion.identity, null, 1f, -1);
	}

	// Token: 0x060018E1 RID: 6369 RVA: 0x000BA1B4 File Offset: 0x000B83B4
	public static bool InsideFactionArea(Vector3 point, Character.Faction faction)
	{
		foreach (PrivateArea privateArea in PrivateArea.m_allAreas)
		{
			if (privateArea.m_ownerFaction == faction && privateArea.IsInside(point, 0f))
			{
				return true;
			}
		}
		return false;
	}

	// Token: 0x060018E2 RID: 6370 RVA: 0x000BA220 File Offset: 0x000B8420
	private bool IsInside(Vector3 point, float radius)
	{
		return Utils.DistanceXZ(base.transform.position, point) < this.m_radius + radius;
	}

	// Token: 0x060018E3 RID: 6371 RVA: 0x000BA23D File Offset: 0x000B843D
	public void ShowAreaMarker()
	{
		if (this.m_areaMarker)
		{
			this.m_areaMarker.gameObject.SetActive(true);
			base.CancelInvoke("HideMarker");
			base.Invoke("HideMarker", 0.5f);
		}
	}

	// Token: 0x060018E4 RID: 6372 RVA: 0x000BA278 File Offset: 0x000B8478
	private void HideMarker()
	{
		this.m_areaMarker.gameObject.SetActive(false);
	}

	// Token: 0x060018E5 RID: 6373 RVA: 0x000BA28B File Offset: 0x000B848B
	private void OnDamaged()
	{
		if (this.IsEnabled())
		{
			this.FlashShield(false);
		}
	}

	// Token: 0x060018E6 RID: 6374 RVA: 0x000BA29C File Offset: 0x000B849C
	private void OnDrawGizmosSelected()
	{
	}

	// Token: 0x04001901 RID: 6401
	public string m_name = "Guard stone";

	// Token: 0x04001902 RID: 6402
	public float m_radius = 10f;

	// Token: 0x04001903 RID: 6403
	public float m_updateConnectionsInterval = 5f;

	// Token: 0x04001904 RID: 6404
	public bool m_enabledByDefault;

	// Token: 0x04001905 RID: 6405
	public Character.Faction m_ownerFaction;

	// Token: 0x04001906 RID: 6406
	public GameObject m_enabledEffect;

	// Token: 0x04001907 RID: 6407
	public CircleProjector m_areaMarker;

	// Token: 0x04001908 RID: 6408
	public EffectList m_flashEffect = new EffectList();

	// Token: 0x04001909 RID: 6409
	public EffectList m_activateEffect = new EffectList();

	// Token: 0x0400190A RID: 6410
	public EffectList m_deactivateEffect = new EffectList();

	// Token: 0x0400190B RID: 6411
	public EffectList m_addPermittedEffect = new EffectList();

	// Token: 0x0400190C RID: 6412
	public EffectList m_removedPermittedEffect = new EffectList();

	// Token: 0x0400190D RID: 6413
	public GameObject m_connectEffect;

	// Token: 0x0400190E RID: 6414
	public GameObject m_inRangeEffect;

	// Token: 0x0400190F RID: 6415
	public MeshRenderer m_model;

	// Token: 0x04001910 RID: 6416
	private ZNetView m_nview;

	// Token: 0x04001911 RID: 6417
	private Piece m_piece;

	// Token: 0x04001912 RID: 6418
	private bool m_flashAvailable = true;

	// Token: 0x04001913 RID: 6419
	private bool m_tempChecked;

	// Token: 0x04001914 RID: 6420
	private List<GameObject> m_connectionInstances = new List<GameObject>();

	// Token: 0x04001915 RID: 6421
	private float m_connectionUpdateTime = -1000f;

	// Token: 0x04001916 RID: 6422
	private List<PrivateArea> m_connectedAreas = new List<PrivateArea>();

	// Token: 0x04001917 RID: 6423
	private static List<PrivateArea> m_allAreas = new List<PrivateArea>();
}
