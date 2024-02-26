# ChatGPS
Let someone guide you through Los Santos, helping you find the hidden locations.
## Installation
1. Install [ScriptHookV](http://www.dev-c.com/gtav/scripthookv) and [ScriptHookVDotNet](https://github.com/scripthookvdotnet/scripthookvdotnet/releases)
2. Download the [latest release](https://github.com/Regynate/ChatGPS/releases/latest/download/ChatGPS.zip)
3. Extract the contents of ChatGPS.zip into the `scripts` folder inside GTA root directory
4. Set up OBS:
    * Add browser source
    * Tick “Local file”
    * Path: \*path to GTA\*/scripts/ChatGPS/mapOverlay/index.html
    * Width/Height works best with 3:4 aspect ratio (480x640, 600x800, 768x1024 and so on)

## Usage
1. Press Ctrl+O in game to activate the mod
2. You can run Test Route to test the setup

## Rules
1. The destination is shown in the overlay, and it's not visible in game
2. When you’re near the destination, the picture of the exact position appears on top of the map
3. Stand still at the location for 5 seconds to progress

Also feel free to check out [PickChatter](https://github.com/Regynate/PickChatter) if you want to run it with Twitch chat guiding you

## Adding your routes
1. Navigate to `scripts\ChatGPS\routes` folder
2. Create a file named `*your route*.xml`
The XML schema:
```
<?xml version="1.0"?>
<Route ID="*your ID*">
  <Name>Test Route</Name>
  <StartPosition X="-1859.5" Y="-1242.5" Z="8.6"/>
  <StartHeading>316</StartHeading>
  <Locations>
    <Location>
      <Position X="-1816.3" Y="-1175.9" Z="13"/>
      <PositionError X="5" Y="5" Z="1"/>
      <CameraDirection>144</CameraDirection>
      <CameraError>40</CameraError>
      <PicturePath>test_pics\location_0.png</PicturePath>
      <PictureRadius>40</PictureRadius>
    </Location>
    <Location>
      ...
    </Location>
    ...
  </Locations>
</Route>
```
Replace `*your ID*` with any string. The only requirement is the ID must be unique.

&nbsp;

Fields description:

`Name`: the route name. It will appear in the in-game menu.

`StartPosition`: the starting position of the route. The player will be placed there upon starting the route.

`StartHeading`: the starting heading of the player.

`Locations`: an array of `Location`

&nbsp;

`Location` describes a cuboid. The player must stay inside of that cuboid to progress. The sides of the cuboid are parallel to the world's XYZ axis.

Fields of a `Location`:

`Position`: The position of the center of the cuboid.

`PositionError`: The distance from the cuboid center to its sides.

`CameraDirection`: The middle point of accepted camera direction.

`CameraError`: The accepted range of the camera direction.

`PicturePath`: Path to the image of the location. It is relative to the `routes` folder

`PictureRadius`: Distance to the location, within which the image is shown.

(Optional) `Time`: The player has to stand still for this amount of seconds at the location to progress. (Default: 5)

You can add as many locations as you want.

## Testing the routes
Select "Debug Mode" when starting the route. The cuboids will be highlighted in yellow, and the accepted camera direction will appear as a green triangle. Also you can press Ctrl+T to teleport to the current destination.
