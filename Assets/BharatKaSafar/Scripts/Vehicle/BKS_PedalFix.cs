using UnityEngine;
using UnityEngine.EventSystems;

namespace BharatKaSafar.Vehicle
{
    /// <summary>
    /// Two-finger pedals: the old pedals call CarInput.ReleaseGasBrake on ANY pedal's release, so pressing gas while still
    /// holding brake and then lifting the brake finger stopped the gas too (it had to be pressed again).
    /// This rewires the pedal EventTriggers (found by their CarInput.Gas / Brake or BKS_GameplayHUD.OnAccelerateDown / OnBrakeDown calls) to remember which pedals are held:
    /// the last pressed pedal drives; lifting one hands over to the other if it is still held.
    /// Added to every gameplay scene by BKS_SelectedVehicleSpawner. The original scripts are not changed.
    /// </summary>
    public class BKS_PedalFix : MonoBehaviour
    {
        private bool gasHeld, brakeHeld;
        private CarController car;

        private void Start()
        {
            foreach (var trigger in FindObjectsByType<EventTrigger>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                int kind = 0; // 1 = gas, -1 = brake
                foreach (var e in trigger.triggers)
                    for (int i = 0; i < e.callback.GetPersistentEventCount(); i++)
                    {
                        var target = e.callback.GetPersistentTarget(i);
                        if (!(target is CarInput) && !(target is UI.BKS_GameplayHUD)) continue;
                        string m = e.callback.GetPersistentMethodName(i);
                        if (m == "Gas" || m == "OnAccelerateDown") kind = 1; else if (m == "Brake" || m == "OnBrakeDown") kind = -1;
                    }
                if (kind == 0) continue;

                bool gas = kind == 1;
                trigger.triggers.Clear(); // drop the old down / up calls (the up one released both pedals)
                Add(trigger, EventTriggerType.PointerDown, () => { if (gas) gasHeld = true; else brakeHeld = true; Push(gas ? 1 : -1); });
                Add(trigger, EventTriggerType.PointerUp, () =>
                {
                    if (gas) gasHeld = false; else brakeHeld = false;
                    Push(gasHeld ? 1 : brakeHeld ? -1 : 0);
                });
            }
        }

        private static void Add(EventTrigger t, EventTriggerType type, UnityEngine.Events.UnityAction action)
        {
            var entry = new EventTrigger.Entry { eventID = type };
            entry.callback.AddListener(_ => action());
            t.triggers.Add(entry);
        }

        private void Push(int input)
        {
            if (car == null) car = FindFirstObjectByType<CarController>();
            if (car == null) return;
            if (input > 0) car.Acceleration();
            else if (input < 0) car.Brake();
            else car.GasBrakeRelease();
        }

        // app sent to background with a finger down: nothing stays held
        private void OnApplicationPause(bool paused)
        {
            if (!paused) return;
            ReleaseAll();
        }

        public void ReleaseAll()
        {
            gasHeld = brakeHeld = false;
            Push(0);
        }
    }
}
