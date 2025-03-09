using System;
using System.Collections;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace B3Project
{

    public class RawImageDataDecoder : DataDecoder<DecodedData>
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

            await ReadEnsurely(stream, intBuffer, 0, 4, 100);
            int imgSize = BitConverter.ToInt32(intBuffer, 0);

            byte[] imgBuffer = new byte[imgSize];
            await ReadEnsurely(stream, imgBuffer, 0, imgSize, 100);

            return new DecodedData(RawImageData.DATA_TYPE, new RawImageData(cameraID, type, uuid, width, height, imgBuffer));
        }
    }

    public class RawImageData : CameraData
    {
        public const string DATA_TYPE = "RawImageData";
        public const int TYPE_BACKGROUND_IMAGE = 0;
        public const int TYPE_BACKGROUND_DEPTH = 1;
        public const int TYPE_FOREGROUND_IMAGE = 2;
        public const int TYPE_FOREGROUND_DEPTH = 3;

        public int Type { get; }
        public string UUID { get; }

        public int Width { get; }
        public int Height { get; }
        public byte[] ImageBuffer { get; }

        public RawImageData(string cameraID, int type, string uuid, int width, int height, byte[] imageBuffer) : base(cameraID)
        {
            this.Type = type;
            this.UUID = uuid;
            this.Width = width;
            this.Height = height;
            this.ImageBuffer = imageBuffer;
        }
    }
}