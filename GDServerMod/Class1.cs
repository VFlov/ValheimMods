using System;
using System.Collections.Generic;
using System.Reflection;
using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using Jotunn.Configs;
using Jotunn.Entities;
using Jotunn.Managers;
using Jotunn.Utils;
using UnityEngine;
using static CharacterDrop;
using static Chat;

namespace GDMod
{
    // Token: 0x02000002 RID: 2
    [BepInPlugin("GrayDwarfMod", "GDMod", "2.0.0")]
    //[BepInDependency("com.jotunn.jotunn", 1)]
    public class Main : BaseUnityPlugin
    {
        // Token: 0x06000001 RID: 1 RVA: 0x00002050 File Offset: 0x00000250
        public void Awake()
        {
            PrefabManager.OnVanillaPrefabsAvailable += GDItems;
            PrefabManager.OnVanillaPrefabsAvailable += CustomItems;
            PrefabManager.OnVanillaPrefabsAvailable += WarfareItems;
            PrefabManager.OnVanillaPrefabsAvailable += CustomMobs;
            PrefabManager.OnVanillaPrefabsAvailable += Economy;
            //PrefabManager.OnVanillaPrefabsAvailable += this.CustomOre;
            PrefabManager.OnVanillaPrefabsAvailable += CustomArmor;
            this.ConfigShip();
            Harmony harmony = Main.harmony;
            if (harmony != null)
            {
                harmony.PatchAll();
            }
        }

        // Token: 0x06000002 RID: 2 RVA: 0x000020E2 File Offset: 0x000002E2
        public void OnDestroy()
        {
            Harmony harmony = Main.harmony;
            if (harmony != null)
            {
                harmony.UnpatchSelf();
            }
        }

        // Token: 0x06000003 RID: 3 RVA: 0x000020F8 File Offset: 0x000002F8
        public void GDItems()
        {
            GameObject gameObject = (GameObject)this.items.LoadAsset("questitem_whetstone");
            GameObject gameObject2 = (GameObject)this.items.LoadAsset("questitem_wind_essence");
            GameObject gameObject3 = (GameObject)this.items.LoadAsset("questitem_wraiths_breath");
            GameObject gameObject4 = (GameObject)this.items.LoadAsset("questitem_dragoneyes");
            GameObject gameObject5 = (GameObject)this.items.LoadAsset("questitem_scroll_red");
            GameObject gameObject6 = (GameObject)this.items.LoadAsset("questitem_scroll_green");
            GameObject gameObject7 = (GameObject)this.items.LoadAsset("questitem_scroll_yellow");
            GameObject gameObject8 = (GameObject)this.items.LoadAsset("questitem_scroll_purple");
            GameObject gameObject9 = (GameObject)this.items.LoadAsset("questitem_scroll_blue");
            GameObject gameObject10 = (GameObject)this.items.LoadAsset("questitem_scroll_black");
            GameObject gameObject11 = (GameObject)this.items.LoadAsset("questitem_necklace_green");
            GameObject gameObject12 = (GameObject)this.items.LoadAsset("questitem_necklace_red");
            GameObject gameObject13 = (GameObject)this.items.LoadAsset("questitem_pot_01");
            GameObject gameObject14 = (GameObject)this.items.LoadAsset("questitem_iceball");
            GameObject gameObject15 = (GameObject)this.items.LoadAsset("questitem_goblet");
            GameObject gameObject16 = (GameObject)this.items.LoadAsset("questitem_coinpurse");
            GameObject gameObject17 = (GameObject)this.items.LoadAsset("questitem_ghostlyheart");
            GameObject gameObject18 = (GameObject)this.items.LoadAsset("questitem_dragonheart");
            GameObject gameObject19 = (GameObject)this.items.LoadAsset("questitem_griffinfeather");
            GameObject gameObject20 = (GameObject)this.items.LoadAsset("questitem_book_01");
            GameObject gameObject21 = (GameObject)this.items.LoadAsset("questitem_book_02");
            GameObject gameObject22 = (GameObject)this.items.LoadAsset("questitem_book_03");
            GameObject gameObject23 = (GameObject)this.items.LoadAsset("questitem_book_04");
            GameObject gameObject24 = (GameObject)this.items.LoadAsset("questitem_straw_doll");
            CustomItem customItem = new CustomItem(gameObject, false, new ItemConfig
            {
                Name = "Оселок",
                Description = "Шероховатый камень, используемый для заточки лезвий"
            });
            customItem.ItemDrop.m_itemData.m_shared.m_teleportable = false;
            customItem.ItemDrop.m_itemData.m_shared.m_maxStackSize = 1;
            customItem.ItemDrop.m_itemData.m_shared.m_weight = 25f;
            CustomItem customItem2 = new CustomItem(gameObject2, false, new ItemConfig
            {
                Name = "Эссенция ветра",
                Description = "Странно сплоченный и твердый шар ветра, случайные порывы которого обдают вашу руку"
            });
            customItem2.ItemDrop.m_itemData.m_shared.m_teleportable = false;
            customItem2.ItemDrop.m_itemData.m_shared.m_maxStackSize = 1;
            customItem2.ItemDrop.m_itemData.m_shared.m_weight = 25f;
            CustomItem customItem3 = new CustomItem(gameObject3, false, new ItemConfig
            {
                Name = "Дыхание призрака",
                Description = "Разлитое дыхание призрака по бутылкам. Пахнет ужасно"
            });
            customItem3.ItemDrop.m_itemData.m_shared.m_teleportable = false;
            customItem3.ItemDrop.m_itemData.m_shared.m_maxStackSize = 1;
            customItem3.ItemDrop.m_itemData.m_shared.m_weight = 25f;
            CustomItem customItem4 = new CustomItem(gameObject4, false, new ItemConfig
            {
                Name = "Глаз дракона",
                Description = "Глаз дракона, вы все еще можете почувствовать его зловещий блеск"
            });
            customItem4.ItemDrop.m_itemData.m_shared.m_teleportable = false;
            customItem4.ItemDrop.m_itemData.m_shared.m_maxStackSize = 1;
            customItem4.ItemDrop.m_itemData.m_shared.m_weight = 25f;
            CustomItem customItem5 = new CustomItem(gameObject5, false, new ItemConfig
            {
                Name = "Свиток в красном переплете",
                Description = "Свиток папируса, вылинявший и хрупкий"
            });
            CustomItem customItem6 = new CustomItem(gameObject6, false, new ItemConfig
            {
                Name = "Свиток особой трансмогрификации",
                Description = "Свиток для трансмогрификации вашей экипировки"
            });
            customItem6.ItemDrop.m_itemData.m_shared.m_icons[0] = AssetUtils.LoadSpriteFromFile("sq1.png");
            CustomItem customItem7 = new CustomItem(gameObject7, false, new ItemConfig
            {
                Name = "Свиток в желтом переплете",
                Description = "Свиток папируса, вылинявший и хрупкий"
            });
            CustomItem customItem8 = new CustomItem(gameObject8, false, new ItemConfig
            {
                Name = "Свиток трансмогрификации",
                Description = "Свиток для трансмогрификации вашей экипировки"
            });
            customItem8.ItemDrop.m_itemData.m_shared.m_icons[0] = AssetUtils.LoadSpriteFromFile("sq2.png");
            CustomItem customItem9 = new CustomItem(gameObject9, false, new ItemConfig
            {
                Name = "Свиток в синем переплете",
                Description = "Свиток папируса, вылинявший и хрупкий"
            });
            CustomItem customItem10 = new CustomItem(gameObject10, false, new ItemConfig
            {
                Name = "Свиток знаний",
                Description = "Свиток папируса, наполненный знаниями"
            });
            customItem10.ItemDrop.m_itemData.m_shared.m_icons[0] = AssetUtils.LoadSpriteFromFile("sq3.png");
            CustomItem customItem11 = new CustomItem(gameObject11, false, new ItemConfig
            {
                Name = "Зеленое ожерелье с подвеской",
                Description = "Изысканный кулон, свисающий с золотой и серебряной цепочки"
            });
            customItem11.ItemDrop.m_itemData.m_shared.m_teleportable = false;
            customItem11.ItemDrop.m_itemData.m_shared.m_maxStackSize = 1;
            customItem11.ItemDrop.m_itemData.m_shared.m_weight = 25f;
            CustomItem customItem12 = new CustomItem(gameObject12, false, new ItemConfig
            {
                Name = "Красное ожерелье с подвеской",
                Description = "Простой красный кристалл, свисающий с золотой цепочки"
            });
            customItem12.ItemDrop.m_itemData.m_shared.m_teleportable = false;
            customItem12.ItemDrop.m_itemData.m_shared.m_maxStackSize = 1;
            customItem12.ItemDrop.m_itemData.m_shared.m_weight = 25f;
            CustomItem customItem13 = new CustomItem(gameObject13, false, new ItemConfig
            {
                Name = "Потускневший горшок",
                Description = "Потускневший железный горшок"
            });
            customItem13.ItemDrop.m_itemData.m_shared.m_teleportable = false;
            customItem13.ItemDrop.m_itemData.m_shared.m_maxStackSize = 1;
            customItem13.ItemDrop.m_itemData.m_shared.m_weight = 25f;
            CustomItem customItem14 = new CustomItem(gameObject14, false, new ItemConfig
            {
                Name = "Ледяной шар",
                Description = "Относительно совершенная ледяная сфера"
            });
            customItem14.ItemDrop.m_itemData.m_shared.m_teleportable = false;
            customItem14.ItemDrop.m_itemData.m_shared.m_maxStackSize = 1;
            customItem14.ItemDrop.m_itemData.m_shared.m_weight = 25f;
            CustomItem customItem15 = new CustomItem(gameObject15, false, new ItemConfig
            {
                Name = "Медный кубок",
                Description = "Украшенный медный кубок"
            });
            customItem15.ItemDrop.m_itemData.m_shared.m_teleportable = false;
            customItem15.ItemDrop.m_itemData.m_shared.m_maxStackSize = 1;
            customItem15.ItemDrop.m_itemData.m_shared.m_weight = 25f;
            CustomItem customItem16 = new CustomItem(gameObject16, false, new ItemConfig
            {
                Name = "Кожаный кошелек для монет",
                Description = "Простой кожаный кошелек для монет, застегивающийся на кожаные завязки"
            });
            customItem16.ItemDrop.m_itemData.m_shared.m_teleportable = false;
            customItem16.ItemDrop.m_itemData.m_shared.m_maxStackSize = 1;
            customItem16.ItemDrop.m_itemData.m_shared.m_weight = 25f;
            CustomItem customItem17 = new CustomItem(gameObject17, false, new ItemConfig
            {
                Name = "Призрачное сердце",
                Description = "Холодное пурпурное пламя, которое, кажется, излучает злобу и ненависть"
            });
            customItem17.ItemDrop.m_itemData.m_shared.m_teleportable = false;
            customItem17.ItemDrop.m_itemData.m_shared.m_maxStackSize = 1;
            customItem17.ItemDrop.m_itemData.m_shared.m_weight = 25f;
            CustomItem customItem18 = new CustomItem(gameObject18, false, new ItemConfig
            {
                Name = "Драконье сердце",
                Description = "Единственное бьющееся сердце дракона, теперь холодный и безжизненный кусок мяса синего цвета"
            });
            customItem18.ItemDrop.m_itemData.m_shared.m_teleportable = true;
            customItem18.ItemDrop.m_itemData.m_shared.m_maxStackSize = 10000;
            customItem18.ItemDrop.m_itemData.m_shared.m_weight = 0f;
            CustomItem customItem19 = new CustomItem(gameObject19, false, new ItemConfig
            {
                Name = "Перо грифона",
                Description = "Шелковистое перо из гривы грифона"
            });
            customItem19.ItemDrop.m_itemData.m_shared.m_teleportable = false;
            customItem19.ItemDrop.m_itemData.m_shared.m_maxStackSize = 1;
            customItem19.ItemDrop.m_itemData.m_shared.m_weight = 25f;
            CustomItem customItem20 = new CustomItem(gameObject20, false, new ItemConfig
            {
                Name = "Потрепанная книга",
                Description = "Эта книга знавала лучшие времена, ее корешок потрескался, а страницы пожелтели"
            });
            customItem20.ItemDrop.m_itemData.m_shared.m_teleportable = false;
            customItem20.ItemDrop.m_itemData.m_shared.m_maxStackSize = 1;
            customItem20.ItemDrop.m_itemData.m_shared.m_weight = 25f;
            CustomItem customItem21 = new CustomItem(gameObject21, false, new ItemConfig
            {
                Name = "Старая книга",
                Description = "Эта книга старая и потрепанная, она слишком долго пролежала среди стихий. Содержит знания тактик ведения боя"
            });
            customItem21.ItemDrop.m_itemData.m_shared.m_teleportable = false;
            customItem21.ItemDrop.m_itemData.m_shared.m_maxStackSize = 1;
            customItem21.ItemDrop.m_itemData.m_shared.m_weight = 1f;
            CustomItem customItem22 = new CustomItem(gameObject22, false, new ItemConfig
            {
                Name = "Книга древней магии",
                Description = "Эта книга древняя по любым меркам, обложка потрескалась и сделана из какой-то кожи животного происхождения, а страницы стали хрупкими от старости."
            });
            customItem22.ItemDrop.m_itemData.m_shared.m_teleportable = false;
            customItem22.ItemDrop.m_itemData.m_shared.m_maxStackSize = 1;
            customItem22.ItemDrop.m_itemData.m_shared.m_weight = 1f;
            CustomItem customItem23 = new CustomItem(gameObject23, false, new ItemConfig
            {
                Name = "Потрепанная книга тайн",
                Description = "Эта книга выглядит так, словно кто-то выбросил ее на улицу и оставил там.  Обложка в пятнах и порвана, да и страницы выглядят не лучше."
            });
            customItem23.ItemDrop.m_itemData.m_shared.m_teleportable = false;
            customItem23.ItemDrop.m_itemData.m_shared.m_maxStackSize = 1;
            customItem23.ItemDrop.m_itemData.m_shared.m_weight = 1f;
            CustomItem customItem24 = new CustomItem(gameObject24, false, new ItemConfig
            {
                Name = "Соломенная кукла",
                Description = "Простая кукла, сделанная из соломы и бечевки."
            });
            ItemManager.Instance.AddItem(customItem);
            ItemManager.Instance.AddItem(customItem2);
            ItemManager.Instance.AddItem(customItem3);
            ItemManager.Instance.AddItem(customItem4);
            ItemManager.Instance.AddItem(customItem5);
            ItemManager.Instance.AddItem(customItem6);
            ItemManager.Instance.AddItem(customItem7);
            ItemManager.Instance.AddItem(customItem8);
            ItemManager.Instance.AddItem(customItem9);
            ItemManager.Instance.AddItem(customItem10);
            ItemManager.Instance.AddItem(customItem11);
            ItemManager.Instance.AddItem(customItem12);
            ItemManager.Instance.AddItem(customItem13);
            ItemManager.Instance.AddItem(customItem14);
            ItemManager.Instance.AddItem(customItem15);
            ItemManager.Instance.AddItem(customItem16);
            ItemManager.Instance.AddItem(customItem17);
            ItemManager.Instance.AddItem(customItem18);
            ItemManager.Instance.AddItem(customItem19);
            ItemManager.Instance.AddItem(customItem20);
            ItemManager.Instance.AddItem(customItem21);
            ItemManager.Instance.AddItem(customItem22);
            ItemManager.Instance.AddItem(customItem23);
            ItemManager.Instance.AddItem(customItem24);
            PrefabManager.OnVanillaPrefabsAvailable -= this.GDItems;
        }

