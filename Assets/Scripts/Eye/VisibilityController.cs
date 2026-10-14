using System.Collections.Generic;
using UnityEngine;

public class VisibilityController : MonoBehaviour
{
    private readonly List<Renderer> _renderersToHideWithEye = new List<Renderer>();
    private readonly List<Renderer> _renderersToShowWithEye = new List<Renderer>();
    
    private void Start()
    {
        Debug.Log("VisibilityController started");
       UpdateRenderersLists();
        Debug.Log("Renderers to show with eye: " + _renderersToShowWithEye.Count);
        Debug.Log("Renderers to hide with eye: " + _renderersToHideWithEye.Count);
    }


    public void UpdateRenderersLists()
    {
        Renderer[] renderers = FindObjectsOfType<Renderer>();
        foreach (Renderer renderer in renderers)
        {
            if (renderer.gameObject.CompareTag("ShowWithEye"))
            {
                _renderersToShowWithEye.Add(renderer);
            }
            else if (renderer.gameObject.CompareTag("HideWithEye"))
            {
                _renderersToHideWithEye.Add(renderer);
            }
        }
    }
    
private void Update()
    {

        /*UpdateRenderersVisibilityForEye(true);  // for testing, it simulates whether the player is wearing the eye or not.*/
    }
    
    private void UpdateRenderersVisibilityForEye(bool IsWearingEye)
    {
        if (_renderersToShowWithEye.Count > 0)
        {
            foreach (Renderer renderer in _renderersToShowWithEye)
            {
                renderer.enabled = IsWearingEye;
            }
        }

        if (_renderersToHideWithEye.Count > 0)
        {
            foreach (Renderer renderer in _renderersToHideWithEye)
            {
                renderer.enabled = !IsWearingEye;
            }
        }
    }
    
}
