using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;
using BepInEx;
using HarmonyLib;
using Jotunn.Configs;
using Jotunn.Entities;
using Jotunn.Managers;
using Jotunn.Utils;
using SkillManager;
using UnityEngine;
using UnityEngine.UIElements;

namespace PrettyPotion
{
    [BepInPlugin("vaffle.PrettyPotion", "PrettyPotion", "1.0.0")]
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
    public class PrettyPotion : BaseUnityPlugin
    {
        void Awake()
        {
            //AddConfiguration(); ADD =========
            PrefabManager.OnVanillaPrefabsAvailable += SetupPotions;

            //AddLocalizations(); ADD===========
            Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly(), null);
        }
        private void SetupPotions()
        {
            // 1. Basis Potion
            StatusEffect basisPotionEffect = ScriptableObject.CreateInstance<StatusEffect>();
            basisPotionEffect.name = "BasisPotion";
            basisPotionEffect.m_name = "$basis_effectname";
            basisPotionEffect.m_icon = GetIconImage("basis.png");
            var numBasis = 10f;
            basisPotionEffect.ModifyStaminaRegen(ref numBasis);
            CustomStatusEffect basisPotionCustomEffect = new CustomStatusEffect(basisPotionEffect, fixReference: false);
            Jotunn.Managers.ItemManager.Instance.AddStatusEffect(basisPotionCustomEffect);

            ItemConfig basisPotionConfig = new ItemConfig();
            basisPotionConfig.Name = "Basis";
            basisPotionConfig.Description = "The basis of any potion";
            basisPotionConfig.CraftingStation = CraftingStations.Cauldron;
            basisPotionConfig.AddRequirement("Mushroom", 2);
            basisPotionConfig.Icon = GetIconImage("basis.png");
            CustomItem basisItem = new CustomItem("BasisPotion", "MeadHealthMedium", basisPotionConfig);
            basisItem.ItemDrop.m_itemData.m_shared.m_consumeStatusEffect = basisPotionCustomEffect.StatusEffect;
            Jotunn.Managers.ItemManager.Instance.AddItem(basisItem);

            // 2. Blue Potion
            StatusEffect bluePotionEffect = ScriptableObject.CreateInstance<StatusEffect>();
            bluePotionEffect.name = "BluePotion";
            bluePotionEffect.m_name = "$bluepotion_effectname";
            bluePotionEffect.m_icon = GetIconImage("blue.png");
            float numBlue = 0.25f;
            bluePotionEffect.ModifyEitrRegen(ref numBlue);
            CustomStatusEffect bluePotionCustomEffect = new CustomStatusEffect(bluePotionEffect, fixReference: false);
            Jotunn.Managers.ItemManager.Instance.AddStatusEffect(bluePotionCustomEffect);

            ItemConfig bluePotionConfig = new ItemConfig();
            bluePotionConfig.Name = "Blue Potion";
            bluePotionConfig.Description = "Blue mushroom tincture";
            bluePotionConfig.CraftingStation = CraftingStations.Cauldron;
            bluePotionConfig.AddRequirement("MushroomMagecap", 2);
            bluePotionConfig.Icon = GetIconImage("blue.png");
            CustomItem blueItem = new CustomItem("BluePotion", "MeadHealthMedium", bluePotionConfig);
            blueItem.ItemDrop.m_itemData.m_shared.m_consumeStatusEffect = bluePotionCustomEffect.StatusEffect;
            Jotunn.Managers.ItemManager.Instance.AddItem(blueItem);

            // 3. Green Potion
            StatusEffect greenPotionEffect = ScriptableObject.CreateInstance<StatusEffect>();
            greenPotionEffect.name = "GreenPotion";
            greenPotionEffect.m_name = "$greenpotion_effectname";
            greenPotionEffect.m_icon = GetIconImage("green.png");
            float numGreen = 1.25f;
            greenPotionEffect.ModifyRaiseSkill(Skills.SkillType.All, ref numGreen);
            CustomStatusEffect greenPotionCustomEffect = new CustomStatusEffect(greenPotionEffect, fixReference: false);
            Jotunn.Managers.ItemManager.Instance.AddStatusEffect(greenPotionCustomEffect);

            ItemConfig greenPotionConfig = new ItemConfig();
            greenPotionConfig.Name = "Green Potion";
            greenPotionConfig.Description = "Decantation potion from vineberry";
            greenPotionConfig.CraftingStation = CraftingStations.Cauldron;
            greenPotionConfig.AddRequirement("Vineberry", 2);
            greenPotionConfig.Icon = GetIconImage("green.png");
            CustomItem greenItem = new CustomItem("GreenPotion", "MeadHealthMedium", greenPotionConfig);
            greenItem.ItemDrop.m_itemData.m_shared.m_consumeStatusEffect = greenPotionCustomEffect.StatusEffect;
            Jotunn.Managers.ItemManager.Instance.AddItem(greenItem);

            // 4. Orange Potion
            StatusEffect orangePotionEffect = ScriptableObject.CreateInstance<StatusEffect>();
            orangePotionEffect.name = "OrangePotion";
            orangePotionEffect.m_name = "$orangepotion_effectname";
            orangePotionEffect.m_icon = GetIconImage("orange.png");
            CustomStatusEffect orangePotionCustomEffect = new CustomStatusEffect(orangePotionEffect, fixReference: false);
            Jotunn.Managers.ItemManager.Instance.AddStatusEffect(orangePotionCustomEffect);

            ItemConfig orangePotionConfig = new ItemConfig();
            orangePotionConfig.Name = "Orange Potion";
            orangePotionConfig.Description = "Jotunn puff mushroom tincture";
            orangePotionConfig.CraftingStation = CraftingStations.Cauldron;
            orangePotionConfig.AddRequirement("MushroomJotunPuffs", 2);
            orangePotionConfig.Icon = GetIconImage("orange.png");
            CustomItem orangeItem = new CustomItem("OrangePotion", "MeadHealthMedium", orangePotionConfig);
            orangeItem.ItemDrop.m_itemData.m_shared.m_consumeStatusEffect = orangePotionCustomEffect.StatusEffect;
            Jotunn.Managers.ItemManager.Instance.AddItem(orangeItem);

            // 5. Pink Potion
            StatusEffect pinkPotionEffect = ScriptableObject.CreateInstance<StatusEffect>();
            pinkPotionEffect.name = "PinkPotion";
            pinkPotionEffect.m_name = "$pinkpotion_effectname";
            pinkPotionEffect.m_icon = GetIconImage("pink.png");
            CustomStatusEffect pinkPotionCustomEffect = new CustomStatusEffect(pinkPotionEffect, fixReference: false);
            Jotunn.Managers.ItemManager.Instance.AddStatusEffect(pinkPotionCustomEffect);

            ItemConfig pinkPotionConfig = new ItemConfig();
            pinkPotionConfig.Name = "Pink Potion";
            pinkPotionConfig.Description = "Thistle extract";
            pinkPotionConfig.CraftingStation = CraftingStations.Cauldron;
            pinkPotionConfig.AddRequirement("Thistle", 2);
            pinkPotionConfig.Icon = GetIconImage("pink.png");
            CustomItem pinkItem = new CustomItem("PinkPotion", "MeadHealthMedium", pinkPotionConfig);
            pinkItem.ItemDrop.m_itemData.m_shared.m_consumeStatusEffect = pinkPotionCustomEffect.StatusEffect;
            Jotunn.Managers.ItemManager.Instance.AddItem(pinkItem);

            // 6. Purple Potion
            StatusEffect purplePotionEffect = ScriptableObject.CreateInstance<StatusEffect>();
            purplePotionEffect.name = "PurplePotion";
            purplePotionEffect.m_name = "$purplepotion_effectname";
            purplePotionEffect.m_icon = GetIconImage("purple.png");
            float numPurple = 5.25f;
            purplePotionEffect.ModifyStaminaRegen(ref numPurple);
            CustomStatusEffect purplePotionCustomEffect = new CustomStatusEffect(purplePotionEffect, fixReference: false);
            Jotunn.Managers.ItemManager.Instance.AddStatusEffect(purplePotionCustomEffect);

            ItemConfig purplePotionConfig = new ItemConfig();
            purplePotionConfig.Name = "Purple Potion";
            purplePotionConfig.Description = "Berry broth";
            purplePotionConfig.CraftingStation = CraftingStations.Cauldron;
            purplePotionConfig.AddRequirement("Raspberry", 2);
            purplePotionConfig.AddRequirement("Blueberries", 2);
            purplePotionConfig.Icon = GetIconImage("purple.png");
            CustomItem purpleItem = new CustomItem("PurplePotion", "MeadHealthMedium", purplePotionConfig);
            purpleItem.ItemDrop.m_itemData.m_shared.m_consumeStatusEffect = purplePotionCustomEffect.StatusEffect;
            Jotunn.Managers.ItemManager.Instance.AddItem(purpleItem);

            // 7. Red Potion
            StatusEffect redPotionEffect = ScriptableObject.CreateInstance<StatusEffect>();
            redPotionEffect.name = "RedPotion";
            redPotionEffect.m_name = "$redpotion_effectname";
            redPotionEffect.m_icon = GetIconImage("red.png");
            CustomStatusEffect redPotionCustomEffect = new CustomStatusEffect(redPotionEffect, fixReference: false);
            Jotunn.Managers.ItemManager.Instance.AddStatusEffect(redPotionCustomEffect);

            ItemConfig redPotionConfig = new ItemConfig();
            redPotionConfig.Name = "Red Potion";
            redPotionConfig.Description = "Bloodbag effusion";
            redPotionConfig.CraftingStation = CraftingStations.Cauldron;
            redPotionConfig.AddRequirement("Bloodbag", 2);
            redPotionConfig.Icon = GetIconImage("red.png");
            CustomItem redItem = new CustomItem("RedPotion", "MeadHealthMedium", redPotionConfig);
            redItem.ItemDrop.m_itemData.m_shared.m_consumeStatusEffect = redPotionCustomEffect.StatusEffect;
            Jotunn.Managers.ItemManager.Instance.AddItem(redItem);

            // 8. Yellow Potion
            StatusEffect yellowPotionEffect = ScriptableObject.CreateInstance<StatusEffect>();
            yellowPotionEffect.name = "YellowPotion";
            yellowPotionEffect.m_name = "$yellowpotion_effectname";
            yellowPotionEffect.m_icon = GetIconImage("yellow.png");
            float numYellow = 5.25f;
            yellowPotionEffect.ModifyHealthRegen(ref numYellow);
            CustomStatusEffect yellowPotionCustomEffect = new CustomStatusEffect(yellowPotionEffect, fixReference: false);
            Jotunn.Managers.ItemManager.Instance.AddStatusEffect(yellowPotionCustomEffect);

            ItemConfig yellowPotionConfig = new ItemConfig();
            yellowPotionConfig.Name = "Yellow Potion";
            yellowPotionConfig.Description = "Yellow mushroom tincture";
            yellowPotionConfig.CraftingStation = CraftingStations.Cauldron;
            yellowPotionConfig.AddRequirement("MushroomYellow", 2);
            yellowPotionConfig.Icon = GetIconImage("yellow.png");
            CustomItem yellowItem = new CustomItem("YellowPotion", "MeadHealthMedium", yellowPotionConfig);
            yellowItem.ItemDrop.m_itemData.m_shared.m_consumeStatusEffect = yellowPotionCustomEffect.StatusEffect;
            Jotunn.Managers.ItemManager.Instance.AddItem(yellowItem);

            // Отписываемся от события после завершения настройки
            PrefabManager.OnVanillaPrefabsAvailable -= SetupPotions;
        }

