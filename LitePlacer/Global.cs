﻿using System;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using System.Xml.Serialization;
using LitePlacer.Properties;
using System.Threading;

namespace LitePlacer {
    public class Global {
        //variables to store
        public FormMain mainForm = null;
        public CNC cnc = null;
        public NeedleClass needle = null;

        public static string BaseDirectory { get { return AppDomain.CurrentDomain.BaseDirectory; } }

        /* SINGLETON */
        private static Global _instance;
        private Global() {          
        }

        public static PartLocation NeedleTo(string name) {
            var l = Instance.Locations.GetLocation(name);
            if (l != null) Instance.needle.Move_m(l);
            return l;
        }

        public static PartLocation GoTo(string name) {
            var l = Instance.Locations.GetLocation(name);
            if (l != null)  Instance.cnc.CNC_XY(l);
            return l;
        }

        public static void DoBackgroundWork(int ms = 10) {
            for (int i = 0; i < ms; i++) {
                Thread.Sleep(1);
                Application.DoEvents();
            }
        }


        public static Global Instance {
            get {
                if (_instance == null) _instance = new Global();
                return _instance;
            }
        }

        public void DisplayText(string text, Color color) {
            if (mainForm == null) { return; } // throw new Exception("mainform not set");
            mainForm.DisplayText(text, color);
        }

        public void DisplayText(string text) {
            if (mainForm == null) { return; } // throw new Exception("mainform not set");
            mainForm.DisplayText(text);
        }

        public static void MoveItem<T>(BindingList<T> list, int index, int offset) {
            if (index + offset < 0 || index + offset >= list.Count) return;
            var item = list[index];
            list.RemoveAt(index);
            list.Insert((index + offset), item);
        }

        public static PartLocation GetFrameCenter(Bitmap frame) {
            return new PartLocation(frame.Width / 2, frame.Height / 2);
        }


        public static double ReduceRotation(double rot) {
            // takes a rotation value, rotates it to +- 45degs.
            while (rot > 45.01) rot = rot - 90;
            while (rot < -45.01)  rot = rot + 90;
            return rot;
        }

        //locations
        public LocationManager Locations { get; set; }

        public static void Serialization<T>(T x, string filename) {
            XmlSerializer s = new XmlSerializer(typeof(T));
            TextWriter xml = new StreamWriter(filename);
            s.Serialize(xml, x);
            xml.Close();
        }

        public static T DeSerialization<T>(string filename) {
            XmlSerializer s = new XmlSerializer(typeof(T));
            TextReader xml = new StreamReader(filename);
            T x = (T)s.Deserialize(xml);
            xml.Close();
            return x;
        }     
    }
}
