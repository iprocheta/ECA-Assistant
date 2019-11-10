using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//parent class for all watson services
public class WatsonCommon : MonoBehaviour
{
    [SerializeField]
    internal string serviceUrl;
    
    [SerializeField]
    internal string iamAPIKey;
   
    
    //protected virtual IEnumerator CreateService(); todo


}
