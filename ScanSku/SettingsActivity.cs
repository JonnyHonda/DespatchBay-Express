using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.OS;
using Android.Support.V7.App;
using Android.Support.V7.Widget;
using Android.Util;
using Android.Views;
using Android.Widget;
using Newtonsoft.Json;
using SQLite;
using System;
using System.Collections.Generic;

namespace DespatchBayExpress
{
    public partial class Configuration
    {
        [JsonProperty("UpdateConfiguration")]
        public List<UpdateConfiguration> UpdateConfiguration { get; set; }
    }

    public partial class UpdateConfiguration
    {
        [JsonProperty("UploadEndPoint")]
        public Uri UploadEndPoint { get; set; }

        [JsonProperty("RegexEndPoint")]
        public Uri RegexEndPoint { get; set; }

        [JsonProperty("ApplicationKey")]
        public Guid ApplicationKey { get; set; }
    }

    [Activity(WindowSoftInputMode = SoftInput.StateAlwaysHidden, Label = "@string/app_name", Theme = "@style/AppTheme.NoActionBar", MainLauncher = false)]
    public class SettingsActivity : AppCompatActivity
    {
        RecyclerView mRecyclerView;
        RecyclerView.LayoutManager mLayoutManager;
        RegExDataAdapter mAdapter;
        RegExList regExList;
        EditText TrackingScan;


        protected override void OnCreate(Bundle savedInstanceState)
        {
            RequestedOrientation = ScreenOrientation.Portrait;
            Context mContext = Application.Context;
            AppPreferences applicationPreferences = new AppPreferences(mContext);
            base.OnCreate(savedInstanceState);
 
            SetContentView(Resource.Layout.activity_settings);
            Android.Support.V7.Widget.Toolbar toolbar = FindViewById<Android.Support.V7.Widget.Toolbar>(Resource.Id.toolbar);
            SetSupportActionBar(toolbar);

            // Load up any stored applicationPreferences
            TextView submitDataUrl = FindViewById<TextView>(Resource.Id.submit_data_url);
            submitDataUrl.Text = applicationPreferences.GetAccessKey("submitDataUrl");
            submitDataUrl.Text = submitDataUrl.Text.TrimEnd('\r', '\n');

            TextView loadConfigUrl = FindViewById<TextView>(Resource.Id.load_config_url);
            loadConfigUrl.Text = applicationPreferences.GetAccessKey("loadConfigUrl");
            loadConfigUrl.Text = loadConfigUrl.Text.TrimEnd('\r', '\n');

            TextView applicationKey = FindViewById<TextView>(Resource.Id.application_key);
            applicationKey.Text = applicationPreferences.GetAccessKey("applicationKey");
            applicationKey.Text = applicationKey.Text.TrimEnd('\r', '\n');

            string databasePath = System.IO.Path.Combine(
                System.Environment.GetFolderPath(System.Environment.SpecialFolder.Personal),
                "localscandata.db3");
            SQLiteConnection databaseConnection = new SQLiteConnection(databasePath);
            databaseConnection.CreateTable<DespatchBayExpressDataBase.TrackingNumberPatterns>();

            /// This Timer, checks the the Recycler views datasource every 2 seconds and updates it
            /// I don't like this
            System.Timers.Timer threadTimer = new System.Timers.Timer();
            threadTimer.Start();
            threadTimer.Interval = 2000;
            threadTimer.Enabled = true;
            threadTimer.Elapsed += (object sender, System.Timers.ElapsedEventArgs e) =>
            {
                RunOnUiThread(() =>
                {
                    // Log.Debug("TAG-TIMER", "Every Two Seconds");
                    mRecyclerView = FindViewById<RecyclerView>(Resource.Id.recyclerView);

                    // Plug in the linear layout manager:
                    mLayoutManager = new LinearLayoutManager(this, LinearLayoutManager.Vertical, false);
                    mRecyclerView.SetLayoutManager(mLayoutManager);


                    // Plug in my adapter:
                    regExList = new RegExList();
                    mAdapter = new RegExDataAdapter(regExList);
                    mRecyclerView.SetAdapter(mAdapter);
                });
            };
            
            TrackingScan = FindViewById<EditText>(Resource.Id.txtentry);

            TrackingScan.Text = "";
            TrackingScan.RequestFocus();

            TrackingScan.KeyPress += (object sender, View.KeyEventArgs e) =>
            {
                if ((e.Event.Action == KeyEventActions.Down) && (e.KeyCode == Keycode.Enter))
                {
                    if (e.Event.RepeatCount == 0)
                    {

                        string jsonstring = TrackingScan.Text;
                        Configuration configuration = new Configuration();
                        try
                        {
                            configuration = JsonConvert.DeserializeObject<Configuration>(jsonstring);
                            if (configuration.UpdateConfiguration.Count == 1)
                            {
                                foreach (UpdateConfiguration configItem in configuration.UpdateConfiguration)
                                {
                                    submitDataUrl.Text = configItem.UploadEndPoint.ToString();
                                    loadConfigUrl.Text = configItem.RegexEndPoint.ToString();
                                    applicationKey.Text = configItem.ApplicationKey.ToString();
                                }
                                // Save some application preferences
                                applicationPreferences.SaveAccessKey("submitDataUrl", submitDataUrl.Text, true);
                                applicationPreferences.SaveAccessKey("loadConfigUrl", loadConfigUrl.Text, true);
                                applicationPreferences.SaveAccessKey("applicationKey", applicationKey.Text, true);
                                Log.Info("TAG-SETTINGS", "Settings - Call the Intent Service");
                                Intent submitDataIntent = new Intent(this, typeof(SubmitDataIntentService));
                                submitDataIntent.PutExtra("httpEndPoint", loadConfigUrl.Text);
                                StartService(submitDataIntent);
                                Toast.MakeText(this, "Config QR code read succesfull", ToastLength.Short).Show();
                                TrackingScan.Text = "";


                            }
                        }
                        catch(Exception ex)
                        {
                            // Any Error in the above block will cause this catch to fire - Even if the json keys don't exist
                            Toast.MakeText(this, "Config QR code not recognised", ToastLength.Long).Show();

                        }

                    }
                }
            };

    }
        
