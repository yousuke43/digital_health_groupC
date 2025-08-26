using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MenuButtons : MonoBehaviour
{
    // Start is called before the first frame update
    public GameObject Menu;
    void Start()
    {
        if(Menu != null)
        {
            Menu.SetActive(false);
        }
    }

   public void onClickHandler(){
        if(Menu != null)
        {
            Menu.SetActive(!Menu.activeSelf);
        }
    }

 
}
