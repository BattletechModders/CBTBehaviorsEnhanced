using CustomComponents;
using Localize;
using System;
using System.Collections.Generic;
using us.frostraptor.modUtils;

namespace CBTBehaviorsEnhanced
{
    public class CheckHelper
    {
        public static bool DidCheckPassThreshold(SortedDictionary<int, float> dict, int heatValue, Mech mech, float skillMod, string floatieText)
        {
            float checkTarget = 0f;
            foreach (KeyValuePair<int, float> kvp in dict)
            {
                if (heatValue >= kvp.Key)
                {
                    checkTarget = kvp.Value;
                }
            }
            Mod.HeatLog.Info?.Write($"  For heat value: {heatValue} target roll is: {checkTarget}");
            return PassedCheck(checkTarget, mech, skillMod, floatieText);
        }
        public static bool DidCheckPassThreshold(float checkTarget, AbstractActor actor, float skillMod, string floatieText)
        {
            return PassedCheck(checkTarget, actor, skillMod, floatieText);
        }

        private static bool PassedCheck(float checkTarget, AbstractActor actor, float skillMod, string floatieText)
        {
            // If the threshold is -1, you auto-fail
            if (checkTarget == -1f)
            {
                actor.Combat.MessageCenter.PublishMessage(
                    new FloatieMessage(actor.GUID, actor.GUID,
                    $"{new Text(floatieText).ToString()} {new Text(Mod.LocalizedText.Floaties[ModText.FT_Auto_Fail]).ToString()}",
                    FloatieMessage.MessageNature.Neutral)
                    );
                return false;
            }
            // If there's no threshold, you auto-pass
            if (checkTarget <= 0f) { return true; }

            float randomRoll = actor.Combat.NetworkRandom.Float();
            float checkResult = randomRoll + skillMod;
            Mod.Log.Info?.Write($"  pilotMod: {skillMod:#.##} + roll: {randomRoll:#.##} = checkResult: {checkResult:#.##} vs. checkTarget: {checkTarget:#.##}");

            string operatorText = "=";
            if (checkResult > checkTarget) { operatorText = ">"; } else if (checkResult < checkTarget) { operatorText = "<"; }

            bool passedCheck = checkTarget != -1f && checkResult >= checkTarget;
            if (!passedCheck)
            {
                actor.Combat.MessageCenter.PublishMessage(
                    new FloatieMessage(actor.GUID, actor.GUID,
                        $"{new Text(floatieText).ToString()} {checkResult:P1} {operatorText} {checkTarget:P1}",
                        FloatieMessage.MessageNature.Neutral)
                    );
            }

            return passedCheck;
        }

        public static bool ResolvePilotInjuryCheck(Mech mech, int heatToCheck, int rootSequenceGUID, int sequenceGUID, float heatCheck)
        {
            //if (mech.pilot.IsIncapacitated)

            bool failedInjuryCheck = !CheckHelper.DidCheckPassThreshold(Mod.Config.Heat.PilotInjury, heatToCheck, mech, heatCheck, ModText.FT_Check_Injury);
            Mod.Log.Debug?.Write($"  failedInjuryCheck: {failedInjuryCheck}");
            if (failedInjuryCheck)
            {
                Mod.Log.Info?.Write($"-- Pilot Heat Injury check failed for {mech.DistinctId()}, forcing injury from heat");
                mech.pilot.InjurePilot(sequenceGUID.ToString(), rootSequenceGUID, 1, DamageType.OverheatSelf, null, mech);
                if (!mech.pilot.IsIncapacitated)
                {
                    AudioEventManager.SetPilotVOSwitch<AudioSwitch_dialog_dark_light>(AudioSwitch_dialog_dark_light.dark, mech);
                    AudioEventManager.PlayPilotVO(VOEvents.Pilot_TakeDamage, mech, null, null, true);
                    if (mech.team.LocalPlayerControlsTeam)
                    {
                        AudioEventManager.PlayAudioEvent("audioeventdef_musictriggers_combat", "friendly_warrior_injured", null, null);
                    }
                }
                else
                {
                    mech.FlagForDeath("Pilot Killed", DeathMethod.PilotKilled, DamageType.OverheatSelf, 1, sequenceGUID, "0", false);
                    string localText = new Text(Mod.LocalizedText.Floaties[ModText.FT_Death_By_Overheat]).ToString();

                    mech.Combat.MessageCenter.PublishMessage(new AddSequenceToStackMessage(
                        new ShowActorInfoSequence(mech, new Text(localText, Array.Empty<object>()), FloatieMessage.MessageNature.PilotInjury, true))
                        );
                    mech.HandleDeath("0");
                }
            }

            return failedInjuryCheck;
        }

