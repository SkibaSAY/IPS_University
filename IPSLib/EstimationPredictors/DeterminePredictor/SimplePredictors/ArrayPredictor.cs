using IPSLib.EstimatesForecasting.EstimateSearcher;
using IPSLib.EstimationPredictors.DeterminePredictor.Models;
using IPSLib.EstimationPredictors.DeterminePredictors;
using IPSLib.Model;
using Microsoft.Data.Analysis;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace IPSLib.EstimationPredictors.DeterminePredictor.SimplePredictors
{
    public class ArrayPredictor<TKey> : PredictorBase
    {
        public ArrayPredictor():base()
        {

        }
        /// <summary>
        ///  Число элементов в группе сочетаний = С к по n
        /// </summary>
        public int MaxItemsInGroup;
        public int MinItemsInGroup;

        public ArrayPredictor(string targetPropName, int maxItemsInGroup = 3, int minItemsInGroup = 1) : base(targetPropName)
        {
            MaxItemsInGroup = maxItemsInGroup;
            MinItemsInGroup = minItemsInGroup;
        }
        protected virtual List<TKey> PrepareTargetArray(DataFrameRow entity)
        {
            return (GetTargetValue(entity) as IEnumerable<TKey>).ToList();
        }

        /// <summary>
        /// Возвращает самую большую оценку, найденную по самой большой группе элементов
        /// </summary>
        /// <param name="estimate"></param>
        /// <returns></returns>
        public override PredictInfo Estimate(DataFrameRow entity)
        {
            var targetList = PrepareTargetArray(entity);

            var keysGroups = PrepareKeyGroupsByEstimate(targetList);
            //По убыванию длины группы ключей
            keysGroups = keysGroups.OrderByDescending(g=>g.Count).ToList();

            //получаем максимальную оценку по самой большой найденной группе
            var selectedKeysGroupCount = 0;
            EstimateKf selectedKf = EstimateKf.Default();
            var selectedKey = "";
            foreach(var keysGroup in keysGroups)
            {
                var key = CreateKey(keysGroup);
                if(History.TryGetByKey(key, out HistoryItem historyItem))
                {
                    //отыскали группу той же длины с лучшим кф
                    if(selectedKeysGroupCount <= keysGroup.Count && selectedKf.Value < historyItem.EstimateKf.Value)
                    {
                        selectedKeysGroupCount = keysGroup.Count;
                        selectedKf = historyItem.EstimateKf;
                        selectedKey = key;
                    }
                }
            }

            return new PredictInfo {Kf = selectedKf, Predictor = this, TargetKey = selectedKey };
        }

        public override bool Filter(DataFrameRow fieldToEstimate)
        {
            var targetList = PrepareTargetArray(fieldToEstimate);
            return FilterList(targetList);
        }

        protected bool FilterList(List<TKey> targetList)
        {
            Predicate<TKey> customFilter = null;
            if (typeof(TKey) == typeof(string))
            {
                customFilter = (k => !String.IsNullOrEmpty(k.ToString()));
            }
            else
            {
                customFilter = (k => !k.ToString().Equals("0"));
            }

            return targetList != null
                && targetList.Count > 0
                && targetList.All(t => customFilter.Invoke(t));
        }

        public override void LoadRow(DataFrameRow row)
        {
            var targetList = PrepareTargetArray(row);
            //иначе число сочетаний получается огромным
            targetList = targetList.Take(10).ToList();

            var keysGroups = PrepareKeyGroupsByLearn(targetList);
            //создаём элементы истории
            keysGroups.ForEach(groupKey =>
            {
                var key = CreateKey(groupKey);
                //TODO: 100 - временно, пока мы не знаем что доверенное значение, а что нет
                History.Add(key, (int)EntityScoreEnum.Application);
            });
        }

        /// <summary>
        /// Разбивает массив на уникальные группы
        /// </summary>
        /// <param name="targetList"></param>
        /// <returns></returns>
        protected virtual List<KeysGroup> PrepareKeyGroupsByLearn(List<TKey> targetList)
        {
            if (targetList.Count == 0) return new List<KeysGroup>();
            

            targetList.Sort();
            var lastItem = targetList.Last();

            var stack = new Stack<List<TKey>>();
            targetList.ForEach(item =>
            {
                stack.Push(new List<TKey> { item });
            });

            //есть пример, где 150 продуктов и 500к групп - извращение
            var currMax = targetList.Count < 20 ? MaxItemsInGroup : 1;
            //формируем группы ключей
            var keysGroups = new List<KeysGroup>();
            while (stack.TryPop(out List<TKey> groupKeys))
            {
                var last = groupKeys.Last();
                if ((last.Equals(lastItem) || groupKeys.Count >= currMax) 
                    && groupKeys.Count >= MinItemsInGroup || groupKeys.Count == targetList.Count)
                {
                    //группа сформирована, убираем её из стека и добавляем в историю
                    keysGroups.Add(new KeysGroup { Items = groupKeys });
                }
                else
                {
                    //продолжаем набивать стек новыми группами [1] -> [1,2], [1,3], [1,4], [1,5]
                    for (int nextIndex = targetList.IndexOf(last) + 1; nextIndex < targetList.Count; nextIndex++)
                    {
                        //делаем копию
                        var newKeys = groupKeys.Select(k => k).ToList();

                        newKeys.Add(targetList[nextIndex]);
                        stack.Push(newKeys);
                    }
                }
            }
            return keysGroups;
        }

        protected virtual List<KeysGroup> PrepareKeyGroupsByEstimate(List<TKey> targetList)
        {
            //в случае обычного массива совпадают, но у наследников есть особенности
            return PrepareKeyGroupsByLearn(targetList);
        }

        public static string KeySeparator = "_";
        private string CreateKey(KeysGroup keyGroup)
        {
            keyGroup.Items.Sort();
            return String.Join(KeySeparator, keyGroup.Items);
        }

        protected class KeysGroup
        {
            public KeysGroup() { }
            public List<TKey> Items { get; set; }
            public int Count => Items.Count;
        }
    }
}
