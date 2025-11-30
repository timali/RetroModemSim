using Newtonsoft.Json;

namespace RetroModemSim
{
    /// <summary>
    /// Simple support for application-specific settings.
    /// </summary>
    public static class AppSettings
    {
        const string tmpFileExtension = ".tmp";
        const string directoryName = "settings/";
        static string pathToDirectory;

        /*************************************************************************************************************/
        /// <summary>
        /// Must be called before any other methods are used.
        /// </summary>
        /// <param name="path">The path to the settings directory, or "." to use the current directory.</param>
        /*************************************************************************************************************/
        public static void Initialize(string path)
        {
            if (!path.EndsWith(Path.DirectorySeparatorChar))
            {
                path = path + Path.DirectorySeparatorChar;
            }

            pathToDirectory = path;
            Directory.CreateDirectory(path + directoryName);
        }

        /*************************************************************************************************************/
        /// <summary>
        /// Gets the file name for the given setting.
        /// </summary>
        /// <param name="setting">The setting to use.</param>
        /// <returns>The file name for that setting.</returns>
        /*************************************************************************************************************/
        static string GetFileName(string setting, bool tempFile = false)
        {
            if (tempFile)
            {
                return GetFileName(setting) + tmpFileExtension;
            }
            else
            {
                return pathToDirectory + directoryName + setting;
            }
        }

        /*************************************************************************************************************/
        /// <summary>
        /// Saves the given setting to disk.
        /// </summary>
        /// <param name="setting">Which setting to save.</param>
        /// <param name="settingData">The data to save for the setting.</param>
        /*************************************************************************************************************/
        static public void Save(string setting, string settingData)
        {
            // First, save a copy to a temporary file.
            File.WriteAllText(GetFileName(setting, true), settingData);

            // Next, delete the original file.
            File.Delete(GetFileName(setting));

            // Finally, rename the temporary file to the original file.
            File.Move(GetFileName(setting, true), GetFileName(setting));
        }

        /*************************************************************************************************************/
        /// <summary>
        /// Saves the given setting to disk.
        /// </summary>
        /// <param name="setting">Which setting to save.</param>
        /// <param name="obj">The object to save for the setting.</param>
        /*************************************************************************************************************/
        static public void Save(string setting, object obj)
        {
            Save(setting, JsonConvert.SerializeObject(obj, Formatting.None));
        }

        /*************************************************************************************************************/
        /// <summary>
        /// Like Save(), but catches all exceptions and silently fails upon error.
        /// </summary>
        /// <param name="setting">Which setting to save.</param>
        /// <param name="settingData">The data to save for the setting.</param>
        /*************************************************************************************************************/
        static public void SaveNoThrow(string setting, string settingData)
        {
            try
            {
                Save(setting, settingData);
            }
            catch { }
        }

        /*************************************************************************************************************/
        /// <summary>
        /// Like Save(), but catches all exceptions and silently fails upon error.
        /// </summary>
        /// <param name="setting">Which setting to save.</param>
        /// <param name="obj">The object to save for the setting.</param>
        /*************************************************************************************************************/
        static public void SaveNoThrow(string setting, object obj)
        {  
            try
            {
                Save(setting, obj);
            }
            catch { }
        }

        /*************************************************************************************************************/
        /// <summary>
        /// Loads the given setting from disk and returns the setting's value.
        /// </summary>
        /// <param name="setting">Which setting to load.</param>
        /// <remarks>
        /// If the given setting does not exist, null is returned. Exceptions can be thrown for other access errors.
        /// </remarks>
        /*************************************************************************************************************/
        static public string Load(string setting)
        {
            try
            {
                // First, try to load the settings from the temporary file, in case we were interrupted after writing
                // the temporary file, but before renaming it to the proper name.
                string settingValue = File.ReadAllText(GetFileName(setting, true));

                // We loaded the temporary file, so finish the write by deleting the original (in case it still exists),
                // and then renaming it.
                File.Delete(GetFileName(setting));
                File.Move(GetFileName(setting, true), GetFileName(setting));

                return settingValue;
            }
            catch(FileNotFoundException)
            {
                // The temporary file does not exist, which is normal, so take no action.
            }
            catch
            {
                // The temporary file exists, but it must be invalid, so delete it.
                File.Delete(GetFileName(setting, true));
            }

            try
            {
                return File.ReadAllText(GetFileName(setting));
            }
            catch (FileNotFoundException)
            {
                return null;
            }
        }

        /*************************************************************************************************************/
        /// <summary>
        /// Loads the given setting from disk and returns the setting's value.
        /// </summary>
        /// <typeparam name="T">The type of object to load. (must be serializable with JSON.</typeparam>
        /// <param name="setting">Which setting to load.</param>
        /// <remarks>
        /// If the given setting does not exist, null is returned. Exceptions can be thrown for other access errors.
        /// </remarks>
        /*************************************************************************************************************/
        static public T Load<T>(string setting)
        {
            return JsonConvert.DeserializeObject<T>(Load(setting));
        }

        /*************************************************************************************************************/
        /// <summary>
        /// Like Load(), but catches all exceptions and returns null upon any error.
        /// </summary>
        /// <param name="setting">Which setting to load.</param>
        /*************************************************************************************************************/
        static public string LoadNoThrow(string setting)
        {
            try
            {
                return Load(setting);
            }
            catch
            {
                return null;
            }
        }

        /*************************************************************************************************************/
        /// <summary>
        /// Loads the given setting from disk and returns the setting's value, or the default value on any error.
        /// </summary>
        /// <typeparam name="T">The type of object to load. (must be serializable with JSON.</typeparam>
        /// <param name="setting">Which setting to load.</param>
        /// <remarks>
        /// All exceptions are caught, and the T's default value is returned on error.
        /// </remarks>
        /*************************************************************************************************************/
        static public T LoadNoThrow<T>(string setting)
        {
            try
            {
                return JsonConvert.DeserializeObject<T>(Load(setting));
            }
            catch
            {
                return default;
            }
        }

        /*************************************************************************************************************/
        /// <summary>
        /// Deletes the given setting.
        /// </summary>
        /// <remarks>
        /// If it does not exist, or on any other error, an exception is thrown.
        /// </remarks>
        /// <param name="setting">The setting to delete.</param>
        /*************************************************************************************************************/
        static public void Delete(string setting)
        {
            File.Delete(GetFileName(setting));
        }

        /*************************************************************************************************************/
        /// <summary>
        /// Deletes the given setting, but catches all exceptions.
        /// </summary>
        /// <param name="setting">The setting to delete.</param>
        /*************************************************************************************************************/
        static public void DeleteNoThrow(string setting)
        {
            try
            {
                File.Delete(GetFileName(setting));
            }
            catch { }
        }
    }
}