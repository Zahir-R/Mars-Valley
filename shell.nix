{
  pkgs ? import <nixpkgs> { },
}:
let
  combinedDotnet = pkgs.dotnetCorePackages.combinePackages [
    pkgs.dotnetCorePackages.sdk_8_0
    pkgs.dotnetCorePackages.sdk_10_0
  ];
in
pkgs.mkShell {
  packages = [
    combinedDotnet
    pkgs.csharp-ls
    pkgs.godot-mono
  ];
  DOTNET_ROOT = "${combinedDotnet}/share/dotnet";
  DOTNET_MULTILEVEL_LOOKUP = "0";
  DOTNET_ROLL_FORWARD = "Minor";
  GODOT = "${pkgs.godot-mono}/bin/godot-mono";
}
