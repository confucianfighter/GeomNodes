using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace DLN
{
    public class C3DLS_DestroyChildren : LayoutOp
    {
        public override void Execute()
        {
            GameObjectUtils.DestroyAllChildren(this.transform);
        }
    }
}