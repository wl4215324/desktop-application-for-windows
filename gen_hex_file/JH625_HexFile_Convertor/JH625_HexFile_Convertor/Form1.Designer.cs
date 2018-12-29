namespace JH625_HexFile_Convertor
{
    partial class JH625_HexFile_Convertor
    {
        /// <summary>
        /// 必需的设计器变量。
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// 清理所有正在使用的资源。
        /// </summary>
        /// <param name="disposing">如果应释放托管资源，为 true；否则为 false。</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows 窗体设计器生成的代码

        /// <summary>
        /// 设计器支持所需的方法 - 不要修改
        /// 使用代码编辑器修改此方法的内容。
        /// </summary>
        private void InitializeComponent()
        {
            this.convert_button = new System.Windows.Forms.Button();
            this.HeaderInfor_groupBox = new System.Windows.Forms.GroupBox();
            this.Hex_StartAddr_textBox = new System.Windows.Forms.TextBox();
            this.label1 = new System.Windows.Forms.Label();
            this.targetfile_full_name_textBox = new System.Windows.Forms.TextBox();
            this.targetfile_full_name_label = new System.Windows.Forms.Label();
            this.file_size_textBox = new System.Windows.Forms.TextBox();
            this.file_type_label = new System.Windows.Forms.Label();
            this.file_size_label = new System.Windows.Forms.Label();
            this.chip_selection_label = new System.Windows.Forms.Label();
            this.file_type_comboBox = new System.Windows.Forms.ComboBox();
            this.chip_selection_comboBox = new System.Windows.Forms.ComboBox();
            this.orgfile_selection_label = new System.Windows.Forms.Label();
            this.orgfile_select_comboBox = new System.Windows.Forms.ComboBox();
            this.browse_select_button = new System.Windows.Forms.Button();
            this.disp_richTextBox = new System.Windows.Forms.RichTextBox();
            this.comboBox_target_file_opt = new System.Windows.Forms.ComboBox();
            this.label2 = new System.Windows.Forms.Label();
            this.HeaderInfor_groupBox.SuspendLayout();
            this.SuspendLayout();
            // 
            // convert_button
            // 
            this.convert_button.Location = new System.Drawing.Point(475, 308);
            this.convert_button.Name = "convert_button";
            this.convert_button.Size = new System.Drawing.Size(75, 23);
            this.convert_button.TabIndex = 0;
            this.convert_button.Text = "确定";
            this.convert_button.UseVisualStyleBackColor = true;
            this.convert_button.Click += new System.EventHandler(this.convert_button_Click);
            // 
            // HeaderInfor_groupBox
            // 
            this.HeaderInfor_groupBox.Controls.Add(this.Hex_StartAddr_textBox);
            this.HeaderInfor_groupBox.Controls.Add(this.label1);
            this.HeaderInfor_groupBox.Controls.Add(this.targetfile_full_name_textBox);
            this.HeaderInfor_groupBox.Controls.Add(this.targetfile_full_name_label);
            this.HeaderInfor_groupBox.Controls.Add(this.file_size_textBox);
            this.HeaderInfor_groupBox.Controls.Add(this.file_type_label);
            this.HeaderInfor_groupBox.Controls.Add(this.file_size_label);
            this.HeaderInfor_groupBox.Controls.Add(this.chip_selection_label);
            this.HeaderInfor_groupBox.Controls.Add(this.file_type_comboBox);
            this.HeaderInfor_groupBox.Controls.Add(this.chip_selection_comboBox);
            this.HeaderInfor_groupBox.Location = new System.Drawing.Point(12, 12);
            this.HeaderInfor_groupBox.Name = "HeaderInfor_groupBox";
            this.HeaderInfor_groupBox.Size = new System.Drawing.Size(558, 132);
            this.HeaderInfor_groupBox.TabIndex = 1;
            this.HeaderInfor_groupBox.TabStop = false;
            this.HeaderInfor_groupBox.Text = "HeaderInfor";
            // 
            // Hex_StartAddr_textBox
            // 
            this.Hex_StartAddr_textBox.Location = new System.Drawing.Point(309, 29);
            this.Hex_StartAddr_textBox.Name = "Hex_StartAddr_textBox";
            this.Hex_StartAddr_textBox.Size = new System.Drawing.Size(78, 21);
            this.Hex_StartAddr_textBox.TabIndex = 9;
            this.Hex_StartAddr_textBox.TextChanged += new System.EventHandler(this.Hex_StartAddr_textBox_TextChanged);
            this.Hex_StartAddr_textBox.KeyPress += new System.Windows.Forms.KeyPressEventHandler(this.Hex_StartAddr_textBox_KeyPress);
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(192, 33);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(119, 12);
            this.label1.TabIndex = 8;
            this.label1.Text = "Hex文件起始地址(0x)";
            // 
            // targetfile_full_name_textBox
            // 
            this.targetfile_full_name_textBox.Location = new System.Drawing.Point(283, 72);
            this.targetfile_full_name_textBox.Name = "targetfile_full_name_textBox";
            this.targetfile_full_name_textBox.Size = new System.Drawing.Size(265, 21);
            this.targetfile_full_name_textBox.TabIndex = 7;
            this.targetfile_full_name_textBox.TextChanged += new System.EventHandler(this.targetfile_full_name_textBox_TextChanged);
            // 
            // targetfile_full_name_label
            // 
            this.targetfile_full_name_label.AutoSize = true;
            this.targetfile_full_name_label.Location = new System.Drawing.Point(160, 76);
            this.targetfile_full_name_label.Name = "targetfile_full_name_label";
            this.targetfile_full_name_label.Size = new System.Drawing.Size(125, 12);
            this.targetfile_full_name_label.TabIndex = 6;
            this.targetfile_full_name_label.Text = "文件全名（绝对路径）";
            // 
            // file_size_textBox
            // 
            this.file_size_textBox.Enabled = false;
            this.file_size_textBox.Location = new System.Drawing.Point(62, 73);
            this.file_size_textBox.Name = "file_size_textBox";
            this.file_size_textBox.Size = new System.Drawing.Size(76, 21);
            this.file_size_textBox.TabIndex = 5;
            // 
            // file_type_label
            // 
            this.file_type_label.AutoSize = true;
            this.file_type_label.Location = new System.Drawing.Point(402, 33);
            this.file_type_label.Name = "file_type_label";
            this.file_type_label.Size = new System.Drawing.Size(53, 12);
            this.file_type_label.TabIndex = 4;
            this.file_type_label.Text = "文件类型";
            // 
            // file_size_label
            // 
            this.file_size_label.AutoSize = true;
            this.file_size_label.Location = new System.Drawing.Point(7, 77);
            this.file_size_label.Name = "file_size_label";
            this.file_size_label.Size = new System.Drawing.Size(53, 12);
            this.file_size_label.TabIndex = 3;
            this.file_size_label.Text = "文件大小";
            // 
            // chip_selection_label
            // 
            this.chip_selection_label.AutoSize = true;
            this.chip_selection_label.Location = new System.Drawing.Point(7, 33);
            this.chip_selection_label.Name = "chip_selection_label";
            this.chip_selection_label.Size = new System.Drawing.Size(95, 12);
            this.chip_selection_label.TabIndex = 3;
            this.chip_selection_label.Text = "Hex文件适用芯片";
            // 
            // file_type_comboBox
            // 
            this.file_type_comboBox.FormattingEnabled = true;
            this.file_type_comboBox.Items.AddRange(new object[] {
            "0444",
            "0555",
            "0666",
            "0755",
            "0777"});
            this.file_type_comboBox.Location = new System.Drawing.Point(459, 29);
            this.file_type_comboBox.Name = "file_type_comboBox";
            this.file_type_comboBox.Size = new System.Drawing.Size(89, 20);
            this.file_type_comboBox.TabIndex = 2;
            // 
            // chip_selection_comboBox
            // 
            this.chip_selection_comboBox.FormattingEnabled = true;
            this.chip_selection_comboBox.Items.AddRange(new object[] {
            "MCU",
            "ARM"});
            this.chip_selection_comboBox.Location = new System.Drawing.Point(107, 29);
            this.chip_selection_comboBox.Name = "chip_selection_comboBox";
            this.chip_selection_comboBox.Size = new System.Drawing.Size(73, 20);
            this.chip_selection_comboBox.TabIndex = 2;
            // 
            // orgfile_selection_label
            // 
            this.orgfile_selection_label.AutoSize = true;
            this.orgfile_selection_label.Location = new System.Drawing.Point(19, 186);
            this.orgfile_selection_label.Name = "orgfile_selection_label";
            this.orgfile_selection_label.Size = new System.Drawing.Size(125, 12);
            this.orgfile_selection_label.TabIndex = 2;
            this.orgfile_selection_label.Text = "选择被转换的原始文件";
            // 
            // orgfile_select_comboBox
            // 
            this.orgfile_select_comboBox.FormattingEnabled = true;
            this.orgfile_select_comboBox.Location = new System.Drawing.Point(148, 183);
            this.orgfile_select_comboBox.Name = "orgfile_select_comboBox";
            this.orgfile_select_comboBox.Size = new System.Drawing.Size(327, 20);
            this.orgfile_select_comboBox.TabIndex = 3;
            // 
            // browse_select_button
            // 
            this.browse_select_button.Location = new System.Drawing.Point(481, 182);
            this.browse_select_button.Name = "browse_select_button";
            this.browse_select_button.Size = new System.Drawing.Size(75, 23);
            this.browse_select_button.TabIndex = 4;
            this.browse_select_button.Text = "浏览选择";
            this.browse_select_button.UseVisualStyleBackColor = true;
            this.browse_select_button.Click += new System.EventHandler(this.browse_select_button_Click);
            // 
            // disp_richTextBox
            // 
            this.disp_richTextBox.Location = new System.Drawing.Point(21, 220);
            this.disp_richTextBox.Name = "disp_richTextBox";
            this.disp_richTextBox.Size = new System.Drawing.Size(431, 96);
            this.disp_richTextBox.TabIndex = 5;
            this.disp_richTextBox.Text = "";
            // 
            // comboBox_target_file_opt
            // 
            this.comboBox_target_file_opt.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.comboBox_target_file_opt.FormattingEnabled = true;
            this.comboBox_target_file_opt.Location = new System.Drawing.Point(475, 248);
            this.comboBox_target_file_opt.Name = "comboBox_target_file_opt";
            this.comboBox_target_file_opt.Size = new System.Drawing.Size(59, 20);
            this.comboBox_target_file_opt.TabIndex = 6;
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(469, 223);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(77, 12);
            this.label2.TabIndex = 2;
            this.label2.Text = "生成文件格式";
            // 
            // JH625_HexFile_Convertor
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 12F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(582, 355);
            this.Controls.Add(this.comboBox_target_file_opt);
            this.Controls.Add(this.disp_richTextBox);
            this.Controls.Add(this.browse_select_button);
            this.Controls.Add(this.orgfile_select_comboBox);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.orgfile_selection_label);
            this.Controls.Add(this.HeaderInfor_groupBox);
            this.Controls.Add(this.convert_button);
            this.MaximizeBox = false;
            this.Name = "JH625_HexFile_Convertor";
            this.Text = "JH625_HexFile_Convertor";
            this.HeaderInfor_groupBox.ResumeLayout(false);
            this.HeaderInfor_groupBox.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Button convert_button;
        private System.Windows.Forms.GroupBox HeaderInfor_groupBox;
        private System.Windows.Forms.ComboBox chip_selection_comboBox;
        private System.Windows.Forms.TextBox Hex_StartAddr_textBox;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.TextBox targetfile_full_name_textBox;
        private System.Windows.Forms.Label targetfile_full_name_label;
        private System.Windows.Forms.TextBox file_size_textBox;
        private System.Windows.Forms.Label file_type_label;
        private System.Windows.Forms.Label file_size_label;
        private System.Windows.Forms.Label chip_selection_label;
        private System.Windows.Forms.ComboBox file_type_comboBox;
        private System.Windows.Forms.Label orgfile_selection_label;
        private System.Windows.Forms.ComboBox orgfile_select_comboBox;
        private System.Windows.Forms.Button browse_select_button;


        private const int CUSTOMIZED_HEADER_SIZE = 96;

        private const int HEADER_CHIP_TYPE_START = 0;
        private const int HEADER_CHIP_TYPE_SIZE = 1;

        private const int HEADER_FILE_TYPE_START = (HEADER_CHIP_TYPE_START + HEADER_CHIP_TYPE_SIZE);
        private const int HEADER_FILE_TYPE_SIZE = 2;

        private const int HEADER_FILE_BYTES_START = HEADER_FILE_TYPE_START + HEADER_FILE_TYPE_SIZE;
        private const int HEADER_FILE_BYTES_SIZE = 4;

        private const int HEADER_FILE_FULL_NAME_START = HEADER_FILE_BYTES_START + HEADER_FILE_BYTES_SIZE;
        private const int HEADER_FILE_FULL_NAME_SIZE = 87;

        private const int HEADER_CRC16_START = HEADER_FILE_FULL_NAME_START + HEADER_FILE_FULL_NAME_SIZE;
        private const int HEADER_CRC16_SIZE = 2;

        private const int FILE_CONTENT_START = 96;

        private System.Windows.Forms.RichTextBox disp_richTextBox;
        private System.Windows.Forms.ComboBox comboBox_target_file_opt;
        private System.Windows.Forms.Label label2;
    }
}