        Sprite GetIconImage(string iconName)
        {
            Assembly assembly = Assembly.GetExecutingAssembly();
            // Загружаем поток из внедренного ресурса
            using (System.IO.Stream stream = assembly.GetManifestResourceStream("PrettyPotion.resources." + iconName))
            {

                if (stream != null)
                {

                    // Читаем поток в массив байтов
                    byte[] imageData = new byte[stream.Length];
                    stream.Read(imageData, 0, (int)stream.Length);
                    // Создаем Texture2D
                    Texture2D texture = new Texture2D(2, 2);
                    texture.LoadImage(imageData); // Автоматически распознает PNG/JPG

                    // Конвертируем Texture2D в Sprite
                    Sprite sprite = Sprite.Create(
                        texture,
                        new Rect(0, 0, texture.width, texture.height),
                        new Vector2(0.5f, 0.5f) // Центр спрайта
                    );
                    Console.Log(sprite);
                    return sprite;
                }
            }
            return null;
        }
        private void AddStatusEffects(CustomStatusEffect customStatusEffect)
        {
            Jotunn.Managers.ItemManager.Instance.AddStatusEffect(customStatusEffect);
        }


        // Патч для эффекта исцеления от Hellbroth of Eternal Life
        [HarmonyPatch(typeof(Aoe), nameof(Aoe.OnHit))]
        private class HealingBrothEffect
        {
            private static bool Prefix(Collider collider, Vector3 hitPoint, Aoe __instance, ref bool __result)
            {
                // Проверяем, является ли эффект взрывом Hellbroth of Eternal Life
                if (!__instance.name.StartsWith("Hellbroth_Life_Explosion", StringComparison.Ordinal))
                {
                    return true;
                }

                __result = false;

                // Проверяем, не был ли объект уже обработан
                GameObject hitObject = Projectile.FindHitObject(collider);
                if (__instance.m_hitList.Contains(hitObject))
                {
                    return true;
                }
                __instance.m_hitList.Add(hitObject);

                // Исцеляем игрока, если он попал под эффект
                if (hitObject.GetComponent<Character>() is Player player)
                {
                    player.Heal(player.GetMaxHealth() * hellbrothOfEternalLifeHealing.Value / 100);
                    __result = true;
                    __instance.m_hitEffects.Create(hitPoint, Quaternion.identity);
                }

                return false;
            }
        }

