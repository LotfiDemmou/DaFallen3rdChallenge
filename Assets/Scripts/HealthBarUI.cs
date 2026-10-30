using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Health bar for a UI Canvas.
///
/// SETUP
/// 1. Create a Canvas (UI > Canvas). Under it add two UI > Image objects, one on top of the other:
///      - "Trail" (optional): the slow-draining bar behind, e.g. white or dark red
///      - "Fill": the main bar, e.g. red
///    For each: Image Type = Filled, Fill Method = Horizontal, Fill Origin = Left.
///    (Both need a sprite assigned for the Filled type to appear; any white square works.)
/// 2. Put this script on the Canvas (or any object), then drag Fill and Trail into the fields.
/// 3. Health defaults to the object tagged "Player"; or drag the player in.
/// </summary>
public class HealthBarUI : MonoBehaviour
{
    public Health health;               // defaults to the player
    public Image fill;                  // instant bar
    public Image trail;                 // optional: drains slowly behind the fill after damage
    public float trailSpeed = 0.5f;     // fraction of the bar per second

    float shown = 1f;

    void Start()
    {
        if (!health)
        {
            var p = GameObject.FindGameObjectWithTag("Player");
            if (p) health = p.GetComponentInChildren<Health>();
        }
        if (!health)
        {
            Debug.LogWarning("HealthBarUI: no Health found. Assign it or tag the player 'Player'.", this);
            return;
        }
        shown = health.Normalized;
    }

    void Update()
    {
        if (!health) return;

        float target = health.Normalized;
        if (fill) fill.fillAmount = target;

        if (trail)
        {
            // heals snap up; damage drains slowly
            shown = target > shown ? target : Mathf.MoveTowards(shown, target, trailSpeed * Time.deltaTime);
            trail.fillAmount = shown;
        }
    }
}
