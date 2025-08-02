using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;

public class LoadingScreenController : MonoBehaviour
{
    [Header("UI References")]
    public GameObject progressBarFilled;
    public Text loadingText;
    
    [Header("Loading Settings")]
    public float minLoadingTime = 2f;
    public float maxLoadingTime = 4f;
    public string nextSceneName = "ParkScene_v01";
    
    private Image fillImage;
    
    private void Start()
    {
        // Find the Fill child object and get its Image component
        if (progressBarFilled != null)
        {
            Transform fillTransform = progressBarFilled.transform.Find("Fill");
            if (fillTransform != null)
            {
                fillImage = fillTransform.GetComponent<Image>();
            }
            else
            {
                Debug.LogWarning("Fill child object not found in ProgressBarFilled GameObject!");
            }
        }
        
        StartCoroutine(LoadingSequence());
    }
    
    private IEnumerator LoadingSequence()
    {
        float loadingTime = Random.Range(minLoadingTime, maxLoadingTime);
        float elapsedTime = 0f;
        
        // Reset progress bar fill
        if (fillImage != null)
        {
            fillImage.fillAmount = 0f;
        }
        
        // Loading animation
        while (elapsedTime < loadingTime)
        {
            elapsedTime += Time.deltaTime;
            float progress = elapsedTime / loadingTime;
            
            // Update progress bar fill amount
            if (fillImage != null)
            {
                fillImage.fillAmount = progress;
            }
            
            // Update loading text
            if (loadingText != null)
            {
                int dots = Mathf.FloorToInt(elapsedTime * 2) % 4;
                loadingText.text = "Loading" + new string('.', dots);
            }
            
            yield return null;
        }
        
        // Ensure progress bar is full
        if (fillImage != null)
        {
            fillImage.fillAmount = 1f;
        }
        
        // Wait a moment before transitioning
        yield return new WaitForSeconds(0.5f);
        
        // Load next scene
        SceneManager.LoadScene(nextSceneName);
    }
} 