        // Token: 0x06000004 RID: 4 RVA: 0x00002D58 File Offset: 0x00000F58
        public void CustomItems()
        {
            GameObject gameObject = PrefabManager.Instance.CreateClonedPrefab("SwampCargo2", "TrophyDraugr");
            GameObject gameObject2 = PrefabManager.Instance.CreateClonedPrefab("MountainCargo2", "TrophyWolf");
            GameObject gameObject3 = PrefabManager.Instance.CreateClonedPrefab("PlainsCargo2", "TrophyGoblinBrute");
            GameObject gameObject4 = PrefabManager.Instance.CreateClonedPrefab("MistyCargo2", "TrophySeeker");
            GameObject gameObject5 = PrefabManager.Instance.CreateClonedPrefab("MeadowCargo", "RawMeat");
            GameObject gameObject6 = PrefabManager.Instance.CreateClonedPrefab("ForestCargo", "Bronze");
            GameObject gameObject7 = PrefabManager.Instance.CreateClonedPrefab("SwampCargo", "Iron");
            GameObject gameObject8 = PrefabManager.Instance.CreateClonedPrefab("MountainCargo", "Silver");
            GameObject gameObject9 = PrefabManager.Instance.CreateClonedPrefab("PlainsCargo", "BlackMetal");
            GameObject gameObject10 = PrefabManager.Instance.CreateClonedPrefab("MistyCargo", "Eitr");
            GameObject gameObject11 = PrefabManager.Instance.CreateClonedPrefab("QuestBox", "chest_hildir1");
            GameObject gameObject12 = PrefabManager.Instance.CreateClonedPrefab("FishCargo", "FishRaw");
            GameObject gameObject13 = PrefabManager.Instance.CreateClonedPrefab("WoodCargo", "FineWood");
            GameObject gameObject14 = PrefabManager.Instance.CreateClonedPrefab("FoodCargo", "BarleyFlour");
            GameObject gameObject15 = PrefabManager.Instance.CreateClonedPrefab("PeltCargo", "DeerHide");
            GameObject prefab = PrefabManager.Instance.GetPrefab("Coins");
            GameObject gameObject16 = PrefabManager.Instance.CreateClonedPrefab("PickaxeTerra", "PickaxeAntler");
            CustomItem customItem = new CustomItem(gameObject5, false);
            customItem.ItemDrop.m_itemData.m_shared.m_teleportable = false;
            customItem.ItemDrop.m_itemData.m_shared.m_name = "Товары из Хьяртейка";
            customItem.ItemDrop.m_itemData.m_shared.m_description = "Товары из города Хьяртейк - места, с богатыми охотничьями угодьями";
            customItem.ItemDrop.m_itemData.m_shared.m_maxStackSize = 1;
            customItem.ItemDrop.m_itemData.m_shared.m_weight = 25f;
            CustomItem customItem2 = new CustomItem(gameObject6, false);
            customItem2.ItemDrop.m_itemData.m_shared.m_teleportable = false;
            customItem2.ItemDrop.m_itemData.m_shared.m_name = "Товары из Блеквуда";
            customItem2.ItemDrop.m_itemData.m_shared.m_description = "Товары из города Блэквуд - источника бронзы";
            customItem2.ItemDrop.m_itemData.m_shared.m_maxStackSize = 1;
            customItem2.ItemDrop.m_itemData.m_shared.m_weight = 25f;
            CustomItem customItem3 = new CustomItem(gameObject7, false);
            customItem3.ItemDrop.m_itemData.m_shared.m_teleportable = false;
            customItem3.ItemDrop.m_itemData.m_shared.m_name = "Товары из Ильдби";
            customItem3.ItemDrop.m_itemData.m_shared.m_description = "Товары из города Ильдби - железо добытое в склепах трудом и потом";
            customItem3.ItemDrop.m_itemData.m_shared.m_maxStackSize = 1;
            customItem3.ItemDrop.m_itemData.m_shared.m_weight = 25f;
            CustomItem customItem4 = new CustomItem(gameObject8, false);
            customItem4.ItemDrop.m_itemData.m_shared.m_teleportable = false;
            customItem4.ItemDrop.m_itemData.m_shared.m_name = "Товары из Скраннингена";
            customItem4.ItemDrop.m_itemData.m_shared.m_description = "Товары из города Скранинген - белое золото, что легко поддается обработке";
            customItem4.ItemDrop.m_itemData.m_shared.m_maxStackSize = 1;
            customItem4.ItemDrop.m_itemData.m_shared.m_weight = 25f;
            CustomItem customItem5 = new CustomItem(gameObject9, false);
            customItem5.ItemDrop.m_itemData.m_shared.m_teleportable = false;
            customItem5.ItemDrop.m_itemData.m_shared.m_name = "Товары из Йоргенхельда";
            customItem5.ItemDrop.m_itemData.m_shared.m_description = "Товары из города Йоргенхельд - метал, что чернее ночи. ";
            customItem5.ItemDrop.m_itemData.m_shared.m_maxStackSize = 1;
            customItem5.ItemDrop.m_itemData.m_shared.m_weight = 25f;
            CustomItem customItem6 = new CustomItem(gameObject10, false);
            customItem6.ItemDrop.m_itemData.m_shared.m_teleportable = false;
            customItem6.ItemDrop.m_itemData.m_shared.m_name = "Товары из Свартгула";
            customItem6.ItemDrop.m_itemData.m_shared.m_description = "Товары из города Свартгул - это вещество жизни, яд, который поглощает сам себя";
            customItem6.ItemDrop.m_itemData.m_shared.m_maxStackSize = 1;
            customItem6.ItemDrop.m_itemData.m_shared.m_weight = 25f;
            CustomItem customItem7 = new CustomItem(gameObject11, false);
            customItem7.ItemDrop.m_itemData.m_shared.m_teleportable = false;
            customItem7.ItemDrop.m_itemData.m_shared.m_name = "Тяжелый сундук";
            customItem7.ItemDrop.m_itemData.m_shared.m_description = "Содержимое этого ящика принадлежат купцу. Крайне тяжелый";
            customItem7.ItemDrop.m_itemData.m_shared.m_maxStackSize = 1;
            customItem7.ItemDrop.m_itemData.m_shared.m_weight = 25f;
            CustomItem customItem8 = new CustomItem(gameObject12, false);
            customItem8.ItemDrop.m_itemData.m_shared.m_teleportable = false;
            customItem8.ItemDrop.m_itemData.m_shared.m_name = "Груз свежей рыбы";
            customItem8.ItemDrop.m_itemData.m_shared.m_description = "Содержимое этого товара - свежепойманная, освежеванная рыба";
            customItem8.ItemDrop.m_itemData.m_shared.m_maxStackSize = 1;
            customItem8.ItemDrop.m_itemData.m_shared.m_weight = 25f;
            CustomItem customItem9 = new CustomItem(gameObject13, false);
            customItem9.ItemDrop.m_itemData.m_shared.m_teleportable = false;
            customItem9.ItemDrop.m_itemData.m_shared.m_name = "Груз многолетней древесины";
            customItem9.ItemDrop.m_itemData.m_shared.m_description = "Это дерево крайне прочное. Плотники дадут за него отличную цену!";
            customItem9.ItemDrop.m_itemData.m_shared.m_maxStackSize = 1;
            customItem9.ItemDrop.m_itemData.m_shared.m_weight = 25f;
            CustomItem customItem10 = new CustomItem(gameObject14, false);
            customItem10.ItemDrop.m_itemData.m_shared.m_teleportable = false;
            customItem10.ItemDrop.m_itemData.m_shared.m_name = "Мешок продуктов";
            customItem10.ItemDrop.m_itemData.m_shared.m_description = "В мешке местный урожай и продукты. Будьте осторожны - запах привлекает зверей!";
            customItem10.ItemDrop.m_itemData.m_shared.m_maxStackSize = 1;
            customItem10.ItemDrop.m_itemData.m_shared.m_weight = 25f;
            CustomItem customItem11 = new CustomItem(gameObject15, false);
            customItem11.ItemDrop.m_itemData.m_shared.m_teleportable = false;
            customItem11.ItemDrop.m_itemData.m_shared.m_name = "Тяжелый сундук";
            customItem11.ItemDrop.m_itemData.m_shared.m_description = "Шкуры редких горных оленей. Согреют даже в самый хлад!";
            customItem11.ItemDrop.m_itemData.m_shared.m_maxStackSize = 1;
            customItem11.ItemDrop.m_itemData.m_shared.m_weight = 25f;
            new CustomItem(prefab, false)
            {
                ItemDrop =
                {
                    m_itemData =
                    {
                        m_shared =
                        {
                            m_weight = 0f,
                            m_maxStackSize = 10000
                        }
                    }
                }
            }.ItemDrop.m_itemData.m_shared.m_icons[0] = AssetUtils.LoadSpriteFromFile("Coins.png");
            CustomItem customItem12 = new CustomItem(gameObject16, false);
            customItem12.ItemDrop.m_itemData.m_shared.m_attackForce = 1f;
            customItem12.ItemDrop.m_itemData.m_shared.m_icons[0] = AssetUtils.LoadSpriteFromFile("pickaxe.png");
            CustomItem customItem13 = new CustomItem(gameObject, false);
            customItem13.ItemDrop.m_itemData.m_shared.m_teleportable = false;
            customItem13.ItemDrop.m_itemData.m_shared.m_name = "Особый товар из Ильдби";
            customItem13.ItemDrop.m_itemData.m_shared.m_description = "Содержимое этого ящика - предметы, что можно найти только в болотах";
            customItem13.ItemDrop.m_itemData.m_shared.m_maxStackSize = 1;
            customItem13.ItemDrop.m_itemData.m_shared.m_weight = 25f;
            CustomItem customItem14 = new CustomItem(gameObject2, false);
            customItem14.ItemDrop.m_itemData.m_shared.m_teleportable = false;
            customItem14.ItemDrop.m_itemData.m_shared.m_name = "Особый товар из Скраннингена";
            customItem14.ItemDrop.m_itemData.m_shared.m_description = "Содержимое этого ящика - предметы, что можно найти только в горах";
            customItem14.ItemDrop.m_itemData.m_shared.m_maxStackSize = 1;
            customItem14.ItemDrop.m_itemData.m_shared.m_weight = 25f;
            CustomItem customItem15 = new CustomItem(gameObject3, false);
            customItem15.ItemDrop.m_itemData.m_shared.m_name = "Особый товар из Йоргенхельда";
            customItem15.ItemDrop.m_itemData.m_shared.m_description = "Содержимое этого ящика - предметы, что можно найти только на равнинах";
            customItem15.ItemDrop.m_itemData.m_shared.m_maxStackSize = 1;
            customItem15.ItemDrop.m_itemData.m_shared.m_weight = 25f;
            CustomItem customItem16 = new CustomItem(gameObject4, false);
            customItem16.ItemDrop.m_itemData.m_shared.m_teleportable = false;
            customItem16.ItemDrop.m_itemData.m_shared.m_name = "Особый товар из Свартгула";
            customItem16.ItemDrop.m_itemData.m_shared.m_description = "Содержимое этого ящика - предметы, что можно найти только в туманных землях";
            customItem16.ItemDrop.m_itemData.m_shared.m_maxStackSize = 1;
            customItem16.ItemDrop.m_itemData.m_shared.m_weight = 25f;
            ItemManager.Instance.AddItem(customItem);
            ItemManager.Instance.AddItem(customItem2);
            ItemManager.Instance.AddItem(customItem3);
            ItemManager.Instance.AddItem(customItem4);
            ItemManager.Instance.AddItem(customItem5);
            ItemManager.Instance.AddItem(customItem6);
            ItemManager.Instance.AddItem(customItem7);
            ItemManager.Instance.AddItem(customItem8);
            ItemManager.Instance.AddItem(customItem9);
            ItemManager.Instance.AddItem(customItem10);
            ItemManager.Instance.AddItem(customItem11);
            ItemManager.Instance.AddItem(customItem12);
            ItemManager.Instance.AddItem(customItem13);
            ItemManager.Instance.AddItem(customItem14);
            ItemManager.Instance.AddItem(customItem15);
            ItemManager.Instance.AddItem(customItem16);
            PrefabManager.OnVanillaPrefabsAvailable -= this.CustomItems;
        }

