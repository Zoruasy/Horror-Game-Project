using System.Collections.Generic;
using UnityEngine;

public class VisibilityController : MonoBehaviour
{
    [SerializeField] private EyeController eye;
    private void OnEnable() => eye.OnEyeToggled += _ => Refresh();
    private void Start() => Refresh();

    private void Refresh()
    {
        RealityObject.FlashActive = !eye.EyeIn;
        foreach (var obj in RealityObject.All) obj.Apply(RealityObject.FlashActive);
    }
}