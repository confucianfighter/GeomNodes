using UnityEngine;
using DLN;
using System.Collections.Generic;

namespace DLN
{
    public class MenuDock : MonoBehaviour
    {
        public List<Transform> dockPoints = new List<Transform>();

        public Transform GetDockPoint()
        {
            Transform dockPoint;
            if (dockPoints.Count == 0)
            {
                dockPoint = CreateDockPoint();
            }
            else
            {
                dockPoint = dockPoints[0];
            }

            return dockPoint;
        }
        public Transform CreateDockPoint()
        {
            GameObject dockPointObj = new GameObject("DockPoint");
            dockPointObj.transform.parent = this.transform;
            dockPointObj.transform.localPosition = Vector3.zero;
            dockPointObj.transform.localRotation = Quaternion.identity;
            dockPoints.Add(dockPointObj.transform);
            return dockPointObj.transform;
        }
    }
}