using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows.Forms;
using AForge.Imaging.Filters;
using MathNet.Numerics.LinearRegression;

using System.Collections;
using System.ComponentModel;
using System.Configuration;
using System.Xml.Serialization;
using System.Globalization;

namespace LitePlacer
{
    [Serializable]
    [System.Xml.Serialization.XmlRoot("NozzleChanger")]
    public class NozzleChanger : INotifyPropertyChanged
    {
        private DataGridView Grid, GridLocations;
        private NeedleClass Needle;
        private FormMain MainForm;
        private CNC Cnc;

        [System.Xml.Serialization.XmlElement("Speed")]
        private string speed = "2000";
        public string Speed { get { return speed; } set { speed = value; Notify("IS loaded"); } }
        
        public SortableBindingList<Nozzle> nozzles = new SortableBindingList<Nozzle>();
        private const string NozzlesSaveName = "Nozzles.xml";

        private string NozzlesFilename { get { return Global.BaseDirectory + @"\" + NozzlesSaveName; } }

        //public SortableBindingList<Nozzle> getDataSource() { return nozzles; }

        public event PropertyChangedEventHandler PropertyChanged;
        private void Notify(string name)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(name));
            }
        }
       
        //public event EventHandler dataChanged = new EventHandler()

        public void DataChanged(object sender, EventArgs e)
        {
                SaveAll();
        }

        public void InitializeObject(DataGridView gr, DataGridView gridLoc, NeedleClass ndl, CNC c, FormMain MainF) {
            Grid = gr;
            Needle = ndl;
            MainForm = MainF;
            Cnc = c;
            GridLocations = gridLoc;
            //if (File.Exists(Global.BaseDirectory + @"\" + NozzlesSaveName))
            //    nozzles = Global.DeSerialization<SortableBindingList<Nozzle>>(NozzlesFilename);
            ReLoad();

            nozzles.ListChanged += nozzles_ListChanged;
            Grid.CellValueChanged += Grid_CellValueChanged;
            Grid.CurrentCellDirtyStateChanged += Grid_CurrentCellDirtyStateChanged;
        }

        private void CheckLoadingStatus() {
            if (Grid.CurrentCell == null) return;
            Nozzle selected = (Nozzle) Grid.CurrentCell.OwningRow.DataBoundItem;
            if (selected == null) return;

            if (((bool) selected.IsLoaded) == true) { 
                foreach (Nozzle nozzle in nozzles)
                {
                    if (nozzle != selected) { nozzle.IsLoaded = false; }
                }
            }

        }

        private void CheckID()
        {
            if (Grid.CurrentCell == null) return;
            Nozzle selected = (Nozzle)Grid.CurrentCell.OwningRow.DataBoundItem;
            if (selected == null) return;

            foreach (Nozzle nozzle in nozzles)
            {
                if ((nozzle.Id == selected.Id) && (!nozzle.Equals(selected)))
                {
                    MainForm.ShowMessageBox("ID already exists", "Duplicate ID", MessageBoxButtons.OK);
                    selected.Id = "";
                }
            }
        }

        private bool isMessingWithValues = false;
        public void nozzles_ListChanged(object sender, EventArgs e)
        {
            if (isMessingWithValues) return;
            isMessingWithValues = true;

            switch (((ListChangedEventArgs)e).ListChangedType) {
                
                case ListChangedType.ItemAdded:
                    if (((ListChangedEventArgs)e).NewIndex >= nozzles.Count) break;
                    nozzles.ElementAt(((ListChangedEventArgs)e).NewIndex).dataChanged += NozzleChanger_dataChanged;
                    nozzles.ElementAt(((ListChangedEventArgs)e).NewIndex).setDataGrid(GridLocations);
                    break;
                
                case ListChangedType.ItemChanged:
                    break;
            }
            
            isMessingWithValues = false;
        }

        // use for checking ID
        public void Grid_CellValueChanged(object sender, EventArgs e)
        {
            //CheckLoadingStatus();
            if (isMessingWithValues) return;
            if (Grid.CurrentCell.ValueType != typeof(string)) return;
            isMessingWithValues = true;
            CheckID();
            isMessingWithValues = false;            
        }

        public void Grid_CurrentCellDirtyStateChanged(object sender, EventArgs e)
        {
            if (isMessingWithValues) return;
            if (Grid.CurrentCell.ValueType != typeof(bool)) return;
            isMessingWithValues = true;
            Grid.CommitEdit(DataGridViewDataErrorContexts.Commit);
            CheckLoadingStatus();
            isMessingWithValues = false;            
        }