        public static bool ResolveSystemFailureCheck(Mech mech, int heatToCheck, int rootSequenceGUID, float heatCheck)
        {
            bool failedSystemFailureCheck = !CheckHelper.DidCheckPassThreshold(Mod.Config.Heat.SystemFailures, heatToCheck, mech, heatCheck, ModText.FT_Check_System_Failure);
            Mod.Log.Debug?.Write($"  failedSystemFailureCheck: {failedSystemFailureCheck}");
            if (failedSystemFailureCheck)
            {
                Mod.Log.Info?.Write($"-- System Failure check failed, forcing system damage on unit: {CombatantUtils.Label(mech)}");
                List<MechComponent> functionalComponents = new List<MechComponent>();
                foreach (MechComponent mc in mech.allComponents)
                {
                    bool canTarget = mc.IsFunctional;
                    if (mc.mechComponentRef.Is<Flags>(out Flags flagsCC))
                    {
                        if (flagsCC.IsSet(ModStats.ME_IgnoreDamage))
                        {
                            canTarget = false;
                            Mod.Log.Trace?.Write($"    Component: {mc.Name} / {mc.UIName} is marked ignores_damage.");
                        }
                    }
                    if (canTarget) { functionalComponents.Add(mc); }
                }
                MechComponent componentToDamage = functionalComponents.GetRandomElement();
                Mod.Log.Info?.Write($"   Destroying component: {componentToDamage.UIName} from heat damage.");

                WeaponHitInfo fakeHit = new WeaponHitInfo(rootSequenceGUID, -1, -1, -1, string.Empty, string.Empty, -1, null, null, null, null, null, null, null,
                    new AttackDirection[] { AttackDirection.None }, null, null, null);
                componentToDamage.DamageComponent(fakeHit, ComponentDamageLevel.Destroyed, true);
            }

            return failedSystemFailureCheck;
        }

        public static bool ResolveRegularAmmoCheck(Mech mech, int heatToCheck, int rootSequenceGUID, float heatCheck)
        {
            bool failedAmmoCheck = false;
            AmmunitionBox mostDamaging = HeatHelper.FindMostDamagingAmmoBox(mech, false);
            if (mostDamaging != null)
            {
                failedAmmoCheck = !CheckHelper.DidCheckPassThreshold(Mod.Config.Heat.Explosion, heatToCheck, mech, heatCheck, ModText.FT_Check_Explosion);
                Mod.Log.Debug?.Write($"  failedAmmoCheck: {failedAmmoCheck}");
                if (failedAmmoCheck)
                {
                    Mod.Log.Info?.Write($"-- Ammo Explosion check failed, forcing ammo explosion on unit: {CombatantUtils.Label(mech)}");

                    if (mostDamaging != null)
                    {
                        Mod.Log.Info?.Write($"   Exploding ammo: {mostDamaging.UIName}");
                        WeaponHitInfo fakeHit = new WeaponHitInfo(rootSequenceGUID, -1, -1, -1, string.Empty, string.Empty, -1, null, null, null, null, null, null, null,
                            new AttackDirection[] { AttackDirection.None }, null, null, null);
                        mostDamaging.DamageComponent(fakeHit, ComponentDamageLevel.Destroyed, true);
                    }
                    else
                    {
                        Mod.Log.Debug?.Write(" Unit has no ammo boxes, skipping.");
                    }
                }
            }

            return failedAmmoCheck;
        }

        public static bool ResolveVolatileAmmoCheck(Mech mech, int heatToCheck, int rootSequenceGUID, float heatCheck)
        {
            bool failedVolatileAmmoCheck = false;
            AmmunitionBox mostDamagingVolatile = HeatHelper.FindMostDamagingAmmoBox(mech, true);
            if (mostDamagingVolatile != null)
            {
                failedVolatileAmmoCheck = !CheckHelper.DidCheckPassThreshold(Mod.Config.Heat.Explosion, heatToCheck, mech, heatCheck, ModText.FT_Check_Explosion);
                Mod.Log.Debug?.Write($"  failedVolatileAmmoCheck: {failedVolatileAmmoCheck}");
                if (failedVolatileAmmoCheck)
                {
                    Mod.Log.Info?.Write($"-- Volatile Ammo Explosion check failed on {CombatantUtils.Label(mech)}, forcing volatile ammo explosion");

                    if (mostDamagingVolatile != null)
                    {
                        Mod.Log.Info?.Write($" Exploding inferno ammo: {mostDamagingVolatile.UIName}");
                        WeaponHitInfo fakeHit = new WeaponHitInfo(rootSequenceGUID, -1, -1, -1, string.Empty, string.Empty, -1, null, null, null, null, null, null, null,
                            new AttackDirection[] { AttackDirection.None }, null, null, null);
                        mostDamagingVolatile.DamageComponent(fakeHit, ComponentDamageLevel.Destroyed, true);
                    }
                    else
                    {
                        Mod.Log.Debug?.Write(" Unit has no Volatile ammo boxes, skipping.");
                    }
                }
            }

            return failedVolatileAmmoCheck;
        }
    }
}
