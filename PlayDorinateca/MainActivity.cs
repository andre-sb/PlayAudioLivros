using System;
using Android.App;
using Android.Content;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using Android.OS;
using System.Collections.Generic;
using System.IO;
using Android.Media;
using System.Threading.Tasks;

namespace PlayDorinateca
{
    [Activity(Label = "PlayDorinateca", MainLauncher = true, Icon = "@drawable/icon")]
    public class MainActivity : Activity
    {
        protected TextView txt;
        protected FileSystemInfo[] top_level;
        protected int current_dir;

        protected FileSystemInfo[] files;
        protected int current_file;

        protected MediaPlayer player = null;
        protected string filePath;

        protected override void OnCreate(Bundle bundle)
        {
            base.OnCreate(bundle);

            // Set our view from the "main" layout resource
            SetContentView(Resource.Layout.Main);

            txt = FindViewById<TextView>(Resource.Id.textView1);

            var dir = new DirectoryInfo("/sdcard/Guerra e Paz");
            top_level = dir.GetFileSystemInfos();
            current_dir = 0;

            dir = new DirectoryInfo(top_level[current_dir].FullName);
            files = dir.GetFileSystemInfos();
            current_file = 0;

            Button buttonNextFolder = FindViewById<Button>(Resource.Id.MyButton);
            buttonNextFolder.Click += delegate { NextFolder(); };

            Button buttonNextFile = FindViewById<Button>(Resource.Id.button1);
            buttonNextFile.Click += delegate { NextFile(); };

            Button buttonFim = FindViewById<Button>(Resource.Id.button2);
            buttonFim.Click += delegate { StopPlayer(); };
        }

        public void NextFolder()
        {
            StopPlayer();

            if (current_dir >= top_level.Length)
            {
                current_dir = 0;
                AddTxt("Livro reiniciado.");
            }

            AddTxt(top_level[current_dir].Name);

            var dir = new DirectoryInfo(top_level[current_dir].FullName);
            files = dir.GetFileSystemInfos();
            current_file = 0;

            current_dir++;
        }

        public void NextFile()
        {
            if (current_file >= files.Length)
            {
                NextFolder();
            }

            StopPlayer();
            filePath = files[current_file].FullName;
            AddTxt(filePath);
            StartPlayerAsync();

            current_file++;
        }

        protected void AddTxt(string s)
        {
            txt.Text += s;
            if (s.EndsWith("\n") == false)
                txt.Text += "\n";
        }

        public async Task StartPlayerAsync()
        {
            try
            {
                if (player == null)
                {
                    player = new MediaPlayer();
                }
                else
                {
                    player.Reset();
                }

                // This method works better than setting the file path in SetDataSource. Don't know why.
                Java.IO.File file = new Java.IO.File(filePath);
                Java.IO.FileInputStream fis = new Java.IO.FileInputStream(file);
                await player.SetDataSourceAsync(fis.FD);
                player.Prepare();
                player.Start();
            }
            catch (Exception ex)
            {
                AddTxt(ex.ToString().Substring(0,400));
            }
        }

        public void StopPlayer()
        {
            if ((player != null))
            {
                if (player.IsPlaying)
                {
                    player.Stop();
                }
                player.Release();
                player = null;
            }
        }
    }
}

