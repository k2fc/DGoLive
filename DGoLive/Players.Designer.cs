namespace DGoLive
{
    partial class PlayersForm
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
            this.btnAddClip = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // btnAddClip
            // 
            this.btnAddClip.Location = new System.Drawing.Point(20, 6);
            this.btnAddClip.Name = "btnAddClip";
            this.btnAddClip.Size = new System.Drawing.Size(165, 23);
            this.btnAddClip.TabIndex = 1;
            this.btnAddClip.Text = "Add New Clip...";
            this.btnAddClip.UseVisualStyleBackColor = true;
            this.btnAddClip.Click += new System.EventHandler(this.btnAddClip_Click);
            // 
            // PlayersForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(210, 36);
            this.Controls.Add(this.btnAddClip);
            this.Name = "PlayersForm";
            this.Text = "Players";
            this.ResumeLayout(false);
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.Form_Closing);
        }

        #endregion
        private System.Windows.Forms.Button btnAddClip;
    }
}