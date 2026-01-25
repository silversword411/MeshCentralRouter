/*
Copyright 2009-2022 Intel Corporation

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
using System.Windows.Forms;
using System.Drawing;
using System.Drawing.Drawing2D;

namespace MeshCentralRouter
{
    public class ThemeManager
    {
        private static ThemeManager _instance;
        private bool _isDarkMode;
        
        public event EventHandler ThemeChanged;

        private ThemeManager()
        {
            // Load theme preference from registry
            _isDarkMode = Settings.GetRegValue("DarkMode", false);
        }

        public static ThemeManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new ThemeManager();
                }
                return _instance;
            }
        }

        public bool IsDarkMode
        {
            get { return _isDarkMode; }
            set
            {
                if (_isDarkMode != value)
                {
                    _isDarkMode = value;
                    Settings.SetRegValue("DarkMode", _isDarkMode);
                    ThemeChanged?.Invoke(this, EventArgs.Empty);
                }
            }
        }

        public Color GetTitleBarColor()
        {
            return _isDarkMode ? Color.FromArgb(45, 45, 48) : SystemColors.Control;
        }

        public Color GetTitleBarTextColor()
        {
            return _isDarkMode ? Color.White : SystemColors.ControlText;
        }

        public Color GetBackgroundColor()
        {
            return _isDarkMode ? Color.FromArgb(30, 30, 30) : Color.White;
        }

        public Color GetForegroundColor()
        {
            return _isDarkMode ? Color.White : Color.Black;
        }

        public Color GetButtonHoverColor()
        {
            return _isDarkMode ? Color.FromArgb(60, 60, 60) : Color.FromArgb(240, 240, 240);
        }

        public ToolStripRenderer GetContextMenuRenderer()
        {
            if (_isDarkMode)
            {
                return new DarkModeToolStripRenderer();
            }
            else
            {
                return new ToolStripProfessionalRenderer();
            }
        }
    }

    /// <summary>
    /// Custom renderer for dark mode context menus
    /// </summary>
    public class DarkModeToolStripRenderer : ToolStripProfessionalRenderer
    {
        private static readonly Color DarkBackground = Color.FromArgb(45, 45, 48);
        private static readonly Color DarkImageMargin = Color.FromArgb(55, 55, 58); // Slightly lighter for left margin
        private static readonly Color DarkBorder = Color.FromArgb(60, 60, 60);
        private static readonly Color DarkSeparator = Color.FromArgb(80, 80, 80);
        private static readonly Color DarkHighlight = Color.FromArgb(62, 62, 66);
        private static readonly Color DarkText = Color.FromArgb(240, 240, 240);

        public DarkModeToolStripRenderer() : base(new DarkModeColorTable())
        {
        }

        protected override void OnRenderToolStripBackground(ToolStripRenderEventArgs e)
        {
            // Fill entire background with dark color
            using (SolidBrush brush = new SolidBrush(DarkBackground))
            {
                e.Graphics.FillRectangle(brush, e.AffectedBounds);
            }

            // Draw a lighter gutter strip on the left side (full height)
            int gutterWidth = 28; // Standard width for icon gutter
            Rectangle gutterRect = new Rectangle(0, 0, gutterWidth, e.AffectedBounds.Height);
            using (SolidBrush brush = new SolidBrush(DarkImageMargin))
            {
                e.Graphics.FillRectangle(brush, gutterRect);
            }
        }

        protected override void OnRenderToolStripBorder(ToolStripRenderEventArgs e)
        {
            using (Pen pen = new Pen(DarkBorder))
            {
                Rectangle rect = new Rectangle(0, 0, e.AffectedBounds.Width - 1, e.AffectedBounds.Height - 1);
                e.Graphics.DrawRectangle(pen, rect);
            }
        }

        protected override void OnRenderMenuItemBackground(ToolStripItemRenderEventArgs e)
        {
            Rectangle rect = new Rectangle(Point.Empty, e.Item.Size);
            int gutterWidth = 28; // Standard width for icon gutter

            if (e.Item.Selected || e.Item.Pressed)
            {
                // When selected, highlight the entire row
                using (SolidBrush brush = new SolidBrush(DarkHighlight))
                {
                    e.Graphics.FillRectangle(brush, rect);
                }
            }
            else
            {
                // Draw the lighter gutter on the left
                Rectangle gutterRect = new Rectangle(0, 0, gutterWidth, rect.Height);
                using (SolidBrush brush = new SolidBrush(DarkImageMargin))
                {
                    e.Graphics.FillRectangle(brush, gutterRect);
                }

                // Draw the main background on the right
                Rectangle mainRect = new Rectangle(gutterWidth, 0, rect.Width - gutterWidth, rect.Height);
                using (SolidBrush brush = new SolidBrush(DarkBackground))
                {
                    e.Graphics.FillRectangle(brush, mainRect);
                }
            }
        }

        protected override void OnRenderSeparator(ToolStripSeparatorRenderEventArgs e)
        {
            int y = e.Item.Height / 2;
            using (Pen pen = new Pen(DarkSeparator))
            {
                e.Graphics.DrawLine(pen, 0, y, e.Item.Width, y);
            }
        }

        protected override void OnRenderItemText(ToolStripItemTextRenderEventArgs e)
        {
            e.TextColor = e.Item.Enabled ? DarkText : Color.FromArgb(128, 128, 128);
            base.OnRenderItemText(e);
        }

        protected override void OnRenderImageMargin(ToolStripRenderEventArgs e)
        {
            // Fill the image margin (left side) with a slightly lighter shade for visual distinction
            using (SolidBrush brush = new SolidBrush(DarkImageMargin))
            {
                e.Graphics.FillRectangle(brush, e.AffectedBounds);
            }
        }

        protected override void OnRenderArrow(ToolStripArrowRenderEventArgs e)
        {
            e.ArrowColor = DarkText;
            base.OnRenderArrow(e);
        }

        protected override void OnRenderItemCheck(ToolStripItemImageRenderEventArgs e)
        {
            Rectangle rect = new Rectangle(e.ImageRectangle.Left - 2, e.ImageRectangle.Top - 2,
                                           e.ImageRectangle.Width + 4, e.ImageRectangle.Height + 4);
            using (SolidBrush brush = new SolidBrush(DarkHighlight))
            {
                e.Graphics.FillRectangle(brush, rect);
            }
            base.OnRenderItemCheck(e);
        }
    }

    /// <summary>
    /// Custom color table for dark mode menus
    /// </summary>
    public class DarkModeColorTable : ProfessionalColorTable
    {
        private static readonly Color DarkBackground = Color.FromArgb(45, 45, 48);
        private static readonly Color DarkImageMargin = Color.FromArgb(55, 55, 58); // Slightly lighter for left margin
        private static readonly Color DarkBorder = Color.FromArgb(60, 60, 60);
        private static readonly Color DarkHighlight = Color.FromArgb(62, 62, 66);

        public override Color MenuItemSelected => DarkHighlight;
        public override Color MenuItemSelectedGradientBegin => DarkHighlight;
        public override Color MenuItemSelectedGradientEnd => DarkHighlight;
        public override Color MenuBorder => DarkBorder;
        public override Color MenuItemBorder => DarkBorder;
        public override Color ToolStripDropDownBackground => DarkBackground;
        public override Color ImageMarginGradientBegin => DarkImageMargin;
        public override Color ImageMarginGradientMiddle => DarkImageMargin;
        public override Color ImageMarginGradientEnd => DarkImageMargin;
        public override Color SeparatorDark => Color.FromArgb(80, 80, 80);
        public override Color SeparatorLight => Color.FromArgb(80, 80, 80);
        public override Color MenuStripGradientBegin => DarkBackground;
        public override Color MenuStripGradientEnd => DarkBackground;
        public override Color MenuItemPressedGradientBegin => DarkHighlight;
        public override Color MenuItemPressedGradientEnd => DarkHighlight;
    }
}
