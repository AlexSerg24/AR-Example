using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class LevelChanger : MonoBehaviour
{
    // Start is called before the first frame update
    public Animator animator;
    public int levelToLoad;

    // Update is called once per frame
    void Update()
    {
        
    }

    public void FadeToLevel (int levelIndex)
    {
        levelToLoad = levelIndex;
        animator.SetTrigger("FadeOut");
    }

    public void OnFadeComplete()
    {
        animator.SetTrigger("FadeOut");
        //SceneManager.LoadScene(levelToLoad);
    }

    public void OnIntroComplete()
    {
        animator.SetTrigger("EndIntroTrigger");
    }

    public void FadeToNextLevel()
    {
        //FadeToLevel(SceneManager.GetActiveScene().buildIndex + 1);
        SceneManager.LoadScene("SampleScene");
    }
}
