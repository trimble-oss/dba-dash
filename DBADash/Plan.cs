using System;
using System.Collections.Generic;
using System.Linq;

namespace DBADash
{
    public class Plan : IEqualityComparer<Plan>
    {
        public readonly byte[] PlanHandle;
        public readonly int StartOffset;
        public readonly int EndOffset;
        public readonly byte[] PlanHash;
        public readonly string Key;

        public Plan(byte[] planHandle, byte[] planHash, int startOffset, int endOffset)
        {
            PlanHandle = planHandle;
            PlanHash = planHash;
            StartOffset = startOffset;
            EndOffset = endOffset;
            Key = Convert.ToBase64String(PlanHandle.Concat(PlanHash).Concat(BitConverter.GetBytes(StartOffset)).Concat(BitConverter.GetBytes(EndOffset)).ToArray());
        }

        public override bool Equals(Object obj)
        {
            //Check for null and compare run-time types.
            if ((obj == null) || !this.GetType().Equals(obj.GetType()))
            {
                return false;
            }
            else
            {
                Plan p = (Plan)obj;
                return p.Key == this.Key;
            }
        }

        public bool Equals(Plan x, Plan y)
        {
            return x.Key == y.Key;
        }

        public override int GetHashCode()
        {
            return Key.GetHashCode();
        }

        public int GetHashCode(Plan obj)
        {
            return obj.Key.GetHashCode();
        }
    }
}
