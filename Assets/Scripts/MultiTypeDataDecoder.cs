using System;
using System.Collections;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace B3Project
{
    public class DecodedData
    {
        public DecodedData(string dataType,object data) 
        {
            this.dataType = dataType;
            this.data = data;
        }

        private string dataType;
        public string DataType { get { return dataType; } }
        private object data;
        public T GetData<T>() where T : class
        {
            return data as T;
        }
    }

    public class MultiTypeDataDecoder : DataDecoder<DecodedData>
    {

        private Dictionary<string, DataDecoder<DecodedData>> decoderMap;
        public MultiTypeDataDecoder(Dictionary<string, DataDecoder<DecodedData>> decoderMap)
        {
            this.decoderMap = decoderMap;
        }


        public override async Task<DecodedData> Accept(NetworkStream stream)
        {
            byte[] intBuffer = new byte[4];
            await ReadEnsurely(stream, intBuffer, 0, 4, 100);
            int typeLength = BitConverter.ToInt32(intBuffer, 0);
            byte[] typeBytes = new byte[typeLength];
            await ReadEnsurely(stream, typeBytes, 0, typeLength, 100);
            string type = Encoding.UTF8.GetString(typeBytes);

            DataDecoder<DecodedData> decoder;
            decoderMap.TryGetValue(type, out decoder);
            if (decoder == null)
            {
                //TODO ÉGÉâÅ[ÇèoÇ≥Ç∏Ç…ì«Ç›îÚÇŒÇ∑ÇÊÇ§Ç…ÇµÇΩÇ¢
                //typeLength(4) type(typeLength) size(4) ÇÃÇÊÇ§Ç…Ç∑ÇÍÇŒâåà
                throw new Exception("Unknown data type received:" + type);
            }
            else
            {
                return await decoder.Accept(stream);
            }
        }
    }
}
