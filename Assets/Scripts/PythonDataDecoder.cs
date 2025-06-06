using System;
using System.Collections;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace B3Project
{

    public class PythonDataDecoder : DataDecoder<ReceiveImage>
    {
        public override async Task<ReceiveImage> Accept(NetworkStream stream)
        {
            /*
             * とりあえずlittle endian限定
             * int idLength
             * byte[idLength] id
             * int type
             * int uuidLength
             * byte[uuidLength] uuid
             * int width
             * int height
             * int imageSize
             * byte[imgSize] image
             * 
             */

            byte[] intBuffer = new byte[4];

            await ReadEnsurely(stream, intBuffer, 0, 4, 100);
            int cameraIDLength = BitConverter.ToInt32(intBuffer, 0);
            byte[] idBytes = new byte[cameraIDLength];
            await ReadEnsurely(stream, idBytes, 0, cameraIDLength, 100);
            string cameraID = Encoding.UTF8.GetString(idBytes);
            
            await ReadEnsurely(stream, intBuffer, 0, 4, 100);
            int type = BitConverter.ToInt32(intBuffer, 0);

            await ReadEnsurely(stream, intBuffer, 0, 4, 100);
            int uuidLength = BitConverter.ToInt32(intBuffer, 0);
            byte[] uuidBytes = new byte[uuidLength];
            await ReadEnsurely(stream, uuidBytes, 0, uuidLength, 100);
            string uuid = Encoding.UTF8.GetString(uuidBytes);

            await ReadEnsurely(stream, intBuffer, 0, 4, 100);
            int width = BitConverter.ToInt32(intBuffer, 0);
            await ReadEnsurely(stream, intBuffer, 0, 4, 100);
            int height = BitConverter.ToInt32(intBuffer, 0);

            int imgSize = BitConverter.ToInt32(intBuffer, 0);
            byte[] imgBuffer = new byte[imgSize];
            await ReadEnsurely(stream, imgBuffer, 0, imgSize, 100);
            
            return new ReceiveImage(cameraID,type,uuid,width,height,imgBuffer);
        }
        
        
    }

    public class ReceiveImage
    {
        public const int TYPE_BACKGROUND_IMAGE = 0;
        public const int TYPE_BACKGROUND_DEPTH = 1;
        public const int TYPE_FOREGROUND_IMAGE = 2;
        public const int TYPE_FOREGROUND_DEPTH = 3;

        private string cameraID;
        public string CameraID { get { return cameraID; } }

        private int type;
        public int Type { get { return type; } }

        private string uuid;

        public string UUID { get { return uuid; } }

        private int width;
        public int Width { get { return width; } }

        private int height;
        public int Height { get { return height; } }

        private byte[] imageBuffer;
        public byte[] ImageBuffer { get { return imageBuffer; } }


        public ReceiveImage(string cameraID,int type,string uuid,int width,int height,byte[] imageBuffer)
        {
            this.cameraID = cameraID;
            this.type = type;
            this.uuid = uuid;
            this.width = width;
            this.height = height;
            this.imageBuffer = imageBuffer;
        }


    }

}