﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace uds_network_layer
{
    /* 为类扩展方法，扩展类要定义为static */
    public static class MyExtension
    {
        public static string HexToStrings(this byte[] Hex, string str)  
        {
            string hexString = string.Empty;

            if (Hex != null)
            {
                StringBuilder strB = new StringBuilder();

                for (int i = 0; i < Hex.Length; i++)
                {
                    strB.Append(Hex[i].ToString("X2"));
                }

                hexString = strB.ToString();
            }

            return hexString;
        }

        public static byte[] StringToHex(this string s)
        {
            if(s == null || s.Length <= 0)
            {
                return new byte[0];
            }

            byte[] byteArray = System.Text.Encoding.Default.GetBytes(s);
            return byteArray;
        }
    }
    
    /* 传输层类定义 */
    public class uds_trans
    {
        public enum AddressingModes
        {
            Physical_Addressing,    //物理寻址方式，一对一通信
            Functional_Addressing,  //功能寻址方式，广播通信
        }

        private int id;

        /// <summary>
        /// UDS默认填充字节
        /// </summary>
        public byte fill_byte = 0x55;
        public int tx_id;
        public int rx_id;
        public int test_id;

        /*
        ** Frame types arranged in numericalorder for efficient switch statement
        ** jump tables.
        */
        private enum FrameType
        {
            TX_SF = 0, /* Single Frame */
            TX_FF = 1, /* First Frame */
            TX_CF = 2, /* Consecutive Frame */
            TX_FC = 3  /* Flow Control Frame */
        };

        /*
        ** Masks for the PCI(Protcol Control Information) byte.
        ** The MS bit contains the frame type.
        ** The LS bit is mapped differently,depending on frame type, as follows:
        ** SF: DL (number of diagnostic bytes NOT including the PCI byte; only the
        ** 3 LS bits are used).
        ** FF: XDL (extended data length; always be 0.)
        ** CF: Sequence number,4 bits, max value:15.
        ** FC: Flow Status. The value of FS shall be set to zero that means that
        **     the tester is ready to receive a maximum number of CF.
        */
        private enum PCI    /* Don't change thesevalues, these must be  */
        {
            /* MS bits -  Frame Type */
            FRAME_TYPE_MASK = 0xF0,
            SF_TPDU = 0x00,             /* Single frame                 */
            FF_TPDU = 0x10,             /* First frame                  */
            CF_TPDU = 0x20,             /* Consecutive frame            */
            FC_TPDU = 0x30,             /* Flow control frame           */
            FC_OVFL_PDU = 0x32,         /* Flow control frame           */

            /* LS bits - SF_DL */
            SF_DL_MAX_BYTES = 0x07,     /* SF Max Data Length */
            SF_DL_MASK = 0x07,          /* number diagnostic data bytes */
            SF_DL_MASK_LONG = 0x0F,     /* change to check the 4 bits for testing, number diagnostic data bytes */

            /* LS bits - FF_DL */
            FF_EX_DL_MASK = 0x0F,       /*Extended data length         */

            /* LS bits - CF_SN */
            CF_SN_MASK = 0x0F,          /* Sequence number mask         */
            CF_SN_MAX_VALUE = 0x0F,     /* Max value of sequence number */

            /* LS bits - FC Saatus */
            FC_STATUS_CONTINUE = 0x00,  /* Flow control frame, CONTINUE */
            FC_STATUS_WAIT = 0x01,      /* Flow control frame, WAIT */
            FC_STATUS_OVERFLOW = 0x02,  /* Flow control frame, OVERFLOW */
            FC_STATUS_MASK = 0x0F,
        };


        private int N_As = 25;  //发送方数据帧经过数据链路层发送的时间（数据发出后对方链路层能收到数据的时间）
        private int N_Ar = 25;  //接收方数据帧经过数据链路层发送的时间

        private int N_Bs = 75;  //发送方接收流控制帧的等待时间
        private int N_Br;       //接收方发送流控制帧的间隔时间

        private int N_Cs;        //发送方发送连续帧的间隔时间
        private int N_Cr = 150;  //接收方接收连续帧的等待时间
        

        private int FC_BS_MAX_VALUE = 0;
        private int FC_ST_MIN_VALUE = 5;

        private int CF_SN_MAX_VALUE = 15;
        private int SF_DL_MAX_BYTES = 7;

        /*
        ** Time to wait for the tester to senda FC frame in response
        ** to a FF(wait for flow control frametime out).
        ** N_As + N_Bs = 25 +75 = 100ms
        */

        private int FC_WAIT_TIMEOUT;  //N_As +N_Bs + 50;


        /*
        ** wait for Consecutive frame time out
        ** N_Cr < 150ms
        */

        private int CF_WAIT_TIMEOUT;  //N_Cr; //(N_Cr- 10))
        private int RX_MAX_TP_BYTES = 0xFFF;

        public uds_trans()
        {
            can_rx_info.frame = new byte[8];
            can_tx_info.frame = new byte[8];

            FC_WAIT_TIMEOUT = N_As + N_Bs + 50; /* N_As + N_Bs + 50 */
            CF_WAIT_TIMEOUT = N_Cr;             /* (N_Cr - 10)) */
        }

        private readonly int beginning_seq_number = 1;
        private readonly int TPCI_Byte = 0;
        private readonly int DL_Byte = 1;
        private readonly int BS_Byte = 1;
        private readonly int STminByte = 2;

        private class tx_info
        {
            public bool tx_rx_idle = false;
            public bool tx_fc_tpdu = false;
            public bool tx_last_frame_error = false;
            public bool tx_wait_fc = false;
            public bool tx_in_progress = false;
            public int tx_block_size = 0;   /* BS(Block Size) in a flow Control Frame */

            public int tx_stmin_time = 20;
            public int tx_cf_stmin_wait_time = 20;   /* STmin Time in Flow Control Frame*/
            public int tx_fc_wait_time = 0;          /* Wait for FC when has sentFF */
            public int lenght;
            public int offset;
            public int next_seq_num;
            public byte[] buffer;
            public byte[] frame;
        }

        private class rx_info
        {
            public bool rx_in_progress = false;
            public bool rx_msg_rcvd = false;        /* if the message has never been received to be used by application level software */
            public bool tx_aborted = false;
            public int rx_cf_wait_time = 0;
            public bool rx_fc_wait_timeout_disable = false;
            public bool rx_overflow = false;
            public int lenght;
            public int offset;
            public int next_seq_num;
            public byte[] buffer;
            public byte[] frame;
        }

        private tx_info can_tx_info = new tx_info();
        private rx_info can_rx_info = new rx_info();

        #region Event
        public class FarmsEventArgs : EventArgs
        {
            public int id = 0;
            public int dlc = 0;
            public byte[] dat = new byte[8];
            public long time = 0;

            public override string ToString()
            {
                time %= 1000000;
                return id.ToString("X3") + " "
                + dlc.ToString("X1") + " "
                + dat.HexToStrings("") + " "
                + (time / 1000).ToString() + "." + (time % 1000).ToString("d3");
            }
        }

        public class RxMsgEventArgs : EventArgs
        {
            public int id = 0;
            public byte[] dat;
            public long time = 0;

            public RxMsgEventArgs(int lenght)
            {
                dat = new byte[lenght];
            }

            public override string ToString()
            {
                time %= 1000000;

                return id.ToString("X3") + " "

                + dat.HexToStrings(" ") + " "

                + (time / 1000).ToString() + "." + (time % 1000).ToString("d3");
            }
        }

        public class ErrorEventArgs : EventArgs
        {
            public string error;
            public override string ToString()
            {
                return error;
            }
        }



        /// <summary>
        /// UDS 传输层发送一帧事件
        /// </summary>

        public event EventHandler EventTxFarms;

        /// <summary>
        /// UDS 传输层接收一帧事件
        /// </summary>

        public event EventHandler EventRxFarms;

        /// <summary>
        /// UDS 传输层接收完成事件
        /// </summary>

        public event EventHandler EventRxMsgs;

        /// <summary>
        /// UDS 传输层错误事件
        /// </summary>

        public event EventHandler EventError;

        private void TxFarmsEvent(int id, byte[] dat, int dlc, long time)
        {
            if (EventTxFarms != null)
            {
                FarmsEventArgs e_args = new FarmsEventArgs();
                e_args.id = id;
                e_args.dlc = dlc;
                e_args.time = time;

                Array.Copy(dat, e_args.dat, dlc);
                EventTxFarms(this, e_args);
            }
        }

        private void RxFarmsEvent(int id, byte[] dat, int dlc, long time)
        {
            if (EventRxFarms != null)
            {
                FarmsEventArgs e_args = new FarmsEventArgs();
                e_args.id = id;
                e_args.dlc = dlc;
                e_args.time = time;

                Array.Copy(dat, e_args.dat, dlc);
                EventRxFarms(this, e_args);
            }
        }

        private void RxMsgEvent(int id, byte[] dat)
        {
            if (EventRxMsgs != null)
            {
                int lenght = dat.Length;
                RxMsgEventArgs e_rx_msg_args = new RxMsgEventArgs(lenght);
                e_rx_msg_args.id = id;

                Array.Copy(dat, e_rx_msg_args.dat, lenght);
                EventRxMsgs(this, e_rx_msg_args);
            }
        }

        private void RrrorEvent(string strings)
        {
            if (EventError != null)
            {
                ErrorEventArgs e_args = new ErrorEventArgs();
                e_args.error = strings;
                EventError(this, e_args);
            }
        }

        public delegate bool CanWriteData(int id, byte[] dat, int dlc, out long time);

        public delegate bool CanReadData(out int id, ref byte[] dat, out int dlc, out long time);



        /// <summary>
        /// 利用委托发送一帧数据
        /// </summary>

        public CanWriteData WriteData;

        /// <summary>
        /// 利用委托接收一帧数据
        /// </summary>

        public CanReadData ReadData;

        #endregion



        #region Trans Thread

        Thread testerPresent_thread;

        /* 该函数每隔3000ms 发送一次 0x02 0x3E 0x80 0x00 0x00 0x00 0x00 0x00 诊断仪在线报文 */
        private void testerPresent_Thread()
        {
            while (true)
            {
                long time;

                if (WriteData != null && WriteData(test_id, new byte[] { 0x02, 0x3E, 0x80, 0x00, 0x00, 0x00, 0x00, 0x00 }, 8, out time) == true)
                {
                    Thread.Sleep(3000);
                }
            }
        }

        private bool tester = false;

        /// <summary>
        /// 诊断保持
        /// </summary>

        public bool testerPresentCheckd
        {
            set
            {
                tester = value;

                if (tester)
                {
                    testerPresent_thread = new Thread(new ThreadStart(testerPresent_Thread));
                    testerPresent_thread.Start();
                }
                else
                {
                    if (testerPresent_thread != null && testerPresent_thread.IsAlive)
                    {
                        testerPresent_thread.Abort();
                    }
                }
            }

            get
            {
                return tester;
            }
        }

        Thread trans_thread;

        private void CanTrans_Thread( )
        {
            long oldTime = DateTime.Now.Ticks;

            while (true)
            {
                long cnt;
                int id;
                int dlc;
                long time;
                byte[] dat = new byte[8];

                while (true)
                {
                    bool rx_frame = false;

                    /* 读取一帧CAN报文 */
                    while (ReadData != null && ReadData(out id, ref dat, out dlc, out time) == true)
                    {
                        if (id == rx_id && dlc == 8)  //
                        {
                            if (can_rx_info.rx_in_progress && dat[0] == 0x02 && dat[1] == 0x7F && dat[3] == 0x78)  //如果收到的是0x02 0x7F 0x78 等待报文
                            {
                                can_rx_info.rx_cf_wait_time = 5000;  //设置接收等待时间为5000ms
                                break;
                            }

                            /* 如果诊断仪收到的报文为非等待报文 */
                            Array.Copy(dat, can_rx_info.frame, 8);
                            RxFarmsEvent(id, dat, dlc, time);
                            rx_frame = true; //接收到CAN报文的标志位置位
                            break;
                        }
                    }

                    long nowTime = DateTime.Now.Ticks;
                    cnt = nowTime - oldTime;

                    /* 如果程序运行时长超过1ms 或者 收到的CAN报文为非等待报文 */
                    if (cnt > 10000 || rx_frame)
                    {
                        oldTime = nowTime;
                        break;
                    }
                    else if (!can_tx_info.tx_in_progress && !can_rx_info.rx_in_progress)     //UDS空闲，释放进程into idle
                    {
                        Thread.Sleep(1);
                    }
                }

                CanTrans_Manage((int)(cnt + 5000) / 10000); //
            }
        }

        /// <summary>

        /// UDS 传输层开启

        /// </summary>

        public void Start()
        {
            trans_thread = new Thread(new ThreadStart(CanTrans_Thread));
            trans_thread.Priority = ThreadPriority.Highest;
            trans_thread.Start();
        }



        /// <summary>

        /// UDS 传输层关闭

        /// </summary>

        public void Stop()
        {
            if (trans_thread != null && trans_thread.IsAlive)
            {
                trans_thread.Abort();
            }

            testerPresentCheckd = false;
        }

        #endregion

        private byte[] tx_msg = new byte[0];

        /// <summary>
        /// 发送信息
        /// </summary>
        /// <paramname="mode"></param>
        /// <paramname="msg"></param>
        /// <returns></returns>
        public bool CanTrans_TxMsg(AddressingModes mode, byte[] msg)
        {
            if (msg.Length == 0)
            {
                RrrorEvent("-->Error:Tx Msg Length Is Zero");
                return false;
            }

            if (msg.Length > RX_MAX_TP_BYTES - 2)  //RX_MAX_TP_BYTES 0xFFFF
            {
                RrrorEvent("-->Error:Tx Msg Length > RX_MAX_TP_BYTES");
                return false;
            }

            if (tx_msg.Length != 0)
            {
                RrrorEvent("-->Error:Tx Msg ing");
                return false;
            }

            if (mode == AddressingModes.Physical_Addressing)
            {
                id = tx_id;  //physical address
            }
            else
            {
                id = test_id;  //functional address
            }

            tx_msg = msg;
            tx_msg = new byte[msg.Length];
            Array.Copy(msg, tx_msg, msg.Length);  //similar with function memcpy() in C language

            return true;
        }



        /// <summary>
        /// 发送信息
        /// </summary>
        /// <paramname="mode"></param>
        /// <paramname="strings"></param>
        /// <returns></returns>
        public bool CanTrans_TxMsg(AddressingModes mode, string strings)
        {
            return CanTrans_TxMsg(mode, strings.StringToHex());
        }

        private void CanTrans_TxMsg()
        {
            if (tx_msg.Length == 0)
            {
                return;
            }

            /*
            ** Set the tx_in_progress bit...it will be cleared when TX is done.
            */
            can_tx_info.tx_in_progress = true;
            can_tx_info.tx_last_frame_error = false;


            /*
            ** Assign fields in the controlstructure to initiate TX, then TX the
            ** appropriate frame type.
            */
            can_tx_info.offset = 0;
            can_tx_info.lenght = tx_msg.Length;
            can_tx_info.buffer = new byte[tx_msg.Length];

            Array.Copy(tx_msg, can_tx_info.buffer, can_tx_info.lenght);  //copy tx_msg data to can_tx_info.buffer
            can_tx_info.offset = 0;
            tx_msg = new byte[0];

            if (can_tx_info.lenght <= SF_DL_MAX_BYTES)  // if tx frame lenth <= 7, send signal frame data
            {
                CanTrans_TxFrame(FrameType.TX_SF);
            }
            else  // or else send first frame data
            {
                CanTrans_TxFrame(FrameType.TX_FF);
            }
        }


        private void CanTrans_TxFrame(FrameType frame_type)
        {
            int tx_farme_index = 0;
            int tx_data_bytes = 0;

            if (can_tx_info.tx_last_frame_error == false)
            {
                can_tx_info.frame = new byte[8] { fill_byte, fill_byte, fill_byte, fill_byte, fill_byte, fill_byte, fill_byte, fill_byte };

                /*
                ** Place control bytes into the frame.
                */
                switch (frame_type)
                {
                    case FrameType.TX_SF: /*single frame */
                        can_tx_info.frame[TPCI_Byte] = (byte)((byte)PCI.SF_TPDU | can_tx_info.lenght);
                        tx_data_bytes = can_tx_info.lenght;
                        tx_farme_index = 1;
                        break;

                    case FrameType.TX_FF: /* first frame */
                        can_tx_info.frame[TPCI_Byte] = (byte)((byte)PCI.FF_TPDU | (can_tx_info.lenght >> 8) & 0x0F);
                        can_tx_info.frame[DL_Byte] = (byte)(can_tx_info.lenght & 0xFF);
                        tx_data_bytes = SF_DL_MAX_BYTES - 1;
                        tx_farme_index = 2;
                        can_tx_info.next_seq_num = beginning_seq_number;
                        can_rx_info.rx_fc_wait_timeout_disable = false;
                        break;

                    case FrameType.TX_CF: /* conscutive frame */
                        can_tx_info.frame[TPCI_Byte] = (byte)((byte)PCI.CF_TPDU | can_tx_info.next_seq_num);
                        tx_farme_index = 1;
                        tx_data_bytes = (can_tx_info.lenght - can_tx_info.offset);

                        if (tx_data_bytes > SF_DL_MAX_BYTES)  //SF_DL_MAX_BYTES 7
                        {
                            tx_data_bytes = SF_DL_MAX_BYTES;
                        }

                        can_tx_info.next_seq_num = (can_tx_info.next_seq_num + 1) % (CF_SN_MAX_VALUE + 1);
                        break;


                    case FrameType.TX_FC: /* flow control frame */
                        if (can_rx_info.rx_overflow == true)
                        {
                            can_tx_info.frame[TPCI_Byte] = (byte)PCI.FC_OVFL_PDU;  //0x32
                        }
                        else
                        {
                            can_tx_info.frame[TPCI_Byte] = (byte)PCI.FC_TPDU;  //0x30
                        }

                        can_tx_info.frame[BS_Byte] = (byte)FC_BS_MAX_VALUE;
                        can_tx_info.frame[STminByte] = (byte)FC_ST_MIN_VALUE;
                        tx_data_bytes = 0;
                        break;

                    default:
                        return;
                }

                while (tx_data_bytes != 0)
                {
                    /* can_tx_info.frame is packed frame of each sent, can_tx_info.buffer is data buffer storing data to be sent */
                    can_tx_info.frame[tx_farme_index++] = can_tx_info.buffer[can_tx_info.offset++]; // packing sent data 
                    tx_data_bytes--;
                }
            }

            long time;

            if (WriteData != null && WriteData(id, can_tx_info.frame, 8, out time) == true)
            {
                TxFarmsEvent(id, can_tx_info.frame, 8, time);
                can_tx_info.tx_last_frame_error = false;
                can_rx_info.frame[TPCI_Byte] = 0;

                /*
                ** Verify if the data has been completely transfered. If not, set flag to
                ** transfer CF frames. (For FCframes, s_cantp_tx_info is not used and there
                ** should not be a CF frameafter a FC frame.)
                */
                if (can_tx_info.lenght > can_tx_info.offset && frame_type != FrameType.TX_FC)  // if tx frame is FF or CF
                {
                    can_tx_info.tx_in_progress = true;

                    if (frame_type == FrameType.TX_FF)  // if tx frame is FF
                    {
                        can_tx_info.tx_wait_fc = true;  // 等待流控制帧标志位置位
                        can_tx_info.tx_fc_wait_time = FC_WAIT_TIMEOUT; /* start flow control wait timer */
                    }
                }
                else  // if tx frame is SF or FC
                {
                    can_tx_info.tx_in_progress = false;
                }
            }
            else
            {
                /* user specific action in case transmission request is not successful */
                can_tx_info.tx_last_frame_error = true;
            }
        }

        private void CanTrans_Manage(int tick)
        {
            CanTrans_TxMsg();  //发送CAN数据函数
            CanTrans_Counter(tick);  //等待时间计算函数

            /*
            ** If new message has been received, process it.
            */
            if (can_rx_info.frame[TPCI_Byte] != 0)
            {
                CanTrans_RxStateAnalyse();  //对收到的CAN数据进行处理

                /* clear first rx frame byte to check a new frame next time */
                can_rx_info.frame[TPCI_Byte] = 0;
            }

            if (can_tx_info.tx_in_progress && !can_tx_info.tx_wait_fc)  //如果发送的是连续帧
            {
                /* block size允许一次连续发送CF的数量 */
                if (0x00 == can_tx_info.tx_block_size)
                {
                    /* st_min time, received from tester */
                    if (0x00 == can_tx_info.tx_stmin_time)  //如果连续帧发送间隔为0ms，直接发送连续帧
                    {
                        CanTrans_TxFrame(FrameType.TX_CF);
                    }
                    else
                    {
                        /* st_min time, received from tester is not 0 */
                        if (0x00 == can_tx_info.tx_cf_stmin_wait_time)  //如果连续帧发送间隔不为0ms，需要等到定时时间到才能发送连续帧数据
                        {
                            CanTrans_TxFrame(FrameType.TX_CF);
                            can_tx_info.tx_cf_stmin_wait_time = can_tx_info.tx_stmin_time;
                        }
                    }
                }
                else if (can_tx_info.tx_block_size > 1)
                {
                    if (0x00 == can_tx_info.tx_stmin_time)
                    {
                        CanTrans_TxFrame(FrameType.TX_CF);

                        if (!can_tx_info.tx_last_frame_error)
                        {
                            can_tx_info.tx_block_size--;
                        }
                    }
                    else
                    {
                        if (0x00 == can_tx_info.tx_cf_stmin_wait_time)
                        {
                            CanTrans_TxFrame(FrameType.TX_CF);

                            if (!can_tx_info.tx_last_frame_error)
                            {
                                can_tx_info.tx_block_size--;
                            }

                            /* start stmin time, interval of consecutive frame */
                            can_tx_info.tx_cf_stmin_wait_time = can_tx_info.tx_stmin_time;
                        }
                    }

                    if (can_tx_info.tx_block_size <= 1)
                    {
                        can_tx_info.tx_wait_fc = true;

                        /* start flow control wait timer */
                        can_tx_info.tx_fc_wait_time = FC_WAIT_TIMEOUT;
                    }
                }
            }
            else if (can_tx_info.tx_fc_tpdu)  //如果刚刚收到的是FF，则需要发送FC
            {
                CanTrans_TxFrame(FrameType.TX_FC);
                can_tx_info.tx_fc_tpdu = false;

                /*start to counter the CF wait time*/
                can_rx_info.rx_cf_wait_time = CF_WAIT_TIMEOUT;
            }

            if (can_tx_info.tx_in_progress && can_tx_info.tx_wait_fc) //如果发送的是首帧
            {
                /* wait for flow control frame time out! */
                if (can_tx_info.tx_fc_wait_time == 0)  //在tx_fc_wait_time时间内没有收到流控制帧
                {
                    can_tx_info.tx_in_progress = false;
                    can_tx_info.tx_wait_fc = false;
                    can_tx_info.tx_last_frame_error = false;
                    RrrorEvent("-->Error: Wait For Flow Control Frame Time Out");
                }
            }

            /* 如果收到首帧且已经发送完流控制帧 */
            if (can_rx_info.rx_in_progress == true && !can_tx_info.tx_fc_tpdu)
            {
                if (0x00 == can_rx_info.rx_cf_wait_time)
                {
                    can_rx_info.tx_aborted = true;

                    /*
                    ** wait for consecutive frame Time out, abort Rx.
                    */
                    can_rx_info.rx_in_progress = false;

                    /*
                    ** When Time out occurs,ECU has to send negative
                    ** resp(71) for the firstframe.First frame is already copied in to
                    ** g_cantp_can_rx_info.msgbuffer but message length is not yet copied.
                    ** So assign data length asFirst Frame length and set RX_MSG_RCVD
                    ** flag.This flag indicatesto a new message has come.
                    */

                    can_rx_info.lenght = SF_DL_MAX_BYTES - 1;
                    can_rx_info.rx_msg_rcvd = true;
                    RrrorEvent("-->Error: Ecu Tx Aborted");
                }
            }

            if (can_rx_info.rx_msg_rcvd == true)
            {
                can_rx_info.rx_msg_rcvd = false;

                if (can_rx_info.tx_aborted == false)
                {
                    RxMsgEvent(rx_id, can_rx_info.buffer);
                }

                can_rx_info.tx_aborted = false;
            }
        }


        private void CanTrans_Counter(int tick)
        {
            /* interval of consecutive frame, STmin = 10ms, separation time */
            if (can_tx_info.tx_cf_stmin_wait_time > 0)  //连续帧发送间隔定时计数器
            {
                if (can_tx_info.tx_cf_stmin_wait_time > tick)
                {
                    can_tx_info.tx_cf_stmin_wait_time -= tick;
                }
                else
                {
                    can_tx_info.tx_cf_stmin_wait_time = 0;
                }
            }

            /* N_Bs, flow control frame wait time out, 75ms*/
            if (can_tx_info.tx_fc_wait_time > 0)
            {
                if (can_tx_info.tx_fc_wait_time > tick)
                {
                    can_tx_info.tx_fc_wait_time -= tick;
                }
                else
                {
                    can_tx_info.tx_fc_wait_time = 0;
                    can_rx_info.rx_fc_wait_timeout_disable = true;
                }
            }

            /* N_Cr,consecutive frame wait timeout, 75ms*/
            if (can_rx_info.rx_cf_wait_time > tick)
            {
                can_rx_info.rx_cf_wait_time -= tick;
            }
            else
            {
                can_rx_info.rx_cf_wait_time = 0;
            }
        }

        private void CanTrans_RxStateAnalyse( )
        {
            PCI flow_control_sts;
            int data_length = 0x00;

            /* single frame */
            if ((can_rx_info.frame[TPCI_Byte] & (byte)PCI.FRAME_TYPE_MASK) == (byte)PCI.SF_TPDU)
            {
                /* 
                ** As per 15765-2 network layerspec when SF_DL is 0 or greater
                ** than 7, just ignore it.
                */
                data_length = (can_rx_info.frame[TPCI_Byte] & (byte)PCI.SF_DL_MASK_LONG);
                can_tx_info.tx_in_progress = false;
                can_tx_info.tx_wait_fc = false;
                can_rx_info.rx_in_progress = false;
                can_tx_info.tx_last_frame_error = false;

                if ((data_length == 0) || (data_length > SF_DL_MAX_BYTES))
                {
                    return;
                }

                can_rx_info.lenght = data_length;
                can_rx_info.buffer = new byte[can_rx_info.lenght];

                /*
                ** Copy the frame to the RXbuffer. Clear the RX_IN_PROGRESS bit
                ** (SF frame) will abortmulti-frame transfer.
                */
                Array.Copy(can_rx_info.frame, 1, can_rx_info.buffer, 0, can_rx_info.lenght);
                can_rx_info.rx_msg_rcvd = true;
            }
            /* first frame */
            else if ((can_rx_info.frame[TPCI_Byte] & (byte)PCI.FRAME_TYPE_MASK) == (byte)PCI.FF_TPDU)
            {
                data_length = ((int)(can_rx_info.frame[TPCI_Byte] & (byte)PCI.FF_EX_DL_MASK) << 8)
                + can_rx_info.frame[DL_Byte];

                can_rx_info.rx_fc_wait_timeout_disable = false;
                can_rx_info.lenght = data_length;
                can_rx_info.buffer = new byte[can_rx_info.lenght];

                /*
                ** Clear the RX buffer, copy first frame to RX buffer and initiate RX.
                */
                Array.Copy(can_rx_info.frame, 2, can_rx_info.buffer, 0, SF_DL_MAX_BYTES - 1);
                can_rx_info.next_seq_num = beginning_seq_number;
                can_rx_info.offset = SF_DL_MAX_BYTES - 1;
                can_tx_info.tx_in_progress = false;
                can_tx_info.tx_wait_fc = false;
                can_rx_info.rx_in_progress = true;

                /* set flag to send flow control frame */
                can_tx_info.tx_fc_tpdu = true;  //准备发送FC
            }
            /* Consecutive Frame */
            else if ((((can_rx_info.frame[TPCI_Byte] & (byte)PCI.FRAME_TYPE_MASK) == (byte)PCI.CF_TPDU)
            /* Don't accept consecutiveframe until flow control frame sent by ECU */ 
                       && (!can_tx_info.tx_fc_tpdu)
            /* Don't accept consecutive frame if we are sending CF */
                       && (!can_tx_info.tx_in_progress)) )
            {
                /*
                ** Ignore frame unless RX inprogress.
                */
                if (can_rx_info.rx_in_progress)
                {
                    /*
                    ** Verify the sequence number is as expected.
                    */
                    if ((can_rx_info.frame[TPCI_Byte] & (byte)PCI.CF_SN_MASK) == can_rx_info.next_seq_num)
                    {
                        data_length = can_rx_info.lenght - can_rx_info.offset;

                        /*
                        **  Last frame in message ?
                        */
                        if (data_length <= SF_DL_MAX_BYTES)
                        {
                            Array.Copy(can_rx_info.frame, 1, can_rx_info.buffer, can_rx_info.offset, data_length);
                            can_rx_info.rx_in_progress = false;
                            can_rx_info.rx_msg_rcvd = true;
                        }
                        else
                        {
                            /*
                            ** not the last frame,copy bytes to RX buffer and
                            ** continue RXing.
                            */
                            Array.Copy(can_rx_info.frame, 1, can_rx_info.buffer, can_rx_info.offset, SF_DL_MAX_BYTES);
                            can_rx_info.next_seq_num = (can_rx_info.next_seq_num + 1) % (CF_SN_MAX_VALUE + 1);
                            can_rx_info.offset += SF_DL_MAX_BYTES;
                            can_rx_info.rx_cf_wait_time = CF_WAIT_TIMEOUT;
                        }
                    }
                    else
                    {
                        /*
                        ** Invalid sequence number...abort Rx.Asa diagnostic measure,
                        ** consideration wasgiven to send an FC frame here, but not done.
                        */
                        can_rx_info.rx_in_progress = false;

                        /*
                        ** When Invalidsequence number is received, ECU has to send
                        ** negative resp forthe first frame.so set RX_MSG_RCVD flag.
                        ** This flag indicatesto DiagManager as new message has come.
                        */
                        can_rx_info.tx_aborted = true;
                        can_rx_info.rx_msg_rcvd = true;
                        RrrorEvent("-->Error: Ecu Invalid Sequence Number");
                    }
                }
            }
            /* flow control frame */
            else if ((can_rx_info.frame[TPCI_Byte] & (byte)PCI.FRAME_TYPE_MASK) == (byte)PCI.FC_TPDU)
            {
                if (can_tx_info.tx_wait_fc)
                {
                    /*
                    ** Receive Flow Status(FS) forTransmiting the CF Frames.
                    ** The value of FS shall be setto zero that means that the
                    ** tester is ready to receive amaximum number of CF.
                    */
                    flow_control_sts = PCI.FC_STATUS_CONTINUE;

                    if ((can_rx_info.frame[TPCI_Byte] & (byte)PCI.FC_STATUS_MASK) != 0x00)
                    {
                        /*
                        ** Flow Status(FS)
                        ** 0: Continue to send(CTS)
                        ** 1: wait(WT)
                        ** 2: Overflow (OVFLW)
                        */
                        flow_control_sts = (PCI)(can_rx_info.frame[TPCI_Byte] & (byte)PCI.FC_STATUS_MASK);
                    }

                    /*
                    ** Receive the BS and STmin time for Transmiting the CF Frames.
                    */
                    if (can_rx_info.frame[BS_Byte] != 0x00)
                    {
                        can_tx_info.tx_block_size = can_rx_info.frame[BS_Byte] + 1;
                    }
                    else
                    {
                        can_tx_info.tx_block_size = 0x00;
                    }

                    if ((can_rx_info.frame[STminByte] & 0x7F) != 0x00)
                    {
                        /*
                        ** Valid Range forSTMin timeout is 0 - 127ms.
                        */
                        can_tx_info.tx_stmin_time = (can_rx_info.frame[STminByte] & 0x7F) + 5;   /* extend the delay time */
                    }
                    else
                    {
                        can_tx_info.tx_stmin_time = 20;
                    }

                    if ((flow_control_sts == PCI.FC_STATUS_CONTINUE) && (can_rx_info.rx_fc_wait_timeout_disable == false))
                    {
                        can_tx_info.tx_wait_fc = false;
                        can_tx_info.tx_fc_wait_time = 0;
                    }
                    else if (flow_control_sts == PCI.FC_STATUS_WAIT)
                    {
                        can_tx_info.tx_fc_wait_time = FC_WAIT_TIMEOUT;  /* if wait, we will wait another time */
                    }
                    else if (flow_control_sts == PCI.FC_STATUS_OVERFLOW)
                    {
                        /* 
                        ** do nothing here, ifover flow, we will stop sending
                        ** any message until we got new cmd 
                        */
                        can_tx_info.tx_fc_wait_time = 1;   /* exit after 10ms */
                        RrrorEvent("-->Error: Ecu Buff Over Flow");
                    }
                    else
                    {
                        /* 
                        ** do nothing here, ifover flow, we will stop sending
                        ** any message until we got new cmd 
                        */
                        can_tx_info.tx_fc_wait_time = 1;  /* exit after 10ms */
                    }
                }
            }
        }
    }
}
