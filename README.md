# Bharat Ka Safar

Unity project for Bharat Ka Safar, including mission scenes, vehicles, gameplay scripts, UI, and shared art/audio dependencies.

## Open the project

1. Install Unity **6000.1.17f1** through Unity Hub.
2. Add this repository's root folder as a project and open it.
3. Allow Unity to restore packages and import assets.
4. Open `Assets/BharatKaSafar/Scenes/Home/BKS_Splash.unity` and enter Play mode.

The enabled scenes and their order are configured in `ProjectSettings/EditorBuildSettings.asset`.

## Source

The Unity project files are synchronized from the [`Bharat-Ka-Safar` branch](https://github.com/kunmunsaubhagyalaxmi/Mad-Hill-Climbing/tree/Bharat-Ka-Safar) at commit `7b92b53170af159edf8ee6be97ad870b185183f0`.

The transfer includes `Assets`, `Packages`, and `ProjectSettings`, retaining their Unity `.meta` files and destination-only assets. Machine-specific files, temporary exports, Burst debug output, and the source Android signing keystore are not included. Configure your own signing key when making a signed Android release.
