using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;
using System.Configuration;
using System.Xml.Serialization;
using System.Windows.Forms;
using System.Drawing;
using System.Threading;

using System.Globalization;
using System.IO;

namespace LitePlacer
{

    [Serializable]
    public class Vertice : INotifyPropertyChanged
    {
        private double x;
        private double y;

        public double X { get { return x; } set { x = value; Notify("X"); } }
        public double Y { get { return y; } set { y = value; Notify("Y"); } }

        private string id;
        public string Id { get { return id; } set { id = value; } }

        public Vertice() { }

        public Vertice(double x, double y)
        {
            X = x;
            Y = y;
        }

        public event PropertyChangedEventHandler PropertyChanged;
        private void Notify(string name)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(name));
            }
        }
    }
    
    [Serializable]
    public class Guard : INotifyPropertyChanged
    {
        private Vertice verticeS;// = new Vertice();
        private Vertice verticeE;// = new Vertice();

        public Vertice VerticeS
        {
            get { return verticeS; }
            set {
                if (verticeS == null) { verticeS = new Vertice(); }
                verticeS = value; 
            }
        }

        public Vertice VerticeE
        {
            get { return verticeE; }
            set {
                if (verticeE == null) { verticeE = new Vertice(); }
                verticeE = value; 
            }
        }

        private bool validated;
        public bool Validated
        {
            get { return validated; }
            set { validated = value; }
        }

        private string id;
        public string Id { 
            get { return id; } 
            set { id = value;
                if (VerticeS.Id == null) { VerticeS.Id = id + "-v1"; }
                if (VerticeE.Id == null) { VerticeE.Id = id + "-v2"; }
            } 
        }

        visibilitygraph parent;

        [XmlIgnore]
        public visibilitygraph Parent
        {
            get { return parent; }
            set { parent = value; }
        }

        public void prepareToDestroy() {
            bool inUseS = false, inUseE = false;
            foreach (Guard guard in parent.Guards)
            {
                if (guard.id != Id)
                {
                    if ((guard.VerticeS == VerticeS) || (guard.VerticeE == VerticeS)) { inUseS = true; }
                    if ((guard.VerticeS == VerticeE) || (guard.VerticeE == VerticeE)) { inUseE = true; }
                }
            }
            if (!inUseS) { parent.Vertices.Remove(verticeS); }
            if (!inUseE) { parent.Vertices.Remove(verticeE); }
        }

        public Guard(visibilitygraph Parent)
        {
            parent = Parent;
            validated = false;
        }

        public Guard() { }

        ~Guard()
        {
//            prepareToDestroy();
            //VerticeE = null;
            //VerticeS = null;
        }

        public bool belongsTo(Vertice vertice)
        {
            if ((vertice == VerticeS) || (vertice == VerticeE)) { return true; }
            return false;
        }

        public bool setRange(double x1, double y1, double x2, double y2)
        {
            if ((x1 > parent.xMax) || (x1 < parent.xMin) ||
                (x2 > parent.xMax) || (x2 < parent.xMin) ||
                (y1 > parent.yMax) || (y1 < parent.yMin) ||
                (y2 > parent.yMax) || (y2 < parent.yMin)) { return false; }

            if (VerticeS == null) { verticeS = new Vertice(); }
            if (VerticeE == null) { verticeE = new Vertice(); }

            VerticeS.X = x1;
            VerticeS.Y = y1;
            VerticeE.X = x2;
            VerticeE.Y = y2;
            
            VerticeS = parent.addVertice(VerticeS);
            VerticeE = parent.addVertice(VerticeE);

            validated = true;

            Notify("Set range");
            return true;
        }

        public event PropertyChangedEventHandler PropertyChanged;
        private void Notify(string name)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(name));
            }
        }
    }

    [Serializable]
    [System.Xml.Serialization.XmlRoot("visibilityGraph")]
    public class visibilitygraph : INotifyPropertyChanged
    {
        private SortableBindingList<Vertice> vertices;// = new SortableBindingList<Vertice>();
        private SortableBindingList<Guard> guards;// = new SortableBindingList<Guard>();

        public SortableBindingList<Vertice> Vertices
        {
            get { return vertices; }
            set {
                if (vertices == null) { vertices = new SortableBindingList<Vertice>(); }
                vertices = value; 
            }
        } 

        public SortableBindingList<Guard> Guards
        {
            get { return guards; }
            set {
                if (guards == null) { guards = new SortableBindingList<Guard>(); }
                guards = value; 
            }
        }

        private bool enabled;

        public bool Enabled { set { enabled = value; } get { return enabled; } }

        private double? xmin = null, xmax = null, ymin = null, ymax = null;
        
        private PictureBox DrawingSurface;
        private Graphics graphicsObject;
        private double? graphicsScale;

        public PictureBox drawingSurface { set { DrawingSurface = value; graphicsObject = value.CreateGraphics(); } }

        public double? xMin { get { return xmin; } set { xmin = value; Notify("Min/Max change"); } }
        public double? xMax { get { return xmax; } set { xmax = value; Notify("Min/Max change"); } }
        public double? yMin { get { return ymin; } set { ymin = value; Notify("Min/Max change"); } }
        public double? yMax { get { return ymax; } set { ymax = value; Notify("Min/Max change"); } }

        private const string GuardsSaveName = "Guards.xml";

        private string GuardsFilename { get { return Global.BaseDirectory + @"\" + GuardsSaveName; } }

        public void combineRedundantVertices() {
            foreach (Vertice vertice_ in Vertices)
            {
                foreach (Vertice vertice__ in Vertices)
                {
                    if ((vertice_ != vertice__) && (vertice_.X == vertice__.X) && (vertice_.Y == vertice__.Y)) { 
                        Vertice vertice = vertice__;
                        vertice = vertice_; 
                    }
                }

            }
        }

        public Vertice addVertice(Vertice vertice)
        {
            if (Vertices == null) { Vertices = new SortableBindingList<Vertice>(); }

            foreach (Vertice vertice_ in Vertices)
            {
                if ((vertice_.X == vertice.X) && (vertice_.Y == vertice.Y)) { return vertice_; }
            }
            Vertices.Add(vertice);
            return vertice;
        }

        public bool setBounds(double? XMin, double? YMin, double? XMax, double? YMax)
        {
            xMin = XMin;
            xMax = XMax;
            yMin = YMin;
            yMax = YMax;

            return true;
        }

        public bool insertGuard(double x1, double y1, double x2, double y2) {
            if (Guards == null) { Guards = new SortableBindingList<Guard>(); }

            Guard guard = new Guard(this);
            if (guard.setRange(x1, y1, x2, y2)) {
                guard.Id = "g-" + Guards.Count.ToString();
                Guards.Add(guard);
                return true;
            }
            return false;
        }

        public bool insertGuard()
        {
            Guard guard = new Guard(this);
            guard.Id = "g-" + Guards.Count.ToString();
            Guards.Add(guard);
            return true;
        }

        private bool calculateGraphicsScale()
        {
            if (DrawingSurface == null) { return false; }

            if ((xmin == null) || (xmax == null) || (ymin == null) || (ymax == null)) { return false; }
            double? largest;
            if ((xmax - xmin) > (ymax - ymin)) { largest = xmax - xmin; } else { largest = ymax - ymin; }
            if (DrawingSurface.Width > DrawingSurface.Height) { graphicsScale = (DrawingSurface.Width - 10) / largest; } else { graphicsScale = (DrawingSurface.Height - 10) / largest; }
            return true;
        }

        public bool redraw()
        {
            // Bounds
            Pen penTb = new Pen(Color.Blue);
            penTb.Width = 2;

            graphicsObject.Clear(Color.White);
            // doesn't account for the 5px starting point correctly
            graphicsObject.DrawRectangle(penTb, new Rectangle(5, 5, (int) (xMax * graphicsScale), (int) (yMax * graphicsScale)+5));

            Pen penTyg = new Pen(Color.YellowGreen);
            penTyg.Width = 2;
            graphicsObject.DrawLine(penTyg, 5, (int)(yMax * graphicsScale) + 15, 5, (int) (yMax * graphicsScale) + 5);
            graphicsObject.DrawLine(penTyg, 0, (int)(yMax * graphicsScale) + 10, 10, (int)(yMax * graphicsScale) + 10);
            
            Pen PenTr = new Pen(Color.Red);

            if (Guards == null) { return true;  }
            foreach (Guard guard in Guards) {
                if (guard.Validated)
                {
                    graphicsObject.DrawLine(PenTr, 5 + (int)(guard.VerticeS.X * graphicsScale), ((int)((yMax - guard.VerticeS.Y) * graphicsScale) + 10),
                       5 + (int)(guard.VerticeE.X * graphicsScale), ((int)((yMax - guard.VerticeE.Y) * graphicsScale) + 10));
                }
            }

            return true;
        }


        // from http://www.geeksforgeeks.org/check-if-two-given-line-segments-intersect/

        // Given three colinear points p, q, r, the function checks if
        // point q lies on line segment 'pr'
        private bool onSegment(Vertice p, Vertice q, Vertice r)
        {
            if (p.X <= Math.Max(p.X, r.X) && q.X >= Math.Max(p.X, r.X) &&
                q.Y <= Math.Min(p.Y, r.Y) && q.Y >= Math.Min(p.Y, r.Y))
                return true;
            
            return false;
        }

        // To find orientation of ordered triplet (p, q, r).
        // The function returns following values
        // 0 --> p, q and r are colinear
        // 1 --> Clockwise
        // 2 --> Counterclockwise
        int orientation(Vertice p, Vertice q, Vertice r)
        {
            // See http://www.geeksforgeeks.org/orientation-3-ordered-points/
            // for details of below formula.
            double val = (q.Y - p.Y) * (r.X - q.X) -
                      (q.X - p.X) * (r.Y - q.Y);

            if (val == 0)
            {
                return 0;  // colinear
            }

            return (val > 0) ? 1 : 2; // clock or counterclock wise
        }

        // The main function that returns true if line segment 'p1q1'
        // and 'p2q2' intersect.
        bool doIntersect(Vertice p1, Vertice q1, Vertice p2, Vertice q2)
        {
            // Find the four orientations needed for general and
            // special cases
            int o1 = orientation(p1, q1, p2);
            int o2 = orientation(p1, q1, q2);
            int o3 = orientation(p2, q2, p1);
            int o4 = orientation(p2, q2, q1);

            // General case
            if (o1 != o2 && o3 != o4)
                return true;

            // Special Cases
            // p1, q1 and p2 are colinear and p2 lies on segment p1q1
            if (o1 == 0 && onSegment(p1, p2, q1)) return true;

            // p1, q1 and p2 are colinear and q2 lies on segment p1q1
            if (o2 == 0 && onSegment(p1, q2, q1)) return true;

            // p2, q2 and p1 are colinear and p1 lies on segment p2q2
            if (o3 == 0 && onSegment(p2, p1, q2)) return true;

            // p2, q2 and q1 are colinear and q1 lies on segment p2q2
            if (o4 == 0 && onSegment(p2, q1, q2)) return true;

            return false; // Doesn't fall in any of the above cases
        }

        private bool isDebug = false;

        private bool checkAgainstAll(Vertice Looking, Vertice Goal)
        {
            bool retVal = true;

            Pen penTg = new Pen(Color.Green);
            penTg.Width = 2;
            Pen penTr = new Pen(Color.Red);
            penTr.Width = 2;

            foreach (Guard guard in Guards)
            {
                if (guard.Validated) { 
                    if (!guard.belongsTo(Looking) && !guard.belongsTo(Goal) && doIntersect(Looking, Goal, guard.VerticeS, guard.VerticeE))
                    {
                        if (!isDebug) { return false; }
                        graphicsObject.DrawLine(penTr, 5 + (int)(guard.VerticeS.X * graphicsScale), ((int)((yMax - guard.VerticeS.Y) * graphicsScale) + 10),
                            5 + (int)(guard.VerticeE.X * graphicsScale), ((int)((yMax - guard.VerticeE.Y) * graphicsScale) + 10));
                        retVal = false;
                    }
                    else
                    {
                   
                        graphicsObject.DrawLine(penTg, 5 + (int)(guard.VerticeS.X * graphicsScale), ((int)((yMax - guard.VerticeS.Y) * graphicsScale) + 10),
                            5 + (int)(guard.VerticeE.X * graphicsScale), ((int)((yMax - guard.VerticeE.Y) * graphicsScale) + 10));
                    }
                }

                if (isDebug) { 
                    Application.DoEvents();
                    Thread.Sleep(50);
                }
            }

            return retVal;
        }

        private bool findNext(Vertice Start, Vertice End, List<Vertice> path)
        {
            Vertice found = null;
            double closestDistance = 0, distance;

            List<Vertice> nonIntersects = new List<Vertice>();

            for (int i = 0; i < Vertices.Count; i++)
            {
                if (checkAgainstAll(Start, Vertices[i]) && !path.Contains(Vertices[i]))
                {
                    nonIntersects.Add(Vertices[i]);
                }

            }

            for (int j = 0; j < nonIntersects.Count; j++)
            {
                distance = Math.Sqrt((Math.Pow(Math.Abs(End.X - nonIntersects[j].X), 2) + Math.Pow(Math.Abs(End.Y - nonIntersects[j].Y), 2)));

                if ((j == 0) || (distance < closestDistance))
                {
                    found = nonIntersects[j];
                    closestDistance = distance;
                }
            }

            if (nonIntersects.Count == 0) { 
                return false;
            }
            else
            {
                path.Add(found);
                return true;
            }
        }

        public bool checkVisibles(Vertice Start, Vertice End, out List<Vertice> path) {
            if (Vertices == null || Guards == null) { path = null;  return false; }
            Vertices.Add(End);
            path = new List<Vertice>();
            path.Add(Start);
            
            int i = 0;
            do
            {
                findNext(path[i], End, path);
                i++;
            } while ((i < path.Count) && (path[i] != End));

            Pen penPr = new Pen(Color.Purple);
            penPr.Width = 1;
            for (i = 0; i < path.Count - 1; i++)
            {
                graphicsObject.DrawLine(penPr, 5 + (int)(path[i].X * graphicsScale), ((int)((yMax - path[i].Y) * graphicsScale) + 10),
                    5 + (int)(path[i + 1].X * graphicsScale), ((int)((yMax - path[i + 1].Y) * graphicsScale) + 10));
            }

            Vertices.Remove(End);
//            Vertices.Remove(Start);
            
            return true;
        }

        public visibilitygraph ReLoad(string filename = null)
        {
            filename = filename ?? GuardsFilename;
            if (!File.Exists(filename))
            {
                //MainForm.ShowSimpleMessageBox("Nozzle file misssing (" + filename + ")");
                return null;
            }

            visibilitygraph deserialization = Global.DeSerialization<visibilitygraph>(filename);
            foreach (Guard guard in deserialization.Guards)
            {
                guard.Parent = this;
                guard.VerticeS = deserialization.Vertices.FirstOrDefault(item => guard.VerticeS.Id == item.Id);
                guard.VerticeE = deserialization.Vertices.FirstOrDefault(item => guard.VerticeE.Id == item.Id);
            }

            return deserialization;
        }

        public void SaveAll(string filename = null)
        {
            filename = filename ?? GuardsFilename;
            //try { 
                Global.Serialization(this, filename);
            //}
            //catch { }

        }

        ~visibilitygraph()
        {
            //SaveAll();
        }


        public event PropertyChangedEventHandler PropertyChanged;
        private void Notify(string name)
        {
            switch (name)
            {
                case "Min/Max change":
                    if (calculateGraphicsScale()) { redraw();  }
                    break;
            }

            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(name));
            }
        }
    }
}
