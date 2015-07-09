﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using System.IO;
using TShockAPI;

namespace StaffChatPlugin
{
    public class Config
    {
        public static string SavePath = Path.Combine(TShock.SavePath, "StaffChat.json");
        public static Clr DefaultChatColor = new Clr(200, 50, 150);
        public Clr ChatColor;
        public string StaffChatPrefix = "[StaffChat]";
        public string StaffChatGuestTag = "<Guest>";

        public static Config Load()
        {
            using (StreamReader sr = new StreamReader(File.Open(SavePath, FileMode.Open)))
            {
                return JsonConvert.DeserializeObject<Config>(sr.ReadToEnd());
            }
        }

        public void Save()
        {
            try
            {
                using (StreamWriter sw = new StreamWriter(File.Open(SavePath, FileMode.Create)))
                {
                    sw.Write(JsonConvert.SerializeObject(this));
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("[StaffChatPlugin] - Error loading config file!");
                Console.WriteLine(ex.ToString());
            }
        }
    }

    public class Clr
    {
        int r;
        int g;
        int b;
        public Clr(int R, int G, int B)
        {
            r=R;
            g=G;
            b=B;
        }
        public Color ToColor()
        {
            return new Color(r,g,b);
        }
    }
}
