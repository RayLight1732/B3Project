using System;
using System.Collections;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Threading.Tasks;
using UnityEngine;

namespace B3Project
{

    public class PythonDataDecoder : DataDecoder<ImageAndDepth>
    {
        public override async Task<ImageAndDepth> Accept(NetworkStream stream)
        {
            /*
             * �Ƃ肠����little endian����
             * int imageSize
             * byte[imgSize] image
             * int width
             * int height
             * float[width*height] depth
             * 
             */


            byte[] intBuffer = new byte[4];
            await ReadEnsurely(stream, intBuffer, 0, 4, 100);
            int imgSize = BitConverter.ToInt32(intBuffer, 0);
            //Debug.Log($"imgSize:{imgSize}");
            byte[] imgBuffer = new byte[imgSize];
            await ReadEnsurely(stream, imgBuffer, 0, imgSize, 100);
            //Debug.Log("end read image");

            //depth�ǂݍ���
            await ReadEnsurely(stream, intBuffer, 0, 4, 100);
            int width = BitConverter.ToInt32(intBuffer, 0);
            //Debug.Log($"Depth Width {width}");
            await ReadEnsurely(stream, intBuffer, 0, 4, 100);
            int height = BitConverter.ToInt32(intBuffer, 0);
            //Debug.Log($"Depth Height {height}");

            byte[] depthBuffer = new byte[width*height*4];
            await ReadEnsurely(stream, depthBuffer, 0, depthBuffer.Length, 100);
            float[] depthFlat = new float[width* height];
            Buffer.BlockCopy(depthBuffer,0, depthFlat, 0,depthBuffer.Length);

            float[,] depth = new float[width,height];
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    depth[x, height-y-1] = depthFlat[y*width+x];//Python���ł͉E�オ0,0����unity�ł͍�����0,0�Ȃ̂ŁA���̍����z��
                }
            }
            Debug.Log("return");
            return new ImageAndDepth(imgBuffer, depth);


        }
        
        
    }

    public class ImageAndDepth
    {
        private byte[] imageBuffer;
        public byte[] ImageBuffer { get { return imageBuffer; } }

        private float[,] depth;
        public float[,] Depth { get { return depth; } }
        public ImageAndDepth(byte[] imageBuffer, float[,] depth)
        {
            this.imageBuffer = imageBuffer;
            this.depth = depth;
        }


    }

}