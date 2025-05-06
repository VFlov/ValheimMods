using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using BepInEx;
using HarmonyLib;
using Jotunn.Configs;
using Jotunn.Entities;
using Jotunn.Managers;
using Jotunn.Utils;
using UnityEngine;

namespace PrettyPotion
{
    [BepInPlugin("noname.PrettyPotion", "PrettyPotion", "1.0.0")]

    public class PrettyPotion : BaseUnityPlugin
    {
        void Awake()
        {
            //AddConfiguration(); ADD =========
            PrefabManager.OnVanillaPrefabsAvailable += BeforeStartup;

            //AddLocalizations(); ADD===========
            Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly(), null);
        }
        void BeforeStartup()
        {
            var basisPotion = CreateStatusEffects("BasisPotion", "$basis_effectname", "basis.png");
            AddStatusEffects(basisPotion);
            AddItem("MeadHealthMedium", "BasisPotion", 
                "Basis", "The basis of any potion", 
                new List<(string, int)> { ("Mushroom", 2) }, "basis.png",
                basisPotion);

            var bluePotion = CreateStatusEffects("BluePotion", "$bluepotion_effectname", "blue.png");
            float num = 0.25f;
            bluePotion.StatusEffect.ModifyEitrRegen(ref num);
            AddStatusEffects(bluePotion);
            AddItem("MeadHealthMedium", "BluePotion",
                "Blue Potion", "Blue mushroom tincture",
                new List<(string, int)> { ("MushroomMagecap", 2) }, "blue.png",
                bluePotion);

            var greenPotion = CreateStatusEffects("GreenPotion", "$greenpotion_effectname", "green.png");
            float num0 = 1.25f;
            greenPotion.StatusEffect.ModifyRaiseSkill(Skills.SkillType.All, ref num0);
            AddStatusEffects(greenPotion);
            AddItem("MeadHealthMedium", "GreenPotion",
                "Green Potion", "Decantation potion from vineberry",
                new List<(string, int)> { ("Vineberry", 2) }, "green.png",
                greenPotion);
                
            var orangePotion = CreateStatusEffects("OrangePotion", "$orangepotion_effectname", "orange.png");
            AddStatusEffects(orangePotion);
            AddItem("MeadHealthMedium", "OrangePotion",
                "Orange Potion", "Jotunn puff mushroom tincture",
                new List<(string, int)> { ("MushroomJotunPuffs", 2) }, "orange.png",
                orangePotion);
                
            var pinkPotion = CreateStatusEffects("PinkPotion", "$pinkpotion_effectname", "pink.png");
            AddStatusEffects(pinkPotion);
            AddItem("MeadHealthMedium", "PinkPotion",
                "Pink Potion", "Thistle extract",
                new List<(string, int)> { ("Thistle", 2) }, "pink.png",
                pinkPotion);

            var purplePotion = CreateStatusEffects("PurplePotion", "$purplepotion_effectname", "purple.png");
            float num1 = 5.25f;
            purplePotion.StatusEffect.ModifyStaminaRegen(ref num1);
            AddStatusEffects(purplePotion);
            AddItem("MeadHealthMedium", "PurplePotion",
                "Purple Potion", "Berry broth",
                new List<(string, int)> { ("Raspberry", 2), ("Blueberries", 2) }, "purple.png",
                purplePotion);

            var redPotion = CreateStatusEffects("RedPotion", "$redpotion_effectname", "red.png");
            AddStatusEffects(redPotion);
            AddItem("MeadHealthMedium", "RedPotion",
                "Red Potion", "Bloodbag effusion",
                new List<(string, int)> { ("Bloodbag", 2) }, "red.png",
                redPotion);

            var yellowPotion = CreateStatusEffects("YellowPotion", "$yellowpotion_effectname", "yellow.png");
            float num2 = 5.25f;
            yellowPotion.StatusEffect.ModifyHealthRegen(ref num2);
            AddStatusEffects(yellowPotion);
            AddItem("MeadHealthMedium", "YellowPotion",
                "Yellow Potion", "Yellow mushroom tincture",
                new List<(string, int)> { ("MushroomYellow", 2) }, "yellow.png",
                yellowPotion);

            PrefabManager.OnVanillaPrefabsAvailable -= BeforeStartup;
        }
        void AddItem(string originalItemName, string newItemConsoleName, string itemName, string itemDescription, List<(string, int)> requirements, string iconName, CustomStatusEffect customStatusEffect)
        {
            ItemConfig somePotionConfig = new ItemConfig();
            somePotionConfig.Name = itemName; //"Basis";
            somePotionConfig.Description = itemDescription; // "The basis of any potion";
            somePotionConfig.CraftingStation = CraftingStations.Cauldron;
            
            foreach (var requirement in requirements)
                somePotionConfig.AddRequirement(requirement.Item1, requirement.Item2);
            somePotionConfig.Icon = AssetUtils.LoadSpriteFromFile("./resources/" + iconName);
            CustomItem someItem = new CustomItem(newItemConsoleName, originalItemName, somePotionConfig);
            someItem.ItemDrop.m_itemData.m_shared.m_consumeStatusEffect = customStatusEffect.StatusEffect;
            Jotunn.Managers.ItemManager.Instance.AddItem(someItem);
        }
        private CustomStatusEffect CreateStatusEffects(string name, string m_name, string m_icon)
        {
            /*
             * 1 Гриб - вода = нет потери скилов ??
                2 Желтый гриб - желтый = хил
                3 Гриб етунов - оранжевый = ModifySpeed
                4 Волшебный гриб - синий = ейтр восстановление
                5 Гроздь - зеленый = увеличение получаемого опыта
                6 Чертополох - розовый = Устойчивость к доджу ??
                7 Туша - красный = урон
                8 Малина + черника - фиолетовый = стамина
            */
            StatusEffect basisPotionEffect = ScriptableObject.CreateInstance<StatusEffect>();
            basisPotionEffect.name = name;
            basisPotionEffect.m_name = m_name ;
            basisPotionEffect.m_icon = AssetUtils.LoadSpriteFromFile(m_icon);
            var num = 10f;
            basisPotionEffect.ModifyStaminaRegen(ref num);
            /*
            basisPotionEffect.ModifyHealthRegen();
            basisPotionEffect.ModifyStaminaRegen();
            basisPotionEffect.ModifyEitrRegen();
            basisPotionEffect.ModifyRaiseSkill(Skills.SkillType.All, 1f);
            */
            var BasisPotionEffect = new CustomStatusEffect(basisPotionEffect, fixReference: false);  
            
            return BasisPotionEffect;
        }
        private void AddStatusEffects(CustomStatusEffect customStatusEffect)
        {
            Jotunn.Managers.ItemManager.Instance.AddStatusEffect(customStatusEffect);
        }
    }
    

    
}
