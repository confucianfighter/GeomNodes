// using UnityEngine;
// using UnityEngine.XR.Interaction.Toolkit;

// public enum GrabMode
// {
//     Trigger,
//     Grip
// }

// [RequireComponent(typeof(Collider))]
// [RequireComponent(typeof(Rigidbody))]
// public class TriggerGrabInteractable : UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable
// {
//     [SerializeField]
//     private GrabMode grabMode = GrabMode.Trigger;
//     // override CanSelect to be false
//     public override bool IsSelectableBy(UnityEngine.XR.Interaction.Toolkit.Interactors.XRBaseInteractor interactor)
//     {
//         return grabMode != GrabMode.Trigger;
//     }

//     // ============= TRIGGER MODE =============
//     // In this mode, “Activate” (trigger) should grab,
//     // and “Select” (grip) is silenced.
//     protected override void OnActivated(ActivateEventArgs args)
//     {//Set the shared mesh material color to redas a test

//         if (grabMode != GrabMode.Trigger)
//             return;

//         if (args.interactorObject is UnityEngine.XR.Interaction.Toolkit.Interactors.IXRSelectInteractor selectInteractor)
//             interactionManager.SelectEnter(selectInteractor, this);
//     }

//     protected override void OnDeactivated(DeactivateEventArgs args)
//     {
//         if (grabMode != GrabMode.Trigger)
//             return;

//         if (args.interactorObject is UnityEngine.XR.Interaction.Toolkit.Interactors.IXRSelectInteractor selectInteractor)
//             interactionManager.SelectExit(selectInteractor, this);
//     }

//     protected override void OnSelectEntered(SelectEnterEventArgs args)
//     {
//         if (grabMode == GrabMode.Trigger)
//         {
//             // In Trigger mode, ignore grip presses:
//             return;
//         }
//         // Otherwise (Grip mode), let the base handle it:
//         base.OnSelectEntered(args);
//     }

//     protected override void OnSelectExited(SelectExitEventArgs args)
//     {
//         if (grabMode == GrabMode.Trigger)
//             return;

//         base.OnSelectExited(args);
//     }

//     // ============= GRIP MODE =============
//     // In this mode, “Select” (grip) will grab normally,
//     // and “Activate” (trigger) does nothing.
//     // (Handled implicitly by the above overrides.)
// }