        // Token: 0x06000005 RID: 5 RVA: 0x0000384C File Offset: 0x00001A4C
        public void WarfareItems()
        {
            GameObject gameObject = (GameObject)this.fantasy.LoadAsset("Shield_09_FA");
            GameObject gameObject2 = (GameObject)this.fantasy.LoadAsset("Shield_08_FA");
            GameObject gameObject3 = (GameObject)this.fantasy.LoadAsset("Shield_07_FA");
            GameObject gameObject4 = (GameObject)this.fantasy.LoadAsset("Shield_06_FA");
            GameObject gameObject5 = (GameObject)this.fantasy.LoadAsset("Shield_05_FA");
            GameObject gameObject6 = (GameObject)this.fantasy.LoadAsset("Shield_04_FA");
            GameObject gameObject7 = (GameObject)this.fantasy.LoadAsset("Shield_03_FA");
            GameObject gameObject8 = (GameObject)this.fantasy.LoadAsset("Shield_02_FA");
            GameObject gameObject9 = (GameObject)this.fantasy.LoadAsset("Shield_01_FA");
            GameObject gameObject10 = (GameObject)this.fantasy.LoadAsset("Axe_1H_07_FA");
            GameObject gameObject11 = (GameObject)this.fantasy.LoadAsset("Axe2H_01_FA");
            GameObject gameObject12 = (GameObject)this.fantasy.LoadAsset("Axe2H_02_FA");
            GameObject gameObject13 = (GameObject)this.fantasy.LoadAsset("Axe2H_03_FA");
            GameObject gameObject14 = (GameObject)this.fantasy.LoadAsset("Axe2H_04_FA");
            GameObject gameObject15 = (GameObject)this.fantasy.LoadAsset("Axe2H_05_FA");
            GameObject gameObject16 = (GameObject)this.fantasy.LoadAsset("Axe2H_06_FA");
            GameObject gameObject17 = (GameObject)this.fantasy.LoadAsset("Hammer_2H_01_FA");
            GameObject gameObject18 = (GameObject)this.fantasy.LoadAsset("Hammer_2H_02_FA");
            GameObject gameObject19 = (GameObject)this.fantasy.LoadAsset("Hammer_2H_03_FA");
            GameObject gameObject20 = (GameObject)this.fantasy.LoadAsset("Sword_2H_01_FA");
            GameObject gameObject21 = (GameObject)this.fantasy.LoadAsset("Sword_2H_02_FA");
            GameObject gameObject22 = (GameObject)this.fantasy.LoadAsset("Sword_2H_03_FA");
            GameObject gameObject23 = (GameObject)this.fantasy.LoadAsset("Sword_2H_04_FA");
            GameObject gameObject24 = (GameObject)this.fantasy.LoadAsset("Sword_2H_05_FA");
            GameObject gameObject25 = (GameObject)this.fantasy.LoadAsset("Sword_2H_06_FA");
            GameObject gameObject26 = (GameObject)this.fantasy.LoadAsset("Scythe2H_01_FA");
            GameObject gameObject27 = (GameObject)this.fantasy.LoadAsset("Staff_2H_01_FA");
            GameObject gameObject28 = (GameObject)this.fantasy.LoadAsset("Staff_2H_02_FA");
            GameObject gameObject29 = (GameObject)this.fantasy.LoadAsset("Staff_2H_03_FA");
            GameObject gameObject30 = (GameObject)this.fantasy.LoadAsset("Staff_2H_04_FA");
            GameObject gameObject31 = (GameObject)this.fantasy.LoadAsset("Staff_2H_05_FA");
            GameObject gameObject32 = (GameObject)this.fantasy.LoadAsset("Axe_1H_01_FA");
            GameObject gameObject33 = (GameObject)this.fantasy.LoadAsset("Axe_1H_02_FA");
            GameObject gameObject34 = (GameObject)this.fantasy.LoadAsset("Axe_1H_03_FA");
            GameObject gameObject35 = (GameObject)this.fantasy.LoadAsset("Axe_1H_04_FA");
            GameObject gameObject36 = (GameObject)this.fantasy.LoadAsset("Axe_1H_05_FA");
            GameObject gameObject37 = (GameObject)this.fantasy.LoadAsset("Axe_1H_06_FA");
            GameObject gameObject38 = (GameObject)this.fantasy.LoadAsset("Sword_1H_01_FA");
            GameObject gameObject39 = (GameObject)this.fantasy.LoadAsset("Sword_1H_02_FA");
            GameObject gameObject40 = (GameObject)this.fantasy.LoadAsset("Sword_1H_03_FA");
            GameObject gameObject41 = (GameObject)this.fantasy.LoadAsset("Sword_1H_04_FA");
            GameObject gameObject42 = (GameObject)this.fantasy.LoadAsset("Sword_1H_05_FA");
            GameObject gameObject43 = (GameObject)this.henrik.LoadAsset("HenriksTestSword");
            GameObject gameObject44 = (GameObject)this.henrik.LoadAsset("ScuffedSword");
            GameObject gameObject45 = (GameObject)this.henrik.LoadAsset("Megid");
            GameObject gameObject46 = (GameObject)this.henrik.LoadAsset("Musou_Isshin");
            GameObject gameObject47 = (GameObject)this.henrik.LoadAsset("Partizan");
            GameObject gameObject48 = (GameObject)this.henrik.LoadAsset("SkywardBlade");
            GameObject gameObject49 = (GameObject)this.henrik.LoadAsset("DarkSword");
            GameObject gameObject50 = (GameObject)this.henrik.LoadAsset("JadeCutter");
            GameObject gameObject51 = (GameObject)this.henrik.LoadAsset("TheFlute");
            GameObject gameObject52 = (GameObject)this.henrik.LoadAsset("HenrikMs");
            GameObject gameObject53 = (GameObject)this.magicbow.LoadAsset("BMB_Crossbow_Fiery");
            GameObject gameObject54 = (GameObject)this.magicbow.LoadAsset("BMB_Crossbow_Frost");
            GameObject gameObject55 = (GameObject)this.magicbow.LoadAsset("BMB_Crossbow_Lightning");
            GameObject gameObject56 = (GameObject)this.magicbow.LoadAsset("BMB_Crossbow_Spirit");
            GameObject gameObject57 = (GameObject)this.magicbow.LoadAsset("BMB_Crossbow_Toxic");
            GameObject gameObject58 = (GameObject)this.magicbow.LoadAsset("BMB_FieryBow");
            GameObject gameObject59 = (GameObject)this.magicbow.LoadAsset("BMB_FrozenBow");
            GameObject gameObject60 = (GameObject)this.magicbow.LoadAsset("BMB_LightningBow");
            GameObject gameObject61 = (GameObject)this.magicbow.LoadAsset("BMB_SpiritBow");
            GameObject gameObject62 = (GameObject)this.magicbow.LoadAsset("BMB_ToxicBow");
            CustomItem customItem = new CustomItem(gameObject, false);
            customItem.ItemDrop.m_itemData.m_shared.m_itemType = (ItemDrop.ItemData.ItemType)5;
            CustomItem customItem2 = new CustomItem(gameObject2, false);
            customItem2.ItemDrop.m_itemData.m_shared.m_itemType = (ItemDrop.ItemData.ItemType)5;
            CustomItem customItem3 = new CustomItem(gameObject3, false);
            customItem3.ItemDrop.m_itemData.m_shared.m_itemType = (ItemDrop.ItemData.ItemType)5;
            CustomItem customItem4 = new CustomItem(gameObject4, false);
            customItem4.ItemDrop.m_itemData.m_shared.m_itemType = (ItemDrop.ItemData.ItemType)5;
            CustomItem customItem5 = new CustomItem(gameObject5, false);
            customItem5.ItemDrop.m_itemData.m_shared.m_itemType = (ItemDrop.ItemData.ItemType)5;
            CustomItem customItem6 = new CustomItem(gameObject6, false);
            customItem6.ItemDrop.m_itemData.m_shared.m_itemType = (ItemDrop.ItemData.ItemType)5;
            CustomItem customItem7 = new CustomItem(gameObject7, false);
            customItem7.ItemDrop.m_itemData.m_shared.m_itemType = (ItemDrop.ItemData.ItemType)5;
            CustomItem customItem8 = new CustomItem(gameObject8, false);
            customItem8.ItemDrop.m_itemData.m_shared.m_itemType = (ItemDrop.ItemData.ItemType)5;
            CustomItem customItem9 = new CustomItem(gameObject9, false);
            customItem9.ItemDrop.m_itemData.m_shared.m_itemType = (ItemDrop.ItemData.ItemType)5;
            CustomItem customItem10 = new CustomItem(gameObject10, false);
            CustomItem customItem11 = new CustomItem(gameObject11, false);
            CustomItem customItem12 = new CustomItem(gameObject12, false);
            CustomItem customItem13 = new CustomItem(gameObject13, false);
            CustomItem customItem14 = new CustomItem(gameObject14, false);
            CustomItem customItem15 = new CustomItem(gameObject15, false);
            CustomItem customItem16 = new CustomItem(gameObject16, false);
            CustomItem customItem17 = new CustomItem(gameObject17, false);
            CustomItem customItem18 = new CustomItem(gameObject18, false);
            CustomItem customItem19 = new CustomItem(gameObject19, false);
            CustomItem customItem20 = new CustomItem(gameObject20, false);
            CustomItem customItem21 = new CustomItem(gameObject21, false);
            CustomItem customItem22 = new CustomItem(gameObject22, false);
            CustomItem customItem23 = new CustomItem(gameObject23, false);
            CustomItem customItem24 = new CustomItem(gameObject24, false);
            CustomItem customItem25 = new CustomItem(gameObject25, false);
            CustomItem customItem26 = new CustomItem(gameObject26, false);
            CustomItem customItem27 = new CustomItem(gameObject27, false);
            CustomItem customItem28 = new CustomItem(gameObject28, false);
            CustomItem customItem29 = new CustomItem(gameObject29, false);
            CustomItem customItem30 = new CustomItem(gameObject30, false);
            CustomItem customItem31 = new CustomItem(gameObject31, false);
            CustomItem customItem32 = new CustomItem(gameObject32, false);
            CustomItem customItem33 = new CustomItem(gameObject33, false);
            CustomItem customItem34 = new CustomItem(gameObject34, false);
            CustomItem customItem35 = new CustomItem(gameObject35, false);
            CustomItem customItem36 = new CustomItem(gameObject36, false);
            CustomItem customItem37 = new CustomItem(gameObject37, false);
            CustomItem customItem38 = new CustomItem(gameObject38, false);
            CustomItem customItem39 = new CustomItem(gameObject39, false);
            CustomItem customItem40 = new CustomItem(gameObject40, false);
            CustomItem customItem41 = new CustomItem(gameObject41, false);
            CustomItem customItem42 = new CustomItem(gameObject42, false);
            CustomItem customItem43 = new CustomItem(gameObject43, false);
            CustomItem customItem44 = new CustomItem(gameObject44, false);
            CustomItem customItem45 = new CustomItem(gameObject45, false);
            CustomItem customItem46 = new CustomItem(gameObject46, false);
            CustomItem customItem47 = new CustomItem(gameObject47, false);
            CustomItem customItem48 = new CustomItem(gameObject48, false);
            CustomItem customItem49 = new CustomItem(gameObject49, false);
            CustomItem customItem50 = new CustomItem(gameObject50, false);
            CustomItem customItem51 = new CustomItem(gameObject51, false);
            CustomItem customItem52 = new CustomItem(gameObject52, false);
            CustomItem customItem53 = new CustomItem(gameObject53, false);
            CustomItem customItem54 = new CustomItem(gameObject54, false);
            CustomItem customItem55 = new CustomItem(gameObject55, false);
            CustomItem customItem56 = new CustomItem(gameObject56, false);
            CustomItem customItem57 = new CustomItem(gameObject57, false);
            CustomItem customItem58 = new CustomItem(gameObject58, false);
            CustomItem customItem59 = new CustomItem(gameObject59, false);
            CustomItem customItem60 = new CustomItem(gameObject60, false);
            CustomItem customItem61 = new CustomItem(gameObject61, false);
            CustomItem customItem62 = new CustomItem(gameObject62, false);
            ItemManager.Instance.AddItem(customItem);
            ItemManager.Instance.AddItem(customItem2);
            ItemManager.Instance.AddItem(customItem3);
            ItemManager.Instance.AddItem(customItem4);
            ItemManager.Instance.AddItem(customItem5);
            ItemManager.Instance.AddItem(customItem6);
            ItemManager.Instance.AddItem(customItem7);
            ItemManager.Instance.AddItem(customItem8);
            ItemManager.Instance.AddItem(customItem9);
            ItemManager.Instance.AddItem(customItem10);
            ItemManager.Instance.AddItem(customItem11);
            ItemManager.Instance.AddItem(customItem12);
            ItemManager.Instance.AddItem(customItem13);
            ItemManager.Instance.AddItem(customItem14);
            ItemManager.Instance.AddItem(customItem15);
            ItemManager.Instance.AddItem(customItem16);
            ItemManager.Instance.AddItem(customItem17);
            ItemManager.Instance.AddItem(customItem18);
            ItemManager.Instance.AddItem(customItem19);
            ItemManager.Instance.AddItem(customItem20);
            ItemManager.Instance.AddItem(customItem21);
            ItemManager.Instance.AddItem(customItem22);
            ItemManager.Instance.AddItem(customItem23);
            ItemManager.Instance.AddItem(customItem24);
            ItemManager.Instance.AddItem(customItem25);
            ItemManager.Instance.AddItem(customItem26);
            ItemManager.Instance.AddItem(customItem27);
            ItemManager.Instance.AddItem(customItem28);
            ItemManager.Instance.AddItem(customItem29);
            ItemManager.Instance.AddItem(customItem30);
            ItemManager.Instance.AddItem(customItem31);
            ItemManager.Instance.AddItem(customItem32);
            ItemManager.Instance.AddItem(customItem33);
            ItemManager.Instance.AddItem(customItem34);
            ItemManager.Instance.AddItem(customItem35);
            ItemManager.Instance.AddItem(customItem36);
            ItemManager.Instance.AddItem(customItem37);
            ItemManager.Instance.AddItem(customItem38);
            ItemManager.Instance.AddItem(customItem39);
            ItemManager.Instance.AddItem(customItem40);
            ItemManager.Instance.AddItem(customItem41);
            ItemManager.Instance.AddItem(customItem42);
            ItemManager.Instance.AddItem(customItem43);
            ItemManager.Instance.AddItem(customItem44);
            ItemManager.Instance.AddItem(customItem45);
            ItemManager.Instance.AddItem(customItem46);
            ItemManager.Instance.AddItem(customItem47);
            ItemManager.Instance.AddItem(customItem48);
            ItemManager.Instance.AddItem(customItem49);
            ItemManager.Instance.AddItem(customItem50);
            ItemManager.Instance.AddItem(customItem51);
            ItemManager.Instance.AddItem(customItem52);
            ItemManager.Instance.AddItem(customItem53);
            customItem53.ItemDrop.m_itemData.m_shared.m_itemType = (ItemDrop.ItemData.ItemType)4;
            ItemManager.Instance.AddItem(customItem54);
            customItem54.ItemDrop.m_itemData.m_shared.m_itemType = (ItemDrop.ItemData.ItemType)4;
            ItemManager.Instance.AddItem(customItem55);
            customItem55.ItemDrop.m_itemData.m_shared.m_itemType = (ItemDrop.ItemData.ItemType)4;
            ItemManager.Instance.AddItem(customItem56);
            customItem56.ItemDrop.m_itemData.m_shared.m_itemType = (ItemDrop.ItemData.ItemType)4;
            ItemManager.Instance.AddItem(customItem57);
            customItem57.ItemDrop.m_itemData.m_shared.m_itemType = (ItemDrop.ItemData.ItemType)4;
            ItemManager.Instance.AddItem(customItem58);
            customItem58.ItemDrop.m_itemData.m_shared.m_itemType = (ItemDrop.ItemData.ItemType)4;
            ItemManager.Instance.AddItem(customItem59);
            customItem59.ItemDrop.m_itemData.m_shared.m_itemType = (ItemDrop.ItemData.ItemType)4;
            ItemManager.Instance.AddItem(customItem60);
            customItem60.ItemDrop.m_itemData.m_shared.m_itemType = (ItemDrop.ItemData.ItemType)4;
            ItemManager.Instance.AddItem(customItem61);
            customItem61.ItemDrop.m_itemData.m_shared.m_itemType = (ItemDrop.ItemData.ItemType)4;
            ItemManager.Instance.AddItem(customItem62);
            customItem62.ItemDrop.m_itemData.m_shared.m_itemType = (ItemDrop.ItemData.ItemType)4;
            PrefabManager.OnVanillaPrefabsAvailable -= this.WarfareItems;
        }

