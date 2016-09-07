using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ECIS.Client.WEIS
{
    public class BizPlanTLASetting
    {
        /// <summary>
        /// Spread Rate Total
        /// </summary>
        public double SpreadRate { get; set; }
        /// <summary>
        /// Trouble free days
        /// </summary>
        public double Days { get; set; }

        /// <summary>
        /// NPT Days   
        /// </summary>
        public double NPTDays { get; set; }

        /// <summary>
        /// Tangible
        /// </summary>
        public double Tangibles { get; set; }

        /// <summary>
        /// Services
        /// </summary>
        public double Services { get; set; }

        /// <summary>
        /// Materials
        /// </summary>
        public double Material { get; set; }
    }
}
