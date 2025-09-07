using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using SFB;
using SimpleFileBrowser;
using UnityEngine;
using UnityEngine.Events;

// adapted some logic from my Revo project
public class FileManager
{
    public static void CopyFile(string origin, string target) {
        Debug.Log($"Copying {origin} to {target}");
        FileBrowserHelpers.CopyFile(origin, target);
    }
    
    public static string GetFileName(string path)
    {
        return Application.isMobilePlatform && FileBrowserHelpers.FileExists(path) ? FileBrowserHelpers.GetFilename(path) : Path.GetFileName(path);
    }
    
    public static string GetExtension(string path)
    {
        return Path.GetExtension(GetFileName(path)).Replace(".","");
    }
    
    public static string[] GetAllowedExtensions(FILETYPE type)
    {
        switch (type)
        {
            case FILETYPE.ZIP: return new[] { "zip" };
            case FILETYPE.ZIP_AND_JSON_AND_ICS: return new[] { "zip", "json", "ics" };
            case FILETYPE.ICS: return new[] { "ics" };
            case FILETYPE.OBJ: return new[] { "obj" };
            default: return new[] { "*" };
        }
    }
    
    public static bool IsValidExtension(string path, FILETYPE type)
    {
        string myExtension = GetExtension(path).ToLowerInvariant();
        return type == FILETYPE.ANY || GetAllowedExtensions(type).Any(ext => ext.Equals(myExtension));
    }
    
    private static ExtensionFilter[] GetPcFilters(FILETYPE type)
    {
        switch (type)
        {
            case FILETYPE.ZIP: return new[] { new ExtensionFilter("ZIP files", GetAllowedExtensions(type)) };
            case FILETYPE.ZIP_AND_JSON_AND_ICS: return new[] { new ExtensionFilter("ZIP, JSON and ICS files", GetAllowedExtensions(type)) };
            case FILETYPE.OBJ: return new[] { new ExtensionFilter("OBJ files", GetAllowedExtensions(type)) };
            case FILETYPE.ICS: return new[] { new ExtensionFilter("Calendar files", GetAllowedExtensions(type)) };
            default: return new[] { new ExtensionFilter("Any file", GetAllowedExtensions(type)) };
        }
    }
    
    private static string[] GetAndroidFilters(FILETYPE type)
    {
        switch (type)
        {
            case FILETYPE.ZIP: return new[] { "application/zip" };
            default: return new string[] { }; // we allow all, which is also needed to load OBJ files
        }
    }
    
    public static void RequestFile(string title, FILETYPE type, UnityAction<string> callback)
    {
        AppController._instance.StartCoroutine(_ChooseFiles(title, type, callback));
    }

    private static IEnumerator _ChooseFiles(string title, FILETYPE type, UnityAction<string> callback)
    {
        yield return new WaitForSeconds(0.1f);
        NativeFilePicker.FilePickedCallback innerCallback = path =>
        {
            if (string.IsNullOrEmpty(path))
            {
                return;
            }
            
            // we check if it's a valid extension
            if (IsValidExtension(path, type))
            {
                callback?.Invoke(path);
            }
            else
            {
                Debug.Log($"Unsupported extension! {GetExtension(path).ToUpperInvariant()}");
            }
        };
        
        if (Application.isMobilePlatform)
        {
            ChooseFilesAndroid(GetAndroidFilters(type),type,innerCallback);
        }
        else
        {
            string[] paths = ChooseFilesPc(title, "", GetPcFilters(type), false);
            if (paths.Length > 0)
            {
                innerCallback.Invoke(paths[0]);
            }
        }
    }

    private static void ChooseFilesAndroid(string[] allowedFileTypes, FILETYPE type, NativeFilePicker.FilePickedCallback callback)
    {
        NativeFilePicker.PickFile(callback, allowedFileTypes);
    }

    private static string[] ChooseFilesPc(string title, string directory, ExtensionFilter[] extensions, bool multiselect)
    {
        return StandaloneFileBrowser.OpenFilePanel(title, directory, extensions, multiselect);
    }
    
    public static void UnzipFile(string zipPath, string extractedPath)
    {
        try
        {
            ZipFile.ExtractToDirectory(zipPath, extractedPath);
        }
        catch (Exception exception)
        {
            // Apparently there's a few Android devices that don't fully support ZipFile.ExtractToDirectory,
            // so we do the extraction manually
            Debug.Log("IO Exception thrown! Using fallback method");
            FallbackUnzipAnimation(zipPath,extractedPath);
        }
    }

    public static void UnzipEntry(ZipArchiveEntry entry, string directory)
    {
        // First we get the file destination path
        string destinationFileName = Path.GetFullPath(Path.Combine(directory, entry.FullName));
        
        // Then we delete it if it already exists
        if (File.Exists(destinationFileName))
        {
            File.Delete(destinationFileName);
        }

        // Now we create the directory where the item should be, in case it's not already created
        string finalDirectory = Path.GetDirectoryName(destinationFileName);
        if (finalDirectory != null && !Directory.Exists(finalDirectory))
        {
            Directory.CreateDirectory(finalDirectory);
        }
        
        // And finally, we write the file
        using (Stream destination = File.Open(destinationFileName, FileMode.Create, FileAccess.Write, FileShare.None))
        {
            using (var entryStream = entry.Open())
            {
                entryStream.CopyTo(destination);
            }
        }
    }

    private static void FallbackUnzipAnimation(string zipPath, string extractedPath)
    {
        using (ZipArchive archive = ZipFile.OpenRead(zipPath))
        {
            foreach (var entry in archive.Entries)
            {
                UnzipEntry(entry,extractedPath);
            }
        }
    }
}