        // Token: 0x06000006 RID: 6 RVA: 0x00004540 File Offset: 0x00002740
        public void CustomMobs()
        {
            CreatureConfig creatureConfig = new CreatureConfig();
            creatureConfig.AddDropConfig(new DropConfig
            {
                Item = "Coins",
                Chance = 0f
            });
            CreatureConfig creatureConfig2 = new CreatureConfig();
            creatureConfig2.AddDropConfig(new DropConfig
            {
                Item = "Coins",
                Chance = 0f
            });
            CreatureConfig creatureConfig3 = new CreatureConfig();
            creatureConfig3.AddDropConfig(new DropConfig
            {
                Item = "Coins",
                Chance = 0f
            });
            CreatureConfig creatureConfig4 = new CreatureConfig();
            creatureConfig4.AddDropConfig(new DropConfig
            {
                Item = "Coins",
                Chance = 0f
            });
            CustomCreature customCreature = new CustomCreature("DraugrNone", "Draugr_Ranged", creatureConfig);
            CustomCreature customCreature2 = new CustomCreature("SkeletonNone", "Skeleton", creatureConfig2);
            CustomCreature customCreature3 = new CustomCreature("SurtlingNone", "Surtling", creatureConfig3);
            CustomCreature customCreature4 = new CustomCreature("HatchlingNone", "Hatchling", creatureConfig4);
            CreatureManager.Instance.AddCreature(customCreature);
            CreatureManager.Instance.AddCreature(customCreature2);
            CreatureManager.Instance.AddCreature(customCreature3);
            CreatureManager.Instance.AddCreature(customCreature4);
            PrefabManager.OnVanillaPrefabsAvailable -= this.CustomMobs;
        }

