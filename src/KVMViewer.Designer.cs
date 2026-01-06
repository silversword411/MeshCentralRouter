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
            this.titleBarPanel = new System.Windows.Forms.Panel();
            this.closeButton = new System.Windows.Forms.Button();
            this.maximizeButton = new System.Windows.Forms.Button();
            this.minimizeButton = new System.Windows.Forms.Button();
            this.themeButton = new System.Windows.Forms.Button();
            this.statusBarTogglePanel = new System.Windows.Forms.Panel();
            this.statusBarLabel = new System.Windows.Forms.Label();
            this.statusBarToggleSwitch = new MeshCentralRouter.ToggleSwitch();
            this.titleLabel = new System.Windows.Forms.Label();
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
            this.themeButton.Location = new System.Drawing.Point(665, 2);
            this.themeButton.Name = "themeButton";
            this.themeButton.Size = new System.Drawing.Size(38, 30);
            this.themeButton.TabIndex = 1;
            this.themeButton.Text = "🌙";
            this.themeButton.UseVisualStyleBackColor = true;
            this.themeButton.Click += new System.EventHandler(this.themeButton_Click);
            //
            // statusBarTogglePanel
            //
            this.statusBarTogglePanel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.statusBarTogglePanel.Controls.Add(this.statusBarLabel);
            this.statusBarTogglePanel.Controls.Add(this.statusBarToggleSwitch);
            this.statusBarTogglePanel.Location = new System.Drawing.Point(545, 6);
            this.statusBarTogglePanel.Name = "statusBarTogglePanel";
            this.statusBarTogglePanel.Size = new System.Drawing.Size(110, 22);
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
            this.topPanel.Controls.Add(this.statsButton);
            this.topPanel.Controls.Add(this.settingsButton);
            this.topPanel.Controls.Add(this.zoomButton);
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
            resources.ApplyResources(this.zoomButton, "zoomButton");
            this.zoomButton.Image = global::MeshCentralRouter.Properties.Resources.ZoomToFit;
            this.zoomButton.Name = "zoomButton";
            this.zoomButton.TabStop = false;
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
            this.Controls.Add(this.consoleMessage);
            this.Controls.Add(this.resizeKvmControl);
            this.Controls.Add(this.topPanel);
            this.Controls.Add(this.mainStatusStrip);
            this.Controls.Add(this.titleBarPanel);
            this.Name = "KVMViewer";
            this.Activated += new System.EventHandler(this.KVMViewer_Activated);
            this.Deactivate += new System.EventHandler(this.KVMViewer_Deactivate);
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.Main_FormClosing);
            this.Load += new System.EventHandler(this.MainForm_Load);
            this.Resize += new System.EventHandler(this.MainForm_Resize);
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
        private Panel titleBarPanel;
        private Label titleLabel;
        private Button themeButton;
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

