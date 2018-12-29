using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace JH625_HexFile_Convertor
{
    public partial class JH625_HexFile_Convertor : Form
    {
        public JH625_HexFile_Convertor()
        {
            InitializeComponent();
            this.file_type_comboBox.SelectedIndex = 2;
            this.chip_selection_comboBox.SelectedIndex = 1;
            this.Hex_StartAddr_textBox.Text = "0A0000";
            this.comboBox_target_file_opt.Items.Add("s19");
            this.comboBox_target_file_opt.Items.Add("hex");
            this.comboBox_target_file_opt.SelectedIndex = 0;
        }

        private void browse_select_button_Click(object sender, EventArgs e)
        {
            OpenFileDialog file = new OpenFileDialog();
            file.RestoreDirectory = false;    //若为false，则打开对话框后为上次的目录。若为true，则为初始目录

            if (file.ShowDialog() == DialogResult.OK)
                this.orgfile_select_comboBox.Text = System.IO.Path.GetFullPath(file.FileName);
        }

        private void convert_button_Click(object sender, EventArgs e)
        {
            if(chip_selection_comboBox.SelectedItem.ToString().Length == 0)
            {
                MessageBox.Show("请选择芯片类型", "芯片类型未选择！", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if(Hex_StartAddr_textBox.TextLength == 0)
            {
                MessageBox.Show("未给定Hex文件起始地址", "请设置Hex文件起始地址！", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            else if((UInt64.Parse(Hex_StartAddr_textBox.Text, System.Globalization.NumberStyles.HexNumber) > 0xffffffff) || 
                    (UInt64.Parse(Hex_StartAddr_textBox.Text, System.Globalization.NumberStyles.HexNumber) < 0x0A0000))
            {
                MessageBox.Show("hex地址越界", "请设置Hex起始地址范围0x0A0000~0xFFFFFFFF！", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                Hex_StartAddr_textBox.Text = "0";
                return;
            }

            if(targetfile_full_name_textBox.TextLength == 0)
            {
                MessageBox.Show("请设定转换程序完整名称", "转换后的程序名不能为空！", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            else if(targetfile_full_name_textBox.TextLength >= HEADER_FILE_FULL_NAME_SIZE)
            {
                MessageBox.Show("设定转换程序名称过长", "转换后的程序名长度不能超过86字节！", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                targetfile_full_name_textBox.Text = "";
                return;
            }
            else
            {
                if (targetfile_full_name_textBox.Text.Substring(targetfile_full_name_textBox.Text.Length - 1, 1) == "/")
                {
                    MessageBox.Show("是文件全名不是文件路径", "文件名格式错误！", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    targetfile_full_name_textBox.Text = "";
                    return;
                }
            }

            if(orgfile_select_comboBox.Text.Length == 0)
            {
                MessageBox.Show("请选择要转换的文件", "文件名不能为空！", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if(!File.Exists(orgfile_select_comboBox.Text))
            {
                MessageBox.Show("文件不存在", "请选择要转换的文件！", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            FileStream fs = new FileStream(orgfile_select_comboBox.Text, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            file_size_textBox.Text = fs.Length.ToString();

            crc_cal crc_check = new crc_cal();
            Sha1_cal sha1_check = new Sha1_cal();

            byte[] org_file_content = new byte[fs.Length + CUSTOMIZED_HEADER_SIZE+ sha1_check.SHA1HashSize];
            org_file_content[HEADER_CHIP_TYPE_START] = (byte)chip_selection_comboBox.SelectedIndex;
            org_file_content[HEADER_FILE_TYPE_START] = (byte)((UInt16.Parse(file_type_comboBox.SelectedItem.ToString())&0xff00)>>8);
            org_file_content[HEADER_FILE_TYPE_START + 1] = (byte)(UInt16.Parse(file_type_comboBox.SelectedItem.ToString()) & 0xff);
            org_file_content[HEADER_FILE_BYTES_START] = (byte)((fs.Length&0xFF000000) >> 24);
            org_file_content[HEADER_FILE_BYTES_START + 1] = (byte)((fs.Length & 0xFF0000) >> 16);
            org_file_content[HEADER_FILE_BYTES_START + 2] = (byte)((fs.Length & 0xFF00) >> 8);
            org_file_content[HEADER_FILE_BYTES_START + 3] = (byte)(fs.Length & 0xFF);

            UInt32 i = 0;

            /* fill header file full name with char ‘\0' */
            for(i=0; i< HEADER_FILE_FULL_NAME_SIZE; i++)
            {
                org_file_content[HEADER_FILE_FULL_NAME_START + i] = (byte)('\0');
            }

            byte[] targetfile_full_name_array = System.Text.Encoding.Default.GetBytes(targetfile_full_name_textBox.Text);

            /* copy target file full name to specified address */
            for (i=0; i< targetfile_full_name_array.Length ; i++)
            {
                org_file_content[HEADER_FILE_FULL_NAME_START + i] = targetfile_full_name_array[i];
            }

            UInt16 crc16_ret = 0;

            /* calculate CRC16 result for customized header */
            crc16_ret = crc_check.crc16(0, org_file_content, CUSTOMIZED_HEADER_SIZE-2);
            org_file_content[HEADER_CRC16_START] = (byte)((crc16_ret&0xff00)>>8);  // high byte of crc16
            org_file_content[HEADER_CRC16_START + 1] = (byte)(crc16_ret&0x00ff);   // low byte of crc16

            byte[] org_file_array = new byte[fs.Length];
            fs.Seek(0, SeekOrigin.Begin);
            fs.Read(org_file_array, 0, (int)fs.Length);  // read all bytes of original file to be converted

            /* copy file data */
            for(i=0; i< org_file_array.Length; i++)
            {
                org_file_content[FILE_CONTENT_START + i] = org_file_array[i];
            }

            /* calculate sha1 */
            //sha1_check.SHA1Reset();
            //sha1_check.SHA1Input(org_file_content, (uint)(fs.Length + CUSTOMIZED_HEADER_SIZE));
            //byte[] sha1_check_result = new byte[sha1_check.SHA1HashSize];
            //sha1_check.SHA1Result(ref sha1_check_result);

            SHA1 sha1 = new SHA1CryptoServiceProvider();//创建SHA1对象
            byte[] bytes_out = sha1.ComputeHash(org_file_content, 0, (int)(fs.Length + CUSTOMIZED_HEADER_SIZE));//Hash运算
            sha1.Dispose();//释放当前实例使用的所有资源
            String result = BitConverter.ToString(bytes_out);//将运算结果转为string类型


            /* copy 20 bytes of sha1 result to the file end */
            for (i=0; i< sha1_check.SHA1HashSize; i++)
            {
                org_file_content[FILE_CONTENT_START + org_file_array.Length + i] = bytes_out[i];
            }

            string[] strArray = orgfile_select_comboBox.Text.Split('\\'); //'\\'为'\'的转义字符

            /* convert bytes to S19 or hex file according to specified format */
            string hex_file_name = System.IO.Directory.GetCurrentDirectory();

            if((comboBox_target_file_opt.SelectedItem.ToString() == "s19") || (comboBox_target_file_opt.SelectedItem.ToString() == "S19"))
            {
                BinToS19 bin_2_s19 = new BinToS19();
                bin_2_s19.BinFile2S19File(UInt32.Parse(Hex_StartAddr_textBox.Text, System.Globalization.NumberStyles.HexNumber), org_file_content, strArray[strArray.Length - 1] + ".s19");
            }
            else if((comboBox_target_file_opt.SelectedItem.ToString() == "hex") || (comboBox_target_file_opt.SelectedItem.ToString() == "Hex"))
            {
                BinToHex bin_2_hex = new BinToHex();
                bin_2_hex.BinFile2HexFile(UInt32.Parse(Hex_StartAddr_textBox.Text, System.Globalization.NumberStyles.HexNumber), org_file_content, strArray[strArray.Length-1] + ".hex");
            }

            fs.Close();
            fs.Dispose();

            StringBuilder strB = new StringBuilder();

            for (i = 0; i < 96; i++)
            {
                strB.Append(org_file_content[i].ToString("X2"));
            }

            disp_richTextBox.Text = result; // strB.ToString();
        }

        private void Hex_StartAddr_textBox_KeyPress(object sender, KeyPressEventArgs e)
        {
            //if (e.KeyChar != 8 && !Char.IsDigit(e.KeyChar))
            //{
            //    e.Handled = true;
            //}

            if ( (e.KeyChar >= 'a' && e.KeyChar <= 'f') || (e.KeyChar >= 'A' && e.KeyChar <= 'F') || 
                 (e.KeyChar >= '0' && e.KeyChar <= '9') || e.KeyChar == 32 || e.KeyChar == 8 || e.KeyChar == 16 )
            {//32 Space,8 Back,13 Enter,20 Capital,46 Delete,16 ShiftKey
                e.Handled = false;//允许字符键入
            }
            else
            {
                e.Handled = true;//阻拦字符键入
            }
        }

        private void targetfile_full_name_textBox_TextChanged(object sender, EventArgs e)
        {
            if(targetfile_full_name_textBox.Text.ToString().IndexOf("\\") >= 0)
            {
                MessageBox.Show("请把‘\\’改为‘/’", "路径格式错误！", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                targetfile_full_name_textBox.Text = "";
                return;
            }
        }

        private void Hex_StartAddr_textBox_TextChanged(object sender, EventArgs e)
        {
            for(int i=0; i< Hex_StartAddr_textBox.TextLength; i++)
            {
                if( (Hex_StartAddr_textBox.Text[i] >= 'a' && Hex_StartAddr_textBox.Text[i] <= 'f') ||
                    (Hex_StartAddr_textBox.Text[i] >= 'A' && Hex_StartAddr_textBox.Text[i] <= 'F') ||
                    (Hex_StartAddr_textBox.Text[i] >= '0' && Hex_StartAddr_textBox.Text[i] <= '9') ||
                    (Hex_StartAddr_textBox.Text[i] == 32) || (Hex_StartAddr_textBox.Text[i] == 8) || 
                    (Hex_StartAddr_textBox.Text[i] == 16) )
                {
                    //32 Space; 8 Back space; 13 Enter; 20 Capital; 46 Delete; 16 ShiftKey
                    ;
                }
                else
                {
                    MessageBox.Show("输入字符为0~F", "地址为16进制！", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    Hex_StartAddr_textBox.Text = "";
                    return;
                }
            }
        }
    }
}
