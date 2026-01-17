using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IPSLib.Examples.MachineIDS
{
    /// <summary>
    /// Счётчик аномалий, проверяет, чтобы за N проверок, было не былее M аномалий
    /// </summary>
    public class AnomalyCounter
    {
        class QueueItem
        {
            public bool IsAnomaly;
            public DateTime CreateDate;
        }

        int TotalCount;
        int AnomalyCount;
        int MaxAnomalyCount;

        Queue<QueueItem> items;

        public AnomalyCounter(int totalCount = 60, int maxAnomalyCount = 10)
        {
            this.TotalCount = totalCount;
            this.Reset();
            this.MaxAnomalyCount = maxAnomalyCount;
        }

        public void Reset()
        {
            this.items = new Queue<QueueItem>();
            this.AnomalyCount = 0;
        }

        public bool IsLongAnomaly()
        {
            return this.AnomalyCount > this.MaxAnomalyCount;
        }

        private void PopIfNeed()
        {
            while (this.items.Count > TotalCount)
            {
                var item = this.items.Dequeue();
                if (item.IsAnomaly)
                {
                    this.AnomalyCount--;
                }
            }
        }

        public void Add(bool isAnomaly = false)
        {
            if (isAnomaly)
            {
                this.AnomalyCount++;
            }
            this.items.Enqueue(new QueueItem { IsAnomaly = isAnomaly, CreateDate = DateTime.Now });
            this.PopIfNeed();
        }
    }
}
