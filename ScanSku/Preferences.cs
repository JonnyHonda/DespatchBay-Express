using System;
using Android.Content;
using Android.Preferences;

namespace DespatchBayExpress
{
    /// <summary>
    /// App preferences.
    /// </summary>
    public class AppPreferences
    {
        private ISharedPreferences mSharedPrefs;
        private ISharedPreferencesEditor mPrefsEditor;
        private Context mContext;

        //private static String PREFERENCE_ACCESS_KEY = "PREFERENCE_ACCESS_KEY";

        public AppPreferences(Context context)
        {
            this.mContext = context;
            mSharedPrefs = PreferenceManager.GetDefaultSharedPreferences(mContext);
            mPrefsEditor = mSharedPrefs.Edit();
        }

        /// <summary>
        /// Saves the access key.
        /// </summary>
        /// <param name="preferenceAccessKey">Preference access key.</param>
        /// <param name="value">Value.</param>
        /// <param name="is_mandatory">If set to <c>true</c> is mandatory.</param>
        public void saveAccessKey(String preferenceAccessKey,string value, bool is_mandatory = false)
        {
            if (string.IsNullOrEmpty(value) && is_mandatory == true)
            {
                throw new ArgumentException(preferenceAccessKey + " cannot be null or empty string", nameof(value));
            }
            else
            {
                mPrefsEditor.PutString(preferenceAccessKey, value);
                mPrefsEditor.Commit();

            }

        }

        /// <summary>
        /// Gets the access key.
        /// </summary>
        /// <returns>The access key.</returns>
        /// <param name="preferenceAccessKey">Preference access key.</param>
        public string getAccessKey(String preferenceAccessKey)
        {
            return mSharedPrefs.GetString(preferenceAccessKey, "");
        }

        /// <summary>
        /// Clears the prefs.
        /// </summary>
        public void clearPrefs(){
            mPrefsEditor.Clear().Commit(); 
        }

    }

}

