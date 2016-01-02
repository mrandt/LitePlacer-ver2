using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration;
using System.Xml.Serialization;
using System.Windows.Forms;

namespace LitePlacer
{
    [Serializable]
    public class Nozzle : INotifyPropertyChanged
    {
        public SortableBindingList<nozzleLocations> loadSequence = new SortableBindingList<nozzleLocations>();
        public SortableBindingList<nozzleLocations> unloadSequence = new SortableBindingList<nozzleLocations>();

        private string id;
        public string Id { get { return id; } set { id = value; Notify("Id"); } }

        private bool isloaded;
        public bool IsLoaded { get { return isloaded; } 
            set { isloaded = value; Notify("IS loaded"); } }

        private string nozzleFilter = "";
        public string NozzleFilter { get { return nozzleFilter; } set { nozzleFilter = value; Notify("Nozzle filter"); } }


        private DataGridView Grid;

        public event EventHandler dataChanged/* = new EventHandler(DataChanged)*/;

        public Nozzle()
        {
            loadSequence.ListChanged += loadSequence_ListChanged;
            unloadSequence.ListChanged += unloadSequence_ListChanged;
        }

        public void setDataGrid(DataGridView grid) {
            Grid = grid;
            Grid.CellValueChanged += Grid_CellValueChanged;
        }

        private void Grid_CellValueChanged(object sender, EventArgs e)
        {
            if (dataChanged == null) { return; }
            dataChanged.Invoke(sender, e);
        }

        private void loadSequence_ListChanged(object sender, EventArgs e)
        {
            if (dataChanged == null) { return;  }
            dataChanged.Invoke(sender, e);
        }

        private void unloadSequence_ListChanged(object sender, EventArgs e)
        {
            dataChanged.Invoke(sender, e);
        }

        public event PropertyChangedEventHandler PropertyChanged;
        private void Notify(string name)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(name));
            }
        }

        public void setPosition(double X, double Y, double Z) {
            if (Grid.CurrentCell == null) return;
            nozzleLocations NozzleLocation = (nozzleLocations) Grid.CurrentCell.OwningRow.DataBoundItem;
            NozzleLocation.X = X;
            NozzleLocation.Y = Y;
            NozzleLocation.Z = Z;
        }
    }

    [Serializable]
    public class nozzleLocations : INotifyPropertyChanged
    {
        private double x;
        private double y;
        private double z;

        public double X { get { return x; } set { x = value; Notify("X"); } }
        public double Y { get { return y; } set { y = value; Notify("Y"); } }
        public double Z { get { return z; } set { z = value; Notify("Z"); } }
        
        public event PropertyChangedEventHandler PropertyChanged;
        private void Notify(string name)
    
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(name));
            }
        }
    }

}
