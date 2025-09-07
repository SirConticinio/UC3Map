using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using Newtonsoft.Json.Linq;
using UnityEngine;

// adapted some logic from my Revo project
public class SaveController : MonoBehaviour
{
    // save folders
    public static string SAVE_FOLDER;
    public static string SAVE_FOLDER_BUNDLES;
    public static string SAVE_FOLDER_BUNDLES_FLOORS;
    public static string SAVE_FOLDER_BUNDLES_CAMPUS;
    public static string SAVE_FOLDER_BUNDLES_CALENDAR;
    public static string TEMP_GENERAL;
    public static string TEMP_ZIP;

    public static SaveController _instance;

    private void Awake()
    {
        _instance = this;
        SAVE_FOLDER = Application.persistentDataPath;
        SAVE_FOLDER_BUNDLES = Application.persistentDataPath + "/Bundles/";
        SAVE_FOLDER_BUNDLES_FLOORS = Application.persistentDataPath + "/Bundles/Floors";
        SAVE_FOLDER_BUNDLES_CAMPUS = Application.persistentDataPath + "/Bundles/Campus";
        SAVE_FOLDER_BUNDLES_CALENDAR = Application.persistentDataPath + "/Calendars/";
        TEMP_GENERAL = Application.persistentDataPath + "/Temp/";
        TEMP_ZIP = Application.persistentDataPath + "/Temp/Zip/";
        
        CreateFolder(SAVE_FOLDER_BUNDLES);
        CreateFolder(SAVE_FOLDER_BUNDLES_FLOORS);
        CreateFolder(SAVE_FOLDER_BUNDLES_CAMPUS);
        CreateFolder(SAVE_FOLDER_BUNDLES_CALENDAR);
        RecreateFolder(TEMP_GENERAL);
        RecreateFolder(TEMP_ZIP);
        CreateNoMedia();
    }
    
    public static void RecreateFolder(string path)
    {
        DeleteFolder(path);
        CreateFolder(path);
    }

    public static void CreateFolder(string path)
    {
        if (!Directory.Exists(path))
        {
            Directory.CreateDirectory(path);
        }
    }
    
    public static void DeleteFolder(string path)
    {
        if (Directory.Exists(path))
        {
            try
            {
                Directory.Delete(path,true);
            }
            catch (Exception _)
            {
                // ignored
            }
        }
    }

    public static void MoveFolder(string originalPath, string newPath)
    {
        if (Directory.Exists(originalPath))
        {
            try
            {
                DeleteFolder(newPath);
                Directory.Move(originalPath, newPath);
            }
            catch (Exception _)
            {
                // ignored
            }
        }
    }
    
    private static void CreateNoMedia()
    {
        // we use this to prevent Android media managers from scanning files in our folder
        string path = SAVE_FOLDER + "/.nomedia";
        if (Directory.Exists(SAVE_FOLDER) && !File.Exists(path))
        {
            File.WriteAllText(path, "");
        }
    }

    public string ImportFloorBundle(string zipPath)
    {
        // we import it to our folder space and extract it
        string extracted = ImportAndExtractZip(zipPath);
        
        // find ID and move to proper folder
        try
        {
            string infoPath = extracted + Path.DirectorySeparatorChar + "map.json";
            JObject jObject = JObject.Parse(File.ReadAllText(infoPath));
            string newFolder = SAVE_FOLDER_BUNDLES_FLOORS + Path.DirectorySeparatorChar + jObject["id"];
            MoveFolder(extracted, newFolder);
            return newFolder;
        }
        catch (Exception e)
        {
            Debug.LogError(e);
            return null;
        }
    }
    
    public string ImportCampusBundle(string bundlePath)
    {
        // we import it to our folder space and extract it
        string newCampusPath = SAVE_FOLDER_BUNDLES_CAMPUS + Path.DirectorySeparatorChar + FileManager.GetFileName(bundlePath);
        TryDeleteFile(newCampusPath);
        
        FileManager.CopyFile(bundlePath, newCampusPath);
        return newCampusPath;
    }

    public string ImportCalendar(string calendarPath)
    {
        // we import it to our folder space and extract it
        string newPath = SAVE_FOLDER_BUNDLES_CALENDAR + Path.DirectorySeparatorChar + "calendar.ics";
        TryDeleteFile(newPath);
        FileManager.CopyFile(calendarPath, newPath);
        return newPath;
    }

    private void TryDeleteFile(string path)
    {
        if (File.Exists(path))
        {
            try
            {
                File.Delete(path);
            }
            catch (Exception _)
            {
                // ignored
            }
        }
    }


    public string ImportAndExtractZip(string zipPath, string fullExtractDir = null)
    {
        // first we copy and check the size
        // we need to copy it to our folder because Android doesn't play nice with files outside our scope
        string newZip = TEMP_GENERAL + Path.GetFileName(zipPath);
        FileManager.CopyFile(zipPath, newZip);
        if (new FileInfo(newZip).Length < 1024)
        {
            Debug.Log("Null zip! 0kb");
            return null;
        }
        
        // now we extract it
        string extractPath = fullExtractDir ?? TEMP_GENERAL + Path.GetFileNameWithoutExtension(zipPath);
        RecreateFolder(extractPath);
        FileManager.UnzipFile(zipPath, extractPath);
        
        // we can delete the copied zip
        File.Delete(newZip);
        
        return extractPath;
    }
}
