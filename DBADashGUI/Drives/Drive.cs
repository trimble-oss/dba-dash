using System;
using System.ComponentModel;
using static DBADashGUI.DBADashStatus;

namespace DBADashGUI
{
    public class DriveTypeConverter : TypeConverter
    {
        public override bool GetPropertiesSupported(ITypeDescriptorContext context)
        {
            return true;
        }

        public override PropertyDescriptorCollection GetProperties(ITypeDescriptorContext context, object value, Attribute[] attributes)
        {
            return TypeDescriptor.GetProperties(typeof(Drive));
        }
    }

    [TypeConverter(typeof(DriveTypeConverter))]
    public class Drive : DriveThreshold
    {
        private const long bytesPerGB = 1073741824;

        public string InstanceName;
        public DBADashStatusEnum DriveStatus;

        public void RefreshDriveStatus()
        {
            if (DriveCheckType == DriveCheckTypeEnum.GB)
            {
                if (FreeSpaceGB <= CriticalThreshold)
                {
                    DriveStatus = DBADashStatusEnum.Critical;
                }
                else if (FreeSpaceGB <= WarningThreshold)
                {
                    DriveStatus = DBADashStatusEnum.Warning;
                }
                else
                {
                    DriveStatus = DBADashStatusEnum.OK;
                }
            }
            else if (DriveCheckType == DriveCheckTypeEnum.Percent)
            {
                if (PercentFreeSpace <= ((double)CriticalThreshold * 100))
                {
                    DriveStatus = DBADashStatusEnum.Critical;
                }
                else if (PercentFreeSpace <= ((double)WarningThreshold * 100))
                {
                    DriveStatus = DBADashStatusEnum.Warning;
                }
                else
                {
                    DriveStatus = DBADashStatusEnum.OK;
                }
            }
            else
            {
                DriveStatus = DBADashStatusEnum.NA;
            }
        }

        public string DriveLetter { get; set; }

        public string DriveLabel { get; set; }

        public DateTime SnapshotDate { get; set; }

        public DBADashStatusEnum SnapshotStatus { get; set; }

        public decimal FreeSpaceGB
        {
            get => FreeSpace / (decimal)bytesPerGB; set => FreeSpace = (long)(value * bytesPerGB);
        }

        public decimal DriveCapacityGB
        {
            get => DriveCapacity / (decimal)bytesPerGB; set => DriveCapacity = (long)(value * bytesPerGB);
        }

        public double PercentFreeSpace => DriveCapacity == 0 ? 0 : (FreeSpace * 1.0 / DriveCapacity) * 100;

        public double PercentUsedSpace => 100d - PercentFreeSpace;

        public long DriveCapacity { get; set; } = 0;

        public long FreeSpace { get; set; } = 0;

        public override string ToString() => DriveLabel + " (" + DriveLetter + ")";
    }
}