using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Android.App;
using Android.Support.V7.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using Newtonsoft.Json;
using SQLite;
using Android.Content.PM;
using System.Net;
using Android.Util;
using static DespatchBayExpress.DespatchBayExpressDataBase;
using Android.Support.V7.Widget;

namespace DespatchBayExpress
{
    [Activity(WindowSoftInputMode = SoftInput.StateAlwaysHidden, Label = "@string/app_name", Theme = "@style/AppTheme.NoActionBar", MainLauncher = false)]
    public class SettingsActivity : AppCompatActivity
    {
        RecyclerView mRecyclerView;
        RecyclerView.LayoutManager mLayoutManager;
        RegExDataAdapter mAdapter;
        RegExList regExList;

        static bool GLOBAL_INTENT_COMPLETE = false;
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
            EditText submitDataUrl = FindViewById<EditText>(Resource.Id.edit_submit_data_url);
            submitDataUrl.Text = applicationPreferences.GetAccessKey("submitDataUrl");
            submitDataUrl.Text = submitDataUrl.Text.TrimEnd('\r', '\n');

            EditText loadConfigUrl = FindViewById<EditText>(Resource.Id.edit_load_config_url);
            loadConfigUrl.Text = applicationPreferences.GetAccessKey("loadConfigUrl");
            loadConfigUrl.Text = loadConfigUrl.Text.TrimEnd('\r', '\n');

            EditText applicationKey = FindViewById<EditText>(Resource.Id.edit_application_key);
            applicationKey.Text = applicationPreferences.GetAccessKey("applicationKey");
            applicationKey.Text = applicationKey.Text.TrimEnd('\r', '\n');

            
            mRecyclerView = FindViewById<RecyclerView>(Resource.Id.recyclerView);

            // Plug in the linear layout manager:
            mLayoutManager = new LinearLayoutManager(this, LinearLayoutManager.Vertical, false);
            mRecyclerView.SetLayoutManager(mLayoutManager);
            string databasePath = System.IO.Path.Combine(
                System.Environment.GetFolderPath(System.Environment.SpecialFolder.Personal),
                "localscandata.db3");
            SQLiteConnection databaseConnection = new SQLiteConnection(databasePath);
            databaseConnection.CreateTable<DespatchBayExpressDataBase.TrackingNumberPatterns>();

            // Plug in my adapter:
            regExList = new RegExList();
            mAdapter = new RegExDataAdapter(regExList);
            mRecyclerView.SetAdapter(mAdapter);

            Button FetchSettingsButton = FindViewById<Button>(Resource.Id.btn_settings);

                FetchSettingsButton.Click += delegate {
                // This service runs off the man thread
                GLOBAL_INTENT_COMPLETE = false;
                // Save some application preferences
                applicationPreferences.SaveAccessKey("submitDataUrl", submitDataUrl.Text, true);
                applicationPreferences.SaveAccessKey("loadConfigUrl", loadConfigUrl.Text, true);
                applicationPreferences.SaveAccessKey("applicationKey", applicationKey.Text, true);

                Log.Info("TAG-SETTINGS", "Settings - Call the Intent Service");
                Intent submitDataIntent = new Intent(this, typeof(SubmitDataIntentService));
                submitDataIntent.PutExtra("databasePath", "SomeStuff");
                StartService(submitDataIntent);
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
                string jsonTrackingRegexs;
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
                        jsonTrackingRegexs = webClient.DownloadString("http://burrin.uk/ParcelRegex.json");
                        Log.Info("TAG-SETTINGS", "Settings - DownLoad Regexs");
                    }
                }
                catch
                {
                    Log.Info("TAG-SETTINGS", "Settings - Use Hardcoded Regexs");
                    jsonTrackingRegexs = @"[{
                                      ""royal-mail"": ""/^([A-Z]{2}[0-9]{9}GB)/gi"",
                                      ""parcelforce-international"": ""/^((EK|CK){2}[0-9]{9}GB)/gi"",
                                      ""parcelforce-domestic"": ""/^(PB[A-Z]{2}[0-9]{10})/gi"",
                                      ""yodel"": ""/^(JJD[0-9]{16})/gi"",
                                      ""dhl"": ""/^(JD[0-9]{18})/gi"",
                                      ""whistl"": ""/^(WSLL10064[0-9]{8})/gi"",
                                      ""dx-freight"": ""/^(51[0-9]{10})/gi"",
                                      ""dx-secure"": ""/^([1-9]{1}[0-9]{9})/gi""
                                    }]";
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
                GLOBAL_INTENT_COMPLETE = true;
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
                    if (!GLOBAL_INTENT_COMPLETE) {
                       // Toast.MakeText(Application.Context, "Settings Intent Not complete", ToastLength.Long).Show();
                    }
                    StartActivity(typeof(MainActivity));
                    break;

                case Resource.Id.menu_about:
                    if (!GLOBAL_INTENT_COMPLETE) {
                       // Toast.MakeText(Application.Context, "Settings Intent Not complete", ToastLength.Long).Show();
                    }
                    StartActivity(typeof(AboutActivity));
                    break;
            }
            return base.OnOptionsItemSelected(item);
        }
    }
}