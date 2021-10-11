using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DBADash
{
    ///<summary>
    /// Configuration for the collection of plans for running queries. <br/> 
    /// Thresholds set to max value = plan collection disabled<br/> 
    /// Thresholds set to 0 = collect all plans<br/> 
    /// Only 1 threshold must be exceeded for plan to be collected.<br/> 
    ///</summary>
    public class PlanCollectionThreshold
    {
        public int CPUThreshold = Int32.MaxValue;
        public int MemoryGrantThreshold = Int32.MaxValue;
        public int DurationThreshold = Int32.MaxValue;
        public int CountThreshold = Int32.MaxValue;

        public bool PlanCollectionEnabled
        {
            get
            {
                if (CPUThreshold == Int32.MaxValue && MemoryGrantThreshold == Int32.MaxValue && DurationThreshold == Int32.MaxValue)
                {
                    return false;
                }
                else
                {
                    return true;
                }
            }
        }

        public static PlanCollectionThreshold DefaultThreshold 
            {
                get{
                    return new PlanCollectionThreshold() { CPUThreshold = 1000, DurationThreshold = 10000, MemoryGrantThreshold = 6400, CountThreshold = 2 };
                }
            } 

        public static PlanCollectionThreshold PlanCollectionDisabledThreshold
        {
            get
            {
                return new PlanCollectionThreshold();
            }
        }

        public static PlanCollectionThreshold CollectAllPlansThreshold
        {
            get
            {
                return new PlanCollectionThreshold() { CountThreshold = 0, CPUThreshold = 0, DurationThreshold = 0, MemoryGrantThreshold = 0 };
            }
        }
    }
}
