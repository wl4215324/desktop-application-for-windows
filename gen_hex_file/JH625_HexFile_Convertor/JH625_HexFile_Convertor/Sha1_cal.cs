using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JH625_HexFile_Convertor
{
    class Sha1_cal
    {
        public UInt16 SHA1HashSize = 20;
        public struct SHA1Context
        {
            public UInt32[] Intermediate_Hash; // = new UInt32[SHA1HashSize / 4];  /* Message Digest */
            public UInt32 Length_Low;  /* Message length in bits */
            public UInt32 Length_High; /* Message length in bits */ 
            public UInt16 Message_Block_Index; /* Index into message block array */
            public byte[] Message_Block; //[64];  /* 512-bit message blocks */
            public int Computed;  /* Is the digest computed? */
            public int Corrupted; /* Is the message digest corrupted? */
        }

        private SHA1Context sha1_context;

        public Sha1_cal( )
        {
            sha1_context.Intermediate_Hash = new UInt32[SHA1HashSize / 4];
            sha1_context.Message_Block = new byte[64];
        }

        public int SHA1Reset( )  //初始化状态
        {
            sha1_context.Length_Low = 0;
            sha1_context.Length_High = 0;
            sha1_context.Message_Block_Index = 0;
            sha1_context.Intermediate_Hash[0] = 0x67452301;  //取得的HASH结果（中间数据）
            sha1_context.Intermediate_Hash[1] = 0xEFCDAB89;
            sha1_context.Intermediate_Hash[2] = 0x98BADCFE;
            sha1_context.Intermediate_Hash[3] = 0x10325476;
            sha1_context.Intermediate_Hash[4] = 0xC3D2E1F0;
            sha1_context.Computed = 0;
            sha1_context.Corrupted = 0;

            return 0;
        }

        private UInt32 SHA1CircularShift(byte bits, UInt32 word)
        {
            return (((word) << (bits)) | ((word) >> (32 - (bits))));
        }

        public void SHA1ProcessMessageBlock( )
        {
            UInt32[] K = new UInt32[4] { /* Constants defined in SHA-1 */
		                                   0x5A827999,
                                           0x6ED9EBA1,
                                           0x8F1BBCDC,
                                           0xCA62C1D6
                                       };

            UInt32 t; /* Loop counter */
            UInt32 temp; /* Temporary word value */
            UInt32[] W = new UInt32[80]; /* Word sequence */
            UInt32 A, B, C, D, E; /* Word buffers */

            /*
            * Initialize the first 16 words in the array W
            */
            for (t = 0; t < 16; t++)
            {
                W[t] = (UInt32)(sha1_context.Message_Block[t * 4] << 24);
                W[t] |= (UInt32)(sha1_context.Message_Block[t * 4 + 1] << 16);
                W[t] |= (UInt32)(sha1_context.Message_Block[t * 4 + 2] << 8);
                W[t] |= (UInt32)(sha1_context.Message_Block[t * 4 + 3]);
            }

            for (t = 16; t < 80; t++)
            {
                W[t] = SHA1CircularShift(1, W[t - 3] ^ W[t - 8] ^ W[t - 14] ^ W[t - 16]);
            }

            A = sha1_context.Intermediate_Hash[0];
            B = sha1_context.Intermediate_Hash[1];
            C = sha1_context.Intermediate_Hash[2];
            D = sha1_context.Intermediate_Hash[3];
            E = sha1_context.Intermediate_Hash[4];

            for (t = 0; t < 20; t++)
            {
                temp = SHA1CircularShift(5, A) +
                    ((B & C) | ((~B) & D)) + E + W[t] + K[0];
                E = D;
                D = C;
                C = SHA1CircularShift(30, B);
                B = A;
                A = temp;
            }

            for (t = 20; t < 40; t++)
            {
                temp = SHA1CircularShift(5, A) +
                    (B ^ C ^ D) + E + W[t] + K[1];
                E = D;
                D = C;
                C = SHA1CircularShift(30, B);
                B = A;
                A = temp;
            }

            for (t = 40; t < 60; t++)
            {
                temp = SHA1CircularShift(5, A) + 
                    ((B & C) | (B & D) | (C & D)) + E + W[t] + K[2];
                E = D;
                D = C;
                C = SHA1CircularShift(30, B);
                B = A;
                A = temp;
            }

            for (t = 60; t < 80; t++)
            {
                temp = SHA1CircularShift(5, A) + 
                    (B ^ C ^ D) + E + W[t] + K[3];
                E = D;
                D = C;
                C = SHA1CircularShift(30, B);
                B = A;
                A = temp;
            }

            sha1_context.Intermediate_Hash[0] += A;
            sha1_context.Intermediate_Hash[1] += B;
            sha1_context.Intermediate_Hash[2] += C;
            sha1_context.Intermediate_Hash[3] += D;
            sha1_context.Intermediate_Hash[4] += E;
            sha1_context.Message_Block_Index = 0;
        }

        public int SHA1Input(byte[] message_array, UInt32 length)
        {
            UInt16 i = 0;

            if ((message_array == null) || (message_array.Length <= 0))
                return 0;

	        if (sha1_context.Computed != 0)
	        {
		        return -1;
	        }

	        if (sha1_context.Corrupted != 0)
	        {
		        return -1;
	        }

	        while((length-- > 0) && (sha1_context.Corrupted == 0))
	        {
                sha1_context.Message_Block[sha1_context.Message_Block_Index++] = (byte)(message_array[i] & 0xFF);
                sha1_context.Length_Low += 8;

		        if (sha1_context.Length_Low == 0)
		        {
                    sha1_context.Length_High++;

			        if (sha1_context.Length_High == 0)
			        {
                        /* Message is too long */
                        sha1_context.Corrupted = 1;
			        }
		        }

		        if (sha1_context.Message_Block_Index == 64)
		        {

                    SHA1ProcessMessageBlock();
		        }

		        i++;
	        }

	        return 0;
        }


        void SHA1PadMessage( )
        {
            /*
            * Check to see if the current message block is too small to hold
            * the initial padding bits and length. If so, we will pad the
            * block, process it, and then continue padding into a second
            * block.
            */
            if(sha1_context.Message_Block_Index > 55)
            {
                sha1_context.Message_Block[sha1_context.Message_Block_Index++] = 0x80;

                while(sha1_context.Message_Block_Index < 64)
                {
                    sha1_context.Message_Block[sha1_context.Message_Block_Index++] = 0;
                }

                SHA1ProcessMessageBlock();

                while (sha1_context.Message_Block_Index < 56)
                {
                    sha1_context.Message_Block[sha1_context.Message_Block_Index++] = 0;
                }
            }
            else
            {
                sha1_context.Message_Block[sha1_context.Message_Block_Index++] = 0x80;

                while(sha1_context.Message_Block_Index < 56)
                {
                    sha1_context.Message_Block[sha1_context.Message_Block_Index++] = 0;
                }
            }

            /*
            * Store the message length as the last 8 octets
            */
            sha1_context.Message_Block[56] = (byte)(sha1_context.Length_High >> 24);
            sha1_context.Message_Block[57] = (byte)(sha1_context.Length_High >> 16);
            sha1_context.Message_Block[58] = (byte)(sha1_context.Length_High >> 8);
            sha1_context.Message_Block[59] = (byte)sha1_context.Length_High;
            sha1_context.Message_Block[60] = (byte)(sha1_context.Length_Low >> 24);
            sha1_context.Message_Block[61] = (byte)(sha1_context.Length_Low >> 16);
            sha1_context.Message_Block[62] = (byte)(sha1_context.Length_Low >> 8);
            sha1_context.Message_Block[63] = (byte)sha1_context.Length_Low;
            SHA1ProcessMessageBlock();
        }

        public int SHA1Result(ref byte[] Message_Digest) //[SHA1HashSize]
        {
            int i;

            if((Message_Digest == null) || (Message_Digest.Length == 0))
            {
                return -1;
            }

            if (sha1_context.Corrupted != 0)
            {
                return sha1_context.Corrupted;
            }

            if (sha1_context.Computed == 0)
            {
                SHA1PadMessage();

                for (i = 0; i < 64; ++i)
                {
                    /* message may be sensitive, clear it out */
                    sha1_context.Message_Block[i] = 0;
                }
                sha1_context.Length_Low = 0; /* and clear length */
                sha1_context.Length_High = 0;
                sha1_context.Computed = 1;
            }

            for (i = 0; i < SHA1HashSize; ++i)
            {
                Message_Digest[i] = (byte) (sha1_context.Intermediate_Hash[i >> 2] >> 8 * (3 - (i & 0x03)));
            }

            return 0;
        }
    }
}
