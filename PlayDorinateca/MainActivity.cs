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
using Android.Speech.Tts;
using Android.Content.Res;

namespace PlayDorinateca
{
    [Activity(Label = "PlayDorinateca", MainLauncher = true, Icon = "@drawable/icon")]
    public class MainActivity : Activity, TextToSpeech.IOnInitListener
    {
        protected TextView txt;
        protected FileSystemInfo[] top_level;
        protected int current_dir;

        protected FileSystemInfo[] files;
        protected int current_file;

        protected MediaPlayer player = null;
        protected string filePath;

        TextToSpeech textToSpeech;
        Java.Util.Locale lang;
        private readonly int NeedLang = 103;

        private string persist_file;

        protected override void OnCreate(Bundle bundle)
        {
            base.OnCreate(bundle);

            // Set our view from the "main" layout resource
            SetContentView(Resource.Layout.Main);

            txt = FindViewById<TextView>(Resource.Id.textView1);

            persist_file = this.FilesDir.AbsolutePath + "/CurrentBook.txt";

            try { 
                if (File.Exists(persist_file) == false)
                {
                    using (StreamWriter sr = new StreamWriter(persist_file))
                    {
                        sr.WriteLine("-1");
                        sr.WriteLine("-1");
                        sr.Flush();
                    }
                    AddTxt("File created");
                }
            }
            catch (Exception ex)
            {
                AddTxt(ex.ToString().Substring(0, 300));
            }

            current_dir = -2;
            current_file = -2;

            // Read the contents of the file
            string content;
            using (StreamReader sr = new StreamReader(persist_file))
            {
                content = sr.ReadLine();
                current_dir = Int32.Parse(content);
                AddTxt("Dir: " + current_dir.ToString());

                content = sr.ReadLine();
                current_file = Int32.Parse(content);
                AddTxt("File: " + current_file.ToString());
            }
            if ( (current_dir==-2) || (current_file==-2) ) {
                AddTxt("File CurrentBook.txt was corrupted");
                current_dir = -1;
                current_file = -1;
            }

            var dir = new DirectoryInfo("/sdcard/Guerra e Paz");
            top_level = dir.GetFileSystemInfos();
            dir = new DirectoryInfo(top_level[current_dir].FullName);
            files = dir.GetFileSystemInfos();

            Button buttonNextFolder = FindViewById<Button>(Resource.Id.MyButton);
            buttonNextFolder.Click += delegate { NextFolder(); };

            Button buttonNextFile = FindViewById<Button>(Resource.Id.button1);
            buttonNextFile.Click += delegate { NextFile(); };

            Button buttonFim = FindViewById<Button>(Resource.Id.button2);
            buttonFim.Click += delegate { PausePlayer(); };

            // set up the TextToSpeech object
            // third parameter is the speech engine to use
            textToSpeech = new TextToSpeech(this, this);

            // set up the speech to use the default langauge
            // if a language is not available, then the default language is used.
            lang = Java.Util.Locale.Default;
            textToSpeech.SetLanguage(lang);

            // set the speed and pitch
            textToSpeech.SetPitch(.5f);
            textToSpeech.SetSpeechRate(.5f);
        }

        // Interface method required for IOnInitListener
        void TextToSpeech.IOnInitListener.OnInit(OperationResult status)
        {
            // if we get an error, default to the default language
            if (status == OperationResult.Error)
                textToSpeech.SetLanguage(Java.Util.Locale.Default);
            // if the listener is ok, set the lang
            if (status == OperationResult.Success)
                textToSpeech.SetLanguage(lang);


            if ((current_dir != -1) && (current_file != -1))
            {
                string a = string.Format("Continuing the book from folder {0}, chapter {1}", current_dir, current_file + 1);
                textToSpeech.Speak(a, QueueMode.Flush, null);
                Task.Delay(3500).Wait();

                StopPlayer();
                filePath = files[current_file].FullName;
                AddTxt(filePath);

                StartPlayerAsync();
            }
        }

        protected override void OnActivityResult(int req, Result res, Intent data)
        {
            if (req == NeedLang)
            {
                // we need a new language installed
                var installTTS = new Intent();
                installTTS.SetAction(TextToSpeech.Engine.ActionInstallTtsData);
                StartActivity(installTTS);
            }
        }

        protected override void OnStop()
        {
            StopPlayer();
        }

        public void NextFolder()
        {
            StopPlayer();

            current_dir++;
            if (current_dir >= top_level.Length)
            {
                current_dir = 0;
                AddTxt("Livro reiniciado.");
                textToSpeech.Speak("Restarting the book", QueueMode.Flush, null);
            }

            AddTxt(top_level[current_dir].Name);
            textToSpeech.Speak(top_level[current_dir].Name, QueueMode.Add, null);

            var dir = new DirectoryInfo(top_level[current_dir].FullName);
            files = dir.GetFileSystemInfos();
            current_file = -1;
        }

        public void NextFile()
        {
            current_file++;
            if ( (current_dir == -1) || (files==null) || (files.Length==0) || (current_file >= files.Length) )
            {
                NextFolder();
                current_file = 0;
            }

            StopPlayer();
            filePath = files[current_file].FullName;
            AddTxt(filePath);

            StartPlayerAsync();

            // Write the new current file to the asset
            try
            {
                using (StreamWriter sr = new StreamWriter(persist_file))
                {
                    sr.WriteLine(current_dir);
                    sr.WriteLine(current_file);
                    sr.Flush();
                }
            }
            catch (Exception ex)
            {
                AddTxt(ex.ToString().Substring(0, 300));
            }
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

        public void PausePlayer()
        {
            if ((player != null))
            {
                if (player.IsPlaying)
                {
                    player.Pause();
                }
                else
                {
                    player.Start();
                }
            }
        }
    }
}