        // Патч для предотвращения разговоров призраков
        [HarmonyPatch(typeof(RandomSpeak), nameof(RandomSpeak.Start))]
        private class PreventGhostFromTalking
        {
            private static bool Prefix(RandomSpeak __instance)
            {
                // Отключаем разговоры для объектов в слое призраков
                return __instance.gameObject.layer != Piece.s_ghostLayer;
            }
        }

        // Патч для обхода проверки боеприпасов для Odins Dragon Staff
        [HarmonyPatch(typeof(Attack), nameof(Attack.Start))]
        private class BypassAmmoCheck
        {
            private static void Prefix(Attack __instance, ItemDrop.ItemData weapon, ref string? __state)
            {
                // Сохраняем тип боеприпасов и очищаем его для дымовой завесы
                if (weapon.m_dropPrefab?.name == "Odins_Dragon_Staff" && __instance.m_spawnOnTrigger?.name == "Staff_Smoke_Cloud")
                {
                    __state = weapon.m_shared.m_ammoType;
                    weapon.m_shared.m_ammoType = "";
                }
            }

            private static void Finalizer(ItemDrop.ItemData weapon, ref string? __state)
            {
                // Восстанавливаем тип боеприпасов
                if (__state is not null)
                {
                    weapon.m_shared.m_ammoType = __state;
                }
            }
        }

