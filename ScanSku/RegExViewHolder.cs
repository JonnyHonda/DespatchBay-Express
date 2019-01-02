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
    public class RegExViewHolder : Android.Support.V7.Widget.RecyclerView.ViewHolder
    {
        public TextView Courier { get; private set; }
        public TextView Regex { get; private set; }

        public RegExViewHolder(View itemView) : base(itemView)
        {
            // Locate and cache view references:
            Courier = itemView.FindViewById<TextView>(Resource.Id.courierTextView);
            Regex = itemView.FindViewById<TextView>(Resource.Id.regexTextView);
        }
    }
}