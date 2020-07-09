namespace VboxTextGrab
{
    partial class MainForm
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
            System.Windows.Forms.Button grabButton;
            System.Windows.Forms.Button calibrateButton;
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(MainForm));
            this.timer = new System.Windows.Forms.Timer(this.components);
            this.richTextBox = new System.Windows.Forms.RichTextBox();
            grabButton = new System.Windows.Forms.Button();
            calibrateButton = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // grabButton
            // 
            grabButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            grabButton.Location = new System.Drawing.Point(620, 12);
            grabButton.Name = "grabButton";
            grabButton.Size = new System.Drawing.Size(75, 23);
            grabButton.TabIndex = 3;
            grabButton.Text = "&Grab";
            grabButton.UseVisualStyleBackColor = true;
            grabButton.Click += new System.EventHandler(this.grabButton_Click);
            // 
            // calibrateButton
            // 
            calibrateButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            calibrateButton.Location = new System.Drawing.Point(539, 12);
            calibrateButton.Name = "calibrateButton";
            calibrateButton.Size = new System.Drawing.Size(75, 23);
            calibrateButton.TabIndex = 2;
            calibrateButton.Text = "&Calibrate";
            calibrateButton.UseVisualStyleBackColor = true;
            calibrateButton.Click += new System.EventHandler(this.calibrateButton_Click);
            // 
            // timer
            // 
            this.timer.Interval = 500;
            this.timer.Tick += new System.EventHandler(this.timer_Tick);
            // 
            // richTextBox
            // 
            this.richTextBox.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.richTextBox.Font = new System.Drawing.Font("Courier New", 9.75F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.richTextBox.Location = new System.Drawing.Point(12, 41);
            this.richTextBox.Name = "richTextBox";
            this.richTextBox.ReadOnly = true;
            this.richTextBox.Size = new System.Drawing.Size(683, 453);
            this.richTextBox.TabIndex = 1;
            this.richTextBox.Text = resources.GetString("richTextBox.Text");
            this.richTextBox.WordWrap = false;
            // 
            // MainForm
            // 
            this.AcceptButton = grabButton;
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(707, 506);
            this.Controls.Add(calibrateButton);
            this.Controls.Add(grabButton);
            this.Controls.Add(this.richTextBox);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.Name = "MainForm";
            this.Text = "VBoxTextGrab - (c) 2014, 2015, 2020 Michael Schierl";
            this.SizeChanged += new System.EventHandler(this.MainForm_SizeChanged);
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Timer timer;
        private System.Windows.Forms.RichTextBox richTextBox;
    }
}

