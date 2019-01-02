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
using System.Net;
using System.IO;
using System.Net.Http;
using Android.Views.InputMethods;

namespace DespatchBayExpress
{
    
    [Activity(WindowSoftInputMode = SoftInput.StateAlwaysHidden, Label = "@string/app_name", Theme = "@style/AppTheme.NoActionBar", MainLauncher = true)]
    public class MainActivity : AppCompatActivity, ILocationListener
    {
        static readonly int REQUEST_LOCATION = 1;
        // static readonly Keycode SCAN_BUTTON = (Keycode)301;
        SQLiteConnection databaseConnection = null;
        string databasePath;

        TextView coordinates;
        Location currentLocation;
        LocationManager locationManager;
        string locationProvider;
  
        MediaPlayer mediaPlayer;
        
        RecyclerView mRecyclerView;
        RecyclerView.LayoutManager mLayoutManager;
        TrackingNumberDataAdapter mAdapter;
        BarcodeScannerList mBarcodeScannerList;
        EditText TrackingScan;

        static bool GLOBAL_RECYCLEVIEW_REFRESHED = false;

        protected override void OnCreate(Bundle savedInstanceState)
        {
            RequestedOrientation = ScreenOrientation.Portrait;
            Context mContext = Application.Context;
            AppPreferences applicationPreferences = new AppPreferences(mContext);
            // Check application Preferences have been saved previously
            if (
                string.IsNullOrEmpty(applicationPreferences.getAccessKey("submitDataUrl")) ||
                string.IsNullOrEmpty(applicationPreferences.getAccessKey("loadConfigUrl")) ||
                string.IsNullOrEmpty(applicationPreferences.getAccessKey("applicationKey"))
                )
            {
                // No, well start the setting activity
                StartActivity(typeof(SettingsActivity));
            }
            base.OnCreate(savedInstanceState);

            databasePath = System.IO.Path.Combine(
                System.Environment.GetFolderPath(System.Environment.SpecialFolder.Personal),
                "localscandata.db3");
            databaseConnection = new SQLiteConnection(databasePath);
            if (ContextCompat.CheckSelfPermission(this, Manifest.Permission.AccessFineLocation) == (int)Permission.Granted)
            {
                
                // Create the ParcelScans table
                databaseConnection.CreateTable<DespatchBayExpressDataBase.ParcelScans>();

                mediaPlayer = MediaPlayer.Create(this, Resource.Raw.beep_07);

                /// This Time waits for the flag GLOBAL_RECYCLEVIEW_REFRESHED to become true
                System.Timers.Timer threadTimer = new System.Timers.Timer();
                threadTimer.Start();
                threadTimer.Interval = 2000;
                threadTimer.Enabled = true;
                threadTimer.Elapsed += (object sender, System.Timers.ElapsedEventArgs e) =>
                {
                    RunOnUiThread(() =>
                    {
                        if (GLOBAL_RECYCLEVIEW_REFRESHED) { 
                           GLOBAL_RECYCLEVIEW_REFRESHED = false;
                            Toast.MakeText(Application.Context, "Upload Complete", ToastLength.Long).Show();
                            this.Recreate();
                         }
                    });
                };

                mBarcodeScannerList = new BarcodeScannerList();
                SetContentView(Resource.Layout.activity_main);
                
                mRecyclerView = FindViewById<RecyclerView>(Resource.Id.recyclerView);

                // Plug in the linear layout manager:
                mLayoutManager = new LinearLayoutManager(this, LinearLayoutManager.Vertical,false);
                mRecyclerView.SetLayoutManager(mLayoutManager);
                
                // Plug in my adapter:
                mAdapter = new TrackingNumberDataAdapter(mBarcodeScannerList);
                mRecyclerView.SetAdapter(mAdapter);
                // We have permission, go ahead and use the GPS.
                Log.Debug("GPS", "We have permission, go ahead and use the GPS.");
                InitializeLocationManager();
                
                coordinates = FindViewById<TextView>(Resource.Id.footer_text);
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
                            TableQuery<TrackingNumberPatterns> trackingPatterns = databaseConnection.Table<TrackingNumberPatterns>();
                            
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
                                newScan.ScanTime = DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss");
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
                                    databaseConnection.Insert(newScan);
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
                
            }
            else
            {
                // GPS permission is not granted. If necessary display rationale & request.
                Log.Debug("GPS", "GPS permission is not granted");

                if (ActivityCompat.ShouldShowRequestPermissionRationale(this, Manifest.Permission.AccessFineLocation))
                {
                    // Provide an additional rationale to the user if the permission was not granted
                    // and the user would benefit from additional context for the use of the permission.
                    // For example if the user has previously denied the permission.
                    Log.Info("GPS", "Displaying GPS permission rationale to provide additional context.");
                    var rootView = FindViewById<CoordinatorLayout>(Resource.Id.root_view);


                    var requiredPermissions = new String[] { Manifest.Permission.AccessFineLocation };
                    Snackbar.Make(rootView,
                                   Resource.String.permission_location_rationale,
                                   Snackbar.LengthIndefinite)
                            .SetAction("OK",
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
                    Toast.MakeText(this, "Bare with...", ToastLength.Short).Show();
                    Context mContext = Application.Context;
                    AppPreferences ap = new AppPreferences(mContext);
                    string httpEndPoint = ap.getAccessKey("submitDataUrl");
                    string loadConfigUrl = ap.getAccessKey("loadConfigUrl");
                    string applicationKey = ap.getAccessKey("applicationKey");

                    // This code might be called from within an Activity, for example in an event
                    // handler for a button click.
                    Intent submitDataIntent = new Intent(this, typeof(SubmitDataIntentService));
                    
                    // Pass some vars to the Intent
                    submitDataIntent.PutExtra("httpEndPoint", httpEndPoint);
                    submitDataIntent.PutExtra("userAgent", "Man-In-VAN Handheld Device");
                    submitDataIntent.PutExtra("token", applicationKey);
                    try
                    {
                        submitDataIntent.PutExtra("lontitude", currentLocation.Longitude);
                        submitDataIntent.PutExtra("latitude", currentLocation.Latitude);
                    }
                    catch
                    {
                        submitDataIntent.PutExtra("lontitude", "");
                        submitDataIntent.PutExtra("latitude", "");
                    }
                    submitDataIntent.PutExtra("databasePath", databasePath);

                    StartService(submitDataIntent);
                    break;
                case Resource.Id.menu_sqldatadelete:
                    databaseConnection.DeleteAll<DespatchBayExpressDataBase.ParcelScans>();
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
        public class SubmitDataIntentService : IntentService
        {
            public SubmitDataIntentService() : base("SubmitDataIntentService")
            {
            }

            protected override void OnHandleIntent(Android.Content.Intent intent)
            {
                Log.Info("TAG-INTENT", "INTENT - Perform some long running work");
                string startTime = DateTime.Now.ToString("yyyy -MM-ddTHH:mm:ss");
                string httpEndPoint = intent.GetStringExtra("httpEndPoint");
                string lontitude = intent.GetStringExtra("lontitude");
                string latitude = intent.GetStringExtra("latitude");
                string userAgent = intent.GetStringExtra("userAgent");
                string token = intent.GetStringExtra("token");
               
                string databasePath = intent.GetStringExtra("databasePath");
                Log.Info("TAG-INTENT", "INTENT - Connect to Database");
                SQLiteConnection databaseConnection = new SQLiteConnection(databasePath);
                // Create a new Collection
                Collection collection = new Collection();
                // Set the Base values
                Gps collectionLocation = new Gps();
                try
                {
                    collectionLocation.Latitude = Convert.ToDouble(latitude);
                    collectionLocation.Longitude = Convert.ToDouble(lontitude);
                }
                catch { }
                collection.Gps = collectionLocation;

                collection.Timestamp = DateTime.Now.ToString("yyyy -MM-ddTHH:mm:ss");
                Log.Info("TAG-INTENT", "INTENT - Collection created");
                
                var parcelScans = databaseConnection.Query<DespatchBayExpressDataBase.ParcelScans>("SELECT * FROM ParcelScans WHERE Sent IS null");              
                List<Scan> scannedParcelList = new List<Scan>();
                Scan scannedParcelListElement = new Scan();
                foreach (var parcel in parcelScans)
                {
                    Gps scannedParcelLocation = new Gps();
                    scannedParcelListElement.Timestamp = parcel.ScanTime;
                    try
                    {
                        scannedParcelLocation.Longitude = (double)parcel.Longitude;
                        scannedParcelLocation.Latitude = (double)parcel.Latitude;
                    }
                    catch { }
                    scannedParcelListElement.Barcode = parcel.TrackingNumber;
                    scannedParcelListElement.Gps = scannedParcelLocation;
                    scannedParcelList.Add(scannedParcelListElement);
                }
                collection.Scans = scannedParcelList;
                string jsonToUpload;
                Log.Info("TAG-INTENT", "INTENT - JSON Created");
                jsonToUpload = collection.ToJson();
                Log.Info("TAG-INTENT", "INTENT - " + jsonToUpload);
                Log.Info("TAG-INTENT", "INTENT - Webrequest Created");
                var httpWebRequest = (HttpWebRequest)WebRequest.Create(httpEndPoint);
                httpWebRequest.ContentType = "application/json";
                httpWebRequest.Method = "POST";
                httpWebRequest.UserAgent += userAgent;
                httpWebRequest.Headers["x-api-key"] = token;
                using (var streamWriter = new StreamWriter(httpWebRequest.GetRequestStream()))
                {
                    streamWriter.Write(jsonToUpload);
                    streamWriter.Flush();
                    streamWriter.Close();
                }
                Log.Info("TAG-INTENT", "INTENT - Fetch Response");
                try
                {
                    var httpResponse = (HttpWebResponse)httpWebRequest.GetResponse();


                    using (var streamReader = new StreamReader(httpResponse.GetResponseStream()))
                    {
                        var result = streamReader.ReadToEnd();

                        Log.Info("TAG-INTENT", "INTENT - " + result);
                       if (result == "\"Event type not found in event data\"")
                        {
                            parcelScans = databaseConnection.Query<DespatchBayExpressDataBase.ParcelScans>("UPDATE ParcelScans set Sent=? WHERE Sent IS null", startTime);
                        }
                    }
                    httpResponse.Close();
                    Log.Info("TAG-INTENT", "INTENT - Response Closes");
                    GLOBAL_RECYCLEVIEW_REFRESHED = true;
                }
                catch (Exception ex){
                    Log.Info("TAG-INTENT", "INTENT - Response Failed");
                    Log.Info("TAG-INTENT", ex.Message);
                }
                Log.Info("TAG-INTENT", "INTENT work complete"); 
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
                Log.Info("GPS", "Received response for Location permission request.");
                var rootView = FindViewById<CoordinatorLayout>(Resource.Id.root_view);
                // Check if the only required permission has been granted
                if ((grantResults.Length == 1) && (grantResults[0] == Permission.Granted))
                {
                    // Location permission has been granted, okay to retrieve the location of the device.
                    Log.Info("GPS", "Location permission has now been granted.");
                    // Snackbar.Make(rootView, Resource.String.permission_available_location, Snackbar.LengthShort).Show();
                    this.FinishAffinity();
                    ;
                }
                else
                {
                    Log.Info("GPS", "Location permission was NOT granted.");
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
            Log.Debug("GPS", "Using " + locationProvider + ".");
        }

        public void OnLocationChanged(Location location)
        {
            currentLocation = location;
            if (currentLocation == null)
            {
                TrackingScan.SetBackgroundColor(Android.Graphics.Color.LightPink);
                coordinates.Text = "No GPS fix yet ";
                //Error Message  
            }
            else
            {
                coordinates.Text = "Lat:" + currentLocation.Latitude.ToString(("#.00000")) + " / Long:" + currentLocation.Longitude.ToString(("#.00000"));
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
                Log.Debug("GPS", "Error creating location service: " + ex.Message);
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
                Log.Debug("GPS", "Error creating location service: " + ex.Message);
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
