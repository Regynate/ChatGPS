using GTA.Math;
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;

namespace ChatGPS
{
    public class Location
    {
        private readonly Vector3 position;
        private readonly float cameraDirection;
        private readonly Vector3 positionError;
        private readonly float cameraError;
        private readonly string picturePath;
        private readonly float pictureRadius;
        private readonly int time;
        private readonly List<Message> messages;

        public Location(Vector3 position, float cameraDirection, Vector3 positionError,
            float cameraError, string picturePath, float pictureRadius, int time, List<Message> messages)
        {
            this.position = position;
            this.cameraDirection = cameraDirection;
            this.positionError = positionError;
            this.cameraError = cameraError;
            this.picturePath = picturePath;
            this.pictureRadius = pictureRadius;
            this.time = time;
            this.messages = messages;
        }

        public Vector3 Position => position;
        public float CameraDirection => cameraDirection;

        public Vector3 PositionError => positionError;
        public float CameraError => cameraError;

        public string PicturePath => "..\\routes\\" + picturePath;

        public float PictureRadius => pictureRadius;

        public int Time => time;

        private bool CheckAngle(float a, float error, float x)
        {
            a %= 360;
            if (a < 0)
            {
                a += 360;
            }

            return Check(a, error, x) || Check(a, error, x + 360) || Check(a, error, x - 360);
        }

        private bool Check(float a, float error, float x)
        {
            return a - error <= x && x < a + error;
        }

        public bool Check(Vector3 playerPos, float cameraDirection)
        {
            return Check(playerPos) &&
                   CheckAngle(this.cameraDirection, cameraError, cameraDirection);
        }

        public bool Check(Vector3 playerPos)
        {
            return Check(position.X, positionError.X, playerPos.X) &&
                   Check(position.Y, positionError.Y, playerPos.Y) &&
                   Check(position.Z, positionError.Z, playerPos.Z);
        }

        public Message? GetMessage(Vector3 playerPos)
        {
            foreach (Message message in messages)
            {
                if (playerPos.DistanceTo2D(position) <= message.Distance)
                {
                    return message;
                }
            }

            return null;
        }

        public void RemoveMessage(Message message)
        {
            messages.Remove(message);
        }
    }
}
