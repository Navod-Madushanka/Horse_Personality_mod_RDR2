# lib/

Put `ScriptHookRDRNetAPI.dll` here before building the Game and DevTools projects.

Take it from the same ScriptHookRDR2DotNet V2 release whose `.asi` you installed in the game folder
(https://github.com/Halen84/ScriptHookRDR2DotNet-V2/releases). The `.asi` and the API DLL must
come from the same release.

Use release **2.2.0 or newer**. In 2.1.1.0 the hook's `Function` class is internal, so the Game and DevTools projects
fail to build with "'Function' is inaccessible due to its protection level". The game folder also needs Alexander
Blade's Script Hook RDR2 (`ScriptHookRDR2.dll` and `dinput8.dll`), which this project does not include.

The DLL is not committed: it is someone else's binary and changes with each hook release.
If you keep it somewhere else, build with `/p:ShdnApiPath=C:\path\to\ScriptHookRDRNetAPI.dll`.
