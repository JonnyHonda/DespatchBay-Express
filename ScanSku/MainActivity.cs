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
using System.Security;
using Android.Support.V4.Content;
using static Android.Manifest;
using Android;
using Android.Content.PM;
using Permission = Android.Content.PM.Permission;
using Android.Support.V4.App;

namespace ScanSku
{
    [Activity(Label = "@string/app_name", Theme = "@style/AppTheme.NoActionBar", MainLauncher = true)]
    public class MainActivity : AppCompatActivity,ILocationListener
    {

        static readonly int REQUEST_LOCATION = 1;
        static readonly Keycode SCAN_BUTTON = (Keycode)301;
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
            if (ContextCompat.CheckSelfPermission(this, Manifest.Permission.AccessFineLocation) == (int)Permission.Granted)
            {
                // We have permission, go ahead and use the GPS.
                Log.Debug(TAG, "We have permission, go ahead and use the GPS.");
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
                EditText scan = FindViewById<EditText>(Resource.Id.txtentry);
                scan.Text = "";
                scan.RequestFocus();
                ToggleButton togglebutton = FindViewById<ToggleButton>(Resource.Id.togglebutton);

                togglebutton.Click += (o, e) => {
                    // Perform action on clicks
                    if (togglebutton.Checked) { }
                    //   Toast.MakeText(this, "Checked", ToastLength.Short).Show();
                    else { }
                     //   Toast.MakeText(this, "Not checked", ToastLength.Short).Show();
                };
            }
            else
            {
                // GPS permission is not granted. If necessary display rationale & request.
                Log.Debug(TAG, "GPS permission is not granted");

                if (ActivityCompat.ShouldShowRequestPermissionRationale(this, Manifest.Permission.AccessFineLocation))
                {
                    // Provide an additional rationale to the user if the permission was not granted
                    // and the user would benefit from additional context for the use of the permission.
                    // For example if the user has previously denied the permission.
                    Log.Info(TAG, "Displaying camera permission rationale to provide additional context.");
                    var rootView = FindViewById<CoordinatorLayout>(Resource.Id.root_view);


                    var requiredPermissions = new String[] { Manifest.Permission.AccessFineLocation };
                    Snackbar.Make(rootView,
                                   Resource.String.permission_location_rationale,
                                   Snackbar.LengthIndefinite)
                            .SetAction(Resource.String.ok,
                                       new Action<View>(delegate (View obj) {
                                           ActivityCompat.RequestPermissions(this, requiredPermissions, REQUEST_LOCATION);
                                       }
                            )
                    ).Show();
                }
                else
                {
                    ActivityCompat.RequestPermissions(this, new String[] { Manifest.Permission.AccessFineLocation }, REQUEST_LOCATION);
                }

            }
           

        }
        public override bool OnKeyUp(Android.Views.Keycode keyCode, Android.Views.KeyEvent e)
        {
            EditText scan = FindViewById<EditText>(Resource.Id.txtentry);
      //      Toast.MakeText(this, keyCode.ToString(), ToastLength.Short).Show();
            if (keyCode == SCAN_BUTTON )
            {
                if (scan.Text.Length > 0) //e.RepeatCount == 0)
                {
                    var newScan = new ScanSkuDatabase.ParcelScans();
                    newScan.TrackingNumber = scan.Text;
                    newScan.ScanTime = DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss");
                    try
                    {
                        newScan.Longitude = Convert.ToDouble(txtlong.Text);
                    }
                    catch
                    {
                        newScan.Longitude = 0;
                    }
                    try
                    {
                        newScan.Latitude = Convert.ToDouble(txtlatitu.Text);
                    }
                    catch
                    {

                        newScan.Latitude = 0;
                    }
                    db.Insert(newScan);
                    scan.Text = "";
                    scan.RequestFocus();
                }
                return true;
            }
            return base.OnKeyDown(keyCode, e);
        }

        public override bool OnKeyDown(Android.Views.Keycode keyCode, Android.Views.KeyEvent e)
        {
            if (keyCode == Android.Views.Keycode.F9)
            {
                if (e.RepeatCount == 0)
                {
                   // MyApplication.BarcodeStopScan();
                }
                return true;
            }
            return base.OnKeyUp(keyCode, e);
        }

        public override void OnRequestPermissionsResult(int requestCode, string[] permissions, Permission[] grantResults)
        {
            if (requestCode == REQUEST_LOCATION)
            {
                // Received permission result for GPS permission.
                Log.Info(TAG, "Received response for Location permission request.");
                var rootView = FindViewById<CoordinatorLayout>(Resource.Id.root_view);
                // Check if the only required permission has been granted
                if ((grantResults.Length == 1) && (grantResults[0] == Permission.Granted))
                {
                    // Location permission has been granted, okay to retrieve the location of the device.
                    Log.Info(TAG, "Location permission has now been granted.");
                    // Snackbar.Make(rootView, Resource.String.permission_available_location, Snackbar.LengthShort).Show();
                    this.FinishAffinity();
                    ;
                }
                else
                {
                    Log.Info(TAG, "Location permission was NOT granted.");
                  //  Snackbar.Make(rootView, Resource.String.permissions_not_granted, Snackbar.LengthShort).Show();
                }
            }
            else
            {
                base.OnRequestPermissionsResult(requestCode, permissions, grantResults);
            }
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
            try
            {
                locationManager.RequestLocationUpdates(locationProvider, 0, 0, this);
            }
            catch (Exception ex)
            {
                Log.Debug(TAG, "Error creating location service: " + ex.Message);
            }

        }

        protected override void OnPause()
        {
            base.OnPause();
            try { 
            locationManager.RemoveUpdates(this);
        }
            catch (Exception ex)
            {
                Log.Debug(TAG, "Error creating location service: " + ex.Message);
            }
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
                case Resource.Id.menu_refresh:
                    db.DeleteAll<ScanSkuDatabase.ParcelScans>();
                    break;

            }

            return base.OnOptionsItemSelected(item);
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

   
  
    }
}