        public void NozzleChanger_dataChanged(object sender, EventArgs e) {
            DataChanged(sender, e);
        }

        public void ReLoad(string filename = null)
        {
            filename = filename ?? NozzlesFilename;
            if (!File.Exists(filename))
            {
                MainForm.ShowSimpleMessageBox("Nozzle file misssing (" + filename + ")");
                return;
            }

            NozzleChanger deserialization = Global.DeSerialization<NozzleChanger>(filename);
            speed = deserialization.speed;
            nozzles = deserialization.nozzles;

            foreach (Nozzle nozzle in nozzles) { nozzle.setDataGrid(GridLocations); }
        }

        public void SaveAll(string filename = null)
        {
            filename = filename ?? NozzlesFilename;
            Global.Serialization(this, filename);
        }

        public void UnloadNozzle(Nozzle nozzle)
        {
            UnloadNozzle_(nozzle);
        }
        
        public void UnloadNozzle()
        {
            
            Nozzle selected = (Nozzle)Grid.CurrentCell.OwningRow.DataBoundItem;
            UnloadNozzle_(selected);            
        }

        private void UnloadNozzle_(Nozzle nozzle)
        {
            if (!nozzle.IsLoaded) return;
            
            Cnc.Zup();
            Cnc.ZGuardOff();
            //Cnc.RawWrite("{\"gc\":\"G1 F " + speed + "\"}");
            string strSpeed = "G1 F" + speed;
            string strSend;
            bool first = true;
            foreach (nozzleLocations NozzleLocation in nozzle.loadSequence.Reverse())
            {
                strSend = "{\"gc\":\"";
                if (first) { strSend = strSend + " G0 "; first = false; } else { strSend = strSend + strSpeed; }
                strSend = strSend + " X" + ((double)NozzleLocation.X).ToString(CultureInfo.InvariantCulture) +
                    " Y" + ((double)NozzleLocation.Y).ToString(CultureInfo.InvariantCulture) +
                    " Z" + ((double)NozzleLocation.Z).ToString(CultureInfo.InvariantCulture) +
                    "\"}";
                Cnc.RawWrite(strSend);
                //    Cnc.CNC_XYZA(NozzleLocation.X, NozzleLocation.Y, 0, NozzleLocation.Z);
            }
            Cnc.ZGuardOn();
            Cnc.Zup();

            nozzle.IsLoaded = false;
        }

        public void LoadNozzle()
        {
            Nozzle selected = (Nozzle)Grid.CurrentCell.OwningRow.DataBoundItem;
            if (selected.IsLoaded) return;
            foreach (Nozzle nozzle in nozzles)
            {
                if (nozzle.IsLoaded) {
                    UnloadNozzle(nozzle);
                    break;
                }
            }
            Cnc.Zup();
            Cnc.ZGuardOff();//("{\"gc\":\""
            string strSpeed = "G1 F" + speed;
            string strSend;
            //Cnc.RawWrite("{\"gc\":\"G1 F " + speed + " X"  ((double)X).ToString(CultureInfo.InvariantCulture);"\"}");
            bool first = true;
            foreach (nozzleLocations NozzleLocation in selected.loadSequence)
            {
                strSend = "{\"gc\":\"";
                if (first) { strSend = strSend + " G0 "; first = false; } else { strSend = strSend + strSpeed; }
                strSend = strSend +" X" + ((double)NozzleLocation.X).ToString(CultureInfo.InvariantCulture) +
                    " Y" + ((double)NozzleLocation.Y).ToString(CultureInfo.InvariantCulture) +
                    " Z" + ((double)NozzleLocation.Z).ToString(CultureInfo.InvariantCulture) +
                    "\"}";
                Cnc.RawWrite(strSend);
                //Cnc.CNC_XYZA(NozzleLocation.X, NozzleLocation.Y,0,NozzleLocation.Z);
            }
            Cnc.ZGuardOn();
            Cnc.Zup();

            selected.IsLoaded = true;
        }

        public void SetPosition(PartLocation loc)
        {
            if (Grid.CurrentCell == null) return;
            Nozzle selected = (Nozzle)Grid.CurrentCell.OwningRow.DataBoundItem;
            selected.setPosition(loc.X, loc.Y, 0);
        }

        public void SetPosition()
        {
            nozzleLocations nz = Cnc.GetCurrentPositionRelativeToZ();

            if (Grid.CurrentCell == null) return;
            Nozzle selected = (Nozzle)Grid.CurrentCell.OwningRow.DataBoundItem;
            selected.setPosition(nz.X, nz.Y, nz.Z);
        }
    }
}
