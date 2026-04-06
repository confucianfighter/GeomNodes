using UnityEngine;
using System;
namespace DLN
{
    public class C3DLS_PreserveRegionsData : MonoBehaviour
    {
        [SerializeField] public PreserveRegions data = PreserveRegions.Init(regionSizeFrom0To1: .03f, desiredSize: .03f);
    }
}