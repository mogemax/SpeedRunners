using UnityEngine;

public class StaminaRechargeZone : MonoBehaviour
{
    [Tooltip("Stamina recargada por segundo mientras el jugador esté dentro")]
    public float rechargeRate = 30f;

    private void OnTriggerStay2D(Collider2D col)
    {
        var stamina = col.attachedRigidbody != null
            ? col.attachedRigidbody.GetComponent<PlayerStamina>()
            : col.GetComponentInParent<PlayerStamina>();

        if (stamina != null)
            stamina.Recharge(rechargeRate * Time.fixedDeltaTime);
    }
}
