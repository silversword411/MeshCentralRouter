using System;
using System.Drawing;
using System.Windows.Forms;

namespace MeshCentralRouter
{

    public class BlankPanel : System.Windows.Forms.Panel
    {
        public BlankPanel()
        {

        }

        protected override void OnPaintBackground(PaintEventArgs e)
        {
            // Do not paint background.
        }
    }

    /// <summary>
    /// Custom title bar panel that draws a centered shaded area with angled edges
    /// Creates a visual effect like:  left side / center area \ right side
    /// </summary>
    public class TitleBarWithCenterPanel : System.Windows.Forms.Panel
    {
        private System.Drawing.Color centerColor = System.Drawing.Color.FromArgb(40, 40, 40);
        private int centerWidth = 230;
        private int angleWidth = 20;

        public System.Drawing.Color CenterColor
        {
            get { return centerColor; }
            set { centerColor = value; Invalidate(); }
        }

        public int CenterWidth
        {
            get { return centerWidth; }
            set { centerWidth = value; Invalidate(); }
        }

        public int AngleWidth
        {
            get { return angleWidth; }
            set { angleWidth = value; Invalidate(); }
        }

        public TitleBarWithCenterPanel()
        {
            this.DoubleBuffered = true;
            this.SetStyle(System.Windows.Forms.ControlStyles.AllPaintingInWmPaint |
                         System.Windows.Forms.ControlStyles.UserPaint |
                         System.Windows.Forms.ControlStyles.OptimizedDoubleBuffer, true);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);

            // Calculate center position
            int centerX = this.Width / 2;
            int halfWidth = centerWidth / 2;

            // Define the trapezoid points for the center area
            // Top is narrower, bottom is wider (inverted trapezoid)
            System.Drawing.Point[] points = new System.Drawing.Point[]
            {
                new System.Drawing.Point(centerX - halfWidth, 0),                         // Top left
                new System.Drawing.Point(centerX + halfWidth, 0),                         // Top right
                new System.Drawing.Point(centerX + halfWidth + angleWidth, this.Height),  // Bottom right
                new System.Drawing.Point(centerX - halfWidth - angleWidth, this.Height)   // Bottom left
            };

            using (System.Drawing.Drawing2D.GraphicsPath path = new System.Drawing.Drawing2D.GraphicsPath())
            {
                path.AddPolygon(points);

                e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

                using (System.Drawing.SolidBrush brush = new System.Drawing.SolidBrush(centerColor))
                {
                    e.Graphics.FillPath(brush, path);
                }
            }
        }

