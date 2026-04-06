// using System.Collections.Generic;
// using UnityEngine;
// using System.Collections;
// using UnityEngine.ProBuilder;

// namespace DLN
// {
//     public static class MathUtils
//     {

//         // range count, bool for start inclusive, bool for end inclusive
//         public static void StartEndSegmentCountRange(float start, float end, out List<float> outFloats, int count, bool startInclusive = true, bool endInclusive = true)
//         {
//             outFloats = new List<float>();
//             float step = (end - start) / count;
//             int i = 0;
//             if (startInclusive)
//             {
//                 outFloats.Add(start);
//                 i = 1;
//             }
//             // if I divide a line segment into 4, I will have a total of 5 points.
//             // if I exclude the start, I will have 4 points.
//             // if I exclude both, I will have 3 points.\
//             // e.g:
//             // 
//             // lets say we include the start and end.
//             // we start at 1, (pos 2) and end at 4 (pos 5)

//             for (; i < count - 1; i++)
//             {
//                 outFloats.Add(start + step * i);
//             }
//             if (endInclusive) outFloats.Add(end);
//         }
//         // same thing but batch
//         public static void StartEndSegmentCountRange(object start, object end, object count, out List<List<float>> outFloats, bool startInclusive = true, bool endInclusive = true)
//         {
//             // zip it
//             var zipped = SequenceUtils.ZipWith(start, end, count);
//             // then we can do the same thing as the non-batch version
//             outFloats = new List<List<float>>();
//             foreach (var (s, e, c) in zipped)
//             {
//                 StartEndSegmentCountRange(
//                     start: (float)s,
//                     end: (float)e,
//                     count: (int)c,
//                     outFloats: out var floats,
//                     startInclusive: startInclusive,
//                     endInclusive: endInclusive
//                 );
//                 outFloats.Add(floats);
//             }
//         }
//         public static void RandomRange(float start, float end, int count, out List<float> outFloats, bool isInt = false)
//         {
//             if (isInt)
//             {
//                 // round start and end to nearest int
//                 // not floor and ceil, nearest.
//                 start = Mathf.Round(start);
//                 end = Mathf.Round(end);
//                 outFloats = new List<float>();
//                 for (int i = 0; i < count; i++)
//                 {
//                     outFloats.Add(Random.Range(start, end + 1)); // +1 to include end in the range
//                 }
//                 return;
//             }
//             else
//             {
//                 outFloats = new List<float>();
//                 for (int i = 0; i < count; i++)
//                 {
//                     outFloats.Add(Random.Range(start, end));
//                 }
//             }
//         }
//         public static void RandomRangeBatch(object start, object end, object count, out List<List<float>> outFloats, bool isInt = false)
//         {
//             // zip it
//             var zipped = SequenceUtils.ZipWith(start, end, count);
//             // then we can do the same thing as the non-batch version
//             outFloats = new List<List<float>>();
//             foreach (var (s, e, c) in zipped)
//             {
//                 RandomRange(
//                     start: (float)s,
//                     end: (float)e,
//                     count: (int)c,
//                     outFloats: out var floats,
//                     isInt: isInt
//                 );
//                 outFloats.Add(floats);
//             }
//         }
//         // start step count range of floats
//         public static void StartStepCountRange(float start, float step, int count, out List<float> outFloats, bool startInclusive = true)
//         {
//             outFloats = new List<float>();
//             for (int i = 0; i < count; i++)
//                 outFloats.Add(start + step * i);
//         }
//         public static bool CastToPlane(Vector3 planeOrigin, Vector3 planeNormal, Vector3 castOrigin, Vector3 castDirection, float maxDistance, out Vector3 hitPoint)
//         {
//             bool isHit = false;
//             hitPoint = Vector3.zero;
//             // Construct a plane from a normal & a point on the plane:
//             Plane p = new Plane(inNormal: planeNormal.normalized, inPoint: planeOrigin);


//             // Test a Ray against it:
//             Ray ray = new Ray(origin: castOrigin, direction: castDirection);
//             if (p.Raycast(ray: ray, enter: out float enterDistance))
//             {
//                 isHit = true;
//                 // Ray hits the plane at ray.GetPoint(enterDistance)
//                 hitPoint = ray.GetPoint(enterDistance);
//                 if (enterDistance < maxDistance)
//                 {
//                     isHit = true;
//                 }
//                 // enterDistance is how far along the ray you travelled
//             }
//             return isHit;


//         }

//     }
// }
