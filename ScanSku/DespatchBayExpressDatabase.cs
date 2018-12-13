﻿using System;
using System.Collections;
using SQLite;

namespace DespatchBayExpress
{
    /// <summary>
    /// ScanSku Database
    /// </summary>
    public class DespatchBayExpressDataBase
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="T:ScanSku.ScanSkuDatabase"/> class.
        /// </summary>
        public DespatchBayExpressDataBase()
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

            [Indexed(Unique = true)]
            public string TrackingNumber { get; set; } // the TrackingNumber

            public string ScanTime { get; set; }

            // ? allow  nulls
            public double? Longitude { get; set; }
            public double? Latitude { get; set; }

            public DateTime Sent { get; set; }

            public override string ToString()
            {
                return string.Format("[Scan: ID={0}, Tracking Number={1}, Scan Time={2}, Longtitude={3}, Latitude={4}, Sent={5}]", ID, TrackingNumber, ScanTime, Longitude, Latitude, Sent);
            }

        }

        /// <summary>
        /// TrackingNumberPatterns
        /// </summary>
        public class TrackingNumberPatterns
        {
            [PrimaryKey]
            [AutoIncrement]
            public int ID { get; set; } // an auto increment data base ID

            public string Courier { get; set; }
            public string Pattern { get; set; }
            public bool IsEnabled { get; set; }

            public override string ToString()
            {
                return string.Format("[Pattern: ID={0}, Courier={1}, Pattern={2}, isEnabled={3}]", ID, Courier, Pattern, IsEnabled);
            }
        }
    }


}


