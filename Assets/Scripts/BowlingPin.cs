using UnityEngine;

public class BowlingPin : MonoBehaviour
{
    public bool isFallen = false;

    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.CompareTag("Floor"))
        {
            isFallen = true;
            if (GameManager.Instance != null)
            {
                GameManager.Instance.AddScore(100);
                SoundManager.instance.PlaySFX(SoundManager.instance.hitClip);
            }
        }
    }

}
