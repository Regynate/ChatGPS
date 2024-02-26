using GTA;
using GTA.Math;
using GTA.Native;
using GTA.UI;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;

namespace ChatGPS
{
    [ScriptAttributes(NoDefaultInstance = true)]
    internal class GameManager : Script
    {
        private IList<Location> locations;

        private int index = 0;
        private LocationState state = LocationState.NotAtLocation;

        public int Index => index;
        public int Count => locations.Count;

        public LocationState State => state;

        public event EventHandler<LocationStateChangedEventArgs> LocationStateChanged;
        public event EventHandler<DestinationChangedEventArgs> DestinationChanged;
        public event EventHandler<LocationFoundEventArgs> LocationFound;
        public bool Running { get; private set; }
        private bool DebugMode;

        public bool ShouldSendPicture(Vector2 playerPos)
        {
            return CurrentDestination.Position.DistanceTo2D(playerPos) < CurrentDestination.PictureRadius;
        }

        public bool ShouldClearPicture(Vector2 playerPos)
        {
            return CurrentDestination.Position.DistanceTo2D(playerPos) > CurrentDestination.PictureRadius * 1.3;
        }

        private void NotifyLocationState(LocationState newState)
        {
            if (state != newState)
            {
                state = newState;
                LocationStateChanged?.Invoke(this, new LocationStateChangedEventArgs(state));
            }
        }

        private void NotifyDestinationChanged(Vector2 destination)
        {
            DestinationChanged?.Invoke(this, new DestinationChangedEventArgs(destination, index));
        }

        private void NotifyLocationFound()
        {
            LocationFound?.Invoke(this, new LocationFoundEventArgs(index, locations.Count));
        }

        public Location CurrentDestination => locations[index];

        private void DrawRect(Vector3 point1, Vector3 point2, Vector3 point3, Vector3 point4)
        {
            World.DrawPolygon(point1, point2, point3, Color.FromArgb(127, Color.Yellow));
            World.DrawPolygon(point1, point3, point2, Color.FromArgb(127, Color.Yellow));
            World.DrawPolygon(point3, point4, point1, Color.FromArgb(127, Color.Yellow));
            World.DrawPolygon(point3, point1, point4, Color.FromArgb(127, Color.Yellow));
        }

        // God forgive me
        private void DrawLocationBox()
        {
            Vector3 position = CurrentDestination.Position;
            Vector3 error = CurrentDestination.PositionError;
            float xmin = position.X - error.X;
            float xmax = position.X + error.X;
            float ymin = position.Y - error.Y;
            float ymax = position.Y + error.Y;
            float zmin = position.Z - error.Z;
            float zmax = position.Z + error.Z;

            DrawRect(new Vector3(xmin, ymin, zmin), new Vector3(xmin, ymax, zmin), new Vector3(xmin, ymax, zmax), new Vector3(xmin, ymin, zmax));
            DrawRect(new Vector3(xmin, ymin, zmin), new Vector3(xmin, ymin, zmax), new Vector3(xmax, ymin, zmax), new Vector3(xmax, ymin, zmin));
            DrawRect(new Vector3(xmin, ymin, zmin), new Vector3(xmin, ymax, zmin), new Vector3(xmax, ymax, zmin), new Vector3(xmax, ymin, zmin));
            DrawRect(new Vector3(xmax, ymin, zmin), new Vector3(xmax, ymax, zmin), new Vector3(xmax, ymax, zmax), new Vector3(xmax, ymin, zmax));
            DrawRect(new Vector3(xmin, ymax, zmin), new Vector3(xmin, ymax, zmax), new Vector3(xmax, ymax, zmax), new Vector3(xmax, ymax, zmin));
            DrawRect(new Vector3(xmin, ymin, zmax), new Vector3(xmin, ymax, zmax), new Vector3(xmax, ymax, zmax), new Vector3(xmax, ymin, zmax));

            const double D2R = 0.01745329251994329576923690768489;

            float amin = CurrentDestination.CameraDirection - CurrentDestination.CameraError;
            float amax = CurrentDestination.CameraDirection + CurrentDestination.CameraError;

            World.DrawPolygon(
                position,
                position - new Vector3((float) Math.Sin(amax * D2R) * 3, -(float) Math.Cos(amax * D2R) * 3, 0),
                position - new Vector3((float) Math.Sin(amin * D2R) * 3, -(float) Math.Cos(amin * D2R) * 3, 0), 
                Color.Green);
            World.DrawPolygon(
                position,
                position - new Vector3((float) Math.Sin(amin * D2R) * 3, -(float) Math.Cos(amin * D2R) * 3, 0), 
                position - new Vector3((float) Math.Sin(amax * D2R) * 3, -(float) Math.Cos(amax * D2R) * 3, 0),
                Color.Green);
        }

        public void OnTick(object sender, EventArgs e)
        {
            Location location = CurrentDestination;

            Vector3 playerPos = Game.Player.Character.Position;
            float direction = GameplayCamera.Rotation.Z;

            if (DebugMode)
            {
                DrawLocationBox();
            }

            if (Game.Player.Character.Speed < 1f || location.Time == 0) // small hack for the cable car
            {
                if (location.Check(playerPos, direction))
                {
                    if (DebugMode)
                    {
                        new ContainerElement(new PointF(64, 36), new SizeF(128, 72), Color.Green).ScaledDraw();
                    }
                    NotifyLocationState(LocationState.AtLocation);
                }
                else if (location.Check(playerPos))
                {
                    NotifyLocationState(LocationState.AtLocationWrongCamera);
                }
                else
                {
                    NotifyLocationState(LocationState.NotAtLocation);
                }
            }
            else if (location.Check(playerPos))
            {
                NotifyLocationState(LocationState.AtLocationRunning);
            }
            else
            {
                NotifyLocationState(LocationState.NotAtLocation);
            }
        }

        public bool IsAtLastLocation => index == locations.Count - 1;

        public void NextLocation()
        {
            NotifyLocationFound();
            if (index < locations.Count - 1)
            {
                ++index;
                NotifyDestinationChanged(CurrentDestination.Position);
            }
        }

        public void Start(IList<Location> locations, int startIndex = -1, bool debugMode = false)
        {
            this.locations = locations;
            state = LocationState.NotAtLocation;
            index = startIndex + 1;
            Running = true;
            DebugMode = debugMode;
            Tick += OnTick;
            
            NotifyDestinationChanged(CurrentDestination.Position);
        }

        public void Stop()
        {
            Running = false;
            Tick -= OnTick;
        }

        public GameManager()
        {
            Running = false;
        }
    }
}
