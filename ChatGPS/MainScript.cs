using GTA;
using GTA.Math;
using GTA.Native;
using GTA.UI;
using LemonUI;
using LemonUI.Menus;
using LemonUI.Scaleform;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using Screen = GTA.UI.Screen;

namespace ChatGPS
{
    public sealed class MainScript : Script
    {
        private readonly GameManager gameManager;
        private readonly HUDManager hudManager;
        private readonly ConfigManager configManager = new ConfigManager();
        private readonly WebSocketServer server = WebSocketServer.Instance;
        private readonly ObjectPool lemonPool = new ObjectPool();
        private readonly NativeMenu menu = new NativeMenu("ChatGPS");
        private readonly NativeItem startButton = new NativeItem("Start");
        private readonly NativeItem stopButton = new NativeItem("Stop");
        private readonly NativeItem continueButton = new NativeItem("Continue");
        private NativeListItem<string> routesItem;
        private NativeListItem<int> startIndexItem;
        private NativeSubmenuItem timeSubmenuItem;
        private NativeCheckboxItem debugModeItem;
        private readonly AdditionalScripts additionalScripts = InstantiateScript<AdditionalScripts>();

        private bool debugMode;

        public MainScript()
        {
            KeyDown += OnKeyDown;
            Tick += OnTick;

            InitMenu();

            gameManager = InstantiateScript<GameManager>();
            hudManager = InstantiateScript<HUDManager>();
            additionalScripts.Register(gameManager);

            gameManager.LocationFound += (_, args) =>
            {
                LocationFound(args.Index, args.Count);
            };

            gameManager.LocationStateChanged += (_, args) =>
            {
                if (args.LocationState == LocationState.AtLocation)
                {
                    countdown = gameManager.CurrentDestination.Time;
                }
                else
                {
                    countdown = -1;
                }
            };

            server.ConnectionOpen += (_, args) =>
            {
                serverUpdateFlag = true;
                server.SendMapInfo(configManager.GetSelectedMapInfo());
            };

            server.SendMapInfo(configManager.GetSelectedMapInfo());
        }

        private bool serverUpdateFlag;

        private bool gamePlaying;
        private int gameStartTick;
        private int locationFoundTick;
        private int lastFrameTick;
        private bool gamePaused;

        private int startTimeTicks;

