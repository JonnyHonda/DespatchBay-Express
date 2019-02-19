﻿
using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.OS;
using Android.Widget;

namespace DespatchBayExpress
{
    [Activity(Label = "@string/app_name", Theme = "@style/AppTheme.NoActionBar",MainLauncher =false)]
    public class AboutActivity : Activity
    {
        protected override void OnCreate(Bundle savedInstanceState)
        {
            RequestedOrientation = ScreenOrientation.Portrait;
            base.OnCreate(savedInstanceState);
            SetContentView(Resource.Layout.activity_about);
            var context = Android.App.Application.Context;
            PackageInfo appInfo = context.PackageManager.GetPackageInfo(context.PackageName, 0);
            TextView Version_TextView = FindViewById<TextView>(Resource.Id.version_info);
            Version_TextView.Text = "Version:" + appInfo.VersionName.ToString() +" / " + appInfo.VersionCode.ToString();
        }
    }
}
