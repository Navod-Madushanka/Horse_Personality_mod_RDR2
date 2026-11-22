# lib/

Put `ScriptHookRDRNetAPI.dll` here before building the Game and DevTools projects.

Take it from the same ScriptHookRDR2DotNet V2 release whose `.asi` you installed in the game folder
(https://github.com/Halen84/ScriptHookRDR2DotNet-V2/releases). The `.asi` and the API DLL must
come from the same release.

The DLL is not committed: it is someone else's binary and changes with each hook release.
If you keep it somewhere else, build with `/p:ShdnApiPath=C:\path\to\ScriptHookRDRNetAPI.dll`.
