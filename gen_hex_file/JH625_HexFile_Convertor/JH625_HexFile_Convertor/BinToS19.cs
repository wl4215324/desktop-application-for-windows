using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JH625_HexFile_Convertor
{
    class BinToS19
    { 
        private UInt32 char_to_uint(char c)
        {
            int res = 0;

            if (c >= '0' && c <= '9')
                res = (c - '0');
            else if (c >= 'A' && c <= 'F')
                res = (c - 'A' + 10);
            else if (c >= 'a' && c <= 'f')
                res = (c - 'a' + 10);

            return ((UInt32) res);
        }

        private UInt32 str_to_uint32(char[] s)
        {
            int i;
            char c;
            UInt32 res = 0;

            for (i = 0; (i < 8) && (s[i] != '\0'); i++)
            {
                c = s[i];
                res <<= 4;
                res += char_to_uint(c);
            }

            return (res);
        }

        private long file_size(string file_name)
        {
            if(file_name.Length <= 0)
            {
                return 0;
            }

            if(!File.Exists(file_name))
            {
                return 0;
            }

            FileStream fs = new FileStream(file_name, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            return fs.Length;
        }

        private int max(int a, int b)
        {
            return (((a) > (b)) ? (a) : (b));
        }

        private int min(int a, int b)
        {
            return (((a) < (b)) ? (a) : (b));
        }

        public int BinFile2S19File(UInt32 start_addr, byte[] org_data_array, string save_file_name)
        {
            int i;
            UInt32 max_addr, line_address;
            int byte_count, this_line;
            byte line_checksum;
            byte c;
            int record_count = 0;
            int addr_bytes = 2;
            bool do_headers = true;
            int line_length = 32;
            byte[] buf = new byte[32];
            FileStream fsWrite;
            int src_file_length;
            byte[] buffer_S19 = new byte[48];

            StringBuilder each_line_content = new StringBuilder();
            UInt32 addr_offset = 0;
            int convert_index = 0;

            if (org_data_array.Length <= 0)
            {
                return -1;
            }

            if (save_file_name.Length <= 0)
            {
                return -1;
            }

            fsWrite = new FileStream(save_file_name, FileMode.Create, FileAccess.Write);

            if (fsWrite == null)
            {
                return -1;
            }

            src_file_length = org_data_array.Length;
            fsWrite.Seek(0, SeekOrigin.Begin);
            addr_offset = start_addr;

            max_addr = (UInt32)(addr_offset + src_file_length - 1);

            if ((max_addr > 0xffff) && (addr_bytes < 3))
                addr_bytes = 3;

            if ((max_addr > 0xffffff) && (addr_bytes < 4))
                addr_bytes = 4;

            /* construct S19 header */
            if (do_headers)
            {
                each_line_content.Append("S00600004844521B\n");  /* Header record */
                buffer_S19 = System.Text.Encoding.Default.GetBytes(each_line_content.ToString());
                fsWrite.Write(buffer_S19, 0, each_line_content.Length);
            }

            line_address = addr_offset;

            for (; src_file_length > 0;)
            {
                /* empty string to be writen */
                each_line_content.Remove(0, each_line_content.Length);


                this_line = min(line_length, src_file_length);
                byte_count = (addr_bytes + this_line + 1);
                each_line_content.Append(string.Format("S{0:X1}", addr_bytes - 1));
                each_line_content.Append(string.Format("{0:X2}",  byte_count));
                line_checksum = (byte)byte_count;

                for (i = addr_bytes - 1; i >= 0; i--)
                {
                    c = (byte)((line_address >> (i << 3)) & 0xff);
                    each_line_content.Append(string.Format("{0:X2}", c));
                    line_checksum += c;
                }

                Array.Copy(org_data_array, convert_index, buf, 0, this_line);

                for (i = 0; i < this_line; i++)
                {
                    each_line_content.Append(string.Format("{0:X2}", buf[i]));
                    line_checksum += buf[i];
                }

                convert_index += this_line;
                each_line_content.Append(string.Format("{0:X2}\n", 255 - line_checksum));
                record_count++;

                //address += line_length;
                line_address = (UInt32)(line_address + this_line);
                buffer_S19 = System.Text.Encoding.Default.GetBytes(each_line_content.ToString());
                fsWrite.Write(buffer_S19, 0, each_line_content.Length);

                /* check before adding to allow for finishing at 0xffffffff */
                //if ((line_address - 1 + this_line) >= max_addr)
                //    break;
                src_file_length = src_file_length - this_line;
            }

            /* S19 end line */
            each_line_content.Remove(0, each_line_content.Length);
            each_line_content.Append("S8");
            this_line = 0;
            byte_count = (addr_bytes + this_line + 1);
            line_checksum = (byte)byte_count;
            each_line_content.Append(string.Format("{0:X2}", byte_count));

            for (i = addr_bytes - 1; i >= 0; i--)
            {
                c = (byte)((addr_offset >> (i << 3)) & 0xff);
                each_line_content.Append(string.Format("{0:X2}", c));
                line_checksum += c;
            }

            each_line_content.Append(string.Format("{0:X2}\n", 255 - line_checksum));
            buffer_S19 = System.Text.Encoding.Default.GetBytes(each_line_content.ToString());
            fsWrite.Write(buffer_S19, 0, each_line_content.Length);



            //if (do_headers)
            //{
            //    if (record_count > 0xffff)
            //    {
            //        checksum = 4 + (record_count & 0xff) + ((record_count >> 8) & 0xff) + ((record_count >> 16) & 0xff);
            //        printf("S604%06X%02X\n", record_count, 255 - checksum);
            //    }
            //    else
            //    {
            //        checksum = 3 + (record_count & 0xff) + ((record_count >> 8) & 0xff);
            //        printf("S503%04X%02X\n", record_count, 255 - checksum);
            //    }

            //    byte_count = (addr_bytes + 1);
            //    printf("S%d%02X", 11 - addr_bytes, byte_count);

            //    checksum = byte_count;

            //    for (i = addr_bytes - 1; i >= 0; i--)
            //    {
            //        c = (addr_offset >> (i << 3)) & 0xff;
            //        printf("%02X", c);
            //        checksum += c;
            //    }

            //    printf("%02X\n", 255 - checksum);
            //}

            fsWrite.Close();
            fsWrite.Dispose();

            return 0;
        }
    }
}
