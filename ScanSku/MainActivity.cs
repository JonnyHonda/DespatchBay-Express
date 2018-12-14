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
using Android.Support.V7.Widget;
using System.Collections;
using Android.Media;
using System.Text.RegularExpressions;
using static DespatchBayExpress.DespatchBayExpressDataBase;

namespace DespatchBayExpress
{
    [Activity(Label = "@string/app_name", Theme = "@style/AppTheme.NoActionBar", MainLauncher = true)]
    public class MainActivity : AppCompatActivity, ILocationListener
    {
        static readonly int REQUEST_LOCATION = 1;
        // static readonly Keycode SCAN_BUTTON = (Keycode)301;
        SQLiteConnection db = null;
        //TextView txtlatitude;
        //TextView txtlong;
        TextView coords;
        Location currentLocation;
        LocationManager locationManager;
        string locationProvider;

        MediaPlayer mediaPlayer;

        public string TAG
        {
            get;
            private set;
        }

        RecyclerView mRecyclerView;
        RecyclerView.LayoutManager mLayoutManager;
        TrackingNumberDataAdapter mAdapter;
        BarcodeScannerList mBarcodeScannerList;
        EditText TrackingScan;

        protected override void OnCreate(Bundle savedInstanceState)
        {
            RequestedOrientation = ScreenOrientation.Portrait;
            base.OnCreate(savedInstanceState);
            if (ContextCompat.CheckSelfPermission(this, Manifest.Permission.AccessFineLocation) == (int)Permission.Granted)
            {
                string dbPath = System.IO.Path.Combine(
                System.Environment.GetFolderPath(System.Environment.SpecialFolder.Personal),
                "localscandata.db3");
                db = new SQLiteConnection(dbPath);
                // Create the ParcelScans table
                db.CreateTable<DespatchBayExpressDataBase.ParcelScans>();
                //db.DeleteAll<DespatchBayExpressDataBase.ParcelScans>();

                mediaPlayer = MediaPlayer.Create(this, Resource.Raw.beep_07);

                mBarcodeScannerList = new BarcodeScannerList();
                SetContentView(Resource.Layout.activity_main);
                
                mRecyclerView = FindViewById<RecyclerView>(Resource.Id.recyclerView);

                // Plug in the linear layout manager:
                mLayoutManager = new LinearLayoutManager(this);
                mRecyclerView.SetLayoutManager(mLayoutManager);

                // Plug in my adapter:
                mAdapter = new TrackingNumberDataAdapter(mBarcodeScannerList);
                mRecyclerView.SetAdapter(mAdapter);
                // We have permission, go ahead and use the GPS.
                Log.Debug(TAG, "We have permission, go ahead and use the GPS.");
                InitializeLocationManager();
                
                coords = FindViewById<TextView>(Resource.Id.coords);
                Android.Support.V7.Widget.Toolbar toolbar = FindViewById<Android.Support.V7.Widget.Toolbar>(Resource.Id.toolbar);
                SetSupportActionBar(toolbar);
               
                TrackingScan = FindViewById<EditText>(Resource.Id.txtentry);
                TrackingScan.Text = "";
                TrackingScan.RequestFocus();

                TrackingScan.KeyPress += (object sender, View.KeyEventArgs e) =>
                {
                    if ((e.Event.Action == KeyEventActions.Down) && (e.KeyCode == Keycode.Enter))
                    {
                        if (e.Event.RepeatCount == 0)
                        {
                            /// need to regex the scan against the Tracking Patterns
                            /// 
                            TableQuery<TrackingNumberPatterns> trackingPatterns = db.Table<TrackingNumberPatterns>();
                            
                            bool patternFound = false;
                            
                            try
                            {
                                foreach (var trackingPattern in trackingPatterns)
                                {
                                    Match m = Regex.Match(@TrackingScan.Text, @trackingPattern.Pattern, RegexOptions.IgnoreCase);
                                    if (m.Success)
                                    {
                                        patternFound = true;
                                    }
                                }
                            }
                            catch { }
                            
                            if (patternFound)
                            {
                                var newScan = new DespatchBayExpressDataBase.ParcelScans();
                                newScan.TrackingNumber = TrackingScan.Text.ToUpper();
                                newScan.ScanTime = DateTime.Now.ToString("yyyy -MM-ddTHH:mm:ss");
                                newScan.Sent = null;
                                try {
                                    newScan.Longitude = currentLocation.Longitude;
                                }
                                catch {
                                    newScan.Longitude = null;
                                }
                                try {
                                    newScan.Latitude = currentLocation.Latitude;
                                }
                                catch {
                                    newScan.Latitude = null;
                                }
                                try
                                {
                                    db.Insert(newScan);
                                    mBarcodeScannerList.FetchBarcodeList();
                                    mAdapter.NotifyDataSetChanged();
                                    mRecyclerView.RefreshDrawableState();
                                    mediaPlayer.Start();
                                }
                                catch (SQLiteException ex) { Toast.MakeText(this, "Scan Error :" + ex.Message, ToastLength.Short).Show(); }
                            }
                            else
                            {
                                Toast.MakeText(this, "Barcode format not recognised", ToastLength.Short).Show();
                            }
                            
                            TrackingScan.RequestFocus();
                            TrackingScan.Text = "";
                        }
                    }
                };
                

                
                ToggleButton togglebutton = FindViewById<ToggleButton>(Resource.Id.togglebutton);

                togglebutton.Click += (o, e) =>
                {
                    // Perform action on clicks
                    if (togglebutton.Checked) {

                    }
                    
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
                                       new Action<View>(delegate (View obj)
                                       {
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


        /*
         * Menu Creation
         * 
         * 
         * */
        public override bool OnCreateOptionsMenu(IMenu menu)
        {
            MenuInflater.Inflate(Resource.Menu.menu_main, menu);
            return true;
        }

        public override bool OnOptionsItemSelected(IMenuItem item)
        {
            switch (item.ItemId)
            {
                case Resource.Id.menu_location:
                    Toast.MakeText(this, "Not implemented", ToastLength.Short).Show();
                    break;
                case Resource.Id.menu_settings:
                    StartActivity(typeof(SettingsActivity));
                    break;
                case Resource.Id.menu_about:
                    StartActivity(typeof(AboutActivity));
                    break;
                case Resource.Id.menu_sqldata:
                    StartActivity(typeof(SqliteActivity));
                    break;
                case Resource.Id.menu_exit:
                    this.FinishAffinity();
                    break;
                case Resource.Id.menu_upload:
                    Toast.MakeText(this, "Not implemented", ToastLength.Short).Show();
                    /*
                    // This code might be called from within an Activity, for example in an event
                    // handler for a button click.
                    Intent downloadIntent = new Intent(this, typeof(DemoIntentService));

                    // This is just one example of passing some values to an IntentService via the Intent:
                    downloadIntent.PutExtra("file_to_download", "http://www.somewhere.com/file/to/download.zip");

                    StartService(downloadIntent);
                    */
                    break;
                case Resource.Id.menu_sqldatadelete:
                    db.DeleteAll<DespatchBayExpressDataBase.ParcelScans>();
                    // Ugly and brutal way to redraw current view
                    this.Recreate();
                    break;

            }

            return base.OnOptionsItemSelected(item);
        }

        /*
         * This service function handles out of thread work
         * 
         * 
         * */
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

        /*
         * From here on these functions releate to GPS and GPS permissions
         * 
         * 
         *
         **/
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
                TrackingScan.SetBackgroundColor(Android.Graphics.Color.LightPink);
                coords.Text = "No GPS fix yet";
                //Error Message  
            }
            else
            {
                TrackingScan.SetBackgroundColor(Android.Graphics.Color.LightGreen);
                coords.Text = "Lat:" + currentLocation.Latitude.ToString(("#.00000")) + " / Long:" + currentLocation.Longitude.ToString(("#.00000"));
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
            try
            {
                locationManager.RemoveUpdates(this);
            }
            catch (Exception ex)
            {
                Log.Debug(TAG, "Error creating location service: " + ex.Message);
            }
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

    }
}
