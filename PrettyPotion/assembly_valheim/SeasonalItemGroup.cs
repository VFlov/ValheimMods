using System;
using System.Collections.Generic;
using UnityEngine;

// Token: 0x020000B6 RID: 182
[CreateAssetMenu(menuName = "Valheim/SeasonalItemGroup")]
public class SeasonalItemGroup : ScriptableObject
{
	// Token: 0x06000B76 RID: 2934 RVA: 0x00060D30 File Offset: 0x0005EF30
	public bool IsInSeason()
	{
		return DateTime.Now.Date >= this.GetStartDate().Date && DateTime.Now.Date <= this.GetEndDate().Date;
	}

	// Token: 0x06000B77 RID: 2935 RVA: 0x00060D84 File Offset: 0x0005EF84
	public DateTime GetStartDate()
	{
		this._startDate = this.ConstrainDates(this._startDate);
		this._endDate = this.ConstrainDates(this._endDate);
		return new DateTime((this._startDate.y > this._endDate.y && (float)DateTime.Now.Month <= this._endDate.y) ? (DateTime.Now.Year - 1) : DateTime.Now.Year, Mathf.RoundToInt(this._startDate.y), Mathf.RoundToInt(this._startDate.x));
	}

	// Token: 0x06000B78 RID: 2936 RVA: 0x00060E2C File Offset: 0x0005F02C
	public DateTime GetEndDate()
	{
		this._startDate = this.ConstrainDates(this._startDate);
		this._endDate = this.ConstrainDates(this._endDate);
		return new DateTime((this._startDate.y > this._endDate.y && (float)DateTime.Now.Month > this._endDate.y) ? (DateTime.Now.Year + 1) : DateTime.Now.Year, Mathf.RoundToInt(this._endDate.y), Mathf.RoundToInt(this._endDate.x));
	}

	// Token: 0x06000B79 RID: 2937 RVA: 0x00060ED4 File Offset: 0x0005F0D4
	private Vector2 ConstrainDates(Vector2 inVector)
	{
		inVector.y = Mathf.Clamp(inVector.y, 1f, 12f);
		if (inVector.x < 1f)
		{
			inVector.x = 1f;
		}
		else if (inVector.y == 2f && inVector.x > 28f)
		{
			inVector.x = (float)(DateTime.IsLeapYear(DateTime.Now.Year) ? 29 : 28);
		}
		else if (inVector.x > 30f)
		{
			inVector.x = (float)((inVector.y % 2f == 0f) ? 30 : 31);
		}
		return inVector;
	}

	// Token: 0x04000CBE RID: 3262
	[Header("Dates (Day, Month)")]
	[SerializeField]
	[global::Tooltip("(Day, Month), date at which the event starts. Month will be limited to 1 - 12 and days to 1 - end of month.")]
	private Vector2 _startDate;

	// Token: 0x04000CBF RID: 3263
	[SerializeField]
	[global::Tooltip("(Day, Month), date at which the event ends. Month will be limited to 1 - 12 and days to 1 - end of month.")]
	private Vector2 _endDate;

	// Token: 0x04000CC0 RID: 3264
	[Space(10f)]
	public List<GameObject> Pieces = new List<GameObject>();

	// Token: 0x04000CC1 RID: 3265
	public List<Recipe> Recipes = new List<Recipe>();
}
