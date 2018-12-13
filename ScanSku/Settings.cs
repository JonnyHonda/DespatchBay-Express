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

namespace DespatchBayExpress
{
    [Activity(Label = "@string/app_name", Theme = "@style/AppTheme.NoActionBar", MainLauncher = false)]
    public class Settings : AppCompatActivity
    {
        SQLiteConnection db = null;

        protected override void OnCreate(Bundle savedInstanceState)
        {
            RequestedOrientation = ScreenOrientation.Portrait;
            base.OnCreate(savedInstanceState);

            SetContentView(Resource.Layout.Settings);
            Android.Support.V7.Widget.Toolbar toolbar = FindViewById<Android.Support.V7.Widget.Toolbar>(Resource.Id.toolbar);
            SetSupportActionBar(toolbar);
            string JsonTrackingRegexs = @"[{
                                            ""royal-mail"": ""([A-Z]{2}[0-9]{9}GB)"",
                                            ""parcelforce-international"": ""((EK|CK){2}[0-9]{9}GB)"",
                                            ""parcelforce-domestic"": ""(PB[A-Z]{2}[0-9]{10})"",
                                            ""yodel"": ""(JJD[0-9]{16})"",
                                            ""dhl"": ""(JD[0-9]{18})"",
                                            ""whistl"": ""(WSLL10064[0-9]{8})"",
                                            ""dx-freight"": ""\b(51[0-9]{10})\b"",
                                            ""dx-secure"": ""\b([1-9]{1}[0-9]{9})\b""
                                        }]";

            string dbPath = System.IO.Path.Combine(
                System.Environment.GetFolderPath(System.Environment.SpecialFolder.Personal),
                "localscandata.db3");
            db = new SQLiteConnection(dbPath);
            try
            {
                db.DeleteAll<DespatchBayExpressDataBase.TrackingNumberPatterns>();
            }
            catch { }
            
            db.CreateTable<DespatchBayExpressDataBase.TrackingNumberPatterns>();
            
            List<Dictionary<string, string>> obj = JsonConvert.DeserializeObject<List<Dictionary<string, string>>>(JsonTrackingRegexs);

            foreach (Dictionary<string, string> lst in obj)
            {
                foreach (KeyValuePair<string, string> item in lst)
                {
                    var record = new DespatchBayExpressDataBase.TrackingNumberPatterns
                    {
                        Courier = item.Key,
                        Pattern = item.Value,
                        IsEnabled = true
                    };
                    db.Insert(record);
                }
            }

            TextView Tv = FindViewById<TextView>(Resource.Id.TEXT_STATUS_ID);

            Tv.Text = "";
            TableQuery<DespatchBayExpressDataBase.TrackingNumberPatterns> patterns = db.Table<DespatchBayExpressDataBase.TrackingNumberPatterns>();

            Tv.Append("======= TrackingNumberPatterns ==========");
            Tv.Append(System.Environment.NewLine);
            try
            {
                foreach (var pattern in patterns)
                {
                    Tv.Append(pattern.ToString());
                    Tv.Append(System.Environment.NewLine);
                }
            }
            catch { }

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
                    StartActivity(typeof(About));
                    break;
            }
            return base.OnOptionsItemSelected(item);
        }
    }
    /*
    public class Regexes
    {
        public string Key { get; set; }
        public string Value { get; set; }
    }
    */

}