        // Token: 0x06000007 RID: 7 RVA: 0x00004684 File Offset: 0x00002884
        /*
        public void CustomOre()
        {
            GameObject gameObject = PrefabManager.Instance.CreateClonedPrefab("Garlik2", "LeatherScraps");
            CustomItem customItem = new CustomItem(gameObject, false, new ItemConfig
            {
                Name = "Измельченные чеснок",
                Description = "Чеснок, что вы измельчили превратились в порошок"
            });
            customItem.ItemDrop.m_itemData.m_shared.m_maxStackSize = 30;
            customItem.ItemDrop.m_itemData.m_shared.m_weight = 1f;
            customItem.ItemDrop.m_itemData.m_shared.m_icons[0] = AssetUtils.LoadSpriteFromFile("Alh1.png");
            ItemManager.Instance.AddItem(customItem);
            GameObject gameObject2 = PrefabManager.Instance.CreateClonedPrefab("Potato2", "LeatherScraps");
            CustomItem customItem2 = new CustomItem(gameObject2, false, new ItemConfig
            {
                Name = "Измельченная мякоть картофеля",
                Description = "Картофель, что вы измельчили превратился в мягкую субстанцию"
            });
            customItem2.ItemDrop.m_itemData.m_shared.m_maxStackSize = 30;
            customItem2.ItemDrop.m_itemData.m_shared.m_weight = 1f;
            customItem2.ItemDrop.m_itemData.m_shared.m_icons[0] = AssetUtils.LoadSpriteFromFile("Alh2.png");
            ItemManager.Instance.AddItem(customItem2);
            GameObject gameObject3 = PrefabManager.Instance.CreateClonedPrefab("Peretz2", "FishingBait");
            CustomItem customItem3 = new CustomItem(gameObject3, false, new ItemConfig
            {
                Name = "Измельченные семяна огнефрукта",
                Description = "Семяна огнефрукта, что вы измельчили превратились в порошок"
            });
            customItem3.ItemDrop.m_itemData.m_shared.m_maxStackSize = 30;
            customItem3.ItemDrop.m_itemData.m_shared.m_weight = 1f;
            customItem3.ItemDrop.m_itemData.m_shared.m_icons[0] = AssetUtils.LoadSpriteFromFile("Alh3.png");
            ItemManager.Instance.AddItem(customItem3);
            GameObject gameObject4 = PrefabManager.Instance.CreateClonedPrefab("Pumkin2", "LeatherScraps");
            CustomItem customItem4 = new CustomItem(gameObject4, false, new ItemConfig
            {
                Name = "Измельченные семяна тыквы",
                Description = "Семяна тыквы, что вы измельчили превратились в порошок"
            });
            customItem4.ItemDrop.m_itemData.m_shared.m_maxStackSize = 30;
            customItem4.ItemDrop.m_itemData.m_shared.m_weight = 1f;
            customItem4.ItemDrop.m_itemData.m_shared.m_icons[0] = AssetUtils.LoadSpriteFromFile("Alh4.png");
            ItemManager.Instance.AddItem(customItem4);
            GameObject gameObject5 = PrefabManager.Instance.CreateClonedPrefab("Tomato2", "LeatherScraps");
            CustomItem customItem5 = new CustomItem(gameObject5, false, new ItemConfig
            {
                Name = "Измельченные семяна помидора",
                Description = "Семяна помидора, что вы измельчили превратились в порошок"
            });
            customItem5.ItemDrop.m_itemData.m_shared.m_maxStackSize = 30;
            customItem5.ItemDrop.m_itemData.m_shared.m_weight = 1f;
            customItem5.ItemDrop.m_itemData.m_shared.m_icons[0] = AssetUtils.LoadSpriteFromFile("Alh5.png");
            ItemManager.Instance.AddItem(customItem5);
            GameObject gameObject6 = PrefabManager.Instance.CreateClonedPrefab("Rice2", "LeatherScraps");
            CustomItem customItem6 = new CustomItem(gameObject6, false, new ItemConfig
            {
                Name = "Измельченные семяна тускароры",
                Description = "Семяна тускароры, что вы измельчили превратились в порошок"
            });
            customItem6.ItemDrop.m_itemData.m_shared.m_maxStackSize = 30;
            customItem6.ItemDrop.m_itemData.m_shared.m_weight = 1f;
            customItem6.ItemDrop.m_itemData.m_shared.m_icons[0] = AssetUtils.LoadSpriteFromFile("Alh6.png");
            ItemManager.Instance.AddItem(customItem6);
            GameObject gameObject7 = PrefabManager.Instance.CreateClonedPrefab("Chert2", "LeatherScraps");
            CustomItem customItem7 = new CustomItem(gameObject7, false, new ItemConfig
            {
                Name = "Измельченный чертополох",
                Description = "Чертополох, что вы измельчили превратился в порошок"
            });
            customItem7.ItemDrop.m_itemData.m_shared.m_maxStackSize = 30;
            customItem7.ItemDrop.m_itemData.m_shared.m_weight = 1f;
            customItem7.ItemDrop.m_itemData.m_shared.m_icons[0] = AssetUtils.LoadSpriteFromFile("Alh7.png");
            ItemManager.Instance.AddItem(customItem7);
            GameObject gameObject8 = PrefabManager.Instance.CreateClonedPrefab("Garlik3", "LeatherScraps");
            CustomItem customItem8 = new CustomItem(gameObject8, false, new ItemConfig
            {
                Name = "Чесночный порошок",
                Description = "Порошек, что вы получили путем запекания измельченного чеснока"
            });
            customItem8.ItemDrop.m_itemData.m_shared.m_maxStackSize = 30;
            customItem8.ItemDrop.m_itemData.m_shared.m_weight = 1f;
            customItem8.ItemDrop.m_itemData.m_shared.m_icons[0] = AssetUtils.LoadSpriteFromFile("Alhim1.png");
            ItemManager.Instance.AddItem(customItem8);
            GameObject gameObject9 = PrefabManager.Instance.CreateClonedPrefab("Potato3", "LeatherScraps");
            CustomItem customItem9 = new CustomItem(gameObject9, false, new ItemConfig
            {
                Name = "Крахмал",
                Description = "Порошек, что вы получили путем выпаривания мякоти картофеля"
            });
            customItem9.ItemDrop.m_itemData.m_shared.m_maxStackSize = 30;
            customItem9.ItemDrop.m_itemData.m_shared.m_weight = 1f;
            customItem9.ItemDrop.m_itemData.m_shared.m_icons[0] = AssetUtils.LoadSpriteFromFile("Alhim2.png");
            ItemManager.Instance.AddItem(customItem9);
            GameObject gameObject10 = PrefabManager.Instance.CreateClonedPrefab("Peretz3", "LeatherScraps");
            CustomItem customItem10 = new CustomItem(gameObject10, false, new ItemConfig
            {
                Name = "Вытяжка из огнефрукта",
                Description = "Вытяжка огнефрукта, что вы получили путем приготовления измельченных семян в котле"
            });
            customItem10.ItemDrop.m_itemData.m_shared.m_maxStackSize = 30;
            customItem10.ItemDrop.m_itemData.m_shared.m_weight = 1f;
            customItem10.ItemDrop.m_itemData.m_shared.m_icons[0] = AssetUtils.LoadSpriteFromFile("Alhim3.png");
            ItemManager.Instance.AddItem(customItem10);
            GameObject gameObject11 = PrefabManager.Instance.CreateClonedPrefab("Pumkin3", "LeatherScraps");
            CustomItem customItem11 = new CustomItem(gameObject11, false, new ItemConfig
            {
                Name = "Вытяжка из тыквы",
                Description = "Вытяжка из тыквы, что вы получили путем приготовления измельченных семян в котле"
            });
            customItem11.ItemDrop.m_itemData.m_shared.m_maxStackSize = 30;
            customItem11.ItemDrop.m_itemData.m_shared.m_weight = 1f;
            customItem11.ItemDrop.m_itemData.m_shared.m_icons[0] = AssetUtils.LoadSpriteFromFile("Alhim4.png");
            ItemManager.Instance.AddItem(customItem11);
            GameObject gameObject12 = PrefabManager.Instance.CreateClonedPrefab("Tomato3", "LeatherScraps");
            CustomItem customItem12 = new CustomItem(gameObject12, false, new ItemConfig
            {
                Name = "Вытяжка из помидора",
                Description = "Вытяжка из помидора, что вы получили путем приготовления измельченных семян в котле"
            });
            customItem12.ItemDrop.m_itemData.m_shared.m_maxStackSize = 30;
            customItem12.ItemDrop.m_itemData.m_shared.m_weight = 1f;
            customItem12.ItemDrop.m_itemData.m_shared.m_icons[0] = AssetUtils.LoadSpriteFromFile("Alhim5.png");
            ItemManager.Instance.AddItem(customItem12);
            GameObject gameObject13 = PrefabManager.Instance.CreateClonedPrefab("Rice3", "LeatherScraps");
            CustomItem customItem13 = new CustomItem(gameObject13, false, new ItemConfig
            {
                Name = "Вытяжка из тускароры",
                Description = "Вытяжка из тускароры, что вы получили путем приготовления измельченных семян в котле"
            });
            customItem13.ItemDrop.m_itemData.m_shared.m_maxStackSize = 30;
            customItem13.ItemDrop.m_itemData.m_shared.m_weight = 1f;
            customItem13.ItemDrop.m_itemData.m_shared.m_icons[0] = AssetUtils.LoadSpriteFromFile("Alhim6.png");
            ItemManager.Instance.AddItem(customItem13);
            GameObject gameObject14 = PrefabManager.Instance.CreateClonedPrefab("CrystalOre", "Crystal");
            CustomItem customItem14 = new CustomItem(gameObject14, false, new ItemConfig
            {
                Name = "Осколок вечного льда",
                Description = "Кристал, что вы смогли добыть в землях дальнего севера. Чтобы его растопить потребуется очень мощный источник тепла"
            });
            customItem14.ItemDrop.m_itemData.m_shared.m_teleportable = false;
            customItem14.ItemDrop.m_itemData.m_shared.m_maxStackSize = 30;
            customItem14.ItemDrop.m_itemData.m_shared.m_weight = 8f;
            customItem14.ItemDrop.m_itemData.m_shared.m_icons[0] = AssetUtils.LoadSpriteFromFile("Crystal.png");
            ItemManager.Instance.AddItem(customItem14);
            GameObject gameObject15 = PrefabManager.Instance.CreateClonedPrefab("CrystalOre2", "Crystal");
            CustomItem customItem15 = new CustomItem(gameObject15, false, new ItemConfig
            {
                Name = "Кристал вечного льда",
                Description = "Кристал, что вы получили путем разрушения его оболочки"
            });
            customItem15.ItemDrop.m_itemData.m_shared.m_teleportable = false;
            customItem15.ItemDrop.m_itemData.m_shared.m_maxStackSize = 30;
            customItem15.ItemDrop.m_itemData.m_shared.m_weight = 8f;
            customItem15.ItemDrop.m_itemData.m_shared.m_icons[0] = AssetUtils.LoadSpriteFromFile("Crystal2.png");
            ItemManager.Instance.AddItem(customItem15);
            GameObject gameObject16 = PrefabManager.Instance.CreateClonedPrefab("FlameOre", "FlametalOre");
            CustomItem customItem16 = new CustomItem(gameObject16, false, new ItemConfig
            {
                Name = "Осколок вечного пламяни",
                Description = "Кристал, что вы смогли получить путем обработки металла в печи."
            });
            customItem16.ItemDrop.m_itemData.m_shared.m_teleportable = false;
            customItem16.ItemDrop.m_itemData.m_shared.m_maxStackSize = 30;
            customItem16.ItemDrop.m_itemData.m_shared.m_weight = 8f;
            customItem16.ItemDrop.m_itemData.m_shared.m_icons[0] = AssetUtils.LoadSpriteFromFile("Crystal3.png");
            ItemManager.Instance.AddItem(customItem16);
            GameObject prefab = PrefabManager.Instance.GetPrefab("MineRock_Meteorite");
            MineRock component = prefab.GetComponent<MineRock>();
            component.m_health = 400f;
            MineRock5 prefab2 = PrefabManager.Cache.GetPrefab<MineRock5>("ice_rock1_frac");
            MineRock component2 = prefab.GetComponent<MineRock>();
            component2.m_health = 300f;
            List<DropTable.DropData> list = new List<DropTable.DropData>();
            DropTable.DropData item = default(DropTable.DropData);
            item.m_item = gameObject14;
            item.m_stackMax = 1;
            item.m_stackMin = 1;
            item.m_weight = 1f;
            list.Add(item);
            List<DropTable.DropData> drops = list;
            prefab2.m_dropItems.m_drops = drops;
            prefab2.m_damageModifiers.m_blunt = 0;
            prefab2.m_damageModifiers.m_slash = (HitData.DamageModifier)3;
            prefab2.m_damageModifiers.m_pierce = (HitData.DamageModifier)3;
            prefab2.m_damageModifiers.m_lightning = 0;
            prefab2.m_minToolTier = 2;
            prefab2.m_dropItems.m_dropMin = 1;
            prefab2.m_dropItems.m_dropMax = 1;
            prefab2.m_dropItems.m_dropChance = 0.5f;
            IncineratorConversionConfig incineratorConversionConfig = new IncineratorConversionConfig();
            incineratorConversionConfig.Requirements.Add(new IncineratorRequirementConfig("CrystalOre", 1));
            incineratorConversionConfig.ToItem = "CrystalOre2";
            incineratorConversionConfig.Station = Incinerators.Incinerator;
            incineratorConversionConfig.ProducedItems = 1;
            incineratorConversionConfig.RequireOnlyOneIngredient = false;
            incineratorConversionConfig.Priority = 5;
            ItemManager.Instance.AddItemConversion(new CustomItemConversion(incineratorConversionConfig));
            SmelterConversionConfig smelterConversionConfig = new SmelterConversionConfig();
            smelterConversionConfig.FromItem = "Flametal";
            smelterConversionConfig.ToItem = "FlameOre";
            smelterConversionConfig.Station = Smelters.Smelter;
            ItemManager.Instance.AddItemConversion(new CustomItemConversion(smelterConversionConfig));
            SmelterConversionConfig smelterConversionConfig2 = new SmelterConversionConfig();
            smelterConversionConfig2.FromItem = "garlic";
            smelterConversionConfig2.ToItem = "Garlik2";
            smelterConversionConfig2.Station = Smelters.Windmill;
            ItemManager.Instance.AddItemConversion(new CustomItemConversion(smelterConversionConfig2));
            SmelterConversionConfig smelterConversionConfig3 = new SmelterConversionConfig();
            smelterConversionConfig3.FromItem = "potato";
            smelterConversionConfig3.ToItem = "Potato2";
            smelterConversionConfig3.Station = Smelters.Windmill;
            ItemManager.Instance.AddItemConversion(new CustomItemConversion(smelterConversionConfig3));
            SmelterConversionConfig smelterConversionConfig4 = new SmelterConversionConfig();
            smelterConversionConfig4.FromItem = "PepperSeeds";
            smelterConversionConfig4.ToItem = "Peretz2";
            smelterConversionConfig4.Station = Smelters.Windmill;
            ItemManager.Instance.AddItemConversion(new CustomItemConversion(smelterConversionConfig4));
            SmelterConversionConfig smelterConversionConfig5 = new SmelterConversionConfig();
            smelterConversionConfig5.FromItem = "PumpkinSeeds";
            smelterConversionConfig5.ToItem = "Pumkin2";
            smelterConversionConfig5.Station = Smelters.Windmill;
            ItemManager.Instance.AddItemConversion(new CustomItemConversion(smelterConversionConfig5));
            SmelterConversionConfig smelterConversionConfig6 = new SmelterConversionConfig();
            smelterConversionConfig6.FromItem = "tomato";
            smelterConversionConfig6.ToItem = "Tomato2";
            smelterConversionConfig6.Station = Smelters.Windmill;
            ItemManager.Instance.AddItemConversion(new CustomItemConversion(smelterConversionConfig6));
            SmelterConversionConfig smelterConversionConfig7 = new SmelterConversionConfig();
            smelterConversionConfig7.FromItem = "rice";
            smelterConversionConfig7.ToItem = "Rice2";
            smelterConversionConfig7.Station = Smelters.Windmill;
            ItemManager.Instance.AddItemConversion(new CustomItemConversion(smelterConversionConfig7));
            SmelterConversionConfig smelterConversionConfig8 = new SmelterConversionConfig();
            smelterConversionConfig8.FromItem = "Thistle";
            smelterConversionConfig8.ToItem = "Chert2";
            smelterConversionConfig8.Station = Smelters.Windmill;
            ItemManager.Instance.AddItemConversion(new CustomItemConversion(smelterConversionConfig8));
            PrefabManager.OnVanillaPrefabsAvailable -= this.CustomOre;
            RecipeConfig recipeConfig = new RecipeConfig();
            recipeConfig.Item = "Garlik3";
            recipeConfig.AddRequirement(new RequirementConfig("Garlik2", 1, 0, false));
            recipeConfig.AddRequirement(new RequirementConfig("Wood", 1, 0, false));
            recipeConfig.CraftingStation = CraftingStations.Cauldron;
            ItemManager.Instance.AddRecipe(new CustomRecipe(recipeConfig));
            RecipeConfig recipeConfig2 = new RecipeConfig();
            recipeConfig2.Item = "Potato3";
            recipeConfig2.AddRequirement(new RequirementConfig("Potato2", 1, 0, false));
            recipeConfig2.AddRequirement(new RequirementConfig("Wood", 1, 0, false));
            recipeConfig2.CraftingStation = CraftingStations.Cauldron;
            ItemManager.Instance.AddRecipe(new CustomRecipe(recipeConfig2));
            RecipeConfig recipeConfig3 = new RecipeConfig();
            recipeConfig3.Item = "Peretz3";
            recipeConfig3.AddRequirement(new RequirementConfig("Peretz2", 1, 0, false));
            recipeConfig3.AddRequirement(new RequirementConfig("Wood", 1, 0, false));
            recipeConfig3.CraftingStation = CraftingStations.Cauldron;
            ItemManager.Instance.AddRecipe(new CustomRecipe(recipeConfig3));
            RecipeConfig recipeConfig4 = new RecipeConfig();
            recipeConfig4.Item = "Pumkin3";
            recipeConfig4.AddRequirement(new RequirementConfig("Pumkin2", 1, 0, false));
            recipeConfig4.AddRequirement(new RequirementConfig("Wood", 1, 0, false));
            recipeConfig4.CraftingStation = CraftingStations.Cauldron;
            ItemManager.Instance.AddRecipe(new CustomRecipe(recipeConfig4));
            RecipeConfig recipeConfig5 = new RecipeConfig();
            recipeConfig5.Item = "Tomato3";
            recipeConfig5.AddRequirement(new RequirementConfig("Tomato2", 1, 0, false));
            recipeConfig5.AddRequirement(new RequirementConfig("Wood", 1, 0, false));
            recipeConfig5.CraftingStation = CraftingStations.Cauldron;
            ItemManager.Instance.AddRecipe(new CustomRecipe(recipeConfig5));
            RecipeConfig recipeConfig6 = new RecipeConfig();
            recipeConfig6.Item = "Rice3";
            recipeConfig6.AddRequirement(new RequirementConfig("Rice2", 1, 0, false));
            recipeConfig6.AddRequirement(new RequirementConfig("Wood", 1, 0, false));
            recipeConfig6.CraftingStation = CraftingStations.Cauldron;
            ItemManager.Instance.AddRecipe(new CustomRecipe(recipeConfig6));
            PrefabManager.OnVanillaPrefabsAvailable -= this.CustomOre;
        }
        */

