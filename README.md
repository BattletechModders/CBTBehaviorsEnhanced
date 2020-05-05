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

## Classic Piloting

CBT Piloting attempts to bring Classic Battletech Tabletop piloting skill checks into HBS's BATTLETECH game. Currently only one check is implemented.

Whenever you mech becomes unstable, every hit that causes stability damage will cause a Piloting skill check. Currently the base difficulty is a flat 30%. On damage, the game will roll a random number and apply your piloting skill. Under 30% will cause a knockdown regardless of your total stability damage. The piloting skill percentage is calculated the way all skill check percentages are calculated in the game, which is to take your skill and divide it by the skill divisor ( Skill / PilotingDivisor). The default PilotingDivisor is 40. So for example, a piloting skill of 5 will add a 12.5% chance to the random roll. The default difficulty of 30% leaves a pilot with a skill of 10 a 5% chance of failure. I figured that was a good trade-off, since the CBT piloting skill checks always had a chance of failure no matter the skill level.

Difficulty percentage is configurable in the mod.json file.

### Piloting TODO 

* TODO: When standing, make a piloting check or fall over again
* TODO: When moving through certain terrain, add instability (water, sand, light jungle, heavy jungle, rubble) 

## Classic Melee

* On a kick, make a piloting check or fall down (source)
* On a missed kick, make a piloting check or fall down
* On a DFA attack, make a piloting check or fall down (source and target)
* On a missed DFA attack, automatically fall down
* On a charge attack, make a piloting check or fall down (source and target)
* Mitigate DFA self damage based upon piloting (reduce damage by 5% per level by default)
* TODO: Allow selection of melee type
* Allows moving to different melee positions (duplicate of MeleeMover by Morphyum)

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

* TODO: If you are shutdown, you auto-fail Piloting checks. Reflect this in Classic Piloting
* TODO: One destroyed leg means movement of 1 MP, no running, gains a +5 PSR modifier, jumping requires a PSR
* Heat Modifiers are applied dynamically; this means you can fire, raise your heat, and suddenly not be able to move. This should be fixed, but requires some state management.
* Disable `MechEngineer.Features.MoveMultiplierStat` - it will be completely ignored by the revamped heat-based movement logic
* TODO: Select melee style
* TODO: Punches should roll on punch table, kicks on kick table, etc
* ERROR: Problems in interleaved mode if you hit "OK" before everyone is done moving.
