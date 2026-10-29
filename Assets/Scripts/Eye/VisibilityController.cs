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

    //private readonly List<Renderer> _renderersToHideWithEye = new List<Renderer>();
    //private readonly List<Renderer> _renderersToShowWithEye = new List<Renderer>();

    //private void OnEnable()
    //{
    //    eye.OnEyeToggled += UpdateRenderersVisibilityForEye;
    //}

    //private void OnDisable()
    //{
    //    eye.OnEyeToggled -= UpdateRenderersVisibilityForEye;
    //}

    //private void Start()
    //{
    //    Debug.Log("VisibilityController started");
    //    UpdateRenderersLists();
    //    Debug.Log("Renderers to show with eye: " + _renderersToShowWithEye.Count);
    //    Debug.Log("Renderers to hide with eye: " + _renderersToHideWithEye.Count);
    //    UpdateRenderersVisibilityForEye(eye.EyeIn);
    //}

    //public void UpdateRenderersLists()
    //{
    //    _renderersToShowWithEye.Clear();
    //    _renderersToHideWithEye.Clear();

    //    Renderer[] renderers = FindObjectsByType<Renderer>(FindObjectsSortMode.None);
    //    foreach (Renderer renderer in renderers)
    //    {
    //        if (renderer.gameObject.CompareTag("ShowWithEye"))
    //        {
    //            _renderersToShowWithEye.Add(renderer);
    //        }
    //        else if (renderer.gameObject.CompareTag("HideWithEye"))
    //        {
    //            _renderersToHideWithEye.Add(renderer);
    //        }
    //    }
    //}

    //private void UpdateRenderersVisibilityForEye(bool IsWearingEye)
    //{
    //    if (_renderersToShowWithEye.Count > 0)
    //    {
    //        foreach (Renderer renderer in _renderersToShowWithEye)
    //        {
    //            renderer.enabled = IsWearingEye;
    //        }
    //    }

    //    if (_renderersToHideWithEye.Count > 0)
    //    {
    //        foreach (Renderer renderer in _renderersToHideWithEye)
    //        {
    //            renderer.enabled = !IsWearingEye;
    //        }
    //    }
    //}
}