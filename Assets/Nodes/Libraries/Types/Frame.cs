using UnityEngine;

namespace DLN
{
    public struct Frame
    {
        public Vector3 Position;
        public Quaternion Rotation;
        public Vector3 Scale;

        public Frame(Vector3 position, Quaternion rotation, Vector3 scale)
        {
            Position = position;
            Rotation = rotation;
            Scale = scale;
        }

        public static Frame World => new Frame(
            Vector3.zero,
            Quaternion.identity,
            Vector3.one
        );
        public static Frame FromForwardUp(
    Vector3 position,
    Vector3 forward,
    Vector3 upHint,
    Vector3 scale)
        {
            return new Frame(
                position,
                Quaternions.RotationFromForwardUp(forward, upHint),
                scale
            );
        }

        public Frame WithPosition(Vector3 position) => new Frame(position, Rotation, Scale);
        public Frame WithRotation(Quaternion rotation) => new Frame(Position, rotation, Scale);
        public Frame WithScale(Vector3 scale) => new Frame(Position, Rotation, scale);

        public Matrix4x4 LocalToWorldMatrix()
        {
            return Matrix4x4.TRS(Position, Rotation, Scale);
        }

        public Matrix4x4 WorldToLocalMatrix()
        {
            return LocalToWorldMatrix().inverse;
        }

        public Matrix4x4 LocalToOtherMatrix(Frame other)
        {
            return other.WorldToLocalMatrix() * LocalToWorldMatrix();
        }

        public Matrix4x4 OtherToLocalMatrix(Frame other)
        {
            return WorldToLocalMatrix() * other.LocalToWorldMatrix();
        }

        public Vector3 LocalToWorldPosition(Vector3 p)
        {
            return LocalToWorldMatrix().MultiplyPoint3x4(p);
        }

        public Vector3 WorldToLocalPosition(Vector3 p)
        {
            return WorldToLocalMatrix().MultiplyPoint3x4(p);
        }

        public Vector3 LocalToOtherPosition(Vector3 p, Frame other)
        {
            return LocalToOtherMatrix(other).MultiplyPoint3x4(p);
        }

        public Vector3 OtherToLocalPosition(Vector3 p, Frame other)
        {
            return OtherToLocalMatrix(other).MultiplyPoint3x4(p);
        }

        public Vector3 LocalToWorldVector(Vector3 v)
        {
            return LocalToWorldMatrix().MultiplyVector(v);
        }

        public Vector3 WorldToLocalVector(Vector3 v)
        {
            return WorldToLocalMatrix().MultiplyVector(v);
        }

        public Vector3 LocalToOtherVector(Vector3 v, Frame other)
        {
            return LocalToOtherMatrix(other).MultiplyVector(v);
        }

        public Vector3 OtherToLocalVector(Vector3 v, Frame other)
        {
            return OtherToLocalMatrix(other).MultiplyVector(v);
        }

        public Vector3 LocalToWorldDirection(Vector3 d)
        {
            return Rotation * d;
        }

        public Vector3 WorldToLocalDirection(Vector3 d)
        {
            return Quaternion.Inverse(Rotation) * d;
        }

        public Vector3 LocalToOtherDirection(Vector3 d, Frame other)
        {
            return other.WorldToLocalDirection(LocalToWorldDirection(d));
        }

        public Vector3 OtherToLocalDirection(Vector3 d, Frame other)
        {
            return WorldToLocalDirection(other.LocalToWorldDirection(d));
        }
    }

    public static class FrameExtensions
    {
        public static Frame ToFrame(this Transform tx)
        {
            if (tx == null)
                return Frame.World;

            return new Frame(
                tx.position,
                tx.rotation,
                tx.lossyScale
            );
        }
    }
}