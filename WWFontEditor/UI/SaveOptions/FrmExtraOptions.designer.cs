﻿namespace Nyerguds.Util.Ui.SaveOptions
{
    partial class FrmExtraOptions
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
            this.pnlOptions = new System.Windows.Forms.Panel();
            this.lstOptions = new Nyerguds.Util.Ui.SaveOptions.SaveOptionsList();
            this.buttonCancel = new System.Windows.Forms.Button();
            this.buttonOK = new System.Windows.Forms.Button();
            this.pnlOptions.SuspendLayout();
            this.SuspendLayout();
            // 
            // pnlOptions
            // 
            this.pnlOptions.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.pnlOptions.AutoScroll = true;
            this.pnlOptions.Controls.Add(this.lstOptions);
            this.pnlOptions.Location = new System.Drawing.Point(13, 13);
            this.pnlOptions.Name = "pnlOptions";
            this.pnlOptions.Size = new System.Drawing.Size(359, 205);
            this.pnlOptions.TabIndex = 1;
            // 
            // lstOptions
            // 
            this.lstOptions.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.lstOptions.Location = new System.Drawing.Point(0, 0);
            this.lstOptions.Name = "lstOptions";
            this.lstOptions.Size = new System.Drawing.Size(359, 119);
            this.lstOptions.TabIndex = 2;
            // 
            // buttonCancel
            // 
            this.buttonCancel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.buttonCancel.CausesValidation = false;
            this.buttonCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.buttonCancel.Location = new System.Drawing.Point(297, 226);
            this.buttonCancel.Name = "buttonCancel";
            this.buttonCancel.Size = new System.Drawing.Size(75, 23);
            this.buttonCancel.TabIndex = 1;
            this.buttonCancel.Text = "Cancel";
            // 
            // buttonOK
            // 
            this.buttonOK.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.buttonOK.DialogResult = System.Windows.Forms.DialogResult.OK;
            this.buttonOK.Location = new System.Drawing.Point(209, 226);
            this.buttonOK.Name = "buttonOK";
            this.buttonOK.Size = new System.Drawing.Size(75, 23);
            this.buttonOK.TabIndex = 0;
            this.buttonOK.Text = "OK";
            // 
            // FrmExtraOptions
            // 
            this.AcceptButton = this.buttonOK;
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.CancelButton = this.buttonCancel;
            this.ClientSize = new System.Drawing.Size(384, 262);
            this.Controls.Add(this.buttonCancel);
            this.Controls.Add(this.buttonOK);
            this.Controls.Add(this.pnlOptions);
            this.Icon = global::WWFontEditor.Properties.Resources.wwfont;
            this.MinimumSize = new System.Drawing.Size(400, 300);
            this.Name = "FrmExtraOptions";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "FrmExtraOptions";
            this.pnlOptions.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion

        private Nyerguds.Util.Ui.SaveOptions.SaveOptionsList lstOptions;
        private System.Windows.Forms.Panel pnlOptions;
        protected System.Windows.Forms.Button buttonCancel;
        protected System.Windows.Forms.Button buttonOK;
    }
}