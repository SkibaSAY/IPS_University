using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RecommendedTendersForUsers
{
    public static class GlobalSettings
    {
        /// <summary>
        /// Интервал от самого нового тендера, в рамках которого проводим оценки, иначе пропускаем
        /// </summary>
        public const long MaxDistanceToLastTenderId = 1000000;
    }
}
