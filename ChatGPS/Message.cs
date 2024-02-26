using System;
using System.Runtime.Serialization;

namespace ChatGPS
{
    public struct Message
    {
        public Message(string text, int time, float distance)
        {
            Text = text;
            Time = time;
            Distance = distance;
        }

        public string Text { get; }
        public int Time { get; }
        public float Distance { get; }

        public override int GetHashCode()
        {
            return Text.GetHashCode() * 23 + Time.GetHashCode() * 11 + Distance.GetHashCode() * 5;
        }

        public override bool Equals(object other)
        {
            if (other?.GetType() != typeof(Message))
            {
                return false;
            }

            return Equals((Message) other);
        }

        public bool Equals(Message other)
        {
            return Text == other.Text && Time == other.Time && Distance == other.Distance;
        }
        public static bool operator ==(Message message1, Message message2)
        {
            return message1.Equals(message2);
        }
        public static bool operator !=(Message message1, Message message2)
        {
            return !message1.Equals(message2);
        }
    }
}
