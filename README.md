# Algiers
Algiers is a tool for writing parser-based interactive fiction in C#. I made it because none of the other tools out there had enough customizability to turn exactly what I had in my head into reality. Instead of compromising, I spent lots of time making this! The parser is fairly naive, and there's no built-in functionality to speak of, but that means you get to create the world from the ground up exactly how you want it. Plus, you have all the power of C# to add new features, or dive in to the namespace itself and beef it up. 

If you wanted to make your own game this way (though really you should be using a real engine like Inform7 or TADS), read below for build instructions, and check out [A Night in Algiers](https://github.com/eliwaksbaum/a-night-in-algiers) and [Saloon Blackjack](https://github.com/eliwaksbaum/saloon-blackjack) for examples. (A Night in Algiers uses an outdated, incompatible version, but the spirit is the same.)

## Build
You need to download and install the dotnet SDK. I built to two platforms: Console and Web. Console is a stand-alone app that runs in the terminal on Windows or Linux. Web is a [Blazor](https://dotnet.microsoft.com/apps/aspnet/web-apps/blazor) app that compiles to Web Assembly (essentially the same output you get from building a Unity project for WebGL, if you're familiar). This is dotnet, though, so you could probably figure out how to publish it in other ways, too.
### Console
- run `dotnet new console` in an empty directory
- place Algiers.cs in the home directory
- using the Algiers namespace, create a public class with a public `SetWorld`method that creates all the objects and rooms in your game and returns a `World` object
- The `Main` method in Program.cs is where the game logic goes. Use  `Console.WriteLine()` `Console.ReadLine()` and `Parser.Parse()`, to collect input, generate responses, and update the display
- To publish, run `dotnet  publish -c Release -r [win/osx/linux]-64 --self-contained`
- The build will appear in bin/Release/netcoreapp**/[win/osx/linux]-64/publish
### Web
- run `dotnet new blazorwasm` in an empty directory
- You can clear all the files out of Pages and Shared except for Index.razor and MainLayout.razor
- place Algiers.cs in the home directory
- using the Algiers namespace, create a public class with a public `SetWorld`method that creates all the objects and rooms in your game and returns a `World` object
- The Index.razor file is where the game logic goes. Use an html `input` element to collect user input, run `Parser.Parse` on it, and use some `@bind` statements to update the displays
- Pay attention to the `<base>` tag in the index.html in wwwroot. That needs to be wherever your game is going to end up relative to your website's home directory.
- To publish, run `dotnet  publish -c Release`
- The build will appear in bin/Release/net**/publish/wwwroot 
