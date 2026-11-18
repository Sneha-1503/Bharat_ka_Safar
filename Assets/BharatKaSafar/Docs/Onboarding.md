# Mission briefing and control practice

The shared `BKS_MissionSelect` scene now contains the restored `MissionBriefing` hierarchy and links it to `BKS_MissionSelectScreen`. ENTER opens this panel; START launches the selected mission. Close/Escape returns to the cards. The panel retains its vehicle picker, shows cumulative star rules, and scrolls long obstacle instructions.

`Practise Controls` defaults ON until the player completes or explicitly skips the tutorial. Switching it OFF in the briefing bypasses practice for that launch only. Players can switch it ON to replay from any mission briefing.

`MissionManager.StartMission` installs `BKS_DrivingTutorial` when required. Practice pauses physics, fuel and mission time, highlights the actual HUD controls, and requires gas, brake, gas-then-brake for airborne balance, and a horn press. The final page explains fuel and the finish goal. This teaches the input sequence safely; it does not simulate an airborne landing while paused.

The tutorial restores the previous time scale, clears held touch pedals, and removes its overlay on completion, skip, or teardown. Practising the horn plays audio but cannot clear obstacles or earn stars. Save key `BKS_DrivingTutorial_v1`: absent = pending, 1 = completed, 2 = explicitly skipped. Existing mission progress and currency are unaffected by this key.

## Verification

1. Play from the home/map flow, select a mission, and press ENTER. Check objective, cumulative star rules, rewards and owned-vehicle selection. Close and reopen the briefing.
2. Set Practise Controls ON, then START. Confirm the car and fuel are paused, only the highlighted control is exposed, and Continue is disabled until the requested input is performed.
3. In Unity Play Mode, use **Bharat Ka Safar > QA > Verify Tutorial (Play Mode)** while the first practice step is showing. This integration check uses the live vehicle/HUD, verifies wrong-input gating, both balance inputs, horn reward isolation, frozen fuel/time, completion, replay, skip and saved preference behavior. It restores the tutorial preference and exits Play Mode. Results appear in the Console as `[Onboarding checks] PASS` or `FAIL`.
4. Verify 16:9, a tall/wide phone, and tablet layouts; long briefing text should scroll rather than shrink. Check pointer release, two-finger pedal handover, phone background/resume, and Back/Escape.

The tutorial is generated at runtime using the shared teal/gold palette. Lesson wording and timing live in `Scripts/UI/BKS_DrivingTutorial.cs`; briefing layout/text lives in `Scripts/UI/BKS_MissionBriefingPopup.cs`. The scene hierarchy remains editable and can be restored with the existing UI restoration tool if needed.
