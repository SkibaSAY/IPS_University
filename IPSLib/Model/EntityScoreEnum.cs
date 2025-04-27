using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IPSLib.EstimatesForecasting.EstimateSearcher
{
    /// <summary>
    /// Отражает интерес к  сущности в зависимости от действий пользователя
    /// </summary>
    public enum EntityScoreEnum
    {
        Hidden = 0,
        Unknown = 0,
        AutoSearch = 20,//отказался от исп автопоисков
        Famous = 80,//+
        MostView = 80,//+
        Application = 100,//+
    }
}
