﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Drawing;
using System.Threading;
using System.Threading.Tasks;
using System.Collections;
using System.Collections.Concurrent;
using BrailleIO.Structs;

namespace BrailleIO
{
    public partial class ShowOff : Form, IBrailleIOShowOffMonitor
    {
        #region Mouse events

        volatile bool mouseToGetureMode = false;

        void pictureBoxTouch_MouseDown(object sender, System.Windows.Forms.MouseEventArgs e)
        {
            //System.Diagnostics.Debug.WriteLine("mouse down");
            if (e.Button.HasFlag(MouseButtons.Left))
                startMouseGestureMode(e);
        }

        void pictureBoxTouch_MouseEnter(object sender, System.EventArgs e)
        {
            //System.Diagnostics.Debug.WriteLine("mouse entered");
            resetMouseGestureMode();
        }

        void pictureBoxTouch_MouseLeave(object sender, System.EventArgs e)
        {
            //System.Diagnostics.Debug.WriteLine("mouse left");
            {
                resetMouseGestureMode();
            }
        }

        void pictureBoxTouch_MouseMove(object sender, System.Windows.Forms.MouseEventArgs e)
        {
            if (mouseToGetureMode)
            {
                //System.Diagnostics.Debug.WriteLine("mouse move: " + e.Location);
                paintMousePosition(e.Location);
            }
        }

        void pictureBoxTouch_MouseUp(object sender, System.Windows.Forms.MouseEventArgs e)
        {
            //System.Diagnostics.Debug.WriteLine("mouse up");
            if (e.Button.HasFlag(MouseButtons.Left))
                resetMouseGestureMode();
        }

        #region utils

        void resetMouseGestureMode()
        {
            try
            {                
                if (mouseToGetureMode)
                {
                    this.PaintTouchMatrix(buildTouchMatrix(null));
                    fireTouchEvent(new List<Touch>());
                }
                mouseToGetureMode = false;
            }
            catch (System.Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Exception in resetting the mouse position " + ex);
            }
        }

        void startMouseGestureMode(System.Windows.Forms.MouseEventArgs e)
        {
            mouseToGetureMode = true;
            paintMousePosition(e.Location);
        }

        private void paintMousePosition(Point p)
        {
            try
            {
                Point pin = getPinForPoint(p);
                double x, y;
                getDetailedPinForPoint(p, out x, out y);

                var touchPoints = handleEllipsePoints(pin);
                var tm = buildTouchMatrix(touchPoints);
                this.PaintTouchMatrix(tm);

                List<Touch> detailedTouches = new List<Touch>(1);
                detailedTouches.Add(new Touch(x, y, 1.0D, TouchSizeRadiusX * 2.0, TouchSizeRadiusY * 2.0));

                // fire event
                fireTouchEvent(detailedTouches, tm);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Exception in painting mouse position " + ex);
            }
        }

        /// <summary>
        /// Converts a pixel point into a pin
        /// </summary>
        /// <param name="p">The mouse point in pixel.</param>
        /// <returns></returns>
        private Point getPinForPoint(Point p)
        {
            Point pin = new Point(0, 0);

            double x, y;
            getDetailedPinForPoint(p, out x, out y);

            if (x > -1 && y > -1)
            {
                pin.X = (int)Math.Round(x);
                pin.Y = (int)Math.Round(y);
            }
            return pin;
        }

        /// <summary>
        /// Converts a pixel point into a pin
        /// </summary>
        /// <param name="p">The mouse point in pixel.</param>
        /// <param name="pinX">The pin x.</param>
        /// <param name="pinY">The pin y.</param>
        private void getDetailedPinForPoint(Point p, out double pinX, out double pinY)
        {
            pinX = -1;
            pinY = -1;
            //Point pin = new Point(0, 0);
            if (this.pictureBoxTouch != null)
            {
                Size pbs = this.pictureBoxTouch.Size;

                double ratioX = (double)p.X / (double)pbs.Width;
                double ratioY = (double)p.Y / (double)pbs.Height;

                pinX = ratioX * cols;
                pinX = Math.Max(0, pinX);
                pinY = ratioY * rows;
                pinY = Math.Max(0, pinY);
            }
        }

        #endregion

        #endregion