        protected override void OnResize(System.EventArgs e)
        {
            base.OnResize(e);
            Invalidate();
        }
    }

    /// <summary>
    /// A button with a rounded rectangle (pill-shaped) background
    /// </summary>
    public class RoundedButton : System.Windows.Forms.Button
    {
        private System.Drawing.Color fillColor = System.Drawing.Color.FromArgb(65, 65, 65);
        private int cornerRadius = 12;

        public System.Drawing.Color FillColor
        {
            get { return fillColor; }
            set { fillColor = value; Invalidate(); }
        }

        public int CornerRadius
        {
            get { return cornerRadius; }
            set { cornerRadius = value; Invalidate(); }
        }

        public RoundedButton()
        {
            this.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.FlatAppearance.BorderSize = 0;
            this.SetStyle(System.Windows.Forms.ControlStyles.AllPaintingInWmPaint |
                         System.Windows.Forms.ControlStyles.UserPaint |
                         System.Windows.Forms.ControlStyles.OptimizedDoubleBuffer, true);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            // Clear background with parent's back color (transparent effect)
            if (this.Parent != null)
            {
                using (System.Drawing.SolidBrush bgBrush = new System.Drawing.SolidBrush(this.Parent.BackColor))
                {
                    e.Graphics.FillRectangle(bgBrush, this.ClientRectangle);
                }
            }

            e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

            // Inset by 1 pixel on all sides to ensure curves are fully visible
            int inset = 1;
            int width = this.Width - (inset * 2);
            int height = this.Height - (inset * 2);

            // Create rounded rectangle path
            using (System.Drawing.Drawing2D.GraphicsPath path = new System.Drawing.Drawing2D.GraphicsPath())
            {
                int diameter = System.Math.Min(cornerRadius * 2, System.Math.Min(width, height));
                System.Drawing.Rectangle arc = new System.Drawing.Rectangle(inset, inset, diameter, diameter);

                // Top left arc
                path.AddArc(arc, 180, 90);
                // Top right arc
                arc.X = inset + width - diameter;
                path.AddArc(arc, 270, 90);
                // Bottom right arc
                arc.Y = inset + height - diameter;
                path.AddArc(arc, 0, 90);
                // Bottom left arc
                arc.X = inset;
                path.AddArc(arc, 90, 90);

                path.CloseFigure();

                // Fill the rounded rectangle
                using (System.Drawing.SolidBrush brush = new System.Drawing.SolidBrush(fillColor))
                {
                    e.Graphics.FillPath(brush, path);
                }
            }

            // Draw the text centered
            System.Windows.Forms.TextRenderer.DrawText(
                e.Graphics,
                this.Text,
                this.Font,
                new System.Drawing.Rectangle(0, 0, this.Width, this.Height),
                this.ForeColor,
                System.Windows.Forms.TextFormatFlags.HorizontalCenter | System.Windows.Forms.TextFormatFlags.VerticalCenter
            );
        }
    }

    /// <summary>
    /// Centralized style definitions and helpers for dropdown pane UI.
    /// All layout constants, theme colors, fonts, and control factory methods
    /// live here so that any visual change only needs to happen in one place.
    /// </summary>
    public class DropdownPaneStyle
    {
        // ── Layout Constants ──────────────────────────────────────
        public const int PaneWidth = 365;
        public const int SectionHeaderHeight = 36;
        public const int ItemHeight = SectionHeaderHeight * 2;  // 72
        public const int SidePadding = 8;
        public const int ButtonSpacing = 4;
        public const int SectionSpacing = 8;
        public const int GroupSpacing = 8;
        public const int PaneHeaderHeight = 28;
        public const int ContentTopPadding = 4;
        public const int StatsRowHeight = 28;
        public const int StatsLabelWidth = 130;

        // ── Fonts ─────────────────────────────────────────────────
        public static readonly Font SectionHeaderFont = new Font("Segoe UI", 10F, FontStyle.Bold);
        public static readonly Font GroupHeaderFont = new Font("Segoe UI Semibold", 9F);
        public static readonly Font ItemFont = new Font("Segoe UI", 9.5F);
        public static readonly Font SmallFont = new Font("Segoe UI", 8F);
        public static readonly Font ZoomButtonFont = new Font("Segoe UI", 10F, FontStyle.Bold);
        public static readonly Font ScalingLabelFont = new Font("Segoe UI", 9F);

        // ── Theme Colors ──────────────────────────────────────────
        public Color PaneBgColor { get; private set; }
        public Color PaneTextColor { get; private set; }
        public Color PaneHoverColor { get; private set; }
        public Color BorderColor { get; private set; }
        public Color SelectedColor { get; private set; }
        public Color SelectedBorderColor { get; private set; }
        public Color LabelColor { get; private set; }

        public DropdownPaneStyle()
        {
            bool dark = ThemeManager.Instance.IsDarkMode;
            PaneBgColor = dark ? Color.FromArgb(45, 45, 45) : Color.FromArgb(250, 250, 250);
            PaneTextColor = dark ? Color.White : Color.Black;
            PaneHoverColor = dark ? Color.FromArgb(60, 60, 60) : Color.FromArgb(230, 230, 230);
            BorderColor = dark ? Color.FromArgb(80, 80, 80) : Color.FromArgb(200, 200, 200);
            SelectedColor = dark ? Color.FromArgb(70, 130, 180) : Color.FromArgb(200, 220, 240);
            SelectedBorderColor = dark ? Color.FromArgb(100, 149, 237) : Color.FromArgb(70, 130, 180);
            LabelColor = dark ? Color.FromArgb(180, 180, 180) : Color.FromArgb(100, 100, 100);
        }

        // ── Helpers ───────────────────────────────────────────────

        /// <summary>
        /// Returns the dynamic pane width for grid-based layouts.
        /// </summary>
        public int CalculateGridPaneWidth(DropdownPaneLayout layout)
        {
            return Math.Max(PaneWidth, layout.ColumnsPerPane * 130
                            + (layout.ColumnsPerPane - 1) * GroupSpacing + 8);
        }

        /// <summary>
        /// Returns the width of a single button unit in a 4-column grid.
        /// </summary>
        public static int ButtonUnitWidth(int paneWidth)
        {
            return (paneWidth - (SidePadding * 2) - (ButtonSpacing * 3)) / 4;
        }

        /// <summary>
        /// Creates a section header label (e.g. "Actions", "Frame Rate").
        /// </summary>
        public Label CreateSectionHeader(string text, int yOffset,
                                         int height = SectionHeaderHeight,
                                         int width = PaneWidth)
        {
            return new Label
            {
                Text = text,
                Font = SectionHeaderFont,
                ForeColor = PaneTextColor,
                Location = new Point(SidePadding, yOffset),
                Size = new Size(width - (SidePadding * 2), height)
            };
        }

        /// <summary>
        /// Creates a group header label (smaller than section header, e.g. "Connection", "Settings").
        /// </summary>
        public Label CreateGroupHeader(string text, int yOffset, int sidePadding = SidePadding)
        {
            return new Label
            {
                Text = text,
                Font = GroupHeaderFont,
                ForeColor = LabelColor,
                BackColor = Color.Transparent,
                Location = new Point(sidePadding, yOffset),
                AutoSize = true
            };
        }

        /// <summary>
        /// Applies standard flat button styling to any Button.
        /// </summary>
        public void ApplyFlatButtonStyle(Button btn, bool isSelected = false)
        {
            btn.FlatStyle = FlatStyle.Flat;
            btn.FlatAppearance.BorderSize = 1;
            btn.FlatAppearance.BorderColor = isSelected ? SelectedBorderColor : BorderColor;
            btn.BackColor = isSelected ? SelectedColor : PaneBgColor;
            btn.ForeColor = PaneTextColor;
            btn.FlatAppearance.MouseOverBackColor = PaneHoverColor;
        }

        /// <summary>
        /// Creates a rounded rectangle region for use with panels/controls.
        /// </summary>
        public static System.Drawing.Region CreateRoundedRegion(int width, int height, int radius = 8)
        {
            System.Drawing.Drawing2D.GraphicsPath path = new System.Drawing.Drawing2D.GraphicsPath();
            int diameter = radius * 2;
            path.AddArc(0, 0, diameter, diameter, 180, 90);
            path.AddArc(width - diameter, 0, diameter, diameter, 270, 90);
            path.AddArc(width - diameter, height - diameter, diameter, diameter, 0, 90);
            path.AddArc(0, height - diameter, diameter, diameter, 90, 90);
            path.CloseFigure();
            return new System.Drawing.Region(path);
        }

        /// <summary>
        /// Creates a rounded rectangle path for drawing borders.
        /// </summary>
        public static System.Drawing.Drawing2D.GraphicsPath CreateRoundedPath(int width, int height, int radius = 8)
        {
            System.Drawing.Drawing2D.GraphicsPath path = new System.Drawing.Drawing2D.GraphicsPath();
            int diameter = radius * 2;
            // Inset by 1 pixel for border drawing
            path.AddArc(1, 1, diameter, diameter, 180, 90);
            path.AddArc(width - diameter - 2, 1, diameter, diameter, 270, 90);
            path.AddArc(width - diameter - 2, height - diameter - 2, diameter, diameter, 0, 90);
            path.AddArc(1, height - diameter - 2, diameter, diameter, 90, 90);
            path.CloseFigure();
            return path;
        }

        /// <summary>
        /// Creates a rounded action button panel with icon and label (Settings pane style).
        /// </summary>
        public Panel CreateActionButton(string icon, string labelText, int width, int height,
            Point location, EventHandler clickHandler, bool isDarkMode)
        {
            Panel itemPanel = new Panel();
            itemPanel.Size = new Size(width, height);
            itemPanel.Location = location;
            itemPanel.BackColor = isDarkMode ? Color.FromArgb(55, 55, 55) : Color.FromArgb(235, 235, 235);
            itemPanel.Cursor = Cursors.Hand;
            itemPanel.Region = CreateRoundedRegion(width, height);

            // Add paint handler for rounded border
            itemPanel.Paint += (s, e) =>
            {
                e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                using (var path = CreateRoundedPath(width, height))
                using (var pen = new Pen(Color.Black, 1))
                {
                    e.Graphics.DrawPath(pen, path);
                }
            };

            // Icon label centered near top
            Label iconLabel = new Label();
            iconLabel.Text = icon;
            iconLabel.Font = new Font("Segoe UI Emoji", 14F);
            iconLabel.ForeColor = isDarkMode ? Color.FromArgb(180, 180, 180) : Color.FromArgb(100, 100, 100);
            iconLabel.BackColor = Color.Transparent;
            iconLabel.Size = new Size(width, 24);
            iconLabel.Location = new Point(0, 10);
            iconLabel.TextAlign = ContentAlignment.MiddleCenter;
            itemPanel.Controls.Add(iconLabel);

            // Text label below icon
            Label textLabel = new Label();
            textLabel.Text = labelText;
            textLabel.Font = new Font("Segoe UI", 8F);
            textLabel.ForeColor = PaneTextColor;
            textLabel.BackColor = Color.Transparent;
            textLabel.Size = new Size(width - 4, 28);
            textLabel.Location = new Point(2, 38);
            textLabel.TextAlign = ContentAlignment.TopCenter;
            itemPanel.Controls.Add(textLabel);

            // Click handlers
            if (clickHandler != null)
            {
                itemPanel.Click += clickHandler;
                iconLabel.Click += clickHandler;
                textLabel.Click += clickHandler;
            }

            return itemPanel;
        }

        /// <summary>
        /// Creates a rounded text-only button panel (Settings pane style, for actions without icons).
        /// </summary>
        public Panel CreateRoundedButton(string text, int width, int height,
            Point location, EventHandler clickHandler, bool isDarkMode,
            Font customFont = null, bool isSelected = false)
        {
            Panel itemPanel = new Panel();
            itemPanel.Size = new Size(width, height);
            itemPanel.Location = location;
            itemPanel.BackColor = isSelected
                ? SelectedColor
                : (isDarkMode ? Color.FromArgb(55, 55, 55) : Color.FromArgb(235, 235, 235));
            itemPanel.Cursor = Cursors.Hand;
            itemPanel.Region = CreateRoundedRegion(width, height);

            // Add paint handler for rounded border
            itemPanel.Paint += (s, e) =>
            {
                e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                using (var path = CreateRoundedPath(width, height))
                using (var pen = new Pen(Color.Black, 1))
                {
                    e.Graphics.DrawPath(pen, path);
                }
            };

            // Text label centered
            Label textLabel = new Label();
            textLabel.Text = text;
            textLabel.Font = customFont ?? SmallFont;
            textLabel.ForeColor = PaneTextColor;
            textLabel.BackColor = Color.Transparent;
            textLabel.Size = new Size(width - 4, height - 4);
            textLabel.Location = new Point(2, 2);
            textLabel.TextAlign = ContentAlignment.MiddleCenter;
            itemPanel.Controls.Add(textLabel);

            // Click handlers
            if (clickHandler != null)
            {
                itemPanel.Click += clickHandler;
                textLabel.Click += clickHandler;
            }

            return itemPanel;
        }

        /// <summary>
        /// Creates a rounded image button panel for display selection buttons.
        /// </summary>
        public Panel CreateRoundedImageButton(int width, int height, Point location,
            ImageList imageList, int imageIndex, EventHandler clickHandler,
            bool isDarkMode, bool isSelected = false, object tag = null)
        {
            Panel itemPanel = new Panel();
            itemPanel.Size = new Size(width, height);
            itemPanel.Location = location;
            itemPanel.BackColor = isSelected
                ? SelectedColor
                : (isDarkMode ? Color.FromArgb(55, 55, 55) : Color.FromArgb(235, 235, 235));
            itemPanel.Cursor = Cursors.Hand;
            itemPanel.Region = CreateRoundedRegion(width, height);
            itemPanel.Tag = tag;

            // Add paint handler for rounded border
            itemPanel.Paint += (s, e) =>
            {
                e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                using (var path = CreateRoundedPath(width, height))
                using (var pen = new Pen(Color.Black, 1))
                {
                    e.Graphics.DrawPath(pen, path);
                }
            };

            // PictureBox for image centered
            PictureBox picBox = new PictureBox();
            picBox.Size = new Size(width - 8, height - 8);
            picBox.Location = new Point(4, 4);
            picBox.SizeMode = PictureBoxSizeMode.CenterImage;
            picBox.BackColor = Color.Transparent;
            if (imageList != null && imageIndex >= 0 && imageIndex < imageList.Images.Count)
            {
                picBox.Image = imageList.Images[imageIndex];
            }
            picBox.Tag = tag;
            itemPanel.Controls.Add(picBox);

            // Click handlers
            if (clickHandler != null)
            {
                itemPanel.Click += clickHandler;
                picBox.Click += clickHandler;
            }

            return itemPanel;
        }

        /// <summary>
        /// Creates a stats row (label + value) and returns the new yOffset.
        /// </summary>
        public int AddStatsRow(Control parent, string labelText, Label valueLabel,
                               int yOffset, int paneWidth = PaneWidth)
        {
            int valueWidth = paneWidth - StatsLabelWidth - 16;

            Label lbl = new Label
            {
                Text = labelText,
                Font = ItemFont,
                ForeColor = LabelColor,
                Location = new Point(SidePadding, yOffset),
                Size = new Size(StatsLabelWidth, StatsRowHeight),
                TextAlign = ContentAlignment.MiddleLeft
            };
            parent.Controls.Add(lbl);

            valueLabel.Font = ItemFont;
            valueLabel.ForeColor = PaneTextColor;
            valueLabel.Location = new Point(StatsLabelWidth + SidePadding, yOffset);
            valueLabel.Size = new Size(valueWidth, StatsRowHeight);
            valueLabel.TextAlign = ContentAlignment.MiddleRight;
            parent.Controls.Add(valueLabel);

            return yOffset + StatsRowHeight + 2;
        }

        /// <summary>
        /// Applies theme colors to the dropdown pane container.
        /// </summary>
        public void ApplyPaneTheme(Panel dropdownPane, Label dropdownPaneLabel,
                                   Panel dropdownPaneContent)
        {
            dropdownPane.BackColor = PaneBgColor;
            dropdownPaneLabel.ForeColor = PaneTextColor;
            dropdownPaneContent.BackColor = PaneBgColor;
        }

        /// <summary>
        /// Sizes, positions, shows, and themes the dropdown pane.
        /// </summary>
        public void FinalizePane(Panel dropdownPane, Label dropdownPaneLabel,
                                 Panel dropdownPaneContent, int contentHeight,
                                 int formWidth, int titleBarBottom,
                                 int paneWidth = PaneWidth)
        {
            dropdownPaneContent.Size = new Size(paneWidth - 2, contentHeight);
            dropdownPane.Size = new Size(paneWidth, PaneHeaderHeight + contentHeight);

            int centerX = (formWidth - dropdownPane.Width) / 2;
            dropdownPane.Location = new Point(centerX, titleBarBottom);

            dropdownPane.Visible = true;
            dropdownPane.BringToFront();

            ApplyPaneTheme(dropdownPane, dropdownPaneLabel, dropdownPaneContent);
        }
    }

    /// <summary>
    /// Specifies the position of an icon relative to the item text
    /// </summary>
    public enum IconPosition
    {
        None,
        Left,
        Right
    }

    /// <summary>
    /// Represents an item in a dropdown section with icon, label, and optional selection state
    /// </summary>
    public class DropdownItem
    {
        public string Icon { get; set; }
        public string Label { get; set; }
        public System.EventHandler ClickHandler { get; set; }
        public bool IsSelected { get; set; }
        public object Tag { get; set; }
        public System.Windows.Forms.Control HelperControl { get; set; }
        public IconPosition IconPosition { get; set; }
        public bool HasHelperTooltip { get; set; }

        public DropdownItem(string label, System.EventHandler clickHandler = null)
        {
            Label = label;
            ClickHandler = clickHandler;
            IsSelected = false;
            IconPosition = IconPosition.Left;
            HasHelperTooltip = false;
        }

        public DropdownItem(string icon, string label, System.EventHandler clickHandler = null)
        {
            Icon = icon;
            Label = label;
            ClickHandler = clickHandler;
            IsSelected = false;
            IconPosition = IconPosition.Left;
            HasHelperTooltip = false;
        }
    }

    /// <summary>
    /// Represents a section in a dropdown pane containing a header and multiple items
    /// </summary>
    public class DropdownSection
    {
        public string Title { get; set; }
        public string InfoIcon { get; set; }
        public System.Collections.Generic.List<DropdownItem> Items { get; set; }

        public DropdownSection(string title)
        {
            Title = title;
            Items = new System.Collections.Generic.List<DropdownItem>();
        }

        public DropdownSection(string title, string infoIcon)
        {
            Title = title;
            InfoIcon = infoIcon;
            Items = new System.Collections.Generic.List<DropdownItem>();
        }

        public DropdownSection AddItem(DropdownItem item)
        {
            Items.Add(item);
            return this;
        }

        public DropdownSection AddItem(string label, System.EventHandler clickHandler = null)
        {
            Items.Add(new DropdownItem(label, clickHandler));
            return this;
        }

        public DropdownSection AddItem(string icon, string label, System.EventHandler clickHandler = null)
        {
            Items.Add(new DropdownItem(icon, label, clickHandler));
            return this;
        }
    }

    /// <summary>
    /// Defines the grid-based layout configuration for a dropdown pane
    /// </summary>
    public class DropdownPaneLayout
    {
        public int ColumnsPerPane { get; set; }
        public int RowsPerPane { get; set; }
        public int ItemsPerGroupRow { get; set; }

        public DropdownPaneLayout(int columnsPerPane, int rowsPerPane, int itemsPerGroupRow)
        {
            ColumnsPerPane = columnsPerPane;
            RowsPerPane = rowsPerPane;
            ItemsPerGroupRow = itemsPerGroupRow;
        }

        /// <summary>
        /// Calculates the width of a single group based on the pane width
        /// </summary>
        public int CalculateGroupWidth(int paneWidth)
        {
            int totalSpacing = (ColumnsPerPane - 1) * 8; // 8px spacing between groups
            int availableWidth = paneWidth - totalSpacing - 8; // 4px padding on each side
            return availableWidth / ColumnsPerPane;
        }

        /// <summary>
        /// Calculates the position of a group in the grid
        /// </summary>
        public System.Drawing.Point CalculateGroupPosition(int groupIndex, int groupWidth)
        {
            int col = groupIndex % ColumnsPerPane;
            int row = groupIndex / ColumnsPerPane;
            int x = 4 + col * (groupWidth + 8); // 4px left padding + column offset
            int y = 28 + row * 0; // Will be calculated based on actual group heights
            return new System.Drawing.Point(x, y);
        }
    }

    /// <summary>
    /// Represents a group of sections in a grid-based dropdown layout
    /// </summary>
    public class DropdownGroup
    {
        public System.Collections.Generic.List<DropdownSection> Sections { get; set; }
        public int Row { get; set; }
        public int Column { get; set; }

        public DropdownGroup()
        {
            Sections = new System.Collections.Generic.List<DropdownSection>();
        }

        public DropdownGroup(params DropdownSection[] sections)
        {
            Sections = new System.Collections.Generic.List<DropdownSection>(sections);
        }

        public DropdownGroup AddSection(DropdownSection section)
        {
            Sections.Add(section);
            return this;
        }
    }

    partial class KVMViewer
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(KVMViewer));
            this.titleBarPanel = new MeshCentralRouter.TitleBarWithCenterPanel();
            this.closeButton = new System.Windows.Forms.Button();
            this.maximizeButton = new System.Windows.Forms.Button();
            this.minimizeButton = new System.Windows.Forms.Button();
            this.gearButton = new System.Windows.Forms.Button();
            this.displayButton = new System.Windows.Forms.Button();
            this.otherButton = new System.Windows.Forms.Button();
            this.infoButton = new System.Windows.Forms.Button();
            this.themeButton = new System.Windows.Forms.Button();
            this.paneStatusBarToggleSwitch = new MeshCentralRouter.ToggleSwitch();
            this.paneAutoReconnectToggleSwitch = new MeshCentralRouter.ToggleSwitch();
            this.paneSwapMouseToggleSwitch = new MeshCentralRouter.ToggleSwitch();
            this.paneRemoteKeyMapToggleSwitch = new MeshCentralRouter.ToggleSwitch();
            this.paneSyncClipboardToggleSwitch = new MeshCentralRouter.ToggleSwitch();
            this.titleLabel = new System.Windows.Forms.Label();
            this.dropdownPane = new System.Windows.Forms.Panel();
            this.dropdownPaneContent = new System.Windows.Forms.Panel();
            this.settingsPaneSettingsButton = new System.Windows.Forms.Button();
            this.settingsPaneStatsButton = new System.Windows.Forms.Button();
            this.dropdownPaneLabel = new System.Windows.Forms.Label();
            this.mainStatusStrip = new MeshCentralRouter.TransparentStatusStrip();
            this.mainToolStripStatusLabel = new System.Windows.Forms.ToolStripStatusLabel();
            this.toolStripStatusLabel1 = new System.Windows.Forms.ToolStripStatusLabel();
            this.updateTimer = new System.Windows.Forms.Timer(this.components);
            this.statisticsRefreshTimer = new System.Windows.Forms.Timer(this.components);
            this.topPanel = new System.Windows.Forms.Panel();
            this.chatButton = new System.Windows.Forms.Button();
            this.chatSeparator = new System.Windows.Forms.Panel();
            this.consentContextMenuStrip = new System.Windows.Forms.ContextMenuStrip(this.components);
            this.askConsentBarToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.askConsentToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.privacyBarToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.openRemoteFilesButton = new System.Windows.Forms.Button();
            this.extraButtonsPanel = new System.Windows.Forms.Panel();
            this.splitButton = new System.Windows.Forms.Button();
            this.clipOutboundButton = new System.Windows.Forms.Button();
            this.clipInboundButton = new System.Windows.Forms.Button();
            this.statsButton = new System.Windows.Forms.Button();
            this.settingsButton = new System.Windows.Forms.Button();
            this.zoomButton = new System.Windows.Forms.Button();
            this.cadButton = new MeshCentralRouter.RoundedButton();
            this.connectButton = new MeshCentralRouter.RoundedButton();
            this.consoleMessage = new System.Windows.Forms.Label();
            this.consoleTimer = new System.Windows.Forms.Timer(this.components);
            this.mainToolTip = new System.Windows.Forms.ToolTip(this.components);
            this.displaySelectorImageList = new System.Windows.Forms.ImageList(this.components);
            this.resizeKvmControl = new MeshCentralRouter.KVMResizeControl();
            this.titleBarPanel.SuspendLayout();
            this.mainStatusStrip.SuspendLayout();
            this.topPanel.SuspendLayout();
            this.consentContextMenuStrip.SuspendLayout();
            this.SuspendLayout();
            //
            // titleBarPanel
            //
            this.titleBarPanel.BackColor = System.Drawing.SystemColors.Control;
            this.titleBarPanel.Controls.Add(this.closeButton);
            this.titleBarPanel.Controls.Add(this.maximizeButton);
            this.titleBarPanel.Controls.Add(this.minimizeButton);
            this.titleBarPanel.Controls.Add(this.themeButton);
            this.titleBarPanel.Controls.Add(this.zoomButton);
            this.titleBarPanel.Controls.Add(this.chatSeparator);
            this.titleBarPanel.Controls.Add(this.chatButton);
            this.titleBarPanel.Controls.Add(this.openRemoteFilesButton);
            this.titleBarPanel.Controls.Add(this.infoButton);
            this.titleBarPanel.Controls.Add(this.gearButton);
            this.titleBarPanel.Controls.Add(this.displayButton);
            this.titleBarPanel.Controls.Add(this.otherButton);
            this.titleBarPanel.Controls.Add(this.titleLabel);
            this.titleBarPanel.Dock = System.Windows.Forms.DockStyle.Top;
            this.titleBarPanel.Location = new System.Drawing.Point(0, 0);
            this.titleBarPanel.Name = "titleBarPanel";
            this.titleBarPanel.Size = new System.Drawing.Size(800, 32);
            this.titleBarPanel.TabIndex = 0;
            this.titleBarPanel.MouseDown += new System.Windows.Forms.MouseEventHandler(this.titleBarPanel_MouseDown);
            this.titleBarPanel.MouseMove += new System.Windows.Forms.MouseEventHandler(this.titleBarPanel_MouseMove);
            this.titleBarPanel.MouseUp += new System.Windows.Forms.MouseEventHandler(this.titleBarPanel_MouseUp);
            this.titleBarPanel.DoubleClick += new System.EventHandler(this.titleBarPanel_DoubleClick);
            //
            // closeButton
            //
            this.closeButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.closeButton.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.closeButton.FlatAppearance.BorderSize = 0;
            this.closeButton.Font = new System.Drawing.Font("Segoe UI", 10F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.closeButton.Location = new System.Drawing.Point(768, 2);
            this.closeButton.Name = "closeButton";
            this.closeButton.Size = new System.Drawing.Size(32, 30);
            this.closeButton.TabIndex = 3;
            this.closeButton.Text = "✕";
            this.closeButton.UseVisualStyleBackColor = true;
            this.closeButton.Click += new System.EventHandler(this.closeButton_Click);

            //
            // maximizeButton
            //
            this.maximizeButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.maximizeButton.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.maximizeButton.FlatAppearance.BorderSize = 0;
            this.maximizeButton.Font = new System.Drawing.Font("Segoe UI", 10F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.maximizeButton.Location = new System.Drawing.Point(736, 2);
            this.maximizeButton.Name = "maximizeButton";
            this.maximizeButton.Size = new System.Drawing.Size(32, 30);
            this.maximizeButton.TabIndex = 4;
            this.maximizeButton.Text = "⬜";
            this.maximizeButton.UseVisualStyleBackColor = true;
            this.maximizeButton.Click += new System.EventHandler(this.maximizeButton_Click);
            //
            // minimizeButton
            //
            this.minimizeButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.minimizeButton.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.minimizeButton.FlatAppearance.BorderSize = 0;
            this.minimizeButton.Font = new System.Drawing.Font("Segoe UI", 10F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.minimizeButton.Location = new System.Drawing.Point(704, 2);
            this.minimizeButton.Name = "minimizeButton";
            this.minimizeButton.Size = new System.Drawing.Size(32, 30);
            this.minimizeButton.TabIndex = 2;
            this.minimizeButton.Text = "−";
            this.minimizeButton.UseVisualStyleBackColor = true;
            this.minimizeButton.Click += new System.EventHandler(this.minimizeButton_Click);
            //
            // themeButton
            //
            this.themeButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.themeButton.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.themeButton.FlatAppearance.BorderSize = 0;
            this.themeButton.Font = new System.Drawing.Font("Segoe UI", 10F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.themeButton.Location = new System.Drawing.Point(703, 2);
            this.themeButton.Name = "themeButton";
            this.themeButton.Size = new System.Drawing.Size(38, 30);
            this.themeButton.TabIndex = 1;
            this.themeButton.Image = global::MeshCentralRouter.Properties.Resources.MoonDark20;
            this.themeButton.ImageAlign = System.Drawing.ContentAlignment.MiddleCenter;
            this.themeButton.Text = "";
            this.themeButton.UseVisualStyleBackColor = false;
            this.themeButton.Click += new System.EventHandler(this.themeButton_Click);
            //
            // gearButton
            //
            this.gearButton.Anchor = System.Windows.Forms.AnchorStyles.Top;
            this.gearButton.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.gearButton.FlatAppearance.BorderSize = 0;
            this.gearButton.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.gearButton.Location = new System.Drawing.Point(409, 4);
            this.gearButton.Name = "gearButton";
            this.gearButton.Size = new System.Drawing.Size(32, 24);
            this.gearButton.TabIndex = 5;
            this.gearButton.Image = global::MeshCentralRouter.Properties.Resources.Gear20;
            this.gearButton.ImageAlign = System.Drawing.ContentAlignment.MiddleCenter;
            this.gearButton.Text = "";
            this.gearButton.UseVisualStyleBackColor = false;
            this.gearButton.Click += new System.EventHandler(this.gearButton_Click);
            //
            // displayButton
            //
            this.displayButton.Anchor = System.Windows.Forms.AnchorStyles.Top;
            this.displayButton.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.displayButton.FlatAppearance.BorderSize = 0;
            this.displayButton.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.displayButton.Location = new System.Drawing.Point(345, 4);
            this.displayButton.Name = "displayButton";
            this.displayButton.Size = new System.Drawing.Size(32, 24);
            this.displayButton.TabIndex = 12;
            this.displayButton.Image = global::MeshCentralRouter.Properties.Resources.Display20;
            this.displayButton.ImageAlign = System.Drawing.ContentAlignment.MiddleCenter;
            this.displayButton.Text = "";
            this.displayButton.UseVisualStyleBackColor = false;
            this.displayButton.Click += new System.EventHandler(this.displayButton_Click);
            //
            // otherButton
            //
            this.otherButton.Anchor = System.Windows.Forms.AnchorStyles.Top;
            this.otherButton.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.otherButton.FlatAppearance.BorderSize = 0;
            this.otherButton.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.otherButton.Location = new System.Drawing.Point(377, 4);
            this.otherButton.Name = "otherButton";
            this.otherButton.Size = new System.Drawing.Size(32, 24);
            this.otherButton.TabIndex = 13;
            this.otherButton.Image = global::MeshCentralRouter.Properties.Resources.Wrench20;
            this.otherButton.ImageAlign = System.Drawing.ContentAlignment.MiddleCenter;
            this.otherButton.Text = "";
            this.otherButton.UseVisualStyleBackColor = false;
            this.otherButton.Click += new System.EventHandler(this.otherButton_Click);
            //
            // infoButton
            //
            this.infoButton.Anchor = System.Windows.Forms.AnchorStyles.Top;
            this.infoButton.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.infoButton.FlatAppearance.BorderSize = 0;
            this.infoButton.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.infoButton.Location = new System.Drawing.Point(441, 4);
            this.infoButton.Name = "infoButton";
            this.infoButton.Size = new System.Drawing.Size(32, 24);
            this.infoButton.TabIndex = 11;
            this.infoButton.Image = global::MeshCentralRouter.Properties.Resources.Statistics20;
            this.infoButton.ImageAlign = System.Drawing.ContentAlignment.MiddleCenter;
            this.infoButton.Text = "";
            this.infoButton.UseVisualStyleBackColor = false;
            this.infoButton.Click += new System.EventHandler(this.infoButton_Click);
            //
            // connectButton - moved to titlebar as RoundedButton
            //
            this.connectButton.ContextMenuStrip = this.consentContextMenuStrip;
            this.connectButton.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.connectButton.FlatAppearance.BorderSize = 0;
            this.connectButton.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.connectButton.Location = new System.Drawing.Point(6, 3);
            this.connectButton.Name = "connectButton";
            this.connectButton.Size = new System.Drawing.Size(80, 26);
            this.connectButton.TabIndex = 9;
            this.connectButton.TabStop = false;
            this.connectButton.Text = "Connect";
            this.connectButton.UseVisualStyleBackColor = true;
            this.connectButton.Click += new System.EventHandler(this.MenuItemDisconnect_Click);
            //
            // dropdownPane - Container for dropdown panes that appear below the center panel
            //
            this.dropdownPane.Anchor = System.Windows.Forms.AnchorStyles.Top;
            this.dropdownPane.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(45)))), ((int)(((byte)(45)))), ((int)(((byte)(45)))));
            this.dropdownPane.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.dropdownPane.Controls.Add(this.dropdownPaneLabel);
            this.dropdownPane.Controls.Add(this.dropdownPaneContent);
            this.dropdownPane.Location = new System.Drawing.Point(325, 32);
            this.dropdownPane.Name = "dropdownPane";
            this.dropdownPane.Size = new System.Drawing.Size(150, 100);
            this.dropdownPane.TabIndex = 6;
            this.dropdownPane.Visible = false;
            //
            // dropdownPaneLabel
            //
            this.dropdownPaneLabel.AutoSize = true;
            this.dropdownPaneLabel.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.dropdownPaneLabel.ForeColor = System.Drawing.Color.White;
            this.dropdownPaneLabel.Location = new System.Drawing.Point(8, 8);
            this.dropdownPaneLabel.Name = "dropdownPaneLabel";
            this.dropdownPaneLabel.Size = new System.Drawing.Size(52, 15);
            this.dropdownPaneLabel.TabIndex = 0;
            this.dropdownPaneLabel.Text = "Settings";
            //
            // dropdownPaneContent - Content area that holds pane-specific controls
            //
            this.dropdownPaneContent.BackColor = System.Drawing.Color.Transparent;
            this.dropdownPaneContent.Location = new System.Drawing.Point(0, 28);
            this.dropdownPaneContent.Name = "dropdownPaneContent";
            this.dropdownPaneContent.Size = new System.Drawing.Size(148, 70);
            this.dropdownPaneContent.TabIndex = 3;
            //
            // settingsPaneSettingsButton
            //
            this.settingsPaneSettingsButton.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.settingsPaneSettingsButton.FlatAppearance.BorderSize = 0;
            this.settingsPaneSettingsButton.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.settingsPaneSettingsButton.ForeColor = System.Drawing.Color.White;
            this.settingsPaneSettingsButton.Location = new System.Drawing.Point(5, 2);
            this.settingsPaneSettingsButton.Name = "settingsPaneSettingsButton";
            this.settingsPaneSettingsButton.Size = new System.Drawing.Size(140, 28);
            this.settingsPaneSettingsButton.TabIndex = 1;
            this.settingsPaneSettingsButton.Image = global::MeshCentralRouter.Properties.Resources.Gear20;
            this.settingsPaneSettingsButton.ImageAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.settingsPaneSettingsButton.Text = "  Remote Desktop";
            this.settingsPaneSettingsButton.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.settingsPaneSettingsButton.UseVisualStyleBackColor = true;
            this.settingsPaneSettingsButton.Click += new System.EventHandler(this.settingsToolStripMenuItem_Click);
            //
            // settingsPaneStatsButton
            //
            this.settingsPaneStatsButton.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.settingsPaneStatsButton.FlatAppearance.BorderSize = 0;
            this.settingsPaneStatsButton.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.settingsPaneStatsButton.ForeColor = System.Drawing.Color.White;
            this.settingsPaneStatsButton.Location = new System.Drawing.Point(5, 34);
            this.settingsPaneStatsButton.Name = "settingsPaneStatsButton";
            this.settingsPaneStatsButton.Size = new System.Drawing.Size(140, 28);
            this.settingsPaneStatsButton.TabIndex = 2;
            this.settingsPaneStatsButton.Image = global::MeshCentralRouter.Properties.Resources.Statistics20;
            this.settingsPaneStatsButton.ImageAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.settingsPaneStatsButton.Text = "  Stats";
            this.settingsPaneStatsButton.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.settingsPaneStatsButton.UseVisualStyleBackColor = true;
            this.settingsPaneStatsButton.Click += new System.EventHandler(this.statsButton_Click);
            //
            // paneStatusBarToggleSwitch - StatusBar toggle for use in dropdown pane
            //
            this.paneStatusBarToggleSwitch.Checked = false;
            this.paneStatusBarToggleSwitch.Location = new System.Drawing.Point(0, 0);
            this.paneStatusBarToggleSwitch.Name = "paneStatusBarToggleSwitch";
            this.paneStatusBarToggleSwitch.OffColor = System.Drawing.Color.LightGray;
            this.paneStatusBarToggleSwitch.OnColor = System.Drawing.Color.FromArgb(((int)(((byte)(76)))), ((int)(((byte)(175)))), ((int)(((byte)(80)))));
            this.paneStatusBarToggleSwitch.Size = new System.Drawing.Size(40, 20);
            this.paneStatusBarToggleSwitch.TabIndex = 0;
            this.paneStatusBarToggleSwitch.CheckedChanged += new System.EventHandler(this.statusBarToggleSwitch_CheckedChanged);
            //
            // paneAutoReconnectToggleSwitch - Auto Reconnect toggle for use in dropdown pane
            //
            this.paneAutoReconnectToggleSwitch.Checked = false;
            this.paneAutoReconnectToggleSwitch.Location = new System.Drawing.Point(0, 0);
            this.paneAutoReconnectToggleSwitch.Name = "paneAutoReconnectToggleSwitch";
            this.paneAutoReconnectToggleSwitch.OffColor = System.Drawing.Color.LightGray;
            this.paneAutoReconnectToggleSwitch.OnColor = System.Drawing.Color.FromArgb(((int)(((byte)(76)))), ((int)(((byte)(175)))), ((int)(((byte)(80)))));
            this.paneAutoReconnectToggleSwitch.Size = new System.Drawing.Size(40, 20);
            this.paneAutoReconnectToggleSwitch.TabIndex = 0;
            this.paneAutoReconnectToggleSwitch.CheckedChanged += new System.EventHandler(this.autoReconnectToggleSwitch_CheckedChanged);
            //
            // paneSwapMouseToggleSwitch - Swap Mouse toggle for use in dropdown pane
            //
            this.paneSwapMouseToggleSwitch.Checked = false;
            this.paneSwapMouseToggleSwitch.Location = new System.Drawing.Point(0, 0);
            this.paneSwapMouseToggleSwitch.Name = "paneSwapMouseToggleSwitch";
            this.paneSwapMouseToggleSwitch.OffColor = System.Drawing.Color.LightGray;
            this.paneSwapMouseToggleSwitch.OnColor = System.Drawing.Color.FromArgb(((int)(((byte)(76)))), ((int)(((byte)(175)))), ((int)(((byte)(80)))));
            this.paneSwapMouseToggleSwitch.Size = new System.Drawing.Size(40, 20);
            this.paneSwapMouseToggleSwitch.TabIndex = 0;
            this.paneSwapMouseToggleSwitch.CheckedChanged += new System.EventHandler(this.swapMouseToggleSwitch_CheckedChanged);
            //
            // paneRemoteKeyMapToggleSwitch - Remote Key Map toggle for use in dropdown pane
            //
            this.paneRemoteKeyMapToggleSwitch.Checked = false;
            this.paneRemoteKeyMapToggleSwitch.Location = new System.Drawing.Point(0, 0);
            this.paneRemoteKeyMapToggleSwitch.Name = "paneRemoteKeyMapToggleSwitch";
            this.paneRemoteKeyMapToggleSwitch.OffColor = System.Drawing.Color.LightGray;
            this.paneRemoteKeyMapToggleSwitch.OnColor = System.Drawing.Color.FromArgb(((int)(((byte)(76)))), ((int)(((byte)(175)))), ((int)(((byte)(80)))));
            this.paneRemoteKeyMapToggleSwitch.Size = new System.Drawing.Size(40, 20);
            this.paneRemoteKeyMapToggleSwitch.TabIndex = 0;
            this.paneRemoteKeyMapToggleSwitch.CheckedChanged += new System.EventHandler(this.remoteKeyMapToggleSwitch_CheckedChanged);
            //
            // paneSyncClipboardToggleSwitch - Sync Clipboard toggle for use in dropdown pane
            //
            this.paneSyncClipboardToggleSwitch.Checked = false;
            this.paneSyncClipboardToggleSwitch.Location = new System.Drawing.Point(0, 0);
            this.paneSyncClipboardToggleSwitch.Name = "paneSyncClipboardToggleSwitch";
            this.paneSyncClipboardToggleSwitch.OffColor = System.Drawing.Color.LightGray;
            this.paneSyncClipboardToggleSwitch.OnColor = System.Drawing.Color.FromArgb(((int)(((byte)(76)))), ((int)(((byte)(175)))), ((int)(((byte)(80)))));
            this.paneSyncClipboardToggleSwitch.Size = new System.Drawing.Size(40, 20);
            this.paneSyncClipboardToggleSwitch.TabIndex = 0;
            this.paneSyncClipboardToggleSwitch.CheckedChanged += new System.EventHandler(this.syncClipboardToggleSwitch_CheckedChanged);
            //
            // titleLabel
            //
            this.titleLabel.AutoSize = true;
            this.titleLabel.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.titleLabel.Location = new System.Drawing.Point(8, 8);
            this.titleLabel.Name = "titleLabel";
            this.titleLabel.Size = new System.Drawing.Size(111, 15);
            this.titleLabel.TabIndex = 0;
            this.titleLabel.Text = "Remote Desktop";
            this.titleLabel.MouseDown += new System.Windows.Forms.MouseEventHandler(this.titleBarPanel_MouseDown);
            this.titleLabel.MouseMove += new System.Windows.Forms.MouseEventHandler(this.titleBarPanel_MouseMove);
            this.titleLabel.MouseUp += new System.Windows.Forms.MouseEventHandler(this.titleBarPanel_MouseUp);
            //
            // mainStatusStrip
            // 
            this.mainStatusStrip.BackColor = System.Drawing.SystemColors.Menu;
            this.mainStatusStrip.ImageScalingSize = new System.Drawing.Size(20, 20);
            this.mainStatusStrip.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.mainToolStripStatusLabel,
            this.toolStripStatusLabel1});
            resources.ApplyResources(this.mainStatusStrip, "mainStatusStrip");
            this.mainStatusStrip.Name = "mainStatusStrip";
            // 
            // mainToolStripStatusLabel
            // 
            this.mainToolStripStatusLabel.Name = "mainToolStripStatusLabel";
            resources.ApplyResources(this.mainToolStripStatusLabel, "mainToolStripStatusLabel");
            this.mainToolStripStatusLabel.Spring = true;
            // 
            // toolStripStatusLabel1
            // 
            this.toolStripStatusLabel1.Name = "toolStripStatusLabel1";
            resources.ApplyResources(this.toolStripStatusLabel1, "toolStripStatusLabel1");
            //
            // updateTimer
            //
            this.updateTimer.Enabled = true;
            this.updateTimer.Interval = 1000;
            this.updateTimer.Tick += new System.EventHandler(this.updateTimer_Tick);
            //
            // statisticsRefreshTimer
            //
            this.statisticsRefreshTimer.Interval = 500;
            this.statisticsRefreshTimer.Tick += new System.EventHandler(this.statisticsRefreshTimer_Tick);
            //
            // topPanel
            //
            this.topPanel.BackColor = System.Drawing.SystemColors.Control;
            this.topPanel.Controls.Add(this.extraButtonsPanel);
            this.topPanel.Controls.Add(this.splitButton);
            this.topPanel.Controls.Add(this.clipOutboundButton);
            this.topPanel.Controls.Add(this.clipInboundButton);
            resources.ApplyResources(this.topPanel, "topPanel");
            this.topPanel.Name = "topPanel";
            this.topPanel.Visible = false;
            //
            // chatButton - moved to center titlebar icons, to the left of settings (gearButton)
            //
            this.chatButton.Anchor = System.Windows.Forms.AnchorStyles.Top;
            this.chatButton.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.chatButton.FlatAppearance.BorderSize = 0;
            this.chatButton.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.chatButton.Location = new System.Drawing.Point(377, 4);
            this.chatButton.Name = "chatButton";
            this.chatButton.Size = new System.Drawing.Size(32, 24);
            this.chatButton.TabIndex = 6;
            this.chatButton.Image = global::MeshCentralRouter.Properties.Resources.Chat20;
            this.chatButton.ImageAlign = System.Drawing.ContentAlignment.MiddleCenter;
            this.chatButton.Text = "";
            this.chatButton.UseVisualStyleBackColor = false;
            this.chatButton.Click += new System.EventHandler(this.chatButton_Click);
            //
            // chatSeparator - no longer used since chat button moved to center
            //
            this.chatSeparator.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.chatSeparator.BackColor = System.Drawing.Color.Gray;
            this.chatSeparator.Location = new System.Drawing.Point(580, 3);
            this.chatSeparator.Name = "chatSeparator";
            this.chatSeparator.Size = new System.Drawing.Size(1, 26);
            this.chatSeparator.TabIndex = 7;
            this.chatSeparator.Visible = false;
            //
            // consentContextMenuStrip
            // 
            this.consentContextMenuStrip.ImageScalingSize = new System.Drawing.Size(20, 20);
            this.consentContextMenuStrip.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.askConsentBarToolStripMenuItem,
            this.askConsentToolStripMenuItem,
            this.privacyBarToolStripMenuItem});
            this.consentContextMenuStrip.Name = "consentContextMenuStrip";
            resources.ApplyResources(this.consentContextMenuStrip, "consentContextMenuStrip");
            this.consentContextMenuStrip.Opening += new System.ComponentModel.CancelEventHandler(this.consentContextMenuStrip_Opening);
            // 
            // askConsentBarToolStripMenuItem
            // 
            this.askConsentBarToolStripMenuItem.Name = "askConsentBarToolStripMenuItem";
            resources.ApplyResources(this.askConsentBarToolStripMenuItem, "askConsentBarToolStripMenuItem");
            this.askConsentBarToolStripMenuItem.Click += new System.EventHandler(this.askConsentBarToolStripMenuItem_Click);
            // 
            // askConsentToolStripMenuItem
            // 
            this.askConsentToolStripMenuItem.Name = "askConsentToolStripMenuItem";
            resources.ApplyResources(this.askConsentToolStripMenuItem, "askConsentToolStripMenuItem");
            this.askConsentToolStripMenuItem.Click += new System.EventHandler(this.askConsentToolStripMenuItem_Click);
            // 
            // privacyBarToolStripMenuItem
            // 
            this.privacyBarToolStripMenuItem.Name = "privacyBarToolStripMenuItem";
            resources.ApplyResources(this.privacyBarToolStripMenuItem, "privacyBarToolStripMenuItem");
            this.privacyBarToolStripMenuItem.Click += new System.EventHandler(this.privacyBarToolStripMenuItem_Click);
            //
            // openRemoteFilesButton - moved to center titlebar icons, to the left of chat
            //
            this.openRemoteFilesButton.Anchor = System.Windows.Forms.AnchorStyles.Top;
            this.openRemoteFilesButton.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.openRemoteFilesButton.FlatAppearance.BorderSize = 0;
            this.openRemoteFilesButton.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.openRemoteFilesButton.Location = new System.Drawing.Point(377, 4);
            this.openRemoteFilesButton.Name = "openRemoteFilesButton";
            this.openRemoteFilesButton.Size = new System.Drawing.Size(32, 24);
            this.openRemoteFilesButton.TabIndex = 8;
            this.openRemoteFilesButton.Image = global::MeshCentralRouter.Properties.Resources.Files20;
            this.openRemoteFilesButton.ImageAlign = System.Drawing.ContentAlignment.MiddleCenter;
            this.openRemoteFilesButton.Text = "";
            this.openRemoteFilesButton.UseVisualStyleBackColor = false;
            this.openRemoteFilesButton.Click += new System.EventHandler(this.openRemoteFilesButton_Click);
            // 
            // extraButtonsPanel
            // 
            resources.ApplyResources(this.extraButtonsPanel, "extraButtonsPanel");
            this.extraButtonsPanel.Name = "extraButtonsPanel";
            // 
            // splitButton
            // 
            resources.ApplyResources(this.splitButton, "splitButton");
            this.splitButton.Name = "splitButton";
            this.splitButton.TabStop = false;
            this.splitButton.UseVisualStyleBackColor = true;
            this.splitButton.Click += new System.EventHandler(this.splitButton_Click);
            // 
            // clipOutboundButton
            // 
            resources.ApplyResources(this.clipOutboundButton, "clipOutboundButton");
            this.clipOutboundButton.Image = global::MeshCentralRouter.Properties.Resources.iconClipboardOut;
            this.clipOutboundButton.Name = "clipOutboundButton";
            this.clipOutboundButton.TabStop = false;
            this.clipOutboundButton.UseVisualStyleBackColor = true;
            this.clipOutboundButton.Click += new System.EventHandler(this.clipOutboundButton_Click);
            // 
            // clipInboundButton
            // 
            resources.ApplyResources(this.clipInboundButton, "clipInboundButton");
            this.clipInboundButton.Image = global::MeshCentralRouter.Properties.Resources.iconClipboardIn;
            this.clipInboundButton.Name = "clipInboundButton";
            this.clipInboundButton.TabStop = false;
            this.mainToolTip.SetToolTip(this.clipInboundButton, resources.GetString("clipInboundButton.ToolTip"));
            this.clipInboundButton.UseVisualStyleBackColor = true;
            this.clipInboundButton.Click += new System.EventHandler(this.clipInboundButton_Click);
            // 
            // statsButton
            // 
            resources.ApplyResources(this.statsButton, "statsButton");
            this.statsButton.Name = "statsButton";
            this.statsButton.TabStop = false;
            this.statsButton.UseVisualStyleBackColor = true;
            this.statsButton.Click += new System.EventHandler(this.statsButton_Click);
            // 
            // settingsButton
            // 
            resources.ApplyResources(this.settingsButton, "settingsButton");
            this.settingsButton.Name = "settingsButton";
            this.settingsButton.TabStop = false;
            this.settingsButton.UseVisualStyleBackColor = true;
            this.settingsButton.Click += new System.EventHandler(this.settingsToolStripMenuItem_Click);
            // 
            // zoomButton
            // 
            this.zoomButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.zoomButton.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.zoomButton.FlatAppearance.BorderSize = 0;
            this.zoomButton.Font = new System.Drawing.Font("Segoe UI", 10F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.zoomButton.Location = new System.Drawing.Point(665, 2);
            this.zoomButton.Name = "zoomButton";
            this.zoomButton.Size = new System.Drawing.Size(38, 30);
            this.zoomButton.TabIndex = 2;
            this.zoomButton.Image = global::MeshCentralRouter.Properties.Resources.ZoomToFit;
            this.zoomButton.ImageAlign = System.Drawing.ContentAlignment.MiddleCenter;
            this.zoomButton.Text = "";
            this.zoomButton.UseVisualStyleBackColor = false;
            this.zoomButton.Click += new System.EventHandler(this.zoomButton_Click);
            //
            // cadButton - moved to Other dropdown pane
            //
            this.cadButton.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.cadButton.FlatAppearance.BorderSize = 0;
            this.cadButton.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.cadButton.Location = new System.Drawing.Point(0, 0);
            this.cadButton.Name = "cadButton";
            this.cadButton.Size = new System.Drawing.Size(50, 26);
            this.cadButton.TabIndex = 10;
            this.cadButton.TabStop = false;
            this.cadButton.Text = "CAD";
            this.cadButton.Visible = false;
            this.cadButton.UseVisualStyleBackColor = true;
            this.cadButton.Click += new System.EventHandler(this.sendCtrlAltDelToolStripMenuItem_Click);
            //
            // consoleMessage
            // 
            resources.ApplyResources(this.consoleMessage, "consoleMessage");
            this.consoleMessage.ForeColor = System.Drawing.Color.Black;
            this.consoleMessage.Name = "consoleMessage";
            // 
            // consoleTimer
            // 
            this.consoleTimer.Interval = 5000;
            this.consoleTimer.Tick += new System.EventHandler(this.consoleTimer_Tick);
            // 
            // displaySelectorImageList
            // 
            this.displaySelectorImageList.ImageStream = ((System.Windows.Forms.ImageListStreamer)(resources.GetObject("displaySelectorImageList.ImageStream")));
            this.displaySelectorImageList.TransparentColor = System.Drawing.Color.Transparent;
            this.displaySelectorImageList.Images.SetKeyName(0, "icon-monitor1.png");
            this.displaySelectorImageList.Images.SetKeyName(1, "icon-monitor1b.png");
            this.displaySelectorImageList.Images.SetKeyName(2, "icon-monitor2.png");
            this.displaySelectorImageList.Images.SetKeyName(3, "icon-monitor2b.png");
            // 
            // resizeKvmControl
            // 
            this.resizeKvmControl.BackColor = System.Drawing.Color.Gray;
            resources.ApplyResources(this.resizeKvmControl, "resizeKvmControl");
            this.resizeKvmControl.Name = "resizeKvmControl";
            this.resizeKvmControl.ZoomToFit = false;
            this.resizeKvmControl.StateChanged += new System.EventHandler(this.kvmControl_StateChanged);
            this.resizeKvmControl.DisplaysReceived += new System.EventHandler(this.resizeKvmControl_DisplaysReceived);
            this.resizeKvmControl.Enter += new System.EventHandler(this.resizeKvmControl_Enter);
            this.resizeKvmControl.Leave += new System.EventHandler(this.resizeKvmControl_Leave);
            // 
            // KVMViewer
            //
            resources.ApplyResources(this, "$this");
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.Color.Gray;
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.None;
            this.Controls.Add(this.dropdownPane);
            this.Controls.Add(this.consoleMessage);
            this.Controls.Add(this.resizeKvmControl);
            this.Controls.Add(this.topPanel);
            this.Controls.Add(this.mainStatusStrip);
            this.Controls.Add(this.titleBarPanel);
            this.dropdownPane.BringToFront();
            this.Name = "KVMViewer";
            this.Activated += new System.EventHandler(this.KVMViewer_Activated);
            this.Deactivate += new System.EventHandler(this.KVMViewer_Deactivate);
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.Main_FormClosing);
            this.Load += new System.EventHandler(this.MainForm_Load);
            this.Resize += new System.EventHandler(this.MainForm_Resize);
            this.dropdownPane.ResumeLayout(false);
            this.dropdownPane.PerformLayout();
            this.titleBarPanel.ResumeLayout(false);
            this.titleBarPanel.PerformLayout();
            this.mainStatusStrip.ResumeLayout(false);
            this.mainStatusStrip.PerformLayout();
            this.topPanel.ResumeLayout(false);
            this.consentContextMenuStrip.ResumeLayout(false);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion
        private TitleBarWithCenterPanel titleBarPanel;
        private Label titleLabel;
        private Button themeButton;
        private Button gearButton;
        private Button displayButton;
        private Button otherButton;
        private Button infoButton;
        private Panel dropdownPane;
        private Panel dropdownPaneContent;
        private Button settingsPaneSettingsButton;
        private Button settingsPaneStatsButton;
        private Label dropdownPaneLabel;
        private ToggleSwitch paneStatusBarToggleSwitch;
        private ToggleSwitch paneAutoReconnectToggleSwitch;
        private ToggleSwitch paneSwapMouseToggleSwitch;
        private ToggleSwitch paneRemoteKeyMapToggleSwitch;
        private ToggleSwitch paneSyncClipboardToggleSwitch;
        private Button minimizeButton;
        private Button maximizeButton;
        private Button closeButton;
        private TransparentStatusStrip mainStatusStrip;
        private ToolStripStatusLabel mainToolStripStatusLabel;
        private Timer updateTimer;
        private Timer statisticsRefreshTimer;
        private Label statsBytesInValueLabel;
        private Label statsBytesOutValueLabel;
        private Label statsCompInValueLabel;
        private Label statsCompOutValueLabel;
        private Label statsInRatioValueLabel;
        private Label statsOutRatioValueLabel;
        private KVMResizeControl resizeKvmControl;
        private ToolStripStatusLabel toolStripStatusLabel1;
        private Panel topPanel;
        private RoundedButton connectButton;
        private RoundedButton cadButton;
        private Button zoomButton;
        private Button settingsButton;
        private Label consoleMessage;
        private Timer consoleTimer;
        private Button statsButton;
        private Button clipInboundButton;
        private Button clipOutboundButton;
        private ToolTip mainToolTip;
        private ContextMenuStrip consentContextMenuStrip;
        private ToolStripMenuItem askConsentBarToolStripMenuItem;
        private ToolStripMenuItem askConsentToolStripMenuItem;
        private ToolStripMenuItem privacyBarToolStripMenuItem;
        private Button splitButton;
        private Panel extraButtonsPanel;
        private ImageList displaySelectorImageList;
        private Button openRemoteFilesButton;
        private Button chatButton;
        private Panel chatSeparator;
    }
}

