/*
Licensed under the Apache License, Version 2.0 (the "License");
you may not use this file except in compliance with the License.
You may obtain a copy of the License at

    http://www.apache.org/licenses/LICENSE-2.0

Unless required by applicable law or agreed to in writing, software
distributed under the License is distributed on an "AS IS" BASIS,
WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
See the License for the specific language governing permissions and
limitations under the License.
*/

using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace MeshCentralRouter
{
    public class VerticalToggleSwitch : Control
    {
        private bool _checked = false;
        private Color _onColor = Color.FromArgb(76, 175, 80); // Green
        private Color _offColor = Color.LightGray;
        private Color _thumbColor = Color.White;
        private Color _trackBorderColor = Color.Empty;
        private Timer _animationTimer;
        private float _animationProgress = 0f;

        public event EventHandler CheckedChanged;

        public VerticalToggleSwitch()
        {
            this.SetStyle(ControlStyles.UserPaint |
                         ControlStyles.AllPaintingInWmPaint |
                         ControlStyles.OptimizedDoubleBuffer, true);
            this.Size = new Size(15, 31); // Vertical orientation: narrow width, taller height
            this.Cursor = Cursors.Hand;

            _animationTimer = new Timer();
            _animationTimer.Interval = 10;
            _animationTimer.Tick += AnimationTimer_Tick;
            
            // Subscribe to theme changes
            ThemeManager.Instance.ThemeChanged += ThemeManager_ThemeChanged;
            UpdateThemeColors();
        }

        private void ThemeManager_ThemeChanged(object sender, EventArgs e)
        {
            UpdateThemeColors();
        }

        private void UpdateThemeColors()
        {
            if (ThemeManager.Instance.IsDarkMode)
            {
                this.BackColor = ThemeManager.Instance.GetBackgroundColor();
                _offColor = Color.FromArgb(60, 60, 60);
                _thumbColor = Color.FromArgb(200, 200, 200);
            }
            else
            {
                this.BackColor = Color.White;
                _offColor = Color.LightGray;
                _thumbColor = Color.White;
            }
            Invalidate();
        }

        public bool Checked
        {
            get { return _checked; }
            set
            {
                if (_checked != value)
                {
                    _checked = value;
                    StartAnimation();
                    CheckedChanged?.Invoke(this, EventArgs.Empty);
                }
            }
        }

        public Color OnColor
        {
            get { return _onColor; }
            set { _onColor = value; Invalidate(); }
        }

        public Color OffColor
        {
            get { return _offColor; }
            set { _offColor = value; Invalidate(); }
        }

        public Color ThumbColor
        {
            get { return _thumbColor; }
            set { _thumbColor = value; Invalidate(); }
        }

        // Optional explicit border color for the track. If empty, a subtle border is auto-selected.
        public Color TrackBorderColor
        {
            get { return _trackBorderColor; }
            set { _trackBorderColor = value; Invalidate(); }
        }

        private void StartAnimation()
        {
            _animationTimer.Start();
        }

        private void AnimationTimer_Tick(object sender, EventArgs e)
        {
            if (_checked)
            {
                _animationProgress += 0.1f;
                if (_animationProgress >= 1f)
                {
                    _animationProgress = 1f;
                    _animationTimer.Stop();
                }
            }
            else
            {
                _animationProgress -= 0.1f;
                if (_animationProgress <= 0f)
                {
                    _animationProgress = 0f;
                    _animationTimer.Stop();
                }
            }
            Invalidate();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            // Clear background so the rounded corners don't show stale pixels.
            g.Clear(this.BackColor);

            // Draw background track (inset by 1 to prevent border clipping)
            int trackWidth = this.Width - 1;
            int trackHeight = this.Height - 1;
            Rectangle trackRect = new Rectangle(0, 0, trackWidth, trackHeight);

            // Interpolate color based on animation progress
            Color currentColor = InterpolateColor(_offColor, _onColor, _animationProgress);

            using (SolidBrush trackBrush = new SolidBrush(currentColor))
            using (GraphicsPath trackPath = GetRoundedRectangle(trackRect, trackWidth / 2))
            {
                g.FillPath(trackBrush, trackPath);

                Color borderColor = _trackBorderColor;
                if (borderColor.IsEmpty)
                {
                    // Pick a subtle contrasting border depending on luminance.
                    int luminance = (int)(0.2126 * currentColor.R + 0.7152 * currentColor.G + 0.0722 * currentColor.B);
                    borderColor = (luminance < 128) ? Color.FromArgb(70, 255, 255, 255) : Color.FromArgb(70, 0, 0, 0);
                }

                using (Pen borderPen = new Pen(borderColor, 1))
                {
                    g.DrawPath(borderPen, trackPath);
                }
            }

            // Draw thumb (the sliding circle) - animates vertically
            // When checked (ON/Green), thumb is at TOP. When unchecked (OFF/Gray), thumb is at BOTTOM.
            int thumbPadding = 2;  // Padding from track edge to thumb
            int thumbSize = trackWidth - (thumbPadding * 2);
            int thumbX = thumbPadding;
            int thumbMinY = thumbPadding;                                    // TOP position (when ON/checked)
            int thumbMaxY = trackHeight - thumbPadding - thumbSize;          // BOTTOM position (when OFF/unchecked)
            // Invert animation: at progress 1 (checked=true), thumb is at TOP; at progress 0 (checked=false), thumb is at BOTTOM
            int thumbY = thumbMaxY - (int)((thumbMaxY - thumbMinY) * _animationProgress);

            Rectangle thumbRect = new Rectangle(thumbX, thumbY, thumbSize, thumbSize);

            using (SolidBrush thumbBrush = new SolidBrush(_thumbColor))
            using (GraphicsPath thumbPath = GetRoundedRectangle(thumbRect, thumbSize / 2))
            {
                g.FillPath(thumbBrush, thumbPath);

                // Optional: Add subtle shadow to thumb
                using (Pen shadowPen = new Pen(Color.FromArgb(30, 0, 0, 0), 1))
                {
                    g.DrawPath(shadowPen, thumbPath);
                }
            }
        }

        private Color InterpolateColor(Color color1, Color color2, float progress)
        {
            int r = (int)(color1.R + (color2.R - color1.R) * progress);
            int gr = (int)(color1.G + (color2.G - color1.G) * progress);
            int b = (int)(color1.B + (color2.B - color1.B) * progress);
            return Color.FromArgb(r, gr, b);
        }

        private GraphicsPath GetRoundedRectangle(Rectangle rect, int radius)
        {
            GraphicsPath path = new GraphicsPath();
            int diameter = radius * 2;

            path.AddArc(rect.X, rect.Y, diameter, diameter, 180, 90);
            path.AddArc(rect.Right - diameter, rect.Y, diameter, diameter, 270, 90);
            path.AddArc(rect.Right - diameter, rect.Bottom - diameter, diameter, diameter, 0, 90);
            path.AddArc(rect.X, rect.Bottom - diameter, diameter, diameter, 90, 90);
            path.CloseFigure();

            return path;
        }

        protected override void OnClick(EventArgs e)
        {
            base.OnClick(e);
            Checked = !Checked;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (_animationTimer != null)
                {
                    _animationTimer.Stop();
                    _animationTimer.Dispose();
                    _animationTimer = null;
                }
                ThemeManager.Instance.ThemeChanged -= ThemeManager_ThemeChanged;
            }
            base.Dispose(disposing);
        }
    }
}
