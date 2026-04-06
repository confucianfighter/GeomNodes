using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace DLN
{
    // this class should manage things like, where to put the menu when it launches.
    // It should go to some offset of XR.Origin.Camera.transform if it was launched 
    // using the pointer. Otherwise it should go near the port represinting the node.
    //
    public enum NodeMenuLaunchMethod
    {
        Pointer,
        Port
    } 
    public class TreeNode: MonoBehaviour
    {
        public Vector3 offsetFromCamera = new Vector3(0, 0, 1);
        // so if I have a PosTarget provider, target is likely set from the tree toggle button
        // this may be unnecessary, then. 

        
    }
}