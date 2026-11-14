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
  ];
  DOTNET_ROOT = "${combinedDotnet}/share/dotnet";
  MSBuildSDKsPath = "${combinedDotnet}/share/dotnet/sdk/10.0.301/Sdks";
}
