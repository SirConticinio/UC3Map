using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using Ical.Net;
using Ical.Net.CalendarComponents;
using Ical.Net.DataTypes;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CalendarController : MonoBehaviour
{
    public static CalendarController _instance;
    
    public TextMeshProUGUI calendarText;
    public Transform contentParent;
    public GameObject calendarEntryPrefab;
    public Calendar loadedCalendar;
    // in reality this should be exported by the map but for testing purposes let's do it like this
    public string defaultEntranceRoomId = "af633275-0f92-4d2f-b5b1-dfad973414e1";
    private DateOnly selectedDate;
    private List<CalendarEvent> selectedEvents = new List<CalendarEvent>();

    void Awake()
    {
        _instance = this;
    }

    private void Start()
    {
        LoadImportedCalendar();
    }

    public void HandleCalendarFile(string path)
    {
        SaveController._instance.ImportCalendar(path);
        LoadImportedCalendar();
    }

    public void LoadImportedCalendar()
    {
        string path = SaveController.SAVE_FOLDER_BUNDLES_CALENDAR + Path.DirectorySeparatorChar + "calendar.ics";
        if (!File.Exists(path))
        {
            return;
        }
        
        string text = File.ReadAllText(path);
        loadedCalendar = Calendar.Load(text);
        if (loadedCalendar == null)
        {
            Debug.LogError("Couldn't load calendar!");
            return;
        }
        
        SelectFirstDay();
    }

    private void SelectFirstDay()
    {
        DateOnly firstDate = DateOnly.MaxValue;
        
        foreach (CalendarEvent calEvent in loadedCalendar.Events)
        {
            CalDateTime dateTime = calEvent.Start;
            if (dateTime != null)
            {
                DateOnly date = dateTime.Date;
                if (date < firstDate)
                {
                    firstDate = date;
                }
            }
        }
        
        SelectDate(firstDate);
    }

    public void SelectNextDate()
    {
        if (loadedCalendar == null)
        {
            return;
        }
        SelectDate(selectedDate.AddDays(1));
    }

    public void SelectPreviousDate()
    {
        if (loadedCalendar == null)
        {
            return;
        }
        SelectDate(selectedDate.AddDays(-1));
    }

    private void SelectDate(DateOnly date)
    {
        Debug.Log("Selected " + date + " as date!");
        selectedDate = date;
        selectedEvents = new List<CalendarEvent>();
        calendarText.text = "Día actual: " + date.ToLongDateString();
        
        StaticUtils.KillChildren(contentParent);
        foreach (CalendarEvent calEvent in loadedCalendar.Events)
        {
            CalDateTime dateTime = calEvent.Start;
            if (dateTime != null && dateTime.Date == selectedDate)
            {
                selectedEvents.Add(calEvent);
                SpawnEvent(calEvent);
            }
        }
    }

    private void SpawnEvent(CalendarEvent calEvent)
    {
        Debug.Log("Spawning event: " + calEvent);
        GameObject spawned = GameObject.Instantiate(calendarEntryPrefab, contentParent);
        
        spawned.transform.Find("TextContainer").Find("Event").GetComponent<TextMeshProUGUI>().text = calEvent.Description;
        string time = calEvent.DtStart != null && calEvent.DtEnd != null
            ? ($"{calEvent.DtStart.Date} | {calEvent.DtStart.Time} - {calEvent.DtEnd.Time}")
            : "";
        spawned.transform.Find("TextContainer").Find("Time").GetComponent<TextMeshProUGUI>().text = time;
        spawned.transform.Find("TextContainer").Find("Place").GetComponent<TextMeshProUGUI>().text = "Lugar: " + calEvent.Location;
        
        spawned.transform.Find("Buttons").Find("Map").GetComponent<Button>().onClick.AddListener(() =>
        {
            RoomData foundRoom = BundleController._instance.FindRoomFromPartialCode(calEvent.Location);
            if (foundRoom != null)
            {
                GameObject model = ModelController._instance.FindSpawnedRoom(null, foundRoom);
                BundleController._instance.SelectRoom(model);
                AppController._instance.GoToMainMenu();
            }
        });
        spawned.transform.Find("Buttons").Find("Route").GetComponent<Button>().onClick.AddListener(() =>
        {
            RoomData targetRoom = BundleController._instance.FindRoomFromPartialCode(calEvent.Location);
            if (targetRoom != null)
            {
                int index = selectedEvents.IndexOf(calEvent);
                RoomData originRoom = index == 0
                    ? BundleController._instance.FindRoom(defaultEntranceRoomId)
                    : BundleController._instance.FindRoomFromPartialCode(selectedEvents[index - 1].Location);
                if (originRoom != null)
                {
                    PathfinderController._instance.CreateRouteFromData(originRoom, targetRoom);
                }
            }
        });
    }
}
