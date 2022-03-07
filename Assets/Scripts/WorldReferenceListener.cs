using HoloToolkit.Unity.InputModule;
using UnityEngine;
using Vuforia;

public class WorldReferenceListener : MonoBehaviour, IInputClickHandler {

    public ImageTargetBehaviour referenceWorldTarget;
    public Renderer render;
    public SpriteRenderer arrow;
    public Color colorOnFree;
    public Color colorOnLock;

    void Awake() {
       SetColor(colorOnFree);
    }

    public void OnInputClicked(InputClickedEventData eventData) {
        referenceWorldTarget.enabled = !referenceWorldTarget.enabled;
        SetColor(referenceWorldTarget.enabled ? colorOnFree : colorOnLock);
    }

    void SetColor(Color color) {
        render.material.color = color;
        arrow.color = color;
    }
}
