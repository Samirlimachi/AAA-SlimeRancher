using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.Events;
using SlimeRancherVR;

namespace SlimeRancher.Area1
{
    public enum Area1StructureAction
    {
        MessageOnly,
        GenerateCarrot,
        GeneratePinkPlort,
        GenerateWater
    }

    [RequireComponent(typeof(XRSimpleInteractable))]
    public sealed class Area1StructureInteractable : MonoBehaviour
    {
        [Header("Interaction")]
        public Area1StructureAction action = Area1StructureAction.GenerateCarrot;
        public int resourceAmount = 1;
        public string interactionMessage = "Recolector activado.";
        public UnityEvent onInteracted;

        XRSimpleInteractable interactable;
        float nextInteraction;

        void Awake()
        {
            interactable = GetComponent<XRSimpleInteractable>();
            interactable.selectEntered.AddListener(OnSelected);
        }

        void OnSelected(SelectEnterEventArgs args)
        {
            if (Time.time < nextInteraction) return;
            nextInteraction = Time.time + 1f;
            Interact();
        }

        public void Interact()
        {
            var game = RanchGame.Instance;
            if (game)
            {
                for (var i = 0; i < Mathf.Max(1, resourceAmount); i++)
                {
                    var kind = action == Area1StructureAction.GeneratePinkPlort ? RanchItemKind.PinkPlort : RanchItemKind.Carrot;
                    if (action == Area1StructureAction.GenerateWater)
                    {
                        game.water = Mathf.Min(30, game.water + 1);
                        continue;
                    }
                    if (action != Area1StructureAction.MessageOnly && !game.Add(kind)) break;
                }
                game.Notify(interactionMessage);
            }
            onInteracted?.Invoke();
        }

        void OnDestroy()
        {
            if (interactable) interactable.selectEntered.RemoveListener(OnSelected);
        }
    }
}