        // Token: 0x06000008 RID: 8 RVA: 0x00005670 File Offset: 0x00003870
        public void CustomArmor()
        {
            GameObject gameObject = (GameObject)this.eikthyr.LoadAsset("ArmorEikthyrChest");
            GameObject gameObject2 = (GameObject)this.eikthyr.LoadAsset("HelmetEikthyr");
            GameObject gameObject3 = (GameObject)this.eikthyr.LoadAsset("ArmorEikthyrLegs");
            CustomItem customItem = new CustomItem(gameObject, false);
            CustomItem customItem2 = new CustomItem(gameObject2, false);
            CustomItem customItem3 = new CustomItem(gameObject3, false);
            ItemManager.Instance.AddItem(customItem);
            ItemManager.Instance.AddItem(customItem2);
            ItemManager.Instance.AddItem(customItem3);
            GameObject gameObject4 = (GameObject)this.platearmor.LoadAsset("ArmorPlateIronHelmetJD");
            GameObject gameObject5 = (GameObject)this.platearmor.LoadAsset("ArmorPlateIronChestJD");
            GameObject gameObject6 = (GameObject)this.platearmor.LoadAsset("ArmorPlateIronLegsJD");
            CustomItem customItem4 = new CustomItem(gameObject4, false);
            CustomItem customItem5 = new CustomItem(gameObject5, false);
            CustomItem customItem6 = new CustomItem(gameObject6, false);
            ItemManager.Instance.AddItem(customItem4);
            ItemManager.Instance.AddItem(customItem5);
            ItemManager.Instance.AddItem(customItem6);
            PrefabManager.OnVanillaPrefabsAvailable -= this.CustomArmor;
        }

        // Token: 0x06000009 RID: 9 RVA: 0x0000579C File Offset: 0x0000399C
        public void ConfigShip()
        {
            ConfigurationManagerAttributes configurationManagerAttributes = new ConfigurationManagerAttributes
            {
                IsAdminOnly = true
            };
            Main.modEnabled = base.Config.Bind<bool>("General", "Enabled", true, new ConfigDescription("Enable this mod", null, new object[]
            {
                configurationManagerAttributes
            }));
            Main.karveChestWidth = base.Config.Bind<int>("Sizes", "KarveChestWidth", 2, new ConfigDescription("Number of items wide for Karve chest containers (max. 8)", null, new object[]
            {
                configurationManagerAttributes
            }));
            Main.karveChestHeight = base.Config.Bind<int>("Sizes", "KarveChestHeight", 2, new ConfigDescription("Number of items tall for karve chest containers", null, new object[]
            {
                configurationManagerAttributes
            }));
            Main.vikingShipChestWidth = base.Config.Bind<int>("Sizes", "VikingShipChestWidth", 6, new ConfigDescription("Number of items wide for longship chest containers (max. 8)", null, new object[]
            {
                configurationManagerAttributes
            }));
            Main.vikingShipChestHeight = base.Config.Bind<int>("Sizes", "VikingShipChestHeight", 3, new ConfigDescription("Number of items tall for longship chest containers", null, new object[]
            {
                configurationManagerAttributes
            }));
            Main.wagonWidth = base.Config.Bind<int>("Sizes", "WagonWidth", 6, new ConfigDescription("Number of items wide for chest containers (max. 8)", null, new object[]
            {
                configurationManagerAttributes
            }));
            Main.wagonHeight = base.Config.Bind<int>("Sizes", "WagonHeight", 3, new ConfigDescription("Number of items tall for chest containers", null, new object[]
            {
                configurationManagerAttributes
            }));
            Main.rae_horsecartWidth = base.Config.Bind<int>("Sizes", "rae_horsecartWidth", 3, new ConfigDescription("HorsecartWidth", null, new object[]
            {
                configurationManagerAttributes
            }));
            Main.rae_horsecartHeight = base.Config.Bind<int>("Sizes", "rae_horsecartHeight", 3, new ConfigDescription("HorsecartHeight", null, new object[]
            {
                configurationManagerAttributes
            }));
            Main.cargoshipplusWidth = base.Config.Bind<int>("Sizes", "cargoshipplusWidth", 3, new ConfigDescription("cargoshipplusWidth", null, new object[]
            {
                configurationManagerAttributes
            }));
            Main.cargoshipplusHeight = base.Config.Bind<int>("Sizes", "cargoshipplusHeight", 3, new ConfigDescription("cargoshippluseHeight", null, new object[]
            {
                configurationManagerAttributes
            }));
            Main.mercantshipWidth = base.Config.Bind<int>("Sizes", "mercantshipWidth", 3, new ConfigDescription("mercantshipWidth", null, new object[]
            {
                configurationManagerAttributes
            }));
            Main.mercantshipHeight = base.Config.Bind<int>("Sizes", "mercantshipHeight", 3, new ConfigDescription("mercantshipHeight", null, new object[]
            {
                configurationManagerAttributes
            }));
            Main.bigcargoshipplusWidth = base.Config.Bind<int>("Sizes", "bigcargoshipplusWidth", 3, new ConfigDescription("bigcargoshipplusWidth", null, new object[]
            {
                configurationManagerAttributes
            }));
            Main.bigcargoshipplusHeight = base.Config.Bind<int>("Sizes", "bigcargoshipplusHeight", 3, new ConfigDescription("bigcargoshipplusHeight", null, new object[]
            {
                configurationManagerAttributes
            }));
            Main.skuldelevWidth = base.Config.Bind<int>("Sizes", "skuldelevWidth", 3, new ConfigDescription("skuldelevWidth", null, new object[]
            {
                configurationManagerAttributes
            }));
            Main.skuldelevHeight = base.Config.Bind<int>("Sizes", "skuldelevHeight", 3, new ConfigDescription("skuldelevHeigh", null, new object[]
            {
                configurationManagerAttributes
            }));
            Main.warshipWidth = base.Config.Bind<int>("Sizes", "warshipWidth", 3, new ConfigDescription("warshipWidth", null, new object[]
            {
                configurationManagerAttributes
            }));
            Main.warshipHeight = base.Config.Bind<int>("Sizes", "warshipHeight", 3, new ConfigDescription("warshipHeight", null, new object[]
            {
                configurationManagerAttributes
            }));
            Main.herculeshipWidth = base.Config.Bind<int>("Sizes", "herculeshipWidth", 3, new ConfigDescription("herculeshipWidth", null, new object[]
            {
                configurationManagerAttributes
            }));
            Main.herculeshipHeight = base.Config.Bind<int>("Sizes", "herculeshippHeight", 3, new ConfigDescription("herculeshipHeight", null, new object[]
            {
                configurationManagerAttributes
            }));
            Main.goblinshipWidth = base.Config.Bind<int>("Sizes", "goblinshipWidth", 3, new ConfigDescription("goblinshipWidth", null, new object[]
            {
                configurationManagerAttributes
            }));
            Main.goblinshipHeight = base.Config.Bind<int>("Sizes", "goblinshipHeight", 3, new ConfigDescription("goblinshipHeight", null, new object[]
            {
                configurationManagerAttributes
            }));
            Main.fastshipskuldelevWidth = base.Config.Bind<int>("Sizes", "fastshipskuldelevWidth", 3, new ConfigDescription("fastshipskuldelevWidth", null, new object[]
            {
                configurationManagerAttributes
            }));
            Main.fastshipskuldelevHeight = base.Config.Bind<int>("Sizes", "fastshipskuldelevHeight", 3, new ConfigDescription("fastshipskuldelevHeight", null, new object[]
            {
                configurationManagerAttributes
            }));
            Main.cargocaravelWidth = base.Config.Bind<int>("Sizes", "cargocaravelWidth", 3, new ConfigDescription("cargocaravelWidth", null, new object[]
            {
                configurationManagerAttributes
            }));
            Main.cargocaravelHeight = base.Config.Bind<int>("Sizes", "cargocaravelHeight", 3, new ConfigDescription("cargocaravelHeight", null, new object[]
            {
                configurationManagerAttributes
            }));
            Main.hugecargoshipWidth = base.Config.Bind<int>("Sizes", "hugecargoshipWidth", 3, new ConfigDescription("hugecargoshipWidth", null, new object[]
            {
                configurationManagerAttributes
            }));
            Main.hugecargoshipHeight = base.Config.Bind<int>("Sizes", "hugecargoshipHeight", 3, new ConfigDescription("hugecargoshipHeight", null, new object[]
            {
                configurationManagerAttributes
            }));
            bool value = Main.modEnabled.Value;
            if (value)
            {
                Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly(), null);
            }
        }

