using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.IO;
using System.Timers;
using System.Diagnostics;
using System.Threading;
namespace IntelligentScissors
{
    public partial class MainForm : Form
    {
        RGBPixel[,] ImageMatrix;
        SortedDictionary<double, Queue<KeyValuePair<int, int>>> shortest = new SortedDictionary<double, Queue<KeyValuePair<int, int>>>();
        List<int> anchor_points = new List<int>();
        List<int> complete_path = new List<int>();
        List<int> path = new List<int>();
        List<int> curr_pre = new List<int>();
        bool stop = false;
        int n, m;
        public MainForm()
        {
            InitializeComponent();
        }
        private void btnOpen_Click(object sender, EventArgs e)
        {
            reset();
            OpenFileDialog openFileDialog1 = new OpenFileDialog();
            if (openFileDialog1.ShowDialog() == DialogResult.OK)
            {
                string OpenedFilePath = openFileDialog1.FileName;
                ImageMatrix = ImageOperations.OpenImage(OpenedFilePath);
                n = ImageOperations.GetHeight(ImageMatrix);
                m = ImageOperations.GetWidth(ImageMatrix);
                ImageOperations.DisplayImage(ImageMatrix, pictureBox1);
                txtWidth.Text = ImageOperations.GetWidth(ImageMatrix).ToString();
                txtHeight.Text = ImageOperations.GetHeight(ImageMatrix).ToString();
            }
        }
        public List<KeyValuePair<int, double>> get_children(int node)
        {
            double weight;
            List<KeyValuePair<int, double>> l = new List<KeyValuePair<int, double>>();
            int i = node / m;
            int j = node % m;
            if (i != n - 1)
            {
                var G = ImageOperations.CalculatePixelEnergies(j, i, ImageMatrix);
                int node1 = (i + 1) * m + j;
                if (G.Y == 0)
                    weight = 10000000000000000;
                else
                    weight = 1 / (G.Y);
                KeyValuePair<int, double> kvp = new KeyValuePair<int, double>(node1, weight);
                l.Add(kvp);
            }
            if (j != m - 1)
            {
                var G = ImageOperations.CalculatePixelEnergies(j, i, ImageMatrix);
                int node1 = i * m + (j + 1);
                if (G.X == 0)
                    weight = 10000000000000000;
                else
                    weight = 1 / (G.X);
                KeyValuePair<int, double> kvp = new KeyValuePair<int, double>(node1, weight);
                l.Add(kvp);
            }
            if (i != 0)
            {

                var G = ImageOperations.CalculatePixelEnergies(j, i - 1, ImageMatrix);
                int node1 = (i - 1) * m + j;
                if (G.Y == 0)
                    weight = 10000000000000000;
                else
                    weight = 1 / (G.Y);
                KeyValuePair<int, double> kvp = new KeyValuePair<int, double>(node1, weight);
                l.Add(kvp);
            }
            if (j != 0)
            {
                var G = ImageOperations.CalculatePixelEnergies(j - 1, i, ImageMatrix);
                int node1 = i * m + (j - 1);
                if (G.X == 0)
                    weight = 10000000000000000;
                else
                    weight = 1 / (G.X);
                KeyValuePair<int, double> kvp = new KeyValuePair<int, double>(node1, weight);
                l.Add(kvp);
            }
            return l;
        }
        public List<int> backtracking(List<int> pre, int dest)
        {
            List<int> path = new List<int>();
            Stack<int> st = new Stack<int>();
            st.Push(dest);
            int pre_node = pre[dest];
            while (pre_node != -1)
            {
                st.Push(pre_node);
                pre_node = pre[pre_node];
            }
            while (st.Count != 0)
            {
                path.Add(st.Peek());
                st.Pop();
            }
            return path;
        }
        public List<int> dijkstra(int source, int dest)
        {
            shortest.Clear();
            const double inf = double.MaxValue;
            int nodes_number = m * n;
            List<double> dis = new List<double>();
            List<int> pre = new List<int>();
            for (int i = 0; i < nodes_number; i++)
                dis.Add(inf);
            for (int i = 0; i < nodes_number; i++)
                pre.Add(-1);
            Queue<KeyValuePair<int, int>> inn = new Queue<KeyValuePair<int, int>>();
            KeyValuePair<int, int> kvp = new KeyValuePair<int, int>(-1, source);
            inn.Enqueue(kvp);
            shortest[0] = inn;
            while (shortest.Count != 0)
            {
                int from = 0;
                int to = 0;
                double W = 0;
                foreach (var x in shortest)
                {
                    kvp = x.Value.Dequeue();
                    W = x.Key;
                    from = kvp.Key;
                    to = kvp.Value;
                    if (x.Value.Count == 0)
                        shortest.Remove(W);
                    break;
                }
                if (W < dis[to])
                {
                    dis[to] = W;
                    pre[to] = from;
                    if (dest == to)
                        break;
                    List<KeyValuePair<int, double>> l = get_children(to);
                    for (int i = 0; i < l.Count; i++)
                    {
                        int from1 = to;
                        int to1 = l[i].Key;
                        double W1 = l[i].Value;
                        if (dis[to1] > dis[from1] + W1 && in_box(source,to1))
                        {
                            double new_W = dis[from1] + W1;
                            if (shortest.ContainsKey(new_W))
                            {
                                KeyValuePair<int, int> kvp1 = new KeyValuePair<int, int>(from1, to1);
                                shortest[new_W].Enqueue(kvp1);
                            }
                            else
                            {
                                Queue<KeyValuePair<int, int>> inn1 = new Queue<KeyValuePair<int, int>>();
                                KeyValuePair<int, int> kvp1 = new KeyValuePair<int, int>(from1, to1);
                                inn1.Enqueue(kvp1);
                                shortest[new_W] = inn1;
                            }
                        }
                    }
                }
            }
            return pre;
        }
        private void pictureBox1_MouseClick(object sender, MouseEventArgs e)
        {
            if (pictureBox1.Image != null && stop==false)
            {
                int node = e.X + e.Y * m;
                if (anchor_points.Count != 0 && in_box(anchor_points[anchor_points.Count - 1],node))
                {
                    for (int i = 1; i < path.Count; i++)
                    {
                        complete_path.Add(path[i - 1]);

                        Graphics g1 = pictureBox1.CreateGraphics();
                        Pen p = new Pen(Color.Cyan, 2);
                        float x1 = path[i - 1] % m;
                        float y1 = path[i - 1] / m;
                        float x2 = path[i] % m;
                        float y2 = path[i] / m;
                        g1.DrawLine(p, x1, y1, x2, y2);
                    }
                    complete_path.Add(path[path.Count - 1]);

                    anchor_points.Add(node);
                    Graphics g = pictureBox1.CreateGraphics();
                    Point anchorsize = new Point(5, 5);
                    g.FillEllipse(Brushes.Green, new Rectangle(new Point(e.X - anchorsize.X / 2, e.Y - anchorsize.Y / 2), new Size(anchorsize)));
                }
                else if(anchor_points.Count==0)
                {
                    anchor_points.Add(node);
                    Graphics g = pictureBox1.CreateGraphics();
                    Point anchorsize = new Point(5, 5);
                    g.FillEllipse(Brushes.Green, new Rectangle(new Point(e.X - anchorsize.X / 2, e.Y - anchorsize.Y / 2), new Size(anchorsize)));
                }
            }
        }
        private void button2_Click(object sender, EventArgs e)
        {
            if (pictureBox1.Image != null)
            {
                if (stop == false && anchor_points.Count >= 2 && in_box(anchor_points[anchor_points.Count - 1], anchor_points[0]))
                {
                    stop = true;
                    int node = anchor_points[0];
                    int anc = anchor_points[anchor_points.Count - 1];
                    curr_pre = dijkstra(anc, node);
                    path = backtracking(curr_pre, node);
                    for (int i = 1; i < path.Count; i++)
                    {
                        complete_path.Add(path[i - 1]);

                        Graphics g1 = pictureBox1.CreateGraphics();
                        Pen p = new Pen(Color.Red, 2);
                        float x1 = path[i - 1] % m;
                        float y1 = path[i - 1] / m;
                        float x2 = path[i] % m;
                        float y2 = path[i] / m;
                        g1.DrawLine(p, x1, y1, x2, y2);
                    }
                    complete_path.Add(path[path.Count - 1]);
                    pictureBox1.Refresh();
                    draw();
                }
            }
        }
        private void pictureBox1_DoubleClick(object sender, EventArgs e)
        {
            if (pictureBox1.Image != null)
            {
                if (stop==false && anchor_points.Count >= 2 && in_box(anchor_points[anchor_points.Count - 1],anchor_points[0]))
                {
                    stop = true;
                    int node = anchor_points[0];
                    int anc = anchor_points[anchor_points.Count - 1];
                    curr_pre = dijkstra(anc, node);
                    path = backtracking(curr_pre, node);
                    for (int i = 1; i < path.Count; i++)
                    {
                        complete_path.Add(path[i - 1]);

                        Graphics g1 = pictureBox1.CreateGraphics();
                        Pen p = new Pen(Color.Red, 2);
                        float x1 = path[i - 1] % m;
                        float y1 = path[i - 1] / m;
                        float x2 = path[i] % m;
                        float y2 = path[i] / m;
                        g1.DrawLine(p, x1, y1, x2, y2);
                    }
                    complete_path.Add(path[path.Count - 1]);
                    pictureBox1.Refresh();
                    draw();
                }
            }
        }
        private void pictureBox1_MouseMove(object sender, MouseEventArgs e)
        {
            if (pictureBox1.Image != null)
            {
                int node = e.Y * m + e.X;
                if (stop == false && anchor_points.Count!=0 && in_box(anchor_points[anchor_points.Count - 1], node))
                {
                    pictureBox1.Refresh();
                    draw();
                    if (anchor_points.Count > 0)
                    {
                        int anc = anchor_points[anchor_points.Count - 1];
                        curr_pre = dijkstra(anc, node);
                        path = backtracking(curr_pre, node);
                        for (int i = 1; i < path.Count; i++)
                        {
                            Graphics g1 = pictureBox1.CreateGraphics();
                            Pen p = new Pen(Color.Red, 2);
                            float x1 = path[i - 1] % m;
                            float y1 = path[i - 1] / m;
                            float x2 = path[i] % m;
                            float y2 = path[i] / m;
                            g1.DrawLine(p, x1, y1, x2, y2);
                        }
                    }
                }
                textBox1.Text = e.X.ToString();
                textBox2.Text = e.Y.ToString();
            }
        }
        public void draw()
        {
            for (int i = 1; i < complete_path.Count; i++)
            {
                Graphics g1 = pictureBox1.CreateGraphics();
                Pen p = new Pen(Color.Cyan, 2);
                float x1 = complete_path[i - 1] % m;
                float y1 = complete_path[i - 1] / m;
                float x2 = complete_path[i] % m;
                float y2 = complete_path[i] / m;
                g1.DrawLine(p, x1, y1, x2, y2);
            }
            for (int i = 0; i < anchor_points.Count; i++)
            {
                Point anchorsize = new Point(5, 5);
                Graphics g = pictureBox1.CreateGraphics();
                g.FillEllipse(Brushes.Green, new Rectangle(new Point(anchor_points[i] % m - anchorsize.X / 2, anchor_points[i] / m - anchorsize.Y / 2), new Size(anchorsize)));
            }
        }
        private void button1_Click(object sender, EventArgs e)
        {
            reset();
        }
        void reset()
        {
            complete_path.Clear();
            anchor_points.Clear();
            pictureBox1.Refresh();
            stop = false;
        }
        bool in_box(int n1,int n2)
        {
            int x1 = n1 % m;
            int y1 = n1 / m;
            int x2 = n2 % m;
            int y2 = n2 / m;
            return Math.Abs(x1 - x2) <150 && Math.Abs(y1 - y2) < 150;
        }
        private void panel1_Scroll(object sender, ScrollEventArgs e)
        {
            draw();
        }
        private void pictureBox1_MouseHover(object sender, EventArgs e)
        {
            draw();
        }
        private void MainForm_Load(object sender, EventArgs e)
        {

        }
        private void pictureBox1_Click(object sender, EventArgs e)
        {

        }
        private void label7_Click(object sender, EventArgs e)
        {

        }
        private void textBox1_TextChanged(object sender, EventArgs e)
        {

        }
        private void txtHeight_TextChanged(object sender, EventArgs e)
        {

        }

        private void panel1_Paint(object sender, PaintEventArgs e)
        {
            
        }

        private void panel1_VisibleChanged(object sender, EventArgs e)
        {
        }

        private void pictureBox1_LoadProgressChanged(object sender, ProgressChangedEventArgs e)
        {
        }

        private void pictureBox1_Paint(object sender, PaintEventArgs e)
        {
        }

        private void pictureBox1_MouseUp(object sender, MouseEventArgs e)
        {

        }

        private void pictureBox1_LoadCompleted(object sender, AsyncCompletedEventArgs e)
        {
        }

        private void pictureBox1_BindingContextChanged(object sender, EventArgs e)
        {
        }

        private void pictureBox1_MouseDown(object sender, MouseEventArgs e)
        {
        }
        private void label6_Click(object sender, EventArgs e)
        {

        }
    }
}
