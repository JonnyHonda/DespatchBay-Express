using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Support.V7.Widget;
using Android.Views;
using Android.Widget;

namespace DespatchBayExpress
{
    public class TrackingNumberDataAdapter : RecyclerView.Adapter
    {
        public BarcodeScannerList mBarcodeScannerList;
        public TrackingNumberDataAdapter(BarcodeScannerList bcl)
        {
            mBarcodeScannerList = bcl;
        }

        public override RecyclerView.ViewHolder
            OnCreateViewHolder(ViewGroup parent, int viewType)
        {
            View itemView = LayoutInflater.From(parent.Context).
                        Inflate(Resource.Layout.recycler_view_item
                        , parent, false);
            TrackingNumberViewHolder vh = new TrackingNumberViewHolder(itemView);
            return vh;
        }

        public override void
            OnBindViewHolder(RecyclerView.ViewHolder holder, int position)
        {
            TrackingNumberViewHolder vh = holder as TrackingNumberViewHolder;
            try
            {
                vh.Caption.Text = mBarcodeScannerList[position].Caption;
            }
            catch { vh.Caption.Text = "Missing Tracking Number"; }
        }

        public override int ItemCount
        {
            get { return mBarcodeScannerList.NumBarcodes; }
        }
    }
}