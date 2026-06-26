using System;
using System.Collections.Generic;
using UnityEngine;

namespace IdleSim
{
    // Serializable store layout, persisted via JsonUtility + PlayerPrefs.
    [Serializable]
    public class XZ
    {
        public float x;
        public float z;
        public float rot;
        public string id;
        public XZ() { }
        public XZ(Vector3 p) { x = p.x; z = p.z; }
    }

    [Serializable]
    public class LayoutData
    {
        public float shopRotation;
        public int unlocked = 1;
        public XZ checkout;
        public List<XZ> shelves = new List<XZ>();
        public List<XZ> dividers = new List<XZ>();
        public List<XZ> decor = new List<XZ>();
    }
}
