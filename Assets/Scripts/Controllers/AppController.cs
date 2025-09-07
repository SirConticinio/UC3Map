using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AppController : MonoBehaviour
{
    public static AppController _instance;
    public GameObject canvasRoot;
    
    [Header("Menus")]
    public GameObject mainMenu;
    public GameObject routeCreateMenu;
    public GameObject routeShowMenu;
    public GameObject routeStepMenu;
    public GameObject roomSelectMenu;
    public GameObject roomShowMenu;
    public GameObject calendarShowMenu;

    // Start is called before the first frame update
    void Awake()
    {
        _instance = this;
    }

    private void Start()
    {
        RefreshLayouts();
    }

    public static void RefreshLayouts()
    {
        StaticUtils.RefreshLayoutGroupsImmediateAndRecursive(_instance.canvasRoot);
    }

    public void DisableMenus()
    {
        mainMenu.SetActive(false);
        routeCreateMenu.SetActive(false);
        routeShowMenu.SetActive(false);
        roomSelectMenu.SetActive(false);
        roomShowMenu.SetActive(false);
        routeStepMenu.SetActive(false);
        calendarShowMenu.SetActive(false);
    }

    public void GoToMainMenu()
    {
        DisableMenus();
        mainMenu.SetActive(true);
    }

    public void GoToRouteCreateMenu()
    {
        DisableMenus();
        routeCreateMenu.SetActive(true);
    }

    public void GoToRoomShowMenu()
    {
        DisableMenus();
        roomShowMenu.SetActive(true);
    }

    public void GoToRoomSelectMenu()
    {
        DisableMenus();
        roomSelectMenu.SetActive(true);
    }

    public void GoToRouteShowMenu()
    {
        DisableMenus();
        routeShowMenu.SetActive(true);
    }

    public void GoToRouteStepMenu()
    {
        DisableMenus();
        routeStepMenu.SetActive(true);
    }

    public void GoToCalendarShowMenu()
    {
        DisableMenus();
        calendarShowMenu.SetActive(true);
    }
}
