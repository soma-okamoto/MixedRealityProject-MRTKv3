using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneTransition : MonoBehaviour
{
    public string targetSceneName = "main";

    private bool armed = false;
    private bool alreadyTransitioned = false;

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Hand"))
        {
            armed = true;
            Debug.Log("Scene Transition Button: Ready");
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!armed) return;
        if (alreadyTransitioned) return;

        if (other.CompareTag("Hand"))
        {
            alreadyTransitioned = true;
            Debug.Log("Scene Transition: " + targetSceneName);
            SceneManager.LoadScene(targetSceneName);
        }
    }
}