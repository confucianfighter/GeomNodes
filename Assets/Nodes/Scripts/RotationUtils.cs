// using System.Collections.Generic;
// using System.Collections;
// using UnityEngine;
// using System.Linq;

// namespace DLN
// {
//     public static class RotationUtils
//     {
//         public static void RandomRotation(out Quaternion quaternion)
//         {
//             quaternion = Quaternion.Euler(
//                 Random.Range(0, 360),
//                 Random.Range(0, 360),
//                 Random.Range(0, 360)
//             );
//         }
//         public static void SetRandomRotation(object @object)
//         {
//             RandomRotation(out var quaternion);
//             @object.AsTransform().rotation = quaternion;
//         }
//         public static void RandomRotationMultiple(out Quaternion quaternion, int count)
//         {
//             quaternion = Quaternion.Euler(
//                 Random.Range(0, 360),
//                 Random.Range(0, 360),
//                 Random.Range(0, 360)
//             );
//         }
//         public static void SetRandomRotationBatch(object @object)
//         {
//             var sequence = SequenceUtils.AsSequence(@object);
//             foreach (var item in sequence)
//             {
//                 SetRandomRotation(item);
//             }
//         }

//     }
// }