        // Token: 0x04000001 RID: 1
        public const string PluginGUID = "GrayDwarfMod";

        // Token: 0x04000002 RID: 2
        public const string PluginName = "GDMod";

        // Token: 0x04000003 RID: 3
        public const string PluginVersion = "2.0.0";

        // Token: 0x04000004 RID: 4
        public static Harmony harmony = new Harmony("GrayDwarfMod");

        // Token: 0x04000005 RID: 5
        public AssetBundle items = AssetUtils.LoadAssetBundleFromResources("questitems");

        // Token: 0x04000006 RID: 6
        public AssetBundle fantasy = AssetUtils.LoadAssetBundleFromResources("FantasyArmoury", Assembly.GetExecutingAssembly());

        // Token: 0x04000007 RID: 7
        public AssetBundle henrik = AssetUtils.LoadAssetBundleFromResources("henrikstests", Assembly.GetExecutingAssembly());

        // Token: 0x04000008 RID: 8
        public AssetBundle magicbow = AssetUtils.LoadAssetBundleFromResources("MagicBows", Assembly.GetExecutingAssembly());

        // Token: 0x04000009 RID: 9
        public AssetBundle eikthyr = AssetUtils.LoadAssetBundleFromResources("eikthyrarmorset", Assembly.GetExecutingAssembly());

        // Token: 0x0400000A RID: 10
        public AssetBundle platearmor = AssetUtils.LoadAssetBundleFromResources("platearmor", Assembly.GetExecutingAssembly());

        // Token: 0x0400000B RID: 11
        public Sprite TestSprite;

        // Token: 0x0400000C RID: 12
        public Sprite TestSprite2;

        // Token: 0x0400000D RID: 13
        public Sprite CoinsS;

        // Token: 0x0400000E RID: 14
        public static ConfigEntry<bool> modEnabled;

        // Token: 0x0400000F RID: 15
        public static ConfigEntry<int> karveChestWidth;

        // Token: 0x04000010 RID: 16
        public static ConfigEntry<int> karveChestHeight;

        // Token: 0x04000011 RID: 17
        public static ConfigEntry<int> vikingShipChestWidth;

        // Token: 0x04000012 RID: 18
        public static ConfigEntry<int> vikingShipChestHeight;

        // Token: 0x04000013 RID: 19
        public static ConfigEntry<int> wagonWidth;

        // Token: 0x04000014 RID: 20
        public static ConfigEntry<int> wagonHeight;

        // Token: 0x04000015 RID: 21
        public static ConfigEntry<int> rae_horsecartWidth;

        // Token: 0x04000016 RID: 22
        public static ConfigEntry<int> rae_horsecartHeight;

        // Token: 0x04000017 RID: 23
        public static ConfigEntry<int> cargoshipplusWidth;

        // Token: 0x04000018 RID: 24
        public static ConfigEntry<int> cargoshipplusHeight;

        // Token: 0x04000019 RID: 25
        public static ConfigEntry<int> mercantshipWidth;

        // Token: 0x0400001A RID: 26
        public static ConfigEntry<int> mercantshipHeight;

        // Token: 0x0400001B RID: 27
        public static ConfigEntry<int> bigcargoshipplusWidth;

        // Token: 0x0400001C RID: 28
        public static ConfigEntry<int> bigcargoshipplusHeight;

        // Token: 0x0400001D RID: 29
        public static ConfigEntry<int> skuldelevWidth;

        // Token: 0x0400001E RID: 30
        public static ConfigEntry<int> skuldelevHeight;

        // Token: 0x0400001F RID: 31
        public static ConfigEntry<int> warshipWidth;

        // Token: 0x04000020 RID: 32
        public static ConfigEntry<int> warshipHeight;

        // Token: 0x04000021 RID: 33
        public static ConfigEntry<int> herculeshipWidth;

        // Token: 0x04000022 RID: 34
        public static ConfigEntry<int> herculeshipHeight;

        // Token: 0x04000023 RID: 35
        public static ConfigEntry<int> goblinshipWidth;

        // Token: 0x04000024 RID: 36
        public static ConfigEntry<int> goblinshipHeight;

        // Token: 0x04000025 RID: 37
        public static ConfigEntry<int> fastshipskuldelevWidth;

        // Token: 0x04000026 RID: 38
        public static ConfigEntry<int> fastshipskuldelevHeight;

        // Token: 0x04000027 RID: 39
        public static ConfigEntry<int> cargocaravelWidth;

        // Token: 0x04000028 RID: 40
        public static ConfigEntry<int> cargocaravelHeight;

        // Token: 0x04000029 RID: 41
        public static ConfigEntry<int> hugecargoshipWidth;

        // Token: 0x0400002A RID: 42
        public static ConfigEntry<int> hugecargoshipHeight;

