using IPSLib.EstimationPredictors.DeterminePredictors;
using Nest;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace IPSLib.Examples.TelecomX
{
    class SessionStatistic
    {
        public static PropertyInfo[] Properties =(from x in typeof(SessionStatistic).GetProperties() select x).ToArray();
        public static PropertyInfo[] LearningProperties = Properties.Where(p => p.GetCustomAttribute(typeof(LearningAttribute)) != null).ToArray();
        /// <summary>
        /// View + Learning
        /// </summary>
        public static PropertyInfo[] ViewProperties;
        
        static SessionStatistic()
        {
            var viewProps = Properties.Where(p => p.GetCustomAttribute(typeof(ViewAttribute)) != null).ToList();
            viewProps.AddRange(LearningProperties);
            ViewProperties = viewProps.ToArray();
        }
        public SessionStatistic() { }
        public SessionStatistic(string userId, DateTime roundedDate)
        {
            this.UserId = userId;
            this.RoundedDate = roundedDate;
        }

        [ViewAttribute]
        public string UserId { get; set; }
        
        [ViewAttribute]
        public DateTime RoundedDate { get; set; }


        [LearningAttribute]
        public Single SessionCount { get; set; }

        [LearningAttribute]
        public Single TotalDuration { get; set; }

        [LearningAttribute]
        public Single MedianDuration { get; set; }

        [LearningAttribute]
        public Single MaxDuration { get; set; }


        // TODO: рассмотреть возможность брать медиану
        // Количество Переданных(Up) бит
        [LearningAttribute]
        public Single TotalTransmittedBit { get; set; }

        [LearningAttribute]
        public Single MedianTransmittedBit { get; set; }

        [LearningAttribute]
        public Single MaxTransmittedBit { get; set; }

        // Количество Полученных(Down) бит
        [LearningAttribute]
        public Single TotalReceivedBit { get; set; }

        [LearningAttribute]
        public Single MedianReceivedBit { get; set; }

        [LearningAttribute]
        public Single MaxReceivedBit { get; set; }

        /// <summary>
        /// Сливает только статистические данные
        /// </summary>
        /// <param name="additional"></param>
        public void Merge(SessionStatistic additional)
        {
            //TODO: избавиться от копипаста
            this.MaxDuration = Math.Max(this.MaxDuration, additional.MaxDuration);
            this.MedianDuration = GetMergedMedian(this.MedianDuration, this.SessionCount, additional.MedianDuration, additional.SessionCount);
            this.TotalDuration += additional.TotalDuration;

            this.MaxTransmittedBit = Math.Max(this.MaxTransmittedBit, additional.MaxTransmittedBit);
            this.MedianTransmittedBit = GetMergedMedian(this.MedianTransmittedBit, this.SessionCount, additional.MedianTransmittedBit, additional.SessionCount);
            this.TotalTransmittedBit += additional.TotalTransmittedBit;

            this.MaxReceivedBit = Math.Max(this.MaxReceivedBit, additional.MaxReceivedBit);
            this.MedianReceivedBit = GetMergedMedian(this.MedianReceivedBit, this.SessionCount, additional.MedianReceivedBit, additional.SessionCount);
            this.TotalReceivedBit += additional.TotalReceivedBit;

            this.SessionCount += additional.SessionCount;
        }

        /// <summary>
        /// Настоящую медиану пока себе не позволяем - неудобно хранить всё
        /// </summary>
        /// <param name="medianA"></param>
        /// <param name="countA"></param>
        /// <param name="medianB"></param>
        /// <param name="countB"></param>
        /// <returns>Средняя медиана</returns>
        private Single GetMergedMedian(Single medianA, Single countA, Single medianB, Single countB)
        {
            return (medianA * countA + medianB * countB) / (countA + countB);
        }
    }
}
