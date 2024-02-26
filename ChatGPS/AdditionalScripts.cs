using GTA;
using GTA.Math;
using GTA.Native;
using GTA.UI;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChatGPS
{
    // Handles Davey and cougar spawn
    [ScriptAttributes(NoDefaultInstance = true)]
    internal class AdditionalScripts : Script
    {
        private GameManager gameManager;

        private bool daveFlag = false;
        private bool cougarFlag = false;
        private bool cougarFlag1 = false;
        private bool carFlag = false;
        private bool heliFlag = false;
        private bool chopFlag = false;
        private readonly List<Ped> cougars = new List<Ped>();
        private Dictionary<Ped, Task> cougarTasks = new Dictionary<Ped, Task>();

        private enum Task
        {
            None,
            EnterVehicle,
            Fight,
            GoTo
        }

        private void SetCougarTask(Ped cougar, Task task, Action taskAction)
        {
            if (cougarTasks[cougar] != task)
            {
                taskAction.Invoke();
                cougarTasks[cougar] = task;
            }
        }

        private void OnTick(object sender, EventArgs args)
        {
            if (daveFlag)
            {
                Ped playerPed = Game.Player.Character;

                Vector3 position = new Vector3(-442.2f, 1059.05f, 326.86f);

                while (playerPed.Position.DistanceTo(position) > 100)
                {
                    Yield();
                }

                string animDict = "missfbi1leadinout";

                Function.Call(Hash.REQUEST_ANIM_DICT, animDict);
                Model daveModel = new Model(PedHash.DaveNorton);
                Model blimpModel = new Model(VehicleHash.Blimp);
                Model michaelModel = new Model(PedHash.Michael);
                daveModel.Request();
                blimpModel.Request();
                michaelModel.Request();
                Function.Call(Hash.REQUEST_CUTSCENE, "fbi_1_int", 8);
                while (
                    !daveModel.IsLoaded || !blimpModel.IsLoaded || !michaelModel.IsLoaded ||
                    !Function.Call<bool>(Hash.HAS_ANIM_DICT_LOADED, animDict) ||
                    !Function.Call<bool>(Hash.HAS_CUTSCENE_LOADED))
                {
                    Yield();
                }
                Ped dave = World.CreatePed(daveModel, position, 165f);
                dave.Task.PlayAnimation(animDict, "fbi_1_int_leadin_loop_daven", 8, -1, AnimationFlags.Loop | AnimationFlags.NotInterruptable);
                dave.SetIsPersistentNoClearTask(true);
                dave.AlwaysKeepTask = true;


                while (playerPed.Position.DistanceTo(dave.Position) > 10)
                {
                    if (!daveFlag)
                    {
                        return;
                    }

                    Yield();
                }

                Vehicle blimp = World.CreateVehicle(blimpModel, new Vector3(-440.490f, 1009.085f, 335.090f), 0.824f);
                blimp.IsEngineRunning = true;
                Ped michael = blimp.CreatePedOnSeat(VehicleSeat.Driver, michaelModel);
                michael.IsInvincible = true;
                blimp.ForwardSpeed = 45f;
                michael.Task.LeaveVehicle(LeaveVehicleFlags.BailOut | LeaveVehicleFlags.DontWaitForVehicleToStop);

                Function.Call(Hash.REGISTER_ENTITY_FOR_CUTSCENE, michael.Handle, "MICHAEL", 0, 0, 64);

                Wait(5000);

                dave.IsVisible = false;
                
                Function.Call(Hash.START_CUTSCENE);
                Wait(6500);
                Function.Call(Hash.STOP_CUTSCENE_IMMEDIATELY);
                Function.Call(Hash.REMOVE_CUTSCENE);

                dave.IsVisible = true;
                daveFlag = false;
            }

            if (cougarFlag)
            {
                Model cougarModel = new Model(PedHash.MountainLion);
                cougarModel.Request();

                while (!cougarModel.IsLoaded)
                {
                    Yield();
                }

                Ped playerPed = Game.Player.Character;

                Random random = new Random();

                for (int i = 0; i < 30; i++)
                {
                    Ped cougar = World.CreatePed(cougarModel, new Vector3(54 + random.Next(-10, 10), 7215 + random.Next(-15, 15), 3), 180 + random.Next(-90, 90));
                    if (cougar != null)
                    {
                        cougar.IsInvincible = true;
                        cougar.RelationshipGroup.SetRelationshipBetweenGroups(playerPed.RelationshipGroup, Relationship.Hate);
                        Function.Call(Hash.SET_PED_COMBAT_ATTRIBUTES, cougar.Handle, 5, true);
                        Function.Call(Hash.SET_PED_COMBAT_ATTRIBUTES, cougar.Handle, 46, true);
                        Function.Call(Hash.SET_PED_COMBAT_ATTRIBUTES, cougar.Handle, 58, true);
                        Function.Call(Hash.SET_PED_COMBAT_ATTRIBUTES, cougar.Handle, 63, false);
                        Function.Call(Hash.SET_PED_FLEE_ATTRIBUTES, cougar.Handle, 2, true);

                        cougar.AlwaysKeepTask = false;
                        cougar.CanRagdoll = false;
                        cougar.BlockPermanentEvents = true;
                        cougar.HearingRange = 9999;
                        Function.Call(Hash.SET_PED_COMBAT_RANGE, cougar.Handle, 3); // very far

                        cougars.Add(cougar);
                    }
                }

                cougarFlag = false;
            }

            if (cougarFlag1)
            {
                Ped playerPed = Game.Player.Character;

                while (!Game.IsControlPressed((Control) 191))
                {
                    Yield();
                }

                cougarTasks = cougars.ToDictionary(c => c, _ => Task.None);

                Random random = new Random();

                while (!playerPed.IsDead)
                {
                    foreach (Ped cougar in cougars)
                    {
                        if (cougar.Position.DistanceTo(playerPed.Position) > 80)
                        {
                            cougar.Position = playerPed.GetOffsetPosition(new Vector3(random.Next(-20, 20), random.Next(-20, 20), random.Next(5, 10)));
                            cougarTasks[cougar] = Task.None;
                        }

                        if (cougar.Position.DistanceTo(playerPed.Position) > 30)
                        {
                            SetCougarTask(cougar, Task.GoTo,
                                () => Function.Call(Hash.TASK_GO_TO_ENTITY, cougar.Handle, playerPed.Handle, -1, 3f, 2f, 0f, 1));
                        }
                        else if (playerPed.IsInVehicle())
                        {
                            SetCougarTask(cougar, Task.EnterVehicle,
                                () => cougar.Task.EnterVehicle(
                                    playerPed.CurrentVehicle, VehicleSeat.Driver, 
                                    -1, 2f, 
                                    EnterVehicleFlags.JustPullPedOut | EnterVehicleFlags.JackAnyone));
                        }
                        else
                        {
                            SetCougarTask(cougar, Task.Fight,
                                () => cougar.Task.FightAgainst(playerPed));
                        }
                    }

                    Yield();
                }

                foreach (Ped cougar in cougars)
                {
                    cougar.MarkAsNoLongerNeeded();
                }

                cougars.Clear();
                cougarFlag1 = false;
                Stop();
            }

            if (carFlag)
            {
                Model vehModel = new Model(VehicleHash.Vagrant);
                vehModel.Request();

                while (!vehModel.IsLoaded)
                {
                    Yield();
                }

                Vector3 position = new Vector3(-771, 5578, 33.48f);

                foreach (var entity in World.GetNearbyEntities(position, 20))
                {
                    if (Function.Call<bool>(Hash.IS_ENTITY_A_VEHICLE, entity.Handle))
                    {
                        entity.Delete();
                    }
                }

                Vehicle vehicle = World.CreateVehicle(vehModel, position, 87);
                vehicle.PlaceOnNextStreet();
                vehicle.Mods.CustomPrimaryColor = Color.HotPink;
                vehicle.Mods.CustomSecondaryColor = Color.HotPink;

                carFlag = false;
            }

            if (heliFlag)
            {
                Model vehModel = new Model(VehicleHash.Havok);
                vehModel.Request();

                while (!vehModel.IsLoaded)
                {
                    Yield();
                }

                Vector3 position = new Vector3(-1091, -2893, 13.95f);

                foreach (var entity in World.GetNearbyEntities(position, 20))
                {
                    entity.Delete();
                }

                Vehicle vehicle = World.CreateVehicle(vehModel, position, 136);
                vehicle.Mods.CustomPrimaryColor = Color.HotPink;
                vehicle.Mods.CustomSecondaryColor = Color.HotPink;
                vehicle.IsInvincible = true;

                heliFlag = false;
            }

            if (chopFlag)
            {
                Model model = new Model(PedHash.Chop);
                model.Request();

                while (!model.IsLoaded)
                {
                    Yield();
                }

                Ped chop = World.CreatePed(model, new Vector3(18.5f, 536.7f, 170.7f), 170f);

                chop.Task.PlayAnimation("creatures@rottweiler@amb@world_dog_sitting@base", "base");

                chop.AlwaysKeepTask = true;
                chop.BlockPermanentEvents = true;

                chopFlag = false;
            }
        }

        private void OnDestinationChanged(object sender, DestinationChangedEventArgs args)
        {
            if (args.Index == 4)
            {
                daveFlag = true;
            }
            else if (args.Index == 5)
            {
                daveFlag = false;
                chopFlag = true;
            }
            else if (args.Index == 10)
            {
                heliFlag = true;
            }
            else if (args.Index == 12)
            {
                carFlag = true;
            }
            else if (args.Index == 13)
            {
                cougarFlag = true;
            }
        }
        
        private void OnLocationFound(object sender, LocationFoundEventArgs args)
        {
            if (args.Index == 13)
            {
                cougarFlag1 = true;
            }
        }

        public AdditionalScripts()
        {
            Tick += OnTick;
        }

        public void Register(GameManager gameManager)
        {
            this.gameManager = gameManager;
        }

        public void Start()
        {
            daveFlag = false;
            cougarFlag = false;
            cougarFlag1 = false;
            carFlag = false;
            heliFlag = false;
            chopFlag = false;
            gameManager.DestinationChanged += OnDestinationChanged;
            gameManager.LocationFound += OnLocationFound;
            Tick += OnTick;
        }

        public void Stop()
        {
            gameManager.DestinationChanged -= OnDestinationChanged;
            gameManager.LocationFound -= OnLocationFound;
            Tick -= OnTick;
        }
    }
}
