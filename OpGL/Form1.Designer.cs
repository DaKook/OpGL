﻿namespace OpGL
{
    partial class Form1
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
            this.glControl = new OpenGL.GlControl();
            this.timer1 = new System.Windows.Forms.Timer(this.components);
            this.SuspendLayout();
            // 
            // glControl
            // 
            this.glControl.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(64)))), ((int)(((byte)(64)))), ((int)(((byte)(64)))));
            this.glControl.ColorBits = ((uint)(24u));
            this.glControl.DepthBits = ((uint)(0u));
            this.glControl.Dock = System.Windows.Forms.DockStyle.Fill;
            this.glControl.Location = new System.Drawing.Point(0, 0);
            this.glControl.MultisampleBits = ((uint)(0u));
            this.glControl.Name = "glControl";
            this.glControl.Size = new System.Drawing.Size(640, 480);
            this.glControl.StencilBits = ((uint)(0u));
            this.glControl.TabIndex = 0;
            this.glControl.MouseClick += new System.Windows.Forms.MouseEventHandler(this.GlControl_MouseClick);
            this.glControl.MouseLeave += new System.EventHandler(this.GlControl_MouseLeave);
            this.glControl.MouseMove += new System.Windows.Forms.MouseEventHandler(this.GlControl_MouseMove);
            this.glControl.PreviewKeyDown += new System.Windows.Forms.PreviewKeyDownEventHandler(this.GlControl_PreviewKeyDown);
            // 
            // timer1
            // 
            this.timer1.Enabled = true;
            this.timer1.Interval = 1;
            this.timer1.Tick += new System.EventHandler(this.timer1_Tick);
            // 
            // Form1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(640, 480);
            this.Controls.Add(this.glControl);
            this.KeyPreview = true;
            this.Name = "Form1";
            this.Text = "VVVVVVV";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.Form1_FormClosing);
            this.Load += new System.EventHandler(this.Form1_Load);
            this.KeyDown += new System.Windows.Forms.KeyEventHandler(this.Form1_KeyDown);
            this.ResumeLayout(false);

        }

        #endregion

        private OpenGL.GlControl glControl;
        private System.Windows.Forms.Timer timer1;
    }
}

