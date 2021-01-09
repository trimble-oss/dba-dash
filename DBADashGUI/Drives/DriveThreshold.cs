using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DBADashGUI
{

   public  class DriveThreshold
    {

        public bool Inherited { get; set; } = false;
        public Int32 InstanceID { get; set; }
        public Int32 DriveID { get; set; }

        public decimal WarningThreshold{get;set;}
        public decimal CriticalThreshold { get; set; }

        public string ConnectionString { get; set; }

        public DriveCheckTypeEnum DriveCheckType { get; set; }
        public enum DriveCheckTypeEnum
        {
            Percent,
            GB,
            None
        }

        public char DriveCheckTypeChar { 
            get {
                if (WarningThreshold <=0 && CriticalThreshold <= 0)
                {
                    return '-';
                }
                if (DriveCheckType== DriveCheckTypeEnum.GB)
                {
                    return 'G';
                }
                if(DriveCheckType== DriveCheckTypeEnum.Percent)
                {
                    return '%';
                }
                else
                {
                    return '-';
                }
            }
            set
            {
                if( value== 'G')
                {
                    DriveCheckType = DriveCheckTypeEnum.GB;
                }
                else if ( value== '%')
                {
                    DriveCheckType= DriveCheckTypeEnum.Percent;
                }
                else
                {
                    DriveCheckType = DriveCheckTypeEnum.None;
                }
            }

        }


        public static DriveThreshold GetDriveThreshold(Int32 InstanceID, Int32 DriveID,string ConnectionString)
        {
            DriveThreshold drv = new DriveThreshold();
            drv.InstanceID = InstanceID;
            drv.DriveID = DriveID;
            drv.ConnectionString = ConnectionString;
            SqlConnection cn = new SqlConnection(ConnectionString);
            using (cn)
            {
                cn.Open();
                SqlCommand cmd = new SqlCommand(@"DriveThreshold_Get", cn);
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("InstanceID", InstanceID);
                cmd.Parameters.AddWithValue("DriveID", DriveID);
                var rdr = cmd.ExecuteReader();
                if (rdr.Read())
                {
                    drv.WarningThreshold = rdr["DriveWarningThreshold"] == DBNull.Value ? 0 : (decimal)rdr["DriveWarningThreshold"];
                    drv.CriticalThreshold = rdr["DriveCriticalThreshold"] == DBNull.Value ? 0 : (decimal)rdr["DriveCriticalThreshold"];
                    drv.Inherited = (bool)rdr["Inherited"];
                    drv.DriveCheckTypeChar = char.Parse((string)rdr["DriveCheckType"]);
                }
                else
                {
                    drv.Inherited = DriveID != -1;
                }
                return drv;
            }
        }


        public void UpdateThresholds()
        {
           SqlConnection cn = new SqlConnection(ConnectionString);
            using (cn)
            {
                cn.Open();
                var cmd = new SqlCommand("dbo.DriveThresholds_Upd", cn);
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("InstanceID", InstanceID);
                cmd.Parameters.AddWithValue("DriveID", DriveID);
                cmd.Parameters.AddWithValue("DriveCheckType", Inherited ? 'I' : DriveCheckTypeChar);

                if (WarningThreshold <= 0 || DriveCheckType == DriveCheckTypeEnum.None || Inherited)
                {
                    cmd.Parameters.AddWithValue("Warning", DBNull.Value);
                }
                else
                {
                    cmd.Parameters.AddWithValue("Warning", WarningThreshold);
                }
                if (CriticalThreshold <= 0 || DriveCheckType == DriveCheckTypeEnum.None || Inherited)
                {
                    cmd.Parameters.AddWithValue("Critical", DBNull.Value);
                }
                else
                {

                    cmd.Parameters.AddWithValue("Critical", CriticalThreshold);
                }
                cmd.ExecuteNonQuery();
                var rdr = cmd.ExecuteReader();
                if (rdr.Read())
                {
                    WarningThreshold = rdr["DriveWarningThreshold"] == DBNull.Value ? 0 : (decimal)rdr["DriveWarningThreshold"];
                    CriticalThreshold = rdr["DriveWarningThreshold"] == DBNull.Value ? 0 : (decimal)rdr["DriveCriticalThreshold"];
                    DriveCheckTypeChar = char.Parse((string)rdr["DriveCheckType"]);
                    Inherited = (bool)rdr["Inherited"];
                }
            }
        }


 

    }
}
