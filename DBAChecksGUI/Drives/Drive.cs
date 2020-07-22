using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DBAChecksGUI
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
    public  class Drive: DriveThreshold
    {
        const Int64 bytesPerGB = 1073741824;

        public enum DriveStatusEnum
        {
            OK=4,
            Warning=2,
            Critical=1,
            NA=3
        }

        public string InstanceName;
        public DriveStatusEnum DriveStatus;

        public void RefreshDriveStatus()
        {
            if (DriveCheckType == DriveCheckTypeEnum.GB)
            {
                if (FreeSpaceGB <= CriticalThreshold)
                {
                    DriveStatus = DriveStatusEnum.Critical;
                }
                else if (FreeSpaceGB <= WarningThreshold)
                {
                    DriveStatus = DriveStatusEnum.Warning;
                }
                else
                {
                    DriveStatus = DriveStatusEnum.OK;
                }
            }
            else if(DriveCheckType == DriveCheckTypeEnum.Percent)
            {
                if(PercentFreeSpace<= ((double)CriticalThreshold*100))
                {
                    DriveStatus = DriveStatusEnum.Critical;
                }
                else if(PercentFreeSpace<= ((double)WarningThreshold*100))
                {
                    DriveStatus = DriveStatusEnum.Warning;
                }
                else
                {
                    DriveStatus = DriveStatusEnum.OK;
                }
            }
            else
            {
                DriveStatus = DriveStatusEnum.NA;
            }
        }


        public string DriveLetter { get; set; }

        public string DriveLabel { get; set; }

       public  decimal FreeSpaceGB
        {
            get
            {
                return FreeSpace / (decimal)bytesPerGB;
            }
            set
            {
                FreeSpace = (Int64)(value * bytesPerGB);
            }
        }
        public decimal DriveCapacityGB
        {
            get
            {
                return DriveCapacity / (decimal)bytesPerGB;
            }
            set
            {
                DriveCapacity = (Int64)(value * bytesPerGB);
            }
        }



        public double PercentFreeSpace
        {
            get
            {
                if (FreeSpace == 0 || DriveCapacity == 0)
                {
                    return 100;
                }
                else
                {
                    return ((FreeSpace * 1.0 / DriveCapacity) * 100);
                }
            }
        }

        public double PercentUsedSpace
        {
            get
            {
                return 100d - PercentFreeSpace;
            }
        }
       

        public Int64 DriveCapacity { get; set; } = 0;

        public Int64 FreeSpace { get; set; } = 0;


        public override string ToString()
        {
            return  DriveLabel + " (" + DriveLetter + ")";
        }
    }
}