        void fireTouchEvent(List<Touch> touches, double[,] touchM = null)
        {
            if (ShowOffAdapter != null)
            {
                if (touchM == null) touchM = buildTouchMatrix(touches);
                ShowOffAdapter.firetouchValuesChangedEvent(touchM, (int)DateTime.UtcNow.Ticks, touches);
            }
        }

        /// <summary>
        /// Builds the touch matrix from a list of points.
        /// </summary>
        /// <param name="touches">The touches.</param>
        /// <returns></returns>
        private double[,] buildTouchMatrix(List<Touch> touches)
        {
            double[,] touchM = new double[rows, cols];

            if (touches != null)
            {
                foreach (var p in touches)
                {
                    if (p.X >= 0 && p.Y >= 0 && p.X < cols && p.Y < rows)
                    { touchM[p.PinY, p.PinX] = p.Intense; }
                }
            }
            return touchM;
        }

        /// <summary>
        /// The radius for an simulated touch in horizontal dimension.
        /// </summary>
        public double TouchSizeRadiusX = 1;
        /// <summary>
        /// The radius for an simulated touch in vertical dimension.
        /// </summary>
        public double TouchSizeRadiusY = 1;

        /// <summary>
        /// Handles the ellipse points.
        /// </summary>
        /// <param name="p">The touch.</param>
        /// <returns>List of touched points</returns>
        private List<Touch> handleEllipsePoints(Point p)
        {
            ConcurrentBag<Touch> touchValues = new ConcurrentBag<Touch>();

            Point pos = new Point((int)Math.Round(TouchSizeRadiusX), (int)Math.Round(TouchSizeRadiusY));

            int width = (int)Math.Round(TouchSizeRadiusX * 2);
            int height = (int)Math.Round(TouchSizeRadiusY * 2);

            //check every element of the bonding box if inside or not
            Parallel.For(0, width + 1, x =>
                        {
                            Parallel.For(0, height + 1, y =>
                            {
                                double touch = PointIsInsideEllipse(new Point(x, y), TouchSizeRadiusX, TouchSizeRadiusY, TouchSizeRadiusX, TouchSizeRadiusY);
                                if (touch <= 1)
                                {
                                    touchValues.Add(new Touch(
                                        x + p.X - (int)Math.Round(TouchSizeRadiusX),
                                        y + p.Y - (int)Math.Round(TouchSizeRadiusY),
                                        Math.Max(0.1, 1 - touch)));
                                }
                            });
                        });
            return touchValues.ToList();
        }

        /// <summary>
        /// Determines whether [the specified pointToCheck] [is inside the ellipse].
        /// The region (disk) bounded by the ellipse is given by the equation:
        /// having an ellipse centered at (c_x,c_y), with semi-major axis r_x, semi-minor axis r_y,
        /// both aligned with the Cartesian plane.
        /// (x−c_x)^2         (y−c_y)^2
        /// ___________   +   ___________   ≤   1      (1)
        /// r_x ^2            r_y ^2
        /// So given a test point (x,y), plug it in (1). If the inequality is satisfied,
        /// then it is inside the ellipse; otherwise it is outside the ellipse.
        /// Moreover, the point is on the boundary of the region (i.e., on the ellipse)
        /// if and only if the inequality is satisfied tightly
        /// (i.e., the left hand side evaluates to 1)
        /// </summary>
        /// <param name="pointToCheck">The point to check.</param>
        /// <param name="c_x">The center point x.</param>
        /// <param name="c_y">The center point y.</param>
        /// <param name="r_x">1/2 width of the ellipse.</param>
        /// <param name="r_y">1/2 height of the ellipse.</param>
        /// <returns>
        /// Value must be smaller or equal to 1 - than the point is inside the ellipse, otherwise it is outside
        /// </returns>
        public static double PointIsInsideEllipse(Point pointToCheck, double c_x, double c_y, double r_x, double r_y)
        {
            if (r_x == 0 || r_y == 0)
                return 2;
            double xComponent =
                Math.Pow((double)(pointToCheck.X - c_x), 2)
                /
                Math.Pow(r_x, 2);

            double yComponent =
                    Math.Pow((double)(pointToCheck.Y - c_y), 2)
                    /
                    Math.Pow(r_y, 2);

            double value = xComponent + yComponent;
            return value;
        }
    }
}