using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace CBTBehaviorsEnhanced
{
    public static class StatValidators
    {
        public static bool CurrentHeatClampValidator<T>(ref int newValue)
        {
            newValue = Mathf.Clamp(newValue, 0, Mod.Config.Heat.MaxHeat);
            return true;
        }
    }
}
