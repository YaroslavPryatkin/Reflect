using UnityEngine;

public class ExplanationTextTriggerController : TriggerController
{
    [SerializeField] private string explanationText="";
    
    protected override void Entered()
    {
        ExplanationTextController.Activate(explanationText);
    }
    
    protected override void Exited()
    {
        ExplanationTextController.Deactivate();
    }
}
