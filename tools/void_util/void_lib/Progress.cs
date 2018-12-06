using System;
using System.Collections.Generic;
using System.Text;

namespace void_lib
{
    public abstract class VoidProgress
    {
        public Guid Id { get; set; }
        public long Size { get; set; }

        public static VoidProgress Create(Guid id, string label = null, string log = null, decimal? percentage = null, long? size = null)
        {
            if (label != null)
            {
                return new LabelVoidProgress()
                {
                    Id = id,
                    Label = label
                };
            }
            else if (log != null)
            {
                return new LogVoidProgress()
                {
                    Id = id,
                    Log = log
                };
            }
            else if (percentage != null)
            {
                return new PercentageVoidProgress()
                {
                    Id = id,
                    Percentage = percentage.Value,
                    Size = size ?? 0
                };
            }
            return null;
        }
    }

    public class LabelVoidProgress : VoidProgress
    {
        public string Label { get; set; }
    }

    public class LogVoidProgress : VoidProgress
    {
        public string Log { get; set; }
    }

    public class PercentageVoidProgress : VoidProgress
    {
        public decimal Percentage { get; set; }
    }
}
