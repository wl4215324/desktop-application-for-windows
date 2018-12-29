using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JH625_HexFile_Convertor
{
    class BinToHex
    {
        public enum RESULT_STATUS
        {
            RES_OK = 0,                     //操作完成
            RES_BIN_FILE_NOT_EXIST,        //相当于bin文件不存在，包括输入的路径可能存在不正确
            RES_HEX_FILE_PATH_ERROR,        //目标文件路径可能输入有误
            FILE_FORMAT_ERROR
        };

        public unsafe struct HexFormat
        {
            public byte len;
            public byte[] addr;//[4]
            public byte type;
            public byte[] data; //* data
        };

        private const byte NUMBER_OF_ONE_LINE = 0x20;
        private const byte MAX_BUFFER_OF_ONE_LINE = (NUMBER_OF_ONE_LINE * 2 + 11);
        private const byte MAX_DATA_LEN_OF_PER_LINE_IN_HEX = 0x20;
        private const byte MAX_MESSAGE_LEN_OF_PER_LINE_IN_HEX = (MAX_DATA_LEN_OF_PER_LINE_IN_HEX * 2 + 11);

        /********************************************************************************
        input:
            dest: 为转换后的结果
            p->addr[0]: 高地址
            p->addr[1]: 低地址
            p->type: 记录类型
            p->data: 为bin格式流有效数据指针
            p->len: 为bin格式流有效数据长度
        output:
            返回有效数据的长度
        ********************************************************************************/
        public UInt16 BinFormatEncode(ref byte[] dest, HexFormat p)
        {
            UInt16 offset = 0;
            byte check = 0, num = 0;     //:(1) + 长度(2) + 地址(4) + 类型(2)

            StringBuilder each_line_content = new StringBuilder();
            each_line_content.Append(string.Format(":{0:X2}", p.len));
            each_line_content.Append(string.Format("{0:X2}", p.addr[0]));
            each_line_content.Append(string.Format("{0:X2}", p.addr[1]));
            each_line_content.Append(string.Format("{0:X2}", p.type));
            offset += 9;   
            check = (byte)(p.len + p.addr[0] + p.addr[1] + p.type); //计算校验和

            while (num < p.len)          //当数据长度不为0，继续在之前的hex格式流添加数据
            {
                if (0 == p.type)
                {
                    //sprintf((char*)dest + offset, "%02X", p.data[num]);
                    each_line_content.Append(string.Format("{0:X2}", p.data[num]));
                    check += p.data[num];      //计算校验和
                    offset += 2;               //hex格式数据流数据指针偏移2
                    num++;                     //下一个字符
                }
                else if (4 == p.type)
                {
                    //sprintf((char*)dest + offset, "%02X", p.addr[num + 2]);
                    each_line_content.Append(string.Format("{0:X2}", p.addr[num + 2]));
                    check += p.addr[num + 2];  //计算校验和
                    offset += 2;               //hex格式数据流数据指针偏移2
                    num++;                     //下一个字符
                }
            }

            check = (byte)(~check + 1);             //反码+1
            //sprintf((char*)dest + offset, "%02X", check);
            each_line_content.Append(string.Format("{0:X2}", check));
            offset += 2;

            dest = System.Text.Encoding.Default.GetBytes(each_line_content.ToString());
            return offset;                  //返回hex格式数据流的长度
        }


        public RESULT_STATUS BinFile2HexFile(string src, string dest)
        {
            HexFormat gHexFor;
            gHexFor.addr = new byte[4];
            gHexFor.data = new byte[MAX_DATA_LEN_OF_PER_LINE_IN_HEX];  //指向需要转换的bin数据流

            FileStream fsReader, fsWrite;
            UInt16 tmp;
            UInt32 low_addr = 0x20, hign_addr = 0;
            byte[] buffer_bin = new byte[MAX_DATA_LEN_OF_PER_LINE_IN_HEX];
            byte[] buffer_hex = new byte[MAX_MESSAGE_LEN_OF_PER_LINE_IN_HEX];
            UInt32 src_file_length;
            UInt32 src_file_quotient, cur_file_page = 0;
            byte src_file_remainder;
            byte[] sha1_check = new byte[20];
            int i = 0;

            fsReader = new FileStream(src, FileMode.Open, FileAccess.Read);        //源文件为bin文件,以二进制的形式打开

            if(fsReader == null)  //这里也是相当于用来检查用户的输入是否准备
            {
                return RESULT_STATUS.RES_BIN_FILE_NOT_EXIST;
            }

            fsWrite = new FileStream(dest, FileMode.OpenOrCreate, FileAccess.Write);  //目的文件为hex文件，以文本的形式打开

            if(fsWrite == null)
            {
                return RESULT_STATUS.RES_HEX_FILE_PATH_ERROR;
            }

            //fseek(src_file, 0, SEEK_END);        //定位到文件末
            //src_file_length = ftell(src_file);   //get total bytes of binary file
            src_file_length = (UInt32)fsReader.Length;
            //fseek(src_file, 0, SEEK_SET);        //重新定位到开头，准备开始读取数据
            fsReader.Seek(0, SeekOrigin.Begin);
            fsWrite.Seek(0, SeekOrigin.Begin);
            src_file_quotient = (UInt32) (src_file_length / MAX_DATA_LEN_OF_PER_LINE_IN_HEX); //商，需要读取多少次
            src_file_remainder = (byte) (src_file_length % MAX_DATA_LEN_OF_PER_LINE_IN_HEX);  //余数，最后一次需要多少个字符

            Sha1_cal sha1_cal = new Sha1_cal();
            sha1_cal.SHA1Reset();

            while (cur_file_page < src_file_quotient)
            {
                fsReader.Read(gHexFor.data, 0, MAX_DATA_LEN_OF_PER_LINE_IN_HEX);
                //fread(buffer_bin, 1, MAX_DATA_LEN_OF_PER_LINE_IN_HEX, src_file);
                //SHA1Input(&file_sha, buffer_bin, MAX_DATA_LEN_OF_PER_LINE_IN_HEX);
                sha1_cal.SHA1Input(gHexFor.data, MAX_DATA_LEN_OF_PER_LINE_IN_HEX);

                if (((low_addr & 0xffff0000) != hign_addr) && ((low_addr & 0xffff0000) > 0))  //只有大于64K以后才写入扩展线性地址，第一次一般是没有
                {
                    hign_addr = low_addr&0xffff0000;
                    //gHexFor.addr[0] = (uint8_t)((hign_addr &0xff000000) >> 24);
                    //gHexFor.addr[1] = (uint8_t)((hign_addr &0xff0000) >> 16);
                    gHexFor.addr[0] = (byte) ((low_addr &0xff00) >> 8);
                    gHexFor.addr[1] = (byte) (low_addr &0xff);
                    gHexFor.addr[2] = (byte) ((hign_addr &0xff000000) >> 24);
                    gHexFor.addr[3] = (byte) ((hign_addr &0x00ff0000) >> 16);
                    gHexFor.type = 4;
                    gHexFor.len = 2;  //记录扩展地址

                    tmp = BinFormatEncode(ref buffer_hex, gHexFor);
                    //fwrite(buffer_hex, 1, tmp, dest_file);
                    fsWrite.Write(buffer_hex, 0, tmp);
                    //fprintf(dest_file,"\n"); //end-of-line character
                    buffer_hex = System.Text.Encoding.Default.GetBytes("\n");
                    fsWrite.Write(buffer_hex, 0, 1);
                }

                gHexFor.addr[0] = (byte) ((low_addr &0xff00) >> 8);
                gHexFor.addr[1] = (byte) (low_addr &0x00ff);
                gHexFor.type =0;  //数据记录
                gHexFor.len = MAX_DATA_LEN_OF_PER_LINE_IN_HEX;
                tmp = BinFormatEncode(ref buffer_hex, gHexFor);
                //fwrite(buffer_hex, 1, tmp, dest_file);
                fsWrite.Write(buffer_hex, 0, tmp);
                //fprintf(dest_file,"\n");
                buffer_hex = System.Text.Encoding.Default.GetBytes("\n");
                fsWrite.Write(buffer_hex, 0, 1);
                cur_file_page++;
                low_addr += NUMBER_OF_ONE_LINE;
            }

            if(src_file_remainder != 0)       //最后一次读取的个数不为0，这继续读取
            {
                //fread(buffer_bin,1, src_file_remainder, src_file);
                //SHA1Input(&file_sha, buffer_bin, src_file_remainder);
                fsReader.Read(gHexFor.data, 0, src_file_remainder);
                sha1_cal.SHA1Input(gHexFor.data, src_file_remainder);

                gHexFor.addr[0] = (byte) ((low_addr &0xff00) >> 8);
                gHexFor.addr[1] = (byte) (low_addr &0x00ff);
                gHexFor.len = src_file_remainder;
                gHexFor.type = 0;  //数据记录
                tmp = BinFormatEncode(ref buffer_hex, gHexFor);
                fsWrite.Write(buffer_hex, 0, tmp);

                buffer_hex = System.Text.Encoding.Default.GetBytes("\n");
                fsWrite.Write(buffer_hex, 0, 1);
                low_addr += src_file_remainder;
            }

            if (((low_addr & 0xffff0000) != hign_addr) && ((low_addr & 0xffff0000) > 0))  //只有大于64K以后才写入扩展线性地址，第一次一般是没有
            {
                hign_addr = low_addr&0xffff0000;
                gHexFor.addr[0] = (byte) ((low_addr &0xff00) >> 8);
                gHexFor.addr[1] = (byte) (low_addr &0xff);
                gHexFor.addr[2] = (byte) ((hign_addr &0xff000000) >> 24);
                gHexFor.addr[3] = (byte) ((hign_addr &0x00ff0000) >> 16);
                gHexFor.type = 4;
                gHexFor.len = 2;  //记录扩展地址
                tmp = BinFormatEncode(ref buffer_hex, gHexFor);
                fsWrite.Write(buffer_hex, 0, tmp);
                //fprintf(dest_file, "\n"); //end-of-line character

                buffer_hex = System.Text.Encoding.Default.GetBytes("\n");
                fsWrite.Write(buffer_hex, 0, 1);
            }

            gHexFor.addr[0] = (byte) ((low_addr &0xff00) >> 8);
            gHexFor.addr[1] = (byte) (low_addr &0x00ff);
            gHexFor.type =0;  //数据记录
            //gHexFor.len = SHA1HashSize;
            gHexFor.len = 20;
            //memcpy(buffer_bin, sha1_check, SHA1HashSize);
        
            for(i=0; i<20; i++)
            {
            
            }
            tmp = BinFormatEncode(ref buffer_hex, gHexFor);
            //fwrite(buffer_hex, 1, tmp, dest_file);
            fsWrite.Write(buffer_hex, 0, tmp);
            //fprintf(dest_file, "\n");
            buffer_hex = System.Text.Encoding.Default.GetBytes("\n");
            fsWrite.Write(buffer_hex, 0, 1);

            gHexFor.addr[0] =0;
            gHexFor.addr[1] =0;
            gHexFor.type =1;  //结束符
            gHexFor.len =0;
            tmp = BinFormatEncode(ref buffer_hex, gHexFor);
            //fwrite(buffer_hex,1, tmp, dest_file);
            fsWrite.Write(buffer_hex, 0, tmp);
            //fprintf(dest_file,"\n");
            buffer_hex = System.Text.Encoding.Default.GetBytes("\n");
            fsWrite.Write(buffer_hex, 0, 1);

            //fclose(src_file);
            //fclose(dest_file);

            fsWrite.Close();
            fsWrite.Dispose();

            fsReader.Close();
            fsReader.Dispose();

            return RESULT_STATUS.RES_OK;
        }


        public int BinFile2HexFile(UInt32 start_addr, byte[] org_data_array, string save_file_name)
        {
            HexFormat gHexFor;
            gHexFor.addr = new byte[4];
            gHexFor.data = new byte[MAX_DATA_LEN_OF_PER_LINE_IN_HEX];  //指向需要转换的bin数据流

            FileStream fsWrite;
            UInt16 tmp;
            UInt32 low_addr = start_addr, hign_addr = 0;
            byte[] buffer_bin = new byte[MAX_DATA_LEN_OF_PER_LINE_IN_HEX];
            byte[] buffer_hex = new byte[MAX_MESSAGE_LEN_OF_PER_LINE_IN_HEX];
            UInt32 src_file_length;
            UInt32 src_file_quotient, cur_file_page = 0;
            byte src_file_remainder;
            byte[] sha1_check = new byte[20];
            int i = 0;

            if(org_data_array.Length <= 0)
            {
                return -1;
            }

            if(save_file_name.Length <= 0)
            {
                return -1;
            }

            //fsWrite = new FileStream(save_file_name, FileMode.OpenOrCreate, FileAccess.Write);  //目的文件为hex文件，以文本的形式打开
            fsWrite = new FileStream(save_file_name, FileMode.Create, FileAccess.Write);

            if (fsWrite == null)
            {
                return -1;
            }

            src_file_length = (UInt32)org_data_array.Length;
            fsWrite.Seek(0, SeekOrigin.Begin);
            src_file_quotient = (UInt32)(src_file_length / MAX_DATA_LEN_OF_PER_LINE_IN_HEX); //商，需要读取多少次
            src_file_remainder = (byte)(src_file_length % MAX_DATA_LEN_OF_PER_LINE_IN_HEX);  //余数，最后一次需要多少个字符
            uint org_data_array_pr = 0;

            while (cur_file_page < src_file_quotient)
            {
                for (i = 0; i < MAX_DATA_LEN_OF_PER_LINE_IN_HEX; i++, org_data_array_pr++)
                {
                    gHexFor.data[i] = org_data_array[org_data_array_pr];
                }

                if (((low_addr & 0xffff0000) != hign_addr) && ((low_addr & 0xffff0000) > 0))  //只有大于64K以后才写入扩展线性地址，第一次一般是没有
                {
                    hign_addr = low_addr & 0xffff0000;
                    //gHexFor.addr[0] = (uint8_t)((hign_addr &0xff000000) >> 24);
                    //gHexFor.addr[1] = (uint8_t)((hign_addr &0xff0000) >> 16);
                    gHexFor.addr[0] = (byte)((low_addr & 0xff00) >> 8);
                    gHexFor.addr[1] = (byte)(low_addr & 0xff);
                    gHexFor.addr[2] = (byte)((hign_addr & 0xff000000) >> 24);
                    gHexFor.addr[3] = (byte)((hign_addr & 0x00ff0000) >> 16);
                    gHexFor.type = 4;
                    gHexFor.len = 2;  //记录扩展地址

                    tmp = BinFormatEncode(ref buffer_hex, gHexFor);
                    //fwrite(buffer_hex, 1, tmp, dest_file);
                    fsWrite.Write(buffer_hex, 0, tmp);
                    //fprintf(dest_file,"\n"); //end-of-line character
                    buffer_hex = System.Text.Encoding.Default.GetBytes("\n");
                    fsWrite.Write(buffer_hex, 0, 1);
                }

                gHexFor.addr[0] = (byte)((low_addr & 0xff00) >> 8);
                gHexFor.addr[1] = (byte)(low_addr & 0x00ff);
                gHexFor.type = 0;  //数据记录
                gHexFor.len = MAX_DATA_LEN_OF_PER_LINE_IN_HEX;
                tmp = BinFormatEncode(ref buffer_hex, gHexFor);
                //fwrite(buffer_hex, 1, tmp, dest_file);
                fsWrite.Write(buffer_hex, 0, tmp);
                //fprintf(dest_file,"\n");
                buffer_hex = System.Text.Encoding.Default.GetBytes("\n");
                fsWrite.Write(buffer_hex, 0, 1);
                cur_file_page++;
                low_addr += NUMBER_OF_ONE_LINE;
            }

            if (src_file_remainder != 0)       //最后一次读取的个数不为0，这继续读取
            {
                for (i = 0; i < src_file_remainder; i++, org_data_array_pr++)
                {
                    gHexFor.data[i] = org_data_array[org_data_array_pr];
                }

                if (((low_addr & 0xffff0000) != hign_addr) && ((low_addr & 0xffff0000) > 0))  //只有大于64K以后才写入扩展线性地址，第一次一般是没有
                {
                    hign_addr = low_addr & 0xffff0000;
                    gHexFor.addr[0] = (byte)((low_addr & 0xff00) >> 8);
                    gHexFor.addr[1] = (byte)(low_addr & 0xff);
                    gHexFor.addr[2] = (byte)((hign_addr & 0xff000000) >> 24);
                    gHexFor.addr[3] = (byte)((hign_addr & 0x00ff0000) >> 16);
                    gHexFor.type = 4;
                    gHexFor.len = 2;  //记录扩展地址
                    tmp = BinFormatEncode(ref buffer_hex, gHexFor);
                    fsWrite.Write(buffer_hex, 0, tmp);
                    //fprintf(dest_file, "\n"); //end-of-line character

                    buffer_hex = System.Text.Encoding.Default.GetBytes("\n");
                    fsWrite.Write(buffer_hex, 0, 1);
                }

                gHexFor.addr[0] = (byte)((low_addr & 0xff00) >> 8);
                gHexFor.addr[1] = (byte)(low_addr & 0x00ff);
                gHexFor.len = src_file_remainder;
                gHexFor.type = 0;  //数据记录
                tmp = BinFormatEncode(ref buffer_hex, gHexFor);
                fsWrite.Write(buffer_hex, 0, tmp);

                buffer_hex = System.Text.Encoding.Default.GetBytes("\n");
                fsWrite.Write(buffer_hex, 0, 1);
                low_addr += src_file_remainder;
            }

            /* the end line */
            gHexFor.addr[0] = 0;
            gHexFor.addr[1] = 0;
            gHexFor.type = 1;  //结束符
            gHexFor.len = 0;
            tmp = BinFormatEncode(ref buffer_hex, gHexFor);
            //fwrite(buffer_hex,1, tmp, dest_file);
            fsWrite.Write(buffer_hex, 0, tmp);
            //fprintf(dest_file,"\n");
            buffer_hex = System.Text.Encoding.Default.GetBytes("\n");
            fsWrite.Write(buffer_hex, 0, 1);

            fsWrite.Close();
            fsWrite.Dispose();

            return 0;
        }
    }
}
