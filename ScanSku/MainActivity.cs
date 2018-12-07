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
using Android.Locations;
using System.Collections.Generic;
using System.Linq;
using Android.Util;

namespace ScanSku
{
    [Activity(Label = "@string/app_name", Theme = "@style/AppTheme.NoActionBar", MainLauncher = true)]
    public class MainActivity : AppCompatActivity,ILocationListener
    {
        SQLiteConnection db = null;
        TextView txtlatitu;
        TextView txtlong;
        Location currentLocation;
        LocationManager locationManager;
        string locationProvider;
        public string TAG
        {
            get;
            private set;
        }

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            InitializeLocationManager();
            SetContentView(Resource.Layout.activity_main);
            txtlatitu = FindViewById<TextView>(Resource.Id.txtlatitude);
            txtlong = FindViewById<TextView>(Resource.Id.txtlong);
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

        private void InitializeLocationManager()
        {
            locationManager = (LocationManager)GetSystemService(LocationService);
            Criteria criteriaForLocationService = new Criteria
            {
                Accuracy = Accuracy.Fine
            };
            IList<string> acceptableLocationProviders = locationManager.GetProviders(criteriaForLocationService, true);
            if (acceptableLocationProviders.Any())
            {
                locationProvider = acceptableLocationProviders.First();
            }
            else
            {
                locationProvider = string.Empty;
            }
            Log.Debug(TAG, "Using " + locationProvider + ".");
        }
        public void OnLocationChanged(Location location)
        {
            currentLocation = location;
            if (currentLocation == null)
            {
                //Error Message  
            }
            else
            {
                txtlatitu.Text = currentLocation.Latitude.ToString();
                txtlong.Text = currentLocation.Longitude.ToString();
            }
        }
        protected override void OnResume()
        {
            base.OnResume();
           locationManager.RequestLocationUpdates(locationProvider, 0, 0, this);
        }
        protected override void OnPause()
        {
            base.OnPause();
            locationManager.RemoveUpdates(this);
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
                case Resource.Id.menu_upload:
                    // This code might be called from within an Activity, for example in an event
                    // handler for a button click.
                    Intent downloadIntent = new Intent(this, typeof(DemoIntentService));

                    // This is just one example of passing some values to an IntentService via the Intent:
                    downloadIntent.PutExtra("file_to_download", "http://www.somewhere.com/file/to/download.zip");

                    StartService(downloadIntent);
                    break;
                case Resource.Id.menu_location:
                    // This code might be called from within an Activity, for example in an event
                    // handler for a button click.
                    Intent fetchGpsIntent = new Intent(this, typeof(GpsIntentService));

                    // This is just one example of passing some values to an IntentService via the Intent:
                    fetchGpsIntent.PutExtra("file_to_download", "http://www.somewhere.com/file/to/download.zip");

                    StartService(fetchGpsIntent);
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
            newScan.Longitude = Convert.ToDouble(txtlong.Text);
            newScan.Latitude = Convert.ToDouble(txtlatitu.Text);
            db.Insert(newScan);


            Snackbar.Make(view, "Barcode " + newScan.TrackingNumber, Snackbar.LengthLong)
                .SetAction("Action", (Android.Views.View.IOnClickListener)null).Show();
        }

        public void OnProviderDisabled(string provider)
        {
            throw new NotImplementedException();
        }

        public void OnProviderEnabled(string provider)
        {
            throw new NotImplementedException();
        }

        public void OnStatusChanged(string provider, [GeneratedEnum] Availability status, Bundle extras)
        {
           // throw new NotImplementedException();
        }

        [Service]
        public class DemoIntentService : IntentService
        {
            public DemoIntentService() : base("DemoIntentService")
            {
            }

            protected override void OnHandleIntent(Android.Content.Intent intent)
            {
                Console.WriteLine("perform some long running work");
                var startTime = DateTime.UtcNow;

                while (DateTime.UtcNow - startTime < TimeSpan.FromSeconds(30))
                {
                    // Execute your loop here...
                }
                Console.WriteLine("work complete");
            }
        }

        [Service]
        public class GpsIntentService : IntentService
        {
            public GpsIntentService() : base("DemoIntentService")
            {
            }

            protected override void OnHandleIntent(Android.Content.Intent intent)
            {
                Console.WriteLine("perform some long running work");
                var startTime = DateTime.UtcNow;

                while (DateTime.UtcNow - startTime < TimeSpan.FromSeconds(30))
                {
                    // Execute your loop here...
                }
                Console.WriteLine("work complete");
            }
        }
    }
}



