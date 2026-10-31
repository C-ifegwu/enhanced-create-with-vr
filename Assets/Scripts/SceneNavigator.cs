using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Moves between scenes with a fade to black. Each scene contains its own copy of the XR rig prefab,
/// so after a transition the rig, input actions and interactors are created fresh and keep working.
/// </summary>
public class SceneNavigator : MonoBehaviour
{
    [Tooltip("Canvas group of a full screen black image that is parented to the camera.")]
    public CanvasGroup fadeGroup;
    [Tooltip("Seconds used for each fade.")]
    public float fadeDuration = 0.6f;

    private bool loading;

    private void Start()
    {
        // Fade in from black when the scene starts.
        if (fadeGroup != null)
        {
            fadeGroup.alpha = 1f;
            StartCoroutine(Fade(0f));
        }
    }

    /// <summary>Fades out, loads the scene with the given name and fades back in once it starts.</summary>
    /// <param name="sceneName">Name of a scene that is listed in the Build Settings.</param>
    public void GoToScene(string sceneName)
    {
        if (!loading) StartCoroutine(LoadRoutine(sceneName));
    }

    private IEnumerator LoadRoutine(string sceneName)
    {
        loading = true;
        yield return Fade(1f);
        AsyncOperation op = SceneManager.LoadSceneAsync(sceneName);
        while (!op.isDone) yield return null;
    }

    private IEnumerator Fade(float target)
    {
        if (fadeGroup == null) yield break;
        float start = fadeGroup.alpha;
        for (float t = 0f; t < fadeDuration; t += Time.unscaledDeltaTime)
        {
            fadeGroup.alpha = Mathf.Lerp(start, target, t / fadeDuration);
            yield return null;
        }
        fadeGroup.alpha = target;
    }
}
