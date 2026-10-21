/*using UnityEngine;
using System.Collections;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class Pause : MonoBehaviour
{

	// Use this for initialization
	public GameObject PauseMen;


	public string menuLevelName = "Home";

	public Text loadingText;
	// Update is called once per frame
	public void Pausing()
	{
		Time.timeScale = 0f;
		//AdsControl.Instance.showAds ();
		PauseMen.SetActive (true);
	}

	public void Resume ()
	{
		Time.timeScale = 1f;
		PauseMen.SetActive (false);
	}

	public void Retry ()
	{
		Time.timeScale = 1f;
		SceneManager.LoadScene (SceneManager.GetActiveScene().name);
	}

	public void Exit ()
	{
		//loadingText.text = "Please Wait ...";
		Time.timeScale = 1f;
		//SceneManager.LoadScene(menuLevelName);
		SceneManager.LoadScene("Home");
	}

}
*/

/*using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class Pause : MonoBehaviour
{
    public GameObject PauseMen;
    public string menuLevelName = "Home";
    public Text loadingText;

    public float bounceTime = 0.5f; // Duration for bounce animation

    private Vector3 originalScale;

    void Start()
    {
        // Store the original scale of the pause panel
        originalScale = PauseMen.transform.localScale;

        // Hide the panel and reset scale
        PauseMen.SetActive(false);
        PauseMen.transform.localScale = Vector3.zero;
    }

    public void Pausing()
    {
        Time.timeScale = 0f;

        PauseMen.SetActive(true);
        PauseMen.transform.localScale = Vector3.zero; // Start from small

        LeanTween.scale(PauseMen, originalScale, bounceTime)
                 .setEase(LeanTweenType.easeOutBack);
    }

    public void Resume()
    {
        LeanTween.scale(PauseMen, Vector3.zero, bounceTime)
                 .setEase(LeanTweenType.easeInBack)
                 .setOnComplete(() =>
                 {
                     PauseMen.SetActive(false);
                     Time.timeScale = 1f;
                 });
    }

    public void Retry()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public void Exit()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene("Home");
    }
}
*/

using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;

public class Pause : MonoBehaviour
{
    public GameObject PauseMen;
    public Button pauseButton;
    public string menuLevelName = "Home";
    // public Text loadingText;

    public float bounceTime = 0.5f; // Duration of bounce effect

    private Vector3 originalScale;

    void Start()
    {
        pauseButton.gameObject.SetActive(true);
        pauseButton.onClick.AddListener(Pausing);
        // Store the original scale of the panel
        originalScale = PauseMen.transform.localScale;

        // Hide panel and reset scale
        PauseMen.SetActive(false);
        PauseMen.transform.localScale = Vector3.zero;
    }

    public void Pausing()
    {
        // Activate and reset panel scale
        PauseMen.SetActive(true);
        PauseMen.transform.localScale = Vector3.zero;
        AudioManager.instance.PlayButtonClickSound();

        // Animate with LeanTween using real-time
        LeanTween.scale(PauseMen, originalScale, bounceTime)
                 .setEase(LeanTweenType.easeOutBack)
                 .setIgnoreTimeScale(true);

        StartCoroutine(PauseAfterDelay());
    }

    private IEnumerator PauseAfterDelay()
    {
        yield return new WaitForSecondsRealtime(bounceTime);
        Time.timeScale = 0f;
    }

    public void Resume()
    {
        AudioManager.instance.PlayButtonClickSound();
        LeanTween.scale(PauseMen, Vector3.zero, bounceTime)
                 .setEase(LeanTweenType.easeInBack)
                 .setIgnoreTimeScale(true)
                 .setOnComplete(() =>
                 {
                     PauseMen.SetActive(false);
                     Time.timeScale = 1f;
                 });
    }

    public void Retry()
    {
        AudioManager.instance.PlayButtonClickSound();
        LeanTween.scale(PauseMen, Vector3.zero, bounceTime)
                 .setEase(LeanTweenType.easeInBack)
                 .setIgnoreTimeScale(true)
                 .setOnComplete(() =>
                 {
                     PauseMen.SetActive(false);
                     Time.timeScale = 1f;
                     SceneManager.LoadScene(SceneManager.GetActiveScene().name);
                 });
    }

    public void Exit()
    {
        AudioManager.instance.PlayButtonClickSound();
        Time.timeScale = 1f;
        SceneManager.LoadScene("Home");
    }
}


/*using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;

public class Pause : MonoBehaviour
{
    public GameObject PauseMen;
    public GameObject YouWinMenu;
    public GameObject YouLostMenu;
    public GameObject NewHighScoreMenu;

    public string menuLevelName = "Home";
    public Text loadingText;

    public float bounceTime = 0.5f; // Duration of bounce effect

    private Vector3 originalScale;

    void Start()
    {
        // Store the original scale (assume all menus have same design scale)
        originalScale = PauseMen.transform.localScale;

        // Hide and reset scale of all panels
        HideAndReset(PauseMen);
        HideAndReset(YouWinMenu);
        HideAndReset(YouLostMenu);
        HideAndReset(NewHighScoreMenu);
    }

    void HideAndReset(GameObject panel)
    {
        if (panel != null)
        {
            panel.SetActive(false);
            panel.transform.localScale = Vector3.zero;
        }
    }

    void PopIn(GameObject panel)
    {
        if (panel != null)
        {
            panel.SetActive(true);
            panel.transform.localScale = Vector3.zero;

            LeanTween.scale(panel, originalScale, bounceTime)
                     .setEase(LeanTweenType.easeOutBack)
                     .setIgnoreTimeScale(true);
        }
    }

    public void Pausing()
    {
        PopIn(PauseMen);
        StartCoroutine(PauseAfterDelay());
    }

    private IEnumerator PauseAfterDelay()
    {
        yield return new WaitForSecondsRealtime(bounceTime);
        Time.timeScale = 0f;
    }

    public void Resume()
    {
        PopIn(YouWinMenu);
        PopIn(YouLostMenu);
        PopIn(NewHighScoreMenu);

        LeanTween.scale(PauseMen, Vector3.zero, bounceTime)
                 .setEase(LeanTweenType.easeInBack)
                 .setIgnoreTimeScale(true)
                 .setOnComplete(() =>
                 {
                     PauseMen.SetActive(false);
                     Time.timeScale = 1f;
                 });
    }

    public void Retry()
    {
        PopIn(YouWinMenu);
        PopIn(YouLostMenu);
        PopIn(NewHighScoreMenu);

        LeanTween.scale(PauseMen, Vector3.zero, bounceTime)
                 .setEase(LeanTweenType.easeInBack)
                 .setIgnoreTimeScale(true)
                 .setOnComplete(() =>
                 {
                     PauseMen.SetActive(false);
                     Time.timeScale = 1f;
                     SceneManager.LoadScene(SceneManager.GetActiveScene().name);
                 });
    }

    public void Exit()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene("Home");
    }
}
*/