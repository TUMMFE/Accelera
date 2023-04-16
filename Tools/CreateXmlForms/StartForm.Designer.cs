namespace CreateXmlForms {

    partial class StartForm {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing) {
            if (disposing && (components != null)) {
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
            this.SelectAppFileButton = new DevComponents.DotNetBar.ButtonX();
            this.AppFilePathTextBox = new DevComponents.DotNetBar.Controls.TextBoxX();
            this.DownloadFilePathTextBox = new DevComponents.DotNetBar.Controls.TextBoxX();
            this.DownloadPathLabel = new DevComponents.DotNetBar.LabelX();
            this.AppPathLabel = new DevComponents.DotNetBar.LabelX();
            this.SelectDownloadFileButton = new DevComponents.DotNetBar.ButtonX();
            this.StorageHintLabel = new DevComponents.DotNetBar.LabelX();
            this.CreateXMLButton = new DevComponents.DotNetBar.ButtonX();
            this.DescriptionLabel = new DevComponents.DotNetBar.LabelX();
            this.AppVersionTextBox = new DevComponents.DotNetBar.Controls.TextBoxX();
            this.openFileDialog1 = new System.Windows.Forms.OpenFileDialog();
            this.AppVersionLabel = new DevComponents.DotNetBar.LabelX();
            this.MD5Label = new DevComponents.DotNetBar.LabelX();
            this.MD5TextBox = new DevComponents.DotNetBar.Controls.TextBoxX();
            this.UpdateDescriptionTextBox = new DevComponents.DotNetBar.Controls.TextBoxX();
            this.SuspendLayout();
            // 
            // SelectAppFileButton
            // 
            this.SelectAppFileButton.AccessibleRole = System.Windows.Forms.AccessibleRole.PushButton;
            this.SelectAppFileButton.ColorTable = DevComponents.DotNetBar.eButtonColor.OrangeWithBackground;
            this.SelectAppFileButton.Location = new System.Drawing.Point(689, 34);
            this.SelectAppFileButton.Name = "SelectAppFileButton";
            this.SelectAppFileButton.Size = new System.Drawing.Size(37, 20);
            this.SelectAppFileButton.Style = DevComponents.DotNetBar.eDotNetBarStyle.StyleManagerControlled;
            this.SelectAppFileButton.TabIndex = 2;
            this.SelectAppFileButton.Text = "...";
            this.SelectAppFileButton.Click += new System.EventHandler(this.SelectAppFileButton_Click);
            // 
            // AppFilePathTextBox
            // 
            this.AppFilePathTextBox.BackColor = System.Drawing.Color.White;
            // 
            // 
            // 
            this.AppFilePathTextBox.Border.Class = "TextBoxBorder";
            this.AppFilePathTextBox.Border.CornerType = DevComponents.DotNetBar.eCornerType.Square;
            this.AppFilePathTextBox.DisabledBackColor = System.Drawing.Color.White;
            this.AppFilePathTextBox.ForeColor = System.Drawing.Color.Black;
            this.AppFilePathTextBox.Location = new System.Drawing.Point(12, 34);
            this.AppFilePathTextBox.Name = "AppFilePathTextBox";
            this.AppFilePathTextBox.PreventEnterBeep = true;
            this.AppFilePathTextBox.Size = new System.Drawing.Size(659, 20);
            this.AppFilePathTextBox.TabIndex = 3;
            // 
            // DownloadFilePathTextBox
            // 
            this.DownloadFilePathTextBox.BackColor = System.Drawing.Color.White;
            // 
            // 
            // 
            this.DownloadFilePathTextBox.Border.Class = "TextBoxBorder";
            this.DownloadFilePathTextBox.Border.CornerType = DevComponents.DotNetBar.eCornerType.Square;
            this.DownloadFilePathTextBox.DisabledBackColor = System.Drawing.Color.White;
            this.DownloadFilePathTextBox.ForeColor = System.Drawing.Color.Black;
            this.DownloadFilePathTextBox.Location = new System.Drawing.Point(12, 88);
            this.DownloadFilePathTextBox.Name = "DownloadFilePathTextBox";
            this.DownloadFilePathTextBox.PreventEnterBeep = true;
            this.DownloadFilePathTextBox.Size = new System.Drawing.Size(659, 20);
            this.DownloadFilePathTextBox.TabIndex = 4;
            // 
            // DownloadPathLabel
            // 
            // 
            // 
            // 
            this.DownloadPathLabel.BackgroundStyle.CornerType = DevComponents.DotNetBar.eCornerType.Square;
            this.DownloadPathLabel.Location = new System.Drawing.Point(12, 60);
            this.DownloadPathLabel.Name = "DownloadPathLabel";
            this.DownloadPathLabel.Size = new System.Drawing.Size(263, 23);
            this.DownloadPathLabel.TabIndex = 5;
            this.DownloadPathLabel.Text = "Path to the file used for downloading";
            // 
            // AppPathLabel
            // 
            // 
            // 
            // 
            this.AppPathLabel.BackgroundStyle.CornerType = DevComponents.DotNetBar.eCornerType.Square;
            this.AppPathLabel.Location = new System.Drawing.Point(12, 5);
            this.AppPathLabel.Name = "AppPathLabel";
            this.AppPathLabel.Size = new System.Drawing.Size(263, 23);
            this.AppPathLabel.TabIndex = 6;
            this.AppPathLabel.Text = "Path to the application file";
            // 
            // SelectDownloadFileButton
            // 
            this.SelectDownloadFileButton.AccessibleRole = System.Windows.Forms.AccessibleRole.PushButton;
            this.SelectDownloadFileButton.ColorTable = DevComponents.DotNetBar.eButtonColor.OrangeWithBackground;
            this.SelectDownloadFileButton.Location = new System.Drawing.Point(689, 88);
            this.SelectDownloadFileButton.Name = "SelectDownloadFileButton";
            this.SelectDownloadFileButton.Size = new System.Drawing.Size(37, 20);
            this.SelectDownloadFileButton.Style = DevComponents.DotNetBar.eDotNetBarStyle.StyleManagerControlled;
            this.SelectDownloadFileButton.TabIndex = 7;
            this.SelectDownloadFileButton.Text = "...";
            this.SelectDownloadFileButton.Click += new System.EventHandler(this.SelectDownloadFileButton_Click);
            // 
            // StorageHintLabel
            // 
            // 
            // 
            // 
            this.StorageHintLabel.BackgroundStyle.CornerType = DevComponents.DotNetBar.eCornerType.Square;
            this.StorageHintLabel.Location = new System.Drawing.Point(327, 379);
            this.StorageHintLabel.Name = "StorageHintLabel";
            this.StorageHintLabel.Size = new System.Drawing.Size(317, 23);
            this.StorageHintLabel.TabIndex = 8;
            this.StorageHintLabel.Text = "The UPDATE.XML file is stored in the download directory.";
            // 
            // CreateXMLButton
            // 
            this.CreateXMLButton.AccessibleRole = System.Windows.Forms.AccessibleRole.PushButton;
            this.CreateXMLButton.ColorTable = DevComponents.DotNetBar.eButtonColor.OrangeWithBackground;
            this.CreateXMLButton.Location = new System.Drawing.Point(650, 379);
            this.CreateXMLButton.Name = "CreateXMLButton";
            this.CreateXMLButton.Size = new System.Drawing.Size(75, 32);
            this.CreateXMLButton.Style = DevComponents.DotNetBar.eDotNetBarStyle.StyleManagerControlled;
            this.CreateXMLButton.TabIndex = 9;
            this.CreateXMLButton.Text = "Create UPDATE.XML";
            this.CreateXMLButton.Click += new System.EventHandler(this.CreateXMLButton_Click);
            // 
            // DescriptionLabel
            // 
            // 
            // 
            // 
            this.DescriptionLabel.BackgroundStyle.CornerType = DevComponents.DotNetBar.eCornerType.Square;
            this.DescriptionLabel.Location = new System.Drawing.Point(12, 126);
            this.DescriptionLabel.Name = "DescriptionLabel";
            this.DescriptionLabel.Size = new System.Drawing.Size(263, 23);
            this.DescriptionLabel.TabIndex = 11;
            this.DescriptionLabel.Text = "Description of the update:";
            // 
            // AppVersionTextBox
            // 
            this.AppVersionTextBox.BackColor = System.Drawing.Color.White;
            // 
            // 
            // 
            this.AppVersionTextBox.Border.Class = "TextBoxBorder";
            this.AppVersionTextBox.Border.CornerType = DevComponents.DotNetBar.eCornerType.Square;
            this.AppVersionTextBox.DisabledBackColor = System.Drawing.Color.White;
            this.AppVersionTextBox.ForeColor = System.Drawing.Color.Black;
            this.AppVersionTextBox.Location = new System.Drawing.Point(116, 272);
            this.AppVersionTextBox.Name = "AppVersionTextBox";
            this.AppVersionTextBox.PreventEnterBeep = true;
            this.AppVersionTextBox.Size = new System.Drawing.Size(100, 20);
            this.AppVersionTextBox.TabIndex = 12;
            // 
            // openFileDialog1
            // 
            this.openFileDialog1.FileName = "openFileDialog1";
            // 
            // AppVersionLabel
            // 
            this.AppVersionLabel.AutoSize = true;
            // 
            // 
            // 
            this.AppVersionLabel.BackgroundStyle.CornerType = DevComponents.DotNetBar.eCornerType.Square;
            this.AppVersionLabel.Location = new System.Drawing.Point(12, 272);
            this.AppVersionLabel.Name = "AppVersionLabel";
            this.AppVersionLabel.Size = new System.Drawing.Size(98, 15);
            this.AppVersionLabel.TabIndex = 13;
            this.AppVersionLabel.Text = "Application version:";
            // 
            // MD5Label
            // 
            this.MD5Label.AutoSize = true;
            // 
            // 
            // 
            this.MD5Label.BackgroundStyle.CornerType = DevComponents.DotNetBar.eCornerType.Square;
            this.MD5Label.Location = new System.Drawing.Point(12, 319);
            this.MD5Label.Name = "MD5Label";
            this.MD5Label.Size = new System.Drawing.Size(29, 15);
            this.MD5Label.TabIndex = 14;
            this.MD5Label.Text = "MD5:";
            // 
            // MD5TextBox
            // 
            this.MD5TextBox.BackColor = System.Drawing.Color.White;
            // 
            // 
            // 
            this.MD5TextBox.Border.Class = "TextBoxBorder";
            this.MD5TextBox.Border.CornerType = DevComponents.DotNetBar.eCornerType.Square;
            this.MD5TextBox.DisabledBackColor = System.Drawing.Color.White;
            this.MD5TextBox.ForeColor = System.Drawing.Color.Black;
            this.MD5TextBox.Location = new System.Drawing.Point(116, 314);
            this.MD5TextBox.Name = "MD5TextBox";
            this.MD5TextBox.PreventEnterBeep = true;
            this.MD5TextBox.Size = new System.Drawing.Size(555, 20);
            this.MD5TextBox.TabIndex = 15;
            // 
            // UpdateDescriptionTextBox
            // 
            this.UpdateDescriptionTextBox.BackColor = System.Drawing.Color.White;
            // 
            // 
            // 
            this.UpdateDescriptionTextBox.Border.Class = "TextBoxBorder";
            this.UpdateDescriptionTextBox.Border.CornerType = DevComponents.DotNetBar.eCornerType.Square;
            this.UpdateDescriptionTextBox.DisabledBackColor = System.Drawing.Color.White;
            this.UpdateDescriptionTextBox.ForeColor = System.Drawing.Color.Black;
            this.UpdateDescriptionTextBox.Location = new System.Drawing.Point(12, 144);
            this.UpdateDescriptionTextBox.Multiline = true;
            this.UpdateDescriptionTextBox.Name = "UpdateDescriptionTextBox";
            this.UpdateDescriptionTextBox.PreventEnterBeep = true;
            this.UpdateDescriptionTextBox.Size = new System.Drawing.Size(659, 109);
            this.UpdateDescriptionTextBox.TabIndex = 16;
            // 
            // StartForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(737, 423);
            this.Controls.Add(this.UpdateDescriptionTextBox);
            this.Controls.Add(this.MD5TextBox);
            this.Controls.Add(this.MD5Label);
            this.Controls.Add(this.AppVersionLabel);
            this.Controls.Add(this.AppVersionTextBox);
            this.Controls.Add(this.DescriptionLabel);
            this.Controls.Add(this.CreateXMLButton);
            this.Controls.Add(this.StorageHintLabel);
            this.Controls.Add(this.SelectDownloadFileButton);
            this.Controls.Add(this.AppPathLabel);
            this.Controls.Add(this.DownloadPathLabel);
            this.Controls.Add(this.DownloadFilePathTextBox);
            this.Controls.Add(this.AppFilePathTextBox);
            this.Controls.Add(this.SelectAppFileButton);
            this.DoubleBuffered = true;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "StartForm";
            this.Text = "Create Update Information File";
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion
        private DevComponents.DotNetBar.ButtonX SelectAppFileButton;
        private DevComponents.DotNetBar.Controls.TextBoxX AppFilePathTextBox;
        private DevComponents.DotNetBar.Controls.TextBoxX DownloadFilePathTextBox;
        private DevComponents.DotNetBar.LabelX DownloadPathLabel;
        private DevComponents.DotNetBar.LabelX AppPathLabel;
        private DevComponents.DotNetBar.ButtonX SelectDownloadFileButton;
        private DevComponents.DotNetBar.LabelX StorageHintLabel;
        private DevComponents.DotNetBar.ButtonX CreateXMLButton;
        private DevComponents.DotNetBar.LabelX DescriptionLabel;
        private DevComponents.DotNetBar.Controls.TextBoxX AppVersionTextBox;
        private System.Windows.Forms.OpenFileDialog openFileDialog1;
        private DevComponents.DotNetBar.LabelX AppVersionLabel;
        private DevComponents.DotNetBar.LabelX MD5Label;
        private DevComponents.DotNetBar.Controls.TextBoxX MD5TextBox;
        private DevComponents.DotNetBar.Controls.TextBoxX UpdateDescriptionTextBox;
    }
}
