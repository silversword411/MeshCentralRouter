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
    /// <summary>
    /// A polished toggle setting item with icon, label, and toggle switch.
    /// Provides hover effects, rounded corners, and consistent spacing.
    /// </summary>
    public class SettingsToggleItem : Control
    {
        private bool _checked = false;
        private bool _isHovered = false;
        private string _icon = "";
        private string _labelText = "";
        private ToggleSwitch _toggleSwitch;
        private Timer _animationTimer;
        private float _hoverProgress = 0f;
        private bool _suppressEvents = false;

        // Theme colors
        private Color _backgroundColor;
        private Color _hoverColor;
        private Color _textColor;
        private Color _iconColor;
        private Color _borderColor;
        private int _cornerRadius = 8;

        public event EventHandler CheckedChanged;

        public SettingsToggleItem()
        {
            this.SetStyle(ControlStyles.UserPaint |
                         ControlStyles.AllPaintingInWmPaint |
                         ControlStyles.OptimizedDoubleBuffer |
                         ControlStyles.ResizeRedraw, true);

            this.Size = new Size(160, 64);
            this.Cursor = Cursors.Hand;

            // Initialize with dark theme defaults
            bool isDark = ThemeManager.Instance.IsDarkMode;
            UpdateThemeColors(isDark);

            // Create the internal toggle switch
            _toggleSwitch = new ToggleSwitch();
            _toggleSwitch.Size = new Size(36, 18);
            _toggleSwitch.OnColor = Color.FromArgb(76, 175, 80);
            _toggleSwitch.CheckedChanged += ToggleSwitch_CheckedChanged;
            this.Controls.Add(_toggleSwitch);

            // Animation timer for smooth hover effect
            _animationTimer = new Timer();
            _animationTimer.Interval = 16; // ~60fps
            _animationTimer.Tick += AnimationTimer_Tick;

            PositionToggleSwitch();
        }

        private void ToggleSwitch_CheckedChanged(object sender, EventArgs e)
        {
            if (_suppressEvents) return;
            _checked = _toggleSwitch.Checked;
            CheckedChanged?.Invoke(this, EventArgs.Empty);
        }

        public bool Checked
        {
            get { return _checked; }
            set
            {
                if (_checked != value)
                {
                    _checked = value;
                    _suppressEvents = true;
                    _toggleSwitch.Checked = value;
                    _suppressEvents = false;
                    // Don't fire event when setting programmatically - only on user interaction
                }
            }
        }

        public string Icon
        {
            get { return _icon; }
            set { _icon = value; Invalidate(); }
        }

        public string LabelText
        {
            get { return _labelText; }
            set { _labelText = value; Invalidate(); }
        }

        public int CornerRadius
        {
            get { return _cornerRadius; }
            set { _cornerRadius = value; Invalidate(); }
        }

        public ToggleSwitch ToggleSwitchControl => _toggleSwitch;

        public void UpdateTheme(bool isDarkMode)
        {
            UpdateThemeColors(isDarkMode);
            _toggleSwitch.OffColor = isDarkMode ? Color.FromArgb(90, 90, 90) : Color.FromArgb(180, 180, 180);
            _toggleSwitch.OnColor = Color.FromArgb(76, 175, 80);
            _toggleSwitch.ThumbColor = isDarkMode ? Color.FromArgb(235, 235, 235) : Color.White;
            _toggleSwitch.BackColor = Color.Transparent;
            Invalidate();
        }

        private void UpdateThemeColors(bool isDarkMode)
        {
            _backgroundColor = isDarkMode ? Color.FromArgb(55, 55, 55) : Color.FromArgb(240, 240, 240);
            _hoverColor = isDarkMode ? Color.FromArgb(70, 70, 70) : Color.FromArgb(225, 225, 225);
            _textColor = isDarkMode ? Color.White : Color.FromArgb(30, 30, 30);
            _iconColor = isDarkMode ? Color.FromArgb(180, 180, 180) : Color.FromArgb(100, 100, 100);
            _borderColor = isDarkMode ? Color.FromArgb(75, 75, 75) : Color.FromArgb(200, 200, 200);
        }

        private void AnimationTimer_Tick(object sender, EventArgs e)
        {
            if (_isHovered)
            {
                _hoverProgress = Math.Min(1f, _hoverProgress + 0.15f);
            }
            else
            {
                _hoverProgress = Math.Max(0f, _hoverProgress - 0.15f);
            }

            if ((_isHovered && _hoverProgress >= 1f) || (!_isHovered && _hoverProgress <= 0f))
            {
                _animationTimer.Stop();
            }

            Invalidate();
        }

        private void PositionToggleSwitch()
        {
            // Position toggle switch at bottom center
            int toggleX = (this.Width - _toggleSwitch.Width) / 2;
            int toggleY = this.Height - _toggleSwitch.Height - 10;
            _toggleSwitch.Location = new Point(toggleX, toggleY);
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            PositionToggleSwitch();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

            // Calculate interpolated background color for hover effect
            Color bgColor = InterpolateColor(_backgroundColor, _hoverColor, _hoverProgress);

            // Draw rounded rectangle background
            Rectangle rect = new Rectangle(1, 1, this.Width - 3, this.Height - 3);
            using (GraphicsPath path = GetRoundedRectangle(rect, _cornerRadius))
            {
                using (SolidBrush brush = new SolidBrush(bgColor))
                {
                    g.FillPath(brush, path);
                }

                // Draw border
                using (Pen borderPen = new Pen(_borderColor, 1))
                {
                    g.DrawPath(borderPen, path);
                }
            }

            // Draw icon if present (centered horizontally, near top)
            if (!string.IsNullOrEmpty(_icon))
            {
                using (Font iconFont = new Font("Segoe UI Emoji", 14f))
                {
                    SizeF iconSize = g.MeasureString(_icon, iconFont);
                    float iconX = (this.Width - iconSize.Width) / 2;
                    float iconY = 8;
                    using (SolidBrush iconBrush = new SolidBrush(_iconColor))
                    {
                        g.DrawString(_icon, iconFont, iconBrush, iconX, iconY);
                    }
                }
            }

            // Draw label text (centered horizontally, below icon)
            if (!string.IsNullOrEmpty(_labelText))
            {
                using (Font labelFont = new Font("Segoe UI", 8.5f))
                {
                    // Handle multi-line text
                    StringFormat sf = new StringFormat();
                    sf.Alignment = StringAlignment.Center;
                    sf.LineAlignment = StringAlignment.Near;

                    float labelY = string.IsNullOrEmpty(_icon) ? 10 : 30;
                    RectangleF labelRect = new RectangleF(4, labelY, this.Width - 8, 30);

                    using (SolidBrush textBrush = new SolidBrush(_textColor))
                    {
                        g.DrawString(_labelText, labelFont, textBrush, labelRect, sf);
                    }
                }
            }
        }

        private Color InterpolateColor(Color color1, Color color2, float progress)
        {
            int r = (int)(color1.R + (color2.R - color1.R) * progress);
            int g = (int)(color1.G + (color2.G - color1.G) * progress);
            int b = (int)(color1.B + (color2.B - color1.B) * progress);
            return Color.FromArgb(r, g, b);
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

        protected override void OnMouseEnter(EventArgs e)
        {
            base.OnMouseEnter(e);
            _isHovered = true;
            _animationTimer.Start();
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            base.OnMouseLeave(e);
            // Check if mouse is still within bounds (including child controls)
            Point clientPoint = this.PointToClient(Control.MousePosition);
            if (!this.ClientRectangle.Contains(clientPoint))
            {
                _isHovered = false;
                _animationTimer.Start();
            }
        }

        protected override void OnClick(EventArgs e)
        {
            base.OnClick(e);
            // Toggle the switch when clicking anywhere on the control
            // This is a user interaction, so we toggle via the internal switch
            // which will fire the CheckedChanged event
            _toggleSwitch.Checked = !_toggleSwitch.Checked;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _animationTimer?.Dispose();
                _toggleSwitch?.Dispose();
            }
            base.Dispose(disposing);
        }
    }

    /// <summary>
    /// A simple action button for the settings pane with consistent styling.
    /// </summary>
    public class SettingsActionButton : Control
    {
        private bool _isHovered = false;
        private string _icon = "";
        private string _labelText = "";
        private Timer _animationTimer;
        private float _hoverProgress = 0f;

        // Theme colors
        private Color _backgroundColor;
        private Color _hoverColor;
        private Color _textColor;
        private Color _iconColor;
        private Color _borderColor;
        private int _cornerRadius = 8;

        public event EventHandler ButtonClick;

        public SettingsActionButton()
        {
            this.SetStyle(ControlStyles.UserPaint |
                         ControlStyles.AllPaintingInWmPaint |
                         ControlStyles.OptimizedDoubleBuffer |
                         ControlStyles.ResizeRedraw, true);

            this.Size = new Size(160, 64);
            this.Cursor = Cursors.Hand;

            // Initialize with dark theme defaults
            bool isDark = ThemeManager.Instance.IsDarkMode;
            UpdateThemeColors(isDark);

            // Animation timer for smooth hover effect
            _animationTimer = new Timer();
            _animationTimer.Interval = 16;
            _animationTimer.Tick += AnimationTimer_Tick;
        }

        public string Icon
        {
            get { return _icon; }
            set { _icon = value; Invalidate(); }
        }

        public string LabelText
        {
            get { return _labelText; }
            set { _labelText = value; Invalidate(); }
        }

        public int CornerRadius
        {
            get { return _cornerRadius; }
            set { _cornerRadius = value; Invalidate(); }
        }

        public void UpdateTheme(bool isDarkMode)
        {
            UpdateThemeColors(isDarkMode);
            Invalidate();
        }

        private void UpdateThemeColors(bool isDarkMode)
        {
            _backgroundColor = isDarkMode ? Color.FromArgb(55, 55, 55) : Color.FromArgb(240, 240, 240);
            _hoverColor = isDarkMode ? Color.FromArgb(70, 70, 70) : Color.FromArgb(225, 225, 225);
            _textColor = isDarkMode ? Color.White : Color.FromArgb(30, 30, 30);
            _iconColor = isDarkMode ? Color.FromArgb(180, 180, 180) : Color.FromArgb(100, 100, 100);
            _borderColor = isDarkMode ? Color.FromArgb(75, 75, 75) : Color.FromArgb(200, 200, 200);
        }

        private void AnimationTimer_Tick(object sender, EventArgs e)
        {
            if (_isHovered)
            {
                _hoverProgress = Math.Min(1f, _hoverProgress + 0.15f);
            }
            else
            {
                _hoverProgress = Math.Max(0f, _hoverProgress - 0.15f);
            }

            if ((_isHovered && _hoverProgress >= 1f) || (!_isHovered && _hoverProgress <= 0f))
            {
                _animationTimer.Stop();
            }

            Invalidate();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

            Color bgColor = InterpolateColor(_backgroundColor, _hoverColor, _hoverProgress);

            Rectangle rect = new Rectangle(1, 1, this.Width - 3, this.Height - 3);
            using (GraphicsPath path = GetRoundedRectangle(rect, _cornerRadius))
            {
                using (SolidBrush brush = new SolidBrush(bgColor))
                {
                    g.FillPath(brush, path);
                }

                using (Pen borderPen = new Pen(_borderColor, 1))
                {
                    g.DrawPath(borderPen, path);
                }
            }

            // Draw icon and label centered vertically
            float totalHeight = 0;
            float iconHeight = 0;
            float labelHeight = 0;

            using (Font iconFont = new Font("Segoe UI Emoji", 16f))
            using (Font labelFont = new Font("Segoe UI", 8.5f))
            {
                if (!string.IsNullOrEmpty(_icon))
                {
                    iconHeight = g.MeasureString(_icon, iconFont).Height;
                    totalHeight += iconHeight;
                }
                if (!string.IsNullOrEmpty(_labelText))
                {
                    labelHeight = g.MeasureString(_labelText, labelFont).Height;
                    totalHeight += labelHeight;
                }

                float startY = (this.Height - totalHeight) / 2;

                if (!string.IsNullOrEmpty(_icon))
                {
                    SizeF iconSize = g.MeasureString(_icon, iconFont);
                    float iconX = (this.Width - iconSize.Width) / 2;
                    using (SolidBrush iconBrush = new SolidBrush(_iconColor))
                    {
                        g.DrawString(_icon, iconFont, iconBrush, iconX, startY);
                    }
                    startY += iconHeight;
                }

                if (!string.IsNullOrEmpty(_labelText))
                {
                    StringFormat sf = new StringFormat();
                    sf.Alignment = StringAlignment.Center;
                    sf.LineAlignment = StringAlignment.Near;

                    RectangleF labelRect = new RectangleF(4, startY, this.Width - 8, labelHeight + 4);
                    using (SolidBrush textBrush = new SolidBrush(_textColor))
                    {
                        g.DrawString(_labelText, labelFont, textBrush, labelRect, sf);
                    }
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

        protected override void OnMouseEnter(EventArgs e)
        {
            base.OnMouseEnter(e);
            _isHovered = true;
            _animationTimer.Start();
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            base.OnMouseLeave(e);
            Point clientPoint = this.PointToClient(Control.MousePosition);
            if (!this.ClientRectangle.Contains(clientPoint))
            {
                _isHovered = false;
                _animationTimer.Start();
            }
        }

        protected override void OnClick(EventArgs e)
        {
            base.OnClick(e);
            ButtonClick?.Invoke(this, EventArgs.Empty);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _animationTimer?.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
