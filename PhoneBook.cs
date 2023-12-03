using Newtonsoft.Json;

namespace RetroModemSim
{
    /*************************************************************************************************************/
    /// <summary>
    /// Allows shortcut references to longer remote destinations.
    /// </summary>
    /*************************************************************************************************************/
    public class PhoneBook
    {
        Dictionary<string, string> phoneBook;
        const string FILE_NAME = "phonebook.json";

        /*************************************************************************************************************/
        /// <summary>
        /// Constructor.
        /// </summary>
        /*************************************************************************************************************/
        public PhoneBook()
        {
            try
            {
                phoneBook = JsonConvert.DeserializeObject<Dictionary<string, string>>(File.ReadAllText(FILE_NAME));
            }
            catch
            {
                phoneBook = new Dictionary<string, string>();
            }
        }

        /*************************************************************************************************************/
        /// <summary>
        /// Saves a new entry to the phonebook.
        /// </summary>
        /// <param name="key">The shortcut to use to reference the value.</param>
        /// <param name="value">The full remote destination string.</param>
        /*************************************************************************************************************/
        public void AddEntry(string key, string value)
        {
            phoneBook.Add(key, value);
            SavePhonebook();
        }

        /*************************************************************************************************************/
        /// <summary>
        /// Deletes an entry from the phonebook.
        /// </summary>
        /// <param name="key">The key to delete.</param>
        /*************************************************************************************************************/
        public void DeleteEntry(string key)
        {
            phoneBook.Remove(key);
            SavePhonebook();
        }

        /*************************************************************************************************************/
        /// <summary>
        /// Returns an entry in the phonebook, or null if the phonebook does not contain a matching key.
        /// </summary>
        /// <param name="key">The key to look up.</param>
        /// <returns>An entry in the phonebook, or null if the phonebook does not contain a matching key.</returns>
        /*************************************************************************************************************/
        public string GetEntry(string key)
        {
            return phoneBook.GetValueOrDefault(key, null);
        }

        /*************************************************************************************************************/
        /// <summary>
        /// Saves the phone book to disk.
        /// </summary>
        /*************************************************************************************************************/
        public void SavePhonebook()
        {
            File.WriteAllText(FILE_NAME, JsonConvert.SerializeObject(phoneBook));
        }

        /*************************************************************************************************************/
        /// <summary>
        /// Gest a copy of the phone book.
        /// </summary>
        /// <returns>A copy of the phone book.</returns>
        /*************************************************************************************************************/
        public IReadOnlyDictionary<string, string> GetContents()
        {
            return phoneBook;
        }
    }
}