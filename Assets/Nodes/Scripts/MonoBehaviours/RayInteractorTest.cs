// mono behaviour with functions to connect with the ray interactor events and print the info
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Utilities;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using UnityEngine.InputSystem;
using UnityEngine.Events;
using UnityEngine.XR.Interaction.Toolkit.Inputs;

namespace DLN
{
    public class RayInteractorTest : MonoBehaviour
    {
        public void OnHoverEntered(HoverEnterEventArgs args)
        {
            Debug.Log("OnHoverEntered: interactor: " + args.interactorObject.transform.name + " interactable: " + args.interactableObject.transform.name);
        }
        public void OnHoverExited(HoverExitEventArgs args)
        {
            Debug.Log("OnHoverExited: interactor: " + args.interactorObject.transform.name + " interactable: " + args.interactableObject.transform.name);
        }
        public void OnSelectEntered(SelectEnterEventArgs args)
        {
            Debug.Log($"OnSelectEntered: interactor: {args.interactorObject.transform.name} interactable: {args.interactableObject.transform.name}, intensity:");
        }
    }
}