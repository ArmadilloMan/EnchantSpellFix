using BlueprintCore.Utils;
using HarmonyLib;
using Kingmaker;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Items.Ecnchantments;
using Kingmaker.Controllers;
using Kingmaker.Designers.Mechanics.EquipmentEnchants;
using Kingmaker.ElementsSystem;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.EntitySystem.Persistence;
using Kingmaker.Enums;
using Kingmaker.Items;
using Kingmaker.PubSubSystem;
using Kingmaker.UI;
using Kingmaker.UI.MVVM._VM.Party;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Abilities;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.UnitLogic.Buffs;
using Kingmaker.UnitLogic.Mechanics;
using Kingmaker.UnitLogic.Mechanics.Actions;
using Kingmaker.Utility;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.Remoting.Contexts;
using System.Threading;
using System.Xml.Linq;
using UnityModManagerNet;
using static Kingmaker.UnitLogic.Parts.UnitPartForceMove;

namespace EnchantSpellFix
{
        public static class Main
        {
        private static Main.LoadListener loadListener;
        private static Main.WarningHandler saveHandler;
        public static UnityModManager.ModEntry.ModLogger logger;

        //just using for quick lookups
        public static Dictionary<string, byte> dictionary;
        public static Boolean loopfix = true;

        public static bool Enabled;
            private static readonly LogWrapper Logger = LogWrapper.Get("MagicWeaponFangMagusShamanBugFix");
            private static Harmony harmony;

            private static bool Load(UnityModManager.ModEntry modEntry)
            {
                Main.logger = modEntry.Logger;
                dictionary = new Dictionary<string, byte>();
                //dictionary.Add("Magic Fang", 0);
                //dictionary.Add("Magic Fang, Greater", 0);
                dictionary.Add("Magic Weapon, Primary", 0);
                dictionary.Add("Magic Weapon, Secondary", 0);
                dictionary.Add("Keen Edge — Primary Hand", 0);
                dictionary.Add("Keen Edge — Secondary Hand", 0);
                dictionary.Add("Magic Weapon, Greater — Primary Hand", 0);
                dictionary.Add("Magic Weapon, Greater — Secondary Hand", 0);
                dictionary.Add("Magical Vestment, Armor", 0);
                dictionary.Add("Magical Vestment, Shield", 0);
                dictionary.Add("Arcane Weapon Enhancement", 0);
                dictionary.Add("Spirit Weapon Enhancement", 0);
                dictionary.Add("Crusader's Edge", 0);

            /*Type targetType = typeof(EquipmentWeaponTypeEnhancement); // Replace YourTargetClass
            MethodInfo originalMethod = targetType.GetMethod("YourPrivateMethodName", new Type[] { typeof(ItemEntityWeapon) });
            originalMethod.*/

            harmony = new Harmony(modEntry.Info.Id);
                harmony.PatchAll();
                modEntry.OnToggle = (Func<UnityModManager.ModEntry, bool, bool>)new Func<UnityModManager.ModEntry, bool, bool>(Main.OnToggle);
            if (Main.loadListener == null)
            {
                Main.loadListener = new Main.LoadListener();
                Main.saveHandler = new Main.WarningHandler();
                EventBus.Subscribe((object)Main.loadListener);
                EventBus.Subscribe((object)(Main.saveHandler));
            }
            return true;
            }

            public static bool OnToggle(UnityModManager.ModEntry modEntry, bool value)
            {
                Enabled = value;
                return true;
            }

        public static void handleBug()
        {
            //return;
            if (Main.Enabled)
            {

                foreach (UnitReference partyCharacter in Game.Instance.Player.PartyCharacters)
                {
                    LinkedList<AbilityExecutionContext> buffOrderList = new LinkedList<AbilityExecutionContext>();
                    LinkedList<Buff> removeBuffs = new LinkedList<Buff>();
                    //LinkedList<Buff> buffOrder = new LinkedList<Buff>();
                    
                    foreach (Buff buff in partyCharacter.Value.Buffs)
                    {
                        string name = buff.Name;
                        //logger.Log(name);
                        bool doLast = false;

                        if(dictionary.ContainsKey(name))
                        {
                            if (name.Equals("Arcane Weapon Enhancement") || name.Equals("Spirit Weapon Enhancement"))
                                doLast = true;
/*
                            logger.Log("buff: " + buff.Name);
                            logger.Log("source ability: " + buff.Context.SourceAbility.name);
                            logger.Log("source ability2: " + buff.Context.SourceAbility.Comment);
                            logger.Log("source ability3: " + buff.Context.SourceAbility.Description);
                            logger.Log("owner: " + buff.Owner.CharacterName);
                            logger.Log("params:");
                            logger.Log("spell source " + buff.Context.Params.SpellSource);
                            logger.Log("spell source " + buff.Context.Params.SpellLevel);
                            logger.Log("spell source " + buff.GetRank());*/

                            //logger.Log(buff.Context.AssociatedBlueprint.GetPropertyValue(this.PropertyName, buff.Context.MaybeCaster, buff.Context));
                            // logger.Log("spell source " + buff.Context.Params.);

                            //logger.Log(buff.Context.SourceAbility.)

                            BlueprintAbility source = buff.Context.SourceAbility;
                            UnitDescriptor caster = buff.Context.MaybeCaster.Descriptor;
                            AbilityParams aparams = buff.Context.Params;

                            /*logger.Log("reapply buff caster level: " + buff.Context.Params.CasterLevel);
                            logger.Log("reapply buff caster level: " + aparams.CasterLevel);*/


                            
                            AbilityExecutionContext executionContext = null;


                            //this turned out to not be necessary
                                //BlueprintAbility ba = buff.Context.SourceAbility;
                                //MechanicsContext newContext = buff.Context.CloneFor(ba, buff.Context.MainTarget.Unit.Descriptor, buff.Context.MaybeCaster);
                            MechanicsContext newContext = buff.Context;
                            /*AbilityRankType art = AbilityRankType.ProjectilesCount;
                            int val = newContext[art];
                            logger.Log("val: " + val);
                            newContext.Recalculate();
                            logger.Log("newval: " + newContext[art]);

                            logger.Log("newContext caster: " + newContext.MaybeCaster.CharacterName);

                            logger.Log("blueprint : " + newContext.AssociatedBlueprint.GetType().Name);*/
                            executionContext = new AbilityExecutionContext(new AbilityData(newContext.SourceAbility, newContext.MaybeCaster), newContext.Params, newContext.MaybeCaster);
                            if (!doLast)
                            {
                                buffOrderList.AddFirst(executionContext);
                            }
                            else
                            {
                                buffOrderList.AddLast(executionContext);
                            }
                            removeBuffs.AddLast(buff);
                        }
                    }

                    foreach (Buff b in removeBuffs)
                    {
                        b.Remove();
                    }

                    foreach (AbilityExecutionContext context in buffOrderList)
                    {

                        //logger.Log("activating buff: " + context.Name);
                        if (context.Name.Equals("Arcane Weapon Enhancement") || context.Name.Equals("Spirit Weapon Enhancement"))
                        {
                            loopfix = false;
                        }
                        AbilityExecutionProcess.ApplyEffectImmediate(context, (UnitEntityData)partyCharacter);
                    }
                }
            }
        }

