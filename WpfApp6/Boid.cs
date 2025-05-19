using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace FlockingAlgoritm
{
    public class Boid2
    {
        public System.Windows.Point Position;
        public Vector Velocity;
        public double Speed = 2.0;

        public Boid2(double x, double y)
        {
            Position = new System.Windows.Point(x, y);
            Velocity = new Vector(Random.Shared.NextDouble() * 2 - 1, Random.Shared.NextDouble() * 2 - 1);
            Velocity.Normalize();
            Velocity *= Speed;
        }

        public void Update(List<Boid2> boids, double width, double height)
        {
            Vector alignment = Align(boids);
            Vector cohesion = Cohere(boids);
            Vector separation = Separate(boids);

            Velocity += alignment + cohesion + separation;

            if (Velocity.Length > Speed)
            {
                Velocity.Normalize();
                Velocity *= Speed;
            }

            Position += Velocity;

            if (Position.X < 0) { Position.X = 0; Velocity.X *= -1; }
            if (Position.X > width) { Position.X = width; Velocity.X *= -1; }
            if (Position.Y < 0) { Position.Y = 0; Velocity.Y *= -1; }
            if (Position.Y > height) { Position.Y = height; Velocity.Y *= -1; }

        }

        private Vector Align(List<Boid2> boids)
        {
            Vector avg = new Vector();
            int count = 0;
            foreach (var b in boids)
            {
                if (b == this) continue;
                if ((b.Position - Position).Length < 50)
                {
                    avg += b.Velocity;
                    count++;
                }
            }
            if (count > 0)
            {
                avg /= count;
                avg.Normalize();
                return (avg - Velocity) * 0.05;
            }
            return new Vector();
        }

        private Vector Cohere(List<Boid2> boids)
        {
            Vector center = new Vector();
            int count = 0;
            foreach (var b in boids)
            {
                if (b == this) continue;
                if ((b.Position - Position).Length < 50)
                {
                    center += (Vector)b.Position;
                    count++;
                }
            }
            if (count > 0)
            {
                center /= count;
                Vector direction = center - (Vector)Position;
                direction.Normalize();
                return direction * 0.01;
            }
            return new Vector();
        }

        private Vector Separate(List<Boid2> boids)
        {
            Vector repulse = new Vector();
            foreach (var b in boids)
            {
                if (b == this) continue;
                double dist = (b.Position - Position).Length;
                if (dist < 20)
                {
                    repulse += (Vector)(Position - b.Position);
                }
            }
            repulse.Normalize();
            return repulse * 0.1;
        }
    }

}
