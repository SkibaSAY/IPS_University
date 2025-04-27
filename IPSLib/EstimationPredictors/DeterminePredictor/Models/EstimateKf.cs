using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IPSLib.EstimationPredictors.DeterminePredictors
{
    /// <summary>
    /// Оценка анализатора полей
    /// </summary>
    public class EstimateKf: IComparable<EstimateKf>
    {
        private double _estimate = 0;
        public bool IsDefault = true;

        /// <summary>
        /// Оценка: 0-1
        /// </summary>
        public double Value
        {
            get
            {
                return _estimate;
            }
            set
            {
                if (value < 0 || value > 1) throw new ArgumentException("Double field Estimate can be between [0,1]");
                _estimate = value;
                IsDefault = false;
            }
        }
        public static EstimateKf Default() => new EstimateKf() {IsDefault = true };
        public static EstimateKf Normalize(double value)
        {
            var normEstimate = value;
            if (value < 0) normEstimate = 0;
            else if (value > 1) normEstimate = 1;

            return new EstimateKf { Value = normEstimate };
        }
        public override string ToString()
        {
            return Value.ToString();
        }

        public int CompareTo(EstimateKf? other)
        {
            if (this.Value < other.Value) return -1;
            else if (this.Value == other.Value) return 0;
            else return 1;
        }
    }
}
