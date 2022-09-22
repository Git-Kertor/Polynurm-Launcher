using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;

namespace Polynurm_Launcher
{
    public class PolyBackground
    {

        public MainWindow win = Application.Current.Windows[0] as MainWindow;

        private List<Point[]> tris;
        private List<Ellipse[]> polys;
        private List<Polygon> polygons;
        private List<Ellipse> pointObjects;
        private List<Point> startPoints;
        private List<Point> nowPoints;
        private List<Point> endPoints;
        private List<float> alphaPointsEnd;
        private List<float> alphaPointsNow;

        public void CreatePolyCanvas()
        {
            SolidColorBrush solidColorBrush = new SolidColorBrush();
            solidColorBrush.Color = Color.FromRgb(255, 255, 255);

            //Absolute pain this is.
            polys = new List<Ellipse[]>
            {
                new Ellipse[3] { win.point6, win.point13, win.point8 },
                new Ellipse[3] { win.point8, win.point13, win.point14 },
                new Ellipse[3] { win.point1, win.point13, win.point14 },
                new Ellipse[3] { win.point13, win.point1, win.point7 },
                new Ellipse[3] { win.point2, win.point1, win.point7 },
                new Ellipse[3] { win.point2, win.point3, win.point14 },
                new Ellipse[3] { win.point2, win.point3, win.point5 },
                new Ellipse[3] { win.point5, win.point2, win.point11 },
                new Ellipse[3] { win.point11, win.point12, win.point5 },
                new Ellipse[3] { win.point9, win.point12, win.point5 },
                new Ellipse[3] { win.point5, win.point9, win.point4 },
                new Ellipse[3] { win.point3, win.point4, win.point8 },
                new Ellipse[3] { win.point8, win.point9, win.point4 },
                new Ellipse[3] { win.point7, win.point2, win.point11 },
                new Ellipse[3] { win.point1, win.point2, win.point14 },
                new Ellipse[3] { win.point3, win.point4, win.point5 },
                new Ellipse[3] { win.point3, win.point14, win.point8 }
            };

            pointObjects = new List<Ellipse>();
            pointObjects.Add(win.point1);
            pointObjects.Add(win.point2);
            pointObjects.Add(win.point3);
            pointObjects.Add(win.point4);
            pointObjects.Add(win.point5);
            pointObjects.Add(win.point6);
            pointObjects.Add(win.point7);
            pointObjects.Add(win.point8);
            pointObjects.Add(win.point9);
            pointObjects.Add(win.point10);
            pointObjects.Add(win.point11);
            pointObjects.Add(win.point12);
            pointObjects.Add(win.point13);
            pointObjects.Add(win.point14);

            startPoints = new List<Point>();
            foreach (Ellipse obj in pointObjects)
            {
                Point temp = new Point(Canvas.GetLeft(obj), Canvas.GetTop(obj));
                startPoints.Add(temp);
            }
            nowPoints = new List<Point>(startPoints);
            endPoints = new List<Point>(startPoints);
            tris = new List<Point[]>();
            foreach (Ellipse[] poly in polys)
            {
                tris.Add(new Point[3] { new Point(Canvas.GetLeft(poly[0]), Canvas.GetTop(poly[0])),
                    new Point(Canvas.GetLeft(poly[1]), Canvas.GetTop(poly[1])),
                    new Point(Canvas.GetLeft(poly[2]), Canvas.GetTop(poly[2]))});
            }
            Random rand = new Random();
            polygons = new List<Polygon>();
            alphaPointsNow = new List<float>();
            alphaPointsEnd = new List<float>();
            foreach (Point[] tri in tris)
            {
                Polygon pol = new Polygon();
                PointCollection polyPoints = new PointCollection();
                polyPoints.Add(tri[0]); polyPoints.Add(tri[1]); polyPoints.Add(tri[2]);
                pol.Points = polyPoints;
                SolidColorBrush brush = new SolidColorBrush();
                float alpha = rand.Next(60, 120);
                brush.Color = Color.FromArgb((byte)alpha, 255, 255, 255);
                pol.Stroke = Brushes.Transparent;

                alphaPointsNow.Add(alpha);
                alphaPointsEnd.Add(rand.Next(60, 120));

                pol.Fill = brush;
                win.polyCanvas.Children.Add(pol);
                polygons.Add(pol);
            }
        }

        private double PointDistance(double x1, double y1, double x2, double y2)
        {
            return Math.Abs(Math.Sqrt(Math.Pow(x1 - x2, 2) + Math.Pow(y1 - y2, 2)));
        }

        public void PolyCanvasTick(object sender, EventArgs e)
        {
            Random rand = new Random();
            for (int i = 0; i < startPoints.Count; i++)
            {
                if (PointDistance(nowPoints[i].X, nowPoints[i].Y, endPoints[i].X, endPoints[i].Y) < 1)
                {
                    endPoints[i] = new Point(startPoints[i].X + Math.Cos(rand.Next(0, 10)) * 50,
                        startPoints[i].Y + Math.Sin(rand.Next(0, 10)) * 50);
                }
                else
                {
                    double _x = nowPoints[i].X + (endPoints[i].X - nowPoints[i].X) * 0.001;
                    double _y = nowPoints[i].Y + (endPoints[i].Y - nowPoints[i].Y) * 0.001;
                    nowPoints[i] = new Point(_x, _y);
                }
                Canvas.SetLeft(pointObjects[i], nowPoints[i].X);
                Canvas.SetTop(pointObjects[i], nowPoints[i].Y);
            }
            tris = new List<Point[]>();
            foreach (Ellipse[] poly in polys)
            {
                tris.Add(new Point[3] { new Point(Canvas.GetLeft(poly[0]), Canvas.GetTop(poly[0])),
                    new Point(Canvas.GetLeft(poly[1]), Canvas.GetTop(poly[1])),
                    new Point(Canvas.GetLeft(poly[2]), Canvas.GetTop(poly[2]))});
            }
            for (int i = 0; i < polygons.Count; i++)
            {
                PointCollection polyPoints = new PointCollection();
                Point[] tri = tris[i];
                polyPoints.Add(tri[0]); polyPoints.Add(tri[1]); polyPoints.Add(tri[2]);
                polygons[i].Points = polyPoints;
                if (Math.Abs(alphaPointsEnd[i] - alphaPointsNow[i]) < 5)
                {
                    alphaPointsEnd[i] = rand.Next(60, 120);
                }
                float alpha = alphaPointsNow[i] + (alphaPointsEnd[i] - alphaPointsNow[i]) / 100;

                SolidColorBrush brush = new SolidColorBrush();
                brush.Color = Color.FromArgb((byte)alpha, 255, 255, 255);
                alphaPointsNow[i] = alpha;
                polygons[i].Fill = brush;
            }
        }
    }
}
