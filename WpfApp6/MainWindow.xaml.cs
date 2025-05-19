using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace FlockingAlgoritm
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {

        private List<Boid> boids;
        private readonly int boidCount = 100;
        private readonly double canvasWidth = 800;
        private readonly double canvasHeight = 600;
        private readonly Random rand = new Random();
        private DispatcherTimer timer;
        private bool isParallel = false;
        private List<double> frameTimes = new List<double>();
        private const int maxFrameTimes = 100;

        public MainWindow()
        {
            InitializeComponent();
            InitializeBoids();
            InitializeUI();
        }

        private void InitializeBoids()
        {
            boids = new List<Boid>();
            for (int i = 0; i < boidCount; i++)
            {
                var boid = new Boid
                {
                    Position = new Vector2(rand.NextDouble() * canvasWidth, rand.NextDouble() * canvasHeight),
                    Velocity = new Vector2(rand.NextDouble() * 4 - 2, rand.NextDouble() * 4 - 2)
                };
                boids.Add(boid);
                var ellipse = new Ellipse { Width = 10, Height = 10, Fill = Brushes.Blue };
                Canvas.SetLeft(ellipse, boid.Position.X - 5);
                Canvas.SetTop(ellipse, boid.Position.Y - 5);
                SimulationCanvas.Children.Add(ellipse);
                boid.Ellipse = ellipse;
            }
        }

        private void InitializeUI()
        {
            timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(16) }; 
            timer.Tick += Timer_Tick;
        }

        private void Timer_Tick(object sender, EventArgs e)
        {
            var stopwatch = Stopwatch.StartNew();
            if (isParallel)
                UpdateBoidsParallel();
            else
                UpdateBoidsSerial();
            stopwatch.Stop();

            frameTimes.Add(stopwatch.ElapsedMilliseconds);
            if (frameTimes.Count > maxFrameTimes)
                frameTimes.RemoveAt(0);

            double avgFrameTime = frameTimes.Average();
            SpeedText.Text = $"Average Frame Time: {avgFrameTime:F2} ms ({(isParallel ? "Parallel" : "Serial")})";
        }

        private void UpdateBoidsSerial()
        {
            foreach (var boid in boids)
            {
                var neighbors = boids.Where(b => b != boid && (b.Position - boid.Position).Length < 50).ToList();
                boid.Update(neighbors, canvasWidth, canvasHeight);
            }
            foreach (var boid in boids)
            {
                Dispatcher.Invoke(() =>
                {
                    Canvas.SetLeft(boid.Ellipse, boid.Position.X - 5);
                    Canvas.SetTop(boid.Ellipse, boid.Position.Y - 5);
                });
            }
        }

        private void UpdateBoidsParallel()
        {
            Parallel.ForEach(boids, boid =>
            {
                var neighbors = boids.Where(b => b != boid && (b.Position - boid.Position).Length < 50).ToList();
                boid.Update(neighbors, canvasWidth, canvasHeight);
            });
            foreach (var boid in boids)
            {
                Dispatcher.Invoke(() =>
                {
                    Canvas.SetLeft(boid.Ellipse, boid.Position.X - 5);
                    Canvas.SetTop(boid.Ellipse, boid.Position.Y - 5);
                });
            }
        }

        private void SerialButton_Click(object sender, RoutedEventArgs e)
        {
            isParallel = false;
            frameTimes.Clear();
            timer.Start();
        }

        private void ParallelButton_Click(object sender, RoutedEventArgs e)
        {
            isParallel = true;
            frameTimes.Clear();
            timer.Start();
        }

        private void StopButton_Click(object sender, RoutedEventArgs e)
        {
            timer.Stop();
        }
    }

    public class Boid
    {
        public Vector2 Position { get; set; }
        public Vector2 Velocity { get; set; }
        public Ellipse Ellipse { get; set; }

        private const double maxSpeed = 3;
        private const double separationDistance = 25;
        private const double separationWeight = 2.5;
        private const double alignmentWeight = 1.0;
        private const double cohesionWeight = 0.5;

        public void Update(List<Boid> neighbors, double canvasWidth, double canvasHeight)
        {
            var separation = CalculateSeparation(neighbors);
            var alignment = CalculateAlignment(neighbors);
            var cohesion = CalculateCohesion(neighbors);

            Velocity += separation * separationWeight + alignment * alignmentWeight + cohesion * cohesionWeight;

            if (Velocity.Length > maxSpeed)
                Velocity = Velocity.Normalized() * maxSpeed;

            Position += Velocity;

            if (Position.X < 0) Position = new Vector2(Position.X + canvasWidth, Position.Y);
            if (Position.X > canvasWidth) Position = new Vector2(Position.X - canvasWidth, Position.Y);
            if (Position.Y < 0) Position = new Vector2(Position.X, Position.Y + canvasHeight);
            if (Position.Y > canvasHeight) Position = new Vector2(Position.X, Position.Y - canvasHeight);
        }

        private Vector2 CalculateSeparation(List<Boid> neighbors)
        {
            Vector2 steer = new Vector2(0, 0);
            int count = 0;
            foreach (var other in neighbors)
            {
                double distance = (other.Position - Position).Length;
                if (distance > 0 && distance < separationDistance)
                {
                    Vector2 diff = Position - other.Position;
                    diff = diff.Normalized() / distance;
                    steer += diff;
                    count++;
                }
            }
            if (count > 0)
                steer /= count;
            return steer;
        }

        private Vector2 CalculateAlignment(List<Boid> neighbors)
        {
            Vector2 avgVelocity = new Vector2(0, 0);
            int count = 0;
            foreach (var other in neighbors)
            {
                avgVelocity += other.Velocity;
                count++;
            }
            if (count > 0)
            {
                avgVelocity /= count;
                if (avgVelocity.Length > 0)
                    avgVelocity = avgVelocity.Normalized() * maxSpeed;
                return (avgVelocity - Velocity) * 0.1;
            }
            return new Vector2(0, 0);
        }

        private Vector2 CalculateCohesion(List<Boid> neighbors)
        {
            Vector2 center = new Vector2(0, 0);
            int count = 0;
            foreach (var other in neighbors)
            {
                center += other.Position;
                count++;
            }
            if (count > 0)
            {
                center /= count;
                return (center - Position) * 0.005;
            }
            return new Vector2(0, 0);
        }
    }

    public struct Vector2
    {
        public double X { get; set; }
        public double Y { get; set; }

        public Vector2(double x, double y)
        {
            X = x;
            Y = y;
        }

        public double Length => Math.Sqrt(X * X + Y * Y);

        public Vector2 Normalized()
        {
            double length = Length;
            return length > 0 ? new Vector2(X / length, Y / length) : new Vector2(0, 0);
        }

        public static Vector2 operator +(Vector2 a, Vector2 b) => new Vector2(a.X + b.X, a.Y + b.Y);
        public static Vector2 operator -(Vector2 a, Vector2 b) => new Vector2(a.X - b.X, a.Y - b.Y);
        public static Vector2 operator *(Vector2 a, double scalar) => new Vector2(a.X * scalar, a.Y * scalar);
        public static Vector2 operator /(Vector2 a, double scalar) => new Vector2(a.X / scalar, a.Y / scalar);
    }

}