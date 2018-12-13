using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;

namespace DespatchBayExpress
{
    public class TrackingNumberViewHolder : Android.Support.V7.Widget.RecyclerView.ViewHolder
    {
     //   public ImageView Image { get; private set; }
        public TextView Caption { get; private set; }
        public TrackingNumberViewHolder(View itemView) : base(itemView)
        {
            // Locate and cache view references:
            Caption = itemView.FindViewById<TextView>(Resource.Id.textView);
        }
    }
}