        private void InitMenu()
        {
            List<MapInfo> maps = configManager.GetMaps();

            NativeMenu timeSubmenu = new NativeMenu("ChatGPS", "Time selector");

            lemonPool.Add(timeSubmenu);

            var hoursItem = new NativeListItem<int>("Hours", Enumerable.Range(0, 50).ToArray());
            var minutesItem = new NativeListItem<int>("Minutes", Enumerable.Range(0, 60).ToArray());
            var secondsItem = new NativeListItem<int>("Seconds", Enumerable.Range(0, 60).ToArray());

            timeSubmenu.Add(hoursItem);
            timeSubmenu.Add(minutesItem);
            timeSubmenu.Add(secondsItem);

            timeSubmenu.Closing += (_, args) =>
            {
                startTimeTicks = ((hoursItem.SelectedItem * 60 + minutesItem.SelectedItem) * 60 + secondsItem.SelectedItem) * 1000;
            };

            timeSubmenuItem = new NativeSubmenuItem(timeSubmenu, menu);
            timeSubmenuItem.Title = "Starting time";

            var mapsItem = new NativeListItem<string>("Map", "Map to show in the overlay", maps.Select(m => m.Name).ToArray());
            mapsItem.SelectedItem = configManager.GetSelectedMapInfo().Name;
            mapsItem.ItemChanged += (_, args) => { server.SendMapInfo(maps[args.Index]); configManager.SaveSelectedMap(args.Index); };
            menu.Add(mapsItem);

            Dictionary<string, string> routes = configManager.GetRouteNames();

            routesItem = new NativeListItem<string>("Route", routes.Values.ToArray());
            var route = configManager.GetCurrentRoute();

            if (!route.HasValue && routes.Count > 0)
            {
                configManager.SaveSelectedRoute(routes.Values.ToArray()[0]);
                route = configManager.GetCurrentRoute();
            }

            startIndexItem = new NativeListItem<int>("Number of found locations", Enumerable.Range(0, route.HasValue ? route.Value.Locations.Count : 0).ToArray());

            debugModeItem = new NativeCheckboxItem("Debug Mode");

            debugModeItem.CheckboxChanged += (_, args) =>
            {
                debugMode = debugModeItem.Checked;
            };

            if (route.HasValue)
            {
                routesItem.SelectedItem = route.Value.Name;
            }
            else if (routes.Count > 0)
            {
                configManager.SaveSelectedRoute(routes.Keys.ToArray()[0]);
            }
            routesItem.ItemChanged += (_, args) => 
            {
                var newRoute = routes.Keys.ToArray()[args.Index];
                configManager.SaveSelectedRoute(newRoute);
                Route? route1 = configManager.GetCurrentRoute();
                startIndexItem.Items = Enumerable.Range(0, route1.HasValue ? route1.Value.Locations.Count : 0).ToList();
                if (startIndexItem.Items.Count > 0)
                {
                    startIndexItem.SelectedIndex = 0;
                }
            };
            menu.Add(routesItem);

            menu.Add(startIndexItem);
            menu.Add(timeSubmenuItem);
            menu.Add(debugModeItem);

            startButton.Colors.BackgroundNormal = Color.DarkGreen;
            startButton.UseCustomBackground = true;
            startButton.Activated += (_, args) =>
            {
                route = configManager.GetCurrentRoute();
                if (route.HasValue)
                {
                    lemonPool.HideAll();
                    StartGame(route.Value);
                }
            };
            menu.Add(startButton);

            stopButton.Colors.BackgroundNormal = Color.DarkRed;
            stopButton.UseCustomBackground = true;
            stopButton.Activated += (_, args) =>
            {
                lemonPool.HideAll();
                StopGame();
            };

            continueButton.Colors.BackgroundNormal = Color.DarkGreen;
            continueButton.UseCustomBackground = true;
            continueButton.Activated += (_, args) =>
            {
                ContinueGame();
            };

            lemonPool.Add(menu);
        }

        private void OnKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Control && e.KeyCode == Keys.O)
            {
                menu.Visible = !menu.Visible;
            }