        // Патч для уменьшения потребления боеприпасов
        [HarmonyPatch(typeof(Attack), nameof(Attack.UseAmmo))]
        private class ReduceAmmoUsage
        {
            private static bool Prefix(Attack __instance, ref bool __result)
            {
                switch (__instance.m_weapon.m_dropPrefab?.name)
                {
                    // Для дымовой завесы Odins Dragon Staff всегда возвращаем true
                    case "Odins_Dragon_Staff" when __instance.m_spawnOnTrigger?.name == "Staff_Smoke_Cloud":
                        __result = true;
                        return false;
                    // Для Odins Alchemy Wand или Dragon Staff с надетой Wizard Hat
                    case "Odins_Alchemy_Wand" or "Odins_Dragon_Staff" when __instance.m_character.m_helmetItem?.m_dropPrefab?.name == "Odins_Wizard_Hat":
                        {
                            if (__instance.m_character.GetInventory().GetAmmoItem(__instance.m_weapon.m_shared.m_ammoType) is not { } ammoItem)
                            {
                                return true;
                            }

                            // Шанс не потреблять заряд с Wizard Hat
                            if (Random.value < wizardHatConsumeChargeReduction.Value / 100f)
                            {
                                __instance.m_ammoItem = ammoItem;
                                __result = true;
                                return false;
                            }
                            break;
                        }
                }

                return true;
            }
        }

        // Патч для обработки дымовой завесы
        [HarmonyPatch(typeof(Projectile), nameof(Projectile.FixedUpdate))]
        private class SmokescreenHitBarrier
        {
            private static int ProjectileBlocker(int mask)
            {
                // Добавляем слой блокиратора в маску
                return mask | 1 << LayerMask.NameToLayer("blocker");
            }

