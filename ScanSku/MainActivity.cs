using System;
using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Support.Design.Widget;
using Android.Support.V7.App;
using Android.Views;
using Android.Widget;
using SQLite;

namespace ScanSku
{
    [Activity(Label = "@string/app_name", Theme = "@style/AppTheme.NoActionBar", MainLauncher = true)]
    public class MainActivity : AppCompatActivity
    {
        SQLiteConnection db = null;
        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            SetContentView(Resource.Layout.activity_main);

            Android.Support.V7.Widget.Toolbar toolbar = FindViewById<Android.Support.V7.Widget.Toolbar>(Resource.Id.toolbar);
            SetSupportActionBar(toolbar);

            // Create database
            string dbPath = System.IO.Path.Combine(
                    System.Environment.GetFolderPath(System.Environment.SpecialFolder.Personal),
                    "localscandata.db3");
            db = new SQLiteConnection(dbPath);
            // Create the ParcelScans table
            db.CreateTable<ScanSkuDatabase.ParcelScans>();
            // db.DeleteAll<ScanSkuDatabase.ParcelScans>();

            FloatingActionButton fab = FindViewById<FloatingActionButton>(Resource.Id.fab);
            fab.Click += FabOnClick;

        }

        public override bool OnCreateOptionsMenu(IMenu menu)
        {
            MenuInflater.Inflate(Resource.Menu.menu_main, menu);
            return true;
        }

        public override bool OnOptionsItemSelected(IMenuItem item)
        {
            switch (item.ItemId)
            {
                case Resource.Id.menu_about:
                    StartActivity(typeof(About));
                    break;
                case Resource.Id.menu_settings:
                    StartActivity(typeof(SqliteActivity));
                    break;
                case Resource.Id.menu_exit:
                    this.FinishAffinity();
                    break;
                case Resource.Id.menu_refresh:
                    db.DeleteAll<ScanSkuDatabase.ParcelScans>();
                    break;

            }

            return base.OnOptionsItemSelected(item);
        }

        private void FabOnClick(object sender, EventArgs eventArgs)
        {
            View view = (View) sender;
            var newScan = new ScanSkuDatabase.ParcelScans();
            newScan.TrackingNumber =DateTime.Now.ToString("JJD"+ "yyyyssfffffff");
            newScan.ScanTime = DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss");
            db.Insert(newScan);


            Snackbar.Make(view, "Barcode " + newScan.TrackingNumber, Snackbar.LengthLong)
                .SetAction("Action", (Android.Views.View.IOnClickListener)null).Show();
        }
	}
}



