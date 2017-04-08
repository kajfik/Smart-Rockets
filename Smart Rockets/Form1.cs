using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Threading;
using System.Windows;
using System.Drawing.Drawing2D;

namespace Smart_Rockets
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        static Bitmap bmp;
        static Graphics g;
        static Bitmap bmpGraph;
        static Graphics gGraph;
        static Random rnd;
        static Population pop;
        const int lifespanBegin = 400;
        const int graphMargin = 35;
        const int rocketsToShow = 50;
        static int lifespan = lifespanBegin;
        const int numberOfWorkers = 4;
        Thread[] workers = new Thread[numberOfWorkers];
        static int generations = 0;
        static int count = 0;
        static PointF target;
        const int targetRadius = 8;
        static List<RectangleF> obstacles = new List<RectangleF>();
        static List<int> graph = new List<int>();
        static int bmpWidth, bmpHeight;
        Thread worker;

        class RectAngle
        {
            public PointF center;
            PointF[] corners = new PointF[4];
            /*
                0---3
                |   |
                1---2 
            */
            public float Left;
            public float Right;
            public float Top;
            public float Bottom;
            float width;
            float height;
            public float angle;
            float fi;
            float r;
            

            public RectAngle(float x, float y, float width, float height, float angle)
            {
                center.X = x;
                center.Y = y;
                this.width = width;
                this.height = height;
                this.angle = angle;

                fi = (float)Math.Tan(height / width);
                r = (float)Math.Sqrt(width * width + height * height) / 2.0F;
            }

            public void calcCorners()
            {
                corners[0].X = center.X + r * (float)Math.Cos(angle + Math.PI + fi);
                corners[0].Y = center.Y + r * (float)Math.Sin(angle + Math.PI + fi);

                corners[1].X = center.X + r * (float)Math.Cos(angle + Math.PI - fi);
                corners[1].Y = center.Y + r * (float)Math.Sin(angle + Math.PI - fi);

                corners[2].X = center.X + r * (float)Math.Cos(angle + fi);
                corners[2].Y = center.Y + r * (float)Math.Sin(angle + fi);

                corners[3].X = center.X + r * (float)Math.Cos(angle - fi);
                corners[3].Y = center.Y + r * (float)Math.Sin(angle - fi);
            }

            public void calcEdges()
            {
                calcCorners();
                int angleStadium = (int)(Math.Floor(angle / (Math.PI / 2.0F)));
                Left = corners[(1 + angleStadium) % 4].X;
                Right = corners[(3 + angleStadium) % 4].X;
                Top = corners[(0 + angleStadium) % 4].Y;
                Bottom = corners[(2 + angleStadium) % 4].Y;
            }

            public void show(Brush brush)
            {
                calcCorners();
                g.FillPolygon(brush, corners);
                //g.DrawLines(pen, corners);
                //g.DrawLine(pen, corners[3], corners[0]);
            }

            public bool intersectsWith(RectAngle r)
            {
                return (Left <= r.Right && Right >= r.Left && Top <= r.Bottom && Bottom >= r.Top);
            }

            public bool intersectsWith(RectangleF r)
            {
                return (Left <= r.Right && Right >= r.Left && Top <= r.Bottom && Bottom >= r.Top);
            }
        }

        class Population
        {
            const int popSize = 30000;
            List<Rocket> rockets = new List<Rocket>();

            public Population(bool initDNA)
            {
                for (int i = 0; i < popSize; i++)
                {
                    rockets.Add(new Rocket(initDNA));
                }
            }

            List<Rocket> matingPool = new List<Rocket>();

            public void evaluate()
            {
                for (int i = 0; i < popSize; i++)
                {
                    rockets[i].calcFitness();
                }

                rockets.Sort((x, y) => y.fitness.CompareTo(x.fitness));
                
                if(rockets[0].completed)
                {
                    lifespan = Math.Min(lifespan - rockets[0].time + 2, lifespan);
                }

                graph.Add(rockets[0].timeActual);
                double maxFitness = rockets[0].fitness;
                for (int i = 0; i < popSize; i++)
                {
                    rockets[i].fitness /= maxFitness;
                }

                for (int i = 0; i < popSize; i++)
                {
                    int n = (int)(rockets[i].fitness * 100);
                    for (int j = 0; j < n; j++)
                    {
                        matingPool.Add(rockets[i]);
                    }
                }
            }
            
            public void selection()
            {
                Population newPop = new Population(false);
                for (int i = 0; i < popSize; i++)
                {
                    if (i < 50)
                    {
                        newPop.rockets[i].dna = rockets[i].dna;
                    }
                    else
                    {
                        DNA parentA = matingPool[rnd.Next(matingPool.Count)].dna;
                        DNA parentB = matingPool[rnd.Next(matingPool.Count)].dna;
                        DNA child = parentA.crossover(parentB);
                        newPop.rockets[i].dna = new DNA(child.genes);
                    }
                }
                pop = newPop;
            }

            public void run(int thread)
            {
                int iBegin = popSize / numberOfWorkers * (thread + 1) - 1;
                int iEnd = popSize / numberOfWorkers * thread;
                for (int i = iBegin; i >= iEnd; i--)
                {
                    rockets[i].update();
                }
            }

            public void run()
            {
                for (int i = popSize - 1; i >= 0; i--)
                {
                    rockets[i].update();
                }
            }

            public void show()
            {
                for (int i = 50 - 1; i >= 0; i--)
                {
                    if (!rockets[i].crashed)
                    {
                        rockets[i].show(i == 0);
                    }
                }
            }
        }

        class DNA
        {
            public Vector[] genes = new Vector[lifespan];
            static double range = 0.3;
            static double mutationRange = 0.05;

            public DNA()
            {
                for (int i = 0; i < lifespan; i++)
                {
                    genes[i] = new Vector(GetRandomNumber(-range, range), GetRandomNumber(-range, range));
                }
            }

            public DNA(Vector[] genes)
            {
                this.genes = genes;
            }

            public DNA crossover(DNA partner)
            {
                Vector[] newGenes = new Vector[lifespan];
                for (int i = 0; i < lifespan; i++)
                {
                    
                    //mutation
                    if (rnd.Next(lifespan) < 2)
                    {
                        newGenes[i].X += GetRandomNumber(-mutationRange, mutationRange);
                        newGenes[i].Y += GetRandomNumber(-mutationRange, mutationRange);
                    }
                    //crossover
                    else
                    {
                        if (rnd.Next(2) == 1)
                        {
                            newGenes[i] = genes[i];
                        }
                        else
                        {
                            newGenes[i] = partner.genes[i];
                        }
                    }
                }
                return new DNA(newGenes);
            }
        }

        class Rocket
        {
            float width;
            float height;
            Vector vel;
            Vector acc;
            RectAngle body;
            public DNA dna;
            public double fitness;
            public int time = lifespan;
            public int timeActual = 0;
            public bool completed = false;
            public bool crashed = false;

            public Rocket(bool initDNA)
            {
                width = 25;
                height = 5;
                acc = new Vector(0, 0);
                vel = new Vector(0, 0);
                time = lifespan;
                timeActual = 0;
                body = new RectAngle(bmp.Width / 2.0F, bmp.Height - Math.Max(width, height) / 2 - 5, width, height, 0);
                if (initDNA)
                {
                    dna = new DNA();
                }
            }

            public void calcFitness()
            {
                if (completed)
                {
                    fitness = Math.Pow(4.0, time);
                }
                else
                {
                    double d = dist(body.center.X, body.center.Y, target.X + targetRadius, target.Y + targetRadius);
                    d = map(d, 0.0, 1000.0, 8.0, 0.0);
                    d = Math.Pow(2.0, d);
                    fitness = map(d, 0.0, Math.Pow(2.0, 8), 0.0, 0.1);
                    if (crashed) { fitness /= Math.Pow(2.0, 8); }
                }

            }

            public bool crash()
            {
                body.calcEdges();
                if (body.Left < 0 || body.Right > bmpWidth || body.Top < 0 || body.Bottom > bmpHeight)
                {
                    return true;
                }
                for (int i = 0; i < obstacles.Count; i++)
                {
                    if(body.intersectsWith(obstacles[i]))
                    {
                        return true;
                    }
                }
                return false;
            }

            public void update()
            {
                if (!completed && !crashed)
                {
                    if (dist(body.center.X, body.center.Y, target.X + targetRadius, target.Y + targetRadius) <= targetRadius)
                    {
                        completed = true;
                        body.center.X = target.X + 8;
                        body.center.Y = target.Y + 8;
                    }
                    else if (crash())
                    {
                        crashed = true;
                        timeActual = lifespan;
                        time = 0;
                    }
                    else
                    {
                        vel += dna.genes[count];
                        body.center.X += (float)vel.X;
                        body.center.Y += (float)vel.Y;
                        body.angle = (float)(Math.Atan2(vel.Y, vel.X) + Math.PI);
                        time--;
                        timeActual++;
                    }
                }
            }

            public void show(bool bestRocket)
            {
                if (bestRocket)
                {
                    body.show(Brushes.Cyan);
                }
                else
                {
                    body.show(Brushes.White);
                }

                /*RectangleF rect = new RectangleF((float)body.center.X, (float)body.center.Y, width, height);
                using (Matrix m = new Matrix())
                {
                    m.RotateAt((float)(Math.Atan2(vel.Y, vel.X) * 180 / Math.PI), new PointF(rect.Left + (rect.Width / 2),
                                              rect.Top + (rect.Height / 2)));
                    g.Transform = m;
                    if(bestRocket)
                    {
                        g.FillRectangle(Brushes.Cyan, rect);
                    }
                    else
                    {
                        g.FillRectangle(Brushes.White, rect);
                    }
                    g.ResetTransform();
                }*/
            }
        }

        public void main()
        {
            while(true)
            {
                g.Clear(Color.Black);
                g.FillEllipse(Brushes.White, target.X, target.Y, 16, 16);
                for (int i = 0; i < obstacles.Count; i++)
                {
                    g.FillRectangle(Brushes.White, obstacles[i]);
                }
                
                workers[0] = new Thread(() => pop.run(0));
                workers[1] = new Thread(() => pop.run(1));
                workers[2] = new Thread(() => pop.run(2));
                workers[3] = new Thread(() => pop.run(3));
                workers[0].Start();
                workers[1].Start();
                workers[2].Start();
                workers[3].Start();

                while (workers[0].IsAlive || workers[1].IsAlive || workers[2].IsAlive || workers[3].IsAlive)
                {
                    
                }

                pop.show();
                count++;
                Invoke((MethodInvoker)delegate
                {
                    label1.Text = count.ToString() + " / " + lifespan.ToString();
                    label1.Update();
                });
                if (count == lifespan)
                {
                    generations++;
                    pop.evaluate();
                    pop.selection();
                    drawGraph();
                    count = 0;
                }
                Invoke((MethodInvoker)delegate
                {
                    pictureBox1.Image = bmp;
                    pictureBox1.Update();
                });
            }
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            g.Clear(Color.Black);
            g.FillEllipse(Brushes.White, target.X, target.Y, targetRadius * 2, targetRadius * 2);
            for (int i = 0; i < obstacles.Count; i++)
            {
                g.FillRectangle(Brushes.White, obstacles[i]);
            }

            pop.run();

            pop.show();
            count++;
            Invoke((MethodInvoker)delegate
            {
                label1.Text = count.ToString() + " / " + lifespan.ToString();
                label1.Update();
            });
            if (count == lifespan)
            {
                generations++;
                pop.evaluate();
                pop.selection();
                drawGraph();
                count = 0;
            }
            pictureBox1.Image = bmp;
            pictureBox1.Update();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            bmp = new Bitmap(pictureBox1.Width, pictureBox1.Height);
            bmpWidth = bmp.Width;
            bmpHeight = bmp.Height;
            g = Graphics.FromImage(bmp);
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.HighSpeed;
            g.Clear(Color.Black);
            pictureBox1.Image = bmp;
            bmpGraph = new Bitmap(pictureBoxGraph.Width, pictureBoxGraph.Height);
            gGraph = Graphics.FromImage(bmpGraph);
            gGraph.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            gGraph.Clear(Color.White);
            drawGraph();
            pictureBoxGraph.Image = bmpGraph;
            rnd = new Random();
            pop = new Population(true);
            target = new PointF(bmp.Width / 2, 50);
            obstacles.Add(new Rectangle(0, 400, 350, 20));
            obstacles.Add(new Rectangle(250, 200, 350, 20));
            label1.Text = "";
            graph.Add(lifespanBegin);
            worker = new Thread(main);
            worker.Start();
            //timer1.Enabled = true;
            //timer1.Start();
        }

        public void drawGraph()
        {
            gGraph.Clear(Color.White);

            gGraph.DrawRectangle(Pens.Black, 0, 0, pictureBoxGraph.Width - 1, pictureBoxGraph.Height - 1);
            gGraph.DrawString(lifespanBegin.ToString(), DefaultFont, Brushes.Black, graphMargin - 25, graphMargin - 5);
            gGraph.DrawString("Time", DefaultFont, Brushes.Black, graphMargin - 30, pictureBoxGraph.Height / 2);
            gGraph.DrawString("0", DefaultFont, Brushes.Black, graphMargin - 15, pictureBoxGraph.Height - graphMargin + 4);
            gGraph.DrawString("Generations", DefaultFont, Brushes.Black, pictureBoxGraph.Width / 2 - 30, pictureBoxGraph.Height - graphMargin + 4);
            gGraph.DrawString(generations.ToString(), DefaultFont, Brushes.Black, pictureBoxGraph.Width - graphMargin - 10, pictureBoxGraph.Height - graphMargin + 4);
            gGraph.DrawLine(Pens.DarkGray, graphMargin, graphMargin, graphMargin, pictureBoxGraph.Height - graphMargin);
            gGraph.DrawLine(Pens.DarkGray, graphMargin, pictureBoxGraph.Height - graphMargin, pictureBoxGraph.Width - graphMargin, pictureBoxGraph.Height - graphMargin);

            if (graph.Count >= 2)
            {
                int start = 0;
                for (int i = 1; i < graph.Count; i++)
                {
                    if (i == graph.Count - 1 || graph[start] != graph[i + 1])
                    {
                        gGraph.DrawLine(Pens.Black,
                            map(start, 0, graph.Count - 1, graphMargin, pictureBoxGraph.Width - graphMargin),
                            map(graph[start], 0, lifespanBegin, pictureBoxGraph.Height - graphMargin, graphMargin),
                            map(i, 0, graph.Count - 1, graphMargin, pictureBoxGraph.Width - graphMargin),
                            map(graph[i], 0, lifespanBegin, pictureBoxGraph.Height - graphMargin, graphMargin)
                        );
                        start = i;
                    }
                }
            }


            Invoke((MethodInvoker)delegate
            {
                pictureBoxGraph.Image = bmpGraph;
                pictureBoxGraph.Update();
            });
        }

        public static double GetRandomNumber(double minimum, double maximum)
        {
            return rnd.NextDouble() * (maximum - minimum) + minimum;
        }

        public static double dist(double x1, double y1, double x2, double y2)
        {
            return Math.Sqrt(Math.Abs(x1 - x2) * Math.Abs(x1 - x2) + Math.Abs(y1 - y2) * Math.Abs(y1 - y2));
        }

        public static int map(int value, int valueMin, int valueMax, int targetMin, int targetMax)
        {
            return (int)Math.Floor((((value - valueMin) / (double)(valueMax - valueMin))) * (targetMax - targetMin) + targetMin);
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            foreach (Thread t in workers)
            {
                t.Abort();
            }
            worker.Abort();
            Thread.Sleep(50);
        }

        public static double map(double value, double valueMin, double valueMax, double targetMin, double targetMax)
        {
            return ((value - valueMin) / (valueMax - valueMin)) * (targetMax - targetMin) + targetMin;
        }
    }
}
