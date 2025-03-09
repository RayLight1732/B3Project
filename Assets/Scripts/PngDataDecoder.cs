using System;
using System.Collections;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace B3Project
{

    public class PngDataDecoder : DataDecoder<DecodedData>
    {
        public override async Task<DecodedData> Accept(NetworkStream stream)
        {
            /*
             * ‚Æ‚è‚ ‚¦‚¸little endianŒÀ’è
             * int idLength
             * byte[idLength] id
             * int type
             * int uuidLength
             * byte[uuidLength] uuid
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
            Debug.Log("cameraID:"+cameraID);

            await ReadEnsurely(stream, intBuffer, 0, 4, 100);
            int type = BitConverter.ToInt32(intBuffer, 0);
            Debug.Log("type:"+type);
            await ReadEnsurely(stream, intBuffer, 0, 4, 100);
            int uuidLength = BitConverter.ToInt32(intBuffer, 0);
            byte[] uuidBytes = new byte[uuidLength];
            await ReadEnsurely(stream, uuidBytes, 0, uuidLength, 100);
            string uuid = Encoding.UTF8.GetString(uuidBytes);
            Debug.Log("uuid:"+uuid);

            await ReadEnsurely(stream, intBuffer, 0, 4, 100);
            int imgSize = BitConverter.ToInt32(intBuffer, 0);
            Debug.Log("img size:" + imgSize);
            byte[] imgBuffer = new byte[imgSize];
            await ReadEnsurely(stream, imgBuffer, 0, imgSize, 100);

            return new DecodedData(PngData.DATA_TYPE,new PngData(cameraID,type,uuid,imgBuffer));
        }
    }

    public class PngData:CameraData
    {
        public const string DATA_TYPE = "PngData";
        public const int TYPE_BACKGROUND_IMAGE = 0;
        public const int TYPE_BACKGROUND_DEPTH = 1;
        public const int TYPE_FOREGROUND_IMAGE = 2;
        public const int TYPE_FOREGROUND_DEPTH = 3;

        public int Type { get; }
        public string UUID { get; }
        public byte[] ImageBuffer { get; }

        public PngData(string cameraID,int type, string uuid, byte[] imageBuffer):base(cameraID)
        {
            this.Type = type;
            this.UUID = uuid;
            this.ImageBuffer = imageBuffer;
        }
    }
}
