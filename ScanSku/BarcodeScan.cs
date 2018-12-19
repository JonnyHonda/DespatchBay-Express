using System;
using Android.App;
using Android.Content;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using Android.OS;
using Android.Support.V7.Widget;
using System.Collections.Generic;
using SQLite;
using static DespatchBayExpress.DespatchBayExpressDataBase;

namespace DespatchBayExpress
{
    // BarcodeScan: contains image resource ID and caption:
    public class BarcodeScan
    {
        // GetBarcodeText text for this Barcode:
        public string BarcodeText;

        /// <summary>
        /// 
        /// </summary>
        public string GetBarcodeText
        {
            get { return BarcodeText; }
        }
        
    }

    /// <summary>
    /// 
    /// </summary>
    public class BarcodeScannerList
    {
        static string dbPath = System.IO.Path.Combine(
                        System.Environment.GetFolderPath(System.Environment.SpecialFolder.Personal),
                        "localscandata.db3");
        SQLiteConnection db = new SQLiteConnection(dbPath);

        List<BarcodeScan> CurrentScans = new List<BarcodeScan>();
        // Array of barcodes that make up the RecycleView
        private BarcodeScan[] barcodes;


        public BarcodeScannerList()
        {
            this.FetchBarcodeList();

        }

        public void FetchBarcodeList()
        {
            var scans = db.Query<DespatchBayExpressDataBase.ParcelScans>("SELECT * FROM ParcelScans WHERE Sent IS null");
            CurrentScans.Clear();
            foreach (var scan in scans)
            {
                BarcodeScan t = new BarcodeScan { BarcodeText = scan.TrackingNumber };
                CurrentScans.Add(t);
            }
            barcodes = CurrentScans.ToArray();
        }
        // Return the number of barcodes in the view 
        public int NumBarcodes
        {
            get { return barcodes.Length; }
        }
        
        // Indexer (read only) for accessing a barcode:
        public BarcodeScan this[int i]
        {
            get { return barcodes[i]; }
        }

    }
}
 