        // Token: 0x02000003 RID: 3
        [HarmonyPatch(typeof(Container), "Awake")]
        private static class Container_Awake_Patch
        {
            // Token: 0x0600000C RID: 12 RVA: 0x00005DE4 File Offset: 0x00003FE4
            private static void Postfix(Container __instance, Inventory ___m_inventory)
            {
                bool flag = ___m_inventory == null;
                bool flag2 = !flag;
                if (flag2)
                {
                    Transform parent = __instance.gameObject.transform.parent;
                    Ship ship = (parent != null) ? parent.GetComponent<Ship>() : null;
                    bool flag3 = ship != null;
                    bool flag4 = flag3;
                    if (flag4)
                    {
                        bool flag5 = ship.name.ToLower().Contains("karve");
                        bool flag6 = flag5;
                        if (flag6)
                        {
                            typeof(Inventory).GetField("m_width", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(___m_inventory, Main.karveChestWidth.Value);
                            typeof(Inventory).GetField("m_height", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(___m_inventory, Main.karveChestHeight.Value);
                        }
                        bool flag7 = ship.name.ToLower().Contains("vikingship");
                        bool flag8 = flag7;
                        if (flag8)
                        {
                            typeof(Inventory).GetField("m_width", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(___m_inventory, Main.vikingShipChestWidth.Value);
                            typeof(Inventory).GetField("m_height", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(___m_inventory, Main.vikingShipChestHeight.Value);
                        }
                        bool flag9 = ship.name.ToLower().Contains("cargoshipplus");
                        bool flag10 = flag9;
                        if (flag10)
                        {
                            typeof(Inventory).GetField("m_width", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(___m_inventory, Main.cargoshipplusWidth.Value);
                            typeof(Inventory).GetField("m_height", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(___m_inventory, Main.cargoshipplusHeight.Value);
                        }
                        bool flag11 = ship.name.ToLower().Contains("mercantship");
                        bool flag12 = flag11;
                        if (flag12)
                        {
                            typeof(Inventory).GetField("m_width", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(___m_inventory, Main.mercantshipWidth.Value);
                            typeof(Inventory).GetField("m_height", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(___m_inventory, Main.mercantshipHeight.Value);
                        }
                        bool flag13 = ship.name.ToLower().Contains("bigcargoshipplus");
                        bool flag14 = flag13;
                        if (flag14)
                        {
                            typeof(Inventory).GetField("m_width", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(___m_inventory, Main.bigcargoshipplusWidth.Value);
                            typeof(Inventory).GetField("m_height", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(___m_inventory, Main.bigcargoshipplusHeight.Value);
                        }
                        bool flag15 = ship.name.ToLower().Contains("skuldelev");
                        bool flag16 = flag15;
                        if (flag16)
                        {
                            typeof(Inventory).GetField("m_width", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(___m_inventory, Main.skuldelevWidth.Value);
                            typeof(Inventory).GetField("m_height", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(___m_inventory, Main.skuldelevHeight.Value);
                        }
                        bool flag17 = ship.name.ToLower().Contains("warship");
                        bool flag18 = flag17;
                        if (flag18)
                        {
                            typeof(Inventory).GetField("m_width", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(___m_inventory, Main.warshipWidth.Value);
                            typeof(Inventory).GetField("m_height", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(___m_inventory, Main.warshipHeight.Value);
                        }
                        bool flag19 = ship.name.ToLower().Contains("herculeship");
                        bool flag20 = flag19;
                        if (flag20)
                        {
                            typeof(Inventory).GetField("m_width", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(___m_inventory, Main.herculeshipWidth.Value);
                            typeof(Inventory).GetField("m_height", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(___m_inventory, Main.herculeshipHeight.Value);
                        }
                        bool flag21 = ship.name.ToLower().Contains("goblinship");
                        bool flag22 = flag21;
                        if (flag22)
                        {
                            typeof(Inventory).GetField("m_width", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(___m_inventory, Main.goblinshipWidth.Value);
                            typeof(Inventory).GetField("m_height", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(___m_inventory, Main.goblinshipHeight.Value);
                        }
                        bool flag23 = ship.name.ToLower().Contains("fastshipskuldelev");
                        bool flag24 = flag23;
                        if (flag24)
                        {
                            typeof(Inventory).GetField("m_width", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(___m_inventory, Main.fastshipskuldelevWidth.Value);
                            typeof(Inventory).GetField("m_height", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(___m_inventory, Main.fastshipskuldelevHeight.Value);
                        }
                        bool flag25 = ship.name.ToLower().Contains("hugecargoship");
                        bool flag26 = flag25;
                        if (flag26)
                        {
                            typeof(Inventory).GetField("m_width", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(___m_inventory, Main.hugecargoshipWidth.Value);
                            typeof(Inventory).GetField("m_height", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(___m_inventory, Main.hugecargoshipHeight.Value);
                        }
                        bool flag27 = ship.name.ToLower().Contains("cargocaravel");
                        bool flag28 = flag27;
                        if (flag28)
                        {
                            typeof(Inventory).GetField("m_width", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(___m_inventory, Main.cargocaravelWidth.Value);
                            typeof(Inventory).GetField("m_height", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(___m_inventory, Main.cargocaravelHeight.Value);
                        }
                    }
                    Vagon vagon = (parent != null) ? parent.GetComponent<Vagon>() : null;
                    bool flag29 = vagon != null;
                    bool flag30 = flag29;
                    if (flag30)
                    {
                        bool flag31 = vagon.name.ToLower().Contains("cart");
                        bool flag32 = flag31;
                        if (flag32)
                        {
                            typeof(Inventory).GetField("m_width", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(___m_inventory, Main.wagonWidth.Value);
                            typeof(Inventory).GetField("m_height", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(___m_inventory, Main.wagonHeight.Value);
                        }
                        bool flag33 = vagon.name.ToLower().Contains("rae_horsecart");
                        bool flag34 = flag33;
                        if (flag34)
                        {
                            typeof(Inventory).GetField("m_width", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(___m_inventory, Main.rae_horsecartWidth.Value);
                            typeof(Inventory).GetField("m_height", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(___m_inventory, Main.rae_horsecartHeight.Value);
                        }
                    }
                }
            }
        }
        private void Economy()
        {
            GameObject gameObject = PrefabManager.Instance.CreateClonedPrefab("GDCoin", "Ruby");
            gameObject.GetComponent<Renderer>().material.color = new Color(238, 130, 238);
            CustomItem customItem = new CustomItem(gameObject, false, new ItemConfig
            {
                Name = "Роскошь",
                Description = "Викингам известна роскошь. Любой торговец отдаст любые сбережения, чтобы заполучить её."
            });
            customItem.ItemDrop.m_itemData.m_shared.m_maxStackSize = 100000;
            customItem.ItemDrop.m_itemData.m_shared.m_weight = 0f;
            customItem.ItemDrop.m_itemData.m_shared.m_value = 10;
            customItem.ItemDrop.m_itemData.m_shared.m_icons[0] = AssetUtils.LoadSpriteFromFile("prem.png");
            ItemManager.Instance.AddItem(customItem);

            GameObject gameObject2 = PrefabManager.Instance.CreateClonedPrefab("Wood2", "Wood");
            CustomItem customItem2 = new CustomItem(gameObject2, false, new ItemConfig
            {
                Name = "Береста",
                Description = "Кора дерева, что вы содрали"
            });
            customItem2.ItemDrop.m_itemData.m_shared.m_maxStackSize = 50;
            customItem2.ItemDrop.m_itemData.m_shared.m_weight = 1f;
            customItem2.ItemDrop.m_itemData.m_shared.m_icons[0] = AssetUtils.LoadSpriteFromFile("wood1.png");
            ItemManager.Instance.AddItem(customItem2);

            RecipeConfig recipeConfig = new RecipeConfig();
            recipeConfig.Item = "Wood2";
            recipeConfig.AddRequirement(new RequirementConfig("Wood", 2, 0, false));
            recipeConfig.CraftingStation = CraftingStations.Workbench;
            ItemManager.Instance.AddRecipe(new CustomRecipe(recipeConfig));



            GameObject gameObject3 = PrefabManager.Instance.CreateClonedPrefab("Wood3", "Wood");
            CustomItem customItem3 = new CustomItem(gameObject3, false, new ItemConfig
            {
                Name = "Опилки",
                Description = "Мягая на ощупь стужка. Вы измельчили древесную кору."
            });
            customItem3.ItemDrop.m_itemData.m_shared.m_maxStackSize = 50;
            customItem3.ItemDrop.m_itemData.m_shared.m_weight = 1f;
            customItem3.ItemDrop.m_itemData.m_shared.m_icons[0] = AssetUtils.LoadSpriteFromFile("wood2.png");
            ItemManager.Instance.AddItem(customItem3);

            RecipeConfig recipeConfig2 = new RecipeConfig();
            recipeConfig2.Item = "Wood3";
            recipeConfig2.AddRequirement(new RequirementConfig("Wood2", 2, 0, false));
            recipeConfig2.CraftingStation = CraftingStations.Workbench;
            ItemManager.Instance.AddRecipe(new CustomRecipe(recipeConfig2));



            GameObject gameObject4 = PrefabManager.Instance.CreateClonedPrefab("Wood4", "Wood");
            CustomItem customItem4 = new CustomItem(gameObject4, false, new ItemConfig
            {
                Name = "Грубый лист",
                Description = "Лист, что вы получили путем высушивания и сдавливания опилок"
            });
            customItem4.ItemDrop.m_itemData.m_shared.m_maxStackSize = 50;
            customItem4.ItemDrop.m_itemData.m_shared.m_weight = 1f;
            customItem4.ItemDrop.m_itemData.m_shared.m_icons[0] = AssetUtils.LoadSpriteFromFile("wood3.png");
            ItemManager.Instance.AddItem(customItem4);

            RecipeConfig recipeConfig3 = new RecipeConfig();
            recipeConfig3.Item = "Wood4";
            recipeConfig3.AddRequirement(new RequirementConfig("Wood3", 2, 0, false));
            recipeConfig3.CraftingStation = CraftingStations.Workbench;
            ItemManager.Instance.AddRecipe(new CustomRecipe(recipeConfig3));



            GameObject gameObject5 = PrefabManager.Instance.CreateClonedPrefab("Wood5", "Wood");
            CustomItem customItem5 = new CustomItem(gameObject5, false, new ItemConfig
            {
                Name = "Свитки",
                Description = "Путем длительной древообработки вы получили прочный рулон гладкого материала. Думаю за него предложат хорошую цену"
            });
            customItem5.ItemDrop.m_itemData.m_shared.m_maxStackSize = 50;
            customItem5.ItemDrop.m_itemData.m_shared.m_weight = 1f;
            customItem5.ItemDrop.m_itemData.m_shared.m_icons[0] = AssetUtils.LoadSpriteFromFile("wood4.png");
            ItemManager.Instance.AddItem(customItem5);

            RecipeConfig recipeConfig4 = new RecipeConfig();
            recipeConfig4.Item = "Wood5";
            recipeConfig4.AddRequirement(new RequirementConfig("Wood4", 2, 0, false));
            recipeConfig4.CraftingStation = CraftingStations.Workbench;
            ItemManager.Instance.AddRecipe(new CustomRecipe(recipeConfig4));





            GameObject gameObject6 = PrefabManager.Instance.CreateClonedPrefab("Stone2", "Stone");
            CustomItem customItem6 = new CustomItem(gameObject6, false, new ItemConfig
            {
                Name = "Разбитый камень",
                Description = "Лишь попытка найти что-то действительно ценное"
            });
            customItem6.ItemDrop.m_itemData.m_shared.m_maxStackSize = 50;
            customItem6.ItemDrop.m_itemData.m_shared.m_weight = 1f;
            customItem6.ItemDrop.m_itemData.m_shared.m_icons[0] = AssetUtils.LoadSpriteFromFile("stone1.png");
            ItemManager.Instance.AddItem(customItem6);

            RecipeConfig recipeConfig5 = new RecipeConfig();
            recipeConfig5.Item = "Stone2";
            recipeConfig5.AddRequirement(new RequirementConfig("Stone", 2, 0, false));
            recipeConfig5.CraftingStation = CraftingStations.Workbench;
            ItemManager.Instance.AddRecipe(new CustomRecipe(recipeConfig5));



            GameObject gameObject7 = PrefabManager.Instance.CreateClonedPrefab("Stone3", "Stone");
            CustomItem customItem7 = new CustomItem(gameObject7, false, new ItemConfig
            {
                Name = "Гранированный камень",
                Description = "Камень очищенный от ненужных примесей"
            });
            customItem7.ItemDrop.m_itemData.m_shared.m_maxStackSize = 50;
            customItem7.ItemDrop.m_itemData.m_shared.m_weight = 1f;
            customItem7.ItemDrop.m_itemData.m_shared.m_icons[0] = AssetUtils.LoadSpriteFromFile("stone2.png");
            ItemManager.Instance.AddItem(customItem7);

            RecipeConfig recipeConfig6 = new RecipeConfig();
            recipeConfig6.Item = "Stone3";
            recipeConfig6.AddRequirement(new RequirementConfig("Stone2", 2, 0, false));
            recipeConfig6.CraftingStation = CraftingStations.Workbench;
            ItemManager.Instance.AddRecipe(new CustomRecipe(recipeConfig6));



            GameObject gameObject8 = PrefabManager.Instance.CreateClonedPrefab("Stone4", "Stone");
            CustomItem customItem8 = new CustomItem(gameObject8, false, new ItemConfig
            {
                Name = "Безделушка",
                Description = "Этот кусок слегка поблескивает. Может придать ему форму?"
            });
            customItem8.ItemDrop.m_itemData.m_shared.m_maxStackSize = 50;
            customItem8.ItemDrop.m_itemData.m_shared.m_weight = 1f;
            customItem8.ItemDrop.m_itemData.m_shared.m_icons[0] = AssetUtils.LoadSpriteFromFile("stone3.png");
            ItemManager.Instance.AddItem(customItem8);

            RecipeConfig recipeConfig7 = new RecipeConfig();
            recipeConfig7.Item = "Stone4";
            recipeConfig7.AddRequirement(new RequirementConfig("Stone3", 2, 0, false));
            recipeConfig7.CraftingStation = CraftingStations.Workbench;
            ItemManager.Instance.AddRecipe(new CustomRecipe(recipeConfig7));


            GameObject gameObject9 = PrefabManager.Instance.CreateClonedPrefab("Stone5", "Stone");
            CustomItem customItem9 = new CustomItem(gameObject9, false, new ItemConfig
            {
                Name = "Драгоценный камень",
                Description = "Отполированная и очищенная руда превратилась в нечто прекрасное"
            });
            customItem9.ItemDrop.m_itemData.m_shared.m_maxStackSize = 50;
            customItem9.ItemDrop.m_itemData.m_shared.m_weight = 1f;
            customItem9.ItemDrop.m_itemData.m_shared.m_icons[0] = AssetUtils.LoadSpriteFromFile("stone4.png");
            ItemManager.Instance.AddItem(customItem9);

            RecipeConfig recipeConfig8 = new RecipeConfig();
            recipeConfig8.Item = "Stone5";
            recipeConfig8.AddRequirement(new RequirementConfig("Stone4", 2, 0, false));
            recipeConfig8.CraftingStation = CraftingStations.Workbench;
            ItemManager.Instance.AddRecipe(new CustomRecipe(recipeConfig8));
            PrefabManager.OnVanillaPrefabsAvailable -= Economy;
        }
        /*
        [HarmonyPatch(typeof(TreeLog), "Awake")]
        private static class EconomyPatch
        {
            // Token: 0x0600000C RID: 12 RVA: 0x00005DE4 File Offset: 0x00003FE4
            private static void TreeLoot(DropTable m_dropWhenDestroyed)
            {
                List<GameObject> dropList = m_dropWhenDestroyed.GetDropList();
                GameObject go = PrefabManager.Instance.GetPrefab("Stone");
                dropList.Clear();
                dropList.Add(go);
            }
        }
        */
    }
}
