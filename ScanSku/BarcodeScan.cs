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
        /*
        // BarcodeScan ID for this photo:
        public int mPhotoID;
        */

        // Caption text for this photo:
        public string mCaption;

        /*
        // Return the ID of the photo:
        public int PhotoID
        {
            get { return mPhotoID; }
        }
        */

        // Return the Caption of the photo:
        public string Caption
        {
            get { return mCaption; }
        }
        
    }

    // BarcodeScan album: holds image resource IDs and caption:
    public class BarcodeScannerList
    {
        static string dbPath = System.IO.Path.Combine(
                        System.Environment.GetFolderPath(System.Environment.SpecialFolder.Personal),
                        "localscandata.db3");
        SQLiteConnection db = new SQLiteConnection(dbPath);

        List<BarcodeScan> CurrentScans = new List<BarcodeScan>();
        // Array of barcodes that make up the album:
        private BarcodeScan[] barcodes;


        public BarcodeScannerList()
        {
            this.FetchBarcodeList();

        }

        public void FetchBarcodeList()
        {
            var scans = db.Table<ParcelScans>();
            CurrentScans.Clear();
            foreach (var scan in scans)
            {
                BarcodeScan t = new BarcodeScan { mCaption = scan.TrackingNumber };
                CurrentScans.Add(t);
            }
            barcodes = CurrentScans.ToArray();
        }
        // Return the number of photos in the photo album:
        public int NumBarcodes
        {
            get { return barcodes.Length; }
        }
        
        // Indexer (read only) for accessing a photo:
        public BarcodeScan this[int i]
        {
            get { return barcodes[i]; }
        }

    }
}
 