            if (debugMode)
            {
                if (e.Control && e.KeyCode == Keys.T && gameManager.Running)
                {
                    Game.Player.Character.Position = gameManager.CurrentDestination.Position;
                }
            }
        }

        private void OnTick(object sender, EventArgs e)
        {
            Ped player = Game.Player.Character;
            Vector2 playerPos = player.Position;
            server.SendPlayerLocation(playerPos, -player.Heading * (float)Math.PI / 180f, !player.IsInVehicle());

            if (debugMode)
            {
                var text = new TextElement($"{player.Position} Rot:{GameplayCamera.Rotation.Z}", new PointF(0.1f, 0.1f), 0.3f, Color.White, GTA.UI.Font.ChaletLondon, Alignment.Left, true, true);
                text.Draw();
            }

            lemonPool.Process();
        }
        
        private string GetTimeString(int deltaTicks)
        {
            int deltaTime = deltaTicks / 1000;
            return $"{deltaTime / 3600}:{deltaTime / 60 % 60:00}:{deltaTime % 60:00}";
        }

        private readonly TextElement timeElement      = new TextElement("", new PointF(0.85f * Screen.Width, 0.8f * Screen.Height), 0.55f, Color.White);
        private readonly TextElement totalTimeElement = new TextElement("", new PointF(0.85f * Screen.Width, 0.85f * Screen.Height), 0.55f, Color.White);
        private readonly TextElement locElement       = new TextElement("", new PointF(0.85f * Screen.Width, 0.9f * Screen.Height), 0.55f, Color.White);
        private readonly ContainerElement infoOverlay = new ContainerElement(
                new PointF(0.84f * Screen.Width, 0.79f * Screen.Height),
                new SizeF(0.16f * Screen.Width, 0.16f * Screen.Height), Color.FromArgb(127, Color.Black));


        private void DisplayProgress()
        {
            if (gamePaused)
            {
                locationFoundTick += Game.GameTime - lastFrameTick;
                gameStartTick += Game.GameTime - lastFrameTick;
            }

            timeElement.Caption = "TIME: " + GetTimeString(Game.GameTime - locationFoundTick);
            totalTimeElement.Caption = "TOTAL: " + GetTimeString(Game.GameTime - gameStartTick);
            locElement.Caption = $"LOCATIONS: {gameManager.Index}/{gameManager.Count}";

            timeElement.Draw();
            totalTimeElement.Draw();
            locElement.Draw();

            infoOverlay.Draw();

            lastFrameTick = Game.GameTime;
        }

        private void LocationFound(int index, int count)
        {
            if (index < count)
            {
                PauseGame();
            }
            hudManager.ShowLocationFoundScaleform(index, count);
            hudManager.PlayLocationFoundSound();
            server.SendMessage("", 0);
            server.ClearPicture();
            server.ClearDestination();
            pictureSent = false;
        }

        private void PauseGame()
        {
            gamePaused = true;
            gameManager.Pause();
            menu.Remove(stopButton);
            menu.Add(continueButton);
        }

        private void ContinueGame()
        {
            locationFoundTick = Game.GameTime;
            gamePaused = false;
            gameManager.Resume();
            server.SendDestinationLocation(gameManager.CurrentDestination.Position);
            menu.Remove(continueButton);
            menu.Add(stopButton);
        }

        private void StopGame()
        {
            gamePlaying = false;
            Tick -= OnGameTick;
            gameManager.Stop();
            hudManager.Stop();
            additionalScripts.Stop();
            menu.Remove(stopButton);
            menu.Remove(continueButton);
            menu.Add(startButton);
            server.SendMessage("", 0);
            server.ClearPicture();
            server.ClearDestination();
            ResetWorldParams();
            routesItem.Enabled = true;
            timeSubmenuItem.Enabled = true;
            startIndexItem.Enabled = true;
            debugModeItem.Enabled = true;
        }

        private void StartGame(Route route)
        {
            gamePlaying = true;

            menu.Remove(startButton);
            menu.Add(stopButton);
            routesItem.Enabled = false;
            timeSubmenuItem.Enabled = false;
            startIndexItem.Enabled = false;
            debugModeItem.Enabled = false;

            Model vehModel = new Model(VehicleHash.Vagrant);
            vehModel.Request();

            Screen.FadeOut(1000);
            Wait(1000);

            Tick += OnGameTick;
            
            int startIndex = startIndexItem.SelectedItem - 1;

            if (route.ID == "1P31Uv")
            {
                additionalScripts.Start();
            }
            hudManager.Start();
            gameManager.Start(route.Locations, startIndex, debugMode);

            countdown = -1;
            countdownTick = 0;
            pictureSent = false;

            SetWorldParams();
            Ped playerPed = Game.Player.Character;


            var pos = playerPed.Position = startIndex == -1 ? route.StartPosition : route.Locations[startIndex].Position;
            playerPed.Heading = startIndex == -1 ? route.StartHeading : route.Locations[startIndex].CameraDirection;
            playerPed.Task.ClearAllImmediately();
            Function.Call(Hash.SET_PED_USING_ACTION_MODE, playerPed.Handle, false, -1, 0);
            GameplayCamera.RelativeHeading = 0;

            Function.Call(Hash.NEW_LOAD_SCENE_START_SPHERE, pos.X, pos.Y, pos.Z, 200f, 0);

            int startTick = Game.GameTime;
            
            Wait(500);

            while (!vehModel.IsLoaded || !Function.Call<bool>(Hash.ARE_NODES_LOADED_FOR_AREA, pos.X - 100, pos.Y - 100, pos.X + 100, pos.Y + 100))
            {
                Function.Call<bool>(Hash.REQUEST_PATH_NODES_IN_AREA_THIS_FRAME, pos.X - 100, pos.Y - 100, pos.X + 100, pos.Y + 100);
                Yield();
            }

            while (!Function.Call<bool>(Hash.IS_NEW_LOAD_SCENE_LOADED) && Game.GameTime - startTick < 10000)
            {
                Yield();
            }

            Vehicle vehicle = World.CreateVehicle(vehModel, pos);
            vehicle.Mods.CustomPrimaryColor = Color.HotPink;
            vehicle.Mods.CustomSecondaryColor = Color.HotPink;
            vehicle.PlaceOnNextStreet();

            Wait(500);

            server.SendDestinationLocation(gameManager.CurrentDestination.Position);

            while (Screen.IsFadedOut)
            {
                Screen.FadeIn(1000);
                Yield();
            }

            Wait(1000);

            locationFoundTick = Game.GameTime;
            gameStartTick = Game.GameTime - startTimeTicks;
        }

        private void SetWorldParams()
        {
            Game.MaxWantedLevel = 0;
            Game.Player.WantedLevel = 0;
            World.CurrentTimeOfDay = TimeSpan.FromHours(9);
            World.IsClockPaused = true;
            Function.Call(Hash.SET_WEATHER_TYPE_NOW_PERSIST, "EXTRASUNNY");
            Game.Player.Character.IsInvincible = true;
        }

        private void ResetWorldParams()
        {
            Game.MaxWantedLevel = 5;
            World.IsClockPaused = false;
            Function.Call(Hash.CLEAR_WEATHER_TYPE_PERSIST);
            Game.Player.Character.IsInvincible = false;
        }

        private int countdown = -1;
        private int countdownTick = 0;
        private bool pictureSent = false;

        private void OnGameTick(object sender, EventArgs e)
        {
            if (!gamePlaying)
            {
                return;
            }

            SetWorldParams();
            DisplayProgress();

            if (gamePaused)
            {
                return;
            }

            int currentTick = Game.GameTime;

            Vector3 playerPos = Game.Player.Character.Position;


            if (countdown >= 0)
            {
                if (currentTick - countdownTick > 1000)
                {
                    if (countdown == 0)
                    {
                        if (gameManager.IsAtLastLocation)
                        {
                            gameManager.NextLocation();
                            gamePlaying = false;

                            Wait(2700);

                            hudManager.ShowMissionPassedScaleform(GetTimeString(currentTick - gameStartTick));
                            Game.Player.CanControlCharacter = false;

                            while (!Game.IsControlPressed((GTA.Control) 191))
                            {
                                Yield();
                            }

                            Game.Player.CanControlCharacter = true;

                            StopGame();
                            return;
                        }
                        else
                        {
                            gameManager.NextLocation();
                        }
                    }
                    else
                    {
                        countdownTick = currentTick;
                        server.SendCountdown(countdown);
                    }
                    countdown--;
                }
            }
            else
            {
                var message = gameManager.CurrentDestination.GetMessage(playerPos);
                if (message.HasValue && !server.HasMessage)
                {
                    server.SendMessage(message.Value.Text, message.Value.Time);
                    gameManager.CurrentDestination.RemoveMessage(message.Value);
                }

                if (gameManager.ShouldSendPicture(playerPos) && !pictureSent)
                {
                    server.SendPicture(gameManager.CurrentDestination.PicturePath);
                    pictureSent = true;
                }
                else if (gameManager.ShouldClearPicture(playerPos) && pictureSent)
                {
                    server.ClearPicture();
                    pictureSent = false;
                }
            }

            if (serverUpdateFlag)
            {
                if (gameManager.Running)
                {
                    server.SendDestinationLocation(gameManager.CurrentDestination.Position);
                    if (gameManager.ShouldSendPicture(playerPos))
                    {
                        server.SendPicture(gameManager.CurrentDestination.PicturePath);
                    }
                }
                serverUpdateFlag = false;
            }
        }
    }
}