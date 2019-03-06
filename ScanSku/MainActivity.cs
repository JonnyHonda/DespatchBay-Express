using Android;
using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.Locations;
using Android.Media;
using Android.OS;
using Android.Runtime;
using Android.Support.Design.Widget;
using Android.Support.V4.App;
using Android.Support.V4.Content;
using Android.Support.V7.App;
using Android.Support.V7.Widget;
using Android.Util;
using Android.Views;
using Android.Widget;
using Newtonsoft.Json;
using SQLite;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using static DespatchBayExpress.DespatchBayExpressDataBase;
using Permission = Android.Content.PM.Permission;

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
        Guid batch;
        string batchnumber;

        protected override void OnCreate(Bundle savedInstanceState)
        {
            RequestedOrientation = ScreenOrientation.Portrait;
            Context applicationContext = Application.Context;
            AppPreferences applicationPreferences = new AppPreferences(applicationContext);
            // Check application Preferences have been saved previously if not open Settings Activity and wait there.
            if (
                string.IsNullOrEmpty(applicationPreferences.GetAccessKey("submitDataUrl")) ||
                string.IsNullOrEmpty(applicationPreferences.GetAccessKey("loadConfigUrl")) ||
                string.IsNullOrEmpty(applicationPreferences.GetAccessKey("applicationKey")) ||
                string.IsNullOrEmpty(applicationPreferences.GetAccessKey("retentionPeriod"))
                )
            {
                // No, well start the setting activity
                StartActivity(typeof(SettingsActivity));
            }
            base.OnCreate(savedInstanceState);
            SetContentView(Resource.Layout.activity_main);
            Android.Support.V7.Widget.Toolbar toolbar = FindViewById<Android.Support.V7.Widget.Toolbar>(Resource.Id.toolbar);
            SetSupportActionBar(toolbar);
            // We only want to create a batch number here once when the app first starts and not everytime the activity loads
            if (batch == Guid.Empty)
            {
                SetBatchNumber(false);

            }
            databasePath = System.IO.Path.Combine(
                System.Environment.GetFolderPath(System.Environment.SpecialFolder.Personal),
                "localscandata.db3");
            databaseConnection = new SQLiteConnection(databasePath);
            // Create the ParcelScans table
            databaseConnection.CreateTable<DespatchBayExpressDataBase.ParcelScans>();
            if (ContextCompat.CheckSelfPermission(this, Manifest.Permission.AccessFineLocation) == (int)Permission.Granted)
            {
                mediaPlayer = MediaPlayer.Create(this, Resource.Raw.beep_07);
                TrackingNumberDataProvider();

                // We have permission, go ahead and use the GPS.
                Log.Debug("GPS", "We have permission, go ahead and use the GPS.");
                InitializeLocationManager();

                coordinates = FindViewById<TextView>(Resource.Id.footer_text);


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
                                var newScan = new DespatchBayExpressDataBase.ParcelScans
                                {
                                    TrackingNumber = TrackingScan.Text.ToUpper(),
                                    ScanTime = DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss"),
                                    Batch = batchnumber,
                                    Sent = null
                                };
                                try
                                {
                                    newScan.Longitude = currentLocation.Longitude;
                                }
                                catch
                                {
                                    newScan.Longitude = null;
                                }
                                try
                                {
                                    newScan.Latitude = currentLocation.Latitude;
                                }
                                catch
                                {
                                    newScan.Latitude = null;
                                }
                                try
                                {
                                    databaseConnection.Insert(newScan);
                                    mBarcodeScannerList.FetchUnCollected();
                                    mAdapter.NotifyDataSetChanged();
                                    mRecyclerView.RefreshDrawableState();
                                    mediaPlayer.Start();
                                }
                                catch (SQLiteException ex)
                                {
                                    Toast.MakeText(this, "Scan Error : Duplicated Barcode Scan", ToastLength.Long).Show();
                                    Log.Info("SCANNER", "Scan Error : " + ex.Message);

                                }
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
                    ActivityCompat.RequestPermissions(this, requiredPermissions, REQUEST_LOCATION);
                }
                else
                {
                    ActivityCompat.RequestPermissions(this, new String[] { Manifest.Permission.AccessFineLocation }, REQUEST_LOCATION);
                }

            }
        }

        /// <summary>
        /// Provides the data adapter for the RecyclerView
        /// This simple gets all the current tracking numbers and populates the recycler
        /// </summary>
        private void TrackingNumberDataProvider()
        {
            mBarcodeScannerList = new BarcodeScannerList();
            mBarcodeScannerList.FetchUnCollected();
            mRecyclerView = FindViewById<RecyclerView>(Resource.Id.recyclerView);
            ScrollView scrollView = FindViewById<ScrollView>(Resource.Id.scroll_view);
            // Plug in the linear layout manager:
            mLayoutManager = new LinearLayoutManager(this, LinearLayoutManager.Vertical, true);
            mLayoutManager.ScrollToPosition(0);
            mRecyclerView.SetLayoutManager(mLayoutManager);
            mRecyclerView.ScrollToPosition(0);
            // Plug in my adapter:
            mAdapter = new TrackingNumberDataAdapter(mBarcodeScannerList);
            mRecyclerView.SetAdapter(mAdapter);
            mRecyclerView.ScrollToPosition(0);
        }

        private void SetBatchNumber(bool regenerate)
        {
            Context mContext = Application.Context;
            AppPreferences applicationPreferences = new AppPreferences(mContext);
            if (string.IsNullOrEmpty(applicationPreferences.GetAccessKey("batchnumber")) || regenerate)
            {
                batch = Guid.NewGuid();
                applicationPreferences.SaveAccessKey("batchnumber", batch.ToString());
            }

            batchnumber = applicationPreferences.GetAccessKey("batchnumber");
        }


        /*
         * Menu Creation
         *  
         *  Inflate Menu and assign
         * */
        public override bool OnCreateOptionsMenu(IMenu menu)
        {
            MenuInflater.Inflate(Resource.Menu.menu_main, menu);
            return true;
        }

        /// <summary>
        /// Menu options
        /// </summary>
        /// <param name="item"></param>
        /// <returns></returns>
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
                case Resource.Id.menu_exportdata:
                    ExportScanData();
                    break;
                case Resource.Id.menu_exit:
                    // This should exit the app
                    this.FinishAffinity();
                    break;
                case Resource.Id.menu_upload:
                    // Begin the process of uploading the data
                    Context mContext = Application.Context;
                    AppPreferences ap = new AppPreferences(mContext);
                    string httpEndPoint = ap.GetAccessKey("submitDataUrl");
                    string loadConfigUrl = ap.GetAccessKey("loadConfigUrl");
                    string applicationKey = ap.GetAccessKey("applicationKey");

                    string retentionPeriod = ap.GetAccessKey("retentionPeriod");

                    // Create a Dictionary for the parameters
                    Dictionary<string, string> Parameters = new Dictionary<string, string>
                    {
                        { "httpEndPoint", httpEndPoint },
                        { "userAgent", "Man-In-VAN Handheld Device" },
                        { "token", applicationKey },
                        { "retentionPeriod", retentionPeriod },

                    };
                    try
                    {
                        Parameters.Add("serialNumber", ap.GetAccessKey("serialNumber"));
                    }
                    catch
                    {
                        Parameters.Add("serialNumber", "");
                    }
                    try
                    {
                        Parameters.Add("lontitude", currentLocation.Longitude.ToString());
                        Parameters.Add("latitude", currentLocation.Latitude.ToString());
                    }
                    catch
                    {
                        Parameters.Add("lontitude", "");
                        Parameters.Add("latitude", "");
                    }
                    Parameters.Add("databasePath", databasePath);

                    bool status = false;
                    try
                    {
                        // Run the SubmitCollectionData as a Async Task
                        System.Threading.Tasks.Task taskA = System.Threading.Tasks.Task.Factory.StartNew(() => status = SubmitCollectionData(Parameters));
                        taskA.Wait();
                    }
                    catch (Exception ex)
                    {
                        Log.Info("SubmitCollectionData", ex.Message);
                    }

                    if (status == false)
                    {
                        Toast.MakeText(this, "There was a problem with the upload", ToastLength.Long).Show();
                        // Instantiate the builder and set notification elements:
                        Notification.Builder builder = null;
                        try
                        {
                            builder = new Notification.Builder(this, "NOTI_CH_ID");
                        }
                        catch
                        {
                            builder = new Notification.Builder(this);
                        }

                        builder.SetContentTitle("Failed Uploads");
                        builder.SetContentText("There are uploads that may have failed.");
                        builder.SetSmallIcon(Resource.Mipmap.ic_warning_black_24dp);


                        // Build the notification:
                        Notification notification = builder.Build();

                        // Get the notification manager:
                        NotificationManager notificationManager = GetSystemService(Context.NotificationService) as NotificationManager;
                        const int notificationId = 0;
                        notificationManager.Notify(notificationId, notification);
                    }
                    else
                    {
                        Toast.MakeText(this, "Upload complete", ToastLength.Long).Show();
                    }

                    TrackingNumberDataProvider();

                    // Create a new Batch number;
                    SetBatchNumber(true);
                    break;
                case Resource.Id.menu_sqldatadelete:
                    databaseConnection.DeleteAll<DespatchBayExpressDataBase.ParcelScans>();
                    TrackingNumberDataProvider();
                    break;

            }

            return base.OnOptionsItemSelected(item);
        }

        private void ExportScanData()
        {
            if (ContextCompat.CheckSelfPermission(this, Manifest.Permission.WriteExternalStorage) == (int)Permission.Granted ||
                ContextCompat.CheckSelfPermission(this, Manifest.Permission.ReadExternalStorage) == (int)Permission.Granted)
            {


                var parcelScans = databaseConnection.Query<DespatchBayExpressDataBase.ParcelScans>("SELECT * FROM ParcelScans");

                string fileName = DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss") + ".csv";
                // Set a variable to the Documents path.
                string docPath = Path.Combine(Android.OS.Environment.ExternalStorageDirectory.AbsolutePath, Android.OS.Environment.DirectoryDownloads);
                string filepath = (Path.Combine(docPath, fileName));
                using (StreamWriter outputFile = new StreamWriter(filepath))
                {
                    foreach (ParcelScans parcelScan in parcelScans)
                        outputFile.WriteLine(parcelScan.ToCSV());
                }

                // Notify the user about the completed "download"
                var downloadManager = DownloadManager.FromContext(Android.App.Application.Context);
                downloadManager.AddCompletedDownload(fileName, "DespatchBay Express Export", true, "application/txt", filepath, File.ReadAllBytes(filepath).Length, true);
            }
            else

                ActivityCompat.RequestPermissions(this, new string[] { Manifest.Permission.WriteExternalStorage, Manifest.Permission.ReadExternalStorage }, 2);

        }

        private bool SubmitCollectionData(Dictionary<string, string> parameters)
        {
            Log.Info("TAG-ASYNCTASK", "Beginning SubmitCollectionData");
            string startTime = DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss");
            string httpEndPoint = parameters["httpEndPoint"];
            string lontitude = parameters["lontitude"];
            string latitude = parameters["latitude"];
            string userAgent = parameters["userAgent"];
            string token = parameters["token"];
            string setialNumber = parameters["serialNumber"];
            string retentionPeriod = parameters["retentionPeriod"];

            bool status = true;
            string databasePath = parameters["databasePath"];
            Log.Info("TAG-ASYNCTASK", "Connect to Database");

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

            // Fetch all the batches that have not been uploaded
            var batchnumbers = databaseConnection.Query<DespatchBayExpressDataBase.ParcelScans>("SELECT Batch FROM ParcelScans WHERE Sent IS null GROUP BY Batch");

            foreach (var batch in batchnumbers)
            {
                collection.Gps = collectionLocation;
                collection.batchnumber = batch.Batch;
                collection.Timestamp = DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss");
                Log.Info("TAG-ASYNCTASK", "Collection created");

                // regardless of whether we get a successful upload we still must flag the items as being collected, 
                // the assumtion being that they will have been taken away even if the dirver did not upload the collection 
                var parcelScans = databaseConnection.Query<DespatchBayExpressDataBase.ParcelScans>("UPDATE ParcelScans set IsCollected = 1  WHERE Sent IS null and batch=?", collection.batchnumber);

                // Need to select all the scans that have not been uploaded and match the current batch
                parcelScans = databaseConnection.Query<DespatchBayExpressDataBase.ParcelScans>("SELECT * FROM ParcelScans WHERE Sent IS null and batch=?", collection.batchnumber);

                List<Scan> scannedParcelList = new List<Scan>();

                foreach (var parcel in parcelScans)
                {
                    Scan scannedParcelListElement = new Scan();
                    Gps scannedParcelLocation = new Gps();
                    scannedParcelListElement.Timestamp = parcel.ScanTime;
                    // Because Locations can be null
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
                Log.Info("TAG-ASYNCTASK", "JSON Created");
                jsonToUpload = collection.ToJson();
                Log.Info("TAG-ASYNCTASK", jsonToUpload);
                Log.Info("TAG-ASYNCTASK", "Webrequest Created");
                var httpWebRequest = (HttpWebRequest)WebRequest.Create(httpEndPoint);
                httpWebRequest.ContentType = "application/json";
                httpWebRequest.Method = "POST";
                httpWebRequest.UserAgent += userAgent;
                httpWebRequest.Headers["x-db-api-key"] = token;
                httpWebRequest.Headers["x-db-batch"] = collection.batchnumber;
                httpWebRequest.Headers["x-db-serial-number"] = setialNumber;

                try
                {
                    using (var streamWriter = new StreamWriter(httpWebRequest.GetRequestStream()))
                    {
                        streamWriter.Write(jsonToUpload);
                        streamWriter.Flush();
                        streamWriter.Close();
                    }
                    Log.Info("TAG-ASYNCTASK", "Fetch Response");

                    HttpWebResponse httpResponse = (HttpWebResponse)httpWebRequest.GetResponse();


                    using (var streamReader = new StreamReader(httpResponse.GetResponseStream()))
                    {
                        var jsonResult = streamReader.ReadToEnd();
                        RemoteServiceResult result = new RemoteServiceResult();
                        result = JsonConvert.DeserializeObject<RemoteServiceResult>(jsonResult);
                        Log.Info("TAG-ASYNCTASK", jsonResult);

                        if (httpResponse.StatusCode == HttpStatusCode.OK)
                        {
                            Log.Info("TAG-ASYNCTASK", "Success, update parcels");
                            parcelScans = databaseConnection.Query<DespatchBayExpressDataBase.ParcelScans>("UPDATE ParcelScans set Sent=? WHERE Sent IS null and batch=?", startTime, collection.batchnumber);
                        }
                        else
                        {
                            Log.Info("TAG-ASYNCTASK", "Did recieve a success response");

                        }
                    }
                    httpResponse.Close();
                    Log.Info("TAG-ASYNCTASK", "Response Closes");
                }
                catch (Exception ex)
                {
                    Log.Info("TAG-ASYNCTASK", "Response Failed");
                    Log.Info("TAG-ASYNCTASK", ex.Message);
                    status = false;
                }

            }
            Int16 days = Convert.ToInt16(retentionPeriod);
            var dateTime = DateTime.Now.AddDays(-days);
            var deleteScans = databaseConnection.Query<DespatchBayExpressDataBase.ParcelScans>("DELETE FROM ParcelScans WHERE Sent <= ?", dateTime.ToString("yyyy-MM-ddTHH:mm:ss"));

            Log.Info("TAG-ASYNCTASK", "Work complete");

            return status;
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

            }
            else if (requestCode == 2 || requestCode == 3)
            {
                ExportScanData();
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
