using GTA;
using GTA.Native;
using GTA.UI;
using System;
using System.Drawing;
using System.Timers;

namespace ChatGPS
{
    [ScriptAttributes(NoDefaultInstance = true)]
    internal class HUDManager : Script
    {
        private Scaleform scaleform, buttonsScaleform;
        private int scaleformTime;
        private bool locationRendering = false;
        private bool passedRendering = false;
        private bool transitionFlag = false;

        private void RenderScaleform()
        {
            if (locationRendering)
            {
                if (!scaleform.IsLoaded)
                {
                    scaleformTime = Game.GameTime;
                    return;
                }
                int deltaTime = Game.GameTime - scaleformTime;
                if (deltaTime < 4000)
                {
                    scaleform.Render2D();
                    if (deltaTime >= 3000 && !transitionFlag)
                    {
                        scaleform.CallFunction("TRANSITION_OUT");
                        transitionFlag = true;
                    }
                }
                else
                {
                    locationRendering = false;
                }
            }

            if (passedRendering)
            {
                if (scaleform.IsLoaded && buttonsScaleform.IsLoaded)
                {
                    buttonsScaleform.Render2D();
                    scaleform.Render2D();

                    if (Game.IsControlPressed((Control)191))
                    {
                        passedRendering = false;
                    }
                }
            }
        }

        public void PlayLocationFoundSound()
        {
            Function.Call(Hash.REQUEST_SCRIPT_AUDIO_BANK, "HUD_AWARDS", false, -1);
            Audio.PlaySoundFrontend("RANK_UP", "HUD_AWARDS");
        }

        public void ShowLocationFoundScaleform(int index, int count)
        {
            locationRendering = true;
            transitionFlag = false;
            scaleformTime = Game.GameTime;
            scaleform.CallFunction("SHOW_SHARD_RANKUP_MP_MESSAGE", "Location found", $"{index + 1}/{count}", 25);
        }

        public void ShowMissionPassedScaleform(string time)
        {
            Function.Call(Hash.PLAY_MISSION_COMPLETE_AUDIO, "MICHAEL_SMALL_01");
            
            while (!Function.Call<bool>(Hash.IS_MISSION_COMPLETE_READY_FOR_UI))
            {
                Yield();
            }

            passedRendering = true;
            scaleformTime = Game.GameTime;
            scaleform.CallFunction("SHOW_SHARD_RANKUP_MP_MESSAGE", "Mission passed", "All locations found in " + time, 12);
            buttonsScaleform.CallFunction("CLEAR_ALL");
            buttonsScaleform.CallFunction("SET_CLEAR_SPACE", 200);
            buttonsScaleform.CallFunction("SET_DATA_SLOT", 0, Function.Call<string>(Hash.GET_CONTROL_INSTRUCTIONAL_BUTTONS_STRING, 2, 191, true), "Continue");
            buttonsScaleform.CallFunction("DRAW_INSTRUCTIONAL_BUTTONS");
            buttonsScaleform.CallFunction("SET_BACKGROUND_COLOUR", 0, 0, 0, 80);
        }

        private void OnTick(object sender, EventArgs e)
        {
            RenderScaleform();
        }

        public HUDManager()
        {

        }

        public void Start()
        {
            Tick += OnTick;
            scaleform = new Scaleform("MP_BIG_MESSAGE_FREEMODE");
            buttonsScaleform = new Scaleform("INSTRUCTIONAL_BUTTONS");
        }

        public void Stop()
        {
            Tick -= OnTick;
        }
    }
}