        private class LoadListener : IAreaLoadingStagesHandler, IGlobalSubscriber
        {
            void IAreaLoadingStagesHandler.OnAreaLoadingComplete()
            {
                Main.handleBug();
            }

            void IAreaLoadingStagesHandler.OnAreaScenesLoaded()
            {
            }
        }

        class WarningHandler : IWarningNotificationUIHandler
        {
            public void HandleWarning(WarningNotificationType warningType, bool addToLog = true)
            {
                if (warningType == WarningNotificationType.GameSavedQuick || warningType == WarningNotificationType.GameSaved || warningType == WarningNotificationType.GameSavedAuto)
                {
                    logger.Log("got notification");
                    Main.handleBug();
                }
            }

            public void HandleWarning(string text, bool addToLog = true)
            {
            }
        }

        [HarmonyPatch(typeof(ContextActionWeaponEnchantPool), "RunAction", MethodType.Normal)]
        internal class PatchEnchantWeapon
        {
            private static void Postfix()
            {
                if (loopfix)
                {
                    logger.Log("fixing order");
                    loopfix = false;
                    Main.handleBug();
                }
                else
                    loopfix = true;
            }
        }

        [HarmonyPatch(typeof(EnhanceWeapon), "RunAction", MethodType.Normal)]
        internal class PatchEnhanceWeaponRun
        {
            private static void Prefix(EnhanceWeapon __instance)
            {

                    /*logger.Log("valuetype: " + __instance.EnchantLevel.ValueType);
                    logger.Log("valuerank: " + __instance.EnchantLevel.ValueRank);*/


                //we want it to modify the calculation only after this is called
                doPrint = true;
                    //__instance.EnchantLevel.
                    //logger.Log("blueprint2 : " + context.AssociatedBlueprint.GetType().Name);
                    //int val1 = __instance.EnchantLevel.Calculate(context);
                    //logger.Log("calculate: " + val1);


            }

            /*private static void Postfix(EnhanceWeapon __instance)
            {

                logger.Log("valuetype: " + __instance.EnchantLevel.ValueType);
                logger.Log("valuerank: " + __instance.EnchantLevel.ValueRank);
                MechanicsContext context = ContextData<MechanicsContext.Data>.Current?.Context;
                logger.Log("blueprint3 : " + context.AssociatedBlueprint.GetType().Name);
                int val1 = __instance.EnchantLevel.Calculate(context);
                logger.Log("calculate: " + val1);

            }*/
        }

        //[HarmonyPatch(typeof(EquipmentWeaponTypeEnhancement), "CheckWeapon", MethodType.Normal)]
        

        static bool doPrint = false;

        

        [HarmonyPatch(typeof(ContextValue), "Calculate", new Type[] { typeof(MechanicsContext) })]
        internal class PatchCalculate
        {
            private static void Prefix(ContextValue __instance, object[] __args)
            {
                MechanicsContext context = (MechanicsContext)__args[0];
                //logger.Log("precalc: " + __instance.Calculate(context));
                if (doPrint)
                {
                    //fixes incorrect values
                    context.Recalculate();
                    /*logger.Log("value type: " + __instance.ValueType);
                    logger.Log("value rank: " + __instance.ValueRank);*/
                    doPrint = false;

                    /*logger.Log("begin ValueRanks:");

                    FieldInfo fi = typeof(MechanicsContext).GetField("Ranks", BindingFlags.NonPublic | BindingFlags.Instance);

                    try
                    {
                        for (int i=0; i < 100; i++)
                        {
                            logger.Log("v: " + context[(AbilityRankType)  i]);
                        }
                    }
                    catch (Exception ex)
                    {
                    }*/
                }
                //  logger.Log("enhancement: " + __instance.);

                //  nameToEnchant.Add(context.MainTarget.Unit.CharacterName, new Tuple<EnhanceWeapon, object[]>(__instance, __args));

            }
        }

    }


}




