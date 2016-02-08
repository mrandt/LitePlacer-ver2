﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;

namespace LitePlacer {
    [Serializable]
    public class JobData : INotifyPropertyChanged {

        private List<PhysicalComponent> _components;

        public JobData() {
            _components = new List<PhysicalComponent>();
        }

        public List<PhysicalComponent> Components{
            get { return _components; }
            set { _components = value; }
        } 
        
       
        public int Count { get { return _components.Count; } }
        public string ComponentType {get {return _components.Count > 0 ? _components[0].TypePlusFootprint : "";}}

        private string _method = "";
        public string Method {
            get {
                if (IsFidducial) return "Fiducial";
                if (_method.Equals("")) return "?";
                return _method;
            }
            set { _method = value; Notify("Method"); } 
        }

        private string _methodParameters;
        public string MethodParameters {
            get { return _methodParameters; }
            set { _methodParameters = value; Notify("MethodParameters"); }
        }


        public string ComponentList {
            get { return string.Join(",", _components.Select(x => x.Designator).ToArray()); }
            // XXX - need to add setter to be able to dynamically adjust the components list
        }

        public bool IsFidducial {
            get {
                return _components.Count > 0 && _components[0].IsFiducial;
            }
        }

        private string nozzleId;
        public string NozzleId
        {
            get { return nozzleId; }
            set { nozzleId = value; Notify("Nozzle ID"); }
        }

        private void UpdateComponents() {
            foreach (var x in _components) {
                x.JobData = this;
            }
            Notify("ComponentList");
            Notify("Count");
        }

        public void RemoveComponent(string designator) {
            _components.RemoveAll(x => x.Designator.Equals(designator));
            UpdateComponents();
        }

        public void AddComponent(PhysicalComponent x) { _components.Add(x); UpdateComponents();}
        public void AddComponent(PhysicalComponent[] x) { _components.AddRange(x); UpdateComponents(); }
        public PhysicalComponent[] GetComponents() {return _components.ToArray(); }

        public override string ToString() {
            var r = string.Join("\",\"", new List<string> {Count.ToString(), ComponentType, Method, _methodParameters, ComponentList});
            return "\""+r+"\"";            
        }

        public event PropertyChangedEventHandler PropertyChanged;
        private void Notify(string name) {
            if (PropertyChanged != null) {
                PropertyChanged(this, new PropertyChangedEventArgs(name));
            }
        }
    }
}
