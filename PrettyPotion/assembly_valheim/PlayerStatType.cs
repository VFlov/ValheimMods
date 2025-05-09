using System;

// Token: 0x0200012E RID: 302
public enum PlayerStatType
{
	// Token: 0x040012CE RID: 4814
	Deaths,
	// Token: 0x040012CF RID: 4815
	CraftsOrUpgrades,
	// Token: 0x040012D0 RID: 4816
	Builds,
	// Token: 0x040012D1 RID: 4817
	Jumps,
	// Token: 0x040012D2 RID: 4818
	Cheats,
	// Token: 0x040012D3 RID: 4819
	EnemyHits,
	// Token: 0x040012D4 RID: 4820
	EnemyKills,
	// Token: 0x040012D5 RID: 4821
	EnemyKillsLastHits,
	// Token: 0x040012D6 RID: 4822
	PlayerHits,
	// Token: 0x040012D7 RID: 4823
	PlayerKills,
	// Token: 0x040012D8 RID: 4824
	HitsTakenEnemies,
	// Token: 0x040012D9 RID: 4825
	HitsTakenPlayers,
	// Token: 0x040012DA RID: 4826
	ItemsPickedUp,
	// Token: 0x040012DB RID: 4827
	Crafts,
	// Token: 0x040012DC RID: 4828
	Upgrades,
	// Token: 0x040012DD RID: 4829
	PortalsUsed,
	// Token: 0x040012DE RID: 4830
	DistanceTraveled,
	// Token: 0x040012DF RID: 4831
	DistanceWalk,
	// Token: 0x040012E0 RID: 4832
	DistanceRun,
	// Token: 0x040012E1 RID: 4833
	DistanceSail,
	// Token: 0x040012E2 RID: 4834
	DistanceAir,
	// Token: 0x040012E3 RID: 4835
	TimeInBase,
	// Token: 0x040012E4 RID: 4836
	TimeOutOfBase,
	// Token: 0x040012E5 RID: 4837
	Sleep,
	// Token: 0x040012E6 RID: 4838
	ItemStandUses,
	// Token: 0x040012E7 RID: 4839
	ArmorStandUses,
	// Token: 0x040012E8 RID: 4840
	WorldLoads,
	// Token: 0x040012E9 RID: 4841
	TreeChops,
	// Token: 0x040012EA RID: 4842
	Tree,
	// Token: 0x040012EB RID: 4843
	TreeTier0,
	// Token: 0x040012EC RID: 4844
	TreeTier1,
	// Token: 0x040012ED RID: 4845
	TreeTier2,
	// Token: 0x040012EE RID: 4846
	TreeTier3,
	// Token: 0x040012EF RID: 4847
	TreeTier4,
	// Token: 0x040012F0 RID: 4848
	TreeTier5,
	// Token: 0x040012F1 RID: 4849
	LogChops,
	// Token: 0x040012F2 RID: 4850
	Logs,
	// Token: 0x040012F3 RID: 4851
	MineHits,
	// Token: 0x040012F4 RID: 4852
	Mines,
	// Token: 0x040012F5 RID: 4853
	MineTier0,
	// Token: 0x040012F6 RID: 4854
	MineTier1,
	// Token: 0x040012F7 RID: 4855
	MineTier2,
	// Token: 0x040012F8 RID: 4856
	MineTier3,
	// Token: 0x040012F9 RID: 4857
	MineTier4,
	// Token: 0x040012FA RID: 4858
	MineTier5,
	// Token: 0x040012FB RID: 4859
	RavenHits,
	// Token: 0x040012FC RID: 4860
	RavenTalk,
	// Token: 0x040012FD RID: 4861
	RavenAppear,
	// Token: 0x040012FE RID: 4862
	CreatureTamed,
	// Token: 0x040012FF RID: 4863
	FoodEaten,
	// Token: 0x04001300 RID: 4864
	SkeletonSummons,
	// Token: 0x04001301 RID: 4865
	ArrowsShot,
	// Token: 0x04001302 RID: 4866
	TombstonesOpenedOwn,
	// Token: 0x04001303 RID: 4867
	TombstonesOpenedOther,
	// Token: 0x04001304 RID: 4868
	TombstonesFit,
	// Token: 0x04001305 RID: 4869
	DeathByUndefined,
	// Token: 0x04001306 RID: 4870
	DeathByEnemyHit,
	// Token: 0x04001307 RID: 4871
	DeathByPlayerHit,
	// Token: 0x04001308 RID: 4872
	DeathByFall,
	// Token: 0x04001309 RID: 4873
	DeathByDrowning,
	// Token: 0x0400130A RID: 4874
	DeathByBurning,
	// Token: 0x0400130B RID: 4875
	DeathByFreezing,
	// Token: 0x0400130C RID: 4876
	DeathByPoisoned,
	// Token: 0x0400130D RID: 4877
	DeathBySmoke,
	// Token: 0x0400130E RID: 4878
	DeathByWater,
	// Token: 0x0400130F RID: 4879
	DeathByEdgeOfWorld,
	// Token: 0x04001310 RID: 4880
	DeathByImpact,
	// Token: 0x04001311 RID: 4881
	DeathByCart,
	// Token: 0x04001312 RID: 4882
	DeathByTree,
	// Token: 0x04001313 RID: 4883
	DeathBySelf,
	// Token: 0x04001314 RID: 4884
	DeathByStructural,
	// Token: 0x04001315 RID: 4885
	DeathByTurret,
	// Token: 0x04001316 RID: 4886
	DeathByBoat,
	// Token: 0x04001317 RID: 4887
	DeathByStalagtite,
	// Token: 0x04001318 RID: 4888
	DoorsOpened,
	// Token: 0x04001319 RID: 4889
	DoorsClosed,
	// Token: 0x0400131A RID: 4890
	BeesHarvested,
	// Token: 0x0400131B RID: 4891
	SapHarvested,
	// Token: 0x0400131C RID: 4892
	TurretAmmoAdded,
	// Token: 0x0400131D RID: 4893
	TurretTrophySet,
	// Token: 0x0400131E RID: 4894
	TrapArmed,
	// Token: 0x0400131F RID: 4895
	TrapTriggered,
	// Token: 0x04001320 RID: 4896
	PlaceStacks,
	// Token: 0x04001321 RID: 4897
	PortalDungeonIn,
	// Token: 0x04001322 RID: 4898
	PortalDungeonOut,
	// Token: 0x04001323 RID: 4899
	BossKills,
	// Token: 0x04001324 RID: 4900
	BossLastHits,
	// Token: 0x04001325 RID: 4901
	SetGuardianPower,
	// Token: 0x04001326 RID: 4902
	SetPowerEikthyr,
	// Token: 0x04001327 RID: 4903
	SetPowerElder,
	// Token: 0x04001328 RID: 4904
	SetPowerBonemass,
	// Token: 0x04001329 RID: 4905
	SetPowerModer,
	// Token: 0x0400132A RID: 4906
	SetPowerYagluth,
	// Token: 0x0400132B RID: 4907
	SetPowerQueen,
	// Token: 0x0400132C RID: 4908
	SetPowerAshlands,
	// Token: 0x0400132D RID: 4909
	SetPowerDeepNorth,
	// Token: 0x0400132E RID: 4910
	UseGuardianPower,
	// Token: 0x0400132F RID: 4911
	UsePowerEikthyr,
	// Token: 0x04001330 RID: 4912
	UsePowerElder,
	// Token: 0x04001331 RID: 4913
	UsePowerBonemass,
	// Token: 0x04001332 RID: 4914
	UsePowerModer,
	// Token: 0x04001333 RID: 4915
	UsePowerYagluth,
	// Token: 0x04001334 RID: 4916
	UsePowerQueen,
	// Token: 0x04001335 RID: 4917
	UsePowerAshlands,
	// Token: 0x04001336 RID: 4918
	UsePowerDeepNorth,
	// Token: 0x04001337 RID: 4919
	Count
}
