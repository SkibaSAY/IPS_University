using IPSLib.EstimationPredictors.DeterminePredictor.Models;
using IPSLib.EstimationPredictors.DeterminePredictors;
using IPSLib.Model;
using Microsoft.Data.Analysis;
using Newtonsoft.Json;
using Shared;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices.Marshalling;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace IPSLib.EstimationPredictors.DeterminePredictor.SimplePredictors
{
    public class StringPredictor : ArrayPredictor<string>
    {
        public StringPredictor(string targetPropName, int maxItemsInGroup = 5, int minItemsInGroup = 3) : base(targetPropName, maxItemsInGroup, minItemsInGroup)
        {

        }
        private static string[] IgnoreTokens = new string[]
        {
            "постав", "закуп", "цель", "оказ"
        };
        private List<string> Normalize(string inputStr)
        {
            //take 10, иначе сильно разростается число сочетаний
            var tokenizedList = RussWordNormalizer.NormalizeWords(inputStr)
                .Where(w=>w.IsAbbreviation || w.Root.Length > 3 && w.Root.Any(ch => !Char.IsDigit(ch)))
                .Select(w=>w.Root.ToString())
                .Distinct().Take(10).ToList();

            //1.часто самое первое слово не несёт пользы
            //например: оказание, обеспечение, запрос и тд
            if (tokenizedList.Count > 5) tokenizedList.RemoveAt(0);

            //2.удаляем игнорируемые слова, если они встретились не на первой позиции
            for(var i = 0; i < tokenizedList.Count; i++)
            {
                var curr = tokenizedList[i];
                if (IgnoreTokens.Any(ignored => curr.StartsWith(ignored)))
                {
                    tokenizedList.RemoveAt(i);
                    i--;
                }
            }

            return tokenizedList;
        }

        protected override List<string> PrepareTargetArray(DataFrameRow row)
        {
            var str = (GetTargetValue(row) as string);
            return PrepareTargetArray(str);
        }
        protected override List<string> PrepareTargetArray(Entity entity)
        {
            var str = (GetTargetValue(entity) as string);
            return PrepareTargetArray(str);
        }

        private static Regex WordRegex = new Regex(@"\w+");
        protected List<string> PrepareTargetArray(string str)
        {
            //var inputArr = WordRegex.Matches(str).Select(m => m.Value).ToList();
            var normArray = Normalize(str);
            return normArray;
        }
    }
}
