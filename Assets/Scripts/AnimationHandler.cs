using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AnimationHandler : Manager<AnimationHandler>
{
    private Animator anim;
    // Start is called before the first frame update
    void Start()
    {
        anim = GetComponent<Animator>();
    }
    
    public void PlayTalk()
    {
        anim.SetBool("talk",true);
    }
    
    public void ResetTalk()
    {
        anim.SetBool("talk",false);
    }
    
}
