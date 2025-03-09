using System;
using System.Collections;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using Unity.VisualScripting.FullSerializer;
using UnityEngine;

namespace B3Project
{

    public class SizeDataDecoder : DataDecoder<DecodedData>
    {
        public override async Task<DecodedData> Accept(NetworkStream stream)
        {
            /*
             * ‚Æ‚è‚ ‚¦‚¸little endianŒÀ’è
             * int idLength
             * byte[idLength] id
             * int width
             * int height
             * 
             */
            byte[] intBuffer = new byte[4];

            await ReadEnsurely(stream, intBuffer, 0, 4, 100);
            int cameraIDLength = BitConverter.ToInt32(intBuffer, 0);
            byte[] idBytes = new byte[cameraIDLength];
            await ReadEnsurely(stream, idBytes, 0, cameraIDLength, 100);
            string cameraID = Encoding.UTF8.GetString(idBytes);

            await ReadEnsurely(stream, intBuffer, 0, 4, 100);
            int width = BitConverter.ToInt32(intBuffer, 0);
            await ReadEnsurely(stream, intBuffer, 0, 4, 100);
            int height = BitConverter.ToInt32(intBuffer, 0);

            return new DecodedData(SizeData.DATA_TYPE,new SizeData(cameraID,width,height));
        }
    }

    public class SizeData:CameraData
    {

        public const string DATA_TYPE = "SizeData";

        
        public int Width { get; }
        public int Height { get; }
        public SizeData(string cameraID, int width, int height):base(cameraID)
        {
            this.Width = width;
            this.Height = height;
        }
    }
}
