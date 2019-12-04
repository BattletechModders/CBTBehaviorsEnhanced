# CBTBehaviors
This mod for the [HBS BattleTech](http://battletechgame.com/) game changes several game behaviors to be closer to the Table Top behaviors. A quick summary of these behaviors are as follows:

* Overheating no longer deals damage, but instead you have a chance to resist the shutdown and/or explode any ammo you have.
* Additional hits when you are unstable can cause a fall. This is resisted by your piloting skill.
* Evasion is no longer removed after attacks.
* Sprinting no longer ends your turn, and incurs a +1 ToHit penalty for attackers.
* Jumping incurs a +2 ToHit penalty for attackers.
* ToHit modifiers can be more effectively stacked to make hits more easily.

This mod directly copies [McFistyBuns'](https://github.com/McFistyBuns) excellent mods, which sadly are no longer updated:

* <https://github.com/McFistyBuns/CBTHeat>
* <https://github.com/McFistyBuns/CBTPiloting>
* <https://github.com/McFistyBuns/CBTMovement>

Credit for the code and text descriptions go to [McFistyBuns](https://github.com/McFistyBuns). He granted LadyAlekto (of RogueTech) fame the right to reuse his mods as she liked, which I've taken and implemented at her request. The code has been relicensed under the MIT license to honor that exchange.

**NOTICE**: This mod enforces combat turns at all times, to prevent some HBS bugs that cause issues in the transition from non-combat to combat states. This cannot be disabled as its essential to the mod's operation!

## Classic Heat

Based upon https://github.com/McFistyBuns/CBTHeat, this mod blends the HBS BattleTech and TableTop styles. Mechs no longer suffer damage from overheating. In exchange, your mech has a chance to shutdown and have any ammo explode each turn you are overheated past the first. You also have to-hit modifiers applied when you are at a high heat.

The chances are tied to the original _ClassicBattleTech (CBT)_ heat scale. The original CBT heat scale had 4 Shutdown roll chances and 4 Ammo Explosion chances as well as Heat modifiers to Hit. The chances (which were originally 2d6 rolls) have been converted  and applied them to the overheat mechanic of the game. The game will roll randomly for each percentage. Ammo Explosion results are applied first.

**NEW in v0.2.0**: Movement Modifiers Movement modifiers work the same way ToHit Modifiers. The movement modifier will increase the longer your mech is overheated. That means the longer you overheat, the less movement you will have available. This only affects walk and sprint movement. Jump is unaffected.

The way the game applies movement modifiers is that it adds up all the the modifiers, subtracts them from 1 to get what is essentially a percentage, and then multiplies that by your total movement. So that means, for example, on the first overheat turn, you will have 90% of your total movement available to use (assuming you don't have any other movement modifiers, like a missing leg).

| Rounds Overheated | Shutdown Chance | Ammo Explosion Chance | ToHit Modifier | Move Modifier |
| ----------------- | --------------- | --------------------- | -------------- | ------------- |
| 1                 | 8.3%            | 0                     | +1             | 0.1           |
| 2                 | 27.8%           | 8.3%                  | +2             | 0.2           |
| 3                 | 58.3%           | 27.8%                 | +3             | 0.3           |
| 4+                | 83.3%           | 58.3%                 | +4             | 0.4           |

These chances are also displayed in the Overheat notification badge above the heat bar. Bringing your heat bar all the way to shutdown still shuts the mech down.

All chances and modifiers are configurable in the mod.json file.

## Classic Piloting

CBT Piloting attempts to bring Classic Battletech Tabletop piloting skill checks into HBS's BATTLETECH game. Currently only one check is implemented.

Whenever you mech becomes unstable, every hit that causes stability damage will cause a Piloting skill check. Currently the base difficulty is a flat 30%. On damage, the game will roll a random number and apply your piloting skill. Under 30% will cause a knockdown regradless of your total stability damage. The piloting skill percentage is calculated the way all skill check percentages are calculated in the game, which is to take your skill and divide it by the skill divisor ( Skill / PilotingDivisor). The default PilotingDivisor is 40. So for example, a piloting skill of 5 will add a 12.5% chance to the random roll. The default difficulty of 30% leaves a pilot with a skill of 10 a 5% chance of failure. I figured that was a good trade-off, since the CBT piloting skill checks always had a chance of failure no matter the skill level.

Difficulty percentage is configurable in the mod.json file.

## Classic Movement 

CBT Movement is an attempt to bring Classic Battletech Tabletop movement rules flavor into HBS's BATTLETECH game. Features include:

- Sprinting no longer ends the turn
- Evasion is no longer removed after attacks
- Any movement now incurs a +1 ToHit Penalty
- Sprinting incurs an addtional +1 ToHit Penalty
- Jumping incurs an additional +2 ToHit Penalty
- ToHit modifiers are allowed to go below your base to hit chance, making something easier to hit if you stack you modifiers right

The way movement currently works in the game is that ToHitSelfWalk modifiers are applied whenever you make any movement. So Sprinting, for example, will have a +1 for movement and an additional +1 for sprinting, bringing it in line with the original Tabletop rules of +2. The same applies to the Jump ToHit Modifiers.

## NOTES

* If you are shutdown, you auto-fail Piloting checks. Reflect this in Classic Piloting
* One destroyed leg means movement of 1 MP, no running, gains a +5 PSR modifier, jumping requires a PSR

