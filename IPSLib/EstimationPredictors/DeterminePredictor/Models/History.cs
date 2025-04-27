using IPSLib.EstimationPredictors.DeterminePredictors;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.ConstrainedExecution;
using System.Text;
using System.Threading.Tasks;

namespace IPSLib.EstimationPredictors.DeterminePredictor.Models
{
    public class History
    {
        public List<HistoryItem> Items = new List<HistoryItem>();

        private Dictionary<string, HistoryItem> _items = new Dictionary<string, HistoryItem>();
        private Dictionary<string, HistoryItem> _Items
        {
            get
            {
                //заполняем в самом начале, после диссериализации
                if (_items.Count != Items.Count)
                {
                    _items.Clear();
                    Items.ForEach(item => { _items.Add(item.Key, item); });
                }
                return _items;
            }
        }
        public History()
        {

        }

        public long Count => Items.Sum(item=>item.Count);
        public History(List<HistoryItem> items)
        {
            if(items == null)
            {
                Items = new List<HistoryItem>();
            }
            Items.ForEach(item => { _Items.Add(item.Key, item); });
        }

        public void Add(string key, double estimation)
        {
            if (_Items.TryGetValue(key, out HistoryItem historyItem))
            {
                historyItem.AddEstimate(estimation);
            }
            else
            {
                var newValue = new HistoryItem(key: key);
                newValue.AddEstimate(estimation);

                _Items.Add(key, newValue);
                Items.Add(newValue);
            }
        }
        public void Remove(string key)
        {
            var removeItem = _items[key];
            Items.Remove(removeItem);
            _items.Remove(key);
        }
        public bool TryGetByKey(string key, out HistoryItem historyItem)
        {
            return _Items.TryGetValue(key,out historyItem);
        }

        /// <summary>
        /// Удаляет из истории элементы, которое отклоняются от среднего в меньшую сторону
        /// </summary>
        public void DropStrangeItems()
        {
            if(Count == 0)
            {
                return;
            }

            var percent = 0.2;
            //эмпирически подобрано, меньше ставить не нужно,
            //иначе не будут удалять данные по 1 и 2 элемента - мусорные данные
            long max = Items.Max(item=>item.Count);

            if(Items.Count > 3)
            {
                //есть смысл брать не первый макс, а второй
                var temp = Items.Select(item => item.Count).ToList();
                temp.Sort();
                temp.Reverse();
                max = temp.ElementAtOrDefault(1);
            }
            
            //Отсеиваем мусор
            //например: если у пользователя 100 тендеров, и в каждом разный заказчик - это нужно исключать          
            if(max > 100)
            {
                //5%
                percent = 0.1;
            }

            
            foreach(var kvp in _items)
            {
                var cur = kvp.Value;
                var key = kvp.Key;
                var currCount = cur.Count;

                //значит пользователь чаще участвовал в таких тендерах - не удаляем
                if(cur.IsNotNeedDelete)
                {
                    return;
                }

                if(Count < 50)
                {
                    return;
                }
                else if((double)currCount / max < percent)
                {
                    Remove(key);
                }
                //else if (currCount < 5)
                //{
                //    Remove(key);
                //}
            }
        }
    }
}
