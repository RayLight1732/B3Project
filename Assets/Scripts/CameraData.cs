using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace B3Project
{
    public class CameraData
    {
        public string CameraID { get; }

        public CameraData(string cameraID)
        {
            this.CameraID = cameraID;
        }
    }
}
