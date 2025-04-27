using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IPSLib.EstimationPredictors.DeterminePredictor.Models
{
    public class HierarchyVector: IComparable<HierarchyVector>
    {
        public string Separator = "/";
        public List<int> Items =  new List<int>();
        public HierarchyVector() { }

        public void Load(string path, string separator = "/")
        {
            Separator = separator;
            Items = path.Split(new string[] { separator }, StringSplitOptions.RemoveEmptyEntries)
                .Select(str=>int.Parse(str)).ToList();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="hvector"></param>
        /// <param name="isAbsoluteDiff">
        /// true - координаты будут взяты по модулю
        /// false - можно понять, кто первее по иерархии
        /// </param>
        /// <returns></returns>
        public HierarchyVector GetDifference(HierarchyVector hvector, bool isAbsoluteDiff = true)
        {
            var diffList = new List<int>();
            //вариант 1:
            //Важно, что именно Min: иначе не будет учитываться ситуация,
            //когда более маленький вектор полностью лежит в более большом
            //var len = Math.Min(Items.Count, hvector.Items.Count);

            //вариант 2:
            //смотрим относительно первого вектора
            //a = 1/92562 b = 1/92562/29134/123213
            //a полностью лежит в b, значит разница [0/0]
            //если меняем местами, то разница [0/0/29134/123213]
            //var len = this.Items.Count;

            //вариант 3:
            //на обучающей выборке плохо работает, но для боевого режима лучше использовать именно его
            //проблема в том, что когда есть всего 2 уровня, и мы сравниваем с 6-8 уровневыми
            //они могут отличаться принципиально, как запчасти для машины с запчастями на компьютер
            var len = Math.Max(Items.Count, hvector.Items.Count);

            for (var i = 0; i < len; i++)
            {
                var a = Items.ElementAtOrDefault(i);
                var b = hvector.Items.ElementAtOrDefault(i);

                if(a != null && b != null)
                {
                    //var diff = Math.Abs(a - b);
                    var diff = isAbsoluteDiff ? Math.Abs(a - b): a - b;
                    diffList.Add(diff);
                }
            }
            var hResult = new HierarchyVector();
            hResult.Items = diffList;
            return hResult;
        }

        public static HierarchyVector CreateFromString(string hierarchyPath, string separator = "/")
        {
            var result = new HierarchyVector();
            result.Load(hierarchyPath,separator);
            return result;
        }

        /// <summary>
        /// Норма(длина, расстояние) в Евклидовом смысле 
        /// </summary>
        public double Norm => Math.Sqrt(Items.Sum(x => Math.Pow(x, 2)));

        public override string ToString()
        {
            return String.Join(Separator, Items)+Separator;
        }

        /// <summary>
        /// Например:
        /// a = [0,0,1] b = [0,1,0] -> a < b
        /// a = [0,0,1] b = [0,-1,0] -> a > b
        /// </summary>
        /// <param name="other"></param>
        /// <returns></returns>
        public int CompareTo(HierarchyVector? other)
        {
            var firstNotZero = this.GetDifference(other, false).Items.FirstOrDefault(i => i != 0);
            //если firstNotZero < 0, то other больше
            //если firstNotZero > 0, то other меньше
            return firstNotZero.CompareTo(0);
        }

        /// <summary>
        /// Выбирает самый близко расположенный вектор из предложенных
        /// </summary>
        /// <param name="vectors"></param>
        /// <returns></returns>
        public HierarchyVector GetMostNearly(out HierarchyVector mostNearlyDifference, IEnumerable<HierarchyVector> vectors)
        {
            var mostNearly = vectors.First();
            mostNearlyDifference = this.GetDifference(mostNearly);

            foreach(var currVector in vectors)
            {
                var currDifference = this.GetDifference(currVector);

                //сравниваем, кто меньше
                //a = [0, 0, 1, 1] b1 = [0, 0, 2, 1] -> a - b1 = [0, 0, 1, 0] (абсолютная разница)
                //a = [0, 0, 1, 1] b2 = [0, 0, 2, 0] -> a - b2 = [0, 0, 0, 1]
                //b2 ближе к а, чем b1
                if (mostNearlyDifference.CompareTo(currDifference) > 0)
                {
                    mostNearly = currVector;
                    mostNearlyDifference = currDifference;
                }
            }

            return mostNearly;
        }

        /// <summary>
        /// Определяет, что расстояние достаточно близкое
        /// </summary>
        /// <param name="mostNearlyDifference">расстояние до bVector</param>
        /// <param name="maxDistancePercent">максимальная разница относительно aVector: 0-1</param>
        /// <returns></returns>
        public bool IsDifferenceNotBig(HierarchyVector mostNearlyDifference)
        {
            var success = true;
            var lastCheckedLevel = mostNearlyDifference.Items.Count;

            //в случае, когда много уровней, последний можем пропустить, тк очень часто там попадается всё подходящее
            if (lastCheckedLevel > 8) lastCheckedLevel -= lastCheckedLevel / 4;
            else if (lastCheckedLevel > 5) lastCheckedLevel -= 2;
            else if (lastCheckedLevel > 2) lastCheckedLevel -= 1;

            //var initialPercent = 0.1;
            //var maxDiff = 100;

            //последний элемент может отличаться, главное попасть в предыдущие
            for (var i = 0; i < lastCheckedLevel; i++)
            {
                //var a = this.Items[i];
                var diff = mostNearlyDifference.Items[i];

                //первые уровни должны совпадать, иначе будет хаос
                if (diff == 0) continue;
                success = false;
                break;

                //var border = a * initialPercent;
                //if(border > maxDiff) border = maxDiff;

                ////+2 для случаев с маленькими векторами
                //if (diff > border + 2)
                //{
                //    success = false;
                //    break;
                //}
            }

            return success;
        }
    }
}