            private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
            {
                FieldInfo maskField = AccessTools.DeclaredField(typeof(Projectile), nameof(Projectile.s_rayMaskSolids));
                foreach (CodeInstruction instruction in instructions)
                {
                    yield return instruction;
                    if (instruction.opcode == OpCodes.Ldsfld && instruction.OperandIs(maskField))
                    {
                        // Добавляем вызов ProjectileBlocker после загрузки маски
                        yield return new CodeInstruction(OpCodes.Call, AccessTools.DeclaredMethod(typeof(SmokescreenHitBarrier), nameof(ProjectileBlocker)));
                    }
                }
            }
        }

        // Патч для уничтожения заблокированных снарядов дымовой завесой
        [HarmonyPatch(typeof(Projectile), nameof(Projectile.OnHit))]
        private class DestroyBlockedProjectile
        {
            private static bool Prefix(Projectile __instance, Collider collider, ref bool __state)
            {
                if (collider && collider.gameObject.layer == LayerMask.NameToLayer("blocker") && collider.transform.parent?.GetComponent<ZNetView>()?.m_zdo is { } smokescreenZDO)
                {
                    // Проверяем владельца дымовой завесы
                    if (ZNetScene.instance.FindInstance(smokescreenZDO.GetZDOID("PotionsPlus SmokeCloud Owner"))?.GetComponent<Character>() is { } smokescreenOwner && !__instance.IsValidTarget(smokescreenOwner))
                    {
                        return false;
                    }

                    __state = true;
                    Random.InitState((int)__instance.m_nview.m_zdo.m_uid.ID);
                    Random.State state = Random.state;
                    // Проверяем шанс блокировки снаряда
                    bool blockIt = Random.value < (smokeScreenChanceToBlock.Value + (smokescreenZDO.GetBool("PotionsPlus SmokeCloud HatBonus") ? warlockHatSmokeScreenBlockIncrease.Value : 0)) / 100f;
                    Random.state = state;

                    return blockIt;
                }
                return true;
            }

            [HarmonyPriority(Priority.Low)]
            private static void Postfix(Projectile __instance, Collider collider, bool __state)
            {
                // Уничтожаем снаряд, если он был заблокирован и остается статичным
                if (__state && __instance is { m_didHit: true, m_stayAfterHitStatic: true })
                {
                    ZNetScene.instance.Destroy(__instance.gameObject);
                }
            }
        }

        // Патч для передачи урона от снаряда к AOE-эффекту
        [HarmonyPatch(typeof(Projectile), nameof(Projectile.SpawnOnHit))]
        private class TransferDamageToAoeProjectile
        {
            private static IProjectile UpdateAoeProjectileDamage(IProjectile newProjectile, IProjectile spawning)
            {
                string projectileName = ((Component)spawning).name;
                // Проверяем, является ли снаряд одним из алхимических
                if (wandProjectiles.Any(p => projectileName.StartsWith(p, StringComparison.Ordinal)))
                {
                    Aoe aoe = (Aoe)newProjectile;
                    Projectile projectile = (Projectile)spawning;

                    // Передаем параметры урона и эффектов
                    aoe.m_damage.Add(projectile.m_damage);
                    aoe.m_attackForce += projectile.m_attackForce;
                    aoe.m_backstabBonus += projectile.m_backstabBonus;
                    aoe.m_statusEffect += projectile.m_statusEffect;
                }
                return newProjectile;
            }

            private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
            {
                MethodInfo projectile = AccessTools.DeclaredMethod(typeof(GameObject), nameof(GameObject.GetComponent), Array.Empty<Type>(), new[] { typeof(IProjectile) });
                foreach (CodeInstruction instruction in instructions)
                {
                    yield return instruction;
                    if (instruction.opcode == OpCodes.Callvirt && instruction.OperandIs(projectile))
                    {
                        // Добавляем вызов UpdateAoeProjectileDamage после получения компонента IProjectile
                        yield return new CodeInstruction(OpCodes.Ldarg_0);
                        yield return new CodeInstruction(OpCodes.Call, AccessTools.DeclaredMethod(typeof(TransferDamageToAoeProjectile), nameof(UpdateAoeProjectileDamage)));
                    }
                }
            }
        }
    }
    

    
}
