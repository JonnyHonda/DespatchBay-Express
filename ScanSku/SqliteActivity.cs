using Android.App;
using Android.OS;
using Android.Widget;
using DespatchBayExpress;
using SQLite;
using static DespatchBayExpress.DespatchBayExpressDataBase;

namespace DespatchBayExpress
{
    [Activity(Label = "ScanSku Sqlite Data", MainLauncher = false)]
    public class SqliteActivity : Activity
    {
        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            SetContentView(Resource.Layout.SqlLayout);
            string dbPath = System.IO.Path.Combine(
                    System.Environment.GetFolderPath(System.Environment.SpecialFolder.Personal),
                    "localscandata.db3");
            var db = new SQLiteConnection(dbPath);
            var parcelScans = db.Table<ParcelScans>();
            TextView Tv = FindViewById<TextView>(Resource.Id.TEXT_STATUS_ID);

            Tv.Text = "";
            Tv.Append("======= Parcel Scans ==========");
            Tv.Append(System.Environment.NewLine);
            try
            {
                foreach (var parcelScan in parcelScans)
                {
                    Tv.Append(parcelScan.ToString());
                    Tv.Append(System.Environment.NewLine);
                }
            }catch{}
            


            Button cancel_button = FindViewById<Button>(Resource.Id.btn_sqlcancel);
            cancel_button.Click += delegate
            {
                StartActivity(typeof(MainActivity));
            };
        }

    }
}
