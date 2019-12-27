# CBTBehaviors
This mod for the [HBS BattleTech](http://battletechgame.com/) game changes several behaviors to more closely emulate the TableTop BattleTech experience. A summary this mod's changes are:

* Overheating is completely revamped and no longer deals structural damage. Instead, you can be forced to shutdown, experience ammo explosions, pilot injuries, or critical hits, and experience significant movement and attacking penalties.
* When attacking, you suffer a +1 penalty if you walked, +2 if you sprinted, and +3 if you jumped.
* Sprinting no longer ends your turn.
* Evasion is not longer removed after attacks.
* Additional hits when you are unstable can cause a fall. This is resisted by your piloting skill.
* ToHit modifiers can be more effectively stacked to make hits more easily.

This mod was influenced by [McFistyBuns'](https://github.com/McFistyBuns) excellent mods, which sadly are no longer updated. His code was released without a license, but in a discussion with LadyAlekto (of RougeTech) he granted a right to re-use his mod as she liked. While most of the code has been replaced, portions of the original code remains and has been relicensed under the MIT license to honor that exchange.

This mod requires [https://github.com/iceraptor/IRBTModUtils/]. Grab the latest release of __IRBTModUtils__ and extract it in your Mods/ directory alongside of this mod.

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

* TODO: When standing, make a piloting check or fall over again
* TODO: When moving through certain terrain, add instability (water, sand, light jungle, heavy jungle, rubble) 
* TODO: When jumping with damaged leg/foot/hip actuator
* TODO: When jumping with destroyed leg
* TODO: When sprinting with damaged hip/gyro

## Classic Melee

* On a kick, make a piloting check or fall down (source)
* On a missed kick, make a piloting check or fall down
* On a DFA attack, make a piloting check or fall down (source and target)
* On a missed DFA attack, automatically fall down
* On a charge attack, make a piloting check or fall down (source and target)
* Mitigate DFA self damage based upon piloting (reduce damage by 5% per level by default)
* TODO: Allow selection of melee type

## Classic Movement

CBT Movement is an attempt to bring Classic Battletech Tabletop movement rules flavor into HBS's BATTLETECH game. Features include:

- Sprinting no longer ends the turn
- Evasion is no longer removed after attacks
- Any movement now incurs a +1 ToHit Penalty
- Sprinting incurs an additional +1 ToHit Penalty
- Jumping incurs an additional +2 ToHit Penalty
- ToHit modifiers are allowed to go below your base to hit chance, making something easier to hit if you stack you modifiers right
- If you are legged, you are dropped to 1 MP

The way movement currently works in the game is that ToHitSelfWalk modifiers are applied whenever you make any movement. So Sprinting, for example, will have a +1 for movement and an additional +1 for sprinting, bringing it in line with the original Tabletop rules of +2. The same applies to the Jump ToHit Modifiers.

## NOTES

* If you are shutdown, you auto-fail Piloting checks. Reflect this in Classic Piloting
* One destroyed leg means movement of 1 MP, no running, gains a +5 PSR modifier, jumping requires a PSR
* Heat Modifiers are applied dynamically; this means you can fire, raise your heat, and suddenly not be able to move. This should be fixed, but requires some state management.
* Disable `MechEngineer.Features.MoveMultiplierStat` - it will be completely ignored by the revamped heat-based movement logic
