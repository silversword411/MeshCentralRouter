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
        private int centerWidth = 150;
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
            this.themeButton = new System.Windows.Forms.Button();
            this.statusBarTogglePanel = new System.Windows.Forms.Panel();
            this.statusBarLabel = new System.Windows.Forms.Label();
            this.statusBarToggleSwitch = new MeshCentralRouter.ToggleSwitch();
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
            this.topPanel = new System.Windows.Forms.Panel();
            this.chatButton = new System.Windows.Forms.Button();
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
            this.cadButton = new System.Windows.Forms.Button();
            this.connectButton = new System.Windows.Forms.Button();
            this.consoleMessage = new System.Windows.Forms.Label();
            this.consoleTimer = new System.Windows.Forms.Timer(this.components);
            this.mainToolTip = new System.Windows.Forms.ToolTip(this.components);
            this.displaySelectorImageList = new System.Windows.Forms.ImageList(this.components);
            this.resizeKvmControl = new MeshCentralRouter.KVMResizeControl();
            this.titleBarPanel.SuspendLayout();
            this.statusBarTogglePanel.SuspendLayout();
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
            this.titleBarPanel.Controls.Add(this.statusBarTogglePanel);
            this.titleBarPanel.Controls.Add(this.titleLabel);
            this.titleBarPanel.Dock = System.Windows.Forms.DockStyle.Top;
            this.titleBarPanel.Location = new System.Drawing.Point(0, 0);
            this.titleBarPanel.Name = "titleBarPanel";
            this.titleBarPanel.Size = new System.Drawing.Size(800, 32);
            this.titleBarPanel.TabIndex = 0;
            this.titleBarPanel.MouseDown += new System.Windows.Forms.MouseEventHandler(this.titleBarPanel_MouseDown);
            this.titleBarPanel.MouseMove += new System.Windows.Forms.MouseEventHandler(this.titleBarPanel_MouseMove);
            this.titleBarPanel.MouseUp += new System.Windows.Forms.MouseEventHandler(this.titleBarPanel_MouseUp);
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
            this.themeButton.UseVisualStyleBackColor = true;
            this.themeButton.Click += new System.EventHandler(this.themeButton_Click);
            //
            // gearButton
            //
            this.gearButton.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.gearButton.FlatAppearance.BorderSize = 0;
            this.gearButton.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.gearButton.Location = new System.Drawing.Point(100, -2);
            this.gearButton.Name = "gearButton";
            this.gearButton.Size = new System.Drawing.Size(28, 24);
            this.gearButton.TabIndex = 5;
            this.gearButton.Image = global::MeshCentralRouter.Properties.Resources.Gear20;
            this.gearButton.ImageAlign = System.Drawing.ContentAlignment.MiddleCenter;
            this.gearButton.Text = "";
            this.gearButton.UseVisualStyleBackColor = true;
            this.gearButton.Click += new System.EventHandler(this.gearButton_Click);
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
            // statusBarTogglePanel
            //
            this.statusBarTogglePanel.Anchor = System.Windows.Forms.AnchorStyles.Top;
            this.statusBarTogglePanel.Controls.Add(this.gearButton);
            this.statusBarTogglePanel.Controls.Add(this.statusBarLabel);
            this.statusBarTogglePanel.Controls.Add(this.statusBarToggleSwitch);
            this.statusBarTogglePanel.Location = new System.Drawing.Point(345, 6);
            this.statusBarTogglePanel.Name = "statusBarTogglePanel";
            this.statusBarTogglePanel.Size = new System.Drawing.Size(130, 22);
            this.statusBarTogglePanel.TabIndex = 4;
            //
            // statusBarToggleSwitch
            //
            this.statusBarToggleSwitch.Checked = false;
            this.statusBarToggleSwitch.Location = new System.Drawing.Point(0, 0);
            this.statusBarToggleSwitch.Name = "statusBarToggleSwitch";
            this.statusBarToggleSwitch.OffColor = System.Drawing.Color.LightGray;
            this.statusBarToggleSwitch.OnColor = System.Drawing.Color.FromArgb(((int)(((byte)(76)))), ((int)(((byte)(175)))), ((int)(((byte)(80)))));
            this.statusBarToggleSwitch.Size = new System.Drawing.Size(26, 15);
            this.statusBarToggleSwitch.TabIndex = 0;
            this.statusBarToggleSwitch.CheckedChanged += new System.EventHandler(this.statusBarToggleSwitch_CheckedChanged);
            //
            // statusBarLabel
            //
            this.statusBarLabel.AutoSize = true;
            this.statusBarLabel.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.statusBarLabel.Location = new System.Drawing.Point(35, 1);
            this.statusBarLabel.Name = "statusBarLabel";
            this.statusBarLabel.Size = new System.Drawing.Size(59, 15);
            this.statusBarLabel.TabIndex = 1;
            this.statusBarLabel.Text = "Statusbar";
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
            // topPanel
            // 
            this.topPanel.BackColor = System.Drawing.SystemColors.Control;
            this.topPanel.Controls.Add(this.chatButton);
            this.topPanel.Controls.Add(this.openRemoteFilesButton);
            this.topPanel.Controls.Add(this.extraButtonsPanel);
            this.topPanel.Controls.Add(this.splitButton);
            this.topPanel.Controls.Add(this.clipOutboundButton);
            this.topPanel.Controls.Add(this.clipInboundButton);
            this.topPanel.Controls.Add(this.cadButton);
            this.topPanel.Controls.Add(this.connectButton);
            resources.ApplyResources(this.topPanel, "topPanel");
            this.topPanel.Name = "topPanel";
            // 
            // chatButton
            // 
            this.chatButton.ContextMenuStrip = this.consentContextMenuStrip;
            resources.ApplyResources(this.chatButton, "chatButton");
            this.chatButton.Name = "chatButton";
            this.chatButton.TabStop = false;
            this.chatButton.UseVisualStyleBackColor = true;
            this.chatButton.Click += new System.EventHandler(this.chatButton_Click);
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
            // openRemoteFilesButton
            // 
            resources.ApplyResources(this.openRemoteFilesButton, "openRemoteFilesButton");
            this.openRemoteFilesButton.Name = "openRemoteFilesButton";
            this.openRemoteFilesButton.TabStop = false;
            this.openRemoteFilesButton.UseVisualStyleBackColor = true;
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
            this.zoomButton.UseVisualStyleBackColor = true;
            this.zoomButton.Click += new System.EventHandler(this.zoomButton_Click);
            // 
            // cadButton
            // 
            resources.ApplyResources(this.cadButton, "cadButton");
            this.cadButton.Name = "cadButton";
            this.cadButton.TabStop = false;
            this.cadButton.UseVisualStyleBackColor = true;
            this.cadButton.Click += new System.EventHandler(this.sendCtrlAltDelToolStripMenuItem_Click);
            // 
            // connectButton
            // 
            this.connectButton.ContextMenuStrip = this.consentContextMenuStrip;
            resources.ApplyResources(this.connectButton, "connectButton");
            this.connectButton.Name = "connectButton";
            this.connectButton.TabStop = false;
            this.connectButton.UseVisualStyleBackColor = true;
            this.connectButton.Click += new System.EventHandler(this.MenuItemDisconnect_Click);
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
            this.statusBarTogglePanel.ResumeLayout(false);
            this.statusBarTogglePanel.PerformLayout();
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
        private Panel dropdownPane;
        private Panel dropdownPaneContent;
        private Button settingsPaneSettingsButton;
        private Button settingsPaneStatsButton;
        private Label dropdownPaneLabel;
        private Panel statusBarTogglePanel;
        private ToggleSwitch statusBarToggleSwitch;
        private Label statusBarLabel;
        private Button minimizeButton;
        private Button maximizeButton;
        private Button closeButton;
        private TransparentStatusStrip mainStatusStrip;
        private ToolStripStatusLabel mainToolStripStatusLabel;
        private Timer updateTimer;
        private KVMResizeControl resizeKvmControl;
        private ToolStripStatusLabel toolStripStatusLabel1;
        private Panel topPanel;
        private Button connectButton;
        private Button cadButton;
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
    }
}

