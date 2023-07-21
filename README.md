# Classic BattleTech Behaviors Enhanced (CBTBE)
This mod for the [HBS BattleTech](http://battletechgame.com/) game changes several behaviors to more closely emulate the TableTop BattleTech experience. A summary this mod's changes are:

* Overheating is completely revamped and no longer deals structural damage. Instead, you can be forced to shutdown, experience ammo explosions, pilot injuries, or critical hits, and experience significant movement and attacking penalties.
* When attacking, you suffer a +1 penalty if you walked, +2 if you sprinted, and +3 if you jumped.
* Sprinting no longer ends your turn.
* Evasion is not longer removed after attacks.
* Additional hits when you are unstable can cause a fall. This is resisted by your piloting skill.
* ToHit modifiers can be more effectively stacked to make hits more easily.

This mod was influenced by [McFistyBuns'](https://github.com/McFistyBuns) excellent mods, which sadly are no longer updated. His code was released without a license, but in a discussion with LadyAlekto (of RougeTech) he granted a right to re-use his mod as she liked. While most of the code has been replaced, portions of the original code remains and has been relicensed under the MIT license to honor that exchange.

This mod requires the following mods. Grab the latest releases for each of them and make sure they are present in your Mods/ directory.

*  [IRBTModUtils](https://github.com/iceraptor/IRBTModUtils) - general utility classes common to all of my mods
* [MechEngineer](https://github.com/BattletechModders/mechengineer) - implements many TableTop components like engines, gyros, actuators, and more. 
* [CustomComponents](https://github.com/BattletechModders/customcomponents) - provides the ability to tag specific components with mod-specific objects. Used to identify MechEngineer gyros, engines, and more.
* [CustomUnits](https://github.com/BattletechModders/CustomAmmoCategories/tree/master/CustomUnits) - provides support for punch and hit tables on units like vtols, etc.

:exclamation: Users of KMission's [CustomActivatableEquipment](https://github.com/CMiSSioN/CustomActivatableEquipment) should change the setting `StartupByHeatControl` in mod.json to false. If you do not, AI-based heat effects (ammo explosions, pilot injuries, etc.) will not be applied.

:exclamation: You are strongly encouraged to set `"CBTWalkAndRunMPRounding" : "true"` in Mods\MechEngineer\Settings.json. This will ensure WalkSpeed will be normalized, as well as RunSpeed.

## Heat Scale Changes

These changes replace the default overheating behaviors, in which high heat levels caused internal structure damage. Instead it introduces a series of progressively more difficult random skill tests that cause negative effects when failed, as well as attack and movement modifiers at high heat levels.

* __ToHit Modifiers__ is a penalty to the unit's chance to hit
* __Movement Modifiers__ is a penalty to the unit's base Walking Speed, which also reduces it's running speed (see Movement Changes)
* __Shutdown Chance__ is the threshold a percentile check (d100) must equal or exceed, or the Mech will be forced into a Shutdown
* __Ammo Explosion Chance__ is the threshold a percentile check (d100) must equal or exceed, or the most damaging ammo box remaining will explode and inflict internal damage
* __Pilot Injury Chance__ is the threshold a percentile check (d100) must equal or exceed, or the Pilot will suffer one point of damage
* __System Failure Chance__ is the threshold a percentile check (d100) must equal or exceed, or one randomly selected internal component will be destroyed

These changes are tied to the _Classic BattleTech (CBT)_ extended heat scale, which normally ranges from 0 to 50 heat. The HBS game expanded heat values on a 3:1 basis, so this implementation uses a 0 to 150 scale for heat. Multiple effects can occur at the same heat level, which can result in the unit shutting down, having an ammo explosion, and injuring the pilot all at once!

All heat effects are resolved at the end of the current turn, __except__ for the movement and attack modifiers. The code patches them them difficult to only implement at end-of-turn, so they are applied as soon as they happen.

The table below lists the thresholds for the default configuration provided in `mod.json`.

Note the chance for an event is modified by the pilot's Guts skill. When a roll is made for an event, the Guts skill is multiplied by the `Piloting.SkillMulti` value (in mod.json) and is added to the random result. This value is then compared against the event chance, and if lower than the threshold percentage the event occurs.

> Example: A mech with piloting 4 has a 60% shutdown chance. The random roll is 0.23 + (4 x 0.05) = 0.43. This is less than the threshold of 0.60, so the mech will shutdown.

| Heat Level | Shutdown Chance | Ammo Explosion | Pilot Injury | System Failure | Attack Modifier | Movement Modifier |
| -- | -- | -- | -- | -- | -- | -- |
| 15 | - | - | - | - | - | -30m |
| 24 | - | - | - | - | -1 | -30m |
| 30 | - | - | - | - | -1 | -60m |
| 39 | - | - | - | - | -2 | -60m |
| 42 | 10% | - | - | - | -2 | -90m |
| 45 | 10% | - | - | - | -2 | -90m |
| 51 | 10% | - | - | - | -3 | -90m |
| 54 | 30% | - | - | - | -3 | -90m |
| 57 | 30% | 10% | - | - | -3 | -90m |
| 60 | 30% | 10% | - | - | -3 | -120m |
| 66 | 60% | 10% | - | - | -3 | -120m |
| 69 | 60% | 30% | - | - | -3 | -120m |
| 72 | 60% | 30% | - | - | -4 | -120m |
| 75 | 60% | 30% | - | - | -4 | -150m |
| 78 | 80% | 30% | - | - | -4 | -150m |
| 84 | 80% | 50% | 30% | - | -4 | -150m |
| 90 | 90% | 50% | 30% | - | -4 | -180m |
| 93 | 90% | 50% | 30% | - | -4 | -180m |
| 99 | 90% | 50% | 30% | - | -5 | -180m |
| 102 | 100% | 50% | 30% | - | -5 | -210m |
| 105 | 100% | 80% | 30% | - | -5 | -210m |
| 108 | 100% | 80% | 30% | 30% | -5 | -210m |
| 111 | 100% | 80% | 30% | 30% | -5 | -210m |
| 114 | 110% | 80% | 30% | 30% | -5 | -210m |
| 117 | 110% | 80% | 60% | 30% | -5 | -210m |
| 120 | 110% | 95% | 60% | 30% | -5 | -210m |
| 123 | 110% | 95% | 60% | 30% | -6 | -210m |
| 126 | 120% | 95% | 60% | 30% | -6 | -210m |
| 129 | 120% | 95% | 60% | 30% | -6 | -240m |
| 132 | 120% | 95% | 60% | 60% | -6 | -240m |
| 135 | 120% | Auto | 60% | 60% | -6 | -240m |
| 138 | 130% | Auto | 60% | 60% | -6 | -240m |
| 141 | 130% | Auto | 80% | 60% | -6 | -240m |
| 144 | 130% | Auto | 80% | 60% | -7 | -240m |
| 147 | 130% | Auto | 80% | 60% | -7 | -270m |
| 150 | Auto | Auto | 80% | 60% | -7 | -270m |

The chances above are displayed in a tooltip shown when hovering over the heat-bar in the bottom left corner, just above the Mech paper doll. These values are dynamically updated when you target an enemy, so you can predict will occur at the end of your turn.

All chances and modifiers are configurable in the mod.json file.

### AI and Heat Scale
The AI was trained to deal with the HBS model of heat. As you gained heat, you eventually did damage to your structure, and could die from 'corroding' it from the inside out. The AI uses the behavior variable `Float_AcceptableHeatLevel` to determine how risky it chooses to play, with a range from 0-3.

CBTBE uses this value, but expects you (the mod developer) to set it to a percentage representing the maximum percentage of failure the AI will accept from heat. This value is from 0.0 - 1.0, with a suggested value between 0.4 - 0.8 if you're using the heat scale as presented.

When the AI calculates it's acceptable heat, CBTBE will evaluate the maximum possible heat in this order:  

1. Volatile ammo boxes: If the unit has any volatile ammo boxes *at all* then the chance of failure is increased, as there are two rolls made. This comparison is against the ammo explosion limits.
2. Regular ammo boxes: If the unit has any ammo boxes, then the `Float_AcceptableHeatLevel` threshold is checked against the ammo explosion limits.
3. Shutdown: The default; if all other tests pass then `Float_AcceptableHeatLevel` is compared against the shutdown thresholds.

(!) Note - because behaviorVars are exposed through JArrays, CBTBE cannot selectively merge changes to `Float_AcceptableHeatLevel`. Other mods like BetterAI already change these values, and CBTBE will overwrite any such changes they make. You are strongly encouraged to modify `Float_AcceptableHeatLevel` in the appropriate file in your own mod, and delete the StreamAssets version shipped with CBTBE. 

## Classic Piloting

CBT Piloting attempts to bring Classic Battletech Tabletop piloting skill checks into HBS's BATTLETECH game. Currently only one check is implemented.

Whenever you mech becomes unstable, every hit that causes stability damage will cause a Piloting skill check. Currently the base difficulty is a flat 30%. On damage, the game will roll a random number and apply your piloting skill. Under 30% will cause a knockdown regardless of your total stability damage. The piloting skill percentage is calculated the way all skill check percentages are calculated in the game, which is to take your skill and divide it by the skill divisor ( Skill / PilotingDivisor). The default PilotingDivisor is 40. So for example, a piloting skill of 5 will add a 12.5% chance to the random roll. The default difficulty of 30% leaves a pilot with a skill of 10 a 5% chance of failure. I figured that was a good trade-off, since the CBT piloting skill checks always had a chance of failure no matter the skill level.

Difficulty percentage is configurable in the mod.json file.

### Fall Damage

In HBS BT, when a BattleMech falls over the pilot takes an injury and nothing else happens. In tabletop, mechs take armor and structure damage proportional to their tonnage. CBTBE replicates this behavior by applying damage after the fall sequence finishes. This damage can result in additional pilot hits, component loss, or ammo explosions! 

By default, the unit takes damage equal to it's tonnage times `Piloting.FallDamagePerTon` value in `mod.json` This damage is reduced by (normalized) piloting skill. The unit takes `piloting_normalized x Piloting.FallDamageReductionMulti` less damage from the fall, simulating more skillful pilots helping their units land gracefully. Actuator damage that reduces the effective piloting skill is applied, as described in "Classic Movement - Leg Damage"

```
Wolverine with tonnage 55 falls
rawDamage = 55 * Piloting.FallDamagePerTon (default: 1) => 55 points of damage
Piloting skill of 5
ignoreMulti = Piloting.FallDamageReductionMulti (default: 0.05) x 5 => 0.25
ignoredDamage = FLOOR(rawDamage (55) x ignoreMulti (0.25)) => FLOOR(13.75) => 13
fallDamage = rawDamage (55) - ignoredDamage(13) = 42
if Piloting.FallDamageClusterDivisor = 25, unit takes two hits of [25, 17] damage
```

The total damage is applied as clusters of damage. No cluster will be greater than `Piloting.FallDamageClusterDivisor`.

### Piloting TODO 

* TODO: When standing, make a piloting check or fall over again
* TODO: When moving through certain terrain, add instability (water, sand, light jungle, heavy jungle, rubble) 

## Classic Melee

Melee has been revamped in CBTBE to allow player selection of melee types, as well as various other small tweaks. Players can choose between the following types of melee attacks:

* **Charge** attacks do greater damage and instability the further the attacker moves,  and can be performed at sprint range. However the attacker also takes damage and instability from the attack. Damage for both the attacker and target are grouped into 25 point clusters and randomly distributed using the standard hit tables. Charges are treated as stomps against target vehicles.
* **Death From Above** apply damage to the target's arms, torsos, and head. The attacker's legs are damaged, and both models take *significant* instability damage and are likely to end up *unsteady*.
* **Kick** attacks are easy to land, and apply all their damage to a single, randomly selected leg. The target takes instability damage and will be made *unsteady*. If the attack missed, the attacker instead is made *unsteady*.
* **Physical Weapon** attacks apply damage and instability to the target based upon their specific characteristics. A sword may apply more damage, while a mace may apply more instability. Physical weapons can choose to use the standard, kick, or punch tables to resolve their attacks. 
* **Punch** attacks do less damage than kicks, but strike the arms, torsos, and head of the target. There are no effects if a punch misses it's target.

Each of these attacks have various statistics that can modify their damage, and unique configuration details. Those are covered in the sections below. 

The AI will always choose to use the most powerful melee attack they have. Attacks that inflict unsteady will be prioritized when the target has evasion pips, but otherwise the attack with the greatest expected target damage will be selected. This can result in the AI tripping or killing itself.

This mod incorporates the functionality of Mophyum's [Melee Mover](https://github.com/morphyum/meleemover/). This allows units to use sprint movement for melee, and allows them to move while engaged in combat. 

### Charge Attacks

Charge attacks inflict damage and instability on both the attacker and target, and those values are increased by the number of *hexes* (not meters!) between the target and attacker. The calculation for damage and instability is the same, and follow this formula:

`finalDamage = RoundUP( ( (raw * attacker/target tonnage) + mod) * multi * hexesMoved)`

By default the attacker's self damage from the charge does not get multiplied by the distance moved. This is TableTop behavior. If `Melee.Charge.MultiplyAttackerSelfDamageByHexesMoved` is true, this is changed so that attacker self damage is multiplied by the distance moved.

The inputs for these values differ based upon configuration values (exposed through `mod.json`) and per-unit statistic values (added through status effects). They vary between attacker and target, allowing mod authors great flexibility in designing attacks.

*Attacker Inputs*

| Input     | Source    | Damage Value                     | Instability Value                       |
| --------- | --------- | ----------------------------------------- | ----------------------------------------- |
| **raw**   | mod.json  | `Melee.Charge.AttackerDamagePerTargetTon` | `Melee.Charge.AttackerInstabilityPerTargetTon` |
| **mod**   | statistic | `CBTBE_Charge_Attacker_Damage_Mod`        | `CBTBE_Charge_Attacker_Instability_Mod` |
| **multi** | statistic | `CBTBE_Charge_Attacker_Damage_Mult`      |`CBTBE_Charge_Attacker_Instability_Multi`|
| **reduction** | statistic | `CBTBE_Charge_Target_Damage_Reduction_Multi`      |`CBTBE_Charge_Target_Instability_Reduction_Multi`|

*Target Inputs*

| Input     | Source    | Damage Value                     | Instability Value                       |
| --------- | --------- | ----------------------------------------- | ----------------------------------------- |
| **raw**   | mod.json  | `Melee.Charge.TargetDamagePerAttackerTon` | `Melee.Charge.TargetInstabilityPerAttackerTon` |
| **mod**   | statistic | `CBTBE_Charge_Target_Damage_Mod`        | `CBTBE_Charge_Target_Instability_Mod` |
| **multi** | statistic | `CBTBE_Charge_Target_Damage_Mult`      |`CBTBE_Charge_Target_Instability_Multi`|

*HexesMoved* is calculated as the magnitude of the distance between the Attacker and Target's current position as vectors. This is then divided by the *Move.MPMetersPerHex* configuration value in `mod.json`. Because this value is a vector magnitude, it may result in more or less hexes of movement than you might expect, due to elevation changes or similar. 

Damage is applied to the attacker and target as a series of clusters. The size of each cluster is determined by the *DamageClusterDivisor* in `mod.json`. Each cluster is resolved on the standard damage table for the target, as per a normal attack. 

**Reductions**: The damage and instability applied to both attacker and target will be modified the ``CBTBE_Charge_Target_Damage_Reduction_Multi` and `CBTBE_Charge_Target_Instability_Reduction_Multi` statistics. Both are ``System.Single` values (aka floats) that default to 1.0, and are read from the unit that suffers damage (attacker for attacker self damage, target for target damage). The value of these are multiplied against the raw values generated by the formula above, before clustering is performed.

**Validations**: Before an attack can be made, several validation checks must be passed. If all of these checks do not pass, the attack is invalid and can't be selected by either player or AI.

* The target position must allow either *Tackle* or *Stomp* animations. These are generally allowed if the attacker midpoint is somewhere between the top and bottom of the target. 
* The target cannot be **prone** 

**Modifiers**: Charge attacks apply a handful of modifiers, in addition to the positional ones (side attack, rear attack, etc) normally added to attacks.

* **Comparative Skill** is the difference between the attacker and target's *Piloting* skill rating. This is applied as a flat modifier, so an attacker with Piloting 7 versus a target with Piloting 4 would add a -3 to hit bonus. An attacker with Piloting 2 versus a target with piloting 6 would suffer a +4 to hit penalty. 

**Unsteady**: Attacks can apply the *Unsteady* state to a unit, dumping it's evasion pips and allowing it to be knocked down when it takes instability. There are three settings in the *Melee.Charge* portion of `mod.json` controlling when units gain unsteady:

* **UnsteadyAttackerOnHit** - if set to true, the *attacker* will be marked Unsteady on a *successful* charge
* **UnsteadyAttackerOnMiss**- if set to true, the *attacker* will be marked Unsteady on a *failed* charge
* **UnsteadyTargetOnHit**- if set to true, the *target* will be marked Unsteady on a *successful* charge

When charging the AI will evaluate its own armor, structure, and evasion loss as part of it's determination to charge. The `Melee.Charge.SelfCTKillVirtDamageMulti` configuration value is a multiplier for this damage in the case that self-damage is greater than the current armor and structure in the attacker's CT.

### Death From Above Attacks

Death from Above (DFA) attacks influence damage and instability on both the attacker and the target. The attacker takes flat amount of damage based upon the target's tonnage:

```
rawDamage = Ceiling(AttackerDamagePerTargetTon * targetTonnage)
totalDamage = Ceiling((rawDamage + DeathFromAboveAttackerDamageMod) * DeathFromAboveAttackerDamageMulti)
```

The target takes damage based upon the attacker's tonnage instead:  

```
rawDamage = Ceiling(TargetDamagePerAttackerTon * attackerTonnage)
totalDamage = Ceiling((rawDamage + DeathFromAboveTargetDamageMod) * DeathFromAboveTargetDamageMulti)
```


The inputs for these values differ based upon configuration values (exposed through `mod.json`) and per-unit statistic values (added through status effects). They vary between attacker and target, allowing mod authors great flexibility in designing attacks.

*Attacker Inputs*

| Input     | Source    | Damage Value                     | Instability Value                       |
| --------- | --------- | ----------------------------------------- | ----------------------------------------- |
| **raw**   | mod.json  | `Melee.DFA.AttackerDamagePerTargetTon` | `Melee.DFA.AttackerInstabilityPerTargetTon` |
| **mod**   | statistic | `CBTBE_DFA_Attacker_Damage_Mod`        | `CBTBE_DFA_Attacker_Instability_Mod` |
| **multi** | statistic | `CBTBE_DFA_Attacker_Damage_Multi`      |`CBTBE_DFA_Attacker_Instability_Multi`|
| **reduction** | statistic | `CBTBE_DFA_Target_Damage_Reduction_Multi`      |`CBTBE_DFA_Target_Instability_Reduction_Multi`|

*Target Inputs*

| Input     | Source    | Damage Value                     | Instability Value                       |
| --------- | --------- | ----------------------------------------- | ----------------------------------------- |
| **raw**   | mod.json  | `Melee.DFA.TargetDamagePerAttackerTon` | `Melee.DFA.TargetInstabilityPerAttackerTon` |
| **mod**   | statistic | `CBTBE_DFA_Target_Damage_Mod`        | `CBTBE_DFA_Target_Instability_Mod` |
| **multi** | statistic | `CBTBE_DFA_Target_Damage_Multi`      |`CBTBE_DFA_Target_Instability_Multi`|

Damage is applied to the attacker and target as a series of clusters. The size of each cluster is determined by the *DFA.DamageClusterDivisor* in `mod.json`. For the attacker, each cluster location is resolved against the Kick table. Cluster locations for the target are resolved against the Punch table, or the Rear location when the target is prone.

**Reductions**: The damage and instability applied to both attacker and target will be modified the ``CBTBE_DFA_Target_Damage_Reduction_Multi` and `CBTBE_DFA_Target_Instability_Reduction_Multi` statistics. Both are ``System.Single` values (aka floats) that default to 1.0, and are read from the unit that suffers damage (attacker for attacker self damage, target for target damage). The value of these are multiplied against the raw values generated by the formula above, before clustering is performed.

**Validations**: Before an attack can be made, several validation checks must be passed. If all of these checks do not pass, the attack is invalid and can't be selected by either player or AI.

* The target position must be within jumping distance
* The target cannot be ``UnaffectedPathing` as per CustomUnits. These are typically VTOL units that ignore terrain effects.

**Modifiers**: Charge attacks apply a handful of modifiers, in addition to the positional ones (side attack, rear attack, etc) normally added to attacks.

* **Comparative Skill** is the difference between the attacker and target's *Piloting* skill rating. This is applied as a flat modifier, so an attacker with Piloting 7 versus a target with Piloting 4 would add a -3 to hit bonus. An attacker with Piloting 2 versus a target with piloting 6 would suffer a +4 to hit penalty. 

**Unsteady**: Attacks can apply the *Unsteady* state to a unit, dumping it's evasion pips and allowing it to be knocked down when it takes instability. There are three settings in the *Melee.Charge* portion of `mod.json` controlling when units gain unsteady:

* **UnsteadyAttackerOnHit** - if set to true, the *attacker* will be marked Unsteady on a *successful* charge
* **UnsteadyAttackerOnMiss**- if set to true, the *attacker* will be marked Unsteady on a *failed* charge
* **UnsteadyTargetOnHit**- if set to true, the *target* will be marked Unsteady on a *successful* charge

**Evasion Strip**: Attacks that target vehicles cannot apply the unsteady effect. Instead, they will remove one or more evasion pips. This value is controlled via the `DFA.TargetVehicleEvasionPipsRemoved` value, which defaults to 4 (evasion pips).

### Kick Attacks

Kick attacks inflict damage and instability on the target only. The calculation for damage and instability is the same, and follow this formula:

`finalDamage = RoundUP( ( (raw * attacker tonnage) + mod) * multi * actuatorMulti)`

The inputs for these values differ based upon configuration values (exposed through `mod.json`) and per-unit statistic values (added through status effects). 

| Input     | Source    | Damage Values                           | Instability Values                           |
| --------- | --------- | --------------------------------------- | -------------------------------------------- |
| **raw**   | mod.json  | `Melee.Kick.TargetDamagePerAttackerTon` | `Melee.Kick.TargetInstabilityPerAttackerTon` |
| **mod**   | statistic | `CBTBE_Kick_Target_Damage_Mod`          | `CBTBE_Kick_Target_Instability_Mod`          |
| **multi** | statistic | `CBTBE_Kick_Target_Damage_Multi`        | `CBTBE_Kick_Target_Instability_Multi`        |

*ActuatorMulti* is determined from the missing or damaged upper and lower leg actuators. For each damaged *LegUpperActuator* or  *LegLowerActuator* on the attacker, the target gains an attack penalty and a damage reduction. These values are set in `mod.json` as *Melee.Kick.LegActuatorDamageMalus* (default value: +2) and *Melee.Kick.LegActuatorDamageReduction* (default value: 0.5) respectively. Damage reduction is multiplicative, so if both leg actuators are damaged, the kick only does 25% of it's base damage (0.5 * 0.5 = 0.25 * base damage).

Damage is applied to the target as single hit that is randomized between the legs only. The distribution of these hit locations is provided below in the **Damage Table Reference**. 

**Reductions**: The damage and instability applied to the target will be modified the ``CBTBE_Kick_Target_Damage_Reduction_Multi` and `CBTBE_Kick_Target_Instability_Reduction_Multi` statistics. Both are ``System.Single` values (aka floats) that default to 1.0, and are read from the target that suffers the damage. The value of these are multiplied against the raw values generated by the formula above, before clustering is performed.

**Validations**: Before an attack can be made, several validation checks must be passed. If all of these checks do not pass, the attack is invalid and can't be selected by either player or AI.

* The target position must allow either *Tackle* or *Stomp* animations. These are generally allowed if the attacker midpoint is somewhere between the top and bottom of the target. 
* Both hip actuators must be undamaged
* The distance between attacker and target must be within the walk speed of the attacker

**Modifiers**: Charge attacks apply a handful of modifiers, in addition to the positional ones (side attack, rear attack, etc) normally added to attacks.

* **Easy to Kick** as per TT rules, Kicks are easy to deliver. They gain a flat attack bonus, defined in `mod.json` in the *Melee.Kick.BaseAttackBonus* setting, which defaults to -2. 
* **Leg Actuator Damage** is applied if the Upper or Lower leg actuators are damaged. The penalty is set in `mod.json` in the *Melee.Kick.LegActuatorDamageMalus* setting, and defaults to +2. This value is additive, so if both actuators are damaged the total modifier will be +4.
* **Foot Actuator Damage** is applied if the Foot actuator is damaged. The penalty is set in `mod.json` in the *Melee.Kick.FootActuatorDamageMalus* settings, which defaults to +1. 
* **Prone Target** is applied when the target is a mech, and they have been knocked down. The attacker gains a bonus defined in the *Melee.ProneTargetAttackModifier* configuration of `mod.json`. This defaults to -2.

**Unsteady**: Attacks can apply the *Unsteady* state to a unit, dumping it's evasion pips and allowing it to be knocked down when it takes instability. There are three settings in the Melee.Kick portion of `mod.json` controlling when units gain unsteady:

* **UnsteadyAttackerOnHit** - if set to true, the *attacker* will be marked Unsteady on a *successful* hit
* **UnsteadyAttackerOnMiss**- if set to true, the *attacker* will be marked Unsteady on a *failed* hit
* **UnsteadyTargetOnHit**- if set to true, the *target* will be marked Unsteady on a *successful* hit

### Physical Weapon Attacks

Physical attacks inflict damage and instability on the target only. The calculation for damage and instability is the same, and follow this formula:

`finalDamage = RoundUP( ( (raw * attacker tonnage) + mod) * multi)`

The inputs for these values differ based upon configuration values (exposed through `mod.json`) and per-unit statistic values (added through status effects). 


| Input     | Source    | Damage Value                                           | Instability Value                                           |
| --------- | --------- | ------------------------------------------------------ | ----------------------------------------------------------- |
| **raw**   | mod.json  | `Melee.PhysicalWeapon.DefaultDamagePerAttackerTon`     | `Melee.PhysicalWeapon.DefaultInstabilityPerAttackerTon`     |
|           | statistic | `CBTBE_Physical_Weapon_Target_Damage_Per_Attacker_Ton` | `CBTBE_Physical_Weapon_Target_Instability_Per_Attacker_Ton` |
| **mod**   | statistic | `CBTBE_Physical_Weapon_Target_Damage_Mod`              | `CBTBE_Physical_Weapon_Target_Instability_Mod`              |
| **multi** | statistic | `CBTBE_Physical_Weapon_Target_Damage_Multi`            | `CBTBE_Physical_Weapon_Target_Instability_Multi`            |

:information_source: Default values are used when per-unit statistics are not present. 

Damage is applied to the target as single hit that is randomized between the arms, torsos, and head of the target. The distribution of these hit locations is provided below in the **Damage Table Reference** (see below) and typically uses the Standard table. The *CBTBE_Physical_Weapon_Location_Table* may be set to one of PUNCH, KICK, or STANDARD to indicate that an alternative table should be used.

**Reductions**: The damage and instability applied to the target will be modified the ``CBTBE_Physical_Weapon_Target_Damage_Reduction_Multi` and `CBTBE_Physical_Weapon_Target_Instability_Reduction_Multi` statistics. Both are ``System.Single` values (aka floats) that default to 1.0, and are read from the target that suffers the damage. The value of these are multiplied against the raw values generated by the formula above, before clustering is performed.

**Animations**: HBS implemented weapon attacks as a unit's punch animation. When they determine if a Punch animation can be used, they validate that the 'punching' arm is available. The punching arm is defined in the *chassisDef* as the `PunchesWithLeftArm` field. If true, and the unit's left arm has been completely destroyed, it can no longer perform the punch animation. This will prevent it from performing attacks that rely upon the punch animation (punch, physical weapons).

**Validations**: Before an attack can be made, several validation checks must be passed. If all of these checks do not pass, the attack is invalid and can't be selected by either player or AI.

* The unit must have a physical weapon, as denoted by the `CBTBE_Punch_Is_Physical_Weapon` statistic being set to true.
* The target position must allow the *Punch* animation. This is generally allowed if the attacker midpoint no lower than the target midpoint and the attacker's top point is no higher than the target's highest point. This is required as HBS animations for physical weapons use the punch animation (see above)
* At least one arm needs a functional Shoulder Actuator *and* a functional Hand actuator
* The distance between attacker and target must be within the walk speed of the attacker

**Modifiers**: Charge attacks apply a handful of modifiers, in addition to the positional ones (side attack, rear attack, etc) normally added to attacks.

* **Arm Actuator Damage** is applied if the Upper or Lower arm actuators are damaged. The penalty is set in `mod.json` in the *Melee.Punch.ArmActuatorDamageMalus* setting, and defaults to +2. This value is additive, so if both actuators are damaged the total modifier will be +4.
* **Prone Target** is applied when the target is a mech, and they have been knocked down. The attacker gains a bonus defined in the *Melee.ProneTargetAttackModifier* configuration of `mod.json`. This defaults to -2.
* **Attack Modifier** is applied is the `CBTBE_Physical_Weapon_Attack_Mod` statistic is set on the attacking unit. 

**Unsteady**: Attacks can apply the *Unsteady* state to a unit, dumping it's evasion pips and allowing it to be knocked down when it takes instability. There are three settings in the *Melee.Punch* portion of `mod.json` controlling when units gain unsteady:

* **DefaultUnsteadyAttackerOnHit** - if set to true, the *attacker* will be marked Unsteady on a *successful* hit
  * The statistic *CBTBE_Physical_Weapon_Unsteady_Attacker_On_Hit* override this behavior on a unit by unit basis.
* **DefaultUnsteadyAttackerOnMiss**- if set to true, the *attacker* will be marked Unsteady on a *failed* hit
  * The statistic *CBTBE_Physical_Weapon_Unsteady_Attacker_On_Miss* overrides this behavior on a unit by unit basis.
* **DefaultUnsteadyTargetOnHit**- if set to true, the *target* will be marked Unsteady on a *successful* hit
  * The statistic *CBTBE_Physical_Weapon_Unsteady_Target_On_Hit* overrides this behavior on a unit by unit basis.

### Punch Attacks

Punch attacks inflict damage and instability on the target only. The calculation for damage and instability is the same, and follow this formula:

`finalDamage = RoundUP( ( (raw * attacker tonnage) + mod) * multi * actuatorMulti)`

The inputs for these values differ based upon configuration values (exposed through `mod.json`) and per-unit statistic values (added through status effects). 

*Damage Inputs*

| Input     | Source    | Damage Values                            | Instability Values                            |
| --------- | --------- | ---------------------------------------- | --------------------------------------------- |
| **raw**   | mod.json  | `Melee.Punch.TargetDamagePerAttackerTon` | `Melee.Punch.TargetInstabilityPerAttackerTon` |
|           | statistic | `CBTBE_Punch_Target_Damage_Per_Attacker_Ton` | `CBTBE_Punch_Target_Damage_Per_Attacker_Ton` |
| **mod**   | statistic | `CBTBE_Punch_Target_Damage_Mod`          | `CBTBE_Punch_Target_Instability_Mod`          |
| **multi** | statistic | `CBTBE_Punch_Target_Damage_Multi`        | `CBTBE_Punch_Target_Instability_Multi`        |

*ActuatorMulti* is determined from the missing or damaged upper and lower arm actuators. For each damaged *ArmUpperActuator* or  *ArmLowerActuator* on the attacker, the target gains an attack penalty and a damage reduction. These values are set in `mod.json` as *Melee.Punch.ArmActuatorDamageMalus* (default value: +2) and *Melee.Punch.ArmActuatorDamageReduction* (default value: 0.5) respectively. Damage reduction is multiplicative, so if both leg actuators are damaged, the kick only does 25% of it's base damage (0.5 * 0.5 = 0.25 * base damage).

Damage is applied to the target as single hit that is randomized between the arms, torsos, and head of the target. The distribution of these hit locations is provided below in the **Damage Table Reference** (see below). 

**Reductions**: The damage and instability applied to the target will be modified the ``CBTBE_Punch_Target_Damage_Reduction_Multi` and `CBTBE_Punch_Target_Instability_Reduction_Multi` statistics. Both are ``System.Single` values (aka floats) that default to 1.0, and are read from the target that suffers the damage. The value of these are multiplied against the raw values generated by the formula above, before clustering is performed.

**Animations**: HBS implemented weapon attacks as a unit's punch animation. When they determine if a Punch animation can be used, they validate that the 'punching' arm is available. The punching arm is defined in the *chassisDef* as the `PunchesWithLeftArm` field. If true, and the unit's left arm has been completely destroyed, it can no longer perform the punch animation. This will prevent it from performing attacks that rely upon the punch animation (punch, physical weapons).

**Validations**: Before an attack can be made, several validation checks must be passed. If all of these checks do not pass, the attack is invalid and can't be selected by either player or AI.

* The target position must allow the *Punch* animation. This is generally allowed if the attacker midpoint no lower than the target midpoint and the attacker's top point is no higher than the target's highest point.
* At least one arm needs a functional Shoulder Actuator
* The distance between attacker and target must be within the walk speed of the attacker

**Modifiers**: Charge attacks apply a handful of modifiers, in addition to the positional ones (side attack, rear attack, etc) normally added to attacks.

* **Arm Actuator Damage** is applied if the Upper or Lower arm actuators are damaged. The penalty is set in `mod.json` in the *Melee.Punch.ArmActuatorDamageMalus* setting, and defaults to +2. This value is additive, so if both actuators are damaged the total modifier will be +4.
* **Hand Actuator Damage** is applied if the Hand actuator is damaged. The penalty is set in `mod.json` in the *Melee.Punch.HandActuatorDamageMalus* settings, which defaults to +1. 
* **Prone Target** is applied when the target is a mech, and they have been knocked down. The attacker gains a bonus defined in the *Melee.ProneTargetAttackModifier* configuration of `mod.json`. This defaults to -2.

**Unsteady**: Attacks can apply the *Unsteady* state to a unit, dumping it's evasion pips and allowing it to be knocked down when it takes instability. There are three settings in the *Melee.Punch* portion of `mod.json` controlling when units gain unsteady:

* **UnsteadyAttackerOnHit** - if set to true, the *attacker* will be marked Unsteady on a *successful* hit
* **UnsteadyAttackerOnMiss**- if set to true, the *attacker* will be marked Unsteady on a *failed* hit
* **UnsteadyTargetOnHit**- if set to true, the *target* will be marked Unsteady on a *successful* hit

### Custom Components Reference

Several melee attacks reduce damage or have increased penalties if a BattleMech's actuators are damaged. This mod uses [Custom Components](https://github.com/battletechmodders/customcomponents/) categories to identity installed equipment as one of these category types. This mapping is defined in `mod.json` in the *CustomCategories* block. The following values are the common category ids shared by RogueTech and BattleTech Advanced 3062. If your modpack uses a different set of values you'll need to update the mappings to allow detection of that equipment.

| MechEngineer Category Id | Notes                                                        |
| ------------------------ | ------------------------------------------------------------ |
| LegHip                   | A mech must have undamaged hip actuators to kick.            |
| LegUpperActuator         | A damaged leg actuator adds a +2 penalty to kicks attacks. It also reduces kick damage by 50%, with multiple modifiers being cumulative. |
| LegLowerActuator         | A damaged leg actuator adds a +2 penalty to kicks attacks. It also reduces kick damage by 50%, with multiple modifiers being cumulative. |
| LegFootActuator          |                                                              |
| ArmShoulder              | A mech must have undamaged shoulder actuators to punch or make physical weapon attacks. |
| ArmUpperActuator         | A damaged arm actuator adds a +2 penalty to punches and physical weapon attacks. It also reduces punch damage by 50%, with multiple modifiers being cumulative. |
| ArmLowerActuator         | A damaged arm actuator adds a +2 penalty to punches and physical weapon attacks. It also reduces punch damage by 50%, with multiple modifiers being cumulative. |
| ArmHandActuator          | A mech must have undamaged hand actuators to make physical weapon attacks. Damaged hand actuators reduce punch accuracy by +1. |

### Damage Table Reference

Some attacks use a non-standard attack table. These are provided below for your convenience.

#### Kick Table

| Location  | Chance to Hit |
| --------- | ------------- |
| Left Leg  | 50%           |
| Right Leg | 50%           |

#### Punch Table
| Attack Direction | Location     | Chance to Hit |
| ---------------- | ------------ | ------------- |
| Front            | Left Arm     | 17%           |
|                  | Left Torso   | 17%           |
|                  | Center Torso | 16%           |
|                  | Right Torso  | 17%           |
|                  | Right Arm    | 17%           |
|                  | Head         | 16%           |
| |  |  |
| Rear             | Left Arm     | 17%           |
|                  | Left Rear Torso   | 17%           |
|                  | Center Rear Torso | 16%           |
|                  | Right Rear Torso  | 17%           |
|                  | Right Arm    | 17%           |
|                  | Head         | 16%           |
| |  |  |
| Left             | Left Arm     | 34%           |
|                  | Left Torso   | 34%           |
|                  | Center Torso | 16%           |
|                  | Head         | 16%           |
| |  |  |
| Right            | Right Arm    | 34%           |
|                  | Right Torso  | 34%           |
|                  | Center Torso | 16%           |
|                  | Head         | 16%           |

#### Standard Tables

These are defined in `StreamingAssets\data\constants\CombatGameConstants.json`. Individual modpacks may have overwritten these values. 

| Attack Direction | Location     | Chance to Hit |
| ---------------- | ------------ | ------------- |
| Front            | Left Leg     | 8%          |
|                  | Left Arm     | 10%          |
|                  | Left Torso   | 14%          |
|                  | Center Torso | 16%           |
|                  | Right Torso  | 14%          |
|                  | Right Arm    | 10%          |
|                  | Right Leg    | 8%          |
|                  | Head         | 1%           |
| |  |  |
| Rear             | Left Leg     | 5%          |
|                  | Left Arm     | 4%          |
|                  | Left Torso   | 14%          |
|                  | Center Torso | 16%           |
|                  | Right Torso  | 14%          |
|                  | Right Arm    | 4%          |
|                  | Right Leg    | 5%          |
|                  | Head         | 0%           |
| |  |  |
| Prone            | Left Leg     | 8%          |
|                  | Left Arm     | 8%          |
|                  | Left Torso   | 16%          |
|                  | Center Torso | 32%           |
|                  | Right Torso  | 16%          |
|                  | Right Arm    | 8%          |
|                  | Right Leg    | 8%          |
|                  | Head         | 1%           |
| |  |  |
| Left             | Left Leg     | 28%          |
|                  | Left Arm     | 28%          |
|                  | Left Torso   | 28%          |
|                  | Center Torso | 4%           |
|                  | Right Torso  | 0%          |
|                  | Right Arm    | 0%          |
|                  | Right Leg    | 0%          |
|                  | Head         | 1%           |
| |  |  |
| Right            | Left Leg     | 0%          |
|                  | Left Arm     | 0%          |
|                  | Left Torso   | 0%          |
|                  | Center Torso | 4%           |
|                  | Right Torso  | 28%          |
|                  | Right Arm    | 28%          |
|                  | Right Leg    | 28%          |
|                  | Head         | 1%           |
| |  |  |

### Melee Statistics Reference

All statistics used in melee values are listed below. See the relevant section for more details on their use.

| Statistic Name | Type | Notes |
| -------------- | ---- | ----- |
| **CHARGE STATISTICS** |  |  |
| CBTBE_Charge_Attack_Mod | System.Int32 |  |
| |||
| CBTBE_Charge_Attacker_Damage_Mod | System.Int32 ||
| CBTBE_Charge_Attacker_Damage_Multi | System.Single |value must be >= 0|
| CBTBE_Charge_Attacker_Instability_Mod | System.Int32 ||
| CBTBE_Charge_Attacker_Instability_Multi | System.Single |value must be >= 0|
| |||
| CBTBE_Charge_Target_Damage_Mod | System.Int32 ||
| CBTBE_Charge_Target_Damage_Multi | System.Single |value must be >= 0|
| CBTBE_Charge_Target_Instability_Mod | System.Int32 ||
| CBTBE_Charge_Target_Instability_Multi | System.Single |value must be >= 0|
| |||
| CBTBE_Charge_Target_Damage_Reduction_Multi | System.Single | value must be >= 0, default 1.0 |
| CBTBE_Charge_Target_Instability_Reduction_Multi | System.Single | value must be >= 0, default 1.0 |
| |||
| **DEATH FROM ABOVE STATISTICS** |  ||
| CBTBE_DFA_Attack_Mod | System.Int32 ||
| |||
| CBTBE_DFA_Attacker_Damage_Mod | System.Int32 ||
| CBTBE_DFA_Attacker_Damage_Multi | System.Single |value must be >= 0|
| CBTBE_DFA_Attacker_Instability_Mod | System.Int32 ||
| CBTBE_DFA_Attacker_Instability_Multi | System.Single |value must be >= 0|
| |||
| CBTBE_DFA_Target_Damage_Mod | System.Int32 ||
| CBTBE_DFA_Target_Damage_Multi | System.Single |value must be >= 0|
| CBTBE_DFA_Target_Instability_Mod | System.Int32 ||
| CBTBE_DFA_Target_Instability_Multi | System.Single |value must be >= 0|
| |||
| CBTBE_DFA_Target_Damage_Reduction_Multi | System.Single | value must be >= 0, default 1.0 |
| CBTBE_DFA_Target_Instability_Reduction_Multi | System.Single | value must be >= 0, default 1.0 |
| |||
| **KICK STATISTICS** |  ||
| CBTBE_Kick_Attack_Mod | System.Int32 ||
| CBTBE_Kick_Extra_Hits_Count | System.Single ||
| |||
| CBTBE_Kick_Target_Damage_Mod | System.Int32 ||
| CBTBE_Kick_Target_Damage_Multi | System.Single |value must be >= 0|
| CBTBE_Kick_Target_Instability_Mod | System.Int32 ||
| CBTBE_Kick_Target_Instability_Multi | System.Single |value must be >= 0|
| |||
| CBTBE_Kick_Target_Damage_Reduction_Multi | System.Single | value must be >= 0, default 1.0 |
| CBTBE_Kick_Target_Instability_Reduction_Multi | System.Single | value must be >= 0, default 1.0 |
| |||
| **PUNCH STATISTICS** |  ||
| CBTBE_Punch_Attack_Mod | System.Int32 ||
| CBTBE_Punch_Extra_Hits_Count | System.Single ||
| |||
| CBTBE_Punch_Target_Damage_Mod | System.Int32 ||
| CBTBE_Punch_Target_Damage_Multi | System.Single |value must be >= 0|
| CBTBE_Punch_Target_Instability_Mod | System.Int32 ||
| CBTBE_Punch_Target_Instability_Multi | System.Single |value must be >= 0|
| |||
| CBTBE_Punch_Target_Damage_Reduction_Multi | System.Single | value must be >= 0, default 1.0 |
| CBTBE_Punch_Target_Instability_Reduction_Multi | System.Single | value must be >= 0, default 1.0 |
| |||
| **PHYSICAL WEAPON STATISTICS** |  ||
| CBTBE_Punch_Is_Physical_Weapon | System.Boolean ||
| CBTBE_Physical_Weapon_Location_Table | System.String |value must be one of PUNCH, KICK, STANDARD|
| CBTBE_Physical_Weapon_Attack_Mod | System.Int32 ||
| CBTBE_Physical_Weapon_Extra_Hits_Count | System.Single ||
| |||
| CBTBE_Physical_Weapon_Unsteady_Attacker_On_Hit | System.Boolean ||
| CBTBE_Physical_Weapon_Unsteady_Attacker_On_Miss | System.Boolean ||
| CBTBE_Physical_Weapon_Unsteady_Target_On_Hit | System.Boolean ||
| |||
| CBTBE_Physical_Weapon_Target_Damage_Per_Attacker_Ton | System.Single |value must be > 0|
| CBTBE_Physical_Weapon_Target_Damage_Mod | System.Int32 ||
| CBTBE_Physical_Weapon_Target_Damage_Multi | System.Single |value must be >= 0|
| |||
| CBTBE_Physical_Weapon_Target_Instability_Per_Attacker_Ton | System.Single |value must be > 0|
| CBTBE_Physical_Weapon_Target_Instability_Mod | System.Int32 ||
| CBTBE_Physical_Weapon_Target_Instability_Multi | System.Single |value must be >= 0|
| |||
| CBTBE_Physical_Weapon_Target_Damage_Reduction_Multi | System.Single | value must be >= 0, default 1.0 |
| CBTBE_Physical_Weapon_Target_Instability_Reduction_Multi | System.Single | value must be >= 0, default 1.0 |


## Classic Movement

CBT Movement is an attempt to bring Classic BattleTech Tabletop movement rules flavor into HBS's BATTLETECH game. Features include:

- Sprinting no longer ends the turn
- Evasion is no longer removed after attacks
- Any movement now incurs a +1 ToHit Penalty
- Sprinting incurs an additional +1 ToHit Penalty
- Jumping incurs an additional +2 ToHit Penalty
- ToHit modifiers are allowed to go below your base to hit chance, making something easier to hit if you stack you modifiers right
- If you are legged, you are dropped to 1 MP

The way movement currently works in the game is that ToHitSelfWalk modifiers are applied whenever you make any movement. So Sprinting, for example, will have a +1 for movement and an additional +1 for sprinting, bringing it in line with the original Tabletop rules of +2. The same applies to the Jump ToHit Modifiers. 

### Leg Damage

In CBT when a unit has damaged legs, it becomes difficult for them to remain upright when running or jumping. In CBTBE this penalty is implemented, and units that have actuator damage are forced to make a piloting skill check or suffer a knockdown. 

CBTBE reads a integer statistic named **CBTBE_ActuatorDamage_Malus**. If this value is non-zero, it assumes the unit has actuator damage and should test on a sprint or jump. At the end of a movement sequence, the unit's Piloting skill value is multiplied by `Move.SkillMulti` and a random check between 0 and 100 is made. If the random check plus the skill multiplier result is less than `Move.FallAfterRunChance`, the unit will be knocked down with all the normal effects (such as pilot injury, fall damage, etc). For jumps the total must be less than `Move.FallAfterJumpChance` instead, but otherwise works the same way.

Any Mech that is missing a Leg entirely is reduced to 1 MP of movement, as per CBT rules. See 'Run and Walk MP' below for how that translates into in-engine distances.

:information_source: HBS BT doesn't track actuator damage, but they are provided through MechEngineer. You're encouraged to apply ****CBTBE_ActuatorDamage_Malus** through a [ME Critical effect](https://github.com/BattletechModders/MechEngineer/blob/master/source/Features/CriticalEffects/CriticalEffectsSettings.cs). A sample of such an effect is provided below for example purposes:

```json
"Settings": [
    {
      "durationData": {
        "duration": -1
      },
      "targetingData": {
        "effectTriggerType": "Passive",
        "effectTargetType": "Creator",
        "showInTargetPreview": true,
        "showInStatusPanel": true
      },
      "effectType": "StatisticEffect",
      "Description": {
        "Id": "CriticalEffect-HipDestroyed-{location}-pilot-pen",
        "Name": "Hip Destroyed Piloting Penalty",
        "Details": "Hit has been destroyed making piloting checks harder to pass",
        "Icon": "uixSvgIcon_equipment_ActuatorLeg"
      },
      "statisticData": {
        "statName": "CBTBE_ActuatorDamage_Malus",
        "operation": "Int_Subtract",
        "modValue": "1",
        "modType": "System.Single"
      },
      "nature": "Debuff"
    },
```

You can see RogueTech's implementation in their various [ME critical effects](https://github.com/BattletechModders/RogueTech/tree/master/RogueTech Core/criticalEffects).

### Walk and Run MP

HBS BattleTech allows engines to contribute small fractions of movements to units. In CBT rules, a unit's walk MP times tonnage determines the engine rating. In HBSBT the same calculation applies, but fractional engines apply fractions of movement. This is done because the in-game battlefield is represented in meters, not hexes, and thus small fractions of a engine can allow slightly more movement. In HBSBT this doesn't matter as engines are fixed, and speeds are supplied as fixed JSON values. When [MechEngineer](https://github.com/battletechmodders/mechengineer) entered the picture it created some odd behaviors.

[MechEngineer recently added](https://github.com/BattletechModders/MechEngineer/blob/3ce0f0a9f58e68f0faf079184ae708981a6ea14d/source/Features/Engines/EngineSettings.cs#L29) a new flag that enforces strict rounding for walk and run MP based upon CBT rules. Because CBTBE applies heat penalties, we also apply this logic to movement. In order to do so, we need to rationalize from movement-as-meters to movement-as-MP. The value `MoveOptions.MetersPerHex` is the factor used to reduce movement-as-meters to movement-as-MP. It *currently* defaults to 24, but ideally is kept the same as ME's [MovementPointDistanceMultiplier](https://github.com/BattletechModders/MechEngineer/blob/3ce0f0a9f58e68f0faf079184ae708981a6ea14d/source/Features/Engines/EngineSettings.cs#L32). 

:information_source: This value defaults to 24, because the HBS engine defaults a Hex width to 24.0 in `HexGrid.HexWidth`. However, this distance does not account for vertical changes in the hex. Each hex is comprised of multiple 5m cells, and thus around 6 cells represents one hex. You can have an elevation change of up to 0.8 from one side to the other giving a total linear distance moved of ~ 38m. Couple this with movement multipliers from *designMasks* (like forests) and it may be more appropriate to represent one MP as 40m instead of 24m. However, this change should be coordinated and both ME and CBTBE should change at the same time.

A unit's run speed is determined by it's walkMP times a factor, given as 1.5 in CBT rules. This factor is configurable as the `MoveOptions.RunMulti` in mod.json. All values are rounded up, thus a walkMP of 5 becomes a runMP of 8. These will be represented in-engine as walkSpeed 5 x 24m = 120m and runSpeed 8 x 24m = 192m.

This calculation can be further modified by a per-unit statistic. Any mech can be assigned the float stat **CBTBE_RunMultiMod**, which if present the value will additively modify the value of `RunMulti`. A **CBTBE_RunMultiMod** value of 0.6 would be added to the default 1.5, for a total multiplier of 2.1. This would give a unit with 5 walk MP a run MP of 11 (5 x 2.1 = 10.5, rounded up) for a total of 11 x 24m = 264m runSpeed.

### Classic Movement Configuration Summary

* `Move.SkillMulti` - a percentage value multiplied by the unit's Piloting skill. The result value is added to  all checks related to `FallAfterRunChance` and `FallAfterJumpChance`. 
* `Move.MetersPerHex` - the number of meters that equals one MP of movement. 
* `Move.RunMulti` - the base multiplier for calculating run MP from walk MP. Defaults to 1.5, as per CBT rules. All values are rounded up.
* `Move.FallAfterRunChance` - a percentage chance for a unit to fall after running, when it's leg actuators are damaged. Defaults to 0.30 (30%).
* `Move.FallAfterJumpChance` - a percentage chance for a unit to fall after it jumps, when its leg actuators are damaged. Defaults to 0.30 (30%).

## NOTES TODO ERRORS

* Heat Modifiers are applied dynamically; this means you can fire, raise your heat, and suddenly not be able to move. This should be fixed, but requires some state management.
* Disable `MechEngineer.Features.MoveMultiplierStat` - it will be completely ignored by the revamped heat-based movement logic
* ERROR: Problems in interleaved mode if you hit "OK" before everyone is done moving.
* ERROR: If mech does not move (just braces) it doesn't generate a heat sequence. Fix.
* ERROR: Heat calculations assume heat > 150. Why is heat > 150? Cap any heat at 150, period.
* TODO: AI should select most favorable melee attack
  * Check for head damage that a punch would eliminate
  * Check for leg damage that would result in a falldown
