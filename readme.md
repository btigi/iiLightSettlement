iiLightSettlement
=========

iiLightSettlement is a C# library supporting the modification of files relating to Dark Colony, the 1997 RTS game developed by Alternative Reality.

| Name   | Read | Write | Comment
|--------|:----:|-------|--------
| BTS    | ✔   |   ✔   |
| FIN    | ✔   |   ✔   |
| JUS    | ✔   |   ✗   |
| MAP    | ✗   |   ✗   |
| MTG    | ✗   |   ✗   |
| OVH    | ✗   |   ✗   |
| PTH    | ✗   |   ✗   |
| SPR    | ✔   |   ✔   | Save uncompressed only


## Usage

Instantiate the relevant class and call the `Process` method passing the filename.

```csharp
var btsProcessor = new BtsProcessor();
var tiles = btsProcessor.Process(@"D:\data\darkcolony\DC\SCENARIO\ATLANTIS.BTS");
for (int i = 0; i < tiles.Count; i++)
{
    var bitmap = tiles[i];
    bitmap.Save(@$"D:\data\dc-out\output_{i}.png");
}

var finProcessor = new FinProcessor();
var leftFin = finProcessor.Process(@"D:\data\darkcolony\DC\ANIMATE\LEFT.fin");

var sprProcessor = new SprProcessor();
var bitmaps = sprProcessor.Process(@"D:\data\darkcolony\DC\SPRITES\vent2.spr");
for (int i = 0; i < bitmaps.Count; i++)
{
    var bitmap = bitmaps[i];
    bitmap.Save(@$"D:\data\dc-out\output_{i}.png");
}
```


## Compiling

To clone and run this application, you'll need [Git](https://git-scm.com) and [.NET](https://dotnet.microsoft.com/) installed on your computer. From your command line:

```
# Clone this repository
$ git clone https://github.com/btigi/iiLightSettlement

# Go into the repository
$ cd src

# Build  the app
$ dotnet build
```

## Licencing

iiLightSettlement is licenced under the MIT License. Full licence details are available in licence.md