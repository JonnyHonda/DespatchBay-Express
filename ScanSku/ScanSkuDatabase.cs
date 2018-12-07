using System;
using SQLite;

namespace ScanSku
{
    /// <summary>
    /// ScanSku Database
    /// </summary>
    public class ScanSkuDatabase
        {
        /// <summary>
        /// Initializes a new instance of the <see cref="T:ScanSku.ScanSkuDatabase"/> class.
        /// </summary>
        public ScanSkuDatabase()
            {
            }

            /// <summary>
            /// Parcel.
            /// </summary>
            public class ParcelScans
            {
                [PrimaryKey]
                [AutoIncrement]
                public int ID { get; set; } // an auto increment data base ID

                public string TrackingNumber { get; set; } // the TrackingNumber
                public string ScanTime { get; set; }
                public double Longitude { get; set; }
                public double Latitude { get; set; }

            public override string ToString()
                {
                    return string.Format("[Scan: ID={0}, Tracking Number={1}, Scan Time={2}, Longtitude={3}, Latitude={4}]", ID, TrackingNumber, ScanTime, Longitude, Latitude);
                }
            }

        }
    

}



