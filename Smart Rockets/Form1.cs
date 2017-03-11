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
        static Random rnd;
        static Population pop;
        static int lifespan = 400;
        static int count = 0;
        static Vector target;
        static List<Rectangle> obstacles = new List<Rectangle>();
        static string maxF;
        
        class Population
        {
            const int popSize = 10000;
            Rocket[] rockets = new Rocket[popSize];

            public Population(bool initDNA)
            {
                for (int i = 0; i < popSize; i++)
                {
                    rockets[i] = new Rocket(initDNA);
                }
            }

            List<Rocket> matingPool = new List<Rocket>();

            public void evaluate()
            {
                double maxFitness = 0;
                int maxFitnessIndex = 0;
                for (int i = 0; i < popSize; i++)
                {
                    rockets[i].calcFitness();
                    if(rockets[i].fitness > maxFitness)
                    {
                        maxFitness = rockets[i].fitness;
                        maxFitnessIndex = i;
                    }
                }
                if(rockets[maxFitnessIndex].completed)
                {
                    lifespan = Math.Min(lifespan - rockets[maxFitnessIndex].time + 2, lifespan);
                }
                for (int i = 0; i < popSize; i++)
                {
                    rockets[i].fitness /= maxFitness;
                }

                maxF = (maxFitness).ToString();

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
                    DNA parentA = matingPool[rnd.Next(matingPool.Count)].dna;
                    DNA parentB = matingPool[rnd.Next(matingPool.Count)].dna;
                    DNA child = parentA.crossover(parentB);
                    newPop.rockets[i].dna = new DNA(child.genes);
                }
                pop = newPop;
            }

            public void run()
            {
                for (int i = 0; i < popSize; i++)
                {
                    rockets[i].update();
                    if (i < 50 && !rockets[i].crashed)
                    {
                        rockets[i].show();
                    }
                }
            }
        }

        class DNA
        {
            public Vector[] genes = new Vector[lifespan];
            double range = 0.3;
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
                    //crossover
                    /*newGenes[i] = this.genes[i] + partner.genes[i];
                    newGenes[i] /= 2;*/
                    if (rnd.Next(2) == 1)
                    {
                        newGenes[i] = this.genes[i];
                    }
                    else
                    {
                        newGenes[i] = partner.genes[i];
                    }
                    //mutation
                    if (rnd.Next(lifespan) < 2)
                    {
                        newGenes[i].X += GetRandomNumber(-0.05, 0.05);
                        newGenes[i].Y += GetRandomNumber(-0.05, 0.05);
                    }
                }
                return new DNA(newGenes);
            }
        }

        class Rocket
        {
            static float width = 25;
            static float height = 5;
            Vector pos = new Vector(bmp.Width / 2, bmp.Height - height);
            Vector vel = new Vector(0, 0);
            Vector acc = new Vector(0, 0);
            public DNA dna;
            public double fitness;
            public int time = lifespan;
            public bool completed = false;
            public bool crashed = false;
            
            public Rocket(bool init)
            {
                if(init)
                {
                     dna = new DNA();
                }
            }

            void applyForce(Vector force)
            {
                acc += force;
            }

            public void calcFitness()
            {
                if(completed)
                {
                    fitness = Math.Pow(2.0, time / 5.0);
                }
                else
                {
                    double d = dist(pos.X, pos.Y, target.X, target.Y);
                    fitness = 1 / d;
                    if (crashed) { fitness /= 10; }
                }
                
            }

            public bool crash()
            {
                for (int i = 0; i < obstacles.Count; i++)
                {
                    if (pos.X > obstacles[i].Left && pos.X < obstacles[i].Right && pos.Y < obstacles[i].Bottom && pos.Y > obstacles[i].Top)
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
                    if (dist(pos.X, pos.Y, target.X, target.Y) <= 8)
                    {
                        completed = true;
                        pos.X = target.X;
                        pos.Y = target.Y;
                    }
                    else if(crash() || pos.X < 0 || pos.X > bmp.Width || pos.Y < 0 || pos.Y > bmp.Height)
                    {
                        crashed = true;
                    }
                    else
                    {
                        applyForce(dna.genes[count]);
                        vel += acc;
                        //if(vel.Length > 10) { vel -= acc; }
                        pos += vel;
                        acc *= 0;
                        time--;
                    }
                }
            }

            public void show()
            {
                RectangleF rect = new RectangleF((float)pos.X, (float)pos.Y, width, height);
                using (Matrix m = new Matrix())
                {
                    m.RotateAt((float)(Math.Atan2(vel.Y, vel.X) * 180 / Math.PI), new PointF(rect.Left + (rect.Width / 2),
                                              rect.Top + (rect.Height / 2)));
                    g.Transform = m;
                    g.FillRectangle(Brushes.White, rect);
                    g.ResetTransform();
                }
            }
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            g.Clear(Color.Black);
            g.FillEllipse(Brushes.White, (float)target.X, (float)target.Y, 16, 16);
            for (int i = 0; i < obstacles.Count; i++)
            {
                g.FillRectangle(Brushes.White, obstacles[i]);
            }
            pop.run();
            label1.Text = count.ToString();
            count++;
            if(count == lifespan)
            {
                pop.evaluate();
                pop.selection();
                label2.Text = maxF;
                count = 0;
            }
            pictureBox1.Image = bmp;
        }

        private void startButton_Click(object sender, EventArgs e)
        {
            startButton.Enabled = false;
            timer1.Enabled = true;
            timer1.Start();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            bmp = new Bitmap(pictureBox1.Width, pictureBox1.Height);
            g = Graphics.FromImage(bmp);
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.HighSpeed;
            g.Clear(Color.Black);
            pictureBox1.Image = bmp;
            rnd = new Random();
            pop = new Population(true);
            target = new Vector(bmp.Width / 2, 50);
            obstacles.Add(new Rectangle(0, 400, 350, 20));
            obstacles.Add(new Rectangle(250, 200, 350, 20));
            label1.Text = "";
            label2.Text = "";
        }

        public static double GetRandomNumber(double minimum, double maximum)
        {
            return rnd.NextDouble() * (maximum - minimum) + minimum;
        }

        public static double dist(double x1, double y1, double x2, double y2)
        {
            return Math.Sqrt(Math.Abs(x1 - x2) * Math.Abs(x1 - x2) + Math.Abs(y1 - y2) * Math.Abs(y1 - y2));
        }
    }
}