        /// <summary>
        /// An Intent service to attempt to load the Regexex from a Production url
        /// </summary>
        [Service]
        public class SubmitDataIntentService : IntentService
        {
            public SubmitDataIntentService() : base("SubmitCollectionDataIntentService")
            {
            }

            protected override void OnHandleIntent(Android.Content.Intent intent)
            {
                string jsonTrackingRegexs = null;
                string httpRegExPatternEndPoint = intent.GetStringExtra("httpEndPoint");

                string databasePath = System.IO.Path.Combine(
                System.Environment.GetFolderPath(System.Environment.SpecialFolder.Personal),
                "localscandata.db3");
                SQLiteConnection databaseConnection = new SQLiteConnection(databasePath);
                // Delete the current Regex data
                try
                {
                    Log.Info("TAG-SETTINGS", "Settings - Delete Exisiting data");
                    databaseConnection.DeleteAll<DespatchBayExpressDataBase.TrackingNumberPatterns>();
                }
                catch {
                    Log.Info("TAG-SETTINGS", "Settings - Unable to delete Exisiting data");
                }

                // Attempt to fetch the new data, on fail use a hard coded set
                try
                {
                    using (var webClient = new System.Net.WebClient())
                    {
                        jsonTrackingRegexs = webClient.DownloadString(httpRegExPatternEndPoint);
                        Log.Info("TAG-SETTINGS", "Settings - DownLoad Regexs");
                    }
                }
                catch(Exception e)
                {
                    Log.Info("TAG-SETTINGS", "Settings - Loading regexs failed");
                    jsonTrackingRegexs = "[{\"Failed\": \"/"+ e.Message +"/\"}]";
                }
                databaseConnection.CreateTable<DespatchBayExpressDataBase.TrackingNumberPatterns>();

                
                List<Dictionary<string, string>> obj = JsonConvert.DeserializeObject<List<Dictionary<string, string>>>(jsonTrackingRegexs);

                foreach (Dictionary<string, string> lst in obj)
                {
                    foreach (KeyValuePair<string, string> item in lst)
                    {
                        // @Todo: There is an ecoding bug here, in the DX numbers because /b encodes incorrectly
                        string testText = item.Value;
                        int startIndex = testText.IndexOf('/');
                        int endIndex = testText.LastIndexOf('/');
                        string patternString = testText.Substring(startIndex + 1, endIndex - startIndex - 1);
                        var record = new DespatchBayExpressDataBase.TrackingNumberPatterns
                        {
                            Courier = item.Key,
                            Pattern = patternString,
                            IsEnabled = true
                        };
                        databaseConnection.Insert(record);
                    }
                }
                   Log.Info("TAG-SETTINGS", "Settings - Intent Complete");
            }
        }

    public override bool OnCreateOptionsMenu(IMenu menu)
        {
            MenuInflater.Inflate(Resource.Menu.menu_settings, menu);
            return true;
        }

        public override bool OnOptionsItemSelected(IMenuItem item)
        {
            switch (item.ItemId)
            {
                case Resource.Id.menu_main:
                    StartActivity(typeof(MainActivity));
                    break;

                case Resource.Id.menu_about:
                    StartActivity(typeof(AboutActivity));
                    break;
            }
            return base.OnOptionsItemSelected(item);
        }
    }
}