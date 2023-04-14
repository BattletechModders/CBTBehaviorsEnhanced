using HBS.Collections;

namespace CBTBehaviorsEnhanced.Extensions
{
    public static class TurretExtensions
    {

        public static float MeleeTonnage(this Turret turret)
        {
            float tonnage = Mod.Config.Melee.Turrets.DefaultTonnage;

            TagSet actorTags = turret.GetTags();
            if (actorTags != null && actorTags.Contains(ModConsts.Turret_Tag_Class_Light))
            {
                tonnage = Mod.Config.Melee.Turrets.LightTonnage;
            }
            else if (actorTags != null && actorTags.Contains(ModConsts.Turret_Tag_Class_Medium))
            {
                tonnage = Mod.Config.Melee.Turrets.LightTonnage;
            }
            else if (actorTags != null && actorTags.Contains(ModConsts.Turret_tag_Class_Heavy))
            {
                tonnage = Mod.Config.Melee.Turrets.LightTonnage;
            }

            return tonnage;
        }
    }
}
